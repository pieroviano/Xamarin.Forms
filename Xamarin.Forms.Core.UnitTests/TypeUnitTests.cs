using Xunit;



namespace Xamarin.Forms.Core.UnitTests
{
	public class TypeUnitTests : BaseTestFixture
	{
		[Fact]
		public void TestVec2()
		{
			var vec2 = new Vec2();

			Assert.Equal(0, vec2.X);
			Assert.Equal(0, vec2.Y);

			vec2 = new Vec2(2, 3);

			Assert.Equal(2, vec2.X);
			Assert.Equal(3, vec2.Y);
		}
	}
}
