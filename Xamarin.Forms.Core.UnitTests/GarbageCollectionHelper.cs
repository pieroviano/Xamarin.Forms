using System;

namespace Xamarin.Forms.Core.UnitTests
{
	static class GarbageCollectionHelper
	{
		// A WeakReference is only cleared once nothing on any live stack frame still points at
		// its target. Under the Debug/tier-0 JIT every local and spilled temp is reported live
		// for the whole method, so the graph under test has to be built - and the last strong
		// reference to it dropped - inside a separate [MethodImpl(NoInlining)] frame that has
		// already returned by the time this runs. Nulling a local in the test method itself is
		// not enough: the temp the JIT spilled it into is still a root.
		// Two passes, because an object that was finalized during the first one is not actually
		// reclaimed until the collection that follows its finalizer.
		internal static void Collect()
		{
			for (int i = 0; i < 2; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}
	}
}
