#nullable enable

using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Persistent, runtime-built mutation explanation surface. It reuses Core presentation
    /// snapshots and the node's mechanic-specific level summary without owning purchase rules.
    /// </summary>
    internal sealed class MutationInspectorPanel : MonoBehaviour
    {
        public const float PreferredWidth = 340f;
        public const float OuterGap = 12f;
        private const float PanelPadding = 16f;
        private const float SectionSpacing = 10f;
        private const float ChipHeight = 36f;

        private RectTransform rootRect = null!;
        private RectTransform contentRect = null!;
        private TextMeshProUGUI titleText = null!;
        private TextMeshProUGUI metadataText = null!;
        private TextMeshProUGUI summaryText = null!;
        private TextMeshProUGUI stateText = null!;
        private TextMeshProUGUI costText = null!;
        private TextMeshProUGUI currentLevelText = null!;
        private TextMeshProUGUI nextLevelText = null!;
        private TextMeshProUGUI maxLevelBonusText = null!;
        private TextMeshProUGUI synergyText = null!;
        private TextMeshProUGUI emptyRequirementsText = null!;
        private TextMeshProUGUI emptyDependentsText = null!;
        private RectTransform requirementsRoot = null!;
        private RectTransform dependentsRoot = null!;
        private readonly List<Button> requirementButtons = new();
        private readonly List<Button> dependentButtons = new();
        private TMP_FontAsset? font;

        public static MutationInspectorPanel Create(RectTransform parent, TMP_FontAsset? font)
        {
            var inspectorObject = new GameObject(
                "UI_MutationInspector",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MutationInspectorPanel));
            inspectorObject.transform.SetParent(parent, false);

            var inspector = inspectorObject.GetComponent<MutationInspectorPanel>();
            inspector.font = font;
            inspector.Build();
            return inspector;
        }

        public void SetLayout(float topInset, float width)
        {
            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-OuterGap, -topInset);
            rootRect.sizeDelta = new Vector2(width, -(topInset + OuterGap));
        }

        public void Show(
            MutationNodeUI node,
            Mutation mutation,
            Player player,
            UI_MutationManager manager,
            Action<int> focusMutation)
        {
            MutationProgressSnapshot snapshot = MutationProgressSnapshot.Create(mutation, player);
            MutationDescriptionSections sections = mutation.DescriptionSections;

            titleText.text = mutation.Name;
            titleText.color = MutationTreeColors.GetCategoryAccent(mutation.Category);
            metadataText.text = $"Tier {mutation.TierNumber}  •  {GetCategoryDisplayName(mutation.Category)}";
            summaryText.text = sections.Summary;
            stateText.text = BuildStateText(snapshot, player, manager);
            stateText.color = GetStateColor(snapshot, player, manager);
            costText.text = BuildCostText(snapshot);

            currentLevelText.text = BuildLevelText(
                $"Current level {snapshot.CurrentLevel} / {mutation.MaxLevel}",
                node.BuildLevelSummary(snapshot.CurrentLevel));
            nextLevelText.gameObject.SetActive(snapshot.NextLevel.HasValue);
            if (snapshot.NextLevel.HasValue)
            {
                nextLevelText.text = BuildLevelText(
                    $"Next level {snapshot.NextLevel.Value}",
                    node.BuildLevelSummary(snapshot.NextLevel.Value));
            }

            maxLevelBonusText.gameObject.SetActive(sections.HasMaxLevelBonus);
            maxLevelBonusText.text = sections.HasMaxLevelBonus
                ? $"<b>Max-level bonus</b>\n{sections.MaxLevelBonus}"
                : string.Empty;

            synergyText.gameObject.SetActive(sections.HasBuffingMutations);
            synergyText.text = sections.HasBuffingMutations
                ? $"<b>Buffed by</b>\n{string.Join("\n", sections.BuffingMutations)}"
                : string.Empty;

            ConfigureRequirementButtons(snapshot.Requirements, focusMutation);
            ConfigureDependentButtons(snapshot.DirectDependents, focusMutation);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        public void Clear()
        {
            titleText.text = "Inspect a mutation";
            titleText.color = UIStyleTokens.Text.Primary;
            metadataText.text = "Hover a node to compare its next level.";
            summaryText.text = "Requirements and direct unlocks stay here while you move around the tree.";
            stateText.text = string.Empty;
            costText.text = string.Empty;
            currentLevelText.text = string.Empty;
            nextLevelText.gameObject.SetActive(false);
            maxLevelBonusText.gameObject.SetActive(false);
            synergyText.gameObject.SetActive(false);
            ConfigureButtons(requirementButtons, requirementsRoot, Array.Empty<ChipData>(), null);
            ConfigureButtons(dependentButtons, dependentsRoot, Array.Empty<ChipData>(), null);
            emptyRequirementsText.gameObject.SetActive(true);
            emptyDependentsText.gameObject.SetActive(true);
        }

        private void Build()
        {
            rootRect = GetComponent<RectTransform>();
            Image background = GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelPrimary;
            background.raycastTarget = true;

            var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(transform, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            Stretch(scrollRectTransform, PanelPadding);

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            Stretch(viewportRect, 0f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = SectionSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            titleText = CreateText("Title", 26f, 42f, FontStyles.Bold, UIStyleTokens.Text.Primary);
            metadataText = CreateText("Metadata", 14f, 22f, FontStyles.Italic, UIStyleTokens.Text.Secondary);
            summaryText = CreateText("Summary", 18f, 82f, FontStyles.Normal, UIStyleTokens.Text.Primary);
            stateText = CreateText("State", 16f, 26f, FontStyles.Bold, UIStyleTokens.State.Info);
            costText = CreateText("Cost", 16f, 30f, FontStyles.Normal, UIStyleTokens.Text.Secondary);
            currentLevelText = CreateText("CurrentLevel", 16f, 70f, FontStyles.Normal, UIStyleTokens.Text.Primary, UIStyleTokens.Surface.PanelSecondary);
            nextLevelText = CreateText("NextLevel", 16f, 70f, FontStyles.Normal, UIStyleTokens.Text.Primary, UIStyleTokens.Surface.PanelElevated);

            _ = CreateText("RequirementsLabel", 16f, 24f, FontStyles.Bold, UIStyleTokens.Accent.Spore, text: "Requirements");
            requirementsRoot = CreateChipRoot("Requirements");
            emptyRequirementsText = CreateText("NoRequirements", 14f, 22f, FontStyles.Italic, UIStyleTokens.Text.Muted, text: "Root mutation — no prerequisites");

            _ = CreateText("UnlocksLabel", 16f, 24f, FontStyles.Bold, UIStyleTokens.Accent.Spore, text: "Direct unlocks");
            dependentsRoot = CreateChipRoot("Dependents");
            emptyDependentsText = CreateText("NoDependents", 14f, 22f, FontStyles.Italic, UIStyleTokens.Text.Muted, text: "No direct dependents");

            maxLevelBonusText = CreateText("MaxLevelBonus", 15f, 64f, FontStyles.Normal, UIStyleTokens.State.Warning, UIStyleTokens.Surface.PanelSecondary);
            synergyText = CreateText("Synergy", 15f, 52f, FontStyles.Normal, UIStyleTokens.Text.Secondary, UIStyleTokens.Surface.PanelSecondary);
            _ = CreateText("Hint", 14f, 42f, FontStyles.Italic, UIStyleTokens.Text.Muted, text: "Click a requirement or unlock to focus it. Purchases remain immediate on the mutation cards.");

            Clear();
        }

        private void ConfigureRequirementButtons(IReadOnlyList<MutationRequirementProgress> requirements, Action<int> focusMutation)
        {
            var chips = new List<ChipData>(requirements.Count);
            foreach (MutationRequirementProgress requirement in requirements)
            {
                string marker = requirement.IsMet ? "✓" : "○";
                chips.Add(new ChipData(
                    requirement.MutationId,
                    $"{marker} {requirement.MutationName}  L{requirement.CurrentLevel}/{requirement.RequiredLevel}",
                    requirement.IsMet ? UIStyleTokens.State.Success : UIStyleTokens.State.Warning));
            }

            ConfigureButtons(requirementButtons, requirementsRoot, chips, focusMutation);
            emptyRequirementsText.gameObject.SetActive(chips.Count == 0);
        }

        private void ConfigureDependentButtons(IReadOnlyList<Mutation> dependents, Action<int> focusMutation)
        {
            var chips = new List<ChipData>(dependents.Count);
            foreach (Mutation dependent in dependents)
            {
                chips.Add(new ChipData(
                    dependent.Id,
                    dependent.Name,
                    MutationTreeColors.GetCategoryAccent(dependent.Category)));
            }

            ConfigureButtons(dependentButtons, dependentsRoot, chips, focusMutation);
            emptyDependentsText.gameObject.SetActive(chips.Count == 0);
        }

        private void ConfigureButtons(
            List<Button> buttons,
            RectTransform parent,
            IReadOnlyList<ChipData> chips,
            Action<int>? focusMutation)
        {
            while (buttons.Count < chips.Count)
            {
                buttons.Add(CreateChipButton(parent));
            }

            for (int index = 0; index < buttons.Count; index++)
            {
                Button button = buttons[index];
                bool active = index < chips.Count;
                button.gameObject.SetActive(active);
                button.onClick.RemoveAllListeners();
                if (!active)
                {
                    continue;
                }

                ChipData chip = chips[index];
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.text = chip.Label;
                label.color = UIStyleTokens.Text.Primary;
                Image image = button.GetComponent<Image>();
                image.color = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, chip.Accent, 0.16f);
                UIStyleTokens.Button.ApplyPanelSecondaryStyle(button);
                image.color = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, chip.Accent, 0.16f);
                button.onClick.AddListener(() => focusMutation?.Invoke(chip.MutationId));
            }
        }

        private Button CreateChipButton(RectTransform parent)
        {
            var buttonObject = new GameObject("MutationChip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredHeight = ChipHeight;
            element.minHeight = ChipHeight;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect, 8f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 14f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            return button;
        }

        private RectTransform CreateChipRoot(string name)
        {
            var rootObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            rootObject.transform.SetParent(contentRect, false);
            VerticalLayoutGroup layout = rootObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = rootObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rootObject.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI CreateText(
            string name,
            float fontSize,
            float preferredHeight,
            FontStyles fontStyle,
            Color color,
            Color? background = null,
            string text = "")
        {
            var rootObject = background.HasValue
                ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement))
                : new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            rootObject.transform.SetParent(contentRect, false);

            GameObject textObject = rootObject;
            if (background.HasValue)
            {
                Image image = rootObject.GetComponent<Image>();
                image.color = background.Value;
                image.raycastTarget = false;

                textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(rootObject.transform, false);
                Stretch(textObject.GetComponent<RectTransform>(), 0f);
            }

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>() ?? textObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.margin = background.HasValue ? new Vector4(10f, 8f, 10f, 8f) : Vector4.zero;
            label.raycastTarget = false;

            LayoutElement element = rootObject.GetComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.minHeight = preferredHeight;
            return label;
        }

        private static string BuildStateText(MutationProgressSnapshot snapshot, Player player, UI_MutationManager manager)
        {
            if (snapshot.IsMaxed) return "FULLY UPGRADED";
            if (snapshot.IsActiveSurge) return $"ACTIVE — {player.GetSurgeTurnsRemaining(snapshot.Mutation.Id)} rounds remain";
            if (IsPendingUntilNextRound(snapshot, player)) return "UNLOCKS NEXT ROUND";
            if (snapshot.HasUnmetPrerequisites) return "LOCKED — requirements unmet";
            if (manager.IsMutationDisabledBecauseNoEffect(snapshot.Mutation, player)) return "NO VALID TARGET RIGHT NOW";
            if (snapshot.IsAffordable) return snapshot.CurrentLevel > 0 ? "READY TO UPGRADE" : "AVAILABLE";
            return $"NEED {snapshot.Cost - snapshot.AvailablePoints} MORE MUTATION POINT{(snapshot.Cost - snapshot.AvailablePoints == 1 ? string.Empty : "S")}";
        }

        private static Color GetStateColor(MutationProgressSnapshot snapshot, Player player, UI_MutationManager manager)
        {
            if (snapshot.IsMaxed) return MutationTreeColors.MaxedGold;
            if (snapshot.IsActiveSurge) return MutationTreeColors.GetCategoryAccent(snapshot.Mutation.Category);
            if (IsPendingUntilNextRound(snapshot, player) || snapshot.HasUnmetPrerequisites || manager.IsMutationDisabledBecauseNoEffect(snapshot.Mutation, player))
                return UIStyleTokens.State.Warning;
            if (snapshot.IsAffordable) return UIStyleTokens.State.Success;
            return UIStyleTokens.Text.Muted;
        }

        private static bool IsPendingUntilNextRound(MutationProgressSnapshot snapshot, Player player)
        {
            var board = GameManager.Instance?.Board;
            return board != null
                && snapshot.Mutation.Prerequisites.Count > 0
                && player.PlayerMutations.TryGetValue(snapshot.Mutation.Id, out var playerMutation)
                && playerMutation.PrereqMetRound.HasValue
                && playerMutation.PrereqMetRound.Value == board.CurrentRound;
        }

        private static string BuildCostText(MutationProgressSnapshot snapshot)
        {
            if (snapshot.IsMaxed)
            {
                return "No further levels available.";
            }

            string pointWord = snapshot.Cost == 1 ? "point" : "points";
            return snapshot.IsAffordable
                ? $"Cost: {snapshot.Cost} mutation {pointWord}  •  After purchase: {snapshot.ProjectedPointsAfterPurchase}"
                : $"Cost: {snapshot.Cost} mutation {pointWord}  •  Available: {snapshot.AvailablePoints}";
        }

        private static string BuildLevelText(string heading, string summary)
        {
            return string.IsNullOrWhiteSpace(summary)
                ? $"<b>{heading}</b>"
                : $"<b>{heading}</b>\n{summary}";
        }

        private static string GetCategoryDisplayName(MutationCategory category)
        {
            return category switch
            {
                MutationCategory.CellularResilience => "Cellular Resilience",
                MutationCategory.GeneticDrift => "Genetic Drift",
                MutationCategory.MycelialSurges => "Mycelial Surges",
                _ => category.ToString()
            };
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private readonly struct ChipData
        {
            public ChipData(int mutationId, string label, Color accent)
            {
                MutationId = mutationId;
                Label = label;
                Accent = accent;
            }

            public int MutationId { get; }
            public string Label { get; }
            public Color Accent { get; }
        }
    }
}
