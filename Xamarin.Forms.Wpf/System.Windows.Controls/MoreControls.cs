using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls
{
	// ---- tabs -------------------------------------------------------------------------------------------------------

	public enum TabStripPlacement
	{
		Left,
		Top,
		Right,
		Bottom,
	}

	/// <summary>A tab: its header in the strip, its content shown while it is selected.</summary>
	public class TabItem : HeaderedContentControl
	{
		public static readonly XF.BindableProperty IsSelectedProperty = Dp.Register<TabItem>(nameof(IsSelected), typeof(bool), false);

		public bool IsSelected
		{
			get => Get<bool>(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		internal TabControl TabControlParent => LogicalParent as TabControl;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == IsSelectedProperty && IsSelected && TabControlParent is TabControl tabs && !ReferenceEquals(tabs.SelectedItem, this))
				tabs.SelectedItem = this;
			else if (p == HeaderProperty || p == IsEnabledProperty || p == VisibilityProperty)
				TabControlParent?.ApplyItems(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			else if (p == ContentProperty && IsSelected)
				TabControlParent?.ApplySelection();
		}

		/// <summary>A tab draws nothing itself: the tab control shows its header and its content.</summary>
		internal override XF.View CreateNativeView() => new XF.ContentView { IsVisible = false };
	}

	/// <summary>A strip of tab headers over the selected tab's content. The first tab is selected as soon as there is one.</summary>
	public class TabControl : Selector
	{
		public static readonly XF.BindableProperty TabStripPlacementProperty = Dp.Register<TabControl>(nameof(TabStripPlacement), typeof(TabStripPlacement), TabStripPlacement.Top);

		XF.StackLayout _strip;
		XF.ContentView _content;

		public TabControl() => SetValue(BorderThicknessProperty, new Thickness(1));

		public TabStripPlacement TabStripPlacement
		{
			get => Get<TabStripPlacement>(TabStripPlacementProperty);
			set => SetValue(TabStripPlacementProperty, value);
		}

		public object SelectedContent => (SelectedItem as TabItem)?.Content ?? SelectedItem;

		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			if (SelectedIndex < 0 && Items.Count > 0)
				Select(0, false);
		}

		internal override void SyncContainers(object oldItem, object newItem)
		{
			if (oldItem is TabItem oldTab && !ReferenceEquals(oldItem, newItem))
				oldTab.IsSelected = false;
			if (newItem is TabItem newTab)
				newTab.IsSelected = true;
		}

		internal override XF.View CreateNativeView()
		{
			_strip = new XF.StackLayout { Orientation = XF.StackOrientation.Horizontal, Spacing = 2, Padding = new XF.Thickness(2, 2, 2, 0) };
			_content = new XF.ContentView { Padding = new XF.Thickness(1) };
			var frame = new XF.Frame { HasShadow = false, CornerRadius = 0, Padding = 0, Content = _content, BorderColor = SystemColors.ControlDarkBrush.ToFormsColor() };
			return new XF.Grid
			{
				RowSpacing = 0,
				RowDefinitions = { new XF.RowDefinition { Height = XF.GridLength.Auto }, new XF.RowDefinition { Height = XF.GridLength.Star } },
				Children = { { _strip, 0, 0 }, { frame, 0, 1 } },
			};
		}

		internal override void ApplyBorder()
		{
		}

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
			if (_strip == null)
				return;

			_strip.Children.Clear();
			for (var i = 0; i < Items.Count; i++)
			{
				var item = Items[i];
				if (item is UIElement u && u.Visibility == Visibility.Collapsed)
					continue;

				var index = i;
				var header = new XF.Label
				{
					Text = ItemText(item),
					Padding = new XF.Thickness(8, 3),
					BackgroundColor = SystemColors.ControlBrush.ToFormsColor(),
					IsEnabled = !(item is UIElement element) || element.IsEnabled,
				};
				NativeText.ApplyFont(header, this);
				var tap = new XF.TapGestureRecognizer();
				tap.Tapped += (s, a) => Select(index, false);
				header.GestureRecognizers.Add(tap);
				_strip.Children.Add(header);
			}

			ApplySelection();
		}

		/// <summary>The selected tab's header raised, its content shown.</summary>
		internal override void ApplySelection()
		{
			if (_strip == null)
				return;

			var visible = Items.Cast<object>().Where(i => !(i is UIElement u) || u.Visibility != Visibility.Collapsed).ToList();
			for (var i = 0; i < _strip.Children.Count && i < visible.Count; i++)
			{
				var selected = ReferenceEquals(visible[i], SelectedItem);
				((XF.Label)_strip.Children[i]).BackgroundColor = selected ? SystemColors.WindowBrush.ToFormsColor() : SystemColors.ControlBrush.ToFormsColor();
				((XF.Label)_strip.Children[i]).FontAttributes = selected ? XF.FontAttributes.Bold : XF.FontAttributes.None;
			}

			var content = SelectedContent;
			_content.Content = content is UIElement element ? element.NativeView : content == null ? null : new XF.Label { Text = content.ToString() };
		}
	}

	// ---- tree ------------------------------------------------------------------------------------------------------

	/// <summary>A node: its header, its child nodes, whether they show, whether it is the selected one.</summary>
	public class TreeViewItem : HeaderedItemsControl
	{
		public static readonly XF.BindableProperty IsExpandedProperty = Dp.Register<TreeViewItem>(nameof(IsExpanded), typeof(bool), false);
		public static readonly XF.BindableProperty IsSelectedProperty = Dp.Register<TreeViewItem>(nameof(IsSelected), typeof(bool), false);

		public static readonly RoutedEvent ExpandedEvent =
			EventManager.RegisterRoutedEvent("Expanded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

		public static readonly RoutedEvent CollapsedEvent =
			EventManager.RegisterRoutedEvent("Collapsed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

		public static readonly RoutedEvent SelectedEvent =
			EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

		public static readonly RoutedEvent UnselectedEvent =
			EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

		public bool IsExpanded
		{
			get => Get<bool>(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		public bool IsSelected
		{
			get => Get<bool>(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public bool IsSelectionActive => IsSelected;

		public event RoutedEventHandler Expanded { add => AddHandler(ExpandedEvent, value); remove => RemoveHandler(ExpandedEvent, value); }

		public event RoutedEventHandler Collapsed { add => AddHandler(CollapsedEvent, value); remove => RemoveHandler(CollapsedEvent, value); }

		public event RoutedEventHandler Selected { add => AddHandler(SelectedEvent, value); remove => RemoveHandler(SelectedEvent, value); }

		public event RoutedEventHandler Unselected { add => AddHandler(UnselectedEvent, value); remove => RemoveHandler(UnselectedEvent, value); }

		protected virtual void OnExpanded(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnCollapsed(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnSelected(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnUnselected(RoutedEventArgs e) => RaiseEvent(e);

		public void ExpandSubtree()
		{
			IsExpanded = true;
			foreach (var child in Items.OfType<TreeViewItem>())
				child.ExpandSubtree();
		}

		internal TreeView ParentTreeView
		{
			get
			{
				for (var d = LogicalParent; d != null; d = d.LogicalParent)
				{
					if (d is TreeView tree)
						return tree;
				}

				return null;
			}
		}

		internal int Depth
		{
			get
			{
				var depth = 0;
				for (var d = LogicalParent; d != null && !(d is TreeView); d = d.LogicalParent)
					depth++;
				return depth;
			}
		}

		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			ParentTreeView?.Refresh();
		}

		internal override void OnHeaderChanged() => ParentTreeView?.Refresh();

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == IsExpandedProperty)
			{
				if (IsExpanded)
					OnExpanded(new RoutedEventArgs(ExpandedEvent, this));
				else
					OnCollapsed(new RoutedEventArgs(CollapsedEvent, this));
				ParentTreeView?.Refresh();
			}
			else if (p == IsSelectedProperty)
			{
				if (IsSelected)
				{
					OnSelected(new RoutedEventArgs(SelectedEvent, this));
					ParentTreeView?.OnItemSelected(this);
				}
				else
				{
					OnUnselected(new RoutedEventArgs(UnselectedEvent, this));
					ParentTreeView?.OnItemUnselected(this);
				}
			}
		}

		/// <summary>A node draws nothing itself: the tree view shows its visible nodes as rows.</summary>
		internal override XF.View CreateNativeView() => new XF.ContentView { IsVisible = false };

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
		}
	}

	/// <summary>A row of the tree view: a visible node, its depth and its text.</summary>
	internal sealed class TreeRow : INotifyPropertyChanged
	{
		internal TreeRow(object node, int depth, string text, bool hasChildren, bool expanded)
		{
			Node = node;
			Indent = depth * 16;
			Text = text;
			Glyph = hasChildren ? (expanded ? "▾" : "▸") : " ";
		}

		public object Node { get; }

		public double Indent { get; }

		public string Text { get; }

		public string Glyph { get; }

		public event PropertyChangedEventHandler PropertyChanged
		{
			add { }
			remove { }
		}
	}

	/// <summary>
	/// A tree over a Xamarin.Forms collection view of its visible nodes: expanding a node (its glyph) shows its
	/// children below it, indented; selecting a row selects its node.
	/// </summary>
	public class TreeView : ItemsControl
	{
		public static readonly RoutedEvent SelectedItemChangedEvent =
			EventManager.RegisterRoutedEvent("SelectedItemChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(TreeView));

		public static readonly XF.BindableProperty SelectedValuePathProperty = Dp.Register<TreeView>(nameof(SelectedValuePath), typeof(string), string.Empty);

		readonly ObservableCollection<TreeRow> _rows = new ObservableCollection<TreeRow>();
		object _selectedItem;
		bool _syncing;

		public TreeView() => SetValue(BorderThicknessProperty, new Thickness(1));

		public object SelectedItem => _selectedItem;

		public object SelectedValue => PropertyOf(_selectedItem is TreeViewItem t ? t.Header : _selectedItem, SelectedValuePath);

		public string SelectedValuePath
		{
			get => Get<string>(SelectedValuePathProperty);
			set => SetValue(SelectedValuePathProperty, value);
		}

		public event RoutedPropertyChangedEventHandler<object> SelectedItemChanged { add => AddHandler(SelectedItemChangedEvent, value); remove => RemoveHandler(SelectedItemChangedEvent, value); }

		protected virtual void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e) => RaiseEvent(e);

		internal void OnItemSelected(TreeViewItem item)
		{
			if (ReferenceEquals(_selectedItem, item))
				return;

			var old = _selectedItem;
			_selectedItem = item;
			if (old is TreeViewItem previous)
				previous.IsSelected = false;

			ApplySelection();
			OnSelectedItemChanged(new RoutedPropertyChangedEventArgs<object>(old, item, SelectedItemChangedEvent) { Source = this });
		}

		internal void OnItemUnselected(TreeViewItem item)
		{
			if (!ReferenceEquals(_selectedItem, item))
				return;

			_selectedItem = null;
			ApplySelection();
			OnSelectedItemChanged(new RoutedPropertyChangedEventArgs<object>(item, null, SelectedItemChangedEvent) { Source = this });
		}

		XF.CollectionView Collection => (NativeView as NativeShell)?.Inner as XF.CollectionView;

		internal override XF.View CreateNativeView()
		{
			var view = new XF.CollectionView { ItemsSource = _rows, SelectionMode = XF.SelectionMode.Single, ItemTemplate = RowTemplate() };
			view.SelectionChanged += (s, e) =>
			{
				if (_syncing)
					return;

				switch ((e.CurrentSelection.FirstOrDefault() as TreeRow)?.Node)
				{
					case TreeViewItem node:
						node.IsSelected = true;
						break;
					case object data:
						SelectData(data);
						break;
				}
			};
			return new NativeShell(view);
		}

		void SelectData(object data)
		{
			var old = _selectedItem;
			(old as TreeViewItem)?.SetValue(TreeViewItem.IsSelectedProperty, false);
			_selectedItem = data;
			OnSelectedItemChanged(new RoutedPropertyChangedEventArgs<object>(old, data, SelectedItemChangedEvent) { Source = this });
		}

		XF.DataTemplate RowTemplate() => new XF.DataTemplate(() =>
		{
			var glyph = new XF.Label { WidthRequest = 14, HorizontalTextAlignment = XF.TextAlignment.Center };
			glyph.SetBinding(XF.Label.TextProperty, new XF.Binding(nameof(TreeRow.Glyph)));
			glyph.SetBinding(XF.View.MarginProperty, new XF.Binding(nameof(TreeRow.Indent), converter: new IndentConverter()));
			var tap = new XF.TapGestureRecognizer();
			tap.Tapped += (s, e) =>
			{
				if (glyph.BindingContext is TreeRow row && row.Node is TreeViewItem node && node.HasItems)
					node.IsExpanded = !node.IsExpanded;
			};
			glyph.GestureRecognizers.Add(tap);

			var text = new XF.Label { Padding = new XF.Thickness(2, 1) };
			text.SetBinding(XF.Label.TextProperty, new XF.Binding(nameof(TreeRow.Text)));
			NativeText.ApplyFont(text, this);
			NativeText.ApplyForeground(text, this);
			return new XF.StackLayout { Orientation = XF.StackOrientation.Horizontal, Spacing = 0, Children = { glyph, text } };
		});

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e) => Refresh();

		/// <summary>The rows again: every node whose ancestors are all expanded, in order.</summary>
		internal void Refresh()
		{
			if (!HasNativeView)
				return;

			_syncing = true;
			try
			{
				_rows.Clear();
				void Add(IEnumerable items, int depth)
				{
					foreach (var item in items)
					{
						if (item is TreeViewItem node)
						{
							if (node.Visibility == Visibility.Collapsed)
								continue;
							_rows.Add(new TreeRow(node, depth, node.HeaderText, node.HasItems, node.IsExpanded));
							if (node.IsExpanded)
								Add(node.Items, depth + 1);
						}
						else
						{
							_rows.Add(new TreeRow(item, depth, ItemText(item), false, false));
						}
					}
				}

				Add(Items, 0);
			}
			finally
			{
				_syncing = false;
			}

			ApplySelection();
		}

		void ApplySelection()
		{
			if (!(Collection is XF.CollectionView view))
				return;

			_syncing = true;
			try
			{
				view.SelectedItem = _rows.FirstOrDefault(r => ReferenceEquals(r.Node, _selectedItem));
			}
			finally
			{
				_syncing = false;
			}
		}

		sealed class IndentConverter : XF.IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
				new XF.Thickness(value is double d ? d : 0, 0, 0, 0);

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
		}
	}

	// ---- bars ------------------------------------------------------------------------------------------------------

	/// <summary>A strip of buttons and separators; a click of any of them bubbles to the tool bar as <c>ButtonBase.Click</c>.</summary>
	public class ToolBar : HeaderedItemsControl
	{
		public static readonly XF.BindableProperty OrientationProperty = Dp.Register<ToolBar>(nameof(Orientation), typeof(Orientation), Orientation.Horizontal);
		public static readonly XF.BindableProperty BandProperty = Dp.Register<ToolBar>(nameof(Band), typeof(int), 0);
		public static readonly XF.BindableProperty BandIndexProperty = Dp.Register<ToolBar>(nameof(BandIndex), typeof(int), 0);

		public ToolBar()
		{
			SetValue(BackgroundProperty, SystemColors.ControlBrush);
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
		}

		public Orientation Orientation => Get<Orientation>(OrientationProperty);

		public int Band
		{
			get => Get<int>(BandProperty);
			set => SetValue(BandProperty, value);
		}

		public int BandIndex
		{
			get => Get<int>(BandIndexProperty);
			set => SetValue(BandIndexProperty, value);
		}

		public static readonly object ButtonStyleKey = new SystemResourceKeyHolder("ToolBar.ButtonStyleKey");

		public static readonly object SeparatorStyleKey = new SystemResourceKeyHolder("ToolBar.SeparatorStyleKey");

		internal override XF.View CreateNativeView() =>
			new NativeShell(new XF.StackLayout { Orientation = XF.StackOrientation.Horizontal, Spacing = 2, Padding = new XF.Thickness(2) });

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
			if (!(((NativeShell)NativeView).Inner is XF.StackLayout stack))
				return;

			stack.Children.Clear();
			foreach (var item in Items)
				stack.Children.Add(ViewOf(item));
		}

		sealed class SystemResourceKeyHolder
		{
			readonly string _name;

			public SystemResourceKeyHolder(string name) => _name = name;

			public override string ToString() => _name;
		}
	}

	public class ToolBarTray : FrameworkElement
	{
		readonly Collection<ToolBar> _toolBars = new Collection<ToolBar>();

		public Collection<ToolBar> ToolBars => _toolBars;
	}
}

namespace System.Windows.Controls.Primitives
{
	public class StatusBarItem : ContentControl
	{
		public StatusBarItem()
		{
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
			SetValue(PaddingProperty, new Thickness(3, 0, 3, 0));
			SetValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
		}
	}

	/// <summary>A strip of items along the bottom; an item that is not a status-bar item is wrapped in one.</summary>
	public class StatusBar : ItemsControl
	{
		public StatusBar()
		{
			SetValue(BackgroundProperty, SystemColors.ControlBrush);
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
		}

		internal override XF.View CreateNativeView() =>
			new NativeShell(new XF.StackLayout { Orientation = XF.StackOrientation.Horizontal, Spacing = 1 });

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
			if (!(((NativeShell)NativeView).Inner is XF.StackLayout stack))
				return;

			stack.Children.Clear();
			foreach (var item in Items)
			{
				var view = ViewOf(item);
				view.VerticalOptions = XF.LayoutOptions.Fill;
				stack.Children.Add(view);
			}
		}
	}
}

