# Fungus-Toast Worklog

This file is the lightweight continuity anchor for OpenClaw-assisted Fungus-Toast work.

## Modus Operandi

Use the following minimal workflow to preserve working memory across sessions:

1. **Session anchor**
   - At the start of a Fungus-Toast session, explicitly say to work in:
       - `c:/Users/jakej/FungusToast`
   - Also name the current thread of work when helpful.

2. **Canonical task list**
   - Use this file as the canonical in-repo task list and handoff for active Fungus-Toast work.
   - At the start of a new task, check `Pending Tasks` here and ask whether one of those should be completed first.

3. **Durable project record**
   - Keep durable project context in the repo only when it is still actively useful.
   - Do not keep transient simulation findings or stale task history here once they stop helping current decisions.

4. **End-of-session checkpoint**
   - Put end-of-session checkpoints in OpenClaw daily memory (`memory/YYYY-MM-DD.md`).
   - Only keep partial-progress resume notes here when they are needed to continue an unfinished task.

## Current Notes

- This file should stay concise and current.
- Detailed balance or simulation findings should live in the most relevant project docs, while this file tracks the active thread, pending tasks, and the next handoff.
- `docs/WORKLOG.md` is the canonical in-repo task and handoff file for active Fungus-Toast work.
- When starting a new Fungus-Toast task, first check the pending tasks here and ask whether one of them should be completed before starting something new.

## Active Thread

- **Repo:** `c:/Users/jakej/FungusToast`
- **Current focus:** Startup menu design and UX polish.
- **Current state:** Tasks 1-7 are complete; Task 8 is next. Jake authorized implementation through Task 11 before consolidated Unity validation.
- **Primary surfaces:** Main menu, custom-game setup, custom mold selection, campaign overview, new-campaign setup, settings, and credits.
- **Design authority:** `FungusToast.Core/docs/UI_STYLE_GUIDE.md`.
- **Architecture authority:** `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`.

### Execution Contract

Complete exactly one numbered task at a time. Do not start a later task until the previous task is committed and synchronized. Jake authorized deferring the consolidated Unity visual walkthrough to Task 11; keep recording affected-flow checks as each slice lands.

Every implementation agent must:

1. Read `.github/copilot-instructions.md`, `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`, and `FungusToast.Core/docs/UI_STYLE_GUIDE.md` before editing.
2. Inspect the current implementation and serialized scene/prefab references before choosing a pattern; the file hints below are starting points, not permission to rewrite whole controllers.
3. Keep the slice presentation-only unless the task explicitly names behavior. Preserve campaign saves, custom-game resume, settings persistence, scene navigation, and button callbacks.
4. Reuse `UIStyleTokens`, existing tooltip infrastructure, existing sprites, and existing runtime-layout patterns. Do not introduce a second theme system, new raw color palette, new font, or new artwork.
5. Preserve the 1920x1080 reference layout and verify at 1920x1080, 1600x900, and 1280x720. Check for overlap, clipping, illegible wrapping, and controls moving off-screen.
6. Validate Core with `dotnet build FungusToast.Core/FungusToast.Core.csproj`; build Simulation only if shared code changes. Treat a clean Unity Editor compile and an in-Editor walkthrough of the affected flow as required manual checks.
7. Capture before/after screenshots of every screen or state changed and report any required Unity Editor/Inspector steps.
8. Review the diff for serialized-reference risk, listener lifecycle, unrelated refactors, duplicated helpers, and style-token compliance.
9. Update this worklog: mark the completed task `[x]`, add concise findings needed by later tasks, and identify the next task. Commit the completed slice, then fetch, pull, and push.

### Scope Guardrails

- First pass uses the existing TMP font and existing art. Font replacement and new decorative assets are out of scope.
- Background animation must stay subtle and nonessential. Do not add a new accessibility/preferences system solely for animation in this pass.
- Development/testing controls may remain available in development builds but must not occupy release-facing layouts.
- Do not move deterministic game or campaign rules into Unity UI code.
- Avoid broad scene or prefab rewrites. Most current startup UI is assembled at runtime; make the smallest safe change around those seams.

## Pending Tasks

### 1. [x] Establish shared startup presentation primitives

**Completed:** Added shared startup card/text, universal choice-state, utility-action, and danger-action primitives to `UIStyleTokens`. Adopted them in main-menu utility actions, custom player-count choices, and campaign difficulty choices. Core build passed; Unity Editor validation is deferred to Task 11 because no Unity executable is available in this environment.

**Goal:** Make later screen work use one visual language without prematurely redesigning an individual screen.

