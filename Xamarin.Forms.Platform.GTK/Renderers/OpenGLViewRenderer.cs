using System;
using System.ComponentModel;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	/// <summary>
	/// Maps <c>Xamarin.Forms.OpenGLView</c> onto <see cref="Controls.OpenGLView"/>, which wraps
	/// <c>Gtk.GLArea</c> (plan M6 §9.2).
	///
	/// Two drawing modes, exactly as on the other backends:
	/// <list type="bullet">
	/// <item><c>HasRenderLoop == true</c>: the native view redraws off the widget's frame clock.</item>
	/// <item><c>HasRenderLoop == false</c>: a frame is produced only when the element raises
	/// <c>DisplayRequested</c> (i.e. the application calls <c>OpenGLView.Display()</c>).</item>
	/// </list>
	///
	/// <c>OpenGLView.OnDisplay</c> is a plain CLR property that the application can replace at any
	/// time, so it is read through a thunk on every frame rather than captured once.
	/// </summary>
	public class OpenGLViewRenderer : ViewRenderer<OpenGLView, Controls.OpenGLView>
	{
		bool _disposed;

		protected override void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				_disposed = true;

				if (Element != null)
					((IOpenGlViewController)Element).DisplayRequested -= OnDisplayRequested;

				if (Control != null)
				{
					Control.HasRenderLoop = false;
					Control.OnDisplay = null;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<OpenGLView> e)
		{
			if (e.OldElement != null)
				((IOpenGlViewController)e.OldElement).DisplayRequested -= OnDisplayRequested;

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var openGlView = new Controls.OpenGLView();
					// Read the Forms callback per frame: OnDisplay is not a BindableProperty, so
					// there is no change notification to re-subscribe on when it is reassigned.
					openGlView.OnDisplay = OnRenderFrame;
					SetNativeControl(openGlView);
				}

				((IOpenGlViewController)e.NewElement).DisplayRequested += OnDisplayRequested;

				UpdateRenderLoop();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == OpenGLView.HasRenderLoopProperty.PropertyName)
				UpdateRenderLoop();
		}

		void OnRenderFrame(Rectangle rectangle)
		{
			if (_disposed)
				return;

			Element?.OnDisplay?.Invoke(rectangle);
		}

		void OnDisplayRequested(object sender, EventArgs eventArgs)
		{
			if (_disposed || Control == null)
				return;

			// With a render loop running the frame clock is already producing frames, so an explicit
			// Display() has nothing to add.
			if (Element != null && Element.HasRenderLoop)
				return;

			Control.RequestRender();
		}

		void UpdateRenderLoop()
		{
			if (Control == null || Element == null)
				return;

			Control.HasRenderLoop = Element.HasRenderLoop;

			// Leaving (or never entering) the render loop still owes the caller one frame, otherwise
			// an OpenGLView that only ever draws once would stay empty.
			if (!Element.HasRenderLoop)
				Control.RequestRender();
		}
	}
}
