using System;
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
	/// Hosts a native <see cref="WebKit.WebView"/> inside the GTK widget tree.
	/// </summary>
	/// <remarks>
	/// <para>WebKitGTK <b>6.0</b>, through Net4x.WebkitGtkSharp. The Gtk 3 version of this file
	/// bound WebKit2GTK 4.1 by hand — 263 lines of lazily-resolved P/Invoke — because GtkSharp had
	/// no wrapper for the <c>WebKitWebView</c> GType. Neither half of that survives the move:
	/// WebKit2GTK 4.1 links against <b>GTK 3</b> and cannot be embedded in a GTK 4 hierarchy at
	/// all, and its GTK 4 successor is bound properly, so the widget is now an ordinary managed
	/// object parented like any other.</para>
	/// <para>The degrade-to-placeholder behaviour is kept: when the WebKitGTK runtime is absent the
	/// widget shows a label instead and never throws out of its constructor, so a missing optional
	/// dependency does not take the application down. The detection is a guarded construction
	/// rather than a library probe — <c>GLibrary</c> is internal to the binding, and a missing
	/// native export surfaces as a null delegate at the first call, which construction is.</para>
	/// </remarks>
	public class WebView : EventBox, IWebView
	{
		WebKit.WebView _webView;
		Gtk.Label _placeholder;
		bool _disposed;

		public event EventHandler LoadStarted;
		public event EventHandler LoadFinished;
		public event EventHandler<string> LoadFailed;

		public WebView()
		{
			BuildWebView();
		}

		/// <summary>True when WebKitGTK was found and the native view was created.</summary>
		public bool IsAvailable => _webView != null;

		/// <summary>The native web view, or null when unavailable.</summary>
		public WebKit.WebView NativeWebView => _webView;

		public string Uri
		{
			get { return _webView?.Uri ?? string.Empty; }
			set { Navigate(value); }
		}

		public string Title
		{
			get { return _webView?.Title ?? string.Empty; }
		}

		public void Navigate(string uri)
		{
			if (_webView == null || string.IsNullOrEmpty(uri))
				return;

			_webView.LoadUri(ToAbsoluteUri(uri));
		}

		public void LoadHTML(string html, string baseUrl)
		{
			if (_webView == null)
				return;

			_webView.LoadHtml(html ?? string.Empty, string.IsNullOrEmpty(baseUrl) ? null : baseUrl);
		}

		public bool CanGoBack()
		{
			return _webView != null && _webView.CanGoBack();
		}

		public void GoBack()
		{
			_webView?.GoBack();
		}

		public bool CanGoForward()
		{
			return _webView != null && _webView.CanGoForward();
		}

		public void GoForward()
		{
			_webView?.GoForward();
		}

		public void Reload()
		{
			_webView?.Reload();
		}

		public void StopLoading()
		{
			_webView?.StopLoading();
		}

		/// <summary>Fire-and-forget script execution (the Forms <c>Eval</c> contract).</summary>
		public void ExecuteScript(string script)
		{
			if (_webView == null || string.IsNullOrEmpty(script))
				return;

			Evaluate(script, null);
		}

		/// <summary>Runs <paramref name="script"/> and completes with its result as a string.</summary>
		public Task<string> EvaluateJavaScriptAsync(string script)
		{
			var tcs = new TaskCompletionSource<string>();

			if (_webView == null || string.IsNullOrEmpty(script))
			{
				tcs.SetResult(null);
				return tcs.Task;
			}

			Evaluate(script, tcs);

			return tcs.Task;
		}

		/// <remarks>
		/// <c>webkit_web_view_evaluate_javascript</c>, which replaced
		/// <c>webkit_web_view_run_javascript</c>. The result is a JavaScriptCore value rather than
		/// a WebKitJavascriptResult, so there is no separate unwrapping step any more.
		///
		/// The completion callback is rooted for the duration of the call: GLib keeps only the
		/// unmanaged thunk, so a delegate the GC can collect between the call and the callback is a
		/// crash in the main loop.
		/// </remarks>
		void Evaluate(string script, TaskCompletionSource<string> completion)
		{
			GLib.AsyncReadyCallback callback = null;

			callback = (source, result, data) =>
			{
				GC.KeepAlive(callback);

				try
				{
					var value = _webView.EvaluateJavascriptFinish(result);

					completion?.SetResult(value == null || value.IsUndefined || value.IsNull
						? null
						: value.ToString());
				}
				catch (Exception ex)
				{
					// A script that throws is a normal outcome, not a fault of the host: report it
					// to the awaiting caller and never let it unwind into the GTK main loop.
					Internals.Log.Warning("WebView", $"JavaScript evaluation failed: {ex.Message}");
					completion?.SetResult(null);
				}
			};

			_webView.EvaluateJavascript(script, null, null, null, callback);
		}

		/// <remarks>
		/// Destroy, not a Destroyed event: Gtk 4 removed the ::destroy signal. See
		/// Controls/OpenGLView.Destroy.
		/// </remarks>
		public override void Destroy()
		{
			DetachWebView();

			base.Destroy();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;
				DetachWebView();
			}

			base.Dispose(disposing);
		}

		void DetachWebView()
		{
			if (_webView == null)
				return;

			_webView.LoadChanged -= OnLoadChanged;
			_webView.LoadFailed -= OnLoadFailed;
			_webView = null;
		}

		void BuildWebView()
		{
			try
			{
				_webView = new WebKit.WebView();
			}
			catch (Exception ex)
			{
				// A missing WebKitGTK surfaces here: FuncLoader hands back a null delegate for an
				// export it cannot resolve, so the first call through it - construction - throws.
				Internals.Log.Warning("WebView", $"WebKitGTK web view creation failed: {ex.Message}");
				_webView = null;
			}

			if (_webView != null)
			{
				Add(_webView);

				var settings = _webView.Settings;

				if (settings != null)
					settings.EnableJavascript = true;

				_webView.LoadChanged += OnLoadChanged;
				_webView.LoadFailed += OnLoadFailed;
			}
			else
			{
				_placeholder = new Gtk.Label("WebView unavailable — libwebkitgtk-6.0.so.4 could not be loaded.")
				{
					Wrap = true,
					Justify = Gtk.Justification.Center
				};

				Add(_placeholder);
			}

			ShowAll();
		}

		void OnLoadChanged(object o, WebKit.LoadChangedArgs args)
		{
			try
			{
				if (args.LoadEvent == WebKit.LoadEvent.Started)
					LoadStarted?.Invoke(this, EventArgs.Empty);
				else if (args.LoadEvent == WebKit.LoadEvent.Finished)
					LoadFinished?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				// Never let a managed exception unwind into the GTK main loop.
				Internals.Log.Warning("WebView", $"load-changed handler failed: {ex}");
			}
		}

		void OnLoadFailed(object o, WebKit.LoadFailedArgs args)
		{
			try
			{
				LoadFailed?.Invoke(this, args.FailingUri);
			}
			catch (Exception ex)
			{
				Internals.Log.Warning("WebView", $"load-failed handler failed: {ex}");
			}

			// false: let WebKit show its own error page.
			args.RetVal = false;
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
				return parsed.AbsoluteUri;

			var basePath = System.IO.Path.GetDirectoryName(
				typeof(WebView).Assembly.Location ?? string.Empty) ?? string.Empty;
			var combined = System.IO.Path.Combine(basePath, uri ?? string.Empty);

			return new System.Uri(combined).AbsoluteUri;
		}
	}
}
