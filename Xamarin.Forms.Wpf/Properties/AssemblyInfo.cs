using Xamarin.Forms;
using Xamarin.Forms.Wpf;

// The WPF presentation namespace, so WPF XAML compiles unchanged: every element of a WPF document names
// its type through this URI (the x: URI is already the one Xamarin.Forms uses).
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Controls")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Controls.Primitives")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Documents")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Shapes")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Media")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Media.Imaging")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Input")]
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Data")]
// WPF resolves its markup extensions ({DynamicResource}, {StaticResource}, {Binding}) through the same URI;
// here they are Xamarin.Forms' own.
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "Xamarin.Forms.Xaml", AssemblyName = "Xamarin.Forms.Xaml")]

namespace Xamarin.Forms.Wpf
{
	internal static class WpfXaml
	{
		internal const string PresentationUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
	}
}
