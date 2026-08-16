using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Capture
{
	/// <summary>The outcome of comparing a capture against a baseline.</summary>
	public sealed class ImageComparison
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public long TotalPixels { get; set; }
		public long DifferingPixels { get; set; }
		public int WorstChannelDelta { get; set; }

		/// <summary>True when the capture had to be resampled to the baseline's size first.</summary>
		public bool Rescaled { get; set; }

		/// <summary>Set when the two images are not the same shape, which no tolerance can excuse.</summary>
		public string ShapeMismatch { get; set; }

		public double DifferingFraction => TotalPixels == 0 ? 0.0 : (double)DifferingPixels / TotalPixels;

		/// <summary>Differing pixels marked in magenta over the baseline, for review. Owned by the caller.</summary>
		public Bitmap Diff { get; set; }

		public string Describe() =>
			ShapeMismatch ??
			$"{DifferingPixels} of {TotalPixels} pixels ({DifferingFraction:P3}) differ by more than the " +
			$"channel tolerance; worst channel delta {WorstChannelDelta}. Compared at {Width}x{Height}" +
			(Rescaled ? " after rescaling the capture to the baseline's size." : ".");
	}

	/// <summary>
	/// Pixel comparison with the two tolerances plan section 6 requires: a per-pixel channel
	/// tolerance, and a percentage-of-differing-pixels threshold.
	/// </summary>
	/// <remarks>
	/// MEASURED 2026-08-16, and it is better than section 6 feared: two separate launches of the
	/// gallery on this machine captured BYTE-IDENTICALLY - 0 of 520812 pixels differing, worst
	/// channel delta 0. So the run-to-run noise the plan worried about ("a raw pixel-equality diff
	/// will not survive DPI scaling, theme, or font availability") is not run-to-run noise at all
	/// on one machine; it is MACHINE-to-machine noise. Both tolerances below therefore have very
	/// large headroom here and exist for the case the plan actually describes: a baseline captured
	/// on a different display scale, theme or font set.
	///
	/// The consequence worth stating: a failure of this comparison on the machine that produced the
	/// baseline is a real change, not noise. Do not widen a tolerance to make one go away.
	/// </remarks>
	public static class ImageComparer
	{
		/// <summary>
		/// How far one channel may move before the pixel counts as different, out of 255.
		/// Absorbs subpixel/greyscale antialiasing differences and DWM blending; well below any
		/// real colour change, so a BoxView that stopped painting still registers on every pixel.
		/// </summary>
		public const int DefaultChannelTolerance = 12;

		/// <summary>
		/// How much of the image may differ before the comparison fails.
		/// </summary>
		/// <remarks>
		/// MEASURED against a deliberately corrupted baseline: a 200x160 block painted over the
		/// golden image - 6.144% of it, and about the area of two gallery list rows - failed this
		/// gate with room to spare, while a clean capture differed by 0%. So the threshold
		/// separates "a control vanished, moved or changed colour" from "nothing changed", which is
		/// what it is for. It is NOT calibrated to catch a one-word text reflow.
		/// </remarks>
		public const double DefaultDifferingFraction = 0.02;

		/// <summary>
		/// The two images must agree on aspect ratio to this fraction before rescaling is allowed.
		/// A capture that is a different SHAPE is a layout regression, not a DPI difference, and
		/// squashing it to fit would hide exactly the defect this suite was built to catch (plan
		/// section 3 R1: an 828x7860 window).
		/// </summary>
		public const double AspectTolerance = 0.02;

		public static ImageComparison Compare(
			Bitmap actual,
			Bitmap baseline,
			int channelTolerance = DefaultChannelTolerance)
		{
			if (actual == null)
				throw new ArgumentNullException(nameof(actual));

			if (baseline == null)
				throw new ArgumentNullException(nameof(baseline));

			var result = new ImageComparison
			{
				Width = baseline.Width,
				Height = baseline.Height,
			};

			double actualAspect = (double)actual.Width / actual.Height;
			double baselineAspect = (double)baseline.Width / baseline.Height;

			if (Math.Abs(actualAspect - baselineAspect) > baselineAspect * AspectTolerance)
			{
				result.ShapeMismatch =
					$"The capture is {actual.Width}x{actual.Height} (aspect {actualAspect:0.0000}) but the " +
					$"baseline is {baseline.Width}x{baseline.Height} (aspect {baselineAspect:0.0000}). " +
					"Those are different shapes, so this is a layout change and not a display-scale " +
					"difference; no pixel tolerance applies.";

				return result;
			}

			Bitmap normalized = actual;
			bool rescaled = false;

			// Scale-normalise, without ever naming a factor. The capture comes back at the display
			// scale (MEASURED 2x here, plan section 1.2), so a baseline taken at a different scale
			// is still comparable - it is resampled onto the baseline's grid and the channel
			// tolerance absorbs the resampling. When the scales agree, which is the normal case,
			// nothing is resampled at all.
			if (actual.Width != baseline.Width || actual.Height != baseline.Height)
			{
				normalized = Rescale(actual, baseline.Width, baseline.Height);
				rescaled = true;
			}

			try
			{
				result.Rescaled = rescaled;

				Measure(normalized, baseline, channelTolerance, result);

				return result;
			}
			finally
			{
				if (rescaled)
					normalized.Dispose();
			}
		}

		static Bitmap Rescale(Bitmap source, int width, int height)
		{
			var scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			try
			{
				using (var graphics = Graphics.FromImage(scaled))
				{
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
					graphics.CompositingMode = CompositingMode.SourceCopy;
					graphics.DrawImage(source, new Rectangle(0, 0, width, height));
				}

				return scaled;
			}
			catch
			{
				scaled.Dispose();
				throw;
			}
		}

		static unsafe void Measure(Bitmap actual, Bitmap baseline, int channelTolerance, ImageComparison result)
		{
			var area = new Rectangle(0, 0, baseline.Width, baseline.Height);

			BitmapData actualData = actual.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			BitmapData baselineData = baseline.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			var diff = new Bitmap(baseline.Width, baseline.Height, PixelFormat.Format32bppArgb);
			BitmapData diffData = diff.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			try
			{
				long differing = 0;
				int worst = 0;

				for (int y = 0; y < area.Height; y++)
				{
					byte* actualRow = (byte*)actualData.Scan0 + ((long)y * actualData.Stride);
					byte* baselineRow = (byte*)baselineData.Scan0 + ((long)y * baselineData.Stride);
					byte* diffRow = (byte*)diffData.Scan0 + ((long)y * diffData.Stride);

					for (int x = 0; x < area.Width; x++)
					{
						int offset = x * 4;

						int deltaB = Math.Abs(actualRow[offset] - baselineRow[offset]);
						int deltaG = Math.Abs(actualRow[offset + 1] - baselineRow[offset + 1]);
						int deltaR = Math.Abs(actualRow[offset + 2] - baselineRow[offset + 2]);

						int delta = Math.Max(deltaB, Math.Max(deltaG, deltaR));

						if (delta > worst)
							worst = delta;

						if (delta > channelTolerance)
						{
							differing++;

							// Magenta: nothing in the gallery's palette is this colour, so a diff
							// image reads at a glance.
							diffRow[offset] = 0xFF;
							diffRow[offset + 1] = 0x00;
							diffRow[offset + 2] = 0xFF;
							diffRow[offset + 3] = 0xFF;
						}
						else
						{
							// Unchanged regions are kept, dimmed, so the marks have context.
							diffRow[offset] = (byte)(baselineRow[offset] / 3);
							diffRow[offset + 1] = (byte)(baselineRow[offset + 1] / 3);
							diffRow[offset + 2] = (byte)(baselineRow[offset + 2] / 3);
							diffRow[offset + 3] = 0xFF;
						}
					}
				}

				result.TotalPixels = (long)area.Width * area.Height;
				result.DifferingPixels = differing;
				result.WorstChannelDelta = worst;
			}
			finally
			{
				diff.UnlockBits(diffData);
				baseline.UnlockBits(baselineData);
				actual.UnlockBits(actualData);
			}

			result.Diff = diff;
		}
	}
}
