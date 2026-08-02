using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class BoxViewUnitTests : BaseTestFixture
	{
		[Fact]
		public void TestConstructor()
		{
			var box = new BoxView
			{
				Color = new Color(0.2, 0.3, 0.4),
				WidthRequest = 20,
				HeightRequest = 30,
				IsPlatformEnabled = true,
			};

			Assert.Equal(new Color(0.2, 0.3, 0.4), box.Color);
			var request = box.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity).Request;
			Assert.Equal(20, request.Width);
			Assert.Equal(30, request.Height);
		}

		[Fact]
		public void DefaultSize()
		{
			var box = new BoxView
			{
				IsPlatformEnabled = true,
			};

			var request = box.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity).Request;
			Assert.Equal(40, request.Width);
			Assert.Equal(40, request.Height);
		}
	}
}
