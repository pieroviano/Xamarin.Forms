using NativeView = Gtk.Widget;

namespace Xamarin.Forms.Platform.GTK
{
	public abstract class ViewRenderer : ViewRenderer<View, NativeView>
	{

	}

	public abstract class ViewRenderer<TView, TNativeView> : VisualElementRenderer<TView, TNativeView>
		where TView : View where TNativeView : NativeView
	{
		private string _defaultAccessibilityLabel;
		private string _defaultAccessibilityHint;

		/// <remarks>
		/// The <c>disposing</c> guard is not tidiness. Without it this ran from the finalizer too,
		/// and <c>Gtk.Widget.Destroy</c> reads <c>Parent</c>, which asks GtkSharp to produce a
		/// managed wrapper for the parent's native handle. On the finalizer thread that peer may
		/// already be gone, so GtkSharp tries to construct a fresh one and throws
		/// <c>GLib.MissingIntPtrCtorException</c> - no renderer declares the (IntPtr) constructor
		/// that would need - and an exception on the finalizer thread takes the process with it.
		/// MEASURED: the gallery died on exactly that, via ImageRenderer.
		///
		/// Only <c>disposing == true</c> may touch other objects; that is the IDisposable contract,
		/// and it is what <c>ImageRenderer.Dispose</c> one level down already does. The native widget
		/// needs no finalizer-time help here in any case - GTK owns it, and its parent drops it when
		/// the tree goes away.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			if (disposing && Control != null)
			{
				Control.Destroy();
				Control = null;
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<TView> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				UpdateBackgroundColor();
			}
		}

		protected override void SetNativeControl(TNativeView view)
		{
			base.SetNativeControl(view);

			Add(view);
		}

		protected override void SetAccessibilityHint()
		{
			if (Control == null)
			{
				base.SetAccessibilityHint();
				return;
			}

			if (Element == null)
				return;

			if (_defaultAccessibilityHint == null)
				_defaultAccessibilityHint = Control.Accessible.Name;

			var helpText = (string)Element.GetValue(AutomationProperties.HelpTextProperty) ?? _defaultAccessibilityHint;

			if (!string.IsNullOrEmpty(helpText))
			{
				Control.Accessible.Name = helpText;
			}
		}

		protected override void SetAccessibilityLabel()
		{
			if (Control == null)
			{
				base.SetAccessibilityLabel();
				return;
			}

			if (Element == null)
				return;

			if (_defaultAccessibilityLabel == null)
				_defaultAccessibilityLabel = Control.Accessible.Description;

			var name = (string)Element.GetValue(AutomationProperties.NameProperty) ?? _defaultAccessibilityLabel;

			if (!string.IsNullOrEmpty(name))
			{
				Control.Accessible.Description = name;
			}
		}
	}
}
