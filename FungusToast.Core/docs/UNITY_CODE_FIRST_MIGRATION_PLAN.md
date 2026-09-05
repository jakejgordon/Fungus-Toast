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

- **State:** Revised twice on 2026-09-05 after two adversarial review passes —
  six findings in section 10 (response in section 11) and three in section 12
  (response in section 13). All nine accepted. Execution then began the same
  day: the Audit/close (Tooltip) and Pilot (Loading Screen) cohorts are both
  done — pending your Editor playtest of Loading Screen (see section 9). Next
  up: Pattern proof (Game Log).
- **Completed:** Home, Campaign, Solo Game, and Settings screens (Phase 0) —
  see `UNITY_CODE_FIRST_MIGRATION.md` for what shipped there. Plus the Pause
  Menu, which the review established was already done before this plan
  existed, and the Tooltip and Loading Screen cohorts (2026-09-05).
- **Migration posture:** Same as the policy doc — incremental, opportunistic,
  compatibility-first. No big-bang rewrite, no deadline. This plan exists to
  give the opportunistic work a *destination* and an *order*, not to schedule
  a dedicated project.
- **How to use this doc:** When you're about to touch a UI system anyway (a
  bug fix, a feature ask), find it in section 5, write the slice contract from
  section 7.2, migrate only what you're touching, verify against section 7.3,
  and record the outcome in section 9 before you're done.
- **Two things to read first if you're starting a slice:** section 7.1 (the
  composition boundary — do *not* reach for a static registry) and section 7.3
  (why a null check is not a sufficient gate).

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
  `UNITY_CODE_FIRST_MIGRATION.md` section 3. The cohort ordering in section 5
  is a *readiness preference* for when several things are equally in scope,
  never a gate that blocks fixing something out of order.
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
     cells — see section 6). A `Button.targetGraphic` pointing
     at a sibling `Image` *within the same prefab* is not a cross-scene
     reference; it's normal prefab authoring. The problem pattern is a
     reference that reaches *out* of the prefab into scene-specific objects.
- **A migration slice should shrink the scene/prefab diff over time, not grow
  it** (unchanged from the policy doc).
- **The Unity Test Framework is installed, but there are no tests.**
  `FungusToast.Unity/Packages/manifest.json:9` declares
  `com.unity.test-framework` 1.7.0. What's missing is a test assembly — there
  is no `.asmdef` anywhere under `Assets/`, and `Assets/Scripts/Tests/` is an
  empty directory. So the cost of executable coverage is "add an asmdef and
  write the first tests," not "adopt a test framework." Until that happens
  verification is manual — and a null-scan self-check is *not* a sufficient
  gate. See section 7.3.
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
- **Consequence of those non-goals — state it plainly.** `GameManager.cs`
  holds serialized scene references to systems this initiative excludes:
  `gridVisualizer`, `cameraCenterer` (`GameManager.cs:312-313`),
  `growthPhaseRunner`, `decayPhaseRunner` (`:315,:317`), and
  `magnifyingGlass` (`:390`). Those stay. Therefore this initiative can
  **not** claim "`GameManager` has no serialized scene references" as a
  finish line, and section 8 does not. The gate is an enumerated list of
  UI-owned fields only. Anyone wanting full composition-root cleanup is
  proposing a *different, larger* initiative that should be scoped as such.

## 5. Current State Inventory

> **Metric warning — read before trusting any number below.** The first
> version of this table sized work using raw `[SerializeField]` counts and
> `new GameObject` counts. Both are bad proxies and the adversarial review in
> section 10 was right to reject them:
>
> - Raw field counts **measure mostly non-work.** `TooltipTrigger.cs`'s 10
>   fields are authored content and per-instance tunables
>   (`staticText`, `hoverDelay`, `maxWidth`, `placement`, `pinOnClick`…);
>   `TooltipView.cs`'s 6 are prefab-internal child references plus a fade
>   duration. Sections 4 and 6 say *all* of those stay. A row sized at "16
>   occurrences" was sizing approximately zero cross-references.
> - `new GameObject` count says nothing about difficulty in either direction.
>   A high count can mean "already self-building" *or* "large layout surface
>   to revalidate." A low count can mean "still Inspector-wired" *or*
>   "constructed by someone else entirely" — which is exactly what a count of
>   1 meant for the Pause Menu.
>
> The only number that sizes this initiative is **confirmed cross-references**:
> a serialized field pointing at an object the owning component does not itself
> create or live inside. That number is not yet known per system. Treat the
> "Effort" column as a *risk/readiness ordering hint only* until the
> classification pass in section 5.1 replaces it with real counts.

### Already code-first (verified during the menu migration or this survey)

