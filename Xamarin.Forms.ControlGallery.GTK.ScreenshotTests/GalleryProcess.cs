using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Capture;
using Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Interop;
using Xunit;

namespace Xamarin.Forms.ControlGallery.GTK.ScreenshotTests
{
	/// <summary>
	/// Everything one launch of the gallery told us: the geometry its toplevel settled on, the
	/// pixels it drew, and every line it wrote to stderr.
	/// </summary>
	public sealed class GalleryRun : IDisposable
	{
		internal GalleryRun(
			int windowWidth,
			int windowHeight,
			IReadOnlyList<string> standardError,
			IReadOnlyList<string> geometrySamples,
			CapturedWindow capture)
		{
			WindowWidth = windowWidth;
			WindowHeight = windowHeight;
			StandardError = standardError;
			GeometrySamples = geometrySamples;
			Capture = capture;
		}

		/// <summary>Toplevel width from <c>GetWindowRect</c> - the same measurement the plan quotes.</summary>
		public int WindowWidth { get; }

		/// <summary>Toplevel height from <c>GetWindowRect</c>.</summary>
		public int WindowHeight { get; }

		/// <summary>Every stderr line the process wrote before it was killed.</summary>
		public IReadOnlyList<string> StandardError { get; }

		/// <summary>The distinct sizes the toplevel passed through, oldest first, for diagnostics.</summary>
		public IReadOnlyList<string> GeometrySamples { get; }

		/// <summary>The captured pixels, or null when the run was asked not to capture.</summary>
		public CapturedWindow Capture { get; }

		public int Count(Regex pattern) => StandardError.Count(line => pattern.IsMatch(line));

		public IEnumerable<string> Matching(Regex pattern) => StandardError.Where(line => pattern.IsMatch(line));

		public string DescribeLog() =>
			StandardError.Count == 0
				? "(the process wrote nothing to stderr)"
				: string.Join(Environment.NewLine, StandardError);

		public void Dispose() => Capture?.Dispose();
	}

	/// <summary>
	/// Launches the gallery, waits for its toplevel to settle, photographs it, and always kills it.
	/// </summary>
	public static class GalleryProcess
	{
		/// <summary>
		/// A container was given a size without being measured first. GTK 4 requires the measure,
		/// and skipping it means the allocation silently does not propagate - root cause R3 in the
		/// plan, and one of the three pre-committed log facts in section 6.
		/// </summary>
		public static readonly Regex AllocateWithoutMeasure =
			new Regex(@"without calling gtk_widget_measure", RegexOptions.Compiled);

		/// <summary>
		/// A widget was appended to a box while it still had a parent. Root cause R2.
		/// </summary>
		public static readonly Regex BoxAppendAssertion =
			new Regex(@"gtk_box_append", RegexOptions.Compiled);

