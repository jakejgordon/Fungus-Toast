# TOOLTIP_HELPER.md

Succinct guide to add a new tooltip (uGUI + TextMeshPro)

1) One-time setup (scene)
- Under your main UICanvas, create UI_TooltipSystem.
- Add TooltipManager and assign TooltipPrefab (the panel prefab asset).
- Make UI_TooltipSystem the last child so tooltips render on top.

2) Build TooltipPrefab (panel)
- Hierarchy: TooltipPrefab (root) → Text (TextMeshProUGUI).
- Root components:
  - Image (panel) Type = Simple or Sliced (Fill Center ON), visible color (e.g., #15181DCC).
  - CanvasGroup.
  - LayoutElement.
  - ContentSizeFitter (Horizontal/Vertical Fit = Preferred).
  - TooltipView (assign: text = child TMP, canvasGroup = root, layoutElement = root).
- Text child:
  - TMP: Rich Text ON, Word Wrapping ON, Font Size ~14–16 (or Auto Size 12–18).
  - RectTransform: stretch with padding via parent or add VerticalLayoutGroup on root if preferred.

3) Static tooltip on any UI element
- Add TooltipTrigger to the UI element.
- Set Static Text in the inspector (supports TMP rich text).
- Optional: set maxWidth (e.g., 380–420) and useCustomDelay.

4) Dynamic tooltip (computed at show-time)
- Create a MonoBehaviour implementing ITooltipContentProvider:
  public class MyTooltip : MonoBehaviour, ITooltipContentProvider {
    public string GetTooltipText() => "<b>Title</b>\nExplanation...";
  }
- Add both TooltipTrigger and your provider to the same GameObject.
- Leave Static Text empty so the trigger uses the provider.
- If your provider needs runtime refs (e.g., Player), expose an Initialize(...) method and call it from the owning UI panel after data is available (example: UI_MoldProfileRoot.Initialize wires its providers).

5) Help icon (optional prefab)
- Root (24x24): Image (circle sprite), Button (tint states optional), LayoutElement (Preferred 24×24), TooltipTrigger (isHelpIcon = true).
- Child: TextMeshProUGUI with "?", centered.
- Place next to the label; attach a provider or set Static Text.

6) Positioning & sizing
- TooltipManager.offset controls distance from anchor (e.g., 16, -16).
- TooltipTrigger.maxWidth controls wrap width; height auto-sizes via ContentSizeFitter.
- Keep UI_TooltipSystem under the main UICanvas (not inside masked/scroll views) to avoid clipping. If using multiple canvases, give UI_TooltipSystem a Canvas with higher Sorting Order and a matching CanvasScaler.

7) Troubleshooting
- Nothing shows: TooltipManager missing prefab, script disabled, or instance not parented under UICanvas.
- Only a thin strip: add VerticalLayoutGroup or ensure ContentSizeFitter is on the root and Text wraps; Image Type must be Simple/Sliced (not Filled).
- Rounded corners: you used a rounded sprite—swap to UI/Sprite (square) or a non-rounded 9-slice.
- Dynamic text not used: ensure a component implementing ITooltipContentProvider is on the same object/children; TooltipTrigger will auto-find it.

That’s it. Reuse the help icon + provider pattern anywhere in the UI.
