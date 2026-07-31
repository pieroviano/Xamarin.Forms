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

			var screen = Gdk.Screen.Default;

			if (screen == null)
				return;

			if (s_customThemeProvider != null)
			{
				StyleContext.RemoveProviderForScreen(screen, s_customThemeProvider);
				s_customThemeProvider = null;
			}

			var provider = new CssProvider();
			provider.LoadFromPath(filename);

			StyleContext.AddProviderForScreen(screen, provider, StyleProviderPriority.Application);
			s_customThemeProvider = provider;
		}
	}
}
