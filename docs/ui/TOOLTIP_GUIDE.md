# Tooltip Guide

Use this document when deciding **what kind of tooltip or guidance UI to add** in Fungus Toast.

For general Unity UI architecture, also see `../../FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`.

## 1. Tooltip / Guidance Types

### A. New-player onboarding tooltips
**Purpose:** teach first-time or returning players something important about the game flow.

**Examples:**
- mutation phase intro banner
- mutation tree first-open guidance
- scoreboard win-condition coachmark
- camera movement coachmark

**Current source of truth:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Onboarding/NewPlayerTooltipCatalog.cs`

**Trigger model:**
- rule-driven
- often once per profile
- may ignore persisted seen-state during forced first-game experience
- may also have a per-session dismissal guard

**Use this when:**
- the player needs help learning a system
- the message is tied to first-time understanding, not hover inspection
- you want the content to be discoverable and resettable from the settings flow

**Do not use this for:**
- ordinary hover descriptions
- board-state inspection
- tiny context hints that should appear every time

---

### B. Hover tooltips for buttons, icons, cards, and widgets
**Purpose:** explain what a specific UI element does or means when the player inspects it.

**Current system:**
- `ITooltipContentProvider`
- `TooltipTrigger`
- `TooltipManager`
- `TooltipView`

**Primary reference:**
- `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`

**Trigger model:**
- pointer hover / focus / inspection
- not persisted as seen/unseen onboarding state

**Use this when:**
- the player is asking “what is this control / icon / card?”
- the explanation should be available any time the element is present

---

### C. Toast cell / board inspection tooltips
**Purpose:** show live game-state details for a hovered board cell or tile.

**Current system/files:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/CellTooltipUI.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MagnifyingGlassFollowMouse.cs`

**Trigger model:**
- board hover / magnifier interaction
- fully state-driven, not onboarding-driven

**Use this when:**
- the content is about a live tile, cell, or board object
- the player is inspecting current game state rather than learning a UI control

---

### D. Coachmarks / modal guidance panels
**Purpose:** pull focused attention to one important area of the UI.

**Examples:**
- scoreboard “How to Win” panel
- mutation tree first-open modal guidance toast

**Current note:**
- these are still part of the broader onboarding family when they teach first-time concepts
- their content should live in `NewPlayerTooltipCatalog.cs` if they are new-player guidance

**Use this when:**
- hover is too subtle
- the player must notice a specific concept before continuing comfortably

---

### E. Non-tooltip informational toasts / banners
**Purpose:** transient status messaging, not inspection.

**Examples:**
- phase banners
- temporary informational popups
- result/status confirmations

**Use this when:**
- the message is event feedback rather than explanation-on-inspect

If it is specifically first-time teaching content, still store the copy and seen-state in the onboarding catalog even if the presentation surface is a banner.

## 2. Decision Rules

When adding explanatory UI, use this decision order:

1. **Is this first-time-player teaching?**
   - Use the onboarding catalog/rules system.
2. **Is this explanation tied to hovering a specific control or icon?**
   - Use the standard hover tooltip pipeline.
3. **Is this explanation about a live toast cell or board state?**
   - Use the cell/board inspection tooltip path.
4. **Is this a transient status message rather than a tooltip?**
   - Use the appropriate toast/banner/panel system.

## 3. Current New-player Onboarding Inventory

These currently live in `NewPlayerTooltipCatalog.cs`:
- `AlphaMutationPhaseIntro`
- `MutationTreeGuidance`
- `TimeLapseModeIntro`
- `ScoreboardWinCondition`
- `CameraPanIntro`
- `MycovariantDraftIntro`
- `EndgameCountdownIntro`

Each onboarding entry should define:
- stable id
- seen key
- title/body copy
- display surface
- trigger summary

## 4. Authoring Rules

- Do **not** scatter new-player onboarding copy and seen keys inline across random controllers.
- Prefer adding new first-time guidance to `NewPlayerTooltipCatalog.cs` and `NewPlayerTooltipRules`.
- Keep trigger logic readable and named.
- If a new onboarding item should be reset by the Settings menu’s replay option, it must be represented in the onboarding catalog.
- If you introduce a brand-new tooltip category, update this guide and link the relevant implementation files.

## 5. Quick “Which one should I use?” examples

- **“Explain what this button does when hovered.”** → hover tooltip
- **“Teach new players what the scoreboard means.”** → onboarding coachmark
- **“Teach new players how to move around the board.”** → onboarding coachmark
- **“Show details for the tile under the mouse.”** → cell/board inspection tooltip
- **“Announce a phase or status change.”** → informational banner/toast
- **“Teach the player a system the first time they encounter it.”** → onboarding catalog entry
