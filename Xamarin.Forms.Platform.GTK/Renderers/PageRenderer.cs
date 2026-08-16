using Xamarin.Forms.Platform.GTK.Packagers;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class PageRenderer : AbstractPageRenderer<Controls.Page, Page>
	{
		private PageElementPackager _packager;

		/// <remarks>
		/// Only <c>disposing == true</c>: VisualElementPackager.Dispose unsubscribes from the Forms
		/// element's ChildAdded/ChildRemoved, which is another object and must not be touched from
		/// the finalizer thread. It also latches its own _isDisposed, so an unguarded finalizer pass
		/// would silently disarm the real disposal. See the remarks on ViewRenderer.Dispose;
		/// LayoutRenderer.Dispose guards the same packager teardown.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && _packager != null)
			{
				_packager.Dispose();
				_packager = null;
			}
		}

		/// <remarks>
		/// OnMapped, not the Gtk 3 "show" vfunc: Gtk 4 widgets are born visible, so that vfunc never
		/// fires. This is the call that builds a page's child renderers, so under Gtk 4 the page
		/// stayed empty. See the remarks on AbstractPageRenderer.OnMapped.
		/// </remarks>
		protected override void OnMapped()
		{
			base.OnMapped();

			if (_packager == null)
			{
				_packager = new PageElementPackager(this);
			}

			_packager.Load();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			if (!Sensitive)
				return;

			base.OnSizeAllocated(allocation);
		}
	}
}
