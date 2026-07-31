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

			var screen = Gdk.Screen.Default;

			if (screen != null)
			{
				// Re-read on monitor changes (resolution switch, monitor plugged in) so a rotated
				// or resized screen updates Forms instead of keeping the start-up value forever.
				screen.SizeChanged += (o, args) => CurrentOrientation = GetOrientation();
				screen.MonitorsChanged += (o, args) => CurrentOrientation = GetOrientation();
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

			// GetMonitor(0) covers displays that report no primary monitor, which is the normal
			// case under Xvfb and some Wayland compositors.
			return display.PrimaryMonitor ?? display.GetMonitor(0);
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
