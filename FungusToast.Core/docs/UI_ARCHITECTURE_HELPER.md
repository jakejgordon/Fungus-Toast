# Unity UI Architecture Helper

> **📚 Related Documentation**: For animation timing, see [second-level/ANIMATION_HELPER.md](second-level/ANIMATION_HELPER.md). For game flow and runtime architecture, see [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md). For visual tokens, component recipes, and screen-level styling rules, see [UI_STYLE_GUIDE.md](UI_STYLE_GUIDE.md). For tooltip taxonomy and when to use onboarding vs hover vs board-inspection tooltips, see [../../docs/ui/TOOLTIP_GUIDE.md](../../docs/ui/TOOLTIP_GUIDE.md). For naming new Unity sprites, icons, and other source assets, see [second-level/UNIT_ASSET_NAMING_CONVENTIONS.md](second-level/UNIT_ASSET_NAMING_CONVENTIONS.md). For board-background silhouette authoring and contour-to-square baking, see [NEW_BACKGROUND_HELPER.md](NEW_BACKGROUND_HELPER.md). For the full documentation hierarchy, see [README.md](README.md).

This document describes the established UI patterns in FungusToast.Unity. Follow these conventions when adding or modifying UI components.

When adding new imported UI art such as button icons or sprite assets, follow [second-level/UNIT_ASSET_NAMING_CONVENTIONS.md](second-level/UNIT_ASSET_NAMING_CONVENTIONS.md). Keep script and prefab naming on the existing project conventions.

---

## Service Extraction Pattern

`GameManager` delegates cohesive clusters of logic to lightweight service classes under `Assets/Scripts/Unity/Services/`. This keeps GameManager thin and testable.

### Existing Services

| Service | Responsibility |
|---------|----------------|
| `EndgameService` | Endgame detection, countdown tracking, final results display |
| `MutationPointService` | Mutation point assignment per round, AI mutation spending |

### Creating a New Service

1. Create a class in `Assets/Scripts/Unity/Services/`.
2. Accept all dependencies through the constructor using `Func<>` delegates — **never** pass `GameManager` directly.
3. Wire the service in `GameManager.BootstrapServices()`.
4. Delegate the relevant `GameManager` methods to the new service.

```csharp
// Example: Service accepts dependencies as Func<> delegates
public class MyNewService
{
    private readonly Func<GameBoard> getBoard;
    private readonly Func<Player?> getHumanPlayer;

    public MyNewService(Func<GameBoard> getBoard, Func<Player?> getHumanPlayer)
    {
        this.getBoard = getBoard;
        this.getHumanPlayer = getHumanPlayer;
    }

    public void DoWork()
    {
        var board = getBoard();
        // ...
    }
}
```

---

## Tooltip System

For the broader tooltip/guidance taxonomy, including onboarding coachmarks and board-inspection tooltips, see `../../docs/ui/TOOLTIP_GUIDE.md`.

The **standard hover-tooltip system** uses `ITooltipContentProvider`, `TooltipTrigger`, `TooltipManager`, and `TooltipView`. **Do not create standalone hover-tooltip implementations.**

### How It Works

1. **`ITooltipContentProvider`** — Interface that any MonoBehaviour implements to supply tooltip text.
2. **`TooltipTrigger`** — Attach to any GameObject with a provider; handles pointer enter/exit events.
3. **`TooltipManager`** — Singleton that routes show/hide requests.
4. **`TooltipView`** — The runtime tooltip instance; supports fade-in/out animation (0.15 s default).

### Adding a Tooltip to a New Component

```csharp
using FungusToast.Unity.UI.Tooltips;

public class MyWidget : MonoBehaviour, ITooltipContentProvider
{
    public string GetTooltipText()
    {
        return "Description of this widget";
    }

    private void Awake()
    {
        // TooltipTrigger auto-wires to this ITooltipContentProvider
        gameObject.AddComponent<TooltipTrigger>();
    }
}
```

### Existing Tooltip Providers

| Component | Location |
|-----------|----------|
| `MycovariantTooltipTrigger` | `UI/MycovariantTooltipTrigger.cs` |
| `PlayerMoldIconHoverHandler` | `UI/PlayerMoldIconHoverHandler.cs` |
| `CellTooltipUI` | `UI/CellTooltipUI.cs` |

### Mutation Workspace Inspector

