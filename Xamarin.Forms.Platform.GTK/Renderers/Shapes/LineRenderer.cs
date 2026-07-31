using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Shapes;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class LineRenderer : ShapeRenderer<Line, LineView>
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Line> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
				UpdateLine();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.OnElementPropertyChanged(sender, args);

			if (args.PropertyName == Line.X1Property.PropertyName ||
				args.PropertyName == Line.Y1Property.PropertyName ||
				args.PropertyName == Line.X2Property.PropertyName ||
				args.PropertyName == Line.Y2Property.PropertyName)
				UpdateLine();
		}

		void UpdateLine()
		{
			Control.UpdateLine(Element.X1, Element.Y1, Element.X2, Element.Y2);
		}
	}
}
