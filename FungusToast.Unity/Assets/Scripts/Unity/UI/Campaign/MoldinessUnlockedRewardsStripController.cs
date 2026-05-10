using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Campaign;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityMoldinessProgressionState = FungusToast.Unity.Campaign.MoldinessProgressionState;

namespace FungusToast.Unity.UI.Campaign
{
    public sealed class MoldinessUnlockedRewardsStripController
    {
        private readonly Transform parent;
        private readonly string namePrefix;
        private readonly float gridWidth;
        private readonly Func<MoldinessUnlockDefinition, Sprite> iconFactory;
        private readonly List<GameObject> iconRoots = new();
        private readonly List<TextMeshProUGUI> countBadges = new();

        private RectTransform rootTransform;
        private TextMeshProUGUI titleLabel;
        private RectTransform gridRoot;
        private GridLayoutGroup gridLayout;

        public MoldinessUnlockedRewardsStripController(
            Transform parent,
            string namePrefix,
            float gridWidth,
            Func<MoldinessUnlockDefinition, Sprite> iconFactory)
        {
            this.parent = parent;
            this.namePrefix = string.IsNullOrWhiteSpace(namePrefix) ? "UI_Moldiness" : namePrefix;
            this.gridWidth = Mathf.Max(120f, gridWidth);
            this.iconFactory = iconFactory;
        }

        public RectTransform RootTransform => rootTransform;

