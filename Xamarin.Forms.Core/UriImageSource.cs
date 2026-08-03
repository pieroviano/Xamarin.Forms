using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using IOPath = System.IO.Path;

namespace Xamarin.Forms
{
	public sealed class UriImageSource : ImageSource
	{
		internal const string CacheName = "ImageLoaderCache";

		public static readonly BindableProperty UriProperty = BindableProperty.Create("Uri", typeof(Uri), typeof(UriImageSource), default(Uri),
			propertyChanged: (bindable, oldvalue, newvalue) => ((UriImageSource)bindable).OnUriChanged(), validateValue: (bindable, value) => value == null || ((Uri)value).IsAbsoluteUri);

		static readonly Xamarin.Forms.Internals.IIsolatedStorageFile Store = Device.PlatformServices.GetUserStoreForApplication();

		static readonly object s_syncHandle = new object();
		static readonly Dictionary<string, LockingSemaphore> s_semaphores = new Dictionary<string, LockingSemaphore>();

		TimeSpan _cacheValidity = TimeSpan.FromDays(1);

		bool _cachingEnabled = true;

		/// <summary>
		/// Ensures the on-disk cache directory exists, without blocking.
		/// </summary>
		/// <remarks>
		/// This work used to live in a STATIC CONSTRUCTOR that blocked on the two tasks with
		/// <c>.Result</c> and <c>.Wait()</c>. Sync-over-async is a deadlock risk under any UI
		/// synchronization context, and a type initializer is the worst possible place to take
		/// that risk: the CLR caches the failure, so a single deadlock or fault would make
		/// <see cref="UriImageSource"/> permanently unusable for the rest of the process, with
		/// every later use throwing a <c>TypeInitializationException</c> whose real cause is
		/// several frames down and long past.
		///
		/// Awaited from the cache path instead. The existence check is one cheap local I/O call
		/// against an operation that is already doing network I/O. Deliberately NOT memoised in a
		/// cached <c>Task</c>: a cached faulted task would reintroduce exactly the permanent
		/// poisoning this removes.
		/// </remarks>
		static async Task EnsureCacheDirectoryAsync()
		{
			if (!await Store.GetDirectoryExistsAsync(CacheName).ConfigureAwait(false))
				await Store.CreateDirectoryAsync(CacheName).ConfigureAwait(false);
		}

		public override bool IsEmpty => Uri == null;

		public TimeSpan CacheValidity
		{
			get { return _cacheValidity; }
			set
			{
				if (_cacheValidity == value)
					return;

				OnPropertyChanging();
				_cacheValidity = value;
				OnPropertyChanged();
			}
		}

		public bool CachingEnabled
		{
			get { return _cachingEnabled; }
			set
			{
				if (_cachingEnabled == value)
					return;

				OnPropertyChanging();
				_cachingEnabled = value;
				OnPropertyChanged();
			}
		}

