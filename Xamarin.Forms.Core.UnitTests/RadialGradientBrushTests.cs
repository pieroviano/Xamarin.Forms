using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class RadialGradientBrushTests : BaseTestFixture
	{
		public RadialGradientBrushTests()
		{
		}

		[Fact]
		public void TestConstructor()
		{
			RadialGradientBrush radialGradientBrush = new RadialGradientBrush();

			int gradientStops = radialGradientBrush.GradientStops.Count;

			Assert.Equal(0, gradientStops);
		}

		[Fact]
		public void TestConstructorUsingGradientStopCollection()
		{
			var gradientStops = new GradientStopCollection
			{
				new GradientStop { Color = Color.Red, Offset = 0.1f },
				new GradientStop { Color = Color.Orange, Offset = 0.8f }
			};

			RadialGradientBrush radialGradientBrush = new RadialGradientBrush(gradientStops, new Point(0, 0), 10);

			Assert.NotEqual(0, radialGradientBrush.GradientStops.Count);
			Assert.Equal(0, radialGradientBrush.Center.X);
			Assert.Equal(0, radialGradientBrush.Center.Y);
			Assert.Equal(10, radialGradientBrush.Radius);
		}

		[Fact]
		public void TestEmptyRadialGradientBrush()
		{
			RadialGradientBrush nullRadialGradientBrush = new RadialGradientBrush();
			Assert.Equal(true, nullRadialGradientBrush.IsEmpty);

			RadialGradientBrush radialGradientBrush = new RadialGradientBrush
			{
				Center = new Point(0, 0),
				Radius = 10,
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Orange, Offset = 0.1f },
					new GradientStop { Color = Color.Red, Offset = 0.8f }
				}
			};

			Assert.Equal(false, radialGradientBrush.IsEmpty);
		}

		[Fact]
		public void TestNullOrEmptyRadialGradientBrush()
		{
			RadialGradientBrush nullRadialGradientBrush = null;
			Assert.Equal(true, Brush.IsNullOrEmpty(nullRadialGradientBrush));

			RadialGradientBrush emptyRadialGradientBrush = new RadialGradientBrush();
			Assert.Equal(true, Brush.IsNullOrEmpty(emptyRadialGradientBrush));

			RadialGradientBrush radialGradientBrush = new RadialGradientBrush
			{
				Center = new Point(0, 0),
				Radius = 10,
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Orange, Offset = 0.1f },
					new GradientStop { Color = Color.Red, Offset = 0.8f }
				}
			};

			Assert.Equal(false, Brush.IsNullOrEmpty(radialGradientBrush));
		}

		[Fact]
		public void TestRadialGradientBrushRadius()
		{
			RadialGradientBrush radialGradientBrush = new RadialGradientBrush();
			radialGradientBrush.Radius = 20;

			Assert.Equal(20, radialGradientBrush.Radius);
		}

		[Fact]
		public void TestRadialGradientBrushOnlyOneGradientStop()
		{
			RadialGradientBrush radialGradientBrush = new RadialGradientBrush
			{
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Red, }
				},
				Radius = 20
			};

			Assert.NotNull(radialGradientBrush);
		}

		[Fact]
		public void TestRadialGradientBrushGradientStops()
		{
			RadialGradientBrush radialGradientBrush = new RadialGradientBrush
			{
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Red, Offset = 0.1f },
					new GradientStop { Color = Color.Blue, Offset = 1.0f }
				},
				Radius = 20
			};

			Assert.Equal(2, radialGradientBrush.GradientStops.Count);
		}
	}
}