using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Threading.Tasks;


namespace Xamarin.Forms.Core.UnitTests
{
	public class ScrollViewUnitTests : BaseTestFixture
	{
		public ScrollViewUnitTests()
		{
			Device.PlatformServices = new MockPlatformServices();
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void TestConstructor()
		{
			ScrollView scrollView = new ScrollView();

			Assert.Null(scrollView.Content);

			View view = new View();
			scrollView = new ScrollView { Content = view };

			Assert.Equal(view, scrollView.Content);
		}

		[Theory]
		[InlineData(ScrollOrientation.Horizontal)]
		[InlineData(ScrollOrientation.Both)]
		public void GetsCorrectSizeRequestWithWrappingContent(ScrollOrientation orientation)
		{
			Device.PlatformServices = new MockPlatformServices(getNativeSizeFunc: null, useRealisticLabelMeasure: true);

			var scrollView = new ScrollView
			{
				IsPlatformEnabled = true,
				Orientation = orientation,
			};

			var hLayout = new StackLayout
			{
				IsPlatformEnabled = true,
				Orientation = StackOrientation.Horizontal,
				Children = {
					new Label {Text = "THIS IS A REALLY LONG STRING", IsPlatformEnabled = true},
					new Label {Text = "THIS IS A REALLY LONG STRING", IsPlatformEnabled = true},
					new Label {Text = "THIS IS A REALLY LONG STRING", IsPlatformEnabled = true},
					new Label {Text = "THIS IS A REALLY LONG STRING", IsPlatformEnabled = true},
					new Label {Text = "THIS IS A REALLY LONG STRING", IsPlatformEnabled = true},
				}
			};

			scrollView.Content = hLayout;

			var r = scrollView.GetSizeRequest(100, 100);

			Assert.Equal(10, r.Request.Height);
		}

		[Fact]
		public void TestContentSizeChangedVertical()
		{
			View view = new View { IsPlatformEnabled = true, WidthRequest = 100, HeightRequest = 100 };

			ScrollView scroll = new ScrollView { Content = view };
			scroll.Layout(new Rectangle(0, 0, 50, 50));

			Assert.Equal(new Size(50, 100), scroll.ContentSize);

			bool changed = false;
			scroll.PropertyChanged += (sender, e) =>
			{
				switch (e.PropertyName)
				{
					case "ContentSize":
						changed = true;
						break;
				}
			};

			view.HeightRequest = 200;

			Assert.True(changed);
			Assert.Equal(new Size(50, 200), scroll.ContentSize);
		}

		[Fact]
		public void TestContentSizeChangedVerticalBidirectional()
		{
			View view = new View { IsPlatformEnabled = true, WidthRequest = 100, HeightRequest = 100 };

			ScrollView scroll = new ScrollView { Content = view, Orientation = ScrollOrientation.Both };
			scroll.Layout(new Rectangle(0, 0, 50, 50));

			Assert.Equal(new Size(100, 100), scroll.ContentSize);

			bool changed = false;
			scroll.PropertyChanged += (sender, e) =>
			{
				switch (e.PropertyName)
				{
					case "ContentSize":
						changed = true;
						break;
				}
			};

			view.HeightRequest = 200;

			Assert.True(changed);
			Assert.Equal(new Size(100, 200), scroll.ContentSize);
		}

		[Fact]
		public void TestContentSizeChangedHorizontal()
		{
			View view = new View { IsPlatformEnabled = true, WidthRequest = 100, HeightRequest = 100 };

			var scroll = new ScrollView
			{
				Orientation = ScrollOrientation.Horizontal,
				Content = view
			};
			scroll.Layout(new Rectangle(0, 0, 50, 50));

			Assert.Equal(new Size(100, 50), scroll.ContentSize);

			bool changed = false;
			scroll.PropertyChanged += (sender, e) =>
			{
				switch (e.PropertyName)
				{
					case "ContentSize":
						changed = true;
						break;
				}
			};

			view.WidthRequest = 200;

			Assert.True(changed);
			Assert.Equal(new Size(200, 50), scroll.ContentSize);
		}

		[Fact]
		public void TestContentSizeChangedHorizontalBidirectional()
		{
			View view = new View { IsPlatformEnabled = true, WidthRequest = 100, HeightRequest = 100 };

			var scroll = new ScrollView
			{
				Orientation = ScrollOrientation.Both,
				Content = view
			};
			scroll.Layout(new Rectangle(0, 0, 50, 50));

			Assert.Equal(new Size(100, 100), scroll.ContentSize);

			bool changed = false;
			scroll.PropertyChanged += (sender, e) =>
			{
				switch (e.PropertyName)
				{
					case "ContentSize":
						changed = true;
						break;
				}
			};

			view.WidthRequest = 200;

			Assert.True(changed);
			Assert.Equal(new Size(200, 100), scroll.ContentSize);
		}

		[Fact]
		public void TestContentSizeDidNotChangeNeither()
		{
			View view = new View { IsPlatformEnabled = true, WidthRequest = 100, HeightRequest = 100 };

			var scroll = new ScrollView
			{
				Orientation = ScrollOrientation.Neither,
				Content = view
			};

			var originalBounds = new Rectangle(0, 0, 50, 50);

			scroll.Layout(originalBounds);

			Assert.Equal(scroll.ContentSize, originalBounds.Size);

			bool changed = false;
			scroll.PropertyChanged += (sender, e) =>
			{
				switch (e.PropertyName)
				{
					case "ContentSize":
						changed = true;
						break;
				}
			};

			view.WidthRequest = 200;

			Assert.False(changed);
			Assert.Equal(scroll.ContentSize, originalBounds.Size);
		}

		[Fact]
		public void TestContentSizeClamping()
		{
			View view = new View { IsPlatformEnabled = true, WidthRequest = 100, HeightRequest = 100 };

			var scroll = new ScrollView
			{
				Orientation = ScrollOrientation.Horizontal,
				Content = view,
			};
			scroll.Layout(new Rectangle(0, 0, 50, 50));

			bool changed = false;
			scroll.PropertyChanged += (sender, e) =>
			{
				switch (e.PropertyName)
				{
					case "ContentSize":
						changed = true;
						break;
				}
			};

			view.HeightRequest = 200;

			Assert.False(changed);
			Assert.Equal(new Size(100, 50), scroll.ContentSize);
		}

		[Fact]
		public void TestChildChanged()
		{
			ScrollView scrollView = new ScrollView();

			bool changed = false;
			scrollView.PropertyChanged += (sender, e) =>
			{
				switch (e.PropertyName)
				{
					case "Content":
						changed = true;
						break;
				}
			};
			View view = new View();
			scrollView.Content = view;

			Assert.True(changed);
		}

		[Fact]
		public void TestChildDoubleSet()
		{
			var scrollView = new ScrollView();

			bool changed = false;
			scrollView.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Content")
					changed = true;
			};

			var child = new View();
			scrollView.Content = child;

			Assert.True(changed);
			Assert.Equal(child, scrollView.Content);

			changed = false;

			scrollView.Content = child;

			Assert.False(changed);

			scrollView.Content = null;

			Assert.True(changed);
			Assert.Null(scrollView.Content);
		}

