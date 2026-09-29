using System.Globalization;
using XF = Xamarin.Forms;

namespace System.Windows
{
	public enum Visibility : byte
	{
		Visible,
		Hidden,
		Collapsed,
	}

	public enum HorizontalAlignment
	{
		Left,
		Center,
		Right,
		Stretch,
	}

	public enum VerticalAlignment
	{
		Top,
		Center,
		Bottom,
		Stretch,
	}

	public enum TextAlignment
	{
		Left,
		Right,
		Center,
		Justify,
	}

	public enum TextWrapping
	{
		WrapWithOverflow,
		NoWrap,
		Wrap,
	}

	public enum TextTrimming
	{
		None,
		CharacterEllipsis,
		WordEllipsis,
	}

	public enum FlowDirection
	{
		LeftToRight,
		RightToLeft,
	}

	public enum WindowStartupLocation
	{
		Manual,
		CenterScreen,
		CenterOwner,
	}

	public enum SizeToContent
	{
		Manual,
		Width,
		Height,
		WidthAndHeight,
	}

	public enum ResizeMode
	{
		NoResize,
		CanMinimize,
		CanResize,
		CanResizeWithGrip,
	}

	public enum WindowStyle
	{
		None,
		SingleBorderWindow,
		ThreeDBorderWindow,
		ToolWindow,
	}

	public enum WindowState
	{
		Normal,
		Minimized,
		Maximized,
	}

	public enum ShutdownMode : byte
	{
		OnLastWindowClose,
		OnMainWindowClose,
		OnExplicitShutdown,
	}

	public enum GridUnitType
	{
		Auto,
		Pixel,
		Star,
	}

	[XF.TypeConverter(typeof(ThicknessConverter))]
	public struct Thickness : IEquatable<Thickness>
	{
		public Thickness(double uniformLength) : this(uniformLength, uniformLength, uniformLength, uniformLength)
		{
		}

		public Thickness(double left, double top, double right, double bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public double Left { get; set; }

		public double Top { get; set; }

		public double Right { get; set; }

		public double Bottom { get; set; }

		public bool Equals(Thickness other) =>
			Left.Equals(other.Left) && Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

		public override bool Equals(object obj) => obj is Thickness other && Equals(other);

		public override int GetHashCode() => Left.GetHashCode() ^ Top.GetHashCode() ^ Right.GetHashCode() ^ Bottom.GetHashCode();

		public static bool operator ==(Thickness a, Thickness b) => a.Equals(b);

		public static bool operator !=(Thickness a, Thickness b) => !a.Equals(b);

		public override string ToString() =>
			string.Join(",", Invariant(Left), Invariant(Top), Invariant(Right), Invariant(Bottom));

		internal XF.Thickness ToForms() => new XF.Thickness(Left, Top, Right, Bottom);

		internal static string Invariant(double d) => double.IsNaN(d) ? "Auto" : d.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>WPF's thickness syntax: one, two (horizontal, vertical) or four comma- or space-separated lengths.</summary>
	public sealed class ThicknessConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value)
		{
			var parts = Lengths.Split(value);
			switch (parts.Length)
			{
				case 1:
					return new Thickness(parts[0]);
				case 2:
					return new Thickness(parts[0], parts[1], parts[0], parts[1]);
				case 4:
					return new Thickness(parts[0], parts[1], parts[2], parts[3]);
				default:
					throw new FormatException($"'{value}' is not a Thickness: one, two or four lengths are expected.");
			}
		}
	}

	/// <summary>Length lists as XAML writes them.</summary>
	internal static class Lengths
	{
		internal static double[] Split(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var result = new double[parts.Length];
			for (var i = 0; i < parts.Length; i++)
				result[i] = Parse(parts[i]);
			return result;
		}

