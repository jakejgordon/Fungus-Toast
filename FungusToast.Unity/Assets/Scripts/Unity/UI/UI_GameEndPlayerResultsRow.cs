using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using System;

namespace FungusToast.Unity.UI
{
    public class UI_GameEndPlayerResultsRow : MonoBehaviour
    {
        // Same YOU treatment as the in-match scoreboard (PlayerSummaryRow): accent strip,
        // Moss row tint, and a Lichen pill on the mold icon.
        private const float YouAccentStripWidth = 4f;
        private const float YouBadgeWidth = 44f;
        private const float YouBadgeHeight = 20f;
        private static readonly Vector2 YouBadgeOffset = new Vector2(4f, -2f);

        [Header("References")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI livingText;
        private TextMeshProUGUI resistantText = null!;
        [SerializeField] private TextMeshProUGUI deadText;
        [SerializeField] private TextMeshProUGUI toxinText;
        private TextMeshProUGUI colonizedText = null!;
        private TextMeshProUGUI spentPointsText = null!;
        [SerializeField] private Button detailsButton;
        [SerializeField] private Image rowBackground;
        private GameObject youAccentStrip = null!;
        private GameObject youBadgeRoot = null!;

        private void Awake()
        {
            if (rowBackground == null)
            {
                rowBackground = GetComponent<Image>();
                if (rowBackground == null)
                {
                    rowBackground = gameObject.AddComponent<Image>();
                }
            }

            if (rowBackground != null)
            {
                var c = UIStyleTokens.Surface.PanelSecondary;
                c.a = 0.6f;
                rowBackground.color = c;
                rowBackground.raycastTarget = false;
            }

            if (rankText != null) rankText.color = UIStyleTokens.Text.Primary;
            if (nameText != null) nameText.color = UIStyleTokens.Text.Primary;
            if (livingText != null) livingText.color = UIStyleTokens.Text.Secondary;
            if (resistantText != null) resistantText.color = UIStyleTokens.State.Success;
            if (deadText != null) deadText.color = UIStyleTokens.Text.Muted;
            if (toxinText != null) toxinText.color = UIStyleTokens.Text.Muted;
            if (colonizedText != null) colonizedText.color = UIStyleTokens.Text.Secondary;
            if (spentPointsText != null) spentPointsText.color = UIStyleTokens.Text.Secondary;

            var rowLayout = GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
            {
                EndGameResultsTableLayout.ApplyRowLayout(rowLayout);
            }

            ConfigureText(rankText, TextAlignmentOptions.Center, 23f, allowAutoSize: false);
            ConfigureText(nameText, TextAlignmentOptions.Left, 23f, allowAutoSize: true);
            ConfigureText(livingText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
            EnsureResistantText();
            ConfigureText(deadText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
            EnsureToxinText();
            EnsureColonizedText();
            EnsureSpentPointsText();
            EnsureDetailsButton();
            EnsureColumnWidths();
        }

        /* -------- public API -------- */
        public void Populate(int rank, Sprite icon, string playerName, int living, int resistant, int dead, int toxins, int colonized, int spentPoints, Action onDetailsRequested = null)
        {
            EnsureResistantText();
            EnsureToxinText();
            EnsureColonizedText();
            EnsureSpentPointsText();

            rankText.text = rank.ToString();
            iconImage.sprite = icon;
            nameText.text = playerName;
            livingText.text = FormatCount(living);
            if (resistantText != null)
            {
                resistantText.text = FormatCountOrZero(resistant);
            }

            deadText.text = FormatCount(dead);
            if (toxinText != null)
            {
                toxinText.text = FormatCountOrZero(toxins);
            }

            if (colonizedText != null)
            {
                colonizedText.text = FormatCountOrZero(colonized);
            }

            if (spentPointsText != null)
            {
                spentPointsText.text = FormatCountOrZero(spentPoints);
            }

            ConfigureDetailsButton(onDetailsRequested);
        }

        /// <summary>Marks the row as the local human's, matching the scoreboard's YOU treatment.</summary>
        public void SetPerspectivePlayer(bool isPerspectivePlayer)
        {
            EnsurePerspectiveVisuals();

            if (rowBackground != null)
            {
                var inactive = UIStyleTokens.Surface.PanelSecondary;
                inactive.a = 0.6f;
                rowBackground.color = isPerspectivePlayer
                    ? UIStyleTokens.WithAlpha(UIStyleTokens.Accent.Moss, UIStyleTokens.Alpha.PerspectiveHighlight)
                    : inactive;
            }

            if (youAccentStrip != null)
            {
                youAccentStrip.SetActive(isPerspectivePlayer);
            }

            if (youBadgeRoot != null)
            {
                youBadgeRoot.SetActive(isPerspectivePlayer);
            }
        }

        private void EnsurePerspectiveVisuals()
        {
            if (youAccentStrip == null)
            {
                var stripObject = new GameObject("UI_YouAccentStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                stripObject.transform.SetParent(transform, false);
                stripObject.GetComponent<LayoutElement>().ignoreLayout = true;

                var stripRect = stripObject.GetComponent<RectTransform>();
                stripRect.anchorMin = new Vector2(0f, 0f);
                stripRect.anchorMax = new Vector2(0f, 1f);
                stripRect.pivot = new Vector2(0f, 0.5f);
                stripRect.anchoredPosition = Vector2.zero;
                stripRect.sizeDelta = new Vector2(YouAccentStripWidth, 0f);

                var strip = stripObject.GetComponent<Image>();
                strip.raycastTarget = false;
                strip.color = UIStyleTokens.Accent.Lichen;
                stripObject.SetActive(false);
                youAccentStrip = stripObject;
            }

            if (youBadgeRoot == null && iconImage != null)
            {
                var badgeObject = new GameObject("UI_YouBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(LayoutElement));
                badgeObject.transform.SetParent(iconImage.transform, false);
                badgeObject.GetComponent<LayoutElement>().ignoreLayout = true;

                var badgeRect = badgeObject.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 1f);
                badgeRect.anchorMax = new Vector2(1f, 1f);
                badgeRect.pivot = new Vector2(1f, 1f);
                badgeRect.anchoredPosition = YouBadgeOffset;
                badgeRect.sizeDelta = new Vector2(YouBadgeWidth, YouBadgeHeight);

                var badgeBackground = badgeObject.GetComponent<Image>();
                badgeBackground.raycastTarget = false;
                badgeBackground.color = UIStyleTokens.Accent.Lichen;

                var badgeOutline = badgeObject.GetComponent<Outline>();
                badgeOutline.effectColor = UIStyleTokens.Surface.PanelPrimary;
                badgeOutline.effectDistance = new Vector2(1f, -1f);
                badgeOutline.useGraphicAlpha = true;

                var badgeTextObject = new GameObject("UI_YouBadgeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                badgeTextObject.transform.SetParent(badgeObject.transform, false);

                var badgeTextRect = badgeTextObject.GetComponent<RectTransform>();
                badgeTextRect.anchorMin = Vector2.zero;
                badgeTextRect.anchorMax = Vector2.one;
                badgeTextRect.offsetMin = Vector2.zero;
                badgeTextRect.offsetMax = Vector2.zero;

                var badgeText = badgeTextObject.GetComponent<TextMeshProUGUI>();
                badgeText.text = "YOU";
                badgeText.color = UIStyleTokens.Text.OnAccent;
                badgeText.alignment = TextAlignmentOptions.Center;
                badgeText.fontStyle = FontStyles.Bold;
                badgeText.enableAutoSizing = false;
                badgeText.fontSize = UIStyleTokens.Typography.MicroMinimum;
                badgeText.margin = new Vector4(4f, 0f, 4f, 0f);
                badgeText.textWrappingMode = TextWrappingModes.NoWrap;
                badgeText.overflowMode = TextOverflowModes.Ellipsis;
                badgeText.raycastTarget = false;
                if (nameText != null)
                {
                    badgeText.font = nameText.font;
                }

                badgeObject.SetActive(false);
                youBadgeRoot = badgeObject;
            }
        }

        private void EnsureResistantText()
        {
            if (resistantText != null || deadText == null)
            {
                return;
            }

            var clone = Instantiate(deadText.gameObject, deadText.transform.parent);
            clone.name = "UI_PlayerResultsResistantText";
            clone.transform.SetSiblingIndex(deadText.transform.GetSiblingIndex());

            resistantText = clone.GetComponent<TextMeshProUGUI>();
            if (resistantText != null)
            {
                resistantText.color = UIStyleTokens.State.Success;
                ConfigureText(resistantText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
                resistantText.text = "0";
            }
        }

        private void EnsureToxinText()
        {
            if (toxinText != null || deadText == null)
            {
                return;
            }

            var clone = Instantiate(deadText.gameObject, deadText.transform.parent);
            clone.name = "UI_PlayerResultsToxinText";
            clone.transform.SetSiblingIndex(deadText.transform.GetSiblingIndex() + 1);

            toxinText = clone.GetComponent<TextMeshProUGUI>();
            if (toxinText != null)
            {
                toxinText.color = UIStyleTokens.Text.Muted;
                ConfigureText(toxinText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
                toxinText.text = "0";
            }
        }

        private void EnsureColonizedText()
        {
            if (colonizedText != null || deadText == null)
            {
                return;
            }

            var clone = Instantiate(deadText.gameObject, deadText.transform.parent);
            clone.name = "UI_PlayerResultsColonizedText";
            int toxinSiblingIndex = toxinText != null ? toxinText.transform.GetSiblingIndex() : deadText.transform.GetSiblingIndex();
            clone.transform.SetSiblingIndex(toxinSiblingIndex + 1);

            colonizedText = clone.GetComponent<TextMeshProUGUI>();
            if (colonizedText != null)
            {
                colonizedText.color = UIStyleTokens.Text.Secondary;
                ConfigureText(colonizedText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
                colonizedText.text = "0";
            }
        }

        private void EnsureSpentPointsText()
        {
            if (spentPointsText != null || deadText == null)
            {
                return;
            }

            var clone = Instantiate(deadText.gameObject, deadText.transform.parent);
            clone.name = "UI_PlayerResultsSpentPointsText";
            Transform precedingColumn = colonizedText != null ? colonizedText.transform : toxinText != null ? toxinText.transform : deadText.transform;
            clone.transform.SetSiblingIndex(precedingColumn.GetSiblingIndex() + 1);

            spentPointsText = clone.GetComponent<TextMeshProUGUI>();
            if (spentPointsText != null)
            {
                spentPointsText.color = UIStyleTokens.Text.Secondary;
                ConfigureText(spentPointsText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
                spentPointsText.text = "0";
            }
        }

        private void EnsureDetailsButton()
        {
            if (detailsButton != null)
            {
                ApplyDetailsButtonStyle(detailsButton);
                return;
            }

            Transform parent = toxinText != null ? toxinText.transform.parent : transform;
            if (parent == null)
            {
                return;
            }

            var buttonObject = new GameObject("UI_PlayerResultsDetailsButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.transform.SetAsLastSibling();

            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;

            detailsButton = buttonObject.GetComponent<Button>();

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 42f;
            layout.minHeight = 38f;
            EndGameResultsTableLayout.ApplyCell(buttonObject, EndGameResultsTableLayout.Column.Details);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Details";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMax = 20f;
            label.fontSizeMin = 13f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(label);
            label.raycastTarget = false;

            ApplyDetailsButtonStyle(detailsButton);
        }

        private void EnsureColumnWidths()
        {
            ApplyColumn(rankText, EndGameResultsTableLayout.Column.Rank);
            ApplyColumn(iconImage, EndGameResultsTableLayout.Column.Icon);
            ApplyColumn(nameText, EndGameResultsTableLayout.Column.Player);
            ApplyColumn(livingText, EndGameResultsTableLayout.Column.Alive);
            ApplyColumn(resistantText, EndGameResultsTableLayout.Column.Resistant);
            ApplyColumn(deadText, EndGameResultsTableLayout.Column.Dead);
            ApplyColumn(toxinText, EndGameResultsTableLayout.Column.Toxins);
            ApplyColumn(colonizedText, EndGameResultsTableLayout.Column.Colonized);
            ApplyColumn(spentPointsText, EndGameResultsTableLayout.Column.SpentPoints);
            ApplyColumn(detailsButton, EndGameResultsTableLayout.Column.Details);
        }

        private static void ApplyColumn(Component component, EndGameResultsTableLayout.Column column)
        {
            if (component != null)
            {
                EndGameResultsTableLayout.ApplyCell(component.gameObject, column);
            }
        }

        private void ConfigureDetailsButton(Action onDetailsRequested)
        {
            EnsureDetailsButton();
            if (detailsButton == null)
            {
                return;
            }

            detailsButton.onClick.RemoveAllListeners();

            bool hasAction = onDetailsRequested != null;
            detailsButton.gameObject.SetActive(hasAction);
            detailsButton.interactable = hasAction;

            if (!hasAction)
            {
                return;
            }

            detailsButton.onClick.AddListener(() => onDetailsRequested());
            var trigger = detailsButton.GetComponent<FungusToast.Unity.UI.Tooltips.TooltipTrigger>();
            if (trigger == null)
            {
                trigger = detailsButton.gameObject.AddComponent<FungusToast.Unity.UI.Tooltips.TooltipTrigger>();
            }

            trigger.SetStaticText("View this player's end-of-game build details.");
        }

        private static void ApplyDetailsButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            UIStyleTokens.Button.ApplyPanelSecondaryStyle(button);

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "Details";
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMax = 20f;
                label.fontSizeMin = 13f;
                label.fontStyle = FontStyles.Bold;
                label.color = UIStyleTokens.Text.Primary;
            }
        }

        private static string FormatCount(int value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture);
        }

        private static string FormatCountOrZero(int value)
        {
            return value <= 0 ? "0" : FormatCount(value);
        }

        private static void ConfigureText(TextMeshProUGUI label, TextAlignmentOptions alignment, float fontSize, bool allowAutoSize)
        {
            if (label == null)
            {
                return;
            }

            label.alignment = alignment;
            label.enableAutoSizing = allowAutoSize;
            if (!allowAutoSize)
            {
                label.fontSize = fontSize;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                TMPOverflowUtility.SetSafeEllipsis(label);
            }
        }
    }

    /// <summary>
    /// The results table's column layout, shared by the header (<c>UI_EndGamePanel.BuildResultsHeader</c>)
    /// and every <see cref="UI_GameEndPlayerResultsRow"/>. Both sides apply these values through
    /// <see cref="ApplyRowLayout"/> and <see cref="ApplyCell"/>, so a header cell and the row cell
    /// beneath it always resolve to the same x and width. Change a column here, never in the
    /// prefab or at a call site.
    /// </summary>
    /// <remarks>
    /// Every fixed column has min = preferred and the Player column is the only one that flexes or
    /// shrinks. When the table is narrower than the sum of preferred widths, a HorizontalLayoutGroup
    /// shrinks each child between its min and preferred width, so any column whose min differs
    /// between header and rows drifts - the header once had no min width and slid right of its
    /// values one column at a time.
    /// </remarks>
    internal static class EndGameResultsTableLayout
    {
        /// <summary>Columns in display order; the order matches the children of the header and each row.</summary>
        internal enum Column
        {
            Rank,
            Icon,
            Player,
            Alive,
            Resistant,
            Dead,
            Toxins,
            Colonized,
            SpentPoints,
            Details,
        }

        internal const float ColumnSpacing = 14f;
        internal const int HorizontalPadding = 18;
        internal const float PlayerMinWidth = 140f;
        private const float MetricWidth = 92f;

        internal static float GetPreferredWidth(Column column)
        {
            switch (column)
            {
                case Column.Rank: return 60f;
                case Column.Icon: return 52f;
                case Column.Player: return 260f;
                case Column.SpentPoints: return 132f;
                case Column.Details: return 108f;
                default: return MetricWidth;
            }
        }

        /// <summary>Applies the shared padding and spacing; vertical padding is left to the caller.</summary>
        internal static void ApplyRowLayout(HorizontalLayoutGroup layout)
        {
            if (layout == null)
            {
                return;
            }

            layout.padding = new RectOffset(HorizontalPadding, HorizontalPadding, layout.padding.top, layout.padding.bottom);
            layout.spacing = ColumnSpacing;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
        }

        internal static void ApplyCell(GameObject cell, Column column)
        {
            if (cell == null)
            {
                return;
            }

            var layout = cell.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = cell.AddComponent<LayoutElement>();
            }

            float preferred = GetPreferredWidth(column);
            bool isPlayer = column == Column.Player;
            layout.ignoreLayout = false;
            layout.preferredWidth = preferred;
            layout.minWidth = isPlayer ? PlayerMinWidth : preferred;
            layout.flexibleWidth = isPlayer ? 1f : 0f;
        }

        /// <summary>
        /// Development check: logs a warning when any header cell and the row cell beneath it
        /// resolved to a different x or width. Call after the layout has been rebuilt.
        /// </summary>
        internal static void WarnIfMisaligned(RectTransform header, RectTransform row)
        {
            if (!Debug.isDebugBuild || header == null || row == null)
            {
                return;
            }

            var headerCells = GetLayoutCells(header);
            var rowCells = GetLayoutCells(row);
            if (headerCells.Count != rowCells.Count)
            {
                Debug.LogWarning($"[EndGameResultsTable] Header has {headerCells.Count} columns but rows have {rowCells.Count}.");
                return;
            }

            for (int i = 0; i < headerCells.Count; i++)
            {
                Rect headerRect = headerCells[i].rect;
                Rect rowRect = rowCells[i].rect;
                if (headerRect.width <= 0f || rowRect.width <= 0f)
                {
                    return;
                }

                float headerX = headerCells[i].localPosition.x + headerRect.xMin;
                float rowX = rowCells[i].localPosition.x + rowRect.xMin;
                if (Mathf.Abs(headerX - rowX) > 0.5f || Mathf.Abs(headerRect.width - rowRect.width) > 0.5f)
                {
                    Debug.LogWarning(
                        $"[EndGameResultsTable] Column {(Column)i} is misaligned: header x={headerX:F1} w={headerRect.width:F1}, "
                        + $"row x={rowX:F1} w={rowRect.width:F1}. Apply EndGameResultsTableLayout to both sides.");
                    return;
                }
            }
        }

        private static System.Collections.Generic.List<RectTransform> GetLayoutCells(RectTransform parent)
        {
            var cells = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeSelf || !(child is RectTransform rect))
                {
                    continue;
                }

                var element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout)
                {
                    continue;
                }

                cells.Add(rect);
            }

            return cells;
        }
    }
}
