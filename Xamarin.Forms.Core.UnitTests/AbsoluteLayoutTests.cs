using System;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class AbsoluteLayoutTests : BaseTestFixture
	{
		public AbsoluteLayoutTests()
		{
			var mockDeviceInfo = new TestDeviceInfo();
			Device.Info = mockDeviceInfo;
		}


		[Fact]
		public void Constructor()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			Assert.Empty(abs.Children);

			var sizeReq = abs.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);
			Assert.Equal(Size.Zero, sizeReq.Request);
			Assert.Equal(Size.Zero, sizeReq.Minimum);
		}

		[Fact]
		public void AbsolutePositionAndSizeUsingRectangle()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View { IsPlatformEnabled = true };

			abs.Children.Add(child, new Rectangle(10, 20, 30, 40));

			abs.Layout(new Rectangle(0, 0, 100, 100));

			Assert.Equal(new Rectangle(10, 20, 30, 40), child.Bounds);
		}

		[Fact]
		public void AbsolutePositionAndSizeUsingRect()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View { IsPlatformEnabled = true };

			abs.Children.Add(child, new Rect(10, 20, 30, 40));

			abs.Layout(new Rect(0, 0, 100, 100));

			Assert.Equal(new Rect(10, 20, 30, 40), (Rect)child.Bounds);
		}

		[Fact]
		public void AbsolutePositionRelativeSize()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View { IsPlatformEnabled = true };


			abs.Children.Add(child, new Rectangle(10, 20, 0.4, 0.5), AbsoluteLayoutFlags.SizeProportional);

			abs.Layout(new Rectangle(0, 0, 100, 100));

			Assert.Equal(10, child.X);
			Assert.Equal(20, child.Y);
			Assert.Equal(40, child.Width, 0.0001);
			Assert.Equal(50, child.Height, 0.0001);
		}

		[Theory]
		[InlineData(30, 40, 0.2, 0.3)]
		[InlineData(35, 45, 0.5, 0.5)]
		[InlineData(35, 45, 0, 0)]
		[InlineData(35, 45, 1, 1)]
		public void RelativePositionAbsoluteSize(double width, double height, double relX, double relY)
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View { IsPlatformEnabled = true };

			abs.Children.Add(child, new Rectangle(relX, relY, width, height), AbsoluteLayoutFlags.PositionProportional);

			abs.Layout(new Rectangle(0, 0, 100, 100));

			double expectedX = Math.Round((100 - width) * relX);
			double expectedY = Math.Round((100 - height) * relY);
			Assert.Equal(expectedX, child.X, 0.0001);
			Assert.Equal(expectedY, child.Y, 0.0001);
			Assert.Equal(width, child.Width);
			Assert.Equal(height, child.Height);
		}

		[Theory]
		[InlineData(0.0, 0.0, 0.0, 0.0)]
		[InlineData(0.0, 0.0, 0.0, 0.2)]
		[InlineData(0.0, 0.0, 0.0, 0.5)]
		[InlineData(0.0, 0.0, 0.0, 1.0)]
		[InlineData(0.0, 0.0, 0.2, 0.0)]
		[InlineData(0.0, 0.0, 0.2, 0.2)]
		[InlineData(0.0, 0.0, 0.2, 0.5)]
		[InlineData(0.0, 0.0, 0.2, 1.0)]
		[InlineData(0.0, 0.0, 0.5, 0.0)]
		[InlineData(0.0, 0.0, 0.5, 0.2)]
		[InlineData(0.0, 0.0, 0.5, 0.5)]
		[InlineData(0.0, 0.0, 0.5, 1.0)]
		[InlineData(0.0, 0.0, 1.0, 0.0)]
		[InlineData(0.0, 0.0, 1.0, 0.2)]
		[InlineData(0.0, 0.0, 1.0, 0.5)]
		[InlineData(0.0, 0.0, 1.0, 1.0)]
		[InlineData(0.0, 0.2, 0.0, 0.0)]
		[InlineData(0.0, 0.2, 0.0, 0.2)]
		[InlineData(0.0, 0.2, 0.0, 0.5)]
		[InlineData(0.0, 0.2, 0.0, 1.0)]
		[InlineData(0.0, 0.2, 0.2, 0.0)]
		[InlineData(0.0, 0.2, 0.2, 0.2)]
		[InlineData(0.0, 0.2, 0.2, 0.5)]
		[InlineData(0.0, 0.2, 0.2, 1.0)]
		[InlineData(0.0, 0.2, 0.5, 0.0)]
		[InlineData(0.0, 0.2, 0.5, 0.2)]
		[InlineData(0.0, 0.2, 0.5, 0.5)]
		[InlineData(0.0, 0.2, 0.5, 1.0)]
		[InlineData(0.0, 0.2, 1.0, 0.0)]
		[InlineData(0.0, 0.2, 1.0, 0.2)]
		[InlineData(0.0, 0.2, 1.0, 0.5)]
		[InlineData(0.0, 0.2, 1.0, 1.0)]
		[InlineData(0.0, 0.5, 0.0, 0.0)]
		[InlineData(0.0, 0.5, 0.0, 0.2)]
		[InlineData(0.0, 0.5, 0.0, 0.5)]
		[InlineData(0.0, 0.5, 0.0, 1.0)]
		[InlineData(0.0, 0.5, 0.2, 0.0)]
		[InlineData(0.0, 0.5, 0.2, 0.2)]
		[InlineData(0.0, 0.5, 0.2, 0.5)]
		[InlineData(0.0, 0.5, 0.2, 1.0)]
		[InlineData(0.0, 0.5, 0.5, 0.0)]
		[InlineData(0.0, 0.5, 0.5, 0.2)]
		[InlineData(0.0, 0.5, 0.5, 0.5)]
		[InlineData(0.0, 0.5, 0.5, 1.0)]
		[InlineData(0.0, 0.5, 1.0, 0.0)]
		[InlineData(0.0, 0.5, 1.0, 0.2)]
		[InlineData(0.0, 0.5, 1.0, 0.5)]
		[InlineData(0.0, 0.5, 1.0, 1.0)]
		[InlineData(0.0, 1.0, 0.0, 0.0)]
		[InlineData(0.0, 1.0, 0.0, 0.2)]
		[InlineData(0.0, 1.0, 0.0, 0.5)]
		[InlineData(0.0, 1.0, 0.0, 1.0)]
		[InlineData(0.0, 1.0, 0.2, 0.0)]
		[InlineData(0.0, 1.0, 0.2, 0.2)]
		[InlineData(0.0, 1.0, 0.2, 0.5)]
		[InlineData(0.0, 1.0, 0.2, 1.0)]
		[InlineData(0.0, 1.0, 0.5, 0.0)]
		[InlineData(0.0, 1.0, 0.5, 0.2)]
		[InlineData(0.0, 1.0, 0.5, 0.5)]
		[InlineData(0.0, 1.0, 0.5, 1.0)]
		[InlineData(0.0, 1.0, 1.0, 0.0)]
		[InlineData(0.0, 1.0, 1.0, 0.2)]
		[InlineData(0.0, 1.0, 1.0, 0.5)]
		[InlineData(0.0, 1.0, 1.0, 1.0)]
		[InlineData(0.2, 0.0, 0.0, 0.0)]
		[InlineData(0.2, 0.0, 0.0, 0.2)]
		[InlineData(0.2, 0.0, 0.0, 0.5)]
		[InlineData(0.2, 0.0, 0.0, 1.0)]
		[InlineData(0.2, 0.0, 0.2, 0.0)]
		[InlineData(0.2, 0.0, 0.2, 0.2)]
		[InlineData(0.2, 0.0, 0.2, 0.5)]
		[InlineData(0.2, 0.0, 0.2, 1.0)]
		[InlineData(0.2, 0.0, 0.5, 0.0)]
		[InlineData(0.2, 0.0, 0.5, 0.2)]
		[InlineData(0.2, 0.0, 0.5, 0.5)]
		[InlineData(0.2, 0.0, 0.5, 1.0)]
		[InlineData(0.2, 0.0, 1.0, 0.0)]
		[InlineData(0.2, 0.0, 1.0, 0.2)]
		[InlineData(0.2, 0.0, 1.0, 0.5)]
		[InlineData(0.2, 0.0, 1.0, 1.0)]
		[InlineData(0.2, 0.2, 0.0, 0.0)]
		[InlineData(0.2, 0.2, 0.0, 0.2)]
		[InlineData(0.2, 0.2, 0.0, 0.5)]
		[InlineData(0.2, 0.2, 0.0, 1.0)]
		[InlineData(0.2, 0.2, 0.2, 0.0)]
		[InlineData(0.2, 0.2, 0.2, 0.2)]
		[InlineData(0.2, 0.2, 0.2, 0.5)]
		[InlineData(0.2, 0.2, 0.2, 1.0)]
		[InlineData(0.2, 0.2, 0.5, 0.0)]
		[InlineData(0.2, 0.2, 0.5, 0.2)]
		[InlineData(0.2, 0.2, 0.5, 0.5)]
		[InlineData(0.2, 0.2, 0.5, 1.0)]
		[InlineData(0.2, 0.2, 1.0, 0.0)]
		[InlineData(0.2, 0.2, 1.0, 0.2)]
		[InlineData(0.2, 0.2, 1.0, 0.5)]
		[InlineData(0.2, 0.2, 1.0, 1.0)]
		[InlineData(0.2, 0.5, 0.0, 0.0)]
		[InlineData(0.2, 0.5, 0.0, 0.2)]
		[InlineData(0.2, 0.5, 0.0, 0.5)]
		[InlineData(0.2, 0.5, 0.0, 1.0)]
		[InlineData(0.2, 0.5, 0.2, 0.0)]
		[InlineData(0.2, 0.5, 0.2, 0.2)]
		[InlineData(0.2, 0.5, 0.2, 0.5)]
		[InlineData(0.2, 0.5, 0.2, 1.0)]
		[InlineData(0.2, 0.5, 0.5, 0.0)]
		[InlineData(0.2, 0.5, 0.5, 0.2)]
		[InlineData(0.2, 0.5, 0.5, 0.5)]
		[InlineData(0.2, 0.5, 0.5, 1.0)]
		[InlineData(0.2, 0.5, 1.0, 0.0)]
		[InlineData(0.2, 0.5, 1.0, 0.2)]
		[InlineData(0.2, 0.5, 1.0, 0.5)]
		[InlineData(0.2, 0.5, 1.0, 1.0)]
		[InlineData(0.2, 1.0, 0.0, 0.0)]
		[InlineData(0.2, 1.0, 0.0, 0.2)]
		[InlineData(0.2, 1.0, 0.0, 0.5)]
		[InlineData(0.2, 1.0, 0.0, 1.0)]
		[InlineData(0.2, 1.0, 0.2, 0.0)]
		[InlineData(0.2, 1.0, 0.2, 0.2)]
		[InlineData(0.2, 1.0, 0.2, 0.5)]
		[InlineData(0.2, 1.0, 0.2, 1.0)]
		[InlineData(0.2, 1.0, 0.5, 0.0)]
		[InlineData(0.2, 1.0, 0.5, 0.2)]
		[InlineData(0.2, 1.0, 0.5, 0.5)]
		[InlineData(0.2, 1.0, 0.5, 1.0)]
		[InlineData(0.2, 1.0, 1.0, 0.0)]
		[InlineData(0.2, 1.0, 1.0, 0.2)]
		[InlineData(0.2, 1.0, 1.0, 0.5)]
		[InlineData(0.2, 1.0, 1.0, 1.0)]
		[InlineData(0.5, 0.0, 0.0, 0.0)]
		[InlineData(0.5, 0.0, 0.0, 0.2)]
		[InlineData(0.5, 0.0, 0.0, 0.5)]
		[InlineData(0.5, 0.0, 0.0, 1.0)]
		[InlineData(0.5, 0.0, 0.2, 0.0)]
		[InlineData(0.5, 0.0, 0.2, 0.2)]
		[InlineData(0.5, 0.0, 0.2, 0.5)]
		[InlineData(0.5, 0.0, 0.2, 1.0)]
		[InlineData(0.5, 0.0, 0.5, 0.0)]
		[InlineData(0.5, 0.0, 0.5, 0.2)]
		[InlineData(0.5, 0.0, 0.5, 0.5)]
		[InlineData(0.5, 0.0, 0.5, 1.0)]
		[InlineData(0.5, 0.0, 1.0, 0.0)]
		[InlineData(0.5, 0.0, 1.0, 0.2)]
		[InlineData(0.5, 0.0, 1.0, 0.5)]
		[InlineData(0.5, 0.0, 1.0, 1.0)]
		[InlineData(0.5, 0.2, 0.0, 0.0)]
		[InlineData(0.5, 0.2, 0.0, 0.2)]
		[InlineData(0.5, 0.2, 0.0, 0.5)]
		[InlineData(0.5, 0.2, 0.0, 1.0)]
		[InlineData(0.5, 0.2, 0.2, 0.0)]
		[InlineData(0.5, 0.2, 0.2, 0.2)]
		[InlineData(0.5, 0.2, 0.2, 0.5)]
		[InlineData(0.5, 0.2, 0.2, 1.0)]
		[InlineData(0.5, 0.2, 0.5, 0.0)]
		[InlineData(0.5, 0.2, 0.5, 0.2)]
		[InlineData(0.5, 0.2, 0.5, 0.5)]
		[InlineData(0.5, 0.2, 0.5, 1.0)]
		[InlineData(0.5, 0.2, 1.0, 0.0)]
		[InlineData(0.5, 0.2, 1.0, 0.2)]
		[InlineData(0.5, 0.2, 1.0, 0.5)]
		[InlineData(0.5, 0.2, 1.0, 1.0)]
		[InlineData(0.5, 0.5, 0.0, 0.0)]
		[InlineData(0.5, 0.5, 0.0, 0.2)]
		[InlineData(0.5, 0.5, 0.0, 0.5)]
		[InlineData(0.5, 0.5, 0.0, 1.0)]
		[InlineData(0.5, 0.5, 0.2, 0.0)]
		[InlineData(0.5, 0.5, 0.2, 0.2)]
		[InlineData(0.5, 0.5, 0.2, 0.5)]
		[InlineData(0.5, 0.5, 0.2, 1.0)]
		[InlineData(0.5, 0.5, 0.5, 0.0)]
		[InlineData(0.5, 0.5, 0.5, 0.2)]
		[InlineData(0.5, 0.5, 0.5, 0.5)]
		[InlineData(0.5, 0.5, 0.5, 1.0)]
		[InlineData(0.5, 0.5, 1.0, 0.0)]
		[InlineData(0.5, 0.5, 1.0, 0.2)]
		[InlineData(0.5, 0.5, 1.0, 0.5)]
		[InlineData(0.5, 0.5, 1.0, 1.0)]
		[InlineData(0.5, 1.0, 0.0, 0.0)]
		[InlineData(0.5, 1.0, 0.0, 0.2)]
		[InlineData(0.5, 1.0, 0.0, 0.5)]
		[InlineData(0.5, 1.0, 0.0, 1.0)]
		[InlineData(0.5, 1.0, 0.2, 0.0)]
		[InlineData(0.5, 1.0, 0.2, 0.2)]
		[InlineData(0.5, 1.0, 0.2, 0.5)]
		[InlineData(0.5, 1.0, 0.2, 1.0)]
		[InlineData(0.5, 1.0, 0.5, 0.0)]
		[InlineData(0.5, 1.0, 0.5, 0.2)]
		[InlineData(0.5, 1.0, 0.5, 0.5)]
		[InlineData(0.5, 1.0, 0.5, 1.0)]
		[InlineData(0.5, 1.0, 1.0, 0.0)]
		[InlineData(0.5, 1.0, 1.0, 0.2)]
		[InlineData(0.5, 1.0, 1.0, 0.5)]
		[InlineData(0.5, 1.0, 1.0, 1.0)]
		[InlineData(1.0, 0.0, 0.0, 0.0)]
		[InlineData(1.0, 0.0, 0.0, 0.2)]
		[InlineData(1.0, 0.0, 0.0, 0.5)]
		[InlineData(1.0, 0.0, 0.0, 1.0)]
		[InlineData(1.0, 0.0, 0.2, 0.0)]
		[InlineData(1.0, 0.0, 0.2, 0.2)]
		[InlineData(1.0, 0.0, 0.2, 0.5)]
		[InlineData(1.0, 0.0, 0.2, 1.0)]
		[InlineData(1.0, 0.0, 0.5, 0.0)]
		[InlineData(1.0, 0.0, 0.5, 0.2)]
		[InlineData(1.0, 0.0, 0.5, 0.5)]
		[InlineData(1.0, 0.0, 0.5, 1.0)]
		[InlineData(1.0, 0.0, 1.0, 0.0)]
		[InlineData(1.0, 0.0, 1.0, 0.2)]
		[InlineData(1.0, 0.0, 1.0, 0.5)]
		[InlineData(1.0, 0.0, 1.0, 1.0)]
		[InlineData(1.0, 0.2, 0.0, 0.0)]
		[InlineData(1.0, 0.2, 0.0, 0.2)]
		[InlineData(1.0, 0.2, 0.0, 0.5)]
		[InlineData(1.0, 0.2, 0.0, 1.0)]
		[InlineData(1.0, 0.2, 0.2, 0.0)]
		[InlineData(1.0, 0.2, 0.2, 0.2)]
		[InlineData(1.0, 0.2, 0.2, 0.5)]
		[InlineData(1.0, 0.2, 0.2, 1.0)]
		[InlineData(1.0, 0.2, 0.5, 0.0)]
		[InlineData(1.0, 0.2, 0.5, 0.2)]
		[InlineData(1.0, 0.2, 0.5, 0.5)]
		[InlineData(1.0, 0.2, 0.5, 1.0)]
		[InlineData(1.0, 0.2, 1.0, 0.0)]
		[InlineData(1.0, 0.2, 1.0, 0.2)]
		[InlineData(1.0, 0.2, 1.0, 0.5)]
		[InlineData(1.0, 0.2, 1.0, 1.0)]
		[InlineData(1.0, 0.5, 0.0, 0.0)]
		[InlineData(1.0, 0.5, 0.0, 0.2)]
		[InlineData(1.0, 0.5, 0.0, 0.5)]
		[InlineData(1.0, 0.5, 0.0, 1.0)]
		[InlineData(1.0, 0.5, 0.2, 0.0)]
		[InlineData(1.0, 0.5, 0.2, 0.2)]
		[InlineData(1.0, 0.5, 0.2, 0.5)]
		[InlineData(1.0, 0.5, 0.2, 1.0)]
		[InlineData(1.0, 0.5, 0.5, 0.0)]
		[InlineData(1.0, 0.5, 0.5, 0.2)]
		[InlineData(1.0, 0.5, 0.5, 0.5)]
		[InlineData(1.0, 0.5, 0.5, 1.0)]
		[InlineData(1.0, 0.5, 1.0, 0.0)]
		[InlineData(1.0, 0.5, 1.0, 0.2)]
		[InlineData(1.0, 0.5, 1.0, 0.5)]
		[InlineData(1.0, 0.5, 1.0, 1.0)]
		[InlineData(1.0, 1.0, 0.0, 0.0)]
		[InlineData(1.0, 1.0, 0.0, 0.2)]
		[InlineData(1.0, 1.0, 0.0, 0.5)]
		[InlineData(1.0, 1.0, 0.0, 1.0)]
		[InlineData(1.0, 1.0, 0.2, 0.0)]
		[InlineData(1.0, 1.0, 0.2, 0.2)]
		[InlineData(1.0, 1.0, 0.2, 0.5)]
		[InlineData(1.0, 1.0, 0.2, 1.0)]
		[InlineData(1.0, 1.0, 0.5, 0.0)]
		[InlineData(1.0, 1.0, 0.5, 0.2)]
		[InlineData(1.0, 1.0, 0.5, 0.5)]
		[InlineData(1.0, 1.0, 0.5, 1.0)]
		[InlineData(1.0, 1.0, 1.0, 0.0)]
		[InlineData(1.0, 1.0, 1.0, 0.2)]
		[InlineData(1.0, 1.0, 1.0, 0.5)]
		[InlineData(1.0, 1.0, 1.0, 1.0)]
		public void RelativePositionRelativeSize(double relX, double relY, double relHeight, double relWidth)
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View
			{
				IsPlatformEnabled = true
			};
			abs.Children.Add(child, new Rectangle(relX, relY, relWidth, relHeight), AbsoluteLayoutFlags.All);
			abs.Layout(new Rectangle(0, 0, 100, 100));

			double expectedWidth = Math.Round(100 * relWidth);
			double expectedHeight = Math.Round(100 * relHeight);
			double expectedX = Math.Round((100 - expectedWidth) * relX);
			double expectedY = Math.Round((100 - expectedHeight) * relY);
			Assert.Equal(expectedX, child.X, 0.0001);
			Assert.Equal(expectedY, child.Y, 0.0001);
			Assert.Equal(expectedWidth, child.Width, 0.0001);
			Assert.Equal(expectedHeight, child.Height, 0.0001);
		}

		[Fact]
		public void SizeRequestWithNormalChild()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View();

			// ChildSizeReq == 100x20
			abs.Children.Add(child, new Rectangle(10, 20, 30, 40));

			var sizeReq = abs.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);

			Assert.Equal(new Size(40, 60), sizeReq.Request);
			Assert.Equal(new Size(40, 60), sizeReq.Minimum);
		}

		[Fact]
		public void SizeRequestWithRelativePositionChild()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View();

			// ChildSizeReq == 100x20
			abs.Children.Add(child, new Rectangle(0.5, 0.5, 30, 40), AbsoluteLayoutFlags.PositionProportional);

			var sizeReq = abs.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);

			Assert.Equal(new Size(30, 40), sizeReq.Request);
			Assert.Equal(new Size(30, 40), sizeReq.Minimum);
		}

		[Fact]
		public void SizeRequestWithRelativeChild()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View
			{
				IsPlatformEnabled = true
			};

			// ChildSizeReq == 100x20
			abs.Children.Add(child, new Rectangle(0.5, 0.5, 0.5, 0.5), AbsoluteLayoutFlags.All);

			var sizeReq = abs.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);

			Assert.Equal(new Size(200, 40), sizeReq.Request);
			Assert.Equal(new Size(0, 0), sizeReq.Minimum);
		}

		[Fact]
		public void SizeRequestWithRelativeSizeChild()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View
			{
				IsPlatformEnabled = true
			};

			// ChildSizeReq == 100x20
			abs.Children.Add(child, new Rectangle(10, 20, 0.5, 0.5), AbsoluteLayoutFlags.SizeProportional);

			var sizeReq = abs.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);

			Assert.Equal(new Size(210, 60), sizeReq.Request);
			Assert.Equal(new Size(10, 20), sizeReq.Minimum);
		}

		[Fact]
		public void MeasureInvalidatedFiresWhenFlagsChanged()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View
			{
				IsPlatformEnabled = true
			};

			abs.Children.Add(child, new Rectangle(1, 1, 100, 100));

			bool fired = false;
			abs.MeasureInvalidated += (sender, args) => fired = true;

			AbsoluteLayout.SetLayoutFlags(child, AbsoluteLayoutFlags.PositionProportional);

			Assert.True(fired);
		}

		[Fact]
		public void MeasureInvalidatedFiresWhenBoundsChanged()
		{
			var abs = new AbsoluteLayout
			{
				IsPlatformEnabled = true
			};

			var child = new View
			{
				IsPlatformEnabled = true
			};

			abs.Children.Add(child, new Rectangle(1, 1, 100, 100));

			bool fired = false;
			abs.MeasureInvalidated += (sender, args) => fired = true;

			AbsoluteLayout.SetLayoutBounds(child, new Rectangle(2, 2, 200, 200));

			Assert.True(fired);
		}

		[Theory]
		[InlineData("en-US")]
		[InlineData("tr-TR")]
		public void TestBoundsTypeConverter(string culture)
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);

			var converter = new BoundsTypeConverter();

			Assert.True(converter.CanConvertFrom(typeof(string)));
			Assert.Equal(new Rectangle(3, 4, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize), converter.ConvertFromInvariantString("3, 4"));
			Assert.Equal(new Rectangle(3, 4, 20, 30), converter.ConvertFromInvariantString("3, 4, 20, 30"));
			Assert.Equal(new Rectangle(3, 4, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize), converter.ConvertFromInvariantString("3, 4, AutoSize, AutoSize"));
			Assert.Equal(new Rectangle(3, 4, AbsoluteLayout.AutoSize, 30), converter.ConvertFromInvariantString("3, 4, AutoSize, 30"));
			Assert.Equal(new Rectangle(3, 4, 20, AbsoluteLayout.AutoSize), converter.ConvertFromInvariantString("3, 4, 20, AutoSize"));

			var autoSize = "AutoSize";
			Assert.Equal(new Rectangle(3.3, 4.4, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize), converter.ConvertFromInvariantString("3.3, 4.4, " + autoSize + ", AutoSize"));
			Assert.Equal(new Rectangle(3.3, 4.4, 5.5, 6.6), converter.ConvertFromInvariantString("3.3, 4.4, 5.5, 6.6"));
		}
	}
}
