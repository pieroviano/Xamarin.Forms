using System;

namespace Xamarin.Forms.Platform.GTK.Extensions
{
	public static class ColorExtensions
	{
		public static Gdk.Color ToGtkColor(this Color color)
		{
			string hex = color.ToRgbaColor();

			// gdk_color_parse is deprecated; gdk_rgba_parse is the GTK3 entry point. Gdk.Color
			// keeps 16-bit channels, so scale the RGBA doubles back up rather than truncating.
			var rgba = new Gdk.RGBA();

			if (!rgba.Parse(hex))
				return new Gdk.Color();

			return new Gdk.Color(
				(byte)Math.Round(rgba.Red * 255),
				(byte)Math.Round(rgba.Green * 255),
				(byte)Math.Round(rgba.Blue * 255));
		}

		internal static Xamarin.Forms.Color ToXFColor(this Gdk.Color color, double opacity = 1)
		{
			// Gdk.Color channels are ushort 0..65535; Xamarin.Forms.Color expects 0..1 and CLAMPS
			// (Color.cs: _r = (float)w.Clamp(0, 1)). Passing the raw channels therefore saturated
			// every non-zero component to 1.0, so every converted colour came out as pure
			// red/green/blue/white. The default opacity had the same bug - 255 clamped to 1.
			const double Max = 65535.0;

			return new Color(color.Red / Max, color.Green / Max, color.Blue / Max, opacity);
		}

		internal static string ToRgbaColor(this Color color)
		{
			int red = (int)(color.R * 255);
			int green = (int)(color.G * 255);
			int blue = (int)(color.B * 255);

			return string.Format("#{0:X2}{1:X2}{2:X2}", red, green, blue);
		}

		internal static bool IsDefaultOrTransparent(this Color color)
		{
			return color == Color.Transparent || color == Color.Default;
		}

	}
}
