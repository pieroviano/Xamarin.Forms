# Restoring `Xamarin.Forms.ControlGallery.GTK` on `gtk4`

Bringing the gallery's rendering back to what the GTK 3 build produces — and, where the GTK 3 build
is itself wrong, past it.

| | |
|---|---|
| Under repair | `d:\CommonLibrary\Xamarin.Forms`, branch **`gtk4`** (`e2e6fbe1` at the time of §9/§11) |
| Reference | the **GTK 3 submodule** at `d:\CommonLibrary\Xamarin.Forms\Xamarin.Forms\`, branch **`5.0.0`** (`4ee520f8`), carrying its own `Xamarin.Forms.Gtk3.sln` |
| Bindings | `d:\CommonLibrary\GtkSharp`, branch **`net4x.gtk4`** (`5f79efd6f`), `Net4x.*` 4.22.4.26228, in scope for edits |
| Visual comparison | the `window-capture` MCP server from `d:\CommonLibrary\ProcessWindowBitmapSave` (`list_windows`, `capture_window`), and the committed harness in §6 |

**The sibling clone `d:\CommonLibrary\Xamarin.Forms_Gtk3` no longer exists.** It was replaced by the
submodule above; older prose in this document and elsewhere that names that path means the submodule.
Both branches still live in *this* repository, so **`git diff 5.0.0 gtk4 -- <path>` remains the primary
diagnostic instrument in this plan** and works unchanged from the outer repo: every behavioural
difference in the gallery is attributable to the GTK 4 port's single commit, or to the bindings
underneath it.

---

## 0. Decisions

| # | Decision | Consequence |
|---|---|---|
| 1 | **Target: the launch screen plus the pages reachable from it.** | Bounded and verifiable. The deep gallery pages (1369 imported upstream issue repros) are explicitly follow-up work, inventoried in §8 rather than silently dropped. |
| 2 | **Match GTK 3, then fix GTK 3's own flaws too.** The reference capture has real defects — "AbsoluteLayout Gallery - Legacy" overprints the search box, "Click to Force GC" sits beside "Accessibility" — and the pre-existing Pango font fallback (§3, R5). ⚠ **The first two of those three do not reproduce; re-captured 2026-08-16 the reference shows neither, and the GTK 4 build is the one that overprints. See §11 — the decision stands, its premise is inverted.** | The GTK 4 build should end up *better* than the reference. Cost: "same as GTK 3" stops being the acceptance test on those specific areas, and each deviation needs its own justification — see the resolution in decision 5. |
| 3 | **Prefer fixing the GtkSharp compat surface** when the defect is "the shim does not behave as the GTK 3 API did". | Every consumer benefits and this repo stays thin — the migration's central mechanism (`gtk4-migration.md` §0 decision 2). Cost: a repack loop per iteration (§1.3). *Preference, not a rule*: where the contract plainly belongs to the Forms backend, the fix goes here, and R1 below is exactly that case. |
| 4 | **Verification: a committed screenshot-diff test**, capturing the gallery through the MCP server's capture path and diffing against a stored baseline with a pixel-delta threshold. | Strongest available gate. Costs a Windows-only, non-headless test lane and baseline maintenance — §6 covers the DPI/theme brittleness that decides whether this is usable or noise. |
| 5 | **Derived, and it resolves the tension between 2 and 4: the stored baseline is a curated GTK 4 golden image, not the raw GTK 3 capture.** | Decisions 2 and 4 are otherwise contradictory — you cannot diff against a reference you have deliberately decided to improve on. The GTK 3 capture is the *review* reference (a human/agent compares and signs off); the signed-off GTK 4 capture then becomes the *regression* baseline. Committing the GTK 3 PNG as the pixel baseline would encode its known defects as required behaviour. |

**Acceptance gate**, and where each clause actually stands as of 2026-08-16. Measured, not asserted;
the ratios come from the protocol in §1.2 and the harness in §6.

| Clause | Status | Evidence |
|---|---|---|
| Window geometry within tolerance of the GTK 3 reference (816 × 639 ± the decoration delta) | **met** | 828 × 629, 10 launches in 10 (§9) |
| The flyout and detail list both rendering | **met** | §9, §11 — red flyout, header, and the full row list |
| **Zero** `gtk_box_append` assertions | **met** | §9 |
| **Zero** allocate-without-measure warnings | **met** | §9 |
| No Pango font fallback | **met** | §9, and R5 corrected below |
| The screenshot-diff test green against its signed-off baseline | **met** | §6, §11 — 4 passed / 0 skipped, twice consecutively |
| *Implied by the above, and originally missed:* one settled layout, not two | **met** | §10 — was 6 bad launches in 10, now 0 in 10 |

**Not met, and deliberately outside the gate as written.** Two visual defects survive into the
signed-off baseline (§11): the `ListView` header band overprints the fifth row, and the navigation
bar title does not revert when `BarTextColor` returns to `Color.Default`. Neither is a geometry, log
or convergence failure, which is precisely the blind spot a pixel gate exists to cover — they were
found by the eye comparison in §11 and are now pinned by the baseline. Every launch also still emits
exactly one `GtkLabel … needs at least 45x59` critical; it is named and budgeted in the harness
rather than swallowed, and is follow-up work (§8).

**And the gate never covered decision 1's second half.** Decision 1 scopes this work to "the launch
screen **plus the pages reachable from it**", but every clause above is a launch-screen measurement.
§5 step 6 — navigate those pages and reconcile each against the reference — **was never done**, and
no gate would have caught that. Everything verified here is one screen deep. The follow-up inventory
is §8.1.

---

## 1. Reproducing this — do it before changing anything

Every number in §2 came from these steps; the plan is falsified if they do not reproduce.

### 1.1 Build both galleries

```powershell
dotnet build-server shutdown

# GTK 4, under repair. Build the SOLUTION, not the gallery csproj alone - alone it hits
# project-ordering failures around Xamarin.Forms.Build.Tasks.
dotnet build d:\CommonLibrary\Xamarin.Forms\Xamarin.Forms.Gtk.sln `
  -c Debug -p:XFAllowStaleBuildTasks=true -nodeReuse:false

# GTK 3 reference, in the submodule. Here the gallery csproj alone is the RIGHT target:
# Xamarin.Forms.Gtk3.sln fails in Xamarin.Forms.Xaml.UnitTests with a DebugXamlCTask
# PdbException ("Could not find the '/NAMES' stream"), which has nothing to do with the
# gallery and does not block it. MEASURED 2026-08-16: the csproj builds clean, 0 warnings.
dotnet build d:\CommonLibrary\Xamarin.Forms\Xamarin.Forms\Xamarin.Forms.ControlGallery.GTK\Xamarin.Forms.ControlGallery.GTK.csproj `
  -c Debug -p:XFAllowStaleBuildTasks=true -nodeReuse:false