**Likely files:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UIStyleTokens.cs`
- The three startup controllers only as needed to adopt the shared primitives

**Work:**
- Inventory repeated startup-only panel, text, choice-state, button, spacing, and layout code in the three controllers.
- Extend `UIStyleTokens` with the smallest reusable helpers needed for startup cards, screen headings, supporting copy, universal selected/available/disabled choice states, focus/hover treatment, and danger actions.
- Reuse the guide's existing surface, text, state, spacing, and button values. Add named layout constants only when the same value is genuinely shared.
- Demonstrate adoption on representative controls in all three controllers so the API is proven, but do not redesign full screens in this task.
- Keep focus/selection meaning visible without color alone where existing hierarchy permits it; later tasks may add checkmarks or locks to specific cards.

**Acceptance criteria:**
- Representative main-menu, setup, and campaign controls use the same shared APIs and still behave identically.
- Selected, enabled, hovered/focused, disabled, and destructive roles are visually distinguishable.
- No new ad hoc palette or screen-specific copy changes are introduced.
- Later tasks can style a startup card or choice without duplicating a `ColorBlock` or raw token combination.

**Special verification:** Open the main menu, custom setup, and campaign overview; exercise hover, click, disabled, and back navigation states. Confirm no serialized references were lost.

### 2. [x] Unify the startup backdrop and main-menu composition

**Completed:** Reframed the logo as the hero, moved alpha status into a compact footer badge beside version metadata, renamed the custom mode and tightened both mode descriptions, unified utility-button treatment, added a calm central card, and substantially reduced ambient mold contrast/motion while strengthening the edge vignette. Core build passed; affected screen states remain queued for Task 11 Editor validation.

**Depends on:** Task 1.

**Goal:** Make the first screen feel intentional and establish the shell inherited by startup subpanels.

**Likely file:** `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_ModeSelectPanelController.cs`

**Work:**
- Keep the logo as the hero and establish clear logo, build-status, mode-choice, utility, and version zones.
- Replace the large `Alpha Test Build` heading/paragraph with a compact build-status badge near the version label.
- Rename `Solo / Hotseat Game` to `Custom Game` and use concise helper copy: solo against AI or shared-device play.
- Shorten the campaign helper copy while preserving meaning.
- Give Custom Game and Campaign equal neutral peer-choice treatment; keep Settings, Credits, and Quit in one consistent lower-emphasis stack.
- Apply one calm loam backdrop across the main menu and subpanels. Reduce decorative mold contrast/saturation, keep the center behind controls clear, and strengthen the dim/vignette layer using existing assets and tokens.
- Keep ambient motion subtle; do not let drifting art cross the primary control column or become necessary to understand the screen.

**Acceptance criteria:**
- The logo is the dominant element; build metadata no longer reads as a subtitle.
- Peer mode choices look equivalent, utility actions look subordinate, and icon/text alignment is consistent within each stack.
- Main, settings, credits, and campaign entry views share the same backdrop without visible jumps.
- All existing navigation, campaign-state messaging, version display, and platform-specific Quit behavior remain functional.

**Special verification:** Check main menu with and without a resumable campaign and in both development/release-style build conditions available locally.

### 3. [x] Restructure the custom-game setup step

**Completed:** Reframed count selection as a `Game Setup` card, added explicit `Total players` and `Human players` labels, normalized the serialized number buttons into compact segmented rows, made board size a nested selection card, removed audio from the flow, retained save-aware Resume visibility, and simplified the human/AI summary. Count validation, persistence, board mapping, callbacks, and save semantics were not changed. Core build passed; Editor checks are deferred to Task 11.

**Depends on:** Tasks 1-2.

**Goal:** Turn the raw control list into a compact, legible game-setup form.

**Likely files:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameStart/UI_StartGamePanel.cs`
- `FungusToast.Unity/Assets/Scenes/SampleScene.unity` only if a serialized edit is truly necessary
- Existing player-count button components if their visual-state API must change

**Work:**
- Change the screen title to `Game Setup`.
- Add explicit row labels `Total players` and `Human players`; retain the current valid-count rules and automatic corrections.
- Present number choices as compact segmented controls using the shared choice states. Do not change supported counts or game setup logic.
- Consolidate the step inside one primary panel with consistent section spacing.
- Remove audio controls from this flow; settings remains their single home.
- Present board size as a clear labeled selection card/row with the current name and dimensions. Reuse the existing dropdown unless replacing it is demonstrably lower risk.
- Make Resume Saved Game show useful save availability/context, or clearly disable it when unavailable; do not alter save format or resume semantics.
- Keep Start Game as the single green forward CTA and Back as a compact neutral action.

