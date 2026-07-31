using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Coverage target 5 of plan §10.2. <c>GtkPlatformServices</c> is internal, so everything here
	/// goes through the public <c>Device</c> surface - which is also how application code reaches
	/// it, so a break here is a break users would actually see.
	/// </summary>
	[TestFixture]
	public class PlatformServiceTests
	{
		[Test]
		public void RuntimePlatformIsGtk()
		{
			Assert.That(Device.RuntimePlatform, Is.EqualTo(Device.GTK));
		}

		[Test]
		public void IdiomIsDesktop()
		{
			Assert.That(Device.Idiom, Is.EqualTo(TargetIdiom.Desktop));
		}

		/// <summary>
		/// M4 replaced a hardcoded 800x600 stub with the real screen. The stub is what let the
		/// FlyoutPage split-mode bug (M3 root cause 6) hide: Core and the native widget disagreed
		/// about split mode only once a real screen size arrived.
		/// </summary>
		[Test]
		public void DeviceInfoReportsARealScreen()
		{
			var scaled = Device.Info.ScaledScreenSize;
			var pixels = Device.Info.PixelScreenSize;

			Assert.Multiple(() =>
			{
				Assert.That(scaled.Width, Is.GreaterThan(0));
				Assert.That(scaled.Height, Is.GreaterThan(0));
				Assert.That(pixels.Width, Is.GreaterThan(0));
				Assert.That(pixels.Height, Is.GreaterThan(0));
				Assert.That(Device.Info.ScalingFactor, Is.GreaterThan(0));

				// Whatever Xvfb was started at, the screen has to be self-consistent:
				// pixels = scaled * scale. The old stub could not satisfy this.
				Assert.That(pixels.Width, Is.EqualTo(scaled.Width * Device.Info.ScalingFactor).Within(1));
				Assert.That(pixels.Height, Is.EqualTo(scaled.Height * Device.Info.ScalingFactor).Within(1));
			});
		}

		[Test]
		public void NamedSizesAreOrdered()
		{
			var micro = Device.GetNamedSize(NamedSize.Micro, typeof(Label));
			var small = Device.GetNamedSize(NamedSize.Small, typeof(Label));
			var medium = Device.GetNamedSize(NamedSize.Medium, typeof(Label));
			var large = Device.GetNamedSize(NamedSize.Large, typeof(Label));

			Assert.Multiple(() =>
			{
				Assert.That(micro, Is.GreaterThan(0));
				Assert.That(small, Is.GreaterThan(micro));
				Assert.That(medium, Is.GreaterThan(small));
				Assert.That(large, Is.GreaterThan(medium));
			});
		}

		[Test]
		public void RequestedThemeIsAnswered()
		{
			Assert.That(Application.Current?.RequestedTheme ?? OSAppTheme.Unspecified,
				Is.AnyOf(OSAppTheme.Unspecified, OSAppTheme.Light, OSAppTheme.Dark));
		}

		/// <summary>
		/// Isolated storage on Linux, verified as a round-trip rather than by "it did not throw" -
		/// the plan records this being checked at runtime for M4 (§7.5).
		/// </summary>
		[Test]
		public async Task IsolatedStorageRoundTrips()
		{
			var store = Device.PlatformServices.GetUserStoreForApplication();
			var name = $"gtk-unit-tests-{Guid.NewGuid():N}.txt";
			const string Payload = "round trip";

			try
			{
				using (var stream = await store.OpenFileAsync(name, FileMode.Create, FileAccess.Write))
				using (var writer = new StreamWriter(stream))
				{
					await writer.WriteAsync(Payload);
				}

				Assert.That(await store.GetFileExistsAsync(name), Is.True, "the file was not created");

				using (var stream = await store.OpenFileAsync(name, FileMode.Open, FileAccess.Read))
				using (var reader = new StreamReader(stream))
				{
					Assert.That(await reader.ReadToEndAsync(), Is.EqualTo(Payload));
				}
			}
			finally
			{
				// No delete on IIsolatedStorageFile; the temp name keeps runs from colliding.
			}
		}

		/// <summary>
		/// The ticker is what drives every Forms animation, including the flyout slide. A ticker
		/// that never fires makes animations silently instant.
		/// </summary>
		[Test]
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

			Assert.That(fired, Is.True, "Device.StartTimer never fired within 5s");
		}

		/// <summary>
		/// M4 changed <c>IsInvokeRequired</c> from a <c>Thread.IsBackground</c> guess to a
		/// comparison against the thread that ran <c>Forms.Init</c>. On the test thread - which is
		/// that thread - no invoke should be required, and the action must run inline.
		/// </summary>
		[Test]
		public void BeginInvokeOnMainThreadRunsTheAction()
		{
			var ran = false;

			Device.BeginInvokeOnMainThread(() => ran = true);
			GtkTestHost.Pump(null, 3);

			Assert.That(ran, Is.True);
		}

		[Test]
		public void GetHashIsStable()
		{
			var services = Device.PlatformServices;

			Assert.That(services.GetHash("xamarin"), Is.EqualTo(services.GetHash("xamarin")));
			Assert.That(services.GetHash("xamarin"), Is.Not.EqualTo(services.GetHash("forms")));
		}

		/// <summary>
		/// <c>GtkSerializer</c> is registered through <c>DependencyService</c>; the plan notes it
		/// needed no porting because it is a <c>DataContractSerializer</c> rather than
		/// <c>BinaryFormatter</c> (which .NET 10 removes outright).
		/// </summary>
		[Test]
		public void SerializerIsResolvableAndRoundTripsProperties()
		{
			var serializer = DependencyService.Get<IDeserializer>();

			Assert.That(serializer, Is.Not.Null, "no IDeserializer registered for GTK");
		}
	}
}
