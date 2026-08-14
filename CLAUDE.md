# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository

Fork of `xamarin/Xamarin.Forms` (origin: `pieroviano/Xamarin.Forms`), working on the **`5.0.0`**
branch, which is also the default/PR-target branch here. Upstream Xamarin.Forms reached end of
support on May 1, 2024 and was succeeded by .NET MAUI; this repo is a maintenance fork of the
5.0.0 line.

**This fork has been pruned to the GTK leg only.** Commit `cc8874b9b` removed the Android, iOS,
UAP, WPF, Tizen, MacOS and DualScreen projects, the Cake build, the Azure pipelines and the
original `Xamarin.Forms.sln`. What remains is a shared portable UI abstraction
(`Xamarin.Forms.Core`) mapped onto native **GTK 4** widgets by the renderers in
`Xamarin.Forms.Platform.GTK`.

**The GTK 4 migration lives on the `gtk4` branch** (`5.0.0` is still GTK 3). It replaced the
`GtkSharp` 3 package with the `Net4x.*` 4.22.4 bindings, most of which is absorbed by a GTK 3
compatibility surface in a sibling repository rather than by churn here — see
`docs/plans/gtk4-migration.md`, which is the reference for the whole migration including the local
package feed, the three waves of compiler errors, and what is deliberately deferred.

Empty directories such as `Xamarin.Forms.Platform.Android/` may still exist in a working tree as
local `bin`/`obj` residue from before the prune. They contain no tracked files — ignore them, or
delete them locally.

## Build

Plain `dotnet` / MSBuild. There is no Cake build and no `build.cmd` — those were deleted.
`global.json` pins .NET SDK **10.0.100** (`rollForward: latestFeature`) and
`MSBuild.Sdk.Extras` 3.0.44.

```powershell
dotnet build Xamarin.Forms.Gtk.sln                 # whole solution
dotnet build Xamarin.Forms.Platform.GTK/Xamarin.Forms.Platform.GTK.csproj
dotnet build Xamarin.Forms.Gtk.sln -c Release      # note the Release caveat below
```

`Xamarin.Forms.Gtk.sln` is the only solution in the tree.

**`Xamarin.Forms.Build.Tasks` must be built before anything else that consumes XAML.** It supplies
the XAML MSBuild tasks; `Directory.Build.props` points `_XFBuildTasksLocation` at
`artifacts/build-tasks/netstandard2.0/` (or `artifacts/build-tasks-unix/netstandard2.0/` when
`UseOSSpecificOutputPaths` is set). After a clean or a `bin`/`obj` wipe, build that project first.
Building the solution handles the ordering for you.

**Release builds.** It used to not: `Xamarin.Forms.Xaml.UnitTests` failed with
`error CS0122: 'XamlCAssemblyResolver' is inaccessible due to its protection level`, because
upstream wrapped `[assembly: InternalsVisibleTo("Xamarin.Forms.Xaml.UnitTests")]` in `#if DEBUG`.
`Xamarin.Forms.Build.Tasks/Properties/AssemblyInfo.cs` now guards it with `#if !SIGNED_ASSEMBLY`
instead, so the test assembly keeps its access in every unsigned configuration. CI still builds
Debug, but Release is no longer expected to fail.

Build-wide settings live in `Directory.Build.props` / `Environment.Build.props`:
- **`TreatWarningsAsErrors=true` for every project** — a new warning breaks the build.
- `LangVersion` is **8.0** (test projects override this to `latest`).
- `UseOSSpecificOutputPaths=true` redirects output to `bin-wsl/` / `obj-wsl/`, so a Windows build
  and a WSL build of the same tree do not fight over `obj/`.
- Versions come from GitInfo + `Version.targets`; `AssemblyVersion` is deliberately frozen at
  `2.0.0.0` (binding-compat) while `PackageVersion` is computed from the git tag/branch. Base
  version is in `GitInfo.txt`.
- `ANDROID_RENDERERS` and `AndroidTargetFrameworks` are still declared in
  `Environment.Build.props` but are vestigial — no Android project remains.

All library projects (`Core`, `Xaml`, `Platform.GTK`, `Build.Tasks`, `Maps`, `Maps.GTK`) target a
single TFM, `netstandard2.0`. The three test projects target `net10.0`.

## Tests

Three suites, all **xUnit v3 on `net10.0`**, all run with `dotnet test`:

| Project | Tests | Notes |
|---|---|---|
| `Xamarin.Forms.Core.UnitTests` | 4848 | platform-independent Core |
| `Xamarin.Forms.Xaml.UnitTests` | 1046 | XAML loader + XamlC compiler |
| `Xamarin.Forms.Platform.GTK.UnitTests` | 169 | **real GTK widgets** |

