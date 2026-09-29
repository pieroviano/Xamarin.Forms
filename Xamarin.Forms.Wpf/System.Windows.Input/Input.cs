using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using XF = Xamarin.Forms;

namespace System.Windows.Input
{
	public enum MouseButton
	{
		Left,
		Middle,
		Right,
		XButton1,
		XButton2,
	}

	public enum MouseButtonState
	{
		Released,
		Pressed,
	}

	public class InputEventArgs : RoutedEventArgs
	{
		public InputEventArgs(InputDevice inputDevice, int timestamp)
		{
			Device = inputDevice;
			Timestamp = timestamp;
		}

		public InputDevice Device { get; }

		public int Timestamp { get; }
	}

	/// <summary>An input device; there is one keyboard and one mouse.</summary>
	public abstract class InputDevice
	{
	}

	public sealed class KeyboardDevice : InputDevice
	{
		internal static readonly KeyboardDevice Primary = new KeyboardDevice();

		KeyboardDevice()
		{
		}

		public ModifierKeys Modifiers => Keyboard.Modifiers;

		public IInputElement FocusedElement => Keyboard.FocusedElement;

		public bool IsKeyDown(Key key) => Keyboard.IsKeyDown(key);

		public bool IsKeyUp(Key key) => Keyboard.IsKeyUp(key);

		public bool IsKeyToggled(Key key) => Keyboard.IsKeyToggled(key);
	}

	public sealed class MouseDevice : InputDevice
	{
		internal static readonly MouseDevice Primary = new MouseDevice();

		MouseDevice()
		{
		}

		public MouseButtonState LeftButton => Mouse.LeftButton;

		public MouseButtonState RightButton => Mouse.RightButton;

		public MouseButtonState MiddleButton => Mouse.MiddleButton;

		public Point GetPosition(IInputElement relativeTo) => Mouse.GetPosition(relativeTo);
	}

	public class KeyboardEventArgs : InputEventArgs
	{
		public KeyboardEventArgs(KeyboardDevice keyboard, int timestamp) : base(keyboard, timestamp)
		{
		}

		public KeyboardDevice KeyboardDevice => (KeyboardDevice)Device;
	}

	public class KeyEventArgs : KeyboardEventArgs
	{
		public KeyEventArgs(KeyboardDevice keyboard, PresentationSource inputSource, int timestamp, Key key)
			: base(keyboard, timestamp)
		{
			Key = key;
		}

		internal KeyEventArgs(Key key, Key systemKey, bool isRepeat, bool isDown)
			: base(KeyboardDevice.Primary, Environment.TickCount)
		{
			Key = key;
			SystemKey = systemKey;
			IsRepeat = isRepeat;
			IsDown = isDown;
		}

		public Key Key { get; }

		public Key SystemKey { get; }

		public Key ImeProcessedKey => Key.None;

		public Key DeadCharProcessedKey => Key.None;

		public bool IsRepeat { get; }

		public bool IsDown { get; }

		public bool IsUp => !IsDown;

		public bool IsToggled => false;

		public KeyStates KeyStates => IsDown ? KeyStates.Down : KeyStates.None;

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is KeyEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void KeyEventHandler(object sender, KeyEventArgs e);

	public class TextCompositionEventArgs : InputEventArgs
	{
		internal TextCompositionEventArgs(string text) : base(KeyboardDevice.Primary, Environment.TickCount) => Text = text;

		public string Text { get; }

		public string SystemText => string.Empty;

		public string ControlText => string.Empty;

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is TextCompositionEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void TextCompositionEventHandler(object sender, TextCompositionEventArgs e);

	public class MouseEventArgs : InputEventArgs
	{
		readonly Func<IInputElement, Point> _position;

		public MouseEventArgs(MouseDevice mouse, int timestamp) : base(mouse, timestamp) => _position = Mouse.GetPosition;

		internal MouseEventArgs(Func<IInputElement, Point> position) : base(MouseDevice.Primary, Environment.TickCount) =>
			_position = position ?? Mouse.GetPosition;

		public MouseDevice MouseDevice => (MouseDevice)Device;

		public MouseButtonState LeftButton => Mouse.LeftButton;

		public MouseButtonState RightButton => Mouse.RightButton;

		public MouseButtonState MiddleButton => Mouse.MiddleButton;

		public MouseButtonState XButton1 => MouseButtonState.Released;

		public MouseButtonState XButton2 => MouseButtonState.Released;