		/// <summary>
		/// Pango could not resolve a font description and fell back. Root cause R5.
		/// </summary>
		public static readonly Regex PangoFontFallback =
			new Regex(@"couldn't load font", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// GTK refused an allocation because the widget's own minimum was larger. This is the
		/// signature of the bistable page layout of plan section 10 - see
		/// <see cref="GalleryScreenshotTests"/> for why it is gated as a ratio and not as zero.
		/// </summary>
		public static readonly Regex AllocationCritical =
			new Regex(@"needs at least", RegexOptions.Compiled);

		/// <summary>How long to wait for the toplevel to appear at all.</summary>
		public static readonly TimeSpan ToplevelTimeout = TimeSpan.FromSeconds(30);

		/// <summary>
		/// How long to keep the process alive after its toplevel appears, at minimum.
		/// </summary>
		/// <remarks>
		/// MEASURED: the gallery's layout, and every critical it is going to emit, is finished well
		/// inside this. Ten consecutive launches on 2026-08-16 each wrote their whole stderr - two
		/// lines - within the first few seconds, and the ~11s dwell the manual protocol in the task
		/// brief uses was chosen the same way. Shortening this trades test time against the risk of
		/// declaring a log clean that had not finished being written.
		/// </remarks>
		public static readonly TimeSpan MinimumDwell = TimeSpan.FromSeconds(8);

		/// <summary>How long to keep waiting for the geometry to stop changing, past the minimum dwell.</summary>
		public static readonly TimeSpan MaximumDwell = TimeSpan.FromSeconds(20);

		/// <summary>The toplevel counts as settled once its rectangle has been unchanged for this long.</summary>
		public static readonly TimeSpan StableFor = TimeSpan.FromSeconds(2);

		static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

		/// <summary>
		/// Set this in an environment that is SUPPOSED to be able to run the gallery, and a
		/// missing executable or a non-interactive session becomes a failure instead of a skip.
		/// The same mechanism, and the same reasoning, as XF_GTK_REQUIRE_NATIVE in
		/// Xamarin.Forms.Platform.GTK.UnitTests: without it a broken layout would report green
		/// by absence.
		/// </summary>
		public const string RequiredVariable = "XF_GALLERY_SCREENSHOTS_REQUIRED";

		/// <summary>
		/// The gallery executable, as MSBuild resolved it at compile time. Baked in rather than
		/// searched for, so a run against a stale or unrelated build is impossible rather than
		/// merely unlikely.
		/// </summary>
		public static string ExecutablePath => Metadata("XFGalleryExePath");

		/// <summary>The committed-baseline directory, inside the test project.</summary>
		public static string BaselineDirectory => Metadata("XFBaselineDirectory");

		/// <summary>
		/// True when this run was explicitly told to rewrite the golden image - by
		/// <c>-p:UpdateGalleryBaseline=true</c> at build time, or by
		/// <c>XF_UPDATE_GALLERY_BASELINE=1</c> in the environment of an already-built run.
		/// </summary>
		public static bool ShouldUpdateBaseline =>
			string.Equals(Metadata("XFUpdateGalleryBaseline"), "true", StringComparison.OrdinalIgnoreCase) ||
			IsTruthy(Environment.GetEnvironmentVariable("XF_UPDATE_GALLERY_BASELINE"));

		/// <summary>
		/// Skips - or, under <see cref="RequiredVariable"/>, fails - when this machine cannot host
		/// the gallery at all.
		/// </summary>
		public static void EnsureRunnable()
		{
			string reason = UnrunnableReason();

			if (reason == null)
				return;

			Assert.False(
				!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RequiredVariable)),
				$"{reason} ({RequiredVariable} is set, so this is a failure rather than a skip.)");

			Assert.Skip(reason);
		}

		static string UnrunnableReason()
		{
			if (!File.Exists(ExecutablePath))
			{
				return $"The gallery executable is not built: {ExecutablePath} does not exist. " +
					"Build the solution first - `dotnet build Xamarin.Forms.Gtk.sln -c Debug`.";
			}

			// PrintWindow needs a real window station: in a service/Session 0 context the gallery
			// never gets a toplevel and the wait below would just time out 30 seconds later with a
			// worse message.
			if (!Environment.UserInteractive)
			{
				return "This session is not interactive, so there is no desktop for the gallery's " +
					"toplevel to appear on. These tests photograph a real window and need one.";
			}

			return null;
		}

		/// <summary>
		/// Runs the gallery once. The process is killed before this returns, on every path.
		/// </summary>
		/// <param name="capture">False to collect geometry and the log only, skipping the bitmap.</param>
		public static GalleryRun Launch(bool capture = true)
		{
			EnsureRunnable();

			string exe = Path.GetFullPath(ExecutablePath);

			var stderr = new List<string>();
			var geometry = new List<string>();

			var info = new ProcessStartInfo(exe)
			{
				WorkingDirectory = Path.GetDirectoryName(exe),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			};

			Process process = Process.Start(info);

			if (process == null)
				throw new InvalidOperationException($"Failed to start {exe}.");

			try
			{
				// Both streams must be drained. The GTK warning stream is half the evidence this
				// suite exists to collect, and an undrained stdout pipe would eventually block the
				// gallery inside a Console.Write and freeze the layout mid-pass.
				process.ErrorDataReceived += (_, e) =>
				{
					if (e.Data == null)
						return;

					// GLib prefixes its criticals with a blank line.
					if (e.Data.Length == 0)
						return;

					lock (stderr)
						stderr.Add(e.Data);
				};

				process.OutputDataReceived += (_, __) => { };

				process.BeginErrorReadLine();
				process.BeginOutputReadLine();

				WindowInfo window = WaitForToplevel(process, stderr);

				window = WaitUntilSettled(process, window, geometry);

				CapturedWindow pixels = capture ? WindowCapture.Capture(window) : null;

				// Re-read after the capture: if these disagree the window moved under us, and the
				// capture is of something the assertions were not told about.
				if (!NativeMethods.GetWindowRect(window.Handle, out var final))
					final = default;

				lock (stderr)
				{
					return new GalleryRun(
						final.Width > 0 ? final.Width : window.Width,
						final.Height > 0 ? final.Height : window.Height,
						stderr.ToArray(),
						geometry.ToArray(),
						pixels);
				}
			}
			finally
			{
				Kill(process);
			}
		}

