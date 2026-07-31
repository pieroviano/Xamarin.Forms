using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;
using GtkImageButton = Xamarin.Forms.Platform.GTK.Controls.ImageButton;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	/// <summary>
	/// ImageButton is an image-only button: unlike <see cref="Button"/> it has no Text, and its
	/// image is the content rather than a decoration. It reuses the same native control, which
	/// already knows how to draw a background, a border and an image.
	/// </summary>
	public class ImageButtonRenderer : ViewRenderer<ImageButton, GtkImageButton>
	{
		private const uint DefaultBorderWidth = 1;

		private bool _disposed;

		protected override bool PreventGestureBubbling { get; set; } = true;

		public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			Control.GetPreferredSize(out _, out var req);

			var widthFits = widthConstraint >= req.Width;
			var heightFits = heightConstraint >= req.Height;

			var size = new Size(widthFits ? req.Width : (int)widthConstraint,
				heightFits ? req.Height : (int)heightConstraint);

			return new SizeRequest(size);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<ImageButton> e)
		{
			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var button = new GtkImageButton();
					SetNativeControl(button);

					Control.Clicked += OnButtonClicked;
					Control.ButtonPressEvent += OnButtonPressEvent;
					Control.ButtonReleaseEvent += OnButtonReleaseEvent;
				}

				UpdateBackgroundColor();
				UpdateBorder();
				UpdatePadding();
				UpdateSource();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == ImageButton.SourceProperty.PropertyName ||
				e.PropertyName == ImageButton.AspectProperty.PropertyName)
				UpdateSource();
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor();
			else if (e.PropertyName == ImageButton.BorderColorProperty.PropertyName ||
				e.PropertyName == ImageButton.BorderWidthProperty.PropertyName)
				UpdateBorder();
			else if (e.PropertyName == ImageButton.PaddingProperty.PropertyName)
				UpdatePadding();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				if (Control != null)
				{
					Control.Clicked -= OnButtonClicked;
					Control.ButtonPressEvent -= OnButtonPressEvent;
					Control.ButtonReleaseEvent -= OnButtonReleaseEvent;
				}
			}

			base.Dispose(disposing);
		}

		protected override void UpdateBackgroundColor()
		{
			if (Element == null)
				return;

			if (Element.BackgroundColor.IsDefault)
				Control.ResetBackgroundColor();
			else if (Element.BackgroundColor != Color.Transparent)
				Control.SetBackgroundColor(Element.BackgroundColor.ToGtkColor());
		}

		private void UpdateBorder()
		{
			if (Element.BorderWidth > 0)
				Control.SetBorderWidth((uint)Element.BorderWidth);
			else
				Control.SetBorderWidth(DefaultBorderWidth);

			if (!Element.BorderColor.IsDefault)
				Control.SetBorderColor(Element.BorderColor.ToGtkColor());
			else
				Control.SetBorderColor(null);
		}

		private void UpdatePadding()
		{
			// Gtk.Container.BorderWidth is uniform, so a non-uniform Forms Padding collapses to
			// its largest edge. Forms allows negative padding; GTK's is unsigned.
			var padding = Element.Padding;

			var largest = Math.Max(
				Math.Max(padding.Left, padding.Right),
				Math.Max(padding.Top, padding.Bottom));

			Control.BorderWidth = (uint)Math.Max(0, largest);
		}

		private void UpdateSource()
		{
			// SetIsLoading drives ImageButton.IsLoading, which apps bind to; ApplyNativeImageAsync
			// reports it through its onLoading callback.
			this.ApplyNativeImageAsync(
				ImageButton.SourceProperty,
				image =>
				{
					Control.ImageWidget.Pixbuf = image;
					Control.ImageWidget.Visible = image != null;
				},
				loading => ((IImageController)Element)?.SetIsLoading(loading));
		}

		private void OnButtonClicked(object sender, EventArgs e)
		{
			((IButtonController)Element)?.SendClicked();
		}

		private void OnButtonPressEvent(object sender, Gtk.ButtonPressEventArgs e)
		{
			((IButtonController)Element)?.SendPressed();
		}

		private void OnButtonReleaseEvent(object sender, Gtk.ButtonReleaseEventArgs e)
		{
			((IButtonController)Element)?.SendReleased();
		}
	}
}