		/// <summary>Where the pointer is, relative to <paramref name="relativeTo"/> (to its window when null).</summary>
		public Point GetPosition(IInputElement relativeTo) => _position(relativeTo);

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is MouseEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void MouseEventHandler(object sender, MouseEventArgs e);

	public class MouseButtonEventArgs : MouseEventArgs
	{
		public MouseButtonEventArgs(MouseDevice mouse, int timestamp, MouseButton button) : base(mouse, timestamp) =>
			ChangedButton = button;

		internal MouseButtonEventArgs(Func<IInputElement, Point> position, MouseButton button, MouseButtonState state, int clickCount)
			: base(position)
		{
			ChangedButton = button;
			ButtonState = state;
			ClickCount = clickCount;
		}

		public MouseButton ChangedButton { get; }

		public MouseButtonState ButtonState { get; }

		public int ClickCount { get; }

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is MouseButtonEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);

	public class MouseWheelEventArgs : MouseEventArgs
	{
		internal MouseWheelEventArgs(Func<IInputElement, Point> position, int delta) : base(position) => Delta = delta;

		public int Delta { get; }

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is MouseWheelEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void MouseWheelEventHandler(object sender, MouseWheelEventArgs e);

	public class KeyboardFocusChangedEventArgs : KeyboardEventArgs
	{
		internal KeyboardFocusChangedEventArgs(IInputElement oldFocus, IInputElement newFocus)
			: base(KeyboardDevice.Primary, Environment.TickCount)
		{
			OldFocus = oldFocus;
			NewFocus = newFocus;
		}

		public IInputElement OldFocus { get; }

		public IInputElement NewFocus { get; }

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is KeyboardFocusChangedEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void KeyboardFocusChangedEventHandler(object sender, KeyboardFocusChangedEventArgs e);

	/// <summary>The keyboard: modifier state and focus, as the input layer last saw them.</summary>
	public static class Keyboard
	{
		static readonly HashSet<Key> s_down = new HashSet<Key>();

		public static readonly RoutedEvent PreviewKeyDownEvent = UIElement.PreviewKeyDownEvent;
		public static readonly RoutedEvent KeyDownEvent = UIElement.KeyDownEvent;
		public static readonly RoutedEvent PreviewKeyUpEvent = UIElement.PreviewKeyUpEvent;
		public static readonly RoutedEvent KeyUpEvent = UIElement.KeyUpEvent;
		public static readonly RoutedEvent GotKeyboardFocusEvent = UIElement.GotKeyboardFocusEvent;
		public static readonly RoutedEvent LostKeyboardFocusEvent = UIElement.LostKeyboardFocusEvent;

		public static ModifierKeys Modifiers { get; internal set; }

		/// <summary>Caps Lock and Num Lock, as the last key event reported them.</summary>
		internal static bool CapsLock { get; set; }

		internal static bool NumLock { get; set; }

		public static IInputElement FocusedElement { get; internal set; }

		public static KeyboardDevice PrimaryDevice => KeyboardDevice.Primary;

		public static IInputElement Focus(IInputElement element)
		{
			element?.Focus();
			return FocusedElement;
		}

		public static void ClearFocus() => FocusedElement = null;

		public static bool IsKeyDown(Key key)
		{
			lock (s_down)
				return s_down.Contains(key);
		}

		public static bool IsKeyUp(Key key) => !IsKeyDown(key);

		public static bool IsKeyToggled(Key key) =>
			key == Key.CapsLock ? CapsLock : key == Key.NumLock && NumLock;

		public static KeyStates GetKeyStates(Key key) =>
			(IsKeyDown(key) ? KeyStates.Down : KeyStates.None) | (IsKeyToggled(key) ? KeyStates.Toggled : KeyStates.None);

		internal static void SetKeyDown(Key key, bool down)
		{
			lock (s_down)
			{
				if (down)
					s_down.Add(key);
				else
					s_down.Remove(key);
			}
		}
	}

	/// <summary>The mouse: button state, position and cursor, as the input layer last saw them.</summary>
	public static class Mouse
	{
		static Cursor s_overrideCursor;

		public static readonly RoutedEvent MouseDownEvent = UIElement.MouseDownEvent;
		public static readonly RoutedEvent MouseUpEvent = UIElement.MouseUpEvent;
		public static readonly RoutedEvent MouseMoveEvent = UIElement.MouseMoveEvent;

