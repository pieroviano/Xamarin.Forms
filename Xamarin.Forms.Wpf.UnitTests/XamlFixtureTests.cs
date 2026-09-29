using System;
using System.Windows;
using System.Windows.Controls;
using Showcase.Forms;
using Xunit;
using WpfLabel = System.Windows.Controls.Label;

namespace Wpf.UnitTests
{
	/// <summary>
	/// WPF XAML as Vb6ToCSharp writes it, compiled by XamlC and loaded at run time: every attribute lands on the
	/// WPF property it names. The fixture is a converted form byte for byte (Fixtures/FrmMain.xaml).
	/// </summary>
	public class XamlFixtureTests : GtkTestBase
	{
		public static readonly TheoryData<string> Forms = new TheoryData<string> { "compiled", "loaded" };

		static Window Create(string how) => how == "compiled" ? (Window)new frmMain() : new frmMainLoaded();

		static T Named<T>(Window window, string name) where T : class =>
			Assert.IsType<T>(window.FindName(name));

		[Theory]
		[MemberData(nameof(Forms))]
		public void TheWindowAttributesAreApplied(string how)
		{
			Run(() =>
			{
				var window = Create(how);

				Assert.Equal("Showcase", window.Title);
				Assert.Equal(SizeToContent.WidthAndHeight, window.SizeToContent);
				Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);
				Assert.Equal(ResizeMode.NoResize, window.ResizeMode);
				window.Close();
			});
		}

		[Theory]
		[MemberData(nameof(Forms))]
		public void ADynamicSystemColorResolvesToTheSystemBrush(string how)
		{
			Run(() =>
			{
				var window = Create(how);

				// The same instance, which is what lets converted code recognise a system color.
				Assert.Same(SystemColors.ControlBrush, window.Background);
				window.Close();
			});
		}

		[Theory]
		[MemberData(nameof(Forms))]
		public void LayoutAttributesAreApplied(string how)
		{
			Run(() =>
			{
				var window = Create(how);
				var root = Named<Grid>(window, "LayoutRoot");
				var combo = Named<ComboBox>(window, "cboShade");

				Assert.Same(window, root.Parent);
				Assert.Equal(408, root.Width);
				Assert.Equal(297, root.Height);
				Assert.Equal(HorizontalAlignment.Left, root.HorizontalAlignment);
				Assert.Equal(new Thickness(8, 84, 0, 0), combo.Margin);
				Assert.Equal(161, combo.Width);
				Assert.Equal(23, combo.Height);
				Assert.Equal(VerticalAlignment.Top, combo.VerticalAlignment);
				Assert.Equal(7, combo.TabIndex);
				Assert.False(combo.IsEditable);
				window.Close();
			});
		}

		[Theory]
		[MemberData(nameof(Forms))]
		public void ContentAttributesAreApplied(string how)
		{
			Run(() =>
			{
				var window = Create(how);

				Assert.Equal("World", Named<TextBox>(window, "txtName").Text);
				Assert.Equal("Name:", Named<WpfLabel>(window, "lblName").Content);
				Assert.Equal(new Thickness(0), Named<WpfLabel>(window, "lblName").Padding);
				Assert.Equal("_Run", Named<Button>(window, "cmdRun").Content);
				Assert.True(Named<Button>(window, "cmdRun").IsDefault);
				Assert.True(Named<Button>(window, "cmdClose").IsCancel);
				Assert.True(Named<RadioButton>(window, "optA").IsChecked);
				Assert.False(Named<RadioButton>(window, "optB").IsChecked);
				Assert.Equal("Mode", Named<GroupBox>(window, "fraMode").Header);
				window.Close();
			});
		}

		[Theory]
		[MemberData(nameof(Forms))]
		public void NestedElementsFormTheLogicalTree(string how)
		{
			Run(() =>
			{
				var window = Create(how);
				var frame = Named<GroupBox>(window, "fraMode");
				var inner = Assert.IsType<Grid>(frame.Content);
				var option = Named<RadioButton>(window, "optA");

				Assert.Same(inner, option.Parent);
				Assert.Same(frame, inner.Parent);
				Assert.Equal(new Thickness(-6, -17, -6, -6), inner.Margin);
				Assert.Same(window, Window.GetWindow(option));
				Assert.Equal(8, Named<Grid>(window, "LayoutRoot").Children.Count);
				window.Close();
			});
		}
	}
}
