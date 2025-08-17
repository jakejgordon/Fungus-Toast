# TOOLTIP_HELPER.md

> Runtime tooltip + help icon system guide (uGUI + TextMeshPro)

This document explains how to (1) build / maintain the shared tooltip system and (2) add new static or dynamic Help Icons anywhere in the UI.

---
## 1. System Overview

Implemented components (namespace `FungusToast.Unity.UI.Tooltips`):

| Component | Purpose |
|-----------|---------|
| `TooltipManager` | Singleton that owns & reuses a single tooltip instance. Handles delayed show, positioning, clamping. |
| `TooltipView` | Mono on the tooltip prefab responsible for assigning text + sizing. |
| `TooltipTrigger` | Attach to any UI element to request a tooltip (static text or dynamic provider). Supports hover (mouse) + tap toggle (mobile help icon). |
| `ITooltipContentProvider` | Interface for dynamic (computed at show‑time) content. |
| `HomeostaticHarmonyTooltipProvider` | Example dynamic provider (mutation stat breakdown). |
| `TooltipRequest` | Lightweight struct bundling parameters for a show request. |

Key characteristics:
- Exactly one tooltip shown at a time.
- Text supports TMP Rich Text tags (color, bold, italics, size, etc.).
- Delay before showing (default 0.38s) to reduce noise.
- Automatic screen edge clamping (keeps panel onscreen with padding).
- Anchors near the source element (top‑right corner by default) + offset.
- Mobile friendly: normal elements show on hover (desktop); Help Icons (isHelpIcon) toggle on tap (mobile long‑press not yet needed).

---
## 2. Tooltip Prefab Specification

Prefab name: `TooltipPrefab` (assign it to `TooltipManager.tooltipPrefab`).

Hierarchy:
```
TooltipPrefab (root)
  Text (TextMeshProUGUI)
```
Root (TooltipPrefab):
- Components: `Image` (9‑slice or solid panel), `CanvasGroup`, `LayoutElement`, `ContentSizeFitter (Preferred/Preferred)`, `TooltipView`.
- RectTransform pivot (0,1) (top‑left) – matches positioning logic.
- Image: dark semi‑transparent color e.g. #15181DCC (or #20252BCC), Raycast Target OFF.
- LayoutElement: left default (preferred width set at runtime when `MaxWidth` provided).
- ContentSizeFitter: HorizontalFit = PreferredSize, VerticalFit = PreferredSize.

Child Text:
- TMP Text: Rich Text ON, Wrapping ON, Alignment Upper Left, Raycast Target OFF.
- RectTransform: Anchors stretch to parent (Left/Right/Top/Bottom = 8) for padding.

Wire `TooltipView` fields:
- `text` → Text child
- `background` → root RectTransform (optional)
- `canvasGroup` → root CanvasGroup
- `layoutElement` → root LayoutElement

Optional polish: add TMP Outline / Shadow, subtle panel border sprite.

---
## 3. TooltipManager Placement

Create an empty GameObject under the main UI Canvas (e.g., `TooltipSystem`) and add `TooltipManager`. Assign the `TooltipPrefab` to its `tooltipPrefab` field. Only one manager should exist.

---
## 4. Adding a Static Tooltip
1. Select target UI element.
2. Add `TooltipTrigger`.
3. In the inspector, put multi‑line text into *Static Text* field.
4. (Optional) Adjust `maxWidth` (defaults to 400). Leave dynamic provider blank.
5. Play: hover shows tooltip after delay.

---
## 5. Adding a Dynamic Tooltip (Help Icon or Inline)
**Workflow:**
1. Implement a provider class:
```csharp
public class MyStatTooltipProvider : MonoBehaviour, ITooltipContentProvider
{
    private Player player; private List<Player> players;
    public void Initialize(Player p, List<Player> all) { player = p; players = all; }
    public string GetTooltipText()
    {
        int lvl = player.GetMutationLevel(MutationIds.SomeMutation);
        float per = GameBalance.SomeMutationEffectPerLevel * 100f;
        return $"<b>Some Mutation</b>\nLevel {lvl}: <color=#FFD769>{lvl * per:0.##}%</color> bonus";    }
}
```
2. Add both `TooltipTrigger` and your provider to the same GameObject.
3. Clear the Static Text field (leave empty) so the trigger uses the dynamic provider.
4. After UI instantiation, call `provider.Initialize(player, board.Players)` (typically where the surrounding panel is initialized).
5. Hover / tap to test: provider executes once per show.