| System | Files | SF density | Notes |
|---|---|---|---|
| Home / Campaign / Solo Game / Settings | `UI/Campaign/UI_ModeSelectPanelController.cs`, `UI/Campaign/UI_CampaignPanelController.cs`, `UI/GameStart/UI_StartGamePanel.cs` | 0 serialized cross-references (2 documented exceptions) | Phase 0, complete. Model implementation for every later phase. |
| Services layer | `Services/*.cs` (8 files) | 0/8 | Already pure C#, constructed in code from `GameManager.BootstrapServices()`. No work needed. |
| Campaign/state logic (non-UI) | `Campaign/CampaignController.cs`, `CampaignState.cs`, `CampaignSaveService.cs`, `MoldinessProgression.cs`, `MoldinessUnlocks.cs`, `GameMode.cs` | 1/9 | Already plain C#; `BoardPreset.cs`'s 2 fields are legitimate ScriptableObject data. No work needed. |
| Grid/board rendering | `Grid/GridVisualizer.cs` (2,940 lines) + 15 more | 2/16 | Already code-driven internally. Explicitly out of scope (section 4). |
| **Pause Menu** | `UI/UI_PauseMenuPanel.cs` (997 lines) | 0 serialized fields | **Reclassified from "Phase 1 candidate" to done.** `PauseMenuService.Initialize()` (`Services/EndgameService.cs:369-390`) already does `AddComponent<UI_PauseMenuPanel>()`, `panel.SetDependencies(...)`, then `gameUIManager.RegisterPauseMenuPanel(panel)`. That is construct-inject-register from a composition root — a *better* pattern than the static registry Phase 0 used, and the reference model for section 7. |
| Tooltip system | `UI/Tooltips/*.cs` (11 files) | 0/11 confirmed | **Closed 2026-09-05, Audit/close cohort.** See decision log — every `dynamicProvider` occurrence is unassigned; all tooltips use `staticText`. Retained by category, no code change. |
| **Loading Screen** | `UI/UI_LoadingScreen.cs`, owner ref at `GameUIManager.cs` | 0 confirmed (was 1) | **Landed 2026-09-05, Pilot slice.** `GameUIManager.Awake()` resolves the singleton via `FindAnyObjectByType<UI_LoadingScreen>(FindObjectsInactive.Include)` and calls the new `RegisterLoadingScreen(...)`, mirroring `RegisterPauseMenuPanel`. The `[SerializeField]` and its scene wire (`SampleScene.unity` `GameUIManager` → `loadingScreen`) are gone. Panel content (`fadeOutDuration`, `statusLabel`, `backgroundImage`) stays scene-authored and serialized, per the pilot's own scope note. See section 9 for the full slice record. |

### Not started — readiness cohorts, not numbered gates

Cohorts express *risk and readiness ordering only*. They are not a schedule
and not a gate: the opportunistic policy still wins, so if a bug lands you in
the Mutation Tree tomorrow, migrate the wiring you touch there tomorrow.
Completion is tracked per component (section 7), not per row.

Audit/close (Tooltip) and Pilot (Loading Screen) are done — moved to the
"Already code-first" table above. What follows starts at Pattern proof.

