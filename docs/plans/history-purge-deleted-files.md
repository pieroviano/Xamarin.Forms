# Purging deleted files from history

Removing, from every commit on every branch, the files this fork deleted — so they are no longer
retrievable from history and no longer occupy the pack.

Repository: `d:\CommonLibrary\Xamarin.Forms`, origin `https://github.com/pieroviano/Xamarin.Forms.git`.
Tool: **`git-filter-repo`** (`a40bce548d2c`, already installed at `C:\Users\Piero\scoop\shims\git-filter-repo.cmd`),
git `2.53.0.windows.3`.

Measured state at the time of writing (`gtk4` = `ca42b7385`, `5.0.0` = `8feab8a3f`):

| | |
|---|---|
| Refs | 2 branches (`5.0.0`, `gtk4`), **0 tags**, 0 stashes, 0 notes, 0 replace refs, no submodules, 1 worktree |
| Commits | 5883 reachable from all refs; 5882 on `5.0.0`; `gtk4` is `5.0.0` + 1 commit |
| Fork point | `2f8f4864a` *"Update README (#15882)"*, Rachel Kang, 2024-04-29 — last upstream commit; **56** commits are this fork's |
| Objects | 69395 in pack (79.73 MiB) + 2402 loose (6.12 MiB); blobs total 603.7 MiB raw / 77.5 MiB packed |
| Origin | holds exactly `HEAD`, `refs/heads/5.0.0`, `refs/heads/gtk4` — no PR refs, no tags |

---

## 0. Decisions