```

**Kill any running gallery before building** — `Get-Process -Name Xamarin.Forms.ControlGallery.GTK |
Stop-Process -Force` — or it holds the output DLLs and the build fails on a file lock.

**Building the submodule dirties it.** MEASURED 2026-08-16: the reference build rewrites its own
`Xamarin.Forms/Directory.Nuget.Props`, replacing `GtkSharpVersion` 3.24.24 with 4.22.4 and renaming
`XamarinFormsGtkVersion`. Restore still resolves the correct `GtkSharp/3.24.24.95` and the reference
gallery genuinely runs as GTK 3 (`gdkWindowToplevel`), so the build is not wrong — but the submodule
is left modified, and a submodule pointer committed in that state would carry the edit. **Check
`git -C Xamarin.Forms status` and `git -C Xamarin.Forms checkout -- Directory.Nuget.Props` after
building the reference.**

**`-nodeReuse:false` is not optional here.** MSBuild node reuse holds the XAML build-task assembly
for fifteen minutes and the next build then fails with XF006.

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

**Correction to the original reading here.** This section used to say the GTK 4 capture comes back at
2× the reported window height (HiDPI), quoting 828 × 7860 on screen against 828 × 15420 in the PNG.
That does not reproduce and was an artefact of R1's runaway window: re-measured 2026-08-16, on the
same machine and the same display, `capture_window` returns **1:1** — an 828 × 629 toplevel captures
as an 828 × 629 PNG. Nothing downstream should assume a factor either way, which is why §6's
comparer normalises on the two images' own dimensions and names no scale in code.

**One launch tells you nothing.** The §10 defect was intermittent — the same binary settled at two
different heights, 6 launches in 10 landing on the wrong one — so a single before-and-after pair is
not evidence, and treating it as evidence is the mistake that cost this effort an hour. **Every claim
about layout here needs at least six launches reported as a ratio; ten is better.** Per launch: start
the exe with stderr redirected to a file, dwell ~11 s, read the toplevel's `GetWindowRect`, count the
lines matching `needs at least`, kill it. §6's harness automates exactly this.

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

The middle column is the GTK 3 control. The third is what the port looked like when this plan was
written; the fourth is where it stands after §9's and §10's fixes, re-measured 2026-08-16 over ten
launches.

| | GTK 3 (reference) | GTK 4, as found | GTK 4, now |
|---|---|---|---|
| Window size | **816 × 639** | **828 × 7860** | **828 × 629**, 10 launches in 10 |
| Client area Forms actually gets | 800 × 600 | — | 800 × **561** (GTK 4 draws its own title bar inside the toplevel; §11) |
| Window class | `gdkWindowToplevel` | `gdkSurfaceToplevel` | `gdkSurfaceToplevel` |
| Flyout (red master pane) | present, left half | **absent** | present, 300 × 561 |
| Detail content | header + list rows ("SwapRoot - Tests", "SwapRoot - CarouselPage", "Go to Test Cases", search box, "Accessibility", "Click to Force GC") | **only the "GTK# Core Gallery" header** | all of it (§11 itemises what still differs) |
| Painted area | whole client area | top ~500 px; everything below is unpainted black | whole client area |
| `gtk_box_append` assertions | 0 | 14 | **0** |
| allocate-without-measure | 0 | 6 | **0** |
| Pango font fallback | 1 | 1 | **0** |
| stderr lines per launch | 2 | 21 | **1**, and the same one every time (§9) |

Both galleries launch, neither crashes, and both stay responsive. This was a rendering and layout
regression, not a startup failure.

### 2.1 The GTK 4 warning stream, with the GTK 3 control

| Count | Message | In GTK 3? | Now |
|---:|---|---|---|
| 14 | `Gtk-CRITICAL: gtk_box_append: assertion 'gtk_widget_get_parent (child) == NULL' failed` | **no — new** | 0 |
| 6 | `Gtk-WARNING: Allocating size to __gtksharp_1_Gtk_EventBox <ptr> without calling gtk_widget_measure(). How does the code know the size to allocate?` | **no — new** | 0 |
| 1 | `Pango-WARNING: couldn't load font "Normal 11" / "Not-Rotated 11", falling back to "Sans …", expect ugly output` | **yes — pre-existing** | 0 |
| — | `Gtk-CRITICAL: Allocation height too small. Tried to allocate 45x20, but GtkLabel … needs at least 45x59` | not checked | **1, every launch** (F4) |

Running the GTK 3 build with stderr captured is what makes this table worth anything: without the
control, the font warning reads as a migration regression and would have been chased as one. It is
not. It is in scope only because of decision 2, and it is the *last* thing to fix, not the first.

All six allocate-without-measure warnings name `__gtksharp_1_Gtk_EventBox` — the compat `EventBox`,
and nothing else in the tree. That reading was *literally* true and led one step in the wrong
direction; see R3.

**A category this table did not have, and should have:** the last row. It is the only warning that
survives, and it was never in the original inventory because the original inventory counted a single
launch. §6's harness now budgets it explicitly rather than letting it hide in a total.

---

## 3. Root causes

Three were pinned to a line when this was written; one was not, and the plan said so rather than
guessing. **All five are now fixed. Two of the five diagnoses below turned out to be wrong or
incomplete and have been corrected in place** — R1 (incomplete: the compat `EventBox` allocation
rule was the real source of the 72 px runaway) and R5 (wrong: the markup was never at fault). R4,
the one the plan refused to guess at, was solved by a mechanism *none* of its three hypotheses
predicted, which is a point in favour of refusing to guess.

Each subsection carries a status line. Where a diagnosis was corrected, the original reasoning is
kept alongside the correction, because the wrong turn is the part a future reader is most likely to
repeat.

### R1 — The 828 × 7860 window: bottom-up size propagation in a top-down layout framework

**Status: FIXED, but this section's original diagnosis was only half of it — see the correction at
the end.**

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

#### Correction — the diagnosis above was incomplete

`PlatformRenderer.OnMeasure` returning zero is correct and it stays; §4.1 confirmed the summation
chain, and the log showed the climb at exactly 72 px a pass. But **that was the amplifier, not the
whole source**. The 72 px runaway's real origin was the **compat `EventBox` using `GtkFixed`'s
allocation rule instead of `GtkBin`'s** (fixed in the bindings, 4.22.4.26228, commit `2f04f35f`).
A `GtkBin` gives its single child the whole allocation; `GtkFixed` gives each child its *natural*
size at a position. With the `Fixed` rule, a page's content collapsed to natural size instead of
filling what it was given, and every renderer above it then re-derived a size request from that
collapse. Fixing the allocation rule is what let a page's content fill the space it was handed —
and it is a bindings fix, so decision 3 does apply after all to this half of R1, even though the
`OnMeasure` half genuinely belonged in `Xamarin.Forms.Platform.GTK`.

Two more defects in the same area turned out to matter as much and are recorded here so R1 is not
read as a single-line fix (both in `2f04f35f` / `ab500f57`):

- **`RemoveFromContainer` was a no-op at 24 call sites.** It was one `self as Container` cast, which
  was exhaustive in GTK 3 because every container derived from `GtkContainer`. GTK 4 has no
  container class at all: `Box`, `Fixed`, `Grid` and the compat `Container` are unrelated types that
  each carry their own `Remove`, so the cast returned null and the removal silently did nothing.
  Widgets a caller believed it had replaced stayed parented for good.
- **`Widget.SizeAllocate` allocated without measuring** — see R3, which is where the visible symptom
  showed up.

### R2 — 14 × `gtk_box_append` assertion: the compat `Box.Add`/`PackStart` are not reparent-safe

**Status: FIXED as designed below (GtkSharp `35257d9cc`). 14 → 0.**

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
open question in §4.2. They are certainly a defect regardless. **Answered: they were not** — the
appends went to zero and the content was still absent. See R4.

