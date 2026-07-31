using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Gtk;

namespace Xamarin.Forms.Platform.GTK.Extensions
{
	/// <summary>
	/// GTK3 replacements for the GTK2 styling API.
	/// </summary>
	/// <remarks>
	/// GTK3 removed <c>Widget.ModifyBg/ModifyFg/ModifyBase/ModifyText/ModifyFont</c> and the
	/// <c>Gtk.Style.Backgrounds/BaseColors/Foregrounds/Text</c> colour arrays. Per-widget
	/// appearance is now expressed as CSS attached to the widget's <see cref="StyleContext"/>,
	/// and theme defaults are read back from that same context.
	///
	/// Declarations accumulate per widget, keyed by selector, and the whole sheet is rebuilt
	/// and reloaded on each change. That matters because callers routinely set one colour per
	/// widget state in sequence (Normal, then Prelight, then Active); a provider-per-call
	/// design would let each call clobber the previous one, and GTK offers no way to mutate
	/// an already-loaded provider.
	/// </remarks>
	public static class StyleExtensions
	{
		sealed class WidgetStyle
		{
			// selector -> (property -> value), preserving insertion order for stable CSS
			public readonly Dictionary<string, Dictionary<string, string>> Rules =
				new Dictionary<string, Dictionary<string, string>>();

			public CssProvider Provider;
		}

		static readonly ConditionalWeakTable<Widget, WidgetStyle> s_styles =
			new ConditionalWeakTable<Widget, WidgetStyle>();

		/// <summary>
		/// GTK2 widget states map onto GTK3 CSS pseudo-classes.
		/// </summary>
		public static string ToCssSelector(this StateType state)
		{
			switch (state)
			{
				case StateType.Prelight: return "*:hover";
				case StateType.Active: return "*:active";
				case StateType.Insensitive: return "*:disabled";
				case StateType.Selected: return "*:selected";
				default: return "*";
			}
		}

		/// <summary>
		/// Sets a single CSS declaration on a widget, keeping any other declarations this
		/// helper previously applied.
		/// </summary>
		/// <param name="selector">
		/// Pass a node/pseudo-class selector when the target is a composite widget or a
		/// specific state - e.g. <c>"entry"</c>, <c>"*:hover"</c> - because setting a colour
		/// on the outer widget does not necessarily reach the internal node that paints it.
		/// </param>
		public static void SetStyleProperty(this Widget widget, string property, string value, string selector = "*")
		{
			if (widget == null)
				return;

			var style = s_styles.GetValue(widget, _ => new WidgetStyle());

			if (!style.Rules.TryGetValue(selector, out var declarations))
			{
				declarations = new Dictionary<string, string>();
				style.Rules[selector] = declarations;
			}

			declarations[property] = value;

			Reapply(widget, style);
		}

		/// <summary>Removes every declaration this helper applied to the widget.</summary>
		public static void ClearStyle(this Widget widget)
		{
			if (widget == null || !s_styles.TryGetValue(widget, out var style))
				return;

			style.Rules.Clear();
			Reapply(widget, style);
		}

		static void Reapply(Widget widget, WidgetStyle style)
		{
			if (style.Provider != null)
			{
				widget.StyleContext.RemoveProvider(style.Provider);
				style.Provider = null;
			}

			if (style.Rules.Count == 0)
				return;

			var css = new StringBuilder();

			foreach (var rule in style.Rules)
			{
				if (rule.Value.Count == 0)
					continue;

				css.Append(rule.Key).Append(" { ");

				foreach (var declaration in rule.Value)
					css.Append(declaration.Key).Append(": ").Append(declaration.Value).Append("; ");

				css.Append("} ");
			}

			if (css.Length == 0)
				return;

			var provider = new CssProvider();
			provider.LoadFromData(css.ToString());
			widget.StyleContext.AddProvider(provider, StyleProviderPriority.Application);
			style.Provider = provider;
		}

		// ---- setters (replacing Modify*) ----------------------------------------------

		/// <summary>Replaces <c>ModifyBg</c>.</summary>
		public static void SetBackgroundColor(this Widget widget, Gdk.Color color, StateType state = StateType.Normal) =>
			widget.SetStyleProperty("background-color", color.ToCss(), state.ToCssSelector());

		public static void SetBackgroundColor(this Widget widget, Gdk.Color? color, StateType state = StateType.Normal)
		{
			if (color.HasValue)
				widget.SetBackgroundColor(color.Value, state);
			else
				widget.ClearStyle();
		}

		/// <summary>Replaces <c>ModifyFg</c>.</summary>
		public static void SetForegroundColor(this Widget widget, Gdk.Color color, StateType state = StateType.Normal) =>
			widget.SetStyleProperty("color", color.ToCss(), state.ToCssSelector());

