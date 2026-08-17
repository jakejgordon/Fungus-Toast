using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Unity.UI.Tooltips;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI.MutationTree
{
    public class MutationTreeBuilder : MonoBehaviour
    {
        private const float HeaderTitleHeight = 40f;
        private const float HeaderInvestmentSummaryFontSize = 14f;
        private const float HeaderInvestmentSummaryMinFontSize = 11f;
        private const float HeaderInvestmentSummaryHeight = 20f;
        private const float HeaderTotalHeight = HeaderTitleHeight + HeaderInvestmentSummaryHeight;
        private const float MutationNodeWidth = 132f;
        private const float MutationNodeHeight = 120f;
        private const float PlannedLaneCardHeight = 100f;
        private const float DirectionalTendrilsCardHeight = 292f;
        private static readonly int[] DirectionalTendrilOrder =
        {
            MutationIds.TendrilNorthwest,
            MutationIds.TendrilNortheast,
            MutationIds.TendrilSouthwest,
            MutationIds.TendrilSoutheast
        };

        [Header("Prefabs")]
        [SerializeField] private GameObject categoryHeaderPrefab;
        [SerializeField] private GameObject mutationNodePrefab;

        [Header("Column Parents")]
        [SerializeField] private RectTransform growthColumn;
        [SerializeField] private RectTransform resilienceColumn;
        [SerializeField] private RectTransform fungicideColumn;
        [SerializeField] private RectTransform driftColumn;
        [SerializeField] private RectTransform mycelialSurgesColumn;
        private RectTransform plannedSubstrateEcologyColumn;

        // Cached header summary text references for investment display
        private readonly Dictionary<MutationCategory, TextMeshProUGUI> headerSummaryTexts = new();

        public List<MutationNodeUI> BuildTree(
            IEnumerable<Mutation> mutations,
            Dictionary<int, MutationLayoutMetadata> layout,
            Player player,
            UI_MutationManager uiManager)
        {
            if (growthColumn == null || resilienceColumn == null || fungicideColumn == null || driftColumn == null || mycelialSurgesColumn == null)
            {
                Debug.LogError("❌ MutationTreeBuilder: One or more column containers are not assigned.");
                return new List<MutationNodeUI>();
            }

            EnsurePlannedSubstrateEcologyColumn();
            if (plannedSubstrateEcologyColumn == null)
            {
                Debug.LogError("❌ MutationTreeBuilder: Could not create the planned Substrate Ecology column.");
                return new List<MutationNodeUI>();
            }

            ClearColumn(growthColumn);
            ClearColumn(resilienceColumn);
            ClearColumn(fungicideColumn);
            ClearColumn(driftColumn);
            ClearColumn(mycelialSurgesColumn);
            ClearColumn(plannedSubstrateEcologyColumn);
            headerSummaryTexts.Clear();

            // Instantiate headers at index 0 in each column
            foreach (MutationCategoryPresentation presentation in MutationCategoryPresentationCatalog.Ordered)
            {
                RectTransform parentColumn = presentation.CoreCategory.HasValue
                    ? GetColumnForCategory(presentation.CoreCategory.Value)
                    : plannedSubstrateEcologyColumn;
                ApplyColumnWidth(presentation, parentColumn);

                GameObject headerGO = Instantiate(categoryHeaderPrefab, parentColumn);
                headerGO.name = $"Header_{presentation.Key}";
                headerGO.transform.localScale = Vector3.one;

                float columnWidth = presentation.PreferredWidth;

                // ── Ensure header has a background Image + readable label ──
                // The prefab root has TMP (a Graphic). Unity allows only one Graphic
                // per GO, so we can't add Image to the root. Strategy:
                //   - Keep root TMP enabled + set text (provides layout preferred-width)
                //     but make it invisible (Color.clear).
                //   - Child 0: "HeaderBG"    — Image (draws first)
                //   - Child 1: "HeaderLabel" — TMP   (draws on top, white text)
                var headerBG = headerGO.GetComponent<Image>();
                TextMeshProUGUI headerText;

                if (headerBG != null)
                {
                    // Prefab already has an Image on root — just use existing TMP
                    headerText = headerGO.GetComponentInChildren<TextMeshProUGUI>();
                }
                else
                {
                    // Keep root TMP for layout sizing, but make it invisible
                    var rootTMP = headerGO.GetComponent<TextMeshProUGUI>();
                    if (rootTMP != null)
                    {
                        rootTMP.text = $"{presentation.IconGlyph}  {presentation.DisplayName}";
                        rootTMP.color = Color.clear; // invisible but drives preferred width
                    }

                    // Background child (sibling index 0 → draws first)
                    var bgGO = new GameObject("HeaderBG");
                    bgGO.transform.SetParent(headerGO.transform, false);
                    headerBG = bgGO.AddComponent<Image>();
                    var bgRect = bgGO.GetComponent<RectTransform>();
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;

                    // Label child (sibling index 1 → draws on top of BG)
                    var labelGO = new GameObject("HeaderLabel");
                    labelGO.transform.SetParent(headerGO.transform, false);
                    headerText = labelGO.AddComponent<TextMeshProUGUI>();
                    if (rootTMP != null)
                    {
                        headerText.font = rootTMP.font;
                        headerText.fontSize = rootTMP.fontSize;
                        headerText.fontStyle = rootTMP.fontStyle;
                        headerText.alignment = rootTMP.alignment;
                        headerText.enableAutoSizing = rootTMP.enableAutoSizing;
                        headerText.fontSizeMin = rootTMP.fontSizeMin;
                        headerText.fontSizeMax = rootTMP.fontSizeMax;
                    }
                    var labelRect = labelGO.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                }

                headerBG.color = MutationTreeColors.GetCategoryHeaderBG(presentation.Accent, 0.95f);
                ConfigureHeaderTitleRect(headerBG.rectTransform);

                if (headerText != null)
                {
                    headerText.text = $"{presentation.IconGlyph}  {presentation.DisplayName}";
                    if (headerText.gameObject != headerGO)
                    {
                        ConfigureHeaderTitleRect(headerText.rectTransform);
                    }
                    headerText.color = Color.white;
                }

                var headerLayout = headerGO.GetComponent<LayoutElement>();
                if (headerLayout == null)
                {
                    headerLayout = headerGO.AddComponent<LayoutElement>();
                }

                headerLayout.preferredWidth = columnWidth;
                headerLayout.minWidth = columnWidth;
                headerLayout.flexibleWidth = 0f;
                headerLayout.minHeight = HeaderTotalHeight;
                headerLayout.preferredHeight = HeaderTotalHeight;
                headerLayout.flexibleHeight = 0f;

                var headerRect = headerGO.GetComponent<RectTransform>();
                if (headerRect != null)
                {
                    headerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, columnWidth);
                    headerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, HeaderTotalHeight);
                }

                AttachHeaderTooltip(headerGO, presentation.TooltipText);

                // ── Investment summary label (child text, created dynamically) ──
                var summaryGO = new GameObject("InvestmentSummary");
                summaryGO.transform.SetParent(headerGO.transform, false);
                var summaryText = summaryGO.AddComponent<TextMeshProUGUI>();
                summaryText.fontSize = HeaderInvestmentSummaryFontSize;
                summaryText.fontSizeMax = HeaderInvestmentSummaryFontSize;
                summaryText.fontSizeMin = HeaderInvestmentSummaryMinFontSize;
                summaryText.alignment = TextAlignmentOptions.Center;
                summaryText.color = MutationTreeColors.UniformSubheaderText;
                summaryText.enableAutoSizing = true;
                summaryText.textWrappingMode = TextWrappingModes.NoWrap;
                summaryText.overflowMode = TextOverflowModes.Truncate;
                var summaryRect = summaryGO.GetComponent<RectTransform>();
                summaryRect.anchorMin = new Vector2(0, 1);
                summaryRect.anchorMax = new Vector2(1, 1);
                summaryRect.pivot = new Vector2(0.5f, 1f);
                summaryRect.anchoredPosition = new Vector2(0, -HeaderTitleHeight);
                summaryRect.sizeDelta = new Vector2(0, HeaderInvestmentSummaryHeight);
                if (presentation.CoreCategory.HasValue)
                {
                    headerSummaryTexts[presentation.CoreCategory.Value] = summaryText;
                }
                else
                {
                    summaryText.text = "Planned • roster in design";
                }

                headerGO.transform.SetSiblingIndex(0); // Ensure header is always first

                if (presentation.IsPlanned)
                {
                    CreatePlannedLaneCard(parentColumn, presentation);
                }
            }

            List<MutationNodeUI> createdNodes = new List<MutationNodeUI>();

            // Group by column/category, then sort within each by row
            var mutationsWithLayout = mutations
                .Select(m => (mutation: m, meta: layout.TryGetValue(m.Id, out var meta) ? meta : null))
                .Where(t => t.meta != null)
                .GroupBy(t => t.meta.Category);

            foreach (var group in mutationsWithLayout)
            {
                bool directionalTendrilsCreated = false;
                // Sort by row
                foreach (var (mutation, meta) in group.OrderBy(t => t.meta.Row))
                {
                    RectTransform parentColumn = GetColumnForCategory(meta.Category);

                    if (DirectionalTendrilOrder.Contains(mutation.Id))
                    {
                        if (!directionalTendrilsCreated)
                        {
                            IEnumerable<Mutation> tendrils = DirectionalTendrilOrder
                                .Select(id => group.Select(entry => entry.mutation).First(candidate => candidate.Id == id));
                            createdNodes.AddRange(CreateDirectionalTendrilsCard(
                                tendrils,
                                parentColumn,
                                meta.Row + 1,
                                player,
                                uiManager));
                            directionalTendrilsCreated = true;
                        }
                        continue;
                    }

                    createdNodes.Add(CreateMutationNode(mutation, parentColumn, meta.Row + 1, player, uiManager));
                }
            }

            // Update investment summaries now that all nodes exist
            UpdateCategoryInvestmentSummaries(createdNodes, player);

            return createdNodes;
        }

        private MutationNodeUI CreateMutationNode(
            Mutation mutation,
            RectTransform parent,
            int siblingIndex,
            Player player,
            UI_MutationManager uiManager,
            bool compact = false,
            string compactName = null)
        {
            GameObject nodeObject = Instantiate(mutationNodePrefab, parent);
            nodeObject.name = $"MutationNode_{mutation.Name}";
            nodeObject.transform.localScale = Vector3.one;
            nodeObject.transform.SetSiblingIndex(siblingIndex);

            var nodeLayout = nodeObject.GetComponent<LayoutElement>();
            if (nodeLayout != null)
            {
                nodeLayout.preferredWidth = compact ? 91f : MutationNodeWidth;
                nodeLayout.preferredHeight = MutationNodeHeight;
            }

            MutationNodeUI node = nodeObject.GetComponent<MutationNodeUI>();
            node.Initialize(mutation, player, uiManager);
            if (compact)
            {
                node.SetCompactLayout(compactName);
            }

            Transform lockOverlay = nodeObject.transform.Find("UI_LockOverlay");
            if (lockOverlay != null)
            {
                lockOverlay.SetAsLastSibling();
                Image image = lockOverlay.GetComponent<Image>();
                if (image != null && image.sprite == null)
                {
                    Debug.LogWarning($"🔒 UI_LockOverlay exists on {mutation.Name} but has no sprite assigned.");
                }
            }

            return node;
        }

        private IEnumerable<MutationNodeUI> CreateDirectionalTendrilsCard(
            IEnumerable<Mutation> tendrils,
            RectTransform parentColumn,
            int siblingIndex,
            Player player,
            UI_MutationManager uiManager)
        {
            var cardObject = new GameObject("DirectionalTendrils", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            cardObject.layer = parentColumn.gameObject.layer;
            cardObject.transform.SetParent(parentColumn, false);
            cardObject.transform.SetSiblingIndex(siblingIndex);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MutationCategoryPresentationCatalog.Growth.PreferredWidth);
            cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, DirectionalTendrilsCardHeight);
            Image background = cardObject.GetComponent<Image>();
            background.color = Color.Lerp(UIStyleTokens.Surface.PanelPrimary, UIStyleTokens.Category.Growth, 0.10f);
            background.raycastTarget = false;
            LayoutElement cardLayout = cardObject.GetComponent<LayoutElement>();
            cardLayout.minWidth = MutationCategoryPresentationCatalog.Growth.PreferredWidth;
            cardLayout.preferredWidth = MutationCategoryPresentationCatalog.Growth.PreferredWidth;
            cardLayout.minHeight = DirectionalTendrilsCardHeight;
            cardLayout.preferredHeight = DirectionalTendrilsCardHeight;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.layer = cardObject.layer;
            titleObject.transform.SetParent(cardObject.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 38f);
            TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
            title.text = "Directional Tendrils";
            title.fontSize = 16f;
            title.fontStyle = FontStyles.Bold;
            title.color = MutationTreeColors.PrimaryText;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;

            var gridObject = new GameObject("Quadrants", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.layer = cardObject.layer;
            gridObject.transform.SetParent(cardObject.transform, false);
            RectTransform gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(6f, 6f);
            gridRect.offsetMax = new Vector2(-6f, -38f);
            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(91f, MutationNodeHeight);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            var compactNames = new Dictionary<int, string>
            {
                { MutationIds.TendrilNorthwest, "↖ NW" },
                { MutationIds.TendrilNortheast, "↗ NE" },
                { MutationIds.TendrilSouthwest, "↙ SW" },
                { MutationIds.TendrilSoutheast, "↘ SE" }
            };

            var nodes = new List<MutationNodeUI>();
            int index = 0;
            foreach (Mutation tendril in tendrils)
            {
                nodes.Add(CreateMutationNode(tendril, gridRect, index++, player, uiManager, compact: true, compactName: compactNames[tendril.Id]));
            }
            return nodes;
        }

        private static void ConfigureHeaderTitleRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, HeaderTitleHeight);
        }

        /// <summary>
        /// Recalculates "X / Y invested" text for each category header.
        /// Call after mutations are built or after any upgrade.
        /// </summary>
        public void UpdateCategoryInvestmentSummaries(List<MutationNodeUI> nodes, Player player)
        {
            if (nodes == null || player == null) return;

            // Aggregate levels per category
            var categoryTotals = new Dictionary<MutationCategory, (int current, int max)>();

            foreach (var node in nodes)
            {
                var mutation = node.GetMutation();
                if (mutation == null) continue;

                var cat = mutation.Category;
                int level = player.GetMutationLevel(mutation.Id);
                int maxLevel = mutation.MaxLevel;

                if (!categoryTotals.ContainsKey(cat))
                    categoryTotals[cat] = (0, 0);

                var (c, m) = categoryTotals[cat];
                categoryTotals[cat] = (c + level, m + maxLevel);
            }

            foreach (var kvp in headerSummaryTexts)
            {
                if (categoryTotals.TryGetValue(kvp.Key, out var totals))
                    kvp.Value.text = $"{totals.current} / {totals.max} invested";
                else
                    kvp.Value.text = "";
            }
        }

        private static void AttachHeaderTooltip(GameObject headerGO, string tooltipText)
        {
            if (headerGO == null || string.IsNullOrWhiteSpace(tooltipText))
            {
                return;
            }

            var tooltipTrigger = headerGO.GetComponent<TooltipTrigger>();
            if (tooltipTrigger == null)
            {
                tooltipTrigger = headerGO.AddComponent<TooltipTrigger>();
            }

            tooltipTrigger.SetStaticText(tooltipText);
            tooltipTrigger.SetAutoPlacementOffsetX(12f);
        }

        private static void ApplyColumnWidth(MutationCategoryPresentation presentation, RectTransform column)
        {
            if (column == null)
            {
                return;
            }

            float width = presentation.PreferredWidth;
            var layout = column.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = width;
                layout.minWidth = width;
                layout.flexibleWidth = 0f;
            }

            column.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private RectTransform GetColumnForCategory(MutationCategory category)
        {
            return category switch
            {
                MutationCategory.Growth => growthColumn,
                MutationCategory.CellularResilience => resilienceColumn,
                MutationCategory.Fungicide => fungicideColumn,
                MutationCategory.GeneticDrift => driftColumn,
                MutationCategory.MycelialSurges => mycelialSurgesColumn,
                _ => throw new System.Exception($"❌ Unknown mutation category: {category}")
            };
        }

        public void AssignColumnParentsFromHierarchy()
        {
            growthColumn = transform.Find("UI_MutationScrollViewContent/Column_Growth")?.GetComponent<RectTransform>();
            resilienceColumn = transform.Find("UI_MutationScrollViewContent/Column_CellularResilience")?.GetComponent<RectTransform>();
            fungicideColumn = transform.Find("UI_MutationScrollViewContent/Column_Fungicide")?.GetComponent<RectTransform>();
            driftColumn = transform.Find("UI_MutationScrollViewContent/Column_GeneticDrift")?.GetComponent<RectTransform>();
            mycelialSurgesColumn = transform.Find("UI_MutationScrollViewContent/Column_MycelialSurges")?.GetComponent<RectTransform>();

            if (growthColumn == null || resilienceColumn == null || fungicideColumn == null || driftColumn == null || mycelialSurgesColumn == null)
                Debug.LogError("❌ One or more columns could not be found in AssignColumnParentsFromHierarchy().");
            else
                EnsurePlannedSubstrateEcologyColumn();
            //else
            //Debug.Log("✅ Successfully assigned all column parents at runtime.");
        }

        private void ClearColumn(RectTransform column)
        {
            for (int i = column.childCount - 1; i >= 0; i--)
            {
                Destroy(column.GetChild(i).gameObject);
            }
        }

        private void EnsurePlannedSubstrateEcologyColumn()
        {
            if (plannedSubstrateEcologyColumn != null)
            {
                return;
            }

            RectTransform content = growthColumn != null ? growthColumn.parent as RectTransform : null;
            if (content == null)
            {
                return;
            }

            var columnObject = new GameObject("Column_SubstrateEcology", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            columnObject.layer = content.gameObject.layer;
            plannedSubstrateEcologyColumn = columnObject.GetComponent<RectTransform>();
            plannedSubstrateEcologyColumn.SetParent(content, false);
            plannedSubstrateEcologyColumn.SetAsLastSibling();

            var group = columnObject.GetComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(0, 0, 0, 0);
            group.spacing = 10f;
            group.childAlignment = TextAnchor.UpperCenter;
            group.childControlWidth = true;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
        }

        private static void CreatePlannedLaneCard(RectTransform parentColumn, MutationCategoryPresentation presentation)
        {
            var cardObject = new GameObject("PlannedRosterCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            cardObject.layer = parentColumn.gameObject.layer;
            cardObject.transform.SetParent(parentColumn, false);

            var image = cardObject.GetComponent<Image>();
            image.color = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, presentation.Accent, 0.08f);

            var layout = cardObject.GetComponent<LayoutElement>();
            layout.preferredWidth = presentation.PreferredWidth;
            layout.minWidth = presentation.PreferredWidth;
            layout.preferredHeight = PlannedLaneCardHeight;
            layout.minHeight = PlannedLaneCardHeight;

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, presentation.PreferredWidth);
            cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, PlannedLaneCardHeight);

            var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = cardObject.layer;
            textObject.transform.SetParent(cardObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 10f);
            textRect.offsetMax = new Vector2(-12f, -10f);

            var message = textObject.GetComponent<TextMeshProUGUI>();
            message.text = "<b>Roster in design</b>\nNot yet purchasable";
            message.fontSize = 15f;
            message.alignment = TextAlignmentOptions.Center;
            message.color = MutationTreeColors.SecondaryText;
            message.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
