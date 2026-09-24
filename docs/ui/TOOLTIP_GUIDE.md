# Tooltip Guide

Use this document when deciding **what kind of tooltip or guidance UI to add** in Fungus Toast.

For general Unity UI architecture, also see `../../FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`.

## 1. Tooltip / Guidance Types

### A. New-player onboarding tooltips
**Purpose:** teach first-time or returning players something important about the game flow.

**Examples:**
- welcome coachmark (centered, round 1)
- Spend Points coachmark
- mutation-workspace guidance modal
- scoreboard win-condition coachmark
- camera movement coachmark

**Current source of truth:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Onboarding/NewPlayerTooltipCatalog.cs`

This onboarding/coaching system is split across two adjacent code surfaces:
- `NewPlayerTooltipCatalog.cs` holds the canonical list of new-player coaching tooltips, including id, copy, display surface, seen key, and a human-readable trigger summary.
- `NewPlayerTooltipRules` in the same file holds the executable trigger predicates that decide whether each coaching tooltip should appear.

If you need to answer "where do the existing coaching tooltips live?", start with that file.

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

**What this tooltip cannot do:**
`TooltipView` is a single shared instance that sets `blocksRaycasts = false` whenever it is shown,
and its whole body is one `TextMeshProUGUI`. So it can never contain anything hoverable or
clickable, and a tooltip opened from inside it would evict itself through the manager's
`NotifyTooltipReleased()` handshake. If the content needs interactive parts, it needs a real panel
— see section F.

**Click-to-pin:**
`TooltipTrigger.SetPinOnClick(true)` makes clicking the element hold the tooltip open past pointer
exit. Only one tooltip exists per session, so when another source takes the shared view the manager
calls `NotifyTooltipReleased()` on the previous trigger — do not add pin state that bypasses that
handshake. A pinned trigger appends its own “Pinned / Click to pin” hint line, so the affordance
comes for free.

**Player summary mold icon:**
Deliberately has *no* `TooltipTrigger`. Its only inspection surface is the player inspector panel
(section F), which handles both hover and click. An earlier version layered the two, and hovering
one row stacked the text tooltip over a panel pinned on another. Player detail content lives in
`FungusToast.Unity/Assets/Scripts/Unity/UI/PlayerInspector/PlayerInspectorContent.cs`, which returns
presentation-neutral sections rendered by `PlayerInspectorMarkup`.

`BuildDevelopmentSections` is appended only when the Development Testing toggle is on, and carries
AI tuning parameters (strategy identity, max tier, economy bias, priority categories, surge
frequency, excluded mutations, mycovariant plan vs. actual drafts). Nothing in it is player-facing
copy.

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
- every coachmark card is built by `CoachmarkLayoutUtility.BuildCard` and is draggable (grip glyph, move cursor, lift while dragging); a host only chooses size, pivot, and where to anchor it, and leaves a card alone once `Draggable.HasBeenMoved` is set. See `UI_STYLE_GUIDE.md` section 5.11.
- a card is either a *HUD* card (teaches the board or always-on HUD; drawn beneath the mutation tree) or an *Overlay* card (teaches a control drawn over the HUD; drawn on top). While a modal task owns the player's attention - today, the mycovariant draft, including any tile placement a pick starts - it calls `CoachmarkLayoutUtility.HoldHudCoachmarks` and every HUD card steps off screen; `ReleaseHudCoachmarks` brings back the ones still unread when the task closes. A held card still reports `IsVisible`, so hosts neither re-show nor treat it as dismissed, and a HUD card shown during the hold waits for the release. Only one teaching layer is ever asking for attention.

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

---

### F. Interactive inspector panels

**Purpose:** the same explanatory content as a hover tooltip, but with parts the player can
interact with — icons to hover, links to click, controls to use.

**Current systems/files:**
- `FungusToast.Unity/Assets/Scripts/Unity/UI/PlayerInspector/PlayerInspectorPanel.cs` — the
  player inspector, previewed by hovering a scoreboard mold icon and pinned by clicking it
- `FungusToast.Unity/Assets/Scripts/Unity/UI/PlayerInspector/PlayerInspectorLauncher.cs` — the
  hover/click handler on the icon that drives it
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/MutationInspectorPanel.cs` — the docked
  mutation inspector

