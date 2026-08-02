using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Coverage target 4 of plan §10.2, guarding risk R8 (GtkSharp Dispose interacting with
	/// <c>Gtk.Widget.Destroy()</c>, suspected of intermittent segfaults on navigation). Run the
	/// suite under <c>G_DEBUG=fatal-criticals</c> to make a GTK critical abort the run instead of
	/// scrolling past.
	/// </summary>
	public class LifecycleTests : GtkTestBase
	{
		[Fact]
		public void RenderersCanBeCreatedAndDestroyedRepeatedly()
		{
			for (int i = 0; i < 20; i++)
			{
				using (var host = GtkTestHost.HostView(new Entry { Text = $"pass {i}" }))
				{
					Assert.NotNull(host.Renderer);
				}
			}
		}

		[Fact]
		public void PagesCanBeLoadedAndTornDownRepeatedly()
		{
			for (int i = 0; i < 5; i++)
			{
				var page = new ContentPage
				{
					Content = new StackLayout
					{
						Children =
						{
							new Label { Text = $"page {i}" },
							new Button { Text = "ok" },
							new Entry { Text = "x" }
						}
					}
				};

				using (var host = GtkTestHost.HostPage(page))
				{
					Assert.NotNull(Platform.GetRenderer(page));
				}
			}
		}

		/// <summary>
		/// <c>Platform.DisposeModelAndChildrenRenderers</c> is what navigation runs when a page
		/// leaves the stack; if it leaves the attached renderer behind, the element stays
		/// <c>IsPlatformEnabled</c> and the next navigation renders against a destroyed widget.
		/// </summary>
		[Fact]
		public void DisposingAPageClearsItsChildrenRenderers()
		{
			var label = new Label { Text = "child" };
			var page = new ContentPage { Content = new StackLayout { Children = { label } } };

			using (var host = GtkTestHost.HostPage(page))
			{
				Assert.True(Platform.GetRenderer(label) != null, "precondition");

				// Actually run the teardown this test is named for. It previously asserted the
				// precondition and stopped, so the behaviour described above - that disposing a
				// page clears its children's renderers - was never checked and the test could not
				// fail.
				Platform.DisposeModelAndChildrenRenderers(page);
				GtkTestHost.Pump(null, 3);

				Assert.Null(Platform.GetRenderer(label));
				Assert.Null(Platform.GetRenderer(page));
			}

			GtkTestHost.Pump(null, 3);
		}

		[Fact]
		public void DestroyingARendererDoesNotThrowOnSubsequentPumps()
		{
			var views = new List<View>
			{
				new Label { Text = "l" },
				new Button { Text = "b" },
				new Entry { Text = "e" },
				new Switch(),
				new CheckBox(),
				new ProgressBar { Progress = 0.5 },
				new ActivityIndicator { IsRunning = true },
				new BoxView { Color = Color.Red },
				new Slider { Value = 1 },
				new Stepper { Value = 1 }
			};

			foreach (var view in views)
			{
				var exception = Record.Exception(() =>
				{
					using (var host = GtkTestHost.HostView(view))
					{
						host.Pump(2);
					}

					GtkTestHost.Pump(null, 2);
				});

				Assert.True(exception == null,
					$"{view.GetType().Name} threw during create/destroy: {exception}");
			}
		}
	}
}
