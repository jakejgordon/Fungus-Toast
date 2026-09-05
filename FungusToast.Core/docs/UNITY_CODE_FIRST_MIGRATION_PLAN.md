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

## 10. Adversarial Review — 2026-09-05 (Pending Planner Response)

> **Review status:** Challenge, not accepted plan revision. This section was
> added without changing the planner's original text so the planner can answer,
> refute, or incorporate each point explicitly. Findings are ordered by risk.

### Findings and concerns

#### AR-1 — High: the stated completion gate cannot be reached within the stated scope

- **Plan locations:** section 4's exclusions and lines 126–132 exclude the grid,
  cameras, `GrowthPhaseRunner`, and `DecayPhaseRunner`; section 8 nevertheless
  requires `GameManager.cs` and `GameUIManager.cs` to retain no scene-object
  cross-references.
- **Concrete evidence:** `GameManager.cs:312-324` still holds scene references to
  `GridVisualizer`, `CameraCenterer`, `MutationManager`, both phase runners,
  `GameUIManager`, phase-progress UI, draft UI, hotseat UI, and selection-prompt
  objects. `GameManager.cs:390` also holds `MagnifyingGlassFollowMouse`.
- **Trigger:** every listed UI row is completed exactly as proposed while the
  explicit non-goals remain untouched.
- **Impact:** the initiative can never truthfully satisfy its own completion
  criteria. A later implementer must either silently expand scope into excluded
  systems or mark the plan complete with known serialized scene wiring left in
  `GameManager`.
- **Required correction:** either limit the gate to an explicit, enumerated set
  of UI-owned fields, or expand the inventory and risk plan to cover the full
  composition root. Do not use the unqualified phrase "no remaining
  scene-object cross-references" while excluding some of those references.

#### AR-2 — High: the plan prescribes a global lookup mechanism before defining ownership and lifecycle

- **Plan location:** lines 165–170 direct later slices to extend or generalize
  `MainMenuRegistry`, while lines 200–207 present the choice as still open.
- **Concrete evidence:** `MainMenuRegistry.cs:17-39` is a static cache whose
  fallback is a scene-wide `FindAnyObjectByType(..., FindObjectsInactive.Include)`.
  It was built specifically for three main-menu controllers. By contrast,
  `GameManager.cs:611-693` already acts as the composition root for gameplay
  services, and `GameUIManager.cs:120-216` is the documented gameplay UI façade.
- **Trigger:** the suggested guidance is followed for in-game panels with
  different activation, teardown, and re-entry lifecycles from the menu.
- **Impact:** hidden dependencies, order-dependent scene scans, ambiguous
  ownership, and static references spanning play/scene lifetimes become the new
  default architecture. Generalizing the class name does not fix those
  properties; multiple narrow registries merely multiply them.
- **Required correction:** settle the lifetime and composition boundary before
  the first slice that truly needs cross-panel access. Treat a static registry
  as a temporary discovery bridge, not the target architecture. Prefer explicit
  construction and `SetDependencies(...)` from the existing composition root,
  with `GameUIManager` exposing only the façade operations consumers need.

#### AR-3 — High: the verification gate checks presence, not behavioral parity

- **Plan locations:** lines 80–82 and 171–175 make `ValidateBuiltUi()` plus a
  manual playtest the only verification model.
- **Concrete evidence:** the Phase 0 validators such as
  `UI_StartGamePanel.cs:229-236` only emit `Debug.LogError` when selected fields
  are null. They do not fail a test, validate callback wiring, prove sibling
  order/layout, or exercise repeated show/hide and teardown. The canonical
  policy is stronger: `UNITY_CODE_FIRST_MIGRATION.md:116-119` calls for a
  play-mode smoke path or edit-mode test that boots the bootstrapper.
- **Trigger:** a control is present but invokes the wrong callback, a listener is
  duplicated on re-entry, an inactive object misses initialization, or code-built
  layout differs at a supported resolution.
- **Impact:** a slice can pass its documented gate while shipping a broken or
  subtly changed flow—the exact regression class that compatibility-first
  migration is supposed to prevent.
- **Required correction:** define a per-slice parity contract before migration:
  required controls, callback routes, initial/terminal states, repeated-entry
  behavior, and supported-resolution checks. A completion record must include
  the exact manual Editor flow and result. Before Medium/Large cohorts, add an
  executable Unity smoke/edit-mode validation path if the project is willing to
  establish that infrastructure; a log-only null scan is not an assertion.

#### AR-4 — Medium: the inventory signals do not measure the work the policy says to do

- **Plan locations:** lines 94–124 use raw `[SerializeField]` counts and
  `new GameObject` counts to estimate migration scope and readiness.
- **Concrete evidence:** the Tooltip row's 16 occurrences are mostly authored
  content/configuration and prefab-local fields (`TooltipTrigger.cs:16-26`,
  `TooltipView.cs:13-20`), which sections 4 and 6 say should stay serialized.
  The Loading Screen's three fields comprise one tunable and two optional
  self-wired child components (`UI_LoadingScreen.cs:23-44`); its actual external
  wiring is the separate `GameUIManager.cs:35-36` reference. The Pause Menu is
  already created and injected by `PauseMenuService`
  (`EndgameService.cs:369-390`) and has no serialized fields.
