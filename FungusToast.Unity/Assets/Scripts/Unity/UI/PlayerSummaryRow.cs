using FungusToast.Core.Players;
using FungusToast.Unity.Grid; // Needed for GridVisualizer
using FungusToast.Unity.UI.MycovariantDraft;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using System.Globalization;

namespace FungusToast.Unity.UI
{
    public class PlayerSummaryRow : MonoBehaviour
    {
        private const float StatTextScale = 1.05f;
        private const float StatColumnWidth = 90f;
        private static readonly Color InactiveRowBackground = new Color(
            UIStyleTokens.Surface.PanelPrimary.r,
            UIStyleTokens.Surface.PanelPrimary.g,
            UIStyleTokens.Surface.PanelPrimary.b,
            0.52f);

        [SerializeField] private Image moldIconImage;
        [SerializeField] private TextMeshProUGUI livingCellsText;
        [SerializeField] private TextMeshProUGUI deadCellsText;
        [SerializeField] private TextMeshProUGUI toxinCellsText;
        [SerializeField] private Transform mycovariantContainer;
        [SerializeField] private MycovariantIcon mycovariantIconPrefab;

        // Add this field to keep reference if needed
        private PlayerMoldIconHoverHandler hoverHandler;
        private Image rowBackground;
        private Image leftAccentStrip;
        private GameObject youBadgeRoot;

        public int PlayerId { get; set; } // <-- Add this property

        private void Awake()
        {
            ApplyStyle();
            EnsurePerspectiveIndicatorVisuals();
            SetPerspectivePlayer(false);
        }

        private void ApplyStyle()
        {
            if (livingCellsText != null)
            {
                livingCellsText.color = UIStyleTokens.Text.Primary;
                livingCellsText.fontStyle = FontStyles.Bold;
                ApplyTextScale(livingCellsText, StatTextScale);
                ConfigureNumericColumn(livingCellsText);
                ApplyColumnWidth(livingCellsText.transform, StatColumnWidth);
            }
            if (deadCellsText != null)
            {
                deadCellsText.color = UIStyleTokens.Text.Secondary;
                deadCellsText.fontStyle = FontStyles.Bold;
                ApplyTextScale(deadCellsText, StatTextScale);
                ConfigureNumericColumn(deadCellsText);
                ApplyColumnWidth(deadCellsText.transform, StatColumnWidth);
            }
            if (toxinCellsText != null)
            {
                toxinCellsText.color = UIStyleTokens.Text.Secondary;
                toxinCellsText.fontStyle = FontStyles.Bold;
                ApplyTextScale(toxinCellsText, StatTextScale);
                ConfigureNumericColumn(toxinCellsText);
                ApplyColumnWidth(toxinCellsText.transform, StatColumnWidth);
            }
        }

        private static void ApplyColumnWidth(Transform cell, float width)
        {
            if (cell == null)
            {
                return;
            }

            var layout = cell.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = cell.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.flexibleWidth = -1f;
        }

        private static void ConfigureNumericColumn(TextMeshProUGUI label)
        {
            if (label == null) return;

            label.alignment = TextAlignmentOptions.MidlineRight;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            float maxSize = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMax = maxSize;
            label.fontSizeMin = Mathf.Max(10f, maxSize * 0.70f);
        }

        private static void ApplyTextScale(TextMeshProUGUI label, float scale)
        {
            if (label == null || scale <= 1f) return;

            if (label.enableAutoSizing)
            {
                label.fontSizeMin *= scale;
                label.fontSizeMax *= scale;
            }
            else
            {
                label.fontSize *= scale;
            }
        }

