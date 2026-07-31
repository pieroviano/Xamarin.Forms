using System;
using Cairo;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	/// <summary>
	/// Draws the indicator dots for <c>Xamarin.Forms.IndicatorView</c>. One DrawingArea painting
	/// every dot, rather than a Box of one widget per dot: the count changes as the bound
	/// collection changes, and repainting is far cheaper than adding and removing widgets.
	/// </summary>
	public class IndicatorViewControl : Gtk.DrawingArea
	{
		const double DotSpacing = 6;

		int _count;
		int _position;
		double _size = 6;
		bool _square;
		Gdk.Color? _indicatorColor;
		Gdk.Color? _selectedIndicatorColor;

		public void Update(int count, int position, double size, bool square)
		{
			_count = Math.Max(0, count);
			_position = position;
			_size = size > 0 ? size : 6;
			_square = square;

			// The control has to ask for the room it needs; nothing else measures it.
			SetSizeRequest((int)Math.Ceiling(_count * _size + Math.Max(0, _count - 1) * DotSpacing),
				(int)Math.Ceiling(_size));

			QueueDraw();
		}

		public void UpdateColors(Gdk.Color? indicatorColor, Gdk.Color? selectedIndicatorColor)
		{
			_indicatorColor = indicatorColor;
			_selectedIndicatorColor = selectedIndicatorColor;

			QueueDraw();
		}

		protected override bool OnDrawn(Context cr)
		{
			if (_count <= 0)
				return true;

			var totalWidth = _count * _size + (_count - 1) * DotSpacing;
			var x = Math.Max(0, (AllocatedWidth - totalWidth) / 2);
			var y = Math.Max(0, (AllocatedHeight - _size) / 2);

			for (int i = 0; i < _count; i++)
			{
				var isSelected = i == _position;
				var color = isSelected ? _selectedIndicatorColor : _indicatorColor;

				SetSource(cr, color, isSelected);

				if (_square)
					cr.Rectangle(x, y, _size, _size);
				else
					cr.Arc(x + _size / 2, y + _size / 2, _size / 2, 0, 2 * Math.PI);

				cr.Fill();

				x += _size + DotSpacing;
			}

			return true;
		}

		static void SetSource(Context cr, Gdk.Color? color, bool isSelected)
		{
			const double ColorMaxValue = 65535;

			if (color.HasValue)
			{
				var c = color.Value;
				cr.SetSourceRGB(c.Red / ColorMaxValue, c.Green / ColorMaxValue, c.Blue / ColorMaxValue);
			}
			else
			{
				// Color.Default: a neutral grey, dimmed for the unselected dots. Deriving these
				// from the theme would be better, but IndicatorView is drawn on top of arbitrary
				// app content whose colour GTK cannot tell us.
				var level = isSelected ? 0.35 : 0.75;
				cr.SetSourceRGB(level, level, level);
			}
		}
	}
}