**Acceptance criteria:**
- A new player can identify total players, humans, board size, and the next action without interpreting unlabeled number rows.
- Invalid human/total combinations remain impossible and persistence still restores valid menu state.
- Audio controls no longer appear in custom setup.
- Resume is never visually active without a usable save and successfully resumes when a save exists.
- No setup behavior, board-size mapping, or save compatibility changes.

**Special verification:** Exercise 1-7 total players, all permitted human counts, each board size, fresh start, saved-game resume, Back, and a return to the screen after leaving it.

### 4. [x] Polish custom-game mold selection

**Completed:** Simplified the step to `Choose a Mold`, added `SETUP 2 OF 2`, separated player context from helper copy, added numbered/color-coded player identity, enlarged mold cards/icons, and made selected/taken states explicit with a lichen outline, checkmark, and owning-player label. Sequential assignment, defaults, uniqueness enforcement, Back, and idle motion were preserved. Core build passed; Editor checks are deferred to Task 11.

**Depends on:** Task 3.

**Goal:** Make the second setup step feel deliberate and make selection ownership unmistakable.

**Likely file:** `FungusToast.Unity/Assets/Scripts/Unity/UI/GameStart/UI_StartGamePanel.cs`

**Work:**
- Use `Choose a Mold` as the title, a concise `Player N of N` context line, and one helper sentence about unique human selections.
- Add a clear `Setup 2 of 2` indicator without introducing a general breadcrumb framework.
- Increase mold art prominence while keeping labels readable.
- Apply universal available, hover/focus, selected, and unavailable/taken states. Selected cards require a strong border plus a non-color indicator such as a checkmark; taken cards require readable ownership/unavailability treatment.
- Show the choosing player's identity/color near the context line when multiple humans are configuring the game.
- Preserve sequential multi-human selection, default assignment, unique-mold enforcement, idle art motion, Back behavior, and the final transition into gameplay.

**Acceptance criteria:**
- The current player and selected mold are obvious at a glance.
- A taken mold cannot be mistaken for an available or selected mold.
- Every mold remains identifiable by image and text without color alone.
- One-human and multi-human flows complete correctly, including backing out and re-entering.

**Special verification:** Complete setup with one human, then with at least three humans; test attempted duplicate choices, Back from player 2+, and all viewport sizes.

### 5. [x] Clarify the campaign overview and progression summary

**Completed:** Replaced the toast-block progress visualization with a non-interactive labeled progress bar, made current Moldiness Level primary, muted lifetime totals, previewed the next reward tier directly from `MoldinessUnlockCatalog`, retained the labeled unlocked-reward strip/tooltips, and standardized `Start New Campaign`. Resume/start CTA priority remains driven by existing save state. Core build passed; Editor/state-boundary checks are deferred to Task 11.

**Depends on:** Tasks 1-2.

**Goal:** Make resume/start hierarchy and moldiness progression understandable in a few seconds.

