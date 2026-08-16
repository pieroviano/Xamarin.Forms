using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Xamarin.Forms.ControlGallery.GTK.ScreenshotTests.Capture;
using Xunit;

namespace Xamarin.Forms.ControlGallery.GTK.ScreenshotTests
{
	/// <summary>
	/// One launch of the gallery, shared by every test that only needs to look at it.
	/// </summary>
	/// <remarks>
	/// LAZY on purpose. Launching from the fixture constructor would attach a skip - "the gallery
	/// is not built", "this session has no desktop" - to fixture construction, where xUnit reports
	/// it once and swallows the rest; the same trap Xamarin.Forms.Platform.GTK.UnitTests' GtkTestHost
	/// documents. Every test calls <see cref="GalleryProcess.EnsureRunnable"/> itself first, so the
	/// verdict lands on the test.
	/// </remarks>
	public sealed class GalleryLaunchFixture : IDisposable
	{
		readonly object _gate = new object();

		GalleryRun _run;
		Exception _failure;

		public GalleryRun Run
		{
			get
			{
				lock (_gate)
				{
					if (_failure != null)
						throw new InvalidOperationException("The shared gallery launch already failed.", _failure);

					if (_run != null)
						return _run;

					try
					{
						_run = GalleryProcess.Launch();
					}
					catch (Exception e)
					{
						_failure = e;
						throw;
					}

					return _run;
				}
			}
		}

		public void Dispose()
		{
			lock (_gate)
				_run?.Dispose();
		}
	}

	/// <summary>
	/// The regression gate for <c>Xamarin.Forms.ControlGallery.GTK</c>'s launch screen, per
	/// <c>docs/plans/controlgallery-gtk4-rendering.md</c> section 6.
	/// </summary>
	/// <remarks>
	/// The gate is layered, cheapest and most diagnostic first:
	///
	/// 1. GEOMETRY. On its own this catches the largest defect the whole effort found - root cause
	///    R1's 828x7860 window - and it needs no baseline, no fonts and no theme.
	/// 2. THE LOG. Three facts that are true or false regardless of what the window looks like:
	///    no gtk_box_append assertion (R2), no allocate-without-measure (R3), no Pango font
	///    fallback (R5). Together with geometry these are the plan's pre-committed
	///    "geometry-and-log fallback gate".
	/// 3. CONVERGENCE, over several launches. Section 10's defect was intermittent - 6 launches in
	///    10 - so a single launch could not have detected it and a single-launch assertion would
	///    have been a coin flip. This one is stated as a ratio by construction.
	/// 4. PIXELS, against a curated golden image (decision 5). Skipped, not failed, when the
	///    baseline is absent, so the suite is green on a fresh clone.
	/// </remarks>
	public sealed class GalleryScreenshotTests : IClassFixture<GalleryLaunchFixture>
	{
		/// <summary>
		/// MEASURED on 2026-08-16, ten consecutive launches, all identical: the gallery's toplevel
		/// settles at 828x629 as GetWindowRect reports it. The GTK 3 reference build is 816x639;
		/// the 12x10 difference is window decoration, not layout (plan section 9).
		///
		/// Machine-specific, and honestly so - a screenshot harness is. Override both numbers with
		/// XF_GALLERY_EXPECTED_WINDOW=WxH rather than editing this when running on a display whose
		/// scaling differs.
		/// </summary>
		const int ExpectedWindowWidth = 828;
		const int ExpectedWindowHeight = 629;

		/// <summary>
		/// How far the toplevel may be from the reference before the test fails.
		///
		/// This number has to stay well under GtkToolbarConstants.ToolbarHeight, which is 72: the
		/// bistable layout of plan section 10 landed the page at 561 or at 633, i.e. exactly one
		/// toolbar apart, and a tolerance that swallowed 72 would swallow the defect. 24 leaves
		/// room for a decoration or theme difference and none at all for a toolbar.
		/// </summary>
		const int GeometryTolerance = 24;

