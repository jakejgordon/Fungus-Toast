# Unity UI Architecture Helper

> **📚 Related Documentation**: For animation timing, see [ANIMATION_HELPER.md](ANIMATION_HELPER.md). For game flow and phases, see [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md). For the full documentation hierarchy, see [README.md](README.md).

This document describes the established UI patterns in FungusToast.Unity. Follow these conventions when adding or modifying UI components.

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

All tooltips use a single unified system based on `ITooltipContentProvider`, `TooltipTrigger`, `TooltipManager`, and `TooltipView`. **Do not create standalone tooltip implementations.**

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
| `MutationNodeUI` | `UI/MutationTree/MutationNodeUI.cs` |
| `MycovariantTooltipTrigger` | `UI/MycovariantTooltipTrigger.cs` |
| `PlayerMoldIconHoverHandler` | `UI/PlayerMoldIconHoverHandler.cs` |
| `CellTooltipUI` | `UI/CellTooltipUI.cs` |

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

_End of UI Architecture Helper._
