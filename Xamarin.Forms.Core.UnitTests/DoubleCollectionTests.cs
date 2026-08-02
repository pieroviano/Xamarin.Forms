using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class DoubleCollectionTests
	{
		DoubleCollectionConverter _doubleCollectionConverter;

		public DoubleCollectionTests()
		{
			_doubleCollectionConverter = new DoubleCollectionConverter();
		}

		[Fact]
		public void ConvertStringToDoubleCollectionTest()
		{
			DoubleCollection result = _doubleCollectionConverter.ConvertFromInvariantString("10,110 60,10 110,110") as DoubleCollection;

			Assert.NotNull(result);
			Assert.Equal(6, result.Count);
		}
	}
}