---
## 6. Help Icon Prefab (Reusable)
Hierarchy:
```
HelpIcon (root, 24x24)
  ? (TMP Text)
```
Root components:
- `Image` (circle sprite) – Type Simple, Color #2C2F36.
- `Button` (optional for focus/keyboard + tint states).
- `LayoutElement` (Preferred Width/Height = 24) if inside LayoutGroups.
- `TooltipTrigger` (check `isHelpIcon = true`).
Child TMP Text: "?", centered, white (#FFFFFF), Raycast Target OFF.

ColorBlock suggestion:
- Normal: #2C2F36
- Highlighted: #39404A
- Pressed: #1F2328
- Selected: #39404A
- Disabled: #2C2F3666 (alpha 40%)
- Color Multiplier: 1, Fade Duration: 0.12

Sprite: provide a small 64x64 filled circle PNG (no built‑in perfect circle). Import as Sprite (2D).

Using in scene:
1. Drag prefab next to a stat label.
2. Add / configure provider (or set static text on trigger).
3. On mobile, tapping toggles visibility (because `isHelpIcon = true`).

---
## 7. Integrating Homeostatic Harmony Example
Location of provider: `Assets/Scripts/Unity/UI/Tooltips/HomeostaticHarmonyTooltipProvider.cs`.
Attach provider + trigger to (for example) the UI element showing "Homeostatic Reduction".
In `UI_MoldProfileRoot.Initialize` or `Refresh` (after player references exist):
```csharp
var provider = harmonyIconGO.GetComponent<HomeostaticHarmonyTooltipProvider>();
provider.Initialize(trackedPlayer, allPlayers);
```
Leave `TooltipTrigger.staticText` blank.

---
## 8. Common Pitfalls & Fixes
| Issue | Cause | Fix |
|-------|-------|-----|
| Tooltip off-screen | Large width, near edge | Auto clamp already; adjust `offset`/`maxWidth`. |
| Help icon circle distorts | Parent LayoutGroup forcing stretch | Add `LayoutElement` (preferred=24x24) & disable Control Child Size / Force Expand. |
| Sprite missing after drop | Prefab never had sprite (or Revert clicked) | Open prefab, assign sprite, Apply. |
| Tooltip never shows | Missing `TooltipManager` or no prefab assigned | Add manager + assign prefab. |
| Dynamic text stale | Provider caches values | Recompute in `GetTooltipText()` (avoid caching). |
| Flicker on rapid enter/exit | Very short delay & small element | Increase `TooltipManager.showDelay` (0.35–0.45). |
| Mobile tap shows then instantly hides | `isHelpIcon` not checked | Ensure help icon trigger has `isHelpIcon = true`. |

---
## 9. Extending (Future Enhancements)
Potential upgrades:
- Pointer follow (update anchored position in `LateUpdate` when `FollowPointer = true`).
- Rich content: replace single TMP with a vertical layout container & provider callback that populates pooled rows.
- Theming: scriptable theme asset (panel color, text style) swapped at runtime.
- Long press detection for non-help icons on touch (track pointer down time). 
- Fade animation: add coroutine to lerp `CanvasGroup.alpha` for show/hide.

---
## 10. Quick Checklist (Add New Dynamic Help Icon)
1. Place HelpIcon prefab where needed.
2. Add or reuse a dynamic provider implementing `ITooltipContentProvider`.
3. Initialize provider with required runtime references.
4. Ensure `TooltipTrigger` has static text blank (dynamic only) & `isHelpIcon` if appropriate.
5. Verify TooltipManager exists & prefab assigned.
6. Enter Play: hover (desktop) or tap (mobile) to test.

---
## 11. Minimal Static Usage Sample
```csharp
// Add via inspector: TooltipTrigger.staticText = "<b>Growth Chance</b>\nBase + mutation bonuses.";
```

## 12. Minimal Dynamic Usage Sample
```csharp
public class GrowthChanceTooltip : MonoBehaviour, ITooltipContentProvider
{
    private Player player; private List<Player> players;
    public void Init(Player p, List<Player> list){ player=p; players=list; }
    public string GetTooltipText()
    {
        float baseChance = GameBalance.BaseGrowthChance * 100f;
        float bonus = player.GetEffectiveGrowthChance()*100f - baseChance;
        return $"<b>Growth Chance</b>\nBase: {baseChance:0.###}%\nBonus: {bonus:0.###}%";
    }
}
```
Attach component + trigger; initialize after player set.

---
## 13. Maintenance Notes
- Keep only one `TooltipManager` in scene. Multiple = race conditions.
- Avoid heavy allocations in `GetTooltipText` (use a shared `StringBuilder` if called very frequently, but once per show is fine).
- Max width tuning: wider than ~420px reduces readability; prefer line breaks.
- If migrating to UI Toolkit later, you'll likely replace manager + triggers with built-in tooltip events.

---
**Done.** Use this guide whenever adding or refining tooltips / help icons.
