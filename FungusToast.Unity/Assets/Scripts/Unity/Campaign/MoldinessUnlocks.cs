using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FungusToast.Core.Campaign;
using FungusToast.Unity.UI;
using UnityEngine;

namespace FungusToast.Unity.Campaign
{
    public enum MoldinessUnlockType
    {
        UnlockAdaptation = 0,
        IncreaseFailedRunAdaptationCarryover = 1,
    }

    [Serializable]
    public class MoldinessUnlockChoiceState
    {
        public int triggerTierIndex;
        public List<string> offeredUnlockIds = new();
    }

    public sealed class MoldinessUnlockDefinition
    {
        public MoldinessUnlockDefinition(
            string id,
            string displayName,
            string description,
            MoldinessUnlockType type,
            int requiredUnlockLevel,
            string adaptationId = null,
            int stackAmount = 0,
            bool isRepeatable = false,
            bool isUniversal = false,
            string categoryLabel = "",
            Color? accentColor = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Unlock id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Unlock display name is required.", nameof(displayName));
            if (type == MoldinessUnlockType.UnlockAdaptation && string.IsNullOrWhiteSpace(adaptationId))
            {
                throw new ArgumentException("UnlockAdaptation rewards require an adaptation id.", nameof(adaptationId));
            }
            if (type == MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover && stackAmount <= 0)
            {
                throw new ArgumentException("Carryover rewards require a positive stack amount.", nameof(stackAmount));
            }

            Id = id;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            Type = type;
            RequiredUnlockLevel = Math.Max(0, requiredUnlockLevel);
            AdaptationId = adaptationId ?? string.Empty;
            StackAmount = Math.Max(0, stackAmount);
            IsRepeatable = isRepeatable;
            IsUniversal = isUniversal;
            CategoryLabel = categoryLabel ?? string.Empty;
            AccentColor = accentColor ?? UIStyleTokens.State.Info;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public MoldinessUnlockType Type { get; }
        public int RequiredUnlockLevel { get; }
        public string AdaptationId { get; }
        public int StackAmount { get; }
        public bool IsRepeatable { get; }
        public bool IsUniversal { get; }
        public string CategoryLabel { get; }
        public Color AccentColor { get; }
    }

    public readonly struct MoldinessUnlockApplicationResult
    {
        public MoldinessUnlockApplicationResult(bool applied, MoldinessUnlockDefinition definition)
        {
            Applied = applied;
            Definition = definition;
        }

        public bool Applied { get; }
        public MoldinessUnlockDefinition Definition { get; }
    }

    public static class MoldinessUnlockCatalog
    {
        public const string LegacySporesInReserveRewardId = "moldiness_reward_failed_run_adaptation_carryover";