namespace System.Windows.Controls
{
	public class ScrollViewer : ContentControl
	{
		public static readonly XF.BindableProperty HorizontalScrollBarVisibilityProperty = Dp.Attached<ScrollViewer>("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), ScrollBarVisibility.Disabled);
		public static readonly XF.BindableProperty VerticalScrollBarVisibilityProperty = Dp.Attached<ScrollViewer>("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), ScrollBarVisibility.Visible);

		XF.ScrollView _scroll;

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => Get<ScrollBarVisibility>(HorizontalScrollBarVisibilityProperty);
			set => SetValue(HorizontalScrollBarVisibilityProperty, value);
		}

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get => Get<ScrollBarVisibility>(VerticalScrollBarVisibilityProperty);
			set => SetValue(VerticalScrollBarVisibilityProperty, value);
		}

		public double HorizontalOffset => _scroll?.ScrollX ?? 0;

		public double VerticalOffset => _scroll?.ScrollY ?? 0;

		public double ExtentWidth => _scroll?.ContentSize.Width ?? 0;

		public double ExtentHeight => _scroll?.ContentSize.Height ?? 0;

		public double ViewportWidth => ActualWidth;

		public double ViewportHeight => ActualHeight;

		public static ScrollBarVisibility GetHorizontalScrollBarVisibility(DependencyObject element) => (ScrollBarVisibility)element.GetValue(HorizontalScrollBarVisibilityProperty);

