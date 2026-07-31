using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xamarin.Forms.Platform.GTK.Helpers;

namespace Xamarin.Forms.Platform.GTK
{
	/// <summary>
	/// Extracts fonts declared with <c>[assembly: ExportFont("MyFont.ttf")]</c> out of the
	/// application assembly and onto disk, so that fontconfig - and therefore Pango, and therefore
	/// every GTK widget - can resolve them by family name.
	/// <para>
	/// Unlike WPF, where <c>PrivateFontCollection</c> makes an arbitrary file loadable, a font on
	/// Linux has to be somewhere fontconfig looks. The file is written under the user font
	/// directory (<c>$XDG_DATA_HOME/fonts/Xamarin.Forms</c>, i.e. <c>~/.local/share/fonts/Xamarin.Forms</c>)
	/// so it survives, and is additionally handed to <c>FcConfigAppFontAddFile</c> so it is usable
	/// immediately rather than only by processes started later.
	/// </para>
	/// </summary>
	public class GtkEmbeddedFontLoader : IEmbeddedFontLoader
	{
		internal const string FontCacheFolderName = "Xamarin.Forms";

		public (bool success, string filePath) LoadFont(EmbeddedFont font)
		{
			if (font == null || string.IsNullOrWhiteSpace(font.FontName))
				return (false, null);

			// The registered name is a resource file name; never let it escape the cache directory.
			var fontName = Path.GetFileName(font.FontName);

			if (string.IsNullOrWhiteSpace(fontName))
				return (false, null);

			string filePath;

			try
			{
				var directory = GetFontDirectory();
				Directory.CreateDirectory(directory);
				filePath = Path.Combine(directory, fontName);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return (false, null);
			}

			if (File.Exists(filePath))
			{
				// Already extracted by a previous run; it still has to be added to *this* process's
				// fontconfig configuration, otherwise it only resolves after a restart.
				FontConfigHelper.AddAppFontFile(filePath);
				return (true, filePath);
			}

			try
			{
				if (font.ResourceStream == null)
					return (false, null);

				using (var fileStream = File.Create(filePath))
				{
					font.ResourceStream.CopyTo(fileStream);
				}

				FontConfigHelper.AddAppFontFile(filePath);

				return (true, filePath);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);

				try
				{
					File.Delete(filePath);
				}
				catch (Exception deleteException)
				{
					Debug.WriteLine(deleteException);
				}
			}

			return (false, null);
		}

		/// <summary>
		/// Where a font file has to live to be discoverable by the platform's font subsystem.
		/// </summary>
		static string GetFontDirectory()
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			if (!string.IsNullOrEmpty(home))
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					return Path.Combine(home, "Library", "Fonts", FontCacheFolderName);

				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					// freedesktop.org basedir spec: fontconfig scans $XDG_DATA_HOME/fonts recursively.
					var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

					if (string.IsNullOrWhiteSpace(dataHome))
						dataHome = Path.Combine(home, ".local", "share");

					return Path.Combine(dataHome, "fonts", FontCacheFolderName);
				}
			}

			// Windows (and any platform without a usable home): mirror what
			// Xamarin.Forms.Platform.WPF does and use a per-application temp folder.
			var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
			var appName = entryAssembly?.GetName()?.Name ?? "Xamarin.Forms";

			return Path.Combine(Path.GetTempPath(), appName, "Fonts");
		}
	}
}