```powershell
dotnet test Xamarin.Forms.Gtk.sln                          # everything
dotnet test Xamarin.Forms.Core.UnitTests --no-build
dotnet test Xamarin.Forms.Core.UnitTests --filter FullyQualifiedName~BindingUnitTests
```

These were converted from NUnit 3. When touching a converted test, be aware of the traps the
conversion hit (all fixed, but the patterns recur):

- **`Assert.Pass()` has no xUnit equivalent.** It unwound the whole test from inside an event
  handler. A bare `return;` only exits the *lambda*, leaving a trailing `Assert.Fail()` reachable.
  Use a `fired` flag and assert it after the act.
- **`Assert.Equal<object>` does not coerce numerics.** NUnit's `Assert.AreEqual` did. Boxed `int`
  vs boxed `double` produces the baffling `Expected: 123 / Actual: 123`.
- **`Assert.ThrowsAsync` returns a `Task`** and must be awaited; NUnit's ran synchronously.
- **`Assert.Throws<T>` matches the type exactly**, where NUnit's accepted derived types.
- **NUnit `Assume.That` → `Assert.SkipWhen`** (both mean "inconclusive"), not an assertion.

**All three suites disable test parallelisation** via an `AssemblyTestConfig.cs`, and this is
load-bearing, not tidiness. These tests mutate process-global state (`Device.PlatformServices`,
`Application.Current`, the `Registrar`). Without the guard `Core.UnitTests` does not merely fail —
it hangs indefinitely. Do not remove it. `Xaml.UnitTests` additionally applies an assembly-level
`[EnsurePlatformServices]` hook, because many of its fixtures never set `Device.PlatformServices`
themselves and previously passed only by accident of ordering.

`Xamarin.Forms.Core.UnitTests` and `Xamarin.Forms.Xaml.UnitTests` are **SDK-style with default
globbing** (`EnableDefaultCompileItems=true`) — a new test file is picked up automatically. (The
legacy explicit-`<Compile Include>` form, where new files were silently ignored, is gone.) Core
unit tests derive from `BaseTestFixture` and rely on `MockPlatformServices`/`MockDispatcher`,
shared into the XAML tests via linked `<Compile Include="..\Xamarin.Forms.Core.UnitTests\...">`.
XAML compiler tests invoke `XamlCTask` in-process through
`Xamarin.Forms.Xaml.UnitTests/MockCompiler.cs`.

### GTK renderer tests

`Xamarin.Forms.Platform.GTK.UnitTests` drives **real GTK 4 widgets** and runs on both OSes:

- **Windows** — works directly against the GDK Win32 backend; no `DISPLAY`, no Xvfb.
- **Linux** — needs an X server: `xvfb-run -a --server-args="-screen 0 1280x1024x24" dotnet test …`
  with `GDK_BACKEND=x11`. The default 640x480 would clamp the 800x600 test windows.

