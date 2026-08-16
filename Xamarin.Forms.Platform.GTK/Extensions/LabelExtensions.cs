using System.Globalization;
using System.Security;
using System.Text;
using Pango;
using Xamarin.Forms.Platform.GTK.Helpers;

namespace Xamarin.Forms.Platform.GTK.Extensions
{
	internal static class LabelExtensions
	{
		internal static void SetTextFromFormatted(this Gtk.Label self, FormattedString formatted)
		{
			string markupText = GenerateMarkupText(formatted);

			if (self != null)
			{
				self.Markup = markupText;
			}
		}

		internal static void SetTextFromSpan(this Gtk.Label self, Span span)
		{
			string markupText = GenerateMarkupText(span);

			if (self != null)
			{
				self.Markup = markupText;
			}
		}

		private static string GenerateMarkupText(FormattedString formatted)
		{
			StringBuilder builder = new StringBuilder();

			foreach (Span span in formatted.Spans)
			{
				builder.Append(GenerateMarkupText(span));
			}

			return builder.ToString();
		}

		/// <summary>
		/// Builds the Pango markup for one <see cref="Span"/>.
		/// </summary>
		/// <remarks>
		/// The font goes out as the discrete <c>font_family</c>/<c>font_size</c>/<c>font_weight</c>/
		/// <c>font_style</c> attributes rather than as a single <c>font</c> attribute, because
		/// <c>font</c> is a font-description <em>string</em>: Pango re-parses it with
		/// <c>pango_font_description_from_string</c>, which reads trailing words of the family as
		/// style keywords. MEASURED: <c>font="Times New Roman Bold 11"</c> resolves to the family
		/// <c>"Times New"</c> - "Roman" is Pango's own spelling of <c>PANGO_STYLE_NORMAL</c> - and
		/// Pango logs <c>couldn't load font "Times New Bold Not-Rotated 11", falling back to
		/// "Sans Bold Not-Rotated 11"</c>, measuring 102px where the family it was asked for measures
		/// 89px. Every family whose name ends in a style word is affected - Century Schoolbook,
		/// Segoe UI Light, Arial Black, Roboto Condensed, Franklin Gothic Book. <c>font_family</c>
		/// takes the name verbatim and cannot mis-parse.
		///
		/// Omitting <c>font_family</c> when the element named none is the other half: an attribute
		/// list carries only the fields it sets, so the widget's own font - the GTK theme's, or
		/// whatever a renderer put on it - supplies the family. That matches what
		/// <c>FontDescriptionHelper</c> means by leaving <c>Family</c> unset.
		///
		/// <c>font_size</c> is emitted in Pango units (1024ths of a point), which is the unit
		/// <see cref="FontDescription.Size"/> already holds and the unit the markup attribute reads a
		/// bare number in, so no point-size formatting is involved. Weight and style are emitted
		/// unconditionally, exactly as the old <c>font</c> string did, so a bold or italic span still
		/// measures bold or italic and a plain one is not left inheriting a themed bold.
		/// </remarks>
		private static string GenerateMarkupText(Span span)
		{
			StringBuilder builder = new StringBuilder();

			builder.Append("<span");

			FontDescription fontDescription = FontDescriptionHelper.CreateFontDescription(
				span.FontSize, span.FontFamily, span.FontAttributes);

			// Family => only when one was actually requested; see the remarks.
			if (!string.IsNullOrWhiteSpace(fontDescription.Family))
			{
				builder.AppendFormat(" font_family=\"{0}\"", SecurityElement.Escape(fontDescription.Family));
			}

			// Size => Pango units, the units FontDescription.Size is already in.
			if (fontDescription.Size > 0)
			{
				builder.AppendFormat(CultureInfo.InvariantCulture, " font_size=\"{0}\"", fontDescription.Size);
			}

			builder.AppendFormat(" font_weight=\"{0}\"",
				fontDescription.Weight >= Pango.Weight.Bold ? "bold" : "normal");

			builder.AppendFormat(" font_style=\"{0}\"",
				fontDescription.Style == Pango.Style.Italic ? "italic" : "normal");

			// BackgroundColor =>
			if (!span.BackgroundColor.IsDefault)
			{
				builder.AppendFormat(" bgcolor=\"{0}\"", span.BackgroundColor.ToRgbaColor());
			}

			// ForegroundColor => 
			if (!span.TextColor.IsDefault)
			{
				builder.AppendFormat(" fgcolor=\"{0}\"", span.TextColor.ToRgbaColor());
			}

			builder.Append(">"); // Complete opening span tag

			// Text
			builder.Append(SecurityElement.Escape(span.Text));
			builder.Append("</span>");

			return builder.ToString();
		}
	}
}
