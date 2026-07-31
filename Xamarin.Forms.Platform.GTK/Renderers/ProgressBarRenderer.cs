using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class ProgressBarRenderer : ViewRenderer<ProgressBar, Gtk.ProgressBar>
	{
		protected override void OnElementChanged(ElementChangedEventArgs<ProgressBar> e)
		{
			if (e.NewElement == null)
				return;

			if (Control == null)
			{
				// Use Gtk.ProgressBar, a widget which indicates progress visually.
				// GTK3 removed ProgressBar.Adjustment; progress is now expressed directly
				// as Fraction (0.0 - 1.0), which is what Element.Progress already is.
				var progressBar = new Gtk.ProgressBar();
				progressBar.Fraction = 0;
				SetNativeControl(progressBar);
			}

			UpdateProgress();
			UpdateBackgroundColor();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == ProgressBar.ProgressProperty.PropertyName)
				UpdateProgress();
			else if (e.PropertyName == ProgressBar.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor();
		}

		private void UpdateProgress()
		{
			if (Control == null)
				return;

			Control.Fraction = Element.Progress;
			Control.TooltipText = string.Format("{0}%", (Element.Progress * 100));
		}

		protected override void UpdateBackgroundColor()
		{
			var backgroundColor = Element.BackgroundColor;

			if (backgroundColor.IsDefault)
				return;

			Control.SetBackgroundColor(backgroundColor.ToGtkColor(), Gtk.StateType.Normal);

			base.UpdateBackgroundColor();
		}
	}
}
