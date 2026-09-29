using System.Collections.Generic;
using System.Windows.Media;

namespace System.Windows
{
	/// <summary>
	/// The system colors, as colors, frozen brushes and resource keys. The values are Windows' defaults - what the
	/// VB6 and WPF programs this library runs were drawn with - rather than the GTK theme's.
	/// </summary>
	/// <remarks>
	/// Each brush is one instance for the life of the process, so code can recognise a system brush by reference, and
	/// each key names that brush in <see cref="Application.Resources"/>, so <c>{DynamicResource}</c> finds it.
	/// </remarks>
	public static class SystemColors
	{
		static readonly Dictionary<string, SolidColorBrush> s_brushes = new Dictionary<string, SolidColorBrush>();
		static readonly Dictionary<string, ResourceKey> s_keys = new Dictionary<string, ResourceKey>();

		/// <summary>Every system color: its name and its default ARGB value.</summary>
		internal static readonly (string Name, uint Argb)[] All =
		{
			("ActiveBorder", 0xFFB4B4B4),
			("ActiveCaption", 0xFF99B4D1),
			("ActiveCaptionText", 0xFF000000),
			("AppWorkspace", 0xFFABABAB),
			("Control", 0xFFF0F0F0),
			("ControlDark", 0xFFA0A0A0),
			("ControlDarkDark", 0xFF696969),
			("ControlLight", 0xFFE3E3E3),
			("ControlLightLight", 0xFFFFFFFF),
			("ControlText", 0xFF000000),
			("Desktop", 0xFF000000),
			("GradientActiveCaption", 0xFFB9D1EA),
			("GradientInactiveCaption", 0xFFD7E4F2),
			("GrayText", 0xFF6D6D6D),
			("Highlight", 0xFF0078D7),
			("HighlightText", 0xFFFFFFFF),
			("HotTrack", 0xFF0066CC),
			("InactiveBorder", 0xFFF4F7FC),
			("InactiveCaption", 0xFFBFCDDB),
			("InactiveCaptionText", 0xFF000000),
			("Info", 0xFFFFFFE1),
			("InfoText", 0xFF000000),
			("Menu", 0xFFF0F0F0),
			("MenuBar", 0xFFF0F0F0),
			("MenuHighlight", 0xFF3399FF),
			("MenuText", 0xFF000000),
			("ScrollBar", 0xFFC8C8C8),
			("Window", 0xFFFFFFFF),
			("WindowFrame", 0xFF646464),
			("WindowText", 0xFF000000),
		};

		public static Color ActiveBorderColor => ColorOf(nameof(ActiveBorderColor));
		public static SolidColorBrush ActiveBorderBrush => BrushOf(nameof(ActiveBorderBrush));
		public static ResourceKey ActiveBorderColorKey => KeyOf(nameof(ActiveBorderColorKey));
		public static ResourceKey ActiveBorderBrushKey => KeyOf(nameof(ActiveBorderBrushKey));

		public static Color ActiveCaptionColor => ColorOf(nameof(ActiveCaptionColor));
		public static SolidColorBrush ActiveCaptionBrush => BrushOf(nameof(ActiveCaptionBrush));
		public static ResourceKey ActiveCaptionColorKey => KeyOf(nameof(ActiveCaptionColorKey));
		public static ResourceKey ActiveCaptionBrushKey => KeyOf(nameof(ActiveCaptionBrushKey));

		public static Color ActiveCaptionTextColor => ColorOf(nameof(ActiveCaptionTextColor));
		public static SolidColorBrush ActiveCaptionTextBrush => BrushOf(nameof(ActiveCaptionTextBrush));
		public static ResourceKey ActiveCaptionTextColorKey => KeyOf(nameof(ActiveCaptionTextColorKey));
		public static ResourceKey ActiveCaptionTextBrushKey => KeyOf(nameof(ActiveCaptionTextBrushKey));

