using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class IndicatorViewRenderer : ViewRenderer<IndicatorView, IndicatorViewControl>
	{
		protected override void OnElementChanged(ElementChangedEventArgs<IndicatorView> e)
		{
			if (e.NewElement != null)
			{
				if (Control == null)
					SetNativeControl(new IndicatorViewControl());

				UpdateIndicators();
				UpdateColors();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == IndicatorView.CountProperty.PropertyName ||
				e.PropertyName == IndicatorView.PositionProperty.PropertyName ||
				e.PropertyName == IndicatorView.IndicatorSizeProperty.PropertyName ||
				e.PropertyName == IndicatorView.IndicatorsShapeProperty.PropertyName ||
				e.PropertyName == IndicatorView.MaximumVisibleProperty.PropertyName ||
				e.PropertyName == IndicatorView.HideSingleProperty.PropertyName ||
				e.PropertyName == IndicatorView.ItemsSourceProperty.PropertyName)
				UpdateIndicators();
			else if (e.PropertyName == IndicatorView.IndicatorColorProperty.PropertyName ||
				e.PropertyName == IndicatorView.SelectedIndicatorColorProperty.PropertyName)
				UpdateColors();
		}

		void UpdateIndicators()
		{
			var count = Element.Count;

			// MaximumVisible caps how many dots are drawn; HideSingle suppresses the control
			// entirely when there is nothing to choose between.
			if (Element.MaximumVisible < count)
				count = Element.MaximumVisible;

			if (count == 1 && Element.HideSingle)
				count = 0;

			Control.Update(
				count,
				Element.Position,
				Element.IndicatorSize,
				Element.IndicatorsShape == IndicatorShape.Square);
		}

		void UpdateColors()
		{
			Control.UpdateColors(
				Element.IndicatorColor == Color.Default ? (Gdk.Color?)null : Element.IndicatorColor.ToGtkColor(),
				Element.SelectedIndicatorColor == Color.Default ? (Gdk.Color?)null : Element.SelectedIndicatorColor.ToGtkColor());
		}
	}
}