        private void EnsurePerspectiveIndicatorVisuals()
        {
            rowBackground = GetComponent<Image>();
            if (rowBackground == null)
            {
                rowBackground = gameObject.AddComponent<Image>();
            }

            rowBackground.raycastTarget = false;

            var rowRect = transform as RectTransform;
            if (rowRect != null)
            {
                var stripObject = new GameObject("UI_YouAccentStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                stripObject.transform.SetParent(rowRect, false);

                var stripRect = stripObject.GetComponent<RectTransform>();
                stripRect.anchorMin = new Vector2(0f, 0f);
                stripRect.anchorMax = new Vector2(0f, 1f);
                stripRect.pivot = new Vector2(0f, 0.5f);
                stripRect.anchoredPosition = Vector2.zero;
                stripRect.sizeDelta = new Vector2(4f, 0f);

                var stripLayout = stripObject.GetComponent<LayoutElement>();
                stripLayout.ignoreLayout = true;

                leftAccentStrip = stripObject.GetComponent<Image>();
                leftAccentStrip.raycastTarget = false;
                leftAccentStrip.color = UIStyleTokens.Accent.Lichen;
            }

            if (moldIconImage == null)
            {
                return;
            }

            var badgeObject = new GameObject("UI_YouBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            badgeObject.transform.SetParent(moldIconImage.transform, false);

            var badgeRect = badgeObject.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(2f, -2f);
            badgeRect.sizeDelta = new Vector2(30f, 14f);

            var badgeLayout = badgeObject.GetComponent<LayoutElement>();
            badgeLayout.ignoreLayout = true;

            var badgeBackground = badgeObject.GetComponent<Image>();
            badgeBackground.raycastTarget = false;
            badgeBackground.color = UIStyleTokens.Accent.Lichen;

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
            badgeText.enableAutoSizing = true;
            badgeText.fontSizeMin = 7f;
            badgeText.fontSizeMax = 11f;
            badgeText.textWrappingMode = TextWrappingModes.NoWrap;
            badgeText.overflowMode = TextOverflowModes.Ellipsis;

            if (livingCellsText != null)
            {
                badgeText.font = livingCellsText.font;
            }

            youBadgeRoot = badgeObject;
        }

        public void SetPerspectivePlayer(bool isPerspectivePlayer)
        {
            if (rowBackground != null)
            {
                rowBackground.color = isPerspectivePlayer
                    ? new Color(UIStyleTokens.Accent.Moss.r, UIStyleTokens.Accent.Moss.g, UIStyleTokens.Accent.Moss.b, 0.38f)
                    : InactiveRowBackground;
            }

            if (leftAccentStrip != null)
            {
                leftAccentStrip.gameObject.SetActive(isPerspectivePlayer);
            }

            if (youBadgeRoot != null)
            {
                youBadgeRoot.SetActive(isPerspectivePlayer);
            }
        }

        /// <summary>
        /// Sets the mold icon sprite.
        /// </summary>
        public void SetIcon(Sprite sprite)
        {
            if (moldIconImage != null)
                moldIconImage.sprite = sprite;
        }

        public void SetCounts(int living, int dead, int toxins)
        {
            if (livingCellsText != null)
                livingCellsText.text = FormatCount(living); // No label, just the number
            if (deadCellsText != null)
                deadCellsText.text = FormatCount(dead);     // No label, just the number
            if (toxinCellsText != null)
                toxinCellsText.text = FormatCount(toxins);
        }

        private static string FormatCount(int value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture);
        }


        /// <summary>
        /// Call this after instantiating the row to wire up hover highlighting!
        /// </summary>
        public void SetHoverHighlight(int playerId, FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            if (moldIconImage == null)
                return;

            hoverHandler = PlayerMoldIconHoverHandler.Attach(moldIconImage.gameObject, playerId, gridVisualizer);

            // --- Wire tooltip provider on the icon ---
            var tooltipTrigger = moldIconImage.GetComponent<TooltipTrigger>();
            if (tooltipTrigger == null)
                tooltipTrigger = moldIconImage.gameObject.AddComponent<TooltipTrigger>();

            var provider = moldIconImage.GetComponent<PlayerSummaryTooltipProvider>();
            if (provider == null)
                provider = moldIconImage.gameObject.AddComponent<PlayerSummaryTooltipProvider>();

            tooltipTrigger.SetPinOnClick(true);

            // Resolve the Player instance from the GameManager's board
            var board = GameManager.Instance?.Board;
            var players = board?.Players;
            if (players != null)
            {
                var player = players.Find(p => p.PlayerId == playerId);
                if (player != null)
                {
                    provider.Initialize(player, players);
                    tooltipTrigger.SetDynamicProvider(provider);
                }
            }
        }

        public void UpdateMycovariants(IReadOnlyList<PlayerMycovariant> mycovariants)
        {
            // Clear old icons
            foreach (Transform child in mycovariantContainer)
                Destroy(child.gameObject);

            // Add new icons
            foreach (var myco in mycovariants)
            {
                var icon = Instantiate(mycovariantIconPrefab, mycovariantContainer);
                icon.SetMycovariant(myco);
            }
        }

    }
}
