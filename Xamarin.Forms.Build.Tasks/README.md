# Net4x.Xamarin.Forms.Build.Tasks

The build-time half of Xamarin.Forms XAML: MSBuild tasks and targets that generate the
code-behind partial class and compile XAML into IL.

```
dotnet add package Net4x.Xamarin.Forms.Build.Tasks
```

This is an MSBuild task package, not a library — it ships no `lib/` folder and adds no
compile-time reference. Its `build/` targets are imported automatically on install; nothing else
is needed to wire it up.

## What it does

| Task | Role |
| --- | --- |
| `XamlGTask` | Generates the `InitializeComponent()` method and the `x:Name` backing fields as a partial class |
| `CssGTask` | Same for CSS `StyleSheet` resources |
| `XamlCTask` | Compiles XAML to IL at build time (Mono.Cecil), replacing runtime parsing |
| `DebugXamlCTask` | Emits the debug-friendly variant; opt in via `build\Xamarin.Forms.Debug.targets` |

XAML files are picked up automatically in SDK-style projects (`**\*.xaml` become
`EmbeddedResource`, paired with their `.xaml.cs`). XAML problems are reported as MSBuild errors
with source positions rather than runtime exceptions.

Whether a page is compiled or parsed at runtime is chosen per assembly or per type with
`[XamlCompilation(XamlCompilationOptions.Compile)]`, from `Net4x.Xamarin.Forms.Xaml`.

## Properties

| Property | Effect |
| --- | --- |
| `EnableDefaultXamlItems` | Set to `False` to stop globbing `**\*.xaml` (default `True`) |
| `EnableDefaultCssItems` | Same for `**\*.css` (default `True`) |
| `XFKeepXamlResources` | Keep the XAML as an embedded resource after compiling it |
| `XFXamlCValidateOnly` | Validate the XAML without emitting compiled IL |
| `XFDisableTargetsValidation` | Skip the duplicate-import and target-framework checks |
| `_XFBuildTasksLocation` | Directory the task assembly is loaded from; defaults to the package's own `build\<tfm>\` |

Importing the targets twice is reported as error `XF001`.

## Package layout

```
build/
  Net4x.Xamarin.Forms.Build.Tasks.props     NuGet auto-import entry point
  Net4x.Xamarin.Forms.Build.Tasks.targets   NuGet auto-import entry point
  Xamarin.Forms.props / .targets            the actual logic
  Xamarin.Forms.DefaultItems.props/.targets XAML/CSS default globs
  Xamarin.Forms.Debug.targets               opt-in debug XAML compilation
  netstandard2.0/                           task assembly + Mono.Cecil + System.CodeDom
```

The entry points are thin forwarders: NuGet only auto-imports `build/<PackageId>.props|.targets`,
while the files carrying the logic keep their historical `Xamarin.Forms.*` names so projects that
import them directly keep working.

## About this fork

Maintenance fork of the `5.0.0` branch of [xamarin/Xamarin.Forms](https://github.com/pieroviano/Xamarin.Forms),
published under the `Net4x.` package prefix. Upstream Xamarin.Forms reached end of support on
**May 1, 2024** and was succeeded by [.NET MAUI](https://github.com/dotnet/maui); new
applications should target .NET MAUI instead.