| # | Decision | Consequence |
|---|---|---|
| 1 | **Path set = this fork's own deletions.** The 2852 paths that existed at the fork point (or were added after it) and are absent from *both* current tips. | Upstream's own decade of churn — `docs/`, `ICSharpCode.Decompiler`, WinRT/WP8 removals, ~81 renamed `Xamarin.Forms.Core` files — is **left alone**. Purging that too would have meant 4973 paths and no defensible stopping point. |
| 2 | **Rewrite range = all history**, not just the 56 fork commits. | 2843 of the 2852 paths existed at the fork point, so a range-limited rewrite would leave them fully intact in the pre-fork history and reclaim nothing. Cost: **every one of the 5883 commit SHAs changes**, including the 5827 inherited from `xamarin/Xamarin.Forms`. History no longer aligns with upstream and every SHA cited anywhere goes dead (§7). |
| 3 | **In place** on `d:\CommonLibrary\Xamarin.Forms`. | No scratch mirror to reconcile afterwards, but the working tree is inside the blast radius — hence the two independent backups in §3, which are not optional. Requires `--force`, because the repo has a remote and a dirty tree. |
| 4 | **Empty commits are dropped** (`--prune-empty auto`, filter-repo's default). | `cc8874b9b` *"Prune files unused by Xamarin.Forms.Gtk.sln"* (2776 deletions) becomes contentless and **disappears entirely**, as will any other commit that touched only purged paths. The record that a prune happened survives only in this repo's docs — which is why §7 is a required step, not a nicety. |
| 5 | **Stop before pushing.** | The plan ends with a verified local repository and a documented push command. Until that command is run, `origin` still holds the old history and is itself a rollback source. |

**Acceptance gate:** both tip trees byte-identical to today's (§6.1), no purged path present anywhere
in history (§6.2), and `Xamarin.Forms.Gtk.sln` still building with all three suites at their
established counts (§6.4).

---

## 1. Blocking verification — before any command in §4 onwards runs

Each of these invalidates the plan if it comes back wrong. None of them are checks the rewrite makes
for you.

**1.1 — No other clone or worktree of this repository exists.**
A rewrite invalidates every clone: after it, the two histories share no commits at all and any pull
becomes an unmergeable mess. `git worktree list` currently reports only `D:/CommonLibrary/Xamarin.Forms`,
but a **separate WSL clone is plausible on this machine** and would not show up there. Confirm by hand:

```powershell
git worktree list
wsl -- bash -lc 'find /home /mnt/d -maxdepth 4 -type d -name .git 2>/dev/null | xargs -I{} dirname {} | grep -i xamarin'
```

If a second clone exists, decide its fate first: re-clone it after the push, or abandon it. Do not
plan to merge it.

**1.2 — The working tree is clean.** `Xamarin.Forms.code-workspace` is currently modified. Commit it
or stash it; filter-repo ends with a hard reset and an in-place run over a dirty tree can lose it.
Note that a stash created *before* the rewrite refers to pre-rewrite commits and will be dangling
afterwards — prefer committing.

```powershell
git status --porcelain          # must be empty (ignored files aside)
```

**1.3 — `git filter-repo` actually runs.** The `python.exe` on `PATH` is the Microsoft Store stub;
the scoop shim carries its own interpreter, so verify the shim rather than python:

```powershell
git filter-repo --version       # expect a version/hash, not a Store redirect or traceback
```

**1.4 — Origin has nothing else on it.** Re-check immediately before pushing, not just now:

```powershell
git ls-remote origin            # expect exactly HEAD, refs/heads/5.0.0, refs/heads/gtk4
```

Anything else — a tag, a `refs/pull/*` entry from an open PR — must be dealt with explicitly, since
the rewrite will not translate it.

**1.5 — Record the invariants.** Capture these now; §6.1 compares against them.

```powershell
git rev-parse '5.0.0^{tree}' 'gtk4^{tree}'
# expected today: 561f3ee37cf867debd4f80d9b1314d4433d28098   (5.0.0)
#                 fea15685fd8fcf4a3dd7f70f6e5656b85a70b68c   (gtk4)
```

If the tips have moved since this plan was written, that is fine — but **regenerate the path list
(§4) against the new tips**, or files added since will be purged as if they had been deleted.

---

## 2. What is purged

**2852 paths**, spanning **13837 blob versions** — 147.5 MiB raw, **21.5 MiB of the 77.5 MiB of
packed blob data** (~28%). Expect the pack to land somewhere around 55–60 MiB; delta compression
makes the exact figure unpredictable.

Whole trees removed by the prune (top 20 roots):

| Paths | Root | | Paths | Root |
|---:|---|---|---:|---|
| 341 | `Xamarin.Forms.Platform.Android` | | 84 | `Xamarin.Forms.ControlGallery.Tizen` |
| 299 | `Xamarin.Forms.Core.Design` | | 80 | `Xamarin.Forms.Platform.MacOS` |
| 263 | `Xamarin.Forms.Platform.iOS` | | 62 | `EmbeddingTestBeds` |
| 235 | `Xamarin.Forms.Platform.Tizen` | | 55 | `Xamarin.Forms.Core.UITests.Shared` |
| 223 | `Xamarin.Forms.Platform.UAP` | | 34 | `Xamarin.Forms.Material.Android` |
| 211 | `Xamarin.Forms.ControlGallery.iOS` | | 30 | `Xamarin.Forms.Platform.Android.UnitTests` |
| 157 | `Xamarin.Forms.Platform.WPF` | | 30 | `Xamarin.Forms.ControlGallery.MacOS` |
| 152 | `DualScreen` | | 27 | `Xamarin.Forms.Platform.iOS.UnitTests` |
| 133 | `Xamarin.Forms.ControlGallery.Android` | | 24 | `build` |
| 109 | `Xamarin.Forms.ControlGallery.WindowsUniversal` | | 23 | `Xamarin.Forms.Material.iOS` |

**2.1 — The 51 paths that need a human look before you run.** Most of the set is whole directories
that no longer exist. These 51 sit *inside* directories that are still live, so purging them edits
history in trees you still work in:

| Paths | Root |
|---:|---|
| 28 | `Xamarin.Forms.Platform.GTK` |
| 9 | `Xamarin.Forms.Controls.Issues` |
| 4 | `Xamarin.Forms.Maps.GTK` |
| 3 | `Xamarin.Forms.Core` |
| 2 | `docs`, 2 `Xamarin.Forms.Platform.GTK.UnitTests` |
| 1 each | `Xamarin.Forms.Xaml.UnitTests`, `Xamarin.Forms.Controls`, `Assets` |

The `Platform.GTK` 28 are the GTK 3 leftovers and the vendored binaries — `Libs/gtk-sharp/**` (14
`.dll`/`.pdb` files), `Libs/webkit-sharp/**`, `Libs/GMaps/*.dll`, the `GLWidget` tree, `GtkOpenGL.cs`,
`app.config`. They are a large share of the reclaimed megabytes and there is no reason to keep them.
Review the list anyway (§4 writes it to a file); this is the one place where an over-broad purge
would take something you still want.

**2.2 — Verified safe.** No entry in the list is quoted or non-ASCII; three contain spaces (the
`Stubs/**/… (Forwarders).csproj` files) which `--paths-from-file` handles literally. No purge entry
is a directory prefix of a live path, so nothing live is shadowed — this was checked explicitly and
must be re-checked if the list is regenerated (§4 does).

---

## 3. Backup — both of them

`origin` is a third safety net, but only until §8 runs.

```powershell
$BK = "d:\CommonLibrary\_backup-xf-prepurge"
New-Item -ItemType Directory -Force $BK
git bundle create "$BK\xamarin-forms-prepurge.bundle" --all
git bundle verify "$BK\xamarin-forms-prepurge.bundle"
Copy-Item -Recurse "d:\CommonLibrary\Xamarin.Forms\.git" "$BK\dot-git-copy"
git rev-parse 5.0.0 gtk4 | Out-File "$BK\pre-purge-tips.txt"
```

The bundle is the portable record; the `.git` copy is the fast rollback (§10). Keep both until the
push in §8 has been verified from a fresh clone.

---

## 4. Generate the path list

Run in **git bash**, not PowerShell — PowerShell's `Set-Content` can emit a UTF-8 BOM, and
filter-repo would then read the first path with a BOM glued to it and silently fail to match it.
Write the list **outside the repository**, so it is neither committed nor swept up by the rewrite.

```bash
cd /d/CommonLibrary/Xamarin.Forms
OUT=/d/CommonLibrary/_backup-xf-prepurge
BASE=2f8f4864a4d289dc89a6228e2ca9d6a49993e365      # fork point: last upstream commit

# live = every path at either tip
{ git ls-tree -r --name-only 5.0.0; git ls-tree -r --name-only gtk4; } | sort -u > $OUT/live.txt

# candidates = the fork-point tree, plus anything this fork added after it
{ git ls-tree -r --name-only $BASE
  git log $BASE..gtk4 $BASE..5.0.0 --pretty=format: --name-only --diff-filter=A | grep -v '^$'
} | sort -u > $OUT/cand.txt

comm -23 $OUT/cand.txt $OUT/live.txt > $OUT/purge-paths.txt
wc -l < $OUT/purge-paths.txt                       # expect 2852 at ca42b7385 / 8feab8a3f
```

Re-run the two safety checks from §2.2 against the freshly generated list:

```bash
grep -c '^"' $OUT/purge-paths.txt                  # expect 0 — quoted/odd paths
awk 'NR==FNR{p[$0]=1;next}{n=split($0,a,"/");s="";for(i=1;i<n;i++){s=(i==1?a[1]:s"/"a[i]);
     if(s in p) print "COLLISION: "s" shadows "$0}}' $OUT/purge-paths.txt $OUT/live.txt
                                                    # expect no output
```

Then **read `purge-paths.txt`** — at minimum the 51 entries under still-live roots:

```bash
awk -F/ '{print $1}' $OUT/live.txt | sort -u > $OUT/live-roots.txt
awk 'NR==FNR{r[$0]=1;next}{split($0,a,"/"); if(a[1] in r) print}' $OUT/live-roots.txt $OUT/purge-paths.txt
```

This file is the entire specification of what the rewrite does. Nothing downstream re-checks it.

---

## 5. The rewrite

```powershell
cd d:\CommonLibrary\Xamarin.Forms
git filter-repo `
  --force `
  --invert-paths `
  --paths-from-file d:/CommonLibrary/_backup-xf-prepurge/purge-paths.txt `
  --prune-empty auto `
  --replace-refs delete-no-add
```

- `--invert-paths` + `--paths-from-file` — remove everything listed, keep the rest. Each line is a
  literal path (matching that path, or anything beneath it if it is a directory).
- `--force` — required: this is not a fresh clone. §1.2 and §3 are what make it safe.
- `--prune-empty auto` — decision 4. Also prunes the merges that degenerate as a result.
- `--replace-refs delete-no-add` — **load-bearing.** Left to accumulate, `refs/replace/*` entries keep
  the old commits reachable, which means the old blobs survive `gc` and the exercise reclaims nothing.

The rewrite touches all 5883 commits; expect it to take minutes, not seconds.

**What filter-repo does to the repo besides rewriting it:** it deletes the `origin` remote (so a
`git push` cannot go out by reflex), expires all reflogs, and repacks. The old objects are gone from
the working repository once it finishes — the backups in §3 are the only local copies.

Then reclaim the space and re-attach the remote:

```powershell
git reflog expire --expire=now --all
git gc --prune=now --aggressive
git count-objects -vH                     # size-pack: expect ~55-60 MiB, was 79.73 MiB
git remote add origin https://github.com/pieroviano/Xamarin.Forms.git
git fetch origin                          # old history lands in refs/remotes/origin/* - expected, ignore
```

filter-repo leaves `.git/filter-repo/commit-map` behind: an old-SHA → new-SHA table for all 5883
commits, with `0000…` in the new column for the commits it dropped. §7 uses it.

---

## 6. Verification

### 6.1 The tips must be unchanged, byte for byte

The strongest available check, and cheap: no purged path is live, so the rewrite must not have
altered the content of either tip.

```powershell
git rev-parse '5.0.0^{tree}' 'gtk4^{tree}'
# MUST equal the values captured in §1.5:
#   561f3ee37cf867debd4f80d9b1314d4433d28098   (5.0.0)
#   fea15685fd8fcf4a3dd7f70f6e5656b85a70b68c   (gtk4)
```

A mismatch means the path list caught something live. Stop and roll back (§10) — do not try to
repair it forward.

### 6.2 No purged path may appear anywhere in history

```bash
cd /d/CommonLibrary/Xamarin.Forms
OUT=/d/CommonLibrary/_backup-xf-prepurge
git log --all --pretty=format: --name-only | grep -v '^$' | sort -u > $OUT/post-paths.txt
comm -12 $OUT/purge-paths.txt $OUT/post-paths.txt        # expect no output
```

And confirm the blobs are actually unreachable, not merely unreferenced by name:

```bash
git rev-list --objects --all | wc -l                     # was 71181
```

### 6.3 Shape of the resulting history

```powershell
git rev-list --all --count                          # < 5883; the difference = commits dropped as empty
git log --oneline -8 gtk4
git log --all --oneline | Select-String 'Prune files unused'   # expect NO match - cc8874b9b is gone
git rev-list --count "$(git rev-parse HEAD)" 2>$null
```

Confirm the fork's 56 commits survive minus whichever went empty, and that `gtk4` is still exactly
one commit ahead of `5.0.0`:

```powershell
git rev-list --left-right --count 5.0.0...gtk4      # expect 0  1
```

### 6.4 The tree must still build and test

Non-negotiable, because §6.1 proves the *tip* is intact but not that no build input was reachable
only through history (nothing should be — this catches the case where it was).

```powershell
git clean -xfd -e Packages -e bin-wsl -e obj-wsl    # NOTE: check what this would delete first
dotnet build Xamarin.Forms.Gtk.sln
dotnet test Xamarin.Forms.Core.UnitTests            # 4848
dotnet test Xamarin.Forms.Xaml.UnitTests            # 1046
dotnet test Xamarin.Forms.Platform.GTK.UnitTests    # 169, not green on gtk4 - compare to the
                                                    # pre-rewrite failure list, not to zero
```

`Packages` is a junction onto `d:\Starb\Packages` (the GtkSharp drop folder) and must survive — run
`git clean -xfdn` first and read the output before dropping the `-n`. `Xamarin.Forms.Build.Tasks`
must exist before anything consuming XAML builds; building the solution handles that ordering.

---

## 7. Repair the fallout — required, not cosmetic

Decision 4 deletes `cc8874b9b` outright, and decision 2 renumbers everything else. Three tracked
files name that commit:

- `.github/workflows/dotnet-format-daily.yml:24` — *"Xamarin.Forms.sln was deleted by the prune in cc8874b9b"*
- `.github/workflows/linux-gtk.yml:4` — *"…in cc8874b9b, so it is the only one with CI here"*
- `CLAUDE.md:12` — *"Commit `cc8874b9b` removed the Android, iOS, UAP, WPF, Tizen, MacOS and DualScreen projects…"*

These must stop citing a SHA that no longer exists — describe the prune without one, since after the
rewrite there is no commit to point at. `Version.targets:19` also carries `2ceec4836` inside an
example error string; that commit survives under a new SHA, so either translate it via the
commit-map or generalise the example.

Translate any other old SHA you care about:

```bash
grep -i '^<old-sha>' .git/filter-repo/commit-map          # new SHA, or 0000… if the commit was dropped
```

Also check `docs/plans/gtk4-migration.md` and `.claude/` notes for SHA references before considering
this step done (none were found at the time of writing). Land the edits as an ordinary commit on
each branch, after the rewrite — doing them beforehand does not help, because the SHAs would be
rewritten along with everything else.

**Versioning:** `GitInfo` derives `PackageVersion` from the commit count and branch. Dropping empty
commits lowers that count, so package versions computed after the rewrite will be *lower* than ones
already published from this branch. `AssemblyVersion` is pinned at `2.0.0.0` and is unaffected. If
anything was published from `Packages/`, bump `GitInfo.txt` rather than letting the version go
backwards.

---

## 8. Publishing — documented, deliberately not executed

Per decision 5, stop here and verify at leisure. When ready:

```powershell
git ls-remote origin                                # re-check §1.4 first
git push --force origin 5.0.0 gtk4
```

Afterwards, verify from a **fresh clone** into a scratch directory — not from this repo — that the
tips build and that the purged paths are absent from its history.

Two things to know before pushing:

1. **GitHub keeps unreachable objects.** Force-pushing does not delete the old commits from
   `pieroviano/Xamarin.Forms`; they stay reachable by direct SHA URL, and via any fork or cached
   view, until GitHub garbage-collects. If the removal must be complete on the remote, ask GitHub
   Support to run `gc` on the repository after the push — or delete and recreate the repository.
2. **Every clone breaks.** Anyone (including the WSL clone from §1.1, and CI caches) must re-clone.
   There is no pull that recovers from this.

---

## 9. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Path list over-broad — takes a live file | High | §6.1 tree-hash invariant catches it deterministically; §2.2 prefix check catches the shadowing case in advance; §4 mandates reading the list |
| Path list generated against stale tips | High | §1.5 and §4 both require regenerating against the current tips |
| `refs/replace/*` left behind → no space reclaimed, old commits still resolvable | Medium | `--replace-refs delete-no-add` in §5, verified by the object count in §6.2 |
| BOM/quoting corrupts the list, entries silently don't match | Medium | Generate in git bash (§4); `grep -c '^"'` check; §6.2 would surface the survivors |
| A second clone exists and later re-introduces the old history | Medium | §1.1 blocking check |
| Dropping `cc8874b9b` erases the only in-history record of the prune | Low, certain | §7 rewrites the three prose references to stand on their own |
| Package versions regress after empty-commit pruning | Low | §7; bump `GitInfo.txt` if anything was published |
| `git clean` in §6.4 removes the `Packages` junction | Low | Dry-run `-xfdn` first, exclude `Packages` |
| Comparability with `xamarin/Xamarin.Forms` is lost permanently | Accepted | Inherent to decision 2; the bundle in §3 preserves the correspondence offline |

---

## 10. Rollback

Valid until §8 is executed. Fastest first:

```powershell
# 1. Restore the .git copy (§3) - exact, instant, restores refs and reflogs
Remove-Item -Recurse -Force d:\CommonLibrary\Xamarin.Forms\.git
Copy-Item -Recurse d:\CommonLibrary\_backup-xf-prepurge\dot-git-copy d:\CommonLibrary\Xamarin.Forms\.git
cd d:\CommonLibrary\Xamarin.Forms
git reset --hard gtk4
git rev-parse '5.0.0^{tree}' 'gtk4^{tree}'          # must match §1.5

# 2. Or from the bundle, into a fresh directory
git clone d:\CommonLibrary\_backup-xf-prepurge\xamarin-forms-prepurge.bundle Xamarin.Forms.restored

# 3. Or from origin, which still holds the old history until §8 runs
git clone https://github.com/pieroviano/Xamarin.Forms.git
```

After §8 has run, only routes 1 and 2 remain.
