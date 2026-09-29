using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows
{
	/// <summary>
	/// Layout (size, margin, alignment), identity (name, tag), styles, resources and the load lifecycle. The layout
	/// properties map one to one onto the Xamarin.Forms view: <c>Width</c> is its width request, <c>Margin</c> its
	/// margin, the alignments its layout options - which is also how WPF's own panels read them.
	/// </summary>
	public class FrameworkElement : UIElement, ISupportInitialize
	{
		ResourceDictionary _resources;
		bool _isLoaded;
		bool _isInitialized;
		int _initCount;

		public FrameworkElement()
		{
		}

		// ---- layout --------------------------------------------------------------------------------------------------

		public static readonly XF.BindableProperty WidthProperty = Dp.Register<FrameworkElement>(nameof(Width), typeof(double), double.NaN);
		public static readonly XF.BindableProperty HeightProperty = Dp.Register<FrameworkElement>(nameof(Height), typeof(double), double.NaN);
		public static readonly XF.BindableProperty MinWidthProperty = Dp.Register<FrameworkElement>(nameof(MinWidth), typeof(double), 0.0);
		public static readonly XF.BindableProperty MinHeightProperty = Dp.Register<FrameworkElement>(nameof(MinHeight), typeof(double), 0.0);
		public static readonly XF.BindableProperty MaxWidthProperty = Dp.Register<FrameworkElement>(nameof(MaxWidth), typeof(double), double.PositiveInfinity);
		public static readonly XF.BindableProperty MaxHeightProperty = Dp.Register<FrameworkElement>(nameof(MaxHeight), typeof(double), double.PositiveInfinity);
		public static readonly XF.BindableProperty MarginProperty = Dp.Register<FrameworkElement>(nameof(Margin), typeof(Thickness), default(Thickness));
		public static readonly XF.BindableProperty HorizontalAlignmentProperty = Dp.Register<FrameworkElement>(nameof(HorizontalAlignment), typeof(HorizontalAlignment), HorizontalAlignment.Stretch);
		public static readonly XF.BindableProperty VerticalAlignmentProperty = Dp.Register<FrameworkElement>(nameof(VerticalAlignment), typeof(VerticalAlignment), VerticalAlignment.Stretch);
		public static readonly XF.BindableProperty FlowDirectionProperty = Dp.Register<FrameworkElement>(nameof(FlowDirection), typeof(FlowDirection), FlowDirection.LeftToRight, inherits: true);

		public double Width
		{
			get => Get<double>(WidthProperty);
			set => SetValue(WidthProperty, value);
		}

		public double Height
		{
			get => Get<double>(HeightProperty);
			set => SetValue(HeightProperty, value);
		}

		public double MinWidth
		{
			get => Get<double>(MinWidthProperty);
			set => SetValue(MinWidthProperty, value);
		}

		public double MinHeight
		{
			get => Get<double>(MinHeightProperty);
			set => SetValue(MinHeightProperty, value);
		}

		public double MaxWidth
		{
			get => Get<double>(MaxWidthProperty);
			set => SetValue(MaxWidthProperty, value);
		}

		public double MaxHeight
		{
			get => Get<double>(MaxHeightProperty);
			set => SetValue(MaxHeightProperty, value);
		}

		public Thickness Margin
		{
			get => Get<Thickness>(MarginProperty);
			set => SetValue(MarginProperty, value);
		}

		public HorizontalAlignment HorizontalAlignment
		{
			get => Get<HorizontalAlignment>(HorizontalAlignmentProperty);
			set => SetValue(HorizontalAlignmentProperty, value);
		}

		public VerticalAlignment VerticalAlignment
		{
			get => Get<VerticalAlignment>(VerticalAlignmentProperty);
			set => SetValue(VerticalAlignmentProperty, value);
		}

		public FlowDirection FlowDirection
		{
			get => Get<FlowDirection>(FlowDirectionProperty);
			set => SetValue(FlowDirectionProperty, value);
		}

		/// <summary>The width the element was laid out at (0 before its first layout).</summary>
		public double ActualWidth => RenderSize.Width;

		public double ActualHeight => RenderSize.Height;

		// ---- identity ------------------------------------------------------------------------------------------------

		public static readonly XF.BindableProperty NameProperty = Dp.Register<FrameworkElement>(nameof(Name), typeof(string), string.Empty);
		public static readonly XF.BindableProperty TagProperty = Dp.Register<FrameworkElement>(nameof(Tag), typeof(object), null);
		public static readonly XF.BindableProperty ToolTipProperty = Dp.Register<FrameworkElement>(nameof(ToolTip), typeof(object), null);
		public static readonly XF.BindableProperty CursorProperty = Dp.Register<FrameworkElement>(nameof(Cursor), typeof(Cursor), null);
		public static readonly XF.BindableProperty StyleProperty = Dp.Register<FrameworkElement>(nameof(Style), typeof(Style), null);
		public static readonly XF.BindableProperty ContextMenuProperty = Dp.Register<FrameworkElement>(nameof(ContextMenu), typeof(ContextMenu), null);
		public static readonly XF.BindableProperty DataContextProperty = Dp.Register<FrameworkElement>(nameof(DataContext), typeof(object), null);
		public static readonly XF.BindableProperty FocusVisualStyleProperty = Dp.Register<FrameworkElement>(nameof(FocusVisualStyle), typeof(Style), null);
		public static readonly XF.BindableProperty UseLayoutRoundingProperty = Dp.Register<FrameworkElement>(nameof(UseLayoutRounding), typeof(bool), false);
		public static readonly XF.BindableProperty OverridesDefaultStyleProperty = Dp.Register<FrameworkElement>(nameof(OverridesDefaultStyle), typeof(bool), false);

		/// <summary>The element's name: <c>x:Name</c> gives it, as Xamarin.Forms gives an element's <c>StyleId</c>.</summary>
		public string Name
		{
			get
			{
				var name = Get<string>(NameProperty);
				return string.IsNullOrEmpty(name) ? StyleId ?? string.Empty : name;
			}
			set => SetValue(NameProperty, value);
		}

		public object Tag
		{
			get => Get<object>(TagProperty);
			set => SetValue(TagProperty, value);
		}

		public object ToolTip
		{
			get => Get<object>(ToolTipProperty);
			set => SetValue(ToolTipProperty, value);
		}

		public Cursor Cursor
		{
			get => Get<Cursor>(CursorProperty);
			set => SetValue(CursorProperty, value);
		}

		public Style Style
		{
			get => Get<Style>(StyleProperty);
			set => SetValue(StyleProperty, value);
		}

		public ContextMenu ContextMenu
		{
			get => Get<ContextMenu>(ContextMenuProperty);
			set => SetValue(ContextMenuProperty, value);
		}

		/// <summary>WPF's data context is the Xamarin.Forms binding context, which descendants inherit the same way.</summary>
		public object DataContext
		{
			get => BindingContext;
			set => SetValue(DataContextProperty, value);
		}

		public Style FocusVisualStyle
		{
			get => Get<Style>(FocusVisualStyleProperty);
			set => SetValue(FocusVisualStyleProperty, value);
		}

		public bool UseLayoutRounding
		{
			get => Get<bool>(UseLayoutRoundingProperty);
			set => SetValue(UseLayoutRoundingProperty, value);
		}

		public bool OverridesDefaultStyle
		{
			get => Get<bool>(OverridesDefaultStyleProperty);
			set => SetValue(OverridesDefaultStyleProperty, value);
		}

		/// <summary>The logical parent.</summary>
		public new DependencyObject Parent => LogicalParent;

		public DependencyObject TemplatedParent => null;

		public event DependencyPropertyChangedEventHandler DataContextChanged;

		// ---- styles --------------------------------------------------------------------------------------------------

		internal override bool TryGetStyleValue(XF.BindableProperty property, out object value)
		{
			var style = IsSet(StyleProperty) ? (Style)base.GetValue(StyleProperty) : null;
			if (style != null && property != StyleProperty && style.TryGetValue(property, out value))
				return true;

			value = null;
			return false;
		}

		/// <summary>A new style changes the value of every property either style sets that has no local value.</summary>
		void OnStyleChanged(Style oldStyle, Style newStyle)
		{
			var properties = (oldStyle?.Properties ?? Enumerable.Empty<DependencyProperty>())
				.Concat(newStyle?.Properties ?? Enumerable.Empty<DependencyProperty>()).Distinct().ToArray();
			foreach (var property in properties)
			{
				if (!IsSet(property.Bindable))
					Notify(property, null, GetEffectiveValue(property.Bindable));
			}
		}

		// ---- resources -----------------------------------------------------------------------------------------------

		public ResourceDictionary Resources
		{
			get => _resources ?? (_resources = new ResourceDictionary());
			set => _resources = value;
		}

		/// <summary>A resource from here up the logical tree, then the application's; throws when there is none.</summary>
		public object FindResource(object resourceKey) =>
			TryFindResource(resourceKey) ?? throw new ResourceReferenceKeyNotFoundException($"'{resourceKey}' resource not found.", resourceKey);

		public object TryFindResource(object resourceKey)
		{
			if (resourceKey == null)
				throw new ArgumentNullException(nameof(resourceKey));

			for (var d = (DependencyObject)this; d != null; d = d.LogicalParent)
			{
				if (d is FrameworkElement fe && fe._resources != null && fe._resources.TryGet(resourceKey, out var value))
					return value;
			}

			return Application.FindApplicationResource(resourceKey);
		}

		/// <summary>Follows the resource <paramref name="name"/> for <paramref name="dp"/>, as <c>{DynamicResource}</c> does.</summary>
		public void SetResourceReference(DependencyProperty dp, object name)
		{
			if (dp == null)
				throw new ArgumentNullException(nameof(dp));

			SetDynamicResource(dp.Bindable, name?.ToString());
		}

		/// <summary>An element named in this element's XAML, else anywhere below it.</summary>
		public object FindName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			try
			{
				var found = XF.NameScopeExtensions.FindByName<object>(this, name);
				if (found != null)
					return found;
			}
			catch (InvalidCastException)
			{
			}

			return LogicalTreeHelper.FindLogicalNode(this, name);
		}

		public void RegisterName(string name, object scopedElement) =>
			XF.Internals.NameScope.GetNameScope(this)?.RegisterName(name, scopedElement);

		public void UnregisterName(string name) => XF.Internals.NameScope.GetNameScope(this)?.UnregisterName(name);

		// ---- lifecycle -----------------------------------------------------------------------------------------------

		public static readonly RoutedEvent LoadedEvent =
			EventManager.RegisterRoutedEvent("Loaded", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(FrameworkElement));

		public static readonly RoutedEvent UnloadedEvent =
			EventManager.RegisterRoutedEvent("Unloaded", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(FrameworkElement));

		public static readonly RoutedEvent SizeChangedEvent =
			EventManager.RegisterRoutedEvent("SizeChanged", RoutingStrategy.Direct, typeof(SizeChangedEventHandler), typeof(FrameworkElement));

		public static readonly RoutedEvent RequestBringIntoViewEvent =
			EventManager.RegisterRoutedEvent("RequestBringIntoView", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FrameworkElement));

		public event RoutedEventHandler Loaded { add => AddHandler(LoadedEvent, value); remove => RemoveHandler(LoadedEvent, value); }

		public event RoutedEventHandler Unloaded { add => AddHandler(UnloadedEvent, value); remove => RemoveHandler(UnloadedEvent, value); }

		public event SizeChangedEventHandler SizeChanged { add => AddHandler(SizeChangedEvent, value); remove => RemoveHandler(SizeChangedEvent, value); }

		public event EventHandler Initialized;

		/// <summary>Whether the element is in the tree of a window that is showing.</summary>
		public bool IsLoaded => _isLoaded;

		public bool IsInitialized => _isInitialized || _initCount == 0;

		public virtual void BeginInit() => _initCount++;

		public virtual void EndInit()
		{
			if (_initCount > 0)
				_initCount--;

			if (_initCount == 0 && !_isInitialized)
			{
				_isInitialized = true;
				OnInitialized(EventArgs.Empty);
			}
		}

		protected virtual void OnInitialized(EventArgs e) => Initialized?.Invoke(this, e);

		/// <summary>Raises <see cref="Loaded"/> here and below, parent first, as WPF does once a window shows.</summary>
		internal void BroadcastLoaded()
		{
			if (_isLoaded)
				return;

			_isLoaded = true;
			RaiseEvent(new RoutedEventArgs(LoadedEvent, this));
			foreach (var child in LogicalChildrenCore.OfType<FrameworkElement>().ToArray())
				child.BroadcastLoaded();
		}

		internal void BroadcastUnloaded()
		{
			if (!_isLoaded)
				return;

			_isLoaded = false;
			RaiseEvent(new RoutedEventArgs(UnloadedEvent, this));
			foreach (var child in LogicalChildrenCore.OfType<FrameworkElement>().ToArray())
				child.BroadcastUnloaded();
		}

		internal override void OnLogicalParentChanged(DependencyObject oldParent)
		{
			base.OnLogicalParentChanged(oldParent);

			// An element added to a showing window loads; one taken out of it unloads.
			if (LogicalParent is FrameworkElement parent && parent.IsLoaded)
				BroadcastLoaded();
			else if (LogicalParent == null && _isLoaded)
				BroadcastUnloaded();

			if (HasNativeView)
				(LogicalParent as FrameworkElement)?.OnChildLayoutChanged(this);
		}

		internal override void OnRenderSizeChanged(Size previous, Size size)
		{
			base.OnRenderSizeChanged(previous, size);
			OnRenderSizeChanged(new SizeChangedInfo(this, previous, size.Width != previous.Width, size.Height != previous.Height));
			RaiseEvent(new SizeChangedEventArgs(this, previous, size));
		}

		protected virtual void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
		}

		public void BringIntoView() => RaiseEvent(new RoutedEventArgs(RequestBringIntoViewEvent, this));

		public virtual void OnApplyTemplate()
		{
		}

		public bool ApplyTemplate() => false;

		public bool MoveFocus(TraversalRequest request) => false;

		// ---- the Xamarin.Forms view ----------------------------------------------------------------------------------

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyLayout();
			NativeView.FlowDirection = FlowDirection == FlowDirection.RightToLeft ? XF.FlowDirection.RightToLeft : XF.FlowDirection.LeftToRight;
		}

		internal override void OnWidgetAttached(Gtk.Widget widget)
		{
			base.OnWidgetAttached(widget);
			ApplyToolTip(widget);
			NativeInput.ApplyCursor(this, widget);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			var p = e.Property.Bindable;
			if (p == StyleProperty)
			{
				OnStyleChanged(e.OldValue as Style, e.NewValue as Style);
				return;
			}

			if (p == DataContextProperty)
			{
				var old = BindingContext;
				BindingContext = e.NewValue;
				DataContextChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(DataContextProperty, old, e.NewValue));
				return;
			}

			if (!HasNativeView)
				return;

			if (IsLayoutProperty(p))
				ApplyLayout();
			else if (p == FlowDirectionProperty)
				NativeView.FlowDirection = FlowDirection == FlowDirection.RightToLeft ? XF.FlowDirection.RightToLeft : XF.FlowDirection.LeftToRight;
			else if (p == ToolTipProperty)
				ApplyToolTip(NativeInput.WidgetOf(this));
			else if (p == CursorProperty)
				NativeInput.ApplyCursor(this, NativeInput.WidgetOf(this));
		}

		internal static bool IsLayoutProperty(XF.BindableProperty p) =>
			p == WidthProperty || p == HeightProperty || p == MinWidthProperty || p == MinHeightProperty
			|| p == MarginProperty || p == HorizontalAlignmentProperty || p == VerticalAlignmentProperty;

		/// <summary>Size, margin and alignment onto the view; then the parent panel places it by its own rules.</summary>
		internal void ApplyLayout()
		{
			var view = NativeView;
			var width = Width;
			var height = Height;
			view.WidthRequest = double.IsNaN(width) ? -1 : width;
			view.HeightRequest = double.IsNaN(height) ? -1 : height;
			view.MinimumWidthRequest = MinWidth > 0 ? MinWidth : -1;
			view.MinimumHeightRequest = MinHeight > 0 ? MinHeight : -1;
			view.Margin = Margin.ToForms();
			view.HorizontalOptions = ToForms(HorizontalAlignment);
			view.VerticalOptions = ToForms(VerticalAlignment);
			(LogicalParent as FrameworkElement)?.OnChildLayoutChanged(this);
		}

		/// <summary>A child's size, margin or alignment changed: panels that place children themselves re-place it.</summary>
		internal virtual void OnChildLayoutChanged(FrameworkElement child)
		{
		}

		internal static XF.LayoutOptions ToForms(HorizontalAlignment alignment)
		{
			switch (alignment)
			{
				case HorizontalAlignment.Left:
					return XF.LayoutOptions.Start;
				case HorizontalAlignment.Center:
					return XF.LayoutOptions.Center;
				case HorizontalAlignment.Right:
					return XF.LayoutOptions.End;
				default:
					return XF.LayoutOptions.Fill;
			}
		}

		internal static XF.LayoutOptions ToForms(VerticalAlignment alignment)
		{
			switch (alignment)
			{
				case VerticalAlignment.Top:
					return XF.LayoutOptions.Start;
				case VerticalAlignment.Center:
					return XF.LayoutOptions.Center;
				case VerticalAlignment.Bottom:
					return XF.LayoutOptions.End;
				default:
					return XF.LayoutOptions.Fill;
			}
		}

		void ApplyToolTip(Gtk.Widget widget)
		{
			if (widget == null)
				return;

			var tip = ToolTip;
			widget.TooltipText = tip == null ? null : (tip as ContentControl)?.Content?.ToString() ?? tip.ToString();
		}

		internal override IEnumerable LogicalChildrenCore => Array.Empty<object>();
	}

	public class SizeChangedInfo
	{
		public SizeChangedInfo(UIElement element, Size previousSize, bool widthChanged, bool heightChanged)
		{
			Element = element;
			PreviousSize = previousSize;
			NewSize = element.RenderSize;
			WidthChanged = widthChanged;
			HeightChanged = heightChanged;
		}

		internal UIElement Element { get; }

		public Size PreviousSize { get; }

		public Size NewSize { get; }

		public bool WidthChanged { get; }

		public bool HeightChanged { get; }
	}

	public class ResourceReferenceKeyNotFoundException : InvalidOperationException
	{
		public ResourceReferenceKeyNotFoundException()
		{
		}

		public ResourceReferenceKeyNotFoundException(string message, object resourceKey) : base(message) => Key = resourceKey;

		public object Key { get; }
	}
}

namespace System.Windows.Input
{
	public enum FocusNavigationDirection
	{
		Next,
		Previous,
		First,
		Last,
		Left,
		Right,
		Up,
		Down,
	}

	public class TraversalRequest
	{
		public TraversalRequest(FocusNavigationDirection focusNavigationDirection) => FocusNavigationDirection = focusNavigationDirection;

		public FocusNavigationDirection FocusNavigationDirection { get; }

		public bool Wrapped { get; set; }
	}
}