The full-screen mutation workspace uses a persistent right-side inspector for decision-critical mutation information. It is not a hover tooltip and should not be implemented through `TooltipManager`.

- `MutationInspectorPanel` is built once at runtime by `UI_MutationManager`; it uses semantic style tokens and introduces no scene/prefab reference.
- Mutation cards do not create hover tooltips. Hover updates the persistent inspector and contextual dependency overlay; the inspector is the sole detailed mutation-information surface.
- Hover inspects a mutation and remembers it as the fallback inspector selection. Switching between nodes uses a short hover-intent delay; leaving a node keeps the last inspector content visible but clears an unpinned relationship overlay, and moving into the inspector preserves the current preview so the pointer can reach its controls without the content changing underneath it.
- The inspector exposes an explicit `Pin` / `Pinned` control for players who want to freeze the current mutation against later hover previews. Clicking a mutation card or purchasing it remembers that mutation without hard-pinning it; requirement/direct-unlock chips replace and pin the focused mutation while still scrolling to its related node.
- Authored simple/technical/max-level/synergy text comes from Core `MutationDescriptionSections`; level/cost/prerequisite/dependent facts come from `MutationProgressSnapshot` plus the existing mechanic-specific level summary.
- Full purchase eligibility remains in `Player.CanUpgrade` and the established Unity availability checks. The inspector must not duplicate or override gameplay rules.
- Named sets and aggregate gates remain Core-owned requirements but are grouped for presentation. Mycotropic Induction shows one `All four Directional Tendrils` summary with its four real, focusable mutation chips beneath it. Category-investment gates show one non-clickable foundation summary plus per-category owned/required progress from `MutationProgressSnapshot`; they must not create synthetic graph edges.
- Search and Pin controls live at the top of the inspector. Search is transient and isolates matches with a dedicated focus border while reducing nonmatches to faint graph context; it never changes availability or interaction. The inspector always shows authored technical details when present; mutation cards remain compact and do not duplicate summary prose beneath their names.

### Mutation Dependency Graph

- `MutationDependencyGraphGraphic` is a runtime-built, non-raycasting viewport overlay above the shared mutation scroll content. `UI_MutationManager` owns its creation, data binding, inspection state, scroll-driven geometry refresh, and one-shot unlock traces.
- Edges are derived only from registered Core `Mutation.Prerequisites`; do not author a second Unity dependency list. Repository integrity tests remain the guard against missing references and cycles.
- The graph is outside horizontal layout and renders above lane/card graphics while remaining non-raycasting. It may redraw after layout/inspection or scroll-value changes, during a brief inspector-navigation route emphasis, and during the short unlock trace, but must remain idle otherwise.
- Dependency edges are contextual rather than permanently visible. Recursive upstream routes point toward the focused mutation in amber; direct downstream routes point away in blue/green. Arrowheads carry direction, direct relationships are strongest, and deeper upstream levels progressively fade and narrow.
- Solid routes mean the named prerequisite level is met; dashed routes mean it is unmet. Cross-category grafts add corner knots so category crossing remains distinguishable without reusing the met/unmet dash language. The inspector must explicitly state that multiple requirements are conjunctive.
- Aggregate requirements remain inspector-only. They may affect eligibility and grouped progress but must never create synthetic graph edges.
- `MutationTreeBuilder` may visually group related nodes, as with the 2×2 Directional Tendrils card, but each quadrant must remain a real `MutationNodeUI` backed by its original Core mutation ID. Visual grouping must never introduce aggregate purchase or save state.
- `MycelialBackdropGraphic` is a static, non-raycasting viewport child. Keep substrate grain/hyphae sparse and mesh-based; decorative mutation-workspace visuals must not add continuous animation, per-frame allocation, or authored gameplay state.

---

## Attention Pulses

Short, one-shot emphasis animations that answer "why did nothing happen?" or "look here". Used by the panel-open affordable shimmer, the blocked-investment pulse (`MutationNodeUI.PlayInsufficientPointsAttentionPulse` / `PlayLockedAttentionPulse`, driven from `PlayBlockedInvestmentFeedbackIfNeeded`; `UI_MutationManager.PulseUnmetPrerequisitesFor`), and the growth-cycle progress pulse (`UI_PhaseProgressTracker`).

Conventions for any new one:

