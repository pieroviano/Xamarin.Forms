using Gdk;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class CustomFrame : Gtk.Frame
	{
		private Color _defaultBorderColor;
		private Color _defaultBackgroundColor;
		private Color? _borderColor;
		private Color? _backgroundColor;

		private uint _borderWidth;
		private bool _hasShadow;
		private uint _shadowWidth;

		public CustomFrame()
		{
			ShadowType = ShadowType.None;
			BorderWidth = 0;

			_borderWidth = 0;
			_hasShadow = false;
			_shadowWidth = 2;
			_defaultBackgroundColor = this.GetDefaultBackgroundColor(Gtk.StateFlags.Normal).ToXFColor();
			_defaultBorderColor = this.GetDefaultBaseColor(Gtk.StateFlags.Active).ToXFColor();
		}

		public void SetBackgroundColor(Color? color)
		{
			_backgroundColor = color;
			QueueDraw();
		}

		public void ResetBackgroundColor()
		{
			_backgroundColor = _defaultBackgroundColor;
			QueueDraw();
		}

		public void SetBorderWidth(uint width)
		{
			_borderWidth = width;
			QueueDraw();
		}

		public void SetBorderColor(Color? color)
		{
			_borderColor = color;
			QueueDraw();
		}

		public void ResetBorderColor()
		{
			_borderColor = _defaultBorderColor;
			QueueDraw();
		}

		public void SetShadow()
		{
			_hasShadow = true;
			QueueDraw();
		}

		public void ResetShadow()
		{
			_hasShadow = false;
			QueueDraw();
		}

		public void SetShadowWidth(uint width)
		{
			_shadowWidth = width;
			QueueDraw();
		}

		// GTK3 draw model: cr is supplied by GTK, already translated to this widget's
		// origin, so all geometry below is widget-local (0,0)-based rather than
		// Allocation-based as it was under GTK2's expose-event.
		protected override bool OnDrawn(Cairo.Context cr)
		{
			double width = AllocatedWidth;
			double height = AllocatedHeight;

			// Draw Shadow
			if (_hasShadow)
			{
				var color = Color.Black;
				cr.SetSourceRGBA(color.R, color.G, color.B, color.A);
				cr.Rectangle(_shadowWidth, _shadowWidth, width + _shadowWidth, height + _shadowWidth);
				cr.Fill();
			}

			// Draw BackgroundColor
			if (_backgroundColor.HasValue)
			{
				var color = _backgroundColor.Value;
				cr.SetSourceRGBA(color.R, color.G, color.B, color.A);
				cr.Rectangle(0, 0, width, height);
				cr.FillPreserve();
			}

			// Draw BorderColor
			if (_borderColor.HasValue)
			{
				cr.LineWidth = _borderWidth;
				var color = _borderColor.Value;
				cr.SetSourceRGBA(color.R, color.G, color.B, color.A);
				cr.Rectangle(0, 0, width, height);
				cr.StrokePreserve();
			}

			return base.OnDrawn(cr);
		}
	}
}
