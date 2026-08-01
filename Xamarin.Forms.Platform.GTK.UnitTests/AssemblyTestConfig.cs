using Xunit;

// GTK is not thread-safe, and Forms.Init / Registrar / Application.Current are process-global.
// xUnit parallelises test COLLECTIONS by default; running two of these concurrently would have
// two threads inside the same GTK main context. Disabling parallelisation, and pinning the
// runner to a single worker thread, keeps the whole assembly on one thread at a time - the same
// execution semantics Xamarin.Forms.Xaml.UnitTests settles on, for the same reason.
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]