		/// <summary>
		/// How many launches the convergence gate takes. Section 10's baseline was 6 bad launches
		/// in 10, so six launches would have caught it with probability 1 - 0.4^6, about 99.6%.
		/// Raise it with XF_GALLERY_CONVERGENCE_LAUNCHES when re-measuring a suspected regression;
		/// the plan's own protocol asks for ten.
		/// </summary>
		const int DefaultConvergenceLaunches = 6;

		/// <summary>
		/// Allocation criticals a launch may emit before the convergence gate calls it bad.
		///
		/// MEASURED 2026-08-16, ten consecutive launches after the section 10 fix: every launch
		/// emitted exactly one, always the same one -
		///     Gtk-CRITICAL: Allocation height too small. Tried to allocate 45x20,
		///     but GtkLabel ... needs at least 45x59.
		/// - a single over-constrained label, at a fixed size unrelated to the page. The defect
		/// this budget exists to catch looked nothing like that: a bad launch emitted 20 to 66
		/// criticals, every one of them naming the whole window ("needs at least 500x633"), and the
		/// rejected arithmetic fixes drove it to 889-2035.
		///
		/// So the budget separates "one known, bounded, non-layout critical" from "the page latched
		/// a toolbar too tall and is now rejecting every allocation forever", which is what it is
		/// for. Raising it needs a new measurement in the plan, not a nudge here; the residual
		/// label itself is follow-up work, not a reason to widen the gate.
		/// </summary>
		const int AllocationCriticalBudget = 1;

		readonly GalleryLaunchFixture _fixture;

		public GalleryScreenshotTests(GalleryLaunchFixture fixture)
		{
			_fixture = fixture;
		}

		/// <summary>
		/// Geometry, first and on its own. Root cause R1 produced an 828x7860 window; nothing else
		/// in this file was needed to see that, and nothing else in this file is as cheap.
		/// </summary>
		[Fact]
		public void TheToplevelSettlesAtTheReferenceGeometry()
		{
			GalleryProcess.EnsureRunnable();

			GalleryRun run = _fixture.Run;

			AssertGeometry(run);
		}

		/// <summary>
		/// The three log facts that need no baseline. Each one is a root cause the plan names, and
		/// each is a hard zero: they are assertions and warnings the backend must not provoke at
		/// all, not thresholds.
		/// </summary>
		[Fact]
		public void TheLaunchLogHasNoStructuralWarnings()
		{
			GalleryProcess.EnsureRunnable();

			GalleryRun run = _fixture.Run;

			AssertNone(run, GalleryProcess.BoxAppendAssertion,
				"gtk_box_append assertions (plan R2 - a compat Box.Add/PackStart that is not reparent-safe)");

			AssertNone(run, GalleryProcess.AllocateWithoutMeasure,
				"allocate-without-measure warnings (plan R3 - GTK 4 requires gtk_widget_measure before " +
				"an allocation, and without it the allocation silently does not propagate)");

			AssertNone(run, GalleryProcess.PangoFontFallback,
				"Pango font-fallback lines (plan R5 - a font description Pango cannot resolve)");
		}

