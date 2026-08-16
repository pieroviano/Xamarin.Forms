# Golden images for the gallery launch screen

`ControlGallery-Launch.png` is a **curated GTK 4** image, not the GTK 3 capture — see decision 5 in
`docs/plans/controlgallery-gtk4-rendering.md`. Committing the GTK 3 reference would encode its
known defects as required behaviour, and the whole point of decision 2 is that the GTK 4 build
should end up better than the reference.

So the image only becomes correct once a human or agent has compared it to the reference and
signed it off. That sign-off is recorded below; the differences it accepts are itemised in plan
§11, which is the document a reviewer should read before moving this file.

## Sign-off — 2026-08-16

| | |
|---|---|
| Captured from | `gtk4` @ `e2e6fbe1` + the working-tree layout fix, `Net4x.*` 4.22.4.26229 |
| Compared against | the GTK 3 submodule at `Xamarin.Forms/` (branch `5.0.0`, `4ee520f8`), captured the same way in the same session |
| Toplevel | 828 × 629; the GTK 3 reference is 816 × 639 (802 × 632 as the DWM frame trims it) |
| Convergence | 10 consecutive launches, all 828 × 629, one allocation critical each — the bistable layout of plan §10 is gone |
| Run-to-run pixel noise | **zero** on this machine: repeated captures differ by 0 of 520 812 pixels |

**Known defects this image encodes**, each open work and each a reason to regenerate rather than
to widen a tolerance (full detail and measurements in plan §11):

1. The `ListView` header band (*Go to Test Cases* / search / *Click to Force GC*) overprints the
   fifth row, *SwapRoot - NavigationPage*, which is drawn clipped through its middle.
2. The navigation bar title stays **yellow**. `CoreNavigationPage` sets `BarTextColor = Yellow` and
   resets it to `Color.Default` two seconds later; the reference honours the reset by the time the
   harness captures (8 s dwell) and this build does not.
3. `BarBackgroundColor = Maroon` is not applied at all — same in the reference, so not a port
   regression.

Anything else that moves is a real change. Do not widen a tolerance to make a failure go away.

## Regenerating

```powershell
dotnet test Xamarin.Forms.ControlGallery.GTK.ScreenshotTests -p:UpdateGalleryBaseline=true
```

or, against binaries that are already built, `XF_UPDATE_GALLERY_BASELINE=1`. Either way the test
writes the file and reports **skipped** — a regenerating run verifies nothing. Review the image,
update the sign-off above, then commit it in a change that says why it moved.

The image is captured at the display's own scale (measured **1×** on the machine this was written
on; plan §1.2's earlier "2×" reading came from the 828 × 7860 runaway window and does not
reproduce). A baseline taken at a different scale still compares: `ImageComparer` resamples onto
the baseline's grid and requires the aspect ratios to agree first. Nothing in the code names a
scale factor.
