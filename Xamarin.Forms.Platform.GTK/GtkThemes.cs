using System;
using Gtk;

namespace Xamarin.Forms.Platform.GTK
{
	/// <summary>
	/// Application-wide GTK theming.
	/// </summary>
	/// <remarks>
	/// The GTK2 implementation probed the Windows registry for a GTK# 2.12 install and
	/// called kernel32!SetDllDirectory so the native DLLs could be found; on Linux the
	/// loader finds libgtk-3 through the normal search path, so none of that applies.
	///
	/// GTK3 also replaced the RC-file mechanism (<c>Gtk.Rc.Parse</c>) with CSS, so
	/// <see cref="LoadCustomTheme"/> now expects a .css file rather than a .rc file.
	/// </remarks>
	public static class GtkThemes
	{
		static CssProvider s_customThemeProvider;

		public static bool IsInitialized { get; private set; }

		public static void Init()
		{
			if (IsInitialized)
				return;

			IsInitialized = true;
		}

		/// <summary>
		/// Applies a custom GTK3 CSS theme across every screen of the application,
		/// replacing any theme previously applied through this method.
		/// </summary>
		/// <param name="filename">Path to a GTK3 CSS file.</param>
		public static void LoadCustomTheme(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return;

			if (!IsInitialized)
				throw new InvalidOperationException("call GtkThemes.Init() before this");

			// The display, not Gdk.Screen: Gtk 4 removed GdkScreen entirely. It was the "one X11
			// screen of a display" abstraction, and since a display has exactly one in practice
			// Gtk folded the two together - so every ForScreen call became a ForDisplay one.
			var display = Gdk.Display.Default;

			if (display == null)
				return;

			if (s_customThemeProvider != null)
			{
				StyleContext.RemoveProviderForDisplay(display, s_customThemeProvider);
				s_customThemeProvider = null;
			}

			var provider = new CssProvider();
			provider.LoadFromPath(filename);

			StyleContext.AddProviderForDisplay(display, provider, StyleProviderPriority.Application);
			s_customThemeProvider = provider;
		}
	}
}
