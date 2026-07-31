using NUnit.Framework;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK.Controls;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Coverage target 2 of plan §10.2: Forms property to native property, both at creation and
	/// on change. The "on change" half is the one that matters - a renderer that maps everything
	/// in <c>OnElementChanged</c> and forgets <c>OnElementPropertyChanged</c> looks perfect in a
	/// screenshot of the initial state and is broken for the rest of the app's life.
	/// </summary>
	[TestFixture]
	public class PropertyMappingTests
	{
		[Test]
		public void EntryMapsTextPlaceholderAndPassword()
		{
			var entry = new Entry { Text = "hi", Placeholder = "type here" };

			using (var host = GtkTestHost.HostView(entry))
			{
				var native = host.Control<EntryWrapper>();

				Assert.Multiple(() =>
				{
					Assert.That(native.Entry.Text, Is.EqualTo("hi"));
					Assert.That(native.PlaceholderText, Is.EqualTo("type here"));
					Assert.That(native.Entry.Visibility, Is.True, "a non-password Entry shows its text");
				});

				entry.Text = "changed";
				entry.Placeholder = "other";
				entry.IsPassword = true;
				host.Pump();

				Assert.Multiple(() =>
				{
					Assert.That(native.Entry.Text, Is.EqualTo("changed"));
					Assert.That(native.PlaceholderText, Is.EqualTo("other"));
					Assert.That(native.Entry.Visibility, Is.False, "IsPassword must hide the glyphs");
				});
			}
		}

		[Test]
		public void EntryTextChangesFlowBackFromTheNativeControl()
		{
			var entry = new Entry { Text = "start" };

			using (var host = GtkTestHost.HostView(entry))
			{
				var native = host.Control<EntryWrapper>();

				native.Entry.Text = "typed by the user";
				host.Pump();

				Assert.That(entry.Text, Is.EqualTo("typed by the user"),
					"native edits must reach the Forms element, or two-way bindings never update");
			}
		}

		[Test]
		public void LabelMapsText()
		{
			var label = new Label { Text = "before" };

			using (var host = GtkTestHost.HostView(label))
			{
				var native = host.Control<Gtk.Label>();
				Assert.That(native.Text, Is.EqualTo("before"));

				label.Text = "after";
				host.Pump();

				Assert.That(native.Text, Is.EqualTo("after"));
			}
		}

		/// <summary>
		/// Regression guard for the double-escaping bug in the plan's "Label double-escaping"
		/// section: the text was escaped by the renderer and then again by
		/// <c>GenerateMarkupText</c>, so a Label containing &amp; or quotes displayed the entity
		/// names on screen. Exactly one escape is correct - zero would let Pango parse
		/// &lt;b&gt; as real markup and throw on a bare ampersand.
		/// </summary>
		[Test]
		public void LabelTextIsEscapedExactlyOnce()
		{
			var label = new Label { Text = "He said \"hi\" & <b>x</b> 5 > 3" };

			using (var host = GtkTestHost.HostView(label))
			{
				var native = host.Control<Gtk.Label>();

				Assert.That(native.Text, Is.EqualTo("He said \"hi\" & <b>x</b> 5 > 3"),
					"Gtk.Label.Text reports the rendered text; entity names here mean double escaping");
			}
		}

		[Test]
		public void SwitchMapsIsToggledBothWays()
		{
			var sw = new Switch { IsToggled = false };

			using (var host = GtkTestHost.HostView(sw))
			{
				var native = host.Control<Gtk.CheckButton>();
				Assert.That(native.Active, Is.False);

				sw.IsToggled = true;
				host.Pump();
				Assert.That(native.Active, Is.True, "Forms -> native");

				native.Active = false;
				host.Pump();
				Assert.That(sw.IsToggled, Is.False, "native -> Forms");
			}
		}

		[Test]
		public void CheckBoxMapsIsCheckedBothWays()
		{
			var checkbox = new CheckBox { IsChecked = true };

			using (var host = GtkTestHost.HostView(checkbox))
			{
				var native = host.Control<Gtk.CheckButton>();
				Assert.That(native.Active, Is.True);

				checkbox.IsChecked = false;
				host.Pump();
				Assert.That(native.Active, Is.False, "Forms -> native");

				native.Active = true;
				host.Pump();
				Assert.That(checkbox.IsChecked, Is.True, "native -> Forms");
			}
		}

		[Test]
		public void ProgressBarMapsProgressToFraction()
		{
			var bar = new ProgressBar { Progress = 0.25 };

			using (var host = GtkTestHost.HostView(bar))
			{
				var native = host.Control<Gtk.ProgressBar>();
				Assert.That(native.Fraction, Is.EqualTo(0.25).Within(0.001));

				bar.Progress = 0.75;
				host.Pump();

				Assert.That(native.Fraction, Is.EqualTo(0.75).Within(0.001));
			}
		}

		[Test]
		public void SliderMapsValueAndRange()
		{
			var slider = new Slider { Minimum = 0, Maximum = 100, Value = 10 };

			using (var host = GtkTestHost.HostView(slider))
			{
				var native = host.Control<Gtk.Scale>();

				Assert.Multiple(() =>
				{
					Assert.That(native.Adjustment.Lower, Is.EqualTo(0).Within(0.001));
					Assert.That(native.Adjustment.Upper, Is.EqualTo(100).Within(0.001));
					Assert.That(native.Value, Is.EqualTo(10).Within(0.001));
				});

				slider.Value = 60;
				host.Pump();

				Assert.That(native.Value, Is.EqualTo(60).Within(0.001));
			}
		}

		[Test]
		public void StepperMapsValue()
		{
			var stepper = new Stepper { Minimum = 0, Maximum = 10, Increment = 2, Value = 4 };

			using (var host = GtkTestHost.HostView(stepper))
			{
				var native = host.Control<Gtk.SpinButton>();
				Assert.That(native.Value, Is.EqualTo(4).Within(0.001));

				stepper.Value = 8;
				host.Pump();

				Assert.That(native.Value, Is.EqualTo(8).Within(0.001));
			}
		}

		[Test]
		public void EditorMapsText()
		{
			var editor = new Editor { Text = "line one" };

			using (var host = GtkTestHost.HostView(editor))
			{
				var native = host.Control<ScrolledTextView>();
				Assert.That(native.TextView.Buffer.Text, Is.EqualTo("line one"));

				editor.Text = "line two";
				host.Pump();

				Assert.That(native.TextView.Buffer.Text, Is.EqualTo("line two"));
			}
		}

		[Test]
		public void SearchBarMapsTextAndPlaceholder()
		{
			var search = new SearchBar { Text = "query", Placeholder = "search me" };

			using (var host = GtkTestHost.HostView(search))
			{
				var native = host.Control<SearchEntry>();
				Assert.That(native.SearchText, Is.EqualTo("query"));
				Assert.That(native.PlaceholderText, Is.EqualTo("search me"));

				search.Text = "another";
				host.Pump();

				Assert.That(native.SearchText, Is.EqualTo("another"));
			}
		}

		[Test]
		public void ButtonMapsText()
		{
			var button = new Button { Text = "Tap me" };

			using (var host = GtkTestHost.HostView(button))
			{
				var native = host.Control<Controls.ImageButton>();
				Assert.That(native.LabelWidget.Text, Is.EqualTo("Tap me"));

				button.Text = "Tapped";
				host.Pump();

				Assert.That(native.LabelWidget.Text, Is.EqualTo("Tapped"));
			}
		}

		/// <summary>
		/// The same double-escaping bug the plan records against <c>LabelRenderer</c>, on the
		/// other renderer that builds a <see cref="Span"/> and hands it to
		/// <c>SetTextFromSpan</c>. <c>GenerateMarkupText</c> already escapes with
		/// <c>SecurityElement.Escape</c>, so an outer <c>GLib.Markup.EscapeText</c> turns
		/// &amp; into &amp;amp; on screen.
		/// </summary>
		[Test]
		public void ButtonTextIsEscapedExactlyOnce()
		{
			var button = new Button { Text = "Save & Close" };

			using (var host = GtkTestHost.HostView(button))
			{
				Assert.That(host.Control<Controls.ImageButton>().LabelWidget.Text,
					Is.EqualTo("Save & Close"));
			}
		}

		[Test]
		public void ButtonClickedIsRaisedFromTheNativeClick()
		{
			var button = new Button { Text = "Go" };
			var clicked = false;
			button.Clicked += (s, e) => clicked = true;

			using (var host = GtkTestHost.HostView(button))
			{
				host.Control<Controls.ImageButton>().Activate();
				host.Pump();

				Assert.That(clicked, Is.True, "the native click must reach Button.Clicked");
			}
		}

		[Test]
		public void PickerMapsItemsAndSelection()
		{
			var picker = new Picker();
			picker.Items.Add("alpha");
			picker.Items.Add("beta");
			picker.SelectedIndex = 1;

			using (var host = GtkTestHost.HostView(picker))
			{
				var native = host.Control<Gtk.ComboBox>();

				Assert.That(native.Active, Is.EqualTo(1));

				picker.SelectedIndex = 0;
				host.Pump();

				Assert.That(native.Active, Is.EqualTo(0));
			}
		}
	}
}
