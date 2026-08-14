# Fungus-Toast Worklog

This is the canonical in-repo task list and handoff for active Fungus-Toast work. Durable design and implementation rules remain in the linked project helpers.

## Working Rules

1. Work through the active queue in priority order unless Jake explicitly reprioritizes it.
2. Treat each numbered item as a separate implementation, validation, and commit slice.
3. Keep Unity presentation changes out of Core unless the UI exposes a genuine rules/state defect.
4. Before editing a slice, inspect its current scripts, prefabs/scene references, call sites, and serialized fields.
5. After a meaningful completed slice, commit it, fetch, pull, and push.
6. Record completed results and any remaining manual Unity checks here; use daily OpenClaw memory for session-only detail.

## Active Thread

- **Focus:** Gameplay UX audit and polish for 16:9 desktop displays.
- **Primary target:** 1920x1080 or larger, using Unity's existing 1920x1080 CanvasScaler reference.
- **Baseline scenario:** Seed Cracker 10x10, observed from initial placement through late-game congestion.
- **Audit evidence:** Eight captured states covering untouched game start, cell inspection, mutation-tree entry, available and locked mutation inspection, growth-cycle progress, round 2, and a later-round phase banner; an additional late-game screenshot covers dense board/log conditions.
- **State:** Priority 1 is completed and visually accepted by Jake. Priority 2 is implemented in code and awaits focused Unity visual/interaction validation. Priority 3 has not started.
- **Design authority:** `FungusToast.Core/docs/UI_STYLE_GUIDE.md`.
- **Architecture authority:** `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`.
- **Tooltip authority:** `docs/ui/TOOLTIP_GUIDE.md`.
- **Primary Unity scene:** `FungusToast.Unity/Assets/Scenes/SampleScene.unity`.

## Scope and Guardrails

- Optimize clarity, decision confidence, readability, and board inspection before cosmetic polish.
- Preserve current rules and phase timing. UX work must not change mutation costs, prerequisite rules, growth/decay behavior, scoring, or AI behavior.
- Mutation purchases are currently immediate. Do not introduce queued purchases or a confirmation transaction without a separate design decision.
- Preserve score-based row reordering unless playtesting proves it is more harmful than useful; improve player reacquisition first.
- Reuse `GameUIManager`, the established tooltip systems, `UIStyleTokens`, pooled log views, and existing service/controller patterns.
- Avoid new raw color systems, one-off tooltip implementations, and fragile runtime-only layout fixes when a prefab/scene or shared-token change is appropriate.
- Do not rely only on hue to communicate selected, affordable, locked, completed, current, or dangerous states.
- Every slice requires a clean Unity compile and focused in-game verification at 1920x1080. Test 1600x900 and 1280x720 when the slice changes shared layout or responsive behavior.

## Priority Queue

### 1. P1 — Mutation workspace decision flow and node clarity

**Implementation status (2026-08-13):** Completed and visually accepted by Jake after the floating HUD controls were removed from the open mutation workspace.

**Problem:** The tree is strategically important but reads like a partial drawer over the board. Exit behavior is represented by an ambiguous arrow, remaining-point projection uses an unexplained `current → projected` display, prerequisite relationships are difficult to scan, and large tooltips cover the nodes they explain.

**Implementation intent:**