		/// <summary>
		/// The section 10 gate: does the page layout land on the same answer every time?
		/// </summary>
		/// <remarks>
		/// Stated as a ratio deliberately. The defect it replaces was intermittent - the same
		/// binary settled at the correct height or at one GtkToolbarConstants.ToolbarHeight (72)
		/// too tall, 6 launches in 10 - so a one-launch assertion would have been a coin flip, and
		/// judging a change on one run before and one run after is the mistake that cost this
		/// effort an hour. Every launch must land inside <see cref="GeometryTolerance"/>, which is
		/// a third of a toolbar, and every launch must stay inside
		/// <see cref="AllocationCriticalBudget"/>.
		///
		/// This is the slow test in the suite - six launches, each dwelling
		/// <see cref="GalleryProcess.MinimumDwell"/> - and that cost is the point. There is no
		/// cheaper way to measure a race.
		/// </remarks>
		[Fact]
		public void TheLayoutConvergesToTheSameGeometryOnEveryLaunch()
		{
			GalleryProcess.EnsureRunnable();

			int launches = ConvergenceLaunches();
			Size expected = ExpectedGeometry();
			var bad = new List<string>();

			for (int i = 1; i <= launches; i++)
			{
				// No capture: this test is about where the layout lands, and photographing six
				// windows would triple its cost for pixels nothing asserts on.
				using (GalleryRun run = GalleryProcess.Launch(capture: false))
				{
					int criticals = run.Count(GalleryProcess.AllocationCritical);

					bool geometryOk =
						Math.Abs(run.WindowWidth - expected.Width) <= GeometryTolerance &&
						Math.Abs(run.WindowHeight - expected.Height) <= GeometryTolerance;

					if (geometryOk && criticals <= AllocationCriticalBudget)
						continue;

					bad.Add(
						$"  launch {i}: {run.WindowWidth}x{run.WindowHeight} " +
						$"(sizes seen: {string.Join(" -> ", run.GeometrySamples)}), " +
						$"{criticals} allocation critical(s)" + Environment.NewLine +
						Indent(run.DescribeLog()));
				}
			}

			Assert.True(
				bad.Count == 0,
				$"{bad.Count} of {launches} launches did not converge to " +
				$"{expected.Width}x{expected.Height} (+/-{GeometryTolerance}) within " +
				$"{AllocationCriticalBudget} allocation critical(s). The baseline this gate was " +
				"built against is docs/plans/controlgallery-gtk4-rendering.md section 10: 6 bad " +
				"launches in 10, each emitting 20-66 criticals naming the whole window, with the " +
				"page one GtkToolbarConstants.ToolbarHeight (72) too tall." + Environment.NewLine +
				string.Join(Environment.NewLine, bad));
		}

		/// <summary>
		/// The pixel gate, against a curated GTK 4 golden image - NOT the GTK 3 capture (plan
		/// decision 5: committing the reference would encode its known defects as required
		/// behaviour).
		/// </summary>
		/// <remarks>
		/// The baseline is deliberately NOT committed with this harness. Landing a golden image in
		/// the same change that lands the harness would mean nobody ever signed the image off, and
		/// decision 5 makes the sign-off the whole point of it. Until someone does, this test
		/// SKIPS - so the suite is green on arrival rather than red for a reason unrelated to the
		/// code. Regenerate with -p:UpdateGalleryBaseline=true (or XF_UPDATE_GALLERY_BASELINE=1),
		/// which writes the image and still reports skipped: a regenerating run has verified
		/// nothing.
		/// </remarks>
		[Fact]
		public void TheLaunchScreenMatchesItsBaseline()
		{
			GalleryProcess.EnsureRunnable();

			GalleryRun run = _fixture.Run;

			// Geometry first here too. A capture of the wrong-sized window would fail the pixel
			// comparison with a shape mismatch, which says far less than the geometry assertion.
			AssertGeometry(run);

			Assert.NotNull(run.Capture);
			Assert.Null(run.Capture.Diagnostics);

			string baselinePath = Path.Combine(GalleryProcess.BaselineDirectory, "ControlGallery-Launch.png");

			if (GalleryProcess.ShouldUpdateBaseline)
			{
				Directory.CreateDirectory(GalleryProcess.BaselineDirectory);
				run.Capture.Image.Save(baselinePath, ImageFormat.Png);

				Assert.Skip(
					$"Baseline REGENERATED at {baselinePath} " +
					$"({run.Capture.Image.Width}x{run.Capture.Image.Height}, " +
					$"scale {run.Capture.ScaleX:0.##}x{run.Capture.ScaleY:0.##}). " +
					"Nothing was verified by this run. Review the image against the GTK 3 reference " +
					"before committing it - it becomes the definition of correct.");
			}

			if (!File.Exists(baselinePath))
			{
				Assert.Skip(
					$"No committed baseline at {baselinePath}, so there is nothing to diff against. " +
					"This is the state the harness ships in, by design: the golden image has to be " +
					"reviewed and signed off (plan decision 5), and a baseline generated by the same " +
					"change that generated the harness would never have been. Produce one with " +
					"`dotnet test Xamarin.Forms.ControlGallery.GTK.ScreenshotTests " +
					"-p:UpdateGalleryBaseline=true`.");
			}

			string artifacts = Path.Combine(AppContext.BaseDirectory, "ScreenshotArtifacts");

			using (var baseline = new Bitmap(baselinePath))
			{
				ImageComparison comparison = ImageComparer.Compare(run.Capture.Image, baseline);

				try
				{
					bool passed =
						comparison.ShapeMismatch == null &&
						comparison.DifferingFraction <= ImageComparer.DefaultDifferingFraction;

					if (passed)
						return;

					Directory.CreateDirectory(artifacts);

					string actualPath = Path.Combine(artifacts, "ControlGallery-Launch.actual.png");
					run.Capture.Image.Save(actualPath, ImageFormat.Png);

					string diffPath = null;

					if (comparison.Diff != null)
					{
						diffPath = Path.Combine(artifacts, "ControlGallery-Launch.diff.png");
						comparison.Diff.Save(diffPath, ImageFormat.Png);
					}

					Assert.Fail(
						"The launch screen no longer matches its baseline." + Environment.NewLine +
						comparison.Describe() + Environment.NewLine +
						$"Threshold: at most {ImageComparer.DefaultDifferingFraction:P1} of pixels may " +
						$"differ by more than {ImageComparer.DefaultChannelTolerance}/255 on any channel." +
						Environment.NewLine +
						$"baseline: {baselinePath}" + Environment.NewLine +
						$"actual:   {actualPath}" +
						(diffPath == null ? string.Empty : Environment.NewLine + $"diff:     {diffPath}"));
				}
				finally
				{
					comparison.Diff?.Dispose();
				}
			}
		}

