# GTK 3 → GTK 4: replacing GtkSharp 3 with the `Net4x.*` packages

Branch: **`gtk4`**, off `5.0.0`.
Bindings: **`Net4x.*` 4.22.4.26225**, built from `d:\CommonLibrary\GtkSharp` branch `net4x.gtk4`.

The GtkSharp repository is local and **in scope for edits**. That is not a convenience — it is the
plan's central mechanism: most of what GTK 4 removed is re-provided there, once, so this repository
changes as little as possible and every other consumer of the bindings benefits.

---

## 0. Decisions

| # | Decision | Consequence |
|---|---|---|
| 1 | **In place on `gtk4`.** `Xamarin.Forms.Platform.GTK` is rewritten against GTK 4; no parallel project, no GTK 3 fallback on this branch. | One backend, one package id. The branch does not compile from the first edit until the compat surface lands — expected, and why the milestones are ordered by *what unblocks the next compile* rather than by feature. |
| 2 | **The GTK 3 → GTK 4 gap is absorbed by compat shims in the GtkSharp repo.** | Smallest diff here. Cost: the bindings must be rebuilt before this repo can compile (M1 gates M2) — cheap, see §1 — and a handful of things cannot be shimmed and are ported here anyway (§4.4). |
| 3 | **`Packages/` is a local NuGet source.** | Works on a development machine immediately: `Packages` is a junction onto the GtkSharp repo's drop folder, so a cake build publishes straight into this feed. CI is the open half — see §7 O1. |
| 4 | **`netstandard2.0` is kept** for `Xamarin.Forms.Platform.GTK` and `Xamarin.Forms.Maps.GTK`. | Every `Net4x.*` package ships `lib/netstandard2.0` beside `lib/net10.0`, so no TFM change is needed and the published packages stay consumable as they are. |

