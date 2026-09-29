using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Xamarin.Forms.Wpf
{
	/// <summary>
	/// Finds what a WPF resource URI names. WPF resolves <c>pack://application:,,,/Folder/Image.png</c> - and a
	/// relative <c>Folder/Image.png</c> - to a resource compiled into the application; a project built for this
	/// library carries the same files either next to the executable or as embedded resources, so both are looked in.
	/// </summary>
	internal static class ResourceLocator
	{
		const string PackPrefix = "pack://application:,,,";

		/// <summary>The bytes of the resource, or null when nothing by that name exists.</summary>
		internal static byte[] Read(Uri uri)
		{
			if (uri == null)
				return null;

			if (uri.IsAbsoluteUri && uri.IsFile)
				return File.Exists(uri.LocalPath) ? File.ReadAllBytes(uri.LocalPath) : null;

			var path = RelativePath(uri.OriginalString);
			if (path == null)
				return null;

			return FromFile(path) ?? FromEmbedded(path);
		}

		/// <summary>The application-relative path a URI names, with forward slashes; null for another scheme.</summary>
		internal static string RelativePath(string uri)
		{
			var s = uri.Trim();
			if (s.StartsWith(PackPrefix, StringComparison.OrdinalIgnoreCase))
				s = s.Substring(PackPrefix.Length);
			else if (s.Contains("://"))
				return null;

			s = s.Replace('\\', '/').TrimStart('/');

			// pack://application:,,,/Assembly;component/Folder/Image.png
			var component = s.IndexOf(";component/", StringComparison.OrdinalIgnoreCase);
			if (component >= 0)
				s = s.Substring(component + ";component/".Length);

			return s;
		}

		/// <summary>
		/// A file under the application folder: at the path itself, else anywhere below it with that path as its
		/// tail - WPF resolves a relative URI against the XAML file that names it, and that folder is not known here.
		/// </summary>
		static byte[] FromFile(string path)
		{
			var root = AppContext.BaseDirectory;
			var direct = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
			if (File.Exists(direct))
				return File.ReadAllBytes(direct);

			var tail = Path.DirectorySeparatorChar + path.Replace('/', Path.DirectorySeparatorChar);
			try
			{
				var match = Directory.EnumerateFiles(root, Path.GetFileName(path), SearchOption.AllDirectories)
					.FirstOrDefault(f => f.EndsWith(tail, StringComparison.OrdinalIgnoreCase));
				return match == null ? null : File.ReadAllBytes(match);
			}
			catch (IOException)
			{
				return null;
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
		}

		/// <summary>An embedded resource whose manifest name ends with the path, dotted.</summary>
		static byte[] FromEmbedded(string path)
		{
			var dotted = "." + path.Replace('/', '.');
			foreach (var assembly in Candidates())
			{
				string[] names;
				try
				{
					names = assembly.GetManifestResourceNames();
				}
				catch (NotSupportedException)
				{
					continue;
				}

				var name = names.FirstOrDefault(n => n.EndsWith(dotted, StringComparison.OrdinalIgnoreCase)
					|| n.Equals(dotted.Substring(1), StringComparison.OrdinalIgnoreCase));
				if (name == null)
					continue;

				using (var stream = assembly.GetManifestResourceStream(name))
				using (var copy = new MemoryStream())
				{
					stream.CopyTo(copy);
					return copy.ToArray();
				}
			}

			return null;
		}

		static Assembly[] Candidates()
		{
			var entry = Assembly.GetEntryAssembly();
			var loaded = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => !a.IsDynamic && a != entry && !a.FullName.StartsWith("System.", StringComparison.Ordinal)
					&& !a.FullName.StartsWith("Microsoft.", StringComparison.Ordinal));
			return (entry == null ? loaded : new[] { entry }.Concat(loaded)).ToArray();
		}
	}

	/// <summary>The pixel size of an encoded PNG, GIF, BMP or JPEG, read from its header.</summary>
	internal static class ImageHeader
	{
		internal static (int Width, int Height) Size(byte[] b)
		{
			if (b == null || b.Length < 24)
				return (0, 0);

			// PNG: IHDR width and height, big-endian, at 16.
			if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
				return (BigEndian(b, 16), BigEndian(b, 20));

			// GIF: logical screen size, little-endian, at 6.
			if (b[0] == 'G' && b[1] == 'I' && b[2] == 'F')
				return (b[6] | (b[7] << 8), b[8] | (b[9] << 8));

			// BMP: BITMAPINFOHEADER width and height, little-endian, at 18.
			if (b[0] == 'B' && b[1] == 'M')
				return (LittleEndian(b, 18), Math.Abs(LittleEndian(b, 22)));

			// JPEG: the first start-of-frame marker.
			if (b[0] == 0xFF && b[1] == 0xD8)
			{
				var i = 2;
				while (i + 9 < b.Length)
				{
					if (b[i] != 0xFF)
					{
						i++;
						continue;
					}

					var marker = b[i + 1];
					var length = (b[i + 2] << 8) | b[i + 3];
					if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
						return ((b[i + 7] << 8) | b[i + 8], (b[i + 5] << 8) | b[i + 6]);

					i += 2 + length;
				}
			}

			return (0, 0);
		}

		static int BigEndian(byte[] b, int i) => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];

		static int LittleEndian(byte[] b, int i) => b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24);
	}
}
