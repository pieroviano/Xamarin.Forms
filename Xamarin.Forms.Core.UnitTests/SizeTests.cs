using Xunit;



namespace Xamarin.Forms.Core.UnitTests
{
	public class SizeTests : BaseTestFixture
	{
		[Fact]
		public void TestSizeIsZero()
		{
			var size = new Size();

			Assert.True(size.IsZero);

			size = new Size(10, 10);

			Assert.False(size.IsZero);
		}

		[Fact]
		public void TestSizeAdd()
		{
			var size1 = new Size(10, 10);
			var size2 = new Size(20, 20);

			var result = size1 + size2;

			Assert.Equal(new Size(30, 30), result);
		}

		[Fact]
		public void TestSizeSubtract()
		{
			var size1 = new Size(10, 10);
			var size2 = new Size(2, 2);

			var result = size1 - size2;

			Assert.Equal(new Size(8, 8), result);
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(0, 1)]
		[InlineData(0, 2)]
		[InlineData(1, 0)]
		[InlineData(1, 1)]
		[InlineData(1, 2)]
		[InlineData(2, 0)]
		[InlineData(2, 1)]
		[InlineData(2, 2)]
		public void TestPointFromSize(double x, double y)
		{
			var size = new Size(x, y);
			var point = (Point)size;

			Assert.Equal(x, point.X);
			Assert.Equal(y, point.Y);
		}

		[Theory]
		[InlineData(3, 3, 3, 3)]
		[InlineData(3, 3, 3, 4)]
		[InlineData(3, 3, 3, 5)]
		[InlineData(3, 3, 4, 3)]
		[InlineData(3, 3, 4, 4)]
		[InlineData(3, 3, 4, 5)]
		[InlineData(3, 3, 5, 3)]
		[InlineData(3, 3, 5, 4)]
		[InlineData(3, 3, 5, 5)]
		[InlineData(3, 4, 3, 3)]
		[InlineData(3, 4, 3, 4)]
		[InlineData(3, 4, 3, 5)]
		[InlineData(3, 4, 4, 3)]
		[InlineData(3, 4, 4, 4)]
		[InlineData(3, 4, 4, 5)]
		[InlineData(3, 4, 5, 3)]
		[InlineData(3, 4, 5, 4)]
		[InlineData(3, 4, 5, 5)]
		[InlineData(3, 5, 3, 3)]
		[InlineData(3, 5, 3, 4)]
		[InlineData(3, 5, 3, 5)]
		[InlineData(3, 5, 4, 3)]
		[InlineData(3, 5, 4, 4)]
		[InlineData(3, 5, 4, 5)]
		[InlineData(3, 5, 5, 3)]
		[InlineData(3, 5, 5, 4)]
		[InlineData(3, 5, 5, 5)]
		[InlineData(4, 3, 3, 3)]
		[InlineData(4, 3, 3, 4)]
		[InlineData(4, 3, 3, 5)]
		[InlineData(4, 3, 4, 3)]
		[InlineData(4, 3, 4, 4)]
		[InlineData(4, 3, 4, 5)]
		[InlineData(4, 3, 5, 3)]
		[InlineData(4, 3, 5, 4)]
		[InlineData(4, 3, 5, 5)]
		[InlineData(4, 4, 3, 3)]
		[InlineData(4, 4, 3, 4)]
		[InlineData(4, 4, 3, 5)]
		[InlineData(4, 4, 4, 3)]
		[InlineData(4, 4, 4, 4)]
		[InlineData(4, 4, 4, 5)]
		[InlineData(4, 4, 5, 3)]
		[InlineData(4, 4, 5, 4)]
		[InlineData(4, 4, 5, 5)]
		[InlineData(4, 5, 3, 3)]
		[InlineData(4, 5, 3, 4)]
		[InlineData(4, 5, 3, 5)]
		[InlineData(4, 5, 4, 3)]
		[InlineData(4, 5, 4, 4)]
		[InlineData(4, 5, 4, 5)]
		[InlineData(4, 5, 5, 3)]
		[InlineData(4, 5, 5, 4)]
		[InlineData(4, 5, 5, 5)]
		[InlineData(5, 3, 3, 3)]
		[InlineData(5, 3, 3, 4)]
		[InlineData(5, 3, 3, 5)]
		[InlineData(5, 3, 4, 3)]
		[InlineData(5, 3, 4, 4)]
		[InlineData(5, 3, 4, 5)]
		[InlineData(5, 3, 5, 3)]
		[InlineData(5, 3, 5, 4)]
		[InlineData(5, 3, 5, 5)]
		[InlineData(5, 4, 3, 3)]
		[InlineData(5, 4, 3, 4)]
		[InlineData(5, 4, 3, 5)]
		[InlineData(5, 4, 4, 3)]
		[InlineData(5, 4, 4, 4)]
		[InlineData(5, 4, 4, 5)]
		[InlineData(5, 4, 5, 3)]
		[InlineData(5, 4, 5, 4)]
		[InlineData(5, 4, 5, 5)]
		[InlineData(5, 5, 3, 3)]
		[InlineData(5, 5, 3, 4)]
		[InlineData(5, 5, 3, 5)]
		[InlineData(5, 5, 4, 3)]
		[InlineData(5, 5, 4, 4)]
		[InlineData(5, 5, 4, 5)]
		[InlineData(5, 5, 5, 3)]
		[InlineData(5, 5, 5, 4)]
		[InlineData(5, 5, 5, 5)]
		public void HashCode(double w1, double h1, double w2, double h2)
		{
			bool result = new Size(w1, h1).GetHashCode() == new Size(w2, h2).GetHashCode();

			if (w1 == w2 && h1 == h2)
				Assert.True(result);
			else
				Assert.False(result);
		}

