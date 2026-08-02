using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class MarginTests : BaseTestFixture
	{
		public MarginTests()
		{
			Device.PlatformServices = new MockPlatformServices(getNativeSizeFunc: (b, d, e) => new SizeRequest(new Size(100, 50)));
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void GetSizeRequestIncludesMargins()
		{
			var parent = new ContentView
			{
				IsPlatformEnabled = true,
			};
			var child = new Button
			{
				Text = "Test",
				IsPlatformEnabled = true,
			};


			child.Margin = new Thickness(10, 20, 30, 40);
			parent.Content = child;

			var result = parent.Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.IncludeMargins);
			Assert.Equal(new Size(140, 110), result.Request);
		}

		[Fact]
		public void MarginsAffectPositionInContentView()
		{
			var parent = new ContentView
			{
				IsPlatformEnabled = true,
			};
			var child = new Button
			{
				Text = "Test",
				IsPlatformEnabled = true,
			};


			child.Margin = new Thickness(10, 20, 30, 40);
			parent.Content = child;

			parent.Layout(new Rectangle(0, 0, 140, 110));
			Assert.Equal(new Rectangle(10, 20, 100, 50), child.Bounds);
		}

		[Fact]
		public void ChangingMarginCausesRelayout()
		{
			var parent = new ContentView
			{
				IsPlatformEnabled = true,
			};
			var child = new Button
			{
				Text = "Test",
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Start,
				IsPlatformEnabled = true,
			};


			child.Margin = new Thickness(10, 20, 30, 40);
			parent.Content = child;

			parent.Layout(new Rectangle(0, 0, 1000, 1000));
			Assert.Equal(new Rectangle(10, 20, 100, 50), child.Bounds);
		}

		[Fact]
		public void IntegrationTest()
		{
			var parent = new StackLayout
			{
				Spacing = 0,
				IsPlatformEnabled = true,
			};

			var child1 = new Button
			{
				Text = "Test",
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Start,
				IsPlatformEnabled = true,
			};

			var child2 = new Button
			{
				Text = "Test",
				IsPlatformEnabled = true,
			};

			child2.Margin = new Thickness(5, 10, 15, 20);

			parent.Children.Add(child1);
			parent.Children.Add(child2);

			parent.Layout(new Rectangle(0, 0, 1000, 1000));

			Assert.Equal(new Rectangle(0, 0, 100, 50), child1.Bounds);
			Assert.Equal(new Rectangle(5, 60, 980, 50), child2.Bounds);

			child1.Margin = new Thickness(10, 20, 30, 40);

			Assert.Equal(new Rectangle(10, 20, 100, 50), child1.Bounds);
			Assert.Equal(new Rectangle(5, 120, 980, 50), child2.Bounds);
		}
	}
}