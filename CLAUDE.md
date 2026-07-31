# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository

Fork of `xamarin/Xamarin.Forms` (origin: `pieroviano/Xamarin.Forms`), working on the **`5.0.0`** branch, which is also the default/PR-target branch here. Upstream Xamarin.Forms reached end of support on May 1, 2024 and was succeeded by .NET MAUI; this repo is a maintenance fork of the 5.0.0 line.

Xamarin.Forms is a cross-platform UI framework: a shared, portable UI abstraction (`Xamarin.Forms.Core`) is mapped at runtime onto native controls by per-platform *renderers* (Android, iOS, UWP/UAP, macOS, WPF, GTK, Tizen).

## Build

Toolchain is MSBuild + [Cake](https://cakebuild.net) (`build.cake`), driven by `build.cmd` / `build.ps1` / `build.sh`, or `dotnet cake` (Cake.Tool 1.0.0 is pinned in `.config/dotnet-tools.json`). `global.json` pins .NET SDK 5.0.102 (`rollForward: latestFeature`) and `MSBuild.Sdk.Extras` 3.0.44.

```powershell
build.cmd -Target provision        # install missing platform SDKs (long)
build.cmd -Target BuildTasks       # build Xamarin.Forms.Build.Tasks ONLY
build.cmd -Target Build            # restore + build the whole solution
build.cmd -Target BuildForNuget    # what CI builds before packing
build.cmd -Target NuGetPack        # default target; produces packages in Nuget/
build.cmd -Target cg-android       # build the Android Control Gallery
build.cmd -Target cg-ios           # build the iOS Control Gallery
build.cmd -Target VSMAC            # bootstrap for Visual Studio for Mac
```

Plain MSBuild also works: `msbuild /restore Xamarin.Forms.sln` (on macOS add `/p:Platform=iPhoneSimulator`).

**`Xamarin.Forms.Build.Tasks` must be built before anything else can build.** It supplies the XAML MSBuild tasks that every project consuming XAML imports (`Directory.Build.props` points `__XFBuildTasksLocation` at `.nuspec/net46` or `.nuspec/netstandard2.0`). After a clean or a `bin`/`obj` wipe, run `msbuild Xamarin.Forms.Build.Tasks/Xamarin.Forms.Build.Tasks.csproj` (or the `BuildTasks` Cake target) first.

Build-wide settings live in `Directory.Build.props` / `Environment.Build.props`:
- **`TreatWarningsAsErrors=true` for every project** — a new warning breaks the build.
- `LangVersion` is **8.0**.
- `AndroidTargetFrameworks` defaults to `MonoAndroid13.0`.
- `ANDROID_RENDERERS` (`FAST` / `PREAPPCOMPAT` / `LEGACY`) selects which Android renderer set to compile; see the commented block in `Environment.Build.props`.
- Versions come from GitInfo + `Version.targets`; `AssemblyVersion` is deliberately frozen at `2.0.0.0` (binding-compat) while `PackageVersion` is computed from the git tag/branch. Base version is in `GitInfo.txt`.

`Xamarin.Forms.DualScreen.sln` is a separate solution built independently by CI.

Note: the `.Xamarin.Forms.{Android,iOS,UAP}.slnf` solution filters are inherited from upstream and list projects (`Xamarin.Forms.Pages`, `Xamarin.Flex`, `XFCorePostProcessor.Tasks`, `PagesGallery`) that are **not present in this fork's `Xamarin.Forms.sln`** — open the full `.sln` instead.

## Tests

Two distinct suites:

**Unit tests** (NUnit 3, .NET Framework, run on Windows): `Xamarin.Forms.Core.UnitTests` (net47), `Xamarin.Forms.Xaml.UnitTests` (net47), `Xamarin.Forms.DualScreen.UnitTests`, plus platform-specific `Xamarin.Forms.Platform.{Android,iOS,UAP}.UnitTests` which run on device/emulator. CI (`build/steps/build-windows.yml`) runs the built DLLs through the VSTest task; locally, build and run the assemblies with `vstest.console.exe` or `nunit3-console.exe`, or use Test Explorer in Visual Studio.

```powershell
# single test / fixture
vstest.console.exe Xamarin.Forms.Core.UnitTests\bin\Debug\Xamarin.Forms.Core.UnitTests.dll /Tests:BindingUnitTests
nunit3-console.exe Xamarin.Forms.Xaml.UnitTests\bin\Debug\Xamarin.Forms.Xaml.UnitTests.dll --where "test =~ /XamlC/"
```

`Xamarin.Forms.Core.UnitTests` is a **legacy-format csproj with explicit `<Compile Include>` items** — a new test file is silently ignored unless you add it to the csproj. Same for the `Xamarin.Forms.Controls.Issues.Shared.projitems` shared project. Core unit tests derive from `BaseTestFixture` and rely on `MockPlatformServices`/`MockDispatcher` (shared into the XAML tests via linked `<Compile Include="..\Xamarin.Forms.Core.UnitTests\...">`). XAML compiler tests invoke `XamlCTask` in-process through `Xamarin.Forms.Xaml.UnitTests/MockCompiler.cs`.

**UI tests** (Xamarin.UITest / Appium, per platform): `Xamarin.Forms.Core.{Android,iOS,Windows,macOS}.UITests` compile the shared `Xamarin.Forms.Core.UITests.Shared` project together with the repro pages in `Xamarin.Forms.Controls.Issues`. Each repro is a single file under `Xamarin.Forms.Controls.Issues/Xamarin.Forms.Controls.Issues.Shared/` that is *both* the gallery page and the test:

```csharp
#if UITEST
[Category(Core.UITests.UITestCategories.Github5000)]
#endif
[Preserve(AllMembers = true)]
[Issue(IssueTracker.Github, 12345, "Short description of the bug")]
public class Issue12345 : TestContentPage   // TestPage/TestNavigationPage/TestShell/... in TestPages/TestPages.cs
{
    protected override void Init() { /* build the repro UI, set AutomationId on probes */ }

#if UITEST
    [Test]
    public void Issue12345Test() => RunningApp.WaitForElement(q => q.Marked("Success"));
#endif
}
```

Test code is guarded by `#if UITEST` (and `__IOS__` / `__ANDROID__` for platform-only tests) so the same file compiles into the Control Gallery apps. Running Android UI tests requires `ANDROID_HOME`, `JAVA_HOME`, and a deployed APK; UWP UI tests require the Windows Application Driver and an installed `Xamarin.Forms.ControlGallery.WindowsUniversal`. The project follows red-green-refactor: a bug fix is expected to ship with a failing-then-passing test (UI test for platform behavior, unit test for Core/XAML).

## Architecture

**`Xamarin.Forms.Core`** (netstandard2.0 + netstandard1.0; the ns1.0 leg additionally compiles `Internals/Legacy/**`) holds everything platform-independent: `BindableObject`/`BindableProperty` (the property system backing bindings, styles, triggers), the `Element` → `VisualElement` → `View`/`Page`/`Layout` tree, layout and measurement, `Shell`, `ResourceDictionary`/`Style`/CSS `StyleSheets`, `MessagingCenter`, `DependencyService`, animation, and `PlatformConfiguration/<Platform>Specific` (the platform-specifics extension methods surface).

**Renderer resolution** is the central mechanism. `Xamarin.Forms.Internals.Registrar<T>` (`Xamarin.Forms.Core/Registrar.cs`) maps a Forms type → renderer type, keyed additionally by *Visual* (`VisualMarker.DefaultVisual` vs `MaterialVisual`) and a priority. It is populated during `Forms.Init(...)` (e.g. `Xamarin.Forms.Platform.Android/Forms.cs` → `SetupInit` → `Registrar.RegisterRenderers` / `RegisterEffects` / `RegisterStylesheets` / `RegisterAll`), which scans assemblies for `[assembly: ExportRenderer(typeof(Control), typeof(ControlRenderer))]`, `ExportCell`, `ExportImageSourceHandler`, and `ExportEffect` attributes.

**Default renderers** are *not* declared with `ExportRenderer`. They come from `[RenderWith(typeof(XRenderer))]` markers on internal placeholder classes in **`Stubs/Xamarin.Forms.Platform.cs`**, which is compiled per platform by the "Forwarders" projects in `Stubs/` into a platform-flavored `Xamarin.Forms.Platform.dll` (`Xamarin.Forms.Platform/Xamarin.Forms.Platform.csproj` is only the empty netstandard facade the Core project references). Adding a new control with a default renderer means editing `Stubs/Xamarin.Forms.Platform.cs` as well as the platform renderer.

**Platform projects** (`Xamarin.Forms.Platform.Android`, `.iOS`, `.UAP`, `.MacOS`, `.WPF`, `.GTK`, `.Tizen`) implement `IVisualElementRenderer` per control plus platform services (`IPlatformServices`, ticker, image sources, native bindings). On Android note the parallel renderer families: `Renderers/` (classic), `AppCompat/`, and `FastRenderers/` (renderers that draw directly into a native view instead of wrapping a container) — which set is active depends on `ANDROID_RENDERERS`. `Xamarin.Forms.Material.{Android,iOS,Tizen}` supply the Material Visual renderers registered against `VisualMarker.MaterialVisual`.

**XAML** is two layers. `Xamarin.Forms.Xaml` is the runtime loader/parser (`LoadFromXaml`, markup extensions, type converters). `Xamarin.Forms.Build.Tasks` is the build-time half: `XamlGTask`/`XamlGenerator` generate the `InitializeComponent` + `x:Name` field partial class, and `XamlCTask` (Mono.Cecil) compiles XAML into IL at build time — the `*Visitor.cs` files (`CreateObjectVisitor`, `SetPropertiesVisitor`, `ExpandMarkupsVisitor`, …) walk the XAML node tree emitting IL, and `CompiledConverters`/`CompiledMarkupExtensions`/`CompiledValueProviders` hold the per-type IL emitters. XAML errors surface as MSBuild errors via `BuildException`/`ErrorMessages.resx`.

Other pieces: `Xamarin.Forms.Maps*` (map control + per-platform renderers), `Xamarin.Forms.DualScreen` (Surface Duo support, separate solution), `Xamarin.Forms.Core.Design`/`Xamarin.Forms.Xaml.Design` (designer metadata assemblies), `Xamarin.Forms.CustomAttributes` (test attributes such as `[Issue]`, `[Preserve]`), and `Xamarin.Forms.Controls` + `Xamarin.Forms.ControlGallery.*` (the sample/QA app hosting the galleries and issue repros).

Packaging is driven by the hand-written `.nuspec/*.nuspec` files plus `.nuspec/Xamarin.Forms.props`/`.targets`, which is what injects the XAML build tasks into consuming apps.

## Coding style

.NET Foundation style with the project's exceptions (see `.editorconfig`, which is authoritative):
- **Hard tabs**, not spaces, in `.cs` files.
- **Never write `private`** — it's the C# default (`dotnet_style_require_accessibility_modifiers = never`).
- Lines up to ~120 characters.
- Allman braces; `var` only when the type is apparent.
- Fields are camelCase, private/internal fields prefixed with `_`; constants PascalCase.