		public static void SetHorizontalScrollBarVisibility(DependencyObject element, ScrollBarVisibility value) => element.SetValue(HorizontalScrollBarVisibilityProperty, value);

		public static ScrollBarVisibility GetVerticalScrollBarVisibility(DependencyObject element) => (ScrollBarVisibility)element.GetValue(VerticalScrollBarVisibilityProperty);

		public static void SetVerticalScrollBarVisibility(DependencyObject element, ScrollBarVisibility value) => element.SetValue(VerticalScrollBarVisibilityProperty, value);

		public void ScrollToVerticalOffset(double offset) => _ = _scroll?.ScrollToAsync(HorizontalOffset, offset, false);

		public void ScrollToHorizontalOffset(double offset) => _ = _scroll?.ScrollToAsync(offset, VerticalOffset, false);

		public void ScrollToTop() => ScrollToVerticalOffset(0);

		public void ScrollToBottom() => ScrollToVerticalOffset(double.MaxValue);

		public void ScrollToHome() => _ = _scroll?.ScrollToAsync(0, 0, false);

		public void ScrollToEnd() => ScrollToVerticalOffset(double.MaxValue);

		public void LineUp() => ScrollToVerticalOffset(Math.Max(0, VerticalOffset - 16));

		public void LineDown() => ScrollToVerticalOffset(VerticalOffset + 16);