| Cohort | System | Key files | Confirmed cross-references | Notes |
|---|---|---|---|---|
| **Pattern proof** | Game Log (player + global) | `UI/GameLog/*.cs` (10 files), `Prefabs/UI/UI_GameLogEntry.prefab`, `UI_GameLogPanel.prefab` | TBD — 4 owner refs at `GameUIManager.cs:28-33` (2 panels + 2 managers) plus panel-internal fields to classify | Deliberately second: **two instances of one implementation** is the case that breaks naive global lookup, so it proves or kills the composition approach before anything larger adopts it. |
| **Medium risk** | End Game panel | `UI/UI_EndGamePanel.cs`, `UI/UI_GameEndPlayerResultsRow.cs`, matching prefabs | TBD (21 fields to classify) | Already substantially self-building; expect most fields to classify as retained, but that must be confirmed field-by-field, not assumed from the count. |
| **Medium risk** | Right Sidebar / Player Summary / Mold Profile | `UI/UI_RightSideBar.cs`, `UI/PlayerSummaryRow.cs`, `UI/UI_MoldProfileRoot.cs` | TBD (32 fields to classify) | `UI_MoldProfileRoot.cs` is the densest non-mutation-tree file; treat each of the three as its own slice. |
| **Medium risk** | Phase Banner + Progress Tracker | `UI/UI_PhaseBanner.cs`, `UI/UI_PhaseProgressTracker.cs` | TBD (5 fields) | Also referenced from `GameManager.cs:318`. |
| **Medium risk** | Mycovariant Draft | `UI/MycovariantDraft/*.cs` (7 files), `Prefabs/UI/UI_DraftChoiceCard.prefab`, `UI_PlayerIconCell.prefab` | TBD (17 fields) | Also referenced from `GameManager.cs:319`. Self-contained overlay; good isolation. |
| **Medium risk** | Hotseat Turn Prompt | `UI/Hotseat/UI_HotseatTurnPrompt.cs` | TBD (9 fields) | Also referenced from `GameManager.cs:320`. |
| **Medium risk** | Selection prompt + selection controllers | `GameManager.cs:321-324` (`SelectionPromptPanel`, `SelectionPromptText`, `selectionPromptCancelButton`, `selectionPromptCancelButtonText`), `UI/TileSelectionController.cs`, `UI/MultiTileSelectionController.cs`, `UI/MultiCellSelectionController.cs` | 4 confirmed in `GameManager` + TBD in the controllers | **Added after review (AR-5).** Missing from the first draft entirely. Already served by `SelectionPromptService` — check whether that service can own construction the way `PauseMenuService` does. |
| **Medium risk** | Cell / mycovariant tooltip panels | `UI/CellTooltipUI.cs` (24 fields — densest loose UI file in the project), `UI/MycovariantTooltipPanel.cs` (4), `UI/MycovariantDraft/MycovariantIcon.cs` (2) | TBD | **Added after review (AR-5).** Omitted from the first draft despite `CellTooltipUI` having the highest field count outside the mutation tree. |
| **High risk** | Mutation Tree | `UI/MutationTree/*.cs` (15 files; `MutationNodeUI.cs` 18 fields, `UI_MutationManager.cs` 17, `MutationTreeBuilder.cs` 7, two more at 3 each) | TBD (48 fields) | Largest and most gameplay-central. Do only after the composition and validation patterns have survived **both** a repeated-entry system and the two-instance Game Log case. Clone templates: `UI_MutationNode`, `UI_MutationRow`, `UI_MutationCategoryHeader`, `UI_MutationPlaceholder`, `UI_RootMutationButton`, `UI_GrowthPreviewCell`. |
| **Byproduct** | UI-owned fields in `GameManager` / `GameUIManager` | `GameManager.cs` (25 fields), `UI/GameUIManager.cs` (17 fields) | Enumerated in section 8's Milestone A | Not a standalone slice. Each field leaves as its owning system migrates, exactly as `startGamePanel`/`modeSelectPanel` left `GameManager` during Phase 0. |

### Explicitly out of scope (see section 4)

`Grid/*` (board rendering), `Cameras/*` (tunable rig parameters),
`Effects/MycovariantEffectResolver.cs`, `Events/GameUIEventSubscriber.cs`,
`Input/UnityInputAdapter.cs`, `Phases/GrowthPhaseRunner.cs` /
`DecayPhaseRunner.cs` — all either already code-first, or hold legitimate
tunable/asset fields rather than scene wiring. Also out of scope and
explicitly retained: `UI/MagnifyingGlassFollowMouse.cs` (5 fields) and the
`GameManager.cs:390` magnifier reference — a gameplay overlay, already
documented as the standing exception in `UI_StartGamePanel`; and
`UI/UiSpriteLibrary.cs` (asset loading by design).

### 5.1 Classification pass (do this before or alongside the pilot)

Every "TBD" above needs converting into a real number before its cohort's
effort can be trusted. The unit is a **field-level ledger row**:

| field | owning file | category (cross-ref / asset / tunable / prefab-internal) | current owner | target resolution | affected YAML | disposition |

Scope discipline: this is an *inventory correction*, not a migration, and it
must not become a project that blocks all work behind a complete audit. Do the
bounded version — classify a system's fields at the moment you pick that
system up, and record the rows here. The only part worth doing up front is
whatever is needed to make section 8's Milestone A list finite and honest.

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

## 7. Doing a Slice

The unit of work is a **component or prefab**, not a system row. A system row
is done when all its component slices are.

### 7.1 The composition boundary (decided — see AR-2 in section 10)

**Do not extend `MainMenuRegistry`, and do not add per-feature static
registries.** The static-registry-with-scene-scan-fallback pattern
(`MainMenuRegistry.cs:17-39`) was a Phase 0 expedient for three menu
controllers whose lifetimes span the whole session. Generalizing it would
make order-dependent scene scans and play-lifetime statics the default
architecture for panels with far more complex activation/teardown/re-entry
behavior.

The target pattern already exists in this codebase — `PauseMenuService`:

```
panel = hostObject.GetComponent<UI_PauseMenuPanel>()
        ?? hostObject.AddComponent<UI_PauseMenuPanel>();   // construct
panel.SetDependencies(gameUIManager, ...callbacks...);      // inject
gameUIManager?.RegisterPauseMenuPanel(panel);               // register w/ façade
```
(`Services/EndgameService.cs:369-390`)

So, in order of preference:
1. **Construct + inject from the existing composition root**
   (`GameManager.BootstrapServices()`, or a service it owns), passing
   dependencies explicitly via `SetDependencies(...)`.
2. **Register with `GameUIManager`** where it already owns the lifetime, and
   have consumers go through that façade.
3. **Static registry** — only as a temporary discovery bridge with a comment
   saying so, never as the destination.