- Present the tree as a deliberate decision workspace with a clear overlay/surface relationship to the board.
- Replace the edge arrow with an explicit `Return to Board`/close affordance using the standard back/close visual language and a minimum 36x36 hit target.
- Keep purchases immediate. Show the actual remaining total after purchase; while hovering, label projected cost unambiguously (for example, `Cost: 1 · After purchase: 4`) instead of a bare arrow.
- Distinguish purchased, available-and-affordable, available-but-unaffordable, prerequisite-locked, and maxed nodes through shape/icon/text/state treatment as well as color.
- Make prerequisite paths and direct dependents readable while inspecting a node. Position or constrain the tooltip so it does not hide the selected node and the highlighted relationship path.
- Keep `Store Mutation Points` and `Time-Lapse` as supporting header utilities and ensure their relationship to ending/accelerating the phase is understandable.
- Preserve current targeted-selection flows and banking behavior.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/UI_MutationManager.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/MutationNodeUI.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/MutationTreeBuilder.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/MutationTreeColors.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/UI_TooltipPositioner.cs`
- `FungusToast.Unity/Assets/Scenes/SampleScene.unity` and referenced mutation-tree prefabs

**Acceptance checks:**

- A first-time player can identify how to buy, bank, and return to the board without interpreting an unlabeled glyph.
- Hovering every node state communicates availability and cost without obscuring the node or its relevant prerequisite/dependent path.
- Point totals are never visually mistaken for a pending or already-committed transaction.
- Repeated open/close, buying to zero, banking, targeted mutation selection, Time-Lapse, and hotseat handoff still work without duplicate listeners, stuck panels, or Console errors.

**Implemented checkpoint:**

- Expanded the mutation panel from the previous 1125px drawer to a full-canvas workspace and centered the five-column tree within it.
- Replaced the unlabeled dock arrow with a styled `Return to Board` header action and explanatory tooltip.
- Replaced the ambiguous points arrow with explicit `Cost` and `After purchase` copy while preserving immediate purchases.
- Added concise text states and semantic borders/backgrounds for available, ready-to-upgrade, owned, unaffordable, locked, next-round, active-surge, no-target, and maxed nodes; cost badges now also show one-point costs.
- Hover now distinguishes the inspected node, all of its prerequisites, and its direct dependents at the same time.
- Increased mutation-tooltip width to reduce excessive height while retaining automatic on-screen placement.
- Hide the floating next-track and pause-menu HUD controls while the full-screen mutation workspace is open, then restore them when it closes, so they cannot overlap `Return to Board`.
- Core and Simulation builds passed with 0 warnings/errors; `git diff --check` passed.
- Manual Unity checks still required: full-canvas composition at 1920x1080/1600x900/1280x720; every node state; tooltip/path visibility at all columns and screen edges; repeated open/close; purchases to zero; banking; targeted selection; Time-Lapse; hotseat handoff; game restart; Console cleanliness.

### 2. P1 — Explicit phase and growth-cycle tracker

**Implementation status (2026-08-13):** Completed in code; Unity Editor validation remains manual.

**Problem:** The compact `MUTATION → GROWTH [1][2][3][4][5] → DECAY` tracker makes the active growth cycle too easy to misread and does not clearly separate completed, current, and upcoming states.

**Implementation intent:**

- Make the current phase explicit in words.
- During growth, show `Cycle N of 5` and a five-segment progression with distinct completed/current/upcoming states.
- Provide clear next-phase context without duplicating noisy information.
- Use text, position, and state styling together; animation may reinforce the change but cannot be the only indicator.
- Verify the tracker receives the correct cycle number at the start and during every growth-cycle transition.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_PhaseProgressTracker.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/Phases/GrowthPhaseRunner.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/GameManager.cs`
- `FungusToast.Unity/Assets/Scenes/SampleScene.unity`

**Acceptance checks:**

- Static screenshots alone make Mutation, each of the five Growth cycles, Decay, and Draft unmistakable.
- Completed cycles cannot be confused with the active cycle.
- Tracker state remains correct during normal speed, Time-Lapse, fast-forward/testing flows, hotseat turns, and game transitions.

**Implemented checkpoint:**

- Growth now reports `CYCLE N OF 5` directly beneath the `GROWTH` phase label, while Mutation, Draft, and Decay retain their explicit named phase labels.
- The five cycle segments use different text treatments as well as semantic styling: completed numbers are struck through, the current number is bracketed/emphasized, and upcoming numbers remain plain/dim.
- Completed-cycle strikes use a tracker-local centered rule rather than the active TMP font asset's low strikethrough metric, preventing them from reading as underlines.
- Growth-cycle progress advances immediately when each cycle starts instead of after its execution and presentation delays, eliminating the observed one-cycle visual lag.
- Existing pulse animation remains supplemental feedback rather than the only current-cycle indicator.
- No scene or serialized-reference changes were required.
- Core and Simulation builds passed; `git diff --check` passed. Unity Editor was unavailable in this environment.
- Manual Unity checks still required: Mutation, Draft, Decay, `GROWTH / STARTING`, and cycles 1–5 at 1920x1080; sidebar fit at 1600x900 and 1280x720; normal speed, Time-Lapse, fast-forward/testing, hotseat, restart/transition paths, and Console cleanliness.

### 3. P1 — Desktop readability and interaction-target pass

**Implementation status (2026-08-14):** In progress. Slices 1–4 (shared tokens/compact HUD utilities, activity-log readability, the human-player marker, and mutation-node compact indicators) are complete in code; Unity Editor validation remains manual.

**Problem:** Several HUD labels, log controls, badges, mutation indicators, legend icons, adaptation icons, and compact utilities are difficult to read or target at 1920x1080.

**Implementation intent:**

