using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Coverage target 5 of plan §10.2. <c>GtkPlatformServices</c> is internal, so everything here
	/// goes through the public <c>Device</c> surface - which is also how application code reaches
	/// it, so a break here is a break users would actually see.
	/// </summary>
	public class PlatformServiceTests : GtkTestBase
	{
		[Fact]
		public void RuntimePlatformIsGtk()
		{
			Assert.Equal(Device.GTK, Device.RuntimePlatform);
		}

		[Fact]
		public void IdiomIsDesktop()
		{
			Assert.Equal(TargetIdiom.Desktop, Device.Idiom);
		}

		/// <summary>
		/// M4 replaced a hardcoded 800x600 stub with the real screen. The stub is what let the
		/// FlyoutPage split-mode bug (M3 root cause 6) hide: Core and the native widget disagreed
		/// about split mode only once a real screen size arrived.
		/// </summary>
		[Fact]
		public void DeviceInfoReportsARealScreen()
		{
			var scaled = Device.Info.ScaledScreenSize;
			var pixels = Device.Info.PixelScreenSize;

			Assert.True(scaled.Width > 0, $"scaled width {scaled.Width}");
			Assert.True(scaled.Height > 0, $"scaled height {scaled.Height}");
			Assert.True(pixels.Width > 0, $"pixel width {pixels.Width}");
			Assert.True(pixels.Height > 0, $"pixel height {pixels.Height}");
			Assert.True(Device.Info.ScalingFactor > 0, "scaling factor");

			// Whatever Xvfb was started at, the screen has to be self-consistent:
			// pixels = scaled * scale. The old stub could not satisfy this.
			Assert.Equal(scaled.Width * Device.Info.ScalingFactor, pixels.Width, 0);
			Assert.Equal(scaled.Height * Device.Info.ScalingFactor, pixels.Height, 0);
		}

		[Fact]
		public void NamedSizesAreOrdered()
		{
			var micro = Device.GetNamedSize(NamedSize.Micro, typeof(Label));
			var small = Device.GetNamedSize(NamedSize.Small, typeof(Label));
			var medium = Device.GetNamedSize(NamedSize.Medium, typeof(Label));
			var large = Device.GetNamedSize(NamedSize.Large, typeof(Label));

			Assert.True(micro > 0, $"micro {micro}");
			Assert.True(small > micro, $"small {small} <= micro {micro}");
			Assert.True(medium > small, $"medium {medium} <= small {small}");
			Assert.True(large > medium, $"large {large} <= medium {medium}");
		}

		[Fact]
		public void RequestedThemeIsAnswered()
		{
			var theme = Application.Current?.RequestedTheme ?? OSAppTheme.Unspecified;

			Assert.True(
				theme == OSAppTheme.Unspecified || theme == OSAppTheme.Light || theme == OSAppTheme.Dark,
				$"unexpected theme {theme}");
		}

		/// <summary>
		/// Isolated storage on Linux, verified as a round-trip rather than by "it did not throw" -
		/// the plan records this being checked at runtime for M4 (§7.5).
		/// </summary>
		[Fact]
		public void IsolatedStorageRoundTrips()
		{
			var store = Device.PlatformServices.GetUserStoreForApplication();
			var name = $"gtk-unit-tests-{Guid.NewGuid():N}.txt";
			const string Payload = "round trip";

			// Deliberately not an async test method - see GtkTestHost.Await.
			using (var stream = GtkTestHost.Await(store.OpenFileAsync(name, FileMode.Create, FileAccess.Write)))
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(Payload);
			}

			Assert.True(GtkTestHost.Await(store.GetFileExistsAsync(name)), "the file was not created");

			using (var stream = GtkTestHost.Await(store.OpenFileAsync(name, FileMode.Open, FileAccess.Read)))
			using (var reader = new StreamReader(stream))
			{
				Assert.Equal(Payload, reader.ReadToEnd());
			}
		}

		/// <summary>
		/// The ticker is what drives every Forms animation, including the flyout slide. A ticker
		/// that never fires makes animations silently instant.
		/// </summary>
		[Fact]
		public void TickerFires()
		{
			var fired = false;
			var deadline = DateTime.UtcNow.AddSeconds(5);

			Device.StartTimer(TimeSpan.FromMilliseconds(20), () =>
			{
				fired = true;
				return false;
			});

			while (!fired && DateTime.UtcNow < deadline)
				GtkTestHost.Pump(null, 1);

			Assert.True(fired, "Device.StartTimer never fired within 5s");
		}

		/// <summary>
		/// M4 changed <c>IsInvokeRequired</c> from a <c>Thread.IsBackground</c> guess to a
		/// comparison against the thread that ran <c>Forms.Init</c>. Either way the action has to
		/// actually run - inline if no invoke is required, off the GTK idle queue if one is.
		/// </summary>
		[Fact]
		public void BeginInvokeOnMainThreadRunsTheAction()
		{
			var ran = false;

			Device.BeginInvokeOnMainThread(() => ran = true);
			GtkTestHost.Pump(null, 3);

			Assert.True(ran, "Device.BeginInvokeOnMainThread never ran the action");
		}

		[Fact]
		public void GetHashIsStable()
		{
			var services = Device.PlatformServices;

			Assert.Equal(services.GetHash("xamarin"), services.GetHash("xamarin"));
			Assert.NotEqual(services.GetHash("xamarin"), services.GetHash("forms"));
		}

		/// <summary>
		/// <c>GtkSerializer</c> is registered through <c>DependencyService</c>; the plan notes it
		/// needed no porting because it is a <c>DataContractSerializer</c> rather than
		/// <c>BinaryFormatter</c> (which .NET 10 removes outright).
		/// </summary>
		[Fact]
		public void SerializerIsResolvable()
		{
			Assert.True(DependencyService.Get<IDeserializer>() != null,
				"no IDeserializer registered for GTK");
		}
	}
}