		/// <summary>A length: a number, <c>Auto</c> (NaN), or a number with a <c>px</c>/<c>in</c>/<c>cm</c>/<c>pt</c> unit.</summary>
		internal static double Parse(string s)
		{
			s = s.Trim();
			if (s.Equals("Auto", StringComparison.OrdinalIgnoreCase))
				return double.NaN;

			var factor = 1.0;
			foreach (var (unit, dips) in s_units)
			{
				if (s.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
				{
					factor = dips;
					s = s.Substring(0, s.Length - unit.Length);
					break;
				}
			}

			return double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture) * factor;
		}

		static readonly (string Unit, double Dips)[] s_units = { ("px", 1), ("in", 96), ("cm", 96 / 2.54), ("pt", 96 / 72.0) };
	}

	[XF.TypeConverter(typeof(SizeConverter))]
	public struct Size : IEquatable<Size>
	{
		public Size(double width, double height)
		{
			if (width < 0 || height < 0)
				throw new ArgumentException("Width and Height must be non-negative.");

			Width = width;
			Height = height;
		}

		public static Size Empty => new Size { Width = double.NegativeInfinity, Height = double.NegativeInfinity };

		public bool IsEmpty => Width < 0;

		public double Width { get; set; }

		public double Height { get; set; }

		public bool Equals(Size other) => Width.Equals(other.Width) && Height.Equals(other.Height);

		public override bool Equals(object obj) => obj is Size other && Equals(other);

		public override int GetHashCode() => Width.GetHashCode() ^ Height.GetHashCode();

		public static bool operator ==(Size a, Size b) => a.Equals(b);

		public static bool operator !=(Size a, Size b) => !a.Equals(b);

		public override string ToString() => Thickness.Invariant(Width) + "," + Thickness.Invariant(Height);
	}

