using System;
using System.Collections.Generic;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class ColorUnitTests : BaseTestFixture
	{
		[Fact]
		public void TestHSLPostSetEquality()
		{
			var color = new Color(1, 0.5, 0.2);
			var color2 = color;

			color2 = color.WithLuminosity(.2);
			Assert.False(color == color2);
		}

		[Fact]
		public void TestHSLPostSetInequality()
		{
			var color = new Color(1, 0.5, 0.2);
			var color2 = color;

			color2 = color.WithLuminosity(.2);

			Assert.True(color != color2);
		}

		[Fact]
		public void TestHSLSetToDefaultValue()
		{
			var color = new Color(0.2, 0.5, 0.8);

			// saturation is initialized to 0, make sure we still update
			color = color.WithSaturation(0);

			Assert.Equal(color.R, color.G);
			Assert.Equal(color.R, color.B);
		}

		[Fact]
		public void TestHSLModifiers()
		{
			var color = Color.Default;
			Assert.Throws<InvalidOperationException>(() => color.WithHue(.1));
			Assert.Throws<InvalidOperationException>(() => color.WithLuminosity(.1));
			Assert.Throws<InvalidOperationException>(() => color.WithSaturation(.1));

			color = Color.FromHsla(.8, .6, .2);
			Assert.Equal(Color.FromHsla(.1, .6, .2), color.WithHue(.1));
			Assert.Equal(Color.FromHsla(.8, .1, .2), color.WithSaturation(.1));
			Assert.Equal(Color.FromHsla(.8, .6, .1), color.WithLuminosity(.1));
		}

		[Fact]
		public void TestMultiplyAlpha()
		{
			var color = new Color(1, 1, 1, 1);
			color = color.MultiplyAlpha(0.25);
			Assert.Equal(.25, color.A);

			color = Color.Default;
			Assert.Throws<InvalidOperationException>(() => color = color.MultiplyAlpha(0.25));

			color = Color.FromHsla(1, 1, 1, 1);
			color = color.MultiplyAlpha(0.25);
			Assert.Equal(.25, color.A);
		}

		[Fact]
		public void TestClamping()
		{
			var color = new Color(2, 2, 2, 2);

			Assert.Equal(1, color.R);
			Assert.Equal(1, color.G);
			Assert.Equal(1, color.B);
			Assert.Equal(1, color.A);

			color = new Color(-1, -1, -1, -1);

			Assert.Equal(0, color.R);
			Assert.Equal(0, color.G);
			Assert.Equal(0, color.B);
			Assert.Equal(0, color.A);
		}

		[Fact]
		public void TestRGBToHSL()
		{
			var color = new Color(.5, .1, .1);

			Assert.Equal(1, color.Hue, 0.001);
			Assert.Equal(0.662, color.Saturation, 0.01);
			Assert.Equal(0.302, color.Luminosity, 0.01);
		}

		[Fact]
		public void TestHSLToRGB()
		{
			var color = Color.FromHsla(0, .662, .302);

			Assert.Equal(0.5, color.R, 0.01);
			Assert.Equal(0.1, color.G, 0.01);
			Assert.Equal(0.1, color.B, 0.01);
		}

		[Fact]
		public void TestColorFromValue()
		{
			var color = new Color(0.2);

			Assert.Equal(new Color(0.2, 0.2, 0.2, 1), color);
		}

		[Fact]
		public void TestAddLuminosity()
		{
			var color = new Color(0.2);
			var brighter = color.AddLuminosity(0.2);
			Assert.Equal(color.Luminosity + 0.2, brighter.Luminosity, 0.001);

			color = Color.Default;
			Assert.Throws<InvalidOperationException>(() => color.AddLuminosity(0.2));
		}

		[Fact]
		public void TestZeroLuminosity()
		{
			var color = new Color(0.1, 0.2, 0.3);
			color = color.AddLuminosity(-1);

			Assert.Equal(0, color.Luminosity);
			Assert.Equal(0, color.R);
			Assert.Equal(0, color.G);
			Assert.Equal(0, color.B);
		}

		[Fact]
		public void TestHashCode()
		{
			var color1 = new Color(0.1);
			var color2 = new Color(0.1);

			Assert.True(color1.GetHashCode() == color2.GetHashCode());
			color2 = Color.FromHsla(color2.Hue, color2.Saturation, .5);

			Assert.False(color1.GetHashCode() == color2.GetHashCode());
		}

		[Fact]
		public void TestHashCodeNamedColors()
		{
			Color red = Color.Red; //R=1, G=0, B=0, A=1
			int hashRed = red.GetHashCode();

			Color blue = Color.Blue; //R=0, G=0, B=1, A=1
			int hashBlue = blue.GetHashCode();

			Assert.False(hashRed == hashBlue);
		}

		[Fact]
		public void TestHashCodeAll()
		{
			Dictionary<int, Color> colorsAndHashes = new Dictionary<int, Color>();
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Transparent.GetHashCode(), Color.Transparent));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Aqua.GetHashCode(), Color.Aqua));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Black.GetHashCode(), Color.Black));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Blue.GetHashCode(), Color.Blue));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Fuchsia.GetHashCode(), Color.Fuchsia));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Gray.GetHashCode(), Color.Gray));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Green.GetHashCode(), Color.Green));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Lime.GetHashCode(), Color.Lime));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Maroon.GetHashCode(), Color.Maroon));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Navy.GetHashCode(), Color.Navy));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Olive.GetHashCode(), Color.Olive));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Purple.GetHashCode(), Color.Purple));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Pink.GetHashCode(), Color.Pink));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Red.GetHashCode(), Color.Red));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Silver.GetHashCode(), Color.Silver));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Teal.GetHashCode(), Color.Teal));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.White.GetHashCode(), Color.White));
			AssertEx.DoesNotThrow(() => colorsAndHashes.Add(Color.Yellow.GetHashCode(), Color.Yellow));
		}

		[Fact]
		public void TestSetHue()
		{
			var color = new Color(0.2, 0.5, 0.7);
			color = Color.FromHsla(.2, color.Saturation, color.Luminosity);

			Assert.Equal(0.6, color.R, 0.001);
			Assert.Equal(0.7, color.G, 0.001);
			Assert.Equal(0.2, color.B, 0.001);
		}

		[Fact]
		public void ZeroLuminToRGB()
		{
			var color = new Color(0);
			Assert.Equal(0, color.Luminosity);
			Assert.Equal(0, color.Hue);
			Assert.Equal(0, color.Saturation);
		}

		[Fact]
		public void TestToString()
		{
			var color = new Color(1, 1, 1, 0.5);
			Assert.Equal("[Color: A=0.5, R=1, G=1, B=1, Hue=0, Saturation=0, Luminosity=1]", color.ToString());
		}

		[Fact]
		public void TestFromHex()
		{
			var color = Color.FromRgb(138, 43, 226);
			Assert.Equal(color, Color.FromHex("8a2be2"));

			Assert.Equal(Color.FromRgba(138, 43, 226, 128), Color.FromHex("#808a2be2"));
			Assert.Equal(Color.FromHex("#aabbcc"), Color.FromHex("#abc"));
			Assert.Equal(Color.FromHex("#aabbccdd"), Color.FromHex("#abcd"));
		}

		[Fact]
		public void TestToHex()
		{
			var colorRgb = Color.FromRgb(138, 43, 226);
			Assert.Equal(Color.FromHex(colorRgb.ToHex()), colorRgb);
			var colorRgba = Color.FromRgba(138, 43, 226, .2);
			Assert.Equal(Color.FromHex(colorRgba.ToHex()), colorRgba);
			var colorHsl = Color.FromHsla(240, 1, 1);
			Assert.Equal(Color.FromHex(colorHsl.ToHex()), colorHsl);
			var colorHsla = Color.FromHsla(240, 1, 1, .1);
			var hexFromHsla = Color.FromHex(colorHsla.ToHex());
			Assert.Equal(colorHsla.A, hexFromHsla.A, 0.002);
			Assert.Equal(colorHsla.R, hexFromHsla.R, 0.001);
			Assert.Equal(colorHsla.G, hexFromHsla.G, 0.001);
			Assert.Equal(colorHsla.B, hexFromHsla.B, 0.001);
		}

		[Fact]
		public void TestFromHsv()
		{
			var color = Color.FromRgb(1, .29, .752);
			var colorHsv = Color.FromHsv(321, 71, 100);
			Assert.Equal(colorHsv.R, color.R, 0.001);
			Assert.Equal(colorHsv.G, color.G, 0.001);
			Assert.Equal(colorHsv.B, color.B, 0.001);
		}

		[Fact]
		public void TestFromHsva()
		{
			var color = Color.FromRgba(1, .29, .752, .5);
			var colorHsv = Color.FromHsva(321, 71, 100, 50);
			Assert.Equal(colorHsv.R, color.R, 0.001);
			Assert.Equal(colorHsv.G, color.G, 0.001);
			Assert.Equal(colorHsv.B, color.B, 0.001);
			Assert.Equal(colorHsv.A, color.A, 0.001);
		}

		[Fact]
		public void TestFromHsvDouble()
		{
			var color = Color.FromRgb(1, .29, .758);
			var colorHsv = Color.FromHsv(.89, .71, 1);
			Assert.Equal(colorHsv.R, color.R, 0.001);
			Assert.Equal(colorHsv.G, color.G, 0.001);
			Assert.Equal(colorHsv.B, color.B, 0.001);
		}

		[Fact]
		public void TestFromHsvaDouble()
		{
			var color = Color.FromRgba(1, .29, .758, .5);
			var colorHsv = Color.FromHsva(.89, .71, 1, .5);
			Assert.Equal(colorHsv.R, color.R, 0.001);
			Assert.Equal(colorHsv.G, color.G, 0.001);
			Assert.Equal(colorHsv.B, color.B, 0.001);
			Assert.Equal(colorHsv.A, color.A, 0.001);
		}

		[Fact]
		public void FromRGBDouble()
		{
			var color = Color.FromRgb(0.2, 0.3, 0.4);

			Assert.Equal(new Color(0.2, 0.3, 0.4), color);
		}

		[Fact]
		public void FromRGBADouble()
		{
			var color = Color.FromRgba(0.2, 0.3, 0.4, 0.5);

			Assert.Equal(new Color(0.2, 0.3, 0.4, 0.5), color);
		}

		[Fact]
		public void TestColorTypeConverter()
		{
			var converter = new ColorTypeConverter();
			Assert.True(converter.CanConvertFrom(typeof(string)));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("Color.Blue"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("Blue"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("blue"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("#0000ff"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("#00f"));
			Assert.Equal(Color.Blue.MultiplyAlpha(2.0 / 3.0), converter.ConvertFromInvariantString("#a00f"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("rgb(0,0, 255)"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("rgb(0,0, 300)"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("rgb(0,0, 300)"));
			Assert.Equal(Color.Blue.MultiplyAlpha(.8), converter.ConvertFromInvariantString("rgba(0%,0%, 100%, .8)"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("rgb(0%,0%, 110%)"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("hsl(240,100%, 50%)"));
			Assert.Equal(Color.Blue, converter.ConvertFromInvariantString("hsl(240,110%, 50%)"));
			Assert.Equal(Color.Blue.MultiplyAlpha(.8), converter.ConvertFromInvariantString("hsla(240,100%, 50%, .8)"));
			Assert.Equal(Color.FromHsla(0.66916666666666669, 1, 0.5), converter.ConvertFromInvariantString("hsl(240.9,100%, 50%)"));
			Assert.Equal(Color.FromHsva(0.89166666666666669, .71, 1, .8), converter.ConvertFromInvariantString("hsva(321,71%, 100%, .8)"));
			Assert.Equal(Color.FromHsv(0.89166666666666669, .71, 1), converter.ConvertFromInvariantString("hsv(321,71%, 100%)"));
			Assert.Equal(Color.Default, converter.ConvertFromInvariantString("Color.Default"));
			Assert.Equal(Color.Accent, converter.ConvertFromInvariantString("Accent"));
			var hotpink = Color.FromHex("#FF69B4");
			Color.Accent = hotpink;
			Assert.Equal(Color.Accent, converter.ConvertFromInvariantString("Accent"));
			Assert.Equal(Color.Default, converter.ConvertFromInvariantString("#12345"));
			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString(""));
			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString("rgb(0,0,255"));
			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString("hsl(12, 100%)"));
			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString("hsv(12, 100%)"));
			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString("hsva(12, 100%)"));
			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString("rgba(0,0,255)"));
			Assert.Equal(Color.FromRgb(0, 122, 255), converter.ConvertFromInvariantString("SystemBlue"));
			Assert.Equal(Color.FromHex("#FF767676"), converter.ConvertFromInvariantString("SystemChromeHighColor"));
			Assert.Equal(Color.FromHex("#ff00ddff"), converter.ConvertFromInvariantString("HoloBlueBright"));
			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString("NonExistentNamedColor"));
		}

		[Fact]
		public void TestDefault()
		{
			Assert.Equal(Color.Default, default(Color));
			Assert.Equal(Color.Default, new Color());
		}

		[Fact]
		public void TestImplicitConversionToSystemDrawingColor()
		{
			var color = Color.FromRgba(0.2, 0.3, 0.4, 0.5);
			System.Drawing.Color sdColor = color;
			Assert.Equal(51, sdColor.R);
			Assert.Equal(76, sdColor.G);
			Assert.Equal(102, sdColor.B);
			Assert.Equal(127, sdColor.A);
		}

		[Fact]
		public void TestDefaultColorToSystemDrawingColorEmpty()
		{
			Assert.Equal(System.Drawing.Color.Empty, (System.Drawing.Color)Color.Default);
		}

		[Fact]
		public void TestImplicitConversionFromSystemDrawingColor()
		{
			System.Drawing.Color sdColor = System.Drawing.Color.FromArgb(32, 64, 128, 255);
			Color color = sdColor;
			Assert.Equal(.125, color.A, .01);
			Assert.Equal(.25, color.R, .01);
			Assert.Equal(.5, color.G, .01);
			Assert.Equal(1, color.B, .01);
		}

		[Fact]
		public void TestSystemDrawingColorEmptyToColorDefault()
		{
			Assert.Equal(Color.Default, (Color)System.Drawing.Color.Empty);
		}

		[Fact]
		public void DefaultColorsMatch()
		{
			//This spot-checks a few of the fields in Color
			Assert.Equal(Color.CornflowerBlue, Color.FromRgb(100, 149, 237));
			Assert.Equal(Color.DarkSalmon, Color.FromRgb(233, 150, 122));
			Assert.Equal(Color.Transparent, Color.FromRgba(255, 255, 255, 0));
			Assert.Equal(Color.Wheat, Color.FromRgb(245, 222, 179));
			Assert.Equal(Color.White, Color.FromRgb(255, 255, 255));
		}
	}
}
