using System.Reflection;
using System.Runtime.InteropServices;
using Xamarin.Forms;
using Xamarin.Forms.Maps.GTK;

[assembly: AssemblyTitle("Xamarin.Forms.Maps.GTK")]
[assembly: AssemblyDescription("GTK3 map renderer for Xamarin.Forms.Maps")]
[assembly: ComVisible(false)]
[assembly: Guid("a9772bb1-0e17-42f5-a6db-60bfccbfdb9d")]

[assembly: ExportRenderer(typeof(Xamarin.Forms.Maps.Map), typeof(MapRenderer))]
