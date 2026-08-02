using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class SolidColorBrushTests : BaseTestFixture
	{
		public SolidColorBrushTests()
		{
		}

		[Fact]
		public void TestConstructor()
		{
			SolidColorBrush solidColorBrush = new SolidColorBrush();
			Assert.Equal("[Color: A=-1, R=-1, G=-1, B=-1, Hue=-1, Saturation=-1, Luminosity=-1]", solidColorBrush.Color.ToString());
		}

		[Fact]
		public void TestConstructorUsingColor()
		{
			SolidColorBrush solidColorBrush = new SolidColorBrush(Color.Red);
			Assert.Equal("[Color: A=1, R=1, G=0, B=0, Hue=1, Saturation=1, Luminosity=0.5]", solidColorBrush.Color.ToString());
		}

		[Fact]
		public void TestEmptySolidColorBrush()
		{
			SolidColorBrush solidColorBrush = new SolidColorBrush();
			Assert.Equal(true, solidColorBrush.IsEmpty);

			SolidColorBrush red = Brush.Red;
			Assert.Equal(false, red.IsEmpty);
		}

		[Fact]
		public void TestNullOrEmptySolidColorBrush()
		{
			SolidColorBrush nullSolidColorBrush = null;
			Assert.Equal(true, Brush.IsNullOrEmpty(nullSolidColorBrush));

			SolidColorBrush emptySolidColorBrush = new SolidColorBrush();
			Assert.Equal(true, Brush.IsNullOrEmpty(emptySolidColorBrush));

			SolidColorBrush solidColorBrush = Brush.Yellow;
			Assert.Equal(false, Brush.IsNullOrEmpty(solidColorBrush));
		}

		[Fact]
		public void TestSolidColorBrushFromColor()
		{
			SolidColorBrush solidColorBrush = new SolidColorBrush(Color.Red);
			Assert.NotNull(solidColorBrush.Color);
			Assert.Equal("#FFFF0000", solidColorBrush.Color.ToHex());
		}

		[Fact]
		public void TestDefaultBrushes()
		{
			SolidColorBrush black = Brush.Black;
			Assert.NotNull(black.Color);
			Assert.Equal("#FF000000", black.Color.ToHex());

			SolidColorBrush white = Brush.White;
			Assert.NotNull(white.Color);
			Assert.Equal("#FFFFFFFF", white.Color.ToHex());
		}
	}
}