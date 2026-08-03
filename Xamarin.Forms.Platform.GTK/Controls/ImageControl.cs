using System;
using Cairo;
using Gdk;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class ImageControl : Gtk.Box, IDesiredSizeProvider
	{
		private Gtk.Image _image;
		private Pixbuf _original;
		private ImageAspect _aspect;

		private double _scaleX;
		private double _scaleY;
		private double _scale;

		private double _rotation;

		private Gdk.Rectangle _lastAllocation = Gdk.Rectangle.Zero;

		public ImageControl()
		{
			_aspect = ImageAspect.AspectFill;
			_scaleX = 1.0;
			_scaleY = 1.0;
			_scale = 1.0;
			_rotation = 0.0;
			BuildImageControl();
		}

		public ImageAspect Aspect
		{
			get
			{
				return _aspect;
			}

			set
			{
				_aspect = value;
				QueueDraw();
			}
		}

		public Pixbuf Pixbuf
		{
			get
			{
				return _image.Pixbuf;
			}
			set
			{
				_lastAllocation = Gdk.Rectangle.Zero;
				_original = value;
				_image.Pixbuf = value;
			}
		}

		public double ScaleX
		{
			get
			{
				return _scaleX;
			}
			set
			{
				_scaleX = value;
				UpdateScaleAndRotation();
			}
		}

		public double ScaleY
		{
			get
			{
				return _scaleY;
			}
			set
			{
				_scaleY = value;
				UpdateScaleAndRotation();
			}
		}

		public double Scale
		{
			get
			{
				return _scale;
			}
			set
			{
				// Scale, ScaleX and ScaleY are three independent Forms properties (all default 1),
				// and the factor an axis really gets is Scale * ScaleX (resp. Scale * ScaleY). This
				// setter used to overwrite _scaleX/_scaleY as well, which threw away whatever the
				// renderer had pushed in from Element.ScaleX/ScaleY.
				_scale = value;
				UpdateScaleAndRotation();
			}

		}

		public double Rotation
		{
			get
			{
				return _rotation;
			}
			set
			{
				_rotation = value;
				UpdateScaleAndRotation();
			}
		}

		// There is deliberately no SetAlpha/opacity member here. Opacity is applied to the
		// container by VisualElementTracker.UpdateOpacity (gtk_widget_set_opacity), which is the
		// only place it belongs. The member that used to live here called Pixbuf.AddAlpha, whose
		// (r,g,b) arguments are a COLOUR KEY - gdk_pixbuf_add_alpha makes pixels EQUAL TO that
		// colour transparent - not a uniform alpha. It therefore did the opposite of what its name
		// promised, and because it read and wrote the already-scaled _image.Pixbuf every call
		// compounded the damage.

		public Gdk.Size GetDesiredSize()
		{
			return _original != null
				? new Gdk.Size(_original.Width, _original.Height)
				: Gdk.Size.Empty;
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (_image.Pixbuf != null && _lastAllocation != allocation)
			{
				_lastAllocation = allocation;
				UpdatePixBufWithAllocation(allocation);
			}
		}

		private void BuildImageControl()
		{
			CanFocus = true;

			_image = new Gtk.Image();

			PackStart(_image, true, true, 0);
		}

		private static Pixbuf GetAspectFitPixBuf(Pixbuf original, Gdk.Rectangle allocation)
		{
			var widthRatio = (float)allocation.Width / original.Width;
			var heigthRatio = (float)allocation.Height / original.Height;

			var fitRatio = Math.Min(widthRatio, heigthRatio);
			var finalWidth = (int)(original.Width * fitRatio);
			var finalHeight = (int)(original.Height * fitRatio);

			return original.ScaleSimple(finalWidth, finalHeight, InterpType.Bilinear);
		}

		private static Pixbuf GetAspectFillPixBuf(Pixbuf original, Gdk.Rectangle allocation)
		{
			var widthRatio = (float)allocation.Width / original.Width;
			var heigthRatio = (float)allocation.Height / original.Height;

			var fitRatio = Math.Max(widthRatio, heigthRatio);
			var finalWidth = (int)(original.Width * fitRatio);
			var finalHeight = (int)(original.Height * fitRatio);

			return original.ScaleSimple(finalWidth, finalHeight, InterpType.Bilinear);
		}

		private static Pixbuf GetFillPixBuf(Pixbuf original, Gdk.Rectangle allocation)
		{
			return original.ScaleSimple(allocation.Width, allocation.Height, InterpType.Bilinear);
		}

		private void UpdatePixBufWithAllocation(Gdk.Rectangle allocation)
		{
			var srcWidth = _original.Width;
			var srcHeight = _original.Height;

			Pixbuf newPixBuf = null;

			// Differents modes in which the image will be scaled to fit the display area.
			switch (Aspect)
			{
				case ImageAspect.AspectFit:
					newPixBuf = GetAspectFitPixBuf(_original, allocation);
					break;
				case ImageAspect.AspectFill:
					newPixBuf = GetAspectFillPixBuf(_original, allocation);
					break;
				case ImageAspect.Fill:
					newPixBuf = GetFillPixBuf(_original, allocation);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(Aspect));
			}

			if (newPixBuf != null)
			{
				_image.Pixbuf = newPixBuf;

				// Important: the image adapts to the window size, so this runs on every allocation
				// change. gtk_image_set_from_pixbuf took its own reference, so disposing the wrapper
				// here (Unref is deprecated) is what releases the previous scaled copy. There is
				// deliberately no GC.Collect() alongside it: the Dispose IS the release, and forcing
				// a blocking gen-2 collection per image per size-allocate stalled the GTK main loop
				// for the whole of a window resize.
				newPixBuf.Dispose();
			}
		}

		private void UpdateScaleAndRotation()
		{
			if (_image != null && _original != null)
			{
				Pixbuf rotated;

				if (_rotation != 0.0)
				{
					ImageSurface surface = new ImageSurface(Format.Argb32, _original.Width, _original.Height);
					Context ctx = new Context(surface);

					ctx.Translate(_original.Width / 2.0, _original.Height / 2.0);
					double radians = _rotation * (Math.PI / 180.0);
					ctx.Rotate(radians);
					ctx.Translate(-_original.Width / 2.0, -_original.Height / 2.0);
					CairoHelper.SetSourcePixbuf(ctx, _original, 0, 0);
					ctx.Paint();

					rotated = GetPixbufFromImageSurface(surface, surface.Width, surface.Height);

					surface.Dispose();
					ctx.GetTarget().Dispose();
					ctx.Dispose();
				}
				else
				{
					rotated = _original;
				}

				Pixbuf scaled = ApplyScale(rotated);
				_image.Pixbuf = scaled;

				// gtk_image_set_from_pixbuf took its own reference, so every pixbuf produced above
				// can be released right away. _original is the one thing here we do not own.
				if (!ReferenceEquals(scaled, rotated))
					scaled.Dispose();

				if (!ReferenceEquals(rotated, _original))
					rotated.Dispose();
			}
			QueueDraw();
		}

		Pixbuf ApplyScale(Pixbuf source)
		{
			// The factor an axis really gets is Scale * ScaleX (resp. Scale * ScaleY) - the three
			// Forms properties multiply. This used to scale both axes by Scale alone, so ScaleX and
			// ScaleY looked like no-ops, and it guarded on "_scaleX != 0.0 || _scaleY != 0.0", which
			// is the inverse of the case that actually needs care.
			double factorX = _scale * _scaleX;
			double factorY = _scale * _scaleY;

			// gdk_pixbuf_scale_simple only takes a positive destination extent, so the sign and the
			// magnitude of a factor have to be applied separately: a negative factor is a mirror
			// about that axis (Flip), and a zero factor - Scale="0", the "collapse it" value - is
			// clamped to a single pixel rather than handed over as a 0-wide destination.
			Pixbuf flipped = null;

			if (factorX < 0.0)
				flipped = source.Flip(true);

			if (factorY < 0.0)
			{
				Pixbuf horizontal = flipped;
				flipped = (horizontal ?? source).Flip(false);
				horizontal?.Dispose();
			}

			Pixbuf current = flipped ?? source;

			int width = Math.Max(1, (int)Math.Round(current.Width * Math.Abs(factorX)));
			int height = Math.Max(1, (int)Math.Round(current.Height * Math.Abs(factorY)));

			if (width == current.Width && height == current.Height)
				return current;

			Pixbuf result = current.ScaleSimple(width, height, InterpType.Bilinear);
			flipped?.Dispose();

			return result;
		}

		private Pixbuf GetPixbufFromImageSurface(ImageSurface surface, int width, int height)
		{
			/*
			 *  This is a simplified implementation of gdk_pixbuf_get_from_surface()
			 *  which is not supported by gtk-sharp.
			 * 
			 *  See https://code.woboq.org/gtk/gtk/gdk/gdkpixbuf-drawable.c.html
			 *  for original implementation.
			 */
			Pixbuf dest = new Pixbuf(Gdk.Colorspace.Rgb, true, 8, width, height);
			ConvertAlpha(dest.Pixels, dest.Rowstride, surface.Data, surface.Stride, 0, 0, width, height);
			return dest;
		}

		private void ConvertAlpha(IntPtr destData, int destStride, byte[] srcData, int srcStride, int srcX, int srcY, int width, int height)
		{
			int srcDataIndex = srcStride * srcY + srcX * 4;

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte a = srcData[srcDataIndex + (x * 4) + 3];

					if (a == 0)
					{
						System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(destData, x * 4 + 0), 0);
						System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(destData, x * 4 + 1), 0);
						System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(destData, x * 4 + 2), 0);
					}
					else
					{
						byte b = (byte)((srcData[srcDataIndex + (x * 4) + 0] * 255 + a / 2) / a);
						byte g = (byte)((srcData[srcDataIndex + (x * 4) + 1] * 255 + a / 2) / a);
						byte r = (byte)((srcData[srcDataIndex + (x * 4) + 2] * 255 + a / 2) / a);

						System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(destData, x * 4 + 0), r);
						System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(destData, x * 4 + 1), g);
						System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(destData, x * 4 + 2), b);
					}
					System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(destData, x * 4 + 3), a);
				}

				srcDataIndex += srcStride;
				destData = IntPtr.Add(destData, destStride);
			}
		}
	}

	public enum ImageAspect
	{
		AspectFit,
		AspectFill,
		Fill
	}
}

