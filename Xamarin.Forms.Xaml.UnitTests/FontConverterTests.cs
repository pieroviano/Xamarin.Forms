using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class FontConverterTests : BaseTestFixture
	{
		[InlineData("Bold", Forms.FontAttributes.Bold)]
		[InlineData("Italic", Forms.FontAttributes.Italic)]
		[InlineData("Bold, Italic", Forms.FontAttributes.Bold | Forms.FontAttributes.Italic)]
		public void FontAttributes(string attributeString, FontAttributes result)
		{
			var xaml = @"
			<Label 
				xmlns=""http://xamarin.com/schemas/2014/forms""
				xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" FontAttributes=""" + result + @""" />";

			Device.PlatformServices = new MockPlatformServices();

			var label = new Label().LoadFromXaml(xaml);

			Assert.Equal(result, label.FontAttributes);
#pragma warning disable 618
			Assert.Equal(result, label.Font.FontAttributes);
#pragma warning restore 618
		}
	}
}