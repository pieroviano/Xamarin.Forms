using Gtk;

namespace Xamarin.Forms.Platform.GTK
{
	internal class PlatformRenderer : GtkFormsContainer
	{
		public PlatformRenderer(Platform platform)
		{
			Platform = platform;
		}

		public Platform Platform { get; set; }

		/// <summary>
		/// Reports no size request at all, so that the window sizes itself rather than being sized
		/// by the page inside it.
		/// </summary>
		/// <remarks>
		/// <para>This is the root of the Forms tree - <c>FormsWindow</c> adds it as the window's only
		/// child - and Forms does its own layout: it measures and positions every element itself and
		/// pushes the result down. A Forms page therefore has no "natural size" to offer GTK, and
		/// offering one inverts the direction layout flows in.</para>
		/// <para>MEASURED, and the reason this override exists: Gtk 4 sizes a toplevel to
		/// <c>max(default size, child minimum)</c>, so whatever this returns overrides
		/// <c>SetDefaultSize(800, 600)</c>. Inherited from <see cref="GtkFormsContainer"/>, the
		/// answer was the union of the children's own requests - and that formed a feedback loop:
		/// the window grew to the reported height, <c>FormsWindow.OnSizeAllocated</c> reported the
		/// new height back to Forms via <c>SetElementSize</c>, Forms re-laid-out at that height, and
		/// the next measure came back one toolbar taller again. The climb was visible in exactly
		/// those steps - 1024, 1096, 1168, ... at 72px a pass, <c>GtkToolbarConstants.ToolbarHeight</c>
		/// - and ran away to a 26292px-tall window on a 600px screen.</para>
		/// <para>Returning zero does not discard a size Forms actually asked for: gtk_widget_measure
		/// floors its result at the widget's own width-request/height-request, so an explicit
		/// <c>SetSizeRequest</c> still wins. Zero means "nothing of my own", which is the truth here,
		/// and it restores the Gtk 3 behaviour where the default size decided the window and the
		/// configure-event then told Forms what it had been given.</para>
		/// </remarks>
		protected override void OnMeasure(Orientation orientation, int forSize,
		                                  out int minimum, out int natural,
		                                  out int minimumBaseline, out int naturalBaseline)
		{
			minimum = 0;
			natural = 0;
			minimumBaseline = naturalBaseline = -1;
		}
	}
}
