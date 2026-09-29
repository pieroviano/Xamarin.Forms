using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.UnitTests.Fixtures;
using Xunit;
using WpfLabel = System.Windows.Controls.Label;

namespace Wpf.UnitTests
{
	/// <summary>
	/// Every element and attribute Vb6ToCSharp's WpfEmitter can write (Fixtures/AllControls.xaml) compiles through
	/// XamlC and lands on its WPF property; the window then shows.
	/// </summary>
	public class EmittedXamlTests : WpfTestBase
	{
		static T Named<T>(Window window, string name) where T : class => Assert.IsType<T>(window.FindName(name));

		[Fact]
		public void WindowAttributes()
		{
			Run(() =>
			{
				var w = new AllControls();

				Assert.Equal(WindowStartupLocation.Manual, w.WindowStartupLocation);
				Assert.Equal(10, w.Left);
				Assert.Equal(ResizeMode.CanMinimize, w.ResizeMode);
				Assert.Equal(WindowStyle.ToolWindow, w.WindowStyle);
				Assert.False(w.ShowInTaskbar);
				Assert.Equal("Fixtures/Pixel.png", ((BitmapImage)w.Icon).UriSource.OriginalString);
				Assert.Equal(Color.FromRgb(0xC0, 0xC0, 0xC0), ((SolidColorBrush)w.Background).Color);
				Assert.Same(SystemColors.WindowTextBrush, w.Foreground);
				Assert.Equal("MS Sans Serif", w.FontFamily.Source);
				Assert.Equal(10.67, w.FontSize, 3);
				Assert.Equal(FontWeights.Bold, w.FontWeight);
				Assert.Equal(FontStyles.Italic, w.FontStyle);
				w.Close();
			});
		}

		[Fact]
		public void MenuAttributes()
		{
			Run(() =>
			{
				var w = new AllControls();
				var file = Named<MenuItem>(w, "mnuFile");

				Assert.Equal(4, file.Items.Count);
				Assert.Equal("Ctrl+O", Named<MenuItem>(w, "mnuOpen").InputGestureText);
				Assert.IsType<Separator>(file.Items[1]);
				Assert.True(Named<MenuItem>(w, "mnuCheck").IsChecked);
				Assert.Equal(Visibility.Collapsed, Named<MenuItem>(w, "mnuHidden").Visibility);
				Assert.Equal(Dock.Top, DockPanel.GetDock((Menu)file.Parent));
				w.Close();
			});
		}

		[Fact]
		public void CommonAttributes()
		{
			Run(() =>
			{
				var w = new AllControls();
				var lbl = Named<WpfLabel>(w, "lbl");

				Assert.False(lbl.IsTabStop);
				Assert.False(lbl.IsEnabled);
				Assert.Equal(Visibility.Hidden, lbl.Visibility);
				Assert.Same(SystemColors.ControlBrush, lbl.Background);
				Assert.Equal("tip", lbl.ToolTip);
				Assert.Equal("tag", lbl.Tag);
				Assert.Same(System.Windows.Input.Cursors.Hand, lbl.Cursor);
				Assert.Equal("Arial", lbl.FontFamily.Source);
				Assert.Equal(HorizontalAlignment.Right, lbl.HorizontalContentAlignment);
				Assert.Equal(new Thickness(1), lbl.BorderThickness);
				w.Close();
			});
		}