### R3 — 6 × allocate-without-measure, all on the compat `EventBox`

**Status: FIXED (GtkSharp `35257d9cc`). 6 → 0, and the culprit was one line — see the resolution at
the end of this subsection, because the reasoning below points one level too far down the tree.**

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

**Resolution, and it was one line from being invisible.** `Widget.SizeAllocate` in the bindings
allocated without measuring. The single call site in the whole backend is
`FlyoutPage.AllocateWrapperToRevealer` (`FlyoutPage.cs:447`), which measures the **revealer**
(`:434-435`) and then allocates the **wrapper** — and GTK 4's precondition is on the widget being
*allocated*, so it was never satisfied. That is why all six warnings named `EventBox` even though
`EventBox` adds no allocation logic of its own, and it is why the flyout "animated invisibly": the
allocation was refused every frame. The audit this section prescribed was the right one; it was the
inference "so the caller is in `Xamarin.Forms.Platform.GTK`" that was a step too far.

### R4 — The missing flyout and the missing list rows

**Status: FIXED (`ab500f57`, `2f04f35f`), and by NONE of the three hypotheses below. The real cause
is stated after them; they are kept because each was eliminated by experiment and re-testing them
would be waste.**

**The real cause: the GTK 3 `show` vfunc never fires under GTK 4.** GTK 3 widgets were born hidden
and `gtk_widget_show_all` walked the tree flipping each one, so the `show` vfunc fired once per
widget. **GTK 4 widgets are born visible**, so setting `Visible = true` is not a transition and
`OnShown` is *never* invoked. Probed on both bindings with the same widget: GTK 3 fires it once,
GTK 4 zero times, while `map` fires on both. `OnShown` was the only initial caller of
`PageElementPackager.Load` — which builds a page's child renderers — and of `UpdateBackgroundColor`,
so pages had neither content nor background. Proven causally by forcing `Visible = false/true` across
the live tree, which made the missing content appear at once. All four `OnShown` overrides moved to
`OnMapped` (`AbstractPageRenderer`, `PageRenderer`, `NavigationPageRenderer`,
`VisualElementRenderer`); `map` is also the more honest hook, since a mapped widget really is on
screen. The detail *list* additionally needed the `RemoveFromContainer` and compat-`EventBox`
allocation fixes recorded under R1.

The three hypotheses this section originally offered, and how each died:

| # | Hypothesis | Outcome |
|---|---|---|
| 1 | A consequence of R2 (silently-refused appends) | **Eliminated.** Appends went to 0 and the content was still absent. |
| 2 | A consequence of R3 (parented and measured but never allocated) | **Eliminated as the cause.** Real defect, fixed, and it was needed — but fixing it alone did not bring the content back. |
| 3 | The flyout is never revealed — `ShowAll`/`NoShowAll` have become no-ops | **Eliminated.** The compat `ShowAll` is `Visible = true` and `NoShowAll` is inert (`Widget.Compat.cs:437, 511`), but GTK 4 widgets are visible by default, so this is consistent rather than lossy. The plan called this "the higher-yield one"; it was wrong, and the neighbouring observation — that GTK 4 changed *when* visibility transitions happen — was the thread worth pulling. |

The original text of the three, for the reasoning they carry:

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

### R5 — `couldn't load font "Normal 11"` — pre-existing, in scope only by decision 2

**Status: FIXED (`971ad859`). The diagnosis this section originally carried was WRONG and has been
replaced; the corrected version is below, and the discarded one is stated with it so the same wrong
turn is not taken again.**

Common ground with the original reading, and still true:
`FontDescriptionHelper.CreateFontDescription` (`Helpers/FontDescriptionHelper.cs:19-23`) deliberately
leaves `Family` unset when the Forms element names no font family, but sets `Weight` and `Style`
unconditionally. `pango_font_description_to_string` serialises every *explicitly set* field, so a
family-less description stringifies to `"Normal 11"` — `"Not-Rotated 11"` being Pango's spelling of
gravity `South`. The binding is not at fault
(`PangoSharp/Generated/Pango/FontDescription.cs:410-412` is a straight call), and the defect is
present identically in both branches.

**What this section got wrong.** It blamed `Extensions/LabelExtensions.cs:51` —
`builder.AppendFormat(" font=\"{0}\"", fontDescription.ToString())` — on the reasoning that Pango's
`font=` markup attribute parses a leading token as a family, so `"Normal 11"` would be looked up as
a family called *Normal*. **Probed directly against GTK 4.22.4's Pango, `<span font="Normal 11">`
produces no warning at all** and measures identically to the discrete attributes:
`pango_font_description_from_string` consumes `Normal` as a *style keyword* and leaves `FAMILY`
unset, and attribute merging only applies fields that are set — so the widget's own family survives.
The markup was never the source.

**The real source** is `EditorRenderer.AdjustMinimumHeight` handing the raw description straight to
`PangoContext.GetMetrics`. `GetMetrics` loads a fontset from exactly what it is given and does *not*
merge with the context, so a family-less description is looked up verbatim and Pango falls back. The
same line also computed `fDescr` and then discarded it, passing the unmerged `font` instead — so the
metrics returned were the *fallback* font's: 17.7 px of ascent+descent against the correct 19.51 at
11 pt. `StyleExtensions.ResolveFont` now merges against the widget's own context first, and
`AdjustMinimumHeight` uses the merged description for both purposes.

**A separate, real, and previously silent defect the probing uncovered**, and the reason the markup
in `LabelExtensions` changed anyway: `font=` is a font-description *string*, so Pango re-parses it
and reads trailing words of a family name as style keywords. MEASURED: `font="Times New Roman Bold 11"`
resolves to the family **"Times New"** — *Roman* is Pango's own spelling of `PANGO_STYLE_NORMAL` —
logs `couldn't load font "Times New Bold Not-Rotated 11"`, and measures 102 px where the family
actually requested measures 89 px. Every family whose name ends in a style word is affected: Century
Schoolbook, Segoe UI Light, Arial Black, Roboto Condensed, Franklin Gothic Book. The fix is to emit
the discrete `font_family` / `font_size` / `font_weight` / `font_style` attributes, which take the
name verbatim and cannot mis-parse, and to omit `font_family` entirely when the element named no
family — an attribute list carries only the fields it sets, so the widget's own font supplies the
rest. That is exactly what `FontDescriptionHelper` means by leaving `Family` unset.

The sequencing advice was right and was followed: this landed **last**, because it changes label
metrics across the whole gallery and would otherwise have contaminated §6's baseline while R1–R4
were still moving.

---

## 4. Diagnostics to run before writing any fix

**Both have been run. Their answers are folded into R1 and R4 above; this section is kept for the
method, which is reusable and was vindicated — every fix that stuck in §9 came out of an experiment
here, and every fix that did not came out of reading the steady state.** In particular §4.1's
insistence on the *first* measure pass is what caught the summation chain, and the "instrument, then
diff a good run against a bad one" pattern is what eventually cracked §10.

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

**Outcome.** The second bullet was *not* the higher-yield one — `ShowAll` being `Visible = true` is
harmless when widgets start visible. But it was one question away from the answer: the right question
was not "does `ShowAll` still set visibility" but "**does anything still observe a visibility
transition**", and it does not, which is why `OnShown` never fires (R4). Ask that of any GTK 3
lifecycle hook that has gone quiet.

