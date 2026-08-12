# Fungus-Toast Worklog

This file is the lightweight continuity anchor for OpenClaw-assisted Fungus-Toast work.

## Modus Operandi

1. Use this file as the canonical in-repo task list and handoff for active work.
2. Keep durable design and implementation authority in the relevant project helpers, not here.
3. Put end-of-session checkpoints in OpenClaw daily memory.
4. After a meaningful completed code slice, commit it, fetch, pull, and push.

## Active Thread

- **Repo:** `c:/Users/jakej/FungusToast`
- **Focus:** Startup menu design and UX polish.
- **State:** Tasks 1-11 are implemented and synchronized. Awaiting Jake's consolidated Unity validation.
- **Design authority:** `FungusToast.Core/docs/UI_STYLE_GUIDE.md`.
- **Architecture authority:** `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`.

## Completed Startup Polish

1. Shared startup card, typography, choice-state, utility, and danger-action primitives in `UIStyleTokens`.
2. Unified main-menu composition, alpha/version treatment, startup backdrop, and mode/utility hierarchy.
3. Restructured custom-game setup with explicit player rows, segmented choices, board-size card, and settings-only audio.
4. Polished custom mold selection with step context, player identity, larger cards, selected markers, and taken ownership.
5. Replaced campaign toast-block progression with a labeled progress bar, clearer hierarchy, and next-reward preview.
6. Restructured campaign creation into numbered difficulty/mold sections with descriptions, lock conditions, and strong selection states.
7. Rebuilt Settings with persisted audio sliders, clear values/mute state, tutorial feedback, and a fact-checked campaign-data Danger Zone.
8. Restructured Credits into role/name/contribution rows with restrained logo decoration.
9. Added one release-aware gate for startup development controls; production builds do not construct or expose testing rails/cards.
10. Added guarded 120 ms startup panel fades with interaction/raycast protection and cancellation cleanup.
11. Completed static regression cleanup, including removal of the retired toast-grid implementation and stale serialized menu copy.

## Validation Completed

- `dotnet build FungusToast.Core/FungusToast.Core.csproj` — passed, 0 warnings/errors.
- `dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj` — passed, 0 warnings/errors.
- Core tests excluding the unrelated known roster assertion — 510 passed.
- `git diff --check` — passed.
- Stale startup copy and retired campaign toast-grid references — clear in affected startup files.
- Unity executable was not available in the implementation environment, so Unity compile/visual validation remains manual.

## Known Unrelated Test Failure

The full Core suite currently has one campaign roster assertion failure outside this UI work:

- `StrategyCatalogTests.Campaign_progression_board_presets_only_use_cmp_strategy_names`
- Offending strategy: `TST_Training_ResilientMycelium_Offset3` (expected `CMP_` prefix)
- Full result: 510 passed, 1 failed.

## Pending Validation — Jake

Run a clean Unity Editor compile, then walk the startup flow at 1920x1080, 1600x900, and 1280x720:

1. Main menu: logo/build/version hierarchy, Custom Game/Campaign peer choices, Settings/Credits/Quit hierarchy, hover/pressed states.
2. Custom Game: all total/human counts, invalid-count prevention, all board sizes, Resume visibility/function, Back/return.
3. Custom mold selection: one and three-human flows, duplicate prevention, selected/taken ownership, Back from player 2+.
4. Campaign overview: no-save, active/resumable, pending reward/carryover, zero/partial/threshold Moldiness progress.
5. New Campaign: each unlocked difficulty, at least one locked state, every mold, Back/return, campaign launch.
6. Settings: SFX/music at 0%, intermediate, and 100%; reopen/restart persistence; tutorial replay; cancel/confirm reset using disposable data.
7. Credits: repeated open/close without duplicated runtime objects.
8. Development UI: visible and functional in Editor/development builds; absent with no empty rail, focus target, or gap in release builds.
9. Stress navigation: rapid open/back/open and Back during/after fades; check for stuck raycasts, half-visible panels, duplicate listeners/objects, and Console errors.

## Next Handoff

- Jake validates the Unity flow above and reports screenshots or defects. Any defects should be fixed as narrow follow-up slices rather than reopening the completed redesign scope.
