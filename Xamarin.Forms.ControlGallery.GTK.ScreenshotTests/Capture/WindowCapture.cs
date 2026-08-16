using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Interop;

namespace Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Capture
{
	/// <summary>
	/// A window's pixels, plus the geometry Win32 reported for it at the moment of capture.
	/// </summary>
	public sealed class CapturedWindow : IDisposable
	{
		internal CapturedWindow(Bitmap image, int windowWidth, int windowHeight, string diagnostics)
		{
			Image = image;
			WindowWidth = windowWidth;
			WindowHeight = windowHeight;
			Diagnostics = diagnostics;
		}

		/// <summary>The captured pixels. Owned by this instance.</summary>
		public Bitmap Image { get; }

		/// <summary>Window width as <c>GetWindowRect</c> reported it, in the units the test asserts on.</summary>
		public int WindowWidth { get; }

		/// <summary>Window height as <c>GetWindowRect</c> reported it.</summary>
		public int WindowHeight { get; }

		/// <summary>Non-fatal problem with the captured pixels, or null.</summary>
		public string Diagnostics { get; }

		/// <summary>
		/// Pixels per reported window unit, horizontally. MEASURED as 2.0 on this machine's HiDPI
		/// display (plan section 1.2: "the GTK 4 capture comes back at 2x the reported window
		/// height"), but it is DERIVED here and never assumed: on a 100%-scale monitor it is 1.0,
		/// and a mixed-DPI desktop can produce 1.5 or 1.25. Anything that compares a capture to a
		/// baseline has to go through <see cref="ImageComparer"/>, which normalises on the images'
		/// own dimensions rather than on any factor named in code.
		/// </summary>
		public double ScaleX => WindowWidth <= 0 ? 1.0 : (double)Image.Width / WindowWidth;

		/// <summary>Pixels per reported window unit, vertically. See <see cref="ScaleX"/>.</summary>
		public double ScaleY => WindowHeight <= 0 ? 1.0 : (double)Image.Height / WindowHeight;

		public void Dispose() => Image.Dispose();
	}

	/// <summary>
	/// PrintWindow-based capture of a single window.
	/// </summary>
	/// <remarks>
	/// LIFTED from ProcessWindowBitmapSave's <c>Capture/WindowCapture.cs</c> - see the remarks on
	/// <see cref="NativeMethods"/> for why lifted rather than referenced. Trimmed to the one mode
	/// this suite uses: whole window, DWM resize border trimmed off, alpha forced opaque. The
	/// file-naming, output-path and minimised-restore paths are the CLI's and did not come across;
	/// a test that had to restore a minimised window would be measuring the desktop, not the
	/// gallery.
	/// </remarks>
	internal static class WindowCapture
	{
		public static CapturedWindow Capture(WindowInfo window)
		{
			NativeMethods.EnsureDpiAware();

			IntPtr hWnd = window.Handle;

			if (!NativeMethods.IsWindow(hWnd))
				throw new InvalidOperationException($"Window 0x{hWnd.ToInt64():X} is no longer valid.");

			if (NativeMethods.IsIconic(hWnd))
			{
				throw new InvalidOperationException(
					$"Window 0x{hWnd.ToInt64():X} is minimized and would capture as garbage. " +
					"This suite deliberately does not restore it: doing so would change what is on " +
					"screen and make the capture depend on the desktop's state.");
			}

			if (!NativeMethods.GetWindowRect(hWnd, out var windowRect))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "GetWindowRect failed.");

			int width = windowRect.Width;
			int height = windowRect.Height;

			if (width <= 0 || height <= 0)
				throw new InvalidOperationException($"Window 0x{hWnd.ToInt64():X} has no drawable area ({width}x{height}).");

			using (Bitmap full = RenderWindow(hWnd, width, height))
			{
				Rectangle crop = ResolveCropRect(hWnd, windowRect, full.Size);

				Bitmap image = crop == new Rectangle(Point.Empty, full.Size)
					? (Bitmap)full.Clone()
					: full.Clone(crop, PixelFormat.Format32bppArgb);

				try
				{
					PixelSummary pixels = NormalizeAlpha(image);

					return new CapturedWindow(image, crop.Width, crop.Height, DescribeWarning(pixels));
				}
				catch
				{
					image.Dispose();
					throw;
				}
			}
		}

