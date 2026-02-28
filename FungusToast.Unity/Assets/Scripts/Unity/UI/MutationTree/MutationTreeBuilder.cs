using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI.MutationTree
{
    public class MutationTreeBuilder : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject categoryHeaderPrefab;
        [SerializeField] private GameObject mutationNodePrefab;

        [Header("Column Parents")]
        [SerializeField] private RectTransform growthColumn;
        [SerializeField] private RectTransform resilienceColumn;
        [SerializeField] private RectTransform fungicideColumn;
        [SerializeField] private RectTransform driftColumn;
        [SerializeField] private RectTransform mycelialSurgesColumn;

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

            ClearColumn(growthColumn);
            ClearColumn(resilienceColumn);
            ClearColumn(fungicideColumn);
            ClearColumn(driftColumn);
            ClearColumn(mycelialSurgesColumn);
            headerSummaryTexts.Clear();

            // Instantiate headers at index 0 in each column
            var headerGOs = new Dictionary<MutationCategory, GameObject>();
            foreach (var (category, parentColumn) in new[] {
                (MutationCategory.Growth, growthColumn),
                (MutationCategory.CellularResilience, resilienceColumn),
                (MutationCategory.Fungicide, fungicideColumn),
                (MutationCategory.GeneticDrift, driftColumn),
                (MutationCategory.MycelialSurges, mycelialSurgesColumn)
            })
            {
                GameObject headerGO = Instantiate(categoryHeaderPrefab, parentColumn);
                headerGO.name = $"Header_{category}";
                headerGO.transform.localScale = Vector3.one;

                // ── Category accent colors on header ──
                Color accent = MutationTreeColors.GetCategoryAccent(category);

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
                        rootTMP.text = SplitCamelCase(category.ToString());
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

                headerBG.color = MutationTreeColors.GetCategoryHeaderBG(category, 0.95f);

                if (headerText != null)
                {
                    headerText.text = SplitCamelCase(category.ToString());
                    headerText.color = Color.white;
                }

                // ── Investment summary label (child text, created dynamically) ──
                var summaryGO = new GameObject("InvestmentSummary");
                summaryGO.transform.SetParent(headerGO.transform, false);
                var summaryText = summaryGO.AddComponent<TextMeshProUGUI>();
                summaryText.fontSize = 10;
                summaryText.alignment = TextAlignmentOptions.Center;
                summaryText.color = new Color(
                    Mathf.Min(accent.r + 0.2f, 1f),
                    Mathf.Min(accent.g + 0.2f, 1f),
                    Mathf.Min(accent.b + 0.2f, 1f),
                    0.85f);
                summaryText.enableAutoSizing = false;
                summaryText.overflowMode = TextOverflowModes.Ellipsis;
                var summaryRect = summaryGO.GetComponent<RectTransform>();
                summaryRect.anchorMin = new Vector2(0, 0);
                summaryRect.anchorMax = new Vector2(1, 0);
                summaryRect.pivot = new Vector2(0.5f, 1f);
                summaryRect.anchoredPosition = new Vector2(0, 0);
                summaryRect.sizeDelta = new Vector2(0, 16);
                headerSummaryTexts[category] = summaryText;

                headerGO.transform.SetSiblingIndex(0); // Ensure header is always first
                headerGOs[category] = headerGO;
            }

            List<MutationNodeUI> createdNodes = new List<MutationNodeUI>();

            // Group by column/category, then sort within each by row
            var mutationsWithLayout = mutations
                .Select(m => (mutation: m, meta: layout.TryGetValue(m.Id, out var meta) ? meta : null))
                .Where(t => t.meta != null)
                .GroupBy(t => t.meta.Category);

            foreach (var group in mutationsWithLayout)
            {
                // Sort by row
                foreach (var (mutation, meta) in group.OrderBy(t => t.meta.Row))
                {
                    RectTransform parentColumn = GetColumnForCategory(meta.Category);

                    GameObject nodeGO = Instantiate(mutationNodePrefab, parentColumn);
                    nodeGO.name = $"MutationNode_{mutation.Name}";
                    nodeGO.transform.localScale = Vector3.one;

                    // Set to row+1 to account for header at index 0
                    nodeGO.transform.SetSiblingIndex(meta.Row + 1);

                    var mutationNodeLayout = nodeGO.GetComponent<LayoutElement>();
                    if (mutationNodeLayout != null)
                    {
                        mutationNodeLayout.preferredWidth = 120;
                        mutationNodeLayout.preferredHeight = 120;
                    }

                    MutationNodeUI nodeUI = nodeGO.GetComponent<MutationNodeUI>();
                    nodeUI.Initialize(mutation, player, uiManager);

                    // Lock overlay and debug info
                    var lockOverlay = nodeGO.transform.Find("UI_LockOverlay");
                    if (lockOverlay != null)
                    {
                        lockOverlay.SetAsLastSibling();
                        var image = lockOverlay.GetComponent<Image>();
                        if (image != null && image.sprite == null)
                        {
                            Debug.LogWarning($"🔒 UI_LockOverlay exists on {mutation.Name} but has no sprite assigned.");
                        }
                    }

                    createdNodes.Add(nodeUI);
                }
            }

            // Update investment summaries now that all nodes exist
            UpdateCategoryInvestmentSummaries(createdNodes, player);

            return createdNodes;
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

        public static string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                input,
                "(\\B[A-Z])",
                " $1"
            );
        }
    }
}
