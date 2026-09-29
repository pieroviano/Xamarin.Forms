using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xamarin.Forms.Wpf;
using Xunit;
using XF = Xamarin.Forms;

namespace Wpf.UnitTests
{
	/// <summary>The content, button, text and range controls: their views, and the events users and code cause.</summary>
	public class ControlTests : WpfTestBase
	{
		static XF.Label ShellLabel(ContentControl control) => (XF.Label)((NativeShell)control.NativeView).Inner;

		[Fact]
		public void ALabelShowsItsContentWithoutTheAccessKeyUnderscore()
		{
			Run(() =>
			{
				var label = new Label { Content = "_Name and my__file" };

				Assert.Equal("Name and my_file", ShellLabel(label).Text);
				label.Content = "Other";
				Assert.Equal("Other", ShellLabel(label).Text);
			});
		}

		[Fact]
		public void AContentControlHostsAnElementsOwnView()
		{
			Run(() =>
			{
				var inner = new Border();
				var control = new ContentControl { Content = inner };

				Assert.Same(inner.NativeView, ((NativeShell)control.NativeView).Inner);
				Assert.Same(control, inner.Parent);
			});
		}

		[Fact]
		public void ABorderBrushDrawsABorder()
		{
			Run(() =>
			{
				var label = new Label { Content = "x", BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(1) };
				var shell = (NativeShell)label.NativeView;

				Assert.IsType<XF.Frame>(shell.Content);
				label.BorderThickness = new Thickness(0);
				Assert.IsType<XF.Label>(shell.Content);
			});
		}

		[Fact]
		public void AClickOfTheViewIsAClickAndRunsTheCommand()
		{
			Run(() =>
			{
				var command = new RoutedCommand();
				var button = new Button { Content = "_Go", Command = command };
				var window = Host(new StackPanel { Children = { button } });
				var events = new List<string>();
				window.CommandBindings.Add(new CommandBinding(command, (s, e) => events.Add("command"), (s, e) => e.CanExecute = true));
				button.Click += (s, e) => events.Add("click");

				Assert.Equal("Go", ((XF.Button)button.NativeView).Text);
				((XF.IButtonController)button.NativeView).SendClicked();

				Assert.Equal(new[] { "click", "command" }, events);
				window.Close();
			});
		}

		[Fact]
		public void ADisabledButtonDoesNotClick()
		{
			Run(() =>
			{
				var button = new Button { IsEnabled = false };
				var clicked = false;
				button.Click += (s, e) => clicked = true;

				button.PerformClick();

				Assert.False(clicked);
			});
		}

		[Fact]
		public void ACheckBoxTogglesThenRaisesClick()
		{
			Run(() =>
			{
				var box = new CheckBox { Content = "Check" };
				var events = new List<string>();
				box.Checked += (s, e) => events.Add("checked");
				box.Unchecked += (s, e) => events.Add("unchecked");
				box.Click += (s, e) => events.Add("click " + box.IsChecked);
				_ = box.NativeView;

				box.PerformClick();
				box.PerformClick();

				Assert.Equal(new[] { "checked", "click True", "unchecked", "click False" }, events);
			});
		}

		[Fact]
		public void AThreeStateCheckBoxGoesThroughNull()
		{
			Run(() =>
			{
				var box = new CheckBox { IsThreeState = true };
				var states = new List<bool?>();

				for (var i = 0; i < 3; i++)
				{
					box.PerformClick();
					states.Add(box.IsChecked);
				}

				Assert.Equal(new bool?[] { true, null, false }, states);
			});
		}

		[Fact]
		public void TheCheckBoxViewFollowsIsCheckedWithoutAClick()
		{
			Run(() =>
			{
				var box = new CheckBox();
				var clicks = 0;
				box.Click += (s, e) => clicks++;
				var native = (XF.StackLayout)box.NativeView;

				box.IsChecked = true;

				Assert.True(((XF.CheckBox)native.Children[0]).IsChecked);
				Assert.Equal(0, clicks);
			});
		}

		[Fact]
		public void CheckingARadioButtonUnchecksTheOthersOfItsParent()
		{
			Run(() =>
			{
				var a = new RadioButton { IsChecked = true };
				var b = new RadioButton();
				var other = new RadioButton { IsChecked = true };
				var root = new StackPanel { Children = { new StackPanel { Children = { a, b } }, new StackPanel { Children = { other } } } };

				b.IsChecked = true;

				Assert.False(a.IsChecked);
				Assert.True(other.IsChecked);
			});
		}

		[Fact]
		public void RadioButtonsWithAGroupNameAreOneGroupWhereverTheyAre()
		{
			Run(() =>
			{
				var a = new RadioButton { GroupName = "g", IsChecked = true };
				var b = new RadioButton { GroupName = "g" };
				var window = new Window { Content = new StackPanel { Children = { new StackPanel { Children = { a } }, new StackPanel { Children = { b } } } } };

				b.IsChecked = true;

				Assert.False(a.IsChecked);
				window.Close();
			});
		}

		[Fact]
		public void TypingIntoTheEntryIsTheTextAndOneTextChanged()
		{
			Run(() =>
			{
				var box = new TextBox { Text = "a" };
				var changes = 0;
				box.TextChanged += (s, e) => changes++;
				var entry = (XF.Entry)((NativeShell)box.NativeView).Inner;
				Assert.Equal("a", entry.Text);

				entry.Text = "ab";

				Assert.Equal("ab", box.Text);
				Assert.Equal(1, changes);

				box.Text = "abc";
				Assert.Equal("abc", entry.Text);
				Assert.Equal(2, changes);
			});
		}

