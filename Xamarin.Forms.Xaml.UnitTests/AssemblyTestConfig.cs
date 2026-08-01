using System;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Xaml.UnitTests;
using Xunit;
using Xunit.v3;

// xUnit parallelises test COLLECTIONS by default; NUnit did not.
// These tests mutate process-global state - Device.PlatformServices, Device.Info,
// Application.Current, the Registrar - from BaseTestFixture and from individual fixtures,
// so running collections concurrently makes them corrupt each other non-deterministically.
// Disabling parallelisation preserves the NUnit execution semantics the suite was written for.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Applied to the whole assembly: xUnit runs Before() ahead of every test method in it.
[assembly: EnsurePlatformServices]

namespace Xamarin.Forms.Xaml.UnitTests
{
	/// <summary>
	/// Guarantees every test runs with a usable <see cref="Device.PlatformServices"/>, whatever
	/// order the tests happen to execute in.
	/// </summary>
	/// <remarks>
	/// Inherited straight from the NUnit suite: of the nested test classes here, only some derive
	/// from <c>BaseTestFixture</c> (or hand-roll the same setup); 181 declare no base type at all
	/// and 58 only implement <c>IDisposable</c>. Those never set
	/// <c>Device.PlatformServices</c> - they passed solely because an earlier class had left the
	/// global set, and every one of the ~179 classes that *does* set it puts it back to
	/// <c>null</c> on teardown. The outcome was therefore decided by which class ran first, and
	/// the suite produced 0-2 failures per run with
	/// <c>InvalidOperationException : You must call Xamarin.Forms.Forms.Init(); prior to using
	/// this property</c> - never the same set twice.
	///
	/// Measured, before this attribute existed:
	/// <c>dotnet test --filter "FullyQualifiedName~XArray"</c> -> 6 tests, 3 passed, 3 failed.
	/// Those same three pass inside a full-suite run. That is the order dependence, isolated.
	///
	/// The hook runs AFTER the test class constructor and BEFORE the test method, so a class that
	/// does its own setup keeps the instance it created; only a class that would otherwise have
	/// none gets one. That is why this restores rather than overwrites, and why the fix does not
	/// need to touch the 179 teardowns that null the property.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	public sealed class EnsurePlatformServicesAttribute : BeforeAfterTestAttribute
	{
		public override void Before(MethodInfo methodUnderTest, IXunitTest test)
		{
			// Device.PlatformServices has no "is it set" probe - its getter is what throws - so
			// the exception is the probe. Deliberately not an unconditional assignment: that
			// would discard the instance a fixture constructor had just installed.
			try
			{
				_ = Device.PlatformServices;
			}
			catch (InvalidOperationException)
			{
				Device.PlatformServices = new MockPlatformServices();
			}
		}
	}
}
