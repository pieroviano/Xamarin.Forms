using System;
using System.Runtime.InteropServices;
using Gtk;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	/// <summary>
	/// Hosts a <see cref="Gtk.GLArea"/> — the GTK 3.16+ widget that owns an OpenGL context and
	/// composites its framebuffer into the widget tree — and turns its <c>::render</c> signal into
	/// the <see cref="OnDisplay"/> callback that <c>Xamarin.Forms.OpenGLView</c> expects.
	///
	/// This replaces the OpenTK 3 <c>GLWidget</c> that used to live in <c>Controls/GLWidget/**</c>:
	/// that widget drove GLX/WGL/AGL by hand through per-platform "window info" initializers, all of
	/// which are GTK 2 era code that cannot work on GTK 3 (a GTK 3 widget owns no drawable of its
	/// own and <c>gdk_x11_drawable_get_xid</c> is gone). <c>Gtk.GLArea</c> owns the context, so no
	/// windowing-system binding is needed and the OpenTK dependency is dropped entirely.
	///
	/// Hosted inside an <see cref="EventBox"/> with a real GdkWindow so the GL surface owns its own
	/// window and cannot be painted over by a Forms sibling (see the layering note in
	/// <c>Controls/WebView.cs</c>; GLArea itself is a no-window widget).
	///
	/// If a GL context cannot be created — no GLX/EGL, a headless display with no GL driver, a
	/// container without <c>/dev/dri</c> — GTK stores a <c>GError</c> on the area, never emits
	/// <c>::render</c>, and draws its own error text instead. Nothing throws:
	/// <see cref="IsAvailable"/> reports the failure and the render loop is not started, so an
	/// application embedding an <c>OpenGLView</c> stays alive on a machine with no usable GL.
	/// </summary>
	public class OpenGLView : EventBox
	{
		readonly GLArea _glArea;

		Action<Rectangle> _onDisplay;
		bool _hasRenderLoop;
		uint _tickCallbackId;
		bool _disposed;

		public OpenGLView()
		{
			VisibleWindow = true;

			_glArea = new GLArea
			{
				// Set before realization: the context is created with these attributes.
				HasDepthBuffer = true,
				HasStencilBuffer = false,
				HasAlpha = false,
				// We decide when a frame is produced: either the Forms render loop (a frame-clock
				// tick callback) or an explicit Display() call. Without AutoRender the last frame is
				// simply re-composited when the widget is redrawn, which is exactly what we want.
				AutoRender = false,
				CanFocus = true
			};

			_glArea.Render += OnGLAreaRender;
			_glArea.Realized += OnGLAreaRealized;

			Add(_glArea);
			_glArea.Show();
		}

		/// <summary>
		/// The Forms-side draw callback (<c>Xamarin.Forms.OpenGLView.OnDisplay</c>). It is invoked
		/// from the <c>::render</c> signal with the GL context already current, and is handed the
		/// area's allocation.
		/// </summary>
		public Action<Rectangle> OnDisplay
		{
			get { return _onDisplay; }
			set { _onDisplay = value; }
		}

		/// <summary>
		/// Drives a continuous redraw off the widget's frame clock while true. Mirrors
		/// <c>Xamarin.Forms.OpenGLView.HasRenderLoop</c>.
		/// </summary>
		public bool HasRenderLoop
		{
			get { return _hasRenderLoop; }
			set
			{
				if (_hasRenderLoop == value)
					return;

				_hasRenderLoop = value;

				if (_hasRenderLoop)
					StartRenderLoop();
				else
					StopRenderLoop();
			}
		}

		/// <summary>
		/// True once a GL context has been created for the area. False before realization, and false
		/// for good if context creation failed — see <see cref="LastError"/>.
		/// </summary>
		public bool IsAvailable
		{
			get
			{
				if (_disposed || _glArea == null)
					return false;

				return _glArea.Error == IntPtr.Zero && _glArea.Context != null;
			}
		}

		/// <summary>
		/// The message of the <c>GError</c> GTK stored when context creation failed, or null.
		/// </summary>
		/// <remarks>
		/// The <c>GError</c> belongs to the GLArea and stays alive for as long as the widget does:
		/// <c>gtk_gl_area_get_error</c> is transfer-none, and GTK reads it back on every draw to
		/// paint its GL error screen. It must therefore be read without taking ownership — wrapping
		/// it in <c>GLib.GException</c>/<c>GLib.Error</c> hands the pointer to a managed owner that
		/// frees it, after which GTK's own draw path reads freed memory (observed: the error text
		/// disappears, then the process dies). So the struct is unpacked by hand:
		/// <c>struct GError { GQuark domain; gint code; gchar *message; }</c>.
		/// </remarks>
		public string LastError
		{
			get
			{
				if (_disposed || _glArea == null)
					return null;

				IntPtr error = _glArea.Error;

				if (error == IntPtr.Zero)
					return null;

				// domain (guint32) + code (gint32) = 8 bytes, then the message pointer.
				return WebKit2.Utf8ToString(Marshal.ReadIntPtr(error, 8))
					?? "OpenGL context creation failed.";
			}
		}

		/// <summary>The underlying GTK widget, exposed for tests and for advanced hosting.</summary>
		public GLArea GLArea
		{
			get { return _glArea; }
		}

		/// <summary>Number of frames actually produced. Used by the runtime smoke tests.</summary>
		public int RenderCount { get; private set; }

		/// <summary>
		/// Requests exactly one frame. This is what <c>Xamarin.Forms.OpenGLView.Display()</c> maps to.
		/// Not named <c>Display</c>: <c>Gtk.Widget.Display</c> is the GdkDisplay property.
		/// </summary>
		public void RequestRender()
		{
			if (_disposed || _glArea == null)
				return;

			_glArea.QueueRender();
		}

		void OnGLAreaRealized(object sender, EventArgs e)
		{
			// Realization is where GDK actually creates the context; a failure lands in
			// gtk_gl_area_get_error rather than in an exception.
			if (!IsAvailable)
			{
				Internals.Log.Warning("OpenGLView",
					"No OpenGL context could be created for this GLArea; the view shows GTK's GL "
					+ "error placeholder. " + (LastError ?? string.Empty));

				StopRenderLoop();
				return;
			}

			if (_hasRenderLoop)
				StartRenderLoop();
		}

		void OnGLAreaRender(object o, RenderArgs args)
		{
			// TRUE == "handled"; GtkGLArea uses a true-handled signal accumulator.
			args.RetVal = true;

			if (_disposed)
				return;

			// GTK makes the context current before emitting ::render, but be explicit: an OnDisplay
			// handler is free to bind another context, and the next frame must not inherit it.
			_glArea.MakeCurrent();

			if (_glArea.Error != IntPtr.Zero)
				return;

			RenderCount++;

			Action<Rectangle> onDisplay = _onDisplay;

			if (onDisplay == null)
				return;

			Gdk.Rectangle allocation = _glArea.Allocation;
			onDisplay(new Rectangle(0, 0, allocation.Width, allocation.Height));
		}

		void StartRenderLoop()
		{
			if (_disposed || _tickCallbackId != 0 || _glArea == null)
				return;

			// The frame clock only ticks while the widget is mapped, so this costs nothing when the
			// page is off-screen — unlike a bare GLib.Timeout, which would spin forever.
			_tickCallbackId = _glArea.AddTickCallback(OnTick);
		}

		void StopRenderLoop()
		{
			if (_tickCallbackId == 0 || _glArea == null)
				return;

			_glArea.RemoveTickCallback(_tickCallbackId);
			_tickCallbackId = 0;
		}

		bool OnTick(Widget widget, Gdk.FrameClock frameClock)
		{
			if (_disposed || !_hasRenderLoop)
			{
				_tickCallbackId = 0;
				return false;
			}

			_glArea.QueueRender();

			return true;
		}

		protected override void OnDestroyed()
		{
			Teardown();

			base.OnDestroyed();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Teardown();

			base.Dispose(disposing);
		}

		void Teardown()
		{
			if (_disposed)
				return;

			_disposed = true;

			StopRenderLoop();

			if (_glArea != null)
			{
				_glArea.Render -= OnGLAreaRender;
				_glArea.Realized -= OnGLAreaRealized;
			}

			_onDisplay = null;
		}
	}
}
