using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// The incremental-loading surface of <see cref="CollectionView"/>: the <c>Scrolled</c> event,
	/// <c>RemainingItemsThreshold</c>/<c>RemainingItemsThresholdReached</c> and
	/// <c>ItemsUpdatingScrollMode</c> - the last row of the §8.2 gap table that was implementable
	/// (reordering needs a Core type this fork does not have).
	///
	/// These assert on the *events the application sees* plus the native
	/// <see cref="Gtk.Adjustment"/>, never on renderer internals: an incremental-loading contract
	/// that is right in the renderer's own bookkeeping and wrong at the event boundary is exactly
	/// the failure an app would hit, and it is invisible to a widget-tree assertion.
	/// </summary>
	public class CollectionViewIncrementalTests : GtkTestBase
	{
		const int RowHeight = 40;
		const int PageWidth = 600;
		const int PageHeight = 500;

		static DataTemplate LabelTemplate() => new DataTemplate(() =>
		{
			var label = new Label { HeightRequest = RowHeight };
			label.SetBinding(Label.TextProperty, ".");

			return label;
		});

		static ContentPage PageWith(View view)
		{
			view.HorizontalOptions = LayoutOptions.FillAndExpand;
			view.VerticalOptions = LayoutOptions.FillAndExpand;

			return new ContentPage { Content = new StackLayout { Children = { view } } };
		}

		static Gtk.ScrolledWindow ScrolledWindowOf(GtkTestHost.PageHost host)
		{
			var scrolled = GtkTestHost.Find<Gtk.ScrolledWindow>(host.Window).FirstOrDefault();

			Assert.True(scrolled != null, "the CollectionView renderer built no Gtk.ScrolledWindow");

			return scrolled;
		}

		/// <summary>
		/// Scrolls by setting the adjustment, which is what a real scroll ultimately does, and pumps
		/// enough rounds for the deferred layout pass (geometry is only ever mutated from an idle
		/// callback - plan M3 root cause 2).
		/// </summary>
		static void ScrollTo(GtkTestHost.PageHost host, Gtk.ScrolledWindow scrolled, double value)
		{
			scrolled.Vadjustment.Value = value;
			host.Pump(12);
		}

		[Fact]
		public void ScrolledReportsOffsetsDeltasAndTheVisibleItemRange()
		{
			Run(() =>
			{
					var collectionView = new CollectionView
					{
						ItemsSource = Enumerable.Range(1, 60).Select(i => $"item {i}").ToList(),
						ItemTemplate = LabelTemplate()
					};

					var events = new List<ItemsViewScrolledEventArgs>();
					collectionView.Scrolled += (s, e) => events.Add(e);

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(12);

						var scrolled = ScrolledWindowOf(host);
						events.Clear();

						ScrollTo(host, scrolled, 400);

						Assert.True(events.Count > 0, "scrolling raised no Scrolled event at all");

						var last = events[events.Count - 1];

						Assert.True(last.VerticalOffset > 0,
							$"VerticalOffset must report where the list is: {last.VerticalOffset}");
						Assert.True(last.VerticalDelta > 0,
							$"scrolling down must report a positive VerticalDelta: {last.VerticalDelta}");
						Assert.True(last.FirstVisibleItemIndex > 0,
							"400px down a 40px-row list, item 0 is no longer the first visible one " +
							$"(got {last.FirstVisibleItemIndex})");
						Assert.True(last.LastVisibleItemIndex >= last.FirstVisibleItemIndex,
							$"the visible range is inverted: {last.FirstVisibleItemIndex}..{last.LastVisibleItemIndex}");
						Assert.True(last.LastVisibleItemIndex < 60,
							$"LastVisibleItemIndex ran past the source: {last.LastVisibleItemIndex}");
						Assert.True(last.CenterItemIndex >= last.FirstVisibleItemIndex &&
							last.CenterItemIndex <= last.LastVisibleItemIndex,
							$"CenterItemIndex {last.CenterItemIndex} is outside " +
							$"{last.FirstVisibleItemIndex}..{last.LastVisibleItemIndex}");

						// Deltas are between consecutive events, not since the beginning of time.
						ScrollTo(host, scrolled, 0);

						var back = events[events.Count - 1];

						Assert.True(back.VerticalDelta < 0,
							$"scrolling back up must report a negative delta: {back.VerticalDelta}");
						Assert.Equal(0, back.FirstVisibleItemIndex);
					}
		
			});
		}

		[Fact]
		public void RemainingItemsThresholdFiresOnceAtTheTailRatherThanPerScrollStep()
		{
			Run(() =>
			{
					var collectionView = new CollectionView
					{
						ItemsSource = Enumerable.Range(1, 60).Select(i => $"item {i}").ToList(),
						ItemTemplate = LabelTemplate(),
						RemainingItemsThreshold = 5
					};

					var fired = 0;
					collectionView.RemainingItemsThresholdReached += (s, e) => fired++;

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(12);

						var scrolled = ScrolledWindowOf(host);

						Assert.Equal(0, fired);

						ScrollTo(host, scrolled, 400);

						Assert.True(fired == 0,
							$"the threshold fired {fired} times in the middle of a 60-item list");

						// To the end, in several steps: each one changes the adjustment, and only the first
						// arrival at the tail may raise the event.
						ScrollTo(host, scrolled, scrolled.Vadjustment.Upper);
						var atTail = fired;

						ScrollTo(host, scrolled, scrolled.Vadjustment.Upper - 5);
						ScrollTo(host, scrolled, scrolled.Vadjustment.Upper);

						Assert.True(atTail == 1,
							$"reaching the tail must raise RemainingItemsThresholdReached exactly once, not {atTail}");
						Assert.True(fired == 1,
							$"further scrolling at the tail re-raised it: {fired} times in total");
					}
		
			});
		}

		[Fact]
		public void RemainingItemsThresholdFiresAgainOnceTheSourceHasGrown()
		{
			Run(() =>
			{
					var source = new ObservableCollection<string>(
						Enumerable.Range(1, 40).Select(i => $"item {i}"));

					var collectionView = new CollectionView
					{
						ItemsSource = source,
						ItemTemplate = LabelTemplate(),
						RemainingItemsThreshold = 3
					};

					var fired = 0;
					collectionView.RemainingItemsThresholdReached += (s, e) => fired++;

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(12);

						var scrolled = ScrolledWindowOf(host);

						ScrollTo(host, scrolled, scrolled.Vadjustment.Upper);

						Assert.True(fired == 1, $"expected one event at the tail, got {fired}");

						// This is what the app does in the handler: load the next page.
						for (var i = 41; i <= 80; i++)
							source.Add($"item {i}");

						host.Pump(12);

						Assert.True(fired == 1,
							$"loading more items must NOT re-raise the event on its own (got {fired}) - " +
							"the tail is 40 items away again");

						ScrollTo(host, scrolled, scrolled.Vadjustment.Upper);

						Assert.True(fired == 2,
							$"reaching the NEW tail must raise it again: fired {fired} times in total");
					}
		
			});
		}

		[Fact]
		public void RemainingItemsThresholdOfMinusOneNeverFires()
		{
			Run(() =>
			{
					var collectionView = new CollectionView
					{
						ItemsSource = Enumerable.Range(1, 40).Select(i => $"item {i}").ToList(),
						ItemTemplate = LabelTemplate()
					};

					// The default, spelled out: -1 is "never", not "a threshold of zero items left".
					Assert.Equal(-1, collectionView.RemainingItemsThreshold);

					var fired = 0;
					collectionView.RemainingItemsThresholdReached += (s, e) => fired++;

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(12);

						var scrolled = ScrolledWindowOf(host);
						ScrollTo(host, scrolled, scrolled.Vadjustment.Upper);

						Assert.True(fired == 0, $"a threshold of -1 fired {fired} times");
					}
		
			});
		}

		/// <summary>
		/// The first page of an incrementally-loaded list is usually short enough to fit the screen,
		/// so the threshold has to be reachable without any scrolling at all - otherwise such a list
		/// loads its first page and stops for ever.
		/// </summary>
		[Fact]
		public void AListShortEnoughToFitReachesTheThresholdWithoutScrolling()
		{
			Run(() =>
			{
					var collectionView = new CollectionView
					{
						ItemsSource = Enumerable.Range(1, 3).Select(i => $"item {i}").ToList(),
						ItemTemplate = LabelTemplate(),
						RemainingItemsThreshold = 5
					};

					var fired = 0;
					collectionView.RemainingItemsThresholdReached += (s, e) => fired++;

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(16);

						Assert.True(fired == 1,
							$"a 3-item list under a threshold of 5 must raise the event once on load, not {fired}");
					}
		
			});
		}

		[Fact]
		public void KeepScrollOffsetLeavesTheOffsetWhereItIsWhenItemsAreInsertedAbove()
		{
			Run(() =>
			{
					var source = new ObservableCollection<string>(
						Enumerable.Range(1, 60).Select(i => $"item {i}"));

					var collectionView = new CollectionView
					{
						ItemsSource = source,
						ItemTemplate = LabelTemplate(),
						ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepScrollOffset
					};

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(12);

						var scrolled = ScrolledWindowOf(host);
						ScrollTo(host, scrolled, 400);

						var before = scrolled.Vadjustment.Value;

						source.Insert(0, "inserted");
						host.Pump(12);

						Assert.True(System.Math.Abs(scrolled.Vadjustment.Value - before) < 2,
							"KeepScrollOffset must leave the numeric offset alone: " +
							$"{before} -> {scrolled.Vadjustment.Value}");
					}
		
			});
		}

		/// <summary>
		/// The default mode, and the interesting one: inserting above the viewport must move the
		/// offset by the inserted extent, so the item the user was looking at stays put.
		/// </summary>
		[Fact]
		public void KeepItemsInViewHoldsTheTopItemStillWhenItemsAreInsertedAbove()
		{
			Run(() =>
			{
					var source = new ObservableCollection<string>(
						Enumerable.Range(1, 60).Select(i => $"item {i}"));

					var collectionView = new CollectionView
					{
						ItemsSource = source,
						ItemTemplate = LabelTemplate()
					};

					// Spelled out rather than assumed: KeepItemsInView is the Core default.
					Assert.Equal(ItemsUpdatingScrollMode.KeepItemsInView, collectionView.ItemsUpdatingScrollMode);

					var firstVisible = -1;
					collectionView.Scrolled += (s, e) => firstVisible = e.FirstVisibleItemIndex;

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(12);

						var scrolled = ScrolledWindowOf(host);
						ScrollTo(host, scrolled, 400);

						var before = scrolled.Vadjustment.Value;
						var topItemBefore = firstVisible;

						Assert.True(topItemBefore > 0, $"the list did not scroll (top item {topItemBefore})");

						source.Insert(0, "inserted");
						host.Pump(16);

						Assert.True(scrolled.Vadjustment.Value > before + RowHeight / 2,
							"KeepItemsInView must push the offset down by the inserted row's extent: " +
							$"{before} -> {scrolled.Vadjustment.Value}");

						// The item that was at the top is one index further along, and still at the top.
						Assert.True(firstVisible == topItemBefore + 1,
							$"the top item moved: was item index {topItemBefore}, now {firstVisible} " +
							"(one insertion above it should leave the same *item* on screen)");
					}
		
			});
		}

		[Fact]
		public void KeepLastItemInViewScrollsToTheNewTail()
		{
			Run(() =>
			{
					var source = new ObservableCollection<string>(
						Enumerable.Range(1, 30).Select(i => $"item {i}"));

					var collectionView = new CollectionView
					{
						ItemsSource = source,
						ItemTemplate = LabelTemplate(),
						ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView
					};

					var lastVisible = -1;
					collectionView.Scrolled += (s, e) => lastVisible = e.LastVisibleItemIndex;

					using (var host = GtkTestHost.HostPage(PageWith(collectionView), PageWidth, PageHeight))
					{
						host.Pump(12);

						var scrolled = ScrolledWindowOf(host);
						var before = scrolled.Vadjustment.Value;

						source.Add("item 31");
						host.Pump(16);

						Assert.True(scrolled.Vadjustment.Value > before,
							"KeepLastItemInView must scroll towards the new last item: " +
							$"{before} -> {scrolled.Vadjustment.Value}");
						Assert.True(lastVisible == source.Count - 1,
							$"the new last item ({source.Count - 1}) is not the last visible one ({lastVisible})");
					}
		
			});
		}
	}
}