		public static MouseButtonState LeftButton { get; internal set; }

		public static MouseButtonState RightButton { get; internal set; }

		public static MouseButtonState MiddleButton { get; internal set; }

		public static IInputElement DirectlyOver { get; internal set; }

		public static IInputElement Captured { get; private set; }

		public static MouseDevice PrimaryDevice => MouseDevice.Primary;

		/// <summary>How the input layer answers <see cref="GetPosition"/>.</summary>
		internal static Func<IInputElement, Point> PositionProvider { get; set; }

		/// <summary>A cursor shown over every window of the application, whatever the element under the pointer asks for.</summary>
		public static Cursor OverrideCursor
		{
			get => s_overrideCursor;
			set
			{
				s_overrideCursor = value;
				NativeInput.ApplyOverrideCursor(value);
			}
		}

		public static Point GetPosition(IInputElement relativeTo) => PositionProvider?.Invoke(relativeTo) ?? new Point();

		public static bool Capture(IInputElement element)
		{
			Captured = element;
			return true;
		}

		public static bool Capture(IInputElement element, CaptureMode captureMode) => Capture(element);

		public static bool SetCursor(Cursor cursor)
		{
			NativeInput.ApplyOverrideCursor(cursor ?? s_overrideCursor);
			return true;
		}
	}

	public enum CaptureMode
	{
		None,
		Element,
		SubTree,
	}

	public enum CursorType
	{
		None,
		No,
		Arrow,
		AppStarting,
		Cross,
		Help,
		IBeam,
		SizeAll,
		SizeNESW,
		SizeNS,
		SizeNWSE,
		SizeWE,
		UpArrow,
		Wait,
		Hand,
		Pen,
		ScrollNS,
		ScrollWE,
		ScrollAll,
		ScrollN,
		ScrollS,
		ScrollW,
		ScrollE,
		ScrollNW,
		ScrollNE,
		ScrollSW,
		ScrollSE,
		ArrowCD,
	}

	[XF.TypeConverter(typeof(CursorConverter))]
	public sealed class Cursor : IDisposable
	{
		internal Cursor(CursorType type) => CursorType = type;

		public Cursor(string cursorFile) => CursorType = CursorType.Arrow;

		internal CursorType CursorType { get; }

		/// <summary>The CSS cursor name GTK draws this cursor by.</summary>
		internal string GtkName
		{
			get
			{
				switch (CursorType)
				{
					case CursorType.None:
						return "none";
					case CursorType.No:
						return "not-allowed";
					case CursorType.AppStarting:
						return "progress";
					case CursorType.Cross:
						return "crosshair";
					case CursorType.Help:
						return "help";
					case CursorType.IBeam:
						return "text";
					case CursorType.SizeAll:
						return "move";
					case CursorType.SizeNESW:
						return "nesw-resize";
					case CursorType.SizeNS:
					case CursorType.ScrollNS:
						return "ns-resize";
					case CursorType.SizeNWSE:
						return "nwse-resize";
					case CursorType.SizeWE:
					case CursorType.ScrollWE:
						return "ew-resize";
					case CursorType.Wait:
						return "wait";
					case CursorType.Hand:
						return "pointer";
					case CursorType.ScrollAll:
						return "all-scroll";
					default:
						return "default";
				}
			}
		}

		public void Dispose()
		{
		}

		public override string ToString() => CursorType.ToString();
	}

	public static class Cursors
	{
		public static Cursor None { get; } = new Cursor(CursorType.None);
		public static Cursor No { get; } = new Cursor(CursorType.No);
		public static Cursor Arrow { get; } = new Cursor(CursorType.Arrow);
		public static Cursor AppStarting { get; } = new Cursor(CursorType.AppStarting);
		public static Cursor Cross { get; } = new Cursor(CursorType.Cross);
		public static Cursor Help { get; } = new Cursor(CursorType.Help);
		public static Cursor IBeam { get; } = new Cursor(CursorType.IBeam);
		public static Cursor SizeAll { get; } = new Cursor(CursorType.SizeAll);
		public static Cursor SizeNESW { get; } = new Cursor(CursorType.SizeNESW);
		public static Cursor SizeNS { get; } = new Cursor(CursorType.SizeNS);
		public static Cursor SizeNWSE { get; } = new Cursor(CursorType.SizeNWSE);
		public static Cursor SizeWE { get; } = new Cursor(CursorType.SizeWE);
		public static Cursor UpArrow { get; } = new Cursor(CursorType.UpArrow);
		public static Cursor Wait { get; } = new Cursor(CursorType.Wait);
		public static Cursor Hand { get; } = new Cursor(CursorType.Hand);
		public static Cursor Pen { get; } = new Cursor(CursorType.Pen);
		public static Cursor ScrollNS { get; } = new Cursor(CursorType.ScrollNS);
		public static Cursor ScrollWE { get; } = new Cursor(CursorType.ScrollWE);
		public static Cursor ScrollAll { get; } = new Cursor(CursorType.ScrollAll);
		public static Cursor ArrowCD { get; } = new Cursor(CursorType.ArrowCD);
	}

