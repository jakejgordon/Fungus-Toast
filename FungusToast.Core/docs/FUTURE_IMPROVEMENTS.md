# Future Improvements

Tracked items for future sessions. Each entry includes context and recommended approach.

---

## Responsive / Resizable Panels

**Priority:** Medium  
**Category:** UX / Layout

Currently the application window is resizable (`ProjectSettings resizableWindow: 1`) and sidebars scale proportionally via `SidebarResizer` + `CanvasScaler`. However, individual panels (left sidebar, right sidebar, mutation tree) are **not** user-draggable or user-resizable at runtime.

### Recommended Approach
- Add drag handles to sidebar edges using `IBeginDragHandler` / `IDragHandler`.
- Clamp width between a min/max fraction of canvas width.
- Persist user-chosen widths in `PlayerPrefs`.
- Consider a split-pane pattern where dragging one panel edge adjusts the adjacent panel.

### Related Files
- `FungusToast.Unity/Assets/Scripts/Unity/UI/SidebarResizer.cs` — current proportional sizing
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameUIManager.cs` — panel references

---

## End-Game Dialog Visual Polish

**Priority:** High  
**Category:** UX / Visual

The end-game results panel has buttons that are hard to see and click (low contrast, small hit targets). This is an Inspector/scene styling issue, not a code issue.

### Recommended Steps (Unity Editor)
1. Select `exitButton` and `continueButton` GameObjects under `UI_EndGamePanel`.
2. Increase `RectTransform` height to 50–60 px (currently too small).
3. Set Button `ColorBlock.normalColor` to a visible contrasting color (e.g., `#4A90D9` for blue, `#D63A3A` for exit red).
4. Increase child `TextMeshProUGUI` font size to 18–20 px.
5. Add horizontal padding (10–15 px on each side) via `LayoutGroup` or anchors.
6. Consider adding a `Play Again` button alongside `Exit`.

### Related Files
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_EndGamePanel.cs`
- Scene: `FungusToast.Unity/Assets/Scenes/SampleScene.unity` (EndGamePanel hierarchy)

---

## Loading Screen Scene Wiring

**Priority:** High (required to see loading screen in-game)  
**Category:** Setup

The `UI_LoadingScreen` script and `GameUIManager` integration are complete in code, but the Panel must be created manually in the Unity Editor.

### Setup Steps
1. Open `SampleScene` in Unity Editor.
2. Right-click main Canvas → **UI → Panel**.
3. Name it `LoadingScreenPanel`.
4. Set anchors to full-stretch (all corners: min 0,0 / max 1,1; offsets all 0).
5. Add a **CanvasGroup** component (alpha = 1, blocks raycasts = true).
6. Attach the **UI_LoadingScreen** script.
7. Optionally add a child **TextMeshProUGUI** for the status label and assign it in the Inspector.
8. Set the Panel's sibling index to be **last** (renders on top of other panels).
9. Set a background color (e.g., the toast brown `#8B7355` with full alpha).
10. Drag the Panel into `GameUIManager` → **Loading Screen** inspector slot.

### Related Files
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_LoadingScreen.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameUIManager.cs`

---

## Scene Splitting (Monolithic Scene)

**Priority:** Low (high risk, needs reduced coupling first)  
**Category:** Architecture

`SampleScene.unity` is a single 127K-line file containing all UI panels, the game board, cameras, and managers. Splitting into separate scenes (menu, game, endgame) would improve load times and maintainability.

### Prerequisites
- GameManager.Instance coupling must be minimal (partially done).
- Services should be injectable, not scene-dependent.
- DontDestroyOnLoad strategy needed for persistent managers.

### Recommended Phased Approach
1. Extract menu/mode-select into its own scene.
2. Use `SceneManager.LoadScene` with additive loading for transitions.
3. Later: split endgame into an overlay scene.
4. Finally: separate game board into its own scene.

### Related Files
- `FungusToast.Unity/Assets/Scenes/SampleScene.unity`
- `FungusToast.Unity/Assets/Scripts/Unity/GameManager.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameUIManager.cs`
