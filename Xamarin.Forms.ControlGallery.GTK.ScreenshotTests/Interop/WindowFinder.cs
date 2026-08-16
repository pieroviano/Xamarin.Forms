using System;
using System.Collections.Generic;

namespace Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Interop
{
	/// <summary>A top-level window, as Win32 reports it.</summary>
	internal sealed class WindowInfo
	{
		public IntPtr Handle { get; set; }
		public int ProcessId { get; set; }
		public string Title { get; set; }
		public string ClassName { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public bool IsVisible { get; set; }
		public bool IsMinimized { get; set; }

		public string Describe() =>
			$"hwnd=0x{Handle.ToInt64():X} class={ClassName} title=\"{Title}\" " +
			$"{Width}x{Height} at {X},{Y} visible={IsVisible} minimized={IsMinimized}";
	}

	/// <summary>
	/// Enumerates a process's real top-level windows.
	/// </summary>
	/// <remarks>
	/// LIFTED and trimmed from ProcessWindowBitmapSave's <c>Windows/WindowFinder.cs</c> - see the
	/// remarks on <see cref="NativeMethods"/> for why lifted rather than referenced. What was
	/// dropped is everything the CLI needed and a test does not: title/name matching, the
	/// ambiguity-resolution heuristics, and the "prefer the single titled window" fallback. A test
	/// must not guess which window it meant, so this returns them all and the caller filters on
	/// the class name the plan records.
	/// </remarks>
	internal static class WindowFinder
	{
		/// <summary>
		/// The window class GTK 4's Win32 GDK backend gives a toplevel. GTK 3 used
		/// <c>gdkWindowToplevel</c>; the rename is recorded in plan section 1.2, and matching on it
		/// is what separates the gallery's real window from the console pseudo-window the host
		/// creates (<c>PseudoConsoleWindow</c>).
		/// </summary>
		public const string GtkToplevelClassName = "gdkSurfaceToplevel";

		public static IReadOnlyList<WindowInfo> FindByProcessId(int processId)
		{
			NativeMethods.EnsureDpiAware();

			var results = new List<WindowInfo>();

			NativeMethods.EnumWindows((hWnd, _) =>
			{
				// Only real top-level windows; owned popups report themselves through GA_ROOT.
				if (NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOT) != hWnd)
					return true;

				NativeMethods.GetWindowThreadProcessId(hWnd, out uint rawPid);
				if ((int)rawPid != processId)
					return true;

				if (!NativeMethods.GetWindowRect(hWnd, out var rect))
					return true;

				results.Add(new WindowInfo
				{
					Handle = hWnd,
					ProcessId = (int)rawPid,
					Title = NativeMethods.GetWindowTitle(hWnd),
					ClassName = NativeMethods.GetWindowClassName(hWnd),
					X = rect.Left,
					Y = rect.Top,
					Width = rect.Width,
					Height = rect.Height,
					IsVisible = NativeMethods.IsWindowVisible(hWnd) && !NativeMethods.IsCloaked(hWnd),
					IsMinimized = NativeMethods.IsIconic(hWnd),
				});

				return true;
			}, IntPtr.Zero);

			return results;
		}

		/// <summary>
		/// The process's visible GTK toplevel, or null while it does not have one yet.
		/// </summary>
		public static WindowInfo FindGtkToplevel(int processId)
		{
			foreach (var window in FindByProcessId(processId))
			{
				if (!string.Equals(window.ClassName, GtkToplevelClassName, StringComparison.Ordinal))
					continue;

				if (!window.IsVisible || window.IsMinimized)
					continue;

				// A toplevel that exists but has not been laid out reports a placeholder rectangle.
				// Photographing that would produce a 1x1 "baseline".
				if (window.Width <= 1 || window.Height <= 1)
					continue;

				return window;
			}

			return null;
		}
	}
}