		public static Color AppWorkspaceColor => ColorOf(nameof(AppWorkspaceColor));
		public static SolidColorBrush AppWorkspaceBrush => BrushOf(nameof(AppWorkspaceBrush));
		public static ResourceKey AppWorkspaceColorKey => KeyOf(nameof(AppWorkspaceColorKey));
		public static ResourceKey AppWorkspaceBrushKey => KeyOf(nameof(AppWorkspaceBrushKey));

		public static Color ControlColor => ColorOf(nameof(ControlColor));
		public static SolidColorBrush ControlBrush => BrushOf(nameof(ControlBrush));
		public static ResourceKey ControlColorKey => KeyOf(nameof(ControlColorKey));
		public static ResourceKey ControlBrushKey => KeyOf(nameof(ControlBrushKey));

		public static Color ControlDarkColor => ColorOf(nameof(ControlDarkColor));
		public static SolidColorBrush ControlDarkBrush => BrushOf(nameof(ControlDarkBrush));
		public static ResourceKey ControlDarkColorKey => KeyOf(nameof(ControlDarkColorKey));
		public static ResourceKey ControlDarkBrushKey => KeyOf(nameof(ControlDarkBrushKey));

		public static Color ControlDarkDarkColor => ColorOf(nameof(ControlDarkDarkColor));
		public static SolidColorBrush ControlDarkDarkBrush => BrushOf(nameof(ControlDarkDarkBrush));
		public static ResourceKey ControlDarkDarkColorKey => KeyOf(nameof(ControlDarkDarkColorKey));
		public static ResourceKey ControlDarkDarkBrushKey => KeyOf(nameof(ControlDarkDarkBrushKey));

		public static Color ControlLightColor => ColorOf(nameof(ControlLightColor));
		public static SolidColorBrush ControlLightBrush => BrushOf(nameof(ControlLightBrush));
		public static ResourceKey ControlLightColorKey => KeyOf(nameof(ControlLightColorKey));
		public static ResourceKey ControlLightBrushKey => KeyOf(nameof(ControlLightBrushKey));

		public static Color ControlLightLightColor => ColorOf(nameof(ControlLightLightColor));
		public static SolidColorBrush ControlLightLightBrush => BrushOf(nameof(ControlLightLightBrush));
		public static ResourceKey ControlLightLightColorKey => KeyOf(nameof(ControlLightLightColorKey));
		public static ResourceKey ControlLightLightBrushKey => KeyOf(nameof(ControlLightLightBrushKey));

		public static Color ControlTextColor => ColorOf(nameof(ControlTextColor));
		public static SolidColorBrush ControlTextBrush => BrushOf(nameof(ControlTextBrush));
		public static ResourceKey ControlTextColorKey => KeyOf(nameof(ControlTextColorKey));
		public static ResourceKey ControlTextBrushKey => KeyOf(nameof(ControlTextBrushKey));

		public static Color DesktopColor => ColorOf(nameof(DesktopColor));
		public static SolidColorBrush DesktopBrush => BrushOf(nameof(DesktopBrush));
		public static ResourceKey DesktopColorKey => KeyOf(nameof(DesktopColorKey));
		public static ResourceKey DesktopBrushKey => KeyOf(nameof(DesktopBrushKey));

		public static Color GradientActiveCaptionColor => ColorOf(nameof(GradientActiveCaptionColor));
		public static SolidColorBrush GradientActiveCaptionBrush => BrushOf(nameof(GradientActiveCaptionBrush));
		public static ResourceKey GradientActiveCaptionColorKey => KeyOf(nameof(GradientActiveCaptionColorKey));
		public static ResourceKey GradientActiveCaptionBrushKey => KeyOf(nameof(GradientActiveCaptionBrushKey));

		public static Color GradientInactiveCaptionColor => ColorOf(nameof(GradientInactiveCaptionColor));
		public static SolidColorBrush GradientInactiveCaptionBrush => BrushOf(nameof(GradientInactiveCaptionBrush));
		public static ResourceKey GradientInactiveCaptionColorKey => KeyOf(nameof(GradientInactiveCaptionColorKey));
		public static ResourceKey GradientInactiveCaptionBrushKey => KeyOf(nameof(GradientInactiveCaptionBrushKey));

