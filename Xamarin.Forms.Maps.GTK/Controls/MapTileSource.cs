using System;

namespace Xamarin.Forms.Maps.GTK.Controls
{
	/// <summary>
	/// An XYZ ("slippy map") raster tile layer: a URL template plus the attribution the tile
	/// provider's terms require you to display.
	/// </summary>
	/// <remarks>
	/// The templates understand <c>{z}</c>, <c>{x}</c> and <c>{y}</c>. The defaults point at public
	/// community servers whose usage policies forbid heavy or commercial traffic - replace
	/// <see cref="Street"/>, <see cref="Satellite"/> and <see cref="Hybrid"/> (or set
	/// <see cref="FormsMaps.TileSourceForMapType"/>) with your own tile server before shipping.
	/// </remarks>
	public sealed class MapTileSource
	{
		public MapTileSource(string urlTemplate, string attribution, int maxZoom = MercatorProjection.MaxZoomLevel, string overlayUrlTemplate = null)
		{
			if (string.IsNullOrEmpty(urlTemplate))
				throw new ArgumentNullException(nameof(urlTemplate));

			UrlTemplate = urlTemplate;
			OverlayUrlTemplate = overlayUrlTemplate;
			Attribution = attribution ?? string.Empty;
			MaxZoom = Math.Max(MercatorProjection.MinZoomLevel, Math.Min(maxZoom, MercatorProjection.MaxZoomLevel));
		}

		public string UrlTemplate { get; }

		/// <summary>A second, transparent layer painted over <see cref="UrlTemplate"/>; may be null.</summary>
		public string OverlayUrlTemplate { get; }

		public string Attribution { get; }

		public int MaxZoom { get; }

		public static MapTileSource Street { get; set; } = new MapTileSource(
			"https://tile.openstreetmap.org/{z}/{x}/{y}.png",
			"© OpenStreetMap contributors",
			19);

		public static MapTileSource Satellite { get; set; } = new MapTileSource(
			"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
			"Imagery © Esri",
			19);

		public static MapTileSource Hybrid { get; set; } = new MapTileSource(
			"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
			"Imagery © Esri",
			19,
			"https://server.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{z}/{y}/{x}");

		public static MapTileSource ForMapType(MapType mapType)
		{
			switch (mapType)
			{
				case MapType.Satellite:
					return Satellite;
				case MapType.Hybrid:
					return Hybrid;
				default:
					return Street;
			}
		}

		public static string FormatUrl(string template, int zoom, int x, int y)
		{
			return template
				.Replace("{z}", zoom.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Replace("{x}", x.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Replace("{y}", y.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}
	}
}