---

## 5. Order of work

Ordered so that each step's effect is observable before the next one perturbs it. Re-run §1.2 and
record geometry + stderr counts after **every** step; a step that does not move a number in §2 needs
explaining before you move on.

| # | Step | Expected observable | Outcome |
|---|---|---|---|
| 1 | §4.1 + §4.2 diagnostics | Root causes for R1 and R4 named; no product change | **done** — R1 named, R4 named but by a mechanism none of the three hypotheses predicted |
| 2 | **R2** — reparent-safe `Add`/`PackStart`/`PackEnd`/`Put` in GtkSharp compat, + repack (§1.3) | `gtk_box_append` assertions: 14 → **0** | **done**, 14 → 0 |
| 3 | **R4** — whatever §4.2 named (likely the `ShowAll`/`NoShowAll`/`Revealer` visibility path) | Flyout and list rows appear | **done** — not the visibility path; the `show`→`map` vfunc change (R4) |
| 4 | **R3** — measure-before-allocate in the backend's allocation paths | Allocate-without-measure: 6 → **0**; the unpainted region below 500 px paints | **done**, 6 → 0 |
| 5 | **R1** — Forms root container stops exporting a size request | Window: 828 × 7860 → **≈800 × 600** | **done**, 828 × 629 (client 800 × 561) |
| 6 | Navigate the pages reachable from the launch screen; reconcile each against the GTK 3 build | Decision 1's scope met | **NOT DONE.** Only the launch screen has been reconciled (§11). This is the largest piece of decision 1 still outstanding. |
| 7 | **R5** — the `font=` markup attribute | Pango fallback warnings → **0**; label metrics shift, so re-baseline §6 afterwards | **done**, 1 → 0, and the diagnosis was corrected on the way (R5) |
| 8 | Decision 2's own item: the GTK 3 layout flaws (overlapping "AbsoluteLayout Gallery - Legacy" / search box, "Click to Force GC" placement) | GTK 4 build renders them correctly, ahead of the reference | **NOT DONE, and the premise has drifted** — see §11. Re-captured 2026-08-16, today's reference build does *not* overprint; the GTK 4 build does. |
| 9 | *Unplanned, and it consumed more time than steps 2–5 combined:* the bistable page layout | One settled geometry, every launch | **done** — §10; 6 bad launches in 10 → 0 in 10 |

Steps 2 and 5 may reorder if §4.1 shows R1 is the seed that makes the rest unmeasurable — a 7860 px
window distorts every subsequent capture. If so, do R1 first and accept that its "before" evidence is
noisier.

**That reordering note fired, and it was right.** Fixing R2/R3 first made the window *worse*
(7860 → 26292): allocations that now propagate and children that now really attach gave the runaway
more to inflate to. R1 had to land before anything else could be judged. The general lesson, which
cost this effort twice: **a fix that removes a suppression makes the symptom it unmasks look like a
regression.** Judge each step against the mechanism it targets, not against the top-line number.

---

## 6. Verification — the committed screenshot-diff test

**Status: BUILT and green. `Xamarin.Forms.ControlGallery.GTK.ScreenshotTests` (`net10.0-windows`),
four tests, 4 passed / 0 skipped in ~77 s, twice consecutively on 2026-08-16.** The design below is
what was specified; what was built follows it, with the deviations recorded at the end of this
section. Read this section for the reasoning and the test file's own remarks for the measurements.

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

### 6.1 What was actually built, and where it deviates

Four tests, layered cheapest and most diagnostic first:

| Test | Gate |
|---|---|
| `TheToplevelSettlesAtTheReferenceGeometry` | 828 × 629 ± 24. The tolerance is deliberately a third of `GtkToolbarConstants.ToolbarHeight` (72) — the exact gap §10's bistable layout settled into — so it cannot swallow the defect it exists to catch. `XF_GALLERY_EXPECTED_WINDOW=WxH` overrides it for a different display scale. |
| `TheLaunchLogHasNoStructuralWarnings` | Hard zeros for R2, R3 and R5. This is the plan's pre-committed geometry-and-log fallback, kept *alongside* the pixel gate rather than instead of it. |
| `TheLayoutConvergesToTheSameGeometryOnEveryLaunch` | Six launches; each must land inside the geometry tolerance and inside a budget of **one** allocation critical. §10's gate. |
| `TheLaunchScreenMatchesItsBaseline` | The pixel diff. Skips, rather than fails, when no golden image is committed. |

Deviations from the design above, each deliberate:

- **The capture code is lifted, not project-referenced.** `ProcessWindowBitmapSave` is an MCP-server
  `Exe` living outside this repository; referencing it would make this suite depend on a tree that is
  not in the solution. `Capture/WindowCapture.cs` says so in its remarks and names its origin.
- **The threshold question resolved better than feared.** §6 predicted run-to-run pixel noise. There
  is none: two separate launches on this machine captured **byte-identically** — 0 of 520 812 pixels
  differing, worst channel delta 0. The noise the plan describes is *machine*-to-machine, not
  run-to-run, so both tolerances (12/255 per channel, 2 % of pixels) have large headroom here. **The
  consequence worth stating: a failure of this comparison on the machine that produced the baseline
  is a real change, not noise. Do not widen a tolerance to make one go away.**
- **The residual critical is budgeted, not asserted to zero and not skipped.** Every launch emits
  exactly one `Allocation height too small. Tried to allocate 45x20, but GtkLabel … needs at least
  45x59` — one over-constrained label at a fixed size, unrelated to the page. §10's defect looked
  nothing like it (20–66 criticals per bad launch, each naming the whole window at `500x633`), so the
  budget of 1 separates "one known, bounded, non-layout critical" from "the page latched a toolbar
  too tall and is now rejecting every allocation". Raising it needs a new measurement here, not a
  nudge in the test.
- **CI.** §8 called a Windows lane a separate decision, and it still is. The Ubuntu lane now builds
  **`Xamarin.Forms.Gtk.Linux.slnf`** instead of the full solution — a filter listing every project
  *except* this one, because `PrintWindow` has no X11/Wayland equivalent. The filter rather than a
  per-project OS condition, because the exclusion is a property of that lane and belongs in a file
  the lane names. **Trap: adding a project to the solution and forgetting the filter is a CI job that
  covers less than it looks like it does.**

**Regression tripwires that must not move**, re-measured 2026-08-16 with every fix in place:

| Suite | Baseline | Now |
|---|---|---|
| `Xamarin.Forms.Core.UnitTests` | 4856 | **4856 passed / 0 failed / 5 skipped** |
| `Xamarin.Forms.Xaml.UnitTests` | 1043 | **1043 passed / 0 failed / 4 skipped** |
| GtkSharp `GtkSharp.Tests` | 1708 / 0 / 36 | **unchanged** |

`Xamarin.Forms.Platform.GTK.UnitTests` is **not** a gate here — it stood at 42 failures of 185 plus
one hang when this plan was written (`gtk4-migration.md` §6). Record its count before and after:
R1–R3 are library-level layout and allocation fixes and *should* move it downward. If it moves
upward, that is a regression this work caused and it must be resolved, not absorbed. **It has not
been re-measured since the §10 fix**, and it should be — that fix changed `AbstractPageRenderer` and
`NavigationPageRenderer`, which several of those tests drive.