		[Fact]
		public void Equality()
		{
			Assert.False(new Size().Equals(null));
			Assert.False(new Size().Equals("Size"));
			Assert.True(new Size(2, 3).Equals(new Size(2, 3)));

			Assert.True(new Size(2, 3) == new Size(2, 3));
			Assert.True(new Size(2, 3) != new Size(3, 2));
		}

		[Theory]
		[InlineData(0, 0, "{Width=0 Height=0}")]
		[InlineData(1, 5, "{Width=1 Height=5}")]
		public void TestToString(double w, double h, string expected)
		{
			Assert.Equal(expected, new Size(w, h).ToString());
		}

		[Theory]
		[InlineData(12, 12, 0.0)]
		[InlineData(12, 12, 2.0)]
		[InlineData(12, 12, 7.0)]
		[InlineData(12, 12, 0.25)]
		[InlineData(12, 13, 0.0)]
		[InlineData(12, 13, 2.0)]
		[InlineData(12, 13, 7.0)]
		[InlineData(12, 13, 0.25)]
		[InlineData(12, 14, 0.0)]
		[InlineData(12, 14, 2.0)]
		[InlineData(12, 14, 7.0)]
		[InlineData(12, 14, 0.25)]
		[InlineData(12, 15, 0.0)]
		[InlineData(12, 15, 2.0)]
		[InlineData(12, 15, 7.0)]
		[InlineData(12, 15, 0.25)]
		[InlineData(13, 12, 0.0)]
		[InlineData(13, 12, 2.0)]
		[InlineData(13, 12, 7.0)]
		[InlineData(13, 12, 0.25)]
		[InlineData(13, 13, 0.0)]
		[InlineData(13, 13, 2.0)]
		[InlineData(13, 13, 7.0)]
		[InlineData(13, 13, 0.25)]
		[InlineData(13, 14, 0.0)]
		[InlineData(13, 14, 2.0)]
		[InlineData(13, 14, 7.0)]
		[InlineData(13, 14, 0.25)]
		[InlineData(13, 15, 0.0)]
		[InlineData(13, 15, 2.0)]
		[InlineData(13, 15, 7.0)]
		[InlineData(13, 15, 0.25)]
		[InlineData(14, 12, 0.0)]
		[InlineData(14, 12, 2.0)]
		[InlineData(14, 12, 7.0)]
		[InlineData(14, 12, 0.25)]
		[InlineData(14, 13, 0.0)]
		[InlineData(14, 13, 2.0)]
		[InlineData(14, 13, 7.0)]
		[InlineData(14, 13, 0.25)]
		[InlineData(14, 14, 0.0)]
		[InlineData(14, 14, 2.0)]
		[InlineData(14, 14, 7.0)]
		[InlineData(14, 14, 0.25)]
		[InlineData(14, 15, 0.0)]
		[InlineData(14, 15, 2.0)]
		[InlineData(14, 15, 7.0)]
		[InlineData(14, 15, 0.25)]
		[InlineData(15, 12, 0.0)]
		[InlineData(15, 12, 2.0)]
		[InlineData(15, 12, 7.0)]
		[InlineData(15, 12, 0.25)]
		[InlineData(15, 13, 0.0)]
		[InlineData(15, 13, 2.0)]
		[InlineData(15, 13, 7.0)]
		[InlineData(15, 13, 0.25)]
		[InlineData(15, 14, 0.0)]
		[InlineData(15, 14, 2.0)]
		[InlineData(15, 14, 7.0)]
		[InlineData(15, 14, 0.25)]
		[InlineData(15, 15, 0.0)]
		[InlineData(15, 15, 2.0)]
		[InlineData(15, 15, 7.0)]
		[InlineData(15, 15, 0.25)]
		public void MultiplyByScalar(int w, int h, double scalar)
		{
			var size = new Size(w, h);
			var result = size * scalar;

			Assert.Equal(w * scalar, result.Width);
			Assert.Equal(h * scalar, result.Height);
		}
	}
}
