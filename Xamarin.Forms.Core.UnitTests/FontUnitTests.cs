using System.Globalization;
using Xunit;



namespace Xamarin.Forms.Core.UnitTests
{
	public class FontUnitTests : BaseTestFixture
	{
		[Fact]
		public void TestFontForSize()
		{
			var font = Font.OfSize("Foo", 12);
			Assert.Equal("Foo", font.FontFamily);
			Assert.Equal(12, font.FontSize);
			Assert.Equal((NamedSize)0, font.NamedSize);
		}

		[Fact]
		public void TestFontForSizeDouble()
		{
			var font = Font.OfSize("Foo", 12.7);
			Assert.Equal("Foo", font.FontFamily);
			Assert.Equal(12.7, font.FontSize);
			Assert.Equal((NamedSize)0, font.NamedSize);
		}

		[Fact]
		public void TestFontForNamedSize()
		{
			var font = Font.OfSize("Foo", NamedSize.Large);
			Assert.Equal("Foo", font.FontFamily);
			Assert.Equal(0, font.FontSize);
			Assert.Equal(NamedSize.Large, font.NamedSize);
		}

		[Fact]
		public void TestSystemFontOfSize()
		{
			var font = Font.SystemFontOfSize(12);
			Assert.Equal(null, font.FontFamily);
			Assert.Equal(12, font.FontSize);
			Assert.Equal((NamedSize)0, font.NamedSize);

			font = Font.SystemFontOfSize(NamedSize.Medium);
			Assert.Equal(null, font.FontFamily);
			Assert.Equal(0, font.FontSize);
			Assert.Equal(NamedSize.Medium, font.NamedSize);
		}

		[Theory]
		[InlineData("en-US")]
		[InlineData("tr-TR")]
		[InlineData("fr-FR")]
		public void CultureTestSystemFontOfSizeDouble(string culture)
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);

			var font = Font.SystemFontOfSize(12.7);
			Assert.Equal(null, font.FontFamily);
			Assert.Equal(12.7, font.FontSize);
			Assert.Equal((NamedSize)0, font.NamedSize);

			font = Font.SystemFontOfSize(NamedSize.Medium);
			Assert.Equal(null, font.FontFamily);
			Assert.Equal(0, font.FontSize);
			Assert.Equal(NamedSize.Medium, font.NamedSize);
		}

		[Fact]
		public void TestEquality()
		{
			var font1 = Font.SystemFontOfSize(12);
			var font2 = Font.SystemFontOfSize(12);

			Assert.True(font1 == font2);
			Assert.False(font1 != font2);

			font2 = Font.SystemFontOfSize(13);

			Assert.False(font1 == font2);
			Assert.True(font1 != font2);
		}

		[Fact]
		public void TestHashCode()
		{
			var font1 = Font.SystemFontOfSize(12);
			var font2 = Font.SystemFontOfSize(12);

			Assert.True(font1.GetHashCode() == font2.GetHashCode());

			font2 = Font.SystemFontOfSize(13);

			Assert.False(font1.GetHashCode() == font2.GetHashCode());
		}

		[Fact]
		public void TestEquals()
		{
			var font = Font.SystemFontOfSize(12);

			Assert.False(font.Equals(null));
			Assert.True(font.Equals(font));
			Assert.False(font.Equals("Font"));
			Assert.True(font.Equals(Font.SystemFontOfSize(12)));
		}

		[Fact]
		public void TestFontConverter()
		{
			var converter = new FontTypeConverter();
			Assert.True(converter.CanConvertFrom(typeof(string)));
			Assert.Equal(Font.SystemFontOfSize(NamedSize.Medium), converter.ConvertFromInvariantString("Medium"));
			Assert.Equal(Font.SystemFontOfSize(42), converter.ConvertFromInvariantString("42"));
			Assert.Equal(Font.OfSize("Foo", NamedSize.Micro), converter.ConvertFromInvariantString("Foo, Micro"));
			Assert.Equal(Font.OfSize("Foo", 42), converter.ConvertFromInvariantString("Foo, 42"));
			Assert.Equal(Font.OfSize("Foo", 12.7), converter.ConvertFromInvariantString("Foo, 12.7"));
			Assert.Equal(Font.SystemFontOfSize(NamedSize.Large, FontAttributes.Bold), converter.ConvertFromInvariantString("Bold, Large"));
			Assert.Equal(Font.SystemFontOfSize(42, FontAttributes.Bold), converter.ConvertFromInvariantString("Bold, 42"));
			Assert.Equal(Font.OfSize("Foo", NamedSize.Medium), converter.ConvertFromInvariantString("Foo"));
			Assert.Equal(Font.OfSize("Foo", NamedSize.Large).WithAttributes(FontAttributes.Bold), converter.ConvertFromInvariantString("Foo, Bold, Large"));
			Assert.Equal(Font.OfSize("Foo", NamedSize.Large).WithAttributes(FontAttributes.Italic), converter.ConvertFromInvariantString("Foo, Italic, Large"));
			Assert.Equal(Font.OfSize("Foo", NamedSize.Large).WithAttributes(FontAttributes.Bold | FontAttributes.Italic), converter.ConvertFromInvariantString("Foo, Bold, Italic, Large"));
			Assert.Equal(Font.OfSize("Foo", 12).WithAttributes(FontAttributes.Bold), converter.ConvertFromInvariantString("Foo, Bold, 12"));
			Assert.Equal(Font.OfSize("Foo", 12.7).WithAttributes(FontAttributes.Bold), converter.ConvertFromInvariantString("Foo, Bold, 12.7"));
			Assert.Equal(Font.OfSize("Foo", 12).WithAttributes(FontAttributes.Italic), converter.ConvertFromInvariantString("Foo, Italic, 12"));
			Assert.Equal(Font.OfSize("Foo", 12.7).WithAttributes(FontAttributes.Italic), converter.ConvertFromInvariantString("Foo, Italic, 12.7"));
			Assert.Equal(Font.OfSize("Foo", 12).WithAttributes(FontAttributes.Bold | FontAttributes.Italic), converter.ConvertFromInvariantString("Foo, Bold, Italic, 12"));
			Assert.Equal(Font.OfSize("Foo", 12.7).WithAttributes(FontAttributes.Bold | FontAttributes.Italic), converter.ConvertFromInvariantString("Foo, Bold, Italic, 12.7"));
		}

		[Fact]
		public void TestFontParsing()
		{
			var input = "PTM55FT#PTMono-Regular";
			var input2 = "PTM55FT.ttf#PTMono-Regular";
			var input3 = "CuteFont-Regular";

			var font1 = FontFile.FromString(input);
			var font2 = FontFile.FromString(input2);
			var font3 = FontFile.FromString(input3);

			Assert.Equal(font1.FileName, "PTM55FT");
			Assert.Equal(font1.PostScriptName, "PTMono-Regular");
			Assert.Null(font1.Extension);


			Assert.Equal(font2.FileName, "PTM55FT");
			Assert.Equal(font2.PostScriptName, "PTMono-Regular");
			Assert.Equal(font2.Extension, ".ttf");


			Assert.Equal(font3.FileName, "CuteFont-Regular");
			Assert.Equal(font3.PostScriptName, "CuteFont-Regular");
			Assert.Null(font3.Extension);

		}

	}
}