        private static readonly ReadOnlyCollection<MoldinessUnlockDefinition> all =
            new ReadOnlyCollection<MoldinessUnlockDefinition>(
                new List<MoldinessUnlockDefinition>
                {
                    new MoldinessUnlockDefinition(
                        id: "moldiness_reward_failed_run_adaptation_carryover_i",
                        displayName: "Spores in Reserve I",
                        description: "Permanently increase failed-run Adaptation carryover capacity by 1.",
                        type: MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover,
                        requiredUnlockLevel: 1,
                        stackAmount: 1,
                        isRepeatable: false,
                        isUniversal: false,
                        categoryLabel: "Permanent Campaign Upgrade",
                        accentColor: UIStyleTokens.State.Warning),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_reward_failed_run_adaptation_carryover_ii",
                        displayName: "Spores in Reserve II",
                        description: "Permanently increase failed-run Adaptation carryover capacity by 1.",
                        type: MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover,
                        requiredUnlockLevel: 5,
                        stackAmount: 1,
                        isRepeatable: false,
                        isUniversal: false,
                        categoryLabel: "Permanent Campaign Upgrade",
                        accentColor: UIStyleTokens.State.Warning),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_reward_failed_run_adaptation_carryover_iii",
                        displayName: "Spores in Reserve III",
                        description: "Permanently increase failed-run Adaptation carryover capacity by 1.",
                        type: MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover,
                        requiredUnlockLevel: 8,
                        stackAmount: 1,
                        isRepeatable: false,
                        isUniversal: false,
                        categoryLabel: "Permanent Campaign Upgrade",
                        accentColor: UIStyleTokens.State.Warning),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_reward_failed_run_adaptation_carryover_iv",
                        displayName: "Spores in Reserve IV",
                        description: "Permanently increase failed-run Adaptation carryover capacity by 1.",
                        type: MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover,
                        requiredUnlockLevel: 10,
                        stackAmount: 1,
                        isRepeatable: false,
                        isUniversal: false,
                        categoryLabel: "Permanent Campaign Upgrade",
                        accentColor: UIStyleTokens.State.Warning),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_reward_failed_run_adaptation_carryover_v",
                        displayName: "Spores in Reserve V",
                        description: "Permanently increase failed-run Adaptation carryover capacity by 1.",
                        type: MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover,
                        requiredUnlockLevel: 12,
                        stackAmount: 1,
                        isRepeatable: false,
                        isUniversal: false,
                        categoryLabel: "Permanent Campaign Upgrade",
                        accentColor: UIStyleTokens.State.Warning),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_spore_salvo",
                        displayName: "Unlock Spore Salvo",
                        description: "Permanently unlock Spore Salvo so it can appear in future campaign drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.SporeSalvo,
                        categoryLabel: "Adaptation Unlock",
                        accentColor: UIStyleTokens.State.Success),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_hyphal_bridge",
                        displayName: "Unlock Hyphal Bridge",
                        description: "Permanently unlock Hyphal Bridge so it can appear in future campaign drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.HyphalBridge,
                        categoryLabel: "Adaptation Unlock",
                        accentColor: UIStyleTokens.State.Success),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_vesicle_burst",
                        displayName: "Unlock Vesicle Burst",
                        description: "Permanently unlock Vesicle Burst so it can appear in future campaign drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.VesicleBurst,
                        categoryLabel: "Adaptation Unlock",
                        accentColor: UIStyleTokens.State.Success),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_hyphal_priming",
                        displayName: "Unlock Hyphal Priming",
                        description: "Permanently unlock Hyphal Priming so it can appear in future campaign drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.HyphalPriming,
                        categoryLabel: "Adaptation Unlock",
                        accentColor: UIStyleTokens.State.Success),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_tropic_lysis",
                        displayName: "Unlock Tropic Lysis",
                        description: "Permanently unlock Tropic Lysis so it can appear in future campaign drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.TropicLysis,
                        categoryLabel: "Adaptation Unlock",
                        accentColor: UIStyleTokens.State.Success),
                });

        private static readonly Dictionary<string, MoldinessUnlockDefinition> byId =
            all.ToDictionary(definition => definition.Id, StringComparer.Ordinal);

        private static readonly Dictionary<string, int> sortIndexById =
            all.Select((definition, index) => new { definition.Id, Index = index })
                .ToDictionary(entry => entry.Id, entry => entry.Index, StringComparer.Ordinal);

        private static readonly ReadOnlyCollection<MoldinessUnlockDefinition> sporesInReserveRewards =
            new ReadOnlyCollection<MoldinessUnlockDefinition>(
                all.Where(definition => definition.Type == MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover).ToList());

        private static readonly HashSet<string> sporesInReserveRewardIds =
            new HashSet<string>(sporesInReserveRewards.Select(definition => definition.Id), StringComparer.Ordinal);

        public static IReadOnlyList<MoldinessUnlockDefinition> All => all;

        public static IReadOnlyList<MoldinessUnlockDefinition> SporesInReserveRewards => sporesInReserveRewards;

        public static bool TryGetById(string id, out MoldinessUnlockDefinition definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        public static bool IsSporesInReserveRewardId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            return string.Equals(id, LegacySporesInReserveRewardId, StringComparison.Ordinal)
                || sporesInReserveRewardIds.Contains(id);
        }

        public static int GetSortIndex(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && sortIndexById.TryGetValue(id, out int sortIndex))
            {
                return sortIndex;
            }

            return int.MaxValue;
        }
    }

    public static class MoldinessUnlockService
    {
        public static bool NormalizeProgressionState(MoldinessProgressionState progressionState)
        {
            if (progressionState == null)
            {
                return false;
            }

            bool changed = false;

            if (progressionState.pendingUnlockTriggers == null)
            {
                progressionState.pendingUnlockTriggers = new List<MoldinessUnlockTrigger>();
                changed = true;
            }

            if (progressionState.unlockedRewardIds == null)
            {
                progressionState.unlockedRewardIds = new List<string>();
                changed = true;
            }

            if (progressionState.unlockedAdaptationIds == null)
            {
                progressionState.unlockedAdaptationIds = new List<string>();
                changed = true;
            }

            if (progressionState.failedRunAdaptationCarryoverCount < 0)
            {
                progressionState.failedRunAdaptationCarryoverCount = 0;
                changed = true;
            }

            bool removedLegacyRewardId = progressionState.unlockedRewardIds.RemoveAll(id =>
                string.Equals(id, MoldinessUnlockCatalog.LegacySporesInReserveRewardId, StringComparison.Ordinal)) > 0;
            if (removedLegacyRewardId)
            {
                changed = true;
            }

            var ownedSporesRewardIds = new HashSet<string>(
                progressionState.unlockedRewardIds.Where(MoldinessUnlockCatalog.IsSporesInReserveRewardId),
                StringComparer.Ordinal);

            int targetSporesRewardCount = Math.Min(
                MoldinessUnlockCatalog.SporesInReserveRewards.Count,
                Math.Max(ownedSporesRewardIds.Count, progressionState.failedRunAdaptationCarryoverCount));

            foreach (var reward in MoldinessUnlockCatalog.SporesInReserveRewards)
            {
                if (ownedSporesRewardIds.Count >= targetSporesRewardCount)
                {
                    break;
                }

                if (ownedSporesRewardIds.Add(reward.Id))
                {
                    progressionState.unlockedRewardIds.Add(reward.Id);
                    changed = true;
                }
            }

            int minimumCarryoverCapacity = Math.Max(ownedSporesRewardIds.Count, removedLegacyRewardId ? 1 : 0);
            if (progressionState.failedRunAdaptationCarryoverCount < minimumCarryoverCapacity)
            {
                progressionState.failedRunAdaptationCarryoverCount = minimumCarryoverCapacity;
                changed = true;
            }

            if (ShouldResetPendingUnlockChoice(progressionState, ownedSporesRewardIds))
            {
                progressionState.pendingUnlockChoice = null;
                changed = true;
            }

            return changed;
        }

        public static List<MoldinessUnlockDefinition> GenerateOffers(MoldinessProgressionState progressionState, System.Random random, int count)
        {
            progressionState ??= MoldinessProgression.CreateDefaultState();
            NormalizeProgressionState(progressionState);

            if (progressionState.pendingUnlockChoice != null && progressionState.pendingUnlockChoice.offeredUnlockIds?.Count > 0)
            {
                return progressionState.pendingUnlockChoice.offeredUnlockIds
                    .Select(id => MoldinessUnlockCatalog.TryGetById(id, out var definition) ? definition : null)
                    .Where(definition => definition != null)
                    .Take(Math.Max(1, count))
                    .ToList();
            }

            var ownedRewardIds = new HashSet<string>(progressionState.unlockedRewardIds, StringComparer.Ordinal);
            var ownedAdaptationIds = new HashSet<string>(progressionState.unlockedAdaptationIds, StringComparer.Ordinal);
            int currentUnlockLevel = Math.Max(0, progressionState.unlockLevel);
            int highestTriggeredUnlockLevel = progressionState.pendingUnlockTriggers != null && progressionState.pendingUnlockTriggers.Count > 0
                ? progressionState.pendingUnlockTriggers.Max(trigger => trigger.tierIndex + 1)
                : 0;
            int availableUnlockLevel = Math.Max(currentUnlockLevel, highestTriggeredUnlockLevel);
            var eligible = MoldinessUnlockCatalog.All
                .Where(definition => definition.RequiredUnlockLevel <= availableUnlockLevel)
                .Where(definition => definition.IsRepeatable || !ownedRewardIds.Contains(definition.Id))
                .Where(definition => definition.Type != MoldinessUnlockType.UnlockAdaptation || !ownedAdaptationIds.Contains(definition.AdaptationId))
                .ToList();

            if (eligible.Count == 0)
            {
                return new List<MoldinessUnlockDefinition>();
            }

            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (eligible[i], eligible[swapIndex]) = (eligible[swapIndex], eligible[i]);
            }

            int takeCount = Math.Min(Math.Max(1, count), eligible.Count);
            var offers = eligible.Take(takeCount).ToList();
            progressionState.pendingUnlockChoice = new MoldinessUnlockChoiceState
            {
                triggerTierIndex = progressionState.currentTierIndex,
                offeredUnlockIds = offers.Select(definition => definition.Id).ToList()
            };

            return offers;
        }

        public static MoldinessUnlockApplicationResult ApplyUnlockChoice(MoldinessProgressionState progressionState, string unlockId)
        {
            progressionState ??= MoldinessProgression.CreateDefaultState();
            NormalizeProgressionState(progressionState);

            if (!MoldinessUnlockCatalog.TryGetById(unlockId, out var definition))
            {
                return new MoldinessUnlockApplicationResult(false, null);
            }

            if (!definition.IsRepeatable && progressionState.unlockedRewardIds.Contains(definition.Id))
            {
                return new MoldinessUnlockApplicationResult(false, definition);
            }

            switch (definition.Type)
            {
                case MoldinessUnlockType.UnlockAdaptation:
                    if (progressionState.unlockedAdaptationIds.Contains(definition.AdaptationId))
                    {
                        return new MoldinessUnlockApplicationResult(false, definition);
                    }

                    progressionState.unlockedAdaptationIds.Add(definition.AdaptationId);
                    break;

                case MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover:
                    progressionState.failedRunAdaptationCarryoverCount += definition.StackAmount;
                    break;
            }

            if (!definition.IsRepeatable)
            {
                progressionState.unlockedRewardIds.Add(definition.Id);
            }

            progressionState.unlockLevel = Math.Max(progressionState.unlockLevel, definition.RequiredUnlockLevel);

            if (progressionState.pendingUnlockChoice != null)
            {
                progressionState.pendingUnlockChoice = null;
            }

            if (progressionState.pendingUnlockTriggers != null && progressionState.pendingUnlockTriggers.Count > 0)
            {
                progressionState.pendingUnlockTriggers.RemoveAt(0);
            }

            return new MoldinessUnlockApplicationResult(true, definition);
        }

        private static bool ShouldResetPendingUnlockChoice(MoldinessProgressionState progressionState, IReadOnlyCollection<string> ownedSporesRewardIds)
        {
            if (progressionState?.pendingUnlockChoice?.offeredUnlockIds == null || progressionState.pendingUnlockChoice.offeredUnlockIds.Count == 0)
            {
                return false;
            }

            var ownedRewardIds = new HashSet<string>(progressionState.unlockedRewardIds, StringComparer.Ordinal);
            var ownedAdaptationIds = new HashSet<string>(progressionState.unlockedAdaptationIds, StringComparer.Ordinal);
            int availableUnlockLevel = GetAvailableUnlockLevel(progressionState);

            foreach (var rewardId in progressionState.pendingUnlockChoice.offeredUnlockIds)
            {
                if (string.IsNullOrWhiteSpace(rewardId)
                    || string.Equals(rewardId, MoldinessUnlockCatalog.LegacySporesInReserveRewardId, StringComparison.Ordinal)
                    || !MoldinessUnlockCatalog.TryGetById(rewardId, out var definition))
                {
                    return true;
                }

                if (definition.RequiredUnlockLevel > availableUnlockLevel)
                {
                    return true;
                }

                if (!definition.IsRepeatable && ownedRewardIds.Contains(definition.Id))
                {
                    return true;
                }

                if (definition.Type == MoldinessUnlockType.UnlockAdaptation && ownedAdaptationIds.Contains(definition.AdaptationId))
                {
                    return true;
                }

                if (definition.Type == MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover
                    && ownedSporesRewardIds.Contains(definition.Id)
                    && !definition.IsRepeatable)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetAvailableUnlockLevel(MoldinessProgressionState progressionState)
        {
            int currentUnlockLevel = Math.Max(0, progressionState?.unlockLevel ?? 0);
            int highestTriggeredUnlockLevel = progressionState?.pendingUnlockTriggers != null && progressionState.pendingUnlockTriggers.Count > 0
                ? progressionState.pendingUnlockTriggers.Max(trigger => trigger.tierIndex + 1)
                : 0;

            return Math.Max(currentUnlockLevel, highestTriggeredUnlockLevel);
        }
    }
}
