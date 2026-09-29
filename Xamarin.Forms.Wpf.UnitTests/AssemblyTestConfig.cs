using Xamarin.Forms.Xaml;
using Xunit;

// GTK is not thread-safe, and Forms.Init, Application.Current and the facade's own statics are process-global:
// one test at a time, on one worker thread, as in the other suites.
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]

// The fixtures' XAML is compiled by XamlC unless a fixture opts out, which one does on purpose: both paths are tested.
[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