**Acceptance gate** (unchanged from the branch's existing one): `Xamarin.Forms.Core.UnitTests` (4848),
`Xamarin.Forms.Xaml.UnitTests` (1046) and `Xamarin.Forms.Platform.GTK.UnitTests` (169) all green on
**both** Windows and Linux/Xvfb, plus `Xamarin.Forms.ControlGallery.GTK` launching and rendering.
Core and Xaml never touch GTK, so they are a regression tripwire rather than a target.

This is a binding migration, not a redesign: the Forms-level contracts (`IVisualElementRenderer`, the
`Fixed`-based absolute layout, `VisualElementTracker`'s property mapping) stay as they are.

---

## 1. The development loop

```
d:\CommonLibrary\GtkSharp   → edit Source/Libs/…              (the shims, §4)
                            → dotnet cake build.cake --BuildTarget=PackageNuGet
                            → BuildOutput/NugetPackages ─┐    (symlink)
                                                         ├→ d:\Starb\Packages
d:\CommonLibrary\Xamarin.Forms\Packages ─────────────────┘    (junction)
                            → dotnet build …                  (no copy, no version bump)
```

**No version bump, and no copy step.** Repacking a version in place would normally be invisible —
NuGet extracts a package into `~/.nuget/packages/<id>/<version>/` on first restore and never re-reads
the `.nupkg`. `Net4x.NuGetUtility`, which `Xamarin.Forms.Maps.GTK` still imports on Windows, deletes
the cached extraction at build, so the repack is picked up. MEASURED: after re-running the cake build
at the same version, `~/.nuget/packages/net4x.gtksharp/` held only the freshly packed copy.

That mechanism is Windows-only; on Linux and CI the restore is cold, so the question does not arise.
`--BuildTarget=PackageNuGet` skips workload and template packaging — ~2 minutes of a 4-minute full
build, and irrelevant here.

**`Packages/` cannot be committed**: it is a junction into a shared drop folder, and `.gitignore:32`
(`/packages`) already excludes it. Leave that rule alone.

### Two things that will bite

- **A `.metadata` rule that matches nothing fails the build.** gapi-fixup runs `--strict`. This is
  correct behaviour and it caught a real mistake: a rule for `GtkImage/method[@name='SetFromPixbuf']`
  silently applied to nothing because an existing rule three hundred lines earlier had already renamed
  that node. Amend the existing rule; do not add a second one.
- **The compiler reveals this migration in three waves, and `dotnet build` is incremental** — so it
  will report a project clean when it did not recompile it. See §3.

---

## 2. Status

| | State |
|---|---|
| **M0** feed, versions, package ids | **Done.** Restore resolves `Net4x.*` 4.22.4.26225 from the local feed; no GTK 3 package anywhere in `project.assets.json`. |
| **M1** compat surface in GtkSharp | **Done.** 13 calling tests in `CompatTests.cs`; the full GtkSharp suite is **1708 passed / 0 failed / 36 skipped**. |
| **M2–M5** container, input, drawing, style | **Done**, largely as a side effect of M1 — see the wave note in §3. |
| **M6** the sweep | **Done.** `Xamarin.Forms.Platform.GTK` and `Xamarin.Forms.Maps.GTK` both build from scratch with **0 errors, 0 warnings**. |
| **M7** WebView → WebKitGTK 6.0 | **Done.** `Controls/WebKit2.cs` (263 lines of hand-rolled P/Invoke against a GTK 3-only library) deleted; `Controls/WebView.cs` hosts a `WebKit.WebView` from Net4x.WebkitGtkSharp. |
| **M8** tests, CI, gallery | **Builds and runs; not green.** All five projects compile with 0 warnings. Core (4856) and Xaml (1043) pass. The GTK renderer suite no longer crashes, but 42 of 185 cases fail on behavioural differences, and one class hangs — see §6. CI and `CLAUDE.md` are updated. |
| Open questions | **O1** (CI feed) and **O2** (WebKitGTK on the runner) — §7. |

Error count over the migration, `Xamarin.Forms.Platform.GTK` only:

| | Unique errors |
|---|---:|
| Start (wave 1: type resolution) | 326 |
| After the compat surface landed | 0 → **115 wave-2 errors became visible**, across 51 files |
| After the `Widget.Color` fix (§4.5) | 58 |
| After the pickers, menus and display/style ports | 18 |
| After the last of those | 0 → **147 wave-3 errors became visible** |
| After the wave-3 sweep and the deprecation policy | **0 errors, 0 warnings** |

---

## 3. How to measure this migration

**A grep over the old API names measures the first wave only. Any estimate built that way is a lower
bound.** The compiler reveals this migration in three passes, and each one is hidden behind the one
before it:

| | What it is |
|---|---|
| **Wave 1** — type resolution | Missing *types*, plus every override and generic constraint that cascades from them: `Gdk.Color`, `Gtk.EventBox`, `Gtk.Container`, the `*EventArgs` family, `StateType`, `ToolButton`, `RadioButton`. Every `VisualElementRenderer` failing to be a `Gtk.Widget` at all. |
| **Wave 2** — member binding | Only reachable once the types resolve. *Members* that moved or vanished on types that still exist: `Widget.BorderWidth`, `Widget.Window`, `Box.BoxChild`, `ShadowType`, `Gdk.Screen`, `IconSize`, `ReliefStyle`, `WindowType`, `Label.LineWrap`, `Image.Stock`, `StyleContext.AddProviderForScreen`, `Seat.Grab`, `MessageDialog.Run`, `GLib.DateTime` vs `System.DateTime`. |
| **Wave 3** — deprecations | Only reachable once everything *compiles*, and only visible at all because `TreatWarningsAsErrors=true`. **147 CS0612s**: `Widget.Show()`/`Hide()` (112 between them), `Allocation`, `AllocatedWidth/Height`, `MessageDialog`, `StyleContext`, `ListStore`, `CellRendererText`, `ComboBox`, `CellView`, `TreeIter`, `Image(Pixbuf)`, `CssProvider.LoadFromData`. |

Wave 2 was about a third of wave 1, and wave 3 about half. Budget for all three.

**Wave 3 has a trap of its own: `dotnet build` is incremental, so it will tell you a project is clean
when it did not recompile it.** These 147 errors were invisible for a while behind a no-op build that
reported "0 Warning(s), 0 Error(s)". Verify with `--no-incremental` before believing a green result.

Wave 3 split cleanly: the mechanical two thirds were ported (`Show()`/`Hide()` → `Visible`,
`Allocated*` → `Width`/`Height`, `Allocation` → widget-local size, `LoadFromData` → `LoadFromString`),
and the rest is a bounded, inventoried `NoWarn` — see §9 R3.

### 3.1 What GTK 4 removed — the work

Counts are matching lines across `Xamarin.Forms.Platform.GTK`, `Xamarin.Forms.Maps.GTK`,
`Xamarin.Forms.ControlGallery.GTK` and `Xamarin.Forms.Platform.GTK.UnitTests` at the start.

| GTK 3 API | Hits | GTK 4 replacement | Absorbed by |
|---|---:|---|---|
| `Gtk.EventBox` | 117 | none; any widget takes events | shim (§4.1) |
| `Gdk.Color` | 117 | `Gdk.RGBA` | shim (§4.2) |
| `ButtonPress`/`Release`, `MotionNotify`, `Enter`/`LeaveNotify`, `KeyPress`/`Release`, `Scroll` | 100 | `GestureClick`, `EventController{Motion,Key,Scroll,Focus}` | shim (§4.1) |
| `ShowAll()` | 94 | visible by default; `Show()` | shim |
| `SizeAllocate(Gdk.Rectangle)` / `OnSizeAllocated` | 77 | `Allocate(w,h,baseline,Gsk.Transform)` | shim (§4.3) |
| `Destroy()` | 52 | `Unparent()` / `Window.Destroy()` | already in the fork |
| `Gtk.Container` | 27 | no `GtkContainer`; `Widget.Parent`/`Unparent` | shim (§4.1) |
| `OnDrawn(Cairo.Context)` | 27 | `OnSnapshot` + `Snapshot.AppendCairo` | shim (§4.3) |
| `GetPreferredSize/Width/Height` | 17 | `Measure(...)` | shim |
| `AddEvents` / `Gdk.EventMask` | 5 | controllers; no masks | shim (no-op) |
| `Gtk.Alignment` (3), `Gtk.Table` (1) | 4 | `Halign`/`Valign`/`Margin*`, `Gtk.Grid` | port |
| `StyleContext.GetProperty` | 2 | **removed outright** | port (§4.4) |
| `libwebkit2gtk-4.1` | 263 LOC | WebKitGTK 6.0 | port (M7) |

### 3.2 What survives — the relief

- **`Gtk.Fixed.Put`/`Move`/`Remove`** — the absolute-layout primitive this whole backend rests on
  (`Extensions/WidgetExtensions.cs:53-102`). Survives verbatim. The single biggest reason the port is
  tractable.
- **`Gtk.Application.Init/Run/Quit/EventsPending/RunIteration`** — `gtk_main` is gone in GTK 4, but
  `Source/Libs/GtkSharp/Application.cs` re-implements the whole family over a `GLib.MainLoop`, so
  `ControlGallery.GTK/Program.cs` and `GtkTestHost` compile and run unchanged.
- `ScrolledWindow`, `Notebook`, `Viewport`, `Frame`, `Grid`, `Overlay`, `Stack`, `Popover`, `Entry`,
  `Label`, `Button`, `Image`, `ProgressBar`, `Scale`, `SpinButton`, `Switch`, `Calendar`, `Spinner`,
  `SearchEntry`, `DrawingArea`, `CssProvider`, `StyleContext`.
- `Gtk.TreeView` / `ListStore` / `TreeViewColumn` / `CellRendererText` — deprecated in 4.10, still
  bound in 4.22, so `Controls/ListView.cs`, `Controls/TableView.cs` and `Cells/*` keep working (R4).
- `Gdk.Pixbuf`, `Cairo.*`, `Pango.*`, `Gdk.CairoHelper` (still exported, still generated).
- Managed `Gtk.Widget` subclassing with `OnMeasure` / `OnSizeAllocate` / `OnSnapshot` overrides.

---

## 4. The compat surface in the GtkSharp repo

All of it lives in `Source/Libs/GtkSharp/Compat/` and `Source/Libs/GdkSharp/Compat/`, one folder so
it stays auditable and removable. Every type documents the GTK 3 API it stands in for and the GTK 4
API it is built on.

**Constraints**: the code must compile as `net10.0` *and* `netstandard2.0`, at `LangVersion 9`
(`Source/Libs/Directory.Build.props`).

**`Compat/` is only for API GTK 4 genuinely removed.** A type GTK 4 still has that the generator
simply did not emit belongs in `<Name>.metadata` or a hand-written partial next to its peers. That
line is what keeps the folder shrinkable.

**Every shim has a test that calls it.** In this binding a missing native export is a null delegate,
not a link error, so an untested shim fails as a `NullReferenceException` naming nothing — or as an
override that is silently never reached.

### 4.1 `Container` and `EventBox` — the load-bearing shims

`GtkFormsContainer`, the base of every renderer, was a `Gtk.EventBox`. It needs four things: hold
children, position them, paint a background under them, receive button events.

- **`Container : Widget`** — child list with (x, y) placement (GtkFixed's model, which is what the
  Forms layout actually uses), `Add`/`Remove`/`Move`/`Put`/`Children`/`Child`, `OnMeasure` and
  `OnSizeAllocate` over `Widget.Parent`/`Unparent` and `Gsk.Transform`.
  **Deliberately not derived from `Gtk.Fixed`**: `Fixed` installs a `GtkFixedLayout`, and a layout
  manager answers the measure vfunc *instead of* the widget, so an `OnMeasure` override on a `Fixed`
  subclass is never called. Bare `Widget` keeps the vfuncs reachable.
- **`EventBox : Container`** — `VisibleWindow` (accepted, ignored), and `OnDrawn(Cairo.Context)` over
  the snapshot vfunc.
- **The input events live on `Widget`**, not on `EventBox`, because `VisualElementTracker` attaches
  them to arbitrary native controls (`Gtk.Entry`, `Gtk.Button`, …), not only to containers. Each
  event family owns a controller created **on first subscription** — every wrapper in the process
  inherits these members, so eager creation would cost one controller per widget for a feature almost
  nothing uses.

Two differences are real and documented rather than papered over: delivery **order** (GTK 3 walked
the GdkWindow stack; these attach in the bubble phase, the closer of GTK 4's two), and **screen
coordinates** (`XRoot`/`YRoot` repeat the widget-local values, because GTK 4 does not tell a client
where the pointer is on screen).

`args.RetVal` is honoured, not decorative: `true` becomes
`Gesture.SetState(EventSequenceState.Claimed)`, which is GTK 4's "I consumed this".

### 4.2 `Gdk.Color` and the event records

`Gdk.Color` keeps **16-bit channels**, as GdkColor's were. That is not cosmetic: consumers divide by
65535, and an 8-bit shim would make every converted colour 257× too dark while still compiling and
still looking like a colour. `Extensions/ColorExtensions.cs` already documents that exact bug from a
previous round.

The `Gdk.Event*` family are **values, not wrappers** — GTK 4's GdkEvent is opaque with no public
constructor. They are populated from the real event the controller was handling plus the coordinates
it reports. `EventButton.NPress` is where `EventType.TwoButtonPress`/`ThreeButtonPress` went; GTK 4
deleted both enum members.

### 4.3 The drawing and sizing vfuncs

`OnDrawn(Cairo.Context)` and `OnSizeAllocated(Gdk.Rectangle)` are added to each of `Window`, `Box`,
`Fixed`, `Grid`, `Button`, `Frame`, `DrawingArea` and `EventBox`, all forwarding to one shared
implementation.

**Per-type, because a partial class cannot override its own virtual method** — `Gtk.Widget.OnSnapshot`
is declared in Widget's generated half, so the override must sit in a subclass.

The **measure** vfunc is deliberately *not* offered this way: a widget with a layout manager never has
its own measure called. Code that needs to control a `Box` subclass's preferred size must subclass the
layout manager instead — see `ShellWidget` in §5 M6.

`OnSnapshot` **is** reached on a managed subclass; `CompatTests.EventBox_draw_override_is_reached_
through_snapshot` renders a real window and asserts it.

### 4.4 What cannot be shimmed — ported here instead

1. **`Widget.Window` and the windowless-`EventBox` input trick.** `VisualElementTracker` implemented
   `InputTransparent` around a shared GdkWindow. GTK 4 has none. → `Widget.CanTarget`, which is
   exactly the intended semantics and simpler than what it replaced.
2. **`StyleContext.GetProperty`.** GTK 4 removed CSS value queries outright. → foreground via
   save/set-state/restore; background via `@theme_bg_color`; font via the widget's Pango context —
   which is a *better* source, being the font the widget will actually use.
3. **Grabs.** `Gdk.Seat.Grab` is gone. → `Popover.Autohide`, which is the implicit grab *and* the
   dismiss-on-outside-click in one property. `Helpers/GrabHelper.cs` is deleted.
4. **Popup toplevels.** `WindowType.Popup`, `gtk_window_move`, window hints — all gone. → `Gtk.Popover`.
5. **`Gtk.Menu`/`MenuItem`.** GTK 4's `PopoverMenu` is driven by a `GMenuModel` and GActions, so
   porting onto it would mean giving every Forms `MenuItem` a registered action name to end up with
   the same rows and handlers. → a plain `Gtk.Popover` of `flat`-styled buttons, which keeps the
   widget-and-handler model these already have.
6. **WebKit2GTK 4.1 → WebKitGTK 6.0** (M7). A different native library, not a binding difference.
7. **`GtkTestHost.PressButton`** (M8). GTK 4 forbids an application constructing events at all. The
   recipe is in `CompatTests.Button_press_event_fires_from_a_click_gesture`: find the widget's
   controller through `ObserveControllers()` and emit its signal. Attaching a *second* gesture and
   emitting on that proves nothing — the shim listens to its own. (That test failed exactly that way
   first.)

### 4.5 A defect found in the binding itself

GTK 4.10 added `gtk_widget_get_color`. Bound under its own name it puts an instance property
**`Color` on `Gtk.Widget`** — and in C# an inherited member beats a type of the same name during
simple-name resolution, so inside *any* `Widget` subclass the identifier `Color` stops meaning the
type. Every GUI codebase has one (`Cairo.Color`, `System.Drawing.Color`, `Xamarin.Forms.Color`), and
the errors name `Gdk.RGBA` and point nowhere near the cause.

MEASURED: **52 errors across 14 files**, all of it this. Renamed to `GetStyleColor` in
`GtkSharp.metadata`, and guarded by `CompatTests.A_widget_subclass_can_still_use_a_type_named_Color`,
which is written to *stop compiling* if the rename is ever lost.

`Gtk.EventArgs` / `Gtk.EventHandler` (from `GtkEventControllerLegacy`) collide the same way and hit
50 sites here. The fix on this side is one file — `Xamarin.Forms.Platform.GTK/GlobalUsings.cs` — since
the alias is true in all 20 affected files. The binding itself qualifies them, as
`Gtk.Application.Invoke` already documents.

---

## 5. Milestones

### M0 — feed, versions, package ids — **done**

- `NuGet.config`: `Packages` added as a **relative** local source (resolves against the config file,
  so it works from Windows and from a `/mnt/d` WSL copy alike — which is what the existing `<clear />`
  comment rules out only for an *absolute* Windows path).
- `Directory.Build.props` / `Directory.Nuget.Props`: `GtkSharpVersion` 3.24.24 → 4.22.4, suffix
  95 → 26225.
- `GtkSharp` → `Net4x.GtkSharp` in `Xamarin.Forms.Platform.GTK.csproj`, `Xamarin.Forms.Maps.GTK.csproj`
  and `Xamarin.Forms.ControlGallery.GTK.csproj` (the last also stops hardcoding its version).
- **An explicit `Net4x.GtkSharp` reference on `Xamarin.Forms.Platform.GTK.UnitTests`.** The package's
  `build/GtkSharp.targets` installs the Windows GTK 4 runtime, and `build/` assets run only for a
  project holding the reference itself — a `ProjectReference` does not carry them. Without it every
  test in that suite errors inside `Gtk.Application.Init`.
- Two guards: **`_XFAssertNoGtk3Package`** (the `Net4x.NuGetUtility` tooling rewrites
  `Include="GtkSharp"` on Windows builds and has undone pins three times; after the rename that would
  restore the GTK 3 package *alongside* the GTK 4 one — two assemblies owning namespace `Gtk`) and
  **`_XFAssertLocalFeedExists`** (a missing local source is only NU1801, a warning, and would fall
  through to nuget.org).

### M1 — the compat surface — **done**

§4, plus `CompatTests.cs`. Gate: GtkSharp's own suite green, and `new Gtk.EventBox()` compiles here.

### M2–M5 — container, input, drawing, style — **done**

Mostly absorbed by M1. `GtkFormsContainer` is unchanged apart from its `OnDrawn` comment, which
described a GTK 3 clipping hazard that no longer exists: the context from `Snapshot.AppendCairo` is
already bounded by this widget, so the explicit clip is now free insurance rather than load-bearing.
`VisualElementRenderer` and `PlatformRenderer` needed no change at all.

### M6 — the sweep — **in progress, 18 errors left**

Done:

- **Both pickers → `Gtk.Popover`.** `DatePickerWindow` and `TimePickerWindow` were borderless popup
  toplevels that positioned themselves by hand and took a seat grab. A popover replaces all three at
  once, so both classes got *shorter*. Their hand-stroked 1px Cairo frames are deleted — a popover is
  themed, and drawing that border would now put a second line inside the real one.
- **All three menus → `Gtk.Popover`** (`CellBase` context menu, `ShellWidget` overflow; the toolbar
  one remains).
- **`ShellWidget`'s preferred size → a `Gtk.BoxLayout` subclass.** Its `OnGetPreferredWidth/Height`
  overrides would have compiled and silently never run (§4.3). The layout manager's measure vfunc is
  where GTK 4 asks the question.
- **`Gdk.Screen` → `Gdk.Display`** throughout, including `GtkPlatformServices.GetNamedColor`, which
  built a synthetic `StyleContext` + `WidgetPath`; both are gone, so it probes a real widget instead.
  There is no primary monitor in GTK 4 — no portable notion of one under Wayland — so the monitor
  list's first entry is what remains.
- **`StyleExtensions`** (§4.4 item 2), **icon/stock names**, **gestures** (`DragBegin`/`DragEnd` are
  bound as `DragStarted`/`DragEnded`; gestures are constructed unattached and given to a widget),
  **focus** (read-only in GTK 4 — `GrabFocus()` returns whether focus was *taken*, better than the old
  unconditional `true`), and **tap counting** via `NPress`, which incidentally lifts a ceiling: the old
  switch could not express four taps because there was no `FourButtonPress`.
- **`Widget.Window.Raise()` ×7 → one `Raise()` helper.** GTK 4 draws siblings in child order, so being
  last *is* being on top.

### M7 — WebView — not started

Delete `Controls/WebKit2.cs` (263 LOC of hand-rolled P/Invoke against `libwebkit2gtk-4.1`, which links
against **GTK 3** and cannot be embedded in a GTK 4 hierarchy). Re-target `Controls/WebView.cs` onto
`Net4x.WebkitGtkSharp`, keeping the degrade-to-placeholder behaviour — with a real binding the check
becomes `GLibrary.IsSupported(Library.Webkit)`.

`Controls/OpenGLView.cs` wraps `Gtk.GLArea`, which exists in GTK 4; only `HasAlpha` goes.

### M8 — tests, CI, gallery — not started

- `GtkTestHost.PressButton` → drive the controller (§4.4 item 7); `Find<T>` → `GetFirstChild`/
  `GetNextSibling`; `Pump`'s `SizeAllocate` → `Allocate`.
- **Re-measure the GTK 3 findings recorded in comments there.** `Gtk.Window.Resize()` is *removed* in
  GTK 4, so the workaround's premise changed even though the workaround still applies; and `Retire`'s
  "never `Destroy()` a test window" predates the fork's idempotent `Window.Destroy`. Re-test both; do
  not change them speculatively.
- `.github/workflows/linux-gtk.yml`: `libgtk-3-0`/`libgtk-3-dev` → `libgtk-4-1`/`libgtk-4-dev`,
  `libwebkit2gtk-4.1-0` → `libwebkitgtk-6.0-4`, `pkg-config gtk+-3.0` → `gtk4`. `GDK_BACKEND=x11`
  still applies — GTK 4 prefers Wayland when `WAYLAND_DISPLAY` is set, and Xvfb is X11.
- Update `CLAUDE.md`, which says GTK 3, GtkSharp 3, `libgtk-3-0` and `Gtk.Window.Resize()`.

---

## 6. What remains

Everything compiles and two of the three suites pass. What is left is **behavioural convergence of
the GTK renderer suite** — a phase of its own, and the only one that cannot be done by reading the
compiler.

### The suite now runs, which it did not

Three harness defects had to be fixed before a single widget test could report anything, and each
was a hard failure rather than a wrong answer:

| Defect | Why it was fatal |
|---|---|
| GTK initialised on an xUnit thread-pool thread | GTK 4's GDK Win32 backend calls `OleInitialize` during `gtk_init`, which **requires STA**; pool threads are MTA. The process died with `Gdk-ERROR: OleInitialize failed`, exit `0xC0000409`, before any test ran. An apartment cannot be changed after a thread starts, so `GtkTestHost` now owns a thread and every test body runs inside `Run(() => …)` on it. (The sibling GtkSharp suite is built the same way, for the same reason.) |
| Allocating without measuring | GTK 4 requires measure-before-allocate. `Pump` allocated the toplevel directly, GTK logged *"Allocating size to … without calling gtk_widget_measure()"*, and the allocation **did not propagate** — so renderers were never laid out and half the suite failed with `NullReferenceException` rather than with a wrong number. |
| `Find<T>` walking `is Gtk.Container` | GTK 4 has no `GtkContainer`; children hang off `GtkWidget` itself. The walk stopped at the first real GTK 4 widget, so tests reported that a renderer "built no `Gtk.ScrolledWindow`" when it had. Now `FirstChild`/`NextSibling`. |

Also ported here: `PressButton` (GTK 4 has no public `GdkEvent` constructor and no
`gtk_widget_event`, so it drives the widget's own `GtkGestureClick`), `PixelAt`/`RenderToBytes` (a
widget owns no pixels; readback goes through a `GtkWidgetPaintable` into a `GdkTexture`), `BoundsIn`
(a GTK 4 allocation is widget-local, so `Allocation.X/Y` are always zero — every position assertion
would have compared 0 with 0 and passed regardless of the layout), and `Resize`.

### The failures that remain

**42 of 185 cases**, measured per class:

| Class | Total | Failed |
|---|---:|---:|
| `RendererRegistrationTests` | 103 | 0 |
| `PlatformServiceTests` | 10 | 0 |
| `LifecycleTests` | 4 | 1 |
| `CoreControlMappingTests` | 8 | 1 |
| `VisualElementTrackerTests` | 6 | 2 |
| `FontLayoutTests` | 8 | 2 |
| `PumpHarnessTests` | 2 | 2 |
| `LayoutTests` | 16 | 8 |
| `CollectionViewIncrementalTests` | 8 | 8 |
| `M5ControlTests` | 20 | 18 |
| `PropertyMappingTests` | 17 | **hangs — see below** |

They are **not** one systemic cause; the systemic ones (measure-before-allocate, the child walk)
are fixed. What is left is per-renderer: minimum sizes GTK 4 enforces where GTK 3 clipped
(`Allocation height too small … needs at least 400x339` is logged throughout), widgets a renderer
no longer builds, and paint timing.

### The one hang, and what is already ruled out

`PropertyMappingTests.EntryMapsTextPlaceholderAndPassword` never returns. Do this one first: a hang
takes CI with it and names no test, and it holds the built assemblies open so that even a rebuild
fails.

Evidence from a full dump (`dotnet-dump collect` on the live process, `clrstack` on the STA GTK
thread) — the managed stack stops here:

```
entry.Text = "changed"                       PropertyMappingTests.cs:32
  → EntryRenderer.OnElementPropertyChanged   EntryRenderer.cs:49
  → EntryRenderer.UpdateText()               EntryRenderer.cs:106
  → Gtk.Entry.set_Text(string)
  → ILStubClass.IL_STUB_PInvoke              ← never returns
```

So GTK is looping inside `gtk_editable_set_text`, which is the correct GTK 4 entry point (the
binding does not still call the removed `gtk_entry_set_text` — that was checked). Ruled out by
experiment:

- **Not our `::changed` handler.** Detaching `_entry.Changed += EntryChanged` entirely does not
  stop the hang.
- **Not the unbounded pump loops.** Both were unbounded and are now capped
  (`GtkTestHost.MaxDrainIterations`); the hang is inside one native call, not in the harness's
  iteration.
- **Not a bare `Gtk.Entry`.** The GtkSharp suite sets `entry.Text` in several tests and passes.

That leaves the widget tree around it — the compat `Container`/`EventBox` subclasses with their
`OnMeasure`/`OnSizeAllocate` overrides, and `EntryWrapper` itself. GTK logs
`gtk_accessible_text_get_contents: assertion 'end >= start' failed` and a GtkTextBuffer
iterator-invalidation warning repeatedly while it spins, which is the next thread to pull.

Two real defects were found and fixed while chasing it, both worth keeping regardless:

- `EntryWrapper` stacked the entry and its placeholder as **two children of the same
  `Gtk.Grid` cell**. GTK 3 tolerated overlapping attachments; GTK 4 does not. It is now a
  `Gtk.Overlay`, which is the widget GTK 4 provides for exactly this.
- `ShowPlaceholderIfNeeded` is now idempotent. It used to restack a GdkWindow (free); it now
  toggles visibility, which **queues a resize** — and it is called inline from size-allocate, which
  the comment above that call already warned was invalid.

### Deferred by decision, not blocked

The deprecated cell-renderer world (`ListStore`, `CellRendererText`, `CellView`, `ComboBox`,
`TreeIter`) → `Gtk.ListView`/`ColumnView`; `Gtk.MessageDialog` → `Gtk.AlertDialog`; per-widget style
providers → `AddProviderForDisplay`; `Gdk.CairoHelper.SetSourcePixbuf` → paintables. Each is
inventoried in the `NoWarn` comment in `Xamarin.Forms.Platform.GTK.csproj` with its replacement
named.

## 7. Open questions

**O1 — how CI gets `Packages/`. RESOLVED for now, option (a).** The workflow checks out
`pieroviano/GtkSharp` at a **pinned commit**, runs `dotnet cake build.cake --BuildTarget=PackageNuGet`,
copies the nupkgs into `Packages/`, and fails loudly if the feed is empty. Pinned rather than
branch-tracking, because the shims there and the backend here move together. The original framing
follows, since option (b) is still the intended end state:

 It is a junction into a machine-local drop folder; committing it is
impossible and `.gitignore` already excludes it. Either **(a)** a CI step that checks out the GtkSharp
repo at a pinned `net4x.gtk4` commit, runs `dotnet cake build.cake`, and copies the nupkgs in — no
binaries in git, CI builds two repositories, and the GtkSharp suite can gate the same run; or
**(b)** publish `Net4x.*` to nuget.org and let CI restore from the public feed. **(a)** during the
migration, since the shims iterate fast and CI needs to see each iteration; **(b)** as the end state.

Until this is settled, every gate below that says "and on Linux" is unverified. A WSL clone on the
Linux filesystem cannot see the junction at all — build from `/mnt/d` there.

**O2 — WebKitGTK 6.0 on the CI runner.** Confirm `libwebkitgtk-6.0-4` is installable on
`ubuntu-latest`. If it is not, WebView degrades to its existing placeholder and M7 is deferred rather
than blocking — but say so explicitly rather than letting the suite go quietly green.

---

## 8. Verification

| Gate | Command | Expected |
|---|---|---|
| Restore, Windows | `dotnet restore Xamarin.Forms.Gtk.sln` | `Net4x.*` 4.22.4.x from `local-net4x` |
| Restore, WSL | `wsl dotnet restore Xamarin.Forms.Gtk.sln` | same; the relative source is the thing under test (O1) |
| Build | `dotnet build Xamarin.Forms.Gtk.sln -c Debug` | clean; `TreatWarningsAsErrors` makes any deprecation a failure |
| Build, Release | `dotnet build Xamarin.Forms.Gtk.sln -c Release` | clean |
| Core | `dotnet test Xamarin.Forms.Core.UnitTests` | 4848 pass — should never have been affected |
| Xaml | `dotnet test Xamarin.Forms.Xaml.UnitTests` | 1046 pass |
| GTK, Windows | `dotnet test Xamarin.Forms.Platform.GTK.UnitTests` | 169 pass |
| GTK, Linux | `xvfb-run -a --server-args="-screen 0 1280x1024x24" dotnet test …` | 169 pass, `GDK_BACKEND=x11` |
| Gallery | `dotnet run --project Xamarin.Forms.ControlGallery.GTK` | window opens, pages navigate, no GTK criticals |
| GtkSharp | `dotnet cake build.cake --BuildTarget=Test` (in the GtkSharp repo) | green, including `CompatTests` |

`CoreControlMappingTests.BoxViewPaintsItsColourAndRepaintsOnChange` is a **known** Windows-passes /
Xvfb-fails difference on GTK 3. Re-check it after the drawing port: the snapshot/GSK pipeline may fix
it or move it. Either way record the result — do not let it silently become "expected to fail".

Before pushing, `git diff` against `5.0.0` and confirm every change is a general GTK 4 rule, not
something reverse-engineered from one gallery page or one test.

---

## 9. Risks

| # | Risk | Mitigation |
|---|---|---|
| **R1** | The input shim does not reproduce GTK 3 propagation closely enough, and gestures land on the wrong element in nested layouts. Still the highest-probability failure: the shims compile and unit-test, but only the renderer suite exercises them in a real tree. | `VisualElementTrackerTests` and the tap/gesture cases in `M5ControlTests` are the oracle. Controllers are attached in the bubble phase and claim the sequence on `RetVal`. |
| **R2** | O1 unresolved: nothing is verified on Linux, so a Windows-only regression can hide until M8. | Settle O1 before M7, not at M8. |
| **R3** | **Materialised** as wave 3, and larger than predicted: not just the cell renderers but `Widget.Show()`/`Hide()`, `Allocation` and `MessageDialog` — 147 errors. | Resolved as planned. The mechanical two thirds are ported; the rest is `<NoWarn>$(NoWarn);CS0612</NoWarn>` in `Xamarin.Forms.Platform.GTK.csproj` and `Xamarin.Forms.Maps.GTK.csproj` only, above a comment naming the replacement for every suppressed API. Deliberately **not** in `Directory.Build.props`, where it would also cover Core, Xaml and Build.Tasks and hide the next deprecation in code that has nothing to do with GTK. |
| **R4** | The repack-in-place loop (§1) depends on `Net4x.NuGetUtility` clearing the package cache — Windows-only, and the very tooling this repo has been removing. If `Maps.GTK` also drops it, repacks stop being visible, silently. | If a shim is "missing" despite being written, check `~/.nuget/packages/net4x.gtksharp/` before debugging anything else. |
| **R5** | The tooling rewrites `PackageReference Include="GtkSharp"` and has undone pins three times, which would now mean two assemblies owning namespace `Gtk`. | `_XFAssertNoGtk3Package` (M0). |
| **R6** | Behavioural drift the compiler cannot see: `Popover.Autohide` dismisses on *any* outside click where the Gtk 3 popups closed on their own button-press handler; `Toggled` fires only on change where `Clicked` fired on every press; `Focused` fires on focus-in where `grab-focus` fired on an explicit grab. All are defensible, none are identical. | These are what the gallery walk-through in §8 is for. Each is documented at its call site. |
| **R7** | WebKitGTK 6.0 unavailable on CI (O2). | Ship M7 behind the placeholder path and say so. |
| **R8** | The "MEASURED:" comments across this backend were measured **on GTK 3** — `FlyoutPage`'s `Fixed` re-allocation, `GtkTestHost`'s `Window.Resize`, `GtkFormsContainer`'s clip. Carrying them forward unexamined bakes in claims that may now be false. | Every milestone that touches such a comment re-measures and rewrites it. A stale "MEASURED:" note is worse than none. |

---

## 10. Not in scope

- Replacing the deprecated `TreeView`-based `ListView`/`TableView` with `Gtk.ListView`/`ColumnView`
  (R3 follow-up).
- libadwaita (`Net4x.AdwaitaSharp`) styling.
- The `gtk` workload / `Net4x.GtkSharp.Sdk` packaging path — this repository consumes plain
  `PackageReference`s and should keep doing so.
- Renaming the published package: `Net4x.Xamarin.Forms.Platform.GTK` stays; only its `<Description>`
  changed.
