using System;
using Cairo;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class EllipseView : ShapeView
	{
		protected override void Draw(Gdk.Rectangle area, Context cr)
		{
			double width = _width;
			double height = _height;

			if (width <= 0 || height <= 0)
				return;

			// Save/Restore around the transform. Without it the translate+scale leaked out of this
			// method: GtkFormsContainer.OnDrawn calls Draw(area, cr) and then passes the SAME Cairo
			// context to base.OnDrawn, so every child of an Ellipse was drawn offset by (w/2, h/2)
			// and scaled by (w/2, h/2). The scale also multiplied the stroke width, so the outline
			// thickness varied with the ellipse's size.
			cr.Save();

			try
			{
				cr.Translate(width / 2, height / 2);
				cr.Scale(width / 2, height / 2);
				cr.Arc(0, 0, 1, 0, 2 * Math.PI);

				// Undo the scale before stroking so the stroke is in device units, then hand the
				// path - which keeps its own coordinates - to the base implementation.
				cr.IdentityMatrix();

				base.Draw(area, cr);
			}
			finally
			{
				// base.Draw uses FillPreserve/StrokePreserve, which deliberately keep the path so
				// it can be both filled and stroked. Clear it here: Restore() does not, and
				// GtkFormsContainer.OnDrawn hands this same context to the children afterwards.
				cr.NewPath();
				cr.Restore();
			}
		}
	}
}
