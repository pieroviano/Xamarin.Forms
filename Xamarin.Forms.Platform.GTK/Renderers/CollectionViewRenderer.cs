using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	/// <summary>
	/// CollectionView on GTK: a <see cref="Gtk.ScrolledWindow"/> over a <see cref="Gtk.Viewport"/>
	/// over a <see cref="Gtk.Box"/> that holds only the <b>visible window</b> of items - one
	/// <see cref="Gtk.EventBox"/> host per materialized item (optionally grouped into per-line boxes
	/// for a <see cref="GridItemsLayout"/>), bracketed by two spacer widgets that stand in for
	/// everything scrolled off either end.
	/// </summary>
	/// <remarks>
	/// <para><b>Virtualized.</b> <c>ItemsSource</c> is turned into a flat list of lightweight
	/// <c>Slot</c> records - no view, no renderer, no widget - and only the slots inside the visible
	/// line window plus <see cref="BufferLines"/> lines of margin on each side are materialized.
	/// Scrolling releases the hosts that left the window into a recycle pool <b>keyed by
	/// DataTemplate</b> and re-binds pooled hosts for the slots that entered it, so a scroll does not
	/// call <c>DataTemplate.CreateContent()</c> at all. Keying by template is not optional: a
	/// <see cref="DataTemplateSelector"/> makes a recycled view reusable only for the template that
	/// produced it.</para>
	///
	/// <para><b>Extents.</b> Virtualization needs a line's extent before the line is materialized,
	/// which the Forms measure API cannot supply without instantiating the view. Lines therefore
	/// carry a measured extent once they have been laid out and a single frozen <i>estimate</i>
	/// (the first item line ever measured, i.e. <see cref="ItemSizingStrategy.MeasureFirstItem"/>
	/// semantics) until then. The estimate is deliberately frozen rather than continuously averaged:
	/// a moving estimate changes the content size on every pass, which changes the allocation, which
	/// re-runs this pass - an oscillation, not a refinement.</para>
	///
	/// <para>Item views are not children of a Forms <see cref="Layout"/>, so nothing lays them out.
	/// This renderer measures and calls <c>view.Layout(...)</c> itself, exactly as
	/// <see cref="CarouselViewRenderer"/> does, and for the same reason sets <c>view.Parent</c>
	/// <b>before</b> <c>view.BindingContext</c> - parenting makes the view inherit the
	/// CollectionView's binding context and would otherwise overwrite the item. On <i>recycle</i> the
	/// parent never changes, so re-binding is only the context assignment - but it must still be
	/// followed by the explicit measure/layout that <c>LayoutLine</c> performs in the same pass, or
	/// the recycled row paints its previous occupant's geometry.</para>
	///
	/// <para>Geometry is only ever mutated from a <see cref="GLib.Idle"/> callback, never from
	/// inside a size-allocate: GTK3 discards resizes queued during allocation and can wedge the
	/// subtree's resize machinery.</para>
	///
	/// <para><b>Widget-tree ownership.</b> Hosts are created detached and the deferred layout pass
	/// re-packs them (<see cref="RepackHosts"/>) whenever the structure or the window changes. That
	/// is what lets one code path serve linear, grid and grouped layouts: the *only* thing that
	/// changes between them is how the flat slot list is cut into lines.</para>
	/// </remarks>
	public class CollectionViewRenderer : ViewRenderer<CollectionView, Gtk.ScrolledWindow>
	{
		enum HostKind
		{
			Item,
			GroupHeader,
			GroupFooter
		}

		/// <summary>
		/// One position in the flattened list - an item, a group header or a group footer. A slot
		/// exists whether or not anything is materialized for it; <see cref="Host"/> is non-null only
		/// while the slot is inside the materialized window.
		/// </summary>
		sealed class Slot
		{
			public object Item;
			public object Group;
			public int GroupIndex = -1;
			public int IndexInGroup = -1;
			public int Index = -1;

			/// <summary>Ordinal among item slots, or -1 for a group header/footer. See RebuildLines.</summary>
			public int ItemIndex = -1;

			public HostKind Kind;

			/// <summary>Resolved up front for group headers/footers only: a null template means the
			/// slot does not exist at all, which cannot be decided lazily. Item templates are resolved
			/// at materialization time so a 10,000-item source costs 10,000 records and nothing else.</summary>
			public DataTemplate Template;

			public ItemHost Host;
		}

		/// <summary>A materialized view + renderer + native host. Recyclable; see <see cref="PoolKey"/>.</summary>
		sealed class ItemHost
		{
			public PoolKey Key;
			public View View;
			public IVisualElementRenderer Renderer;
			public HostBox Host;
			public Slot Slot;

			/// <summary>The no-template fallback <see cref="Label"/> carries no binding, so its text
			/// has to be re-assigned by hand when the host is recycled.</summary>
			public bool IsFallbackLabel;
		}

		/// <summary>
		/// A recycled view is only reusable for the same template <b>and</b> the same kind: an
		/// application may legitimately use one <see cref="DataTemplate"/> for both items and group
		/// headers, and an item host carries a button-press handler a header host must not have.
		/// </summary>
		readonly struct PoolKey : IEquatable<PoolKey>
		{
			public PoolKey(DataTemplate template, HostKind kind)
			{
				Template = template;
				Kind = kind;
			}

			public DataTemplate Template { get; }

			public HostKind Kind { get; }

			public bool Equals(PoolKey other) => ReferenceEquals(Template, other.Template) && Kind == other.Kind;

			public override bool Equals(object obj) => obj is PoolKey other && Equals(other);

			public override int GetHashCode()
			{
				var hash = Template == null
					? 0
					: System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Template);

				return (hash * 397) ^ (int)Kind;
			}
		}

		/// <summary>
		/// A non-item view owned by the CollectionView itself: <c>Header</c>, <c>Footer</c> or
		/// <c>EmptyView</c>. All three are "an object or a template, resolved to one view", so they
		/// share one materialization path rather than three near-copies of it.
		/// </summary>
		sealed class Decoration
		{
			public View View;
			public IVisualElementRenderer Renderer;
			public Gtk.EventBox Host;
		}

		/// <summary>
		/// The native host for one item, group header/footer or decoration.
		/// </summary>
		/// <remarks>
		/// A plain <see cref="Gtk.EventBox"/> reports its child's minimum size, and the child here is
		/// a Forms renderer container whose size is whatever the item was last laid out at. A
		/// <see cref="Gtk.Box"/> asks GTK for the sum of its children's minima, so three hosts still
		/// carrying the width they had at <c>Span=2</c> make this control demand <c>3 x (w/2)</c> as
		/// its <b>minimum</b> width. GTK grows the toplevel to satisfy a minimum, the next layout
		/// pass then derives the item size from the grown viewport, and the control never comes
		/// back: measured as a 540px grid ratcheting to 803px - wider than the screen, with the grid
		/// no longer visible where it had been - intermittently, and only ever caught by a
		/// screenshot, because every geometry assertion passed in the same run.
		///
		/// <para>So a host reports a minimum of zero and lets the explicit size request this
		/// renderer sets on every layout pass be the only minimum it has
		/// (<c>gtk_widget_adjust_size_request</c> folds the request back in with a MAX). A stale
		/// child size can then no longer push anything wider than the pass that set it.</para>
		///
		/// <para>Recycling makes this stricter, not looser: a host arriving from the pool still
		/// carries the previous occupant's child, so without the zero minimum a recycled row could
		/// widen the control on the way in.</para>
		/// </remarks>
		sealed class HostBox : Gtk.EventBox
		{
			protected override void OnGetPreferredWidth(out int minimum_width, out int natural_width)
			{
				base.OnGetPreferredWidth(out minimum_width, out natural_width);
				minimum_width = 0;
			}

			protected override void OnGetPreferredHeight(out int minimum_height, out int natural_height)
			{
				base.OnGetPreferredHeight(out minimum_height, out natural_height);
				minimum_height = 0;
			}
		}

		/// <summary>
		/// One run of slots across the cross axis. Lines stack along the scrolling axis, and the
		/// window this renderer materializes is a range of <i>lines</i>, never of items.
		/// </summary>
		sealed class Line
		{
			public int Start;
			public int Count;

			/// <summary>The line owns the whole cross axis: linear items, and group headers/footers.</summary>
			public bool Spanning;

			/// <summary>Extent along the scrolling axis, valid only while <see cref="Measured"/>.</summary>
			public double Extent;

			public bool Measured;
		}

		static readonly Gdk.Color DefaultSelectionColor = Color.FromHex("#3498DB").ToGtkColor();

		/// <summary>
		/// Lines materialized above and below the visible range. Big enough that every list short
		/// enough to fit a screen and a half is materialized in full - which is what keeps small
		/// lists behaving exactly as they did before virtualization - and small enough that the
		/// materialized set stays bounded at a couple of dozen hosts on a 10,000-item source.
		/// </summary>
		const int BufferLines = 8;

		/// <summary>Recycled hosts kept per template, so a long scroll cannot grow the pool forever.</summary>
		const int MaxPooledPerTemplate = 16;

		readonly List<Slot> _slots = new List<Slot>();
		readonly List<object> _items = new List<object>();
		readonly List<Line> _lines = new List<Line>();
		readonly List<ItemHost> _live = new List<ItemHost>();
		readonly Dictionary<PoolKey, Stack<ItemHost>> _pool = new Dictionary<PoolKey, Stack<ItemHost>>();
		readonly List<Gtk.Box> _lineBoxes = new List<Gtk.Box>();
		readonly List<INotifyCollectionChanged> _groupSources = new List<INotifyCollectionChanged>();

		Gtk.Viewport _viewport;
		Gtk.Box _itemsBox;

		// Gtk.Fixed, not Gtk.EventBox: a spacer must not be mistaken for a host by anything that
		// walks the tree, and Gtk.Fixed is windowless, paints nothing and has a zero minimum.
		Gtk.Fixed _leadSpacer;
		Gtk.Fixed _trailSpacer;

		Gtk.Adjustment _watchedAdjustment;

		Decoration _header;
		Decoration _footer;
		Decoration _empty;

		INotifyCollectionChanged _observableSource;
		ItemsLayout _itemsLayout;
		ItemsLayoutOrientation _orientation = ItemsLayoutOrientation.Vertical;
		bool _isGrid;
		int _span = 1;
		double _lineSpacing;
		double _withinLineSpacing;
		bool _layoutQueued;
		bool _repackNeeded = true;
		bool _linesDirty = true;
		bool _disposed;

		int _windowFirst;
		int _windowLast = -1;

		/// <summary>Item slots in the source, grouped or not; the count RemainingItemsThreshold counts down from.</summary>
		int _itemCount;

		// ---- incremental loading -------------------------------------------------------------
		double _lastHorizontalOffset;
		double _lastVerticalOffset;

		/// <summary>
		/// True while the tail is already known to be within <c>RemainingItemsThreshold</c>. Without
		/// it every pixel of scrolling at the bottom of the list re-raises
		/// <c>RemainingItemsThresholdReached</c>, and an app that loads a page per event would load
		/// one per scroll step instead of one per arrival at the tail.
		/// </summary>
		bool _thresholdLatched;

		/// <summary>The item count the latch was set against; a change in it means new items arrived.</summary>
		int _thresholdItemCount = -1;

		/// <summary>Pending <see cref="ItemsUpdatingScrollMode"/> work, set when the source changes.</summary>
		bool _scrollModePending;

		/// <summary>The item that was at the top of the viewport when the source changed, and how far past it we were.</summary>
		object _anchorItem;
		double _anchorDelta;

		/// <summary>Frozen after the first item line is measured; see the class remarks.</summary>
		double _estimate;

		/// <summary>The cross-axis extent every measured line was measured at. A change invalidates them.</summary>
		int _measuredCross = -1;

		protected override void OnElementChanged(ElementChangedEventArgs<CollectionView> e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.ScrollToRequested -= OnScrollToRequested;
				UnsubscribeSource();
				UnsubscribeItemsLayout();
			}

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					_itemsBox = new Gtk.Box(Gtk.Orientation.Vertical, 0);

					_leadSpacer = new Gtk.Fixed();
					_trailSpacer = new Gtk.Fixed();
					_leadSpacer.Show();
					_trailSpacer.Show();

					_viewport = new Gtk.Viewport
					{
						ShadowType = Gtk.ShadowType.None,
						BorderWidth = 0
					};

					_viewport.Add(_itemsBox);

					var scrolled = new Gtk.ScrolledWindow
					{
						CanFocus = true,
						ShadowType = Gtk.ShadowType.None,
						BorderWidth = 0
					};

					scrolled.Add(_viewport);

					SetNativeControl(scrolled);

					Control.ShowAll();
				}

				e.NewElement.ScrollToRequested += OnScrollToRequested;

				SubscribeItemsLayout();
				ApplyItemsLayout();
				UpdateHeader();
				UpdateFooter();
				SubscribeSource();
				ReloadItems();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == ItemsView.ItemsSourceProperty.PropertyName)
			{
				UnsubscribeSource();
				SubscribeSource();
				ReloadItems();
			}
			else if (e.PropertyName == ItemsView.ItemTemplateProperty.PropertyName ||
				e.PropertyName == GroupableItemsView.IsGroupedProperty.PropertyName ||
				e.PropertyName == GroupableItemsView.GroupHeaderTemplateProperty.PropertyName ||
				e.PropertyName == GroupableItemsView.GroupFooterTemplateProperty.PropertyName)
			{
				// Grouping changes what the source *means*, so the group subscriptions go with it.
				UnsubscribeSource();
				SubscribeSource();
				ReloadItems();
			}
			else if (e.PropertyName == StructuredItemsView.ItemsLayoutProperty.PropertyName)
			{
				UnsubscribeItemsLayout();
				SubscribeItemsLayout();
				ApplyItemsLayout();
				QueueItemsLayout();
			}
			else if (e.PropertyName == ItemsView.RemainingItemsThresholdProperty.PropertyName)
			{
				// Turning the threshold on while the list is already sitting at its tail has to be
				// able to fire it; the check runs at the end of the layout pass.
				QueueItemsLayout();
			}
			else if (e.PropertyName == ItemsView.EmptyViewProperty.PropertyName ||
				e.PropertyName == ItemsView.EmptyViewTemplateProperty.PropertyName)
			{
				UpdateEmptyView();
				QueueItemsLayout();
			}
			else if (e.PropertyName == StructuredItemsView.HeaderProperty.PropertyName ||
				e.PropertyName == StructuredItemsView.HeaderTemplateProperty.PropertyName)
			{
				UpdateHeader();
				QueueItemsLayout();
			}
			else if (e.PropertyName == StructuredItemsView.FooterProperty.PropertyName ||
				e.PropertyName == StructuredItemsView.FooterTemplateProperty.PropertyName)
			{
				UpdateFooter();
				QueueItemsLayout();
			}
			else if (e.PropertyName == ItemsView.HorizontalScrollBarVisibilityProperty.PropertyName ||
				e.PropertyName == ItemsView.VerticalScrollBarVisibilityProperty.PropertyName)
			{
				UpdateScrollBarVisibility();
			}
			else if (e.PropertyName == SelectableItemsView.SelectedItemProperty.PropertyName ||
				e.PropertyName == SelectableItemsView.SelectedItemsProperty.PropertyName ||
				e.PropertyName == SelectableItemsView.SelectionModeProperty.PropertyName)
			{
				UpdateSelectionVisuals();
			}
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			// Measuring and laying the items out mutates geometry; GTK3 discards that when it
			// happens inside a size-allocate, so it is always deferred.
			QueueItemsLayout();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				if (Element != null)
					Element.ScrollToRequested -= OnScrollToRequested;

				if (_watchedAdjustment != null)
				{
					_watchedAdjustment.ValueChanged -= OnAdjustmentValueChanged;
					_watchedAdjustment = null;
				}

				UnsubscribeSource();
				UnsubscribeItemsLayout();
				ClearItems();
				DestroyDecoration(ref _empty);
				DestroyDecoration(ref _header);
				DestroyDecoration(ref _footer);
				ClearLineBoxes();

				// The spacers are only packed while there is something outside the window, so they
				// can still be floating here.
				Detach(_leadSpacer);
				Detach(_trailSpacer);
				_leadSpacer?.Destroy();
				_trailSpacer?.Destroy();

				_leadSpacer = null;
				_trailSpacer = null;
				_itemsBox = null;
				_viewport = null;
			}

			base.Dispose(disposing);
		}

		// ---- items layout (orientation / span / spacing) --------------------------------------

		void SubscribeItemsLayout()
		{
			_itemsLayout = Element?.ItemsLayout as ItemsLayout;

			if (_itemsLayout != null)
				_itemsLayout.PropertyChanged += OnItemsLayoutPropertyChanged;
		}

		void UnsubscribeItemsLayout()
		{
			if (_itemsLayout != null)
			{
				_itemsLayout.PropertyChanged -= OnItemsLayoutPropertyChanged;
				_itemsLayout = null;
			}
		}

		void OnItemsLayoutPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == LinearItemsLayout.ItemSpacingProperty.PropertyName ||
				e.PropertyName == GridItemsLayout.SpanProperty.PropertyName ||
				e.PropertyName == GridItemsLayout.HorizontalItemSpacingProperty.PropertyName ||
				e.PropertyName == GridItemsLayout.VerticalItemSpacingProperty.PropertyName)
			{
				ApplyItemsLayout();
				QueueItemsLayout();
			}
		}

		/// <summary>
		/// Maps <c>ItemsLayout</c> onto the box orientation, the span, and the two spacings.
		/// </summary>
		/// <remarks>
		/// Everything downstream is expressed in *lines*: a line runs across the cross axis and
		/// lines stack along the scrolling axis. A <see cref="LinearItemsLayout"/> is simply the
		/// case where every line holds one item and owns the whole cross axis, which is why grid
		/// and linear share one layout, one re-pack, one virtualization window and one
		/// <c>ScrollTo</c>.
		/// </remarks>
		void ApplyItemsLayout()
		{
			var layout = Element?.ItemsLayout as ItemsLayout;
			var grid = layout as GridItemsLayout;
			var linear = layout as LinearItemsLayout;

			var orientation = layout?.Orientation ?? ItemsLayoutOrientation.Vertical;
			var isGrid = grid != null;
			var span = Math.Max(1, grid?.Span ?? 1);

			if (orientation != _orientation || isGrid != _isGrid || span != _span)
			{
				// The cut of slots into lines changes, so every measured extent and the window
				// derived from them are meaningless.
				_linesDirty = true;
				_repackNeeded = true;
				_estimate = 0;
				_measuredCross = -1;
			}

			_orientation = orientation;
			_isGrid = isGrid;
			_span = span;

			if (grid != null)
			{
				// Lines stack along the scrolling axis, items run along the cross axis.
				_lineSpacing = _orientation == ItemsLayoutOrientation.Horizontal
					? grid.HorizontalItemSpacing
					: grid.VerticalItemSpacing;

				_withinLineSpacing = _orientation == ItemsLayoutOrientation.Horizontal
					? grid.VerticalItemSpacing
					: grid.HorizontalItemSpacing;
			}
			else
			{
				_lineSpacing = linear?.ItemSpacing ?? 0;
				_withinLineSpacing = 0;
			}

			if (_itemsBox != null)
			{
				_itemsBox.Orientation = _orientation == ItemsLayoutOrientation.Horizontal
					? Gtk.Orientation.Horizontal
					: Gtk.Orientation.Vertical;

				_itemsBox.Spacing = (int)Math.Round(Math.Max(0, _lineSpacing));
			}

			foreach (var box in _lineBoxes)
				box.Spacing = (int)Math.Round(Math.Max(0, _withinLineSpacing));

			UpdateScrollBarVisibility();
		}

		void UpdateScrollBarVisibility()
		{
			if (Control == null || Element == null)
				return;

			var horizontal = Element.HorizontalScrollBarVisibility;
			var vertical = Element.VerticalScrollBarVisibility;

			if (_orientation == ItemsLayoutOrientation.Horizontal)
			{
				Control.HscrollbarPolicy = ToPolicy(horizontal, Gtk.PolicyType.Automatic);
				Control.VscrollbarPolicy = ToPolicy(vertical, Gtk.PolicyType.Never);
			}
			else
			{
				Control.HscrollbarPolicy = ToPolicy(horizontal, Gtk.PolicyType.Never);
				Control.VscrollbarPolicy = ToPolicy(vertical, Gtk.PolicyType.Automatic);
			}
		}

		static Gtk.PolicyType ToPolicy(ScrollBarVisibility visibility, Gtk.PolicyType fallback)
		{
			switch (visibility)
			{
				case ScrollBarVisibility.Always:
					return Gtk.PolicyType.Always;
				case ScrollBarVisibility.Never:
					return Gtk.PolicyType.Never;
				default:
					return fallback;
			}
		}

		// ---- source --------------------------------------------------------------------------

		void SubscribeSource()
		{
			_observableSource = Element?.ItemsSource as INotifyCollectionChanged;

			if (_observableSource != null)
				_observableSource.CollectionChanged += OnSourceCollectionChanged;

			SubscribeGroups();
		}

		void UnsubscribeSource()
		{
			if (_observableSource != null)
			{
				_observableSource.CollectionChanged -= OnSourceCollectionChanged;
				_observableSource = null;
			}

			UnsubscribeGroups();
		}

		/// <summary>
		/// When grouped, the *inner* collections are what actually holds the items, so each one that
		/// can raise <see cref="INotifyCollectionChanged"/> is observed too. Without this, adding an
		/// item to a group updates nothing.
		/// </summary>
		void SubscribeGroups()
		{
			UnsubscribeGroups();

			if (Element?.IsGrouped != true || !(Element.ItemsSource is IEnumerable source))
				return;

			foreach (var group in source)
			{
				if (group is INotifyCollectionChanged observable)
				{
					observable.CollectionChanged += OnGroupCollectionChanged;
					_groupSources.Add(observable);
				}
			}
		}

		void UnsubscribeGroups()
		{
			foreach (var observable in _groupSources)
				observable.CollectionChanged -= OnGroupCollectionChanged;

			_groupSources.Clear();
		}

		void OnGroupCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_disposed || _itemsBox == null)
				return;

			CaptureScrollAnchor();

			// A change inside a group shifts every following header/footer/item in the flat slot
			// list, so the incremental path does not apply. Correctness over cleverness here - and
			// with virtualization a "full reload" now throws away only the materialized window
			// rather than every renderer in the source.
			ReloadItems();
		}

		void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_disposed || _itemsBox == null)
				return;

			// Before the slots move: which item is at the top, and by how much. ItemsUpdatingScrollMode
			// is applied once the change has been laid out (LayoutItems).
			CaptureScrollAnchor();

			if (Element?.IsGrouped == true)
			{
				// The outer collection is groups, not items: adding one group inserts a header, its
				// items and a footer. Rebuild, and re-observe the new set of groups.
				UnsubscribeGroups();
				SubscribeGroups();
				ReloadItems();
				return;
			}

			// Incremental where the notification carries enough information, full reload
			// otherwise. A full reload is always correct but throws away the realized window along
			// with the scroll position, so it is the fallback rather than the default.
			if (e.Action == NotifyCollectionChangedAction.Add &&
				e.NewItems != null && e.NewStartingIndex >= 0)
			{
				for (var i = 0; i < e.NewItems.Count; i++)
					InsertItem(e.NewStartingIndex + i, e.NewItems[i]);
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove &&
				e.OldItems != null && e.OldStartingIndex >= 0)
			{
				for (var i = e.OldItems.Count - 1; i >= 0; i--)
					RemoveItemAt(e.OldStartingIndex + i);
			}
			else if (e.Action == NotifyCollectionChangedAction.Replace &&
				e.NewItems != null && e.NewStartingIndex >= 0)
			{
				for (var i = 0; i < e.NewItems.Count; i++)
				{
					RemoveItemAt(e.NewStartingIndex + i);
					InsertItem(e.NewStartingIndex + i, e.NewItems[i]);
				}
			}
			else
			{
				ReloadItems();
				return;
			}

			UpdateEmptyView();
			UpdateSelectionVisuals();
			QueueItemsLayout();
		}

		void ReloadItems()
		{
			if (_itemsBox == null)
				return;

			ClearItems();

			if (Element?.ItemsSource is IEnumerable source)
			{
				if (Element.IsGrouped)
					BuildGroupedSlots(source);
				else
				{
					var index = 0;

					foreach (var item in source)
						InsertItem(index++, item);
				}
			}

			UpdateEmptyView();

			// Rebuilding throws every host away, so a selection made before the reload has to be
			// re-applied to the new ones - otherwise SelectedItem silently stops being visible.
			UpdateSelectionVisuals();
			QueueItemsLayout();
		}

		/// <summary>
		/// Flattens <c>ItemsSource</c>-of-groups into the single ordered slot list everything else
		/// in this renderer works against: header, items, footer, per group. Nothing is materialized
		/// here - a slot is a record, not a widget.
		/// </summary>
		void BuildGroupedSlots(IEnumerable source)
		{
			var groupIndex = 0;

			foreach (var group in source)
			{
				AppendGroupSlot(HostKind.GroupHeader, Element.GroupHeaderTemplate, group, groupIndex);

				// A string is IEnumerable but is never a group of items; treating it as one would
				// silently explode a group into characters.
				if (group is IEnumerable inner && !(group is string))
				{
					var indexInGroup = 0;

					foreach (var item in inner)
					{
						_items.Add(item);

						_slots.Add(new Slot
						{
							Kind = HostKind.Item,
							Item = item,
							Group = group,
							GroupIndex = groupIndex,
							IndexInGroup = indexInGroup++
						});
					}
				}

				AppendGroupSlot(HostKind.GroupFooter, Element.GroupFooterTemplate, group, groupIndex);

				groupIndex++;
			}

			_linesDirty = true;
			_repackNeeded = true;
		}

		void AppendGroupSlot(HostKind kind, DataTemplate template, object group, int groupIndex)
		{
			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(group, Element);

			// No template means no header/footer at all: fabricating one would invent content the
			// application never asked for. This is the one template that has to be resolved before
			// materialization, because it decides whether the slot exists.
			if (template == null)
				return;

			_slots.Add(new Slot
			{
				Kind = kind,
				Group = group,
				GroupIndex = groupIndex,
				Template = template
			});
		}

		void InsertItem(int index, object item)
		{
			if (_itemsBox == null)
				return;

			if (index < 0 || index > _items.Count)
				index = _items.Count;

			_items.Insert(index, item);

			_slots.Insert(index, new Slot
			{
				Kind = HostKind.Item,
				Item = item,
				IndexInGroup = index
			});

			_linesDirty = true;
			_repackNeeded = true;
		}

		void RemoveItemAt(int index)
		{
			if (index < 0 || index >= _slots.Count)
				return;

			var slot = _slots[index];

			_slots.RemoveAt(index);
			_items.RemoveAt(index);

			if (slot.Host != null)
			{
				_live.Remove(slot.Host);
				Release(slot.Host);
			}

			_linesDirty = true;
			_repackNeeded = true;
		}

		void ClearItems()
		{
			foreach (var host in _live.ToList())
			{
				host.Slot = null;
				DestroyHost(host);
			}

			_live.Clear();

			foreach (var slot in _slots)
				slot.Host = null;

			// The pool is keyed by DataTemplate, and a reload is exactly the moment the templates
			// themselves may have changed. Keeping pooled views across it would recycle a view
			// built from a template the application has replaced.
			ClearPool();

			_slots.Clear();
			_items.Clear();
			_lines.Clear();
			ClearLineBoxes();

			_windowFirst = 0;
			_windowLast = -1;
			_estimate = 0;
			_measuredCross = -1;
			_linesDirty = true;
			_repackNeeded = true;
		}

		void ClearPool()
		{
			foreach (var stack in _pool.Values)
			{
				foreach (var host in stack)
					DestroyHost(host);
			}

			_pool.Clear();
		}

		void ClearLineBoxes()
		{
			foreach (var box in _lineBoxes)
			{
				Detach(box);
				box.Destroy();
			}

			_lineBoxes.Clear();
		}

		static void Detach(Gtk.Widget widget)
		{
			if (widget?.Parent is Gtk.Container container)
				container.Remove(widget);
		}

		// ---- materialization / recycling -------------------------------------------------------

		DataTemplate ResolveItemTemplate(object item)
		{
			var template = Element?.ItemTemplate;

			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(item, Element);

			return template;
		}

		/// <summary>
		/// Gives a slot a host: a recycled one when the pool holds one for the same template and
		/// kind, otherwise a freshly built view + renderer + host.
		/// </summary>
		/// <remarks>
		/// The recycle path deliberately does <b>not</b> touch <c>view.Parent</c>: it is already the
		/// CollectionView and re-assigning it would re-run the inherited-context propagation that
		/// the "Parent before BindingContext" rule exists to sequence. All that changes is the
		/// binding context; the measure and layout that make the re-bound row the right size happen
		/// in <see cref="LayoutLine"/> later in the same idle pass.
		/// </remarks>
		void Acquire(Slot slot)
		{
			var template = slot.Kind == HostKind.Item ? ResolveItemTemplate(slot.Item) : slot.Template;
			var context = slot.Kind == HostKind.Item ? slot.Item : slot.Group;
			var key = new PoolKey(template, slot.Kind);

			ItemHost host = null;

			if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
			{
				host = stack.Pop();

				if (host.IsFallbackLabel)
				{
					// No template means no binding either, so the text has to be re-assigned.
					// Without this a recycled fallback row shows its previous occupant.
					if (host.View is Label label)
						label.Text = context?.ToString() ?? string.Empty;
				}

				host.View.BindingContext = context;
			}
			else
			{
				var fallback = false;
				var view = template?.CreateContent() as View;

				if (view == null)
				{
					// No template: Forms' documented fallback is the item's own text.
					view = new Label { Text = context?.ToString() ?? string.Empty, Margin = new Thickness(6) };
					fallback = true;
				}

				// Parent first, then BindingContext: parenting wires the element chain and would
				// otherwise overwrite the binding context by inheriting the CollectionView's.
				view.Parent = Element;
				view.BindingContext = context;

				var renderer = Platform.CreateRenderer(view);
				Platform.SetRenderer(view, renderer);

				var box = new HostBox { VisibleWindow = false };
				box.Add(renderer.Container);
				box.ShowAll();

				host = new ItemHost
				{
					Key = key,
					View = view,
					Renderer = renderer,
					Host = box,
					IsFallbackLabel = fallback
				};

				// Only items are selectable; a group header must not react to a click. The pool key
				// carries the kind, so a pooled host always already has the right handler.
				if (slot.Kind == HostKind.Item)
					box.ButtonPressEvent += OnItemButtonPress;
			}

			host.Slot = slot;
			slot.Host = host;

			ApplySelectionVisual(slot);
		}

		/// <summary>Returns a host to the pool (or destroys it when the pool is full).</summary>
		void Release(ItemHost host)
		{
			if (host == null)
				return;

			if (host.Slot != null)
			{
				host.Slot.Host = null;
				host.Slot = null;
			}

			Detach(host.Host);

			// A recycled host must never arrive pre-highlighted.
			host.Host.VisibleWindow = false;
			host.Host.ClearStyle();

			if (!_pool.TryGetValue(host.Key, out var stack))
			{
				stack = new Stack<ItemHost>();
				_pool[host.Key] = stack;
			}

			if (stack.Count < MaxPooledPerTemplate)
				stack.Push(host);
			else
				DestroyHost(host);
		}

		void DestroyHost(ItemHost host)
		{
			if (host == null)
				return;

			if (host.Key.Kind == HostKind.Item)
				host.Host.ButtonPressEvent -= OnItemButtonPress;

			Detach(host.Host);

			host.Renderer?.Dispose();

			if (host.View != null)
			{
				Platform.SetRenderer(host.View, null);
				host.View.Parent = null;
			}

			host.Host.Destroy();
		}

		// ---- header / footer / empty view ------------------------------------------------------

		void UpdateEmptyView()
		{
			DestroyDecoration(ref _empty);

			// The EmptyView is the one decoration whose existence depends on the source: it is
			// materialized only while there is nothing to show.
			if (_disposed || _itemsBox == null || Element == null || _items.Count > 0)
				return;

			_empty = BuildDecoration(Element.EmptyView, Element.EmptyViewTemplate, center: true);
		}

		void UpdateHeader()
		{
			DestroyDecoration(ref _header);

			if (_disposed || _itemsBox == null || Element == null)
				return;

			_header = BuildDecoration(Element.Header, Element.HeaderTemplate, center: false);
		}

		void UpdateFooter()
		{
			DestroyDecoration(ref _footer);

			if (_disposed || _itemsBox == null || Element == null)
				return;

			_footer = BuildDecoration(Element.Footer, Element.FooterTemplate, center: false);
		}

		/// <summary>
		/// Materializes one decoration and gives it a renderer and a host, or returns
		/// <c>null</c> when there is nothing to show.
		/// </summary>
		/// <remarks>
		/// Decorations are never virtualized and never recycled: there is at most one of each, they
		/// bracket the whole list, and their extents are what every scroll offset is measured from.
		/// Like item hosts, the host is left <b>unparented</b>: <see cref="RepackHosts"/> decides
		/// where it goes.
		/// </remarks>
		Decoration BuildDecoration(object content, DataTemplate template, bool center)
		{
			var view = MaterializeDecoration(content, template, center);

			if (view == null)
				return null;

			var renderer = Platform.CreateRenderer(view);
			Platform.SetRenderer(view, renderer);

			var host = new HostBox { VisibleWindow = false };
			host.Add(renderer.Container);
			host.ShowAll();

			_repackNeeded = true;

			return new Decoration { View = view, Renderer = renderer, Host = host };
		}

		/// <summary>
		/// Resolves the "an object, or a template applied to that object" contract that
		/// <c>Header</c>, <c>Footer</c> and <c>EmptyView</c> all share.
		/// </summary>
		/// <remarks>
		/// <para>A <b>null</b> content means no decoration at all, even when a template is set. That
		/// is not an arbitrary choice: iOS' <c>ItemsViewController.UpdateView</c> short-circuits on
		/// <c>view == null</c> before it ever looks at the template, and the same method serves
		/// Header, Footer and EmptyView there. Applying the template anyway materializes it against
		/// a null binding context, which renders as a blank strip - measured here as a footer that
		/// would not go away when <c>Footer</c> was set to null while <c>FooterTemplate</c> stayed.</para>
		///
		/// <para>Otherwise precedence is the template first (with the object as its binding context),
		/// then the object itself if it is already a <see cref="View"/>, then its text. A
		/// caller-owned view is parented but its <c>BindingContext</c> is deliberately left alone to
		/// inherit - overwriting it would break a header the application has already bound.</para>
		/// </remarks>
		View MaterializeDecoration(object content, DataTemplate template, bool center)
		{
			if (content == null)
				return null;

			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(content, Element);

			if (template != null)
			{
				if (template.CreateContent() is View templated)
				{
					// Parent first, then BindingContext - see Acquire.
					templated.Parent = Element;
					templated.BindingContext = content;

					return templated;
				}
			}

			if (content is View plain)
			{
				// A view the caller owns: parent it so the element chain is right, but leave
				// its BindingContext to inherit rather than clobbering it.
				plain.Parent = Element;

				return plain;
			}

			var label = new Label { Text = content.ToString() };

			if (center)
			{
				label.HorizontalTextAlignment = TextAlignment.Center;
				label.VerticalTextAlignment = TextAlignment.Center;
			}
			else
			{
				label.Margin = new Thickness(6);
			}

			label.Parent = Element;

			return label;
		}

		void DestroyDecoration(ref Decoration decoration)
		{
			if (decoration == null)
				return;

			if (decoration.Host != null)
			{
				Detach(decoration.Host);
				_repackNeeded = true;
			}

			decoration.Renderer?.Dispose();

			if (decoration.View != null)
			{
				Platform.SetRenderer(decoration.View, null);
				decoration.View.Parent = null;
			}

			decoration.Host?.Destroy();
			decoration = null;
		}

		// ---- lines and the virtualization window ------------------------------------------------

		/// <summary>
		/// Cuts the flat slot list into lines. This is the <b>only</b> place layout mode matters:
		/// linear gives one spanning line per slot, grid packs up to <c>Span</c> item slots per
		/// line, and a group header/footer always takes a spanning line of its own.
		/// </summary>
		void RebuildLines()
		{
			_lines.Clear();

			var span = _isGrid ? Math.Max(1, _span) : 1;
			Line current = null;
			var itemIndex = 0;

			for (var i = 0; i < _slots.Count; i++)
			{
				var slot = _slots[i];
				slot.Index = i;

				// The ordinal among *items*, which is what ItemsViewScrolledEventArgs reports and
				// what RemainingItemsThreshold counts down from. It differs from Index the moment
				// the source is grouped, because headers and footers occupy slots and are not items.
				slot.ItemIndex = slot.Kind == HostKind.Item ? itemIndex++ : -1;

				if (!_isGrid || slot.Kind != HostKind.Item)
				{
					current = null;
					_lines.Add(new Line { Start = i, Count = 1, Spanning = true });

					continue;
				}

				if (current == null || current.Count >= span)
				{
					current = new Line { Start = i, Count = 0 };
					_lines.Add(current);
				}

				current.Count++;
			}

			_itemCount = itemIndex;
		}

		/// <summary>
		/// The gap <see cref="Gtk.Box"/> actually puts between two lines.
		/// </summary>
		/// <remarks>
		/// <c>Gtk.Box.Spacing</c> is an <c>int</c>, so this is the rounded <c>ItemSpacing</c> and not
		/// the raw one. Every scroll offset and both spacers have to be summed with the value GTK
		/// uses or they drift by up to half a pixel <b>per off-screen line</b> - which is invisible
		/// on ten rows and is thousands of pixels of misplacement on ten thousand.
		/// </remarks>
		double LineGap => Math.Round(Math.Max(0, _lineSpacing));

		/// <summary>A line's extent: measured if it has ever been laid out, estimated otherwise.</summary>
		double LineExtent(int index)
		{
			var line = _lines[index];

			if (line.Measured)
				return line.Extent;

			return _estimate > 0 ? _estimate : 1;
		}

		void InvalidateMeasuredExtents()
		{
			foreach (var line in _lines)
				line.Measured = false;

			_estimate = 0;
		}

		/// <summary>
		/// The range of lines to materialize: everything the viewport can show, plus
		/// <see cref="BufferLines"/> lines of margin at each end.
		/// </summary>
		void ComputeWindow(out int first, out int last) => ComputeWindow(out first, out last, out _, out _);

		/// <summary>
		/// As above, additionally reporting the range of lines that are genuinely <b>on screen</b> -
		/// the window without its buffer. That is the range the <c>Scrolled</c> event and
		/// <c>RemainingItemsThreshold</c> have to be computed from: reporting the materialized window
		/// instead would claim 8 lines of items are visible at each end that are not, and would fire
		/// the threshold that many lines early.
		/// </summary>
		void ComputeWindow(out int first, out int last, out int onScreenFirst, out int onScreenLast)
		{
			first = 0;
			last = -1;
			onScreenFirst = 0;
			onScreenLast = -1;

			if (_lines.Count == 0 || Control == null)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;
			var adjustment = horizontal ? Control.Hadjustment : Control.Vadjustment;
			var value = adjustment?.Value ?? 0;
			var page = ViewportExtent(horizontal, adjustment);

			if (page < 1)
				page = 1;

			var lead = Extent(_header?.Host, horizontal);

			if (lead > 0)
				lead += LineGap;

			var position = lead;
			var visibleFirst = -1;
			var visibleLast = -1;

			for (var i = 0; i < _lines.Count; i++)
			{
				var extent = LineExtent(i);
				var start = position;
				var end = position + extent;

				if (end > value && start < value + page)
				{
					if (visibleFirst < 0)
						visibleFirst = i;

					visibleLast = i;
				}

				if (start >= value + page)
					break;

				position = end + LineGap;
			}

			if (visibleFirst < 0)
			{
				// Scrolled past the end (which happens transiently while the content shrinks):
				// keep the tail materialized rather than nothing.
				visibleFirst = _lines.Count - 1;
				visibleLast = _lines.Count - 1;
			}

			// Before anything has been measured a line is assumed to be 1px tall, so "what the
			// viewport can show" is every line there is - which on a 10,000-item source would
			// materialize the whole thing on the very first pass, i.e. exactly what virtualization
			// exists to avoid. Until the estimate exists the window is a single probe line plus the
			// buffer; measuring it sets the estimate and LayoutItems queues the pass that opens the
			// window to its real size.
			if (_estimate <= 0)
				visibleLast = visibleFirst;

			onScreenFirst = visibleFirst;
			onScreenLast = visibleLast;

			first = Math.Max(0, visibleFirst - BufferLines);
			last = Math.Min(_lines.Count - 1, visibleLast + BufferLines);
		}

		/// <summary>Materializes the slots that entered the window and releases those that left it.</summary>
		bool ReconcileWindow()
		{
			var changed = false;

			var slotFirst = int.MaxValue;
			var slotLast = -1;

			if (_windowLast >= _windowFirst && _windowFirst >= 0 && _windowLast < _lines.Count)
			{
				slotFirst = _lines[_windowFirst].Start;
				slotLast = _lines[_windowLast].Start + _lines[_windowLast].Count - 1;
			}

			for (var i = _live.Count - 1; i >= 0; i--)
			{
				var host = _live[i];
				var index = host.Slot?.Index ?? -1;

				if (index < slotFirst || index > slotLast)
				{
					_live.RemoveAt(i);
					Release(host);
					changed = true;
				}
			}

			for (var i = slotFirst; i <= slotLast; i++)
			{
				var slot = _slots[i];

				if (slot.Host != null)
					continue;

				Acquire(slot);
				_live.Add(slot.Host);
				changed = true;
			}

			return changed;
		}

		/// <summary>
		/// Rebuilds the native child structure from the materialized window.
		/// </summary>
		/// <remarks>
		/// Spanning lines are packed straight into <c>_itemsBox</c>, so a linear CollectionView whose
		/// window covers the whole source has exactly the tree it had before virtualization existed -
		/// hosts as direct children, no spacers. Grid lines get an intermediate
		/// <see cref="Gtk.Box"/> running across the cross axis. Re-parenting a host is safe here
		/// because the managed wrapper holds its own reference: <c>gtk_container_remove</c>'s unref
		/// cannot drop it to zero.
		///
		/// <para>The two spacers stand in for the lines outside the window. They are packed only when
		/// there <i>are</i> such lines, which is what keeps the small-list tree byte-for-byte what it
		/// was, and their sizes are set later in the same pass by <see cref="UpdateSpacers"/> once
		/// the window has actually been measured.</para>
		/// </remarks>
		void RepackHosts()
		{
			if (_itemsBox == null)
				return;

			foreach (var host in _live)
			{
				Detach(host.Host);

				// Drop the previous pass' size request before re-packing. It was computed for the
				// *previous* line structure - or, with recycling, for the previous occupant of this
				// very host - and a Gtk.Box asks GTK for the sum of its children's requests:
				// re-packing hosts that still request half the viewport three-to-a-line makes this
				// control demand 3 x (w/2) as its *minimum* width. GTK grows the toplevel to satisfy
				// a minimum, the next layout pass then derives the item width from the grown
				// viewport, and the control never shrinks back - measured as a 540px grid ratcheting
				// to 803px after a Span 3 -> 2 -> 3 round trip, wider than the screen and painting
				// nothing where it used to be. LayoutLine re-applies the correct request further
				// down this same idle callback, before anything is drawn.
				host.Host.SetSizeRequest(-1, -1);
			}

			_header?.Host.SetSizeRequest(-1, -1);
			_footer?.Host.SetSizeRequest(-1, -1);
			_empty?.Host.SetSizeRequest(-1, -1);

			Detach(_header?.Host);
			Detach(_empty?.Host);
			Detach(_footer?.Host);
			Detach(_leadSpacer);
			Detach(_trailSpacer);
			ClearLineBoxes();

			// The header leads and the footer trails along the scrolling axis, so they are simply
			// the first and last children of the items box; Gtk.Box already spaces them from the
			// lines by the same ItemSpacing everything else uses.
			if (_header?.Host != null)
				_itemsBox.PackStart(_header.Host, false, false, 0);

			if (_windowFirst > 0 && _leadSpacer != null)
				_itemsBox.PackStart(_leadSpacer, false, false, 0);

			var lineOrientation = _orientation == ItemsLayoutOrientation.Horizontal
				? Gtk.Orientation.Vertical
				: Gtk.Orientation.Horizontal;

			var within = (int)Math.Round(Math.Max(0, _withinLineSpacing));

			for (var i = _windowFirst; i <= _windowLast && i < _lines.Count; i++)
			{
				var line = _lines[i];

				if (line.Spanning)
				{
					var host = _slots[line.Start].Host;

					if (host != null)
						_itemsBox.PackStart(host.Host, false, false, 0);

					continue;
				}

				var box = new Gtk.Box(lineOrientation, within);

				for (var s = line.Start; s < line.Start + line.Count; s++)
				{
					var host = _slots[s].Host;

					if (host != null)
						box.PackStart(host.Host, false, false, 0);
				}

				_itemsBox.PackStart(box, false, false, 0);
				_lineBoxes.Add(box);

				box.Show();
			}

			if (_windowLast < _lines.Count - 1 && _trailSpacer != null)
				_itemsBox.PackStart(_trailSpacer, false, false, 0);

			if (_empty?.Host != null)
				_itemsBox.PackStart(_empty.Host, true, true, 0);

			if (_footer?.Host != null)
				_itemsBox.PackStart(_footer.Host, false, false, 0);
		}

		/// <summary>
		/// Sizes the two spacers so the scroll extent, and therefore every scroll position, is the
		/// same as it would be with every line materialized.
		/// </summary>
		/// <remarks>
		/// A <see cref="Gtk.Box"/> puts <c>Spacing</c> between every pair of visible children, so
		/// replacing <c>n</c> off-screen lines with one spacer removes <c>n - 1</c> gaps: the spacer
		/// has to be their summed extent <b>plus</b> those gaps. Getting this wrong is invisible at
		/// the top of a list and drifts by <c>Spacing</c> per off-screen line further down.
		///
		/// <para><b>Known ceiling, inherited from GTK and not from this code.</b>
		/// <c>gtk_viewport_size_allocate</c> sizes its <c>bin_window</c> to the whole content extent,
		/// and an X11 window dimension is 16 bits - so a list whose total extent exceeds 65535px is
		/// truncated by the X server, and everything past that point scrolls into blank space. At a
		/// 32px row that is about 2,000 rows. The materialized set stays bounded regardless (that is
		/// what this class does), but the *scrollable* range does not, and fixing it means giving up
		/// <see cref="Gtk.Viewport"/> for a custom <c>Gtk.Scrollable</c> container that maps a
		/// virtual offset onto a viewport-sized allocation. That is a separate piece of work; it is
		/// recorded here rather than discovered again. <c>scratchpad/cv7-virtual.sh</c> deliberately
		/// carries both a 10,000-row list (over the ceiling) and a 1,500-row list (under it) so a
		/// failure names its own cause.</para>
		/// </remarks>
		void UpdateSpacers()
		{
			if (_itemsBox == null)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;

			if (_leadSpacer != null && _windowFirst > 0)
			{
				double lead = 0;

				for (var i = 0; i < _windowFirst; i++)
					lead += LineExtent(i);

				lead += (_windowFirst - 1) * LineGap;

				SetSpacer(_leadSpacer, lead, horizontal);
			}

			if (_trailSpacer != null && _windowLast < _lines.Count - 1)
			{
				double trail = 0;
				var count = 0;

				for (var i = Math.Max(0, _windowLast + 1); i < _lines.Count; i++)
				{
					trail += LineExtent(i);
					count++;
				}

				trail += (count - 1) * LineGap;

				SetSpacer(_trailSpacer, trail, horizontal);
			}
		}

		static void SetSpacer(Gtk.Widget spacer, double extent, bool horizontal)
		{
			var size = (int)Math.Max(0, Math.Round(extent));

			if (horizontal)
				spacer.SetSizeRequest(size, -1);
			else
				spacer.SetSizeRequest(-1, size);
		}

		// ---- layout --------------------------------------------------------------------------

		void QueueItemsLayout()
		{
			if (_layoutQueued || _disposed)
				return;

			_layoutQueued = true;

			GLib.Idle.Add(() =>
			{
				_layoutQueued = false;

				if (!_disposed)
					LayoutItems();

				return false;
			});
		}

		/// <summary>
		/// The scrolled window's adjustment is what drives virtualization, so it has to be observed -
		/// and it is not guaranteed to be the same object for the whole life of the renderer
		/// (a <see cref="Gtk.ScrolledWindow"/> may replace it, and swapping orientation swaps which
		/// of the two matters), so the subscription is re-checked rather than made once.
		/// </summary>
		void EnsureAdjustmentSubscription()
		{
			if (Control == null)
				return;

			var adjustment = _orientation == ItemsLayoutOrientation.Horizontal
				? Control.Hadjustment
				: Control.Vadjustment;

			var current = _watchedAdjustment == null ? IntPtr.Zero : _watchedAdjustment.Handle;
			var wanted = adjustment == null ? IntPtr.Zero : adjustment.Handle;

			if (current == wanted)
				return;

			if (_watchedAdjustment != null)
				_watchedAdjustment.ValueChanged -= OnAdjustmentValueChanged;

			_watchedAdjustment = adjustment;

			if (_watchedAdjustment != null)
				_watchedAdjustment.ValueChanged += OnAdjustmentValueChanged;
		}

		void OnAdjustmentValueChanged(object sender, EventArgs e)
		{
			if (_disposed || _itemsBox == null)
				return;

			ComputeWindow(out var first, out var last, out var onScreenFirst, out var onScreenLast);

			// Only a *change of window* is worth a layout pass. Without this guard every pixel of
			// scrolling would re-measure the visible rows, and setting the spacers would feed
			// straight back in through the adjustment's own clamping.
			if (first != _windowFirst || last != _windowLast)
				QueueItemsLayout();

			// The Scrolled event, on the other hand, is raised for every value change: that is what
			// "scrolled" means, and an app tracking the offset would otherwise see it jump a whole
			// window at a time.
			NotifyScrolled(onScreenFirst, onScreenLast);
		}

		// ---- incremental loading ---------------------------------------------------------------

		void NotifyScrolled(int onScreenFirstLine, int onScreenLastLine)
		{
			if (Element == null || Control == null)
				return;

			var horizontalOffset = Control.Hadjustment?.Value ?? 0;
			var verticalOffset = Control.Vadjustment?.Value ?? 0;

			VisibleItems(onScreenFirstLine, onScreenLastLine, out var firstItem, out var lastItem);

			Element.SendScrolled(new ItemsViewScrolledEventArgs
			{
				HorizontalOffset = horizontalOffset,
				VerticalOffset = verticalOffset,
				HorizontalDelta = horizontalOffset - _lastHorizontalOffset,
				VerticalDelta = verticalOffset - _lastVerticalOffset,
				FirstVisibleItemIndex = firstItem,
				CenterItemIndex = firstItem < 0 ? -1 : firstItem + (lastItem - firstItem) / 2,
				LastVisibleItemIndex = lastItem
			});

			_lastHorizontalOffset = horizontalOffset;
			_lastVerticalOffset = verticalOffset;

			CheckRemainingItemsThreshold(lastItem);
		}

		/// <summary>
		/// The first and last <b>item</b> indices inside a range of lines, or -1/-1 when the range
		/// holds no items at all (a grouped list scrolled to a lone group header does exactly that).
		/// </summary>
		void VisibleItems(int firstLine, int lastLine, out int firstItem, out int lastItem)
		{
			firstItem = -1;
			lastItem = -1;

			if (firstLine < 0 || lastLine < firstLine || lastLine >= _lines.Count)
				return;

			var slotFirst = _lines[firstLine].Start;
			var slotLast = _lines[lastLine].Start + _lines[lastLine].Count - 1;

			for (var i = slotFirst; i <= slotLast && i < _slots.Count; i++)
			{
				// Group headers and footers occupy slots and are not items.
				if (_slots[i].ItemIndex < 0)
					continue;

				if (firstItem < 0)
					firstItem = _slots[i].ItemIndex;

				lastItem = _slots[i].ItemIndex;
			}
		}

		/// <summary>
		/// Raises <c>RemainingItemsThresholdReached</c> when the tail comes within
		/// <c>RemainingItemsThreshold</c> items of the last visible one - once per arrival, not once
		/// per scroll step, and again as soon as the source has grown.
		/// </summary>
		/// <remarks>
		/// The latch is the whole design here. An app's handler for this event loads the next page,
		/// which takes a round trip; without the latch every subsequent pixel of scrolling raises it
		/// again and the app issues a request per scroll step. Un-latching on a change of item count
		/// rather than on a timer is what makes the next page load when the user reaches the *new*
		/// tail: it is the same condition the app itself just satisfied.
		/// </remarks>
		void CheckRemainingItemsThreshold(int lastVisibleItemIndex)
		{
			if (Element == null)
				return;

			if (_thresholdItemCount != _itemCount)
			{
				_thresholdItemCount = _itemCount;
				_thresholdLatched = false;
			}

			// -1 is the documented "never" - not "a threshold of nothing left".
			if (Element.RemainingItemsThreshold < 0 || lastVisibleItemIndex < 0 || _itemCount == 0)
				return;

			var remaining = _itemCount - 1 - lastVisibleItemIndex;

			if (remaining > Element.RemainingItemsThreshold)
			{
				_thresholdLatched = false;
				return;
			}

			if (_thresholdLatched)
				return;

			_thresholdLatched = true;
			Element.SendRemainingItemsThresholdReached();
		}

		/// <summary>
		/// Remembers which item is at the top of the viewport, and by how much it is scrolled past,
		/// so that <see cref="ItemsUpdatingScrollMode.KeepItemsInView"/> can put it back there after
		/// the source has changed underneath it.
		/// </summary>
		/// <remarks>
		/// Anchored on the item <b>object</b>, not on its index: inserting above the viewport is the
		/// case this exists for, and it is precisely the case where every index below the insertion
		/// has moved.
		/// </remarks>
		void CaptureScrollAnchor()
		{
			_scrollModePending = true;
			_anchorItem = null;
			_anchorDelta = 0;

			if (Element == null || Control == null || _lines.Count == 0)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;
			var adjustment = horizontal ? Control.Hadjustment : Control.Vadjustment;

			if (adjustment == null)
				return;

			ComputeWindow(out _, out _, out var onScreenFirst, out var onScreenLast);
			VisibleItems(onScreenFirst, onScreenLast, out var firstItem, out _);

			if (firstItem < 0)
				return;

			var slot = _slots.FindIndex(s => s.ItemIndex == firstItem);

			if (slot < 0 || !TryMeasureSlot(slot, horizontal, out var offset, out _, out _))
				return;

			_anchorItem = _slots[slot].Item;
			_anchorDelta = adjustment.Value - offset;
		}

		/// <summary>
		/// Applies <c>ItemsUpdatingScrollMode</c> after a source change has been laid out.
		/// </summary>
		void ApplyItemsUpdatingScrollMode()
		{
			if (!_scrollModePending)
				return;

			_scrollModePending = false;

			var anchor = _anchorItem;
			_anchorItem = null;

			if (Element == null || Control == null || _lines.Count == 0)
				return;

			switch (Element.ItemsUpdatingScrollMode)
			{
				case ItemsUpdatingScrollMode.KeepScrollOffset:
					// The offset stays where it is, which is what GTK does on its own; the items
					// under it are allowed to move. Nothing to do, and doing nothing is the point.
					break;

				case ItemsUpdatingScrollMode.KeepLastItemInView:
					var last = _slots.FindLastIndex(s => s.Kind == HostKind.Item);

					if (last >= 0)
						ScrollToIndex(last, ScrollToPosition.End);
					break;

				default: // KeepItemsInView
					RestoreScrollAnchor(anchor);
					break;
			}
		}

		void RestoreScrollAnchor(object anchor)
		{
			if (anchor == null)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;
			var adjustment = horizontal ? Control.Hadjustment : Control.Vadjustment;

			if (adjustment == null)
				return;

			var slot = _slots.FindIndex(s => s.Kind == HostKind.Item && Equals(s.Item, anchor));

			if (slot < 0 || !TryMeasureSlot(slot, horizontal, out var offset, out _, out var content))
				return;

			var viewportExtent = ViewportExtent(horizontal, adjustment);
			var max = Math.Max(0, content - viewportExtent);
			var target = Math.Max(adjustment.Lower, Math.Min(offset + _anchorDelta, adjustment.Lower + max));

			// Widened for the same reason ScrollToIndex widens it: the spacers that will carry the
			// off-screen extent have not been allocated yet, so Upper is routinely far too small and
			// Gtk.Adjustment's own setter would clamp the value into a stale range.
			if (adjustment.Upper < adjustment.Lower + content)
				adjustment.Upper = adjustment.Lower + content;

			adjustment.Value = target;

			QueueItemsLayout();
		}

		void LayoutItems()
		{
			if (_disposed || Control == null || Element == null)
				return;

			if (_linesDirty)
			{
				_linesDirty = false;
				RebuildLines();
			}

			EnsureAdjustmentSubscription();

			var width = _viewport != null && _viewport.AllocatedWidth > 1
				? _viewport.AllocatedWidth
				: Control.AllocatedWidth;

			var height = _viewport != null && _viewport.AllocatedHeight > 1
				? _viewport.AllocatedHeight
				: Control.AllocatedHeight;

			var cross = _orientation == ItemsLayoutOrientation.Horizontal ? height : width;

			if (cross > 1 && cross != _measuredCross)
			{
				// Every measured extent was measured against the old cross axis, so none of them
				// describe the list any more - including the frozen estimate.
				InvalidateMeasuredExtents();
				_measuredCross = cross;
			}

			ComputeWindow(out var first, out var last, out var onScreenFirst, out var onScreenLast);

			if (first != _windowFirst || last != _windowLast)
			{
				_windowFirst = first;
				_windowLast = last;
				_repackNeeded = true;
			}

			if (ReconcileWindow())
				_repackNeeded = true;

			// The re-pack has to happen even when the viewport has no size yet, otherwise hosts
			// created before the first allocation would never be parented at all.
			if (_repackNeeded)
			{
				_repackNeeded = false;
				RepackHosts();
			}

			if (width <= 1 || height <= 1)
				return;

			var hadEstimate = _estimate > 0;

			LayoutDecoration(_header, width, height);

			for (var i = _windowFirst; i <= _windowLast && i < _lines.Count; i++)
				LayoutLine(i, width, height);

			LayoutDecoration(_footer, width, height);

			UpdateSpacers();

			// The probe pass above (see ComputeWindow) deliberately materialized one line to find
			// out how big a line is. Now that the estimate exists the window is wrong by
			// construction, so re-run rather than waiting for an allocation to happen to come back.
			if (!hadEstimate && _estimate > 0)
				QueueItemsLayout();

			// The EmptyView is the one decoration that fills what is left rather than taking its
			// natural size: an empty list should show it across the whole viewport, not as a
			// one-line strip at the top.
			if (_empty?.View != null)
			{
				_empty.View.Layout(new Rectangle(0, 0, width, height));
				_empty.Host.SetSizeRequest(width, height);
			}

			// Now that the new content has real extents, put the viewport back where
			// ItemsUpdatingScrollMode says it belongs.
			ApplyItemsUpdatingScrollMode();

			// A list short enough to be visible in full reaches RemainingItemsThreshold without
			// anyone ever scrolling, so the check cannot live on the adjustment alone - and neither
			// can the first page of an incrementally-loaded list, which is exactly that case.
			VisibleItems(onScreenFirst, onScreenLast, out _, out var lastVisibleItem);
			CheckRemainingItemsThreshold(lastVisibleItem);
		}

		/// <summary>
		/// Lays out a header or footer: it owns the whole cross axis and takes its measured size
		/// along the scrolling axis, i.e. exactly the geometry of a spanning line.
		/// </summary>
		void LayoutDecoration(Decoration decoration, int width, int height)
		{
			if (decoration?.View == null)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;

			var request = horizontal
				? decoration.View.Measure(double.PositiveInfinity, height, MeasureFlags.IncludeMargins)
				: decoration.View.Measure(width, double.PositiveInfinity, MeasureFlags.IncludeMargins);

			var main = (int)Math.Ceiling(Math.Max(1,
				horizontal ? request.Request.Width : request.Request.Height));

			if (horizontal)
			{
				decoration.View.Layout(new Rectangle(0, 0, main, height));
				decoration.Host.SetSizeRequest(main, height);
			}
			else
			{
				decoration.View.Layout(new Rectangle(0, 0, width, main));
				decoration.Host.SetSizeRequest(width, main);
			}
		}

		/// <summary>
		/// Measures and lays out one materialized line, and records its extent.
		/// </summary>
		/// <remarks>
		/// The cross-axis extent is fixed first (the viewport for a spanning line, an equal share of
		/// it for a grid line) and the main-axis extent is then the largest measurement in the line,
		/// applied to every host in it - which is what makes a grid a grid rather than a ragged set
		/// of columns. Measuring is why this must never run inside a size-allocate.
		///
		/// <para>This is also the only place a <i>re-bound</i> row gets its geometry: recycling
		/// changes the binding context and nothing else, so without the explicit measure/layout here
		/// a recycled row would keep the previous occupant's size.</para>
		/// </remarks>
		void LayoutLine(int index, int width, int height)
		{
			var line = _lines[index];

			if (line.Count == 0)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;
			var cross = horizontal ? height : width;
			var span = line.Spanning ? 1 : Math.Max(1, _span);
			var within = (int)Math.Round(Math.Max(0, _withinLineSpacing));

			var itemCross = span <= 1
				? cross
				: Math.Max(1, (cross - (span - 1) * within) / span);

			var main = 1;

			for (var s = line.Start; s < line.Start + line.Count; s++)
			{
				var view = _slots[s].Host?.View;

				if (view == null)
					continue;

				var request = horizontal
					? view.Measure(double.PositiveInfinity, itemCross, MeasureFlags.IncludeMargins)
					: view.Measure(itemCross, double.PositiveInfinity, MeasureFlags.IncludeMargins);

				var measured = (int)Math.Ceiling(Math.Max(1,
					horizontal ? request.Request.Width : request.Request.Height));

				if (measured > main)
					main = measured;
			}

			for (var s = line.Start; s < line.Start + line.Count; s++)
			{
				var host = _slots[s].Host;

				if (host?.View == null)
					continue;

				if (horizontal)
				{
					host.View.Layout(new Rectangle(0, 0, main, itemCross));
					host.Host.SetSizeRequest(main, itemCross);
				}
				else
				{
					host.View.Layout(new Rectangle(0, 0, itemCross, main));
					host.Host.SetSizeRequest(itemCross, main);
				}
			}

			line.Extent = main;
			line.Measured = true;

			// The estimate for every line that has never been on screen. Frozen at the first item
			// line ever measured (ItemSizingStrategy.MeasureFirstItem semantics): a continuously
			// re-averaged estimate changes the content size on every pass, which changes the
			// allocation, which re-runs this pass.
			if (_estimate <= 0 && _slots[line.Start].Kind == HostKind.Item)
				_estimate = main;
		}

		// ---- selection -----------------------------------------------------------------------

		void OnItemButtonPress(object o, Gtk.ButtonPressEventArgs args)
		{
			if (_disposed || Element == null)
				return;

			var host = _live.FirstOrDefault(h => ReferenceEquals(h.Host, o));
			var slot = host?.Slot;

			if (slot == null || slot.Kind != HostKind.Item)
				return;

			switch (Element.SelectionMode)
			{
				case SelectionMode.Single:
					Element.SetValueFromRenderer(SelectableItemsView.SelectedItemProperty, slot.Item);
					break;

				case SelectionMode.Multiple:
					var selection = Element.SelectedItems == null
						? new List<object>()
						: new List<object>(Element.SelectedItems);

					if (selection.Contains(slot.Item))
						selection.Remove(slot.Item);
					else
						selection.Add(slot.Item);

					Element.UpdateSelectedItems(selection);
					break;
			}
		}

		bool IsSelected(Slot slot)
		{
			if (Element == null || slot.Kind != HostKind.Item)
				return false;

			var mode = Element.SelectionMode;

			if (mode == SelectionMode.Single)
				return Equals(Element.SelectedItem, slot.Item);

			if (mode == SelectionMode.Multiple)
				return Element.SelectedItems != null && Element.SelectedItems.Contains(slot.Item);

			return false;
		}

		/// <summary>
		/// Paints the selected state of one materialized slot.
		/// </summary>
		/// <remarks>
		/// Two mechanisms, deliberately: the host <see cref="Gtk.EventBox"/> gets a real window and a
		/// selection background (which an opaque item template will cover - it sits *under* the
		/// item, exactly as a cell background does on the other platforms), and the item view is put
		/// into the <c>Selected</c> visual state so a template that defines one can render the
		/// selection itself. Without the second, an application whose rows have a background has no
		/// way to show selection at all.
		///
		/// <para>With recycling this also has to run at materialization time, not only when the
		/// selection changes: a host arriving from the pool carries the previous occupant's state.</para>
		/// </remarks>
		void ApplySelectionVisual(Slot slot)
		{
			var host = slot?.Host;

			if (host == null)
				return;

			var selected = IsSelected(slot);

			// A no-window EventBox paints nothing of its own, so the background only exists
			// once the host owns a GdkWindow.
			host.Host.VisibleWindow = selected;

			if (selected)
				host.Host.SetBackgroundColor(DefaultSelectionColor, Gtk.StateType.Normal);
			else
				host.Host.ClearStyle();

			if (host.View != null)
				VisualStateManager.GoToState(host.View,
					selected ? VisualStateManager.CommonStates.Selected : VisualStateManager.CommonStates.Normal);
		}

		void UpdateSelectionVisuals()
		{
			if (_disposed || Element == null)
				return;

			foreach (var host in _live)
			{
				if (host.Slot != null)
					ApplySelectionVisual(host.Slot);
			}
		}

		// ---- ScrollTo ------------------------------------------------------------------------

		void OnScrollToRequested(object sender, ScrollToRequestEventArgs e)
		{
			if (_disposed)
				return;

			var position = e.ScrollToPosition;

			// Deferred so the layout pass queued by the same batch of changes runs first: the
			// target offset is summed from the per-line extents LayoutItems computes, and inline
			// it would read lines that have not been built yet.
			GLib.Idle.Add(() =>
			{
				if (_disposed)
					return false;

				if (_linesDirty)
				{
					_linesDirty = false;
					RebuildLines();
				}

				var index = IndexOfRequest(e);

				if (index >= 0)
					ScrollToIndex(index, position);

				return false;
			});
		}

		/// <summary>
		/// Resolves a <see cref="ScrollToRequestEventArgs"/> to a slot index.
		/// </summary>
		/// <remarks>
		/// When grouped, <c>Index</c> is the index *within* the group and <c>GroupIndex</c> selects
		/// the group, so a flat lookup would scroll to the wrong item; ungrouped, <c>GroupIndex</c>
		/// is -1 and the flat item order is used. This runs against slots, not widgets, so it works
		/// for an item that has never been materialized.
		/// </remarks>
		int IndexOfRequest(ScrollToRequestEventArgs e)
		{
			if (e.Mode == ScrollToMode.Position)
			{
				if (e.GroupIndex >= 0)
					return _slots.FindIndex(s => s.Kind == HostKind.Item &&
						s.GroupIndex == e.GroupIndex && s.IndexInGroup == e.Index);

				var seen = 0;

				for (var i = 0; i < _slots.Count; i++)
				{
					if (_slots[i].Kind != HostKind.Item)
						continue;

					if (seen++ == e.Index)
						return i;
				}

				return -1;
			}

			if (e.Group != null)
				return _slots.FindIndex(s => s.Kind == HostKind.Item &&
					Equals(s.Group, e.Group) && Equals(s.Item, e.Item));

			return _slots.FindIndex(s => s.Kind == HostKind.Item && Equals(s.Item, e.Item));
		}

		/// <summary>
		/// Where the line holding <paramref name="slotIndex"/> starts along the scrolling axis, how
		/// long that line is, and the total content extent - all summed from the same per-line
		/// extents, which is what makes the three mutually consistent.
		/// </summary>
		/// <remarks>
		/// Shared by <see cref="ScrollToIndex"/> and the <c>ItemsUpdatingScrollMode</c> anchor, and
		/// deliberately one implementation rather than two: the arithmetic here - the header lead,
		/// the <c>(n - 1) x LineGap</c> term and the footer trail - is what §8.2.3 verified against a
		/// 10,000-item source landing at the exact unvirtualized pixel. A second copy of it would be
		/// a second chance to get that wrong.
		/// </remarks>
		bool TryMeasureSlot(int slotIndex, bool horizontal, out double offset, out double itemExtent, out double content)
		{
			offset = 0;
			itemExtent = 0;

			// The header is packed before the first line, so every item sits that much further
			// down the scrolling axis. Summing lines from zero would scroll the header's height
			// short on every single ScrollTo - silently, and only when a header exists.
			var lead = Extent(_header?.Host, horizontal);

			if (lead > 0)
				lead += LineGap;

			content = lead;

			var found = false;

			for (var i = 0; i < _lines.Count; i++)
			{
				var extent = LineExtent(i);
				var line = _lines[i];

				if (!found && slotIndex >= line.Start && slotIndex < line.Start + line.Count)
				{
					offset = content;
					itemExtent = extent;
					found = true;
				}

				content += extent;

				if (i < _lines.Count - 1)
					content += LineGap;
			}

			if (!found)
				return false;

			// The footer adds to the scrollable extent even though nothing scrolls *to* it; leaving
			// it out would make the clamp below cut a ScrollTo(last, End) short by its height.
			var trail = Extent(_footer?.Host, horizontal);

			if (trail > 0)
				content += LineGap + trail;

			return true;
		}

		void ScrollToIndex(int slotIndex, ScrollToPosition position)
		{
			if (Control == null || slotIndex < 0 || slotIndex >= _slots.Count || _lines.Count == 0)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;
			var adjustment = horizontal ? Control.Hadjustment : Control.Vadjustment;

			if (adjustment == null)
				return;

			if (!TryMeasureSlot(slotIndex, horizontal, out var offset, out var itemExtent, out var content))
				return;

			var viewportExtent = ViewportExtent(horizontal, adjustment);

			double value;

			switch (position)
			{
				case ScrollToPosition.Start:
					value = offset;
					break;

				case ScrollToPosition.Center:
					value = offset - (viewportExtent - itemExtent) / 2;
					break;

				case ScrollToPosition.End:
					value = offset - viewportExtent + itemExtent;
					break;

				default: // MakeVisible - only move if the item is not already fully on screen.
					if (offset < adjustment.Value)
						value = offset;
					else if (offset + itemExtent > adjustment.Value + viewportExtent)
						value = offset + itemExtent - viewportExtent;
					else
						return;
					break;
			}

			// Clamped against the content extent measured here, NOT against Adjustment.Upper.
			// Upper is only recomputed when the box is re-allocated, so right after rows are
			// added or removed it still describes the *previous* content - measured 446 (14
			// rows) for a box that already held 12. Clamping against a stale-large Upper scrolls
			// into blank space; against a stale-small one it silently truncates a legitimate
			// scroll. The per-line extents this method already sums are self-consistent, so they
			// are the honest bound. GTK re-clamps the value itself on the next allocation.
			var max = Math.Max(0, content - viewportExtent);
			var target = Math.Max(adjustment.Lower, Math.Min(value, adjustment.Lower + max));

			// Widening Upper is *not* the same mistake. Gtk.Adjustment's own setter clamps Value
			// into [Lower, Upper - PageSize], and with virtualization Upper is routinely far too
			// small for the target: the spacers that will carry the off-screen extent have not been
			// allocated yet. Without this, ScrollTo(9999) on a 10,000-item source silently lands
			// wherever the last allocation happened to end. The value is only ever widened, and the
			// next allocation replaces it with GTK's own.
			if (adjustment.Upper < adjustment.Lower + content)
				adjustment.Upper = adjustment.Lower + content;

			adjustment.Value = target;

			// The window is derived from the adjustment, so the new one has to be materialized.
			QueueItemsLayout();
		}

		double ViewportExtent(bool horizontal, Gtk.Adjustment adjustment)
		{
			var allocated = _viewport == null
				? 0
				: (horizontal ? _viewport.AllocatedWidth : _viewport.AllocatedHeight);

			if (allocated > 1)
				return allocated;

			if (adjustment != null && adjustment.PageSize > 0)
				return adjustment.PageSize;

			return Control == null ? 0 : (horizontal ? Control.AllocatedWidth : Control.AllocatedHeight);
		}

		/// <summary>
		/// The size of a decoration host along the scrolling axis.
		/// </summary>
		/// <remarks>
		/// The size *request* is preferred over <c>Allocation</c>, which is the opposite of what
		/// looks natural. `LayoutItems` sets the request from the Forms measure in this renderer's
		/// own idle callback, so it is correct the moment the content changes; <c>Allocation</c> is
		/// GTK's and only catches up on the next allocation cycle - which, headlessly, does not
		/// happen just because the main loop was drained (see plan section 10.2). Summing stale
		/// allocations made `ScrollTo` land a row off, non-deterministically.
		/// </remarks>
		static double Extent(Gtk.Widget host, bool horizontal)
		{
			if (host == null)
				return 0;

			var requested = horizontal ? host.WidthRequest : host.HeightRequest;

			if (requested > 0)
				return requested;

			var allocation = host.Allocation;
			var allocated = horizontal ? allocation.Width : allocation.Height;

			return allocated > 1 ? allocated : 0;
		}
	}
}