		public void PageUp() => ScrollToVerticalOffset(Math.Max(0, VerticalOffset - ActualHeight));

		public void PageDown() => ScrollToVerticalOffset(VerticalOffset + ActualHeight);

		internal override XF.View CreateNativeView() => _scroll = new XF.ScrollView();

		internal override void ApplyContent()
		{
			_scroll.Content = Content is UIElement element ? element.NativeView : Content == null ? null : new XF.Label { Text = ContentText };
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyScrollBars();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (_scroll != null && (e.Property.Bindable == HorizontalScrollBarVisibilityProperty || e.Property.Bindable == VerticalScrollBarVisibilityProperty))
				ApplyScrollBars();
		}

		void ApplyScrollBars()
		{
			var h = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
			var v = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
			_scroll.Orientation = h && v ? XF.ScrollOrientation.Both : h ? XF.ScrollOrientation.Horizontal : XF.ScrollOrientation.Vertical;
			_scroll.HorizontalScrollBarVisibility = ToForms(HorizontalScrollBarVisibility);
			_scroll.VerticalScrollBarVisibility = ToForms(VerticalScrollBarVisibility);
		}

		static XF.ScrollBarVisibility ToForms(ScrollBarVisibility v) =>
			v == ScrollBarVisibility.Visible ? XF.ScrollBarVisibility.Always : v == ScrollBarVisibility.Auto ? XF.ScrollBarVisibility.Default : XF.ScrollBarVisibility.Never;

