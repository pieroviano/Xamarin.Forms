using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;
using Xunit;

/// <summary>
/// Assembly-wide GTK + Forms bootstrap and the layout pump. Deliberately in the global namespace
/// so every fixture can reach it without a using.
///
/// xUnit has no <c>[SetUpFixture]</c>/<c>[OneTimeSetUp]</c>, so the bootstrap is a latched static
/// initializer instead. <see cref="GtkTestBase"/> calls it from its constructor, which xUnit runs
/// before every test; the latch makes all but the first call free. That is on purpose rather than
/// an assembly fixture: it keeps the verdict on an unusable environment ("no DISPLAY", "no native
/// GTK") attached to each test rather than to fixture construction, where xUnit reports it once
/// and swallows the rest.
/// </summary>
public static class GtkTestHost
{
	/// <summary>
	/// The number of Pump rounds a layout assertion needs. Geometry is pushed to GTK from a
	/// GLib.Idle callback (plan M3 root cause 2 - "never mutate geometry inside a size-allocate"),
	/// so a single drain of the event queue is one round short of the answer.
	/// </summary>
	public const int DefaultPumpRounds = 6;

	/// <summary>
	/// Set this in an environment that is SUPPOSED to be able to run GTK - CI does, in
	/// linux-gtk.yml - and an unusable environment becomes a hard failure instead of a skip.
	/// Without it a broken xvfb, or a GTK runtime that silently stopped being installed, would
	/// turn the whole suite green-by-absence, which is exactly the outcome the comments in that
	/// workflow forbid.
	/// </summary>
	const string RequiredVariable = "XF_GTK_REQUIRE_NATIVE";

	static bool s_initialized;
	static string s_unavailable;
	static readonly object s_gate = new object();

	public static void EnsureInitialized()
	{
		lock (s_gate)
		{
			if (s_initialized)
				return;

			// Already ruled the environment out once - don't pay for 169 more DllNotFoundExceptions.
			if (s_unavailable != null)
				Unavailable(s_unavailable);

			var reason = DisplayUnavailableReason();

			if (reason == null)
			{
				try
				{
					Gtk.Application.Init();
					Forms.Init();

					s_initialized = true;
					return;
				}
				catch (DllNotFoundException e)
				{
					reason = NoNativeRuntime(e.Message);
				}
				catch (TypeInitializationException e) when (e.InnerException is DllNotFoundException inner)
				{
					reason = NoNativeRuntime(inner.Message);
				}
			}

			s_unavailable = reason;
			Unavailable(reason);
		}
	}

	/// <summary>
	/// Why GTK cannot be brought up here, or null if it can.
	/// </summary>
	/// <remarks>
	/// DISPLAY is an X11 concept. GTK3 on Windows draws through the Win32 GDK backend and has no
	/// DISPLAY at all, so gating on it there rejected an environment that was never asked for one;
	/// the honest test on Windows is whether the native runtime loads, which is left to
	/// <see cref="Gtk.Application.Init"/> to answer.
	/// </remarks>
	static string DisplayUnavailableReason()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return null;

		// Wayland sessions have no DISPLAY either unless Xwayland is up.
		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ||
			!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
			return null;

