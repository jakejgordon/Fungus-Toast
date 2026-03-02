# UI Polish Task List

Purpose: persistent tracker for UX/style-guide implementation across chat sessions.
Source of truth for style decisions: [UI_STYLE_GUIDE.md](UI_STYLE_GUIDE.md).

## Current Status

- [x] Chunk 1: Add centralized UI style tokens
- [x] Chunk 2: Adopt tokenized button styles in pre-game flow
- [x] Chunk 2b: Apply visible pre-game panel/text surfaces
- [x] Chunk 3: Tooltip + help affordance styling (implementation complete)
- [ ] Chunk 4: In-game sidebars + HUD hierarchy
- [ ] Chunk 5: Game logs (global + human)
- [ ] Chunk 6: Mutation tree alignment
- [ ] Chunk 7: Overlays/endgame/hotseat/loading

## Session Handoff Notes

### Completed in recent sessions
- Added token infrastructure in Unity UI scripts.
- Added pre-game button styling and fixed selected-number readability.
- Added visible pre-game surface/text theming for mode select, campaign, and start panel.
- Added tooltip runtime theming for tooltip background/text and mycovariant tooltip panel.
- Verified C# compile after each chunk.

### Next Chunk (start here)
- Chunk 4: In-game sidebars + HUD hierarchy (style-only, no behavior change).
- Target files:
  - FungusToast.Unity/Assets/Scripts/Unity/UI/UI_RightSideBar.cs
  - FungusToast.Unity/Assets/Scripts/Unity/UI/UI_MoldProfileRoot.cs
  - FungusToast.Unity/Assets/Scripts/Unity/UI/UI_PhaseProgressTracker.cs

## Verification Checklist (run after each chunk)

- [ ] Unity compile succeeds
- [ ] Affected screens visually reviewed in Play Mode
- [ ] No gameplay flow changes introduced
- [ ] Contrast/readability acceptable for changed labels
- [ ] Regression check for selected/disabled states

## Pending Visual Verification

- Chunk 3 requires Unity Play Mode verification for tooltip readability/contrast on mutation and help surfaces.

## Working Rules

- Keep changes in small chunks that can be visually verified quickly.
- Prefer style-only diffs; avoid mixing gameplay logic changes.
- Use semantic style tokens; avoid introducing new ad hoc colors.
- Update this file at the end of every chunk.
