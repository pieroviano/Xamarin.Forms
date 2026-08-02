using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class ImageTests : BaseTestFixture
	{
		public ImageTests()
		{
			Device.PlatformServices = new MockPlatformServices(getStreamAsync: GetStreamAsync);
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void TestSizing()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png"), IsPlatformEnabled = true };

			var result = image.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);

			Assert.Equal(100, result.Request.Width);
			Assert.Equal(20, result.Request.Height);
		}

		[Fact]
		public void TestAspectSizingWithConstrainedHeight()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png"), IsPlatformEnabled = true };

			var result = image.GetSizeRequest(double.PositiveInfinity, 10);

			Assert.Equal(50, result.Request.Width);
			Assert.Equal(10, result.Request.Height);
		}

		[Fact]
		public void TestAspectSizingWithConstrainedWidth()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png"), IsPlatformEnabled = true };

			var result = image.GetSizeRequest(25, double.PositiveInfinity);

			Assert.Equal(25, result.Request.Width);
			Assert.Equal(5, result.Request.Height);
		}

		[Fact]
		public void TestAspectFillSizingWithConstrainedHeight()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png"), IsPlatformEnabled = true };

			image.Aspect = Aspect.AspectFill;
			var result = image.GetSizeRequest(double.PositiveInfinity, 10);

			Assert.Equal(50, result.Request.Width);
			Assert.Equal(10, result.Request.Height);
		}

		[Fact]
		public void TestAspectFillSizingWithConstrainedWidth()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png"), IsPlatformEnabled = true };

			image.Aspect = Aspect.AspectFill;
			var result = image.GetSizeRequest(25, double.PositiveInfinity);

			Assert.Equal(25, result.Request.Width);
			Assert.Equal(5, result.Request.Height);
		}

		[Fact]
		public void TestFillSizingWithConstrainedHeight()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png"), IsPlatformEnabled = true };

			image.Aspect = Aspect.AspectFill;
			var result = image.GetSizeRequest(double.PositiveInfinity, 10);

			Assert.Equal(50, result.Request.Width);
			Assert.Equal(10, result.Request.Height);
		}

		[Fact]
		public void TestFillSizingWithConstrainedWidth()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png"), IsPlatformEnabled = true };

			image.Aspect = Aspect.AspectFill;
			var result = image.GetSizeRequest(25, double.PositiveInfinity);

			Assert.Equal(25, result.Request.Width);
			Assert.Equal(5, result.Request.Height);
		}

		[Fact]
		public void TestSizeChanged()
		{
			var image = new Image { Source = "File0.png" };
			Assert.Equal("File0.png", ((FileImageSource)image.Source).File);

			var preferredSizeChanged = false;
			image.MeasureInvalidated += (sender, args) => preferredSizeChanged = true;

			image.Source = "File1.png";
			Assert.Equal("File1.png", ((FileImageSource)image.Source).File);
			Assert.True(preferredSizeChanged);
		}

		[Fact]
		public void TestSource()
		{
			var image = new Image();

			Assert.Null(image.Source);

			bool signaled = false;
			image.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == "Source")
					signaled = true;
			};

			var source = ImageSource.FromFile("File.png");
			image.Source = source;

			Assert.Equal(source, image.Source);
			Assert.True(signaled);
		}

		[Fact]
		public void TestSourceDoubleSet()
		{
			var image = new Image { Source = ImageSource.FromFile("File.png") };

			bool signaled = false;
			image.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == "Source")
					signaled = true;
			};

			image.Source = image.Source;

			Assert.False(signaled);
		}

		[Fact]
		public void TestFileImageSourceChanged()
		{
			var source = (FileImageSource)ImageSource.FromFile("File.png");

			bool signaled = false;
			source.SourceChanged += (sender, e) =>
			{
				signaled = true;
			};

			source.File = "Other.png";
			Assert.Equal("Other.png", source.File);

			Assert.True(signaled);
		}

		[Fact]
		public void TestFileImageSourcePropertiesChangedTriggerResize()
		{
			var source = new FileImageSource();
			var image = new Image { Source = source };
			bool fired = false;
			image.MeasureInvalidated += (sender, e) => fired = true;
			Assert.Null(source.File);
			source.File = "foo.png";
			Assert.NotNull(source.File);
			Assert.True(fired);
		}

		[Fact]
		public void TestStreamImageSourcePropertiesChangedTriggerResize()
		{
			var source = new StreamImageSource();
			var image = new Image { Source = source };
			bool fired = false;
			image.MeasureInvalidated += (sender, e) => fired = true;
			Assert.Null(source.Stream);
			source.Stream = token => Task.FromResult<Stream>(null);
			Assert.NotNull(source.Stream);
			Assert.True(fired);
		}

		[Fact]
		public async Task TestImageSourceToNullCancelsLoading()
		{
			var image = new Image();
			var mockImageRenderer = new MockImageRenderer(image);
			var loader = new UriImageSource { Uri = new Uri("http://www.public-domain-image.com/free-images/miscellaneous/big-high-border-fence.jpg") };
			image.Source = loader;
			Assert.True(image.IsLoading);
			image.Source = null;

			// Cancelling unwinds the load asynchronously, so wait for the renderer to finish
			// rather than assuming SetIsLoading(false) has already run on this thread.
			await mockImageRenderer.LoadingCompleted;

			Assert.False(image.IsLoading);
			Assert.True(cancelled);
		}

		static bool cancelled;

		static async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
		{
			try
			{
				await Task.Delay(5000, cancellationToken);
			}
			catch (TaskCanceledException ex)
			{
				cancelled = true;
				throw;
			}

			if (cancellationToken.IsCancellationRequested)
			{
				cancelled = true;
				throw new TaskCanceledException();
			}

			var stream = typeof(ImageTests).Assembly.GetManifestResourceStream(uri.LocalPath.Substring(1));
			return stream;
		}

		class MockImageRenderer
		{
			public MockImageRenderer(Image element)
			{
				Element = element;
				Element.PropertyChanged += (sender, e) =>
				{
					if (e.PropertyName == nameof(Image.Source))
						Load();
				};
			}

			public Image Element { get; set; }

			// Load() was async void, which gave the test no way to observe the end of the load.
			// Handing the task back lets it await instead of racing the continuation.
			public Task LoadingCompleted { get; private set; } = Task.CompletedTask;

			public void Load()
			{
				if (initialLoad && Element.Source != null)
				{
					initialLoad = false;
					LoadingCompleted = LoadAsync();
				}
			}

			async Task LoadAsync()
			{
				Element.SetIsLoading(true);
				await (Element.Source as UriImageSource).GetStreamAsync();
				Element.SetIsLoading(false);
			}

			bool initialLoad = true;
		}
	}
}