- Inventory all in-game text below the style guide's normal 14–16px floor and justify or increase exceptions.
- Enforce at least 36x36 interactive hit areas for compact desktop controls, even when the visible icon remains smaller.
- Prioritize `Clear`, the `YOU` marker, log text/round tags, lock and DNA-cost indicators, Common Symbols, adaptation/mycovariant icons, Time-Lapse, and menu/fast-forward controls.
- Add or strengthen hover/focus feedback for every interactive compact control.
- Use shared typography, spacing, and button/icon tokens rather than local literals.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/UIStyleTokens.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_RightSideBar.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_MoldProfileRoot.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameLog/UI_GameLogPanel.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameLog/UI_GameLogEntry.cs`
- Mutation-tree scripts/prefabs and `SampleScene.unity`

**Acceptance checks:**

- Targeted controls and essential labels are comfortably readable/clickable at 1920x1080 without clipping or overlap.
- Shared layout remains usable at 1600x900 and 1280x720.
- No active control visually resembles a disabled control.

**Implemented checkpoint:**

- Added shared `UIStyleTokens` primitives for the 14px micro-text floor and 36x36 minimum / 40x40 preferred desktop interaction targets.
- Increased the floating pause-menu and next-track HUD controls from 28x28 to 36x36, preserving their 20px visible icons and standard hover/pressed styling.
- Repositioned the adjacent HUD controls to retain an 8px gap at the larger target size.
- Routed Store Mutation Points and Time-Lapse minimum heights through the shared 36px interaction token without changing their current labels, state behavior, or header composition.
- Increased the shared Activity Log `Clear` control from 40x20 with 12px text to 64x36 with 16px text while preserving its existing tooltip and button-state styling.
- Increased pooled log messages from 14px to 16px and round tags from 10px to the shared 14px micro-text floor.
- Increased the pooled entry baseline from 24px to 32px and expanded the round-tag rect/reserved space so larger tags do not collide with wrapped messages.
- Kept entry height calculation, category colors, pooling, filtering, caps, fade-in, and automatic scrolling behavior unchanged.
- Increased the runtime `YOU` marker from a 30x14 chip with 7–11px autosizing to a 44x20 chip with fixed 14px bold text.
- Added a dark token-based outline so the `YOU` chip stays distinct over varied mold sprites, while retaining the existing text label and left accent strip so identity does not rely on color alone.
- Disabled raycasts on the chip visuals so the underlying mold-icon hover highlight and pinned player tooltip remain accessible.
- Preserved score-based row ordering, stat columns, row sizing, perspective switching, and scoreboard data flow; rank and broader identity work remain scoped to item 7.
- Raised mutation-name and node-state autosizing floors to the shared 14px micro-text minimum and capped mutation names at their authored 18px size instead of the legacy 72px TMP maximum.
- Increased the DNA-cost treatment to a 36px-high group with a 28px icon and fixed 16px bold cost text; retained content-driven width for multi-digit costs.
- Increased lock, pending-unlock, and active-surge indicators from 32x32 to 36x36 and moved them to a shared upper-left lane so they remain separate from the upper-right DNA cost.
- Increased the runtime `MAX` badge from 36x16 with 10px text to 44x20 with fixed 14px text and the node's existing font.
- Disabled raycasts on status and `MAX` graphics so mutation-node hover, tooltip, relationship highlighting, and button ownership remain intact.
- Preserved mutation costs, availability, prerequisite timing, purchase behavior, state text, and category/state colors.
- `git diff --check` passed. Unity Editor was unavailable in this environment.
- Manual Unity checks still required: pause and next-track target placement/hover/click behavior; mutation workspace header fit and Store/Time-Lapse states; both Activity Logs with empty, single-line, wrapped, 30-entry, and multi-digit-round content; both `Clear` actions; player-specific filtering; special top actions; pooled reuse/reinitialization; the `YOU` chip with 1–8 players, score reordering, hotseat perspective changes, high stat counts, mold-icon hover/pinning, and restart; every mutation-node state and long/multi-line name; one- and multi-digit DNA costs; node hover/path highlighting and purchases; and Console cleanliness at 1920x1080, 1600x900, and 1280x720.

### 4. P1 — Progressive board-inspection tooltip

**Problem:** The default cell tooltip exposes diagnostic-level detail, is visually noisy over the board, and can conceal the inspected neighborhood.

**Implementation intent:**

- Default to decision-relevant facts: owner/state, source, age or vulnerability, resistance/toxin/death condition, and unusual local effects.
- Move coordinates, adjacency totals, positional classification, resistance source detail, and similar diagnostics into an expanded/advanced inspection state.
- Use a more opaque standard tooltip surface and shared readable type styles.
- Clamp placement to the viewport and avoid the hovered cell plus its immediate neighborhood where practical.
- Preserve the existing board-inspection path; do not route cell inspection through a new generic hover-tooltip implementation.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/CellTooltipUI.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MagnifyingGlassFollowMouse.cs`
- relevant cell-tooltip prefab/scene references