---

## 7. Risks

| Risk | Severity | Mitigation | Did it fire? |
|---|---|---|---|
| R1's self-reinforcing loop makes the steady state look consistent and the wrong widget gets "fixed" | High | §4.1 instruments the *first* measure pass specifically, and cross-checks against the GTK 3 build | **Yes, and worse than predicted.** §10's variant of it — a recurrence with no anchor at all, in which *every* height is a fixed point — cost three failed fixes before anyone stopped treating it as arithmetic. The mitigation worked where it was applied; the risk was simply not stated broadly enough. |
| Fixing R2 in GtkSharp changes behaviour for every consumer of the bindings | Medium | The change makes the shims match `Container.Add`, which is the existing in-repo precedent; the GtkSharp suite (1708 passed / 0 failed / 36 skipped) is the regression gate, and reparenting semantics get stated in the doc comment | No. Suite unchanged. |
| Same-day repack serves a stale package and a fix appears to do nothing | Medium, and it will happen | §1.3: pass `--BuildVersion` explicitly; if a fix produces no observable change, verify the package version in `project.assets.json` **before** re-diagnosing | No — the counter was used from the start. Bindings are at 4.22.4.26228. |
| Screenshot baseline brittleness (DPI/theme/fonts) makes the gate flaky | Medium | §6's tolerance design, plus a pre-committed fallback to the geometry-and-log gate | **No, and the fear was misplaced** — run-to-run capture is byte-identical here (§6.1). |
| Decision 2 ("fix GTK 3's flaws too") quietly expands into a redesign | Medium | Scope is fixed to the two named flaws in step 8; anything else found goes to §8 as follow-up | Not yet — step 8 is untouched. But its *premise* has drifted (§11): the reference no longer shows one of the two named flaws, and the GTK 4 build now does. |
| R5 shifts label metrics and invalidates the baseline | Low, certain | Sequenced last, with an explicit re-baseline step | Yes, as designed. R5 landed before the baseline was ever taken, so nothing had to be re-baselined. |
| The gallery's deeper pages are still broken and it looks "fixed" | Low | §8 states the boundary; step 6 covers only what the launch screen reaches | **Live and unmitigated.** Step 6 was never done. The launch screen is verified; nothing behind it is. |
| *Unforeseen:* judging an intermittent defect on one run before and one run after | — | Added after the fact: §1.2's six-launch protocol, and §6's convergence test which states the gate as a ratio by construction | **Yes, twice.** It is the single most expensive mistake recorded in this document. |

---

## 8. Not in scope

- The 1369 imported upstream issue repros and the gallery pages not reachable from the launch screen
  (decision 1). If step 6 surfaces failures there, inventory them here rather than fixing them.
- The `Xamarin.Forms.Platform.GTK.UnitTests` failures and the `PropertyMappingTests` hang — tracked
  in `gtk4-migration.md` §6. Watched as a tripwire (§6), not repaired here.
- The deprecated cell-renderer world (`ListStore`/`CellRendererText`/`ComboBox` → `Gtk.ListView`/
  `ColumnView`), `MessageDialog` → `AlertDialog`, per-widget style providers — deferred by decision
  in `gtk4-migration.md` §6, and unrelated to what §2 measured.
- **Running** the screenshot test in CI: it is Windows-only and non-headless, and the existing lane
  (`.github/workflows/linux-gtk.yml`) is Ubuntu. Adding a Windows lane is still a separate decision.
  *Building* it is now handled — the Ubuntu lane builds `Xamarin.Forms.Gtk.Linux.slnf`, which
  excludes exactly that project (§6.1).

### 8.1 Follow-up work this effort found and did not fix

Each is measured, none is a blocker for what §0's gate covers, and none should be rediscovered.

| # | Item | Evidence |
|---|---|---|
| F1 | The `ListView` header band overprints the fifth row on the launch screen | §11 |
| F2 | The navigation bar title does not revert when `BarTextColor` returns to `Color.Default` | §11 |
| F3 | `BarBackgroundColor` is not applied at all — present in the reference too, so not a port regression | §11 |
| F4 | One `GtkLabel … needs at least 45x59` critical on every launch: a Forms `Label` given a rectangle narrower than the width its desired size was measured at, so Pango wraps to three lines | §6.1, §10 |
| F5 | The page layout path still has **eight nested `GLib.Idle.Add` deferrals** across four files. §10's fix removed the *ambiguity* they were racing over, not the deferrals. They are each a documented workaround for GTK discarding a resize queued from inside size-allocate, and collapsing them into one ordered pass is a redesign, not an edit | §10 |
| F6 | `Xamarin.Forms.Platform.GTK.UnitTests` has not been re-measured since the §10 fix | §6 |
| F7 | Step 6 — the pages reachable from the launch screen — was never reconciled against the reference | §5 |

---

## 9. Progress — 2026-08-16

Steps 1–5, 7 and 9 done; steps 6 and 8 open (§5). Measured against §2 each time, as §5 requires.

**Every number below is a ratio over ten launches**, re-measured after the §10 fix. An earlier
revision of this section quoted single launches — in particular "total stderr lines 21 → 1", which
was the *best case* of a bistable binary and not its state; at the time, 6 launches in 10 emitted
20–66 lines. That is corrected here and the protocol that prevents it is §1.2.

| Observable | §2 baseline | Now (10 launches) |
|---|---:|---:|
| `gtk_box_append` assertions | 14 | **0**, every launch |
| allocate-without-measure | 6 | **0**, every launch |
| Pango font fallback | 1 | **0**, every launch |
| total stderr lines | 21 | **1**, every launch — always the same `GtkLabel … needs at least 45x59` (F4) |
| window size | 828 × 7860 | **828 × 629**, 10 of 10 *(reference: 816 × 639)* |
| client area Forms receives | — | **800 × 561** *(reference: 800 × 600; §11)* |
| flyout + detail rows | absent | **present** — red flyout 300 × 561, header, and the full row list |
| launches settling at the wrong height | 6 in 10 | **0 in 10** (§10) |

### What was fixed, and where

**R2 — GtkSharp** (`35257d9cc`). `Box.Add`/`PackStart`/`PackEnd`, `Fixed.Add`, `Grid.Add` now refuse
to append a child that is already parented, and detach it from a previous parent first, through that
parent's own `Remove` rather than `gtk_widget_unparent`. `Container.Add` had had the first half of
this guard all along; the partials never did.

**R3 — GtkSharp** (`35257d9cc`), and it was one line from being invisible. `Widget.SizeAllocate`
allocated without measuring. The single call site in the whole backend is
`FlyoutPage.AllocateWrapperToRevealer` (`FlyoutPage.cs:447`), which measures the **revealer**
(`:434-435`) and then allocates the **wrapper** — and Gtk 4's precondition is on the widget being
allocated, so it was never satisfied. That is why all six warnings named `EventBox`, and why the
flyout "animated invisibly": the allocation was refused every frame.