`GtkTestHost` is the harness. Read its comments before adding a test — they record measured
behaviour, not theory. In particular: pump with `GtkTestHost.Pump`/`PumpUntil` rather than
assuming a relayout happened; never write an `async` test method (a `GtkSynchronizationContext` is
installed on the GTK thread and every continuation deadlocks — use `GtkTestHost.Await`); and
never `Destroy()` a test window (GtkSharp's finalizer aborts the run — `Retire` hides and holds).

**Every test body runs inside `Run(() => { … })`, on a dedicated STA thread.** MEASURED: GTK 4's
GDK Win32 backend calls `OleInitialize` during `gtk_init`, which requires a single-threaded
apartment; xUnit's thread-pool threads are MTA, so initialising GTK there killed the process
outright (`Gdk-ERROR: OleInitialize failed`, exit `0xC0000409`). An apartment cannot be changed
after a thread starts, so GTK owns a thread of its own — and since GTK may only be used from the
thread that called `gtk_init`, every body has to be marshalled onto it.

**Measure before you allocate.** GTK 4 requires it; allocating without it logs *"Allocating size to
… without calling gtk_widget_measure()"* and the allocation silently does not propagate, so
assertions read geometry that was never laid out. `GtkTestHost.Pump` and `Resize` do this for you.

**`Gtk.Window.Resize()` is gone** — GTK 4 removed it, because on Wayland the compositor owns the
geometry. Use `GtkTestHost.Resize`, which sets the default size and then drives the allocation.
That is strictly more reliable than what it replaces: the Gtk 3 call was only a *request* to the
window manager, serviced immediately under Xvfb and never at all on Win32 in a non-interactive
session.

**Input cannot be synthesised directly.** GTK 4 has no public `GdkEvent` constructor and no
`gtk_widget_event`, so `GtkTestHost.PressButton` finds the widget's own `GtkGestureClick` through
`ObserveControllers()` and emits its signal. Attaching a second gesture and emitting on that proves
nothing — the compat `ButtonPressEvent` listens to its own.

**Widgets own no pixels.** `GtkTestHost.RenderToBytes`/`PixelAt` render through a
`GtkWidgetPaintable` into a `GdkTexture`; there is no GdkWindow to photograph and no
`gtk_widget_draw`.

The two OSes do not agree on everything. Known open difference from the GTK 3 era:
`CoreControlMappingTests.BoxViewPaintsItsColourAndRepaintsOnChange` passed on Windows and failed
under Xvfb ("a red BoxView painted rgb(0,0,0)"). Verify both sides before calling the suite green —
and re-check this one specifically, since the readback now goes through GSK rather than a GdkWindow.

**This suite is not green yet on `gtk4`.** It builds and runs, and the crash is fixed, but a batch
of renderer tests still fail on genuine GTK 3 → GTK 4 behavioural differences (minimum sizes,
allocation propagation, paint timing). See `docs/plans/gtk4-migration.md` §2 for the current count
and §6 for what is left.

CI is `.github/workflows/linux-gtk.yml` (Ubuntu, Debug). All three suites gate it. On `gtk4` it
also builds the `Net4x.*` bindings from a pinned commit of the GtkSharp repository first, because
the `Packages/` local feed those come from is a junction on a development machine and cannot be
committed — see `docs/plans/gtk4-migration.md` §1 and O1.

## Architecture

**`Xamarin.Forms.Core`** (netstandard2.0) holds everything platform-independent:
`BindableObject`/`BindableProperty` (the property system backing bindings, styles, triggers), the
`Element` → `VisualElement` → `View`/`Page`/`Layout` tree, layout and measurement, `Shell`,
`ResourceDictionary`/`Style`/CSS `StyleSheets`, `MessagingCenter`, `DependencyService`, animation,
and `PlatformConfiguration/<Platform>Specific`.

**Renderer resolution.** `Xamarin.Forms.Internals.Registrar<T>` (`Xamarin.Forms.Core/Registrar.cs`)
maps a Forms type → renderer type, keyed additionally by *Visual* and a priority. It is populated
during `Forms.Init()` (`Xamarin.Forms.Platform.GTK/Forms.cs`), which scans assemblies for
`[assembly: ExportRenderer(typeof(Control), typeof(ControlRenderer))]`, `ExportCell`,
`ExportImageSourceHandler` and `ExportEffect`.

**Default renderers** are *not* declared with `ExportRenderer`. They come from
`[RenderWith(typeof(XRenderer))]` markers on internal placeholder classes in
**`Stubs/Xamarin.Forms.Platform.cs`**, compiled into a platform-flavoured
`Xamarin.Forms.Platform.dll` (`Xamarin.Forms.Platform/Xamarin.Forms.Platform.csproj` is the
netstandard facade Core references). Adding a control with a default renderer means editing
`Stubs/Xamarin.Forms.Platform.cs` as well as the GTK renderer.

**`Xamarin.Forms.Platform.GTK`** implements `IVisualElementRenderer` per control plus the platform
services (`IPlatformServices`, ticker, image sources). `FormsWindow` hosts an `Application`;
`Platform.cs` owns renderer creation and the layout pass.

**XAML** is two layers. `Xamarin.Forms.Xaml` is the runtime loader/parser (`LoadFromXaml`, markup
extensions, type converters). `Xamarin.Forms.Build.Tasks` is the build-time half: `XamlGTask`
generates the `InitializeComponent` + `x:Name` partial class, and `XamlCTask` (Mono.Cecil) compiles
XAML into IL — the `*Visitor.cs` files walk the XAML node tree emitting IL. XAML errors surface as
MSBuild errors via `BuildException`/`ErrorMessages.resx`.

Other pieces: `Xamarin.Forms.Maps` + `Xamarin.Forms.Maps.GTK`, `Xamarin.Forms.CustomAttributes`
(test attributes such as `[Issue]`, `[Preserve]`), and `Xamarin.Forms.Controls` +
`Xamarin.Forms.ControlGallery.GTK` (the sample/QA app). `Xamarin.Forms.Controls` imports the 1369
upstream issue repros from `Xamarin.Forms.Controls.Issues/…Shared.projitems`, so they compile into
the gallery even though the per-platform UITest runners were pruned.

## Coding style

.NET Foundation style with the project's exceptions (see `.editorconfig`, which is authoritative):
- **Hard tabs**, not spaces, in `.cs` files.
- **Never write `private`** — it's the C# default (`dotnet_style_require_accessibility_modifiers = never`).
  Much of the inherited GTK code does write it; leave existing declarations alone.
- Lines up to ~120 characters.
- Allman braces; `var` only when the type is apparent.
- Fields are camelCase, private/internal fields prefixed with `_`; constants PascalCase.
