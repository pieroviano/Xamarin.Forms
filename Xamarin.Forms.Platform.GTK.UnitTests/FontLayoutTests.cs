using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Extensions;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// The stale-layout hazard recorded in plan §8.3.1 and left standing there: a font applied as
	/// CSS does not invalidate the cached <see cref="Pango.Layout"/> behind a text widget, so a
	/// Forms element whose font changes *after* it has been laid out keeps the size - and, until
	/// something else invalidates it, the glyphs - of the old font. It was first caught as a
	/// de-selected Shell tab still drawn bold, and fixed locally in <c>ShellTabBar</c>; these tests
	/// pin the general fix in <see cref="StyleExtensions.SetFont"/>, which every renderer that maps
	/// a Forms <c>Font</c> goes through.
	///
	/// Every assertion here is a *measurement of the widget's own text layout*, never of a Forms
	/// property or of the style context. The style context reported the new font throughout the
	/// whole time the bug was live - that is precisely why it went unnoticed - so asking it
	/// anything would reproduce the original blindness.
	///
	/// Note also what is deliberately NOT used: <see cref="GtkTestHost.Pump"/> on a toplevel calls
	/// <c>SizeAllocate</c>, and an allocation change clears every label layout in the tree, curing
	/// the staleness before it can be observed (§8.3.1 records this masking the defect in three
	/// separate runs). The bare-widget cases below therefore have no window at all, and the two
	/// renderer cases measure the native layout rather than the allocation.
	/// </summary>
	public class FontLayoutTests : GtkTestBase
	{
		const string Sample = "Ambidextrous handwriting";

		static Pango.FontDescription Font(
			double points,
			Pango.Weight weight = Pango.Weight.Normal,
			Pango.Style style = Pango.Style.Normal) =>
			new Pango.FontDescription
			{
				Size = (int)(points * Pango.Scale.PangoScale),
				Weight = weight,
				Style = style
			};

		/// <summary>
		/// A bare label, shown but never put in a window - see the class remarks for why no toplevel.
		/// <c>Show</c> is not cosmetic here: <c>gtk_widget_get_preferred_width</c> short-circuits to
		/// 0/0 for a non-visible, non-toplevel widget, so an unshown label measures 0 under every
		/// font there is and every assertion below would compare 0 to 0.
		/// </summary>
		static Gtk.Label ShownLabel(string text = Sample)
		{
			var label = new Gtk.Label(text);
			label.Visible = true;

			return label;
		}

		static int NaturalWidth(Gtk.Widget widget)
		{
			widget.GetPreferredWidth(out _, out int natural);
			return natural;
		}

		/// <remarks>
		/// A layout built from the entry's own Pango context, not gtk_entry_get_layout, which Gtk 4
		/// removed - an entry delegates its text to an inner GtkText and no longer exposes the
		/// layout. Same measurement either way: the context carries the resolved font, which is
		/// what these tests vary.
		/// </remarks>
		static int LayoutWidth(Gtk.Entry entry)
		{
			using (var layout = new Pango.Layout(entry.PangoContext))
			{
				layout.FontDescription = entry.PangoContext.FontDescription;
				layout.SetText(entry.Text ?? string.Empty);
				layout.GetPixelSize(out int width, out _);

				return width;
			}
		}

		/// <summary>
		/// The measurement from §8.3.1, as a test: through CSS alone a label taken bold and back
		/// kept the bold width (201 → 236 → 236); it must now return (215 → 253 → 215).
		/// </summary>
		[Fact]
		public void LabelFontWeightRoundTripRestoresTheNaturalWidth()
		{
			Run(() =>
			{
					var label = ShownLabel();

					label.SetFont(Font(11));
					int normal = NaturalWidth(label);

					label.SetFont(Font(11, Pango.Weight.Bold));
					int bold = NaturalWidth(label);

					label.SetFont(Font(11));
					int back = NaturalWidth(label);

					Assert.True(bold > normal,
						$"bold glyphs must measure wider than normal ones: normal={normal} bold={bold}");
					Assert.True(back == normal,
						"a label taken bold and back must return to its normal width - staying at the bold " +
						$"width is the stale cached layout of plan §8.3.1: {normal} -> {bold} -> {back}");
		
			});
		}

		/// <summary>
		/// Size is the other half: a shrinking font must shrink the request, which is the direction
		/// a stale layout hides, because a widget that keeps the larger measurement still looks
		/// plausible on screen.
		/// </summary>
		[Fact]
		public void LabelFontSizeChangeIsReflectedInTheNaturalWidth()
		{
			Run(() =>
			{
					var label = ShownLabel();

					label.SetFont(Font(9));
					int small = NaturalWidth(label);

					label.SetFont(Font(22));
					int large = NaturalWidth(label);

					label.SetFont(Font(9));
					int back = NaturalWidth(label);

					Assert.True(large > small, $"22pt must measure wider than 9pt: {small} vs {large}");
					Assert.True(back == small,
						$"shrinking the font must shrink the request back: {small} -> {large} -> {back}");
		
			});
		}

		[Fact]
		public void EntryFontWeightRoundTripRestoresTheTextLayout()
		{
			Run(() =>
			{
					var entry = new Gtk.Entry { Text = Sample };

					entry.SetFont(Font(11));
					int normal = LayoutWidth(entry);

					entry.SetFont(Font(11, Pango.Weight.Bold));
					int bold = LayoutWidth(entry);

					entry.SetFont(Font(11));
					int back = LayoutWidth(entry);

					Assert.True(bold > normal,
						$"the entry's own Pango layout must widen under bold: normal={normal} bold={bold}");
					Assert.True(back == normal,
						$"and must come back: {normal} -> {bold} -> {back}");
		
			});
		}

		/// <summary>
		/// Clearing the font must clear the attribute list too, or the next theme change is
		/// overridden by a font nobody asked for any more.
		/// </summary>
		[Fact]
		public void ClearingTheFontRemovesTheAttributes()
		{
			Run(() =>
			{
					var label = ShownLabel();
					int themeWidth = NaturalWidth(label);

					label.SetFont(Font(24, Pango.Weight.Bold));
					Assert.True(label.Attributes != null, "the font must be applied as a Pango attribute");
					int styled = NaturalWidth(label);

					label.SetFont(null);

					Assert.True(label.Attributes == null, "a null font must clear the attribute list");
					Assert.True(styled > themeWidth,
						$"24pt bold must be wider than the theme font: theme={themeWidth} styled={styled}");
					Assert.True(NaturalWidth(label) == themeWidth,
						$"clearing must return the label to the theme font's width: {NaturalWidth(label)} != {themeWidth}");
		
			});
		}

		/// <summary>
		/// A label whose text came from <c>GenerateMarkupText</c> carries its font *inside* the
		/// markup. Setting an explicit font-description attribute on top of that overrides it - the
		/// first version of this fix did, and this test caught it - so <c>SetFont</c> takes the
		/// markup path instead and only invalidates the layout.
		/// </summary>
		[Fact]
		public void MarkupAttributesSurviveAFontChange()
		{
			Run(() =>
			{
					var plain = ShownLabel();
					plain.Markup = "plain text";

					var marked = ShownLabel();
					marked.Markup = "<b>plain text</b>";

					var plainBefore = NaturalWidth(plain);
					var markedBefore = NaturalWidth(marked);

					Assert.True(markedBefore > plainBefore,
						"the premise of this test is that bold markup measures wider at all: " +
						$"plain={plainBefore} bold-markup={markedBefore}");

					plain.SetFont(Font(11));
					marked.SetFont(Font(11));

					Assert.True(NaturalWidth(marked) > NaturalWidth(plain),
						"a font applied through SetFont must not flatten the label's own markup: " +
						$"plain={NaturalWidth(plain)} bold-markup={NaturalWidth(marked)}");
		
			});
		}

		/// <summary>
		/// The fail-first evidence, as a standing test: the CSS half of <c>SetFont</c> on its own
		/// does not move a label's measurement at all, so the Pango attributes are not belt and
		/// braces - they are the entire mechanism. Delete them and this suite goes red.
		/// </summary>
		[Fact]
		public void CssAloneDoesNotMoveALabelsMeasurement()
		{
			Run(() =>
			{
					var viaCss = ShownLabel();
					int cssBefore = NaturalWidth(viaCss);
					viaCss.SetStyleProperty("font-size", "28pt");
					int cssAfter = NaturalWidth(viaCss);

					var viaSetFont = ShownLabel();
					int fontBefore = NaturalWidth(viaSetFont);
					viaSetFont.SetFont(Font(28));
					int fontAfter = NaturalWidth(viaSetFont);

					Assert.True(cssAfter == cssBefore,
						"MEASURED: a CSS font-size change alone leaves the label's cached layout untouched " +
						$"({cssBefore} -> {cssAfter}). If this ever starts failing, GTK has changed and the " +
						"attribute path in StyleExtensions.SetFont may no longer be load-bearing.");

					Assert.True(fontAfter > fontBefore,
						"and SetFont, which adds the Pango attribute, must move it: " +
						$"{fontBefore} -> {fontAfter}");
		
			});
		}

		/// <summary>
		/// End to end through the renderer, which is the surface an app actually touches: a
		/// <c>FontAttributes</c> toggle on a live <see cref="Entry"/> - a VisualState setter, a
		/// binding - after the element has been laid out.
		/// </summary>
		[Fact]
		public void FormsEntryFontAttributesChangeAtRuntime()
		{
			Run(() =>
			{
					var entry = new Entry { Text = Sample, FontSize = 14 };

					using (var host = GtkTestHost.HostView(entry))
					{
						var native = host.Control<EntryWrapper>();

						int normal = LayoutWidth(native.Entry);

						entry.FontAttributes = FontAttributes.Bold;
						host.Pump();
						int bold = LayoutWidth(native.Entry);

						entry.FontAttributes = FontAttributes.None;
						host.Pump();
						int back = LayoutWidth(native.Entry);

						Assert.True(bold > normal,
							$"Entry.FontAttributes=Bold must re-lay out the text: normal={normal} bold={bold}");
						Assert.True(back == normal,
							$"and clearing it must undo that: {normal} -> {bold} -> {back}");
					}
		
			});
		}

		/// <summary>
		/// The same for <see cref="Label"/>, which reaches its font through markup rather than
		/// through <c>SetFont</c>. It was never exposed to the CSS hazard; this is here so that a
		/// later "simplification" of <c>LabelExtensions</c> onto the CSS path fails a test instead
		/// of silently reintroducing §8.3.1 on the most common control in the framework.
		/// </summary>
		[Fact]
		public void FormsLabelFontAttributesChangeAtRuntime()
		{
			Run(() =>
			{
					var label = new Label { Text = Sample, FontSize = 14 };

					using (var host = GtkTestHost.HostView(label))
					{
						var native = host.Control<Gtk.Label>();

						int normal = NaturalWidth(native);

						label.FontAttributes = FontAttributes.Bold;
						host.Pump();
						int bold = NaturalWidth(native);

						label.FontAttributes = FontAttributes.None;
						host.Pump();
						int back = NaturalWidth(native);

						Assert.True(bold > normal,
							$"Label.FontAttributes=Bold must re-lay out the text: normal={normal} bold={bold}");
						Assert.True(back == normal,
							$"and clearing it must undo that: {normal} -> {bold} -> {back}");
					}
		
			});
		}
	}
}
