using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.UI;

namespace FungusToast.UI.MutationTree
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

        public void BuildTree(IEnumerable<Mutation> mutations, Dictionary<int, MutationLayoutMetadata> layout, Player player, UI_MutationManager uiManager)
        {
            if (growthColumn == null || resilienceColumn == null || fungicideColumn == null || driftColumn == null)
            {
                Debug.LogError("❌ MutationTreeBuilder: One or more column containers are not assigned.");
                return;
            }

            // Clear existing children from each column
            ClearColumn(growthColumn);
            ClearColumn(resilienceColumn);
            ClearColumn(fungicideColumn);
            ClearColumn(driftColumn);

            // Track which categories we've already created headers for
            HashSet<MutationCategory> createdHeaders = new HashSet<MutationCategory>();

            foreach (var mutation in mutations.OrderBy(m => m.Id))
            {
                if (!layout.TryGetValue(mutation.Id, out var metadata))
                {
                    Debug.LogWarning($"⚠️ No layout metadata for mutation ID {mutation.Id} ({mutation.Name})");
                    continue;
                }

                RectTransform parentColumn = GetColumnForCategory(metadata.Category);

                // Create a header for this category if not already added
                if (!createdHeaders.Contains(metadata.Category))
                {
                    createdHeaders.Add(metadata.Category);
                    Debug.Log($"🧬 Placing {mutation.Name} under {metadata.Category} in {parentColumn.name}");
                    GameObject headerGO = Instantiate(categoryHeaderPrefab, parentColumn);
                    headerGO.name = $"Header_{metadata.Category}";
                    headerGO.transform.localScale = Vector3.one;

                    var text = headerGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null)
                        text.text = metadata.Category.ToString();
                }

                // Add the mutation node
                GameObject nodeGO = Instantiate(mutationNodePrefab, parentColumn);
                nodeGO.name = $"MutationNode_{mutation.Name}";
                nodeGO.transform.localScale = Vector3.one;
                var mutationNodeLayout = nodeGO.GetComponent<LayoutElement>();
                if (mutationNodeLayout != null)
                {
                    mutationNodeLayout.preferredWidth = 120;
                    mutationNodeLayout.preferredHeight = 120;
                }

                MutationNodeUI nodeUI = nodeGO.GetComponent<MutationNodeUI>();
                nodeUI.Initialize(mutation, player, uiManager);
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
                _ => throw new System.Exception($"❌ Unknown mutation category: {category}")
            };
        }

        public void AssignColumnParentsFromHierarchy()
        {
            growthColumn = transform.Find("UI_MutationScrollViewContent/GrowthColumn")?.GetComponent<RectTransform>();
            resilienceColumn = transform.Find("UI_MutationScrollViewContent/ResilienceColumn")?.GetComponent<RectTransform>();
            fungicideColumn = transform.Find("UI_MutationScrollViewContent/FungicideColumn")?.GetComponent<RectTransform>();
            driftColumn = transform.Find("UI_MutationScrollViewContent/DriftColumn")?.GetComponent<RectTransform>();

            if (growthColumn == null || resilienceColumn == null || fungicideColumn == null || driftColumn == null)
                Debug.LogError("❌ One or more columns could not be found in AssignColumnParentsFromHierarchy().");
            else
                Debug.Log("✅ Successfully assigned all column parents at runtime.");
        }

        private void ClearColumn(RectTransform column)
        {
            for (int i = column.childCount - 1; i >= 0; i--)
            {
                Destroy(column.GetChild(i).gameObject);
            }
        }
    }
}
