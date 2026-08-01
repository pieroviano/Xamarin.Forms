using System;
using System.Linq;
using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Tests of the harness itself, not of the backend.
	///
	/// Everything in <see cref="LayoutTests"/> rests on <see cref="GtkTestHost.Pump"/> committing a
	/// new allocation, and plan §10.2 warns that a pump which only drains the event queue leaves
	/// <c>Widget.Allocation</c> STALE - so layout assertions compare stale to stale and pass
	/// vacuously. That warning is load-bearing for the whole suite and is therefore asserted here
	/// rather than trusted: if the forced <c>SizeAllocate</c> ever stops being necessary, or ever
	/// stops being sufficient, one of these fails.
	/// </summary>
	public class PumpHarnessTests : GtkTestBase
	{
		/// <summary>
		/// The plan's claim, made falsifiable: drain-only pumping does NOT commit a new allocation
		/// under Xvfb (no frame clock, no redraw cycle), and the forced SizeAllocate on the toplevel
		/// is what does. Both halves are asserted, so this fails whichever way the claim breaks.
		/// </summary>
		[Fact]
		public void OnlyTheForcedSizeAllocateCommitsANewAllocation()
		{
			var box = new BoxView { Color = Color.Red };
			var page = new ContentPage { Content = new StackLayout { Children = { box } } };

			using (var host = GtkTestHost.HostPage(page, 400, 300))
			{
				var widget = (Gtk.Widget)Platform.GetRenderer(box);
				var before = widget.Allocation.Width;

				Assert.True(before > 1, $"nothing was allocated to begin with: {GtkTestHost.Describe(widget)}");

				host.Window.Resize(900, 600);
				Platform.GetRenderer(page)?.SetElementSize(new Size(900, 600));

				// Pump(null, ...) is the drain-only pump: EventsPending/RunIteration plus
				// GLib.MainContext.Iteration, and no SizeAllocate on any toplevel.
				GtkTestHost.Pump(null, 12);
				var drained = widget.Allocation.Width;

				GtkTestHost.Pump(host.Window, 12);
				var forced = widget.Allocation.Width;

				Assert.True(drained == before,
					$"drain-only pumping already committed a new allocation ({before}px -> {drained}px). " +
					"If that is genuinely true now, the plan's §10.2 Pump warning is obsolete - but " +
					"check first that this test is not simply racing a real frame clock.");

				Assert.True(forced > drained,
					$"the forced SizeAllocate did not commit the resize ({drained}px -> {forced}px); " +
					"every layout assertion in this suite is then reading stale rectangles");
			}
		}

		/// <summary>
		/// The harness must be able to FAIL on a wrong number, not merely on an exception, and this
		/// is the test that was used to prove it: it was first written asserting
		/// <c>tr.X == 0</c> - the value a broken backend produced throughout the M3 outage, every
		/// child allocated at (0,0) at its natural size - and it failed with
		///
		///     DELIBERATELY WRONG - column 1 is at x=403, column 0 at x=0
		///
		/// i.e. a real measured allocation, not GTK's unallocated sentinel and not a stale
		/// rectangle. The assertion below is that same measurement stated correctly: in an 800px
		/// window two star columns put column 1 near the middle. Deliberately a RANGE around the
		/// half-way point rather than the literal 403, which carries the window chrome of this
		/// particular run.
		/// </summary>
		[Fact]
		public void AllocationsAreRealNumbersAndNotTheBrokenM3Ones()
		{
			var topLeft = new BoxView { Color = Color.Red };
			var topRight = new BoxView { Color = Color.Green };

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
				}
			};

			grid.Children.Add(topLeft, 0, 0);
			grid.Children.Add(topRight, 1, 0);

			using (var host = GtkTestHost.HostPage(new ContentPage { Content = grid }, 800, 600))
			{
				var tl = ((Gtk.Widget)Platform.GetRenderer(topLeft)).Allocation;
				var tr = ((Gtk.Widget)Platform.GetRenderer(topRight)).Allocation;

				Assert.True(tl.X == 0,
					$"column 0 should start at the left edge, got x={tl.X}");

				Assert.True(tr.X >= 360 && tr.X <= 440,
					$"column 1 of two star columns in an 800px window should start near x=400; " +
					$"got x={tr.X} (column 0 at x={tl.X}). x=0 is the broken-M3 value, " +
					"x=-1 is GTK's unallocated sentinel.");

				Assert.True(tl.Width >= 360 && tr.Width >= 360,
					$"each star column should be about half the window: {tl.Width}px / {tr.Width}px");
			}
		}
	}
}