**Trigger model:**
- hover shows a **preview** after the standard tooltip delay: the same panel, but with its
  `CanvasGroup` raycasts off, so it behaves like a tooltip — it cannot block board cells or
  placement clicks, and closes on pointer exit
- click **pins** it: raycasts turn on and the trait icons become hoverable. While pinned, hovering
  other icons does nothing (like the mutation tree’s Pin); clicking another icon moves the pin,
  clicking the same one or the close button unpins
- never make the hover preview interactive. A raycast-blocking panel that appeared on hover over the
  live board would break cell hover, placement clicks, and its own pointer-exit

**Use this when:**
- the content needs hoverable or clickable parts (the hover tooltip cannot host them at all)
- the player will read it for more than a moment, so it must survive pointer-exit

**How the player inspector is built:**
It renders `PlayerInspectorContent` sections with `includeTraitLines: false`, then shows owned
adaptations and mycovariants as real icon tiles from `CompactIconTileFactory` — the same tiles the
mold profile sidebar uses. Each tile carries its own `AdaptationTooltipProvider` /
`MycovariantTooltipProvider` plus a `TooltipTrigger`, so hovering one explains that trait. Nested
tooltips work here precisely because the panel is *not* the shared tooltip view.

The panel repositions against its anchor every `LateUpdate`, because the scoreboard re-sorts its
rows by rank. Content re-derives on a timer rather than per frame, and the icon grids only rebuild
when the owned-trait signature changes.

If you add another interactive inspector, reuse `CompactIconTileFactory` for icon tiles rather than
hand-rolling the tile geometry a third time.

---

## 2. Decision Rules

When adding explanatory UI, use this decision order:

1. **Is this first-time-player teaching?**
   - Use the onboarding catalog/rules system.
2. **Is this explanation tied to hovering a specific control or icon?**
   - Use the standard hover tooltip pipeline.
3. **Is this explanation about a live toast cell or board state?**
   - Use the cell/board inspection tooltip path.
4. **Does the explanation itself need hoverable or clickable parts?**
   - Use an interactive inspector panel. The shared hover tooltip cannot host them.
5. **Is this a transient status message rather than a tooltip?**
   - Use the appropriate toast/banner/panel system.

## 3. Current New-player Onboarding Inventory

These currently live in `NewPlayerTooltipCatalog.cs`:
- `WelcomeIntro`
- `SpendMutationPointsIntro`
- `MutationWorkspaceIntro`
- `TimeLapseModeIntro`
- `BankMutationPointsIntro`
- `ScoreboardWinCondition`
- `InspectPlayersIntro`
- `AdaptationPanelIntro`
- `CameraPanIntro`
- `MycovariantDraftIntro`
- `AutoPlacementIntro`
- `EndgameCountdownIntro`

Each onboarding entry should define:
- stable id
- seen key
- title/body copy
- display surface
- trigger summary

### Human-readable coaching tooltip inventory

Use this section when you want a quick description of what already exists without reading the code first.

