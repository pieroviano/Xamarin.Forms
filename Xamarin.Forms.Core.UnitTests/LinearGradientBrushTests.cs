using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class LinearGradientBrushTests : BaseTestFixture
	{
		public LinearGradientBrushTests()
		{
		}

		[Fact]
		public void TestConstructor()
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush();

			Assert.Equal(1.0d, linearGradientBrush.EndPoint.X);
			Assert.Equal(1.0d, linearGradientBrush.EndPoint.Y);
		}

		[Fact]
		public void TestConstructorUsingGradientStopCollection()
		{
			var gradientStops = new GradientStopCollection
			{
				new GradientStop { Color = Color.Red, Offset = 0.1f },
				new GradientStop { Color = Color.Orange, Offset = 0.8f }
			};

			LinearGradientBrush linearGradientBrush = new LinearGradientBrush(gradientStops, new Point(0, 0), new Point(0, 1));

			Assert.NotEqual(0, linearGradientBrush.GradientStops.Count);
			Assert.Equal(0.0d, linearGradientBrush.EndPoint.X);
			Assert.Equal(1.0d, linearGradientBrush.EndPoint.Y);
		}

		[Fact]
		public void TestEmptyLinearGradientBrush()
		{
			LinearGradientBrush nullLinearGradientBrush = new LinearGradientBrush();
			Assert.Equal(true, nullLinearGradientBrush.IsEmpty);

			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0),
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Orange, Offset = 0.1f },
					new GradientStop { Color = Color.Red, Offset = 0.8f }
				}
			};

			Assert.Equal(false, linearGradientBrush.IsEmpty);
		}

		[Fact]
		public void TestNullOrEmptyLinearGradientBrush()
		{
			LinearGradientBrush nullLinearGradientBrush = null;
			Assert.Equal(true, Brush.IsNullOrEmpty(nullLinearGradientBrush));

			LinearGradientBrush emptyLinearGradientBrush = new LinearGradientBrush();
			Assert.Equal(true, Brush.IsNullOrEmpty(emptyLinearGradientBrush));

			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0),
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Orange, Offset = 0.1f },
					new GradientStop { Color = Color.Red, Offset = 0.8f }
				}
			};

			Assert.Equal(false, Brush.IsNullOrEmpty(linearGradientBrush));
		}

		[Fact]
		public void TestLinearGradientBrushPoints()
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0)
			};

			Assert.Equal(0, linearGradientBrush.StartPoint.X);
			Assert.Equal(0, linearGradientBrush.StartPoint.Y);

			Assert.Equal(1, linearGradientBrush.EndPoint.X);
			Assert.Equal(0, linearGradientBrush.EndPoint.Y);
		}

		[Fact]
		public void TestLinearGradientBrushOnlyOneGradientStop()
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Red, }
				},
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0)
			};

			Assert.NotNull(linearGradientBrush);
		}

		[Fact]
		public void TestLinearGradientBrushGradientStops()
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Color.Red, Offset = 0.1f },
					new GradientStop { Color = Color.Blue, Offset = 1.0f }
				},
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0)
			};

			Assert.Equal(2, linearGradientBrush.GradientStops.Count);
		}
	}
}