**R1 — Xamarin.Forms** (`f3908350`). Root cause confirmed by instrumenting
`Container.MeasureChildren` (§4.1): the window's only child, `PlatformRenderer`, inherited
`GtkFormsContainer`'s union-of-children measure, and Gtk 4 sizes a toplevel to
`max(default size, child minimum)` — so that answer beat `SetDefaultSize(800, 600)` and formed a
feedback loop with `FormsWindow.OnSizeAllocated` feeding the new height back into Forms. The climb
was visible in the log as **1024 → 1096 → 1168 … at exactly 72px a pass**,
`GtkToolbarConstants.ToolbarHeight`. `PlatformRenderer.OnMeasure` now returns zero.

The plan's §5 note about reordering was right, and it fired: fixing R2/R3 first made the window
*worse* (7860 → 26292), because allocations that now propagate and children that now really attach
gave the runaway more to inflate to. R1 had to land before anything else could be judged.

### A defect the plan did not predict

Once the layout fixes let the gallery run further it **died on the finalizer thread**:
`GLib.MissingIntPtrCtorException` out of `ImageRenderer`. `ViewRenderer.Dispose` called
`Control.Destroy()` regardless of the `disposing` flag; `Gtk.Widget.Destroy` reads `Parent`, which
asks GtkSharp to wrap the parent's native handle, and on the finalizer thread that peer may be gone,
so it tries to construct one — and no renderer declares the `(IntPtr)` constructor that needs. Fixed
by honouring `disposing`, which `ImageRenderer.Dispose` one level down already did.

**The sweep, done (`ab500f57`), and the list this section originally printed was WRONG.** It was
produced by reading, not by checking, and it got two entries wrong in opposite directions:

- **`OpenGLViewRenderer` was already correct** — it has guarded on `!_disposed && disposing` all
  along. Listing it as broken would have sent someone to "fix" working code.
- **`SliderRenderer` was broken and was missed entirely.** It is not in the original list.

The seven that actually needed the flag honoured: `ButtonRenderer`, `ListViewRenderer`,
`PageRenderer`, `RadioButtonRenderer`, `ScrollViewRenderer`, `SliderRenderer`, `StepperRenderer`,
`TabbedPageRenderer`. `TabbedPageRenderer` additionally needed a `Page` null check, because
`Destroy()` calls `Dispose(true)` without suppressing finalization, so a second pass is *guaranteed*
rather than merely possible. The general lesson worth keeping: **a list of "same-pattern" defects
compiled by inspection is a hypothesis, not an inventory** — verify each member before quoting the
list, because a wrong entry costs in both directions.

### R4 — resolved

The three eliminations recorded here were all correct, and none of them was the answer; the cause is
now written up in §3 R4 (the GTK 3 `show` vfunc never fires under GTK 4, so `OnShown` — the only
initial caller of `PageElementPackager.Load` — never ran). Kept for the eliminations themselves:

- **Not the missing appends (R2).** They went to zero and the content was still absent.
- **Not `Fixed` swallowing the layout hook.** `FlyoutPage` overrides `OnSizeAllocated`, and
  `Drawing.Compat.cs:159-188` does give `Fixed` that hook and drives it from `OnSizeAllocate`.
- **Not `ShowAll` non-recursion on its own.** The compat `ShowAll` is `Visible = true` and `NoShowAll`
  is inert (`Widget.Compat.cs:437, 511`), but GTK 4 widgets are visible by default, so this is
  consistent rather than lossy.

The tree walk this section queued as "next" is what was run, and forcing `Visible = false/true`
across the live tree is what proved causation — the missing content appeared at once.

### Regression state

GtkSharp `1708 passed / 0 failed / 36 skipped` — unchanged from its documented baseline. Bindings
are at **4.22.4.26228** (26228 plus the compat `EventBox` allocation rule, R1);
`Directory.Build.props:120` and `Directory.Nuget.Props:18` both bumped, and `project.assets.json`
confirms the new version resolved rather than a cached one. `Xamarin.Forms.Core.UnitTests`
4856 / 0 / 5 and `Xamarin.Forms.Xaml.UnitTests` 1043 / 0 / 4, both re-run 2026-08-16 (§6).

---

## 10. The bistable page layout — RESOLVED

Filed as "the toolbar double-count", which is what it looked like and is not what it was. **The
resolution is the first subsection; everything after it is the record of how the defect was measured
and what was eliminated, kept because four separate readings of it were wrong and re-deriving them
would be expensive.** §9's numbers have been corrected in place.

### The fix

**Result: 20 launches, 0 bad, against a 5-in-10 bad control measured on the same machine in the same
session. Independently re-measured for §11: 10 launches, all 828 × 629, one critical each.**

The root cause was not a wrong constant but **a degenerate recurrence with no anchor**.
`NavigationPageRenderer.ApplyChildPageSizes` derived `ContainerArea` from the renderer's own GTK
allocation — while that allocation was itself produced by the size requests `ContainerArea`
generates, because the child page's content is requested at `area.Height` inside a `Controls.Page`
that adds the 72 px toolbar header back. That is

```
alloc' = (alloc − 72) + 72
```

an identity in which **every** height is a fixed point. 561 and 633 were both stable, and nothing in
the system chose between them; which one a launch latched depended only on what the first allocation
pass happened to observe.

What *should* have anchored the loop to the real available space — the parent `FlyoutPage`'s
`DetailBounds` of 500 × 561 — was destroyed by `AbstractPageRenderer.SetPageSize` writing the GTK
allocation back into the Forms `Bounds` of a page whose parent owns them.

The fix has two halves, both general framework-contract corrections with no gallery-specific values:

1. **A page with a `VisualElement` parent no longer writes its GTK allocation back into its Forms
   `Bounds`.** Xamarin.Forms lays out strictly top-down; only the root page takes its size from the
   platform, which `FormsWindow.OnSizeAllocated` already does. A renderer's job is to make GTK match
   the bounds its Forms parent assigned — never the reverse.
2. **`ContainerArea` is derived from `Element.Bounds` instead of the GTK allocation**, and is applied
   from the `Width`/`Height` property notifications — which `VisualElement.SetSize` raises *before*
   calling `SizeAllocated`, i.e. before `Page.LayoutChildren` runs. The toolbar inset therefore
   exists before the child page is ever laid out, which eliminates the transient over-request
   entirely rather than correcting it afterwards.

**This also explains why the three arithmetic fixes below all failed.** With a degenerate recurrence,
changing the constant only moves *which* value gets latched. Every variant reads as "strictly
consistent, still wrong", which is exactly what was observed three times.

**The 50 ms wall-clock guard at `AbstractPageRenderer.cs:154` was NOT the cause**, contrary to the
suspicion recorded at the end of this section. The trace shows it firing identically in good and bad
runs. It is merely unreachable for child pages now, as a side effect of half 1.

**Still open (F5):** the eight nested `GLib.Idle.Add` deferrals are all still there. The fix removed
the ambiguity they were racing over, not the race. Collapsing them into one ordered pass remains the
real structural repair, and it is a redesign of that path rather than an edit to it.

### The measurement in §9 was not sound

§9 and several progress reports quoted "stderr 21 lines → 1". **That figure came from single
launches, and the gallery is not deterministic.** Sampled properly, on the build as it then stood:

| launches | runs with the `needs at least 500x633` criticals |
|---|---|
| 4 | 2 |
| 6 | 4 |