		static WindowInfo WaitForToplevel(Process process, List<string> stderr)
		{
			var deadline = DateTime.UtcNow + ToplevelTimeout;

			while (DateTime.UtcNow < deadline)
			{
				if (process.HasExited)
				{
					string log;
					lock (stderr)
						log = stderr.Count == 0 ? "(nothing on stderr)" : string.Join(Environment.NewLine, stderr);

					throw new InvalidOperationException(
						$"The gallery exited with code {process.ExitCode} before showing a toplevel." +
						Environment.NewLine + log);
				}

				WindowInfo window = WindowFinder.FindGtkToplevel(process.Id);

				if (window != null)
					return window;

				Thread.Sleep(PollInterval);
			}

			throw new TimeoutException(
				$"No window of class {WindowFinder.GtkToplevelClassName} appeared for the gallery " +
				$"within {ToplevelTimeout.TotalSeconds:0} seconds. Windows the process did own: " +
				Describe(WindowFinder.FindByProcessId(process.Id)));
		}

		/// <summary>
		/// Waits out the minimum dwell, then for the toplevel rectangle to hold still.
		/// </summary>
		/// <remarks>
		/// Both halves are load-bearing. The dwell is what gives the eight deferred layout passes
		/// of plan section 10 time to run at all - assert too early and every launch looks clean.
		/// The stability wait is what makes the geometry assertion mean "the layout converged
		/// here" rather than "this is where it happened to be when the clock ran out"; the sizes it
		/// passed through are kept, so a run that never settles reports what it was doing instead
		/// of a bare number.
		/// </remarks>
		static WindowInfo WaitUntilSettled(Process process, WindowInfo window, List<string> geometry)
		{
			var started = DateTime.UtcNow;
			var hardDeadline = started + MaximumDwell;

			WindowInfo current = window;
			string lastSize = null;
			DateTime unchangedSince = started;

			while (true)
			{
				if (process.HasExited)
					throw new InvalidOperationException($"The gallery exited with code {process.ExitCode} while its layout was settling.");

				WindowInfo latest = WindowFinder.FindGtkToplevel(process.Id) ?? current;
				current = latest;

				string size = $"{latest.Width}x{latest.Height}";

				if (size != lastSize)
				{
					geometry.Add(size);
					lastSize = size;
					unchangedSince = DateTime.UtcNow;
				}

				var now = DateTime.UtcNow;
				bool dwelled = now - started >= MinimumDwell;
				bool stable = now - unchangedSince >= StableFor;

				if (dwelled && stable)
					return current;

				if (now >= hardDeadline)
					return current;

				Thread.Sleep(PollInterval);
			}
		}

		static void Kill(Process process)
		{
			try
			{
				if (!process.HasExited)
					process.Kill(entireProcessTree: true);
			}
			catch (InvalidOperationException)
			{
				// Already gone between the check and the kill.
			}
			catch (System.ComponentModel.Win32Exception)
			{
				// Already terminating.
			}

			try
			{
				process.WaitForExit(5000);
			}
			catch (SystemException)
			{
			}

			process.Dispose();
		}

		static string Describe(IReadOnlyList<WindowInfo> windows) =>
			windows.Count == 0
				? "(none)"
				: Environment.NewLine + string.Join(Environment.NewLine, windows.Select(w => "  - " + w.Describe()));

		static bool IsTruthy(string value) =>
			!string.IsNullOrEmpty(value) &&
			!string.Equals(value, "0", StringComparison.Ordinal) &&
			!string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);

		static string Metadata(string key)
		{
			foreach (var attribute in typeof(GalleryProcess).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
			{
				if (string.Equals(attribute.Key, key, StringComparison.Ordinal))
					return attribute.Value;
			}

			throw new InvalidOperationException(
				$"The assembly carries no [AssemblyMetadata(\"{key}\")]. It is supplied by " +
				"Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.csproj; a build that lost it is broken.");
		}
	}
}
