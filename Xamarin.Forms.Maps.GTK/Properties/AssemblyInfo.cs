using System.Reflection;
using System.Runtime.InteropServices;
using Xamarin.Forms;
using Xamarin.Forms.Maps.GTK;

// AssemblyTitle and AssemblyDescription are NOT declared here any more. GenerateAssemblyInfo is
// on for this project so the SDK emits them - along with AssemblyVersion, FileVersion and
// InformationalVersion, which is the point: with generation off and no version attributes in this
// file, the assembly shipped as 0.0.0.0 while the rest of the framework was 2.0.0.0. Title comes
// from $(AssemblyName) and description from $(Description) in the csproj.
[assembly: ComVisible(false)]
[assembly: Guid("a9772bb1-0e17-42f5-a6db-60bfccbfdb9d")]

[assembly: ExportRenderer(typeof(Xamarin.Forms.Maps.Map), typeof(MapRenderer))]
