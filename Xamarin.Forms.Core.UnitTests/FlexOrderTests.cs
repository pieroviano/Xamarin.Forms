using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class FlexOrderTests : BaseTestFixture
	{
		[Fact]
		public void TestOrderingElements()
		{
			var label0 = new Label { IsPlatformEnabled = true };
			var label1 = new Label { IsPlatformEnabled = true };
			var label2 = new Label { IsPlatformEnabled = true };
			var label3 = new Label { IsPlatformEnabled = true };

			FlexLayout.SetOrder(label3, 0);
			FlexLayout.SetOrder(label2, 1);
			FlexLayout.SetOrder(label1, 2);
			FlexLayout.SetOrder(label0, 3);

			var layout = new FlexLayout
			{
				IsPlatformEnabled = true,
				Direction = FlexDirection.Column,
				Children = {
					label0,
					label1,
					label2,
					label3
				}
			};

			layout.Layout(new Rectangle(0, 0, 912, 912));

			Assert.Equal(new Rectangle(0, 0, 912, 912), layout.Bounds);
			Assert.Equal(new Rectangle(0, 0, 912, 20), label3.Bounds);
			Assert.Equal(new Rectangle(0, 20, 912, 20), label2.Bounds);
			Assert.Equal(new Rectangle(0, 40, 912, 20), label1.Bounds);
			Assert.Equal(new Rectangle(0, 60, 912, 20), label0.Bounds);
		}
	}
}
