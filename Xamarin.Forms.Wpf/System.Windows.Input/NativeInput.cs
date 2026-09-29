using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms.Wpf;
using WpfWindow = System.Windows.Window;
using XF = Xamarin.Forms;

namespace System.Windows.Input
{
	/// <summary>
	/// WPF input over GTK: the keyboard, mouse and focus events of a window, routed to the element they are for.
	/// </summary>
	/// <remarks>
	/// <para>Xamarin.Forms has no key events and only taps for the mouse, so the input comes from GTK itself: one
	/// set of event controllers on each toplevel, in the capture phase, sees every event before the widget it is
	/// for. The widget under the pointer (or with the focus) is mapped back to the element that owns it, and the
	/// event is raised the WPF way - the tunnelling <c>Preview</c> event from the window down, then the bubbling one
	/// back up, with one set of arguments. An event a handler marks handled stops there, as it does in WPF: the
	/// widget never sees it.</para>
	/// <para>The native widget of an element exists once Xamarin.Forms has created its renderer; <see cref="Track"/>
	/// waits for that, and records which element each widget belongs to.</para>
	/// </remarks>
	internal static class NativeInput
	{
		static readonly Dictionary<IntPtr, WeakReference<UIElement>> s_elements = new Dictionary<IntPtr, WeakReference<UIElement>>();
		static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UIElement, Gtk.Widget> s_widgets =
			new System.Runtime.CompilerServices.ConditionalWeakTable<UIElement, Gtk.Widget>();
		static readonly List<Gtk.Window> s_windows = new List<Gtk.Window>();

		/// <summary>The toplevel the last pointer event came from, and where in it.</summary>
		static Gtk.Window s_pointerWindow;
		static double s_pointerX, s_pointerY;

		static NativeInput() => Mouse.PositionProvider = relativeTo => PositionIn(s_pointerWindow, s_pointerX, s_pointerY, relativeTo);

		// ---- elements and their widgets ------------------------------------------------------------------------------