	public sealed class CursorConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value)
		{
			var name = value?.Trim();
			var property = typeof(Cursors).GetProperty(name ?? "", Reflection.BindingFlags.Public | Reflection.BindingFlags.Static | Reflection.BindingFlags.IgnoreCase);
			return property?.GetValue(null, null) ?? throw new FormatException($"'{value}' is not a cursor.");
		}
	}

	// ---- commands -------------------------------------------------------------------------------------------------

	public class RoutedCommand : ICommand
	{
		public RoutedCommand() : this(string.Empty, typeof(RoutedCommand))
		{
		}

		public RoutedCommand(string name, Type ownerType) : this(name, ownerType, null)
		{
		}

		public RoutedCommand(string name, Type ownerType, InputGestureCollection inputGestures)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
			InputGestures = inputGestures ?? new InputGestureCollection();
		}

		public string Name { get; }

		public Type OwnerType { get; }

		public InputGestureCollection InputGestures { get; }

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		/// <summary>Runs the command on the first element from <paramref name="target"/> up that binds it.</summary>
		public void Execute(object parameter, IInputElement target)
		{
			foreach (var (element, binding) in Bindings(target))
			{
				var args = new ExecutedRoutedEventArgs(this, parameter);
				binding.RaiseExecuted(element, args);
				if (args.Handled)
					return;
			}
		}

		public bool CanExecute(object parameter, IInputElement target)
		{
			foreach (var (element, binding) in Bindings(target))
			{
				var args = new CanExecuteRoutedEventArgs(this, parameter);
				binding.RaiseCanExecute(element, args);
				if (args.Handled)
					return args.CanExecute;
			}

			return false;
		}

		bool ICommand.CanExecute(object parameter) => CanExecute(parameter, Keyboard.FocusedElement);

		void ICommand.Execute(object parameter) => Execute(parameter, Keyboard.FocusedElement);

		/// <summary>The command bindings from <paramref name="target"/> to the root, nearest first.</summary>
		IEnumerable<(UIElement Element, CommandBinding Binding)> Bindings(IInputElement target)
		{
			for (var d = target as DependencyObject; d != null; d = d.LogicalParent)
			{
				if (!(d is UIElement element) || element.CommandBindingsIfAny == null)
					continue;

				foreach (var binding in element.CommandBindingsIfAny.Where(b => b.Command == this).ToArray())
					yield return (element, binding);
			}
		}
	}

	public class RoutedUICommand : RoutedCommand
	{
		public RoutedUICommand()
		{
		}

		public RoutedUICommand(string text, string name, Type ownerType) : base(name, ownerType) => Text = text;

		public RoutedUICommand(string text, string name, Type ownerType, InputGestureCollection inputGestures)
			: base(name, ownerType, inputGestures) => Text = text;

		public string Text { get; set; }
	}

	public static class CommandManager
	{
		public static event EventHandler RequerySuggested;

		public static void InvalidateRequerySuggested() => RequerySuggested?.Invoke(null, EventArgs.Empty);
	}

	public sealed class ExecutedRoutedEventArgs : RoutedEventArgs
	{
		internal ExecutedRoutedEventArgs(ICommand command, object parameter)
		{
			Command = command;
			Parameter = parameter;
		}

		public ICommand Command { get; }

		public object Parameter { get; }
	}

	public delegate void ExecutedRoutedEventHandler(object sender, ExecutedRoutedEventArgs e);

	public sealed class CanExecuteRoutedEventArgs : RoutedEventArgs
	{
		internal CanExecuteRoutedEventArgs(ICommand command, object parameter)
		{
			Command = command;
			Parameter = parameter;
		}

		public ICommand Command { get; }

		public object Parameter { get; }