        public void EnsureBuilt()
        {
            if (parent == null)
            {
                return;
            }

            if (rootTransform == null)
            {
                var existingRoot = parent.Find($"{namePrefix}UnlockedRewardsSection") as RectTransform;
                if (existingRoot != null)
                {
                    rootTransform = existingRoot;
                }
                else
                {
                    var rootObject = new GameObject(
                        $"{namePrefix}UnlockedRewardsSection",
                        typeof(RectTransform),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    rootObject.transform.SetParent(parent, false);
                    rootTransform = rootObject.GetComponent<RectTransform>();
                }
            }

            var rootLayout = rootTransform.GetComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 8f;
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;

            var rootFitter = rootTransform.GetComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rootElement = rootTransform.GetComponent<LayoutElement>();
            rootElement.minWidth = gridWidth;
            rootElement.preferredWidth = gridWidth;
            rootElement.minHeight = 0f;
            rootElement.preferredHeight = -1f;

            if (titleLabel == null)
            {
                var existingLabel = rootTransform.Find($"{namePrefix}UnlockedRewardsLabel") as RectTransform;
                if (existingLabel != null)
                {
                    titleLabel = existingLabel.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var labelObject = new GameObject(
                        $"{namePrefix}UnlockedRewardsLabel",
                        typeof(RectTransform),
                        typeof(LayoutElement),
                        typeof(TextMeshProUGUI));
                    labelObject.transform.SetParent(rootTransform, false);
                    titleLabel = labelObject.GetComponent<TextMeshProUGUI>();
                }
            }

            titleLabel.fontSize = 18f;
            titleLabel.fontStyle = FontStyles.Normal;
            titleLabel.color = UIStyleTokens.Text.Secondary;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 16f;
            titleLabel.fontSizeMax = 18f;
            titleLabel.textWrappingMode = TextWrappingModes.Normal;
            titleLabel.overflowMode = TextOverflowModes.Overflow;

            var titleElement = titleLabel.GetComponent<LayoutElement>();
            titleElement.minWidth = gridWidth;
            titleElement.preferredWidth = gridWidth;
            titleElement.minHeight = 24f;
            titleElement.preferredHeight = -1f;

            if (gridRoot == null)
            {
                var existingGrid = rootTransform.Find($"{namePrefix}UnlockedRewardsGrid") as RectTransform;
                if (existingGrid != null)
                {
                    gridRoot = existingGrid;
                }
                else
                {
                    var gridObject = new GameObject(
                        $"{namePrefix}UnlockedRewardsGrid",
                        typeof(RectTransform),
                        typeof(GridLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    gridObject.transform.SetParent(rootTransform, false);
                    gridRoot = gridObject.GetComponent<RectTransform>();
                }
            }

            gridLayout = gridRoot.GetComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 7;
            gridLayout.cellSize = new Vector2(48f, 48f);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            var gridFitter = gridRoot.GetComponent<ContentSizeFitter>();
            gridFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var gridElement = gridRoot.GetComponent<LayoutElement>();
            gridElement.minWidth = gridWidth;
            gridElement.preferredWidth = gridWidth;
            gridElement.minHeight = 0f;
            gridElement.preferredHeight = -1f;
        }

        public void Refresh(UnityMoldinessProgressionState progressionState)
        {
            EnsureBuilt();
            if (rootTransform == null || titleLabel == null || gridRoot == null || gridLayout == null)
            {
                return;
            }

            var unlockedRewards = GetOrderedUnlockedRewards(progressionState);
            rootTransform.gameObject.SetActive(unlockedRewards.Count > 0);
            titleLabel.text = unlockedRewards.Count > 0 ? "Unlocked Rewards" : string.Empty;

            if (unlockedRewards.Count == 0)
            {
                return;
            }

            int carryoverCapacity = Mathf.Max(0, progressionState?.failedRunAdaptationCarryoverCount ?? 0);
            var rewardCounts = unlockedRewards
                .GroupBy(reward => reward.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var uniqueRewards = unlockedRewards
                .GroupBy(reward => reward.Id, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            while (iconRoots.Count < uniqueRewards.Count)
            {
                CreateIconSlot(iconRoots.Count + 1);
            }

            for (int index = 0; index < iconRoots.Count; index++)
            {
                var iconRoot = iconRoots[index];
                bool shouldShow = index < uniqueRewards.Count;
                iconRoot.SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                var reward = uniqueRewards[index];
                int ownedCount = rewardCounts.TryGetValue(reward.Id, out int count) ? count : 1;
                var background = iconRoot.GetComponent<Image>();
                background.color = new Color(reward.AccentColor.r, reward.AccentColor.g, reward.AccentColor.b, 0.16f);

                var outline = iconRoot.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = new Color(reward.AccentColor.r, reward.AccentColor.g, reward.AccentColor.b, 0.45f);
                }

                var iconImage = iconRoot.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = iconFactory != null ? iconFactory(reward) : AdaptationArtRepository.GetIcon(null);
                    iconImage.preserveAspect = true;
                    iconImage.color = Color.white;
                    iconImage.raycastTarget = false;
                }

                if (index < countBadges.Count)
                {
                    var badgeLabel = countBadges[index];
                    var badgeRoot = badgeLabel != null ? badgeLabel.transform.parent?.gameObject : null;
                    bool showBadge = reward.IsRepeatable && ownedCount > 1;
                    if (badgeRoot != null)
                    {
                        badgeRoot.SetActive(showBadge);
                        var badgeImage = badgeRoot.GetComponent<Image>();
                        if (badgeImage != null)
                        {
                            badgeImage.color = new Color(reward.AccentColor.r, reward.AccentColor.g, reward.AccentColor.b, 0.92f);
                        }
                    }

                    if (badgeLabel != null)
                    {
                        badgeLabel.text = CompactCountLabel(ownedCount);
                        badgeLabel.color = UIStyleTokens.Text.Primary;
                    }
                }

                var tooltipTrigger = iconRoot.GetComponent<TooltipTrigger>();
                if (reward.Type == MoldinessUnlockType.UnlockAdaptation && AdaptationRepository.TryGetById(reward.AdaptationId, out var adaptation))
                {
                    var provider = iconRoot.GetComponent<AdaptationTooltipProvider>() ?? iconRoot.AddComponent<AdaptationTooltipProvider>();
                    provider.Initialize(adaptation);
                    tooltipTrigger.SetDynamicProvider(provider);
                }
                else
                {
                    var provider = iconRoot.GetComponent<MoldinessRewardTooltipProvider>() ?? iconRoot.AddComponent<MoldinessRewardTooltipProvider>();
                    provider.Initialize(reward, ownedCount, carryoverCapacity);
                    tooltipTrigger.SetDynamicProvider(provider);
                }

                tooltipTrigger.SetAutoPlacementOffsetX(18f);
                tooltipTrigger.SetPinOnClick(false);
            }
        }

        public static List<MoldinessUnlockDefinition> GetOrderedUnlockedRewards(UnityMoldinessProgressionState progressionState)
        {
            if (progressionState?.unlockedRewardIds == null)
            {
                return new List<MoldinessUnlockDefinition>();
            }

            return progressionState.unlockedRewardIds
                .Select(id => MoldinessUnlockCatalog.TryGetById(id, out var definition) ? definition : null)
                .Where(definition => definition != null)
                .OrderBy(GetMoldinessRewardCategorySortOrder)
                .ThenBy(definition => MoldinessUnlockCatalog.GetSortIndex(definition.Id))
                .ToList();
        }

        private static int GetMoldinessRewardCategorySortOrder(MoldinessUnlockDefinition definition)
        {
            if (definition == null)
            {
                return int.MaxValue;
            }

            return definition.Type switch
            {
                MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover => 0,
                MoldinessUnlockType.UnlockCampaignIntel => 0,
                MoldinessUnlockType.UnlockCampaignDraftRedraw => 0,
                MoldinessUnlockType.UnlockAdaptation => 1,
                MoldinessUnlockType.UnlockMycovariant => 2,
                _ => 3
            };
        }

        private void CreateIconSlot(int slotIndex)
        {
            var iconRoot = new GameObject(
                $"{namePrefix}UnlockedReward_{slotIndex}",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(TooltipTrigger));
            iconRoot.transform.SetParent(gridLayout.transform, false);

            var background = iconRoot.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelSecondary;
            background.raycastTarget = true;

            var layout = iconRoot.GetComponent<LayoutElement>();
            layout.minWidth = 48f;
            layout.preferredWidth = 48f;
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;

            var outline = iconRoot.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            outline.effectDistance = new Vector2(1f, -1f);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(iconRoot.transform, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(34f, 34f);
            iconRect.anchoredPosition = Vector2.zero;

            var badgeObject = new GameObject("CountBadge", typeof(RectTransform), typeof(Image));
            badgeObject.transform.SetParent(iconRoot.transform, false);
            var badgeRect = badgeObject.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(1f, 0f);
            badgeRect.anchoredPosition = new Vector2(-2f, 2f);
            badgeRect.sizeDelta = new Vector2(20f, 20f);
            var badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.color = UIStyleTokens.Surface.PanelPrimary;
            badgeImage.raycastTarget = false;

            var badgeOutline = badgeObject.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);

            var badgeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeLabelObject.transform.SetParent(badgeObject.transform, false);
            var badgeLabelRect = badgeLabelObject.GetComponent<RectTransform>();
            badgeLabelRect.anchorMin = Vector2.zero;
            badgeLabelRect.anchorMax = Vector2.one;
            badgeLabelRect.offsetMin = Vector2.zero;
            badgeLabelRect.offsetMax = Vector2.zero;
            var badgeLabel = badgeLabelObject.GetComponent<TextMeshProUGUI>();
            badgeLabel.alignment = TextAlignmentOptions.Center;
            badgeLabel.fontSize = 13f;
            badgeLabel.fontStyle = FontStyles.Bold;
            badgeLabel.color = UIStyleTokens.Text.Primary;
            badgeLabel.enableAutoSizing = true;
            badgeLabel.fontSizeMin = 10f;
            badgeLabel.fontSizeMax = 13f;
            badgeLabel.raycastTarget = false;

            iconRoots.Add(iconRoot);
            countBadges.Add(badgeLabel);
        }

        private static string CompactCountLabel(int count)
        {
            if (count < 100)
            {
                return count.ToString();
            }

            if (count < 1000)
            {
                return $"{count / 100f:0.#}h";
            }

            return $"{count / 1000f:0.#}k";
        }
    }
}