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
			// (GetStreamAsyncUnchecked calls Device.GetStreamAsync when there is no locally cached
			// copy), so falling through on a null result issued a SECOND identical request for
			// every failed retrieval - two network round-trips for one missing image. The direct
			// fetch is only the right thing to do when caching is switched off and the branch
			// above was therefore skipped entirely.
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
			if (await GetHasLocallyCachedCopyAsync(key).ConfigureAwait(false))
			{
				var retry = 5;
				while (retry >= 0)
				{
					int backoff;
					try
					{
						Stream result = await Store.OpenFileAsync(IOPath.Combine(CacheName, key), FileMode.Open, FileAccess.Read).ConfigureAwait(false);
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

			Stream stream;
			try
			{
				stream = await Device.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
				if (stream == null)
					return null;
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
				Stream writeStream = await Store.OpenFileAsync(IOPath.Combine(CacheName, key), FileMode.Create, FileAccess.Write).ConfigureAwait(false);
				await stream.CopyToAsync(writeStream, 16384, cancellationToken).ConfigureAwait(false);
				if (writeStream != null)
					writeStream.Dispose();

				stream.Dispose();

				return await Store.OpenFileAsync(IOPath.Combine(CacheName, key), FileMode.Open, FileAccess.Read).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warning("Image Loading", $"Error getting stream for {Uri}: {ex}");
				return null;
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
			catch (OperationCanceledException)
			{
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