**Likely files:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_CampaignPanelController.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/MoldinessUnlockedRewardsStripController.cs` only if needed for labels/tooltips

**Work:**
- Make current Moldiness Level the summary card's primary information.
- Replace the toast-block progress grid with a conventional labeled progress bar such as `12 / 21 to Level 7`, driven by existing progression values.
- De-emphasize lifetime-earned statistics without removing useful information.
- Add a clear label for the unlocked-reward strip and keep using the existing tooltip system for reward details.
- Preview the next reward/milestone when existing catalog data supports it; do not duplicate unlock rules in UI.
- Rename `New Campaign` to `Start New Campaign`.
- Preserve the single-green rule: Resume Campaign when resumable, otherwise Start New Campaign. Back remains subordinate.

**Acceptance criteria:**
- Current level, progress toward the next level, and unlocked rewards have distinct hierarchy.
- Progress text/bar exactly reflects existing moldiness state at boundary values and after level advancement.
- Resume, start-new, and Back states remain correct with no campaign, active campaign, completed level, and pending carryover/spore-preservation states.
- Reward tooltips still use the shared tooltip stack.

**Special verification:** Inspect zero progress, partial progress, threshold reached, active campaign, and no-save states using existing development data controls only in a development build.

### 6. [x] Polish new-campaign difficulty and mold setup

**Completed:** Widened only the campaign-creation step, structured it as numbered difficulty/mold sections, surfaced start depth and AI drafting behavior on each difficulty card, added visible lock prerequisites, and aligned selected difficulty/mold cards with the shared checkmark/outline language. Existing option metadata, unlock calculations, selected indices, and campaign start parameters were preserved. Core build passed; Editor checks are deferred to Task 11.

**Depends on:** Task 5.

**Goal:** Turn campaign creation into a clear two-section decision rather than a dense centered column.

**Likely file:** `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_CampaignPanelController.cs`

**Work:**
- Structure the screen as two numbered sections: difficulty, then mold.
- Use `Choose Your Mold` and concise supporting copy; widen the primary panel enough to avoid awkward heading wraps at the reference resolution.
- Give every difficulty a short gameplay description derived from existing difficulty behavior/metadata. Do not invent numeric promises that the rules do not guarantee.
- Show locked difficulties with a lock indicator and explicit unlock condition sourced from existing state.
- Apply the same mold available/hover/selected/locked language established in Task 4, including a non-color selected marker.
- Keep Start Campaign as the only green CTA and Back as the quieter action.

**Acceptance criteria:**
- Difficulty effects and lock reasons are understandable before selection.
- Locked options cannot be confused with merely unselected options.
- Selected difficulty and mold remain stable while navigating within the setup step.
- Start Campaign enables only for a valid configuration and launches the same campaign state as before.

**Special verification:** Test each unlocked difficulty, at least one locked-difficulty state, every available mold, Back/return, and campaign launch.

### 7. [x] Rebuild settings as a compact preferences panel

**Completed:** Replaced audio cycle buttons with live sliders backed by the existing persisted setters, added percentage and speaker/mute feedback, compacted Help/Tutorial controls, renamed the data section to `Campaign Data`, and isolated reset inside a danger-accented `Danger Zone` with fact-checked erase/retain copy and the existing two-step confirmation. Added responsive scaling for startup overlay cards. Core build passed; persistence, keyboard focus, and reset-state Editor checks are deferred to Task 11.

**Depends on:** Tasks 1-2.

**Goal:** Replace prototype-like CTA buttons with conventional settings controls and isolate destructive campaign-data actions.

**Likely files:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_ModeSelectPanelController.cs`
- Existing audio settings services only if a narrow setter is missing

**Work:**
- Present Sound Effects and Music as labeled rows with sliders, percentage values, and familiar speaker/mute indicators using existing assets or text glyphs that render reliably. Preserve current stored value ranges and persistence keys.
- Keep settings controls on dark secondary surfaces rather than styling them as page-level CTAs.
- Rename `Advanced Campaign` to `Campaign Data` or `Progress Management`.
- Create a visually separated `Danger Zone` for Reset Campaign Rewards using danger-accented outline/text rather than green or ordinary neutral styling.
- Expand confirmation copy to state exactly what reset erases and retains based on actual service behavior.
- Show a brief, non-blocking confirmation after tutorial tips are re-enabled and after settings changes where feedback is otherwise ambiguous.
- Preserve Back/Escape behavior and do not introduce a separate settings store.

**Acceptance criteria:**
- Audio can be adjusted precisely, percentage labels match saved values, mute/zero is clear, and values survive leaving/reopening settings and restarting Play mode as supported today.
- Reset cannot occur from one accidental click and its confirmation is factually accurate.
- Tutorial replay feedback is visible and campaign progress is untouched.
- Keyboard/mouse focus does not get trapped in sliders or confirmation state.

**Special verification:** Test minimum, intermediate, and maximum audio values; reopen settings; cancel and confirm campaign reset using disposable development data; verify Back in both normal and confirmation states.

### 8. [ ] Restructure credits content

**Depends on:** Task 2.

**Goal:** Make credits read as an intentional release screen while preserving the personal acknowledgements.

**Likely file:** `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_ModeSelectPanelController.cs`

**Work:**
- Rename `Special Credits` to `Credits` everywhere, including button label, heading, and tooltip if present.
- Replace conversational centered paragraphs with structured `Artwork` and `Music` sections, names as the primary lines, and concise contribution details below.
- Retain the factual names and attribution currently shown; do not add or remove contributors without Jake's direction.
- Add only restrained decoration using the existing logo or one existing specimen asset, keeping the content panel compact.
- Keep the shared startup backdrop and compact Back treatment.

**Acceptance criteria:**
- Credits are quickly scannable, factually unchanged, and visually consistent with the other startup subpanels.
- No long centered paragraph remains.
- Back, Escape, and repeated open/close behavior work without duplicated runtime objects.

**Special verification:** Reopen Credits several times in one session and check object hierarchy for duplicated labels/buttons.

