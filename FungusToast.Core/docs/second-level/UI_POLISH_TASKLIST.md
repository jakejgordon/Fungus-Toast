# UI Polish Task List

Purpose: persistent tracker for UX/style-guide implementation across chat sessions.
Source of truth for style decisions: [../UI_STYLE_GUIDE.md](../UI_STYLE_GUIDE.md).

## Current Status

- [x] Chunk 1: Add centralized UI style tokens
- [x] Chunk 2: Adopt tokenized button styles in pre-game flow
- [x] Chunk 2b: Apply visible pre-game panel/text surfaces
- [x] Chunk 3: Tooltip + help affordance styling (implementation complete)
- [x] Chunk 4: In-game sidebars + HUD hierarchy (implementation complete)
- [x] Chunk 5: Game logs (global + human) (implementation complete)
- [x] Chunk 6: Mutation tree alignment (implementation complete)
- [x] Chunk 7: Overlays/endgame/hotseat/loading (implementation complete)

## Session Handoff Notes

### Completed in recent sessions
- Added token infrastructure in Unity UI scripts.
- Added pre-game button styling and fixed selected-number readability.
- Added visible pre-game surface/text theming for mode select, campaign, and start panel.
- Added tooltip runtime theming for tooltip background/text and mycovariant tooltip panel.
- Added sidebar/HUD theming for right sidebar, phase tracker, phase banner, and player summary row text hierarchy.
- Added game log theming: panel surfaces, header/clear button styling, and unified per-category text/background colors.
- Aligned mutation tree palette and action controls with global style tokens; removed hardcoded node tooltip/badge/border colors.
- Added overlay/endgame theming for endgame panel, hotseat turn prompt, loading screen, and endgame result rows.
- Verification/tuning pass started: replaced remaining high-visibility hardcoded rich-text colors in mutation points preview, random decay tooltip title, and growth surge bonus text.
- Verified C# compile after each chunk.

### Next Chunk (start here)
- Full-playthrough visual verification + token tuning pass.
- Target areas:
  - Pre-game + campaign flow
  - Tooltips + logs
  - Right sidebar + mutation tree
  - Endgame/hotseat/loading overlays

## Verification Checklist (run after each chunk)

- [ ] Unity compile succeeds
- [ ] Affected screens visually reviewed in Play Mode
- [ ] No gameplay flow changes introduced
- [ ] Contrast/readability acceptable for changed labels
- [ ] Regression check for selected/disabled states

## Pending Visual Verification

- Chunk 3 requires Unity Play Mode verification for tooltip readability/contrast on mutation and help surfaces.
- Chunk 4 requires Unity Play Mode verification for sidebar/HUD contrast and text hierarchy.
- Chunk 5 requires Unity Play Mode verification for log readability and category contrast consistency.
- Chunk 6 requires Unity Play Mode verification for mutation tree contrast, category accents, and node tooltip readability.
- Chunk 7 requires Unity Play Mode verification for overlay readability and button prominence in endgame/hotseat/loading prompts.

## Working Rules

- Keep changes in small chunks that can be visually verified quickly.
- Prefer style-only diffs; avoid mixing gameplay logic changes.
- Use semantic style tokens; avoid introducing new ad hoc colors.
- Update this file at the end of every chunk.
