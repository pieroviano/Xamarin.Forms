using Gtk;
using Xamarin.Forms.Platform.GTK.Packagers;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class LayoutRenderer : ViewRenderer<Layout, Fixed>
	{
		private Fixed _fixed;
		private LayoutElementPackager _packager;

		protected override void OnElementChanged(ElementChangedEventArgs<Layout> e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.LayoutChanged -= LayoutChanged;
			}

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					if (_fixed == null)
					{
						// Use a Gtk.Fixed, a container which you to position widgets at fixed coordinates.
						// This allows apply transformations.
						_fixed = new Fixed();
					}

					SetNativeControl(_fixed);
				}

				e.NewElement.LayoutChanged += LayoutChanged;

				if (_packager == null)
				{
					_packager = new LayoutElementPackager(this);
				}

				_packager.Load();
			}

			base.OnElementChanged(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Element != null)
				{
					Element.LayoutChanged -= LayoutChanged;
				}
			}

			base.Dispose(disposing);
		}

		private bool _resizeQueued;

		/// <summary>
		/// Re-queues a GTK resize when Forms has re-laid this layout out.
		/// </summary>
		/// <remarks>
		/// Deferred to idle, and this one is not a nicety. Forms raises <c>LayoutChanged</c> from
		/// inside its layout pass, and <c>AbstractPageRenderer.SetPageSize</c> runs that whole pass
		/// SYNCHRONOUSLY from inside <c>OnSizeAllocated</c> - so calling QueueResize() here called
		/// it from inside GTK's size-allocate cycle.
		///
		/// GTK3 does not merely drop such a resize. gtk_widget_queue_resize_internal sets
		/// resize_needed on this widget and every ancestor and then, because the toplevel is already
		/// mid-allocation, schedules nothing; the flags are never cleared, because clearing happens
		/// in gtk_widget_size_allocate, which will not run. Every later queue_resize from anywhere
		/// beneath then hits the early-out on the first ancestor that already carries the flag and
		/// is swallowed too. That is the "resize wedge" the plan records - the flyout's animated
		/// width never reaching its allocation, and this defect: the ControlGallery's detail content
		/// pinned at its natural 257x230 with the request correctly set to (500,528), so the
		/// AbsoluteLayout's proportional children were painted at natural size and overlapped.
		///
		/// Measured (scratchpad/m3-timeline.log): request (500,528) at t=336ms, no allocation ever
		/// followed, and at t=3.5s a bare QueueResize() STILL did not move it - only a forced
		/// SizeAllocate on the toplevel did, which is the signature of the flags being stuck rather
		/// than of a single lost resize.
		/// </remarks>
		private void LayoutChanged(object sender, System.EventArgs e)
		{
			if (_resizeQueued)
				return;

			_resizeQueued = true;

			GLib.Idle.Add(() =>
			{
				_resizeQueued = false;
				QueueResize();

				return false;
			});
		}
	}
}