**6 of 10 launches are bad**, emitting 20–66 criticals; the rest emit exactly 1. The good runs are
real but not representative, and the "1 line" claim should be read as "the best case", not "the
state". Anything measured here by launching once is noise — take at least six samples and report the
ratio.

### The defect, as it was read at the time

This reading is *descriptively* accurate — every number in it was measured — and *causally* wrong:
it treats the loop's two sides as an arithmetic disagreement, when the loop had no independent input
at all. Kept because it is what three failed fixes were derived from.

The navigation toolbar is double-counted:

- `Controls/Page.cs:110-136` builds `root = Box(vertical){ _headerContainer (toolbar, expand false),
  _contentContainerWrapper (expand+fill) }`.
- `AbstractPageRenderer.cs:314-326` `SetPageSize()` takes the renderer container's own allocation and
  subtracts `GtkToolbarConstants.ToolbarHeight` to produce the Forms element's `Bounds`.
- The parent writes those same `Bounds` back onto that container as its GTK size request
  (`AbstractPageRenderer.cs:288-292`, `VisualElementRenderer.cs:233-237`).

So the container is *requested* at content height while physically containing toolbar + content, and
its GTK minimum becomes request + 72. Measured chain, window client 561:
`PageRenderer alloc=500x633 req=500x561 min=500x633`, and `633 = 72 + 561`. GTK 4 notices and logs;
GTK 3 never propagated the page size at all, so it pinned content at natural size instead and the
disagreement stayed silent.

### Three arithmetic fixes, all eliminated by experiment

Each was tested with the six-launch protocol, against a baseline of **6 bad launches in 10**. All
three are reverted; none is in the tree.

| # | Hypothesis | Change | Result |
|---|---|---|---|
| 1 | The containers use the wrong allocation rule | `Controls/Page.cs` `_contentContainer` and `NavigationPageRenderer.Widget` `Gtk.Fixed` → `GtkFormsContainer`, plus a `GtkFormsContainer.OnGetPreferred*` short-circuit | **6 bad of 6** — worse; pixels identical |
| 2 | `SetPageSize`'s subtraction is the duplicate, so `ContainerArea` should be the only channel | Removed the `-= GtkToolbarConstants.ToolbarHeight` at `AbstractPageRenderer.cs:320` | **6 bad of 6, catastrophically** — 889 to 2035 criticals per launch, an unbounded loop |
| 3 | The container is sized a toolbar short; mirror the subtraction with an addition | `UpdateChildrenLayout` (`AbstractPageRenderer.cs:288-292`) adds `ToolbarHeight` when the child page is in a `NavigationPage` with a nav bar | **6 bad of 6** |

Hypothesis 1 arrived claiming "three launches out of three give 561". It does not reproduce: sampled
properly it is *worse*, turning an intermittent defect into a consistent one with pixels identical
either way.

Result 2 was the most informative of the three: **the subtraction is load-bearing**, not the
duplicate. Removing it does not merely mis-size the page, it diverges — which in hindsight is exactly
what a degenerate recurrence does when you delete one of the two terms that cancel.

**Three arithmetic fixes were tried, in three different places, and all three produced exactly 6 bad
launches out of 6 against a 6-in-10 baseline.** Nothing improved it; everything made it strictly
consistent. That uniformity was itself the evidence, and it pointed at the right conclusion by the
wrong route: **if the recurrence is an identity, changing the constant only moves which value gets
latched** — so every variant must read as "strictly consistent, still wrong". The reframing at the
time called this a *timing* race; it is better described as an *underdetermined* system, in which
timing decides the outcome only because nothing else does.

**The trap that cost an hour here, worth repeating:** the first comparison was one run before against
one run after, which is exactly how a bistable defect gets misattributed. It read as "the patch broke
it" when the true reading was "both states are unstable". Do not evaluate anything in this area on a
single launch.

### The deferral path — still there, still worth collapsing (F5)

The page layout path is eight nested `GLib.Idle.Add` deferrals across four files —
`AbstractPageRenderer.cs:220, 249, 353`, `NavigationPageRenderer.cs:141, 569`,
`VisualElementRenderer.cs:266, 309`, `Controls/Page.cs:186` — every one of them a documented
workaround for GTK discarding a resize queued from inside size-allocate. The fix above removed the
ambiguity they were racing over; it did not remove the race, and any future change to this path
inherits it. The steps below are what was queued when the defect was still open, and steps 1 and 3
remain the right approach if it ever recurs:

1. Instrument the order. Log every one of those eight callbacks with a sequence number and the
   allocation/request it observes, over ten launches, and diff a good run against a bad one. The
   divergence point is the defect.
2. ~~The `50ms` re-entrancy guard at `AbstractPageRenderer.cs:154` is a wall-clock threshold in a
   layout path — a prime suspect for machine- and load-dependent behaviour.~~ **Eliminated.** Step 1
   was run, and the trace shows the guard firing identically in good and bad runs. A wall-clock
   threshold in a layout path is still a bad smell, but it was not this defect.
3. Only then consider collapsing the deferrals into a single ordered pass. That is a redesign of this
   path rather than an edit to it. **Still the right long-term move, and still not done (F5).**

**Do not attempt another arithmetic adjustment without evidence from step 1.** Three were tried and
paid for; the constant was never where the defect lived.

**The general lesson, and the one worth carrying out of this whole document:** when several
independent corrections to a value all leave the system *equally* wrong, stop correcting the value.
That pattern is the signature of a loop whose output is its own input — and the repair is to find
the quantity that is supposed to come from outside the loop and restore it, not to adjust either
term inside it. Here that quantity was the parent's `Bounds`.

---

## 11. The curated baseline — sign-off and the remaining visual differences

Decision 5's review step, carried out 2026-08-16. Both galleries were built and launched in the same
session on the same display, captured through the same `PrintWindow` path, and compared by eye at
1× and at 3–4× on the regions that differ.

**Precondition, checked first and the reason this was safe to do at all:** §10's bistable layout had
to be gone, because a golden image taken while the layout flips between two states bakes in whichever
one the capture happened to catch. Verified independently of §10's own report: **10 consecutive
launches, all 828 × 629, one allocation critical each, zero at the 633 height.** Baseline taken only
after that.

`Baselines/ControlGallery-Launch.png` is committed, the pixel test passes, and the whole suite runs
**4 passed / 0 skipped**, twice consecutively. The sign-off record lives next to the image in
`Baselines/README.md`; this section is the reasoning behind it.

### Measured geometry, side by side

| | GTK 3 reference | GTK 4 |
|---|---|---|
| Toplevel (`GetWindowRect`) | 816 × 639 | 828 × 629 |
| Capture after DWM frame trim | 802 × 632 | 828 × 629 |
| Red flyout pane | 300 × **600**, at x 1…300, y 31…630 | 300 × **561**, at x 14…313, y 51…611 |
| Client area Forms receives | 800 × 600 | 800 × **561** |
| List row pitch | 41 px | 41 px |

The 39 px the GTK 4 build loses is **GTK 4's client-side decoration**: it draws its own title bar
*inside* the toplevel, where GTK 3 had a native Win32 title bar outside it. That is inherent to the
toolkit, not a layout defect, and it is why the GTK 4 window is 10 px shorter overall while giving
Forms 39 px less. It is also why the toplevel is 12 px wider — the CSD shadow inset.

### What differs, and whether each is an improvement

