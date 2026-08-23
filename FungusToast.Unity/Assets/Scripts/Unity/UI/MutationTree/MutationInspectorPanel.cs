#nullable enable

using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Persistent, runtime-built mutation explanation surface. It reuses Core presentation
    /// snapshots and the node's mechanic-specific level summary without owning purchase rules.
    /// </summary>
    internal sealed class MutationInspectorPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float PreferredWidth = 400f;
        public const float OuterGap = 12f;
        private const float PanelPadding = 16f;
        private const float SectionSpacing = 8f;
        private const float RelatedItemSpacing = 4f;
        private const float ChipHeight = 36f;
        private const float ToolbarHeight = 40f;
        private const float ToolbarGap = 8f;
        private const float RequirementStatusBadgeSize = 22f;
        private const float RequirementStatusBadgeLeftInset = 8f;
        private const float RequirementStatusBadgeToLabelGap = 8f;

        private RectTransform rootRect = null!;
        private RectTransform contentRect = null!;
        private TextMeshProUGUI titleText = null!;
        private TextMeshProUGUI metadataText = null!;
        private TextMeshProUGUI summaryText = null!;
        private TextMeshProUGUI technicalDetailsText = null!;
        private TextMeshProUGUI stateText = null!;
        private TextMeshProUGUI costText = null!;
        private TextMeshProUGUI currentLevelText = null!;
        private TextMeshProUGUI nextLevelText = null!;
        private TextMeshProUGUI maxLevelBonusText = null!;
        private TextMeshProUGUI synergyText = null!;
        private TextMeshProUGUI emptyRequirementsText = null!;
        private TextMeshProUGUI requirementsLabelText = null!;
        private TextMeshProUGUI emptyDependentsText = null!;
        private RectTransform requirementsRoot = null!;
        private RectTransform groupedRequirementsRoot = null!;
        private RectTransform dependentsRoot = null!;
        private readonly List<Button> requirementButtons = new();
        private readonly List<RequirementStatusRow> groupedRequirementRows = new();
        private readonly List<Button> dependentButtons = new();
        private TMP_FontAsset? font;
        private TMP_InputField searchInput = null!;
        private Button pinButton = null!;
        private TextMeshProUGUI pinButtonLabel = null!;
        private Action? pointerEntered;
        private Action? pointerExited;

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
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            RefreshTextHeights();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
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
            titleText.color = MutationTreeColors.GetReadableCategoryAccent(mutation.Category);
            metadataText.text = $"Tier {mutation.TierNumber}  •  {GetCategoryDisplayName(mutation.Category)}";
            summaryText.text = sections.Summary;
            technicalDetailsText.text = $"<b>Technical details</b>\n{sections.TechnicalDetails}";
            SetTextBlockActive(technicalDetailsText, sections.HasTechnicalDetails);
            stateText.text = BuildStateText(snapshot, player, manager);
            stateText.color = GetStateColor(snapshot, player, manager);
            costText.text = BuildCostText(snapshot);
            int requirementGroupCount = GetRequirementGroupCount(snapshot);
            requirementsLabelText.text = requirementGroupCount > 1
                ? "Requirements — ALL required"
                : "Requirements";

            currentLevelText.text = BuildLevelText(
                $"Current level {snapshot.CurrentLevel} / {mutation.MaxLevel}",
                node.BuildLevelSummary(snapshot.CurrentLevel));
            SetTextBlockActive(nextLevelText, snapshot.NextLevel.HasValue);
            if (snapshot.NextLevel.HasValue)
            {
                nextLevelText.text = BuildLevelText(
                    $"Next level {snapshot.NextLevel.Value}",
                    node.BuildLevelSummary(snapshot.NextLevel.Value));
            }

            SetTextBlockActive(maxLevelBonusText, sections.HasMaxLevelBonus);
            maxLevelBonusText.text = sections.HasMaxLevelBonus
                ? $"<b>Max-level bonus</b>\n{sections.MaxLevelBonus}"
                : string.Empty;

            SetTextBlockActive(synergyText, sections.HasBuffingMutations);
            synergyText.text = sections.HasBuffingMutations
                ? $"<b>Buffed by</b>\n{string.Join("\n", sections.BuffingMutations)}"
                : string.Empty;

            ConfigureRequirements(snapshot, focusMutation);
            ConfigureDependentButtons(snapshot.DirectDependents, focusMutation);
            RefreshTextHeights();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        public void Clear()
        {
            titleText.text = "Inspect a mutation";
            titleText.color = UIStyleTokens.Text.Primary;
            metadataText.text = "Hover a node to compare its next level.";
            summaryText.text = "Requirements and direct unlocks stay here while you move around the tree.";
            technicalDetailsText.text = string.Empty;
            SetTextBlockActive(technicalDetailsText, false);
            stateText.text = string.Empty;
            costText.text = string.Empty;
            currentLevelText.text = string.Empty;
            SetTextBlockActive(nextLevelText, false);
            SetTextBlockActive(maxLevelBonusText, false);
            SetTextBlockActive(synergyText, false);
            ConfigureButtons(requirementButtons, requirementsRoot, Array.Empty<ChipData>(), null);
            ConfigureGroupedRequirementRows(Array.Empty<GroupedRequirementData>());
            ConfigureButtons(dependentButtons, dependentsRoot, Array.Empty<ChipData>(), null);
            requirementsRoot.gameObject.SetActive(false);
            groupedRequirementsRoot.gameObject.SetActive(false);
            dependentsRoot.gameObject.SetActive(false);
            emptyRequirementsText.gameObject.SetActive(true);
            emptyDependentsText.gameObject.SetActive(true);
            RefreshTextHeights();
        }

        public void BindWorkspaceControls(
            Action<string> searchChanged,
            Action pinToggled,
            Action inspectorPointerEntered,
            Action inspectorPointerExited)
        {
            searchInput.onValueChanged.RemoveAllListeners();
            searchInput.onValueChanged.AddListener(value => searchChanged?.Invoke(value));
            pinButton.onClick.RemoveAllListeners();
            pinButton.onClick.AddListener(() => pinToggled?.Invoke());
            pointerEntered = inspectorPointerEntered;
            pointerExited = inspectorPointerExited;
        }

        public void SetPinState(bool isPinned, bool canPin)
        {
            pinButton.interactable = canPin;
            pinButtonLabel.text = isPinned ? "Pinned" : "Pin";
            if (isPinned)
            {
                UIStyleTokens.Button.ApplyStyle(pinButton, useSelectedAsNormal: true);
                pinButtonLabel.color = UIStyleTokens.Text.OnAccent;
            }
            else
            {
                UIStyleTokens.Button.ApplyPanelSecondaryStyle(pinButton);
                pinButtonLabel.color = canPin ? UIStyleTokens.Text.Primary : UIStyleTokens.Text.Disabled;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => pointerEntered?.Invoke();

        public void OnPointerExit(PointerEventData eventData) => pointerExited?.Invoke();

        public void FocusSearch()
        {
            searchInput.Select();
            searchInput.ActivateInputField();
        }

        public void ClearSearch() => searchInput.text = string.Empty;

        public bool IsSearchFocused => searchInput != null && searchInput.isFocused;

        private void Build()
        {
            rootRect = GetComponent<RectTransform>();
            Image background = GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelPrimary;
            background.raycastTarget = true;

            BuildWorkspaceToolbar();

            var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(transform, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            Stretch(scrollRectTransform, PanelPadding);
            scrollRectTransform.offsetMax = new Vector2(-PanelPadding, -(PanelPadding + ToolbarHeight + ToolbarGap));

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
            layout.childControlHeight = true;
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

            titleText = CreateText("Title", 26f, 32f, FontStyles.Bold, UIStyleTokens.Text.Primary);
            metadataText = CreateText("Metadata", 14f, 18f, FontStyles.Italic, UIStyleTokens.Text.Secondary);
            summaryText = CreateText("Summary", 18f, 22f, FontStyles.Normal, UIStyleTokens.Text.Primary);
            technicalDetailsText = CreateText("TechnicalDetails", 15f, 32f, FontStyles.Normal, UIStyleTokens.Text.Primary, UIStyleTokens.Surface.PanelSecondary);
            stateText = CreateText("State", 16f, 22f, FontStyles.Bold, UIStyleTokens.State.Info);
            costText = CreateText("Cost", 16f, 22f, FontStyles.Normal, UIStyleTokens.Text.Primary);
            currentLevelText = CreateText("CurrentLevel", 16f, 32f, FontStyles.Normal, UIStyleTokens.Text.Primary, UIStyleTokens.Surface.PanelSecondary);
            nextLevelText = CreateText("NextLevel", 16f, 32f, FontStyles.Normal, UIStyleTokens.Text.Primary, UIStyleTokens.Surface.PanelElevated);

            RectTransform requirementsSection = CreateSectionRoot("RequirementsSection");
            requirementsLabelText = CreateText("RequirementsLabel", 16f, 20f, FontStyles.Bold, UIStyleTokens.Accent.Spore, text: "Requirements", parent: requirementsSection);
            groupedRequirementsRoot = CreateChipRoot("GroupedRequirements", requirementsSection);
            requirementsRoot = CreateChipRoot("Requirements", requirementsSection);
            emptyRequirementsText = CreateText("NoRequirements", 14f, 18f, FontStyles.Italic, UIStyleTokens.Text.Muted, text: "Root mutation — no prerequisites", parent: requirementsSection);

            RectTransform dependentsSection = CreateSectionRoot("DependentsSection");
            _ = CreateText("UnlocksLabel", 16f, 20f, FontStyles.Bold, UIStyleTokens.Accent.Spore, text: "Direct unlocks", parent: dependentsSection);
            dependentsRoot = CreateChipRoot("Dependents", dependentsSection);
            emptyDependentsText = CreateText("NoDependents", 14f, 18f, FontStyles.Italic, UIStyleTokens.Text.Muted, text: "No direct dependents", parent: dependentsSection);

            maxLevelBonusText = CreateText("MaxLevelBonus", 15f, 32f, FontStyles.Normal, UIStyleTokens.State.Warning, UIStyleTokens.Surface.PanelSecondary);
            synergyText = CreateText("Synergy", 15f, 32f, FontStyles.Normal, UIStyleTokens.Text.Primary, UIStyleTokens.Surface.PanelSecondary);
            _ = CreateText("Hint", 14f, 36f, FontStyles.Italic, UIStyleTokens.Text.Muted, text: "Click a requirement or unlock to focus it. Purchases remain immediate on the mutation cards.");

            Clear();
        }

        private void BuildWorkspaceToolbar()
        {
            var toolbarObject = new GameObject("WorkspaceToolbar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            toolbarObject.transform.SetParent(transform, false);
            RectTransform toolbarRect = toolbarObject.GetComponent<RectTransform>();
            toolbarRect.anchorMin = new Vector2(0f, 1f);
            toolbarRect.anchorMax = new Vector2(1f, 1f);
            toolbarRect.pivot = new Vector2(0.5f, 1f);
            toolbarRect.anchoredPosition = new Vector2(0f, -PanelPadding);
            toolbarRect.sizeDelta = new Vector2(-(PanelPadding * 2f), ToolbarHeight);

            HorizontalLayoutGroup toolbarLayout = toolbarObject.GetComponent<HorizontalLayoutGroup>();
            toolbarLayout.spacing = 8f;
            toolbarLayout.childAlignment = TextAnchor.MiddleCenter;
            toolbarLayout.childControlWidth = true;
            toolbarLayout.childControlHeight = true;
            toolbarLayout.childForceExpandWidth = false;
            toolbarLayout.childForceExpandHeight = true;

            var inputObject = new GameObject("Search", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(TMP_InputField), typeof(LayoutElement));
            inputObject.transform.SetParent(toolbarObject.transform, false);
            Image inputBackground = inputObject.GetComponent<Image>();
            inputBackground.color = UIStyleTokens.Surface.PanelSecondary;
            LayoutElement inputLayout = inputObject.GetComponent<LayoutElement>();
            inputLayout.flexibleWidth = 1f;
            inputLayout.minWidth = 90f;
            inputLayout.preferredHeight = ToolbarHeight;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(inputObject.transform, false);
            Stretch(textObject.GetComponent<RectTransform>(), 8f);
            TextMeshProUGUI inputText = textObject.GetComponent<TextMeshProUGUI>();
            if (font != null) inputText.font = font;
            inputText.fontSize = 14f;
            inputText.color = UIStyleTokens.Text.Primary;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            inputText.enableWordWrapping = false;

            var placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            placeholderObject.transform.SetParent(inputObject.transform, false);
            Stretch(placeholderObject.GetComponent<RectTransform>(), 8f);
            TextMeshProUGUI placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
            if (font != null) placeholder.font = font;
            placeholder.text = "Search (Ctrl+F)";
            placeholder.fontSize = 14f;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = UIStyleTokens.Text.Muted;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            searchInput = inputObject.GetComponent<TMP_InputField>();
            searchInput.textViewport = inputObject.GetComponent<RectTransform>();
            searchInput.textComponent = inputText;
            searchInput.placeholder = placeholder;
            searchInput.lineType = TMP_InputField.LineType.SingleLine;

            var pinObject = new GameObject("Pin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            pinObject.transform.SetParent(toolbarObject.transform, false);
            LayoutElement pinLayout = pinObject.GetComponent<LayoutElement>();
            pinLayout.minWidth = 64f;
            pinLayout.preferredWidth = 64f;
            pinLayout.preferredHeight = ToolbarHeight;
            pinButton = pinObject.GetComponent<Button>();
            pinButton.targetGraphic = pinObject.GetComponent<Image>();
            UIStyleTokens.Button.ApplyPanelSecondaryStyle(pinButton);

            var pinLabelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            pinLabelObject.transform.SetParent(pinObject.transform, false);
            Stretch(pinLabelObject.GetComponent<RectTransform>(), 6f);
            pinButtonLabel = pinLabelObject.GetComponent<TextMeshProUGUI>();
            if (font != null) pinButtonLabel.font = font;
            pinButtonLabel.fontSize = 14f;
            pinButtonLabel.fontStyle = FontStyles.Bold;
            pinButtonLabel.color = UIStyleTokens.Text.Primary;
            pinButtonLabel.alignment = TextAlignmentOptions.Center;
            pinButtonLabel.raycastTarget = false;
            pinButtonLabel.text = "Pin";
        }

        private void ConfigureRequirements(MutationProgressSnapshot snapshot, Action<int> focusMutation)
        {
            bool groupDirectionalTendrils = IsCompleteDirectionalTendrilSet(snapshot);
            var chips = new List<ChipData>(snapshot.Requirements.Count);
            foreach (MutationRequirementProgress requirement in snapshot.Requirements)
            {
                string prefix = groupDirectionalTendrils && IsDirectionalTendril(requirement.MutationId)
                    ? "  "
                    : string.Empty;
                chips.Add(new ChipData(
                    requirement.MutationId,
                    $"{prefix}{requirement.MutationName}  L{requirement.CurrentLevel}/{requirement.RequiredLevel}",
                    requirement.IsMet ? UIStyleTokens.State.Success : UIStyleTokens.State.Warning,
                    requirement.IsMet ? RequirementStatus.Met : RequirementStatus.Unmet));
            }
            foreach (MutationAnyRequirementGroupProgress group in snapshot.AnyRequirementGroups)
            {
                foreach (MutationRequirementProgress requirement in group.Alternatives)
                {
                    chips.Add(new ChipData(
                        requirement.MutationId,
                        $"  {requirement.MutationName}  L{requirement.CurrentLevel}/{requirement.RequiredLevel}",
                        requirement.IsMet ? UIStyleTokens.State.Success : UIStyleTokens.State.Warning,
                        requirement.IsMet ? RequirementStatus.Met : RequirementStatus.Unmet));
                }
            }

            ConfigureButtons(requirementButtons, requirementsRoot, chips, focusMutation);
            var groupedRows = new List<GroupedRequirementData>(snapshot.CategoryInvestmentRequirements.Count + 1);
            if (groupDirectionalTendrils)
            {
                int metCount = snapshot.Requirements.Count(requirement => requirement.IsMet);
                bool isMet = metCount == snapshot.Requirements.Count;
                groupedRows.Add(new GroupedRequirementData(
                    $"<b>All four Directional Tendrils</b>  {metCount}/4\nComplete each compass-direction Tendril below.",
                    isMet ? UIStyleTokens.State.Success : UIStyleTokens.State.Warning,
                    isMet ? RequirementStatus.Met : RequirementStatus.Unmet));
            }

            foreach (MutationCategoryInvestmentRequirementProgress requirement in snapshot.CategoryInvestmentRequirements)
            {
                string categoryLines = string.Join("\n", requirement.Categories.Select(category =>
                    $"  {GetCategoryDisplayName(category.Category)}  L{category.CurrentLevel}/{category.RequiredLevel}"));
                groupedRows.Add(new GroupedRequirementData(
                    $"<b>Tier {(int)requirement.Tier} category foundations</b>  " +
                    $"{requirement.SatisfiedCategoryCount}/{requirement.RequiredCategoryCount} categories\n" +
                    $"Reach {requirement.RequiredLevelsPerCategory} root levels in each qualifying category.\n{categoryLines}",
                    requirement.IsMet ? UIStyleTokens.State.Success : UIStyleTokens.State.Warning,
                    requirement.IsMet ? RequirementStatus.Met : RequirementStatus.Unmet));
            }
            foreach (MutationAnyRequirementGroupProgress group in snapshot.AnyRequirementGroups)
            {
                int metCount = group.Alternatives.Count(requirement => requirement.IsMet);
                groupedRows.Add(new GroupedRequirementData(
                    $"<b>One branch requirement</b>  {metCount}/{group.Alternatives.Count}\nReach the required level in any one option below.",
                    group.IsMet ? UIStyleTokens.State.Success : UIStyleTokens.State.Warning,
                    group.IsMet ? RequirementStatus.Met : RequirementStatus.Unmet));
            }

            ConfigureGroupedRequirementRows(groupedRows);
            requirementsRoot.gameObject.SetActive(chips.Count > 0);
            groupedRequirementsRoot.gameObject.SetActive(groupedRows.Count > 0);
            emptyRequirementsText.gameObject.SetActive(chips.Count == 0 && groupedRows.Count == 0);
        }

        private void ConfigureGroupedRequirementRows(IReadOnlyList<GroupedRequirementData> rows)
        {
            while (groupedRequirementRows.Count < rows.Count)
            {
                groupedRequirementRows.Add(CreateRequirementStatusRow(groupedRequirementsRoot));
            }

            for (int index = 0; index < groupedRequirementRows.Count; index++)
            {
                RequirementStatusRow row = groupedRequirementRows[index];
                bool active = index < rows.Count;
                row.Root.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                GroupedRequirementData data = rows[index];
                row.Label.text = data.Label;
                row.Label.color = Color.Lerp(UIStyleTokens.Text.Primary, data.Accent, 0.3f);
                SetRequirementStatusBadge(row.Badge, data.Status);
                FitTextHeight(row.Label, ChipHeight);
            }
        }

        private static int GetRequirementGroupCount(MutationProgressSnapshot snapshot)
        {
            int namedRequirementGroups = snapshot.Requirements.Count + snapshot.AnyRequirementGroups.Sum(group => group.Alternatives.Count);
            if (IsCompleteDirectionalTendrilSet(snapshot))
            {
                namedRequirementGroups -= 3;
            }

            return namedRequirementGroups + snapshot.CategoryInvestmentRequirements.Count + snapshot.AnyRequirementGroups.Count;
        }

        private static bool IsCompleteDirectionalTendrilSet(MutationProgressSnapshot snapshot)
        {
            return snapshot.Mutation.Id == MutationIds.MycotropicInduction
                && snapshot.Requirements.Count(requirement => IsDirectionalTendril(requirement.MutationId)) == 4;
        }

        private static bool IsDirectionalTendril(int mutationId)
        {
            return mutationId == MutationIds.TendrilNorthwest
                || mutationId == MutationIds.TendrilNortheast
                || mutationId == MutationIds.TendrilSoutheast
                || mutationId == MutationIds.TendrilSouthwest;
        }

        private void ConfigureDependentButtons(IReadOnlyList<Mutation> dependents, Action<int> focusMutation)
        {
            var chips = new List<ChipData>(dependents.Count);
            foreach (Mutation dependent in dependents)
            {
                chips.Add(new ChipData(
                    dependent.Id,
                    $"> {dependent.Name}",
                    MutationTreeColors.GetCategoryAccent(dependent.Category),
                    RequirementStatus.None));
            }

            ConfigureButtons(dependentButtons, dependentsRoot, chips, focusMutation);
            dependentsRoot.gameObject.SetActive(chips.Count > 0);
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
                label.rectTransform.offsetMin = new Vector2(
                    chip.Status == RequirementStatus.None
                        ? 8f
                        : RequirementStatusBadgeSize + RequirementStatusBadgeToLabelGap + RequirementStatusBadgeLeftInset,
                    8f);
                SetRequirementStatusBadge(button.GetComponentInChildren<RequirementStatusBadge>(true), chip.Status);
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
            labelRect.offsetMin = new Vector2(RequirementStatusBadgeSize + RequirementStatusBadgeToLabelGap + RequirementStatusBadgeLeftInset, 8f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 14f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            CreateRequirementStatusBadge(buttonObject.transform);

            return button;
        }

        private RequirementStatusRow CreateRequirementStatusRow(RectTransform parent)
        {
            var rootObject = new GameObject("GroupedRequirement", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            rootObject.transform.SetParent(parent, false);
            Image background = rootObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelSecondary;
            background.raycastTarget = false;
            LayoutElement element = rootObject.GetComponent<LayoutElement>();
            element.preferredHeight = ChipHeight;
            element.minHeight = ChipHeight;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(rootObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect, 0f);
            labelRect.offsetMin = new Vector2(RequirementStatusBadgeSize + RequirementStatusBadgeToLabelGap + RequirementStatusBadgeLeftInset, 8f);
            labelRect.offsetMax = new Vector2(-10f, -8f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 14f;
            label.color = UIStyleTokens.Text.Primary;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;

            return new RequirementStatusRow(rootObject.GetComponent<RectTransform>(), label, CreateRequirementStatusBadge(rootObject.transform));
        }

        private static RequirementStatusBadge CreateRequirementStatusBadge(Transform parent)
        {
            var badgeObject = new GameObject("RequirementStatusBadge", typeof(RectTransform), typeof(RequirementStatusBadge));
            badgeObject.transform.SetParent(parent, false);
            RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(RequirementStatusBadgeLeftInset, 0f);
            badgeRect.sizeDelta = new Vector2(RequirementStatusBadgeSize, RequirementStatusBadgeSize);
            return badgeObject.GetComponent<RequirementStatusBadge>();
        }

        private static void SetRequirementStatusBadge(RequirementStatusBadge? badge, RequirementStatus status)
        {
            if (badge != null)
            {
                badge.SetStatus(status);
            }
        }

        private RectTransform CreateSectionRoot(string name)
        {
            var rootObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            rootObject.transform.SetParent(contentRect, false);
            VerticalLayoutGroup layout = rootObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = RelatedItemSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = rootObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rootObject.GetComponent<RectTransform>();
        }

        private RectTransform CreateChipRoot(string name, RectTransform parent)
        {
            var rootObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            rootObject.transform.SetParent(parent, false);
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
            string text = "",
            RectTransform? parent = null)
        {
            var rootObject = background.HasValue
                ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement))
                : new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            rootObject.transform.SetParent(parent != null ? parent : contentRect, false);

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
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.margin = background.HasValue ? new Vector4(10f, 8f, 10f, 8f) : Vector4.zero;
            label.raycastTarget = false;

            LayoutElement element = rootObject.GetComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.minHeight = preferredHeight;
            return label;
        }

        private void RefreshTextHeights()
        {
            FitTextHeight(titleText, 32f);
            FitTextHeight(metadataText, 18f);
            FitTextHeight(summaryText, 22f);
            FitTextHeight(technicalDetailsText, 32f);
            FitTextHeight(stateText, 22f);
            FitTextHeight(costText, 22f);
            FitTextHeight(currentLevelText, 32f);
            FitTextHeight(nextLevelText, 32f);
            FitTextHeight(maxLevelBonusText, 32f);
            FitTextHeight(synergyText, 32f);
        }

        private void FitTextHeight(TextMeshProUGUI label, float minimumHeight)
        {
            if (label == null)
            {
                return;
            }

            LayoutElement? element = label.GetComponent<LayoutElement>()
                ?? label.transform.parent?.GetComponent<LayoutElement>();
            if (element == null)
            {
                return;
            }

            float contentWidth = contentRect != null && contentRect.rect.width > 0f
                ? contentRect.rect.width
                : Mathf.Max(120f, rootRect.rect.width - (PanelPadding * 2f));
            float horizontalMargins = label.margin.x + label.margin.z;
            float verticalMargins = label.margin.y + label.margin.w;
            float textWidth = Mathf.Max(80f, contentWidth - horizontalMargins);
            float requiredHeight = label.GetPreferredValues(label.text, textWidth, 0f).y + verticalMargins;
            element.minHeight = minimumHeight;
            element.preferredHeight = Mathf.Max(minimumHeight, requiredHeight);
        }

        private static void SetTextBlockActive(TextMeshProUGUI label, bool active)
        {
            LayoutElement? layoutElement = label.GetComponent<LayoutElement>()
                ?? label.transform.parent?.GetComponent<LayoutElement>();
            GameObject block = layoutElement != null ? layoutElement.gameObject : label.gameObject;
            block.SetActive(active);
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
            if (snapshot.IsActiveSurge) return MutationTreeColors.GetReadableCategoryAccent(snapshot.Mutation.Category);
            if (IsPendingUntilNextRound(snapshot, player) || snapshot.HasUnmetPrerequisites || manager.IsMutationDisabledBecauseNoEffect(snapshot.Mutation, player))
                return UIStyleTokens.State.Warning;
            if (snapshot.IsAffordable) return UIStyleTokens.State.Success;
            return UIStyleTokens.Text.Muted;
        }

        private static bool IsPendingUntilNextRound(MutationProgressSnapshot snapshot, Player player)
        {
            var board = GameManager.Instance?.Board;
            return board != null
                && MutationPrerequisiteEvaluator.HasRequirements(snapshot.Mutation)
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
            return MutationCategoryPresentationCatalog.Get(category).DisplayName;
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
            public ChipData(int mutationId, string label, Color accent, RequirementStatus status)
            {
                MutationId = mutationId;
                Label = label;
                Accent = accent;
                Status = status;
            }

            public int MutationId { get; }
            public string Label { get; }
            public Color Accent { get; }
            public RequirementStatus Status { get; }
        }

        private readonly struct GroupedRequirementData
        {
            public GroupedRequirementData(string label, Color accent, RequirementStatus status)
            {
                Label = label;
                Accent = accent;
                Status = status;
            }

            public string Label { get; }
            public Color Accent { get; }
            public RequirementStatus Status { get; }
        }

        private readonly struct RequirementStatusRow
        {
            public RequirementStatusRow(RectTransform root, TextMeshProUGUI label, RequirementStatusBadge badge)
            {
                Root = root;
                Label = label;
                Badge = badge;
            }

            public RectTransform Root { get; }
            public TextMeshProUGUI Label { get; }
            public RequirementStatusBadge Badge { get; }
        }

    }

    internal enum RequirementStatus
    {
        None,
        Met,
        Unmet
    }
}
