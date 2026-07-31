using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	/// <summary>
	/// Minimal hand-written P/Invoke binding to WebKit2GTK (<c>libwebkit2gtk-4.1.so.0</c>).
	///
	/// There is no maintained GtkSharp 3 binding for WebKit2GTK — webkit-sharp 1.1.15, which this
	/// backend used to reference, binds WebKit *1* and has been dead for over a decade. Only the
	/// handful of entry points the Forms <see cref="Xamarin.Forms.WebView"/> contract needs are
	/// bound here.
	///
	/// Every entry point is resolved lazily: <see cref="IsAvailable"/> probes the library once and
	/// the widget degrades to a placeholder label when WebKitGTK is not installed, rather than
	/// aborting the process on <c>Forms.Init</c>.
	/// </summary>
	internal static class WebKit2
	{
		// Full sonames on purpose: only the -dev packages ship the unversioned "libwebkit2gtk-4.1.so"
		// symlink, and the .NET native library loader probes the given name verbatim first.
		const string WebKitLib = "libwebkit2gtk-4.1.so.0";
		const string JavaScriptCoreLib = "libjavascriptcoregtk-4.1.so.0";
		const string GObjectLib = "libgobject-2.0.so.0";
		const string GLibLib = "libglib-2.0.so.0";
		const string GtkLib = "libgtk-3.so.0";

		// WebKitLoadEvent
		internal const int LoadStarted = 0;
		internal const int LoadRedirected = 1;
		internal const int LoadCommitted = 2;
		internal const int LoadFinished = 3;

		static bool? _available;

		internal static bool IsAvailable
		{
			get
			{
				if (!_available.HasValue)
				{
					try
					{
						_available = webkit_web_view_get_type() != IntPtr.Zero;
					}
					catch (DllNotFoundException)
					{
						_available = false;
					}
					catch (EntryPointNotFoundException)
					{
						_available = false;
					}
					catch (BadImageFormatException)
					{
						_available = false;
					}
				}

				return _available.Value;
			}
		}

		#region Delegates

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void LoadChangedHandler(IntPtr webView, int loadEvent, IntPtr userData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate bool LoadFailedHandler(IntPtr webView, int loadEvent, IntPtr failingUri, IntPtr error,
			IntPtr userData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void AsyncReadyHandler(IntPtr sourceObject, IntPtr result, IntPtr userData);

		#endregion

		#region WebKitWebView

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_web_view_get_type();

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_web_view_new();

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_load_uri(IntPtr webView, IntPtr uri);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_load_html(IntPtr webView, IntPtr content, IntPtr baseUri);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_web_view_get_uri(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_web_view_get_title(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_go_back(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_go_forward(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool webkit_web_view_can_go_back(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool webkit_web_view_can_go_forward(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_reload(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_stop_loading(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_web_view_get_settings(IntPtr webView);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_settings_set_enable_javascript(IntPtr settings, bool enabled);

		// WebKitGTK 2.40+. Superseded webkit_web_view_run_javascript, which is kept below as the
		// fallback for older runtimes (2.38 and earlier still ship on some LTS distros).
		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_evaluate_javascript(IntPtr webView, IntPtr script, IntPtr length,
			IntPtr worldName, IntPtr sourceUri, IntPtr cancellable, AsyncReadyHandler callback, IntPtr userData);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_web_view_evaluate_javascript_finish(IntPtr webView, IntPtr result,
			out IntPtr error);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_web_view_run_javascript(IntPtr webView, IntPtr script, IntPtr cancellable,
			AsyncReadyHandler callback, IntPtr userData);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_web_view_run_javascript_finish(IntPtr webView, IntPtr result,
			out IntPtr error);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr webkit_javascript_result_get_js_value(IntPtr jsResult);

		[DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void webkit_javascript_result_unref(IntPtr jsResult);

		#endregion

		#region JavaScriptCore

		[DllImport(JavaScriptCoreLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr jsc_value_to_string(IntPtr value);

		[DllImport(JavaScriptCoreLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool jsc_value_is_undefined(IntPtr value);

		[DllImport(JavaScriptCoreLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool jsc_value_is_null(IntPtr value);

		#endregion

		#region GLib / GObject / Gtk

		[DllImport(GObjectLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong g_signal_connect_data(IntPtr instance, IntPtr detailedSignal, IntPtr handler,
			IntPtr data, IntPtr destroyData, int connectFlags);

		[DllImport(GObjectLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void g_object_unref(IntPtr obj);

		[DllImport(GLibLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void g_free(IntPtr mem);

		[DllImport(GLibLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void g_error_free(IntPtr error);

		[DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void gtk_container_add(IntPtr container, IntPtr widget);

		[DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void gtk_widget_show(IntPtr widget);

		#endregion

		#region Helpers

		/// <summary>Connects <paramref name="handler"/> to a GObject signal on a raw instance.</summary>
		/// <remarks>
		/// The caller must keep the managed delegate alive for as long as the signal can fire —
		/// GObject stores only the unmanaged thunk, which the GC knows nothing about.
		/// </remarks>
		internal static void ConnectSignal(IntPtr instance, string signal, Delegate handler)
		{
			IntPtr name = StringToUtf8(signal);

			try
			{
				g_signal_connect_data(instance, name, Marshal.GetFunctionPointerForDelegate(handler), IntPtr.Zero,
					IntPtr.Zero, 0);
			}
			finally
			{
				FreeUtf8(name);
			}
		}

		/// <summary>Marshals a managed string into a NUL-terminated UTF-8 buffer (free with <see cref="FreeUtf8"/>).</summary>
		internal static IntPtr StringToUtf8(string value)
		{
			if (value == null)
				return IntPtr.Zero;

			byte[] bytes = Encoding.UTF8.GetBytes(value);
			IntPtr buffer = Marshal.AllocHGlobal(bytes.Length + 1);
			Marshal.Copy(bytes, 0, buffer, bytes.Length);
			Marshal.WriteByte(buffer, bytes.Length, 0);

			return buffer;
		}

		internal static void FreeUtf8(IntPtr buffer)
		{
			if (buffer != IntPtr.Zero)
				Marshal.FreeHGlobal(buffer);
		}

		/// <summary>Reads a NUL-terminated UTF-8 string owned by native code (does not free it).</summary>
		internal static string Utf8ToString(IntPtr value)
		{
			if (value == IntPtr.Zero)
				return null;

			int length = 0;

			while (Marshal.ReadByte(value, length) != 0)
				length++;

			if (length == 0)
				return string.Empty;

			byte[] bytes = new byte[length];
			Marshal.Copy(value, bytes, 0, length);

			return Encoding.UTF8.GetString(bytes);
		}

		/// <summary>Reads <c>GError.message</c>, then frees the error.</summary>
		internal static string TakeErrorMessage(IntPtr error)
		{
			if (error == IntPtr.Zero)
				return null;

			// struct GError { GQuark domain; gint code; gchar *message; }
			// GQuark and gint are both 32-bit, so the pointer starts at offset 8 on 64-bit and 32-bit alike.
			string message = Utf8ToString(Marshal.ReadIntPtr(error, 8));
			g_error_free(error);

			return message;
		}

		#endregion
	}
}
