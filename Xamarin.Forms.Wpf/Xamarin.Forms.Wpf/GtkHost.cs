using System;
using System.Windows;
using XFApplication = Xamarin.Forms.Application;

namespace Xamarin.Forms.Wpf
{
	/// <summary>
	/// Brings GTK and Xamarin.Forms up the first time a WPF element is made, on that thread - WPF's rule too: the
	/// thread that creates the first window is the UI thread (an STA thread on Windows, as GTK needs there).
	/// </summary>
	internal static class GtkHost
	{
		static bool s_initialized;

		/// <summary>The Xamarin.Forms application that holds the resources <c>{DynamicResource}</c> finds.</summary>
		internal static XFApplication FormsApplication { get; private set; }

		internal static void EnsureInitialized()
		{
			if (s_initialized)
				return;

			s_initialized = true;
			if (!Forms.IsInitialized)
			{
				Gtk.Application.Init();
				Forms.Init();
			}

			// The view that shows a GTK widget as it is: Forms.Init scans only the backend's own renderers.
			Internals.Registrar.Registered.Register(typeof(NativeHostView), typeof(NativeHostRenderer));

			// An exception in a handler GTK calls (a click, a key, a timer) would otherwise end the process from
			// inside GLib; WPF surfaces it from the message loop instead, and so does this (Dispatcher.PushFrame).
			GLib.ExceptionManager.UnhandledException += args =>
			{
				if (args.IsTerminating)
					return;

				System.Windows.Threading.Dispatcher.CurrentDispatcher.Defer(args.ExceptionObject as Exception);
			};

			FormsApplication = XFApplication.Current ?? new ResourceApplication();
			foreach (var resource in SystemColors.Resources())
			{
				if (!FormsApplication.Resources.ContainsKey(resource.Key))
					FormsApplication.Resources.Add(resource.Key, resource.Value);
			}
		}

		/// <summary>An application that is only a resource holder: the WPF application is the real one.</summary>
		sealed class ResourceApplication : XFApplication
		{
		}
	}
}