		public bool CanExecute { get; set; }

		public bool ContinueRouting { get; set; }
	}

	public delegate void CanExecuteRoutedEventHandler(object sender, CanExecuteRoutedEventArgs e);

	public class CommandBinding
	{
		public CommandBinding()
		{
		}

		public CommandBinding(ICommand command) => Command = command;

		public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed) : this(command) => Executed += executed;

		public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute)
			: this(command, executed) => CanExecute += canExecute;

		public ICommand Command { get; set; }

		public event ExecutedRoutedEventHandler Executed;

		public event ExecutedRoutedEventHandler PreviewExecuted;

		public event CanExecuteRoutedEventHandler CanExecute;

		public event CanExecuteRoutedEventHandler PreviewCanExecute;

		internal void RaiseExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (PreviewExecuted == null && Executed == null)
				return;

			PreviewExecuted?.Invoke(sender, e);
			if (!e.Handled)
			{
				Executed?.Invoke(sender, e);
				e.Handled = true;
			}
		}

		/// <summary>WPF: a binding with no CanExecute handler can execute; one with a handler answers for itself.</summary>
		internal void RaiseCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (PreviewCanExecute == null && CanExecute == null)
			{
				if (Executed == null && PreviewExecuted == null)
					return;

				e.CanExecute = true;
				e.Handled = true;
				return;
			}

			PreviewCanExecute?.Invoke(sender, e);
			if (!e.Handled)
				CanExecute?.Invoke(sender, e);
			e.Handled = !e.ContinueRouting;
		}
	}

	/// <summary>A binding added or removed may change what commands can run: command sources ask again, as WPF's do.</summary>
	public sealed class CommandBindingCollection : Collection<CommandBinding>
	{
		protected override void InsertItem(int index, CommandBinding item)
		{
			base.InsertItem(index, item);
			CommandManager.InvalidateRequerySuggested();
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			CommandManager.InvalidateRequerySuggested();
		}
	}

	public abstract class InputGesture
	{
		public abstract bool Matches(object targetElement, InputEventArgs inputEventArgs);
	}

	public sealed class InputGestureCollection : Collection<InputGesture>
	{
	}

	public class KeyGesture : InputGesture
	{
		public KeyGesture(Key key) : this(key, ModifierKeys.None)
		{
		}

		public KeyGesture(Key key, ModifierKeys modifiers) : this(key, modifiers, string.Empty)
		{
		}

		public KeyGesture(Key key, ModifierKeys modifiers, string displayString)
		{
			Key = key;
			Modifiers = modifiers;
			DisplayString = displayString ?? string.Empty;
		}

		public Key Key { get; }

		public ModifierKeys Modifiers { get; }

		public string DisplayString { get; }

		public override bool Matches(object targetElement, InputEventArgs inputEventArgs) =>
			inputEventArgs is KeyEventArgs e && (e.Key == Key.System ? e.SystemKey : e.Key) == Key && Keyboard.Modifiers == Modifiers;
	}

	public class InputBinding
	{
		public InputBinding()
		{
		}

		public InputBinding(ICommand command, InputGesture gesture)
		{
			Command = command;
			Gesture = gesture;
		}

		public ICommand Command { get; set; }

		public object CommandParameter { get; set; }

		public IInputElement CommandTarget { get; set; }

		public virtual InputGesture Gesture { get; set; }
	}

	public class KeyBinding : InputBinding
	{
		public KeyBinding()
		{
		}

		public KeyBinding(ICommand command, KeyGesture gesture) : base(command, gesture)
		{
		}

		public KeyBinding(ICommand command, Key key, ModifierKeys modifiers) : this(command, new KeyGesture(key, modifiers))
		{
		}

		public Key Key => (Gesture as KeyGesture)?.Key ?? Key.None;

		public ModifierKeys Modifiers => (Gesture as KeyGesture)?.Modifiers ?? ModifierKeys.None;
	}

	public sealed class InputBindingCollection : Collection<InputBinding>
	{
	}

	public static class FocusManager
	{
		public static IInputElement GetFocusedElement(DependencyObject element) => Keyboard.FocusedElement;

		public static void SetFocusedElement(DependencyObject element, IInputElement value) => value?.Focus();
	}
}

namespace System.Windows
{
	/// <summary>The window-system surface input comes from; there is no public way to create one.</summary>
	public abstract class PresentationSource
	{
	}
}
