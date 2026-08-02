using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class BindingTypeConverterTests : BaseTestFixture
	{
		[Fact]
		public void CanConvertFrom()
		{
			var c = new BindingTypeConverter();
			Assert.True(c.CanConvertFrom(typeof(string)));
			Assert.False(c.CanConvertFrom(typeof(int)));
		}

		[Fact]
		public void Convert()
		{
			var c = new BindingTypeConverter();
			var binding = c.ConvertFromInvariantString("Path");

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Path", ((Binding)binding).Path);
			Assert.Equal(BindingMode.Default, ((Binding)binding).Mode);
		}
	}
}