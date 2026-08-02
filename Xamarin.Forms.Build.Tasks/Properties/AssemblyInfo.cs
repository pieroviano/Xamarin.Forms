using System.Reflection;
using System.Runtime.CompilerServices;

// Deliberately NOT inside #if DEBUG. It used to be, and the effect was that
// Xamarin.Forms.Xaml.UnitTests could not compile in Release at all - XamlCAssemblyResolver,
// ILContext and ICompiledTypeConverter are all internal - so `dotnet build Xamarin.Forms.Gtk.sln
// -c Release` failed with CS0122 and the suite could only ever run against Debug binaries. That
// made Release the one configuration that ships and the one configuration nothing verifies.
//
// This is an MSBuild task assembly: app authors never reference it, so widening internals to a
// single named test assembly costs nothing at the public API surface.
[assembly: InternalsVisibleTo("Xamarin.Forms.Xaml.UnitTests")]