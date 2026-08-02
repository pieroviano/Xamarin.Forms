using System;
using System.Globalization;
using System.Threading;

namespace Xamarin.Forms.Core.UnitTests
{
	/// <summary>
	/// Base fixture for both Xamarin.Forms.Core.UnitTests and Xamarin.Forms.Xaml.UnitTests, which
	/// links this file rather than keeping a copy.
	/// </summary>
	/// <remarks>
	/// NUnit's [SetUp]/[TearDown] become the constructor and <see cref="Dispose"/>: xUnit creates
	/// one instance per test, so the constructor runs before every test and Dispose after it.
	/// Ordering is preserved - base constructor before derived, derived Dispose before base -
	/// provided overrides call <c>base.Dispose()</c> last.
	///
	/// Its job is to restore PROCESS-GLOBAL state between tests. These suites mutate
	/// <c>Device.PlatformServices</c>, <c>Application.Current</c> and the <c>Registrar</c>, none of
	/// which xUnit isolates for you, so anything added here must be undone here - a global left set
	/// makes the suite order-dependent, and xUnit's method ordering differs between platforms, so
	/// the resulting failure typically shows up on one OS only.
	/// </remarks>
	public class BaseTestFixture : IDisposable
	{
		readonly CultureInfo _defaultCulture;
		readonly CultureInfo _defaultUICulture;

		public BaseTestFixture()
		{
			_defaultCulture = Thread.CurrentThread.CurrentCulture;
			_defaultUICulture = Thread.CurrentThread.CurrentUICulture;
			Device.PlatformServices = new MockPlatformServices();
		}

		public virtual void Dispose()
		{
			Device.PlatformServices = null;

			// Application.Current is process-global and was the one such global this teardown did
			// not restore, which made the suite order-dependent. MEASURED on Linux:
			// LoaderTests.StaticResourceLookForApplicationResources installs a MyApp whose
			// Resources contain {"foo", "FOO"} and never clears it, so the later
			// LoaderTests.MissingStaticResourceShouldThrow resolved "foo" through the leaked
			// Application.Current instead of throwing. Deterministic in a full-suite run, passes
			// in isolation, and passed on Windows only because xUnit happened to order those two
			// the other way round.
			Application.Current = null;

			Thread.CurrentThread.CurrentCulture = _defaultCulture;
			Thread.CurrentThread.CurrentUICulture = _defaultUICulture;
		}
	}
}