- **One rise-and-fall sweep, never a loop.** Drive it from a single `Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI)` strength (0 → 1 → 0). A persistent loop is reserved for genuine ongoing state (e.g. the purchasable-prerequisite outline) and causes attention fatigue everywhere else.
- **Unscaled time** (`Time.unscaledDeltaTime` / `Time.unscaledTime`) so it plays during pauses and time-lapse; **shorten** it (not skip) when `GameManager.IsFastRoundPresentationMode`.
- **Pair motion with a semantic color**, never color alone — scale punch + a brief `UIStyleTokens.State.*` tint that settles back. The literal label ("NEED POINTS", "LOCKED") is a third, redundant cue.
- **Non-blocking**, re-triggerable (a repeat click replays it), and self-restoring: capture base scale/color, and reset both plus re-run the authoritative refresh (`UpdateDisplay()`) on completion and in `OnDisable` so a killed coroutine can't freeze an element mid-pulse.
- **Emphasize the specific blocker, and emphasize it in both places it lives**: the element under the cursor (the clicked lock / the cost badge) *and* the thing that resolves it (the unmet prerequisite node). Leave states the player cannot act on here alone (maxed, pending-next-round, active surge, no-target).
- Durations and peak scales are constants in `UIEffectConstants` (`MutationNodeBlockedInvestmentPulse*`, `GrowthCycleProgressPulse*`).
- **Click detection gotcha:** `MutationNodeUI` is on the card root, which has no `Graphic`. The child upgrade `Button` is a `Selectable` (an `IPointerDownHandler`), so `ExecuteHierarchy` stops there and the root's `OnPointerDown` never fires for a normal click. Clicks that must be seen even when the Button is non-interactable (the blocked-investment feedback) are relayed by `BlockedInvestmentClickForwarder`, a runtime component added to the Button GameObject; a real upgrade click is filtered out with a `Time.frameCount` marker set in `OnUpgradeClicked`.

---

## Object Pooling

Use `UnityEngine.Pool.ObjectPool<T>` for UI elements that are frequently created and destroyed (log entries, list rows, etc.). This avoids GC pressure and frame hitches.

### Pattern

1. Add a pool field and initialize in `Awake()`.
2. Add a `ResetForReuse()` method to the pooled component.
3. Replace `Instantiate()` → `pool.Get()` and `Destroy()` → `pool.Release()`.

```csharp
using UnityEngine.Pool;

public class MyListPanel : MonoBehaviour
{
    [SerializeField] private MyListEntry entryPrefab;
    [SerializeField] private Transform contentParent;

    private ObjectPool<MyListEntry> entryPool;

    private void Awake()
    {
        entryPool = new ObjectPool<MyListEntry>(
            createFunc:      () => Instantiate(entryPrefab, contentParent),
            actionOnGet:     entry => entry.gameObject.SetActive(true),
            actionOnRelease: entry => { entry.ResetForReuse(); entry.gameObject.SetActive(false); },
            actionOnDestroy: entry => Destroy(entry.gameObject),
            defaultCapacity: 20,
            maxSize: 40
        );
    }

    private void AddEntry(string text)
    {
        var entry = entryPool.Get();
        entry.Configure(text);
    }

    private void RemoveEntry(MyListEntry entry)
    {
        entryPool.Release(entry);
    }
}
```

### `ResetForReuse()` Convention

Every pooled component **must** implement a `ResetForReuse()` method that:
- Stops all running coroutines on that object.
- Clears text fields, images, and dynamic state.
- Resets CanvasGroup alpha to 1 (if applicable).

### Current Pooled Components

| Component | Pool Location |
|-----------|---------------|
| `UI_GameLogEntry` | `UI_GameLogPanel.cs` |

---

## GameUIManager Façade

`GameUIManager` is a lightweight façade that UI scripts use instead of reaching into `GameManager.Instance`. This reduces coupling and makes panels easier to test.

### Usage

```csharp
// GOOD — panels reference the façade
var board = gameUIManager.Board;

// BAD — panels reach into the singleton
var board = GameManager.Instance.Board;
```

### SetDependencies Pattern

Panels that need callbacks or references receive them via a `SetDependencies()` method called during initialization. This avoids constructor/Awake timing issues.

