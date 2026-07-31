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
	/// CollectionView on GTK: a <see cref="Gtk.Box"/> of per-item hosts (optionally grouped into
	/// per-line boxes for <see cref="GridItemsLayout"/>) inside a <see cref="Gtk.Viewport"/> inside a
	/// <see cref="Gtk.ScrolledWindow"/>.
	/// </summary>
	/// <remarks>
	/// <para><b>Not virtualized.</b> Every item in <c>ItemsSource</c> is materialized: a Forms view
	/// from the template, a renderer for it, and a <see cref="Gtk.EventBox"/> host. That is
	/// pathological on large sources and the plan says so explicitly (§8) - it is recorded as a
	/// known limitation rather than hidden. Virtualizing needs item extents *before* materializing,
	/// which the Forms measure API cannot give without instantiating the view, so it needs a
	/// uniform-extent fast path plus scroll-driven recycling; that is a separate piece of work.</para>
	///
	/// <para>Item views are not children of a Forms <see cref="Layout"/>, so nothing lays them out.
	/// This renderer measures and calls <c>view.Layout(...)</c> itself, exactly as
	/// <see cref="CarouselViewRenderer"/> does, and for the same reason sets <c>view.Parent</c>
	/// <b>before</b> <c>view.BindingContext</c> - parenting makes the view inherit the
	/// CollectionView's binding context and would otherwise overwrite the item.</para>
	///
	/// <para>Geometry is only ever mutated from a <see cref="GLib.Idle"/> callback, never from
	/// inside a size-allocate: GTK3 discards resizes queued during allocation and can wedge the
	/// subtree's resize machinery.</para>
	///
	/// <para><b>Widget-tree ownership.</b> Hosts are created detached and the deferred layout pass
	/// re-packs them (<see cref="RepackHosts"/>) whenever the structure changes. That is what lets
	/// one code path serve linear, grid and grouped layouts: the *only* thing that changes between
	/// them is how the flat host list is cut into lines.</para>
	/// </remarks>
	public class CollectionViewRenderer : ViewRenderer<CollectionView, Gtk.ScrolledWindow>
	{
		enum HostKind
		{
			Item,
			GroupHeader,
			GroupFooter
		}

		sealed class ItemHost
		{
			public object Item;
			public object Group;
			public int GroupIndex = -1;
			public int IndexInGroup = -1;
			public HostKind Kind;
			public View View;
			public IVisualElementRenderer Renderer;
			public Gtk.EventBox Host;
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
		/// One run of hosts across the cross axis. Lines stack along the scrolling axis.
		/// </summary>
		sealed class Line
		{
			public readonly List<ItemHost> Hosts = new List<ItemHost>();

			/// <summary>The line owns the whole cross axis: linear items, and group headers/footers.</summary>
			public bool Spanning;
		}

		static readonly Gdk.Color DefaultSelectionColor = Color.FromHex("#3498DB").ToGtkColor();

		readonly List<ItemHost> _hosts = new List<ItemHost>();
		readonly List<object> _items = new List<object>();
		readonly List<Gtk.Box> _lineBoxes = new List<Gtk.Box>();
		readonly List<INotifyCollectionChanged> _groupSources = new List<INotifyCollectionChanged>();

		Gtk.Viewport _viewport;
		Gtk.Box _itemsBox;

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
		bool _disposed;

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

				UnsubscribeSource();
				UnsubscribeItemsLayout();
				ClearItems();
				DestroyDecoration(ref _empty);
				DestroyDecoration(ref _header);
				DestroyDecoration(ref _footer);
				ClearLineBoxes();

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
		/// and linear share one layout, one re-pack and one <c>ScrollTo</c>.
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
				_repackNeeded = true;

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

			// A change inside a group shifts every following header/footer/item in the flat host
			// list, so the incremental path does not apply. Correctness over cleverness here.
			ReloadItems();
		}

		void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_disposed || _itemsBox == null)
				return;

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
			// otherwise. A full reload is always correct but throws away every realized
			// renderer, so it is the fallback rather than the default.
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
					BuildGroupedHosts(source);
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
		/// Flattens <c>ItemsSource</c>-of-groups into the single ordered host list everything else
		/// in this renderer works against: header, items, footer, per group.
		/// </summary>
		void BuildGroupedHosts(IEnumerable source)
		{
			var groupIndex = 0;

			foreach (var group in source)
			{
				AppendGroupHost(HostKind.GroupHeader, Element.GroupHeaderTemplate, group, groupIndex);

				// A string is IEnumerable but is never a group of items; treating it as one would
				// silently explode a group into characters.
				if (group is IEnumerable inner && !(group is string))
				{
					var indexInGroup = 0;

					foreach (var item in inner)
						AppendItemHost(item, group, groupIndex, indexInGroup++);
				}

				AppendGroupHost(HostKind.GroupFooter, Element.GroupFooterTemplate, group, groupIndex);

				groupIndex++;
			}
		}

		void AppendGroupHost(HostKind kind, DataTemplate template, object group, int groupIndex)
		{
			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(group, Element);

			// No template means no header/footer at all: fabricating one would invent content the
			// application never asked for.
			if (template == null)
				return;

			if (!(template.CreateContent() is View view))
				return;

			// Parent first, then BindingContext - see CreateItemView.
			view.Parent = Element;
			view.BindingContext = group;

			var entry = CreateHost(view, kind);

			entry.Group = group;
			entry.GroupIndex = groupIndex;

			_hosts.Add(entry);
			_repackNeeded = true;
		}

		void AppendItemHost(object item, object group, int groupIndex, int indexInGroup)
		{
			var view = CreateItemView(item);

			if (view == null)
				return;

			var entry = CreateHost(view, HostKind.Item);

			entry.Item = item;
			entry.Group = group;
			entry.GroupIndex = groupIndex;
			entry.IndexInGroup = indexInGroup;

			_items.Add(item);
			_hosts.Add(entry);
			_repackNeeded = true;
		}

		void InsertItem(int index, object item)
		{
			if (_itemsBox == null)
				return;

			if (index < 0 || index > _items.Count)
				index = _items.Count;

			var view = CreateItemView(item);

			if (view == null)
				return;

			var entry = CreateHost(view, HostKind.Item);

			entry.Item = item;
			entry.IndexInGroup = index;

			_items.Insert(index, item);
			_hosts.Insert(index, entry);
			_repackNeeded = true;
		}

		/// <summary>
		/// Creates the renderer and the host <see cref="Gtk.EventBox"/> for a materialized view. The
		/// host is deliberately left <b>unparented</b>: <see cref="RepackHosts"/> owns placement, so
		/// linear, grid and grouped structures share one path instead of three.
		/// </summary>
		ItemHost CreateHost(View view, HostKind kind)
		{
			var renderer = Platform.CreateRenderer(view);
			Platform.SetRenderer(view, renderer);

			var host = new HostBox { VisibleWindow = false };
			host.Add(renderer.Container);

			var entry = new ItemHost
			{
				Kind = kind,
				View = view,
				Renderer = renderer,
				Host = host
			};

			// Only items are selectable; a group header must not react to a click.
			if (kind == HostKind.Item)
				host.ButtonPressEvent += OnItemButtonPress;

			host.ShowAll();

			return entry;
		}

		void RemoveItemAt(int index)
		{
			if (index < 0 || index >= _hosts.Count)
				return;

			var entry = _hosts[index];

			_hosts.RemoveAt(index);
			_items.RemoveAt(index);

			DestroyHost(entry);
			_repackNeeded = true;
		}

		void ClearItems()
		{
			foreach (var entry in _hosts.ToList())
				DestroyHost(entry);

			_hosts.Clear();
			_items.Clear();
			ClearLineBoxes();
			_repackNeeded = true;
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

		void DestroyHost(ItemHost entry)
		{
			if (entry == null)
				return;

			if (entry.Kind == HostKind.Item)
				entry.Host.ButtonPressEvent -= OnItemButtonPress;

			Detach(entry.Host);

			entry.Renderer?.Dispose();

			if (entry.View != null)
			{
				Platform.SetRenderer(entry.View, null);
				entry.View.Parent = null;
			}

			entry.Host.Destroy();
		}

		static void Detach(Gtk.Widget widget)
		{
			if (widget?.Parent is Gtk.Container container)
				container.Remove(widget);
		}

		View CreateItemView(object item)
		{
			var template = Element?.ItemTemplate;

			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(item, Element);

			View view = null;

			if (template != null)
				view = template.CreateContent() as View;

			// No template: Forms' documented fallback is the item's own text.
			if (view == null)
				view = new Label { Text = item?.ToString() ?? string.Empty, Margin = new Thickness(6) };

			// Parent first, then BindingContext: parenting wires the element chain and would
			// otherwise overwrite the binding context by inheriting the CollectionView's.
			view.Parent = Element;
			view.BindingContext = item;

			return view;
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
		/// Like item hosts, the host is left <b>unparented</b>: <see cref="RepackHosts"/> decides
		/// where it goes, so a decoration appearing or disappearing cannot get out of step with the
		/// item structure around it.
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
					// Parent first, then BindingContext - see CreateItemView.
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

		// ---- lines ---------------------------------------------------------------------------

		/// <summary>
		/// Cuts the flat host list into lines. This is the <b>only</b> place layout mode matters:
		/// linear gives one spanning line per host, grid packs up to <c>Span</c> item hosts per
		/// line, and a group header/footer always takes a spanning line of its own.
		/// </summary>
		List<Line> BuildLines()
		{
			var lines = new List<Line>();
			var span = _isGrid ? Math.Max(1, _span) : 1;
			Line current = null;

			foreach (var entry in _hosts)
			{
				if (!_isGrid || entry.Kind != HostKind.Item)
				{
					current = null;

					var spanning = new Line { Spanning = true };
					spanning.Hosts.Add(entry);
					lines.Add(spanning);

					continue;
				}

				if (current == null || current.Hosts.Count >= span)
				{
					current = new Line();
					lines.Add(current);
				}

				current.Hosts.Add(entry);
			}

			return lines;
		}

		/// <summary>
		/// Rebuilds the native child structure from the current host list.
		/// </summary>
		/// <remarks>
		/// Spanning lines are packed straight into <c>_itemsBox</c>, so a linear CollectionView has
		/// exactly the tree it had before grid support existed - hosts as direct children. Grid
		/// lines get an intermediate <see cref="Gtk.Box"/> running across the cross axis.
		/// Re-parenting a host is safe here because the managed wrapper holds its own reference:
		/// <c>gtk_container_remove</c>'s unref cannot drop it to zero.
		/// </remarks>
		void RepackHosts()
		{
			if (_itemsBox == null)
				return;

			foreach (var entry in _hosts)
			{
				Detach(entry.Host);

				// Drop the previous pass' size request before re-packing. It was computed for the
				// *previous* line structure, and a Gtk.Box asks GTK for the sum of its children's
				// requests: re-packing hosts that still request half the viewport three-to-a-line
				// makes this control demand 3 x (w/2) as its *minimum* width. GTK grows the
				// toplevel to satisfy a minimum, the next layout pass then derives the item width
				// from the grown viewport, and the control never shrinks back - measured as a
				// 540px grid ratcheting to 803px after a Span 3 -> 2 -> 3 round trip, wider than
				// the screen and painting nothing where it used to be. LayoutLine re-applies the
				// correct request further down this same idle callback, before anything is drawn.
				entry.Host.SetSizeRequest(-1, -1);
			}

			_header?.Host.SetSizeRequest(-1, -1);
			_footer?.Host.SetSizeRequest(-1, -1);
			_empty?.Host.SetSizeRequest(-1, -1);

			Detach(_header?.Host);
			Detach(_empty?.Host);
			Detach(_footer?.Host);
			ClearLineBoxes();

			// The header leads and the footer trails along the scrolling axis, so they are simply
			// the first and last children of the items box; Gtk.Box already spaces them from the
			// lines by the same ItemSpacing everything else uses.
			if (_header?.Host != null)
				_itemsBox.PackStart(_header.Host, false, false, 0);

			var lineOrientation = _orientation == ItemsLayoutOrientation.Horizontal
				? Gtk.Orientation.Vertical
				: Gtk.Orientation.Horizontal;

			var within = (int)Math.Round(Math.Max(0, _withinLineSpacing));

			foreach (var line in BuildLines())
			{
				if (line.Spanning)
				{
					_itemsBox.PackStart(line.Hosts[0].Host, false, false, 0);
					continue;
				}

				var box = new Gtk.Box(lineOrientation, within);

				foreach (var entry in line.Hosts)
					box.PackStart(entry.Host, false, false, 0);

				_itemsBox.PackStart(box, false, false, 0);
				_lineBoxes.Add(box);

				box.Show();
			}

			if (_empty?.Host != null)
				_itemsBox.PackStart(_empty.Host, true, true, 0);

			if (_footer?.Host != null)
				_itemsBox.PackStart(_footer.Host, false, false, 0);
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

		void LayoutItems()
		{
			if (_disposed || Control == null || Element == null)
				return;

			// The re-pack has to happen even when the viewport has no size yet, otherwise hosts
			// created before the first allocation would never be parented at all.
			if (_repackNeeded)
			{
				_repackNeeded = false;
				RepackHosts();
			}

			var width = _viewport != null && _viewport.AllocatedWidth > 1
				? _viewport.AllocatedWidth
				: Control.AllocatedWidth;

			var height = _viewport != null && _viewport.AllocatedHeight > 1
				? _viewport.AllocatedHeight
				: Control.AllocatedHeight;

			if (width <= 1 || height <= 1)
				return;

			LayoutDecoration(_header, width, height);

			foreach (var line in BuildLines())
				LayoutLine(line, width, height);

			LayoutDecoration(_footer, width, height);

			// The EmptyView is the one decoration that fills what is left rather than taking its
			// natural size: an empty list should show it across the whole viewport, not as a
			// one-line strip at the top.
			if (_empty?.View != null)
			{
				_empty.View.Layout(new Rectangle(0, 0, width, height));
				_empty.Host.SetSizeRequest(width, height);
			}
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
		/// Measures and lays out one line.
		/// </summary>
		/// <remarks>
		/// The cross-axis extent is fixed first (the viewport for a spanning line, an equal share of
		/// it for a grid line) and the main-axis extent is then the largest measurement in the line,
		/// applied to every host in it - which is what makes a grid a grid rather than a ragged set
		/// of columns. Measuring is why this must never run inside a size-allocate.
		/// </remarks>
		void LayoutLine(Line line, int width, int height)
		{
			if (line.Hosts.Count == 0)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;
			var cross = horizontal ? height : width;
			var span = line.Spanning ? 1 : Math.Max(1, _span);
			var within = (int)Math.Round(Math.Max(0, _withinLineSpacing));

			var itemCross = span <= 1
				? cross
				: Math.Max(1, (cross - (span - 1) * within) / span);

			var main = 1;

			foreach (var entry in line.Hosts)
			{
				if (entry.View == null)
					continue;

				var request = horizontal
					? entry.View.Measure(double.PositiveInfinity, itemCross, MeasureFlags.IncludeMargins)
					: entry.View.Measure(itemCross, double.PositiveInfinity, MeasureFlags.IncludeMargins);

				var measured = (int)Math.Ceiling(Math.Max(1,
					horizontal ? request.Request.Width : request.Request.Height));

				if (measured > main)
					main = measured;
			}

			foreach (var entry in line.Hosts)
			{
				if (entry.View == null)
					continue;

				if (horizontal)
				{
					entry.View.Layout(new Rectangle(0, 0, main, itemCross));
					entry.Host.SetSizeRequest(main, itemCross);
				}
				else
				{
					entry.View.Layout(new Rectangle(0, 0, itemCross, main));
					entry.Host.SetSizeRequest(itemCross, main);
				}
			}
		}

		// ---- selection -----------------------------------------------------------------------

		void OnItemButtonPress(object o, Gtk.ButtonPressEventArgs args)
		{
			if (_disposed || Element == null)
				return;

			var entry = _hosts.FirstOrDefault(h => h.Kind == HostKind.Item && ReferenceEquals(h.Host, o));

			if (entry == null)
				return;

			switch (Element.SelectionMode)
			{
				case SelectionMode.Single:
					Element.SetValueFromRenderer(SelectableItemsView.SelectedItemProperty, entry.Item);
					break;

				case SelectionMode.Multiple:
					var selection = Element.SelectedItems == null
						? new List<object>()
						: new List<object>(Element.SelectedItems);

					if (selection.Contains(entry.Item))
						selection.Remove(entry.Item);
					else
						selection.Add(entry.Item);

					Element.UpdateSelectedItems(selection);
					break;
			}
		}

		/// <summary>
		/// Paints the selected state.
		/// </summary>
		/// <remarks>
		/// Two mechanisms, deliberately: the host <see cref="Gtk.EventBox"/> gets a real window and a
		/// selection background (which an opaque item template will cover - it sits *under* the
		/// item, exactly as a cell background does on the other platforms), and the item view is put
		/// into the <c>Selected</c> visual state so a template that defines one can render the
		/// selection itself. Without the second, an application whose rows have a background has no
		/// way to show selection at all.
		/// </remarks>
		void UpdateSelectionVisuals()
		{
			if (_disposed || Element == null)
				return;

			var mode = Element.SelectionMode;

			foreach (var entry in _hosts)
			{
				bool selected;

				if (entry.Kind != HostKind.Item)
					selected = false;
				else if (mode == SelectionMode.Single)
					selected = Equals(Element.SelectedItem, entry.Item);
				else if (mode == SelectionMode.Multiple)
					selected = Element.SelectedItems != null && Element.SelectedItems.Contains(entry.Item);
				else
					selected = false;

				// A no-window EventBox paints nothing of its own, so the background only exists
				// once the host owns a GdkWindow.
				entry.Host.VisibleWindow = selected;

				if (selected)
					entry.Host.SetBackgroundColor(DefaultSelectionColor, Gtk.StateType.Normal);
				else
					entry.Host.ClearStyle();

				if (entry.View != null)
					VisualStateManager.GoToState(entry.View,
						selected ? VisualStateManager.CommonStates.Selected : VisualStateManager.CommonStates.Normal);
			}
		}

		// ---- ScrollTo ------------------------------------------------------------------------

		void OnScrollToRequested(object sender, ScrollToRequestEventArgs e)
		{
			if (_disposed)
				return;

			var index = IndexOfRequest(e);

			if (index < 0)
				return;

			var position = e.ScrollToPosition;

			// Deferred so the layout pass queued by the same batch of changes runs first: the
			// target offset is summed from the per-host sizes LayoutItems computes, and inline
			// it would read rows that have not been measured yet (offset ~0 every time).
			GLib.Idle.Add(() =>
			{
				if (!_disposed)
					ScrollToIndex(index, position);

				return false;
			});
		}

		/// <summary>
		/// Resolves a <see cref="ScrollToRequestEventArgs"/> to a host index.
		/// </summary>
		/// <remarks>
		/// When grouped, <c>Index</c> is the index *within* the group and <c>GroupIndex</c> selects
		/// the group, so a flat lookup would scroll to the wrong item; ungrouped, <c>GroupIndex</c>
		/// is -1 and the flat item order is used.
		/// </remarks>
		int IndexOfRequest(ScrollToRequestEventArgs e)
		{
			if (e.Mode == ScrollToMode.Position)
			{
				if (e.GroupIndex >= 0)
					return _hosts.FindIndex(h => h.Kind == HostKind.Item &&
						h.GroupIndex == e.GroupIndex && h.IndexInGroup == e.Index);

				var seen = 0;

				for (var i = 0; i < _hosts.Count; i++)
				{
					if (_hosts[i].Kind != HostKind.Item)
						continue;

					if (seen++ == e.Index)
						return i;
				}

				return -1;
			}

			if (e.Group != null)
				return _hosts.FindIndex(h => h.Kind == HostKind.Item &&
					Equals(h.Group, e.Group) && Equals(h.Item, e.Item));

			return _hosts.FindIndex(h => h.Kind == HostKind.Item && Equals(h.Item, e.Item));
		}

		void ScrollToIndex(int index, ScrollToPosition position)
		{
			if (Control == null || index < 0 || index >= _hosts.Count)
				return;

			var horizontal = _orientation == ItemsLayoutOrientation.Horizontal;
			var adjustment = horizontal ? Control.Hadjustment : Control.Vadjustment;

			if (adjustment == null)
				return;

			var target = _hosts[index];
			var lines = BuildLines();

			// The header is packed before the first line, so every item sits that much further
			// down the scrolling axis. Summing lines from zero would scroll the header's height
			// short on every single ScrollTo - silently, and only when a header exists.
			var lead = Extent(_header?.Host, horizontal);

			if (lead > 0)
				lead += _lineSpacing;

			double offset = 0;
			double content = lead;
			double itemExtent = 0;
			var found = false;

			for (var i = 0; i < lines.Count; i++)
			{
				var extent = LineExtent(lines[i], horizontal);

				if (!found && lines[i].Hosts.Contains(target))
				{
					offset = content;
					itemExtent = extent;
					found = true;
				}

				content += extent;

				if (i < lines.Count - 1)
					content += _lineSpacing;
			}

			if (!found)
				return;

			// The footer adds to the scrollable extent even though nothing scrolls *to* it; leaving
			// it out would make the clamp below cut a ScrollTo(last, End) short by its height.
			var trail = Extent(_footer?.Host, horizontal);

			if (trail > 0)
				content += _lineSpacing + trail;

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
			// scroll. The per-host extents this method already sums are self-consistent, so they
			// are the honest bound. GTK re-clamps the value itself on the next allocation.
			var max = Math.Max(0, content - viewportExtent);

			adjustment.Value = Math.Max(adjustment.Lower, Math.Min(value, adjustment.Lower + max));
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

			return horizontal ? Control.AllocatedWidth : Control.AllocatedHeight;
		}

		/// <summary>The size of a line along the scrolling axis: the largest of its hosts.</summary>
		static double LineExtent(Line line, bool horizontal)
		{
			double extent = 0;

			foreach (var entry in line.Hosts)
			{
				var candidate = Extent(entry, horizontal);

				if (candidate > extent)
					extent = candidate;
			}

			return extent;
		}

		/// <summary>
		/// The size of a host along the scrolling axis.
		/// </summary>
		/// <remarks>
		/// The size *request* is preferred over <c>Allocation</c>, which is the opposite of what
		/// looks natural. `LayoutItems` sets the request from the Forms measure in this renderer's
		/// own idle callback, so it is correct the moment a row is added or removed;
		/// <c>Allocation</c> is GTK's and only catches up on the next allocation cycle - which,
		/// headlessly, does not happen just because the main loop was drained (see §10.2). Summing
		/// stale allocations made `ScrollTo` land a row off, non-deterministically. Allocation is
		/// still the fallback for a host that has no request yet (viewport not allocated when the
		/// layout pass ran).
		/// </remarks>
		static double Extent(ItemHost entry, bool horizontal) => Extent(entry?.Host, horizontal);

		/// <summary>
		/// The size of any host widget along the scrolling axis - item hosts and the header/footer
		/// alike, so a header can never be measured by a different rule than the rows below it.
		/// </summary>
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