		internal override void ApplyBorder()
		{
		}

		internal override XF.View TextView => null;
	}

	/// <summary>A tooltip's content; the element showing it hands its text to GTK.</summary>
	public class ToolTip : ContentControl
	{
		public static readonly XF.BindableProperty IsOpenProperty = Dp.Register<ToolTip>(nameof(IsOpen), typeof(bool), false);

		public bool IsOpen
		{
			get => Get<bool>(IsOpenProperty);
			set => SetValue(IsOpenProperty, value);
		}
	}

	// ---- dates -----------------------------------------------------------------------------------------------------

	public enum DatePickerFormat
	{
		Long,
		Short,
	}

	/// <summary>A date entry over a Xamarin.Forms date picker. No date is the picker showing today, with <see cref="SelectedDate"/> null.</summary>
	public class DatePicker : Control
	{
		public static readonly XF.BindableProperty SelectedDateProperty = Dp.Register<DatePicker>(nameof(SelectedDate), typeof(DateTime?), null, mode: XF.BindingMode.TwoWay);
		public static readonly XF.BindableProperty DisplayDateProperty = Dp.Register<DatePicker>(nameof(DisplayDate), typeof(DateTime), DateTime.Today);
		public static readonly XF.BindableProperty DisplayDateStartProperty = Dp.Register<DatePicker>(nameof(DisplayDateStart), typeof(DateTime?), null);
		public static readonly XF.BindableProperty DisplayDateEndProperty = Dp.Register<DatePicker>(nameof(DisplayDateEnd), typeof(DateTime?), null);
		public static readonly XF.BindableProperty SelectedDateFormatProperty = Dp.Register<DatePicker>(nameof(SelectedDateFormat), typeof(DatePickerFormat), DatePickerFormat.Short);
		public static readonly XF.BindableProperty IsTodayHighlightedProperty = Dp.Register<DatePicker>(nameof(IsTodayHighlighted), typeof(bool), true);
		public static readonly XF.BindableProperty IsDropDownOpenProperty = Dp.Register<DatePicker>(nameof(IsDropDownOpen), typeof(bool), false);

		public static readonly RoutedEvent SelectedDateChangedEvent =
			EventManager.RegisterRoutedEvent("SelectedDateChanged", RoutingStrategy.Direct, typeof(EventHandler<SelectionChangedEventArgs>), typeof(DatePicker));

		bool _syncing;

		public DateTime? SelectedDate
		{
			get => Get<DateTime?>(SelectedDateProperty);
			set => SetValue(SelectedDateProperty, value);
		}