```csharp
public class UI_EndGamePanel : MonoBehaviour
{
    private GameUIManager? gameUI;
    private Action? onCampaignResume;
    private Action? onExitToModeSelect;

    public void SetDependencies(GameUIManager ui, Action resumeCampaign, Action exitToModeSelect)
    {
        gameUI = ui;
        onCampaignResume = resumeCampaign;
        onExitToModeSelect = exitToModeSelect;
    }
}
```

### Current Façade Properties

| Property / Method | Description |
|-------------------|-------------|
| `Board` | Returns the current `GameBoard` (set via `SetBoard()`) |
| Panel references | Direct references to all major UI panels |

---

## Responsive Sidebar Sizing

`SidebarResizer` keeps sidebar panels at a fixed fraction of canvas width. It uses `OnRectTransformDimensionsChange()` to react to window resizes and CanvasScaler-aware width calculations.

### Key Implementation Details

- Uses `rootCanvas.scaleFactor` to convert screen pixels to Canvas units.
- Caches last width to avoid redundant layout rebuilds.
- Marked `[RequireComponent(typeof(RectTransform))]`.
- `targetWidthFraction` is configurable per sidebar (default 0.215f).

---

## Board Background Variants

Board backgrounds are authored through `BoardMediumConfig` in `FungusToast.Unity/Assets/Scripts/Unity/Grid/BoardMediumConfig.cs`.
The current toast configuration asset lives at `FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset`.

### Authoring Rules

1. Keep the board medium as the theme-level owner (`toast`, future surfaces, etc.).
2. Use the medium's default background fields for the primary image.
3. Add future alternate images through `boardBackgroundOverrides`, ordered from smallest / most specific match to broadest fallback.
4. Size rules are inclusive min/max width/height thresholds: an override matches when `boardWidth >= minBoardWidth`, `boardHeight >= minBoardHeight`, `boardWidth <= maxBoardWidth`, and `boardHeight <= maxBoardHeight`.
5. Every bread-photo background should also have a matching `boardBackgroundSpriteMetadata` entry for that sprite.
6. Treat `visibleAlphaBoundsNormalized` as the measured source-of-truth envelope for the sprite's visible non-transparent pixels.
7. Treat `boardBoundsNormalized` as an optional override, not a default requirement. It is a high-risk knob because it replaces normal inset composition unless `composeSafeAreaWithBoardBoundsMetadata` is enabled.
8. Prefer starting from visible-alpha fitting plus light per-band insets. Only add `boardBoundsNormalized` after visual verification proves the sprite needs a different canonical playable footprint.
9. Keep `backgroundScaleMultiplier` near `1.0` unless a specific image still needs render framing adjustment after safe-area tuning. It is another high-risk knob because it changes background placement and mask derivation together.
10. Use `composeSafeAreaWithBoardBoundsMetadata` only when a size-specific override needs extra inset inside a deliberately verified `boardBoundsNormalized` footprint instead of replacing that footprint.
11. For bread-photo boards, prefer the shared alpha-mask path:
   - enable `deriveBlockedTilesFromBackgroundAlpha`
    - keep blocker sampling and sprite placement aligned to the same effective safe area, then build the runtime `SpriteMask` from the live blocked-tile footprint so mold/overlay visuals follow the actual playable silhouette instead of the raw sprite alpha
    - when `backgroundMaxTileClipFraction` is `0.0`, treat that as strict no-overrun clipping; do not add a center-sample fallback that can re-allow partially outside tiles
    - inscribe the requested board aspect ratio inside visible-alpha-derived safe areas before placement/mask sampling so square boards do not inherit rectangular alpha slack
    - start with `backgroundMaxTileClipFraction: 0.0`, `backgroundTileClipSampleResolution: 5`, and `backgroundScaleMultiplier: 1.0`, then only retune safe-area insets before considering anything looser
12. When alpha-derived geometry keeps drifting from the actual intended board shape, add explicit authored shape metadata for that sprite instead of continuing to infer the shape from art alpha.
13. Use `hasPlayableEllipse` when the intended shape is genuinely ellipse-like, use an authored horizontal-span profile when the shape needs stable row-wise trimming such as cheese, and use `bakedBlockedTileMasks` when an irregular silhouette needs an exact contour bake against a deliberate square gameplay envelope. See `NEW_BACKGROUND_HELPER.md` for the baked-mask workflow.
14. Keep tooling and runtime texture sampling in the same Y-axis orientation. Unity texture space is effectively bottom-origin for this workflow, so any validator, preview, or bake step that reads PNG rows top-origin will silently mirror asymmetric contours vertically and produce believable-but-wrong blocked footprints.

