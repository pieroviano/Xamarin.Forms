using Xunit;

// One gallery process at a time, and one only.
//
// Every test here launches Xamarin.Forms.ControlGallery.GTK.exe, waits for its toplevel and
// photographs it. Two of those running concurrently would be two GTK applications competing for
// the same desktop: window enumeration is by process name and class, so a second instance makes
// the "which toplevel is mine" question ambiguous, and the layout timing this suite exists to
// measure (plan section 10 - the bistable page height) is exactly the thing a second process
// under the same CPU would perturb.
//
// Same reasoning, and the same declaration, as Xamarin.Forms.Platform.GTK.UnitTests: disable
// collection parallelisation AND pin the runner to one worker thread.
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]
