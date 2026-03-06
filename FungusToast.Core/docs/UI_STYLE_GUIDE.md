# Fungus Toast UI Style Guide

> Purpose: Establish a consistent, moldy/fungal, desktop-first UX motif across all Unity UI surfaces.
> Audience: Human developers and GitHub Copilot.
> Scope (v1): Full UI coverage (menus, in-game HUD, mutation tree, logs, tooltips, overlays/endgame).
> Enforcement (v1): Soft guidance (strong defaults + checklist), not hard blockers.

## 1) Design Intent

### Brand Tone
- **Niche + geeky + fungal**: scientific-lab readability with organic mold accents.
- **Mood**: earthy, damp, spore-laden, strategic.
- **Readability priority**: gameplay data must remain immediately legible.

### Core UX Principles
1. **Clarity over decoration**: gameplay state is always more important than style flourish.
2. **Semantic consistency**: same meaning must use same colors/states everywhere.
3. **Progressive emphasis**: use strong accents only for important actions or alerts.
4. **Desktop-first layout**: optimize for mouse/keyboard and wide aspect ratios.
5. **Mobile-retrofit aware**: avoid tiny text and low-hit-target controls.

---

## 2) Canonical Token Dictionary (v1)

Use these names in docs, comments, and future theme assets.
Do not introduce ad hoc color names in new UI work.

### 2.1 Core Surfaces
- `Surface.Canvas`: `#2C3140` (global background/navy-earth anchor)
- `Surface.PanelPrimary`: `#3A4350` (sidebar + major panel)
- `Surface.PanelSecondary`: `#465164` (nested cards/sections)
- `Surface.PanelElevated`: `#576277` (hovered/active section)
- `Surface.OverlayDim`: `#12161DCC` (modal/overlay dim)

### 2.2 Fungal Accent Palette
- `Accent.Moss`: `#6D8F3A` (primary fungal accent)
- `Accent.Lichen`: `#8FAF52` (positive/selected)
- `Accent.Spore`: `#B3C77A` (soft highlight)
- `Accent.Hyphae`: `#D5DDB0` (subtle bright details)
- `Accent.Putrefaction`: `#7A5B3A` (warning-secondary, earthy)

### 2.3 Text Colors
- `Text.Primary`: `#F1F3EE`
- `Text.Secondary`: `#C9D0C2`
- `Text.Muted`: `#9BA392`
- `Text.Disabled`: `#7A8174`
- `Text.OnAccent`: `#1B2117`

### 2.4 Semantic States
- `State.Success`: `#8FAF52`
- `State.Info`: `#7EA4A6`
- `State.Warning`: `#B8924A`
- `State.Danger`: `#B45E5E`
- `State.Focus`: `#B3C77A`

### 2.5 Buttons (State Tokens)
- `Button.Bg.Default`: `#E7E8E5`
- `Button.Bg.Hover`: `#F4F5F2`
- `Button.Bg.Pressed`: `#D3D7C9`
- `Button.Bg.Selected`: `#8FD28A`
- `Button.Bg.Disabled`: `#B7BBB2`
- `Button.Text.Default`: `#34392E`
- `Button.Text.Disabled`: `#747A71`

### 2.6 Mutation Category Accents (Map Existing Categories)
- `Category.Growth`: `#5F8F61`
- `Category.CellularResilience`: `#5A7289`
- `Category.Fungicide`: `#6E5A86`
- `Category.GeneticDrift`: `#7D6B4E`
- `Category.MycelialSurges`: `#80607A`

### 2.7 Player Mold Icon Palette (Color-vision-safe)
- `Player.Blue`: `#0072D1`
- `Player.Orange`: `#FF8A00`
- `Player.Sky`: `#00AEEF`
- `Player.Purple`: `#8E5DFF`
- `Player.Yellow`: `#7EA000`
- `Player.Teal`: `#008F7A`
- `Player.Vermillion`: `#C73E1D`
- `Player.NeutralDead`: `#6F7680`

