using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;

// GTK is not thread-safe: every widget touch has to happen on the thread that ran
// Gtk.Application.Init (Forms.MainThread). Running fixtures in parallel would spread them
// across NUnit's worker threads, so the whole assembly is serialized.
[assembly: NonParallelizable]
[assembly: LevelOfParallelism(1)]

/// <summary>
/// Assembly-wide GTK + Forms bootstrap. Deliberately in the global namespace so NUnit applies
/// it to every fixture.
/// </summary>
[SetUpFixture]
public class GtkTestHost
{
	/// <summary>
	/// The number of Pump rounds a layout assertion needs. Geometry is pushed to GTK from a
	/// GLib.Idle callback (plan M3 root cause 2 - "never mutate geometry inside a size-allocate"),
	/// so a single drain of the event queue is one round short of the answer.
	/// </summary>
	public const int DefaultPumpRounds = 6;

	[OneTimeSetUp]
	public void Init()
	{
		if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
		{
			Assert.Fail(
				"No DISPLAY. These are real GTK widget tests and need an X server: " +
				"run them as `xvfb-run -a dotnet test Xamarin.Forms.Platform.GTK.UnitTests`.");
		}

		Gtk.Application.Init();
		Forms.Init();
	}

	[OneTimeTearDown]
	public void Teardown()
	{
		// Nothing owns the main loop here - Gtk.Application.Run is never called - so there is
		// no Quit to issue. Draining what is left keeps a pending idle callback from running
		// against a torn-down fixture.
		Drain();
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
	/// Waits for a task by pumping GTK, instead of blocking the thread.
	///
	/// MEASURED: an <c>async</c> test method deadlocks this suite outright. <see cref="FormsWindow"/>'s
	/// constructor installs a <see cref="GtkSynchronizationContext"/> on whatever thread creates it
	/// (FormsWindow.cs:22), which is the NUnit test thread; every subsequent <c>await</c>
	/// continuation is then posted to the GTK main loop, and nothing runs that loop during a test.
	/// The run hangs with no failure and no output - the first version of this suite stopped dead
	/// after PlatformServiceTests.IdiomIsDesktop and had to be killed.
	///
	/// So: no <c>async</c> test methods in this assembly. Await through here.
	/// </summary>
	public static T Await<T>(Task<T> task, int timeoutMs = 10000)
	{
		var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

		while (!task.IsCompleted && DateTime.UtcNow < deadline)
			Pump(null, 1);

		Assert.That(task.IsCompleted, Is.True, $"task did not complete within {timeoutMs}ms");

		return task.GetAwaiter().GetResult();
	}

	public static void Await(Task task, int timeoutMs = 10000)
	{
		var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

		while (!task.IsCompleted && DateTime.UtcNow < deadline)
			Pump(null, 1);

		Assert.That(task.IsCompleted, Is.True, $"task did not complete within {timeoutMs}ms");

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
	/// Holding a reference keeps the finalizer from ever running, which is what makes an 85-test
	/// run survive. The windows are small and the process is short-lived.
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
	/// makes the widget realizable and its allocation meaningful. Dispose the handle to destroy
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

			Assert.That(native, Is.Not.Null,
				$"{Renderer.GetType().Name} does not expose a native control");

			return (TNative)(object)native.Control;
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
