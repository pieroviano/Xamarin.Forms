using System;

namespace Xamarin.Forms.Maps.GTK.Controls
{
	/// <summary>
	/// Spherical Web-Mercator (EPSG:3857) projection, in the "slippy map" pixel convention every
	/// XYZ tile server uses: the whole world is a square of <c>256 * 2^zoom</c> pixels, with
	/// (0,0) at the north-west corner (lat +85.05, lon -180).
	/// </summary>
	internal static class MercatorProjection
	{
		public const int TileSize = 256;

		/// <summary>
		/// The latitude at which the Mercator projection becomes square. Beyond it y runs to
		/// infinity, so every tile server clamps here.
		/// </summary>
		public const double MaxLatitude = 85.05112877980659;

		public const int MinZoomLevel = 0;
		public const int MaxZoomLevel = 19;

		/// <summary>Metres per pixel at the equator, zoom 0 - the standard slippy-map constant.</summary>
		const double EquatorMetersPerPixel = 156543.033928041;

		public static double WorldSize(int zoom)
		{
			return TileSize * Math.Pow(2.0, zoom);
		}

		public static int TileCount(int zoom)
		{
			return 1 << zoom;
		}

		public static double ClampLatitude(double latitude)
		{
			return Math.Max(-MaxLatitude, Math.Min(MaxLatitude, latitude));
		}

		public static double NormalizeLongitude(double longitude)
		{
			var normalized = (longitude + 180.0) % 360.0;

			if (normalized < 0.0)
				normalized += 360.0;

			return normalized - 180.0;
		}

		public static double LongitudeToX(double longitude, int zoom)
		{
			return (NormalizeLongitude(longitude) + 180.0) / 360.0 * WorldSize(zoom);
		}

		public static double LatitudeToY(double latitude, int zoom)
		{
			var radians = ClampLatitude(latitude) * Math.PI / 180.0;
			var mercator = Math.Log(Math.Tan(radians) + 1.0 / Math.Cos(radians));

			return (1.0 - mercator / Math.PI) / 2.0 * WorldSize(zoom);
		}

		public static double XToLongitude(double x, int zoom)
		{
			return x / WorldSize(zoom) * 360.0 - 180.0;
		}

		public static double YToLatitude(double y, int zoom)
		{
			var n = Math.PI - 2.0 * Math.PI * y / WorldSize(zoom);

			return 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
		}

		public static double MetersPerPixel(double latitude, int zoom)
		{
			return EquatorMetersPerPixel * Math.Cos(ClampLatitude(latitude) * Math.PI / 180.0) / Math.Pow(2.0, zoom);
		}

		/// <summary>
		/// The largest zoom level at which a <see cref="MapSpan"/> still fits inside a viewport of
		/// <paramref name="widthPixels"/> x <paramref name="heightPixels"/>.
		/// </summary>
		public static int ZoomForSpan(
			double centerLatitude,
			double latitudeDegrees,
			double longitudeDegrees,
			int widthPixels,
			int heightPixels,
			int maxZoom)
		{
			if (widthPixels <= 0 || heightPixels <= 0)
				return MinZoomLevel;

			var north = ClampLatitude(centerLatitude + latitudeDegrees / 2.0);
			var south = ClampLatitude(centerLatitude - latitudeDegrees / 2.0);

			for (var zoom = Math.Min(maxZoom, MaxZoomLevel); zoom > MinZoomLevel; zoom--)
			{
				var spanHeight = Math.Abs(LatitudeToY(south, zoom) - LatitudeToY(north, zoom));
				var spanWidth = Math.Min(longitudeDegrees, 360.0) / 360.0 * WorldSize(zoom);

				if (spanWidth <= widthPixels && spanHeight <= heightPixels)
					return zoom;
			}

			return MinZoomLevel;
		}
	}
}
