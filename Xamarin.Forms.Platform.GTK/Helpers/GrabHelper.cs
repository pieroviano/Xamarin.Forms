using Gtk;

namespace Xamarin.Forms.Platform.GTK.Helpers
{
	public class GrabHelper
	{
		public static void GrabWindow(Window window)
		{
			window.GrabFocus();

			Grab.Add(window);

			// GTK 3.20 replaced the separate Gdk.Pointer.Grab / Gdk.Keyboard.Grab pair with a
			// single seat grab that covers both device classes, so what used to be a nested
			// pointer-then-keyboard sequence is now one call with one failure path.
			var seat = window.Display?.DefaultSeat;

			var grabbed = seat?.Grab(
				window.Window,
				Gdk.SeatCapabilities.AllPointing | Gdk.SeatCapabilities.Keyboard,
				true,
				null,
				null,
				null);

			if (grabbed != Gdk.GrabStatus.Success)
			{
				Grab.Remove(window);
				window.Destroy();
			}
		}

		public static void RemoveGrab(Window window)
		{
			Grab.Remove(window);
			window.Display?.DefaultSeat?.Ungrab();
		}
	}
}
