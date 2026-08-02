using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using GLib;

namespace Xamarin.Forms.Platform.GTK
{
	public class GtkSynchronizationContext : SynchronizationContext
	{
		public override void Post(SendOrPostCallback d, object state)
		{
			Gtk.Application.Invoke((s, e) =>
			{
				d(state);
			});
		}

		public override void Send(SendOrPostCallback d, object state)
		{
			if (System.Threading.Thread.CurrentThread.ManagedThreadId == FormsWindow.MainThreadID)
			{
				d(state);
			}
			else
			{
				var evt = new ManualResetEvent(false);
				Exception exception = null;

				Gtk.Application.Invoke((s, e) =>
				{
					try
					{
						d(state);
					}
					catch (Exception ex)
					{
						exception = ex;
					}
					finally
					{
						evt.Set();
					}
				});

				evt.WaitOne();

				// ExceptionDispatchInfo rather than `throw exception;`. This is a cross-thread
				// rethrow - the exception was captured on the GTK main loop and is being surfaced
				// on the caller's thread - so a bare `throw;` is not available, and `throw
				// exception;` would overwrite the trace from the delegate that actually failed
				// with one anchored here. Capture().Throw() preserves the original.
				if (exception != null)
					ExceptionDispatchInfo.Capture(exception).Throw();
			}
		}
	}
}
