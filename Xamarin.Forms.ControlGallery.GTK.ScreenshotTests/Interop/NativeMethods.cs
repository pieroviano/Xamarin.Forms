using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Interop
{
	/// <summary>
	/// Win32 entry points used to enumerate top-level windows and to render one into a device
	/// context.
	/// </summary>
	/// <remarks>
	/// LIFTED, near-verbatim, from
	/// <c>d:\CommonLibrary\ProcessWindowBitmapSave\src\ProcessWindowBitmapSave\Native\NativeMethods.cs</c>
	/// - the same code the <c>window-capture</c> MCP server runs, and the code every capture in
	/// <c>docs/plans/controlgallery-gtk4-rendering.md</c> was taken with.
	///
	/// Lifted rather than referenced, deliberately. That project is an <c>Exe</c> whose package
	/// closure is ModelContextProtocol + Microsoft.Extensions.Hosting; a ProjectReference would
	/// drag an MCP server host into this repository's test graph, and it lives outside this
	/// repository entirely, so a clone or a CI checkout would not have it. Plan section 6 allows
	/// either ("reference the project, or lift the capture routine") and forbids the third option,
	/// driving the MCP server from a test, which would make the suite depend on an agent host.
	/// Only the enumeration and PrintWindow paths came across; the CLI, the MCP tool surface and
	/// the output-path/naming logic did not.
	/// </remarks>
	internal static class NativeMethods
	{
		internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		// ---- window enumeration / identity ---------------------------------------------

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll")]
		internal static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		internal static extern int GetWindowTextLengthW(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		// ---- geometry --------------------------------------------------------------------

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

		// ---- process state ----------------------------------------------------------------

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);

		// ---- capture -----------------------------------------------------------------------

		/// <summary>
		/// Asks the window to render itself into <paramref name="hdcBlt"/>. Unlike a screen BitBlt
		/// this also works while the window is occluded by other windows - which is what lets this
		/// suite run without stealing focus.
		/// </summary>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

		// ---- DWM ----------------------------------------------------------------------------

		[DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
		internal static extern int DwmGetWindowAttributeRect(IntPtr hWnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

		[DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
		internal static extern int DwmGetWindowAttributeInt(IntPtr hWnd, int dwAttribute, out int pvAttribute, int cbAttribute);

		// ---- constants -----------------------------------------------------------------------

		/// <summary>Render the full window content, including DirectComposition/DWM-drawn surfaces.</summary>
		internal const uint PW_RENDERFULLCONTENT = 0x00000002;

		internal const uint GA_ROOT = 2;

		internal const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
		internal const int DWMWA_CLOAKED = 14;

		internal static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public readonly int Width => Right - Left;
			public readonly int Height => Bottom - Top;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct POINT
		{
			public int X;
			public int Y;
		}

		internal static string GetWindowTitle(IntPtr hWnd)
		{
			int length = GetWindowTextLengthW(hWnd);
			if (length <= 0)
				return string.Empty;

			var buffer = new StringBuilder(length + 1);
			int copied = GetWindowTextW(hWnd, buffer, buffer.Capacity);
			return copied > 0 ? buffer.ToString(0, copied) : string.Empty;
		}

		internal static string GetWindowClassName(IntPtr hWnd)
		{
			var buffer = new StringBuilder(256);
			int copied = GetClassNameW(hWnd, buffer, buffer.Capacity);
			return copied > 0 ? buffer.ToString(0, copied) : string.Empty;
		}

		/// <summary>
		/// True for windows the shell hides from the user. A GTK toplevel is briefly cloaked
		/// between creation and first present, so this is what separates "the window exists" from
		/// "the window is on screen and has something to photograph".
		/// </summary>
		internal static bool IsCloaked(IntPtr hWnd) =>
			DwmGetWindowAttributeInt(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;

		/// <summary>
		/// Opts this process into per-monitor DPI awareness, once.
		/// </summary>
		/// <remarks>
		/// Must happen before any window geometry is read. Without it Windows virtualises the
		/// coordinates GetWindowRect returns, so on a scaled monitor the rectangle disagrees with
		/// the surface PrintWindow renders into and the capture comes back cropped. That is the
		/// same 2x mismatch plan section 1.2 records ("the GTK 4 capture comes back at 2x the
		/// reported window height"), and it is why nothing here may assume a scale factor:
		/// see <see cref="Capture.CapturedWindow.ScaleX"/>.
		/// </remarks>
		internal static void EnsureDpiAware()
		{
			if (s_dpiAware)
				return;

			s_dpiAware = true;

			try
			{
				SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
			}
			catch (EntryPointNotFoundException)
			{
				// Pre-1703 Windows: the process stays system-DPI aware, which is good enough.
			}
		}

		static bool s_dpiAware;
	}
}
