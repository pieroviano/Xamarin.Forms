using System;
using System.Linq;
using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Coverage target 3 of plan §10.2, and the regression net for M3's root causes. Every one of
	/// those bugs was silent: <c>Element.Bounds</c> was correct, every size request was correct,
	/// and the pixels were wrong, because geometry was pushed to GTK from inside a size-allocate
	/// and GTK3 discards resizes queued there. So these assertions are deliberately on
	/// <c>Widget.Allocation</c> - the thing that was wrong - and never on Forms bounds alone,
	/// which would have passed throughout the entire M3 outage.
	/// </summary>
	public class LayoutTests : GtkTestBase
	{
		[Fact]
		public void PageFillsTheWindow()
		{
			var page = new ContentPage
			{
				Content = new BoxView { Color = Color.Red }
			};

			using (var host = GtkTestHost.HostPage(page, 800, 600))
			{
				Assert.True(page.Width > 0, "the page never received a size");
				Assert.True(page.Height > 0, "the page never received a height");

				var renderer = Platform.GetRenderer(page);
				var widget = (Gtk.Widget)renderer;

				Assert.True(widget.Allocation.Width > 1,
					$"the page widget is still at GTK's unallocated sentinel: {GtkTestHost.Describe(widget)}");
			}
		}

		/// <summary>
		/// The M3 headline: a StackLayout's children must be allocated at distinct positions.
		/// When geometry never reached GTK, every child stacked at (0,0) at its natural size
		/// while Forms' own bounds were perfect.
		/// </summary>
		[Fact]
		public void StackLayoutChildrenAreAllocatedAtDistinctPositions()
		{
			var first = new BoxView { Color = Color.Red, HeightRequest = 40 };
			var second = new BoxView { Color = Color.Green, HeightRequest = 40 };
			var third = new BoxView { Color = Color.Blue, HeightRequest = 40 };

			var page = new ContentPage
			{
				Content = new StackLayout
				{
					Spacing = 10,
					Children = { first, second, third }
				}
			};

			using (var host = GtkTestHost.HostPage(page))
			{
				var ys = new[] { first, second, third }
					.Select(b => (Gtk.Widget)Platform.GetRenderer(b))
					.Select(w => w.Allocation.Y)
					.ToArray();

				Assert.True(ys.Distinct().Count() == 3,
					$"children stacked on top of each other at y = [{string.Join(", ", ys)}]");

				Assert.True(ys[0] < ys[1], $"y = [{string.Join(", ", ys)}] is not in order");
				Assert.True(ys[1] < ys[2], $"y = [{string.Join(", ", ys)}] is not in order");
			}
		}

		[Fact]
		public void StackLayoutChildAllocationsAgreeWithFormsBounds()
		{
			var box = new BoxView { Color = Color.Red, HeightRequest = 50 };

			var page = new ContentPage
			{
				Content = new StackLayout { Padding = new Thickness(20), Children = { box } }
			};

			using (var host = GtkTestHost.HostPage(page))
			{
				var widget = (Gtk.Widget)Platform.GetRenderer(box);

				// One pixel of slack: Forms works in doubles, GTK in ints.
				Assert.True(Math.Abs(widget.Allocation.Width - (int)box.Width) <= 1,
					$"native width {widget.Allocation.Width} disagrees with Forms {(int)box.Width} - " +
					"geometry did not reach GTK");
				Assert.True(Math.Abs(widget.Allocation.Height - (int)box.Height) <= 1,
					$"native height {widget.Allocation.Height} disagrees with Forms {(int)box.Height}");
			}
		}

		[Fact]
		public void GridPlacesChildrenInTheirCells()
		{
			var topLeft = new BoxView { Color = Color.Red };
			var topRight = new BoxView { Color = Color.Green };
			var bottomLeft = new BoxView { Color = Color.Blue };

			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
					new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
				},
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
				}
			};

			grid.Children.Add(topLeft, 0, 0);
			grid.Children.Add(topRight, 1, 0);
			grid.Children.Add(bottomLeft, 0, 1);

			using (var host = GtkTestHost.HostPage(new ContentPage { Content = grid }))
			{
				var tl = ((Gtk.Widget)Platform.GetRenderer(topLeft)).Allocation;
				var tr = ((Gtk.Widget)Platform.GetRenderer(topRight)).Allocation;
				var bl = ((Gtk.Widget)Platform.GetRenderer(bottomLeft)).Allocation;

				Assert.True(tr.X > tl.X, $"column 1 (x={tr.X}) must sit right of column 0 (x={tl.X})");
				Assert.True(bl.Y > tl.Y, $"row 1 (y={bl.Y}) must sit below row 0 (y={tl.Y})");
				Assert.True(Math.Abs(tr.Y - tl.Y) <= 1, $"same row, same y: {tr.Y} vs {tl.Y}");
				Assert.True(Math.Abs(bl.X - tl.X) <= 1, $"same column, same x: {bl.X} vs {tl.X}");
			}
		}

		/// <summary>
		/// Nested layouts are where M3 root cause 1 actually bit: a Forms layout pass does not
		/// queue a GTK resize, so a child added below an already-allocated parent kept its
		/// natural size forever.
		/// </summary>
		[Fact]
		public void NestedLayoutsPropagateGeometryToTheDeepestChild()
		{
			var leaf = new BoxView { Color = Color.Purple };

			var page = new ContentPage
			{
				Content = new StackLayout
				{
					Padding = new Thickness(10),
					Children =
					{
						new Grid
						{
							Children =
							{
								new StackLayout { Children = { leaf } }
							}
						}
					}
				}
			};

			using (var host = GtkTestHost.HostPage(page, 600, 400))
			{
				var widget = (Gtk.Widget)Platform.GetRenderer(leaf);

				Assert.True(widget.Allocation.Width > 100,
					$"leaf allocated {widget.Allocation.Width}px wide inside a 600px window - " +
					"it is still at its natural size, i.e. geometry stopped propagating");
			}
		}

		/// <summary>
		/// M3 root cause 4: rows appended after the first allocation were never allocated at all
		/// (they sat at GTK's -1,-1 1x1 sentinel while reporting Visible == true). ListView was
		/// the visible casualty, so it is the shape asserted here.
		/// </summary>
		[Fact]
		public void ListViewRowsAddedAfterTheFirstAllocationAreStillAllocated()
		{
			var items = Enumerable.Range(1, 8).Select(i => $"row {i}").ToList();

			var listView = new ListView
			{
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var cell = new TextCell();
					cell.SetBinding(TextCell.TextProperty, ".");
					return cell;
				})
			};

			using (var host = GtkTestHost.HostPage(new ContentPage { Content = listView }))
			{
				// The rows are appended by an idle loader, so give it several rounds.
				host.Pump(12);

				var renderer = (Gtk.Widget)Platform.GetRenderer(listView);
				var labels = GtkTestHost.Find<Gtk.Label>(renderer)
					.Where(l => l.Text != null && l.Text.StartsWith("row "))
					.ToList();

				Assert.True(labels.Count >= 2,
					$"fewer than two rows were realized at all ({labels.Count})");

				var unallocated = labels
					.Where(GtkTestHost.IsUnallocated)
					.Select(l => l.Text)
					.ToList();

				Assert.True(unallocated.Count == 0,
					$"rows left at GTK's unallocated sentinel: {string.Join(", ", unallocated)}");
			}
		}

		[Fact]
		public void ResizingTheWindowRelaysTheContent()
		{
			var box = new BoxView { Color = Color.Red };
			var page = new ContentPage { Content = new StackLayout { Children = { box } } };

			using (var host = GtkTestHost.HostPage(page, 400, 300))
			{
				var before = ((Gtk.Widget)Platform.GetRenderer(box)).Allocation.Width;

				host.Window.Resize(900, 600);
				Platform.GetRenderer(page)?.SetElementSize(new Size(900, 600));
				host.Pump(10);

				var after = ((Gtk.Widget)Platform.GetRenderer(box)).Allocation.Width;

				Assert.True(after > before,
					$"content did not follow the window: {before}px -> {after}px");
			}
		}
	}
}