		[TypeConverter(typeof(UriTypeConverter))]
		public Uri Uri
		{
			get { return (Uri)GetValue(UriProperty); }
			set { SetValue(UriProperty, value); }
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public async Task<Stream> GetStreamAsync(CancellationToken userToken = default(CancellationToken))
		{
			OnLoadingStarted();
			userToken.Register(CancellationTokenSource.Cancel);
			Stream stream;

			try
			{
				stream = await GetStreamAsync(Uri, CancellationTokenSource.Token);
				OnLoadingCompleted(false);
			}
			catch (OperationCanceledException)
			{
				OnLoadingCompleted(true);
				throw;
			}
			catch (Exception ex)
			{
				Xamarin.Forms.Internals.Log.Warning("Image Loading", $"Error getting stream for {Uri}: {ex}");
				throw;
			}

			return stream;
		}

		public override string ToString()
		{
			return $"Uri: {Uri}";
		}

		static string GetCacheKey(Uri uri)
		{
			return Device.PlatformServices.GetHash(uri.AbsoluteUri);
		}

		async Task<bool> GetHasLocallyCachedCopyAsync(string key, bool checkValidity = true)
		{
			DateTime now = DateTime.UtcNow;
			DateTime? lastWriteTime = await GetLastWriteTimeUtcAsync(key).ConfigureAwait(false);
			return lastWriteTime.HasValue && now - lastWriteTime.Value < CacheValidity;
		}

		static async Task<DateTime?> GetLastWriteTimeUtcAsync(string key)
		{
			string path = IOPath.Combine(CacheName, key);
			if (!await Store.GetFileExistsAsync(path).ConfigureAwait(false))
				return null;

			return (await Store.GetLastWriteTimeAsync(path).ConfigureAwait(false)).UtcDateTime;
		}

		async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			Stream stream = null;

			// else, NOT "if (stream == null)". The cache path already performs the download itself
			// (GetStreamAsyncUnchecked calls Device.GetStreamAsync when it has no usable locally
			// cached copy), so falling through on a null result issued a SECOND identical request
			// for every failed retrieval - two network round-trips for one missing image. The
			// direct fetch is only the right thing to do when caching is switched off and the
			// branch above was therefore skipped entirely.
			//
			// This is only safe because GetStreamAsyncUnchecked never gives up before trying the
			// network: an unusable cache entry falls through to the download instead of returning
			// null. Do not reintroduce an early null return there without restoring a fallback
			// here, or one broken cache file blanks the image for the whole CacheValidity window.
			//
			// Covered by UriImageSourceTests.DoNotKeepFailedRetrieveInCache, which asserts exactly
			// one network call per attempt and was disabled - as [Ignore("DoNotKeepFailedRetrieve
			// InCache")] - for long enough to hide this.
			if (CachingEnabled)
			{
				stream = await GetStreamFromCacheAsync(uri, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				try
				{
					stream = await Device.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Xamarin.Forms.Internals.Log.Warning("Image Loading", $"Error getting stream for {Uri}: {ex}");
					stream = null;
				}
			}

			return stream;
		}

		async Task<Stream> GetStreamAsyncUnchecked(string key, Uri uri, CancellationToken cancellationToken)
		{
			string path = IOPath.Combine(CacheName, key);

			// A USABLE cached copy returns here; every other outcome falls through to the download
			// below. That is what lets GetStreamAsync skip its direct-fetch fallback (see the
			// comment there), and it has to stay true. An entry that is still inside CacheValidity
			// but cannot be read - a zero-byte or truncated file left behind when the copy below
			// was interrupted (app killed, disk full), or one every open attempt lost the race for
			// - must NOT short-circuit to null: its timestamp keeps it "valid", so the image would
			// come back blank on every attempt for the rest of the validity window, a day by
			// default. Falling through re-downloads and rewrites the entry in place, so a broken
			// cache heals itself on the first retry, exactly as it did before the fallback moved.
			if (await GetHasLocallyCachedCopyAsync(key).ConfigureAwait(false))
			{
				Stream cached = await OpenLocallyCachedCopyAsync(path).ConfigureAwait(false);
				if (cached != null)
					return cached;
			}

			Stream stream;
			try
			{
				stream = await Device.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warning("Image Loading", $"Error getting stream for {Uri}: {ex}");
				return null;
			}

			if (stream == null || !stream.CanRead)
			{
				stream?.Dispose();
				return null;
			}

			try
			{
				using (stream)
				using (Stream writeStream = await Store.OpenFileAsync(
					path, FileMode.Create, FileAccess.Write).ConfigureAwait(false))
				{
					await stream.CopyToAsync(writeStream, 16384, cancellationToken).ConfigureAwait(false);
				}

				return await Store.OpenFileAsync(path, FileMode.Open, FileAccess.Read).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warning("Image Loading", $"Error getting stream for {Uri}: {ex}");

				// The entry may now hold a truncated copy of the download, which the read path
				// cannot tell from a complete one. Blank it so it is rejected as a miss rather
				// than served as half an image until it expires.
				await InvalidateCacheEntryAsync(path).ConfigureAwait(false);
				return null;
			}
		}

		/// <summary>
		/// Opens the locally cached copy, or returns null when there is no usable one - in which
		/// case the caller must fall through to the network.
		/// </summary>
		static async Task<Stream> OpenLocallyCachedCopyAsync(string path)
		{
			var retry = 5;
			while (retry >= 0)
			{
				int backoff;
				try
				{
					Stream result = await Store.OpenFileAsync(path, FileMode.Open, FileAccess.Read).ConfigureAwait(false);

					// Zero-length or unreadable means the entry is broken, not that the image is
					// empty: the download writes the cache file in place, so an interrupted write
					// leaves precisely this behind. Report a miss and let the caller re-download
					// over it. (GetStreamFromCacheAsync repeats the length check for the freshly
					// downloaded copy, where a zero-length result is a genuinely empty response.)
					if (result == null || !result.CanRead || (result.CanSeek && result.Length == 0))
					{
						result?.Dispose();
						return null;
					}

					return result;
				}
				catch (IOException)
				{
					// iOS seems to not like 2 readers opening the file at the exact same time, back off for random amount of time
					backoff = new Random().Next(1, 5);
					retry--;
				}

				if (backoff > 0)
				{
					await Task.Delay(backoff);
				}
			}

			return null;
		}

		/// <summary>
		/// Marks a cache entry unusable so the next load re-downloads it.
		/// </summary>
		/// <remarks>
		/// <see cref="Xamarin.Forms.Internals.IIsolatedStorageFile"/> exposes no delete, and there
		/// is no rename to write the download to a temporary file and swap it in atomically, so
		/// the entry is truncated to zero bytes instead - the length the read path already treats
		/// as broken. Best effort: if even that fails the next successful download overwrites the
		/// entry anyway, since it opens with <see cref="FileMode.Create"/>.
		/// </remarks>
		static async Task InvalidateCacheEntryAsync(string path)
		{
			try
			{
				if (!await Store.GetFileExistsAsync(path).ConfigureAwait(false))
					return;

				using (await Store.OpenFileAsync(path, FileMode.Create, FileAccess.Write).ConfigureAwait(false))
				{
				}
			}
			catch (Exception ex)
			{
				Log.Warning("Image Loading", $"Error invalidating cached image {path}: {ex}");
			}
		}

		async Task<Stream> GetStreamFromCacheAsync(Uri uri, CancellationToken cancellationToken)
		{
			// Replaces the blocking static constructor - see EnsureCacheDirectoryAsync. This is
			// the only entry point that touches the cache directory, so it is the right gate.
			await EnsureCacheDirectoryAsync().ConfigureAwait(false);

			string key = GetCacheKey(uri);
			LockingSemaphore sem;
			lock (s_syncHandle)
			{
				if (s_semaphores.ContainsKey(key))
					sem = s_semaphores[key];
				else
					s_semaphores.Add(key, sem = new LockingSemaphore(1));
			}

			try
			{
				await sem.WaitAsync(cancellationToken);
				Stream stream = await GetStreamAsyncUnchecked(key, uri, cancellationToken);
				if (stream == null || stream.Length == 0 || !stream.CanRead)
				{
					sem.Release();
					return null;
				}
				var wrapped = new StreamWrapper(stream);
				wrapped.Disposed += (o, e) => sem.Release();
				return wrapped;
			}
			catch (Exception)
			{
				// Release on ANY failure, not just cancellation. The success path hands the permit
				// to the wrapper's Disposed handler, so an exception escaping with the semaphore
				// still held would hang every later load of this Uri for the life of the process.
				// Cancellation out of WaitAsync is deliberately included: that waiter is still
				// queued inside LockingSemaphore, and this Release is what drains it back out.
				sem.Release();
				throw;
			}
		}

		void OnUriChanged()
		{
			if (CancellationTokenSource != null)
				CancellationTokenSource.Cancel();
			OnSourceChanged();
		}
	}
}