using System.Reflection;
using System.Runtime.CompilerServices;

// Keyed on SIGNED_ASSEMBLY, not on #if DEBUG. It used to be #if DEBUG, and the effect was that
// Xamarin.Forms.Xaml.UnitTests could not compile in Release at all - XamlCAssemblyResolver,
// ILContext and ICompiledTypeConverter are all internal - so `dotnet build Xamarin.Forms.Gtk.sln
// -c Release` failed with CS0122 and the suite could only ever run against Debug binaries. That
// made Release the one configuration that ships and the one configuration nothing verifies.
//
// Dropping the guard outright is not the answer either. This project strong-names itself once
// .NET Framework MSBuild builds it outside Debug (the SignAssembly groups in the csproj, and the
// `sn -R` re-sign at the end of it), and a strong-named assembly may only befriend an assembly
// whose public key it names - so an unqualified name is CS1726 there. That is a Visual Studio
// Release build, which is why `dotnet build -c Release` never saw it: MSBuildRuntimeType is Core
// there and signing is switched off. Signing the test assembly to match is a dead end - it
// references the unsigned Core/Xaml, and StrongNamer cannot rewrite those under .NET Core
// MSBuild, so it would trade CS1726 for CS8002-as-error.
//
// So the internals are granted in exactly the configurations that do NOT strong-name, which is
// every configuration the test suite is built in. The csproj derives SIGNED_ASSEMBLY from the
// resolved value of SignAssembly, so the guard and the signing decision cannot drift apart.
//
// This is an MSBuild task assembly: app authors never reference it, so widening internals to a
// single named test assembly costs nothing at the public API surface.
#if !SIGNED_ASSEMBLY
[assembly: InternalsVisibleTo("Xamarin.Forms.Xaml.UnitTests")]
#endif