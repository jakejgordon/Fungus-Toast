using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Unity.UI;
using System.Collections;
using TMPro;

public class MagnifyingGlassFollowMouse : MonoBehaviour
{
    [Header("Grid Reference")]
    // Assign this in the Inspector to your GridVisualizer instance
    public FungusToast.Unity.Grid.GridVisualizer gridVisualizer;

    [Header("Visual Components")]
    // Assign this in the Inspector to the 'MagnifierVisuals' child GameObject
    public GameObject visualRoot;

    [Header("Tooltip Prefab")]
    // Assign the tooltip prefab here - will be instantiated dynamically
    public GameObject tooltipPrefab;
    
    [Header("Tooltip Settings")]
    public float hoverDelaySeconds = 0.2f;
    public Vector2 tooltipOffset = new Vector2(150f, 30f);

    [Header("Debug")]
    public bool enableDebugLogs = false;

    // Static flag to indicate if the game has started
    public static bool gameStarted = false;

    // Private state for tooltip management
    private Vector3Int lastHoveredCellPos = Vector3Int.zero;
    private bool isHoveringOverValidCell = false;
    private Coroutine hoverCoroutine;
    private bool isTooltipVisible = false;
    private Coroutine fadeCoroutine;
    
    // Dynamic tooltip instance and components
    private GameObject tooltipInstance;
    private RectTransform tooltipRectTransform;
    private CanvasGroup tooltipCanvasGroup;
    private CellTooltipUI tooltipUI;

    void Start()
    {
        // Tooltip will be created dynamically when needed
    }

    void Update()
    {
        // Always move the magnifying glass to the mouse position (screen space)
        transform.position = Input.mousePosition;

        // Only show visuals if the game has started, mouse is over the bread, and NOT over UI
        bool shouldShowMagnifier = gameStarted && IsMouseOverBread() && !EventSystem.current.IsPointerOverGameObject();
        
        if (shouldShowMagnifier)
        {
            if (visualRoot != null && !visualRoot.activeSelf)
                visualRoot.SetActive(true);
            
            // Handle tooltip logic
            Vector3Int currentCellPos = GetCurrentCellPosition();
            if (currentCellPos != lastHoveredCellPos)
            {
                // Moved to a different cell - reset hover state
                OnCellHoverChanged(currentCellPos);
                lastHoveredCellPos = currentCellPos;
            }
        }
        else
        {
            if (visualRoot != null && visualRoot.activeSelf)
                visualRoot.SetActive(false);
            
            // Hide tooltip when magnifier is not visible
            if (isTooltipVisible)
                HideTooltip();
            
            // Reset hover state
            if (isHoveringOverValidCell)
            {
                isHoveringOverValidCell = false;
                if (hoverCoroutine != null)
                {
                    StopCoroutine(hoverCoroutine);
                    hoverCoroutine = null;
                }
            }
        }
    }

    bool IsMouseOverBread()
    {
        if (gridVisualizer == null || gridVisualizer.toastTilemap == null)
            return false;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = gridVisualizer.toastTilemap.WorldToCell(mouseWorld);
        return gridVisualizer.toastTilemap.HasTile(cellPos);
    }

    Vector3Int GetCurrentCellPosition()
    {
        if (gridVisualizer == null || gridVisualizer.toastTilemap == null)
            return Vector3Int.zero;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return gridVisualizer.toastTilemap.WorldToCell(mouseWorld);
    }

    void OnCellHoverChanged(Vector3Int newCellPos)
    {
        // Stop any existing hover coroutine
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        // Hide tooltip immediately when changing cells
        if (isTooltipVisible)
            HideTooltip();

        // Start new hover timer for the new cell
        isHoveringOverValidCell = true;
        hoverCoroutine = StartCoroutine(HoverDelay(newCellPos));
    }

    IEnumerator HoverDelay(Vector3Int cellPos)
    {
        yield return new WaitForSeconds(hoverDelaySeconds);
        
        // Only show tooltip if still hovering over the same cell
        if (isHoveringOverValidCell && cellPos == lastHoveredCellPos)
        {
            ShowTooltipForCell(cellPos);
        }
        
        hoverCoroutine = null;
    }