| Tooltip | Surface | When it appears |
| --- | --- | --- |
| `WelcomeIntro` | screen-center coachmark | Shown centered on screen about four seconds into round 1 (after the game-start title card clears) for a human player, unless the game is fast-forwarding. Like `CameraPanIntro` it is not suppressed by plain testing mode; outside forced first-game experience it only shows once per profile. It is the first thing a new player reads: the round-1 `SpendMutationPointsIntro` and `CameraPanIntro` coachmarks wait until it is closed, then follow in sequence. Clicking Spend Points while it is open closes it and skips the Spend Points coachmark for that game. Presented by `NewPlayerWelcomeCoachmark.cs`, owned by `GameManager`. |
| `SpendMutationPointsIntro` | sidebar coachmark | Shown beside the Spend Points button during round 1 for a human player, unless the game is fast-forwarding or `WelcomeIntro` is still open (it appears as soon as that closes). Outside forced first-game experience, it is also suppressed in testing mode and after being seen before. Clicking Spend Points acknowledges and closes it. |
| `MutationWorkspaceIntro` | mutation tree modal | Shown the first time the player opens the mutation tree by clicking Spend Points. It combines spending and inspector guidance in one modal positioned toward the inspector side of the workspace. It is suppressed while fast-forwarding, then suppressed for the current game and, outside forced first-game experience, shown once per profile. |
| `TimeLapseModeIntro` | sidebar coachmark | Shown from round 4 onward at the start of the human mutation phase, hanging below the `Pace: Normal / Time-Lapse` toggle in the sidebar's top row, unless the player already dismissed it that game or the game is fast-forwarding. Switching Time-Lapse on retires it for good. Outside forced first-game experience, it only shows once per profile. |
| `BankMutationPointsIntro` | mutation tree coachmark | Shown when the mutation tree opens on round 6 or later, unless the player already dismissed it that game or the game is fast-forwarding. Outside forced first-game experience, it only shows once per profile. |
| `ScoreboardWinCondition` | sidebar coachmark | Shown from round 2 onward, unless the player already dismissed it that game or the game is fast-forwarding. Outside forced first-game experience, it only shows once per profile. |
| `InspectPlayersIntro` | sidebar coachmark | Shown from round 8 onward — deliberately the last of the fixed-round hints, after the round 1–4 sidebar/mold-profile coachmarks and the round-6 mutation tree one, and before the round-15 draft intro, so it never shares a round with another. Suppressed while fast-forwarding, after dismissal this game, and for a player who already pinned the inspector this game. Pinning the inspector marks it seen for good. Outside forced first-game experience, it only shows once per profile. |
| `AdaptationPanelIntro` | mold profile coachmark | Shown from round 3 onward when the adaptations section is visible, unless the player already dismissed it that game or the game is fast-forwarding. Outside forced first-game experience, it only shows once per profile. |
| `CameraPanIntro` | board coachmark | Titled "Pan and Zoom the Board". Shown during round 1 half a second after `WelcomeIntro` closes, for a human player who has not already dismissed it and has not yet moved or zoomed the camera. It is suppressed while fast-forwarding and otherwise only shows once per profile outside forced first-game experience. |
| `MycovariantDraftIntro` | draft coachmark | Shown the first time the Mycovariant draft panel opens, unless the player already dismissed it that game or the game is fast-forwarding. Outside forced first-game experience, it only shows once per profile. |
| `AutoPlacementIntro` | selection-prompt coachmark | Shown the first time an active Mycovariant offers the Auto Placement button (for example, Mycelial Bastion), unless the game is fast-forwarding. It is marked seen when displayed, so it may first appear several games into a profile but never repeats automatically. |
| `EndgameCountdownIntro` | sidebar coachmark | Shown the first time the endgame countdown begins, unless the player already dismissed it that game or the game is fast-forwarding. Outside forced first-game experience, it only shows once per profile. It retires itself (counted as dismissed) when the countdown next advances, handing off to the sidebar countdown. |

For exact gating conditions, prefer `NewPlayerTooltipRules` over this prose summary.

## 4. Authoring Rules

- Do **not** scatter new-player onboarding copy and seen keys inline across random controllers.
- Prefer adding new first-time guidance to `NewPlayerTooltipCatalog.cs` and `NewPlayerTooltipRules`.
- Keep trigger logic readable and named.
- If a new onboarding item should be reset by the Settings menu’s replay option, it must be represented in the onboarding catalog.
- If you add, remove, rename, or materially retime a new-player coaching tooltip, update this guide so the human-readable inventory stays in sync with `NewPlayerTooltipCatalog.cs` and `NewPlayerTooltipRules`.
- If you introduce a brand-new tooltip category, update this guide and link the relevant implementation files.

## 5. Quick “Which one should I use?” examples

- **“Explain what this button does when hovered.”** → hover tooltip
- **“Teach new players what the scoreboard means.”** → onboarding coachmark
- **“Teach new players how to move around the board.”** → onboarding coachmark
- **“Show details for the tile under the mouse.”** → cell/board inspection tooltip
- **“Let the player hover the icons inside an explanation.”** → interactive inspector panel
- **“Announce a phase or status change.”** → informational banner/toast
- **“Teach the player a system the first time they encounter it.”** → onboarding catalog entry
