using System;

using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	internal class TestDeviceInfo : DeviceInfo
	{
		public TestDeviceInfo()
		{
			CurrentOrientation = DeviceOrientation.Portrait;
		}
		public override Size PixelScreenSize
		{
			get { return new Size(100, 200); }
		}

		public override Size ScaledScreenSize
		{
			get { return new Size(50, 100); }
		}

		public override double ScalingFactor
		{
			get { return 2; }
		}
	}

	public class MasterDetailPageUnitTests : BaseTestFixture
	{
		public MasterDetailPageUnitTests()
		{
			var mockDeviceInfo = new TestDeviceInfo();
			Device.Info = mockDeviceInfo;
		}

		[Fact]
		public void TestConstructor()
		{

			MasterDetailPage page = new MasterDetailPage();

			Assert.Null(page.Master);
			Assert.Null(page.Detail);
		}

		[Fact]
		public void TestMasterSetter()
		{
			MasterDetailPage page = new MasterDetailPage();
			var child = new ContentPage { Content = new Label(), Title = "Foo" };
			page.Master = child;

			Assert.Equal(child, page.Master);
		}

		[Fact]
		public void TestMasterSetNull()
		{
			MasterDetailPage page = new MasterDetailPage();
			var child = new ContentPage { Content = new Label(), Title = "Foo" };
			page.Master = child;

			Assert.Throws<ArgumentNullException>(() => { page.Master = null; });
		}

		[Fact]
		public void TestMasterChanged()
		{
			MasterDetailPage page = new MasterDetailPage();
			var child = new ContentPage { Content = new Label(), Title = "Foo" };

			bool changed = false;
			page.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == "Master")
					changed = true;
			};

			page.Master = child;

			Assert.True(changed);
		}

		[Fact]
		public void TestDetailSetter()
		{
			MasterDetailPage page = new MasterDetailPage();
			var child = new ContentPage { Content = new Label() };
			page.Detail = child;

			Assert.Equal(child, page.Detail);
		}

		[Fact]
		public void TestDetailSetNull()
		{
			MasterDetailPage page = new MasterDetailPage();
			var child = new ContentPage { Content = new Label() };
			page.Detail = child;

			Assert.Throws<ArgumentNullException>(() => { page.Detail = null; });
		}

		[Fact]
		public void TestDetailChanged()
		{
			MasterDetailPage page = new MasterDetailPage();
			var child = new ContentPage { Content = new Label() };

			bool changed = false;
			page.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == "Detail")
					changed = true;
			};

			page.Detail = child;

			Assert.True(changed);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void ThrowsWhenMasterSetWithoutValidTitle(string title)
		{
			var page = new MasterDetailPage();
			Assert.Throws<InvalidOperationException>(() => page.Master = new ContentPage { Title = title });
		}

		[Fact]
		public void TestThrowsWhenPackedWithoutSetting()
		{
			MasterDetailPage page = new MasterDetailPage();
			Assert.Throws<InvalidOperationException>(() => new TabbedPage { Children = { page } });
		}

		[Fact]
		public void TestDoesNotThrowWhenPackedWithSetting()
		{
			MasterDetailPage page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), Title = "Foo" },
				Detail = new ContentPage { Content = new View() }
			};
			AssertEx.DoesNotThrow(() => new TabbedPage { Children = { page } });
		}

		[Fact]
		public void TestMasterVisible()
		{
			var page = new MasterDetailPage();

			Assert.Equal(false, page.IsPresented);

			bool signaled = false;
			page.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == MasterDetailPage.IsPresentedProperty.PropertyName)
					signaled = true;
			};

			page.IsPresented = true;

			Assert.Equal(true, page.IsPresented);
			Assert.True(signaled);
		}

		[Fact]
		public void TestMasterVisibleDoubleSet()
		{
			var page = new MasterDetailPage();

			bool signaled = false;
			page.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == MasterDetailPage.IsPresentedProperty.PropertyName)
					signaled = true;
			};

			page.IsPresented = page.IsPresented;

			Assert.False(signaled);
		}

		[Fact]
		public void TestSetMasterBounds()
		{
			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), Title = "Foo" },
				Detail = new ContentPage { Content = new View() }
			};

			((IMasterDetailPageController)page).MasterBounds = new Rectangle(0, 0, 100, 100);
			Assert.Equal(new Rectangle(0, 0, 100, 100), page.Master.Bounds);
			Assert.Equal(new Rectangle(0, 0, 100, 100), ((IMasterDetailPageController)page).MasterBounds);
		}

		[Fact]
		public void TestSetDetailBounds()
		{
			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), Title = "Foo" },
				Detail = new ContentPage { Content = new View() }
			};

			((IMasterDetailPageController)page).DetailBounds = new Rectangle(0, 0, 100, 100);
			Assert.Equal(new Rectangle(0, 0, 100, 100), page.Detail.Bounds);
			Assert.Equal(new Rectangle(0, 0, 100, 100), ((IMasterDetailPageController)page).DetailBounds);
		}

		[Fact]
		public void TestLayoutChildren()
		{
			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), IsPlatformEnabled = true, Title = "Foo" },
				Detail = new ContentPage { Content = new View(), IsPlatformEnabled = true },
				IsPlatformEnabled = true,
			};

			((IMasterDetailPageController)page).MasterBounds = new Rectangle(0, 0, 100, 200);
			((IMasterDetailPageController)page).DetailBounds = new Rectangle(0, 0, 100, 100);

			page.Master.Layout(new Rectangle(0, 0, 1, 1));
			page.Detail.Layout(new Rectangle(0, 0, 1, 1));

			page.Layout(new Rectangle(0, 0, 200, 200));

			Assert.Equal(new Rectangle(0, 0, 100, 200), page.Master.Bounds);
			Assert.Equal(new Rectangle(0, 0, 100, 100), page.Detail.Bounds);
		}

		[Fact]
		public void ThorwsInLayoutChildrenWithNullDetail()
		{
			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), IsPlatformEnabled = true, Title = "Foo" },
				IsPlatformEnabled = true,
			};

			Assert.Throws<InvalidOperationException>(() => page.Layout(new Rectangle(0, 0, 200, 200)));
		}

		[Fact]
		public void ThorwsInLayoutChildrenWithNullMaster()
		{
			var page = new MasterDetailPage
			{
				Detail = new ContentPage { Content = new View(), IsPlatformEnabled = true },
				IsPlatformEnabled = true,
			};

			Assert.Throws<InvalidOperationException>(() => page.Layout(new Rectangle(0, 0, 200, 200)));
		}

		[Fact]
		public void ThorwsInSetDetailBoundsWithNullDetail()
		{
			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), Title = "Foo" },
				IsPlatformEnabled = true,
			};

			Assert.Throws<InvalidOperationException>(() => ((IMasterDetailPageController)page).DetailBounds = new Rectangle(0, 0, 200, 200));
		}

		[Fact]
		public void ThrowsInSetMasterBoundsWithNullMaster()
		{
			var page = new MasterDetailPage
			{
				Detail = new ContentPage { Content = new View() },
				IsPlatformEnabled = true,
			};

			Assert.Throws<InvalidOperationException>(() => ((IMasterDetailPageController)page).MasterBounds = new Rectangle(0, 0, 200, 200));
		}

		[Fact]
		public void ThrowsInSetIsPresentOnSplitModeOnTablet()
		{
			Device.Idiom = TargetIdiom.Tablet;
			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), IsPlatformEnabled = true, Title = "Foo" },
				Detail = new ContentPage { Content = new View(), IsPlatformEnabled = true },
				IsPlatformEnabled = true,
				MasterBehavior = MasterBehavior.Split
			};

			Assert.Throws<InvalidOperationException>(() => page.IsPresented = false);
		}

		[Fact]
		public void ThorwsInSetIsPresentOnSplitPortraitModeOnTablet()
		{
			Device.Idiom = TargetIdiom.Tablet;
			Device.Info.CurrentOrientation = DeviceOrientation.Portrait;

			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), IsPlatformEnabled = true, Title = "Foo" },
				Detail = new ContentPage { Content = new View(), IsPlatformEnabled = true },
				IsPlatformEnabled = true,
				MasterBehavior = MasterBehavior.SplitOnPortrait
			};

			Assert.Throws<InvalidOperationException>(() => page.IsPresented = false);
		}

		[Fact]
		public void TestSetIsPresentedOnPopoverMode()
		{
			Device.Info.CurrentOrientation = DeviceOrientation.Landscape;

			var page = new MasterDetailPage
			{
				Master = new ContentPage { Content = new View(), IsPlatformEnabled = true, Title = "Foo" },
				Detail = new ContentPage { Content = new View(), IsPlatformEnabled = true },
				IsPlatformEnabled = true,
				MasterBehavior = MasterBehavior.Popover
			};
			page.IsPresented = true;

			Assert.Equal(true, page.IsPresented);
		}

		[Fact]
		public void SendsBackEventToPresentedMasterFirst()
		{
			var detail = new BackButtonPage() { Handle = true };
			var master = new BackButtonPage() { Title = "Master" };
			var mdp = new MasterDetailPage()
			{
				Detail = detail,
				Master = master,
				IsPresented = true,
				IsPlatformEnabled = true,
			};

			((IMasterDetailPageController)mdp).BackButtonPressed += (sender, args) =>
			{
				args.Handled = mdp.IsPresented;
				mdp.IsPresented = false;
			};

			var detailEmitted = false;
			var masterEmitted = false;

			detail.BackPressed += (sender, args) => detailEmitted = true;
			master.BackPressed += (sender, args) => masterEmitted = true;

			var result = mdp.SendBackButtonPressed();

			Assert.True(masterEmitted);
			Assert.False(detailEmitted);
			Assert.True(result);
		}

		[Fact]
		public void EmitsCorrectlyWhenPresentedOnBackPressed()
		{
			var detail = new BackButtonPage();
			var master = new BackButtonPage { Title = "Master" };
			var mdp = new MasterDetailPage
			{
				Detail = detail,
				Master = master,
				IsPresented = true,
				IsPlatformEnabled = true,
			};

			((IMasterDetailPageController)mdp).BackButtonPressed += (sender, args) =>
			{
				args.Handled = mdp.IsPresented;
				mdp.IsPresented = false;
			};

			var detailEmitted = false;
			var masterEmitted = false;

			detail.BackPressed += (sender, args) => detailEmitted = true;
			master.BackPressed += (sender, args) => masterEmitted = true;

			var result = mdp.SendBackButtonPressed();

			Assert.True(masterEmitted);
			Assert.False(detailEmitted);
			Assert.True(result);
		}

		[Fact]
		public void ThrowsExceptionWhenAddingAlreadyParentedDetail()
		{
			var detail = new ContentPage { };

			// give detail a parent
			var nav = new NavigationPage(detail);

			var mdp = new MasterDetailPage();
			Assert.Throws<InvalidOperationException>(() => mdp.Detail = detail);
		}

		[Fact]
		public void ThrowsExceptionWhenAddingAlreadyParentedMaster()
		{
			var master = new ContentPage { Title = "Foo" };

			// give master a parent
			var nav = new NavigationPage(master);

			var mdp = new MasterDetailPage();
			Assert.Throws<InvalidOperationException>(() => mdp.Master = master);
		}


		[Fact]
		public void TestFlyoutLayoutBehaviorGetsSetViaBinding()
		{
			var master = new MasterDetailPage { Title = "Foo" };

			master.SetValue(MasterDetailPage.MasterBehaviorProperty, MasterBehavior.Split);

			Assert.Equal(MasterBehavior.Split, master.MasterBehavior);
			Assert.Equal(FlyoutLayoutBehavior.Split, master.FlyoutLayoutBehavior);
		}
	}

}
