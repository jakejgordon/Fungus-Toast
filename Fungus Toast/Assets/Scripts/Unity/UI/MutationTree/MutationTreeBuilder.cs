﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.UI;

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

                var text = headerGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                    text.text = SplitCamelCase(category.ToString());

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

            return createdNodes;
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
