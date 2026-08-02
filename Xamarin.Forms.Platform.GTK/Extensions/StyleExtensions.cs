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
		/// <remarks>
		/// The CSS declarations alone are not enough, and the reason is measured rather than
		/// theoretical (plan §8.3.1, <c>scratchpad/shell23-boldprobe2.sh</c>): a CSS
		/// <c>font-weight</c>/<c>font-size</c> change does <b>not</b> invalidate the cached
		/// <see cref="Pango.Layout"/> behind a <see cref="Gtk.Label"/> or <see cref="Gtk.Entry"/>.
		/// The style context reports the new font and the colour repaints, while the widget keeps
		/// the size - and, until something else invalidates it, the glyphs - it had under the old
		/// one: a label taken bold and back through CSS measured 201 → 236 → <b>236</b>, where the
		/// same round trip through <see cref="Pango.AttrList"/> gives 215 → 253 → 215. So the font
		/// is *also* pushed as a Pango attribute, which is what clears the layout and queues the
		/// resize; the CSS is kept because it is what reaches composite sub-nodes (an
		/// <c>Gtk.Entry</c>'s <c>text</c> node, a <c>Gtk.Button</c>'s inner label) that have no
		/// attribute list of their own.
		///
		/// A widget with neither an attribute list nor a cached layout of its own -
		/// <see cref="Gtk.TextView"/> is the one this backend uses - gets an explicit
		/// <c>QueueResize</c> instead, so no caller is left relying on a style change alone to
		/// produce a new size request.
		/// </remarks>
		public static void SetFont(this Widget widget, Pango.FontDescription font)
		{
			if (widget == null)
				return;

			if (font == null)
			{
				widget.ClearStyle();
				widget.ApplyFontAttributes(null);
				return;
			}

			if (!string.IsNullOrWhiteSpace(font.Family))
				widget.SetStyleProperty("font-family", $"\"{font.Family}\"");

			widget.SetStyleProperty("font-size",
				(font.Size / Pango.Scale.PangoScale).ToString("0.##", CultureInfo.InvariantCulture) + "pt");
			widget.SetStyleProperty("font-weight", font.Weight >= Pango.Weight.Bold ? "bold" : "normal");
			widget.SetStyleProperty("font-style", font.Style == Pango.Style.Italic ? "italic" : "normal");

			widget.ApplyFontAttributes(font);
		}

		/// <summary>
		/// Pushes <paramref name="font"/> onto the widget's Pango attribute list, or clears it when
		/// <paramref name="font"/> is null. See the remarks on <see cref="SetFont"/> for why this
		/// exists alongside the CSS.
		/// </summary>
		/// <remarks>
		/// An attribute list built from a <see cref="Pango.FontDescription"/> only carries the
		/// fields that description actually set, so leaving <c>Family</c> unset - which
		/// <c>FontDescriptionHelper</c> does when the Forms element asks for no font family - still
		/// leaves the theme font in place instead of pinning the label to a hardcoded one.
		/// </remarks>
		public static void ApplyFontAttributes(this Widget widget, Pango.FontDescription font)
		{
			if (widget == null)
				return;

			Pango.AttrList attributes = null;

			if (font != null)
			{
				attributes = new Pango.AttrList();
				attributes.Insert(new Pango.AttrFontDesc(font));
			}

			switch (widget)
			{
				case Gtk.Label label when label.UseMarkup:
					// MEASURED: an explicit font-description attribute set here *overrides* the
					// attributes the markup itself carries - a <b> in the output of
					// GenerateMarkupText stopped measuring bold (70px against the plain 70px, where
					// it is 76px on its own). A markup label carries its font inside the markup
					// already - that is how LabelRenderer maps Forms fonts - so the markup wins here
					// and this call only re-parses it, which is what clears the cached layout
					// (gtk_label_set_markup runs gtk_label_recalculate unconditionally).
					//
					// Consequence, deliberate and worth knowing before calling this on a markup
					// label: the FontDescription is then NOT applied to it. No renderer in this
					// backend does - LabelRenderer goes through GenerateMarkupText and never through
					// SetFont - and flattening an app's markup would be the worse of the two.
					label.Markup = label.LabelProp;
					break;

				case Gtk.Label label:
					label.Attributes = attributes;
					break;
				case Gtk.Entry entry:
					entry.Attributes = attributes;
					break;
				default:
					widget.QueueResize();
					widget.QueueDraw();
					break;
			}
		}

		// ---- theme-default readers (replacing Gtk.Style.* arrays) ----------------------

		/// <summary>
		/// Replaces <c>Style.Backgrounds[(int)state]</c>. Reads the CSS property directly:
		/// gtk_style_context_get_background_color is deprecated, while the generic property
		/// getter is not, and both return the same "background-color" value.
		/// </summary>
		public static Gdk.Color GetDefaultBackgroundColor(this Widget widget, StateFlags state = StateFlags.Normal)
		{
			if (widget == null)
				return new Gdk.Color(0, 0, 0);

			using (var value = widget.StyleContext.GetProperty("background-color", state))
				return ((Gdk.RGBA)value.Val).ToGdkColor();
		}

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

		/// <summary>
		/// Replaces <c>Style.FontDescription</c>. As above, the generic property getter stands in
		/// for the deprecated gtk_style_context_get_font.
		/// </summary>
		public static Pango.FontDescription GetDefaultFont(this Widget widget, StateFlags state = StateFlags.Normal)
		{
			if (widget == null)
				return null;

			using (var value = widget.StyleContext.GetProperty("font", state))
				return value.Val as Pango.FontDescription;
		}

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
