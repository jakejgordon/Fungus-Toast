using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FungusToast.Core.Campaign;

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
            bool isUniversal = false)
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
        private static readonly ReadOnlyCollection<MoldinessUnlockDefinition> all =
            new ReadOnlyCollection<MoldinessUnlockDefinition>(
                new List<MoldinessUnlockDefinition>
                {
                    new MoldinessUnlockDefinition(
                        id: "moldiness_reward_failed_run_adaptation_carryover",
                        displayName: "Spores in Reserve",
                        description: "On failed campaigns, permanently carry over 1 additional adaptation into your next run.",
                        type: MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover,
                        requiredUnlockLevel: 1,
                        stackAmount: 1,
                        isRepeatable: true,
                        isUniversal: true),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_spore_salvo",
                        displayName: "Unlock Spore Salvo",
                        description: "Spore Salvo can now appear in future normal adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.SporeSalvo),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_hyphal_bridge",
                        displayName: "Unlock Hyphal Bridge",
                        description: "Hyphal Bridge can now appear in future normal adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.HyphalBridge),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_vesicle_burst",
                        displayName: "Unlock Vesicle Burst",
                        description: "Vesicle Burst can now appear in future normal adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.VesicleBurst),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_hyphal_priming",
                        displayName: "Unlock Hyphal Priming",
                        description: "Hyphal Priming can now appear in future normal adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptation,
                        requiredUnlockLevel: 1,
                        adaptationId: AdaptationIds.HyphalPriming),
                });

        private static readonly Dictionary<string, MoldinessUnlockDefinition> byId =
            all.ToDictionary(definition => definition.Id, StringComparer.Ordinal);

        public static IReadOnlyList<MoldinessUnlockDefinition> All => all;

        public static bool TryGetById(string id, out MoldinessUnlockDefinition definition)
        {
            return byId.TryGetValue(id, out definition);
        }
    }

    public static class MoldinessUnlockService
    {
        public static List<MoldinessUnlockDefinition> GenerateOffers(MoldinessProgressionState progressionState, System.Random random, int count)
        {
            progressionState ??= MoldinessProgression.CreateDefaultState();
            progressionState.unlockedRewardIds ??= new List<string>();
            progressionState.unlockedAdaptationIds ??= new List<string>();
            progressionState.pendingUnlockChoice ??= null;

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
            progressionState.unlockedRewardIds ??= new List<string>();
            progressionState.unlockedAdaptationIds ??= new List<string>();

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
    }
}