		internal static void Track(UIElement element, XF.View view)
		{
			view.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == "Renderer")
					Attach(element, view);
			};
			Attach(element, view);
		}

		static void Attach(UIElement element, XF.View view)
		{
			if (!(XF.Platform.GTK.Platform.GetRenderer(view)?.Container is Gtk.Widget widget))
				return;

			lock (s_elements)
				s_elements[widget.Handle] = new WeakReference<UIElement>(element);

			s_widgets.Remove(element);
			s_widgets.Add(element, widget);
			element.OnWidgetAttached(widget);
		}

		/// <summary>The GTK widget an element draws with, once it has one.</summary>
		internal static Gtk.Widget WidgetOf(UIElement element) =>
			element != null && s_widgets.TryGetValue(element, out var widget) ? widget : null;

		/// <summary>The element a widget belongs to: the nearest one up its widget tree.</summary>
		internal static UIElement ElementOf(Gtk.Widget widget)
		{
			lock (s_elements)
			{
				for (var w = widget; w != null; w = w.Parent)
				{
					if (s_elements.TryGetValue(w.Handle, out var weak) && weak.TryGetTarget(out var element))
						return element;
				}
			}

			return null;
		}

		// ---- cursors -------------------------------------------------------------------------------------------------

		internal static void ApplyCursor(FrameworkElement element, Gtk.Widget widget)
		{
			if (widget == null || Mouse.OverrideCursor != null)
				return;

			widget.Cursor = element.Cursor == null ? null : new Gdk.Cursor(element.Cursor.GtkName, null);
		}

		internal static void ApplyOverrideCursor(System.Windows.Input.Cursor cursor)
		{
			foreach (var window in s_windows.ToArray())
				window.Cursor = cursor == null ? null : new Gdk.Cursor(cursor.GtkName, null);
		}

		// ---- windows -------------------------------------------------------------------------------------------------

		/// <summary>Routes the input of <paramref name="toplevel"/> to the elements of <paramref name="window"/>.</summary>
		internal static void AttachWindow(WpfWindow window, Gtk.Window toplevel)
		{
			s_windows.Add(toplevel);

			var click = new Gtk.GestureClick { Button = 0, PropagationPhase = Gtk.PropagationPhase.Capture };
			click.Pressed += (o, a) =>
			{
				if (OnButton(window, toplevel, click.CurrentButton, a.NPress, a.X, a.Y, true, click.CurrentEventState))
					click.SetState(Gtk.EventSequenceState.Claimed);
			};
			click.Released += (o, a) =>
			{
				if (OnButton(window, toplevel, click.CurrentButton, a.NPress, a.X, a.Y, false, click.CurrentEventState))
					click.SetState(Gtk.EventSequenceState.Claimed);
			};
			toplevel.AddController(click);

			var motion = new Gtk.EventControllerMotion { PropagationPhase = Gtk.PropagationPhase.Capture };
			motion.Motion += (o, a) => OnMotion(window, toplevel, a.X, a.Y, motion.CurrentEventState);
			motion.Leave += (o, a) => SetDirectlyOver(null, _ => new Point());
			toplevel.AddController(motion);

			var scroll = new Gtk.EventControllerScroll(Gtk.EventControllerScrollFlags.Vertical) { PropagationPhase = Gtk.PropagationPhase.Capture };
			scroll.Scroll += (o, a) => a.RetVal = OnWheel(toplevel, a.Dy);
			toplevel.AddController(scroll);

			var keys = new Gtk.EventControllerKey { PropagationPhase = Gtk.PropagationPhase.Capture };
			keys.KeyPressed += (o, a) => a.RetVal = OnKey(window, a.Keyval, a.State, true);
			keys.KeyReleased += (o, a) => OnKey(window, a.Keyval, a.State, false);
			toplevel.AddController(keys);

			toplevel.AddNotification("focus-widget", (o, a) => OnFocusWidget(toplevel));
		}

		internal static void DetachWindow(Gtk.Window toplevel) => s_windows.Remove(toplevel);

		// ---- mouse ---------------------------------------------------------------------------------------------------

		static UIElement ElementAt(Gtk.Window toplevel, double x, double y) =>
			ElementOf(toplevel.Pick(x, y, Gtk.PickFlags.Default));

		internal static bool OnButton(WpfWindow window, Gtk.Window toplevel, uint button, int clickCount, double x, double y, bool pressed, Gdk.ModifierType state)
		{
			var handled = HandleButton(window, toplevel, button, clickCount, x, y, pressed, state);
			CommandManager.InvalidateRequerySuggested();
			return handled;
		}

		static bool HandleButton(WpfWindow window, Gtk.Window toplevel, uint button, int clickCount, double x, double y, bool pressed, Gdk.ModifierType state)
		{
			Remember(toplevel, x, y);
			UpdateModifiers(state);

			var mouseButton = button == 3 ? MouseButton.Right : button == 2 ? MouseButton.Middle : button == 8 ? MouseButton.XButton1 : button == 9 ? MouseButton.XButton2 : MouseButton.Left;
			var buttonState = pressed ? MouseButtonState.Pressed : MouseButtonState.Released;
			switch (mouseButton)
			{
				case MouseButton.Left:
					Mouse.LeftButton = buttonState;
					break;
				case MouseButton.Right:
					Mouse.RightButton = buttonState;
					break;
				case MouseButton.Middle:
					Mouse.MiddleButton = buttonState;
					break;
			}

			var target = ElementAt(toplevel, x, y) ?? window;
			var position = Snapshot(toplevel, x, y);
			var e = new MouseButtonEventArgs(position, mouseButton, buttonState, clickCount)
			{
				RoutedEvent = pressed ? UIElement.PreviewMouseDownEvent : UIElement.PreviewMouseUpEvent,
			};
			target.RaiseEvent(e);
			e.RoutedEvent = pressed ? UIElement.MouseDownEvent : UIElement.MouseUpEvent;
			target.RaiseEvent(e);

			// WPF opens an element's context menu when the right button is released over it.
			if (!pressed && mouseButton == MouseButton.Right && !e.Handled)
				e.Handled = Controls.ContextMenu.OpenFor(target);

			return e.Handled;
		}

		internal static void OnMotion(WpfWindow window, Gtk.Window toplevel, double x, double y, Gdk.ModifierType state)
		{
			Remember(toplevel, x, y);
			UpdateModifiers(state);

			var target = ElementAt(toplevel, x, y) ?? window;
			var position = Snapshot(toplevel, x, y);
			SetDirectlyOver(target, position);

			var e = new MouseEventArgs(position) { RoutedEvent = UIElement.PreviewMouseMoveEvent };
			target.RaiseEvent(e);
			e.RoutedEvent = UIElement.MouseMoveEvent;
			target.RaiseEvent(e);
		}

		static bool OnWheel(Gtk.Window toplevel, double dy)
		{
			var target = ElementAt(toplevel, s_pointerX, s_pointerY);
			if (target == null)
				return false;

			var e = new MouseWheelEventArgs(Snapshot(toplevel, s_pointerX, s_pointerY), (int)Math.Round(-dy * 120))
			{
				RoutedEvent = UIElement.PreviewMouseWheelEvent,
			};
			target.RaiseEvent(e);
			e.RoutedEvent = UIElement.MouseWheelEvent;
			target.RaiseEvent(e);
			return e.Handled;
		}

		/// <summary>Moves <see cref="Mouse.DirectlyOver"/>, raising MouseLeave and MouseEnter on the elements it leaves and enters.</summary>
		static void SetDirectlyOver(UIElement element, Func<IInputElement, Point> position)
		{
			var old = Mouse.DirectlyOver as UIElement;
			if (ReferenceEquals(old, element))
				return;

			var left = Ancestors(old).ToList();
			var entered = Ancestors(element).ToList();
			Mouse.DirectlyOver = element;

			foreach (var e in left.Except(entered))
				e.RaiseEvent(new MouseEventArgs(position) { RoutedEvent = UIElement.MouseLeaveEvent });
			foreach (var e in entered.Except(left).Reverse())
				e.RaiseEvent(new MouseEventArgs(position) { RoutedEvent = UIElement.MouseEnterEvent });
		}

		static IEnumerable<UIElement> Ancestors(UIElement element)
		{
			for (DependencyObject d = element; d != null; d = d.LogicalParent)
			{
				if (d is UIElement u)
					yield return u;
			}
		}

		static void Remember(Gtk.Window toplevel, double x, double y)
		{
			s_pointerWindow = toplevel;
			s_pointerX = x;
			s_pointerY = y;
		}

		/// <summary>Where the pointer was at this event, for <see cref="MouseEventArgs.GetPosition"/> after the fact.</summary>
		static Func<IInputElement, Point> Snapshot(Gtk.Window toplevel, double x, double y) =>
			relativeTo => PositionIn(toplevel, x, y, relativeTo);

		static Point PositionIn(Gtk.Window toplevel, double x, double y, IInputElement relativeTo)
		{
			if (toplevel == null)
				return new Point();

			var widget = relativeTo is WpfWindow ? null : WidgetOf(relativeTo as UIElement);
			if (widget == null || !widget.ComputeBounds(toplevel, out var bounds))
				return new Point(x, y);

			return new Point(x - bounds.X, y - bounds.Y);
		}

		// ---- keyboard ------------------------------------------------------------------------------------------------

		static void UpdateModifiers(Gdk.ModifierType state)
		{
			var m = ModifierKeys.None;
			if ((state & Gdk.ModifierType.ShiftMask) != 0)
				m |= ModifierKeys.Shift;
			if ((state & Gdk.ModifierType.ControlMask) != 0)
				m |= ModifierKeys.Control;
			if ((state & Gdk.ModifierType.AltMask) != 0)
				m |= ModifierKeys.Alt;
			if ((state & Gdk.ModifierType.SuperMask) != 0)
				m |= ModifierKeys.Windows;
			Keyboard.Modifiers = m;
			Keyboard.CapsLock = (state & Gdk.ModifierType.LockMask) != 0;
		}

		/// <returns>Whether a handler handled the key, which keeps it from the focused widget.</returns>
		/// <remarks>After any input WPF asks command sources whether their commands can run now; so does this.</remarks>
		internal static bool OnKey(WpfWindow window, uint keyval, Gdk.ModifierType state, bool down)
		{
			var handled = HandleKey(window, keyval, state, down);
			CommandManager.InvalidateRequerySuggested();
			return handled;
		}

		static bool HandleKey(WpfWindow window, uint keyval, Gdk.ModifierType state, bool down)
		{
			var key = GdkKeys.ToKey(keyval);

			// The state GTK reports is the state before this key: a modifier key changes it itself.
			UpdateModifiers(state);
			var modifier = GdkKeys.ModifierOf(key);
			if (modifier != ModifierKeys.None)
				Keyboard.Modifiers = down ? Keyboard.Modifiers | modifier : Keyboard.Modifiers & ~modifier;

			var repeat = down && Keyboard.IsKeyDown(key);
			Keyboard.SetKeyDown(key, down);

			// With Alt down (and for F10) WPF reports a system key: Key.System, the key itself in SystemKey.
			var system = (Keyboard.Modifiers & ModifierKeys.Alt) != 0 || key == Key.F10;
			var target = (Keyboard.FocusedElement as UIElement) is UIElement focused && WindowOf(focused) == window ? focused : window;

			var e = new KeyEventArgs(system ? Key.System : key, system ? key : Key.None, repeat, down)
			{
				RoutedEvent = down ? UIElement.PreviewKeyDownEvent : UIElement.PreviewKeyUpEvent,
			};
			target.RaiseEvent(e);
			e.RoutedEvent = down ? UIElement.KeyDownEvent : UIElement.KeyUpEvent;
			target.RaiseEvent(e);

			if (!down || e.Handled)
				return e.Handled;

			if (ExecuteInputBinding(target, e))
				return true;

			if (window.HandleDialogKey(key, target))
				return true;

			if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == 0)
			{
				var text = GdkKeys.TextOf(keyval, key);
				if (text != null)
				{
					var t = new TextCompositionEventArgs(text) { RoutedEvent = UIElement.PreviewTextInputEvent };
					target.RaiseEvent(t);
					t.RoutedEvent = UIElement.TextInputEvent;
					if (!t.Handled && !(target is System.Windows.Controls.Primitives.TextBoxBase))
						target.RaiseEvent(t);
					return t.Handled;
				}
			}

			return false;
		}

		/// <summary>A key binding of the focused element or an ancestor that matches: its command runs, and the key is handled.</summary>
		static bool ExecuteInputBinding(UIElement target, KeyEventArgs e)
		{
			for (DependencyObject d = target; d != null; d = d.LogicalParent)
			{
				if (!(d is UIElement element) || element.InputBindingsIfAny == null)
					continue;

				foreach (var binding in element.InputBindingsIfAny.ToArray())
				{
					if (binding.Command == null || binding.Gesture == null || !binding.Gesture.Matches(target, e))
						continue;

					var commandTarget = binding.CommandTarget ?? target;
					if (binding.Command is RoutedCommand routed)
					{
						if (routed.CanExecute(binding.CommandParameter, commandTarget))
							routed.Execute(binding.CommandParameter, commandTarget);
					}
					else if (binding.Command.CanExecute(binding.CommandParameter))
					{
						binding.Command.Execute(binding.CommandParameter);
					}

					return true;
				}
			}

			return false;
		}

		static WpfWindow WindowOf(DependencyObject element)
		{
			for (var d = element; d != null; d = d.LogicalParent)
			{
				if (d is WpfWindow w)
					return w;
			}

			return null;
		}

		// ---- focus ---------------------------------------------------------------------------------------------------

		static void OnFocusWidget(Gtk.Window toplevel) => SetFocus(ElementOf(toplevel.FocusWidget));

		/// <summary>Moves <see cref="Keyboard.FocusedElement"/>, raising the focus events on the elements it leaves and reaches.</summary>
		internal static void SetFocus(UIElement element)
		{
			var old = Keyboard.FocusedElement as UIElement;
			if (ReferenceEquals(old, element))
				return;

			// Tunnelling and before the move, as in WPF: a handler that marks it handled keeps focus where it is.
			// That is how a cancellable focus change is expressed, and what a VB6 Validate handler setting Cancel
			// relies on. The native widget is not pulled back, so GTK may still draw the focus ring on the one the
			// user clicked; the focused element WPF reports, and the events that follow from it, stay put.
			if (old != null)
			{
				var preview = new KeyboardFocusChangedEventArgs(old, element)
				{
					RoutedEvent = UIElement.PreviewLostKeyboardFocusEvent
				};
				old.RaiseEvent(preview);
				if (preview.Handled)
					return;
			}

			Keyboard.FocusedElement = element;
			if (old != null)
			{
				old.RaiseEvent(new KeyboardFocusChangedEventArgs(old, element) { RoutedEvent = UIElement.LostKeyboardFocusEvent });
				old.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, old));
			}

			if (element != null)
			{
				element.RaiseEvent(new KeyboardFocusChangedEventArgs(old, element) { RoutedEvent = UIElement.GotKeyboardFocusEvent });
				element.RaiseEvent(new RoutedEventArgs(UIElement.GotFocusEvent, element));
			}
		}
	}

	/// <summary>GDK key values to WPF keys, and the text a key types.</summary>
	internal static class GdkKeys
	{
		static readonly Dictionary<uint, Key> s_keys = Build();

		internal static Key ToKey(uint keyval)
		{
			if (keyval >= 'a' && keyval <= 'z')
				return Key.A + (int)(keyval - 'a');
			if (keyval >= 'A' && keyval <= 'Z')
				return Key.A + (int)(keyval - 'A');
			if (keyval >= '0' && keyval <= '9')
				return Key.D0 + (int)(keyval - '0');
			if (keyval >= 0xFFBE && keyval <= 0xFFD5)
				return Key.F1 + (int)(keyval - 0xFFBE);
			if (keyval >= 0xFFB0 && keyval <= 0xFFB9)
				return Key.NumPad0 + (int)(keyval - 0xFFB0);

			return s_keys.TryGetValue(keyval, out var key) ? key : Key.None;
		}

		internal static ModifierKeys ModifierOf(Key key)
		{
			switch (key)
			{
				case Key.LeftShift:
				case Key.RightShift:
					return ModifierKeys.Shift;
				case Key.LeftCtrl:
				case Key.RightCtrl:
					return ModifierKeys.Control;
				case Key.LeftAlt:
				case Key.RightAlt:
					return ModifierKeys.Alt;
				case Key.LWin:
				case Key.RWin:
					return ModifierKeys.Windows;
				default:
					return ModifierKeys.None;
			}
		}

		/// <summary>The text a key press types, as WPF's TextInput reports it (Enter is "\r", Backspace "\b"); null for none.</summary>
		internal static string TextOf(uint keyval, Key key)
		{
			switch (key)
			{
				case Key.Enter:
					return "\r";
				case Key.Back:
					return "\b";
				case Key.Escape:
					return "\u001b";
				case Key.Tab:
					return "\t";
			}

			var c = Gdk.Keyval.ToUnicode(keyval);
			return c == 0 || c < 0x20 || c == 0x7F ? null : char.ConvertFromUtf32((int)c);
		}

		static Dictionary<uint, Key> Build() => new Dictionary<uint, Key>
		{
			{ 0xFF0D, Key.Enter }, { 0xFF8D, Key.Enter }, { 0xFF1B, Key.Escape }, { 0xFF09, Key.Tab }, { 0xFE20, Key.Tab },
			{ 0xFF08, Key.Back }, { 0xFFFF, Key.Delete }, { 0xFF9F, Key.Delete }, { 0xFF63, Key.Insert }, { 0xFF9E, Key.Insert },
			{ 0xFF50, Key.Home }, { 0xFF95, Key.Home }, { 0xFF57, Key.End }, { 0xFF9C, Key.End },
			{ 0xFF51, Key.Left }, { 0xFF96, Key.Left }, { 0xFF52, Key.Up }, { 0xFF97, Key.Up },
			{ 0xFF53, Key.Right }, { 0xFF98, Key.Right }, { 0xFF54, Key.Down }, { 0xFF99, Key.Down },
			{ 0xFF55, Key.PageUp }, { 0xFF9A, Key.PageUp }, { 0xFF56, Key.PageDown }, { 0xFF9B, Key.PageDown },
			{ 0xFF58, Key.Clear }, { 0xFF9D, Key.Clear }, { 0xFF0B, Key.Clear }, { 0xFF0A, Key.LineFeed },
			{ 0xFF13, Key.Pause }, { 0xFF14, Key.Scroll }, { 0xFF15, Key.PrintScreen }, { 0xFF61, Key.PrintScreen },
			{ 0xFF67, Key.Apps }, { 0xFF7F, Key.NumLock }, { 0xFFE5, Key.CapsLock },
			{ 0xFF60, Key.Select }, { 0xFF62, Key.Execute }, { 0xFF6A, Key.Help }, { 0xFF69, Key.Cancel },
			{ 0xFFAA, Key.Multiply }, { 0xFFAB, Key.Add }, { 0xFFAC, Key.Separator }, { 0xFFAD, Key.Subtract },
			{ 0xFFAE, Key.Decimal }, { 0xFFAF, Key.Divide },
			{ 0xFFE1, Key.LeftShift }, { 0xFFE2, Key.RightShift }, { 0xFFE3, Key.LeftCtrl }, { 0xFFE4, Key.RightCtrl },
			{ 0xFFE9, Key.LeftAlt }, { 0xFFEA, Key.RightAlt }, { 0xFE03, Key.RightAlt },
			{ 0xFFEB, Key.LWin }, { 0xFFEC, Key.RWin },
			{ 0x20, Key.Space },
			{ ';', Key.Oem1 }, { ':', Key.Oem1 }, { '=', Key.OemPlus }, { '+', Key.OemPlus }, { ',', Key.OemComma }, { '<', Key.OemComma },
			{ '-', Key.OemMinus }, { '_', Key.OemMinus }, { '.', Key.OemPeriod }, { '>', Key.OemPeriod }, { '/', Key.Oem2 }, { '?', Key.Oem2 },
			{ '`', Key.Oem3 }, { '~', Key.Oem3 }, { '[', Key.Oem4 }, { '{', Key.Oem4 }, { '\\', Key.Oem5 }, { '|', Key.Oem5 },
			{ ']', Key.Oem6 }, { '}', Key.Oem6 }, { '\'', Key.Oem7 }, { '"', Key.Oem7 },
			// The shifted digits of a US keyboard type these; the key is still the digit.
			{ '!', Key.D1 }, { '@', Key.D2 }, { '#', Key.D3 }, { '$', Key.D4 }, { '%', Key.D5 },
			{ '^', Key.D6 }, { '&', Key.D7 }, { '*', Key.D8 }, { '(', Key.D9 }, { ')', Key.D0 },
		};
	}
}