### Add A New Background

1. Import the sprite and keep it square unless there is a clear reason not to.
2. Open `FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset`.
3. If the new image is the general fallback background, replace the medium's default `backgroundSprite`. If it only applies to a size band, add or update a `boardBackgroundOverrides` entry in the correct matching order.
4. Enable `deriveBlockedTilesFromBackgroundAlpha` for bread-photo backgrounds unless the image is intentionally using explicit geometry instead. If a background needs explicit geometry, prefer `hasPlayableEllipse` for circular shapes, an authored horizontal-span profile for stable row-wise silhouettes, or `bakedBlockedTileMasks` for irregular contour-baked boards.
5. Add a `boardBackgroundSpriteMetadata` entry for the sprite.
6. Set `visibleAlphaBoundsNormalized` to the measured normalized bounds of the sprite's visible non-transparent pixels.
7. Leave `boardBoundsNormalized` off unless visual verification proves the playable board needs a different canonical footprint than the visible-alpha envelope.
8. If `boardBoundsNormalized` is added, verify it deliberately and remember that ordinary inset fields stop affecting runtime placement unless `composeSafeAreaWithBoardBoundsMetadata` is also enabled.
9. Start new alpha-mask backgrounds from the current tuning baseline: `backgroundMaxTileClipFraction: 0.0`, `backgroundTileClipSampleResolution: 5`, and `backgroundScaleMultiplier: 1.0`.
10. If the image still needs fit adjustment, prefer retuning `backgroundInset*Normalized`, edge-fade values, or override bands before introducing `boardBoundsNormalized` or a non-`1.0` scale multiplier.
11. If the new background should participate in background-themed startup flavor text, update the mapping logic in `FungusToast.Unity/Assets/Scripts/Unity/GameManager.cs` as well. The current non-campaign intro generator keys off `ResolveBoardThemeFlavor()` and the resolved background sprite name, so a newly added bread photo will otherwise fall back to generic wording.
12. Run `python3 scripts/validate_board_backgrounds.py` after asset edits, then still do the in-Unity visual pass before considering the change done.
13. If the sprite needs a centered square gameplay envelope with exact conservative trimming, follow `NEW_BACKGROUND_HELPER.md` instead of hand-authoring blocked IDs or continuing to widen insets.
14. When investigating a localized overhang on an asymmetric board, compare it against the opposite vertical edge of the source art before retuning insets. If the bad region echoes the top contour at the bottom or vice versa, suspect a Y-axis mismatch in bake/validation first.

### Metadata Field Intent

- `visibleAlphaBoundsNormalized`: the measured raw visible-pixel envelope for the sprite.
- `boardBoundsNormalized`: an optional hard override for the playable-board footprint inside the sprite. For alpha-masked backgrounds, this footprint may intentionally extend beyond the visible art when you want a larger behind-the-scenes square and expect alpha masking to trim the overhang. On non-square sprites, author it from the intended pixel-space footprint rather than by forcing equal normalized width and height.
- `hasPlayableEllipse` + `playableEllipseCenterNormalized` + `playableEllipseRadiiNormalized`: optional explicit geometric shape metadata. When present, runtime placement and blocked-tile derivation use the ellipse bounds instead of visible-alpha fitting for that sprite.
- `hasPlayableHorizontalSpanProfile` + `playableHorizontalSpanProfile`: optional explicit non-elliptical shape metadata. Each stop defines a normalized board-space row and the playable horizontal span for that row, which lets runtime interpolate asymmetric shapes such as cheese's side trims and top-left notch.
- `bakedBlockedTileMasks`: optional exact blocked-tile footprints for specific board sizes. Use these when a sprite needs a reproducible explicit contour bake rather than a parametric ellipse/profile.
- Baked-mask tooling, stored metadata, and runtime blocked-tile sampling must all agree on row orientation. If one stage flips Y while the others do not, the serialized mask can validate structurally yet still clip the wrong side of an asymmetric sprite.
- If `boardBoundsNormalized` exists, runtime safe-area resolution uses it first. The older inset fields only compose inside it when `composeSafeAreaWithBoardBoundsMetadata` is enabled.
- If `boardBoundsNormalized` is absent but `visibleAlphaBoundsNormalized` exists, the configured safe area is composed inside the visible-alpha bounds instead of the full `0..1` sprite rect.
- If `hasPlayableEllipse` is present, that ellipse becomes the source-of-truth footprint for placement and mask derivation, and the configured safe area composes inside the ellipse bounds.
- If `hasPlayableHorizontalSpanProfile` is present, blocked-tile derivation evaluates that authored board-space profile directly instead of sampling sprite alpha, while placement can still use `boardBoundsNormalized` to position the art against the same intended square footprint.
- The renderer can also draw a very faint production playable-area overlay derived from the same live blocked-tile footprint, so any guidance tint stays aligned with real growth boundaries rather than inferred sprite alpha.

