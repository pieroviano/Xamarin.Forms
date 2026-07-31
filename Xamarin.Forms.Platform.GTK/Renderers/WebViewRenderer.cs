using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class WebViewRenderer : ViewRenderer<WebView, Controls.WebView>, IWebViewDelegate, IEffectControlProvider
	{
		private bool _disposed;
		private bool _ignoreSourceChanges;
		private WebNavigationEvent _lastBackForwardEvent;
		private WebNavigationEvent _lastEvent;

		IWebViewController WebViewController => Element;

		void IEffectControlProvider.RegisterEffect(Effect effect)
		{
			var platformEffect = effect as PlatformEffect;
			if (platformEffect != null)
				platformEffect.SetContainer(Container);
		}

		void IWebViewDelegate.LoadHtml(string html, string baseUrl)
		{
			try
			{
				if (string.IsNullOrEmpty(html))
				{
					var urlWebViewSource = Element.Source as HtmlWebViewSource;

					if (urlWebViewSource != null)
					{
						html = urlWebViewSource.Html;
					}
				}

				if (Control != null)
				{
					Control.LoadHTML(html, baseUrl ?? string.Empty);
				}
			}
			catch (Exception ex)
			{
				Log.Warning("WebView load string", $"WebView load string failed: {ex}");
			}
		}

		void IWebViewDelegate.LoadUrl(string url)
		{
			try
			{
				if (string.IsNullOrEmpty(url))
				{
					var urlWebViewSource = Element.Source as UrlWebViewSource;

					if (urlWebViewSource != null)
					{
						url = urlWebViewSource.Url;
					}
				}

				if (Control != null)
				{
					Control.Navigate(url);
				}
			}
			catch (Exception ex)
			{
				Log.Warning("WebView load url", $"WebView load url failed: {ex}");
			}
		}

		protected override void OnElementChanged(ElementChangedEventArgs<WebView> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					Controls.WebView webView = null;

					try
					{
						// WebKit2GTK, bound by hand in Controls/WebKit2.cs. The control falls back to a
						// placeholder label instead of throwing when libwebkit2gtk-4.1 is not installed,
						// so a missing runtime dependency degrades rather than aborting the app.
						webView = new Controls.WebView();
					}
					catch (Exception ex)
					{
						Log.Warning("WebView loading", $"WebView load failed: {ex}");
					}

					if (webView == null)
						return;

					SetNativeControl(webView);

					Control.LoadStarted += OnLoadStarted;
					Control.LoadFinished += OnLoadFinished;
					Control.LoadFailed += OnLoadFailed;

					WebViewController.EvalRequested += OnEvalRequested;
					WebViewController.EvaluateJavaScriptRequested += OnEvaluateJavaScriptRequested;
					WebViewController.GoBackRequested += OnGoBackRequested;
					WebViewController.GoForwardRequested += OnGoForwardRequested;
					WebViewController.ReloadRequested += OnReloadRequested;
				}
			}

			Load();

			EffectUtilities.RegisterEffectControlProvider(this, e.OldElement, e.NewElement);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == WebView.SourceProperty.PropertyName)
				Load();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				if (Control != null)
				{
					Control.LoadStarted -= OnLoadStarted;
					Control.LoadFinished -= OnLoadFinished;
					Control.LoadFailed -= OnLoadFailed;
				}

				if (WebViewController != null)
				{
					WebViewController.EvalRequested -= OnEvalRequested;
					WebViewController.EvaluateJavaScriptRequested -= OnEvaluateJavaScriptRequested;
					WebViewController.GoBackRequested -= OnGoBackRequested;
					WebViewController.GoForwardRequested -= OnGoForwardRequested;
					WebViewController.ReloadRequested -= OnReloadRequested;
				}
			}

			base.Dispose(disposing);
		}

		private void Load()
		{
			if (_ignoreSourceChanges)
				return;

			Element?.Source?.Load(this);

			UpdateCanGoBackForward();
		}

		private void UpdateCanGoBackForward()
		{
			if (Element == null)
				return;

			if (Control != null)
			{
				WebViewController.CanGoBack = Control.CanGoBack();
				WebViewController.CanGoForward = Control.CanGoForward();
			}
		}

		private void OnLoadStarted(object sender, EventArgs e)
		{
			var uri = Control.Uri;

			if (!string.IsNullOrEmpty(uri))
			{
				var args = new WebNavigatingEventArgs(_lastEvent, new UrlWebViewSource { Url = uri }, uri);

				Element.SendNavigating(args);

				if (args.Cancel)
				{
					// WebKit2 decides navigation policy through "decide-policy", which is not bound;
					// stopping the load is the closest approximation available here.
					Control.StopLoading();
					_lastEvent = WebNavigationEvent.NewPage;
				}
			}
		}

		private void OnLoadFinished(object o, EventArgs args)
		{
			if (Control == null)
			{
				return;
			}

			_ignoreSourceChanges = true;
			ElementController?.SetValueFromRenderer(WebView.SourceProperty,
				new UrlWebViewSource { Url = Control.Uri });
			_ignoreSourceChanges = false;

			_lastEvent = _lastBackForwardEvent;
			WebViewController?.SendNavigated(new WebNavigatedEventArgs(
				_lastEvent,
				Element?.Source,
				Control.Uri,
				WebNavigationResult.Success));

			UpdateCanGoBackForward();
		}

		private void OnLoadFailed(object sender, string failingUri)
		{
			if (Control == null)
			{
				return;
			}

			WebViewController?.SendNavigated(new WebNavigatedEventArgs(
				_lastEvent,
				Element?.Source,
				failingUri,
				WebNavigationResult.Failure));

			UpdateCanGoBackForward();
		}

		private void OnEvalRequested(object sender, EvalRequested eventArg)
		{
			if (Control != null)
			{
				Control.ExecuteScript(eventArg?.Script);
			}
		}

		Task<string> OnEvaluateJavaScriptRequested(string script)
		{
			if (Control == null)
			{
				return Task.FromResult<string>(null);
			}

			return Control.EvaluateJavaScriptAsync(script);
		}

		private void OnGoBackRequested(object sender, EventArgs eventArgs)
		{
			if (Control == null)
			{
				return;
			}

			if (Control.CanGoBack())
			{
				_lastBackForwardEvent = WebNavigationEvent.Back;
				Control.GoBack();
			}

			UpdateCanGoBackForward();
		}

		private void OnGoForwardRequested(object sender, EventArgs eventArgs)
		{
			if (Control == null)
			{
				return;
			}

			if (Control.CanGoForward())
			{
				_lastBackForwardEvent = WebNavigationEvent.Forward;
				Control.GoForward();
			}

			UpdateCanGoBackForward();
		}

		void OnReloadRequested(object sender, EventArgs eventArgs)
		{
			Control?.Reload();
		}
	}
}
