using System;
using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class FlexLayoutFlexDirectionTests : BaseTestFixture
	{
		[Fact]
		public void TestFlexDirectionColumnWithoutHeight()
		{
			var view0 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var view1 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var view2 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var layout = new FlexLayout
			{
				IsPlatformEnabled = true,
				Children = {
					view0,
					view1,
					view2,
				},

				Direction = FlexDirection.Column,
			};

			var sizeRequest = layout.Measure(100, double.PositiveInfinity);
			layout.Layout(new Rectangle(0, 0, sizeRequest.Request.Width, sizeRequest.Request.Height));
			Assert.Equal(new Rectangle(0, 0, 100, 30), layout.Bounds);
			Assert.Equal(new Rectangle(0, 0, 100, 10), view0.Bounds);
			Assert.Equal(new Rectangle(0, 10, 100, 10), view1.Bounds);
			Assert.Equal(new Rectangle(0, 20, 100, 10), view2.Bounds);
		}

		[Fact]
		public void TestFlexDirectionRowNoWidth()
		{
			var view0 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var view1 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var view2 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var layout = new FlexLayout
			{
				IsPlatformEnabled = true,
				Children = {
					view0,
					view1,
					view2,
				},

				Direction = FlexDirection.Row,
			};


			var measure = layout.Measure(double.PositiveInfinity, 100);
			layout.Layout(new Rectangle(0, 0, measure.Request.Width, measure.Request.Height));
			Assert.Equal(new Rectangle(0, 0, 30, 100), layout.Bounds);
			Assert.Equal(new Rectangle(0, 0, 10, 100), view0.Bounds);
			Assert.Equal(new Rectangle(10, 0, 10, 100), view1.Bounds);
			Assert.Equal(new Rectangle(20, 0, 10, 100), view2.Bounds);
		}

		[Fact]
		public void TestFlexDirectionColumn()
		{
			var view0 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var view1 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var view2 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var layout = new FlexLayout
			{
				IsPlatformEnabled = true,
				Children = {
					view0,
					view1,
					view2,
				},

				Direction = FlexDirection.Column,
			};

			layout.Layout(new Rectangle(0, 0, 100, 100));
			Assert.Equal(new Rectangle(0, 0, 100, 100), layout.Bounds);
			Assert.Equal(new Rectangle(0, 0, 100, 10), view0.Bounds);
			Assert.Equal(new Rectangle(0, 10, 100, 10), view1.Bounds);
			Assert.Equal(new Rectangle(0, 20, 100, 10), view2.Bounds);
		}

		[Fact]
		public void TestFlexDirectionRow()
		{
			var view0 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var view1 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var view2 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var layout = new FlexLayout
			{
				IsPlatformEnabled = true,
				Children = {
					view0,
					view1,
					view2,
				},

				Direction = FlexDirection.Row,
			};

			layout.Layout(new Rectangle(0, 0, 100, 100));
			Assert.Equal(new Rectangle(0, 0, 100, 100), layout.Bounds);
			Assert.Equal(new Rectangle(0, 0, 10, 100), view0.Bounds);
			Assert.Equal(new Rectangle(10, 0, 10, 100), view1.Bounds);
			Assert.Equal(new Rectangle(20, 0, 10, 100), view2.Bounds);
		}

		[Fact]
		public void TestFlexDirectionColumnReverse()
		{
			var view0 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var view1 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var view2 = new View { IsPlatformEnabled = true, HeightRequest = 10 };
			var layout = new FlexLayout
			{
				IsPlatformEnabled = true,
				Children = {
					view0,
					view1,
					view2,
				},

				Direction = FlexDirection.ColumnReverse,
			};

			layout.Layout(new Rectangle(0, 0, 100, 100));
			Assert.Equal(new Rectangle(0, 0, 100, 100), layout.Bounds);
			Assert.Equal(new Rectangle(0, 90, 100, 10), view0.Bounds);
			Assert.Equal(new Rectangle(0, 80, 100, 10), view1.Bounds);
			Assert.Equal(new Rectangle(0, 70, 100, 10), view2.Bounds);
		}

		[Fact]
		public void TestFlexDirectionRowReverse()
		{
			var view0 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var view1 = new View { IsPlatformEnabled = true, WidthRequest = 10, };
			var view2 = new View { IsPlatformEnabled = true, WidthRequest = 10, };

			var layout = new FlexLayout
			{
				IsPlatformEnabled = true,
				Children = {
					view0,
					view1,
					view2,
				},

				Direction = FlexDirection.RowReverse,
			};

			layout.Layout(new Rectangle(0, 0, 100, 100));
			Assert.Equal(new Rectangle(0, 0, 100, 100), layout.Bounds);
			Assert.Equal(new Rectangle(90, 0, 10, 100), view0.Bounds);
			Assert.Equal(new Rectangle(80, 0, 10, 100), view1.Bounds);
			Assert.Equal(new Rectangle(70, 0, 10, 100), view2.Bounds);
		}
	}
}