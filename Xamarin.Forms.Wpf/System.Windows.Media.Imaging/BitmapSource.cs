using System.IO;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Media.Imaging
{
	public enum BitmapCacheOption
	{
		Default,
		OnDemand,
		OnLoad,
		None,
	}

	public enum BitmapCreateOptions
	{
		None = 0,
		PreservePixelFormat = 1,
		DelayCreation = 2,
		IgnoreColorProfile = 4,
		IgnoreImageCache = 8,
	}

	/// <summary>
	/// A bitmap, kept as its encoded bytes (or the location they come from): Xamarin.Forms decodes an image when it
	/// shows it, so there is nothing to decode here.
	/// </summary>
	public abstract class BitmapSource : ImageSource
	{
		byte[] _bytes;

		public virtual int PixelWidth => Dimensions().Width;

		public virtual int PixelHeight => Dimensions().Height;

		public override double Width => PixelWidth;

		public override double Height => PixelHeight;

		/// <summary>The encoded image, or null when it cannot be found.</summary>
		internal byte[] Bytes => _bytes ?? (_bytes = LoadBytes());

		internal abstract byte[] LoadBytes();

		internal override XF.ImageSource ToForms()
		{
			var bytes = Bytes;
			return bytes == null ? null : XF.ImageSource.FromStream(() => new MemoryStream(bytes));
		}

		(int Width, int Height) Dimensions() => ImageHeader.Size(Bytes);
	}

	public class BitmapImage : BitmapSource, System.ComponentModel.ISupportInitialize
	{
		public BitmapImage()
		{
		}

		public BitmapImage(Uri uriSource) => UriSource = uriSource ?? throw new ArgumentNullException(nameof(uriSource));

		public BitmapImage(Uri uriSource, System.Net.Cache.RequestCachePolicy uriCachePolicy) : this(uriSource)
		{
		}

		public Uri UriSource { get; set; }

		public Uri BaseUri { get; set; }

		public Stream StreamSource { get; set; }

		public BitmapCacheOption CacheOption { get; set; }

		public BitmapCreateOptions CreateOptions { get; set; }

		public int DecodePixelWidth { get; set; }

		public int DecodePixelHeight { get; set; }

		public void BeginInit()
		{
		}

		public void EndInit()
		{
			if (UriSource == null && StreamSource == null)
				throw new InvalidOperationException("Property 'UriSource' or property 'StreamSource' must be set.");
		}

		internal override byte[] LoadBytes()
		{
			if (StreamSource != null)
			{
				if (StreamSource.CanSeek)
					StreamSource.Position = 0;
				using (var copy = new MemoryStream())
				{
					StreamSource.CopyTo(copy);
					return copy.ToArray();
				}
			}

			return UriSource == null ? null : ResourceLocator.Read(UriSource);
		}

		protected override Freezable CreateInstanceCore() =>
			new BitmapImage { UriSource = UriSource, BaseUri = BaseUri, StreamSource = StreamSource };

		public override string ToString() => UriSource?.OriginalString ?? base.ToString();
	}

	public sealed class BitmapFrame : BitmapSource
	{
		readonly Uri _uri;
		readonly byte[] _bytes;

		BitmapFrame(Uri uri, byte[] bytes)
		{
			_uri = uri;
			_bytes = bytes;
		}

		public static BitmapFrame Create(Uri bitmapUri) =>
			new BitmapFrame(bitmapUri ?? throw new ArgumentNullException(nameof(bitmapUri)), null);

		public static BitmapFrame Create(Stream bitmapStream)
		{
			if (bitmapStream == null)
				throw new ArgumentNullException(nameof(bitmapStream));

			using (var copy = new MemoryStream())
			{
				bitmapStream.CopyTo(copy);
				return new BitmapFrame(null, copy.ToArray());
			}
		}

		public static BitmapFrame Create(BitmapSource source) =>
			new BitmapFrame(null, (source ?? throw new ArgumentNullException(nameof(source))).Bytes);

		internal override byte[] LoadBytes() => _bytes ?? ResourceLocator.Read(_uri);

		protected override Freezable CreateInstanceCore() => new BitmapFrame(_uri, _bytes);

		public override string ToString() => _uri?.OriginalString ?? base.ToString();
	}
}