`MainMenuRegistry` stays scoped to legacy menu discovery and should shrink,
not grow. If a menu panel's lookup can move to construct-inject during a slice
that touches it anyway, do that.

### 7.2 Slice contract — write this before editing

A slice does not start until these are written down (in the PR/commit body or
this doc):

- Exact serialized references to be removed, by file and field.
- Who constructs the object afterward, and in what initialization order.
- Behavior while the GameObject is **inactive** (the Phase 0 bug that broke
  Custom Game / Campaign was precisely this: `Awake` never runs on an inactive
  object, so registry self-registration never happened).
- Teardown and **repeated-entry** behavior — enter, leave, re-enter. Duplicate
  listeners and stale caches live here.
- Which scene/prefab YAML is expected to shrink.
- The parity checks that must pass (see 7.3).

If writing the contract reveals cross-system rewiring, defer it — that is the
canonical policy's own rule, not a new one.

### 7.3 Verification — parity, not presence

A `ValidateBuiltUi()` null scan is **not** a sufficient gate, and this
session's own history proves it: the "buttons do nothing" bug, the duplicate
`Back` button, the wrong logo asset, and the font-size regressions would each
have passed a null check cleanly. Every constructed field was non-null in all
four cases.

Minimum per-slice parity evidence:

- **Controls present** — the null scan. Necessary, not sufficient.
- **Callback routes** — each control invokes the intended handler exactly once.
  Verify listener count after a re-entry, not just on first build.
- **Initial and terminal state** — what's visible/interactable on open, and on
  close.
- **Repeated entry** — open → leave → reopen at least once.
- **Supported resolutions** — at least the narrowest and widest the flow
  supports, since code-built layout is where this most often diverges.
- **Scene/prefab diff reviewed** — confirm it shrank, per the policy doc.

Record the actual Editor flow performed and its result in the completion note.
"Playtested it" is not a completion record.

**Open choice, to settle before the Mutation Tree cohort:** the Unity Test
Framework is already installed (`Packages/manifest.json:9`,
`com.unity.test-framework` 1.7.0) — the only missing pieces are an EditMode or
PlayMode `.asmdef` and the first tests. Given that the framework cost is
already paid, adding a bootstrap/parity smoke suite is a small lift that would
make most of the checklist above executable instead of manual. Decide this
before the highest-risk cohort, not after it.

> **Resolved 2026-09-05 (R2-2).** The canonical policy previously described
> this gate as "asserts nothing critical is null," which meant the
> *authoritative* doc carried the weaker instruction while this tracker carried
> the stronger one. That has been fixed at the source rather than deferred:
> `UNITY_CODE_FIRST_MIGRATION.md` section 5 now requires parity evidence, and
> its section 7 checklist now includes the pre-edit slice contract and the
> parity/record step. The two docs no longer diverge.

### 7.4 Recording completion

Update this file: mark the component slice done, note which
`GameManager`/`GameUIManager` fields actually left, and log anything
surprising in section 9.

## 8. Completion Criteria

Split into two milestones, because the original single finish line was not
reachable within the stated scope (AR-1).

### Milestone A — UI-owned cross-references resolved

**Definition:** every confirmed cross-reference in the section 5 field ledgers
is either resolved via the section 7.1 composition pattern or **explicitly
retained** with a recorded category and reason.

That means Milestone A cannot be evaluated until section 5.1's classification
has actually been done for every in-scope row — the "TBD" entries are not
absent work, they are unmeasured work. Explicit retention is a valid outcome;
silent omission is not, and neither is leaving a row unclassified.

The manager-owned fields below are a **known baseline**, not the complete
enumeration. They are listed because they were confirmable up front; the
component-owned references inside Game Log, End Game, sidebar/profile, phase
HUD, draft, hotseat, selection, tooltip panels, and the Mutation Tree count
toward Milestone A on exactly equal footing once classified.

Known baseline — UI-owned manager fields:

- `GameUIManager.cs`: `mutationUIManager`, `playerUIBinder`, `leftSidebar`,
  `rightSidebar`, `moldProfileRoot`, `playerActivityLogPanel`,
  `playerActivityLogManager`, `globalEventsLogPanel`, `globalEventsLogManager`,
  `loadingScreen`, `endGamePanel`, `pauseMenuPanel`, `phaseBanner`,
  `phaseProgressTracker`. *(Not the three `Sprite` icon fields — retained as
  asset references.)*
- `GameManager.cs`: `mutationManager`, `gameUIManager`, `phaseProgressTracker`,
  `mycovariantDraftController`, `hotseatTurnPrompt`, `SelectionPromptPanel`,
  `SelectionPromptText`, `selectionPromptCancelButton`,
  `selectionPromptCancelButtonText`.

Resolving that baseline alone does **not** meet Milestone A. It is met only
when the baseline *and* every classified component-owned cross-reference are
resolved or explicitly retained, and no in-scope row in section 5 is still
carrying an unclassified "TBD."

