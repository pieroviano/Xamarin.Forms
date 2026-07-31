using Cairo;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class LineView : ShapeView
	{
		double _x1, _y1, _x2, _y2;

		protected override void Draw(Gdk.Rectangle area, Context cr)
		{
			// A line has no interior, so only the path is built here; ShapeView.Draw applies the
			// stroke (and would apply a fill, which is a no-op for a degenerate path).
			cr.MoveTo(_x1, _y1);
			cr.LineTo(_x2, _y2);

			base.Draw(area, cr);
		}

		public void UpdateLine(double x1, double y1, double x2, double y2)
		{
			_x1 = x1;
			_y1 = y1;
			_x2 = x2;
			_y2 = y2;

			QueueDraw();
		}
	}
}
