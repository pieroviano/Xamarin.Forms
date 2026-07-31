using System;
using Xunit;

using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class FontImageExtension : TabBar
	{
		public FontImageExtension() => InitializeComponent();
		public FontImageExtension(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public static string FontFamily => "MyFontFamily";
		public static string Glyph => "MyGlyph";
		public static Color Color => Color.Black;
		public static double Size = 50d;

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true), InlineData(false)]
			public void FontImageExtension_Positive(bool useCompiledXaml)
			{
				var layout = new FontImageExtension(useCompiledXaml);
				var tabs = layout.AllChildren;

				int i = 0;
				foreach (var tab in tabs)
				{
					Tab myTab = (Tab)tab;
					if (myTab == null)
						continue;

					Assert.IsType<FontImageSource>(myTab.Icon);

					var fontImage = (FontImageSource)myTab.Icon;
					Assert.Equal(FontFamily, fontImage.FontFamily);
					Assert.Equal(Glyph, fontImage.Glyph);

					if (i == 3)
						Assert.Equal(30d, fontImage.Size);
					else
						Assert.Equal(Size, fontImage.Size);

					Assert.Equal(Color, fontImage.Color);
					i++;
				}
			}

			[Theory]
			[InlineData(true), InlineData(false)]
			public void FontImageExtension_Negative(bool useCompiledXaml)
			{
				var layout = new FontImageExtension(useCompiledXaml);
				var tabs = layout.AllChildren;

				foreach (var tab in tabs)
				{
					Tab myTab = (Tab)tab;
					if (myTab == null)
						continue;

					// ImageSource is abstract: exactMatch:false is the analyzer-sanctioned form
					Assert.IsNotType<ImageSource>(myTab.Icon, exactMatch: false);
				}
			}
		}
	}
}