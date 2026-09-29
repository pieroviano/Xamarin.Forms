using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media.Imaging;
using XF = Xamarin.Forms;

namespace System.Windows.Media
{
	[XF.TypeConverter(typeof(ColorConverter))]
	public struct Color : IEquatable<Color>
	{
		public byte A { get; set; }

		public byte R { get; set; }

		public byte G { get; set; }

		public byte B { get; set; }

		public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color { A = a, R = r, G = g, B = b };

		public static Color FromRgb(byte r, byte g, byte b) => FromArgb(255, r, g, b);

		internal static Color FromUInt32(uint argb) =>
			FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

		internal XF.Color ToForms() => XF.Color.FromRgba(R, G, B, A);

		public bool Equals(Color other) => A == other.A && R == other.R && G == other.G && B == other.B;

		public override bool Equals(object obj) => obj is Color other && Equals(other);

		public override int GetHashCode() => (A << 24) | (R << 16) | (G << 8) | B;

		public static bool operator ==(Color a, Color b) => a.Equals(b);

		public static bool operator !=(Color a, Color b) => !a.Equals(b);

		public override string ToString() => "#" + A.ToString("X2") + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
	}

	/// <summary>A color as XAML writes it: a name, or <c>#RGB</c>, <c>#ARGB</c>, <c>#RRGGBB</c>, <c>#AARRGGBB</c>.</summary>
	public sealed class ColorConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value) => Parse(value);

		public static object ConvertFromString(string value) => Parse(value);

		internal static Color Parse(string value)
		{
			var s = (value ?? throw new ArgumentNullException(nameof(value))).Trim();
			if (s.StartsWith("#", StringComparison.Ordinal))
			{
				var hex = s.Substring(1);
				if (hex.Length == 3 || hex.Length == 4)
				{
					var expanded = "";
					foreach (var c in hex)
						expanded += new string(c, 2);
					hex = expanded;
				}

				if (hex.Length == 6)
					hex = "FF" + hex;

				if (hex.Length == 8 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
					return Color.FromUInt32(argb);
			}
			else if (Colors.TryGet(s, out var named))
			{
				return named;
			}

			throw new FormatException($"'{value}' is not a color.");
		}
	}

	public abstract class Brush : Freezable
	{
		public double Opacity
		{
			get => Get<double>(OpacityProperty);
			set => SetValue(OpacityProperty, value);
		}

		public static readonly XF.BindableProperty OpacityProperty = Xamarin.Forms.Wpf.Dp.Register<Brush>(nameof(Opacity), typeof(double), 1.0);

		/// <summary>The single color Xamarin.Forms can paint this brush with, if it has one.</summary>
		internal virtual XF.Color ToFormsColor() => XF.Color.Default;
	}

	[XF.TypeConverter(typeof(BrushConverter))]
	public sealed class SolidColorBrush : Brush
	{
		public SolidColorBrush()
		{
		}

		public SolidColorBrush(Color color) => Color = color;

		public Color Color
		{
			get => Get<Color>(ColorProperty);
			set
			{
				ThrowIfFrozen();
				SetValue(ColorProperty, value);
			}
		}

		public static readonly XF.BindableProperty ColorProperty = Xamarin.Forms.Wpf.Dp.Register<SolidColorBrush>(nameof(Color), typeof(Color), default(Color));

		internal override XF.Color ToFormsColor()
		{
			var c = Color.ToForms();
			return Opacity < 1 ? c.MultiplyAlpha(Opacity) : c;
		}

		public override string ToString() => Color.ToString();

		protected override Freezable CreateInstanceCore() => new SolidColorBrush(Color) { Opacity = Opacity };
	}

	/// <summary>A brush as XAML writes it: a color (<see cref="ColorConverter"/>) paints solid.</summary>
	public sealed class BrushConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value) => new SolidColorBrush(ColorConverter.Parse(value));
	}

	public enum Stretch
	{
		None,
		Fill,
		Uniform,
		UniformToFill,
	}

	public enum AlignmentX
	{
		Left,
		Center,
		Right,
	}

	public enum AlignmentY
	{
		Top,
		Center,
		Bottom,
	}

	public abstract class TileBrush : Brush
	{
		public Stretch Stretch { get; set; } = Stretch.Fill;

		public AlignmentX AlignmentX { get; set; } = AlignmentX.Center;

		public AlignmentY AlignmentY { get; set; } = AlignmentY.Center;
	}

	public sealed class ImageBrush : TileBrush
	{
		public ImageBrush()
		{
		}

		public ImageBrush(ImageSource image) => ImageSource = image;

		public ImageSource ImageSource { get; set; }

		protected override Freezable CreateInstanceCore() =>
			new ImageBrush(ImageSource) { Stretch = Stretch, AlignmentX = AlignmentX, AlignmentY = AlignmentY, Opacity = Opacity };
	}

	[XF.TypeConverter(typeof(ImageSourceConverter))]
	public abstract class ImageSource : Freezable
	{
		public virtual double Width => 0;

		public virtual double Height => 0;

		/// <summary>The same image for Xamarin.Forms.</summary>
		internal abstract XF.ImageSource ToForms();
	}

	/// <summary>An image as XAML names it: a path or a <c>pack://application:,,,/</c> URI.</summary>
	public sealed class ImageSourceConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value) =>
			new BitmapImage(new Uri(value ?? throw new ArgumentNullException(nameof(value)), UriKind.RelativeOrAbsolute));
	}

	[XF.TypeConverter(typeof(FontFamilyConverter))]
	public class FontFamily
	{
		public FontFamily() : this("Segoe UI")
		{
		}

		public FontFamily(string familyName) => Source = familyName ?? throw new ArgumentNullException(nameof(familyName));

		public string Source { get; }

		/// <summary>The first family of a fallback list, which is the one a desktop font system can look up.</summary>
		internal string FirstFamily
		{
			get
			{
				var comma = Source.IndexOf(',');
				return (comma < 0 ? Source : Source.Substring(0, comma)).Trim();
			}
		}

		public override bool Equals(object obj) => obj is FontFamily other && string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase);

		public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Source);

		public override string ToString() => Source;
	}

	public sealed class FontFamilyConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value) => new FontFamily(value);
	}

	public class Typeface
	{
		public Typeface(string typefaceName) : this(new FontFamily(typefaceName), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal)
		{
		}

		public Typeface(FontFamily fontFamily, FontStyle style, FontWeight weight, FontStretch stretch)
		{
			FontFamily = fontFamily;
			Style = style;
			Weight = weight;
			Stretch = stretch;
		}

		public FontFamily FontFamily { get; }

		public FontStyle Style { get; }

		public FontWeight Weight { get; }

		public FontStretch Stretch { get; }
	}

	/// <summary>Text measured the way the screen will draw it, by Pango when GTK is up.</summary>
	public class FormattedText
	{
		public FormattedText(string textToFormat, CultureInfo culture, FlowDirection flowDirection, Typeface typeface, double emSize, Brush foreground)
			: this(textToFormat, culture, flowDirection, typeface, emSize, foreground, 1.0)
		{
		}

		public FormattedText(string textToFormat, CultureInfo culture, FlowDirection flowDirection, Typeface typeface, double emSize, Brush foreground, double pixelsPerDip)
		{
			Text = textToFormat ?? throw new ArgumentNullException(nameof(textToFormat));
			var (width, height) = Xamarin.Forms.Wpf.TextMeasure.Measure(Text, typeface, emSize);
			WidthIncludingTrailingWhitespace = width;
			Width = width;
			Height = height;
		}

		public string Text { get; }

		public double Width { get; }

		public double WidthIncludingTrailingWhitespace { get; }

		public double Height { get; }
	}

	[XF.TypeConverter(typeof(DoubleCollectionConverter))]
	public sealed class DoubleCollection : List<double>
	{
		public DoubleCollection()
		{
		}

		public DoubleCollection(IEnumerable<double> collection) : base(collection)
		{
		}

		internal XF.DoubleCollection ToForms()
		{
			var result = new XF.DoubleCollection();
			foreach (var d in this)
				result.Add(d);
			return result;
		}
	}

	public sealed class DoubleCollectionConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value) => new DoubleCollection(Lengths.Split(value));
	}

	/// <summary>An element that renders: WPF's base of every <see cref="UIElement"/>.</summary>
	public abstract class Visual : DependencyObject
	{
		protected Visual()
		{
		}
	}

	public struct DpiScale
	{
		public DpiScale(double dpiScaleX, double dpiScaleY)
		{
			DpiScaleX = dpiScaleX;
			DpiScaleY = dpiScaleY;
		}

		public double DpiScaleX { get; }

		public double DpiScaleY { get; }

		public double PixelsPerDip => DpiScaleY;

		public double PixelsPerInchX => 96 * DpiScaleX;

		public double PixelsPerInchY => 96 * DpiScaleY;
	}

	/// <summary>The visual tree, which here is the logical one: this library draws no templates of its own.</summary>
	public static class VisualTreeHelper
	{
		public static DependencyObject GetParent(DependencyObject reference) =>
			(reference ?? throw new ArgumentNullException(nameof(reference))).LogicalParent;

		public static int GetChildrenCount(DependencyObject reference) => Children(reference).Count;

		public static DependencyObject GetChild(DependencyObject reference, int childIndex) => Children(reference)[childIndex];

		/// <summary>GTK scales for the display itself: to code, a device-independent pixel is one pixel.</summary>
		public static DpiScale GetDpi(Visual visual) => new DpiScale(1, 1);

		static List<DependencyObject> Children(DependencyObject reference)
		{
			var children = new List<DependencyObject>();
			foreach (var child in (reference ?? throw new ArgumentNullException(nameof(reference))).LogicalChildrenCore)
			{
				if (child is DependencyObject d)
					children.Add(d);
			}

			return children;
		}
	}
}
