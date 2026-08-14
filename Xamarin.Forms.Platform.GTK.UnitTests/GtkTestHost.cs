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
					StartGtkThread();

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

	static System.Threading.Thread s_gtkThread;
	static readonly System.Collections.Concurrent.BlockingCollection<Action> s_work =
		new System.Collections.Concurrent.BlockingCollection<Action>();

	/// <summary>
	/// Starts the thread GTK is initialised on, and runs every test body on it.
	/// </summary>
	/// <remarks>
	/// <para>MEASURED, and mandatory on Windows under Gtk 4: the GDK Win32 backend calls
	/// OleInitialize during gtk_init, which REQUIRES a single-threaded apartment. xUnit runs test
	/// bodies on thread-pool threads, which are MTA, so initialising GTK there aborted the process
	/// outright - "COM runtime already initialized on the main thread with an incompatible
	/// apartment model", then "Gdk-ERROR: OleInitialize failed", exit code 0xC0000409. Gtk 3 did
	/// not call OleInitialize from gtk_init, which is why this suite could get away with the test
	/// thread until now.</para>
	///
	/// <para>An apartment cannot be changed after a thread starts, so GTK needs a thread of its
	/// own - and once it has one, every GTK call has to be marshalled onto it, because GTK may
	/// only be used from the thread that called gtk_init. That is what GtkTestBase.Run is for.
	/// The sibling GtkSharp suite is built the same way, for the same reason.</para>
	/// </remarks>
	static void StartGtkThread()
	{
		var ready = new System.Threading.ManualResetEventSlim();
		System.Runtime.ExceptionServices.ExceptionDispatchInfo startupError = null;

		s_gtkThread = new System.Threading.Thread(() =>
		{
			try
			{
				Gtk.Application.Init();
				Forms.Init();
			}
			catch (Exception e)
			{
				startupError = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
				ready.Set();
				return;
			}

			ready.Set();

			foreach (var work in s_work.GetConsumingEnumerable())
				work();
		});

		s_gtkThread.IsBackground = true;
		s_gtkThread.Name = "Gtk";

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			s_gtkThread.SetApartmentState(System.Threading.ApartmentState.STA);

		s_gtkThread.Start();
		ready.Wait();

		startupError?.Throw();
	}

	/// <summary>Runs a test body on the GTK thread, rethrowing whatever it throws.</summary>
	public static void Run(Action body)
	{
		EnsureInitialized();

		if (System.Threading.Thread.CurrentThread == s_gtkThread)
		{
			body();
			return;
		}

		System.Runtime.ExceptionServices.ExceptionDispatchInfo error = null;

		using (var done = new System.Threading.ManualResetEventSlim())
		{
			s_work.Add(() =>
			{
				try
				{
					body();
				}
				catch (Exception e)
				{
					error = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
				}
				finally
				{
					done.Set();
				}
			});

			done.Wait();
		}

		error?.Throw();
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
				Allocate(toplevel, toplevel.Width, toplevel.Height);

			// Bounded for the same reason as Drain - see MaxDrainIterations. This loop had no
			// limit either, and between the two of them a self-requeuing idle handler hung the
			// runner with no failing test to point at.
			for (int j = 0; j < MaxDrainIterations && GLib.MainContext.Iteration(false); j++)
			{
			}
		}
	}

	/// <summary>Resizes a test window and commits the new allocation.</summary>
	/// <remarks>
	/// <para>Gtk 4 removed gtk_window_resize: an application can set a window's DEFAULT size and
	/// nothing more, because on Wayland the compositor owns the geometry. So a test that wants a
	/// window to become a different size has to allocate it, which is exactly what
	/// <see cref="Pump"/> already does at the toplevel's current size.</para>
	///
	/// <para>This is strictly more reliable than what it replaces. Gtk 3's Resize was a REQUEST to
	/// the window manager - serviced immediately under Xvfb, never serviced at all on the Win32
	/// backend in a non-interactive session - which is why the two call sites for this carried
	/// long comments about waiting for an answer that might never come. There is no request and no
	/// answer any more: the size is set here.</para>
	/// </remarks>
	public static void Resize(Gtk.Window window, int width, int height)
	{
		if (window == null)
			throw new ArgumentNullException(nameof(window));

		window.SetDefaultSize(width, height);
		Allocate(window, width, height);

		Pump(window);
	}

	/// <summary>Renders a widget and returns its pixels as tightly-packed BGRA.</summary>
	/// <remarks>
	/// <para>The Gtk 4 pixel-readback path, and there is no shorter one. Gtk 3 let a test ask a
	/// realized widget's GdkWindow for its contents (<c>new Gdk.Pixbuf(window, ...)</c>); Gtk 4 has
	/// no per-widget windows and nothing to photograph, because a widget does not own pixels - it
	/// contributes render nodes to a tree that a GskRenderer rasterises.</para>
	///
	/// <para>So the widget is asked for that tree instead: a GtkWidgetPaintable snapshots it, the
	/// snapshot becomes a GskRenderNode, and the toplevel's own renderer turns the node into a
	/// GdkTexture whose bytes can be read. Rendering explicitly is also what replaces the Gtk 3
	/// ProcessUpdates call the callers needed - there is no queued frame to flush, because this
	/// does not wait for a frame at all. That removes the headless flakiness the old path had, where
	/// a screenshot could return the PREVIOUS frame because the frame clock does not tick on demand
	/// under Xvfb.</para>
	///
	/// <para>The format is BGRA8 (<c>Gdk.MemoryFormat.B8g8r8a8Premultiplied</c> is what a renderer
	/// produces on every backend this suite runs on), premultiplied, four bytes per pixel, with no
	/// row padding.</para>
	/// </remarks>
	public static byte[] RenderToBytes(Gtk.Widget widget, out int width, out int height)
	{
		if (widget == null)
			throw new ArgumentNullException(nameof(widget));

		width = widget.Width;
		height = widget.Height;

		if (width <= 0 || height <= 0)
			throw new InvalidOperationException(
				$"the widget has no allocation to render: {Describe(widget)}");

		var native = widget.Native
			?? throw new InvalidOperationException(
				$"the widget is not in a realized toplevel, so there is no renderer: {Describe(widget)}");

		var paintable = new Gtk.WidgetPaintable(widget);
		var snapshot = new Gtk.Snapshot();

		((Gdk.IPaintable)paintable).Snapshot(snapshot, width, height);

		var node = snapshot.ToNode();

		if (node == null)
			throw new InvalidOperationException(
				$"the widget produced no render nodes: {Describe(widget)}");

		var bounds = Graphene.Rect.Alloc();
		bounds.Init(0, 0, width, height);

		using (var texture = native.Renderer.RenderTexture(node, bounds))
			return texture.Download();
	}

	/// <summary>The colour of one pixel of a rendered widget, as 0-255 RGB.</summary>
	/// <remarks>
	/// Deliberately not Gdk.Color: its channels are 16-bit, so the 0-255 thresholds callers assert
	/// on would silently be wrong.
	/// </remarks>
	public static (byte R, byte G, byte B) PixelAt(Gtk.Widget widget, int x, int y)
	{
		int width, height;
		var bytes = RenderToBytes(widget, out width, out height);

		if (x < 0 || y < 0 || x >= width || y >= height)
			throw new ArgumentOutOfRangeException(
				nameof(x), $"({x},{y}) is outside the {width}x{height} widget");

		// BGRA, so blue comes first - the opposite of the pixbuf this replaces.
		var offset = (y * width * 4) + (x * 4);

		return (bytes[offset + 2], bytes[offset + 1], bytes[offset]);
	}

	/// <summary>Where a widget sits inside its toplevel, as a rectangle.</summary>
	/// <remarks>
	/// <para>Replaces <c>Widget.Allocation</c> for every assertion about POSITION. Gtk 3 allocated
	/// a widget a rectangle in its parent's coordinates, so Allocation.X/Y answered "where is this";
	/// Gtk 4 allocates a SIZE in the widget's own coordinates and carries position separately as a
	/// transform, so Allocation.X/Y are always zero and an unported assertion compares 0 with 0 -
	/// passing whatever the layout does.</para>
	///
	/// <para>gtk_widget_compute_bounds is the replacement: it walks the transforms between two
	/// widgets and reports one's bounds in the other's coordinate space. Passing the toplevel
	/// reproduces what the Gtk 3 assertions were reading.</para>
	/// </remarks>
	public static Gdk.Rectangle BoundsIn(Gtk.Widget widget, Gtk.Widget ancestor = null)
	{
		if (widget == null)
			throw new ArgumentNullException(nameof(widget));

		var target = ancestor ?? (Gtk.Widget)widget.Root ?? widget;

		if (!widget.ComputeBounds(target, out var bounds))
			throw new InvalidOperationException(
				$"{Describe(widget)} has no position relative to {Describe(target)} - "
				+ "they are not in the same widget tree, or neither has been allocated");

		return new Gdk.Rectangle(
			(int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
	}

	/// <summary>Measures a widget and then allocates it, in that order.</summary>
	/// <remarks>
	/// MEASURED, and the reason every layout assertion read stale geometry at first: Gtk 4 requires
	/// measure-before-allocate. Allocating without it logs
	///     "Allocating size to ... without calling gtk_widget_measure(). How does the code know the
	///     size to allocate?"
	/// and the allocation does not propagate - so no renderer was ever laid out, Platform.GetRenderer
	/// handed back widgets with no size, and half this suite failed with NullReferenceException
	/// rather than with a wrong number.
	///
	/// Gtk 3 had no such rule, which is why the Gtk 3 harness could call SizeAllocate on its own.
	/// </remarks>
	static void Allocate(Gtk.Widget widget, int width, int height)
	{
		if (width <= 0 || height <= 0)
			return;

		int minimumWidth, minimumHeight;

		widget.Measure(Gtk.Orientation.Horizontal, -1, out minimumWidth, out _, out _, out _);

		// Clamped to the minimum, not allocated at whatever was asked for. Gtk 4 REFUSES an
		// allocation smaller than a widget's minimum - "Allocation height too small. Tried to
		// allocate 400x300, but GtkWindow needs at least 400x339", a Gtk-CRITICAL - and then the
		// layout does not settle, so every assertion downstream reads geometry that was never
		// committed. Gtk 3 allowed the under-allocation and simply clipped.
		//
		// The width is fixed first and the height measured FOR that width, because these are
		// height-for-width widgets: a narrower window needs a taller one to fit the same text.
		width = Math.Max(width, minimumWidth);

		widget.Measure(Gtk.Orientation.Vertical, width, out minimumHeight, out _, out _, out _);

		height = Math.Max(height, minimumHeight);

		widget.SizeAllocate(new Gdk.Rectangle(0, 0, width, height));
	}

	/// <summary>The most iterations one Drain will run before giving up.</summary>
	/// <remarks>
	/// A bound, where the Gtk 3 harness looped while EventsPending() with no limit. That was safe
	/// only by luck: an idle handler that re-queues itself makes the loop infinite, and the runner
	/// then hangs rather than failing - which is strictly worse, because a hang takes CI with it
	/// and names no test. MEASURED under Gtk 4:
	/// PropertyMappingTests.EntryMapsTextPlaceholderAndPassword did exactly that, spinning for
	/// minutes while GTK logged text-buffer and accessibility criticals, and held the built
	/// assemblies open so that even a rebuild failed.
	///
	/// 2000 is far more than any settling layout needs here - the next-slowest test drains in
	/// tens of iterations - so a test that reaches it is looping, and should fail on its own
	/// assertions instead.
	/// </remarks>
	const int MaxDrainIterations = 2000;

	static void Drain()
	{
		for (int i = 0; i < MaxDrainIterations && Gtk.Application.EventsPending(); i++)
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

		window.Visible = false;
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

		// GetFirstChild/GetNextSibling, not "is Gtk.Container". Gtk 4 has no GtkContainer: children
		// are a linked list on GtkWidget itself, and ANY widget may have them. The Gtk 3 test only
		// descended into containers, and the compat Gtk.Container covers just this backend's own
		// wrappers - so a walk looking for, say, the GtkScrolledWindow inside a CollectionView
		// stopped at the first real Gtk 4 widget and reported that the renderer had built nothing.
		void Walk(Gtk.Widget w)
		{
			if (w is T t)
				found.Add(t);

			for (var child = w.FirstChild; child != null; child = child.NextSibling)
				Walk(child);
		}

		if (root != null)
			Walk(root);

		return found;
	}

	/// <summary>Synthesizes a button press on a widget.</summary>
	/// <remarks>
	/// <para>By emitting the signal of the widget's own GtkGestureClick, found by walking its
	/// controllers. Gtk 4 removed EVERY way for an application to fabricate input: there is no
	/// public GdkEvent constructor, no gdk_event_new, and no gtk_widget_event. Driving the
	/// controller is what is left, and it is the same path a real press takes once GDK has
	/// dispatched it.</para>
	///
	/// <para>The gesture has to be the one the widget is already listening through - attaching a
	/// second GestureClick and emitting on that proves nothing, because the compat
	/// ButtonPressEvent listens to its own. (The GtkSharp test that pins this behaviour failed
	/// exactly that way first; see CompatTests.Button_press_event_fires_from_a_click_gesture.)</para>
	///
	/// <para>So a widget with no ButtonPressEvent subscriber has no gesture to drive, and this
	/// says so rather than silently doing nothing - the same failure mode the Gtk 3 version
	/// guarded against when it checked for an unrealized window.</para>
	/// </remarks>
	public static void PressButton(Gtk.Widget widget, uint button = 1)
	{
		if (widget == null)
			throw new ArgumentNullException(nameof(widget));

		var gesture = ControllerOf<Gtk.GestureClick>(widget);

		if (gesture == null)
			throw new InvalidOperationException(
				"the widget has no GestureClick to press: nothing has subscribed to its "
				+ $"ButtonPressEvent, so no controller was ever attached. {Describe(widget)}");

		// n_press, x, y - the arguments of GtkGestureClick::pressed. The coordinates are the
		// widget's centre, so a handler that hit-tests lands inside it.
		GLib.Signal.Emit(gesture, "pressed", 1, widget.Width / 2.0, widget.Height / 2.0);
	}

	/// <summary>The first controller of the given kind attached to a widget, or null.</summary>
	static T ControllerOf<T>(Gtk.Widget widget) where T : Gtk.EventController
	{
		var controllers = widget.ObserveControllers();

		for (uint i = 0; i < controllers.NItems; i++)
		{
			if (controllers.GetObject(i) is T match)
				return match;
		}

		return null;
	}

	/// <summary>
	/// True when GTK never gave the widget a real allocation. GTK3's sentinel for
	/// "not allocated yet" is (-1, -1, 1, 1), and every M3 layout bug in this backend showed up
	/// as exactly this while the widget still reported <c>Visible == true</c>.
	/// </summary>
	public static bool IsUnallocated(Gtk.Widget widget) =>
		widget == null || (widget.Width <= 1 && widget.Height <= 1);

	public static string Describe(Gtk.Widget widget) =>
		widget == null
			? "<null>"
			: $"{widget.GetType().Name}[{widget.Width}x{widget.Height} visible={widget.Visible}]";

	public sealed class ViewHost<TView> : IDisposable where TView : View
	{
		public ViewHost(TView view, int width, int height)
		{
			View = view;
			// No WindowType: Gtk 4 removed the enum - a GtkWindow is always a toplevel.
			Window = new Gtk.Window();
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

			// SetDefaultSize, not Resize: Gtk 4 removed gtk_window_resize outright. The reason
			// this call exists is unchanged - under Xvfb no window manager configures the window,
			// so the size-allocate that normally tells Forms how big the page is may never come -
			// but the default size is now the only lever an application has over its own geometry.
			Window.SetDefaultSize(width, height);
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

	/// <summary>Runs a test body on the GTK thread. See <see cref="GtkTestHost.Run"/>.</summary>
	protected static void Run(Action body) => GtkTestHost.Run(body);
}