Current sprite mapping (64x64 mold set):
- `purple_mold_new_64x64.png` -> `Player.Blue`
- `orange_red_mold_64x64.png` -> `Player.Orange`
- `aqua_mold_64x64.png` -> `Player.Sky`
- `pink_mold_64x64.png` -> `Player.Purple`
- `yellow_mold_64x64.png` -> `Player.Yellow`
- `green_mold_64x64.png` -> `Player.Teal`
- `red_mold_64x64.png` -> `Player.Vermillion`
- `mold_dead_64x64.png` -> `Player.NeutralDead`

Notes:
- Category accents are for headers, chips, and small emphasis areas.
- Do not use category colors for body text contrast-critical content.

---

## 3) Typography (v1)

### Current Rule
- Keep current TMP default font for phase 1 to minimize risk.

### Usage Levels
- `Type.H1` (major title): 52–68 px
- `Type.H2` (screen title): 36–48 px
- `Type.H3` (section title): 26–32 px
- `Type.Body`: 20–24 px
- `Type.Caption`: 16–18 px
- `Type.Micro`: 14–16 px (avoid smaller unless non-interactive)

### Text Rules
- Use sentence case for instructional labels.
- Keep button labels short and verb-oriented.
- Avoid all-caps except tiny overline/meta labels.
- Avoid abbreviations when the full word fits in the available UI space; prefer full wording to reduce ambiguity (especially for non-native English readers).

---

## 4) Layout, Spacing, and Shape

### Spacing Scale
- `Space.2` = 2px
- `Space.4` = 4px
- `Space.8` = 8px
- `Space.12` = 12px
- `Space.16` = 16px
- `Space.24` = 24px
- `Space.32` = 32px

### Corner Radius
- `Radius.Small`: 4px (chips)
- `Radius.Medium`: 6px (buttons/cards)
- `Radius.Large`: 10px (major panels/overlays)

### Interaction Sizes (desktop-first, mobile-aware)
- Minimum click target: 36x36px (goal 40x40).
- Standard primary action button height: 52–64px.
- Avoid dense clusters of controls with <8px gap.

---

## 5) Component Recipes

### 5.1 Primary Buttons
- Background from `Button.Bg.*` tokens.
- Text from `Button.Text.*` tokens.
- Hover and pressed states must be visibly distinct.
- Selected state uses `Button.Bg.Selected`.

### 5.2 Secondary/Tertiary Buttons
- Prefer lower contrast fill with clear border/label.
- Reserve primary style for the main call to action only.

### 5.3 Panels and Sidebars
- Major sidebars: `Surface.PanelPrimary`.
- Nested containers/cards: `Surface.PanelSecondary`.
- Active/expanded subsections: `Surface.PanelElevated`.
- Keep 1–2 panel surface levels per region (avoid rainbow surfaces).

### 5.4 Tooltips
- Background: `Surface.PanelSecondary` with high text contrast.
- Header text: `Text.Primary`; body text: `Text.Secondary`.
- Semantic values may use `State.*` accents sparingly.

### 5.5 Mutation Nodes
- Keep category identity via `Category.*` accents.
- Locked/inactive states must prioritize readability and clear affordance.
- Upgradeable state should be obvious without relying only on hue.

### 5.6 Logs (Human + Global)
- Base rows on panel surfaces, not bright color blocks.
- Category/state highlights should be small strips/icons/text accents.
- Never reduce contrast in long-text rows for style reasons.

### 5.7 Overlays / Endgame / Prompts
- Use `Surface.OverlayDim` behind modal content.
- Primary result text must be high-contrast and concise.
- Reuse semantic tokens (`State.Success/Warning/Danger`) consistently.

---

## 6) Screen-by-Screen Rules (Full UI Pass)

### 6.1 Mode Select / Campaign / Start Setup
- Use one consistent panel + button language across all pre-game screens.
- Selected options should share one universal selected style.
- Explanatory helper text uses `Text.Secondary`.

### 6.2 In-Game HUD + Sidebars
- Left and right sidebars should share the same surface hierarchy.
- Round/phase/occupancy text should use consistent emphasis weights.
- Avoid introducing unique per-widget colors unless semantic.

