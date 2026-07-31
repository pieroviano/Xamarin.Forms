using System;
using System.Threading.Tasks;
using Xamarin.Forms.Maps.GTK.Controls;

namespace Xamarin.Forms.Maps.GTK
{
	public static class FormsMaps
	{
		static bool _isInitialized;

		/// <summary>
		/// Kept for source compatibility with the other backends. The GTK renderer draws
		/// OpenStreetMap-compatible XYZ tiles and needs no key; set
		/// <see cref="MapTileSource.Street"/> (etc.) or <see cref="TileSourceForMapType"/> if your
		/// tile server wants a token in the URL.
		/// </summary>
		internal static string AuthenticationToken { get; set; }

		/// <summary>
		/// Overrides which tile layer each <see cref="MapType"/> uses. Defaults to
		/// <see cref="MapTileSource.ForMapType"/>.
		/// </summary>
		public static Func<MapType, MapTileSource> TileSourceForMapType { get; set; }

		/// <summary>
		/// Supplies the position drawn when <see cref="Map.IsShowingUser"/> is true. GTK has no
		/// location service of its own, so without this the flag does nothing - see the README.
		/// </summary>
		public static Func<Task<Position?>> UserPositionProvider { get; set; }

		/// <summary>How often <see cref="UserPositionProvider"/> is polled while showing the user.</summary>
		public static TimeSpan UserPositionRefreshInterval { get; set; } = TimeSpan.FromSeconds(5);

		public static void Init()
		{
			if (_isInitialized)
				return;

			GeocoderBackend.Register();

			_isInitialized = true;
		}

		public static void Init(string authenticationToken)
		{
			AuthenticationToken = authenticationToken;

			Init();
		}

		internal static MapTileSource ResolveTileSource(MapType mapType)
		{
			return TileSourceForMapType?.Invoke(mapType) ?? MapTileSource.ForMapType(mapType);
		}
	}
}
