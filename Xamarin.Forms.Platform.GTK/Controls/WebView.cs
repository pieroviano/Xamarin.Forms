using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gtk;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public interface IWebView
	{
		string Uri { get; set; }
		bool IsAvailable { get; }
		void Navigate(string uri);
		void LoadHTML(string html, string baseUrl);
		bool CanGoBack();
		void GoBack();
		bool CanGoForward();
		void GoForward();
		void Reload();
		void ExecuteScript(string script);
		Task<string> EvaluateJavaScriptAsync(string script);
		event EventHandler LoadStarted;
		event EventHandler LoadFinished;
		event EventHandler<string> LoadFailed;
	}

	/// <summary>
	/// Hosts a native <c>WebKitWebView</c> (WebKit2GTK) inside a GTK 3 widget tree.
	///
	/// The WebKit widget is created and owned natively — see <see cref="WebKit2"/> — and parented
	/// with <c>gtk_container_add</c> rather than through GtkSharp, because GtkSharp has no wrapper
	/// for the <c>WebKitWebView</c> GType. An <see cref="EventBox"/> is used as the managed host so
	/// the subtree owns its own GdkWindow.
	///
	/// When <c>libwebkit2gtk-4.1.so.0</c> cannot be loaded the widget degrades to a placeholder
	/// label; it never throws out of the constructor, so a missing runtime dependency does not take
	/// the application down.
	/// </summary>
	public class WebView : EventBox, IWebView
	{
		IntPtr _webView;
		Gtk.Label _placeholder;
		bool _disposed;

		// GObject stores only the unmanaged thunk for a connected signal, so the managed delegates
		// must be rooted here for as long as the native widget lives.
		WebKit2.LoadChangedHandler _loadChangedHandler;
		WebKit2.LoadFailedHandler _loadFailedHandler;

		public event EventHandler LoadStarted;
		public event EventHandler LoadFinished;
		public event EventHandler<string> LoadFailed;

		public WebView()
		{
			BuildWebView();
		}

		/// <summary>True when WebKit2GTK was found and the native view was created.</summary>
		public bool IsAvailable => _webView != IntPtr.Zero;

		/// <summary>Raw <c>WebKitWebView*</c>, or <see cref="IntPtr.Zero"/> when unavailable.</summary>
		public IntPtr NativeWebView => _webView;

		public string Uri
		{
			get
			{
				if (_webView == IntPtr.Zero)
					return string.Empty;

				return WebKit2.Utf8ToString(WebKit2.webkit_web_view_get_uri(_webView)) ?? string.Empty;
			}
			set { Navigate(value); }
		}

		public string Title
		{
			get
			{
				if (_webView == IntPtr.Zero)
					return string.Empty;

				return WebKit2.Utf8ToString(WebKit2.webkit_web_view_get_title(_webView)) ?? string.Empty;
			}
		}

		public void Navigate(string uri)
		{
			if (_webView == IntPtr.Zero || string.IsNullOrEmpty(uri))
				return;

			IntPtr native = WebKit2.StringToUtf8(ToAbsoluteUri(uri));

			try
			{
				WebKit2.webkit_web_view_load_uri(_webView, native);
			}
			finally
			{
				WebKit2.FreeUtf8(native);
			}
		}

		public void LoadHTML(string html, string baseUrl)
		{
			if (_webView == IntPtr.Zero)
				return;

			IntPtr nativeHtml = WebKit2.StringToUtf8(html ?? string.Empty);
			IntPtr nativeBase = string.IsNullOrEmpty(baseUrl) ? IntPtr.Zero : WebKit2.StringToUtf8(baseUrl);

			try
			{
				WebKit2.webkit_web_view_load_html(_webView, nativeHtml, nativeBase);
			}
			finally
			{
				WebKit2.FreeUtf8(nativeHtml);
				WebKit2.FreeUtf8(nativeBase);
			}
		}

		public bool CanGoBack()
		{
			return _webView != IntPtr.Zero && WebKit2.webkit_web_view_can_go_back(_webView);
		}

		public void GoBack()
		{
			if (_webView != IntPtr.Zero)
				WebKit2.webkit_web_view_go_back(_webView);
		}

		public bool CanGoForward()
		{
			return _webView != IntPtr.Zero && WebKit2.webkit_web_view_can_go_forward(_webView);
		}

		public void GoForward()
		{
			if (_webView != IntPtr.Zero)
				WebKit2.webkit_web_view_go_forward(_webView);
		}

		public void Reload()
		{
			if (_webView != IntPtr.Zero)
				WebKit2.webkit_web_view_reload(_webView);
		}

		public void StopLoading()
		{
			if (_webView != IntPtr.Zero)
				WebKit2.webkit_web_view_stop_loading(_webView);
		}

		/// <summary>Fire-and-forget script execution (the Forms <c>Eval</c> contract).</summary>
		public void ExecuteScript(string script)
		{
			if (_webView == IntPtr.Zero || string.IsNullOrEmpty(script))
				return;

			JavaScriptEvaluator.Evaluate(_webView, script, null);
		}

		/// <summary>Runs <paramref name="script"/> and completes with its result converted to a string.</summary>
		public Task<string> EvaluateJavaScriptAsync(string script)
		{
			var tcs = new TaskCompletionSource<string>();

			if (_webView == IntPtr.Zero)
			{
				tcs.SetResult(null);
				return tcs.Task;
			}

			if (string.IsNullOrEmpty(script))
			{
				tcs.SetResult(null);
				return tcs.Task;
			}

			JavaScriptEvaluator.Evaluate(_webView, script, tcs);

			return tcs.Task;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;
				_webView = IntPtr.Zero;
				_loadChangedHandler = null;
				_loadFailedHandler = null;
			}

			base.Dispose(disposing);
		}

		void BuildWebView()
		{
			if (WebKit2.IsAvailable)
			{
				try
				{
					_webView = WebKit2.webkit_web_view_new();
				}
				catch (Exception ex)
				{
					Internals.Log.Warning("WebView", $"WebKit2GTK web view creation failed: {ex.Message}");
					_webView = IntPtr.Zero;
				}
			}

			if (_webView != IntPtr.Zero)
			{
				// gtk_container_add sinks the floating reference returned by webkit_web_view_new,
				// so the container owns the widget from here on and we must not unref it.
				WebKit2.gtk_container_add(Handle, _webView);
				WebKit2.gtk_widget_show(_webView);

				IntPtr settings = WebKit2.webkit_web_view_get_settings(_webView);

				if (settings != IntPtr.Zero)
					WebKit2.webkit_settings_set_enable_javascript(settings, true);

				ConnectSignals();

				// The native child is destroyed with the container; drop the pointer before
				// anything can dereference it.
				Destroyed += (sender, args) => _webView = IntPtr.Zero;
			}
			else
			{
				_placeholder = new Gtk.Label("WebView unavailable — libwebkit2gtk-4.1.so.0 could not be loaded.")
				{
					Wrap = true,
					Justify = Gtk.Justification.Center
				};

				Add(_placeholder);
			}

			ShowAll();
		}

		void ConnectSignals()
		{
			_loadChangedHandler = OnLoadChanged;
			_loadFailedHandler = OnLoadFailed;

			WebKit2.ConnectSignal(_webView, "load-changed", _loadChangedHandler);
			WebKit2.ConnectSignal(_webView, "load-failed", _loadFailedHandler);
		}

		void OnLoadChanged(IntPtr webView, int loadEvent, IntPtr userData)
		{
			try
			{
				if (loadEvent == WebKit2.LoadStarted)
					LoadStarted?.Invoke(this, EventArgs.Empty);
				else if (loadEvent == WebKit2.LoadFinished)
					LoadFinished?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				// Never let a managed exception unwind into the GTK main loop.
				Internals.Log.Warning("WebView", $"load-changed handler failed: {ex}");
			}
		}

		bool OnLoadFailed(IntPtr webView, int loadEvent, IntPtr failingUri, IntPtr error, IntPtr userData)
		{
			try
			{
				string uri = WebKit2.Utf8ToString(failingUri);
				LoadFailed?.Invoke(this, uri);
			}
			catch (Exception ex)
			{
				Internals.Log.Warning("WebView", $"load-failed handler failed: {ex}");
			}

			// false: let WebKit show its own error page.
			return false;
		}

		/// <summary>
		/// Turns a bare relative path into a <c>file:</c> URI rooted at the application directory,
		/// mirroring what the other backends do for <c>UrlWebViewSource</c> values that are not
		/// absolute URIs. Absolute URIs (http, https, file, data, about, …) pass through untouched.
		/// </summary>
		static string ToAbsoluteUri(string uri)
		{
			if (System.Uri.IsWellFormedUriString(uri, UriKind.Absolute))
				return uri;

			System.Uri parsed;

			if (System.Uri.TryCreate(uri, UriKind.Absolute, out parsed))
				return uri;

			try
			{
				string location = typeof(WebView).GetTypeInfo().Assembly.Location;
				string root = string.IsNullOrEmpty(location)
					? System.IO.Directory.GetCurrentDirectory()
					: System.IO.Path.GetDirectoryName(location);
				string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(root ?? string.Empty, uri));

				return new System.Uri(full).AbsoluteUri;
			}
			catch (Exception)
			{
				return uri;
			}
		}

		/// <summary>
		/// Marshalling for <c>webkit_web_view_evaluate_javascript</c>'s <c>GAsyncReadyCallback</c>.
		///
		/// The completion source is handed to native code as a <see cref="GCHandle"/> and the
		/// callback delegate is rooted in a static field, because the call is asynchronous and
		/// nothing on the managed side keeps either alive otherwise.
		/// </summary>
		static class JavaScriptEvaluator
		{
			static readonly WebKit2.AsyncReadyHandler Callback = OnReady;

			// webkit_web_view_evaluate_javascript arrived in WebKitGTK 2.40; fall back to the older
			// run_javascript on runtimes that predate it.
			static bool _useLegacyApi;

			internal static void Evaluate(IntPtr webView, string script, TaskCompletionSource<string> tcs)
			{
				IntPtr nativeScript = WebKit2.StringToUtf8(script);
				GCHandle handle = tcs != null ? GCHandle.Alloc(tcs) : default(GCHandle);
				IntPtr userData = tcs != null ? GCHandle.ToIntPtr(handle) : IntPtr.Zero;

				try
				{
					if (!_useLegacyApi)
					{
						try
						{
							WebKit2.webkit_web_view_evaluate_javascript(webView, nativeScript, new IntPtr(-1),
								IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, tcs != null ? Callback : null, userData);

							return;
						}
						catch (EntryPointNotFoundException)
						{
							_useLegacyApi = true;
						}
					}

					WebKit2.webkit_web_view_run_javascript(webView, nativeScript, IntPtr.Zero,
						tcs != null ? Callback : null, userData);
				}
				catch (Exception ex)
				{
					if (tcs != null)
					{
						handle.Free();
						tcs.TrySetResult(null);
					}

					Internals.Log.Warning("WebView", $"JavaScript evaluation failed: {ex.Message}");
				}
				finally
				{
					WebKit2.FreeUtf8(nativeScript);
				}
			}

			static void OnReady(IntPtr sourceObject, IntPtr result, IntPtr userData)
			{
				TaskCompletionSource<string> tcs = null;

				try
				{
					if (userData == IntPtr.Zero)
						return;

					GCHandle handle = GCHandle.FromIntPtr(userData);
					tcs = handle.Target as TaskCompletionSource<string>;
					handle.Free();

					if (tcs == null)
						return;

					tcs.TrySetResult(ReadResult(sourceObject, result));
				}
				catch (Exception ex)
				{
					Internals.Log.Warning("WebView", $"JavaScript callback failed: {ex}");
					tcs?.TrySetResult(null);
				}
			}

			static string ReadResult(IntPtr webView, IntPtr asyncResult)
			{
				IntPtr error;
				IntPtr value;
				IntPtr jsResult = IntPtr.Zero;

				if (_useLegacyApi)
				{
					jsResult = WebKit2.webkit_web_view_run_javascript_finish(webView, asyncResult, out error);
					value = jsResult == IntPtr.Zero
						? IntPtr.Zero
						: WebKit2.webkit_javascript_result_get_js_value(jsResult);
				}
				else
				{
					value = WebKit2.webkit_web_view_evaluate_javascript_finish(webView, asyncResult, out error);
				}

				if (value == IntPtr.Zero)
				{
					string message = WebKit2.TakeErrorMessage(error);

					if (!string.IsNullOrEmpty(message))
						Internals.Log.Warning("WebView", $"JavaScript evaluation failed: {message}");

					return null;
				}

				try
				{
					if (WebKit2.jsc_value_is_undefined(value) || WebKit2.jsc_value_is_null(value))
						return null;

					IntPtr text = WebKit2.jsc_value_to_string(value);

					try
					{
						return WebKit2.Utf8ToString(text);
					}
					finally
					{
						WebKit2.g_free(text);
					}
				}
				finally
				{
					if (jsResult != IntPtr.Zero)
						WebKit2.webkit_javascript_result_unref(jsResult);
					else
						WebKit2.g_object_unref(value);
				}
			}
		}
	}
}