### Current Small-Board Pattern

- `ToastBoardMedium.asset` keeps the bread image as the default background.
- Boards `20x20` and smaller automatically switch to the seeded cracker image through the first size override.
- Boards `40x40` and smaller automatically switch to the plain cracker image unless a smaller override matched first.
- Boards `80x80` and smaller automatically switch to the cheese image unless a smaller override matched first.
- Boards `100x100` and larger automatically switch to the pita image through a min-bound override, leaving bread as the fallback band between the cheese and pita thresholds.
- This applies to campaign presets and development/testing board-size overrides without additional preset wiring.
- White bread, seeded cracker, and plain cracker still use the shared alpha-mask fitting rule.
- Cheese uses explicit authored horizontal-span profile metadata with vertical min/max bounds so it can keep a larger square placement footprint while trimming the left/right edges, top-left notch, and top/bottom bands deliberately across the cheese size band.
- Pita now uses explicit stored ellipse metadata so the square gameplay board, the blocked-tile footprint, and the rendered background all read the same authored circular shape.
- Shaped photo boards can also enable a very faint playable-area overlay tint in `BoardMediumConfig`, and that overlay is generated from the live playable footprint rather than a separate authored shape.

### Import Guidance

- Keep board background sprites square unless there is a clear gameplay-presentational reason not to.
- Match the established sprite import baseline where possible, especially pixels-per-unit and filtering behavior, so world-space fit remains predictable.
- If a new image has different composition or margins, create a new override entry and retune safe-area insets rather than reusing bread values verbatim.

### Validation Checklist

- Confirm the sprite is referenced by either the medium default background or the intended override band.
- Confirm the sprite also has a `boardBackgroundSpriteMetadata` entry.
- Confirm the override ordering still matches from narrowest / most specific band to broadest fallback.
- If the background should have custom startup-banner flavor text, confirm `FungusToast.Unity/Assets/Scripts/Unity/GameManager.cs` still recognizes it in `ResolveBoardThemeFlavor()`.
- Run `python3 scripts/validate_board_backgrounds.py` and fix any metadata or footprint errors it reports.
- Do an in-Unity visual pass at the target board sizes and verify all of the following agree with the intended silhouette:
  - background placement
    - faint playable-area overlay
  - blocked-tile footprint
  - hover / magnifier hit-testing
  - highlight / ping clipping
  - board-edge fade
- If the image is replacing or overlapping an existing size band, quickly compare neighboring board sizes too so no transition band regresses.

---

## Resizable Window

The Unity Player window is configured as resizable (`resizableWindow: 1` in `ProjectSettings.asset`), allowing users to resize the game window at runtime. The `SidebarResizer` and CanvasScaler (Scale With Screen Size, 1920×1080 reference, 0.5 match) handle responsive layout automatically.

---

## Quick Reference: What Pattern to Use

| Scenario | Pattern |
|----------|---------|
| New tooltip on a component | Implement `ITooltipContentProvider` + add `TooltipTrigger` |
| New list/log that creates many entries | `ObjectPool<T>` + `ResetForReuse()` |
| GameManager method cluster is growing | Extract into `Services/` class with `Func<>` dependencies |
| UI panel needs GameManager data | Add property to `GameUIManager`, use `SetDependencies()` |
| New sidebar or resizable panel | Use `SidebarResizer` component with `targetWidthFraction` |
| One-shot "look here" / "why did nothing happen?" emphasis | Follow the **Attention Pulses** conventions above |

_End of UI Architecture Helper._
