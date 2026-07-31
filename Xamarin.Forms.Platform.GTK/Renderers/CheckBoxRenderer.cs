using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class CheckBoxRenderer : ViewRenderer<CheckBox, Gtk.CheckButton>
	{
		private bool _disposed;

		protected override void OnElementChanged(ElementChangedEventArgs<CheckBox> e)
		{
			if (e.NewElement != null)
			{
				if (Control == null)
				{
					// A label-less Gtk.CheckButton: Forms puts the caption in a separate Label,
					// unlike GTK where the check button usually carries its own text.
					SetNativeControl(new Gtk.CheckButton());
				}

				Control.Toggled -= OnCheckButtonToggled;

				UpdateIsChecked();
				UpdateColor();

				Control.Toggled += OnCheckButtonToggled;
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == CheckBox.IsCheckedProperty.PropertyName)
				UpdateIsChecked();
			else if (e.PropertyName == CheckBox.ColorProperty.PropertyName)
				UpdateColor();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				if (Control != null)
					Control.Toggled -= OnCheckButtonToggled;
			}

			base.Dispose(disposing);
		}

		private void UpdateIsChecked()
		{
			if (Control.Active != Element.IsChecked)
				Control.Active = Element.IsChecked;
		}

		private void UpdateColor()
		{
			if (Element.Color == Color.Default)
			{
				// Let the theme draw the check, as elsewhere in this backend.
				Control.ClearStyle();
				return;
			}

			Control.SetForegroundColor(Element.Color.ToGtkColor());
		}

		private void OnCheckButtonToggled(object sender, EventArgs e)
		{
			ElementController.SetValueFromRenderer(CheckBox.IsCheckedProperty, Control.Active);
		}
	}
}
