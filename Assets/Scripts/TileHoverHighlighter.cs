using UnityEngine;
using UnityEngine.Tilemaps;

public class TileHoverHighlighter : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap hoverTilemap;

    public Tile glowyTile;
    public Tile pressedTile;

    private Vector3Int? lastHoveredCell = null;
    private bool isAnimatingPress = false;  // NEW: flag to suppress hover

    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = groundTilemap.WorldToCell(mouseWorldPos);

        // Hover logic
        if (lastHoveredCell != null && lastHoveredCell != cellPos)
        {
            hoverTilemap.SetTile(lastHoveredCell.Value, null); // Clear previous
        }

        if (groundTilemap.HasTile(cellPos))
        {
            hoverTilemap.SetTile(cellPos, glowyTile);
            lastHoveredCell = cellPos;
        }

        // Click logic
        if (Input.GetMouseButtonDown(0) && groundTilemap.HasTile(cellPos))
        {
            TriggerPressedAnimation(cellPos);
        }
    }

    void TriggerPressedAnimation(Vector3Int cell)
    {
        Debug.Log($"Pressed tile triggered at: {cell}");

        isAnimatingPress = true;
        hoverTilemap.SetTile(cell, pressedTile);

        CancelInvoke(nameof(RestoreGlowyTile));
        Invoke(nameof(RestoreGlowyTile), 0.25f);  // Slightly longer for visibility
    }

    void RestoreGlowyTile()
    {
        if (lastHoveredCell != null)
        {
            hoverTilemap.SetTile(lastHoveredCell.Value, glowyTile);
        }
        isAnimatingPress = false;
    }
}