**Acceptance checks:**

- Alive, dead, toxic, resistant, initial-spore, and notable special-effect cells have concise and accurate default summaries.
- Advanced details remain available without crowding normal hover inspection.
- Tooltips stay on-screen and do not cover the inspected cell/neighborhood at board edges or corners.

### 5. P2 — Left-sidebar strategic labels and symbol legend

**Problem:** Growth probabilities and Random Decay lack enough hierarchy, while `Common Symbols` and adaptation/mycovariant icons are too cryptic and small.

**Implementation intent:**

- Keep the existing `Growth Chance Per Living Cell` title visible and confirm the center/directional relationship reads without prior knowledge.
- Give Random Decay a clearly separated labeled row with accessible explanation.
- Convert Common Symbols into a readable legend with larger targets and icon-plus-label/tooltips; show only relevant symbols if that reduces clutter without harming discoverability.
- Give adaptations and mycovariants readable names or counts at the section level while retaining detailed hover inspection.
- Ensure changed mutation values visibly refresh and remain explainable.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_MoldProfileRoot.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GrowthDirectionCell.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Tooltips/TooltipProviders/RandomDecayChanceTooltipProvider.cs`
- adaptation/mycovariant and overlay-legend providers/assets

**Acceptance checks:**

- A player can name what the percentage grid, Random Decay value, and every visible legend symbol represent without guessing from art alone.
- Hover highlights and tooltips remain accurate after mutations, adaptations, mycovariants, and board-state changes.

### 6. P2 — Activity-log information architecture

**Problem:** Permanent Human and Global Activity panels waste space early and become dense, tiny, and scroll-heavy late. Current automatic bottom scrolling does not account for a player reading older entries.

**Implementation intent:**

- Prototype the smallest coherent structure that lets the board reclaim unused space: collapsible sections first; use tabs only if two simultaneous feeds still prove unnecessary in playtesting.
- Keep a compact recent-events view and make full history deliberately expandable.
- Auto-scroll only when the player is already at/near the bottom; preserve reading position otherwise and expose a clear return-to-latest action.
- Group entries by round/phase and use semantic strips/icons/text accents rather than full-row category fills.
- Preserve log routing, filtering, pooling, entry caps, and special top actions.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameLog/UI_GameLogPanel.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameLog/UI_GameLogEntry.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameLog/GameLogColorSchemes.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_RightSideBar.cs`
- left-sidebar/scene layout references in `SampleScene.unity`

**Acceptance checks:**

- Early-game empty logs no longer dominate both sidebars.
- Late-game entries remain readable, grouped, and inspectable.
- Incoming events do not steal scroll position while the player is reading history.
- Human/global filtering, clear actions, special action prompts, reinitialization, and pooled-entry reuse remain correct.

### 7. P2 — Scoreboard identity and rank comprehension

**Problem:** Rows reorder by living cells and then dead cells, but the small `YOU` marker makes it slow to reacquire the human player after ranking changes.

**Implementation intent:**

