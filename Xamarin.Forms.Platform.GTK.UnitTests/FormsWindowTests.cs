using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// <see cref="FormsWindow.LoadPage"/> and <see cref="FormsWindow.QuitOnClose"/>: more than one window
	/// open at once, none of them replacing the current application or ending the program on close.
	/// </summary>
	public class FormsWindowTests : GtkTestBase
	{
		/// <summary>Windows closed by a test: kept alive so their wrappers are never finalized (see GtkTestHost.Retire).</summary>
		static readonly List<Gtk.Window> s_closed = new List<Gtk.Window>();

		[Fact]
		public void LoadPageRendersThePageWithoutReplacingTheCurrentApplication()
		{
			Run(() =>
			{
				var main = new ContentPage { Content = new Label { Text = "main" } };

				using (var host = GtkTestHost.HostPage(main))
				{
					var current = Application.Current;
					var second = new ContentPage { Content = new Label { Text = "second" } };
					var window = new FormsWindow();

					window.LoadPage(second);
					window.ShowAll();
					GtkTestHost.Pump(window);

					Assert.Same(current, Application.Current);
					Assert.Same(second, window.CurrentPage);
					Assert.NotNull(Platform.GetRenderer(second));
					Assert.NotNull(Platform.GetRenderer(main));

					Close(window);
				}
			});
		}

		[Fact]
		public void EveryLoadedPageKeepsItsOwnWindow()
		{
			Run(() =>
			{
				var first = new ContentPage { Content = new Label { Text = "first" } };
				var second = new ContentPage { Content = new Label { Text = "second" } };
				var a = new FormsWindow();
				var b = new FormsWindow();

				a.LoadPage(first);
				b.LoadPage(second);
				a.ShowAll();
				b.ShowAll();
				GtkTestHost.Pump(a);
				GtkTestHost.Pump(b);

				Assert.Same(first, a.CurrentPage);
				Assert.Same(second, b.CurrentPage);
				Assert.NotSame(Platform.GetRenderer(first), Platform.GetRenderer(second));

				Close(a);
				Close(b);
			});
		}

		[Fact]
		public void CurrentPageIsTheApplicationsMainPageForAnApplicationWindow()
		{
			Run(() =>
			{
				var page = new ContentPage();

				using (var host = GtkTestHost.HostPage(page))
					Assert.Same(page, host.Window.CurrentPage);
			});
		}

		[Fact]
		public void AWindowHostsEitherAnApplicationOrAPage()
		{
			Run(() =>
			{
				var window = new FormsWindow();
				window.LoadPage(new ContentPage());

				Assert.Throws<InvalidOperationException>(() => window.LoadApplication(new TestApplication()));

				using (var host = GtkTestHost.HostPage(new ContentPage()))
					Assert.Throws<InvalidOperationException>(() => host.Window.LoadPage(new ContentPage()));

				s_closed.Add(window);
			});
		}

		[Fact]
		public void LoadPageRejectsNull()
		{
			Run(() => Assert.Throws<ArgumentNullException>(() => new FormsWindow().LoadPage(null)));
		}

		[Fact]
		public void ClosingAWindowThatDoesNotQuitOnCloseLeavesTheMainLoopRunning()
		{
			Run(() =>
			{
				var window = new FormsWindow { QuitOnClose = false };
				window.LoadPage(new ContentPage());
				window.ShowAll();

				Assert.True(RunUntilQuitOrTimeout(window), "closing the window quit the main loop");
			});
		}

		[Fact]
		public void ClosingAWindowQuitsTheMainLoopByDefault()
		{
			Run(() =>
			{
				var window = new FormsWindow();
				window.LoadPage(new ContentPage());
				window.ShowAll();

				Assert.True(window.QuitOnClose);
				Assert.False(RunUntilQuitOrTimeout(window), "closing the window did not quit the main loop");

				window.Visible = false;
			});
		}

		/// <summary>
		/// Runs the GTK main loop, closes <paramref name="window"/> from inside it and quits the loop from a
		/// timer that fires later. True when that timer is what ended the loop, i.e. the close did not.
		/// </summary>
		static bool RunUntilQuitOrTimeout(FormsWindow window)
		{
			var timedOut = false;

			GLib.Timeout.Add(0, () =>
			{
				window.Close();
				return false;
			});

			var timer = GLib.Timeout.Add(500, () =>
			{
				timedOut = true;
				Gtk.Application.Quit();
				return false;
			});

			Gtk.Application.Run();

			if (!timedOut)
				GLib.Source.Remove(timer);

			s_closed.Add(window);

			return timedOut;
		}

		static void Close(FormsWindow window)
		{
			window.Visible = false;
			GtkTestHost.Pump(null, 2);
			s_closed.Add(window);
		}

		sealed class TestApplication : Application
		{
		}
	}
}
