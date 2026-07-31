using Xunit;

// xUnit parallelises test COLLECTIONS by default; NUnit did not.
// These tests mutate process-global state - Device.PlatformServices, Device.Info,
// Application.Current, the Registrar - from BaseTestFixture and from individual fixtures,
// so running collections concurrently makes them corrupt each other non-deterministically.
// Disabling parallelisation preserves the NUnit execution semantics the suite was written for.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