		public DateTime DisplayDate
		{
			get => Get<DateTime>(DisplayDateProperty);
			set => SetValue(DisplayDateProperty, value);
		}

		public DateTime? DisplayDateStart
		{
			get => Get<DateTime?>(DisplayDateStartProperty);
			set => SetValue(DisplayDateStartProperty, value);
		}

		public DateTime? DisplayDateEnd
		{
			get => Get<DateTime?>(DisplayDateEndProperty);
			set => SetValue(DisplayDateEndProperty, value);
		}

		public DatePickerFormat SelectedDateFormat
		{
			get => Get<DatePickerFormat>(SelectedDateFormatProperty);
			set => SetValue(SelectedDateFormatProperty, value);
		}

		public bool IsTodayHighlighted
		{
			get => Get<bool>(IsTodayHighlightedProperty);
			set => SetValue(IsTodayHighlightedProperty, value);
		}

		public bool IsDropDownOpen
		{
			get => Get<bool>(IsDropDownOpenProperty);
			set => SetValue(IsDropDownOpenProperty, value);
		}

		/// <summary>The selected date as the picker shows it.</summary>
		public string Text =>
			SelectedDate?.ToString(SelectedDateFormat == DatePickerFormat.Long ? "D" : "d", CultureInfo.CurrentCulture) ?? string.Empty;

		public event EventHandler<SelectionChangedEventArgs> SelectedDateChanged { add => AddHandler(SelectedDateChangedEvent, value); remove => RemoveHandler(SelectedDateChangedEvent, value); }

		public event RoutedEventHandler CalendarOpened;

		public event RoutedEventHandler CalendarClosed;

		protected virtual void OnSelectedDateChanged(SelectionChangedEventArgs e) => RaiseEvent(e);

		protected virtual void OnCalendarOpened(RoutedEventArgs e) => CalendarOpened?.Invoke(this, e);

		protected virtual void OnCalendarClosed(RoutedEventArgs e) => CalendarClosed?.Invoke(this, e);

		internal override XF.View CreateNativeView()
		{
			var picker = new XF.DatePicker();
			picker.DateSelected += (s, e) =>
			{
				if (!_syncing)
					SelectedDate = e.NewDate;
			};
			picker.Focused += (s, e) =>
			{
				IsDropDownOpen = true;
				OnCalendarOpened(new RoutedEventArgs(null, this));
			};
			picker.Unfocused += (s, e) =>
			{
				IsDropDownOpen = false;
				OnCalendarClosed(new RoutedEventArgs(null, this));
			};
			return picker;
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyDate();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == SelectedDateProperty)
			{
				if (SelectedDate is DateTime date)
					DisplayDate = date;
				if (HasNativeView)
					ApplyDate();
				OnSelectedDateChanged(new SelectionChangedEventArgs(SelectedDateChangedEvent,
					e.OldValue == null ? (IList)Array.Empty<object>() : new[] { e.OldValue },
					e.NewValue == null ? (IList)Array.Empty<object>() : new[] { e.NewValue }) { Source = this });
			}
			else if (HasNativeView && (p == SelectedDateFormatProperty || p == DisplayDateStartProperty || p == DisplayDateEndProperty))
			{
				ApplyDate();
			}
		}

