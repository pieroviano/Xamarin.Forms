using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls
{
	public enum Orientation
	{
		Horizontal,
		Vertical,
	}

	/// <summary>
	/// The children of a panel. Each child's view goes into the panel's Xamarin.Forms layout at the same position,
	/// so the order - and so what draws over what - is the same.
	/// </summary>
	public class UIElementCollection : IList
	{
		readonly List<UIElement> _items = new List<UIElement>();
		readonly Panel _owner;

		public UIElementCollection(UIElement visualParent, FrameworkElement logicalParent) => _owner = (Panel)logicalParent;

		public int Count => _items.Count;

		public UIElement this[int index]
		{
			get => _items[index];
			set
			{
				RemoveAt(index);
				Insert(index, value);
			}
		}

		object IList.this[int index]
		{
			get => this[index];
			set => this[index] = (UIElement)value;
		}

		public int Add(UIElement element)
		{
			Insert(_items.Count, element);
			return _items.Count - 1;
		}

		public void Insert(int index, UIElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			if (element.LogicalParent != null)
				throw new ArgumentException("Specified element is already the logical child of another element. Disconnect it first.");

			_items.Insert(index, element);
			_owner.OnChildAdded(index, element);
		}

		public void Remove(UIElement element)
		{
			var index = _items.IndexOf(element);
			if (index >= 0)
				RemoveAt(index);
		}

		public void RemoveAt(int index)
		{
			var element = _items[index];
			_items.RemoveAt(index);
			_owner.OnChildRemoved(element);
		}

		public void RemoveRange(int index, int count)
		{
			for (var i = 0; i < count; i++)
				RemoveAt(index);
		}

		public void Clear()
		{
			while (_items.Count > 0)
				RemoveAt(_items.Count - 1);
		}

		public bool Contains(UIElement element) => _items.Contains(element);

		public int IndexOf(UIElement element) => _items.IndexOf(element);

		public void CopyTo(UIElement[] array, int index) => _items.CopyTo(array, index);

		public IEnumerator GetEnumerator() => _items.GetEnumerator();

		int IList.Add(object value) => Add((UIElement)value);

		bool IList.Contains(object value) => value is UIElement e && Contains(e);

		int IList.IndexOf(object value) => value is UIElement e ? IndexOf(e) : -1;

		void IList.Insert(int index, object value) => Insert(index, (UIElement)value);

		void IList.Remove(object value)
		{
			if (value is UIElement e)
				Remove(e);
		}

		void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;
	}

	/// <summary>
	/// A panel over a Xamarin.Forms layout. Its background can be an image (a VB6 picture), which is drawn by an
	/// extra first view below the children.
	/// </summary>
	[XF.ContentProperty(nameof(Children))]
	public abstract class Panel : FrameworkElement
	{
		UIElementCollection _children;
		XF.Image _backgroundImage;

		protected Panel()
		{
		}

		public static readonly XF.BindableProperty BackgroundProperty = Dp.Register<Panel>(nameof(Background), typeof(Brush), null);
		public static readonly XF.BindableProperty ZIndexProperty = Dp.Attached<Panel>("ZIndex", typeof(int), 0);
		public static readonly XF.BindableProperty IsItemsHostProperty = Dp.Register<Panel>(nameof(IsItemsHost), typeof(bool), false);

		public Brush Background
		{
			get => Get<Brush>(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public bool IsItemsHost
		{
			get => Get<bool>(IsItemsHostProperty);
			set => SetValue(IsItemsHostProperty, value);
		}

		public UIElementCollection Children => _children ?? (_children = CreateUIElementCollection(this));

		public static int GetZIndex(UIElement element) => (int)element.GetValue(ZIndexProperty);

		public static void SetZIndex(UIElement element, int value) => element.SetValue(ZIndexProperty, value);

		protected virtual UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent) => new UIElementCollection(this, logicalParent);

		internal override IEnumerable LogicalChildrenCore => _children == null ? Array.Empty<object>() : _children.Cast<object>().ToArray();

		/// <summary>The Xamarin.Forms layout the children's views go into.</summary>
		internal XF.Layout<XF.View> NativeLayout => (XF.Layout<XF.View>)NativeView;

		/// <summary>Where the view of the child at <paramref name="index"/> goes: after the background image, if there is one.</summary>
		int NativeIndex(int index) => index + (_backgroundImage == null ? 0 : 1);

		internal void OnChildAdded(int index, UIElement element)
		{
			AddLogicalChild(element);
			if (HasNativeView)
			{
				NativeLayout.Children.Insert(NativeIndex(index), element.NativeView);
				PlaceChild(element);
				ApplyZOrder();
			}
		}

		internal void OnChildRemoved(UIElement element)
		{
			if (HasNativeView && element.HasNativeView)
				NativeLayout.Children.Remove(element.NativeView);
			RemoveLogicalChild(element);
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			var layout = NativeLayout;
			layout.Children.Clear();
			_backgroundImage = null;
			ApplyBackground();
			if (_children != null)
			{
				foreach (UIElement child in _children)
				{
					layout.Children.Add(child.NativeView);
					PlaceChild(child);
				}
			}

			ApplyZOrder();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (HasNativeView && e.Property.Bindable == BackgroundProperty)
				ApplyBackground();
		}

		/// <summary>A solid color paints the layout; an image brush is an image view under the children.</summary>
		void ApplyBackground()
		{
			var layout = NativeLayout;
			var brush = Background;
			layout.BackgroundColor = brush?.ToFormsColor() ?? XF.Color.Default;

			if (brush is ImageBrush image && image.ImageSource != null)
			{
				if (_backgroundImage == null)
				{
					_backgroundImage = new XF.Image { InputTransparent = true };
					layout.Children.Insert(0, _backgroundImage);
				}

				_backgroundImage.Source = image.ImageSource.ToForms();
				_backgroundImage.Aspect = image.Stretch == Stretch.Fill ? XF.Aspect.Fill : image.Stretch == Stretch.UniformToFill ? XF.Aspect.AspectFill : XF.Aspect.AspectFit;
				_backgroundImage.HorizontalOptions = image.Stretch == Stretch.None
					? image.AlignmentX == AlignmentX.Left ? XF.LayoutOptions.Start : image.AlignmentX == AlignmentX.Right ? XF.LayoutOptions.End : XF.LayoutOptions.Center
					: XF.LayoutOptions.Fill;
				_backgroundImage.VerticalOptions = image.Stretch == Stretch.None
					? image.AlignmentY == AlignmentY.Top ? XF.LayoutOptions.Start : image.AlignmentY == AlignmentY.Bottom ? XF.LayoutOptions.End : XF.LayoutOptions.Center
					: XF.LayoutOptions.Fill;
				PlaceBackground(_backgroundImage);
			}
			else if (_backgroundImage != null)
			{
				layout.Children.Remove(_backgroundImage);
				_backgroundImage = null;
			}
		}

		/// <summary>Stretches the background image over the whole panel, by the panel's own rules.</summary>
		internal virtual void PlaceBackground(XF.View image)
		{
		}

		/// <summary>Places a child's view by the rules of this panel (its attached properties).</summary>
		internal virtual void PlaceChild(UIElement child)
		{
		}

		internal override void OnChildLayoutChanged(FrameworkElement child)
		{
			if (HasNativeView && child.HasNativeView)
				PlaceChild(child);
		}

		/// <summary>
		/// Xamarin.Forms draws children in order, so a higher <see cref="ZIndexProperty"/> means later: a stable sort,
		/// which leaves the order alone when no child sets one.
		/// </summary>
		void ApplyZOrder()
		{
			if (_children == null || !_children.Cast<UIElement>().Any(c => c.IsSet(ZIndexProperty)))
				return;

			var layout = NativeLayout;
			var ordered = _children.Cast<UIElement>().Select((c, i) => (Child: c, Index: i))
				.OrderBy(t => GetZIndex(t.Child)).ThenBy(t => t.Index).Select(t => t.Child.NativeView).ToList();
			for (var i = 0; i < ordered.Count; i++)
				layout.RaiseChild(ordered[i]);
		}

		internal static void OnAttachedChanged(DependencyObject d)
		{
			if (d is UIElement element && element.LogicalParent is Panel panel && panel.HasNativeView && element.HasNativeView)
			{
				panel.PlaceChild(element);
				panel.ApplyZOrder();
			}
		}
	}

	public class RowDefinition : DependencyObject
	{
		public static readonly XF.BindableProperty HeightProperty = Dp.Register<RowDefinition>(nameof(Height), typeof(GridLength), new GridLength(1, GridUnitType.Star));
		public static readonly XF.BindableProperty MinHeightProperty = Dp.Register<RowDefinition>(nameof(MinHeight), typeof(double), 0.0);
		public static readonly XF.BindableProperty MaxHeightProperty = Dp.Register<RowDefinition>(nameof(MaxHeight), typeof(double), double.PositiveInfinity);

		public GridLength Height
		{
			get => Get<GridLength>(HeightProperty);
			set => SetValue(HeightProperty, value);
		}

		public double MinHeight
		{
			get => Get<double>(MinHeightProperty);
			set => SetValue(MinHeightProperty, value);
		}

		public double MaxHeight
		{
			get => Get<double>(MaxHeightProperty);
			set => SetValue(MaxHeightProperty, value);
		}

		public double ActualHeight => Height.IsAbsolute ? Height.Value : 0;

		internal XF.RowDefinition Native { get; set; }

		internal Grid Owner { get; set; }

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			Owner?.ApplyDefinitions();
		}
	}

	public class ColumnDefinition : DependencyObject
	{
		public static readonly XF.BindableProperty WidthProperty = Dp.Register<ColumnDefinition>(nameof(Width), typeof(GridLength), new GridLength(1, GridUnitType.Star));
		public static readonly XF.BindableProperty MinWidthProperty = Dp.Register<ColumnDefinition>(nameof(MinWidth), typeof(double), 0.0);
		public static readonly XF.BindableProperty MaxWidthProperty = Dp.Register<ColumnDefinition>(nameof(MaxWidth), typeof(double), double.PositiveInfinity);

		public GridLength Width
		{
			get => Get<GridLength>(WidthProperty);
			set => SetValue(WidthProperty, value);
		}

		public double MinWidth
		{
			get => Get<double>(MinWidthProperty);
			set => SetValue(MinWidthProperty, value);
		}

		public double MaxWidth
		{
			get => Get<double>(MaxWidthProperty);
			set => SetValue(MaxWidthProperty, value);
		}

		public double ActualWidth => Width.IsAbsolute ? Width.Value : 0;

		internal XF.ColumnDefinition Native { get; set; }

		internal Grid Owner { get; set; }

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			Owner?.ApplyDefinitions();
		}
	}

	public class RowDefinitionCollection : Collection<RowDefinition>
	{
		readonly Grid _owner;

		internal RowDefinitionCollection(Grid owner) => _owner = owner;

		protected override void InsertItem(int index, RowDefinition item)
		{
			base.InsertItem(index, item ?? throw new ArgumentNullException(nameof(item)));
			item.Owner = _owner;
			_owner.ApplyDefinitions();
		}

		protected override void RemoveItem(int index)
		{
			this[index].Owner = null;
			base.RemoveItem(index);
			_owner.ApplyDefinitions();
		}

		protected override void SetItem(int index, RowDefinition item)
		{
			this[index].Owner = null;
			base.SetItem(index, item);
			item.Owner = _owner;
			_owner.ApplyDefinitions();
		}

		protected override void ClearItems()
		{
			foreach (var item in this)
				item.Owner = null;
			base.ClearItems();
			_owner.ApplyDefinitions();
		}
	}

	public class ColumnDefinitionCollection : Collection<ColumnDefinition>
	{
		readonly Grid _owner;

		internal ColumnDefinitionCollection(Grid owner) => _owner = owner;

		protected override void InsertItem(int index, ColumnDefinition item)
		{
			base.InsertItem(index, item ?? throw new ArgumentNullException(nameof(item)));
			item.Owner = _owner;
			_owner.ApplyDefinitions();
		}

		protected override void RemoveItem(int index)
		{
			this[index].Owner = null;
			base.RemoveItem(index);
			_owner.ApplyDefinitions();
		}

		protected override void SetItem(int index, ColumnDefinition item)
		{
			this[index].Owner = null;
			base.SetItem(index, item);
			item.Owner = _owner;
			_owner.ApplyDefinitions();
		}

		protected override void ClearItems()
		{
			foreach (var item in this)
				item.Owner = null;
			base.ClearItems();
			_owner.ApplyDefinitions();
		}
	}

	/// <summary>
	/// Rows and columns over a Xamarin.Forms grid, with WPF's spacing (none). With no definitions there is one cell,
	/// and children placed in it by margin and alignment - a converted VB6 form - overlap where WPF puts them.
	/// </summary>
	public class Grid : Panel
	{
		public static readonly XF.BindableProperty RowProperty = Dp.Attached<Grid>("Row", typeof(int), 0);
		public static readonly XF.BindableProperty ColumnProperty = Dp.Attached<Grid>("Column", typeof(int), 0);
		public static readonly XF.BindableProperty RowSpanProperty = Dp.Attached<Grid>("RowSpan", typeof(int), 1);
		public static readonly XF.BindableProperty ColumnSpanProperty = Dp.Attached<Grid>("ColumnSpan", typeof(int), 1);
		public static readonly XF.BindableProperty ShowGridLinesProperty = Dp.Register<Grid>(nameof(ShowGridLines), typeof(bool), false);

		public Grid()
		{
			RowDefinitions = new RowDefinitionCollection(this);
			ColumnDefinitions = new ColumnDefinitionCollection(this);
		}

		public RowDefinitionCollection RowDefinitions { get; }

		public ColumnDefinitionCollection ColumnDefinitions { get; }

		public bool ShowGridLines
		{
			get => Get<bool>(ShowGridLinesProperty);
			set => SetValue(ShowGridLinesProperty, value);
		}

		public static int GetRow(UIElement element) => (int)element.GetValue(RowProperty);

		public static void SetRow(UIElement element, int value) => element.SetValue(RowProperty, value);

		public static int GetColumn(UIElement element) => (int)element.GetValue(ColumnProperty);

		public static void SetColumn(UIElement element, int value) => element.SetValue(ColumnProperty, value);

		public static int GetRowSpan(UIElement element) => (int)element.GetValue(RowSpanProperty);

		public static void SetRowSpan(UIElement element, int value) => element.SetValue(RowSpanProperty, value);

		public static int GetColumnSpan(UIElement element) => (int)element.GetValue(ColumnSpanProperty);

		public static void SetColumnSpan(UIElement element, int value) => element.SetValue(ColumnSpanProperty, value);

		internal override XF.View CreateNativeView() => new XF.Grid { RowSpacing = 0, ColumnSpacing = 0 };

		internal override void SyncNative()
		{
			ApplyDefinitions();
			base.SyncNative();
		}

		internal void ApplyDefinitions()
		{
			if (!HasNativeView)
				return;

			var grid = (XF.Grid)NativeView;
			grid.RowDefinitions.Clear();
			foreach (var row in RowDefinitions)
			{
				row.Native = new XF.RowDefinition { Height = row.Height.ToForms() };
				grid.RowDefinitions.Add(row.Native);
			}

			grid.ColumnDefinitions.Clear();
			foreach (var column in ColumnDefinitions)
			{
				column.Native = new XF.ColumnDefinition { Width = column.Width.ToForms() };
				grid.ColumnDefinitions.Add(column.Native);
			}
		}

		internal override void PlaceChild(UIElement child)
		{
			var view = child.NativeView;
			XF.Grid.SetRow(view, Math.Max(0, GetRow(child)));
			XF.Grid.SetColumn(view, Math.Max(0, GetColumn(child)));
			XF.Grid.SetRowSpan(view, Math.Max(1, GetRowSpan(child)));
			XF.Grid.SetColumnSpan(view, Math.Max(1, GetColumnSpan(child)));
		}

		internal override void PlaceBackground(XF.View image)
		{
			XF.Grid.SetRowSpan(image, Math.Max(1, RowDefinitions.Count));
			XF.Grid.SetColumnSpan(image, Math.Max(1, ColumnDefinitions.Count));
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) => base.OnPropertyChanged(e);
	}

	/// <summary>Children at absolute positions, over a Xamarin.Forms absolute layout.</summary>
	public class Canvas : Panel
	{
		public static readonly XF.BindableProperty LeftProperty = Dp.Attached<Canvas>("Left", typeof(double), double.NaN);
		public static readonly XF.BindableProperty TopProperty = Dp.Attached<Canvas>("Top", typeof(double), double.NaN);
		public static readonly XF.BindableProperty RightProperty = Dp.Attached<Canvas>("Right", typeof(double), double.NaN);
		public static readonly XF.BindableProperty BottomProperty = Dp.Attached<Canvas>("Bottom", typeof(double), double.NaN);

		public static double GetLeft(UIElement element) => (double)element.GetValue(LeftProperty);

		public static void SetLeft(UIElement element, double length) => element.SetValue(LeftProperty, length);

		public static double GetTop(UIElement element) => (double)element.GetValue(TopProperty);

		public static void SetTop(UIElement element, double length) => element.SetValue(TopProperty, length);

		public static double GetRight(UIElement element) => (double)element.GetValue(RightProperty);

		public static void SetRight(UIElement element, double length) => element.SetValue(RightProperty, length);

		public static double GetBottom(UIElement element) => (double)element.GetValue(BottomProperty);

		public static void SetBottom(UIElement element, double length) => element.SetValue(BottomProperty, length);

		internal override XF.View CreateNativeView() => new XF.AbsoluteLayout();

		internal override void OnNativeCreated()
		{
			base.OnNativeCreated();
			NativeView.SizeChanged += (s, e) =>
			{
				// Right and Bottom are measured from the far edges, so they move with the canvas.
				foreach (UIElement child in Children)
				{
					if (!double.IsNaN(GetRight(child)) || !double.IsNaN(GetBottom(child)))
						PlaceChild(child);
				}
			};
		}

		/// <summary>
		/// WPF: Left wins over Right and Top over Bottom; an element's size is its own (width/height, else what it
		/// asks for); its margin shifts it.
		/// </summary>
		internal override void PlaceChild(UIElement child)
		{
			var view = child.NativeView;
			var fe = child as FrameworkElement;
			var width = fe != null && !double.IsNaN(fe.Width) ? fe.Width : XF.AbsoluteLayout.AutoSize;
			var height = fe != null && !double.IsNaN(fe.Height) ? fe.Height : XF.AbsoluteLayout.AutoSize;

			double x = GetLeft(child), y = GetTop(child);
			if (double.IsNaN(x))
			{
				var right = GetRight(child);
				x = double.IsNaN(right) ? 0 : Math.Max(0, NativeView.Width - right - Measured(view).Width);
			}

			if (double.IsNaN(y))
			{
				var bottom = GetBottom(child);
				y = double.IsNaN(bottom) ? 0 : Math.Max(0, NativeView.Height - bottom - Measured(view).Height);
			}

			XF.AbsoluteLayout.SetLayoutFlags(view, XF.AbsoluteLayoutFlags.None);
			XF.AbsoluteLayout.SetLayoutBounds(view, new XF.Rectangle(x, y, width, height));
		}

		static XF.Size Measured(XF.View view) => view.Measure(double.PositiveInfinity, double.PositiveInfinity, XF.MeasureFlags.IncludeMargins).Request;

		internal override void PlaceBackground(XF.View image)
		{
			XF.AbsoluteLayout.SetLayoutFlags(image, XF.AbsoluteLayoutFlags.None);
			XF.AbsoluteLayout.SetLayoutBounds(image, new XF.Rectangle(0, 0, XF.AbsoluteLayout.AutoSize, XF.AbsoluteLayout.AutoSize));
		}
	}

	public enum Dock
	{
		Left,
		Top,
		Right,
		Bottom,
	}

	/// <summary>Children against the edges in order, the last one filling what is left (WPF's algorithm).</summary>
	public class DockPanel : Panel
	{
		public static readonly XF.BindableProperty DockProperty = Dp.Attached<DockPanel>("Dock", typeof(Dock), Dock.Left);
		public static readonly XF.BindableProperty LastChildFillProperty = Dp.Register<DockPanel>(nameof(LastChildFill), typeof(bool), true);

		public bool LastChildFill
		{
			get => Get<bool>(LastChildFillProperty);
			set => SetValue(LastChildFillProperty, value);
		}

		public static Dock GetDock(UIElement element) => (Dock)element.GetValue(DockProperty);

		public static void SetDock(UIElement element, Dock dock) => element.SetValue(DockProperty, dock);

		internal override XF.View CreateNativeView() => new DockLayout(this);

		internal override void PlaceChild(UIElement child) => ((DockLayout)NativeView).Relayout();

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (HasNativeView && e.Property.Bindable == LastChildFillProperty)
				((DockLayout)NativeView).Relayout();
		}

		/// <summary>The Xamarin.Forms layout doing the docking.</summary>
		sealed class DockLayout : XF.Layout<XF.View>
		{
			readonly DockPanel _owner;

			public DockLayout(DockPanel owner) => _owner = owner;

			internal void Relayout()
			{
				InvalidateMeasure();
				ForceLayout();
			}

			Dock DockOf(XF.View view)
			{
				foreach (UIElement child in _owner.Children)
				{
					if (child.HasNativeView && child.NativeView == view)
						return GetDock(child);
				}

				return Dock.Left;
			}

			protected override XF.SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
			{
				double usedWidth = 0, usedHeight = 0, maxWidth = 0, maxHeight = 0;
				foreach (var view in Children.Where(v => v.IsVisible))
				{
					var request = view.Measure(Math.Max(0, widthConstraint - usedWidth), Math.Max(0, heightConstraint - usedHeight), XF.MeasureFlags.IncludeMargins).Request;
					switch (DockOf(view))
					{
						case Dock.Left:
						case Dock.Right:
							maxHeight = Math.Max(maxHeight, usedHeight + request.Height);
							usedWidth += request.Width;
							break;
						default:
							maxWidth = Math.Max(maxWidth, usedWidth + request.Width);
							usedHeight += request.Height;
							break;
					}
				}

				return new XF.SizeRequest(new XF.Size(Math.Max(maxWidth, usedWidth), Math.Max(maxHeight, usedHeight)));
			}

			protected override void LayoutChildren(double x, double y, double width, double height)
			{
				var visible = Children.Where(v => v.IsVisible).ToList();
				double left = x, top = y, right = x + width, bottom = y + height;
				for (var i = 0; i < visible.Count; i++)
				{
					var view = visible[i];
					var remainingWidth = Math.Max(0, right - left);
					var remainingHeight = Math.Max(0, bottom - top);
					if (i == visible.Count - 1 && _owner.LastChildFill)
					{
						LayoutChildIntoBoundingRegion(view, new XF.Rectangle(left, top, remainingWidth, remainingHeight));
						break;
					}

					var request = view.Measure(remainingWidth, remainingHeight, XF.MeasureFlags.IncludeMargins).Request;
					switch (DockOf(view))
					{
						case Dock.Left:
							LayoutChildIntoBoundingRegion(view, new XF.Rectangle(left, top, Math.Min(request.Width, remainingWidth), remainingHeight));
							left += Math.Min(request.Width, remainingWidth);
							break;
						case Dock.Right:
							LayoutChildIntoBoundingRegion(view, new XF.Rectangle(right - Math.Min(request.Width, remainingWidth), top, Math.Min(request.Width, remainingWidth), remainingHeight));
							right -= Math.Min(request.Width, remainingWidth);
							break;
						case Dock.Top:
							LayoutChildIntoBoundingRegion(view, new XF.Rectangle(left, top, remainingWidth, Math.Min(request.Height, remainingHeight)));
							top += Math.Min(request.Height, remainingHeight);
							break;
						default:
							LayoutChildIntoBoundingRegion(view, new XF.Rectangle(left, bottom - Math.Min(request.Height, remainingHeight), remainingWidth, Math.Min(request.Height, remainingHeight)));
							bottom -= Math.Min(request.Height, remainingHeight);
							break;
					}
				}
			}
		}
	}

	public class StackPanel : Panel
	{
		public static readonly XF.BindableProperty OrientationProperty = Dp.Register<StackPanel>(nameof(Orientation), typeof(Orientation), Orientation.Vertical);

		public Orientation Orientation
		{
			get => Get<Orientation>(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		internal override XF.View CreateNativeView() => new XF.StackLayout { Spacing = 0 };

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyOrientation();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (HasNativeView && e.Property.Bindable == OrientationProperty)
				ApplyOrientation();
		}

		void ApplyOrientation() =>
			((XF.StackLayout)NativeView).Orientation = Orientation == Orientation.Horizontal ? XF.StackOrientation.Horizontal : XF.StackOrientation.Vertical;
	}

	public class WrapPanel : Panel
	{
		public static readonly XF.BindableProperty OrientationProperty = Dp.Register<WrapPanel>(nameof(Orientation), typeof(Orientation), Orientation.Horizontal);

		public Orientation Orientation
		{
			get => Get<Orientation>(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		internal override XF.View CreateNativeView() => new XF.FlexLayout { Wrap = XF.FlexWrap.Wrap, AlignItems = XF.FlexAlignItems.Start, AlignContent = XF.FlexAlignContent.Start };

		internal override void SyncNative()
		{
			base.SyncNative();
			((XF.FlexLayout)NativeView).Direction = Orientation == Orientation.Horizontal ? XF.FlexDirection.Row : XF.FlexDirection.Column;
		}
	}
}
