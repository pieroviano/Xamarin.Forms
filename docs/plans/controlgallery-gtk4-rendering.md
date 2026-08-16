# Restoring `Xamarin.Forms.ControlGallery.GTK` on `gtk4`

Bringing the gallery's rendering back to what the GTK 3 build produces — and, where the GTK 3 build
is itself wrong, past it.

| | |
|---|---|
| Under repair | `d:\CommonLibrary\Xamarin.Forms`, branch **`gtk4`** (`ca42b7385`) |
| Reference | `d:\CommonLibrary\Xamarin.Forms_Gtk3`, branch **`5.0.0`** (`8feab8a3f`) — the same repository, one commit behind; the whole GTK 4 port is the single commit `ca42b7385` |
| Bindings | `d:\CommonLibrary\GtkSharp`, branch **`net4x.gtk4`** (`4819ad247`), `Net4x.*` 4.22.4.26225, in scope for edits |
| Visual comparison | the `window-capture` MCP server from `d:\CommonLibrary\ProcessWindowBitmapSave` (`list_windows`, `capture_window`) |

Because the two trees differ by exactly one commit, **`git diff 5.0.0 gtk4 -- <path>` is the primary
diagnostic instrument in this plan**: every behavioural difference in the gallery is attributable to
those 66 files, or to the bindings underneath them.

---

## 0. Decisions

| # | Decision | Consequence |
|---|---|---|
| 1 | **Target: the launch screen plus the pages reachable from it.** | Bounded and verifiable. The deep gallery pages (1369 imported upstream issue repros) are explicitly follow-up work, inventoried in §8 rather than silently dropped. |
| 2 | **Match GTK 3, then fix GTK 3's own flaws too.** The reference capture has real defects — "AbsoluteLayout Gallery - Legacy" overprints the search box, "Click to Force GC" sits beside "Accessibility" — and the pre-existing Pango font fallback (§3, R5). | The GTK 4 build should end up *better* than the reference. Cost: "same as GTK 3" stops being the acceptance test on those specific areas, and each deviation needs its own justification — see the resolution in decision 5. |
| 3 | **Prefer fixing the GtkSharp compat surface** when the defect is "the shim does not behave as the GTK 3 API did". | Every consumer benefits and this repo stays thin — the migration's central mechanism (`gtk4-migration.md` §0 decision 2). Cost: a repack loop per iteration (§1.3). *Preference, not a rule*: where the contract plainly belongs to the Forms backend, the fix goes here, and R1 below is exactly that case. |
| 4 | **Verification: a committed screenshot-diff test**, capturing the gallery through the MCP server's capture path and diffing against a stored baseline with a pixel-delta threshold. | Strongest available gate. Costs a Windows-only, non-headless test lane and baseline maintenance — §6 covers the DPI/theme brittleness that decides whether this is usable or noise. |
| 5 | **Derived, and it resolves the tension between 2 and 4: the stored baseline is a curated GTK 4 golden image, not the raw GTK 3 capture.** | Decisions 2 and 4 are otherwise contradictory — you cannot diff against a reference you have deliberately decided to improve on. The GTK 3 capture is the *review* reference (a human/agent compares and signs off); the signed-off GTK 4 capture then becomes the *regression* baseline. Committing the GTK 3 PNG as the pixel baseline would encode its known defects as required behaviour. |

**Acceptance gate:** window geometry within tolerance of the GTK 3 reference (816 × 639 ± the
decoration delta), the flyout and detail list both rendering, **zero** `gtk_box_append` assertions,
**zero** allocate-without-measure warnings, no Pango font fallback, and the screenshot-diff test
green against its signed-off baseline.

---

## 1. Reproducing this — do it before changing anything

Every number in §2 came from these steps; the plan is falsified if they do not reproduce.

### 1.1 Build both galleries

```powershell
dotnet build-server shutdown
dotnet build d:\CommonLibrary\Xamarin.Forms\Xamarin.Forms.ControlGallery.GTK\Xamarin.Forms.ControlGallery.GTK.csproj -c Debug
dotnet build d:\CommonLibrary\Xamarin.Forms_Gtk3\Xamarin.Forms.ControlGallery.GTK\Xamarin.Forms.ControlGallery.GTK.csproj -c Debug
```