		[Fact]
		public void ControlAttributes()
		{
			Run(() =>
			{
				var w = new AllControls();

				var txt = Named<TextBox>(w, "txt");
				Assert.True(txt.AcceptsReturn);
				Assert.Equal(TextWrapping.Wrap, txt.TextWrapping);
				Assert.Equal(ScrollBarVisibility.Visible, txt.VerticalScrollBarVisibility);
				Assert.Equal(10, txt.MaxLength);
				Assert.Equal(TextAlignment.Right, txt.TextAlignment);

				Assert.Null(Named<CheckBox>(w, "chk").IsChecked);
				Assert.True(Named<CheckBox>(w, "chk").IsThreeState);
				Assert.True(Named<CheckBox>(w, "chk2").IsChecked);
				Assert.Equal("b", Named<ComboBox>(w, "cbo").Text);
				Assert.Equal(SelectionMode.Extended, Named<ListBox>(w, "lst").SelectionMode);
				Assert.Equal(2, Named<ListBox>(w, "lst").Items.Count);
				Assert.Equal(5, Canvas.GetLeft(Named<WpfLabel>(w, "inCanvas")));
				Assert.IsType<ImageBrush>(Named<Canvas>(w, "pic").Background);
				Assert.Equal(Stretch.None, Named<Image>(w, "img").Stretch);
				Assert.Equal(Orientation.Horizontal, Named<ScrollBar>(w, "hsb").Orientation);
				Assert.Equal(32767, Named<ScrollBar>(w, "vsb").Maximum);
				Assert.Equal(2, ((GridView)Named<ListView>(w, "lvw").View).Columns.Count);
				Assert.Equal(96, ((GridView)Named<ListView>(w, "lvw").View).Columns[0].Width);
				Assert.Equal(30, Named<ProgressBar>(w, "prg").Value);
				Assert.Equal(TickPlacement.BottomRight, Named<Slider>(w, "sld").TickPlacement);
				Assert.Equal(2, Named<StatusBar>(w, "sbr").Items.Count);
				Assert.Equal(3, Named<ToolBar>(w, "tbr").Items.Count);
				Assert.Equal(2, Named<TabControl>(w, "tab").Items.Count);
				Assert.Same(Named<TabControl>(w, "tab").Items[0], ((Grid)Named<WpfLabel>(w, "onFirstTab").Parent).Parent);
				Assert.Equal(new DateTime(2020, 1, 2), Named<DatePicker>(w, "dtp").SelectedDate);
				Assert.True(Named<RichTextBox>(w, "rtb").IsReadOnly);
				Assert.Equal("rich", new System.Windows.Documents.TextRange(Named<RichTextBox>(w, "rtb").Document.ContentStart, Named<RichTextBox>(w, "rtb").Document.ContentEnd).Text);
				Assert.Equal(new[] { 4.0, 2.0 }, Named<Line>(w, "lin").StrokeDashArray);
				Assert.Equal(2, Named<Rectangle>(w, "shp").RadiusX);
				Assert.Same(SystemColors.ControlDarkBrush, Named<Ellipse>(w, "ell").Stroke);
				Assert.Equal(new Thickness(1), Named<Border>(w, "dat").BorderThickness);
				w.Close();
			});
		}

		[Fact]
		public void AProjectUserControlKeepsItsOwnXamlAndTheFormsAttributes()
		{
			Run(() =>
			{
				var w = new AllControls();
				var widget = Named<Widget>(w, "usr");

				Assert.Equal(30, widget.Width);
				Assert.Equal(Color.FromArgb(0xFF, 0xFF, 0xFF, 0x80), ((SolidColorBrush)widget.Background).Color);
				Assert.Equal("Widget", widget.caption.Content);
				w.Close();
			});
		}

		[Fact]
		public void TheFieldModifierMakesAFieldInternalAndItsAbsenceLeavesItPrivate()
		{
			var caption = typeof(Widget).GetField("caption", BindingFlags.Instance | BindingFlags.NonPublic);
			var root = typeof(Widget).GetField("LayoutRoot", BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.True(caption.IsAssembly);
			Assert.True(root.IsPrivate);
		}

		[Fact]
		public void TheWholeWindowShowsAndCloses()
		{
			Run(() =>
			{
				var w = new AllControls();
				w.Show();
				Pump(w);

				Assert.True(PumpUntil(() => w.IsLoaded, w));
				Assert.True(Named<TabControl>(w, "tab").IsLoaded);
				Assert.NotNull(System.Windows.Input.NativeInput.WidgetOf(Named<Button>(w, "btn")));
				w.Close();
				Assert.False(w.IsLoaded);
			});
		}
	}
}
