using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xamarin.Forms.Platform.GTK;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows
{
	/// <summary>
	/// A top-level window: a GTK window of its own, hosting the window's content as a Xamarin.Forms page. Any number
	/// can be open; closing one ends the program only by the application's <see cref="ShutdownMode"/>.
	/// </summary>
	public class Window : ContentControl
	{
		/// <summary>GTK windows that have been closed, kept so their wrappers are never finalized after the widget is gone.</summary>
		static readonly List<Gtk.Window> s_closed = new List<Gtk.Window>();

		static readonly List<Window> s_open = new List<Window>();

		/// <summary>The windows shown and not closed, in the order they were first shown.</summary>
		internal static IReadOnlyList<Window> OpenWindows => s_open.ToArray();

		HostWindow _host;
		XF.ContentPage _page;
		DispatcherFrame _dialogFrame;
		bool? _dialogResult;
		bool _isDialog;
		bool _closing;
		bool _shown;
		Window _owner;
		readonly List<Window> _ownedWindows = new List<Window>();

		public Window()
		{
			GtkHost.EnsureInitialized();
			SetValue(FocusableProperty, false);
			Application.Current?.OnWindowCreated(this);
		}

		public static readonly XF.BindableProperty TitleProperty = Dp.Register<Window>(nameof(Title), typeof(string), string.Empty);
		public static readonly XF.BindableProperty IconProperty = Dp.Register<Window>(nameof(Icon), typeof(ImageSource), null);
		public static readonly XF.BindableProperty LeftProperty = Dp.Register<Window>(nameof(Left), typeof(double), double.NaN);
		public static readonly XF.BindableProperty TopProperty = Dp.Register<Window>(nameof(Top), typeof(double), double.NaN);
		public static readonly XF.BindableProperty WindowStartupLocationProperty = Dp.Register<Window>(nameof(WindowStartupLocation), typeof(WindowStartupLocation), WindowStartupLocation.Manual);
		public static readonly XF.BindableProperty SizeToContentProperty = Dp.Register<Window>(nameof(SizeToContent), typeof(SizeToContent), SizeToContent.Manual);
		public static readonly XF.BindableProperty ResizeModeProperty = Dp.Register<Window>(nameof(ResizeMode), typeof(ResizeMode), ResizeMode.CanResize);
		public static readonly XF.BindableProperty WindowStyleProperty = Dp.Register<Window>(nameof(WindowStyle), typeof(WindowStyle), WindowStyle.SingleBorderWindow);
		public static readonly XF.BindableProperty WindowStateProperty = Dp.Register<Window>(nameof(WindowState), typeof(WindowState), WindowState.Normal);
		public static readonly XF.BindableProperty ShowInTaskbarProperty = Dp.Register<Window>(nameof(ShowInTaskbar), typeof(bool), true);
		public static readonly XF.BindableProperty TopmostProperty = Dp.Register<Window>(nameof(Topmost), typeof(bool), false);
		public static readonly XF.BindableProperty ShowActivatedProperty = Dp.Register<Window>(nameof(ShowActivated), typeof(bool), true);
		public static readonly XF.BindableProperty AllowsTransparencyProperty = Dp.Register<Window>(nameof(AllowsTransparency), typeof(bool), false);

		public string Title
		{
			get => Get<string>(TitleProperty);
			set => SetValue(TitleProperty, value ?? string.Empty);
		}

		public ImageSource Icon
		{
			get => Get<ImageSource>(IconProperty);
			set => SetValue(IconProperty, value);
		}

		/// <summary>The requested position. GTK 4 leaves window placement to the window manager, so it is kept but not applied.</summary>
		public double Left
		{
			get => Get<double>(LeftProperty);
			set => SetValue(LeftProperty, value);
		}

		public double Top
		{
			get => Get<double>(TopProperty);
			set => SetValue(TopProperty, value);
		}

		public WindowStartupLocation WindowStartupLocation
		{
			get => Get<WindowStartupLocation>(WindowStartupLocationProperty);
			set => SetValue(WindowStartupLocationProperty, value);
		}

		public SizeToContent SizeToContent
		{
			get => Get<SizeToContent>(SizeToContentProperty);
			set => SetValue(SizeToContentProperty, value);
		}

		public ResizeMode ResizeMode
		{
			get => Get<ResizeMode>(ResizeModeProperty);
			set => SetValue(ResizeModeProperty, value);
		}

		public WindowStyle WindowStyle
		{
			get => Get<WindowStyle>(WindowStyleProperty);
			set => SetValue(WindowStyleProperty, value);
		}

		public WindowState WindowState
		{
			get => Get<WindowState>(WindowStateProperty);
			set => SetValue(WindowStateProperty, value);
		}

		public bool ShowInTaskbar
		{
			get => Get<bool>(ShowInTaskbarProperty);
			set => SetValue(ShowInTaskbarProperty, value);
		}

		public bool Topmost
		{
			get => Get<bool>(TopmostProperty);
			set => SetValue(TopmostProperty, value);
		}

		public bool ShowActivated
		{
			get => Get<bool>(ShowActivatedProperty);
			set => SetValue(ShowActivatedProperty, value);
		}

		public bool AllowsTransparency
		{
			get => Get<bool>(AllowsTransparencyProperty);
			set => SetValue(AllowsTransparencyProperty, value);
		}

		/// <summary>The window this one stays above, and closes with.</summary>
		public Window Owner
		{
			get => _owner;
			set
			{
				if (value == this)
					throw new ArgumentException("Cannot set Owner property to itself.");

				_owner?._ownedWindows.Remove(this);
				_owner = value;
				_owner?._ownedWindows.Add(this);
				if (_host != null)
					_host.TransientFor = _owner?._host;
			}
		}

		public WindowCollection OwnedWindows => new WindowCollection(_ownedWindows);

		/// <summary>The result <see cref="ShowDialog"/> returns; setting it on a dialog closes the dialog.</summary>
		public bool? DialogResult
		{
			get => _dialogResult;
			set
			{
				if (!_isDialog)
					throw new InvalidOperationException("DialogResult can be set only after Window is created and shown as dialog.");

				if (_dialogResult == value)
					return;

				_dialogResult = value;
				if (value.HasValue && !_closing)
					Close();
			}
		}

		public bool IsActive => _host != null && _host.IsActive;

		/// <summary>Whether <see cref="Show"/> has put the window on screen (and it has not closed since).</summary>
		internal bool IsShown => _host != null && !IsClosed && _host.Visible;

		internal bool IsClosed { get; private set; }

		/// <summary>Whether the window is showing through <see cref="ShowDialog"/>.</summary>
		internal bool IsDialog => _isDialog;

		/// <summary>The GTK window, once shown.</summary>
		internal Gtk.Window NativeWindow => _host;

		public Rect RestoreBounds => new Rect(double.IsNaN(Left) ? 0 : Left, double.IsNaN(Top) ? 0 : Top, ActualWidth, ActualHeight);

		public event EventHandler Activated;

		public event EventHandler Deactivated;

		public event CancelEventHandler Closing;

		public event EventHandler Closed;

		public event EventHandler StateChanged;

		public event EventHandler LocationChanged;

		public event EventHandler ContentRendered;

		public event EventHandler SourceInitialized;

		protected virtual void OnActivated(EventArgs e) => Activated?.Invoke(this, e);

		protected virtual void OnDeactivated(EventArgs e) => Deactivated?.Invoke(this, e);

		protected virtual void OnClosing(CancelEventArgs e) => Closing?.Invoke(this, e);

		protected virtual void OnClosed(EventArgs e) => Closed?.Invoke(this, e);

		protected virtual void OnStateChanged(EventArgs e) => StateChanged?.Invoke(this, e);

		protected virtual void OnLocationChanged(EventArgs e) => LocationChanged?.Invoke(this, e);

		protected virtual void OnContentRendered(EventArgs e) => ContentRendered?.Invoke(this, e);

		protected virtual void OnSourceInitialized(EventArgs e) => SourceInitialized?.Invoke(this, e);

		public static Window GetWindow(DependencyObject dependencyObject)
		{
			for (var d = dependencyObject ?? throw new ArgumentNullException(nameof(dependencyObject)); d != null; d = d.LogicalParent)
			{
				if (d is Window w)
					return w;
			}

			return null;
		}

		// ---- showing and closing -------------------------------------------------------------------------------------

		public void Show()
		{
			VerifyAccess();
			if (IsClosed)
				throw new InvalidOperationException("Cannot set Visibility or call Show, ShowDialog, or WindowInteropHelper.EnsureHandle after a Window has closed.");

			// ShowDialog makes the host first (to make it modal), so the first show is not "no host yet".
			var first = !_shown;
			_shown = true;
			if (_host == null)
				CreateHost();
			if (first)
				s_open.Add(this);

			SetValue(VisibilityProperty, Visibility.Visible);
			_host.Visible = true;
			_host.Present();
			ApplyWindowState();

			if (first)
			{
				OnSourceInitialized(EventArgs.Empty);

				// As WPF does: Loaded once the window is up, from the message loop rather than inside Show.
				Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
				{
					if (IsClosed)
						return;

					BroadcastLoaded();
					OnContentRendered(EventArgs.Empty);
				}));
			}
		}

		/// <summary>Shows the window and returns when it closes, keeping the rest of the program's windows out of reach.</summary>
		public bool? ShowDialog()
		{
			VerifyAccess();
			if (IsClosed)
				throw new InvalidOperationException("Cannot set Visibility or call Show, ShowDialog, or WindowInteropHelper.EnsureHandle after a Window has closed.");
			if (IsShown)
				throw new InvalidOperationException("ShowDialog can be called only on hidden windows.");

			_isDialog = true;
			_dialogResult = null;
			try
			{
				if (_host == null)
					CreateHost();

				_host.Modal = true;
				if (_owner == null)
				{
					var active = Application.Current?.Windows.Cast<Window>().FirstOrDefault(w => w != this && w.IsActive)
						?? Application.Current?.MainWindow;
					if (active != null && active != this && active._host != null)
						_host.TransientFor = active._host;
				}

				Show();

				_dialogFrame = new DispatcherFrame();
				Dispatcher.PushFrame(_dialogFrame);
			}
			finally
			{
				_dialogFrame = null;
				_isDialog = false;
				if (_host != null)
					_host.Modal = false;
			}

			return _dialogResult;
		}

		public void Hide()
		{
			VerifyAccess();
			SetValue(VisibilityProperty, Visibility.Hidden);
			if (_host != null)
				_host.Visible = false;

			// A hidden dialog is no longer in the way: ShowDialog returns.
			if (_dialogFrame != null)
				_dialogFrame.Continue = false;
		}

		public bool Activate()
		{
			if (!IsShown)
				return false;

			_host.Present();
			return true;
		}

		public void DragMove()
		{
		}

		/// <summary>Closes the window, unless a <see cref="Closing"/> handler cancels.</summary>
		public void Close()
		{
			VerifyAccess();
			if (IsClosed || _closing)
				return;

			var e = new CancelEventArgs();
			_closing = true;
			try
			{
				OnClosing(e);
			}
			finally
			{
				_closing = false;
			}

			if (e.Cancel)
			{
				// A dialog whose closing was cancelled keeps its result pending, as WPF does.
				if (_isDialog)
					_dialogResult = null;
				return;
			}

			IsClosed = true;
			s_open.Remove(this);
			foreach (var owned in _ownedWindows.ToArray())
				owned.Close();

			BroadcastUnloaded();
			if (_host != null)
			{
				NativeInput.DetachWindow(_host);
				_host.Visible = false;
				s_closed.Add(_host);
			}

			if (Keyboard.FocusedElement is DependencyObject focused && GetWindow(focused) == this)
				Keyboard.FocusedElement = null;

			Owner = null;
			OnClosed(EventArgs.Empty);
			Application.Current?.OnWindowClosed(this);

			if (_dialogFrame != null)
				_dialogFrame.Continue = false;
		}

		/// <summary>The GTK window, sized and styled by this window's properties, with the content as its page.</summary>
		void CreateHost()
		{
			_page = new XF.ContentPage { Content = NativeView, Padding = 0 };
			_host = new HostWindow(this) { QuitOnClose = false };
			_host.LoadPage(_page);
			_host.TransientFor = _owner?._host;
			NativeInput.AttachWindow(this, _host);
			ApplyHostProperties();

			_host.AddNotification("is-active", (o, a) =>
			{
				if (_host.IsActive)
				{
					Application.Current?.OnWindowActivated(this);
					OnActivated(EventArgs.Empty);
				}
				else
				{
					OnDeactivated(EventArgs.Empty);
				}
			});
			_host.AddNotification("maximized", (o, a) =>
			{
				var state = _host.Maximized ? WindowState.Maximized : WindowState.Normal;
				if (state != WindowState)
				{
					SetValue(WindowStateProperty, state);
					OnStateChanged(EventArgs.Empty);
				}
			});
		}

		void ApplyHostProperties()
		{
			if (_host == null)
				return;

			_host.Title = Title;
			_host.Decorated = WindowStyle != WindowStyle.None;
			_host.Resizable = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;
			_page.BackgroundColor = Background?.ToFormsColor() ?? XF.Color.Default;

			// FormsWindow asks for 400x400 at least; a WPF window is as small as its content lets it be.
			_host.SetSizeRequest(MinWidth > 0 ? (int)MinWidth : -1, MinHeight > 0 ? (int)MinHeight : -1);
			var (width, height) = InitialSize();
			_host.SetDefaultSize(width, height);
		}

		/// <summary>The size to open at: the content's when the window sizes to it, else Width/Height, else the content's.</summary>
		internal (int Width, int Height) InitialSize()
		{
			var request = NativeView.Measure(double.PositiveInfinity, double.PositiveInfinity, XF.MeasureFlags.IncludeMargins).Request;
			var sizeToContent = SizeToContent;
			var width = sizeToContent == SizeToContent.Width || sizeToContent == SizeToContent.WidthAndHeight || double.IsNaN(Width) ? request.Width : Width;
			var height = sizeToContent == SizeToContent.Height || sizeToContent == SizeToContent.WidthAndHeight || double.IsNaN(Height) ? request.Height : Height;
			return ((int)Math.Ceiling(Math.Max(1, width)), (int)Math.Ceiling(Math.Max(1, height)));
		}

		void ApplyWindowState()
		{
			if (_host == null)
				return;

			switch (WindowState)
			{
				case WindowState.Maximized:
					_host.Maximize();
					break;
				case WindowState.Minimized:
					_host.Minimize();
					break;
				default:
					if (_host.Maximized)
						_host.Unmaximize();
					break;
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			var p = e.Property.Bindable;
			if (p == VisibilityProperty)
			{
				// A window's visibility is whether it is shown, not how its content draws.
				if (_host != null && !_closing)
				{
					if (Visibility == Visibility.Visible && !_host.Visible && !IsClosed)
						Show();
					else if (Visibility != Visibility.Visible && _host.Visible)
						Hide();
				}

				return;
			}

			base.OnPropertyChanged(e);

			if (_host == null)
				return;

			if (p == TitleProperty || p == WindowStyleProperty || p == ResizeModeProperty || p == BackgroundProperty)
				ApplyHostProperties();
			else if (p == WindowStateProperty)
				ApplyWindowState();
			else if (p == WidthProperty || p == HeightProperty)
				_host.SetDefaultSize(InitialSize().Width, InitialSize().Height);
		}

		internal override void SyncNative()
		{
			base.SyncNative();

			// The window's own size is the GTK window's; its content fills it.
			NativeView.WidthRequest = -1;
			NativeView.HeightRequest = -1;
			NativeView.HorizontalOptions = XF.LayoutOptions.Fill;
			NativeView.VerticalOptions = XF.LayoutOptions.Fill;
			NativeView.IsVisible = true;
			NativeView.Opacity = Opacity;
		}

		internal override void ApplyBackground()
		{
			base.ApplyBackground();
			if (_page != null)
				_page.BackgroundColor = Background?.ToFormsColor() ?? XF.Color.Default;
		}

		// ---- keys the window handles ---------------------------------------------------------------------------------

		/// <summary>
		/// Enter clicks the default button and Escape the cancel button (unless the focused element takes the key
		/// itself); Alt with a letter clicks or focuses the element with that access key. True when it acted.
		/// </summary>
		internal bool HandleDialogKey(Key key, UIElement focused)
		{
			if (key == Key.Enter && !(focused is ButtonBase) && !(focused is TextBoxBase t && t.AcceptsReturn))
				return Click(LogicalDescendants().OfType<Button>().FirstOrDefault(b => b.IsDefault && b.IsEnabled && b.IsVisible));

			if (key == Key.Escape)
				return Click(LogicalDescendants().OfType<Button>().FirstOrDefault(b => b.IsCancel && b.IsEnabled && b.IsVisible));

			if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && key >= Key.A && key <= Key.Z || key >= Key.D0 && key <= Key.D9 && (Keyboard.Modifiers & ModifierKeys.Alt) != 0)
			{
				var letter = key >= Key.A && key <= Key.Z ? (char)('A' + (key - Key.A)) : (char)('0' + (key - Key.D0));
				return AccessKey(letter);
			}

			return false;
		}

		static bool Click(ButtonBase button)
		{
			if (button == null)
				return false;

			button.PerformClick();
			return true;
		}

		/// <summary>The element whose content has <paramref name="letter"/> as its access key: a button clicks, a label focuses its target.</summary>
		bool AccessKey(char letter)
		{
			foreach (var element in LogicalDescendants())
			{
				if (!element.IsEnabled || !element.IsVisible)
					continue;

				switch (element)
				{
					case ButtonBase button when button.Content is string s && NativeText.AccessKeyOf(s) == letter:
						button.Focus();
						button.PerformClick();
						return true;
					case Label label when label.Content is string s && NativeText.AccessKeyOf(s) == letter:
						label.Target?.Focus();
						return true;
					case MenuItem item when item.Header is string s && NativeText.AccessKeyOf(s) == letter && item.LogicalParent is Menu:
						item.OpenSubmenu();
						return true;
				}
			}

			return false;
		}

		IEnumerable<UIElement> LogicalDescendants()
		{
			var stack = new Stack<DependencyObject>();
			stack.Push(this);
			while (stack.Count > 0)
			{
				var d = stack.Pop();
				if (d is UIElement u && d != this)
					yield return u;
				foreach (var child in d.LogicalChildrenCore.OfType<DependencyObject>().Reverse())
					stack.Push(child);
			}
		}

		/// <summary>The GTK window: a window-manager close request is a WPF <see cref="Close"/>, which a handler may cancel.</summary>
		sealed class HostWindow : FormsWindow
		{
			readonly Window _owner;

			public HostWindow(Window owner) => _owner = owner;

			protected override bool OnCloseRequest()
			{
				_owner.Close();
				return true;
			}
		}
	}

	/// <summary>A snapshot of windows, as WPF's collection is.</summary>
	public sealed class WindowCollection : ICollection
	{
		readonly List<Window> _windows;

		public WindowCollection() => _windows = new List<Window>();

		internal WindowCollection(IEnumerable<Window> windows) => _windows = new List<Window>(windows);

		public Window this[int index] => _windows[index];

		public int Count => _windows.Count;

		public bool IsSynchronized => false;

		public object SyncRoot => this;

		public void CopyTo(Window[] array, int index) => _windows.CopyTo(array, index);

		void ICollection.CopyTo(Array array, int index) => ((ICollection)_windows).CopyTo(array, index);

		public IEnumerator GetEnumerator() => _windows.ToArray().GetEnumerator();

		internal void Add(Window window)
		{
			if (!_windows.Contains(window))
				_windows.Add(window);
		}

		internal void Remove(Window window) => _windows.Remove(window);
	}

	/// <summary>
	/// The application: its windows, its resources and its message loop. Creating one brings GTK and Xamarin.Forms up;
	/// <see cref="Run()"/> runs until <see cref="Shutdown()"/> - which, by default, is when the last window closes.
	/// </summary>
	public class Application
	{
		static Application s_current;

		ResourceDictionary _resources;
		DispatcherFrame _frame;
		int _exitCode;
		bool _running;
		bool _shutdownRequested;

		public Application()
		{
			lock (typeof(Application))
			{
				if (s_current != null)
					throw new InvalidOperationException("Cannot create more than one System.Windows.Application instance in the same AppDomain.");
				s_current = this;
			}

			GtkHost.EnsureInitialized();
			Dispatcher = Dispatcher.CurrentDispatcher;
		}

		public static Application Current => s_current;

		public Dispatcher Dispatcher { get; }

		public WindowCollection Windows { get; } = new WindowCollection();

		/// <summary>The main window: set explicitly, else the first window the application created.</summary>
		public Window MainWindow { get; set; }

		public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnLastWindowClose;

		public ResourceDictionary Resources
		{
			get => _resources ?? (_resources = new ResourceDictionary());
			set => _resources = value;
		}

		public IDictionary Properties { get; } = new Hashtable();

		public Uri StartupUri { get; set; }

		public event StartupEventHandler Startup;

		public event ExitEventHandler Exit;

		public event EventHandler Activated;

		public event EventHandler Deactivated;

		public event DispatcherUnhandledExceptionEventHandler DispatcherUnhandledException;

		public event SessionEndingCancelEventHandler SessionEnding;

		public int Run() => Run(null);

		/// <summary>Shows <paramref name="window"/> (if any) and runs the message loop until the application shuts down.</summary>
		public int Run(Window window)
		{
			Dispatcher.VerifyAccess();
			if (_running)
				throw new InvalidOperationException("Cannot call Run more than once.");

			_running = true;
			OnStartup(new StartupEventArgs(Environment.GetCommandLineArgs().Skip(1).ToArray()));

			if (window != null)
			{
				if (MainWindow == null)
					MainWindow = window;
				window.Show();
			}

			if (!_shutdownRequested)
			{
				_frame = new DispatcherFrame();
				Dispatcher.PushFrame(_frame);
				_frame = null;
			}

			OnExit(new ExitEventArgs(_exitCode));
			return _exitCode;
		}

		public void Shutdown() => Shutdown(0);

		/// <summary>Ends the message loop <see cref="Run()"/> is in; windows still open are closed first.</summary>
		public void Shutdown(int exitCode)
		{
			if (_shutdownRequested)
				return;

			_shutdownRequested = true;
			_exitCode = exitCode;
			foreach (Window window in Windows)
				window.Close();

			if (_frame != null)
				_frame.Continue = false;
			Dispatcher.ExitAllFrames();
		}

		public object FindResource(object resourceKey) =>
			TryFindResource(resourceKey) ?? throw new ResourceReferenceKeyNotFoundException($"'{resourceKey}' resource not found.", resourceKey);

		public object TryFindResource(object resourceKey) => FindApplicationResource(resourceKey);

		protected virtual void OnStartup(StartupEventArgs e) => Startup?.Invoke(this, e);

		protected virtual void OnExit(ExitEventArgs e) => Exit?.Invoke(this, e);

		protected virtual void OnActivated(EventArgs e) => Activated?.Invoke(this, e);

		protected virtual void OnDeactivated(EventArgs e) => Deactivated?.Invoke(this, e);

		protected virtual void OnSessionEnding(SessionEndingCancelEventArgs e) => SessionEnding?.Invoke(this, e);

		/// <summary>The application's resources, then the system ones (colors).</summary>
		internal static object FindApplicationResource(object key)
		{
			if (s_current?._resources != null && s_current._resources.TryGet(key, out var value))
				return value;

			var name = key is ResourceKey rk ? rk.ToString() : key as string;
			return name != null && GtkHost.FormsApplication != null && GtkHost.FormsApplication.Resources.TryGetValue(name, out value) ? value : null;
		}

		/// <summary>WPF lists a window from the moment it is created, not from when it is shown.</summary>
		internal void OnWindowCreated(Window window)
		{
			if (!Dispatcher.CheckAccess())
				return;

			Windows.Add(window);
			if (MainWindow == null)
				MainWindow = window;
		}

		internal void OnWindowActivated(Window window) => OnActivated(EventArgs.Empty);

		internal void OnWindowClosed(Window window)
		{
			Windows.Remove(window);
			var wasMain = MainWindow == window;
			if (wasMain)
				MainWindow = null;

			if (_shutdownRequested)
				return;

			if (ShutdownMode == ShutdownMode.OnLastWindowClose && Windows.Count == 0
				|| ShutdownMode == ShutdownMode.OnMainWindowClose && wasMain)
			{
				Shutdown();
			}
		}

		/// <summary>Raised by nothing here: an exception in a handler propagates, as it would with no handler in WPF.</summary>
		internal bool RaiseDispatcherUnhandledException(Exception e)
		{
			var args = new DispatcherUnhandledExceptionEventArgs(Dispatcher, e);
			DispatcherUnhandledException?.Invoke(this, args);
			return args.Handled;
		}
	}

	public class StartupEventArgs : EventArgs
	{
		internal StartupEventArgs(string[] args) => Args = args;

		public string[] Args { get; }
	}

	public delegate void StartupEventHandler(object sender, StartupEventArgs e);

	public class ExitEventArgs : EventArgs
	{
		internal ExitEventArgs(int exitCode) => ApplicationExitCode = exitCode;

		public int ApplicationExitCode { get; set; }
	}

	public delegate void ExitEventHandler(object sender, ExitEventArgs e);

	public enum ReasonSessionEnding : byte
	{
		Logoff,
		Shutdown,
	}

	public class SessionEndingCancelEventArgs : CancelEventArgs
	{
		internal SessionEndingCancelEventArgs(ReasonSessionEnding reason) => ReasonSessionEnding = reason;

		public ReasonSessionEnding ReasonSessionEnding { get; }
	}

	public delegate void SessionEndingCancelEventHandler(object sender, SessionEndingCancelEventArgs e);
}

namespace System.Windows.Threading
{
	public sealed class DispatcherUnhandledExceptionEventArgs : EventArgs
	{
		internal DispatcherUnhandledExceptionEventArgs(Dispatcher dispatcher, Exception exception)
		{
			Dispatcher = dispatcher;
			Exception = exception;
		}

		public Dispatcher Dispatcher { get; }

		public Exception Exception { get; }

		public bool Handled { get; set; }
	}

	public delegate void DispatcherUnhandledExceptionEventHandler(object sender, DispatcherUnhandledExceptionEventArgs e);
}
