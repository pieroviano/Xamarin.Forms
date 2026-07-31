using System.Collections.Generic;
using System.IO;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.GTK.Helpers;

namespace Xamarin.Forms.Platform.GTK.Extensions
{
	/// <summary>
	/// Turns a Xamarin.Forms <c>FontFamily</c> into something Pango can match on.
	/// <para>
	/// Pango resolves fonts by <em>family name</em> through fontconfig. A Forms app, on the other
	/// hand, names fonts the way the other backends do: by the file name or alias given to
	/// <c>[assembly: ExportFont("MyFont.ttf", Alias = "MyFont")]</c>, optionally with the
	/// <c>"file.ttf#Family Name"</c> suffix. Handing those straight to
	/// <c>Pango.FontDescription.Family</c> - which is what this backend used to do - silently
	/// produced the theme's default font.
	/// </para>
	/// <para>
	/// This mirrors <c>Xamarin.Forms.Platform.WPF/Extensions/FontExtensions.cs</c>: ask
	/// <see cref="FontRegistrar"/> first (which extracts the font through
	/// <see cref="GtkEmbeddedFontLoader"/> on the way), and resolve the resulting file to the family
	/// name that is actually inside it.
	/// </para>
	/// </summary>
	public static class FontExtensions
	{
		static readonly object _sync = new object();
		static readonly Dictionary<string, string> _resolvedFamilies = new Dictionary<string, string>();

		/// <summary>
		/// Builds a Pango font description for a Forms <see cref="Font"/>, resolving embedded fonts.
		/// </summary>
		public static Pango.FontDescription ToPangoFontDescription(this Font self)
			=> FontDescriptionHelper.CreateFontDescription(self.FontSize, self.FontFamily, self.FontAttributes);

		/// <summary>
		/// Resolves a Forms <c>FontFamily</c> to the Pango/fontconfig family name, extracting the
		/// font from the assembly first when it was declared with <c>[assembly: ExportFont]</c>.
		/// Returns <see langword="null"/> when no family was requested, which leaves the font
		/// description's family unset - i.e. lets the GTK theme decide.
		/// </summary>
		public static string ToPangoFamily(this string fontFamily)
		{
			if (string.IsNullOrWhiteSpace(fontFamily))
				return null;

			lock (_sync)
			{
				if (_resolvedFamilies.TryGetValue(fontFamily, out var cached))
					return cached;
			}

			var resolved = ResolveFamily(fontFamily);

			lock (_sync)
			{
				_resolvedFamilies[fontFamily] = resolved;
			}

			return resolved;
		}

		static string ResolveFamily(string fontFamily)
		{
			// 1. The alias, or the exact file name, as given to [ExportFont].
			if (TryResolveEmbedded(fontFamily, out var family))
				return family;

			var fontFile = FontFile.FromString(fontFamily);

			// 2. The file name, with the extension the caller gave - or each extension we support
			//    when they only named the font ("MyFont" -> "MyFont.ttf", "MyFont.otf").
			if (!string.IsNullOrWhiteSpace(fontFile.Extension))
			{
				if (TryResolveEmbedded(fontFile.FileNameWithExtension(), out family))
					return family;
			}
			else
			{
				foreach (var extension in FontFile.Extensions)
				{
					if (TryResolveEmbedded(fontFile.FileNameWithExtension(extension), out family))
						return family;
				}
			}

			// 3. Not an embedded font. Honour the "path/to/file.ttf#Family Name" convention that
			//    Forms allows everywhere, then fall back to the bare name for fontconfig to match.
			var separator = fontFamily.IndexOf('#');

			if (separator >= 0)
			{
				var declared = fontFamily.Substring(separator + 1).Trim();

				if (!string.IsNullOrWhiteSpace(declared))
					return declared;

				var path = fontFamily.Substring(0, separator);

				// A loose file on disk still has a real family name inside it; prefer that over
				// guessing from the file name.
				if (File.Exists(path))
				{
					FontConfigHelper.AddAppFontFile(path);

					var queried = FontConfigHelper.QueryFamilyName(path);

					if (!string.IsNullOrWhiteSpace(queried))
						return queried;
				}

				return Path.GetFileNameWithoutExtension(path);
			}

			// A path to a font file that was never registered: load it and use its real family.
			if (!string.IsNullOrWhiteSpace(fontFile.Extension) && File.Exists(fontFamily))
			{
				FontConfigHelper.AddAppFontFile(fontFamily);

				var queried = FontConfigHelper.QueryFamilyName(fontFamily);

				if (!string.IsNullOrWhiteSpace(queried))
					return queried;
			}

			// "MyFont.ttf" that is neither registered nor on disk: strip the extension, otherwise
			// fontconfig matches nothing at all.
			return string.IsNullOrWhiteSpace(fontFile.Extension) ? fontFamily : fontFile.FileName;
		}

		static bool TryResolveEmbedded(string key, out string family)
		{
			family = null;

			if (string.IsNullOrWhiteSpace(key))
				return false;

			// HasFont is what runs GtkEmbeddedFontLoader: it extracts the resource to disk, adds it
			// to the fontconfig configuration, and hands back the file it wrote.
			var (hasFont, fontPath) = FontRegistrar.HasFont(key);

			if (!hasFont || string.IsNullOrWhiteSpace(fontPath))
				return false;

			family = FontConfigHelper.QueryFamilyName(fontPath);

			if (string.IsNullOrWhiteSpace(family))
			{
				// fontconfig could not be asked (or the file is not a font it understands): fall
				// back to the same name-mangling the other backends use - "PTSansNarrow-Regular"
				// becomes "PT Sans Narrow". This is a guess: the family name inside the file is
				// authoritative and is what the fontconfig route above returns.
				family = FontFile.FromString(Path.GetFileNameWithoutExtension(fontPath)).GetPostScriptNameWithSpaces();
			}

			return !string.IsNullOrWhiteSpace(family);
		}
	}
}