		[Fact]
		public void TextSetOnAShownTextBoxReachesTheWidget()
		{
			Run(() =>
			{
				var box = new TextBox { Text = "World" };
				var window = Host(new StackPanel { Children = { box } });
				var changes = 0;
				box.TextChanged += (s, e) => changes++;

				box.Text = "Hello, VB6!";
				Pump(window);

				var entry = Assert.Single(GtkWidgets.Descendants<Gtk.Entry>(NativeInput.WidgetOf(box)));
				Assert.Equal("Hello, VB6!", entry.Text);
				Assert.Equal(1, changes);
				window.Close();
			});
		}

		[Fact]
		public void AMultilineTextBoxIsAnEditor()
		{
			Run(() =>
			{
				var box = new TextBox { Text = "x" };
				var shell = (NativeShell)box.NativeView;
				Assert.IsType<XF.Entry>(shell.Inner);

				box.AcceptsReturn = true;

				var editor = Assert.IsType<XF.Editor>(shell.Inner);
				Assert.Equal("x", editor.Text);
			});
		}

		[Fact]
		public void MaxLengthReadOnlyAndCasingReachTheEntry()
		{
			Run(() =>
			{
				var box = new TextBox { MaxLength = 3, IsReadOnly = true, CharacterCasing = CharacterCasing.Upper };
				var entry = (XF.Entry)((NativeShell)box.NativeView).Inner;

				box.Text = "abc";

				Assert.Equal(3, entry.MaxLength);
				Assert.True(entry.IsReadOnly);
				Assert.Equal("ABC", box.Text);
			});
		}

		[Fact]
		public void SettingSelectedTextReplacesTheSelection()
		{
			Run(() =>
			{
				var box = new TextBox { Text = "hello world" };
				box.Select(6, 5);

				box.SelectedText = "there";

				Assert.Equal("hello there", box.Text);
				Assert.Equal(6, box.SelectionStart);
				Assert.Equal(5, box.SelectionLength);
			});
		}

		[Fact]
		public void TheRangeIsClampedAndValueChangedFollowsIt()
		{
			Run(() =>
			{
				var bar = new ScrollBar { Minimum = 0, Maximum = 10, Value = 5 };
				var values = new List<double>();
				bar.ValueChanged += (s, e) => values.Add(e.NewValue);

				bar.Value = 20;
				bar.Maximum = 8;

				Assert.Equal(8, bar.Value);
				Assert.Equal(new[] { 10.0, 8.0 }, values);
			});
		}

		[Fact]
		public void MovingTheSliderViewIsAScroll()
		{
			Run(() =>
			{
				var bar = new ScrollBar { Minimum = 0, Maximum = 100 };
				var scrolls = new List<double>();
				bar.Scroll += (s, e) => scrolls.Add(e.NewValue);
				var slider = (XF.Slider)bar.NativeView;

				slider.Value = 40;

				Assert.Equal(40, bar.Value);
				Assert.Equal(new[] { 40.0 }, scrolls);
			});
		}

		[Fact]
		public void AnEmptyRangeDoesNotBreakTheSlider()
		{
			Run(() =>
			{
				var slider = new Slider { Minimum = 5, Maximum = 5, Value = 5 };

				Assert.Equal(5, ((XF.Slider)slider.NativeView).Value, 6);
				Assert.Equal(5, slider.Maximum);
			});
		}

		[Fact]
		public void AProgressBarShowsTheValueAsAFraction()
		{
			Run(() =>
			{
				var bar = new ProgressBar { Minimum = 10, Maximum = 20, Value = 15 };

				Assert.Equal(0.5, ((XF.ProgressBar)bar.NativeView).Progress, 6);
			});
		}

		[Fact]
		public void AGroupBoxShowsItsHeaderAndItsContent()
		{
			Run(() =>
			{
				var content = new Grid();
				var box = new GroupBox { Header = "_Mode", Content = content };
				var root = (XF.Grid)box.NativeView;

				Assert.Contains(root.Children, v => v is XF.Label l && l.Text == "Mode");
				Assert.Contains(root.Children, v => v is XF.ContentView c && c.Content == content.NativeView);
			});
		}

		[Fact]
		public void FontsAreSetInPointsFromWpfsDeviceIndependentPixels()
		{
			Run(() =>
			{
				var text = new TextBlock { Text = "x", FontSize = 16, FontWeight = FontWeights.Bold, FontFamily = new System.Windows.Media.FontFamily("Sans, Arial") };
				var label = (XF.Label)text.NativeView;

				Assert.Equal(12, label.FontSize);
				Assert.Equal(XF.FontAttributes.Bold, label.FontAttributes);
				Assert.Equal("Sans", label.FontFamily);
			});
		}

		[Fact]
		public void WithNoFontSetTheViewKeepsTheThemesFont()
		{
			Run(() =>
			{
				var label = (XF.Label)new TextBlock { Text = "x" }.NativeView;

				Assert.False(label.IsSet(XF.Label.FontSizeProperty));
				Assert.False(label.IsSet(XF.Label.FontFamilyProperty));
			});
		}
	}
}