### 6.3 Mutation Tree
- Keep category accents and dark tree baseline; align text/button states with global tokens.
- Store/bank actions use primary action style; less critical actions use secondary.

### 6.4 Tooltips + Logs
- Unify tooltip/log text sizing and line-height for readability.
- Ensure the same event type is colored the same in both logs.

### 6.5 Endgame + Hotseat + Loading
- Use same overlay, title, and button conventions as other major modal surfaces.
- Keep result hierarchy obvious: winner/outcome first, details second.

---

## 7) Accessibility Baseline (Practical WCAG-Aware)

- Body text on panel surfaces should target ~4.5:1 contrast where practical.
- Large headers should target ~3:1 minimum.
- Do not rely on red vs green alone to communicate outcome; use wording/icons/position.
- Disabled controls must look disabled and remain readable.
- Keyboard/gamepad support is future work, but new UI should not block it structurally.

---

## 8) Recommended Asset File Sizes (UI)

Use these as default targets for PNG UI art to balance visual quality, memory, and load time.

- **Small icons (inline, badges, helper icons):** source at `64x64` or `128x128`.
- **Standard UI icons/buttons (most HUD/menu icon use):** source at `256x256`.
- **Primary menu logo (typical start-screen display):** source at `256x256`.
- **Large hero/promo logo (full-width splash, zoomed usage, high-res capture):** source at `1024x1024`.

Practical rule:
- If the on-screen display is roughly `<= 300px` wide/tall, prefer `256x256`.
- If the on-screen display is frequently much larger than `300px`, use `1024x1024`.

Authoring/export notes:
- Keep UI textures to the smallest resolution that still looks crisp at target display size.
- Prefer a single canonical source file per use-case and avoid duplicate near-identical large files.
- Keep transparency clean (tight alpha edges) and avoid unnecessary empty transparent borders.

---

## 9) Copilot Authoring Rules (Important)

When Copilot edits Unity UI code/prefabs:
1. **Reuse existing UI systems first** (`GameUIManager`, tooltip stack, pooled log entries).
2. **Use semantic token names** from this guide in new style-related code/comments/docs.
3. **Avoid new raw color literals** in UI scripts unless unavoidable; if used, add TODO to migrate.
4. **No one-off button styles** if an existing style/prefab can be reused.
5. **Prefer prefab-level consistency** over scene-only overrides.
6. **Keep style changes separate from gameplay logic changes** whenever possible.
7. **Call out required Unity Editor steps** whenever script changes depend on prefab/scene/inspector updates (for example, LayoutGroup, ContentSizeFitter, anchors, or serialized references). Provide concrete follow-up instructions so UI architecture remains scalable and avoids fragile runtime-only hacks.

Soft guidance note:
- Existing legacy styles may remain temporarily, but all new UI work should move toward this guide.

---

## 10) Migration Playbook (Low Risk)

### Stage 1: Shared Reusables
- Normalize: common buttons, tooltip prefab, game log entry/panel, mutation node shells, endgame panel shell.

### Stage 2: Main User Flows
- Pre-game (mode/campaign/start), then in-game HUD + sidebars.

### Stage 3: Mutation Tree + Detail Surfaces
- Align tree states and action buttons with shared tokens.

### Stage 4: Script Hardcode Cleanup
- Replace local color literals and rich-text hex tags with semantic state references where feasible.

### Stage 5: Consistency + Readability Sweep
- Verify readability and state consistency across all major screens in a playtest pass.

---

## 11) Definition of Done for “Style Guide Adopted”

- All major UI surfaces are explicitly covered by this guide.
- New UI PRs reference semantic tokens and shared component recipes.
- No new ad hoc color systems are introduced.
- Visual language is recognizably consistent from start screens to in-game overlays.

---

## 12) Implementation Targets (Current Codebase Anchors)

Use these files as first-pass integration points:
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/MutationTreeColors.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameLog/GameLogColorSchemes.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UIEffectConstants.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Tooltips/TooltipView.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameStart/UI_StartGamePanel.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_ModeSelectPanelController.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_RightSideBar.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_EndGamePanel.cs`

These are guidance anchors; exact implementation sequence can vary by polish sprint.