	public sealed class SizeConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value)
		{
			var parts = Lengths.Split(value);
			if (parts.Length != 2)
				throw new FormatException($"'{value}' is not a Size.");
			return new Size(parts[0], parts[1]);
		}
	}

	[XF.TypeConverter(typeof(PointConverter))]
	public struct Point : IEquatable<Point>
	{
		public Point(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double X { get; set; }

		public double Y { get; set; }

		public void Offset(double offsetX, double offsetY)
		{
			X += offsetX;
			Y += offsetY;
		}

		public bool Equals(Point other) => X.Equals(other.X) && Y.Equals(other.Y);

		public override bool Equals(object obj) => obj is Point other && Equals(other);

		public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

		public static bool operator ==(Point a, Point b) => a.Equals(b);

		public static bool operator !=(Point a, Point b) => !a.Equals(b);

		public override string ToString() => Thickness.Invariant(X) + "," + Thickness.Invariant(Y);
	}

	public sealed class PointConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value)
		{
			var parts = Lengths.Split(value);
			if (parts.Length != 2)
				throw new FormatException($"'{value}' is not a Point.");
			return new Point(parts[0], parts[1]);
		}
	}

	public struct Rect : IEquatable<Rect>
	{
		public Rect(double x, double y, double width, double height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public Rect(Point location, Size size) : this(location.X, location.Y, size.Width, size.Height)
		{
		}

		public Rect(Size size) : this(0, 0, size.Width, size.Height)
		{
		}

		public static Rect Empty => new Rect(double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);

		public bool IsEmpty => Width < 0;

		public double X { get; set; }

		public double Y { get; set; }

		public double Width { get; set; }

		public double Height { get; set; }

		public double Left => X;

		public double Top => Y;

		public double Right => X + Width;

		public double Bottom => Y + Height;

		public Point Location => new Point(X, Y);

		public Size Size => new Size(Math.Max(0, Width), Math.Max(0, Height));

		public bool Contains(Point point) =>
			point.X >= X && point.X <= X + Width && point.Y >= Y && point.Y <= Y + Height;

		public bool Equals(Rect other) =>
			X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);

		public override bool Equals(object obj) => obj is Rect other && Equals(other);

		public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();

		public static bool operator ==(Rect a, Rect b) => a.Equals(b);

		public static bool operator !=(Rect a, Rect b) => !a.Equals(b);
	}

	[XF.TypeConverter(typeof(CornerRadiusConverter))]
	public struct CornerRadius : IEquatable<CornerRadius>
	{
		public CornerRadius(double uniformRadius) : this(uniformRadius, uniformRadius, uniformRadius, uniformRadius)
		{
		}

		public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
		{
			TopLeft = topLeft;
			TopRight = topRight;
			BottomRight = bottomRight;
			BottomLeft = bottomLeft;
		}

		public double TopLeft { get; set; }

		public double TopRight { get; set; }

		public double BottomRight { get; set; }

		public double BottomLeft { get; set; }

		public bool Equals(CornerRadius other) =>
			TopLeft.Equals(other.TopLeft) && TopRight.Equals(other.TopRight) && BottomRight.Equals(other.BottomRight) && BottomLeft.Equals(other.BottomLeft);

		public override bool Equals(object obj) => obj is CornerRadius other && Equals(other);

		public override int GetHashCode() => TopLeft.GetHashCode() ^ TopRight.GetHashCode() ^ BottomRight.GetHashCode() ^ BottomLeft.GetHashCode();

		public static bool operator ==(CornerRadius a, CornerRadius b) => a.Equals(b);

		public static bool operator !=(CornerRadius a, CornerRadius b) => !a.Equals(b);
	}

	public sealed class CornerRadiusConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value)
		{
			var parts = Lengths.Split(value);
			switch (parts.Length)
			{
				case 1:
					return new CornerRadius(parts[0]);
				case 4:
					return new CornerRadius(parts[0], parts[1], parts[2], parts[3]);
				default:
					throw new FormatException($"'{value}' is not a CornerRadius.");
			}
		}
	}

	[XF.TypeConverter(typeof(GridLengthConverter))]
	public struct GridLength : IEquatable<GridLength>
	{
		public GridLength(double pixels) : this(pixels, GridUnitType.Pixel)
		{
		}

		public GridLength(double value, GridUnitType type)
		{
			Value = type == GridUnitType.Auto ? 1 : value;
			GridUnitType = type;
		}

		public static GridLength Auto => new GridLength(1, GridUnitType.Auto);

		public double Value { get; }

		public GridUnitType GridUnitType { get; }

		public bool IsAbsolute => GridUnitType == GridUnitType.Pixel;

		public bool IsAuto => GridUnitType == GridUnitType.Auto;

		public bool IsStar => GridUnitType == GridUnitType.Star;

		public bool Equals(GridLength other) => Value.Equals(other.Value) && GridUnitType == other.GridUnitType;

		public override bool Equals(object obj) => obj is GridLength other && Equals(other);

		public override int GetHashCode() => Value.GetHashCode() ^ (int)GridUnitType;

		public static bool operator ==(GridLength a, GridLength b) => a.Equals(b);

		public static bool operator !=(GridLength a, GridLength b) => !a.Equals(b);

		public override string ToString() =>
			IsAuto ? "Auto" : IsStar ? (Value.Equals(1.0) ? "*" : Thickness.Invariant(Value) + "*") : Thickness.Invariant(Value);

		internal XF.GridLength ToForms() =>
			IsAuto ? XF.GridLength.Auto : IsStar ? new XF.GridLength(Value, XF.GridUnitType.Star) : new XF.GridLength(Value, XF.GridUnitType.Absolute);
	}

	public sealed class GridLengthConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value)
		{
			var s = (value ?? throw new ArgumentNullException(nameof(value))).Trim();
			if (s.Equals("Auto", StringComparison.OrdinalIgnoreCase))
				return GridLength.Auto;
			if (s.EndsWith("*", StringComparison.Ordinal))
			{
				var factor = s.Length == 1 ? 1 : double.Parse(s.Substring(0, s.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture);
				return new GridLength(factor, GridUnitType.Star);
			}
			return new GridLength(Lengths.Parse(s));
		}
	}

	[XF.TypeConverter(typeof(FontWeightConverter))]
	public struct FontWeight : IEquatable<FontWeight>, IComparable<FontWeight>
	{
		readonly int _weight;

		FontWeight(int weight) => _weight = weight;

		public static FontWeight FromOpenTypeWeight(int weightValue)
		{
			if (weightValue < 1 || weightValue > 999)
				throw new ArgumentOutOfRangeException(nameof(weightValue));
			return new FontWeight(weightValue);
		}

		public int ToOpenTypeWeight() => _weight == 0 ? 400 : _weight;

		public bool Equals(FontWeight other) => ToOpenTypeWeight() == other.ToOpenTypeWeight();

		public int CompareTo(FontWeight other) => ToOpenTypeWeight().CompareTo(other.ToOpenTypeWeight());

		public override bool Equals(object obj) => obj is FontWeight other && Equals(other);

		public override int GetHashCode() => ToOpenTypeWeight();

		public static bool operator ==(FontWeight a, FontWeight b) => a.Equals(b);

		public static bool operator !=(FontWeight a, FontWeight b) => !a.Equals(b);

		public static bool operator <(FontWeight a, FontWeight b) => a.CompareTo(b) < 0;

		public static bool operator >(FontWeight a, FontWeight b) => a.CompareTo(b) > 0;

		public static bool operator <=(FontWeight a, FontWeight b) => a.CompareTo(b) <= 0;

		public static bool operator >=(FontWeight a, FontWeight b) => a.CompareTo(b) >= 0;

		public override string ToString() => FontWeights.NameOf(ToOpenTypeWeight());
	}

	public static class FontWeights
	{
		public static FontWeight Thin => FontWeight.FromOpenTypeWeight(100);
		public static FontWeight ExtraLight => FontWeight.FromOpenTypeWeight(200);
		public static FontWeight UltraLight => FontWeight.FromOpenTypeWeight(200);
		public static FontWeight Light => FontWeight.FromOpenTypeWeight(300);
		public static FontWeight Normal => FontWeight.FromOpenTypeWeight(400);
		public static FontWeight Regular => FontWeight.FromOpenTypeWeight(400);
		public static FontWeight Medium => FontWeight.FromOpenTypeWeight(500);
		public static FontWeight DemiBold => FontWeight.FromOpenTypeWeight(600);
		public static FontWeight SemiBold => FontWeight.FromOpenTypeWeight(600);
		public static FontWeight Bold => FontWeight.FromOpenTypeWeight(700);
		public static FontWeight ExtraBold => FontWeight.FromOpenTypeWeight(800);
		public static FontWeight UltraBold => FontWeight.FromOpenTypeWeight(800);
		public static FontWeight Black => FontWeight.FromOpenTypeWeight(900);
		public static FontWeight Heavy => FontWeight.FromOpenTypeWeight(900);
		public static FontWeight ExtraBlack => FontWeight.FromOpenTypeWeight(950);
		public static FontWeight UltraBlack => FontWeight.FromOpenTypeWeight(950);

		static readonly (string Name, int Weight)[] s_names =
		{
			("Thin", 100), ("ExtraLight", 200), ("UltraLight", 200), ("Light", 300), ("Normal", 400), ("Regular", 400),
			("Medium", 500), ("DemiBold", 600), ("SemiBold", 600), ("Bold", 700), ("ExtraBold", 800), ("UltraBold", 800),
			("Black", 900), ("Heavy", 900), ("ExtraBlack", 950), ("UltraBlack", 950),
		};

		internal static bool TryParse(string s, out FontWeight weight)
		{
			foreach (var (name, value) in s_names)
			{
				if (name.Equals(s, StringComparison.OrdinalIgnoreCase))
				{
					weight = FontWeight.FromOpenTypeWeight(value);
					return true;
				}
			}

			if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 1 && n <= 999)
			{
				weight = FontWeight.FromOpenTypeWeight(n);
				return true;
			}

			weight = Normal;
			return false;
		}

		internal static string NameOf(int weight)
		{
			foreach (var (name, value) in s_names)
			{
				if (value == weight)
					return name;
			}

			return weight.ToString(CultureInfo.InvariantCulture);
		}
	}

	public sealed class FontWeightConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value) =>
			FontWeights.TryParse(value?.Trim(), out var weight) ? weight : throw new FormatException($"'{value}' is not a FontWeight.");
	}

	[XF.TypeConverter(typeof(FontStyleConverter))]
	public struct FontStyle : IEquatable<FontStyle>
	{
		readonly int _style;

		internal FontStyle(int style) => _style = style;

		public bool Equals(FontStyle other) => _style == other._style;

		public override bool Equals(object obj) => obj is FontStyle other && Equals(other);

		public override int GetHashCode() => _style;

		public static bool operator ==(FontStyle a, FontStyle b) => a.Equals(b);

		public static bool operator !=(FontStyle a, FontStyle b) => !a.Equals(b);

		public override string ToString() => _style == 1 ? "Oblique" : _style == 2 ? "Italic" : "Normal";
	}

	public static class FontStyles
	{
		public static FontStyle Normal => new FontStyle(0);

		public static FontStyle Oblique => new FontStyle(1);

		public static FontStyle Italic => new FontStyle(2);
	}

	public sealed class FontStyleConverter : XF.TypeConverter
	{
		public override object ConvertFromInvariantString(string value)
		{
			switch (value?.Trim().ToLowerInvariant())
			{
				case "normal":
					return FontStyles.Normal;
				case "oblique":
					return FontStyles.Oblique;
				case "italic":
					return FontStyles.Italic;
				default:
					throw new FormatException($"'{value}' is not a FontStyle.");
			}
		}
	}

	[XF.TypeConverter(typeof(FontStretchConverter))]
	public struct FontStretch : IEquatable<FontStretch>
	{
		readonly int _stretch;

		FontStretch(int stretch) => _stretch = stretch;

		public static FontStretch FromOpenTypeStretch(int stretchValue) => new FontStretch(stretchValue);

		public int ToOpenTypeStretch() => _stretch == 0 ? 5 : _stretch;

		public bool Equals(FontStretch other) => ToOpenTypeStretch() == other.ToOpenTypeStretch();

		public override bool Equals(object obj) => obj is FontStretch other && Equals(other);

		public override int GetHashCode() => ToOpenTypeStretch();

		public static bool operator ==(FontStretch a, FontStretch b) => a.Equals(b);

		public static bool operator !=(FontStretch a, FontStretch b) => !a.Equals(b);
	}

	public static class FontStretches
	{
		public static FontStretch UltraCondensed => FontStretch.FromOpenTypeStretch(1);
		public static FontStretch ExtraCondensed => FontStretch.FromOpenTypeStretch(2);
		public static FontStretch Condensed => FontStretch.FromOpenTypeStretch(3);
		public static FontStretch SemiCondensed => FontStretch.FromOpenTypeStretch(4);
		public static FontStretch Normal => FontStretch.FromOpenTypeStretch(5);
		public static FontStretch Medium => FontStretch.FromOpenTypeStretch(5);
		public static FontStretch SemiExpanded => FontStretch.FromOpenTypeStretch(6);
		public static FontStretch Expanded => FontStretch.FromOpenTypeStretch(7);
		public static FontStretch ExtraExpanded => FontStretch.FromOpenTypeStretch(8);
		public static FontStretch UltraExpanded => FontStretch.FromOpenTypeStretch(9);
	}

	public sealed class FontStretchConverter : XF.TypeConverter
	{
		static readonly string[] s_names =
			{ "UltraCondensed", "ExtraCondensed", "Condensed", "SemiCondensed", "Normal", "SemiExpanded", "Expanded", "ExtraExpanded", "UltraExpanded" };

		public override object ConvertFromInvariantString(string value)
		{
			var s = value?.Trim();
			if ("Medium".Equals(s, StringComparison.OrdinalIgnoreCase))
				return FontStretches.Normal;
			for (var i = 0; i < s_names.Length; i++)
			{
				if (s_names[i].Equals(s, StringComparison.OrdinalIgnoreCase))
					return FontStretch.FromOpenTypeStretch(i + 1);
			}
			throw new FormatException($"'{value}' is not a FontStretch.");
		}
	}
}
