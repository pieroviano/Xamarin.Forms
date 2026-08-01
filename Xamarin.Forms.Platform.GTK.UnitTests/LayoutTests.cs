using System.Linq;
using NUnit.Framework;
using Xamarin.Forms;

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
	[TestFixture]
	public class LayoutTests
	{
		[Test]
		public void PageFillsTheWindow()
		{
			var page = new ContentPage
			{
				Content = new BoxView { Color = Color.Red }
			};

			using (var host = GtkTestHost.HostPage(page, 800, 600))
			{
				Assert.That(page.Width, Is.GreaterThan(0), "the page never received a size");
				Assert.That(page.Height, Is.GreaterThan(0));

				var renderer = Platform.GetRenderer(page);
				var widget = (Gtk.Widget)renderer;

				Assert.That(widget.Allocation.Width, Is.GreaterThan(1),
					"the page widget is still at GTK's unallocated sentinel");
			}
		}

		/// <summary>
		/// The M3 headline: a StackLayout's children must be allocated at distinct positions.
		/// When geometry never reached GTK, every child stacked at (0,0) at its natural size
		/// while Forms' own bounds were perfect.
		/// </summary>
		[Test]
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

				Assert.That(ys.Distinct().Count(), Is.EqualTo(3),
					$"children stacked on top of each other at y = [{string.Join(", ", ys)}]");

				Assert.That(ys[0], Is.LessThan(ys[1]));
				Assert.That(ys[1], Is.LessThan(ys[2]));
			}
		}

		[Test]
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
				Assert.Multiple(() =>
				{
					Assert.That(widget.Allocation.Width, Is.EqualTo((int)box.Width).Within(1),
						"native width disagrees with Forms - geometry did not reach GTK");
					Assert.That(widget.Allocation.Height, Is.EqualTo((int)box.Height).Within(1));
				});
			}
		}

		[Test]
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

				Assert.Multiple(() =>
				{
					Assert.That(tr.X, Is.GreaterThan(tl.X), "column 1 must sit right of column 0");
					Assert.That(bl.Y, Is.GreaterThan(tl.Y), "row 1 must sit below row 0");
					Assert.That(tr.Y, Is.EqualTo(tl.Y).Within(1), "same row, same y");
					Assert.That(bl.X, Is.EqualTo(tl.X).Within(1), "same column, same x");
				});
			}
		}

		/// <summary>
		/// Nested layouts are where M3 root cause 1 actually bit: a Forms layout pass does not
		/// queue a GTK resize, so a child added below an already-allocated parent kept its
		/// natural size forever.
		/// </summary>
		[Test]
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

				Assert.That(widget.Allocation.Width, Is.GreaterThan(100),
					$"leaf allocated {widget.Allocation.Width}px wide inside a 600px window - " +
					"it is still at its natural size, i.e. geometry stopped propagating");
			}
		}

		/// <summary>
		/// M3 root cause 4: rows appended after the first allocation were never allocated at all
		/// (they sat at GTK's -1,-1 1x1 sentinel while reporting Visible == true). ListView was
		/// the visible casualty, so it is the shape asserted here.
		/// </summary>
		[Test]
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

				Assert.That(labels.Count, Is.GreaterThanOrEqualTo(2),
					"fewer than two rows were realized at all");

				var unallocated = labels
					.Where(l => l.Allocation.Width <= 1 && l.Allocation.Height <= 1)
					.Select(l => l.Text)
					.ToList();

				Assert.That(unallocated, Is.Empty,
					$"rows left at GTK's unallocated sentinel: {string.Join(", ", unallocated)}");
			}
		}

		/// <summary>
		/// The M3 residual, reduced to a unit test: the ControlGallery's CoreRootPage
		/// (Xamarin.Forms.Controls/CoreGallery.cs:645) is an AbsoluteLayout with three
		/// proportional children that tile the page, and on screen the buttons painted on top of
		/// the ListView rows.
		///
		/// MEASURED cause (scratchpad/overlap.log): every size REQUEST was right - 724x244 for a
		/// child whose Forms bounds were 724x244 - while the ALLOCATION was the natural 257x80.
		/// A resize lost during an allocation is never re-queued, because gtk_widget_set_size_request
		/// will not queue one for an unchanged value and Forms raises BatchCommitted only when
		/// bounds CHANGE, so a settled page never calls the geometry push again. Hence the verify
		/// pass in VisualElementRenderer/AbstractPageRenderer.
		///
		/// Asserting on allocations, not requests, is the whole point: the requests were correct
		/// throughout the outage.
		/// </summary>
		[Test]
		public void AbsoluteLayoutProportionalChildrenAreAllocatedWhereFormsPutThem()
		{
			// Mirrors CoreRootPage as closely as a unit test can: two ListViews whose rows are
			// appended by an idle loader (so their natural size is small and arrives late) with a
			// button stack between them, and the whole thing inside NavigationPage -> FlyoutPage,
			// which is the nesting the gallery actually runs.
			ListView MakeList() => new ListView
			{
				ItemsSource = Enumerable.Range(1, 12).Select(i => $"row {i}").ToList(),
				ItemTemplate = new DataTemplate(() =>
				{
					var cell = new TextCell();
					cell.SetBinding(TextCell.TextProperty, ".");
					return cell;
				})
			};

			var top = MakeList();
			var middle = new StackLayout
			{
				Children = { new Button { Text = "Go to Test Cases" }, new SearchBar(), new Button { Text = "Click to Force GC" } }
			};
			var bottom = MakeList();

			var abs = new AbsoluteLayout();
			abs.Children.Add(top, new Rectangle(0, 0.0, 1, 0.35), AbsoluteLayoutFlags.All);
			abs.Children.Add(middle, new Rectangle(0, 0.5, 1, 0.30), AbsoluteLayoutFlags.All);
			abs.Children.Add(bottom, new Rectangle(0, 1.0, 1, 0.35), AbsoluteLayoutFlags.All);

			var detail = new NavigationPage(new ContentPage { Title = "Gallery", Content = abs });
			var flyout = new FlyoutPage
			{
				Flyout = new ContentPage { Title = "Flyout", Content = new Label { Text = "flyout" } },
				Detail = detail
			};

			using (var host = GtkTestHost.HostPage(flyout, 1024, 768))
			{
				host.Pump(20);

				var children = new View[] { top, middle, bottom };

				foreach (var child in children)
				{
					var widget = (Gtk.Widget)Platform.GetRenderer(child);

					Assert.That(widget.Allocation.Width, Is.EqualTo((int)child.Width).Within(2),
						$"{child.GetType().Name}: Forms width {child.Width:0}, " +
						$"request {widget.WidthRequest}, allocation {widget.Allocation.Width}");

					Assert.That(widget.Allocation.Height, Is.EqualTo((int)child.Height).Within(2),
						$"{child.GetType().Name}: Forms height {child.Height:0}, " +
						$"request {widget.HeightRequest}, allocation {widget.Allocation.Height}");
				}

				// And the symptom itself: the three must not paint over one another.
				var rects = children
					.Select(c => ((Gtk.Widget)Platform.GetRenderer(c)).Allocation)
					.OrderBy(r => r.Y)
					.ToList();

				for (int i = 0; i + 1 < rects.Count; i++)
				{
					Assert.That(rects[i].Y + rects[i].Height, Is.LessThanOrEqualTo(rects[i + 1].Y + 2),
						$"child {i} [{rects[i].Y}..{rects[i].Y + rects[i].Height}] overlaps " +
						$"child {i + 1} [{rects[i + 1].Y}..]");
				}
			}
		}

		[Test]
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

				Assert.That(after, Is.GreaterThan(before),
					$"content did not follow the window: {before}px -> {after}px");
			}
		}
	}
}