		static void AssertGeometry(GalleryRun run)
		{
			Size expected = ExpectedGeometry();

			string detail =
				$"Toplevel settled at {run.WindowWidth}x{run.WindowHeight}; expected " +
				$"{expected.Width}x{expected.Height} +/-{GeometryTolerance}. " +
				$"Sizes it passed through: {string.Join(" -> ", run.GeometrySamples)}. " +
				"On a display whose scaling differs from the machine this was measured on, set " +
				"XF_GALLERY_EXPECTED_WINDOW=WxH rather than widening the tolerance - it is " +
				"deliberately narrower than GtkToolbarConstants.ToolbarHeight." + Environment.NewLine +
				Indent(run.DescribeLog());

			Assert.True(Math.Abs(run.WindowWidth - expected.Width) <= GeometryTolerance, detail);
			Assert.True(Math.Abs(run.WindowHeight - expected.Height) <= GeometryTolerance, detail);
		}

		static void AssertNone(GalleryRun run, System.Text.RegularExpressions.Regex pattern, string what)
		{
			var hits = run.Matching(pattern).ToArray();

			Assert.True(
				hits.Length == 0,
				$"The gallery emitted {hits.Length} {what}:" + Environment.NewLine +
				Indent(string.Join(Environment.NewLine, hits)));
		}

		static Size ExpectedGeometry()
		{
			string raw = Environment.GetEnvironmentVariable("XF_GALLERY_EXPECTED_WINDOW");

			if (string.IsNullOrWhiteSpace(raw))
				return new Size(ExpectedWindowWidth, ExpectedWindowHeight);

			string[] parts = raw.Split('x', 'X');

			if (parts.Length == 2 &&
				int.TryParse(parts[0], out int width) &&
				int.TryParse(parts[1], out int height))
			{
				return new Size(width, height);
			}

			throw new InvalidOperationException(
				$"XF_GALLERY_EXPECTED_WINDOW is \"{raw}\"; it has to look like 828x629.");
		}

		static int ConvergenceLaunches()
		{
			string raw = Environment.GetEnvironmentVariable("XF_GALLERY_CONVERGENCE_LAUNCHES");

			return !string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out int launches) && launches > 0
				? launches
				: DefaultConvergenceLaunches;
		}

		static string Indent(string text) =>
			string.Join(
				Environment.NewLine,
				text.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None).Select(l => "    " + l));
	}
}
