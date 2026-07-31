using Pango;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Helpers
{
	internal static class FontDescriptionHelper
	{
		internal static FontDescription CreateFontDescription(double fontSize, string fontFamily, FontAttributes attributes)
		{
			FontDescription fontDescription = new FontDescription();
			fontDescription.Size = (int)(fontSize * Scale.PangoScale);

			// Never hand the raw FontFamily to Pango: an [ExportFont] font is named by its file
			// name or alias, which fontconfig knows nothing about. ToPangoFamily extracts the font
			// if needed and returns the family name that is actually inside the file. A null result
			// means "no family was asked for" - leave the field unset so the theme font is used.
			var family = fontFamily.ToPangoFamily();

			if (!string.IsNullOrWhiteSpace(family))
				fontDescription.Family = family;

			fontDescription.Weight = attributes.HasFlag(FontAttributes.Bold) ? Weight.Bold : Weight.Normal;
			fontDescription.Style = attributes.HasFlag(FontAttributes.Italic) ? Pango.Style.Italic : Pango.Style.Normal;

			return fontDescription;
		}
	}
}