- Preserve score-based ordering initially.
- Add clear rank numbers and a full readable `YOU` chip beside the human identity.
- Strengthen mold/player naming and aligned Alive/Dead/Toxin stat labels without relying only on color or texture.
- Add restrained row-transition feedback when rank changes if the current update flow supports it safely.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_RightSideBar.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/PlayerSummaryRow.cs` or the current summary-row component/prefab
- scoreboard onboarding/provider references and `SampleScene.unity`

**Acceptance checks:**

- The human player and current rank can be found immediately after any reorder.
- Ties and multiplayer/hotseat identities remain deterministic and understandable.
- Rows do not jump, overlap, or leave stale rank/identity state during rapid updates or game reset.

### 8. P2 — Late-game board legibility

**Problem:** At high occupancy, mold art, dead cells, toxins, resistance/status overlays, selection, highlights, and substrate detail compete for attention.

**Implementation intent:**

- Establish and document a strict visual priority for ownership, life/death/toxin state, exceptional status, hover, and selection.
- Strengthen hovered/selected cells and suppress lower-value substrate/secondary embellishment beneath dense occupancy.
- Evaluate an inspection overlay for ownership or cell state before changing the underlying mold art.
- Verify recognition without relying only on color.
- Measure before adding effects that increase render/layout work.

**Likely code/assets:**

- board/grid visualizer, overlay, highlight, and cell-view scripts under `FungusToast.Unity/Assets/Scripts/Unity/Grid` and `UI`
- `CellTooltipUI.cs`, board-medium configuration, and relevant materials/sprites

**Acceptance checks:**

- Ownership, living/dead/toxic state, resistance, hover, and selection remain distinguishable on a nearly full Seed Cracker 10x10 board.
- Improvements hold on at least one medium and one large board/background.
- No clipping, sorting-order, hover-hit-testing, or obvious frame-time regression is introduced.

### 9. P3 — Phase-banner restraint and sidebar surface cohesion

**Problem:** Routine phase banners can obscure the board and duplicate the tracker, while the two sidebars still contain inconsistent surface treatments.

**Implementation intent:**

- Shorten routine phase copy, improve contrast, and align semantic colors with the persistent tracker.
- Reduce duration/emphasis or suppress familiar routine banners where the tracker is sufficient; retain stronger treatment for major events, drafts, endgame, and special interruptions.
- Keep banners away from active cells when feasible.
- Apply the shared `PanelPrimary`/`PanelSecondary`/`PanelElevated` hierarchy consistently across both sidebars and logs.

**Likely code/assets:**

- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_PhaseBanner.cs`
- phase/banner call sites in `GameManager`, phase runners, and presentation services
- `UI_RightSideBar.cs`, `UI_MoldProfileRoot.cs`, log panels, and `SampleScene.unity`

**Acceptance checks:**

- Routine phase changes are noticeable but do not repeatedly cover important play information.
- Major-event banners retain appropriate emphasis.
- Both sidebars read as one visual system without losing semantic player/event accents.

## Cross-Slice Validation Matrix

Use the smallest relevant subset for each completed slice, then run the complete matrix after item 9:

1. Unity Editor clean compile with no new Console errors/warnings.
2. Seed Cracker 10x10 at 1920x1080: initial state, cell hover, mutation tree, all mutation node states, each growth cycle, round transition, phase banner, and dense late game.
3. One medium and one large background at 1920x1080 to catch board/sidebar interaction regressions.
4. Shared-layout checks at 1600x900 and 1280x720.
5. One-human, hotseat/multi-human, and maximum-player scoreboard/log checks where affected.
6. Normal speed, Time-Lapse, and development fast-forward where affected.
7. Rapid open/close/navigation and game restart to catch stale state, duplicate listeners, stuck raycasts, and coroutine cleanup defects.
8. `git diff --check`; build Core/Simulation only when shared code changed.

## Decisions to Make During the Relevant Slice

These are deliberately deferred until the current implementation and a focused mockup/playtest provide evidence:

- **Mutation overlay:** full-screen modal surface versus a near-full-width workspace that retains limited board context.
- **Advanced cell detail input:** explicit `More Details` affordance versus a held modifier key; prefer an explicit discoverable control unless space makes it impractical.
- **Logs:** independent collapsible feeds versus one tabbed activity area; prototype collapse first because it preserves simultaneous visibility.
- **Late-game inspection:** ownership overlay, state overlay, or both; choose after testing the minimal layer/contrast adjustments.
- **Routine banners:** reduced duration versus player-configurable suppression; do not add a setting until reduced presentation has been tested.

Ask Jake before implementing any decision that changes gameplay behavior, removes currently available information, or materially expands scope beyond this audit.

## Historical Checkpoint — Startup Menu Polish

Startup tasks 1–11 were implemented and synchronized before this gameplay audit. They covered shared startup styling, main menu and setup flows, mold selection, campaign overview/creation, Settings, Credits, development-control gating, startup panel fades, and regression cleanup.

Automated validation at that checkpoint:

- Core and Simulation builds passed with 0 warnings/errors.
- Core tests excluding the unrelated known roster assertion: 510 passed.
- `git diff --check` passed.
- Unity compile/visual validation remained manual because the Unity executable was unavailable in the implementation environment.

Known unrelated Core test failure at that checkpoint:

- `StrategyCatalogTests.Campaign_progression_board_presets_only_use_cmp_strategy_names`
- Offending strategy: `TST_Training_ResilientMycelium_Offset3` (expected `CMP_` prefix).

Jake's consolidated startup-flow Unity validation remains outstanding and should be handled as narrow defect follow-ups rather than reopening that completed redesign scope.
