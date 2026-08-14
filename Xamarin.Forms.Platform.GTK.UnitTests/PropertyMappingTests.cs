using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK.Controls;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Coverage target 2 of plan §10.2: Forms property to native property, both at creation and
	/// on change. The "on change" half is the one that matters - a renderer that maps everything
	/// in <c>OnElementChanged</c> and forgets <c>OnElementPropertyChanged</c> looks perfect in a
	/// screenshot of the initial state and is broken for the rest of the app's life.
	/// </summary>
	public class PropertyMappingTests : GtkTestBase
	{
		[Fact]
		public void EntryMapsTextPlaceholderAndPassword()
		{
			Run(() =>
			{
					var entry = new Entry { Text = "hi", Placeholder = "type here" };

					using (var host = GtkTestHost.HostView(entry))
					{
						var native = host.Control<EntryWrapper>();

						Assert.Equal("hi", native.Entry.Text);
						Assert.Equal("type here", native.PlaceholderText);
						Assert.True(native.Entry.Visibility, "a non-password Entry shows its text");

						entry.Text = "changed";
						entry.Placeholder = "other";
						entry.IsPassword = true;
						host.Pump();

						Assert.Equal("changed", native.Entry.Text);
						Assert.Equal("other", native.PlaceholderText);
						Assert.False(native.Entry.Visibility, "IsPassword must hide the glyphs");
					}
			});
		}

		[Fact]
		public void EntryTextChangesFlowBackFromTheNativeControl()
		{
			Run(() =>
			{
					var entry = new Entry { Text = "start" };

					using (var host = GtkTestHost.HostView(entry))
					{
						var native = host.Control<EntryWrapper>();

						native.Entry.Text = "typed by the user";
						host.Pump();

						Assert.True(entry.Text == "typed by the user",
							"native edits must reach the Forms element, or two-way bindings never update");
					}
			});
		}

		[Fact]
		public void LabelMapsText()
		{
			Run(() =>
			{
					var label = new Label { Text = "before" };

					using (var host = GtkTestHost.HostView(label))
					{
						var native = host.Control<Gtk.Label>();
						Assert.Equal("before", native.Text);

						label.Text = "after";
						host.Pump();

						Assert.Equal("after", native.Text);
					}
			});
		}

		/// <summary>
		/// Regression guard for the double-escaping bug in the plan's "Label double-escaping"
		/// section: the text was escaped by the renderer and then again by
		/// <c>GenerateMarkupText</c>, so a Label containing &amp; or quotes displayed the entity
		/// names on screen. Exactly one escape is correct - zero would let Pango parse
		/// &lt;b&gt; as real markup and throw on a bare ampersand.
		/// </summary>
		[Fact]
		public void LabelTextIsEscapedExactlyOnce()
		{
			Run(() =>
			{
					var label = new Label { Text = "He said \"hi\" & <b>x</b> 5 > 3" };

					using (var host = GtkTestHost.HostView(label))
					{
						var native = host.Control<Gtk.Label>();

						Assert.True(native.Text == "He said \"hi\" & <b>x</b> 5 > 3",
							"Gtk.Label.Text reports the rendered text; entity names here mean double " +
							$"escaping. Got: {native.Text}");
					}
			});
		}

		[Fact]
		public void SwitchMapsIsToggledBothWays()
		{
			Run(() =>
			{
					var sw = new Switch { IsToggled = false };

					using (var host = GtkTestHost.HostView(sw))
					{
						var native = host.Control<Gtk.CheckButton>();
						Assert.False(native.Active);

						sw.IsToggled = true;
						host.Pump();
						Assert.True(native.Active, "Forms -> native");

						native.Active = false;
						host.Pump();
						Assert.False(sw.IsToggled, "native -> Forms");
					}
			});
		}

		[Fact]
		public void ProgressBarMapsProgressToFraction()
		{
			Run(() =>
			{
					var bar = new ProgressBar { Progress = 0.25 };

					using (var host = GtkTestHost.HostView(bar))
					{
						var native = host.Control<Gtk.ProgressBar>();
						Assert.Equal(0.25, native.Fraction, 3);

						bar.Progress = 0.75;
						host.Pump();

						Assert.Equal(0.75, native.Fraction, 3);
					}
			});
		}

		[Fact]
		public void SliderMapsValueAndRange()
		{
			Run(() =>
			{
					var slider = new Slider { Minimum = 0, Maximum = 100, Value = 10 };

					using (var host = GtkTestHost.HostView(slider))
					{
						var native = host.Control<Gtk.Scale>();

						Assert.Equal(0, native.Adjustment.Lower, 3);
						Assert.Equal(100, native.Adjustment.Upper, 3);
						Assert.Equal(10, native.Value, 3);

						slider.Value = 60;
						host.Pump();

						Assert.Equal(60, native.Value, 3);
					}
			});
		}

		[Fact]
		public void StepperMapsValue()
		{
			Run(() =>
			{
					var stepper = new Stepper { Minimum = 0, Maximum = 10, Increment = 2, Value = 4 };

					using (var host = GtkTestHost.HostView(stepper))
					{
						var native = host.Control<Gtk.SpinButton>();
						Assert.Equal(4, native.Value, 3);

						stepper.Value = 8;
						host.Pump();

						Assert.Equal(8, native.Value, 3);
					}
			});
		}

		/// <summary>
		/// Both halves of this caught the same real defect. <c>gtk_text_buffer_set_text</c> deletes
		/// and then inserts, and both steps raise <c>Changed</c>; the renderer's write-back handler
		/// saw the momentarily-empty buffer and pushed "" into <c>Element.Text</c> mid-assignment,
		/// so a programmatic set left the control empty and any binding observed a spurious "".
		/// </summary>
		[Fact]
		public void EditorMapsText()
		{
			Run(() =>
			{
					var editor = new Editor { Text = "line one" };

					using (var host = GtkTestHost.HostView(editor))
					{
						var native = host.Control<ScrolledTextView>();
						Assert.Equal("line one", native.TextView.Buffer.Text);

						var observed = new List<string>();
						editor.PropertyChanged += (s, e) =>
						{
							if (e.PropertyName == Editor.TextProperty.PropertyName)
								observed.Add(editor.Text);
						};

						editor.Text = "line two";
						host.Pump();

						Assert.Equal("line two", native.TextView.Buffer.Text);
						Assert.Equal("line two", editor.Text);
						Assert.True(!observed.Contains(string.Empty),
							"a binding on Editor.Text saw a spurious empty value: " +
							$"[{string.Join(", ", observed.Select(v => $"\"{v}\""))}]");
					}
			});
		}

		[Fact]
		public void EditorTextChangesFlowBackFromTheNativeControl()
		{
			Run(() =>
			{
					var editor = new Editor { Text = "start" };

					using (var host = GtkTestHost.HostView(editor))
					{
						// The guard added for the test above must not deafen the renderer to real edits.
						host.Control<ScrolledTextView>().TextView.Buffer.Text = "typed by the user";
						host.Pump();

						Assert.Equal("typed by the user", editor.Text);
					}
			});
		}

		[Fact]
		public void SearchBarMapsTextAndPlaceholder()
		{
			Run(() =>
			{
					var search = new SearchBar { Text = "query", Placeholder = "search me" };

					using (var host = GtkTestHost.HostView(search))
					{
						var native = host.Control<SearchEntry>();
						Assert.Equal("query", native.SearchText);
						Assert.Equal("search me", native.PlaceholderText);

						search.Text = "another";
						host.Pump();

						Assert.Equal("another", native.SearchText);
					}
			});
		}

		[Fact]
		public void ButtonMapsText()
		{
			Run(() =>
			{
					var button = new Button { Text = "Tap me" };

					using (var host = GtkTestHost.HostView(button))
					{
						var native = host.Control<Controls.ImageButton>();
						Assert.Equal("Tap me", native.LabelWidget.Text);

						button.Text = "Tapped";
						host.Pump();

						Assert.Equal("Tapped", native.LabelWidget.Text);
					}
			});
		}

		/// <summary>
		/// The same double-escaping bug the plan records against <c>LabelRenderer</c>, on the
		/// other renderer that builds a <see cref="Span"/> and hands it to
		/// <c>SetTextFromSpan</c>. <c>GenerateMarkupText</c> already escapes with
		/// <c>SecurityElement.Escape</c>, so an outer <c>GLib.Markup.EscapeText</c> turns
		/// &amp; into &amp;amp; on screen.
		/// </summary>
		[Fact]
		public void ButtonTextIsEscapedExactlyOnce()
		{
			Run(() =>
			{
					var button = new Button { Text = "Save & Close" };

					using (var host = GtkTestHost.HostView(button))
					{
						Assert.Equal("Save & Close", host.Control<Controls.ImageButton>().LabelWidget.Text);
					}
			});
		}

		[Fact]
		public void ButtonClickedIsRaisedFromTheNativeClick()
		{
			Run(() =>
			{
					var button = new Button { Text = "Go" };
					var clicked = false;
					button.Clicked += (s, e) => clicked = true;

					using (var host = GtkTestHost.HostView(button))
					{
						// Emit the signal ButtonRenderer actually subscribes to (Control.Clicked).
						// Gtk.Widget.Activate() is not enough: it is a no-op for a widget that is not
						// the activatable focus target, and measured as such here - the handler never ran.
						GLib.Signal.Emit(host.Control<Controls.ImageButton>(), "clicked");
						host.Pump();

						Assert.True(clicked, "the native click must reach Button.Clicked");
					}
			});
		}

		[Fact]
		public void PickerMapsItemsAndSelection()
		{
			Run(() =>
			{
					var picker = new Picker();
					picker.Items.Add("alpha");
					picker.Items.Add("beta");
					picker.SelectedIndex = 1;

					using (var host = GtkTestHost.HostView(picker))
					{
						var native = host.Control<Gtk.ComboBox>();

						Assert.Equal(1, native.Active);

						picker.SelectedIndex = 0;
						host.Pump();

						Assert.Equal(0, native.Active);
					}
			});
		}
		[Fact]
		public void EditorEnforcesMaxLength()
		{
			Run(() =>
			{
					// MaxLength was wired up but enforced nothing: the insert-text handler assigned
					// args.RetVal, which GtkTextBuffer ignores because that signal returns void, and
					// compared the length of the INSERTED text rather than of the resulting buffer.
					var editor = new Editor { MaxLength = 5 };

					using (var host = GtkTestHost.HostView(editor))
					{
						var native = host.Control<ScrolledTextView>();

						native.TextView.Buffer.Text = "0123456789";
						host.Pump();

						Assert.Equal(5, native.TextView.Buffer.CharCount);
						Assert.Equal("01234", native.TextView.Buffer.Text);
					}
			});
		}

		[Fact]
		public void EditorWithoutMaxLengthKeepsItsText()
		{
			Run(() =>
			{
					// Guards the other half: the backing field defaults to 0, so a truncating
					// implementation that ran before the renderer pushed MaxLength down would empty
					// every Editor.
					var editor = new Editor();

					using (var host = GtkTestHost.HostView(editor))
					{
						var native = host.Control<ScrolledTextView>();

						native.TextView.Buffer.Text = "the quick brown fox";
						host.Pump();

						Assert.Equal("the quick brown fox", native.TextView.Buffer.Text);
					}
			});
		}

	}
}
