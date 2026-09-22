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

    // The ordinal sits in the gap between the 48px portrait and the 80px row's bottom edge.
    private const float OrdinalOffsetY = -32f;
    private const float OrdinalHeight = 16f;
    private const float StatusLabelWidth = 170f;

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

            bool isHuman = draftOrder[i].PlayerType == PlayerTypeEnum.Human;

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

            AddOrdinal(cellGO.transform, i + 1, isHuman, isDone: i < activeIndex);

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

        AddStatusLabel(draftOrder, activeIndex);
    }

    /// <summary>
    /// Small pick number under each portrait so the sequence reads without decoding the arrows.
    /// The human's number uses the scoreboard's YOU accent so their slot is easy to find.
    /// </summary>
    private static void AddOrdinal(Transform cell, int pickNumber, bool isHuman, bool isDone)
    {
        var ordinalObj = new GameObject("OrdinalText", typeof(RectTransform), typeof(TextMeshProUGUI));
        ordinalObj.transform.SetParent(cell, false);

        var rect = ordinalObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, OrdinalOffsetY);
        rect.sizeDelta = new Vector2(50f, OrdinalHeight);

        var text = ordinalObj.GetComponent<TextMeshProUGUI>();
        text.text = pickNumber.ToString();
        text.fontSize = UIStyleTokens.Typography.MicroMinimum;
        text.fontStyle = isHuman ? FontStyles.Bold : FontStyles.Normal;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        var color = isHuman ? UIStyleTokens.Accent.Lichen : UIStyleTokens.Text.Secondary;
        if (isDone)
        {
            color.a *= 0.5f;
        }
        text.color = color;
    }

    /// <summary>
    /// Plain-language summary of where the human sits in the order, appended after the last portrait.
    /// </summary>
    private void AddStatusLabel(List<Player> draftOrder, int activeIndex)
    {
        int humanIndex = draftOrder.FindIndex(p => p.PlayerType == PlayerTypeEnum.Human);
        if (humanIndex < 0)
        {
            return;
        }

        string ordinal = ToOrdinal(humanIndex + 1);
        string label;
        if (humanIndex == activeIndex)
        {
            label = "Your pick";
        }
        else if (humanIndex < activeIndex)
        {
            label = $"You picked {ordinal}";
        }
        else
        {
            label = $"You pick {ordinal} of {draftOrder.Count}";
        }

        var labelObj = new GameObject("PickStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(transform, false);

        var rect = labelObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(StatusLabelWidth, 0f); // Height follows the row like the arrows do.

        var text = labelObj.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = UIStyleTokens.Typography.CaptionMinimum;
        text.fontStyle = humanIndex == activeIndex ? FontStyles.Bold : FontStyles.Normal;
        text.color = humanIndex == activeIndex ? UIStyleTokens.Accent.Lichen : UIStyleTokens.Text.Secondary;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(12f, 0f, 0f, 0f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        cells.Add(labelObj);
    }

    private static string ToOrdinal(int number)
    {
        int lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return $"{number}th";
        }

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th",
        };
    }
}