		return "No DISPLAY. These are real GTK widget tests and need an X server: " +
			"run them as `xvfb-run -a dotnet test Xamarin.Forms.Platform.GTK.UnitTests`.";
	}

	static string NoNativeRuntime(string detail) =>
		"The native GTK 3 runtime is not available, so there are no widgets to test: " + detail +
		". On Linux install libgtk-3-0; on Windows these tests need a GTK 3 runtime on PATH " +
		"(the GtkSharp package ships managed bindings only).";

	/// <summary>
	/// Skips, so that an environment which cannot host GTK reports as such rather than as 169
	/// identical assertion failures - unless <see cref="RequiredVariable"/> says the environment
	/// was meant to be able to, in which case it fails.
	/// </summary>
	static void Unavailable(string reason)
	{
		Assert.False(
			!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RequiredVariable)),
			$"{reason} ({RequiredVariable} is set, so this is a failure rather than a skip.)");

		Assert.Skip(reason);
	}

	/// <summary>
	/// Pumps GTK so that queued relayout actually happens.
	///
	/// MEASURED (plan V3c/§10.2), and the single most important detail in this harness: draining
	/// EventsPending() is NOT sufficient headlessly. GTK3 relayout is driven by the frame clock,
	/// which does not tick under Xvfb without a redraw cycle, so Fixed.Move() and
	/// SetSizeRequest() register on the widget (the Fixed child properties do update) while
	/// Widget.Allocation keeps returning the STALE rectangle. Forcing an explicit SizeAllocate on
	/// the toplevel is what commits it.
	///
	/// Without this, layout assertions read stale allocations and quietly compare stale to
	/// stale - passing when they should fail.
	/// </summary>
	public static void Pump(Gtk.Window toplevel = null, int rounds = DefaultPumpRounds)
	{
		for (int i = 0; i < rounds; i++)
		{
			Drain();

			if (toplevel != null)
			{
				toplevel.SizeAllocate(new Gdk.Rectangle(
					0, 0, toplevel.AllocatedWidth, toplevel.AllocatedHeight));
			}

			while (GLib.MainContext.Iteration(false))
			{
			}
		}
	}

	static void Drain()
	{
		while (Gtk.Application.EventsPending())
			Gtk.Application.RunIteration();
	}

	/// <summary>
	/// Pumps until <paramref name="condition"/> holds, or the deadline expires. Returns whether
	/// it held.
	///
	/// MEASURED, and the reason a fixed <see cref="Pump"/> round count is not always enough:
	/// anything that goes through the window manager - <c>Gtk.Window.Resize</c> above all - is a
	/// REQUEST, not a state change. Under Xvfb there is no real window manager and the request
	/// is serviced within a round or two, so a fixed count happened to be sufficient on Linux.
	/// The Win32 GDK backend round-trips through the actual Windows window manager, where the
	/// latency is neither zero nor bounded by a round count, and a fixed pump silently reads the
	/// pre-resize geometry.
	///
	/// Callers should still assert on the result: this waits for the condition, it does not
	/// excuse it never happening.
	/// </summary>
	public static bool PumpUntil(Func<bool> condition, Gtk.Window toplevel = null, int timeoutMs = 5000)
	{
		var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

		while (true)
		{
			if (condition())
				return true;

			if (DateTime.UtcNow >= deadline)
				return false;

			Pump(toplevel, 1);
		}
	}

	/// <summary>
	/// Waits for a task by pumping GTK, instead of blocking the thread.
	///
	/// MEASURED: an <c>async</c> test method deadlocks this suite outright. <see cref="FormsWindow"/>'s
	/// constructor installs a <see cref="GtkSynchronizationContext"/> on whatever thread creates it
	/// (FormsWindow.cs:22), which is the test thread; every subsequent <c>await</c> continuation is
	/// then posted to the GTK main loop, and nothing runs that loop during a test. The run hangs
	/// with no failure and no output - the first version of this suite stopped dead after
	/// PlatformServiceTests.IdiomIsDesktop and had to be killed.
	///
	/// So: no <c>async</c> test methods in this assembly. Await through here.
	/// </summary>
	public static T Await<T>(Task<T> task, int timeoutMs = 10000)
	{
		var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

		while (!task.IsCompleted && DateTime.UtcNow < deadline)
			Pump(null, 1);

		Assert.True(task.IsCompleted, $"task did not complete within {timeoutMs}ms");

		return task.GetAwaiter().GetResult();
	}

	public static void Await(Task task, int timeoutMs = 10000)
	{
		var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

		while (!task.IsCompleted && DateTime.UtcNow < deadline)
			Pump(null, 1);

		Assert.True(task.IsCompleted, $"task did not complete within {timeoutMs}ms");

		task.GetAwaiter().GetResult();
	}

	/// <summary>
	/// Retires a test window: hide it, pump so the hide is processed, and keep the managed
	/// wrapper alive for the rest of the run.
	///
	/// MEASURED, and the reason this is not simply <c>Window.Destroy()</c>: destroying widgets
	/// between tests aborts the whole test host part-way through the suite with
	///
	///   GLib-GObject-CRITICAL: g_object_remove_toggle_ref: assertion 'G_IS_OBJECT (object)' failed
	///   GLib-GObject:ERROR:gobject.c:3379:toggle_refs_notify: assertion failed: (tstack.n_toggle_refs == 1)
	///
	/// GtkSharp's wrappers drop their toggle reference from a <c>GLib.Object</c> finalizer, which
	/// the CLR runs on the finalizer thread - not the GTK main thread - and by then
	/// <c>gtk_widget_destroy</c> has already freed the GObject. That is plan risk R8
	/// ("GtkSharp Dispose interacting with Gtk.Widget.Destroy()") reproduced deterministically.
	/// Holding a reference keeps the finalizer from ever running, which is what makes a
	/// hundred-test run survive. The windows are small and the process is short-lived.
	/// </summary>
	static void Retire(Gtk.Window window)
	{
		if (window == null)
			return;

		window.Hide();
		Pump(null, 2);

		s_retired.Add(window);
	}

	static readonly List<Gtk.Window> s_retired = new List<Gtk.Window>();

	/// <summary>
	/// Creates a renderer for a standalone view and parents it in a real toplevel, which is what
	/// makes the widget realizable and its allocation meaningful. Dispose the handle to retire
	/// the window.
	/// </summary>
	public static ViewHost<TView> HostView<TView>(TView view, int width = 400, int height = 300)
		where TView : View
	{
		return new ViewHost<TView>(view, width, height);
	}

	/// <summary>
	/// Loads a real <see cref="Page"/> through <see cref="FormsWindow"/>, i.e. the same path the
	/// gallery takes, so page renderers and the Forms layout pass are exercised rather than
	/// bypassed.
	/// </summary>
	public static PageHost HostPage(Page page, int width = 800, int height = 600)
	{
		return new PageHost(page, width, height);
	}

	/// <summary>Depth-first search of a native widget tree.</summary>
	public static List<T> Find<T>(Gtk.Widget root) where T : Gtk.Widget
	{
		var found = new List<T>();

		void Walk(Gtk.Widget w)
		{
			if (w is T t)
				found.Add(t);

			if (w is Gtk.Container c)
			{
				foreach (var child in c.Children)
					Walk(child);
			}
		}

		if (root != null)
			Walk(root);

		return found;
	}

	/// <summary>
	/// Delivers a synthetic button press to <paramref name="widget"/>, as gtk_widget_event would.
	/// </summary>
	/// <remarks>
	/// <c>GLib.Signal.Emit(w, "clicked")</c> - what this suite uses for Button and ImageButton -
	/// cannot stand in for this: <c>button-press-event</c> carries a <c>Gdk.EventButton</c>
	/// argument, and its handlers read the button number and the press type off it. So the event
	/// has to be built.
	///
	/// <para>It needs a real, viewable GdkWindow. gtk_widget_event drops a button event whose
	/// window is null or unmapped (event_window_is_still_viewable) and returns WITHOUT emitting
	/// the signal, which a test then reads as "the handler was never attached" - the opposite
	/// conclusion. The widget's own window is borrowed for that reason; for a windowless widget
	/// (<see cref="Xamarin.Forms.Platform.GTK.GtkFormsContainer"/> is a no-window
	/// <c>Gtk.EventBox</c>) that is the parent's, which is exactly what a real press would carry.</para>
	///
	/// <para>Deliberately not freed: gdk_event_free unrefs event-&gt;any.window, and that window is
	/// borrowed from a live widget. One leaked event per press is the cheaper half of the trade.</para>
	/// </remarks>
	public static void PressButton(Gtk.Widget widget, uint button = 1)
	{
		if (widget == null || widget.Window == null)
			throw new InvalidOperationException(
				$"the widget is not realized, so it has no window to press: {Describe(widget)}");

		Gdk.Event evnt = Gdk.EventHelper.New(Gdk.EventType.ButtonPress);
		var press = new Gdk.EventButton(evnt.Handle);

		press.Window = widget.Window;
		press.Button = button;

		widget.ProcessEvent(evnt);
	}

	/// <summary>
	/// True when GTK never gave the widget a real allocation. GTK3's sentinel for
	/// "not allocated yet" is (-1, -1, 1, 1), and every M3 layout bug in this backend showed up
	/// as exactly this while the widget still reported <c>Visible == true</c>.
	/// </summary>
	public static bool IsUnallocated(Gtk.Widget widget) =>
		widget == null || (widget.Allocation.Width <= 1 && widget.Allocation.Height <= 1);

	public static string Describe(Gtk.Widget widget) =>
		widget == null
			? "<null>"
			: $"{widget.GetType().Name}[{widget.Allocation.X},{widget.Allocation.Y} " +
			  $"{widget.Allocation.Width}x{widget.Allocation.Height} visible={widget.Visible}]";

	public sealed class ViewHost<TView> : IDisposable where TView : View
	{
		public ViewHost(TView view, int width, int height)
		{
			View = view;
			Window = new Gtk.Window(Gtk.WindowType.Toplevel);
			Window.SetDefaultSize(width, height);

			Renderer = Platform.CreateRenderer(view);
			Container = new Gtk.Fixed();
			Container.Put((Gtk.Widget)Renderer, 0, 0);
			Window.Add(Container);
			Window.ShowAll();

			// A standalone view is not inside a Forms layout pass, so nothing else will ever
			// give it bounds; do it explicitly, as CarouselViewRenderer does for its items.
			view.Layout(new Rectangle(0, 0, width, height));
			Renderer.SetElementSize(new Size(width, height));

			GtkTestHost.Pump(Window);
		}

		public TView View { get; }
		public Gtk.Window Window { get; }
		public Gtk.Fixed Container { get; }
		public IVisualElementRenderer Renderer { get; }

		/// <summary>The native control, for renderers that expose one.</summary>
		public TNative Control<TNative>() where TNative : Gtk.Widget
		{
			var native = Renderer as IVisualNativeElementRenderer;

			Assert.True(native != null,
				$"{Renderer.GetType().Name} does not expose a native control");

			return Assert.IsAssignableFrom<TNative>(native.Control);
		}

		public void Pump(int rounds = DefaultPumpRounds) => GtkTestHost.Pump(Window, rounds);

		public void Dispose() => Retire(Window);
	}

	public sealed class PageHost : IDisposable
	{
		readonly TestApplication _app;

		public PageHost(Page page, int width, int height)
		{
			Window = new FormsWindow();
			Window.SetDefaultSize(width, height);

			_app = new TestApplication { MainPage = page };
			Window.LoadApplication(_app);
			Window.ShowAll();

			// Under Xvfb no window manager ever configures the window, so OnConfigureEvent -
			// which is what normally tells Forms how big the page is - may never fire.
			Window.Resize(width, height);
			Platform.GetRenderer(page)?.SetElementSize(new Size(width, height));

			GtkTestHost.Pump(Window);
		}

		public FormsWindow Window { get; }
		public Page Page => _app.MainPage;

		public void Pump(int rounds = DefaultPumpRounds) => GtkTestHost.Pump(Window, rounds);

		public void Dispose() => Retire(Window);

		sealed class TestApplication : Application
		{
		}
	}
}

/// <summary>
/// Base class for every fixture here. Its constructor runs the GTK/Forms bootstrap, which xUnit
/// guarantees happens before each test method.
/// </summary>
public abstract class GtkTestBase
{
	protected GtkTestBase() => GtkTestHost.EnsureInitialized();
}
