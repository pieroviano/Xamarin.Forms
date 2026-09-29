using System.Windows;
using System.Windows.Controls;

namespace Wpf.UnitTests.Fixtures
{
	/// <summary>Every element and attribute Vb6ToCSharp's WpfEmitter writes, in one window.</summary>
	internal partial class AllControls : Window
	{
		public AllControls() => InitializeComponent();
	}

	/// <summary>A project user control, as the emitter writes one (usercontrols:Name in a form).</summary>
	internal partial class Widget : UserControl
	{
		public Widget() => InitializeComponent();
	}
}
