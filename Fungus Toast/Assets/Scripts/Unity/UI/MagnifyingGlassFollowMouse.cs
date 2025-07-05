using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class MagnifyingGlassFollowMouse : MonoBehaviour
{
    // Assign this in the Inspector to your GridVisualizer instance
    public FungusToast.Unity.Grid.GridVisualizer gridVisualizer;

    // Assign this in the Inspector to the 'MagnifierVisuals' child GameObject
    public GameObject visualRoot;

    // Static flag to indicate if the game has started
    public static bool gameStarted = false;

    void Update()
    {
        // Always move the magnifying glass to the mouse position (screen space)
        transform.position = Input.mousePosition;

        // Only show visuals if the game has started and mouse is over the bread
        if (gameStarted && IsMouseOverBread())
        {
            if (visualRoot != null && !visualRoot.activeSelf)
                visualRoot.SetActive(true);
        }
        else
        {
            if (visualRoot != null && visualRoot.activeSelf)
                visualRoot.SetActive(false);
        }
    }

    bool IsMouseOverBread()
    {
        if (gridVisualizer == null || gridVisualizer.toastTilemap == null)
            return false;

        // 1. Convert mouse position to world point
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 2. Convert world point to cell position
        Vector3Int cellPos = gridVisualizer.toastTilemap.WorldToCell(mouseWorld);

        // 3. Check if the cell is within the board bounds (has a tile)
        return gridVisualizer.toastTilemap.HasTile(cellPos);
    }
} 