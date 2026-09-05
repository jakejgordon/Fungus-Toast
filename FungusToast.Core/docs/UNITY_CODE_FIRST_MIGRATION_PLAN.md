# Unity Code-First Migration Plan

> **Related Documentation**: This is the execution plan and tracking doc for
> the initiative defined in
> [UNITY_CODE_FIRST_MIGRATION.md](UNITY_CODE_FIRST_MIGRATION.md) — that doc is
> the canonical *policy* (why, what migrates vs. stays, target patterns,
> guardrails); this doc is the *inventory, roadmap, and decision log* for
> applying it beyond the main menu. For Unity UI service/construction recipes,
> see [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md). For scene/prefab
> staging and the churn guard, see
> [../../docs/UNITY_CONCURRENT_WORKFLOW.md](../../docs/UNITY_CONCURRENT_WORKFLOW.md).

## 1. Status

- **State:** Planning complete for scope and ordering; no phase past the main
  menu has started.
- **Completed:** Home, Campaign, Solo Game, and Settings screens (Phase 0
  below) — see `UNITY_CODE_FIRST_MIGRATION.md` for what shipped there.
- **Migration posture:** Same as the policy doc — incremental, opportunistic,
  compatibility-first. No big-bang rewrite, no deadline. This plan exists to
  give the opportunistic work a *destination* and an *order*, not to schedule
  a dedicated project.
- **How to use this doc:** When you're about to touch a UI system anyway (a
  bug fix, a feature ask), check the table in section 5 for that system's
  phase and current state, do the slice per section 8, and update this file's
  status/decision log before you're done.

## 2. Objective

Extend the code-first approach from the main menu to the rest of the Unity
front end — the in-game HUD, overlays, and pooled UI — so an AI-assisted
session can read, modify, and verify any part of the game's UI code with the
same confidence it now has for the menus, instead of that confidence being
scoped to four screens.

This is explicitly about **wiring**, not about eliminating the Inspector.
Section 4 draws that line precisely, because getting it wrong in either
direction wastes effort: converting genuine designer-tunable values into code
constants doesn't help AI-assisted development, and leaving scene-authored
cross-references in place doesn't either.

## 3. Why a Separate Plan Doc

`UNITY_CODE_FIRST_MIGRATION.md` is meant to stay stable — it's the rules any
slice, anywhere in the codebase, should follow. This document's inventory and
phase status will change every time a slice lands, which is exactly the kind
of churn a rules doc shouldn't carry (see
[README.md](README.md) section 5's distinction between canonical docs and
tracking docs). Keep new findings, scope changes, and phase completions here;
keep the *policy* changes (if any are ever needed) in the other file.

## 4. Guardrails and Non-Goals

- **Follow the opportunistic-refactor policy exactly as written** — migrate
  the wiring in whatever system you're already touching, per
  `UNITY_CODE_FIRST_MIGRATION.md` section 3. This plan's phase ordering is a
  *preference* for when several things are equally in scope, not a gate that
  blocks fixing something out of order.
- **Do not combine a wiring migration with a gameplay or visual behavior
  change** in the same slice, unless the behavior change is explicitly called
  out and tested separately (same rule as the AI overhaul initiative).