**Trap, hit while writing this plan.** If an editor or a stale MSBuild node holds
`artifacts\build-tasks\netstandard2.0\Xamarin.Forms.Build.Tasks.dll`, the build fails with
`error XF006` and its own advice (`dotnet build-server shutdown`). If the lock survives that — it did
here, held by an MSBuild node started outside this session — add `-p:XFAllowStaleBuildTasks=true`,
which downgrades XF006 to a warning. That is safe **only** while `Xamarin.Forms.Build.Tasks` is not
being modified, which is true for all of this work. Do not kill the holding process blindly; it is
likely an open Visual Studio.

The two runtimes do **not** collide: `%LOCALAPPDATA%\Gtk` holds `3.24.24` and `4.22.4` side by side,
and each output directory carries its own managed bindings. This was checked, because a shared
unpack directory would have been a far simpler explanation than anything in §3.

### 1.2 Run both and capture

Launch each `bin\Debug\net10.0\Xamarin.Forms.ControlGallery.GTK.exe` with its own directory as the
working directory, **redirecting stderr to a file** — the GTK warning stream is half the evidence and
it is otherwise lost. Then, through the MCP server:

- `list_windows` with `processName: "Xamarin.Forms.ControlGallery.GTK.exe"` — record `width`/`height`
  per `hwnd`. Ignore the `PseudoConsoleWindow` entry; the real one has class `gdkSurfaceToplevel`
  (GTK 4) or `gdkWindowToplevel` (GTK 3).
- `capture_window` with that `hwnd` and an explicit `outputPath`.

The GTK 4 capture comes back at 2× the reported window height (HiDPI): 828 × 7860 on screen, 828 ×
15420 in the PNG. Account for that scale factor before comparing anything, and in §6's diff.

### 1.3 The GtkSharp repack loop (needed for every decision-3 fix)

```powershell
cd d:\CommonLibrary\GtkSharp
dotnet cake build.cake --BuildTarget=PackageNuGet --BuildVersion=4.22.4.<new>
```

`Packages/` in this repo is a junction onto the GtkSharp drop folder, so the packages land in this
feed directly. **The version must change or NuGet serves the cached copy** — `build.cake:41` defaults
`BuildVersion` to `DateVersion()`, which is `4.22.4.{yy}{dayOfYear}` and therefore *constant within a
day*. Two rebuilds on the same day silently produce the same version. Either pass `--BuildVersion`
explicitly with a counter, or clear `%USERPROFILE%\.nuget\packages\net4x.gtksharp\<version>` between
iterations. Then bump both spellings in this repo:
`Directory.Build.props:119-120` and `Directory.Nuget.Props:18-19`.

---

## 2. The measured regression

| | GTK 3 (reference) | GTK 4 (`gtk4`) |
|---|---|---|
| Window size | **816 × 639** | **828 × 7860** |
| Window class | `gdkWindowToplevel` | `gdkSurfaceToplevel` |
| Flyout (red master pane) | present, left half | **absent** |
| Detail content | header + list rows ("SwapRoot - Tests", "SwapRoot - CarouselPage", "Go to Test Cases", search box, "Accessibility", "Click to Force GC") | **only the "GTK# Core Gallery" header** |
| Painted area | whole client area | top ~500 px; everything below is unpainted black |
| stderr lines | **2** | **21** |

Both galleries launch, neither crashes, and both stay responsive. This is a rendering and layout
regression, not a startup failure.

### 2.1 The GTK 4 warning stream, with the GTK 3 control

| Count | Message | In GTK 3? |
|---:|---|---|
| 14 | `Gtk-CRITICAL: gtk_box_append: assertion 'gtk_widget_get_parent (child) == NULL' failed` | **no — new** |
| 6 | `Gtk-WARNING: Allocating size to __gtksharp_1_Gtk_EventBox <ptr> without calling gtk_widget_measure(). How does the code know the size to allocate?` | **no — new** |
| 1 | `Pango-WARNING: couldn't load font "Normal 11" / "Not-Rotated 11", falling back to "Sans …", expect ugly output` | **yes — pre-existing** |

Running the GTK 3 build with stderr captured is what makes this table worth anything: without the
control, the font warning reads as a migration regression and would have been chased as one. It is
not. It is in scope only because of decision 2, and it is the *last* thing to fix, not the first.

All six allocate-without-measure warnings name `__gtksharp_1_Gtk_EventBox` — the compat `EventBox`,
and nothing else in the tree.

---

## 3. Root causes

Three are pinned to a line. One is not, and this plan says so rather than guessing — §4 is the step
that pins it.

