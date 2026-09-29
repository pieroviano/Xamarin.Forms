using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Wpf.UnitTests
{
	/// <summary>
	/// The facade's tests: every body runs on the GTK thread (GtkTestHost.Run), where the facade's elements live,
	/// with helpers to show a window, let GTK lay it out, and run the message loop until something happens.
	/// </summary>
	public abstract class WpfTestBase : GtkTestBase
	{
		/// <summary>A window around <paramref name="content"/>, shown and laid out.</summary>
		protected static Window Host(UIElement content, double width = 400, double height = 300)
		{
			var window = new Window { Content = content, Width = width, Height = height };
			return Shown(window);
		}

		protected static T Shown<T>(T window) where T : Window
		{
			window.Show();
			Pump(window);
			return window;
		}

		/// <summary>
		/// Runs GTK and the dispatcher until the window is laid out (and its Loaded has run). An exception a handler
		/// threw in the meantime is rethrown here, as the facade's own message loop would.
		/// </summary>
		protected static void Pump(Window window = null, int rounds = GtkTestHost.DefaultPumpRounds)
		{
			GtkTestHost.Pump(window?.NativeWindow, rounds);
			Dispatcher.CurrentDispatcher.ThrowPending();
		}

		/// <summary>Runs the message loop until <paramref name="condition"/> holds; false when it never did.</summary>
		protected static bool PumpUntil(Func<bool> condition, Window window = null, int timeoutMs = 3000)
		{
			var held = GtkTestHost.PumpUntil(condition, window?.NativeWindow, timeoutMs);
			Dispatcher.CurrentDispatcher.ThrowPending();
			return held;
		}

		/// <summary>Runs <paramref name="action"/> from the message loop, once whatever is running (a dialog) is up.</summary>
		protected static void Later(Action action) =>
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action);

		/// <summary>The window a dialog opened: the last one shown.</summary>
		protected static Window LastOpenWindow => Window.OpenWindows.LastOrDefault();
	}
}