**Explicitly excluded from Milestone A** (and therefore from any "no scene
references" claim): `gridVisualizer`, `cameraCenterer`, `growthPhaseRunner`,
`decayPhaseRunner`, `magnifyingGlass`, `campaignProgression`, and all
`AudioClip` fields on `GameManager`. These belong to systems section 4 puts
out of scope. Removing them would be a *different* initiative — full
composition-root cleanup — and should be scoped separately if ever wanted.

### Milestone B — thin bootstrap scene

The policy doc's end state: menu and HUD panels spawned at runtime, scene YAML
stripped of the dead authored hierarchy. Tracked today as the "Main Menu
Bootstrapper + Scene YAML Cleanup" entry in
[second-level/FUTURE_IMPROVEMENTS.md](second-level/FUTURE_IMPROVEMENTS.md);
expand that entry's scope to the whole front end once Milestone A lands.

Milestone A does **not** depend on Milestone B, and B should not gate A.

### Doc-completion

This document may be marked complete only when **both** hold:

1. Milestone A is met as defined above — including every in-scope component
   row, not just the manager baseline.
2. Every row in section 5's cohort table is marked done, or explicitly
   reclassified as retained/out-of-scope with a reason recorded in section 9.

Then update `UNITY_CODE_FIRST_MIGRATION.md` section 1 to describe the actual
coverage achieved — including what was explicitly retained — rather than
claiming blanket front-end coverage.

## 9. Open Decisions / Decision Log

Record scope changes, surprises, and judgment calls here as slices land —
newest entries first.

**2026-09-05 — Pilot (Loading Screen) landed. Awaiting Editor playtest.**
Slice contract: remove `GameUIManager.cs`'s `[SerializeField] private
UI_LoadingScreen loadingScreen;`; resolve it instead in `GameUIManager.Awake()`
via `FindAnyObjectByType<UI_LoadingScreen>(FindObjectsInactive.Include)`,
injected through a new `RegisterLoadingScreen(UI_LoadingScreen)` method
(mirrors `RegisterPauseMenuPanel`). Two alternatives were considered and
rejected: moving the `[SerializeField]` onto `GameManager` (would only
relocate the unresolved reference, not resolve it — `GameManager` already
carries 9 such fields in the Milestone A baseline); and self-registration from
`UI_LoadingScreen.Awake()` via `GameManager.Instance` (rejected as
Awake-order-dependent across objects — the exact failure class that broke
Custom Game/Campaign in Phase 0). `FindAnyObjectByType(...,
FindObjectsInactive.Include)` has no such dependency and is already the
established idiom in this codebase, including inside the reference
implementation itself (`UI_PauseMenuPanel.cs:248`). The panel's own 3 fields
(`fadeOutDuration`, `statusLabel`, `backgroundImage`) are untouched — the pilot
note in section 5 scoped the work to `GameUIManager` only, not a panel
rebuild. `GameManager.cs` required zero changes; its
`gameUIManager.LoadingScreen?.…` call sites are unaffected.
Scene diff: `SampleScene.unity`'s `GameUIManager` MonoBehaviour block lost
its `loadingScreen: {fileID: 1121346468}` line — the only change to that
file. `dotnet build FungusToast.Unity/Assembly-CSharp.csproj` succeeds (0
errors; pre-existing unrelated warnings only). **Not yet parity-verified per
section 7.3** — that requires an Editor playtest, which this session cannot
run. Editor flow needed: enter Play, confirm the loading overlay still shows
during game init and fades out on ready (Custom Game start), then restart/start
a second game in the same session and confirm it still shows/hides correctly
the second time (repeated-entry check). Record the actual result here once
run.

**2026-09-05 — Tooltip audit closed, zero cross-references.** Checked every
`dynamicProvider` occurrence in `SampleScene.unity` (13) and the two prefabs
that reference the field (`UI_GrowthPreviewCell.prefab`,
`UI_ToolTipHelpIconPrefab.prefab`, 1 each). All 15 are `{fileID: 0}` —
unassigned. Every `TooltipTrigger` instance in the project uses `staticText`
instead. Section 5's "confirm per instance" check is satisfied; the row is
retained by category with no code change. Pilot (Loading Screen) is next.

**2026-09-05 — Composition boundary decided (closes the prior open question).**
The original draft left "generalize `MainMenuRegistry` vs. per-feature
registries" open while simultaneously instructing slices to extend it. Both
options were wrong. Resolved in favor of construct-inject-register from the
existing composition root, per section 7.1, on the evidence that
`PauseMenuService` already implements exactly that and predates this plan.
`MainMenuRegistry` is now legacy-scoped and expected to shrink.

**2026-09-05 — Pause Menu reclassified from candidate to already-done.** It was
listed as a Phase 1 slice with a note to "investigate why" it had only one
`new GameObject` call. The answer was that a service constructs and injects it
externally. This was a direct consequence of surveying by aggregate counts
instead of reading the code, and it is why section 5's proxy metrics were
withdrawn.

**2026-09-05 — Proxy metrics withdrawn.** Raw `[SerializeField]` and
`new GameObject` counts were removed as sizing signals; only confirmed
cross-references count. See the metric warning in section 5.

**2026-09-05 — Inventory gaps closed.** `CellTooltipUI.cs` (24 fields — the
densest loose UI file in the project), the selection-prompt fields and their
three controllers, `MycovariantTooltipPanel.cs`, and `MycovariantIcon.cs` were
absent from the first draft's tables entirely. Added.
`MagnifyingGlassFollowMouse.cs` and `UiSpriteLibrary.cs` are now explicitly
retained rather than silently omitted.

## 10. Adversarial Review — 2026-09-05 (Planner Response Recorded in §11)

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

## 11. Planner Response — 2026-09-05

**Verdict: all six findings accepted.** Each was checked against the code
before responding, not accepted on assertion. Two carry small corrections to
the review's own reasoning, noted below. The plan body above has been revised;
this section records the disposition and answers the five questions.

### Disposition

| ID | Verdict | Verification | Where fixed |
|---|---|---|---|
| AR-1 | **Accepted** | Confirmed: `GameManager.cs:312-317` holds `gridVisualizer`, `cameraCenterer`, `growthPhaseRunner`, `decayPhaseRunner`; `:390` holds `magnifyingGlass` — all excluded by §4 while §8 demanded their removal. Genuine unreachable gate. | §4 (consequence stated), §8 (Milestone A enumerated, exclusions listed) |
| AR-2 | **Accepted** | Confirmed: `MainMenuRegistry.cs:17-39` is a static cache with a scene-wide `FindAnyObjectByType(..., Include)` fallback; `PauseMenuService` (`EndgameService.cs:369-390`) already does construct-inject-register. The draft prescribed extending the former in §7 while calling the question open in §9 — internally inconsistent. | §7.1 (decided), §9 (decision recorded) |
| AR-3 | **Accepted, with a correction** | Confirmed: `ValidateBuiltUi` (`UI_StartGamePanel.cs:229-236`) only logs on null. **Correction:** the review says the canonical policy is "stronger," but `UNITY_CODE_FIRST_MIGRATION.md:116-119` also specifies only "asserts nothing critical is null" — the policy is equally weak, not a standard the plan fell short of. The conclusion still holds on independent evidence (below). | §7.3 (parity contract), plus a flag to tighten the policy doc |
| AR-4 | **Accepted — most consequential finding** | Confirmed all three examples: `TooltipTrigger.cs:16-26` (content + tunables), `TooltipView.cs:13-20` (prefab-internal + fade), `UI_LoadingScreen.cs:23-44` (1 tunable + 2 optional, one self-wiring), and Pause Menu already injected. The metrics counted fields the plan's own guardrails exclude. | §5 metric warning, table rebuilt, §5.1 ledger |
| AR-5 | **Accepted** | True from the survey's own output: `CellTooltipUI.cs` has 24 serialized fields — the highest of any loose UI file — and was omitted from every table. Selection-prompt fields likewise. | §5 rows added, retentions made explicit |
| AR-6 | **Accepted** | Fair: §7 was titled "Doing a Phase" and instructed moving whole system rows to done, which pressures scope-widening against the opportunistic policy. | §5 (cohorts, not gates), §7 (slice = component) |

### On AR-3's evidence

The review's remedy is right, but the strongest argument for it isn't the
policy-doc comparison — it's this session's own bug history. Four regressions
shipped during the Phase 0 menu migration and were caught only by the user
looking at screenshots:

1. Custom Game / Campaign buttons did nothing (registry returned null for
   inactive panels — `Awake` never ran).
2. A duplicate `Back` button rendered (legacy scene object never destroyed).
3. The wrong logo asset was referenced (orphaned GUID from a deleted file).
4. Font sizes were wrong on two screens (never set; TMP default applied).

**Every one of those passes a null scan.** Every constructed field was
non-null in all four cases. That is the empirical case for a parity contract,
and it is more persuasive than any doc-wording argument.

### On the counter-proposal's item 1

Accepted **with a scope guard.** "Run a classification pass before Phase 1"
across all serialized UI files risks becoming a large up-front audit that
blocks all work — which conflicts with the opportunistic policy both docs
share, and with how Phase 0 actually succeeded. §5.1 therefore adopts the
ledger format in full, but bounds the up-front portion to whatever is needed
to make Milestone A's list finite and honest, with per-system classification
happening when that system is picked up. If that proves too loose in practice,
tighten it — but the failure mode of "audit everything first" is real.

Counter-proposal items 2–6 are adopted essentially as written.

### Answers to the five questions

**Q1 — Which exact Phase 1 dependency requires any registry?**
None. That was the tell that the guidance was wrong. Pause Menu is already
injected, Loading Screen needs one owner reference resolved, and the Tooltip
row is an audit. No registry is needed for any of them, which is precisely why
§7.1 now decides the boundary *before* a slice can quietly set the precedent.

**Q2 — UI cross-references, full scene composition, or both?**
UI cross-references only. The draft implied both by pairing a UI-scoped
inventory with a composition-root-scoped gate. §8 Milestone A now enumerates
UI-owned fields and explicitly lists the non-UI `GameManager` references as
out of scope and retained. Full composition-root cleanup is a legitimate
larger initiative, but it is not this one.

**Q3 — What evidence makes `new GameObject` count correlate with lower effort?**
None. I asserted it without support and it is withdrawn. The Pause Menu
disproves it directly: a count of 1 meant "constructed elsewhere by a service,"
i.e. already complete — the opposite of what the column implied. High counts
are equally ambiguous, since more constructed layout means more surface to
revalidate. See the metric warning in §5.

**Q4 — What concrete parity failures must fail a slice?**
Per §7.3: a control that invokes the wrong handler or invokes it more than
once; a listener duplicated after leave-and-reenter; an object that misses
initialization because it was inactive when constructed; initial/terminal
visibility or interactability differing from pre-migration; layout diverging
at a supported resolution; or a scene/prefab diff that grew instead of shrank.
Any of these fails the slice regardless of a clean null scan.

**Q5 — Which omitted serialized UI files were deliberately retained?**
None were deliberate — the omission was an oversight, not a judgment. Now
classified: `CellTooltipUI.cs`, `MycovariantTooltipPanel.cs`,
`MycovariantIcon.cs`, and the three selection controllers are **in scope**
(§5, Medium risk). `MagnifyingGlassFollowMouse.cs` is **retained** (gameplay
overlay, matching the standing `UI_StartGamePanel` exception).
`UiSpriteLibrary.cs` is **retained** (asset loading by design; its one match
was a doc comment, not a field).

## 12. Second-Pass Adversarial Review — 2026-09-05 (Planner Response in §13)

> **Review status:** The 2026-09-05 revision substantively resolves the first
> review's architecture, inventory, sizing, and slice-boundary findings. This
> follow-up records three remaining issues without changing the planner's
> revised proposal, so each can be accepted, refuted, or corrected explicitly.

### R2-1 — High: Milestone A still permits premature completion

- **Plan locations:** section 5 lists component-owned cross-references as TBD
  across Game Log, End Game, sidebar/profile, phase HUD, draft, hotseat,
  selection, tooltip, and Mutation Tree systems (`:151-164`). Section 7 says a
  system row is complete only when its component slices are complete
  (`:211-214`). However, Milestone A's supposedly enumerated gate (`:311-329`)
  lists only fields owned by `GameUIManager` and `GameManager`, and
  doc-completion depends only on that milestone (`:348-352`).
- **Trigger:** the manager-owned references are constructed/injected or
  explicitly retained while confirmed external references remain inside one or
  more component rows—for example, an End Game or Mutation Tree component
  still points outside its prefab/owning hierarchy.
- **Impact:** the plan can be marked complete despite not resolving or
  explicitly retaining every UI cross-reference in the initiative's own
  inventory. This recreates AR-1 in the opposite direction: the original gate
  was unreachable; the revised gate is reachable too early.
- **Required correction:** define Milestone A as **all confirmed
  cross-references in the section 5 field ledgers**, with the current manager
  lists labeled as a known baseline rather than the complete enumeration.
  Doc-completion must also require every in-scope component row to be done or
  explicitly reclassified/retained.

### R2-2 — Medium: the accepted parity standard still conflicts with the canonical policy

- **Plan locations:** section 7.3 correctly requires callback, state,
  repeated-entry, resolution, and YAML-diff evidence (`:267-293`), but
  `:295-298` defers tightening the canonical policy until "when convenient."
- **Concrete evidence:** `UNITY_CODE_FIRST_MIGRATION.md:116-119` still defines
  validation as a bootstrap path that asserts only that critical values are
  non-null, and its execution checklist at `:137-150` requires only a smoke
  assertion plus an affected-flow run. This plan's own section 3 says that
  policy doc is canonical, while this document is the mutable tracker.
- **Trigger:** an implementer follows the canonical policy or the top-level
  router without reading the tracker deeply enough to discover section 7.3.
- **Impact:** a slice can follow the authoritative instructions yet omit the
  stronger parity evidence whose absence already produced four documented
  Phase 0 regressions.
- **Required correction:** update the canonical policy before the Pilot slice,
  not opportunistically later. Its target patterns and migration checklist
  should require the same parity contract and evidence record as section 7.3.

### R2-3 — Medium: the plan incorrectly says the Unity Test Framework does not exist

- **Plan locations:** section 4 says "No Unity test framework exists"
  (`:84-88`), and section 7.3 frames standing up the Unity Test Framework as a
  possible future action (`:290-293`).
- **Concrete evidence:** `FungusToast.Unity/Packages/manifest.json:9` already
  declares `com.unity.test-framework` version `1.7.0`. What is absent is a Unity
  test assembly (`.asmdef`) and executable tests; `Assets/Scripts/Tests/` is
  empty.
- **Trigger:** test strategy is planned from the assumption that the framework
  itself must first be introduced.
- **Impact:** the plan overstates the infrastructure cost and weakens the case
  for adding executable parity coverage before high-risk work. It also records
  a factual claim that the repository disproves.
- **Required correction:** state that the package is installed but no test
  assembly/tests exist. Reframe the open choice as whether to add an EditMode
  or PlayMode `.asmdef` plus the first bootstrap/parity smoke tests before the
  Mutation Tree cohort.

### Second-pass disposition

- **Accepted from the revision:** the full-composition-root scope contradiction
  is removed; the static gameplay registry direction is rejected; Pause Menu
  and Tooltip are correctly reclassified; proxy metrics are withdrawn; omitted
  systems are now classified; work is tracked per component rather than per
  system phase; and the behavioral parity contract is materially stronger.
- **Remaining status:** the plan is substantively sound, but should not be
  treated as final until R2-1 corrects the completion gate. R2-2 and R2-3 are
  required documentation-consistency fixes before the Pilot slice begins.

## 13. Planner Response (Second Pass) — 2026-09-05

**Verdict: all three findings accepted, and one additional defect found while
fixing R2-2.** As with the first pass, each claim was checked against the repo
before responding. All corrections are applied in this commit — including to
the canonical policy doc, which R2-2 correctly argued should not wait.

### Disposition

| ID | Verdict | Verification | Fix |
|---|---|---|---|
| R2-1 | **Accepted** | Correct reading: §5 carries TBD component-owned references, §7 defines a row as done only when its component slices are, yet Milestone A enumerated *only* manager-owned fields and doc-completion depended solely on Milestone A. The gate could be satisfied with unclassified component references outstanding. | §8: Milestone A redefined as **all confirmed cross-references in the §5 ledgers**, manager fields relabeled a "known baseline," and doc-completion now additionally requires every cohort row done or explicitly reclassified. |
| R2-2 | **Accepted** | Confirmed: policy `:116-119` said "asserts nothing critical is null"; its checklist step 5 asked only for "a smoke assertion" and step 6 only to "run the affected flow." Since §3 designates that doc canonical and the router points at it, the authoritative path carried the weaker gate. | Fixed at the source, now, not deferred. Policy §5 validation bullet rewritten around parity (with the four Phase 0 regressions as the stated rationale); policy §7 checklist gains a pre-edit slice-contract step and a parity/record step. §7.3's deferral note replaced with a resolution note. |
| R2-3 | **Accepted** | Confirmed: `Packages/manifest.json:9` declares `com.unity.test-framework` 1.7.0. Verified separately that no `.asmdef` exists anywhere under `Assets/` and `Assets/Scripts/Tests/` is empty. My "no Unity test framework exists" was simply wrong. | §4 and §7.3 corrected: package installed, no test assembly or tests. The open choice is now framed as "add an EditMode/PlayMode asmdef + first smoke tests," a materially smaller lift than implied. |

### Additional defect found while fixing R2-2 (not in the review)

The divergence between the two docs was **wider than the validation half**.
The canonical policy's §5 Target Patterns still read:

> "A panel gets its collaborators through `SetDependencies(...)` **or a
> lightweight service locator**…"

and its checklist step 3 listed "**service locator entry**" as the first
option. That is precisely the direction §7.1 rejected in response to AR-2 —
so an implementer following the authoritative doc would have been steered
toward the pattern the plan had just ruled out, on top of the weaker
verification gate. Policy §5 now states the three-tier preference order
explicitly (construct+inject → façade → static registry as temporary bridge
only) and names `PauseMenuService` as the reference implementation.

This strengthens R2-2's underlying point rather than qualifying it: deferring
canonical-doc corrections until "convenient" let *two* superseded instructions
sit in the authoritative location simultaneously.

### On R2-1 being "AR-1 in the opposite direction"

Accepted, and worth recording as a lesson rather than a one-off fix. AR-1
found an unreachable gate; the correction made it enumerable; enumerating it
from the confirmable subset made it reachable too early. The general failure
was defining completion from *what was easy to list* rather than from *what
the initiative claims to accomplish*. Milestone A is now defined by the
ledger — which means it deliberately cannot be evaluated until §5.1's
classification is actually done. That is the intended property: "TBD" now
blocks completion instead of quietly falling outside it.

### Status

R2-1's correction is applied, so the plan is no longer gated on it. R2-2 and
R2-3 were named as prerequisites for the Pilot slice; both are fixed in this
commit, so the Pilot (Loading Screen) is unblocked.
