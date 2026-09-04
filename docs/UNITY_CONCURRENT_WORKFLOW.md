# Playtesting While Agents Develop

Goal: keep Unity open and playtest continuously while background agents commit and
merge, **without** `SampleScene.unity` churning or corrupting under you.

This complements [GIT_WORKTREE_WORKFLOW.md](GIT_WORKTREE_WORKFLOW.md).

## One-time setup (per clone / per worktree root)

```powershell
pwsh tools/git/setup.ps1
```

(bash: `bash tools/git/setup.sh`). This wires up:

| Setting | Effect |
| --- | --- |
| `core.hooksPath = tools/git/hooks` | Pre-commit guard reverts cosmetic-only scene churn ([scene_churn_guard.py](../tools/git/scene_churn_guard.py)). Bypass with `git commit --no-verify`. |
| `merge.unityyamlmerge` | Unity SmartMerge does a *semantic* 3-way merge of `*.unity` / `*.prefab` / `*.asset` (wired in [.gitattributes](../.gitattributes)). Most scene merges stop conflicting. |
| `rerere.enabled` | Git remembers how you resolved a conflict and replays it. |

## Turn off Unity auto-refresh in your playtest editor

This is the single change that makes "pull while Unity is running" safe. With
auto-refresh on, a `git pull` triggers an immediate reimport, and if Unity
auto-saves or you enter Play mode during that window it writes half-resolved asset
references back into the scene.

**Edit ▸ Preferences ▸ Asset Pipeline**

- **Auto Refresh** → *disabled*
- **Directory Monitoring** → leave on (cheap; only affects detection, not timing)

**Edit ▸ Preferences ▸ General**

- **Script Changes While Playing** → *Recompile After Finished Playing*

Now Unity only reimports when you explicitly press **Ctrl+R** (or focus the editor
with "Refresh on focus", which you can also disable). Pull whenever you want; press
Ctrl+R when you're at a safe point (not mid-playtest, not entering Play mode).

## Recommended layout

```
FungusToast/                      <- main checkout. Agents never open Unity here.
FungusToast.worktrees/
  agent-*/                        <- one per agent task/branch
  playtest/                       <- YOUR Unity session lives here
```

Create the playtest worktree once:

```powershell
git worktree add ..\FungusToast.worktrees\playtest -b playtest origin/main
cd ..\FungusToast.worktrees\playtest
pwsh tools/git/setup.ps1
.\scripts\open_unity_project.ps1
```

You control when `playtest` moves:

```powershell
# when you want the latest agent work, at a calm moment:
git fetch origin
git merge --no-edit origin/main      # SmartMerge handles scene/prefab
# then Ctrl+R in Unity
```

Because Unity is pinned to this folder and auto-refresh is off, agent commits on
other branches cannot perturb your running session.

## If the scene still shows spurious changes

```powershell
git checkout -- FungusToast.Unity/Assets/Scenes/SampleScene.unity
```

Safe whenever the diff is only:

- `m_AnchoredPosition` / `m_SizeDelta` / `m_LocalPosition` sub-pixel floats
- `m_Value:` on a scrollbar/slider (persisted runtime UI state)
- `m_EditorClassIdentifier:` trailing whitespace
- `{fileID: 0}` on `...Tile` fields that have a real GUID in `HEAD` (transient
  import miss — it comes back on a full reimport)

The pre-commit guard already refuses to commit a scene whose *entire* diff is one
of the above, so in practice you rarely need to do this by hand.

## For coding agents (no Unity installed)

An agent *may* edit `SampleScene.unity` when a task genuinely needs it — adding a
component, a GameObject, or a GUID reference is safe to do in YAML without Unity.
The pre-commit guard only reverts a scene whose **entire** diff is cosmetic drift,
so real edits pass straight through.

Rules:

- Never `git add -A` / `git add .`. Stage files by path.
- An agent may edit `FungusToast.Unity/Assets/Scenes/SampleScene.unity` when a
  task genuinely requires a new component, GameObject, or GUID reference; these
  changes can be made directly in YAML without Unity. Prefer self-wiring code
  (`GetComponent`, `FindObjectByType`, ScriptableObjects, UI built in code) over
  adding serialized fields that require Inspector assignment. This is the churn-safe
  minimum; the deliberate direction and the opportunistic-migration policy are in
  [`FungusToast.Core/docs/UNITY_CODE_FIRST_MIGRATION.md`](../FungusToast.Core/docs/UNITY_CODE_FIRST_MIGRATION.md).
  A wiring migration should shrink the scene/prefab diff over time, not add to it.
- Never stage `*.unity` / `*.prefab` for a task that is not about that
  scene/prefab. If one is modified and the task did not intend it, run
  `git checkout -- <file>` before committing.
- Keep any scene diff to exactly the lines the task requires.
- After committing a scene change, run `git show --stat HEAD`. If the scene is
  missing, the guard classified the diff as cosmetic — re-stage and
  `git commit --no-verify`. Do not routinely pass `--no-verify`; a reverted
  all-cosmetic scene is the guard working as intended.
- On a merge conflict in a `.unity` / `.prefab` the branch didn't deliberately
  change: `git checkout --theirs -- <file> && git add <file>`.

## What was done to shrink the problem

- `GridFiller` no longer bakes its 100×100 editor preview into the scene
  (`SampleScene.unity` dropped ~100k lines). The preview is now opt-in and
  non-persistent: **Fungus Toast ▸ Grid ▸ Toggle Edit-Mode Board Preview**.
- The mold-variant tile references on `GridVisualizer` were restored to their
  last-good state.
- `Assets/_Recovery/` (Unity crash-dump scenes, ~40 MB) is untracked and ignored.
