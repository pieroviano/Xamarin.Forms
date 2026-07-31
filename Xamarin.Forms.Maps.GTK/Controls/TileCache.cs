using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Forms.Maps.GTK.Controls
{
	/// <summary>
	/// Fetches and caches raster map tiles.
	/// </summary>
	/// <remarks>
	/// Every field here is touched from the GTK main thread only: <see cref="TryGetTile"/> and
	/// <see cref="RequestTile"/> are called from the draw handler, and the completion of a download
	/// is marshalled back with <see cref="GLib.Idle"/> before anything is mutated. The download and
	/// the disk cache I/O are the only work that happens off-thread.
	/// </remarks>
	internal sealed class TileCache
	{
		const int MaxMemoryEntries = 512;
		const int MaxConcurrentDownloads = 6;

		static readonly HttpClient Http = CreateHttpClient();

		readonly Dictionary<string, Gdk.Pixbuf> _memory = new Dictionary<string, Gdk.Pixbuf>();
		readonly LinkedList<string> _order = new LinkedList<string>();
		readonly HashSet<string> _pending = new HashSet<string>();
		readonly HashSet<string> _failed = new HashSet<string>();
		readonly Queue<string> _queued = new Queue<string>();
		readonly Dictionary<string, Action> _callbacks = new Dictionary<string, Action>();

		int _inFlight;

		public static TileCache Default { get; } = new TileCache();

		/// <summary>
		/// Where downloaded tiles are kept between runs. Set to null to disable the disk cache.
		/// </summary>
		public static string CacheDirectory { get; set; } = DefaultCacheDirectory();

		static HttpClient CreateHttpClient()
		{
			var client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20)
			};

			// OpenStreetMap's tile usage policy requires a identifying User-Agent; anonymous
			// clients are served 429/403.
			client.DefaultRequestHeaders.TryAddWithoutValidation(
				"User-Agent",
				"Xamarin.Forms.Maps.GTK/1.0 (+https://github.com/pieroviano/Xamarin.Forms)");

			return client;
		}

		static string DefaultCacheDirectory()
		{
			try
			{
				var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

				return string.IsNullOrEmpty(root)
					? null
					: Path.Combine(root, "Xamarin.Forms.Maps.GTK", "tiles");
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>True when the tile is decoded and ready to paint.</summary>
		public bool TryGetTile(string url, out Gdk.Pixbuf pixbuf)
		{
			return _memory.TryGetValue(url, out pixbuf);
		}

		/// <summary>True when the tile has been tried and could not be fetched or decoded.</summary>
		public bool HasFailed(string url)
		{
			return _failed.Contains(url);
		}

		/// <summary>
		/// Schedules a fetch for <paramref name="url"/> unless it is already cached, in flight or
		/// known-bad. <paramref name="onLoaded"/> runs on the GTK main thread when the tile lands.
		/// </summary>
		public void RequestTile(string url, Action onLoaded)
		{
			if (_memory.ContainsKey(url) || _pending.Contains(url) || _failed.Contains(url))
				return;

			_pending.Add(url);
			_callbacks[url] = onLoaded;
			_queued.Enqueue(url);

			PumpQueue();
		}

		void PumpQueue()
		{
			while (_inFlight < MaxConcurrentDownloads && _queued.Count > 0)
			{
				var url = _queued.Dequeue();

				_inFlight++;
				StartFetch(url);
			}
		}

		void StartFetch(string url)
		{
			var cachePath = DiskPathFor(url);

			Task.Run(() => FetchBytes(url, cachePath)).ContinueWith(task =>
			{
				var bytes = task.Status == TaskStatus.RanToCompletion ? task.Result : null;

				// Back onto the GTK main thread: Gdk.Pixbuf and every collection below belong to it.
				GLib.Idle.Add(() =>
				{
					OnFetchCompleted(url, bytes);
					return false;
				});
			}, TaskScheduler.Default);
		}

		static byte[] FetchBytes(string url, string cachePath)
		{
			if (cachePath != null && File.Exists(cachePath))
			{
				try
				{
					return File.ReadAllBytes(cachePath);
				}
				catch (IOException)
				{
					// Fall through and re-download.
				}
			}

			try
			{
				var response = Http.GetAsync(url).GetAwaiter().GetResult();

				if (!response.IsSuccessStatusCode)
					return null;

				var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

				if (bytes != null && bytes.Length > 0 && cachePath != null)
					TryWriteDiskCache(cachePath, bytes);

				return bytes;
			}
			catch (Exception)
			{
				// No network, DNS failure, throttling: the caller renders a placeholder tile.
				return null;
			}
		}

		static void TryWriteDiskCache(string cachePath, byte[] bytes)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(cachePath));

				var temporary = cachePath + ".tmp";
				File.WriteAllBytes(temporary, bytes);

				if (File.Exists(cachePath))
					File.Delete(cachePath);

				File.Move(temporary, cachePath);
			}
			catch (Exception)
			{
				// A cold cache is not an error.
			}
		}

		void OnFetchCompleted(string url, byte[] bytes)
		{
			_inFlight = Math.Max(0, _inFlight - 1);
			_pending.Remove(url);

			_callbacks.TryGetValue(url, out var callback);
			_callbacks.Remove(url);

			if (bytes == null || bytes.Length == 0)
			{
				_failed.Add(url);
			}
			else
			{
				try
				{
					Store(url, new Gdk.Pixbuf(bytes));
				}
				catch (Exception)
				{
					// Not an image (an HTML error page, a truncated body): treat as a miss.
					_failed.Add(url);
				}
			}

			PumpQueue();

			callback?.Invoke();
		}

		void Store(string url, Gdk.Pixbuf pixbuf)
		{
			_memory[url] = pixbuf;
			_order.AddLast(url);

			while (_order.Count > MaxMemoryEntries)
			{
				var oldest = _order.First.Value;
				_order.RemoveFirst();

				// Deliberately not Dispose()d: the draw handler may still be holding the reference
				// it fetched a moment ago, and unref-ing it under GTK's feet is a native crash.
				// Dropping the managed reference lets GtkSharp's finalizer unref it safely.
				_memory.Remove(oldest);
			}
		}

		static string DiskPathFor(string url)
		{
			var directory = CacheDirectory;

			if (string.IsNullOrEmpty(directory))
				return null;

			string hash;

			using (var sha = SHA1.Create())
			{
				var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(url));
				var builder = new StringBuilder(digest.Length * 2);

				foreach (var b in digest)
					builder.Append(b.ToString("x2"));

				hash = builder.ToString();
			}

			// Two levels of fan-out so a big cache does not end up as one enormous directory.
			return Path.Combine(directory, hash.Substring(0, 2), hash.Substring(2, 2), hash + ".tile");
		}
	}
}
