using System.IO;
using Xunit;

namespace Xamarin.Forms.StyleSheets.UnitTests
{
	public class StyleParserTests
	{
		[Theory]
		[InlineData("font-size: 10", 1)]
		[InlineData("font-size: 10;", 1)]
		[InlineData("font-size: 10 background-color: blue", 0)]
		[InlineData("font-size: 10; background-color: blue", 2)]
		[InlineData("font-size: 10; background-color: blue;", 2)]
		public void TestCase(string css, int propertyCount)
		{
			using (var reader = new StringReader(css))
			{
				var parser = Style.Parse(new CssReader(reader));

				Assert.Equal(propertyCount, parser.Declarations.Count);
			}
		}
	}
}