- **Not every `[SerializeField]` is a target.** Three categories are
  explicitly out of scope for *this* initiative:
  1. **Designer-tunable numeric knobs** (camera speed/sensitivity, animation
     timings, easing curves). These aren't wiring; forcing them into code
     constants trades an Inspector slider for a recompile with no
     AI-assisted-development benefit. Leave them, or migrate them to the
     appropriate balance/constants file only if the "no magic constants" rule
     already calls for it independent of this initiative.
  2. **Genuine asset references** — sprites, materials, audio clips, fonts,
     prefab templates. These stay serialized per the policy doc's own table.
  3. **Prefab-internal wiring** for a prefab used purely as a clone template
     (mutation nodes, log entries, tooltip panels, draft cards, player icon
     cells — see section 5's Prefabs row). A `Button.targetGraphic` pointing
     at a sibling `Image` *within the same prefab* is not a cross-scene
     reference; it's normal prefab authoring. The problem pattern is a
     reference that reaches *out* of the prefab into scene-specific objects.
- **A migration slice should shrink the scene/prefab diff over time, not grow
  it** (unchanged from the policy doc).
- **No Unity test framework exists.** Verification is a runtime self-check
  (matching the `ValidateBuiltUi()` pattern already used on the menu panels)
  plus an actual Editor playtest of the affected flow.
- **Non-goals for this initiative specifically:**
  - `GridVisualizer.cs` and the board/tilemap rendering system. It's already
    ~99% code-driven (2 serialized fields across ~2,900 lines) and is a huge,
    algorithmically dense file. Any cleanup there is a code-quality/size
    concern, not a wiring migration — track it separately if it ever becomes
    a priority.
  - Camera rigs, render pipeline assets, input action assets, project
    settings. Pure Unity-editor territory; not in scope.
  - ScriptableObject data assets that are rarely hand-edited and hold no
    asset references (see section 6).

## 5. Current State Inventory

Signal columns: **SF** = files with `[SerializeField]` / total `.cs` files in
that folder. **`new GameObject`** = a rough proxy for how much of the system
already builds its own content at runtime (higher usually means less work
left). Effort is a rough sizing, not a commitment.

### Already code-first (verified during the menu migration or this survey)

| System | Files | SF density | Notes |
|---|---|---|---|
| Home / Campaign / Solo Game / Settings | `UI/Campaign/UI_ModeSelectPanelController.cs`, `UI/Campaign/UI_CampaignPanelController.cs`, `UI/GameStart/UI_StartGamePanel.cs` | 0 serialized cross-references (2 documented exceptions) | Phase 0, complete. Model implementation for every later phase. |
| Services layer | `Services/*.cs` (8 files) | 0/8 | Already pure C#, constructed in code from `GameManager.BootstrapServices()`. No work needed. |
| Campaign/state logic (non-UI) | `Campaign/CampaignController.cs`, `CampaignState.cs`, `CampaignSaveService.cs`, `MoldinessProgression.cs`, `MoldinessUnlocks.cs`, `GameMode.cs` | 1/9 | Already plain C#; `BoardPreset.cs`'s 2 fields are legitimate ScriptableObject data. No work needed. |
| Grid/board rendering | `Grid/GridVisualizer.cs` (2,940 lines) + 15 more | 2/16 | Already code-driven internally. Explicitly out of scope (section 4). |

### Not started — candidate phases, in suggested order

| Phase | System | Key files | SF density | `new GameObject` signal | Effort | Why this order |
|---|---|---|---|---|---|---|
| 1 | Tooltip system | `UI/Tooltips/*.cs` (11 files) | 2/11, 16 occurrences (`TooltipTrigger.cs` 10, `TooltipView.cs` 6) | 0 in `TooltipView.cs` | Small–Medium | Foundational: every other screen depends on it. `TooltipView` is a prefab component (`Prefabs/UI/UI_TooltipPrefab.prefab`, `UI_ToolTipHelpIconPrefab.prefab`) — likely stays prefab-authored per section 4; audit for any reach-outside-the-prefab references first. |
| 1 | Pause Menu | `UI/UI_PauseMenuPanel.cs` (997 lines) | 0 serialized fields already | 1 (low — investigate why) | Small | Zero `[SerializeField]` already; verify it isn't secretly relying on `FindObjectOfType`/scene-name lookups that should become a registry entry instead. Likely mostly done — confirm and document. |
| 1 | Loading Screen | `UI/UI_LoadingScreen.cs` | 3 fields | — | Small | Small, self-contained, low risk — good warm-up slice. |
| 2 | Game Log (player + global) | `UI/GameLog/*.cs` (10 files), `Prefabs/UI/UI_GameLogEntry.prefab`, `UI_GameLogPanel.prefab` | 2/10, 18 occurrences | 6 in `UI_GameLogPanel.cs` | Medium | Well-scoped pooling pattern already partially dynamic; two log instances (player/global) sharing one implementation is a good test of the registry-vs-serialized-field question at moderate stakes. |
| 2 | End Game panel | `UI/UI_EndGamePanel.cs`, `UI/UI_GameEndPlayerResultsRow.cs`, matching prefabs | 13 + 8 fields | 59 in `UI_EndGamePanel.cs` | Medium | Already mostly code-built (59 `new GameObject` calls) with a handful of top-level anchors still serialized — same shape the menu panels were in before their migration. |
| 3 | Right Sidebar + Player Summary + Mold Profile Root | `UI/UI_RightSideBar.cs`, `UI/PlayerSummaryRow.cs`, `UI/UI_MoldProfileRoot.cs` | 4 + 6 + 22 fields | 10 in `UI_RightSideBar.cs` | Medium–Large | `UI_MoldProfileRoot.cs` (22 fields) is the single densest file outside the mutation tree — worth its own careful slice rather than folding into a bigger phase. |
| 3 | Phase Banner + Progress Tracker | `UI/UI_PhaseBanner.cs`, `UI/UI_PhaseProgressTracker.cs` | 1 + 4 fields | — | Small | Small HUD anchors, low risk, natural companion to the sidebar phase. |
| 4 | Mycovariant Draft | `UI/MycovariantDraft/*.cs` (7 files), `Prefabs/UI/UI_DraftChoiceCard.prefab`, `UI_PlayerIconCell.prefab` | 3/7, 17 occurrences | 38 in `MycovariantDraftController.cs` | Medium | Already heavily code-built; draft overlays are self-contained and don't leak into other systems, good isolation for a mid-size slice. |
| 4 | Hotseat Turn Prompt | `UI/Hotseat/UI_HotseatTurnPrompt.cs` | 1/1, 9 occurrences | — | Small | Single file, single responsibility. |
| 5 | Mutation Tree | `UI/MutationTree/*.cs` (15 files: `MutationNodeUI.cs` 18 fields, `UI_MutationManager.cs` 17, `MutationTreeBuilder.cs` 7, `UI_RemainingPointsPanel.cs`/`UI_MutationTreeToastPresenter.cs` 3 each) | 5/15, 48 occurrences | Present but modest (4 in `MutationNodeUI.cs`) | Large | The largest, most central, most gameplay-adjacent UI system. Do this once the pattern is proven on four smaller phases first — the blast radius of getting it wrong is the highest in the game. `Prefabs/UI/UI_MutationNode.prefab`, `UI_MutationRow.prefab`, `UI_MutationCategoryHeader.prefab`, `UI_MutationPlaceholder.prefab`, `UI_RootMutationButton.prefab`, `UI_GrowthPreviewCell.prefab` are its clone templates. |
| 6 | GameManager / GameUIManager wiring cleanup | `GameManager.cs` (25 fields), `UI/GameUIManager.cs` (17 fields) | — | — | Ongoing, not a discrete slice | These two are the "master glue" holding a serialized reference to nearly every system above. Each field gets removed *as a byproduct* of migrating its owning system (mirroring how `startGamePanel`/`modeSelectPanel` left `GameManager.cs` during Phase 0) rather than as its own phase. Track remaining count here as systems complete. |

### Explicitly out of scope (see section 4)

`Grid/*` (board rendering), `Cameras/*` (tunable rig parameters),
`Effects/MycovariantEffectResolver.cs`, `Events/GameUIEventSubscriber.cs`,
`Input/UnityInputAdapter.cs`, `Phases/GrowthPhaseRunner.cs` /
`DecayPhaseRunner.cs` — all either already code-first, or hold legitimate
tunable/asset fields rather than scene wiring.

## 6. What Should Stay Inspector-Driven

Restating the policy doc's table with this survey's specifics:

- **Art/sprite/material/tile/palette/audio-clip assignments** everywhere,
  including `GameUIManager`'s `pauseMenuButtonIcon` / `nextTrackButtonIcon` /
  `nextTrackMenuButtonIcon` and every `AudioClip` field on `GameManager.cs`.
- **Prefabs as clone templates.** All 18 prefabs under `Assets/Prefabs/UI/`
  stay prefab-authored visual shells (mutation nodes/rows/headers, log
  entries, tooltips, draft cards, player icon cells, testing options
  section). Migration work here is *auditing* them for stray cross-scene
  references, not rebuilding them from `new GameObject(...)` calls.
- **ScriptableObject config data**: the 16 Board Preset assets, campaign
  progression, and toast board configs under `Assets/Configs/`. These are
  rarely hand-edited and hold no problematic asset references — leave them.
- **Camera rig tunables** (`CameraControls.cs`'s 8 fields) and any similar
  designer-facing numeric knobs elsewhere.
- **Project-level configuration**: URP assets, input action assets,
  `ProjectSettings/*`, the Canvas/EventSystem setup itself.

## 7. Doing a Phase

Same procedure as `UNITY_CODE_FIRST_MIGRATION.md` section 7, applied per
system instead of per screen:

1. Read every serialized field in the target file(s); sort into
   cross-reference (migrate), asset reference (leave), tunable value (leave),
   prefab-internal wiring (leave).
2. Check whether the system already self-scaffolds (`Ensure*`/`Build*`
   methods) — most of the systems in section 5 do, at least partially. Close
   the remaining gaps rather than rewriting what already works.
3. For any cross-panel reference (this system needs to reach another
   already-migrated or about-to-be-migrated system), extend
   `MainMenuRegistry` — or, once enough in-game systems have migrated that
   "main menu" no longer describes it, rename/generalize the registry (see
   the open decision in section 9) rather than inventing a second registry
   pattern.
4. Add a `ValidateBuiltUi()`-style self-check.
5. Verify: build Core/Simulation for sanity, validate Unity compile health,
   and actually exercise the affected flow in the Editor (open the mutation
   tree, trigger the draft, end a game, etc.) — there is no automated Unity
   test suite to lean on.
6. Update this file: move the row from "not started" to "done" in section 5,
   note the actual field count removed from `GameManager.cs`/`GameUIManager.cs`
   if applicable, and log anything surprising in section 9.

## 8. Completion Criteria

- Every system in section 5's "not started" table has moved to "done" or has
  a documented reason it was reclassified as out of scope.
- `GameManager.cs` and `GameUIManager.cs` hold only genuine asset-reference
  and tunable fields — no remaining scene-object cross-references.
- The registry pattern introduced for the menu (`MainMenuRegistry`) has
  either been generalized to cover in-game panels too, or a documented
  decision explains why a different pattern was used instead.
- `UNITY_CODE_FIRST_MIGRATION.md` section 1's "Completed special case" note
  is updated to reflect full front-end coverage, and this plan doc can be
  marked complete (or retired) at that point.

## 9. Open Decisions / Decision Log

Record scope changes, surprises, and judgment calls here as phases land —
newest entries first.

- *(none yet — this plan has not started execution)*

**Open question to resolve before or during Phase 1:** should
`MainMenuRegistry` (see
`FungusToast.Unity/Assets/Scripts/Unity/UI/MainMenuRegistry.cs`) be reused/
renamed for in-game panel lookups, or should each phase introduce its own
narrowly-scoped registry (e.g., a `GameHudRegistry`)? Leaning toward
generalizing the existing one once Phase 1 needs cross-panel lookups, to
avoid a proliferation of near-identical registries — but this should be
decided with the first phase that actually needs it, not speculatively now.