### R1 — The 828 × 7860 window: bottom-up size propagation in a top-down layout framework

**Status: mechanism identified, exact culprit widget to be confirmed by §4.1.**

Xamarin.Forms performs its own layout. It measures and positions every element itself, then pushes
the result down as explicit sizes; nothing in a Forms page has a meaningful "natural size" to report
to GTK. GTK 4 nevertheless asks, and — unlike the GTK 3 code path — the answer now travels upward:

- `Compat/Container.cs:170-188` (`MeasureChildren`) reports `max(offset + childMinimum)` and
  `max(offset + childNatural)` to its parent.
- `Controls/FlyoutPage.cs:15` — `public class FlyoutPage : Fixed` — is a **real GTK 4 `Gtk.Fixed`**,
  so `GtkFixedLayout` answers the measure vfunc with the union of its children's bounds. Note that
  `Compat/Container.cs:19-25` documents precisely this hazard for its own class and deliberately
  derives from bare `Widget` to keep the vfunc reachable; `FlyoutPage` predates that reasoning and
  does not benefit from it.
- `FormsWindow.cs:15-17` asks for `SetDefaultSize(800, 600)` and `SetSizeRequest(400, 400)`. In
  GTK 4 a toplevel is sized to `max(default size, child minimum)`, so **a child minimum of ~7800
  overrides the 600 outright**. GTK 3's window resolved the same conflict in favour of the default
  size at first map, which is why the reference build is 639 px tall.

The loop is self-reinforcing, and that matters for the fix: the window grows to the content's
natural height → `FormsWindow.OnSizeAllocated` (`FormsWindow.cs:104-113`) reports that height back
to Forms via `SetElementSize(new Size(828, 7860))` → Forms lays the page out at 7860 → the size
requests it writes down make the natural height 7860 for real. Whatever the seed value was, after
one cycle the state is consistent and self-justifying. **Do not diagnose this from the steady state**
— instrument the *first* measure pass (§4.1).

**Fix, subject to §4.1 confirming which widget reports the outsized minimum.** The Forms root
container must not export its content extent as a GTK size request: a Forms page's minimum size is
not a GTK concept, and reporting one hands GTK authority that Forms already owns. Concretely, the
page-level container overrides `OnMeasure` to return `0` for both minimum and natural, leaving
GTK's own width-request/height-request handling to apply any size Forms explicitly asked for.

This is the one place where **decision 3's preference is deliberately overridden**: the compat
`Container` is a faithful stand-in for `gtk_container_add` + GtkFixed semantics, and GTK 3's GtkFixed
genuinely did report the bounding box of its children. Changing that in GtkSharp would make the shim
*wrong* for every other consumer in order to suit this one — precisely the inversion the repository's
own rule forbids. The policy "a Forms-hosting container reports no size request" belongs to
`Xamarin.Forms.Platform.GTK`, in `GtkFormsContainer` / the page renderer's container.

`FlyoutPage : Fixed` is the exception that may still need a GtkSharp-side answer: a `Gtk.Fixed`
subclass cannot override the measure vfunc at all, because its layout manager answers first. If
§4.1 implicates it, the fix is to reparent it onto the compat `Container` (which exists for exactly
this reason) rather than to fight `GtkFixedLayout`.

### R2 — 14 × `gtk_box_append` assertion: the compat `Box.Add`/`PackStart` are not reparent-safe

`Compat/Children.Compat.cs:83-87`:

```csharp
public void Add(Widget widget)
{
    if (widget != null)
        Append(widget);
}
```

`gtk_box_append` asserts that the child has no parent, and **on assertion failure it does nothing** —
the widget is silently not added. `PackStart`/`PackEnd` (`:47-64`) have the same hole; so, by
inspection, do `Fixed.Add` → `Put` (`:133-137`) and `Grid.Add` → `Attach` (`:150-156`).

The GTK 3 API these stand in for did not behave this way. `gtk_container_add` on an already-parented
widget warned and refused; GTK 3 code therefore treats a repeat `Add` as a harmless no-op, and code
that *moves* a widget between containers calls `Remove` first. The compat `Container.Add`
(`Compat/Container.cs:51-59`) already gets this right — `if (widget == null || Find(widget) != null) return;`
— and the `Box`/`Fixed`/`Grid` partials simply do not.

