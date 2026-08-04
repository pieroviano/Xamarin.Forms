using System.Reflection;
using System.Runtime.CompilerServices;

// Keyed on SIGNED_ASSEMBLY, not on #if DEBUG. It used to be #if DEBUG, and the effect was that
// Xamarin.Forms.Xaml.UnitTests could not compile in Release at all - XamlCAssemblyResolver,
// ILContext and ICompiledTypeConverter are all internal - so the suite could only ever run
// against Debug binaries. That made Release the one configuration that ships and the one
// configuration nothing verifies.
//
// Dropping the guard outright is not the answer either. A strong-named assembly may only
// befriend an assembly whose public key it names, so an unqualified name is CS1726 whenever this
// project actually signs. Signing the test assembly to match is a dead end: it references the
// unsigned Core/Xaml, and StrongNamer cannot rewrite those under .NET Core MSBuild, so it would
// trade CS1726 for CS8002-as-error.
//
// The guard therefore stays, and the csproj makes signing opt-in (-p:SignBuildTasks=true) rather
// than automatic outside Debug. Signing off is the default in every MSBuild flavour, so the
// internals are granted in every build the test suite takes part in. Turning signing on is still
// possible and still mutually exclusive with building those tests - that is the documented cost
// of a signed drop, and it is now a deliberate choice instead of what a Visual Studio Release
// build did silently.
//
// SIGNED_ASSEMBLY is derived from the resolved value of SignAssembly, so the guard and the
// signing decision cannot drift apart.
//
// This is an MSBuild task assembly: app authors never reference it, so widening internals to a
// single named test assembly costs nothing at the public API surface.
#if !SIGNED_ASSEMBLY
[assembly: InternalsVisibleTo("Xamarin.Forms.Xaml.UnitTests")]
#endif