using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.Players;
using FungusToast.Unity;
using FungusToast.Unity.UI;
using System.Collections.Generic;
using TMPro; // For TextMeshProUGUI
using UnityEngine;

public class DraftOrderRow : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject playerIconCellPrefab;

    [Header("Arrow")]
    public string arrowChar = "→";
    public float arrowFontSize = 28f; // Match your style

    [Header("Colors")]
    public Color activeHighlightColor = new Color(1f, 1f, 0.5f, 0.8f); // Yellow glow
    public Color inactiveColor = Color.white;
    public Color previousColor = new Color(1f, 1f, 1f, 0.3f); // Faded
    public Color arrowColor = Color.white;

    private readonly List<GameObject> cells = new();

    public void SetDraftOrder(List<Player> draftOrder, int activeIndex)
    {
        // Get PlayerBinder from GameManager
        var playerBinder = GameManager.Instance?.GameUI?.PlayerUIBinder;
        if (playerBinder == null)
        {
            Debug.LogError("[DraftOrderRow] UI_PlayerBinder not found! Cannot display player icons.");
            return;
        }

        // Clear old cells
        foreach (var cell in cells)
            Destroy(cell);
        cells.Clear();

        for (int i = 0; i < draftOrder.Count; i++)
        {
            var cellGO = Instantiate(playerIconCellPrefab, transform);
            var cellUI = cellGO.GetComponent<PlayerIconCellUI>();
            if (cellUI == null)
            {
                Debug.LogError("PlayerIconCell prefab is missing the PlayerIconCellUI component.");
                continue;
            }

            // Set icon
            var icon = cellUI.IconImage;
            icon.sprite = playerBinder.GetIcon(draftOrder[i]);
            icon.color = (i < activeIndex) ? previousColor : inactiveColor;

            // Set highlight
            var highlightBG = cellUI.HighlightBackground;
            if (highlightBG != null)
            {
                highlightBG.enabled = (i == activeIndex);
                highlightBG.color = (i == activeIndex) ? activeHighlightColor : Color.clear;
            }

            cells.Add(cellGO);

            // Add arrow (TextMeshProUGUI) if not last
            if (i < draftOrder.Count - 1)
            {
                var arrowObj = new GameObject("ArrowText", typeof(RectTransform), typeof(TextMeshProUGUI));
                arrowObj.transform.SetParent(transform, false);

                var text = arrowObj.GetComponent<TextMeshProUGUI>();
                text.text = arrowChar;
                text.fontSize = arrowFontSize;
                text.color = arrowColor;
                text.alignment = TextAlignmentOptions.Center; // Ensures horizontal and vertical center

                var arrowRect = arrowObj.GetComponent<RectTransform>();
                arrowRect.sizeDelta = new Vector2(32, 0); // 32px wide, height flexible

                cells.Add(arrowObj);
            }
        }
    }
}