**Fix (GtkSharp, decision 3 squarely):** give every compat `Add`/`PackStart`/`PackEnd`/`Put` the same
guard as `Container.Add` — no-op when the child is already parented **to this widget**, and unparent
first when it is parented elsewhere. The second half is a deliberate improvement on GTK 3's refuse-
and-warn, and it needs to be spelled out in the doc comment as such, because it is the difference
between "a stale parent silently eats the child" and "reparenting works".

Whether these 14 failed appends are *why* the flyout and the list rows are missing is exactly the
open question in §4.2. They are certainly a defect regardless.

### R3 — 6 × allocate-without-measure, all on the compat `EventBox`

GTK 4 requires `gtk_widget_measure()` before `gtk_widget_allocate()`; when it is skipped, **the
allocation does not propagate** and children keep whatever geometry they had — which is how a widget
tree ends up laid out but unpainted, as in §2's "top 500 px" observation.

`gtk4-migration.md` §6 records this same defect being found and fixed *in the test harness*
(`GtkTestHost.Pump`). The gallery's warnings prove it also exists **in the library**, which the
harness fix never touched — and the library is the product. The allocation path to audit is
`Compat/Container.cs:210-230` (`AllocateChildren`, which does measure — twice, correctly) against
every caller that reaches `Widget.Allocate` or `OnSizeAllocated` *without* going through it. Since
all six warnings name `EventBox` and `EventBox` adds no allocation logic of its own
(`Compat/EventBox.cs`), the caller is in `Xamarin.Forms.Platform.GTK` — the renderers that override
`OnSizeAllocated` and allocate children by hand.

**Fix:** wherever the backend allocates a child directly, measure first. Where the pattern repeats,
lift it into a helper next to `AllocateChildren` rather than repeating the two `Measure` calls.

### R4 — The missing flyout and the missing list rows

**Status: not root-caused. Do not write the fix until §4.2 has run.**

The most visible half of the regression is the least understood, and the honest statement is that
three mechanisms could each produce it, they are not mutually exclusive, and the evidence collected
so far does not separate them:

1. **A consequence of R2.** 14 silently-refused appends is more than enough to lose a flyout and a
   list. `Controls/FlyoutPage.cs:61-76` builds its tree with exactly the calls in question —
   `_flyoutContainer.PackStart(_titleContainer, …)`, `PackEnd(_flyout, …)`, `_flyoutRevealer.Add(...)`,
   `_flyoutContainerWrapper.Add(...)`, then `Add(_detail)` and `Add(_flyoutContainerWrapper)`.
2. **A consequence of R3.** Content that is parented and measured but never allocated renders as
   nothing, and the unpainted region below 500 px fits this exactly.
3. **Independent: the flyout is never revealed.** The flyout sits inside a `Gtk.Revealer` created
   with `RevealChild = false` (`FlyoutPage.cs:54-59`) and a `_flyoutContainerWrapper` with
   `NoShowAll = true` (`:47`), whose visibility is driven by `RefreshFlyoutVisibility`. GTK 4 removed
   `gtk_widget_show_all` and `NoShowAll` outright; if the compat provides them as no-ops, the
   wrapper's visibility is now governed by something that has stopped doing anything. The reference
   build shows the flyout *expanded on launch*, so this path demonstrably ran in GTK 3.

Hypothesis 3 is the one that is independent of R1–R3 and would survive fixing them, so §4.2 tests it
first.

### R5 — `couldn't load font "Normal 11"` — pre-existing, in scope only by decision 2

`Extensions/LabelExtensions.cs:51`:

```csharp
builder.AppendFormat(" font=\"{0}\"", fontDescription.ToString());
```

`FontDescriptionHelper.CreateFontDescription` (`Helpers/FontDescriptionHelper.cs:19-23`)
deliberately leaves `Family` unset when the Forms element names no font family, but then sets
`Weight` and `Style` unconditionally. `pango_font_description_to_string` serialises every
*explicitly set* field, so a family-less description stringifies to `"Normal 11"` — and the `font=`
attribute in Pango markup parses a leading token as a family. Hence the fallback, and hence
`"Not-Rotated 11"`, which is Pango's spelling of gravity `South`.

The binding is not at fault: `PangoSharp/Generated/Pango/FontDescription.cs:410-412` is a straight
call to `pango_font_description_to_string`. This is a Xamarin.Forms defect, present identically in
both branches (`LabelExtensions.cs` is untouched by `ca42b7385`).