- **Trigger:** phase order or effort is selected from these aggregate counts.
- **Impact:** no-op systems appear to be migration work, legitimate serialized
  fields inflate risk, and many `new GameObject` calls can be misread as
  simplicity even though they may indicate more layout surface to revalidate.
- **Required correction:** replace the proxy columns with a field-level ledger:
  field, category, current owner, target owner/resolution path, affected YAML,
  lifecycle, validation, and disposition. Count only confirmed cross-references
  when sizing wiring work. Reclassify Pause Menu as already complete and Tooltip
  as an audit unless a real external reference is found.

#### AR-5 — Medium: the inventory is not complete enough to support the objective or completion claim

- **Plan locations:** section 2 targets the rest of the Unity front end, and
  section 8 defines completion solely in terms of systems listed in section 5.
- **Concrete evidence:** serialized UI files not classified in the candidate or
  excluded tables include `CellTooltipUI.cs`, `MultiCellSelectionController.cs`,
  `MultiTileSelectionController.cs`, `TileSelectionController.cs`,
  `MycovariantTooltipPanel.cs`, `MycovariantIcon.cs`,
  `MagnifyingGlassFollowMouse.cs`, and `UiSpriteLibrary.cs`. `GameManager`'s
  selection-prompt references (`GameManager.cs:321-324`) are also absent.
- **Trigger:** all listed rows are moved to done and the document is retired.
- **Impact:** unreviewed Inspector wiring can survive while the initiative claims
  full front-end coverage.
- **Required correction:** classify every serialized Unity UI field/file as
  migrate, retain, already code-first, or separately owned before calling the
  inventory complete. Explicit retention is acceptable; omission is not.

#### AR-6 — Medium: system-sized "phases" conflict with the opportunistic slice boundary

- **Plan locations:** the policy at lines 54–58 says to migrate only the system
  already being touched, but section 7 is titled "Doing a Phase" and line 176
  says to move an entire system row to done. Several rows combine multiple
  controllers, prefabs, and manager fields.
- **Trigger:** a small bug touches one component in a multi-file row.
- **Impact:** the tracking model pressures the implementer either to broaden an
  unrelated change or to overstate an entire row as complete. It also makes
  phase ordering look like a gate despite the disclaimer.
- **Required correction:** keep ordering only as risk/readiness cohorts. Track
  completion per component/prefab slice, and derive each system's status from
  those slices.

### Counter-proposal

1. **Run a classification pass before Phase 1.** Build the field-level ledger
   described in AR-4 across all serialized UI files and the UI-owned fields in
   `GameManager`/`GameUIManager`. This is an inventory correction, not a wiring
   migration, and should produce a finite list of actual cross-references.
2. **Adopt one explicit gameplay UI composition boundary.** Keep
   `MainMenuRegistry` narrowly scoped as legacy menu discovery. Use
   `GameManager.BootstrapServices()` (or a deliberately extracted
   `GameUiCompositionRoot`) to construct/register gameplay UI and inject
   dependencies. Use `GameUIManager` as the façade where it already owns the
   lifetime; do not add a general static service locator or per-feature static
   registries.
3. **Replace numbered execution phases with readiness cohorts:**
   - **Audit/close:** Pause Menu (already code-built), Tooltip prefabs/triggers
     (retain prefab-local/config fields unless an external reference is proven).
   - **Pilot:** Loading Screen's external owner reference, as the smallest real
     inactive-overlay lifecycle case.
   - **Pattern proof:** Game Log, specifically because two instances must be
     configured without global ambiguity.
   - **Medium risk:** End Game, sidebar/summary/profile, phase HUD, draft, and
     hotseat—one component at a time when touched.
   - **High risk:** Mutation Tree only after the composition and validation
     patterns have survived at least one repeated-entry and two-instance system.
4. **Require a slice contract before editing:** exact serialized references to
   remove, construction owner, initialization order, inactive-object behavior,
   teardown/re-entry behavior, YAML files expected to shrink, and parity checks.
   If that contract reveals cross-system rewiring, defer it as the canonical
   policy requires.
5. **Use evidence-bearing completion records.** For each slice, record Core/
   Simulation sanity builds where applicable, Unity compile result, exact Editor
   flow, repeated-entry check, supported-resolution visual check, and the scene/
   prefab diff reviewed. A self-check may supplement this evidence but cannot
   replace it.
6. **Split the finish line.** Milestone A is "all inventoried UI-owned
   cross-references resolved or explicitly retained." Milestone B is the thin
   bootstrap scene from the policy doc. If non-UI `GameManager` references stay
   out of scope, say so and do not make their removal a Milestone A gate.

### Questions for the original planner to answer or refute

1. Which exact Phase 1 dependency requires any registry, given that Pause Menu
   is already injected and the Tooltip/Loading candidates can self-wire or be
   injected by their existing owner?
2. Is the goal removal of UI cross-references, full scene composition, or both?
   If both, why are the non-UI `GameManager` references excluded while the
   completion gate requires their removal?
3. What evidence makes `new GameObject` count positively correlated with lower
   migration effort or risk in this codebase?
4. What concrete parity failures must make a slice fail, beyond a subset of
   constructed fields being non-null?
5. Which omitted serialized UI files were deliberately retained, and under
   which category?