		static Bitmap RenderWindow(IntPtr hWnd, int width, int height)
		{
			var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			try
			{
				using (var graphics = Graphics.FromImage(bitmap))
				{
					IntPtr hdc = graphics.GetHdc();

					try
					{
						if (!NativeMethods.PrintWindow(hWnd, hdc, NativeMethods.PW_RENDERFULLCONTENT))
						{
							int error = Marshal.GetLastWin32Error();

							throw new Win32Exception(
								error,
								$"PrintWindow failed for window 0x{hWnd.ToInt64():X}" +
								(error != 0 ? $" (Win32 error {error})." : ".") +
								" An elevated window cannot be captured by a non-elevated process.");
						}
					}
					finally
					{
						graphics.ReleaseHdc(hdc);
					}
				}

				return bitmap;
			}
			catch
			{
				bitmap.Dispose();
				throw;
			}
		}

		/// <summary>
		/// PrintWindow renders into a surface the size of GetWindowRect, which on DWM includes an
		/// invisible resize border. Trimming to the extended frame bounds is what makes the
		/// captured width match the width the plan quotes.
		/// </summary>
		static Rectangle ResolveCropRect(IntPtr hWnd, NativeMethods.RECT windowRect, Size surface)
		{
			var whole = new Rectangle(Point.Empty, surface);

			bool haveFrame = NativeMethods.DwmGetWindowAttributeRect(
				hWnd,
				NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
				out var frame,
				Marshal.SizeOf<NativeMethods.RECT>()) == 0;

			if (!haveFrame || frame.Width <= 0 || frame.Height <= 0)
				return whole;

			var trimmed = new Rectangle(
				frame.Left - windowRect.Left,
				frame.Top - windowRect.Top,
				frame.Width,
				frame.Height);

			trimmed.Intersect(whole);

			return trimmed.Width <= 0 || trimmed.Height <= 0 ? whole : trimmed;
		}

		internal readonly struct PixelSummary
		{
			public PixelSummary(bool isUniform, bool isFullyTransparent)
			{
				IsUniform = isUniform;
				IsFullyTransparent = isFullyTransparent;
			}

			public bool IsUniform { get; }
			public bool IsFullyTransparent { get; }
		}

		/// <summary>
		/// Inspects the captured pixels and forces every one of them opaque. PrintWindow commonly
		/// leaves alpha at zero, which produces a PNG that looks empty - and, here, a baseline
		/// comparison that would be comparing two blank images and passing.
		/// </summary>
		internal static unsafe PixelSummary NormalizeAlpha(Bitmap bitmap)
		{
			var area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData data = bitmap.LockBits(area, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			try
			{
				bool uniform = true;
				bool fullyTransparent = true;
				uint first = *(uint*)data.Scan0;

				for (int y = 0; y < data.Height; y++)
				{
					uint* row = (uint*)((byte*)data.Scan0 + ((long)y * data.Stride));

					for (int x = 0; x < data.Width; x++)
					{
						uint pixel = row[x];

						if (pixel != first)
							uniform = false;

						if ((pixel & 0xFF000000u) != 0)
							fullyTransparent = false;

						row[x] = pixel | 0xFF000000u;
					}
				}

				return new PixelSummary(uniform, fullyTransparent);
			}
			finally
			{
				bitmap.UnlockBits(data);
			}
		}

		internal static string DescribeWarning(PixelSummary pixels)
		{
			if (pixels.IsUniform)
			{
				return "The window rendered as a single flat colour. That usually means it draws " +
					"through a surface PrintWindow cannot read, or it had not painted yet.";
			}

			if (pixels.IsFullyTransparent)
				return "Every captured pixel had alpha 0 before normalisation.";

			return null;
		}
	}
}
