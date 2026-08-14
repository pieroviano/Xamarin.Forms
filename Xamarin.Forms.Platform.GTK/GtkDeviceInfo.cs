using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.GTK
{
	/// <summary>
	/// Reports the real screen geometry rather than the hardcoded 800x600 the GTK2 backend used.
	/// This matters beyond cosmetics: <c>FlyoutPage.ShouldShowSplitMode</c> asks
	/// <c>Device.Info.CurrentOrientation.IsLandscape()</c>, so a stubbed orientation silently
	/// picks the wrong flyout behaviour on desktop.
	/// </summary>
	public class GtkDeviceInfo : DeviceInfo
	{
		// Fallbacks for a headless/screenless process, where Gdk.Display.Default is null.
		const int FallbackWidth = 800;
		const int FallbackHeight = 600;

		public GtkDeviceInfo()
		{
			CurrentOrientation = GetOrientation();

			// Gtk 4 removed GdkScreen and with it screen-level size/monitor signals. What replaced
			// them is the display's monitor list, which is a GListModel - so "the monitors
			// changed" is items-changed on that model, and it covers both cases the two Gtk 3
			// signals did between them (a resolution switch changes a monitor's geometry, a
			// plugged-in monitor changes the list).
			var monitors = Gdk.Display.Default?.Monitors;

            if (monitors != null)
            {
                monitors.ItemsChanged += (o, args) => CurrentOrientation = GetOrientation();
            }
		}

		public override Size PixelScreenSize
		{
			get
			{
				var scaled = ScaledScreenSize;
				var scale = ScalingFactor;

				return new Size(scaled.Width * scale, scaled.Height * scale);
			}
		}

		public override Size ScaledScreenSize
		{
			get
			{
				var geometry = GetPrimaryMonitorGeometry();

				return new Size(geometry.Width, geometry.Height);
			}
		}

		public override double ScalingFactor
		{
			get
			{
				var monitor = GetPrimaryMonitor();

				// Gdk reports an integer scale factor (1 for normal, 2 for HiDPI).
				return monitor?.ScaleFactor ?? 1;
			}
		}

		static Gdk.Monitor GetPrimaryMonitor()
		{
			var display = Gdk.Display.Default;

			if (display == null)
				return null;

			// The first monitor, full stop. Gtk 4 removed gdk_display_get_primary_monitor - there
			// is no portable notion of a primary monitor under Wayland, which is why it went - so
			// the list's first entry is what is left, and it is what the Gtk 3 fallback path here
			// already used under Xvfb and some Wayland compositors.
			var monitors = display.Monitors;

			return monitors != null && monitors.NItems > 0
				? monitors.GetObject(0) as Gdk.Monitor
				: null;
		}

		static Gdk.Rectangle GetPrimaryMonitorGeometry()
		{
			var monitor = GetPrimaryMonitor();

			if (monitor == null)
				return new Gdk.Rectangle(0, 0, FallbackWidth, FallbackHeight);

			return monitor.Geometry;
		}

		static DeviceOrientation GetOrientation()
		{
			var geometry = GetPrimaryMonitorGeometry();

			return geometry.Width >= geometry.Height
				? DeviceOrientation.Landscape
				: DeviceOrientation.Portrait;
		}
	}
}
