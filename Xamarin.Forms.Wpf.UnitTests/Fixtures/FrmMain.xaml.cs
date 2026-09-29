using System.Windows;
using Xamarin.Forms.Xaml;

// The namespace and class of the converted form this XAML comes from (VB6\Showcase in Vb6ToCSharp, converted with
// --ui wpf): the XAML is that file byte for byte, which is the point of the fixture.
namespace Showcase.Forms
{
	/// <summary>The converted form, its XAML compiled by XamlC.</summary>
	public partial class frmMain : Window
	{
		public frmMain() => InitializeComponent();
	}

	/// <summary>The same XAML loaded at run time, the path a project without XamlC takes.</summary>
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class frmMainLoaded : Window
	{
		public frmMainLoaded() => InitializeComponent();
	}
}