**Fix:** do not round-trip through a description string when no family was requested — emit only the
markup attributes that were actually asked for (`size`, `weight`, `style`), or omit the `font`
attribute entirely and let the widget's own font apply. Fix it **last**: it changes label metrics
across the whole gallery and would otherwise contaminate the screenshot baseline while R1–R4 are
still moving.

---

## 4. Diagnostics to run before writing any fix

Two experiments, each answering a question the plan cannot answer by reading.

### 4.1 Which widget reports the outsized minimum, on the *first* measure

Instrument temporarily — this is diagnosis, not a change to keep. In `FormsWindow`, and in the
compat `Container.MeasureChildren`, log `GetType().Name`, the widget's `WidthRequest`/`HeightRequest`
and the `minimum`/`natural` it is about to return, for the passes that happen **before** the first
`OnSizeAllocated`. Launch, capture stderr, read the first 50 lines.

Answers: whether the ~7800 originates in a Forms container's explicit size request or is
manufactured by summation/union on the way up, and whether `FlyoutPage`'s `GtkFixedLayout` is in the
chain. That decides between the two fixes named in R1 — and it must be read from the first pass,
because the steady state is self-consistent either way.

**Cross-check, free and immediate:** run the same instrumented question against the GTK 3 build. The
two logs side by side name the divergence directly.

### 4.2 Is the flyout absent, or present-but-unrevealed?

Before touching R2 or R3, determine which of R4's three mechanisms is live:

- Walk the widget tree at startup (`FirstChild`/`NextSibling` — **not** `is Gtk.Container`, per
  `gtk4-migration.md` §6) and log each node's type, `Visible`, and allocation. If
  `_flyoutContainerWrapper`, `_flyoutRevealer` and `_flyout` are all *present* in the tree, R2 is not
  the cause of the missing flyout and hypothesis 3 is.
- Check what `NoShowAll` and `ShowAll()` resolve to in the compat surface. If either is a no-op, then
  `RefreshFlyoutVisibility`'s contract — "the wrapper's visibility is driven by RefreshFlyoutVisibility,
  so it must survive the ShowAll() calls the renderers make on their parents" (`FlyoutPage.cs:45-47`)
  — is a comment describing a mechanism that no longer exists, and every renderer that relies on
  `ShowAll` to make its subtree visible is suspect, not just this one.

The second bullet is the higher-yield one: if `ShowAll` is a no-op, that is a systemic cause with a
single fix, and it would explain missing content well beyond the flyout.

---

## 5. Order of work

Ordered so that each step's effect is observable before the next one perturbs it. Re-run §1.2 and
record geometry + stderr counts after **every** step; a step that does not move a number in §2 needs
explaining before you move on.

| # | Step | Expected observable |
|---|---|---|
| 1 | §4.1 + §4.2 diagnostics | Root causes for R1 and R4 named; no product change |
| 2 | **R2** — reparent-safe `Add`/`PackStart`/`PackEnd`/`Put` in GtkSharp compat, + repack (§1.3) | `gtk_box_append` assertions: 14 → **0** |
| 3 | **R4** — whatever §4.2 named (likely the `ShowAll`/`NoShowAll`/`Revealer` visibility path) | Flyout and list rows appear |
| 4 | **R3** — measure-before-allocate in the backend's allocation paths | Allocate-without-measure: 6 → **0**; the unpainted region below 500 px paints |
| 5 | **R1** — Forms root container stops exporting a size request | Window: 828 × 7860 → **≈800 × 600** |
| 6 | Navigate the pages reachable from the launch screen; reconcile each against the GTK 3 build | Decision 1's scope met |
| 7 | **R5** — the `font=` markup attribute | Pango fallback warnings → **0**; label metrics shift, so re-baseline §6 afterwards |
| 8 | Decision 2's own item: the GTK 3 layout flaws (overlapping "AbsoluteLayout Gallery - Legacy" / search box, "Click to Force GC" placement) | GTK 4 build renders them correctly, ahead of the reference |

Steps 2 and 5 may reorder if §4.1 shows R1 is the seed that makes the rest unmeasurable — a 7860 px
window distorts every subsequent capture. If so, do R1 first and accept that its "before" evidence is
noisier.

---

## 6. Verification — the committed screenshot-diff test

Per decisions 4 and 5. Three parts, and the third is the one that decides whether this is a gate or a
nuisance.

