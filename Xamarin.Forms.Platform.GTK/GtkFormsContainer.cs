using System;
using Cairo;
using Gdk;
using Gtk;

namespace Xamarin.Forms.Platform.GTK
{
	/// <summary>
	/// A generic container to embed visual elements.
	/// </summary>
	public class GtkFormsContainer : Gtk.EventBox
	{
		Color _backgroundColor;

		public GtkFormsContainer()
		{
			VisibleWindow = false;
			BackgroundColor = Color.Transparent;
		}

		Color BackgroundColor
		{
			get => _backgroundColor;
			set
			{
				_backgroundColor = value;
				QueueDraw();
			}
		}

		public void SetBackgroundColor(Color color)
		{
			BackgroundColor = color;
		}

		/// <summary>
		/// Subclasses can override this method to draw custom content over the background.
		/// </summary>
		/// <param name="clipArea">The clipped area that needs a redraw.</param>
		/// <param name="cr">Context.</param>
		protected virtual void Draw(Gdk.Rectangle clipArea, Context cr)
		{
		}

		// GTK3 replaced the expose-event model with "draw": the Cairo context arrives
		// already translated to the widget's top-left and already clipped to the damage
		// region, so the manual Clip()/Translate() arithmetic GTK2 required is gone.
		protected override bool OnDrawn(Context cr)
		{
			var area = new Gdk.Rectangle(0, 0, AllocatedWidth, AllocatedHeight);

			// Draw first the background with the color defined in BackgroundColor.
			//
			// Clipped to this widget's allocation, and that is not optional. cairo_paint() fills the
			// WHOLE current clip region, and for a VisibleWindow = false EventBox - which is what
			// every one of these containers is - the context arriving here is clipped to the damage
			// region of the SHARED parent GdkWindow, not to this widget. An unclipped Paint()
			// therefore painted this element's background straight over its siblings: measured with
			// three AbsoluteLayout children stacked 72..257 / 257..415 / 415..600, the middle one's
			// green filled a solid band from y=80 to y=414, hiding the list above it entirely
			// (scratchpad/m3-overlap.sh, "nothing green is painted above the middle block's top
			// edge"). It only ever showed on a layout with an explicit BackgroundColor, because the
			// default here is Color.Transparent and painting that is a no-op.
			cr.Save();
			cr.Rectangle(0, 0, AllocatedWidth, AllocatedHeight);
			cr.Clip();
			cr.SetSourceRGBA(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, BackgroundColor.A);
			cr.Operator = Operator.Over;
			cr.Paint();
			cr.Restore();

			// Let subclasses perform their own drawing operations
			Draw(area, cr);

			// And finally forward the draw to the children
			return base.OnDrawn(cr);
		}
	}
}