		[Fact]
		public void TestOrientation()
		{
			var scrollView = new ScrollView();

			Assert.Equal(ScrollOrientation.Vertical, scrollView.Orientation);

			bool signaled = false;
			scrollView.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Orientation")
					signaled = true;
			};

			scrollView.Orientation = ScrollOrientation.Horizontal;

			Assert.Equal(ScrollOrientation.Horizontal, scrollView.Orientation);
			Assert.True(signaled);

			scrollView.Orientation = ScrollOrientation.Both;
			Assert.Equal(ScrollOrientation.Both, scrollView.Orientation);
			Assert.True(signaled);

			scrollView.Orientation = ScrollOrientation.Neither;
			Assert.Equal(ScrollOrientation.Neither, scrollView.Orientation);
			Assert.True(signaled);
		}

		[Fact]
		public void TestOrientationDoubleSet()
		{
			var scrollView = new ScrollView();

			bool signaled = false;
			scrollView.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Orientation")
					signaled = true;
			};

			scrollView.Orientation = scrollView.Orientation;

			Assert.False(signaled);
		}


		[Fact]
		public void TestScrollTo()
		{
			var scrollView = new ScrollView();

			var item = new View { };
			scrollView.Content = new StackLayout { Children = { item } };

			bool requested = false;
			((IScrollViewController)scrollView).ScrollToRequested += (sender, args) =>
			{
				requested = true;
				Assert.Equal(args.ScrollY, 100);
				Assert.Equal(args.ScrollX, 0);
				Assert.Null(args.Item);
				Assert.Equal(true, args.ShouldAnimate);
			};

			scrollView.ScrollToAsync(0, 100, true);
			Assert.True(requested);
		}

		[Fact]
		public void TestScrollWasNotFiredOnNeither()
		{
			var scrollView = new ScrollView
			{
				Orientation = ScrollOrientation.Neither
			};

			var item = new View { };
			scrollView.Content = new StackLayout { Children = { item } };

			bool requested = false;
			((IScrollViewController)scrollView).ScrollToRequested += (sender, args) =>
			{
				requested = true;
			};

			scrollView.ScrollToAsync(0, 100, true);
			Assert.False(requested);
		}

		[Fact]
		public void TestScrollToNotAnimated()
		{
			var scrollView = new ScrollView();

			var item = new View { };
			scrollView.Content = new StackLayout { Children = { item } };

			bool requested = false;
			((IScrollViewController)scrollView).ScrollToRequested += (sender, args) =>
			{
				requested = true;
				Assert.Equal(args.ScrollY, 100);
				Assert.Equal(args.ScrollX, 0);
				Assert.Null(args.Item);
				Assert.Equal(false, args.ShouldAnimate);
			};

			scrollView.ScrollToAsync(0, 100, false);
			Assert.True(requested);
		}

		[Fact]
		public void TestScrollToElement()
		{
			var scrollView = new ScrollView();

			var item = new Label { Text = "Test" };
			scrollView.Content = new StackLayout { Children = { item } };

			bool requested = false;
			((IScrollViewController)scrollView).ScrollToRequested += (sender, args) =>
			{
				requested = true;

				Assert.Same(item, args.Element);
				Assert.Equal(ScrollToPosition.Center, args.Position);
				Assert.Equal(true, args.ShouldAnimate);
			};

			scrollView.ScrollToAsync(item, ScrollToPosition.Center, true);
			Assert.True(requested);
		}

		[Fact]
		public void TestScrollToElementNotAnimated()
		{
			var scrollView = new ScrollView();

			var item = new Label { Text = "Test" };
			scrollView.Content = new StackLayout { Children = { item } };

			bool requested = false;
			((IScrollViewController)scrollView).ScrollToRequested += (sender, args) =>
			{
				requested = true;

				Assert.Same(item, args.Element);
				Assert.Equal(ScrollToPosition.Center, args.Position);
				Assert.Equal(false, args.ShouldAnimate);
			};

			scrollView.ScrollToAsync(item, ScrollToPosition.Center, false);
			Assert.True(requested);
		}

		[Fact]
		public async Task TestScrollToInvalid()
		{
			var scrollView = new ScrollView();

			await Assert.ThrowsAsync<ArgumentException>(() => scrollView.ScrollToAsync(new VisualElement(), ScrollToPosition.Center, true));
			await Assert.ThrowsAsync<ArgumentException>(() => scrollView.ScrollToAsync(null, (ScrollToPosition)500, true));
		}

		[Fact]
		public void SetScrollPosition()
		{
			var scroll = new ScrollView();
			IScrollViewController controller = scroll;
			controller.SetScrolledPosition(100, 100);

			Assert.Equal(100, scroll.ScrollX);
			Assert.Equal(100, scroll.ScrollY);
		}

		[Fact]
		public void TestScrollContentMarginHorizontal()
		{
			View view = new View { IsPlatformEnabled = true, Margin = 100, WidthRequest = 100, HeightRequest = 100 };

			var scroll = new ScrollView
			{
				Content = view,
				Orientation = ScrollOrientation.Horizontal,
			};
			scroll.Layout(new Rectangle(0, 0, 100, 100));

			Assert.Equal(new Size(300, 100), scroll.ContentSize);
			Assert.Equal(100, scroll.Height);
			Assert.Equal(100, scroll.Width);
		}

		[Fact]
		public void TestScrollContentMarginVertical()
		{
			View view = new View { IsPlatformEnabled = true, Margin = 100, WidthRequest = 100, HeightRequest = 100 };

			var scroll = new ScrollView
			{
				Content = view,
				Orientation = ScrollOrientation.Vertical,
			};
			scroll.Layout(new Rectangle(0, 0, 100, 100));

			Assert.Equal(new Size(100, 300), scroll.ContentSize);
			Assert.Equal(100, scroll.Height);
			Assert.Equal(100, scroll.Width);
		}

		[Fact]
		public void TestScrollContentMarginBiDirectional()
		{
			View view = new View { IsPlatformEnabled = true, Margin = 100, WidthRequest = 100, HeightRequest = 100 };

			var scroll = new ScrollView
			{
				Content = view,
				Orientation = ScrollOrientation.Both,
			};
			scroll.Layout(new Rectangle(0, 0, 100, 100));

			Assert.Equal(new Size(300, 300), scroll.ContentSize);
			Assert.Equal(100, scroll.Height);
			Assert.Equal(100, scroll.Width);
		}

		[Fact]
		public void TestBackToBackBiDirectionalScroll()
		{
			var scrollView = new ScrollView
			{
				Orientation = ScrollOrientation.Both,
				Content = new Grid
				{
					WidthRequest = 1000,
					HeightRequest = 1000
				}
			};

			var y100Count = 0;

			((IScrollViewController)scrollView).ScrollToRequested += (sender, args) =>
			{
				if (args.ScrollY == 100)
				{
					++y100Count;
				}
			};

			scrollView.ScrollToAsync(100, 100, true);
			Assert.Equal(y100Count, 1);

			scrollView.ScrollToAsync(0, 100, true);
			Assert.Equal(y100Count, 2);
		}
	}
}