**The harness.** A test project referencing the capture code from
`d:\CommonLibrary\ProcessWindowBitmapSave\src\ProcessWindowBitmapSave` — `net10.0-windows`,
`System.Drawing.Common`, already carrying `PrintWindow`-based capture that works on a background
window. Reference the project, or lift the capture routine; **do not** drive the MCP server from a
test, which would make the suite depend on an agent host. The test launches the gallery, waits for
the toplevel (class `gdkSurfaceToplevel`) to appear, captures, compares, and kills the process.

**The baseline.** A signed-off GTK 4 PNG committed under the test project — *not* the GTK 3 capture
(decision 5). Regenerating it must be an explicit, reviewed act: an `-p:UpdateGalleryBaseline=true`
switch or equivalent, never an automatic overwrite on mismatch. Every regeneration after step 7 or 8
changes what the project considers correct, so the commit that moves the baseline is the commit that
must justify it.

**The threshold, and the honest risk.** A raw pixel-equality diff will not survive DPI scaling
(the capture already returns 2× the window height on this machine), theme, or font availability.
The test must therefore: assert window geometry within a tolerance first — that alone catches the
828 × 7860 defect, the single largest one here; normalise scale before comparing; and compare with a
per-pixel tolerance plus a percentage-of-differing-pixels threshold rather than exact equality.
**If the false-positive rate makes it unusable, the fallback is the geometry-and-log gate**
(window size within tolerance, zero `gtk_box_append` assertions, zero allocate-without-measure,
zero Pango fallback) which is CI-friendly and catches every defect found so far. Decide this
explicitly after seeing the first ten runs; do not let a flaky pixel gate rot in the suite.

**Regression tripwires that must not move:** `Xamarin.Forms.Core.UnitTests` (4856) and
`Xamarin.Forms.Xaml.UnitTests` (1043) never touch GTK and must stay green throughout.
`Xamarin.Forms.Platform.GTK.UnitTests` is **not** a gate here — it stands at 42 failures of 185 plus
one hang (`gtk4-migration.md` §6). Record its count before and after: R1–R3 are library-level layout
and allocation fixes and *should* move it downward. If it moves upward, that is a regression this
work caused and it must be resolved, not absorbed.

---

## 7. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| R1's self-reinforcing loop makes the steady state look consistent and the wrong widget gets "fixed" | High | §4.1 instruments the *first* measure pass specifically, and cross-checks against the GTK 3 build |
| Fixing R2 in GtkSharp changes behaviour for every consumer of the bindings | Medium | The change makes the shims match `Container.Add`, which is the existing in-repo precedent; the GtkSharp suite (1708 passed / 0 failed / 36 skipped) is the regression gate, and reparenting semantics get stated in the doc comment |
| Same-day repack serves a stale package and a fix appears to do nothing | Medium, and it will happen | §1.3: pass `--BuildVersion` explicitly; if a fix produces no observable change, verify the package version in `project.assets.json` **before** re-diagnosing |
| Screenshot baseline brittleness (DPI/theme/fonts) makes the gate flaky | Medium | §6's tolerance design, plus a pre-committed fallback to the geometry-and-log gate |
| Decision 2 ("fix GTK 3's flaws too") quietly expands into a redesign | Medium | Scope is fixed to the two named flaws in step 8; anything else found goes to §8 as follow-up |
| R5 shifts label metrics and invalidates the baseline | Low, certain | Sequenced last, with an explicit re-baseline step |
| The gallery's deeper pages are still broken and it looks "fixed" | Low | §8 states the boundary; step 6 covers only what the launch screen reaches |

---

## 8. Not in scope

- The 1369 imported upstream issue repros and the gallery pages not reachable from the launch screen
  (decision 1). If step 6 surfaces failures there, inventory them here rather than fixing them.
- The `Xamarin.Forms.Platform.GTK.UnitTests` failures and the `PropertyMappingTests` hang — tracked
  in `gtk4-migration.md` §6. Watched as a tripwire (§6), not repaired here.
- The deprecated cell-renderer world (`ListStore`/`CellRendererText`/`ComboBox` → `Gtk.ListView`/
  `ColumnView`), `MessageDialog` → `AlertDialog`, per-widget style providers — deferred by decision
  in `gtk4-migration.md` §6, and unrelated to what §2 measured.
- CI integration of the screenshot test: it is Windows-only and non-headless, and the existing lane
  (`.github/workflows/linux-gtk.yml`) is Ubuntu. Adding a Windows lane is a separate decision.