		public static Color GrayTextColor => ColorOf(nameof(GrayTextColor));
		public static SolidColorBrush GrayTextBrush => BrushOf(nameof(GrayTextBrush));
		public static ResourceKey GrayTextColorKey => KeyOf(nameof(GrayTextColorKey));
		public static ResourceKey GrayTextBrushKey => KeyOf(nameof(GrayTextBrushKey));

		public static Color HighlightColor => ColorOf(nameof(HighlightColor));
		public static SolidColorBrush HighlightBrush => BrushOf(nameof(HighlightBrush));
		public static ResourceKey HighlightColorKey => KeyOf(nameof(HighlightColorKey));
		public static ResourceKey HighlightBrushKey => KeyOf(nameof(HighlightBrushKey));

		public static Color HighlightTextColor => ColorOf(nameof(HighlightTextColor));
		public static SolidColorBrush HighlightTextBrush => BrushOf(nameof(HighlightTextBrush));
		public static ResourceKey HighlightTextColorKey => KeyOf(nameof(HighlightTextColorKey));
		public static ResourceKey HighlightTextBrushKey => KeyOf(nameof(HighlightTextBrushKey));

		public static Color HotTrackColor => ColorOf(nameof(HotTrackColor));
		public static SolidColorBrush HotTrackBrush => BrushOf(nameof(HotTrackBrush));
		public static ResourceKey HotTrackColorKey => KeyOf(nameof(HotTrackColorKey));
		public static ResourceKey HotTrackBrushKey => KeyOf(nameof(HotTrackBrushKey));

		public static Color InactiveBorderColor => ColorOf(nameof(InactiveBorderColor));
		public static SolidColorBrush InactiveBorderBrush => BrushOf(nameof(InactiveBorderBrush));
		public static ResourceKey InactiveBorderColorKey => KeyOf(nameof(InactiveBorderColorKey));
		public static ResourceKey InactiveBorderBrushKey => KeyOf(nameof(InactiveBorderBrushKey));

		public static Color InactiveCaptionColor => ColorOf(nameof(InactiveCaptionColor));
		public static SolidColorBrush InactiveCaptionBrush => BrushOf(nameof(InactiveCaptionBrush));
		public static ResourceKey InactiveCaptionColorKey => KeyOf(nameof(InactiveCaptionColorKey));
		public static ResourceKey InactiveCaptionBrushKey => KeyOf(nameof(InactiveCaptionBrushKey));

		public static Color InactiveCaptionTextColor => ColorOf(nameof(InactiveCaptionTextColor));
		public static SolidColorBrush InactiveCaptionTextBrush => BrushOf(nameof(InactiveCaptionTextBrush));
		public static ResourceKey InactiveCaptionTextColorKey => KeyOf(nameof(InactiveCaptionTextColorKey));
		public static ResourceKey InactiveCaptionTextBrushKey => KeyOf(nameof(InactiveCaptionTextBrushKey));

		public static Color InfoColor => ColorOf(nameof(InfoColor));
		public static SolidColorBrush InfoBrush => BrushOf(nameof(InfoBrush));
		public static ResourceKey InfoColorKey => KeyOf(nameof(InfoColorKey));
		public static ResourceKey InfoBrushKey => KeyOf(nameof(InfoBrushKey));

		public static Color InfoTextColor => ColorOf(nameof(InfoTextColor));
		public static SolidColorBrush InfoTextBrush => BrushOf(nameof(InfoTextBrush));
		public static ResourceKey InfoTextColorKey => KeyOf(nameof(InfoTextColorKey));
		public static ResourceKey InfoTextBrushKey => KeyOf(nameof(InfoTextBrushKey));

		public static Color MenuColor => ColorOf(nameof(MenuColor));
		public static SolidColorBrush MenuBrush => BrushOf(nameof(MenuBrush));
		public static ResourceKey MenuColorKey => KeyOf(nameof(MenuColorKey));
		public static ResourceKey MenuBrushKey => KeyOf(nameof(MenuBrushKey));