		/// <summary>
		/// Replaces <c>ModifyBase</c>. In GTK2 "base" was the background of text-entry
		/// surfaces; in GTK3 that is background-color on the widget and its text node.
		/// </summary>
		public static void SetBaseColor(this Widget widget, Gdk.Color color, StateType state = StateType.Normal)
		{
			var css = color.ToCss();
			widget.SetStyleProperty("background-color", css, state.ToCssSelector());
			widget.SetStyleProperty("background-color", css, "text");
		}

		/// <summary>Replaces <c>ModifyText</c> (the foreground of text-entry surfaces).</summary>
		public static void SetTextColor(this Widget widget, Gdk.Color color, StateType state = StateType.Normal)
		{
			var css = color.ToCss();
			widget.SetStyleProperty("color", css, state.ToCssSelector());
			widget.SetStyleProperty("color", css, "text");
		}

		/// <summary>Replaces <c>ModifyFont</c>.</summary>
		public static void SetFont(this Widget widget, Pango.FontDescription font)
		{
			if (widget == null)
				return;

			if (font == null)
			{
				widget.ClearStyle();
				return;
			}

			if (!string.IsNullOrWhiteSpace(font.Family))
				widget.SetStyleProperty("font-family", $"\"{font.Family}\"");

			widget.SetStyleProperty("font-size",
				(font.Size / Pango.Scale.PangoScale).ToString("0.##", CultureInfo.InvariantCulture) + "pt");
			widget.SetStyleProperty("font-weight", font.Weight >= Pango.Weight.Bold ? "bold" : "normal");
			widget.SetStyleProperty("font-style", font.Style == Pango.Style.Italic ? "italic" : "normal");
		}

		// ---- theme-default readers (replacing Gtk.Style.* arrays) ----------------------

		/// <summary>Replaces <c>Style.Backgrounds[(int)state]</c>.</summary>
		public static Gdk.Color GetDefaultBackgroundColor(this Widget widget, StateFlags state = StateFlags.Normal) =>
			widget == null ? new Gdk.Color(0, 0, 0) : widget.StyleContext.GetBackgroundColor(state).ToGdkColor();

		/// <summary>Replaces <c>Style.Foregrounds[(int)state]</c>.</summary>
		public static Gdk.Color GetDefaultForegroundColor(this Widget widget, StateFlags state = StateFlags.Normal) =>
			widget == null ? new Gdk.Color(0, 0, 0) : widget.StyleContext.GetColor(state).ToGdkColor();

		/// <summary>
		/// Replaces <c>Style.BaseColors[(int)state]</c>. GTK3 has no separate "base" colour,
		/// so the background colour of the relevant state is the closest equivalent.
		/// </summary>
		public static Gdk.Color GetDefaultBaseColor(this Widget widget, StateFlags state = StateFlags.Normal) =>
			widget.GetDefaultBackgroundColor(state);

		/// <summary>Replaces <c>Style.Text(state)</c>.</summary>
		public static Gdk.Color GetDefaultTextColor(this Widget widget, StateFlags state = StateFlags.Normal) =>
			widget.GetDefaultForegroundColor(state);

		/// <summary>Replaces <c>Style.FontDescription</c>.</summary>
		public static Pango.FontDescription GetDefaultFont(this Widget widget, StateFlags state = StateFlags.Normal) =>
			widget?.StyleContext.GetFont(state);

		// ---- conversions ---------------------------------------------------------------

		/// <summary>Gdk.Color channels are 16-bit; CSS wants 8-bit (65535 / 257 = 255).</summary>
		public static string ToCss(this Gdk.Color color) =>
			string.Format(CultureInfo.InvariantCulture, "rgb({0},{1},{2})",
				color.Red / 257, color.Green / 257, color.Blue / 257);

		public static string ToCss(this Gdk.RGBA color) =>
			string.Format(CultureInfo.InvariantCulture, "rgba({0},{1},{2},{3})",
				(int)(color.Red * 255), (int)(color.Green * 255), (int)(color.Blue * 255),
				color.Alpha.ToString("0.##", CultureInfo.InvariantCulture));

		public static Gdk.Color ToGdkColor(this Gdk.RGBA rgba) =>
			new Gdk.Color((byte)(rgba.Red * 255), (byte)(rgba.Green * 255), (byte)(rgba.Blue * 255));

		public static Gdk.RGBA ToRgba(this Gdk.Color color) => new Gdk.RGBA
		{
			Red = color.Red / 65535.0,
			Green = color.Green / 65535.0,
			Blue = color.Blue / 65535.0,
			Alpha = 1.0
		};
	}
}
