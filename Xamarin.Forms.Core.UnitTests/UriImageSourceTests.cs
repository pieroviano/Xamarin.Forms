using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Threading.Tasks;
using IOPath = System.IO.Path;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class UriImageSourceTests : BaseTestFixture
	{
		IsolatedStorageFile NativeStore { get; set; }

		public UriImageSourceTests()
		{
			Device.PlatformServices = new MockPlatformServices(getStreamAsync: GetStreamAsync);
			NativeStore = IsolatedStorageFile.GetUserStoreForAssembly();
			networkcalls = 0;
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
			string cacheName = "ImageLoaderCache";
			if (NativeStore.DirectoryExists(cacheName))
			{
				foreach (var f in NativeStore.GetFileNames(cacheName + "/*"))
					NativeStore.DeleteFile(IOPath.Combine(cacheName, f));
			}
			NativeStore.Dispose();
			NativeStore = null;
		}

		static Random rnd = new Random();
		static int networkcalls = 0;
		static async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
		{
			await Task.Delay(rnd.Next(30, 2000));
			if (cancellationToken.IsCancellationRequested)
				throw new TaskCanceledException();
			networkcalls++;
			return typeof(UriImageSourceTests).Assembly.GetManifestResourceStream(uri.LocalPath.Substring(1));
		}

		[Fact(Skip = "LoadImageFromStream")]
		public void LoadImageFromStream()
		{
			var loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/Images/crimson.jpg"),
			};
			Stream s0 = loader.GetStreamAsync().Result;

			Assert.Equal(79109, s0.Length);
		}

		[Fact(Skip = "SecondCallLoadFromCache")]
		public void SecondCallLoadFromCache()
		{
			var loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/Images/crimson.jpg"),
			};
			Assert.Equal(0, networkcalls);

			using (var s0 = loader.GetStreamAsync().Result)
			{
				Assert.Equal(79109, s0.Length);
				Assert.Equal(1, networkcalls);
			}

			using (var s1 = loader.GetStreamAsync().Result)
			{
				Assert.Equal(79109, s1.Length);
				Assert.Equal(1, networkcalls);
			}
		}

		[Fact(Skip = "DoNotKeepFailedRetrieveInCache")]
		public void DoNotKeepFailedRetrieveInCache()
		{
			var loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/missing.png"),
			};
			Assert.Equal(0, networkcalls);

			var s0 = loader.GetStreamAsync().Result;
			Assert.Null(s0);
			Assert.Equal(1, networkcalls);

			var s1 = loader.GetStreamAsync().Result;
			Assert.Null(s1);
			Assert.Equal(2, networkcalls);
		}

		[Fact(Skip = "ConcurrentCallsOnSameUriAreQueued")]
		public void ConcurrentCallsOnSameUriAreQueued()
		{
			var loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/Images/crimson.jpg"),
			};
			Assert.Equal(0, networkcalls);

			var t0 = loader.GetStreamAsync();
			var t1 = loader.GetStreamAsync();

			//var s0 = t0.Result;
			using (var s1 = t1.Result)
			{
				Assert.Equal(1, networkcalls);
				Assert.Equal(79109, s1.Length);
			}
		}

		[Fact]
		public void NullUriDoesNotCrash()
		{
			var loader = new UriImageSource();
			AssertEx.DoesNotThrow(() =>
			{
				loader.Uri = null;
			});
		}

		[Fact]
		public void UrlHashKeyAreTheSame()
		{
			var urlHash1 = Device.PlatformServices.GetHash("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasdasdasdasd");
			var urlHash2 = Device.PlatformServices.GetHash("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasdasdasdasd");
			Assert.True(urlHash1 == urlHash2);
		}

		[Fact]
		public void UrlHashKeyAreNotTheSame()
		{
			var urlHash1 = Device.PlatformServices.GetHash("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasdasdasdasd");
			var urlHash2 = Device.PlatformServices.GetHash("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasda");
			Assert.True(urlHash1 != urlHash2);
		}

	}
}