    void ShowTooltipForCell(Vector3Int cellPos)
    {
        if (tooltipPrefab == null || FungusToast.Unity.GameManager.Instance == null)
            return;

        // Get the tile ID and cell data
        var board = FungusToast.Unity.GameManager.Instance.Board;
        int tileId = cellPos.y * board.Width + cellPos.x;
        var cell = board.GetCell(tileId);

        // Only show tooltip if the cell is occupied (has a FungalCell)
        if (cell == null)
            return;

        // Create tooltip instance if it doesn't exist
        if (tooltipInstance == null)
        {
            CreateTooltipInstance();
        }

        // Activate tooltip and set as last sibling for proper rendering order
        tooltipInstance.SetActive(true);
        tooltipInstance.transform.SetAsLastSibling();

        // Update tooltip content using the CellTooltipUI component
        if (tooltipUI != null)
        {
            tooltipUI.UpdateTooltip(cell, board, gridVisualizer);
        }

        // Position the tooltip
        PositionTooltip();

        // Fade in the tooltip
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        if (tooltipCanvasGroup != null)
        {
            fadeCoroutine = StartCoroutine(FadeTooltip(1f, 0.15f));
        }

        isTooltipVisible = true;
    }

    void CreateTooltipInstance()
    {
        // Find the UI Canvas
        Canvas uiCanvas = FindFirstObjectByType<Canvas>();
        if (uiCanvas == null)
        {
            Debug.LogError("[Tooltip] No Canvas found! Tooltip cannot be created.");
            return;
        }

        // Instantiate tooltip prefab under the canvas
        tooltipInstance = Instantiate(tooltipPrefab, uiCanvas.transform);
        
        // Get components from the instantiated tooltip
        tooltipRectTransform = tooltipInstance.GetComponent<RectTransform>();
        tooltipCanvasGroup = tooltipInstance.GetComponent<CanvasGroup>();
        tooltipUI = tooltipInstance.GetComponent<CellTooltipUI>();

        // Add CanvasGroup if it doesn't exist
        if (tooltipCanvasGroup == null)
        {
            tooltipCanvasGroup = tooltipInstance.AddComponent<CanvasGroup>();
        }

        // Configure CanvasGroup
        tooltipCanvasGroup.alpha = 0f;
        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;

        // Start hidden
        tooltipInstance.SetActive(false);

        if (enableDebugLogs)
            Debug.Log($"[Tooltip] Created tooltip instance from prefab: {tooltipInstance.name}");
    }

    void PositionTooltip()
    {
        if (tooltipRectTransform == null)
            return;

        Vector3 mousePos = Input.mousePosition;
        Vector2 tooltipSize = tooltipRectTransform.sizeDelta;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        // Calculate position to the right of the mouse with offset
        Vector3 tooltipPos = mousePos + new Vector3(tooltipOffset.x, tooltipOffset.y, 0);
        
        // Center the tooltip vertically with the mouse cursor (magnifying glass circle)
        // Subtract half the tooltip height to center it
        tooltipPos.y -= tooltipSize.y * 0.5f;
        
        // If tooltip would go off right edge of screen, position it to the left instead
        if (tooltipPos.x + tooltipSize.x > screenSize.x)
        {
            tooltipPos = mousePos + new Vector3(-tooltipOffset.x - tooltipSize.x, tooltipOffset.y, 0);
            // Center vertically for left-side positioning too
            tooltipPos.y -= tooltipSize.y * 0.5f;
        }
        
        // Ensure tooltip stays within screen bounds
        tooltipPos.x = Mathf.Clamp(tooltipPos.x, 0, screenSize.x - tooltipSize.x);
        tooltipPos.y = Mathf.Clamp(tooltipPos.y, 0, screenSize.y - tooltipSize.y);

        tooltipRectTransform.anchoredPosition = new Vector2(tooltipPos.x, tooltipPos.y);
    }

    void HideTooltip()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (tooltipCanvasGroup != null)
        {
            fadeCoroutine = StartCoroutine(FadeTooltip(0f, 0.1f));
        }
        else if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(false);
        }
        
        isTooltipVisible = false;
    }

    IEnumerator FadeTooltip(float targetAlpha, float duration)
    {
        if (tooltipCanvasGroup == null) 
            yield break;

        float startAlpha = tooltipCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            tooltipCanvasGroup.alpha = newAlpha;
            yield return null;
        }

        tooltipCanvasGroup.alpha = targetAlpha;

        // If fading out, deactivate the panel
        if (targetAlpha <= 0f && tooltipInstance != null)
        {
            tooltipInstance.SetActive(false);
        }

        fadeCoroutine = null;
    }

    void OnDisable()
    {
        // Clean up when disabled
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        HideTooltip();
    }

    void OnDestroy()
    {
        // Clean up tooltip instance when this object is destroyed
        if (tooltipInstance != null)
        {
            Destroy(tooltipInstance);
        }
    }
}