		void ApplyDate()
		{
			var picker = (XF.DatePicker)NativeView;
			_syncing = true;
			try
			{
				picker.Format = SelectedDateFormat == DatePickerFormat.Long ? "D" : "d";
				picker.MinimumDate = DisplayDateStart ?? new DateTime(1900, 1, 1);
				picker.MaximumDate = DisplayDateEnd ?? new DateTime(2100, 12, 31);
				picker.Date = SelectedDate ?? DisplayDate;
			}
			finally
			{
				_syncing = false;
			}
		}
	}

	public enum CalendarMode
	{
		Month,
		Year,
		Decade,
	}

	public enum CalendarSelectionMode
	{
		SingleDate,
		SingleRange,
		MultipleRange,
		None,
	}

	public sealed class SelectedDatesCollection : ObservableCollection<DateTime>
	{
		public void AddRange(DateTime start, DateTime end)
		{
			for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
				Add(d);
		}
	}

	/// <summary>A month calendar: GTK's own, hosted as it is.</summary>
	public class Calendar : Control
	{
		public static readonly XF.BindableProperty SelectedDateProperty = Dp.Register<Calendar>(nameof(SelectedDate), typeof(DateTime?), null, mode: XF.BindingMode.TwoWay);
		public static readonly XF.BindableProperty DisplayDateProperty = Dp.Register<Calendar>(nameof(DisplayDate), typeof(DateTime), DateTime.Today);
		public static readonly XF.BindableProperty DisplayModeProperty = Dp.Register<Calendar>(nameof(DisplayMode), typeof(CalendarMode), CalendarMode.Month);
		public static readonly XF.BindableProperty SelectionModeProperty = Dp.Register<Calendar>(nameof(SelectionMode), typeof(CalendarSelectionMode), CalendarSelectionMode.SingleDate);
		public static readonly XF.BindableProperty IsTodayHighlightedProperty = Dp.Register<Calendar>(nameof(IsTodayHighlighted), typeof(bool), true);

		public static readonly RoutedEvent SelectedDatesChangedEvent =
			EventManager.RegisterRoutedEvent("SelectedDatesChanged", RoutingStrategy.Direct, typeof(EventHandler<SelectionChangedEventArgs>), typeof(Calendar));

		Gtk.Calendar _calendar;
		bool _syncing;

		public Calendar()
		{
			SelectedDates = new SelectedDatesCollection();
			SelectedDates.CollectionChanged += (s, e) =>
			{
				if (_syncing)
					return;

				SelectedDate = SelectedDates.Count > 0 ? SelectedDates[0] : (DateTime?)null;
			};
		}

		public SelectedDatesCollection SelectedDates { get; }

		public DateTime? SelectedDate
		{
			get => Get<DateTime?>(SelectedDateProperty);
			set => SetValue(SelectedDateProperty, value);
		}

		public DateTime DisplayDate
		{
			get => Get<DateTime>(DisplayDateProperty);
			set => SetValue(DisplayDateProperty, value);
		}

		public CalendarMode DisplayMode
		{
			get => Get<CalendarMode>(DisplayModeProperty);
			set => SetValue(DisplayModeProperty, value);
		}

		public CalendarSelectionMode SelectionMode
		{
			get => Get<CalendarSelectionMode>(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		public bool IsTodayHighlighted
		{
			get => Get<bool>(IsTodayHighlightedProperty);
			set => SetValue(IsTodayHighlightedProperty, value);
		}

		public event EventHandler<SelectionChangedEventArgs> SelectedDatesChanged { add => AddHandler(SelectedDatesChangedEvent, value); remove => RemoveHandler(SelectedDatesChangedEvent, value); }

		public event EventHandler<CalendarDateChangedEventArgs> DisplayDateChanged;

		protected virtual void OnSelectedDatesChanged(SelectionChangedEventArgs e) => RaiseEvent(e);

		internal override XF.View CreateNativeView()
		{
			_calendar = new Gtk.Calendar();
			_calendar.DaySelected += (s, e) =>
			{
				if (_syncing)
					return;

				var date = _calendar.Date;
				SelectedDate = new DateTime(date.Year, date.Month, date.DayOfMonth);
			};
			return new NativeHostView(_calendar);
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyDate();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == SelectedDateProperty)
			{
				_syncing = true;
				try
				{
					SelectedDates.Clear();
					if (SelectedDate is DateTime date)
					{
						SelectedDates.Add(date.Date);
						DisplayDate = date;
					}
				}
				finally
				{
					_syncing = false;
				}

				if (_calendar != null)
					ApplyDate();

				OnSelectedDatesChanged(new SelectionChangedEventArgs(SelectedDatesChangedEvent,
					e.OldValue == null ? (IList)Array.Empty<object>() : new[] { e.OldValue },
					e.NewValue == null ? (IList)Array.Empty<object>() : new[] { e.NewValue }) { Source = this });
			}
			else if (p == DisplayDateProperty)
			{
				DisplayDateChanged?.Invoke(this, new CalendarDateChangedEventArgs(e.OldValue as DateTime?, e.NewValue as DateTime?));
				if (_calendar != null)
					ApplyDate();
			}
		}

		void ApplyDate()
		{
			var date = SelectedDate ?? DisplayDate;
			_syncing = true;
			try
			{
				_calendar.SelectDay(new GLib.DateTime(date.Year, date.Month, date.Day, 0, 0, 0));
			}
			finally
			{
				_syncing = false;
			}
		}
	}

	public class CalendarDateChangedEventArgs : RoutedEventArgs
	{
		internal CalendarDateChangedEventArgs(DateTime? removedDate, DateTime? addedDate)
		{
			RemovedDate = removedDate;
			AddedDate = addedDate;
		}

		public DateTime? AddedDate { get; }

		public DateTime? RemovedDate { get; }
	}

	// ---- list view -------------------------------------------------------------------------------------------------

	public abstract class ViewBase : DependencyObject
	{
	}

	public class GridViewColumn : DependencyObject
	{
		public static readonly XF.BindableProperty HeaderProperty = Dp.Register<GridViewColumn>(nameof(Header), typeof(object), null);
		public static readonly XF.BindableProperty WidthProperty = Dp.Register<GridViewColumn>(nameof(Width), typeof(double), double.NaN);

		public object Header
		{
			get => Get<object>(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public double Width
		{
			get => Get<double>(WidthProperty);
			set => SetValue(WidthProperty, value);
		}

		public double ActualWidth => double.IsNaN(Width) ? 0 : Width;

		public BindingBase DisplayMemberBinding { get; set; }

		public DataTemplate CellTemplate { get; set; }

		public DataTemplate HeaderTemplate { get; set; }

		internal GridView Owner { get; set; }

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			Owner?.OnChanged();
		}
	}

	public class GridViewColumnCollection : ObservableCollection<GridViewColumn>
	{
	}

	/// <summary>Columns for a <see cref="ListView"/>: a header row, and a cell per column in each item's row.</summary>
	[XF.ContentProperty(nameof(Columns))]
	public class GridView : ViewBase
	{
		public GridView()
		{
			Columns.CollectionChanged += (s, e) =>
			{
				foreach (GridViewColumn column in Columns)
					column.Owner = this;
				OnChanged();
			};
		}

		public GridViewColumnCollection Columns { get; } = new GridViewColumnCollection();

		public bool AllowsColumnReorder { get; set; } = true;

		internal ListView Owner { get; set; }

		internal void OnChanged() => Owner?.OnViewChanged();
	}

	/// <summary>A list box that can show its items in columns (<see cref="View"/> a <see cref="GridView"/>).</summary>
	public class ListView : ListBox
	{
		public static readonly XF.BindableProperty ViewProperty = Dp.Register<ListView>(nameof(View), typeof(ViewBase), null);

		XF.Grid _header;

		public ViewBase View
		{
			get => Get<ViewBase>(ViewProperty);
			set => SetValue(ViewProperty, value);
		}

		GridView Grid => View as GridView;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable == ViewProperty)
			{
				if (e.OldValue is GridView old)
					old.Owner = null;
				if (e.NewValue is GridView grid)
					grid.Owner = this;
				OnViewChanged();
			}
		}

		internal void OnViewChanged()
		{
			if (!HasNativeView)
				return;

			ApplyHeader();
			ApplyItems(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			if (((NativeShell)NativeView).Inner is XF.Grid outer && outer.Children.OfType<XF.CollectionView>().FirstOrDefault() is XF.CollectionView view)
				view.ItemTemplate = Grid == null ? ItemTemplateFor(this) : ColumnsTemplate();
		}

		internal override XF.View CreateNativeView()
		{
			var shell = (NativeShell)base.CreateNativeView();
			var list = shell.Inner;
			_header = new XF.Grid { ColumnSpacing = 0, BackgroundColor = SystemColors.ControlBrush.ToFormsColor() };
			shell.Inner = new XF.Grid
			{
				RowSpacing = 0,
				RowDefinitions = { new XF.RowDefinition { Height = XF.GridLength.Auto }, new XF.RowDefinition { Height = XF.GridLength.Star } },
				Children = { { _header, 0, 0 }, { list, 0, 1 } },
			};
			return shell;
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			OnViewChanged();
		}

		void ApplyHeader()
		{
			_header.Children.Clear();
			_header.ColumnDefinitions.Clear();
			_header.IsVisible = Grid != null;
			if (Grid == null)
				return;

			for (var i = 0; i < Grid.Columns.Count; i++)
			{
				var column = Grid.Columns[i];
				_header.ColumnDefinitions.Add(new XF.ColumnDefinition { Width = double.IsNaN(column.Width) ? new XF.GridLength(100) : new XF.GridLength(column.Width) });
				var label = new XF.Label { Text = column.Header?.ToString() ?? string.Empty, Padding = new XF.Thickness(4, 2) };
				NativeText.ApplyFont(label, this);
				_header.Children.Add(label, i, 0);
			}
		}

		XF.DataTemplate ColumnsTemplate()
		{
			var columns = Grid.Columns.ToList();
			return new XF.DataTemplate(() =>
			{
				var row = new XF.Grid { ColumnSpacing = 0 };
				for (var i = 0; i < columns.Count; i++)
				{
					row.ColumnDefinitions.Add(new XF.ColumnDefinition { Width = double.IsNaN(columns[i].Width) ? new XF.GridLength(100) : new XF.GridLength(columns[i].Width) });
					var cell = new XF.Label { Padding = new XF.Thickness(4, 1), LineBreakMode = XF.LineBreakMode.TailTruncation };
					cell.SetBinding(XF.Label.TextProperty, new XF.Binding($"{nameof(ItemBox.Cells)}[{i}]"));
					NativeText.ApplyFont(cell, this);
					NativeText.ApplyForeground(cell, this);
					row.Children.Add(cell, i, 0);
				}

				return row;
			});
		}

		/// <summary>With columns, the text of each: its member binding's value, else (first column) the item's text.</summary>
		internal override ItemBox Box(object item)
		{
			if (Grid == null)
				return base.Box(item);

			var cells = Grid.Columns.Select((c, i) =>
			{
				if (c.DisplayMemberBinding is Binding binding)
					return Convert.ToString(PropertyOf(item, binding.Path?.Path), CultureInfo.CurrentCulture) ?? string.Empty;
				return i == 0 ? ItemText(item) : string.Empty;
			}).ToArray();
			return new ItemBox(item, ItemText(item), cells);
		}
	}

	public class ListViewItem : ListBoxItem
	{
	}
}
