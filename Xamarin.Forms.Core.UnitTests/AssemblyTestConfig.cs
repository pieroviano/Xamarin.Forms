using Xunit;

// xUnit parallelises test COLLECTIONS by default; NUnit did not.
// These tests mutate process-global state - Device.PlatformServices, Device.Info,
// Application.Current, the Registrar, MessagingCenter - from BaseTestFixture and from
// individual fixtures, so running collections concurrently makes them corrupt each other
// non-deterministically. Disabling parallelisation preserves the NUnit execution semantics
// the suite was written for, and matches what Xamarin.Forms.Xaml.UnitTests and
// Xamarin.Forms.Platform.GTK.UnitTests already do.
//
// MEASURED, and the reason this is not merely a tidiness fix: without it the assembly does not
// just fail, it never finishes. `Xamarin.Forms.Core.UnitTests.exe` with default parallelism
// emits 3000+ lines of cascading failures and then hangs indefinitely - which is what made
// `dotnet test` on this project appear to stall forever. The clearest single culprit is
// DeviceUnitTests.InvokeOnMainThreadThrowsWhenNull, which sets Device.PlatformServices to null
// deliberately; every other collection running at that moment then throws
// "You must call Xamarin.Forms.Forms.Init(); prior to using this property", and the async
// Device.InvokeOnMainThreadAsync tests deadlock against MockPlatformServices being swapped
// mid-flight.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
