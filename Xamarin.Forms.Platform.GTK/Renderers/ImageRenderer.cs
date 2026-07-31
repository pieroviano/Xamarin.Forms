using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gdk;
using Xamarin.Forms.Platform.GTK.Extensions;
using IOPath = System.IO.Path;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class ImageRenderer : ViewRenderer<Image, Controls.ImageControl>
	{
		bool _isDisposed;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				if (Control != null)
				{
					Control.Dispose();
					Control = null;
				}
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			if (Control == null)
			{
				var image = new Controls.ImageControl();
				SetNativeControl(image);
			}

			if (e.NewElement != null)
			{
				SetImage(e.OldElement);
				SetAspect();
				SetOpacity();
				SetScaleX();
				SetScaleY();
				SetRotation();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Image.SourceProperty.PropertyName)
				SetImage();
			else if (e.PropertyName == Image.IsOpaqueProperty.PropertyName)
				SetOpacity();
			else if (e.PropertyName == Image.AspectProperty.PropertyName)
				SetAspect();
			else if (e.PropertyName == Image.ScaleProperty.PropertyName)
				SetScale();
			else if (e.PropertyName == Image.ScaleXProperty.PropertyName)
				SetScaleX();
			else if (e.PropertyName == Image.ScaleYProperty.PropertyName)
				SetScaleY();
			else if (e.PropertyName == Image.RotationProperty.PropertyName)
				SetRotation();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			Control.SetSizeRequest(allocation.Width, allocation.Height);
		}

		async void SetImage(Image oldElement = null)
		{
			var source = Element.Source;

			if (oldElement != null)
			{
				var oldSource = oldElement.Source;
				if (Equals(oldSource, source))
					return;

				if (oldSource is FileImageSource && source is FileImageSource
					&& ((FileImageSource)oldSource).File == ((FileImageSource)source).File)
					return;

				Control.Pixbuf = null;
			}

			((IImageController)Element).SetIsLoading(true);

			var image = await source.GetNativeImageAsync();

			var imageView = Control;
			if (imageView != null)
				imageView.Pixbuf = image;

			if (!_isDisposed)
			{
				((IVisualElementController)Element).NativeSizeChanged();
				((IImageController)Element).SetIsLoading(false);
			}
		}

		void SetAspect()
		{
			switch (Element.Aspect)
			{
				case Aspect.AspectFit:
					Control.Aspect = Controls.ImageAspect.AspectFit;
					break;
				case Aspect.AspectFill:
					Control.Aspect = Controls.ImageAspect.AspectFill;
					break;
				case Aspect.Fill:
					Control.Aspect = Controls.ImageAspect.Fill;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(Element.Aspect));
			}
		}

		void SetOpacity()
		{
			var opacity = Element.Opacity;

			Control.SetAlpha(opacity);
		}

		void SetScale()
		{
			Control.Scale = Element.Scale;
		}

		void SetScaleX()
		{
			Control.ScaleX = Element.ScaleX;
		}

		void SetScaleY()
		{
			Control.ScaleY = Element.ScaleY;
		}

		void SetRotation()
		{
			Control.Rotation = Element.Rotation;
		}
	}

	public interface IImageSourceHandler : IRegisterable
	{
		Task<Pixbuf> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken =
			default(CancellationToken), float scale = 1);
	}

	public sealed class FileImageSourceHandler : IImageSourceHandler
	{
		public async Task<Pixbuf> LoadImageAsync(
			ImageSource imagesource,
			CancellationToken cancelationToken = default(CancellationToken),
			float scale = 1f)
		{
			Pixbuf image = null;
			var filesource = imagesource as FileImageSource;

			if (filesource != null)
			{
				var file = filesource.File;
				if (!string.IsNullOrEmpty(file))
				{
					var imagePath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, file);

					if (File.Exists(imagePath))
					{
						await Device.InvokeOnMainThreadAsync(() =>
						{
							image = new Pixbuf(imagePath);
						});
					}
				}
			}

			return image;
		}
	}

	public sealed class StreamImagesourceHandler : IImageSourceHandler
	{
		public async Task<Pixbuf> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken = default(CancellationToken), float scale = 1)
		{
			Pixbuf image = null;

			var streamsource = imagesource as StreamImageSource;
			if (streamsource?.Stream == null)
				return null;
			using (var streamImage = await ((IStreamImageSource)streamsource)
				.GetStreamAsync(cancelationToken).ConfigureAwait(false))
			{
				if (streamImage != null)
				{
					await Device.InvokeOnMainThreadAsync(() =>
					{
						image = new Pixbuf(streamImage);
					});
				}
			}

			return image;
		}
	}

	public sealed class UriImageSourceHandler : IImageSourceHandler
	{
		public async Task<Pixbuf> LoadImageAsync(
			ImageSource imagesource,
			CancellationToken cancelationToken = default(CancellationToken),
			float scale = 1)
		{
			Pixbuf image = null;

			var imageLoader = imagesource as UriImageSource;

			if (imageLoader?.Uri == null)
				return null;

			using (Stream streamImage = await imageLoader.GetStreamAsync(cancelationToken))
			{
				if (streamImage == null || !streamImage.CanRead)
				{
					return null;
				}

				await Device.InvokeOnMainThreadAsync(() =>
				{
					image = new Pixbuf(streamImage);
				});
			}

			return image;
		}
	}


	public sealed class FontImageSourceHandler : IImageSourceHandler
	{
		public async Task<Pixbuf> LoadImageAsync(ImageSource imageSource,
			CancellationToken cancellationToken = new CancellationToken(), float scale = 1)
		{
			if (!(imageSource is FontImageSource fontImageSource))
				return null;

			// Ported off System.Drawing (Bitmap/Graphics/PrivateFontCollection): on .NET 7+
			// System.Drawing.Common throws PlatformNotSupportedException on non-Windows.
			// The glyph is now rasterised with Cairo + Pango, which is native to GTK.
			var size = Math.Max(1, (int)fontImageSource.Size);
			Pixbuf pixbuf = null;

			await Device.InvokeOnMainThreadAsync(() =>
			{
				using (var surface = new Cairo.ImageSurface(Cairo.Format.Argb32, size, size))
				using (var cr = new Cairo.Context(surface))
				using (var layout = Pango.CairoHelper.CreateLayout(cr))
				{
					layout.FontDescription = CreateFontDescription(fontImageSource);
					layout.SetText(fontImageSource.Glyph ?? string.Empty);

					var fontColor = fontImageSource.Color != Color.Default
						? fontImageSource.Color
						: Color.White;
					cr.SetSourceRGBA(fontColor.R, fontColor.G, fontColor.B, fontColor.A);

					// Centre the glyph in the square, matching the sizing intent of the
					// previous implementation (glyph drawn at half the requested size).
					layout.GetPixelSize(out var glyphWidth, out var glyphHeight);
					cr.MoveTo((size - glyphWidth) / 2.0, (size - glyphHeight) / 2.0);
					Pango.CairoHelper.ShowLayout(cr, layout);

					surface.Flush();
					pixbuf = new Pixbuf(surface, 0, 0, size, size);
				}
			});

			return pixbuf;
		}

		static Pango.FontDescription CreateFontDescription(FontImageSource fontImageSource)
		{
			// Pango resolves by family name through fontconfig. ToPangoFamily handles the
			// "path/to/file.ttf#Family Name" convention, and - for an icon font shipped with
			// [assembly: ExportFont] - extracts it, registers it with fontconfig and returns the
			// family name that is really inside the file.
			var family = fontImageSource.FontFamily.ToPangoFamily();

			var description = new Pango.FontDescription();

			if (!string.IsNullOrWhiteSpace(family))
				description.Family = family;

			// Preserve the previous half-of-requested-size glyph metric.
			description.Size = (int)(fontImageSource.Size * .5f * Pango.Scale.PangoScale);

			return description;
		}
	}
}

