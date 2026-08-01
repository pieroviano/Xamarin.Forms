using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Renderers;
using Xunit;
using FormsImageButton = Xamarin.Forms.ImageButton;
using GtkImageButton = Xamarin.Forms.Platform.GTK.Controls.ImageButton;
using LineShape = Xamarin.Forms.Shapes.Line;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// The M5 controls (plan §8.1/§8.2): CheckBox, ImageButton, Line, RefreshView, IndicatorView,
	/// SwipeView, CarouselView, CollectionView.
	///
	/// These are asserted at the ALLOCATION level on purpose. Every one of them was originally
	/// verified with a smoke app plus a screenshot precisely because "the property round-trips" was
	/// not enough twice over in this port: the RefreshView spinner reported Visible == true, was
	/// mapped, was correctly allocated at [184,52 32x32] and was still invisible (it had no
	/// GdkWindow of its own), and the SwipeView items had the right Forms colour set and rendered
	/// theme-grey. So the assertions here are on <c>Widget.Allocation</c>, on which widget owns a
	/// window, and on the native widget tree - the things that were actually wrong - rather than on
	/// the Forms element, which was right throughout.
	/// </summary>
	public class M5ControlTests : GtkTestBase
	{
		// ---- CheckBox --------------------------------------------------------------------

		[Fact]
		public void CheckBoxMapsIsCheckedBothWays()
		{
			var checkbox = new CheckBox { IsChecked = true };

			using (var host = GtkTestHost.HostView(checkbox))
			{
				var native = host.Control<Gtk.CheckButton>();
				Assert.True(native.Active, "initial IsChecked did not reach the native widget");

				checkbox.IsChecked = false;
				host.Pump();
				Assert.False(native.Active, "Forms -> native");

				native.Active = true;
				host.Pump();
				Assert.True(checkbox.IsChecked, "native -> Forms");
			}
		}

		[Fact]
		public void CheckBoxIsAllocatedInsideALayout()
		{
			var checkbox = new CheckBox { IsChecked = true, HorizontalOptions = LayoutOptions.Start };

			using (var host = GtkTestHost.HostPage(PageWith(checkbox), 500, 400))
			{
				var widget = (Gtk.Widget)Platform.GetRenderer(checkbox);

				Assert.False(GtkTestHost.IsUnallocated(widget),
					$"CheckBox never got a real allocation: {GtkTestHost.Describe(widget)}");

				AssertAgreesWithForms(checkbox, widget);
			}
		}

		// ---- ImageButton -----------------------------------------------------------------

		[Fact]
		public void ImageButtonUsesItsOwnRendererAndIsAllocated()
		{
			var button = new FormsImageButton { HeightRequest = 48, WidthRequest = 64 };

			using (var host = GtkTestHost.HostPage(PageWith(button), 500, 400))
			{
				var renderer = Platform.GetRenderer(button);

				Assert.True(renderer is ImageButtonRenderer,
					$"ImageButton resolved to {renderer?.GetType().Name}, not ImageButtonRenderer - " +
					"it is falling back to some other renderer");

				var widget = (Gtk.Widget)renderer;

				Assert.False(GtkTestHost.IsUnallocated(widget),
					$"ImageButton never got a real allocation: {GtkTestHost.Describe(widget)}");

				AssertAgreesWithForms(button, widget);

				var native = GtkTestHost.Find<GtkImageButton>(widget).FirstOrDefault();
				Assert.True(native != null, "no native ImageButton control under the renderer");
			}
		}

		[Fact]
		public void ImageButtonClickedIsRaisedFromTheNativeClick()
		{
			var button = new FormsImageButton();
			var clicked = 0;
			button.Clicked += (s, e) => clicked++;

			using (var host = GtkTestHost.HostView(button))
			{
				var native = host.Control<GtkImageButton>();

				GLib.Signal.Emit(native, "clicked");
				host.Pump();

				Assert.True(clicked == 1,
					$"the native click must reach ImageButton.Clicked (raised {clicked} times)");
			}
		}

		// ---- Line (Shapes) ---------------------------------------------------------------

		[Fact]
		public void LineIsAllocatedToItsFormsBounds()
		{
			var line = new LineShape
			{
				X1 = 0,
				Y1 = 0,
				X2 = 180,
				Y2 = 60,
				Stroke = Brush.Red,
				StrokeThickness = 4,
				WidthRequest = 200,
				HeightRequest = 80,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Start
			};

			using (var host = GtkTestHost.HostPage(PageWith(line), 600, 400))
			{
				var renderer = Platform.GetRenderer(line);

				Assert.True(renderer is LineRenderer,
					$"Line resolved to {renderer?.GetType().Name}, not LineRenderer");

				var widget = (Gtk.Widget)renderer;

				Assert.False(GtkTestHost.IsUnallocated(widget),
					$"Line never got a real allocation: {GtkTestHost.Describe(widget)}");

				Assert.True(Math.Abs(widget.Allocation.Width - 200) <= 2,
					$"Line allocated {widget.Allocation.Width}px wide, expected ~200");
				Assert.True(Math.Abs(widget.Allocation.Height - 80) <= 2,
					$"Line allocated {widget.Allocation.Height}px tall, expected ~80");

				Assert.True(GtkTestHost.Find<LineView>(widget).Count == 1,
					"the renderer did not create exactly one native LineView");
			}
		}

		[Fact]
		public void LineGeometryChangesAreAcceptedAfterAllocation()
		{
			var line = new LineShape
			{
				X1 = 0,
				Y1 = 0,
				X2 = 10,
				Y2 = 10,
				Stroke = Brush.Blue,
				StrokeThickness = 2,
				WidthRequest = 150,
				HeightRequest = 150
			};

			using (var host = GtkTestHost.HostPage(PageWith(line), 400, 400))
			{
				var widget = (Gtk.Widget)Platform.GetRenderer(line);
				var before = widget.Allocation;

				line.X2 = 140;
				line.Y2 = 140;
				host.Pump();

				// The point is that a post-allocation geometry change neither throws nor wedges
				// the subtree: M3 root cause 2 was that mutating geometry at the wrong moment made
				// GTK discard the resize and leave the widget stuck.
				Assert.False(GtkTestHost.IsUnallocated(widget),
					$"Line lost its allocation after a geometry change: {GtkTestHost.Describe(widget)}");
				Assert.True(widget.Allocation.Width == before.Width,
					$"Line width changed from {before.Width} to {widget.Allocation.Width} " +
					"for a change that only moves the end point");
			}
		}

		// ---- RefreshView -----------------------------------------------------------------

		[Fact]
		public void RefreshViewLaysOutItsContent()
		{
			var content = new BoxView { Color = Color.Teal };
			var refreshView = new RefreshView
			{
				Content = new StackLayout { Children = { content } }
			};

			using (var host = GtkTestHost.HostPage(PageWith(refreshView, fill: true), 600, 400))
			{
				var widget = (Gtk.Widget)Platform.GetRenderer(content);

				Assert.False(GtkTestHost.IsUnallocated(widget),
					$"RefreshView content was never allocated: {GtkTestHost.Describe(widget)}");

				Assert.True(widget.Allocation.Width > 300,
					$"RefreshView content allocated {widget.Allocation.Width}px wide inside a " +
					"600px page - the RefreshView is not laying its content out");
			}
		}

		/// <summary>
		/// The spinner is the whole reason RefreshView needed a screenshot: it reported
		/// <c>Visible == true</c>, was mapped and was correctly allocated while being completely
		/// invisible, because a no-window widget paints into its parent's window and lands under
		/// any sibling that owns one. The fix was to host it in a
		/// <c>Gtk.EventBox { VisibleWindow = true }</c>. So this asserts the host window exists,
		/// not merely that the spinner is "visible".
		/// </summary>
		[Fact]
		public void RefreshViewSpinnerIsShownHiddenAndOwnsItsWindow()
		{
			var refreshView = new RefreshView
			{
				Content = new StackLayout { Children = { new BoxView { Color = Color.Teal } } }
			};

			using (var host = GtkTestHost.HostPage(PageWith(refreshView, fill: true), 600, 400))
			{
				var renderer = (Gtk.Widget)Platform.GetRenderer(refreshView);

				var spinner = GtkTestHost.Find<Gtk.Spinner>(renderer).FirstOrDefault();
				Assert.True(spinner != null, "RefreshViewRenderer created no Gtk.Spinner");

				var spinnerHost = spinner.Parent as Gtk.EventBox;
				Assert.True(spinnerHost != null,
					$"the spinner's parent is {spinner.Parent?.GetType().Name}, not a Gtk.EventBox - " +
					"without its own GdkWindow it paints under the content and is invisible");
				Assert.True(spinnerHost.VisibleWindow,
					"the spinner host EventBox has VisibleWindow = false, so it owns no GdkWindow " +
					"and the spinner is drawn underneath the content");

				Assert.False(spinnerHost.Visible, "the spinner must be hidden while not refreshing");

				refreshView.IsRefreshing = true;
				host.Pump();

				Assert.True(spinnerHost.Visible, "IsRefreshing = true did not show the spinner");
				Assert.True(spinner.Active, "the spinner was shown but is not animating");
				Assert.False(GtkTestHost.IsUnallocated(spinnerHost),
					$"the spinner host was shown but never allocated: {GtkTestHost.Describe(spinnerHost)}");

				refreshView.IsRefreshing = false;
				host.Pump();

				Assert.False(spinnerHost.Visible, "IsRefreshing = false did not hide the spinner");
			}
		}

		// ---- IndicatorView ---------------------------------------------------------------
		//
		// MEASURED, and it invalidates the obvious test: the IndicatorView's ALLOCATION is not the
		// renderer's to decide. Xamarin.Forms.Core/IndicatorView.cs:112 overrides OnMeasure and,
		// whenever IndicatorTemplate is null, returns
		//
		//     new SizeRequest(new Size(Count * (IndicatorSize + 4 + 4 + 1), IndicatorSize), ...)
		//
		// - ignoring both MaximumVisible and the platform renderer's GetDesiredSize entirely. Probed
		// on this tree: Count=3/size=10 allocated 57px (3 x 19) against a renderer request of 42,
		// and Count=20 allocated 380px (20 x 19) whether MaximumVisible was 4 (request 58) or unset
		// (request 314). That is upstream Core behaviour every backend shares, not a GTK defect.
		//
		// So an allocation-width assertion here passes for the wrong reason - it tracks Count
		// through Core's OnMeasure and would keep passing with the renderer's capping deleted. What
		// the GTK renderer actually decides is the control's SIZE REQUEST and, from the same
		// numbers, how many dots OnDrawn paints; those are what is asserted, plus the allocation
		// being real and large enough to hold the dots.

		const double DotSpacing = 6;   // IndicatorViewControl.DotSpacing

		static int ExpectedDotWidth(int count, double size) =>
			count <= 0 ? 0 : (int)Math.Ceiling(count * size + (count - 1) * DotSpacing);

		[Fact]
		public void IndicatorViewSizeRequestTracksTheDotCount()
		{
			var indicator = new IndicatorView
			{
				Count = 3,
				Position = 0,
				IndicatorSize = 10,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Start
			};

			using (var host = GtkTestHost.HostPage(PageWith(indicator), 600, 400))
			{
				var renderer = (Gtk.Widget)Platform.GetRenderer(indicator);
				var control = GtkTestHost.Find<IndicatorViewControl>(renderer).SingleOrDefault();

				Assert.True(control != null, "the renderer did not create an IndicatorViewControl");

				Assert.False(GtkTestHost.IsUnallocated(control),
					$"IndicatorView was never allocated: {GtkTestHost.Describe(control)}");

				control.GetSizeRequest(out var width, out var height);

				Assert.True(width == ExpectedDotWidth(3, 10),
					$"3 x 10px dots + 6px spacing should request {ExpectedDotWidth(3, 10)}px, got {width}");
				Assert.True(height == 10, $"the dot row should be 10px tall, got {height}");

				Assert.True(control.Allocation.Width >= width,
					$"the control was allocated {control.Allocation.Width}px but needs {width}px " +
					"for its dots");

				indicator.Count = 7;
				host.Pump();

				control.GetSizeRequest(out var wider, out _);

				Assert.True(wider == ExpectedDotWidth(7, 10),
					$"Count 3 -> 7 should re-request {ExpectedDotWidth(7, 10)}px, got {wider}; " +
					"OnElementPropertyChanged is not re-measuring");
			}
		}

		[Fact]
		public void IndicatorViewMaximumVisibleCapsTheDotCount()
		{
			var indicator = new IndicatorView
			{
				Count = 20,
				MaximumVisible = 4,
				IndicatorSize = 10,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Start
			};

			using (var host = GtkTestHost.HostPage(PageWith(indicator), 600, 400))
			{
				var renderer = (Gtk.Widget)Platform.GetRenderer(indicator);
				var control = GtkTestHost.Find<IndicatorViewControl>(renderer).Single();

				control.GetSizeRequest(out var width, out _);

				Assert.True(width == ExpectedDotWidth(4, 10),
					$"MaximumVisible = 4 should draw 4 dots ({ExpectedDotWidth(4, 10)}px); the " +
					$"control asked for {width}px, and all 20 dots would be " +
					$"{ExpectedDotWidth(20, 10)}px");
			}
		}

		[Fact]
		public void IndicatorViewHideSingleSuppressesTheOnlyDot()
		{
			var indicator = new IndicatorView
			{
				Count = 1,
				HideSingle = true,
				IndicatorSize = 10,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Start
			};

			using (var host = GtkTestHost.HostPage(PageWith(indicator), 600, 400))
			{
				var control = GtkTestHost
					.Find<IndicatorViewControl>((Gtk.Widget)Platform.GetRenderer(indicator))
					.Single();

				control.GetSizeRequest(out var hidden, out _);

				// The renderer asks for 0px when it draws no dots; MEASURED, GTK reports that back
				// as 1px, so the bar is "no room for a dot" rather than a literal zero. One dot
				// would be 10px, so this still fails outright if HideSingle is ignored.
				Assert.True(hidden <= 1,
					$"HideSingle should draw no dots at all for Count = 1, but the control asked " +
					$"for {hidden}px (one dot would be {ExpectedDotWidth(1, 10)}px)");

				indicator.HideSingle = false;
				host.Pump();

				control.GetSizeRequest(out var shown, out _);

				Assert.True(shown == ExpectedDotWidth(1, 10),
					$"HideSingle = false should bring the single dot back " +
					$"({ExpectedDotWidth(1, 10)}px), got {shown}px");

				indicator.Count = 4;
				host.Pump();

				control.GetSizeRequest(out var four, out _);

				Assert.True(four == ExpectedDotWidth(4, 10),
					$"Count = 4 should request {ExpectedDotWidth(4, 10)}px, got {four}px");
			}
		}

		// ---- SwipeView -------------------------------------------------------------------

		/// <summary>
		/// <c>SwipeView.Open</c> is the scriptable half of the gesture, and it goes through exactly
		/// the same <c>TryBeginSwipe</c>/<c>OpenSwipe</c>/<c>ApplyOffset</c> path a real drag does -
		/// so this covers the geometry without synthesising X events.
		/// </summary>
		[Fact]
		public void SwipeViewOpenRevealsItsItemsAtTheExpectedExtent()
		{
			var swipeView = new SwipeView
			{
				Content = new StackLayout { Children = { new BoxView { Color = Color.Silver } } },
				RightItems = new SwipeItems
				{
					new SwipeItem { Text = "Delete", BackgroundColor = Color.Red }
				}
			};

			using (var host = GtkTestHost.HostPage(PageWith(swipeView, fill: true), 600, 400))
			{
				var renderer = (Gtk.Widget)Platform.GetRenderer(swipeView);

				swipeView.Open(OpenSwipeItem.RightItems);
				host.Pump();

				var item = GtkTestHost.Find<Gtk.Button>(renderer)
					.FirstOrDefault(b => b.Label == "Delete");

				Assert.True(item != null,
					"Open(RightItems) built no native button for the SwipeItem");

				var itemsHost = Ancestor<Gtk.EventBox>(item);

				Assert.True(itemsHost != null,
					"the swipe items are not inside a Gtk.EventBox, so they have no GdkWindow " +
					"of their own and are drawn underneath the SwipeView content");
				Assert.True(itemsHost.VisibleWindow,
					"the swipe items host has VisibleWindow = false and cannot be raised above " +
					"the content");
				Assert.True(itemsHost.Visible, "Open() did not show the swipe items host");

				// One visible item, no Threshold => DefaultItemExtent (80px), against the right edge.
				Assert.True(Math.Abs(itemsHost.Allocation.Width - 80) <= 2,
					$"swipe items allocated {itemsHost.Allocation.Width}px wide, expected ~80: " +
					GtkTestHost.Describe(itemsHost));

				var contentRight = renderer.Allocation.X + renderer.Allocation.Width;
				var itemsRight = itemsHost.Allocation.X + itemsHost.Allocation.Width;

				Assert.True(Math.Abs(itemsRight - contentRight) <= 2,
					$"RightItems must sit against the right edge: items end at {itemsRight}, " +
					$"content ends at {contentRight}");

				swipeView.Close();
				host.Pump();

				Assert.False(itemsHost.Visible, "Close() did not hide the swipe items host");
			}
		}

		[Fact]
		public void SwipeViewThresholdSetsTheRevealedExtent()
		{
			var swipeView = new SwipeView
			{
				Threshold = 140,
				Content = new StackLayout { Children = { new BoxView { Color = Color.Silver } } },
				LeftItems = new SwipeItems
				{
					new SwipeItem { Text = "Archive", BackgroundColor = Color.Blue }
				}
			};

			using (var host = GtkTestHost.HostPage(PageWith(swipeView, fill: true), 600, 400))
			{
				var renderer = (Gtk.Widget)Platform.GetRenderer(swipeView);

				swipeView.Open(OpenSwipeItem.LeftItems);
				host.Pump();

				var item = GtkTestHost.Find<Gtk.Button>(renderer)
					.FirstOrDefault(b => b.Label == "Archive");

				Assert.True(item != null, "Open(LeftItems) built no native button");

				var itemsHost = Ancestor<Gtk.EventBox>(item);
				Assert.True(itemsHost != null, "no EventBox host for the swipe items");

				Assert.True(Math.Abs(itemsHost.Allocation.Width - 140) <= 2,
					$"Threshold = 140 should set the revealed extent; got " +
					$"{itemsHost.Allocation.Width}px");

				Assert.True(Math.Abs(itemsHost.Allocation.X - renderer.Allocation.X) <= 2,
					$"LeftItems must sit against the left edge: items at x={itemsHost.Allocation.X}, " +
					$"content at x={renderer.Allocation.X}");
			}
		}

		// ---- CarouselView ----------------------------------------------------------------

		/// <summary>
		/// A templated item view is not a child of a Forms Layout, so nothing lays it out - the
		/// renderer has to run the layout pass itself (plan rule 7). Asserting on the native
		/// allocation is the only way to tell whether it did: the Forms element's bounds are set by
		/// the very call under test, so reading them back proves nothing.
		/// </summary>
		[Fact]
		public void CarouselViewLaysOutTheCurrentItemAtFullSize()
		{
			var carousel = new CarouselView
			{
				ItemsSource = new[] { "item 1", "item 2", "item 3" },
				ItemTemplate = LabelTemplate()
			};

			using (var host = GtkTestHost.HostPage(PageWith(carousel, fill: true), 600, 400))
			{
				host.Pump(10);

				var renderer = (Gtk.Widget)Platform.GetRenderer(carousel);
				var label = LabelWithText(renderer, "item 1");

				Assert.True(label != null,
					"the carousel rendered no label for its first item: " +
					Describe(GtkTestHost.Find<Gtk.Label>(renderer)));

				Assert.False(GtkTestHost.IsUnallocated(label),
					$"the carousel item was never allocated: {GtkTestHost.Describe(label)}");

				Assert.True(label.Allocation.Width > 300,
					$"the carousel item is {label.Allocation.Width}px wide inside a 600px page - " +
					"the renderer did not run view.Layout() on it");
			}
		}

		[Fact]
		public void CarouselViewPositionSwapsTheRenderedItem()
		{
			var carousel = new CarouselView
			{
				ItemsSource = new[] { "item 1", "item 2", "item 3" },
				ItemTemplate = LabelTemplate()
			};

			using (var host = GtkTestHost.HostPage(PageWith(carousel, fill: true), 600, 400))
			{
				host.Pump(10);

				var renderer = (Gtk.Widget)Platform.GetRenderer(carousel);
				Assert.True(LabelWithText(renderer, "item 1") != null, "precondition: item 1 shown");

				carousel.Position = 2;
				host.Pump(10);

				var third = LabelWithText(renderer, "item 3");

				Assert.True(third != null,
					"Position = 2 did not render the third item: " +
					Describe(GtkTestHost.Find<Gtk.Label>(renderer)));

				Assert.True(LabelWithText(renderer, "item 1") == null,
					"the previous item is still in the native tree after Position changed");

				Assert.True(third.Allocation.Width > 300,
					$"the swapped-in item is {third.Allocation.Width}px wide - it was rendered but " +
					"never laid out");

				Assert.Equal("item 3", carousel.CurrentItem);
			}
		}

		[Fact]
		public void CarouselViewFollowsItsObservableSource()
		{
			var source = new ObservableCollection<string> { "item 1" };

			var carousel = new CarouselView
			{
				ItemsSource = source,
				ItemTemplate = LabelTemplate()
			};

			using (var host = GtkTestHost.HostPage(PageWith(carousel, fill: true), 600, 400))
			{
				host.Pump(10);

				var renderer = (Gtk.Widget)Platform.GetRenderer(carousel);
				Assert.True(LabelWithText(renderer, "item 1") != null, "precondition");

				source.Add("item 2");
				carousel.Position = 1;
				host.Pump(10);

				Assert.True(LabelWithText(renderer, "item 2") != null,
					"an item appended to the ObservableCollection never rendered: " +
					Describe(GtkTestHost.Find<Gtk.Label>(renderer)));
			}
		}

		// ---- CollectionView --------------------------------------------------------------

		[Fact]
		public void CollectionViewAllocatesItemsAtDistinctPositions()
		{
			var collectionView = new CollectionView
			{
				ItemsSource = Enumerable.Range(1, 6).Select(i => $"item {i}").ToList(),
				ItemTemplate = LabelTemplate()
			};

			using (var host = GtkTestHost.HostPage(PageWith(collectionView, fill: true), 600, 500))
			{
				host.Pump(12);

				var renderer = (Gtk.Widget)Platform.GetRenderer(collectionView);
				var labels = ItemLabels(renderer);

				Assert.True(labels.Count >= 4,
					$"only {labels.Count} of 6 items were realized: {Describe(labels)}");

				var unallocated = labels.Where(GtkTestHost.IsUnallocated).Select(l => l.Text).ToList();
				Assert.True(unallocated.Count == 0,
					$"items left at GTK's unallocated sentinel: {string.Join(", ", unallocated)}");

				var ys = labels.Select(l => Root(l).Allocation.Y).ToList();
				Assert.True(ys.Distinct().Count() == ys.Count,
					$"a linear CollectionView stacked items on top of each other at y = " +
					$"[{string.Join(", ", ys)}]");

				Assert.True(ys.SequenceEqual(ys.OrderBy(y => y)),
					$"items are out of order: y = [{string.Join(", ", ys)}]");

				Assert.True(labels[0].Allocation.Width > 200,
					$"a linear item is {labels[0].Allocation.Width}px wide inside a 600px page - " +
					"it was never given the viewport width");
			}
		}

		[Fact]
		public void CollectionViewGridLayoutPlacesItemsInColumns()
		{
			var collectionView = new CollectionView
			{
				ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical),
				ItemsSource = Enumerable.Range(1, 6).Select(i => $"item {i}").ToList(),
				ItemTemplate = LabelTemplate()
			};

			using (var host = GtkTestHost.HostPage(PageWith(collectionView, fill: true), 600, 500))
			{
				host.Pump(12);

				var renderer = (Gtk.Widget)Platform.GetRenderer(collectionView);
				var labels = ItemLabels(renderer);

				Assert.True(labels.Count >= 4,
					$"only {labels.Count} of 6 items were realized: {Describe(labels)}");

				var first = Root(labels[0]).Allocation;
				var second = Root(labels[1]).Allocation;

				Assert.True(Math.Abs(first.Y - second.Y) <= 2,
					$"a 2-column grid must put items 1 and 2 on the same row: " +
					$"y = {first.Y} and {second.Y}");

				Assert.True(second.X > first.X,
					$"item 2 must sit to the right of item 1: x = {first.X} and {second.X}");

				var third = Root(labels[2]).Allocation;

				Assert.True(third.Y > first.Y,
					$"item 3 must wrap to the second row: y = {first.Y} then {third.Y}");
				Assert.True(Math.Abs(third.X - first.X) <= 2,
					$"item 3 must start a new row at column 0: x = {first.X} then {third.X}");

				// Two columns across 600px: each item gets roughly half, never the whole width.
				Assert.True(first.Width < 400,
					$"a 2-column item is {first.Width}px wide inside a 600px page - " +
					"the grid is laying out as a single column");
			}
		}

		[Fact]
		public void CollectionViewSelectionReachesTheElement()
		{
			var items = Enumerable.Range(1, 4).Select(i => $"item {i}").ToList();

			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				ItemsSource = items,
				ItemTemplate = LabelTemplate()
			};

			var changed = 0;
			collectionView.SelectionChanged += (s, e) => changed++;

			using (var host = GtkTestHost.HostPage(PageWith(collectionView, fill: true), 600, 500))
			{
				host.Pump(12);

				collectionView.SelectedItem = items[2];
				host.Pump();

				Assert.Equal(items[2], collectionView.SelectedItem);
				Assert.True(changed >= 1,
					$"SelectionChanged never fired (raised {changed} times)");
			}
		}

		[Fact]
		public void CollectionViewFollowsItsObservableSource()
		{
			var source = new ObservableCollection<string>(
				Enumerable.Range(1, 3).Select(i => $"item {i}"));

			var collectionView = new CollectionView
			{
				ItemsSource = source,
				ItemTemplate = LabelTemplate()
			};

			using (var host = GtkTestHost.HostPage(PageWith(collectionView, fill: true), 600, 500))
			{
				host.Pump(12);

				var renderer = (Gtk.Widget)Platform.GetRenderer(collectionView);
				var before = ItemLabels(renderer).Count;

				source.Add("item 4");
				host.Pump(12);

				var after = ItemLabels(renderer);

				Assert.True(after.Count == before + 1,
					$"appending one item took the realized count from {before} to {after.Count}");

				var appended = after.FirstOrDefault(l => l.Text == "item 4");

				Assert.True(appended != null,
					$"the appended item never rendered: {Describe(after)}");
				Assert.False(GtkTestHost.IsUnallocated(Root(appended)),
					"an item appended after the first allocation was never allocated - " +
					"M3 root cause 4, on CollectionView this time: " +
					GtkTestHost.Describe(Root(appended)));
			}
		}

		// ---- helpers ---------------------------------------------------------------------

		static ContentPage PageWith(View view, bool fill = false)
		{
			if (fill)
			{
				view.HorizontalOptions = LayoutOptions.FillAndExpand;
				view.VerticalOptions = LayoutOptions.FillAndExpand;
			}

			return new ContentPage { Content = new StackLayout { Children = { view } } };
		}

		static DataTemplate LabelTemplate() => new DataTemplate(() =>
		{
			var label = new Label { HeightRequest = 40 };
			label.SetBinding(Label.TextProperty, ".");
			return label;
		});

		static Gtk.Label LabelWithText(Gtk.Widget root, string text) =>
			GtkTestHost.Find<Gtk.Label>(root).FirstOrDefault(l => l.Text == text);

		static List<Gtk.Label> ItemLabels(Gtk.Widget root) =>
			GtkTestHost.Find<Gtk.Label>(root)
				.Where(l => l.Text != null && l.Text.StartsWith("item ", StringComparison.Ordinal))
				.ToList();

		static string Describe(IEnumerable<Gtk.Label> labels) =>
			"[" + string.Join(", ", labels.Select(l => $"\"{l.Text}\" {GtkTestHost.Describe(l)}")) + "]";

		/// <summary>
		/// The item's positioned widget: a CollectionView item label is nested inside the Forms
		/// renderer's container inside the host EventBox, and it is the OUTERMOST of those that
		/// carries the position the layout assigned.
		/// </summary>
		static Gtk.Widget Root(Gtk.Widget widget)
		{
			var current = widget;

			while (current.Parent != null && !(current.Parent is Gtk.Fixed) &&
				   !(current.Parent is Gtk.Box) && !(current.Parent is Gtk.Viewport))
			{
				current = current.Parent;
			}

			return current;
		}

		static T Ancestor<T>(Gtk.Widget widget) where T : Gtk.Widget
		{
			for (var current = widget?.Parent; current != null; current = current.Parent)
			{
				if (current is T match)
					return match;
			}

			return null;
		}

		static void AssertAgreesWithForms(View view, Gtk.Widget widget)
		{
			// One pixel of slack: Forms works in doubles, GTK in ints.
			Assert.True(Math.Abs(widget.Allocation.Width - (int)view.Width) <= 1,
				$"{view.GetType().Name} native width {widget.Allocation.Width} disagrees with " +
				$"Forms {(int)view.Width} - geometry did not reach GTK");
			Assert.True(Math.Abs(widget.Allocation.Height - (int)view.Height) <= 1,
				$"{view.GetType().Name} native height {widget.Allocation.Height} disagrees with " +
				$"Forms {(int)view.Height}");
		}
	}
}
