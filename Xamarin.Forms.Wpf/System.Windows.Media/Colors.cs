using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace System.Windows.Media
{
	/// <summary>The named colors of WPF, with WPF's values.</summary>
	public sealed class Colors
	{
		Colors()
		{
		}

		public static Color AliceBlue => Color.FromUInt32(0xFFF0F8FF);
		public static Color AntiqueWhite => Color.FromUInt32(0xFFFAEBD7);
		public static Color Aqua => Color.FromUInt32(0xFF00FFFF);
		public static Color Aquamarine => Color.FromUInt32(0xFF7FFFD4);
		public static Color Azure => Color.FromUInt32(0xFFF0FFFF);
		public static Color Beige => Color.FromUInt32(0xFFF5F5DC);
		public static Color Bisque => Color.FromUInt32(0xFFFFE4C4);
		public static Color Black => Color.FromUInt32(0xFF000000);
		public static Color BlanchedAlmond => Color.FromUInt32(0xFFFFEBCD);
		public static Color Blue => Color.FromUInt32(0xFF0000FF);
		public static Color BlueViolet => Color.FromUInt32(0xFF8A2BE2);
		public static Color Brown => Color.FromUInt32(0xFFA52A2A);
		public static Color BurlyWood => Color.FromUInt32(0xFFDEB887);
		public static Color CadetBlue => Color.FromUInt32(0xFF5F9EA0);
		public static Color Chartreuse => Color.FromUInt32(0xFF7FFF00);
		public static Color Chocolate => Color.FromUInt32(0xFFD2691E);
		public static Color Coral => Color.FromUInt32(0xFFFF7F50);
		public static Color CornflowerBlue => Color.FromUInt32(0xFF6495ED);
		public static Color Cornsilk => Color.FromUInt32(0xFFFFF8DC);
		public static Color Crimson => Color.FromUInt32(0xFFDC143C);
		public static Color Cyan => Color.FromUInt32(0xFF00FFFF);
		public static Color DarkBlue => Color.FromUInt32(0xFF00008B);
		public static Color DarkCyan => Color.FromUInt32(0xFF008B8B);
		public static Color DarkGoldenrod => Color.FromUInt32(0xFFB8860B);
		public static Color DarkGray => Color.FromUInt32(0xFFA9A9A9);
		public static Color DarkGreen => Color.FromUInt32(0xFF006400);
		public static Color DarkKhaki => Color.FromUInt32(0xFFBDB76B);
		public static Color DarkMagenta => Color.FromUInt32(0xFF8B008B);
		public static Color DarkOliveGreen => Color.FromUInt32(0xFF556B2F);
		public static Color DarkOrange => Color.FromUInt32(0xFFFF8C00);
		public static Color DarkOrchid => Color.FromUInt32(0xFF9932CC);
		public static Color DarkRed => Color.FromUInt32(0xFF8B0000);
		public static Color DarkSalmon => Color.FromUInt32(0xFFE9967A);
		public static Color DarkSeaGreen => Color.FromUInt32(0xFF8FBC8F);
		public static Color DarkSlateBlue => Color.FromUInt32(0xFF483D8B);
		public static Color DarkSlateGray => Color.FromUInt32(0xFF2F4F4F);
		public static Color DarkTurquoise => Color.FromUInt32(0xFF00CED1);
		public static Color DarkViolet => Color.FromUInt32(0xFF9400D3);
		public static Color DeepPink => Color.FromUInt32(0xFFFF1493);
		public static Color DeepSkyBlue => Color.FromUInt32(0xFF00BFFF);
		public static Color DimGray => Color.FromUInt32(0xFF696969);
		public static Color DodgerBlue => Color.FromUInt32(0xFF1E90FF);
		public static Color Firebrick => Color.FromUInt32(0xFFB22222);
		public static Color FloralWhite => Color.FromUInt32(0xFFFFFAF0);
		public static Color ForestGreen => Color.FromUInt32(0xFF228B22);
		public static Color Fuchsia => Color.FromUInt32(0xFFFF00FF);
		public static Color Gainsboro => Color.FromUInt32(0xFFDCDCDC);
		public static Color GhostWhite => Color.FromUInt32(0xFFF8F8FF);
		public static Color Gold => Color.FromUInt32(0xFFFFD700);
		public static Color Goldenrod => Color.FromUInt32(0xFFDAA520);
		public static Color Gray => Color.FromUInt32(0xFF808080);
		public static Color Green => Color.FromUInt32(0xFF008000);
		public static Color GreenYellow => Color.FromUInt32(0xFFADFF2F);
		public static Color Honeydew => Color.FromUInt32(0xFFF0FFF0);
		public static Color HotPink => Color.FromUInt32(0xFFFF69B4);
		public static Color IndianRed => Color.FromUInt32(0xFFCD5C5C);
		public static Color Indigo => Color.FromUInt32(0xFF4B0082);
		public static Color Ivory => Color.FromUInt32(0xFFFFFFF0);
		public static Color Khaki => Color.FromUInt32(0xFFF0E68C);
		public static Color Lavender => Color.FromUInt32(0xFFE6E6FA);
		public static Color LavenderBlush => Color.FromUInt32(0xFFFFF0F5);
		public static Color LawnGreen => Color.FromUInt32(0xFF7CFC00);
		public static Color LemonChiffon => Color.FromUInt32(0xFFFFFACD);
		public static Color LightBlue => Color.FromUInt32(0xFFADD8E6);
		public static Color LightCoral => Color.FromUInt32(0xFFF08080);
		public static Color LightCyan => Color.FromUInt32(0xFFE0FFFF);
		public static Color LightGoldenrodYellow => Color.FromUInt32(0xFFFAFAD2);
		public static Color LightGray => Color.FromUInt32(0xFFD3D3D3);
		public static Color LightGreen => Color.FromUInt32(0xFF90EE90);
		public static Color LightPink => Color.FromUInt32(0xFFFFB6C1);
		public static Color LightSalmon => Color.FromUInt32(0xFFFFA07A);
		public static Color LightSeaGreen => Color.FromUInt32(0xFF20B2AA);
		public static Color LightSkyBlue => Color.FromUInt32(0xFF87CEFA);
		public static Color LightSlateGray => Color.FromUInt32(0xFF778899);
		public static Color LightSteelBlue => Color.FromUInt32(0xFFB0C4DE);
		public static Color LightYellow => Color.FromUInt32(0xFFFFFFE0);
		public static Color Lime => Color.FromUInt32(0xFF00FF00);
		public static Color LimeGreen => Color.FromUInt32(0xFF32CD32);
		public static Color Linen => Color.FromUInt32(0xFFFAF0E6);
		public static Color Magenta => Color.FromUInt32(0xFFFF00FF);
		public static Color Maroon => Color.FromUInt32(0xFF800000);
		public static Color MediumAquamarine => Color.FromUInt32(0xFF66CDAA);
		public static Color MediumBlue => Color.FromUInt32(0xFF0000CD);
		public static Color MediumOrchid => Color.FromUInt32(0xFFBA55D3);
		public static Color MediumPurple => Color.FromUInt32(0xFF9370DB);
		public static Color MediumSeaGreen => Color.FromUInt32(0xFF3CB371);
		public static Color MediumSlateBlue => Color.FromUInt32(0xFF7B68EE);
		public static Color MediumSpringGreen => Color.FromUInt32(0xFF00FA9A);
		public static Color MediumTurquoise => Color.FromUInt32(0xFF48D1CC);
		public static Color MediumVioletRed => Color.FromUInt32(0xFFC71585);
		public static Color MidnightBlue => Color.FromUInt32(0xFF191970);
		public static Color MintCream => Color.FromUInt32(0xFFF5FFFA);
		public static Color MistyRose => Color.FromUInt32(0xFFFFE4E1);
		public static Color Moccasin => Color.FromUInt32(0xFFFFE4B5);
		public static Color NavajoWhite => Color.FromUInt32(0xFFFFDEAD);
		public static Color Navy => Color.FromUInt32(0xFF000080);
		public static Color OldLace => Color.FromUInt32(0xFFFDF5E6);
		public static Color Olive => Color.FromUInt32(0xFF808000);
		public static Color OliveDrab => Color.FromUInt32(0xFF6B8E23);
		public static Color Orange => Color.FromUInt32(0xFFFFA500);
		public static Color OrangeRed => Color.FromUInt32(0xFFFF4500);
		public static Color Orchid => Color.FromUInt32(0xFFDA70D6);
		public static Color PaleGoldenrod => Color.FromUInt32(0xFFEEE8AA);
		public static Color PaleGreen => Color.FromUInt32(0xFF98FB98);
		public static Color PaleTurquoise => Color.FromUInt32(0xFFAFEEEE);
		public static Color PaleVioletRed => Color.FromUInt32(0xFFDB7093);
		public static Color PapayaWhip => Color.FromUInt32(0xFFFFEFD5);
		public static Color PeachPuff => Color.FromUInt32(0xFFFFDAB9);
		public static Color Peru => Color.FromUInt32(0xFFCD853F);
		public static Color Pink => Color.FromUInt32(0xFFFFC0CB);
		public static Color Plum => Color.FromUInt32(0xFFDDA0DD);
		public static Color PowderBlue => Color.FromUInt32(0xFFB0E0E6);
		public static Color Purple => Color.FromUInt32(0xFF800080);
		public static Color Red => Color.FromUInt32(0xFFFF0000);
		public static Color RosyBrown => Color.FromUInt32(0xFFBC8F8F);
		public static Color RoyalBlue => Color.FromUInt32(0xFF4169E1);
		public static Color SaddleBrown => Color.FromUInt32(0xFF8B4513);
		public static Color Salmon => Color.FromUInt32(0xFFFA8072);
		public static Color SandyBrown => Color.FromUInt32(0xFFF4A460);
		public static Color SeaGreen => Color.FromUInt32(0xFF2E8B57);
		public static Color SeaShell => Color.FromUInt32(0xFFFFF5EE);
		public static Color Sienna => Color.FromUInt32(0xFFA0522D);
		public static Color Silver => Color.FromUInt32(0xFFC0C0C0);
		public static Color SkyBlue => Color.FromUInt32(0xFF87CEEB);
		public static Color SlateBlue => Color.FromUInt32(0xFF6A5ACD);
		public static Color SlateGray => Color.FromUInt32(0xFF708090);
		public static Color Snow => Color.FromUInt32(0xFFFFFAFA);
		public static Color SpringGreen => Color.FromUInt32(0xFF00FF7F);
		public static Color SteelBlue => Color.FromUInt32(0xFF4682B4);
		public static Color Tan => Color.FromUInt32(0xFFD2B48C);
		public static Color Teal => Color.FromUInt32(0xFF008080);
		public static Color Thistle => Color.FromUInt32(0xFFD8BFD8);
		public static Color Tomato => Color.FromUInt32(0xFFFF6347);
		public static Color Transparent => Color.FromUInt32(0x00FFFFFF);
		public static Color Turquoise => Color.FromUInt32(0xFF40E0D0);
		public static Color Violet => Color.FromUInt32(0xFFEE82EE);
		public static Color Wheat => Color.FromUInt32(0xFFF5DEB3);
		public static Color White => Color.FromUInt32(0xFFFFFFFF);
		public static Color WhiteSmoke => Color.FromUInt32(0xFFF5F5F5);
		public static Color Yellow => Color.FromUInt32(0xFFFFFF00);
		public static Color YellowGreen => Color.FromUInt32(0xFF9ACD32);

		static Dictionary<string, Color> s_byName;

		/// <summary>A named color, ignoring case (what a XAML color attribute may name).</summary>
		internal static bool TryGet(string name, out Color color)
		{
			if (s_byName == null)
			{
				var byName = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
				foreach (var property in typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static))
					byName[property.Name] = (Color)property.GetValue(null, null);
				s_byName = byName;
			}

			return s_byName.TryGetValue(name, out color);
		}
	}

	/// <summary>A frozen brush per named color: the same instance on every read, as WPF's are.</summary>
	public sealed class Brushes
	{
		static readonly Dictionary<string, SolidColorBrush> s_brushes = new Dictionary<string, SolidColorBrush>();

		Brushes()
		{
		}

		public static SolidColorBrush AliceBlue => Get(nameof(AliceBlue), Colors.AliceBlue);
		public static SolidColorBrush AntiqueWhite => Get(nameof(AntiqueWhite), Colors.AntiqueWhite);
		public static SolidColorBrush Aqua => Get(nameof(Aqua), Colors.Aqua);
		public static SolidColorBrush Aquamarine => Get(nameof(Aquamarine), Colors.Aquamarine);
		public static SolidColorBrush Azure => Get(nameof(Azure), Colors.Azure);
		public static SolidColorBrush Beige => Get(nameof(Beige), Colors.Beige);
		public static SolidColorBrush Bisque => Get(nameof(Bisque), Colors.Bisque);
		public static SolidColorBrush Black => Get(nameof(Black), Colors.Black);
		public static SolidColorBrush BlanchedAlmond => Get(nameof(BlanchedAlmond), Colors.BlanchedAlmond);
		public static SolidColorBrush Blue => Get(nameof(Blue), Colors.Blue);
		public static SolidColorBrush BlueViolet => Get(nameof(BlueViolet), Colors.BlueViolet);
		public static SolidColorBrush Brown => Get(nameof(Brown), Colors.Brown);
		public static SolidColorBrush BurlyWood => Get(nameof(BurlyWood), Colors.BurlyWood);
		public static SolidColorBrush CadetBlue => Get(nameof(CadetBlue), Colors.CadetBlue);
		public static SolidColorBrush Chartreuse => Get(nameof(Chartreuse), Colors.Chartreuse);
		public static SolidColorBrush Chocolate => Get(nameof(Chocolate), Colors.Chocolate);
		public static SolidColorBrush Coral => Get(nameof(Coral), Colors.Coral);
		public static SolidColorBrush CornflowerBlue => Get(nameof(CornflowerBlue), Colors.CornflowerBlue);
		public static SolidColorBrush Cornsilk => Get(nameof(Cornsilk), Colors.Cornsilk);
		public static SolidColorBrush Crimson => Get(nameof(Crimson), Colors.Crimson);
		public static SolidColorBrush Cyan => Get(nameof(Cyan), Colors.Cyan);
		public static SolidColorBrush DarkBlue => Get(nameof(DarkBlue), Colors.DarkBlue);
		public static SolidColorBrush DarkCyan => Get(nameof(DarkCyan), Colors.DarkCyan);
		public static SolidColorBrush DarkGoldenrod => Get(nameof(DarkGoldenrod), Colors.DarkGoldenrod);
		public static SolidColorBrush DarkGray => Get(nameof(DarkGray), Colors.DarkGray);
		public static SolidColorBrush DarkGreen => Get(nameof(DarkGreen), Colors.DarkGreen);
		public static SolidColorBrush DarkKhaki => Get(nameof(DarkKhaki), Colors.DarkKhaki);
		public static SolidColorBrush DarkMagenta => Get(nameof(DarkMagenta), Colors.DarkMagenta);
		public static SolidColorBrush DarkOliveGreen => Get(nameof(DarkOliveGreen), Colors.DarkOliveGreen);
		public static SolidColorBrush DarkOrange => Get(nameof(DarkOrange), Colors.DarkOrange);
		public static SolidColorBrush DarkOrchid => Get(nameof(DarkOrchid), Colors.DarkOrchid);
		public static SolidColorBrush DarkRed => Get(nameof(DarkRed), Colors.DarkRed);
		public static SolidColorBrush DarkSalmon => Get(nameof(DarkSalmon), Colors.DarkSalmon);
		public static SolidColorBrush DarkSeaGreen => Get(nameof(DarkSeaGreen), Colors.DarkSeaGreen);
		public static SolidColorBrush DarkSlateBlue => Get(nameof(DarkSlateBlue), Colors.DarkSlateBlue);
		public static SolidColorBrush DarkSlateGray => Get(nameof(DarkSlateGray), Colors.DarkSlateGray);
		public static SolidColorBrush DarkTurquoise => Get(nameof(DarkTurquoise), Colors.DarkTurquoise);
		public static SolidColorBrush DarkViolet => Get(nameof(DarkViolet), Colors.DarkViolet);
		public static SolidColorBrush DeepPink => Get(nameof(DeepPink), Colors.DeepPink);
		public static SolidColorBrush DeepSkyBlue => Get(nameof(DeepSkyBlue), Colors.DeepSkyBlue);
		public static SolidColorBrush DimGray => Get(nameof(DimGray), Colors.DimGray);
		public static SolidColorBrush DodgerBlue => Get(nameof(DodgerBlue), Colors.DodgerBlue);
		public static SolidColorBrush Firebrick => Get(nameof(Firebrick), Colors.Firebrick);
		public static SolidColorBrush FloralWhite => Get(nameof(FloralWhite), Colors.FloralWhite);
		public static SolidColorBrush ForestGreen => Get(nameof(ForestGreen), Colors.ForestGreen);
		public static SolidColorBrush Fuchsia => Get(nameof(Fuchsia), Colors.Fuchsia);
		public static SolidColorBrush Gainsboro => Get(nameof(Gainsboro), Colors.Gainsboro);
		public static SolidColorBrush GhostWhite => Get(nameof(GhostWhite), Colors.GhostWhite);
		public static SolidColorBrush Gold => Get(nameof(Gold), Colors.Gold);
		public static SolidColorBrush Goldenrod => Get(nameof(Goldenrod), Colors.Goldenrod);
		public static SolidColorBrush Gray => Get(nameof(Gray), Colors.Gray);
		public static SolidColorBrush Green => Get(nameof(Green), Colors.Green);
		public static SolidColorBrush GreenYellow => Get(nameof(GreenYellow), Colors.GreenYellow);
		public static SolidColorBrush Honeydew => Get(nameof(Honeydew), Colors.Honeydew);
		public static SolidColorBrush HotPink => Get(nameof(HotPink), Colors.HotPink);
		public static SolidColorBrush IndianRed => Get(nameof(IndianRed), Colors.IndianRed);
		public static SolidColorBrush Indigo => Get(nameof(Indigo), Colors.Indigo);
		public static SolidColorBrush Ivory => Get(nameof(Ivory), Colors.Ivory);
		public static SolidColorBrush Khaki => Get(nameof(Khaki), Colors.Khaki);
		public static SolidColorBrush Lavender => Get(nameof(Lavender), Colors.Lavender);
		public static SolidColorBrush LavenderBlush => Get(nameof(LavenderBlush), Colors.LavenderBlush);
		public static SolidColorBrush LawnGreen => Get(nameof(LawnGreen), Colors.LawnGreen);
		public static SolidColorBrush LemonChiffon => Get(nameof(LemonChiffon), Colors.LemonChiffon);
		public static SolidColorBrush LightBlue => Get(nameof(LightBlue), Colors.LightBlue);
		public static SolidColorBrush LightCoral => Get(nameof(LightCoral), Colors.LightCoral);
		public static SolidColorBrush LightCyan => Get(nameof(LightCyan), Colors.LightCyan);
		public static SolidColorBrush LightGoldenrodYellow => Get(nameof(LightGoldenrodYellow), Colors.LightGoldenrodYellow);
		public static SolidColorBrush LightGray => Get(nameof(LightGray), Colors.LightGray);
		public static SolidColorBrush LightGreen => Get(nameof(LightGreen), Colors.LightGreen);
		public static SolidColorBrush LightPink => Get(nameof(LightPink), Colors.LightPink);
		public static SolidColorBrush LightSalmon => Get(nameof(LightSalmon), Colors.LightSalmon);
		public static SolidColorBrush LightSeaGreen => Get(nameof(LightSeaGreen), Colors.LightSeaGreen);
		public static SolidColorBrush LightSkyBlue => Get(nameof(LightSkyBlue), Colors.LightSkyBlue);
		public static SolidColorBrush LightSlateGray => Get(nameof(LightSlateGray), Colors.LightSlateGray);
		public static SolidColorBrush LightSteelBlue => Get(nameof(LightSteelBlue), Colors.LightSteelBlue);
		public static SolidColorBrush LightYellow => Get(nameof(LightYellow), Colors.LightYellow);
		public static SolidColorBrush Lime => Get(nameof(Lime), Colors.Lime);
		public static SolidColorBrush LimeGreen => Get(nameof(LimeGreen), Colors.LimeGreen);
		public static SolidColorBrush Linen => Get(nameof(Linen), Colors.Linen);
		public static SolidColorBrush Magenta => Get(nameof(Magenta), Colors.Magenta);
		public static SolidColorBrush Maroon => Get(nameof(Maroon), Colors.Maroon);
		public static SolidColorBrush MediumAquamarine => Get(nameof(MediumAquamarine), Colors.MediumAquamarine);
		public static SolidColorBrush MediumBlue => Get(nameof(MediumBlue), Colors.MediumBlue);
		public static SolidColorBrush MediumOrchid => Get(nameof(MediumOrchid), Colors.MediumOrchid);
		public static SolidColorBrush MediumPurple => Get(nameof(MediumPurple), Colors.MediumPurple);
		public static SolidColorBrush MediumSeaGreen => Get(nameof(MediumSeaGreen), Colors.MediumSeaGreen);
		public static SolidColorBrush MediumSlateBlue => Get(nameof(MediumSlateBlue), Colors.MediumSlateBlue);
		public static SolidColorBrush MediumSpringGreen => Get(nameof(MediumSpringGreen), Colors.MediumSpringGreen);
		public static SolidColorBrush MediumTurquoise => Get(nameof(MediumTurquoise), Colors.MediumTurquoise);
		public static SolidColorBrush MediumVioletRed => Get(nameof(MediumVioletRed), Colors.MediumVioletRed);
		public static SolidColorBrush MidnightBlue => Get(nameof(MidnightBlue), Colors.MidnightBlue);
		public static SolidColorBrush MintCream => Get(nameof(MintCream), Colors.MintCream);
		public static SolidColorBrush MistyRose => Get(nameof(MistyRose), Colors.MistyRose);
		public static SolidColorBrush Moccasin => Get(nameof(Moccasin), Colors.Moccasin);
		public static SolidColorBrush NavajoWhite => Get(nameof(NavajoWhite), Colors.NavajoWhite);
		public static SolidColorBrush Navy => Get(nameof(Navy), Colors.Navy);
		public static SolidColorBrush OldLace => Get(nameof(OldLace), Colors.OldLace);
		public static SolidColorBrush Olive => Get(nameof(Olive), Colors.Olive);
		public static SolidColorBrush OliveDrab => Get(nameof(OliveDrab), Colors.OliveDrab);
		public static SolidColorBrush Orange => Get(nameof(Orange), Colors.Orange);
		public static SolidColorBrush OrangeRed => Get(nameof(OrangeRed), Colors.OrangeRed);
		public static SolidColorBrush Orchid => Get(nameof(Orchid), Colors.Orchid);
		public static SolidColorBrush PaleGoldenrod => Get(nameof(PaleGoldenrod), Colors.PaleGoldenrod);
		public static SolidColorBrush PaleGreen => Get(nameof(PaleGreen), Colors.PaleGreen);
		public static SolidColorBrush PaleTurquoise => Get(nameof(PaleTurquoise), Colors.PaleTurquoise);
		public static SolidColorBrush PaleVioletRed => Get(nameof(PaleVioletRed), Colors.PaleVioletRed);
		public static SolidColorBrush PapayaWhip => Get(nameof(PapayaWhip), Colors.PapayaWhip);
		public static SolidColorBrush PeachPuff => Get(nameof(PeachPuff), Colors.PeachPuff);
		public static SolidColorBrush Peru => Get(nameof(Peru), Colors.Peru);
		public static SolidColorBrush Pink => Get(nameof(Pink), Colors.Pink);
		public static SolidColorBrush Plum => Get(nameof(Plum), Colors.Plum);
		public static SolidColorBrush PowderBlue => Get(nameof(PowderBlue), Colors.PowderBlue);
		public static SolidColorBrush Purple => Get(nameof(Purple), Colors.Purple);
		public static SolidColorBrush Red => Get(nameof(Red), Colors.Red);
		public static SolidColorBrush RosyBrown => Get(nameof(RosyBrown), Colors.RosyBrown);
		public static SolidColorBrush RoyalBlue => Get(nameof(RoyalBlue), Colors.RoyalBlue);
		public static SolidColorBrush SaddleBrown => Get(nameof(SaddleBrown), Colors.SaddleBrown);
		public static SolidColorBrush Salmon => Get(nameof(Salmon), Colors.Salmon);
		public static SolidColorBrush SandyBrown => Get(nameof(SandyBrown), Colors.SandyBrown);
		public static SolidColorBrush SeaGreen => Get(nameof(SeaGreen), Colors.SeaGreen);
		public static SolidColorBrush SeaShell => Get(nameof(SeaShell), Colors.SeaShell);
		public static SolidColorBrush Sienna => Get(nameof(Sienna), Colors.Sienna);
		public static SolidColorBrush Silver => Get(nameof(Silver), Colors.Silver);
		public static SolidColorBrush SkyBlue => Get(nameof(SkyBlue), Colors.SkyBlue);
		public static SolidColorBrush SlateBlue => Get(nameof(SlateBlue), Colors.SlateBlue);
		public static SolidColorBrush SlateGray => Get(nameof(SlateGray), Colors.SlateGray);
		public static SolidColorBrush Snow => Get(nameof(Snow), Colors.Snow);
		public static SolidColorBrush SpringGreen => Get(nameof(SpringGreen), Colors.SpringGreen);
		public static SolidColorBrush SteelBlue => Get(nameof(SteelBlue), Colors.SteelBlue);
		public static SolidColorBrush Tan => Get(nameof(Tan), Colors.Tan);
		public static SolidColorBrush Teal => Get(nameof(Teal), Colors.Teal);
		public static SolidColorBrush Thistle => Get(nameof(Thistle), Colors.Thistle);
		public static SolidColorBrush Tomato => Get(nameof(Tomato), Colors.Tomato);
		public static SolidColorBrush Transparent => Get(nameof(Transparent), Colors.Transparent);
		public static SolidColorBrush Turquoise => Get(nameof(Turquoise), Colors.Turquoise);
		public static SolidColorBrush Violet => Get(nameof(Violet), Colors.Violet);
		public static SolidColorBrush Wheat => Get(nameof(Wheat), Colors.Wheat);
		public static SolidColorBrush White => Get(nameof(White), Colors.White);
		public static SolidColorBrush WhiteSmoke => Get(nameof(WhiteSmoke), Colors.WhiteSmoke);
		public static SolidColorBrush Yellow => Get(nameof(Yellow), Colors.Yellow);
		public static SolidColorBrush YellowGreen => Get(nameof(YellowGreen), Colors.YellowGreen);

		static SolidColorBrush Get(string name, Color color)
		{
			lock (s_brushes)
			{
				if (!s_brushes.TryGetValue(name, out var brush))
				{
					brush = new SolidColorBrush(color);
					brush.Freeze();
					s_brushes.Add(name, brush);
				}

				return brush;
			}
		}
	}
}
