using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Unity.UI;
using System.Collections;
using System;
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
    public Vector2 tooltipOffset = new Vector2(75f, 15f); // Reduced from 150f, 30f
    
    [Header("Magnifying Glass Settings")]
    [SerializeField] private bool autoDetectRadius = true; // Automatically detect radius from visual root
    [SerializeField] private float manualRadius = 128f; // Manual override if auto-detection fails

    [Header("Debug")]
    public bool enableDebugLogs = true; // Enable debugging by default

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
        
        // Auto-detect magnifying glass radius from visual root if enabled
        if (autoDetectRadius && visualRoot != null)
        {
            DetectMagnifyingGlassRadius();
        }
    }

    /// <summary>
    /// Automatically detects the magnifying glass radius from the visual root's Image component.
    /// Assumes the magnifying glass is a circular image and uses half the width as radius.
    /// </summary>
    void DetectMagnifyingGlassRadius()
    {
        // Look for Image component in visualRoot or its children
        Image magnifierImage = visualRoot.GetComponent<Image>();
        if (magnifierImage == null)
        {
            magnifierImage = visualRoot.GetComponentInChildren<Image>();
        }

        if (magnifierImage != null)
        {
            RectTransform rectTransform = magnifierImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Use half the width as radius (assuming square/circular image)
                float detectedRadius = rectTransform.sizeDelta.x * 0.5f;
                
                if (enableDebugLogs)
                    Debug.Log($"[Tooltip] Auto-detected magnifying glass radius: {detectedRadius} (from image size: {rectTransform.sizeDelta})");
                
                // Only update manual radius if it's currently at default value (128) or 0
                // This preserves any custom value set in the Inspector
                if (manualRadius <= 128f)
                {
                    manualRadius = detectedRadius;
                    if (enableDebugLogs)
                        Debug.Log($"[Tooltip] Updated manual radius to detected value: {manualRadius}");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log($"[Tooltip] Keeping custom manual radius: {manualRadius} (detected: {detectedRadius})");
                }
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[Tooltip] Could not auto-detect magnifying glass radius - no Image component found in visualRoot. Using manual radius: {manualRadius}");
        }
    }

    /// <summary>
    /// Gets the current magnifying glass radius, either auto-detected or manual override.
    /// </summary>
    float GetMagnifyingGlassRadius()
    {
        if (autoDetectRadius && visualRoot != null)
        {
            Image magnifierImage = visualRoot.GetComponent<Image>();
            if (magnifierImage == null)
                magnifierImage = visualRoot.GetComponentInChildren<Image>();

            if (magnifierImage != null)
            {
                RectTransform rectTransform = magnifierImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    return rectTransform.sizeDelta.x * 0.5f;
                }
            }
        }
        
        return manualRadius;
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
        if (enableDebugLogs)
            Debug.Log($"[Tooltip Debug] ShowTooltipForCell called for position: {cellPos}");

        if (tooltipPrefab == null)
        {
            Debug.LogError("[Tooltip] Tooltip prefab is not assigned! Please assign it in the Inspector.");
            return;
        }

        if (FungusToast.Unity.GameManager.Instance == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[Tooltip] GameManager.Instance is null!");
            return;
        }

        // Get the tile ID and cell data
        var board = FungusToast.Unity.GameManager.Instance.Board;
        int tileId = cellPos.y * board.Width + cellPos.x;
        var cell = board.GetCell(tileId);

        if (enableDebugLogs)
            Debug.Log($"[Tooltip Debug] Cell at {cellPos} (tileId: {tileId}): {(cell != null ? $"Found {(cell.IsAlive ? "Alive" : cell.IsDead ? "Dead" : "Toxin")} cell" : "null (empty)")}");

        // Only show tooltip if the cell is occupied (has a FungalCell)
        if (cell == null)
        {
            if (enableDebugLogs)
                Debug.Log("[Tooltip Debug] Cell is null - no tooltip will be shown");
            return;
        }

        // Create tooltip instance if it doesn't exist
        if (tooltipInstance == null)
        {
            if (enableDebugLogs)
                Debug.Log("[Tooltip Debug] Creating tooltip instance...");
            CreateTooltipInstance();
        }

        if (tooltipInstance == null)
        {
            Debug.LogError("[Tooltip] Failed to create tooltip instance!");
            return;
        }

        // Activate tooltip and set as last sibling for proper rendering order
        tooltipInstance.SetActive(true);
        tooltipInstance.transform.SetAsLastSibling();

        if (enableDebugLogs)
            Debug.Log($"[Tooltip Debug] Tooltip instance activated: {tooltipInstance.name}");

        // Update tooltip content using the CellTooltipUI component
        if (tooltipUI != null)
        {
            tooltipUI.UpdateTooltip(cell, board, gridVisualizer);
            if (enableDebugLogs)
                Debug.Log("[Tooltip Debug] Tooltip content updated via CellTooltipUI");
        }
        else
        {
            Debug.LogError("[Tooltip] CellTooltipUI component not found on tooltip prefab!");
            return;
        }

        // CRITICAL: Fix layout components every time tooltip is shown (not just when created)
        FixAllLayoutComponents();

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
            if (enableDebugLogs)
                Debug.Log("[Tooltip Debug] Started fade in animation");
        }

        isTooltipVisible = true;
        
        if (enableDebugLogs)
            Debug.Log("[Tooltip Debug] Tooltip should now be visible!");
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

        // Inject PlayerBinder dependency into the tooltip
        if (tooltipUI != null && FungusToast.Unity.GameManager.Instance?.GameUI?.PlayerUIBinder != null)
        {
            tooltipUI.SetPlayerBinder(FungusToast.Unity.GameManager.Instance.GameUI.PlayerUIBinder);
            if (enableDebugLogs)
                Debug.Log("[Tooltip] Injected PlayerBinder dependency into CellTooltipUI");
        }
        else if (tooltipUI != null)
        {
            Debug.LogWarning("[Tooltip] PlayerUIBinder not available for dependency injection");
        }

        // CRITICAL: Configure RectTransform for proper positioning
        if (tooltipRectTransform != null)
        {
            // Set anchors to bottom-left (0,0) for screen-space positioning
            tooltipRectTransform.anchorMin = Vector2.zero;
            tooltipRectTransform.anchorMax = Vector2.zero;
            
            // Set pivot to bottom-left (0,0) for consistent positioning
            tooltipRectTransform.pivot = Vector2.zero;
            
            // Ensure minimum width for better readability
            Vector2 currentSize = tooltipRectTransform.sizeDelta;
            if (currentSize.x < 250f)
            {
                tooltipRectTransform.sizeDelta = new Vector2(250f, currentSize.y);
                if (enableDebugLogs)
                    Debug.Log($"[Tooltip] Adjusted width from {currentSize.x} to 250 for better readability");
            }
            
            if (enableDebugLogs)
                Debug.Log($"[Tooltip] Configured RectTransform: anchors=(0,0), pivot=(0,0), size={tooltipRectTransform.sizeDelta}");
        }

        // CRITICAL: Configure all TextMeshPro components to prevent ellipsis truncation
        ConfigureTextMeshProComponents();

        // Ensure tooltip has a visible background (fallback only)
        Image backgroundImage = tooltipInstance.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = tooltipInstance.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.9f);
            backgroundImage.raycastTarget = false;
            
            if (enableDebugLogs)
                Debug.Log("[Tooltip] Added fallback background Image component");
        }

        // Add CanvasGroup if it doesn't exist (fallback only)
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

    /// <summary>
    /// Configures all TextMeshPro components in the tooltip to prevent ellipsis truncation
    /// and enable proper text wrapping.
    /// </summary>
    void ConfigureTextMeshProComponents()
    {
        // Find all TextMeshPro components in the tooltip
        TextMeshProUGUI[] textComponents = tooltipInstance.GetComponentsInChildren<TextMeshProUGUI>(true);
        
        foreach (var textComponent in textComponents)
        {
            if (textComponent != null)
            {
                // Fix overflow settings to prevent ellipsis
                textComponent.overflowMode = TextOverflowModes.Overflow;
                
                // Enable word wrapping for multi-line text (using new property)
                textComponent.textWrappingMode = TextWrappingModes.Normal;
                
                // Disable auto-sizing that might cause issues
                textComponent.enableAutoSizing = false;
                
                // Ensure reasonable font size
                if (textComponent.fontSize < 10f)
                    textComponent.fontSize = 12f;
                
                // CRITICAL: Fix layout components that might be constraining width
                FixTextComponentLayout(textComponent);
                
                if (enableDebugLogs)
                    Debug.Log($"[Tooltip] Configured TextMeshPro component: {textComponent.name} - overflow: {textComponent.overflowMode}, wrapping: {textComponent.textWrappingMode}");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[Tooltip] Configured {textComponents.Length} TextMeshPro components to prevent ellipsis truncation");
    }

    /// <summary>
    /// Fixes layout components on text elements that might be constraining their width.
    /// </summary>
    void FixTextComponentLayout(TextMeshProUGUI textComponent)
    {
        // Check for and fix LayoutElement components that might be constraining width
        LayoutElement layoutElement = textComponent.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            // Store original values for debugging
            float originalPreferred = layoutElement.preferredWidth;
            float originalMin = layoutElement.minWidth;
            float originalFlexible = layoutElement.flexibleWidth;
            
            // Disable width constraints to prevent narrow width issues
            layoutElement.preferredWidth = -1;   // Use layout group's calculation
            layoutElement.minWidth = -1;         // No minimum width constraint
            layoutElement.flexibleWidth = 1;     // Allow flexible width expansion
            
            if (enableDebugLogs && (originalPreferred != -1 || originalMin != -1 || originalFlexible != 1))
            {
                Debug.Log($"[Tooltip] Fixed LayoutElement on {textComponent.name}: " +
                         $"preferredWidth: {originalPreferred} → -1, " +
                         $"minWidth: {originalMin} → -1, " +
                         $"flexibleWidth: {originalFlexible} → 1");
            }
        }
        
        // Check for and fix ContentSizeFitter components that might be constraining width
        ContentSizeFitter sizeFitter = textComponent.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            ContentSizeFitter.FitMode originalHorizontal = sizeFitter.horizontalFit;
            
            // Disable horizontal fit to prevent width constraints
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            
            if (enableDebugLogs && originalHorizontal != ContentSizeFitter.FitMode.Unconstrained)
            {
                Debug.Log($"[Tooltip] Fixed ContentSizeFitter on {textComponent.name}: " +
                         $"horizontalFit: {originalHorizontal} → Unconstrained");
            }
        }
        
        // Check for problematic HorizontalLayoutGroup components (shouldn't be on text elements)
        HorizontalLayoutGroup horizontalLayout = textComponent.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayout != null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[Tooltip] Found unexpected HorizontalLayoutGroup on {textComponent.name} - this may cause width issues");
        }
    }

    /// <summary>
    /// Fixes all layout components in the tooltip that might be constraining width.
    /// Called every time the tooltip is shown to ensure constraints are always resolved.
    /// </summary>
    void FixAllLayoutComponents()
    {
        if (tooltipInstance == null) return;

        if (enableDebugLogs)
            Debug.Log("[Tooltip Debug] Checking and fixing layout components...");

        // Find all TextMeshPro components and fix their layout constraints
        TextMeshProUGUI[] textComponents = tooltipInstance.GetComponentsInChildren<TextMeshProUGUI>(true);
        
        int fixedComponents = 0;
        foreach (var textComponent in textComponents)
        {
            if (textComponent != null)
            {
                // Check current width before fixing
                float widthBefore = textComponent.rectTransform.sizeDelta.x;
                
                // Fix layout components
                FixTextComponentLayout(textComponent);
                
                // SPECIAL FIX: Check for shifted expiration group
                if (textComponent.name.Contains("Expiration"))
                {
                    FixExpirationGroupPosition(textComponent);
                }
                
                // Force layout rebuild to see immediate results
                LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);
                
                float widthAfter = textComponent.rectTransform.sizeDelta.x;
                
                if (enableDebugLogs && Math.Abs(widthBefore - widthAfter) > 0.1f)
                {
                    Debug.Log($"[Tooltip] Width changed for {textComponent.name}: {widthBefore:F1} → {widthAfter:F1}");
                }
                
                fixedComponents++;
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[Tooltip Debug] Processed {fixedComponents} text components for layout fixes");
    }

    /// <summary>
    /// Fixes positioning issues with the expiration group that might be shifted left.
    /// </summary>
    void FixExpirationGroupPosition(TextMeshProUGUI expirationComponent)
    {
        // Check the parent row container for positioning issues
        Transform parentRow = expirationComponent.transform.parent;
        if (parentRow != null && parentRow.name.Contains("ExpirationGroupRow"))
        {
            RectTransform rowRect = parentRow.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                // Store original position for debugging
                Vector2 originalAnchoredPos = rowRect.anchoredPosition;
                Vector2 originalOffsetMin = rowRect.offsetMin;
                Vector2 originalOffsetMax = rowRect.offsetMax;
                
                // Fix anchoring to prevent left shift
                rowRect.anchorMin = new Vector2(0f, rowRect.anchorMin.y);
                rowRect.anchorMax = new Vector2(1f, rowRect.anchorMax.y);
                
                // Reset horizontal offsets to use full width
                rowRect.offsetMin = new Vector2(0f, rowRect.offsetMin.y);
                rowRect.offsetMax = new Vector2(0f, rowRect.offsetMax.y);
                
                // Ensure position is not shifted left
                if (rowRect.anchoredPosition.x < 0)
                {
                    rowRect.anchoredPosition = new Vector2(0f, rowRect.anchoredPosition.y);
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[Tooltip] Fixed expiration group position: " +
                             $"anchoredPos: {originalAnchoredPos} → {rowRect.anchoredPosition}, " +
                             $"offsetMin: {originalOffsetMin} → {rowRect.offsetMin}, " +
                             $"offsetMax: {originalOffsetMax} → {rowRect.offsetMax}");
                }
            }
        }
        
        // Also fix the text component itself
        RectTransform textRect = expirationComponent.rectTransform;
        if (textRect.anchoredPosition.x < 0)
        {
            Vector2 originalPos = textRect.anchoredPosition;
            textRect.anchoredPosition = new Vector2(0f, textRect.anchoredPosition.y);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[Tooltip] Fixed expiration text position: {originalPos} → {textRect.anchoredPosition}");
            }
        }
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

        if (enableDebugLogs)
            Debug.Log($"[Tooltip Debug] Positioned tooltip at: {tooltipPos} (mouse: {mousePos}, offset: {tooltipOffset})");
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