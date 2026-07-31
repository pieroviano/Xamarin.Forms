using System;

namespace Xamarin.Forms.Maps.GTK.Controls
{
	public class MapPositionEventArgs : EventArgs
	{
		public MapPositionEventArgs(Position position)
		{
			Position = position;
		}

		public Position Position { get; }
	}

	public class MapPinEventArgs : EventArgs
	{
		public MapPinEventArgs(Pin pin)
		{
			Pin = pin;
		}

		public Pin Pin { get; }
	}
}