### 9. [ ] Enforce release UI hygiene for development controls

**Depends on:** Tasks 3 and 5-6.

**Goal:** Guarantee that testing tools do not affect player-facing composition or ship visibly in normal builds.

**Likely files:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameStart/UI_StartGamePanel.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_CampaignPanelController.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Testing/DevelopmentTestingCardController.cs` only if shared gating requires it
- `FungusToast.Unity/Assets/Prefabs/UI/UI_TestingOptionsSection.prefab` only if serialized defaults are unsafe

**Work:**
- Audit every startup development/testing entry point, including legacy scene objects and runtime-created rails/cards.
- Centralize or consistently apply the existing build/development gate; do not create divergent rules between custom and campaign screens.
- Ensure hidden controls do not reserve width, shift the main column, remain focusable, or retain invisible raycast targets.
- Keep the tools usable in authorized development builds and preserve their current test functionality.
- Remove obsolete runtime or serialized remnants only when references are proven unused; do not delete assets speculatively.

**Acceptance criteria:**
- A normal/release build condition exposes no development-testing label, toggle, card, empty rail, or layout gap on any startup screen.
- A development build still exposes functional tools without pulling the primary action stack off-center.
- Hidden objects cannot receive input or appear in navigation order.

**Special verification:** Compare screenshots and hierarchy state under both build conditions for custom setup, campaign overview, and new-campaign setup.

### 10. [ ] Add restrained interaction and transition polish

**Depends on:** Tasks 2-9.

**Goal:** Make the completed visual system feel responsive without adding distracting motion or a new framework.

**Likely files:** The three startup controllers and existing shared UI effect/timing helpers.

**Work:**
- Audit every startup interactive control for visible resting, hover/focus, pressed, selected, and disabled feedback.
- Add short, consistent fades between main menu and startup subpanels using existing CanvasGroup/coroutine patterns where safe. Prevent double-clicks and stale coroutines during transitions.
- Keep decorative backdrop motion extremely slow and behind content; suppress or simplify nonessential motion if an existing preference/platform condition supports that behavior.
- Use existing UI sound hooks/assets for hover/confirm/back only if already standardized. Do not add new audio files or parallel playback services.
- Ensure rapid navigation cannot leave panels half-visible, block raycasts, duplicate listeners, or trigger actions twice.

**Acceptance criteria:**
- Interaction feedback is consistent across all seven screens and does not depend on color alone for selection/lock state.
- Transitions are brief, never delay required input noticeably, and leave one authoritative active panel.
- Repeated rapid open/back/open sequences produce no exceptions, duplicated objects, or stuck input.
- No new per-frame allocations or noisy logging are introduced.

**Special verification:** Stress-click mode/subpanel navigation, use Back/Escape during and after transitions, and watch the Console and hierarchy for leaks or duplicates.

### 11. [ ] Perform the final startup-flow UX and regression pass

**Depends on:** Tasks 1-10.

**Goal:** Validate the startup experience as one coherent product flow and fix only small issues discovered by the pass.

**Likely files:** Any startup UI files, limited to narrow fixes; update the style guide only if a genuinely reusable rule emerged.

**Work:**
- Walk every route: launch -> Custom Game -> mold choice -> start/back; launch -> Campaign -> resume/new -> difficulty/mold -> start/back; Settings normal/confirmation states; Credits; Quit where supported.
- Verify 1920x1080, 1600x900, and 1280x720 plus one wider aspect ratio if practical.
- Check title/body hierarchy, centered versus left-aligned copy, hit targets, single-green rule, tooltips, selected/locked/disabled meanings, contrast, and icon-label alignment.
- Test fresh-data, save-present, campaign-active, locked-content, and development/release UI states.
- Compare a complete after-screenshot set against the audit baseline and fix only local defects. Log larger new ideas as separate follow-ups rather than expanding this task.
- Confirm a clean Unity Editor compile and no new warnings/errors while traversing the flow.

**Acceptance criteria:**
- All seven screens look like one product and retain their complete behavior.
- No clipped/overlapping content, unreadable state, misleading active control, or release-visible testing UI remains at supported validation sizes.
- Navigation, saves/resume, settings persistence, locks, campaign start, and custom-game start pass.
- Before/after screenshots and any remaining non-blocking limitations are included in the handoff.
- `docs/WORKLOG.md` is reduced back to a concise completed checkpoint or updated with only genuinely outstanding follow-ups.

## Next Handoff

- Assign **Task 8: Restructure credits content**. Settings presentation and persistence wiring are code-complete.
