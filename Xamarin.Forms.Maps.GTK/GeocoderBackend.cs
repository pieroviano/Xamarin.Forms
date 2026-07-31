using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Forms.Maps.GTK
{
	/// <summary>
	/// Backs <see cref="Geocoder"/> with Nominatim, the OpenStreetMap geocoder.
	/// </summary>
	/// <remarks>
	/// The GTK# 2 implementation called <c>GMapProviders.GoogleMap.GetPoints</c> /
	/// <c>GetDirections</c>; GMap.NET is gone with the GTK 2 control, so this talks to Nominatim
	/// over plain HTTP and parses with <see cref="DataContractJsonSerializer"/> (no JSON package
	/// dependency). Nominatim's usage policy caps this at roughly one request per second and
	/// requires an identifying User-Agent - point <see cref="ServiceRoot"/> at your own instance
	/// for anything beyond light interactive use.
	/// </remarks>
	internal static class GeocoderBackend
	{
		static readonly HttpClient Http = CreateHttpClient();

		/// <summary>Base URL of the Nominatim instance to query.</summary>
		public static string ServiceRoot { get; set; } = "https://nominatim.openstreetmap.org";

		public static void Register()
		{
			Geocoder.GetPositionsForAddressAsyncFunc = GetPositionsForAddressAsync;
			Geocoder.GetAddressesForPositionFuncAsync = GetAddressesForPositionAsync;
		}

		static HttpClient CreateHttpClient()
		{
			var client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20)
			};

			client.DefaultRequestHeaders.TryAddWithoutValidation(
				"User-Agent",
				"Xamarin.Forms.Maps.GTK/1.0 (+https://github.com/pieroviano/Xamarin.Forms)");

			return client;
		}

		public static async Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
		{
			if (string.IsNullOrWhiteSpace(address))
				return Enumerable.Empty<Position>();

			var url = string.Format(
				CultureInfo.InvariantCulture,
				"{0}/search?format=json&limit=10&q={1}",
				ServiceRoot.TrimEnd('/'),
				Uri.EscapeDataString(address));

			var places = await GetAsync<List<NominatimPlace>>(url).ConfigureAwait(false);

			if (places == null)
				return Enumerable.Empty<Position>();

			var positions = new List<Position>();

			foreach (var place in places)
			{
				if (TryParsePosition(place, out var position))
					positions.Add(position);
			}

			return positions;
		}

		public static async Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
		{
			var url = string.Format(
				CultureInfo.InvariantCulture,
				"{0}/reverse?format=json&lat={1}&lon={2}",
				ServiceRoot.TrimEnd('/'),
				position.Latitude.ToString("R", CultureInfo.InvariantCulture),
				position.Longitude.ToString("R", CultureInfo.InvariantCulture));

			var place = await GetAsync<NominatimPlace>(url).ConfigureAwait(false);

			if (place == null || string.IsNullOrEmpty(place.DisplayName))
				return Enumerable.Empty<string>();

			return new[] { place.DisplayName };
		}

		static bool TryParsePosition(NominatimPlace place, out Position position)
		{
			position = default(Position);

			if (place == null)
				return false;

			if (!double.TryParse(place.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
				return false;

			if (!double.TryParse(place.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
				return false;

			position = new Position(latitude, longitude);

			return true;
		}

		static async Task<T> GetAsync<T>(string url) where T : class
		{
			try
			{
				var response = await Http.GetAsync(url).ConfigureAwait(false);

				if (!response.IsSuccessStatusCode)
					return null;

				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				if (string.IsNullOrWhiteSpace(json))
					return null;

				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
				{
					return new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
				}
			}
			catch (Exception)
			{
				// Offline, throttled, or a shape we do not understand: the Forms API contract is an
				// empty result set, not an exception.
				return null;
			}
		}

		[DataContract]
		internal class NominatimPlace
		{
			[DataMember(Name = "lat")]
			public string Latitude { get; set; }

			[DataMember(Name = "lon")]
			public string Longitude { get; set; }

			[DataMember(Name = "display_name")]
			public string DisplayName { get; set; }
		}
	}
}