| # | Difference | Verdict |
|---|---|---|
| 1 | **Navigation bar title colour.** GTK 4 renders "GTK# Core Gallery" in **yellow**; the reference renders it in the theme's default blue-grey. | **Mixed, and net a defect** — see below. |
| 2 | **`ListView` header band overprints the fifth row.** In GTK 4 the *Go to Test Cases* / search / *Click to Force GC* band is drawn over the lower half of *SwapRoot - NavigationPage*, which appears clipped through its middle. The reference draws all five `SwapRoot` rows clear of the band. | **Worse than the reference.** F1. |
| 3 | **Row chrome.** GTK 4 gives each row a white background and a hairline separator; the reference draws rows transparent on the window background with no separator. | Neutral — GTK 4 theme defaults, not a Forms decision. |
| 4 | **Search box and buttons.** GTK 4 insets the search entry with margins and rounded corners and renders the two buttons in grey; the reference draws a flat full-bleed entry and blue button text. | Neutral — theme. |
| 5 | **Window decoration.** CSD header bar with rounded corners versus a native Win32 title bar. | Neutral — inherent to GTK 4. |
| 6 | **Rows visible below the fold.** GTK 4 shows one fewer complete row, a direct consequence of the 39 px lost to CSD. | Neutral. |

**On difference 1**, because the verdict is not obvious. `CoreNavigationPage`
(`Xamarin.Forms.Controls/CoreGallery.cs:106-115`) sets `BarBackgroundColor = Maroon` and
`BarTextColor = Yellow`, then resets **both to `Color.Default` on a two-second timer**. So:

- Applying yellow at all is something the reference does *not* visibly do — the GTK 4 build is
  honouring `BarTextColor` where the reference appears not to.
- But the harness captures after an 8 s dwell, well past the reset, and **the correct rendering at
  that moment is the default colour**. The reference shows the default; GTK 4 still shows yellow. So
  GTK 4 applies the property and then **fails to revert it** (F2).
- Neither build applies `BarBackgroundColor = Maroon` at any point observed (checked at ~1 s and at
  ~12 s: the band is the theme's grey in both) — **not a port regression** (F3).

The revert path exists and is not obviously dead: `FlyoutPageRenderer`'s `MessagingCenter`
subscription (`FlyoutPageRenderer.cs:18-30`) routes `Color.Default` to `UpdateBarTextColor(null)`,
which reaches `FlyoutPage.TitleContainer.UpdateTitleColor` (`Controls/FlyoutPage.cs:660-673`) and
restores `_defaultTextColor`, captured from
`_titleLabel.GetDefaultForegroundColor(StateFlags.Normal)` at `:626`. Which of those links is broken
is **not** established — the candidates are the message never arriving and
`SetForegroundColor(_defaultTextColor, …)` not overriding whatever set the yellow. Stated at low
confidence deliberately; it needs the same instrument-and-diff treatment as everything else here,
not a guess.

### Correction to decision 2's premise

Decision 2 justified "fix GTK 3's flaws too" by naming two defects in the reference capture:
*"AbsoluteLayout Gallery - Legacy" overprints the search box*, and *"Click to Force GC" sits beside
"Accessibility"*. **Re-captured today, the reference shows neither.** Its band and its rows are
cleanly separated, *Click to Force GC* sits in the band where it belongs, and *AbsoluteLayout Gallery
- Legacy* is a clear row below it.

The overprint is now in the **GTK 4** build (difference 2). Whether the original observation was of a
different reference build, a different window size, or was simply misread is not recoverable —
what is recoverable is the measurement, and it says step 8's premise is inverted from what the plan
assumed. **Step 8 is therefore not "fix the reference's flaws"; it is "fix ours", and F1 is the
concrete item.** The reasoning behind decision 2 stands — a GTK 4 build free of the reference's
defects is still the goal, and decision 5 still follows from it — but it must not be cited as
evidence that the reference is the worse of the two here.

### What the baseline encodes, and when to move it

The committed image contains differences 1 and 2 — a defect and a defect-adjacent colour — because a
regression baseline records *current verified state*, not aspiration. Both are named in
`Baselines/README.md` and in §8.1 so that a future fix knows the baseline must move with it.

**Do not widen a tolerance to make a failure go away.** Run-to-run capture on this machine is
byte-identical (§6.1), so on the machine that produced the baseline any failure of the pixel gate is
a real change. Regenerate deliberately, update the sign-off, and say in the commit message why the
image moved.

---

## 11. Renderer suite after the library fixes

Re-measured on 2026-08-16, after the `OnMapped`, `RemoveFromContainer`, compat `EventBox`,
`SizeAllocate`, child-attach and layout-anchor fixes.

| Suite | Documented before | Now |
|---|---|---|
| `Core.UnitTests` | 4856 | **4856 passed / 0 failed** |
| `Xaml.UnitTests` | 1043 | **1043 passed / 0 failed** |
| `Platform.GTK.UnitTests` | 42 failed of 185 | **7 failed of 177** |

The GTK figure excludes `PropertyMappingTests` (the known hang), so it is not strictly
apples-to-apples — but the direction is unambiguous and the two tripwires are exactly on their
documented counts. The plan predicted these library-level fixes would move the renderer suite
downward, and they did.

The 7 remaining: 3 `FontLayoutTests`, 2 `VisualElementTrackerTests` (`InputTransparent` scoping),
`M5ControlTests.LineGeometryChangesAreAcceptedAfterAllocation`,
`CoreControlMappingTests.ImageLoadsItsSourceAndFollowsAChange`, and
`CoreControlMappingTests.BoxViewPaintsItsColourAndRepaintsOnChange` — the last being the
Windows/Linux disagreement already documented in `CLAUDE.md`.

### `FormsEntryFontAttributesChangeAtRuntime` is flaky, and possibly aggravated by the font fix

Two of the three font failures — `EntryFontWeightRoundTripRestoresTheTextLayout`
("the entry's own Pango layout must widen under bold: normal=172 bold=172") and
`ClearingTheFontRemovesTheAttributes` ("clearing must return the label to the theme font's width:
407 != 141") — fail in **every** configuration and are genuinely pre-existing.

The third is not deterministic. Isolated by reverting only the font commit (`971ad859`) and
re-running:

| | fails |
|---|---|
| without the font fix | 1 of 4 runs |
| with the font fix | 4 of 5 runs |

So it is flaky either way, but markedly more likely to fail with the change in. **No mechanism is
known**: the commit touches Label markup, `EditorRenderer` and an additive `ResolveFont` helper, none
of which is on an `Entry`'s path. The plausible link is shared process state — this suite mutates
`Device.PlatformServices`, the `Registrar` and a single GTK context, and runs unparallelised, so an
earlier Label test whose metrics changed can perturb a later Entry one. Unproven.

Do not "fix" it from this evidence. Establish the mechanism first, with enough repeats to separate
the two rates properly; 4-of-5 against 1-of-4 is suggestive, not a result.

**A trap worth repeating**, because it nearly produced a wrong conclusion here: `git revert
--no-commit` stages its inverse, and `git checkout -- <file>` then restores the file *from the index*,
i.e. to the reverted content — so a "with the change" run measured the change absent. Verify which
state the tree is actually in (`grep` for a token the commit introduced) before trusting a
before/after comparison.
