using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows
{
	/// <summary>
	/// An element on screen. It owns a Xamarin.Forms view (<see cref="NativeView"/>), created the first time it is
	/// needed, and keeps it in step with its WPF properties; the element itself stays in the WPF logical tree, which
	/// is what routed events, inheritance and resource lookup follow.
	/// </summary>
	public class UIElement : Visual, IInputElement
	{
		XF.View _native;
		Dictionary<RoutedEvent, List<(Delegate Handler, bool HandledEventsToo)>> _handlers;
		CommandBindingCollection _commandBindings;
		InputBindingCollection _inputBindings;
		Size _lastSize;

		public UIElement()
		{
		}

		// ---- the Xamarin.Forms view ----------------------------------------------------------------------------------

		/// <summary>The view that draws this element, created on first use and then kept in step.</summary>
		internal XF.View NativeView
		{
			get
			{
				if (_native == null)
				{
					_native = CreateNativeView() ?? new XF.ContentView();
					NativeInput.Track(this, _native);
					_native.SizeChanged += OnNativeSizeChanged;
					OnNativeCreated();
					SyncNative();
				}

				return _native;
			}
		}

		internal bool HasNativeView => _native != null;

		/// <summary>The Xamarin.Forms view this kind of element draws with.</summary>
		internal virtual XF.View CreateNativeView() => new XF.ContentView();

		/// <summary>Called once, right after <see cref="CreateNativeView"/>, before the first <see cref="SyncNative"/>.</summary>
		internal virtual void OnNativeCreated()
		{
		}

		/// <summary>Pushes every property this element maps onto its view; overrides add their own.</summary>
		internal virtual void SyncNative()
		{
			ApplyVisibility();
			ApplyEnabled();
			_native.InputTransparent = !IsHitTestVisible || Visibility != Visibility.Visible;
			if (_native is XF.Layout layout)
				layout.IsClippedToBounds = ClipToBounds;
			if (!Focusable)
				_native.IsTabStop = false;
		}

		/// <summary>The GTK widget of the view has been created: apply what only GTK can do (cursor, tooltip).</summary>
		internal virtual void OnWidgetAttached(Gtk.Widget widget)
		{
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			var p = e.Property.Bindable;
			if (p == IsEnabledProperty)
			{
				OnEnabledTreeChanged();
				return;
			}

			if (p == VisibilityProperty)
			{
				if (HasNativeView)
				{
					ApplyVisibility();
					_native.InputTransparent = !IsHitTestVisible || Visibility != Visibility.Visible;
				}

				IsVisibleChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(IsVisibleProperty, !IsVisible, IsVisible));
				return;
			}

			if (!HasNativeView)
			{
				if (p == FocusableProperty)
					FocusableChanged?.Invoke(this, e);
				return;
			}

			if (p == OpacityProperty)
				ApplyVisibility();
			else if (p == IsHitTestVisibleProperty)
				_native.InputTransparent = !IsHitTestVisible || Visibility != Visibility.Visible;
			else if (p == ClipToBoundsProperty && _native is XF.Layout layout)
				layout.IsClippedToBounds = ClipToBounds;
			else if (p == FocusableProperty)
				_native.IsTabStop = Focusable;

			if (p == FocusableProperty)
				FocusableChanged?.Invoke(this, e);
		}

		/// <summary>
		/// WPF's three states: <see cref="Visibility.Hidden"/> keeps its place in the layout, so it is transparent
		/// rather than invisible - Xamarin.Forms lays out nothing for an invisible view, which is <see cref="Visibility.Collapsed"/>.
		/// </summary>
		void ApplyVisibility()
		{
			var visibility = Visibility;
			_native.IsVisible = visibility != Visibility.Collapsed;
			_native.Opacity = visibility == Visibility.Hidden ? 0 : Opacity;
		}

		void ApplyEnabled()
		{
			// Xamarin.Forms does not disable the children of a disabled layout; WPF does.
			_native.IsEnabled = IsEnabled;
		}

		void OnEnabledTreeChanged()
		{
			if (HasNativeView)
				ApplyEnabled();

			IsEnabledChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(IsEnabledProperty, !IsEnabled, IsEnabled));

			foreach (var child in LogicalChildrenCore.OfType<UIElement>())
			{
				if (child.LocalEnabled)
					child.OnEnabledTreeChanged();
			}
		}

		/// <summary>Re-evaluates <see cref="IsEnabled"/> after a change of <see cref="IsEnabledCore"/>.</summary>
		protected void CoerceIsEnabled() => OnEnabledTreeChanged();

		internal override void OnLogicalParentChanged(DependencyObject oldParent)
		{
			base.OnLogicalParentChanged(oldParent);
			if (!LocalEnabled)
				return;
			if (HasNativeView)
				ApplyEnabled();
			foreach (var child in LogicalChildrenCore.OfType<UIElement>())
				child.OnLogicalParentChanged(this);
		}

		void OnNativeSizeChanged(object sender, EventArgs e)
		{
			var size = RenderSize;
			if (size == _lastSize)
				return;

			var previous = _lastSize;
			_lastSize = size;
			OnRenderSizeChanged(previous, size);
		}

		internal virtual void OnRenderSizeChanged(Size previous, Size size)
		{
		}

		// ---- properties ----------------------------------------------------------------------------------------------

		public static readonly XF.BindableProperty VisibilityProperty = Dp.Register<UIElement>(nameof(Visibility), typeof(Visibility), Visibility.Visible);
		public static readonly XF.BindableProperty IsEnabledProperty = Dp.Register<UIElement>(nameof(IsEnabled), typeof(bool), true);
		public static readonly XF.BindableProperty OpacityProperty = Dp.Register<UIElement>(nameof(Opacity), typeof(double), 1.0);
		public static readonly XF.BindableProperty IsHitTestVisibleProperty = Dp.Register<UIElement>(nameof(IsHitTestVisible), typeof(bool), true);
		public static readonly XF.BindableProperty FocusableProperty = Dp.Register<UIElement>(nameof(Focusable), typeof(bool), false);
		public static readonly XF.BindableProperty ClipToBoundsProperty = Dp.Register<UIElement>(nameof(ClipToBounds), typeof(bool), false);
		public static readonly XF.BindableProperty AllowDropProperty = Dp.Register<UIElement>(nameof(AllowDrop), typeof(bool), false);
		public static readonly XF.BindableProperty SnapsToDevicePixelsProperty = Dp.Register<UIElement>(nameof(SnapsToDevicePixels), typeof(bool), false);
		public static readonly XF.BindableProperty UidProperty = Dp.Register<UIElement>(nameof(Uid), typeof(string), string.Empty);
		/// <summary>Read-only in WPF, and never set here: <see cref="IsVisible"/> is computed. It exists for the event arguments.</summary>
		public static readonly XF.BindableProperty IsVisibleProperty = Dp.Register<UIElement>(nameof(IsVisible), typeof(bool), false);

		public Visibility Visibility
		{
			get => Get<Visibility>(VisibilityProperty);
			set => SetValue(VisibilityProperty, value);
		}

		/// <summary>Whether the element can be used: its own setting, and every ancestor's (WPF coerces it the same way).</summary>
		public bool IsEnabled
		{
			get => LocalEnabled && IsEnabledCore && (!(LogicalParent is UIElement parent) || parent.IsEnabled);
			set => SetValue(IsEnabledProperty, value);
		}

		/// <summary>A further condition a subclass puts on <see cref="IsEnabled"/> (a command source: its command).</summary>
		protected virtual bool IsEnabledCore => true;

		/// <summary>The element's own setting, whatever its ancestors say.</summary>
		bool LocalEnabled => (bool)GetEffectiveValue(IsEnabledProperty);

		public double Opacity
		{
			get => Get<double>(OpacityProperty);
			set => SetValue(OpacityProperty, value);
		}

		public bool IsHitTestVisible
		{
			get => Get<bool>(IsHitTestVisibleProperty);
			set => SetValue(IsHitTestVisibleProperty, value);
		}

		public bool Focusable
		{
			get => Get<bool>(FocusableProperty);
			set => SetValue(FocusableProperty, value);
		}

		public bool ClipToBounds
		{
			get => Get<bool>(ClipToBoundsProperty);
			set => SetValue(ClipToBoundsProperty, value);
		}

		public bool AllowDrop
		{
			get => Get<bool>(AllowDropProperty);
			set => SetValue(AllowDropProperty, value);
		}

		public bool SnapsToDevicePixels
		{
			get => Get<bool>(SnapsToDevicePixelsProperty);
			set => SetValue(SnapsToDevicePixelsProperty, value);
		}

		public string Uid
		{
			get => Get<string>(UidProperty);
			set => SetValue(UidProperty, value);
		}

		/// <summary>Whether the element is on screen: visible itself, in a visible ancestor, in a shown window.</summary>
		public bool IsVisible
		{
			get
			{
				for (DependencyObject d = this; d != null; d = d.LogicalParent)
				{
					if (d is Window w)
						return w.IsShown && w.Visibility == Visibility.Visible;
					if (d is UIElement u && u.Visibility != Visibility.Visible)
						return false;
				}

				return false;
			}
		}

		public bool IsFocused => ReferenceEquals(Keyboard.FocusedElement, this);

		public bool IsKeyboardFocused => IsFocused;

		public bool IsKeyboardFocusWithin
		{
			get
			{
				for (var d = Keyboard.FocusedElement as DependencyObject; d != null; d = d.LogicalParent)
				{
					if (ReferenceEquals(d, this))
						return true;
				}

				return false;
			}
		}

		public bool IsMouseOver
		{
			get
			{
				for (var d = Mouse.DirectlyOver as DependencyObject; d != null; d = d.LogicalParent)
				{
					if (ReferenceEquals(d, this))
						return true;
				}

				return false;
			}
		}

		public bool IsMouseDirectlyOver => ReferenceEquals(Mouse.DirectlyOver, this);

		public bool IsMouseCaptured => ReferenceEquals(Mouse.Captured, this);

		public bool IsStylusOver => false;

		/// <summary>The size the element was laid out at.</summary>
		public Size RenderSize
		{
			get
			{
				if (!HasNativeView)
					return new Size();

				return new Size(Math.Max(0, _native.Width), Math.Max(0, _native.Height));
			}
			set
			{
			}
		}

		/// <summary>The size the element asks for, measured by Xamarin.Forms.</summary>
		public Size DesiredSize
		{
			get
			{
				var request = NativeView.Measure(double.PositiveInfinity, double.PositiveInfinity, XF.MeasureFlags.IncludeMargins).Request;
				return new Size(Math.Max(0, request.Width), Math.Max(0, request.Height));
			}
		}

		public bool IsMeasureValid => true;

		public bool IsArrangeValid => true;

		public event DependencyPropertyChangedEventHandler IsEnabledChanged;

		public event DependencyPropertyChangedEventHandler IsVisibleChanged;

		public event DependencyPropertyChangedEventHandler FocusableChanged;

		public bool Focus()
		{
			if (!Focusable || !IsEnabled)
				return false;

			return HasNativeView && NativeView.Focus();
		}

		public void Measure(Size availableSize) => NativeView.Measure(availableSize.Width, availableSize.Height, XF.MeasureFlags.IncludeMargins);

		public void Arrange(Rect finalRect)
		{
		}

		/// <summary>Lays the element out now rather than on the next pass.</summary>
		public void UpdateLayout() => (_native as XF.Layout)?.ForceLayout();

		public void InvalidateMeasure() => (_native as XF.Layout)?.ForceLayout();

		public void InvalidateArrange() => InvalidateMeasure();

		public void InvalidateVisual() => InvalidateMeasure();

		public CommandBindingCollection CommandBindings => _commandBindings ?? (_commandBindings = new CommandBindingCollection());

		public InputBindingCollection InputBindings => _inputBindings ?? (_inputBindings = new InputBindingCollection());

		internal CommandBindingCollection CommandBindingsIfAny => _commandBindings;

		internal InputBindingCollection InputBindingsIfAny => _inputBindings;

		// ---- routed events -------------------------------------------------------------------------------------------

		public static readonly RoutedEvent PreviewMouseDownEvent = Register<MouseButtonEventHandler>("PreviewMouseDown", RoutingStrategy.Tunnel);
		public static readonly RoutedEvent MouseDownEvent = Register<MouseButtonEventHandler>("MouseDown", RoutingStrategy.Bubble);
		public static readonly RoutedEvent PreviewMouseUpEvent = Register<MouseButtonEventHandler>("PreviewMouseUp", RoutingStrategy.Tunnel);
		public static readonly RoutedEvent MouseUpEvent = Register<MouseButtonEventHandler>("MouseUp", RoutingStrategy.Bubble);
		public static readonly RoutedEvent PreviewMouseLeftButtonDownEvent = Register<MouseButtonEventHandler>("PreviewMouseLeftButtonDown", RoutingStrategy.Direct);
		public static readonly RoutedEvent MouseLeftButtonDownEvent = Register<MouseButtonEventHandler>("MouseLeftButtonDown", RoutingStrategy.Direct);
		public static readonly RoutedEvent PreviewMouseLeftButtonUpEvent = Register<MouseButtonEventHandler>("PreviewMouseLeftButtonUp", RoutingStrategy.Direct);
		public static readonly RoutedEvent MouseLeftButtonUpEvent = Register<MouseButtonEventHandler>("MouseLeftButtonUp", RoutingStrategy.Direct);
		public static readonly RoutedEvent PreviewMouseRightButtonDownEvent = Register<MouseButtonEventHandler>("PreviewMouseRightButtonDown", RoutingStrategy.Direct);
		public static readonly RoutedEvent MouseRightButtonDownEvent = Register<MouseButtonEventHandler>("MouseRightButtonDown", RoutingStrategy.Direct);
		public static readonly RoutedEvent PreviewMouseRightButtonUpEvent = Register<MouseButtonEventHandler>("PreviewMouseRightButtonUp", RoutingStrategy.Direct);
		public static readonly RoutedEvent MouseRightButtonUpEvent = Register<MouseButtonEventHandler>("MouseRightButtonUp", RoutingStrategy.Direct);
		public static readonly RoutedEvent PreviewMouseMoveEvent = Register<MouseEventHandler>("PreviewMouseMove", RoutingStrategy.Tunnel);
		public static readonly RoutedEvent MouseMoveEvent = Register<MouseEventHandler>("MouseMove", RoutingStrategy.Bubble);
		public static readonly RoutedEvent PreviewMouseWheelEvent = Register<MouseWheelEventHandler>("PreviewMouseWheel", RoutingStrategy.Tunnel);
		public static readonly RoutedEvent MouseWheelEvent = Register<MouseWheelEventHandler>("MouseWheel", RoutingStrategy.Bubble);
		public static readonly RoutedEvent MouseEnterEvent = Register<MouseEventHandler>("MouseEnter", RoutingStrategy.Direct);
		public static readonly RoutedEvent MouseLeaveEvent = Register<MouseEventHandler>("MouseLeave", RoutingStrategy.Direct);
		public static readonly RoutedEvent PreviewKeyDownEvent = Register<KeyEventHandler>("PreviewKeyDown", RoutingStrategy.Tunnel);
		public static readonly RoutedEvent KeyDownEvent = Register<KeyEventHandler>("KeyDown", RoutingStrategy.Bubble);
		public static readonly RoutedEvent PreviewKeyUpEvent = Register<KeyEventHandler>("PreviewKeyUp", RoutingStrategy.Tunnel);
		public static readonly RoutedEvent KeyUpEvent = Register<KeyEventHandler>("KeyUp", RoutingStrategy.Bubble);
		public static readonly RoutedEvent PreviewTextInputEvent = Register<TextCompositionEventHandler>("PreviewTextInput", RoutingStrategy.Tunnel);
		public static readonly RoutedEvent TextInputEvent = Register<TextCompositionEventHandler>("TextInput", RoutingStrategy.Bubble);
		public static readonly RoutedEvent GotFocusEvent = Register<RoutedEventHandler>("GotFocus", RoutingStrategy.Bubble);
		public static readonly RoutedEvent LostFocusEvent = Register<RoutedEventHandler>("LostFocus", RoutingStrategy.Bubble);
		public static readonly RoutedEvent GotKeyboardFocusEvent = Register<KeyboardFocusChangedEventHandler>("GotKeyboardFocus", RoutingStrategy.Bubble);
		public static readonly RoutedEvent LostKeyboardFocusEvent = Register<KeyboardFocusChangedEventHandler>("LostKeyboardFocus", RoutingStrategy.Bubble);

		static RoutedEvent Register<THandler>(string name, RoutingStrategy strategy) =>
			EventManager.RegisterRoutedEvent(name, strategy, typeof(THandler), typeof(UIElement));

		public event MouseButtonEventHandler PreviewMouseDown { add => AddHandler(PreviewMouseDownEvent, value); remove => RemoveHandler(PreviewMouseDownEvent, value); }
		public event MouseButtonEventHandler MouseDown { add => AddHandler(MouseDownEvent, value); remove => RemoveHandler(MouseDownEvent, value); }
		public event MouseButtonEventHandler PreviewMouseUp { add => AddHandler(PreviewMouseUpEvent, value); remove => RemoveHandler(PreviewMouseUpEvent, value); }
		public event MouseButtonEventHandler MouseUp { add => AddHandler(MouseUpEvent, value); remove => RemoveHandler(MouseUpEvent, value); }
		public event MouseButtonEventHandler PreviewMouseLeftButtonDown { add => AddHandler(PreviewMouseLeftButtonDownEvent, value); remove => RemoveHandler(PreviewMouseLeftButtonDownEvent, value); }
		public event MouseButtonEventHandler MouseLeftButtonDown { add => AddHandler(MouseLeftButtonDownEvent, value); remove => RemoveHandler(MouseLeftButtonDownEvent, value); }
		public event MouseButtonEventHandler PreviewMouseLeftButtonUp { add => AddHandler(PreviewMouseLeftButtonUpEvent, value); remove => RemoveHandler(PreviewMouseLeftButtonUpEvent, value); }
		public event MouseButtonEventHandler MouseLeftButtonUp { add => AddHandler(MouseLeftButtonUpEvent, value); remove => RemoveHandler(MouseLeftButtonUpEvent, value); }
		public event MouseButtonEventHandler PreviewMouseRightButtonDown { add => AddHandler(PreviewMouseRightButtonDownEvent, value); remove => RemoveHandler(PreviewMouseRightButtonDownEvent, value); }
		public event MouseButtonEventHandler MouseRightButtonDown { add => AddHandler(MouseRightButtonDownEvent, value); remove => RemoveHandler(MouseRightButtonDownEvent, value); }
		public event MouseButtonEventHandler PreviewMouseRightButtonUp { add => AddHandler(PreviewMouseRightButtonUpEvent, value); remove => RemoveHandler(PreviewMouseRightButtonUpEvent, value); }
		public event MouseButtonEventHandler MouseRightButtonUp { add => AddHandler(MouseRightButtonUpEvent, value); remove => RemoveHandler(MouseRightButtonUpEvent, value); }
		public event MouseEventHandler PreviewMouseMove { add => AddHandler(PreviewMouseMoveEvent, value); remove => RemoveHandler(PreviewMouseMoveEvent, value); }
		public event MouseEventHandler MouseMove { add => AddHandler(MouseMoveEvent, value); remove => RemoveHandler(MouseMoveEvent, value); }
		public event MouseWheelEventHandler PreviewMouseWheel { add => AddHandler(PreviewMouseWheelEvent, value); remove => RemoveHandler(PreviewMouseWheelEvent, value); }
		public event MouseWheelEventHandler MouseWheel { add => AddHandler(MouseWheelEvent, value); remove => RemoveHandler(MouseWheelEvent, value); }
		public event MouseEventHandler MouseEnter { add => AddHandler(MouseEnterEvent, value); remove => RemoveHandler(MouseEnterEvent, value); }
		public event MouseEventHandler MouseLeave { add => AddHandler(MouseLeaveEvent, value); remove => RemoveHandler(MouseLeaveEvent, value); }
		public event KeyEventHandler PreviewKeyDown { add => AddHandler(PreviewKeyDownEvent, value); remove => RemoveHandler(PreviewKeyDownEvent, value); }
		public event KeyEventHandler KeyDown { add => AddHandler(KeyDownEvent, value); remove => RemoveHandler(KeyDownEvent, value); }
		public event KeyEventHandler PreviewKeyUp { add => AddHandler(PreviewKeyUpEvent, value); remove => RemoveHandler(PreviewKeyUpEvent, value); }
		public event KeyEventHandler KeyUp { add => AddHandler(KeyUpEvent, value); remove => RemoveHandler(KeyUpEvent, value); }
		public event TextCompositionEventHandler PreviewTextInput { add => AddHandler(PreviewTextInputEvent, value); remove => RemoveHandler(PreviewTextInputEvent, value); }
		public event TextCompositionEventHandler TextInput { add => AddHandler(TextInputEvent, value); remove => RemoveHandler(TextInputEvent, value); }
		public event RoutedEventHandler GotFocus { add => AddHandler(GotFocusEvent, value); remove => RemoveHandler(GotFocusEvent, value); }
		public event RoutedEventHandler LostFocus { add => AddHandler(LostFocusEvent, value); remove => RemoveHandler(LostFocusEvent, value); }
		public event KeyboardFocusChangedEventHandler GotKeyboardFocus { add => AddHandler(GotKeyboardFocusEvent, value); remove => RemoveHandler(GotKeyboardFocusEvent, value); }
		public event KeyboardFocusChangedEventHandler LostKeyboardFocus { add => AddHandler(LostKeyboardFocusEvent, value); remove => RemoveHandler(LostKeyboardFocusEvent, value); }

		public void AddHandler(RoutedEvent routedEvent, Delegate handler) => AddHandler(routedEvent, handler, false);

		public void AddHandler(RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
		{
			if (routedEvent == null)
				throw new ArgumentNullException(nameof(routedEvent));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			if (_handlers == null)
				_handlers = new Dictionary<RoutedEvent, List<(Delegate, bool)>>();
			if (!_handlers.TryGetValue(routedEvent, out var list))
				_handlers.Add(routedEvent, list = new List<(Delegate, bool)>());

			list.Add((handler, handledEventsToo));
			OnHandlerAdded(routedEvent);
		}

		public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
		{
			if (routedEvent == null || handler == null || _handlers == null || !_handlers.TryGetValue(routedEvent, out var list))
				return;

			for (var i = list.Count - 1; i >= 0; i--)
			{
				if (Equals(list[i].Handler, handler))
				{
					list.RemoveAt(i);
					return;
				}
			}
		}

		/// <summary>A handler was attached: a control whose event comes from its native view can start listening.</summary>
		internal virtual void OnHandlerAdded(RoutedEvent routedEvent)
		{
		}

		internal bool HasHandlers(RoutedEvent routedEvent) =>
			_handlers != null && _handlers.TryGetValue(routedEvent, out var list) && list.Count > 0;

		/// <summary>Raises <paramref name="e"/> along its route: down from the root, up to it, or here only.</summary>
		public void RaiseEvent(RoutedEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException(nameof(e));
			if (e.RoutedEvent == null)
				throw new InvalidOperationException("The RoutedEvent of the arguments must be set.");

			if (e.Source == null)
				e.SetSource(this);

			foreach (var target in Route(e.RoutedEvent.RoutingStrategy))
				InvokeHandlersOf(target, e);
		}

		/// <summary>Where an event raised here travels, in order.</summary>
		IEnumerable<DependencyObject> Route(RoutingStrategy strategy)
		{
			if (strategy == RoutingStrategy.Direct)
				return new[] { this };

			var route = new List<DependencyObject>();
			for (DependencyObject d = this; d != null; d = d.LogicalParent)
				route.Add(d);

			if (strategy == RoutingStrategy.Tunnel)
				route.Reverse();

			return route;
		}

		/// <summary>This element's part of a route: its class handler, the registered ones, then its own handlers.</summary>
		internal static void InvokeHandlersOf(DependencyObject target, RoutedEventArgs e)
		{
			if (target is UIElement element)
				element.InvokeHandlers(e);
			else
				EventManager.InvokeClassHandlers(target, e);
		}

		void InvokeHandlers(RoutedEventArgs e)
		{
			OnClassHandler(e);
			EventManager.InvokeClassHandlers(this, e);

			if (_handlers == null || !_handlers.TryGetValue(e.RoutedEvent, out var list))
				return;

			foreach (var (handler, handledToo) in list.ToArray())
			{
				if (!e.Handled || handledToo)
					e.InvokeHandler(handler, this);
			}
		}

		/// <summary>
		/// The virtual <c>OnXxx</c> methods, WPF's class handlers: they run for every element on the route, before
		/// the handlers attached to it. The mouse-button events also raise their left/right-button counterparts here,
		/// on this element, as WPF does.
		/// </summary>
		void OnClassHandler(RoutedEventArgs e)
		{
			var re = e.RoutedEvent;
			if (re == MouseDownEvent)
			{
				var m = (MouseButtonEventArgs)e;
				OnMouseDown(m);
				RaiseButtonEvent(m, m.ChangedButton == MouseButton.Left ? MouseLeftButtonDownEvent : m.ChangedButton == MouseButton.Right ? MouseRightButtonDownEvent : null);
				OnMouseDownRouted(m);
			}
			else if (re == MouseUpEvent)
			{
				var m = (MouseButtonEventArgs)e;
				OnMouseUp(m);
				RaiseButtonEvent(m, m.ChangedButton == MouseButton.Left ? MouseLeftButtonUpEvent : m.ChangedButton == MouseButton.Right ? MouseRightButtonUpEvent : null);
			}
			else if (re == PreviewMouseDownEvent)
			{
				var m = (MouseButtonEventArgs)e;
				OnPreviewMouseDown(m);
				RaiseButtonEvent(m, m.ChangedButton == MouseButton.Left ? PreviewMouseLeftButtonDownEvent : m.ChangedButton == MouseButton.Right ? PreviewMouseRightButtonDownEvent : null);
			}
			else if (re == PreviewMouseUpEvent)
			{
				var m = (MouseButtonEventArgs)e;
				OnPreviewMouseUp(m);
				RaiseButtonEvent(m, m.ChangedButton == MouseButton.Left ? PreviewMouseLeftButtonUpEvent : m.ChangedButton == MouseButton.Right ? PreviewMouseRightButtonUpEvent : null);
			}
			else if (re == MouseLeftButtonDownEvent)
				OnMouseLeftButtonDown((MouseButtonEventArgs)e);
			else if (re == MouseLeftButtonUpEvent)
				OnMouseLeftButtonUp((MouseButtonEventArgs)e);
			else if (re == MouseRightButtonDownEvent)
				OnMouseRightButtonDown((MouseButtonEventArgs)e);
			else if (re == MouseRightButtonUpEvent)
				OnMouseRightButtonUp((MouseButtonEventArgs)e);
			else if (re == PreviewMouseLeftButtonDownEvent)
				OnPreviewMouseLeftButtonDown((MouseButtonEventArgs)e);
			else if (re == PreviewMouseLeftButtonUpEvent)
				OnPreviewMouseLeftButtonUp((MouseButtonEventArgs)e);
			else if (re == MouseMoveEvent)
				OnMouseMove((MouseEventArgs)e);
			else if (re == PreviewMouseMoveEvent)
				OnPreviewMouseMove((MouseEventArgs)e);
			else if (re == MouseWheelEvent)
				OnMouseWheel((MouseWheelEventArgs)e);
			else if (re == PreviewMouseWheelEvent)
				OnPreviewMouseWheel((MouseWheelEventArgs)e);
			else if (re == MouseEnterEvent)
				OnMouseEnter((MouseEventArgs)e);
			else if (re == MouseLeaveEvent)
				OnMouseLeave((MouseEventArgs)e);
			else if (re == KeyDownEvent)
				OnKeyDown((KeyEventArgs)e);
			else if (re == PreviewKeyDownEvent)
				OnPreviewKeyDown((KeyEventArgs)e);
			else if (re == KeyUpEvent)
				OnKeyUp((KeyEventArgs)e);
			else if (re == PreviewKeyUpEvent)
				OnPreviewKeyUp((KeyEventArgs)e);
			else if (re == TextInputEvent)
				OnTextInput((TextCompositionEventArgs)e);
			else if (re == PreviewTextInputEvent)
				OnPreviewTextInput((TextCompositionEventArgs)e);
			else if (re == GotFocusEvent)
				OnGotFocus(e);
			else if (re == LostFocusEvent)
				OnLostFocus(e);
			else
				OnOtherClassHandler(e);
		}

		/// <summary>Class handlers of the routed events subclasses declare.</summary>
		internal virtual void OnOtherClassHandler(RoutedEventArgs e)
		{
		}

		/// <summary>After the mouse-down handlers of this element have run: where a control detects a double click.</summary>
		internal virtual void OnMouseDownRouted(MouseButtonEventArgs e)
		{
		}

		/// <summary>Raises the button-specific counterpart of a mouse-button event on this element, with the same arguments.</summary>
		void RaiseButtonEvent(MouseButtonEventArgs e, RoutedEvent buttonEvent)
		{
			if (buttonEvent == null)
				return;

			var original = e.RoutedEvent;
			e.RoutedEvent = buttonEvent;
			try
			{
				InvokeHandlers(e);
			}
			finally
			{
				e.RoutedEvent = original;
			}
		}

		protected virtual void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnMouseDown(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnMouseUp(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnMouseRightButtonDown(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnPreviewMouseMove(MouseEventArgs e)
		{
		}

		protected virtual void OnMouseMove(MouseEventArgs e)
		{
		}

		protected virtual void OnPreviewMouseWheel(MouseWheelEventArgs e)
		{
		}

		protected virtual void OnMouseWheel(MouseWheelEventArgs e)
		{
		}

		protected virtual void OnMouseEnter(MouseEventArgs e)
		{
		}

		protected virtual void OnMouseLeave(MouseEventArgs e)
		{
		}

		protected virtual void OnPreviewKeyDown(KeyEventArgs e)
		{
		}

		protected virtual void OnKeyDown(KeyEventArgs e)
		{
		}

		protected virtual void OnPreviewKeyUp(KeyEventArgs e)
		{
		}

		protected virtual void OnKeyUp(KeyEventArgs e)
		{
		}

		protected virtual void OnPreviewTextInput(TextCompositionEventArgs e)
		{
		}

		protected virtual void OnTextInput(TextCompositionEventArgs e)
		{
		}

		protected virtual void OnGotFocus(RoutedEventArgs e)
		{
		}

		protected virtual void OnLostFocus(RoutedEventArgs e)
		{
		}
	}
}