		public static Color MenuBarColor => ColorOf(nameof(MenuBarColor));
		public static SolidColorBrush MenuBarBrush => BrushOf(nameof(MenuBarBrush));
		public static ResourceKey MenuBarColorKey => KeyOf(nameof(MenuBarColorKey));
		public static ResourceKey MenuBarBrushKey => KeyOf(nameof(MenuBarBrushKey));

		public static Color MenuHighlightColor => ColorOf(nameof(MenuHighlightColor));
		public static SolidColorBrush MenuHighlightBrush => BrushOf(nameof(MenuHighlightBrush));
		public static ResourceKey MenuHighlightColorKey => KeyOf(nameof(MenuHighlightColorKey));
		public static ResourceKey MenuHighlightBrushKey => KeyOf(nameof(MenuHighlightBrushKey));

		public static Color MenuTextColor => ColorOf(nameof(MenuTextColor));
		public static SolidColorBrush MenuTextBrush => BrushOf(nameof(MenuTextBrush));
		public static ResourceKey MenuTextColorKey => KeyOf(nameof(MenuTextColorKey));
		public static ResourceKey MenuTextBrushKey => KeyOf(nameof(MenuTextBrushKey));

		public static Color ScrollBarColor => ColorOf(nameof(ScrollBarColor));
		public static SolidColorBrush ScrollBarBrush => BrushOf(nameof(ScrollBarBrush));
		public static ResourceKey ScrollBarColorKey => KeyOf(nameof(ScrollBarColorKey));
		public static ResourceKey ScrollBarBrushKey => KeyOf(nameof(ScrollBarBrushKey));

		public static Color WindowColor => ColorOf(nameof(WindowColor));
		public static SolidColorBrush WindowBrush => BrushOf(nameof(WindowBrush));
		public static ResourceKey WindowColorKey => KeyOf(nameof(WindowColorKey));
		public static ResourceKey WindowBrushKey => KeyOf(nameof(WindowBrushKey));

		public static Color WindowFrameColor => ColorOf(nameof(WindowFrameColor));
		public static SolidColorBrush WindowFrameBrush => BrushOf(nameof(WindowFrameBrush));
		public static ResourceKey WindowFrameColorKey => KeyOf(nameof(WindowFrameColorKey));
		public static ResourceKey WindowFrameBrushKey => KeyOf(nameof(WindowFrameBrushKey));

		public static Color WindowTextColor => ColorOf(nameof(WindowTextColor));
		public static SolidColorBrush WindowTextBrush => BrushOf(nameof(WindowTextBrush));
		public static ResourceKey WindowTextColorKey => KeyOf(nameof(WindowTextColorKey));
		public static ResourceKey WindowTextBrushKey => KeyOf(nameof(WindowTextBrushKey));

		static Color ColorOf(string member) => Color.FromUInt32(ArgbOf(member.Substring(0, member.Length - "Color".Length)));

		static SolidColorBrush BrushOf(string member)
		{
			lock (s_brushes)
			{
				if (!s_brushes.TryGetValue(member, out var brush))
				{
					brush = new SolidColorBrush(Color.FromUInt32(ArgbOf(member.Substring(0, member.Length - "Brush".Length))));
					brush.Freeze();
					s_brushes.Add(member, brush);
				}

				return brush;
			}
		}

		static ResourceKey KeyOf(string member)
		{
			lock (s_keys)
			{
				if (!s_keys.TryGetValue(member, out var key))
					s_keys.Add(member, key = new SystemResourceKey("SystemColors." + member));

				return key;
			}
		}

		static uint ArgbOf(string name)
		{
			foreach (var (n, argb) in All)
			{
				if (n == name)
					return argb;
			}

			throw new ArgumentException(name);
		}

		/// <summary>The resources <see cref="Application"/> starts with: every brush and color under its key.</summary>
		internal static IEnumerable<KeyValuePair<string, object>> Resources()
		{
			foreach (var (name, _) in All)
			{
				yield return new KeyValuePair<string, object>(KeyOf(name + "BrushKey").ToString(), BrushOf(name + "Brush"));
				yield return new KeyValuePair<string, object>(KeyOf(name + "ColorKey").ToString(), ColorOf(name + "Color"));
			}
		}
	}
}
