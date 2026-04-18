using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FungusToast.Core.Campaign;

namespace FungusToast.Unity.Campaign
{
    public enum MoldinessUnlockType
    {
        UnlockAdaptationForDrafting = 0,
    }

    [Serializable]
    public class MoldinessUnlockChoiceState
    {
        public int triggerTierIndex;
        public List<string> offeredUnlockIds = new();
    }

    public sealed class MoldinessUnlockDefinition
    {
        public MoldinessUnlockDefinition(string id, string displayName, string description, MoldinessUnlockType type, string payloadId, bool isRepeatable = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Unlock id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Unlock display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(payloadId)) throw new ArgumentException("Unlock payload id is required.", nameof(payloadId));

            Id = id;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            Type = type;
            PayloadId = payloadId;
            IsRepeatable = isRepeatable;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public MoldinessUnlockType Type { get; }
        public string PayloadId { get; }
        public bool IsRepeatable { get; }
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
                        id: "moldiness_unlock_adaptation_spore_salvo",
                        displayName: "Unlock Spore Salvo",
                        description: "Add Spore Salvo to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.SporeSalvo),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_hyphal_bridge",
                        displayName: "Unlock Hyphal Bridge",
                        description: "Add Hyphal Bridge to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.HyphalBridge),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_vesicle_burst",
                        displayName: "Unlock Vesicle Burst",
                        description: "Add Vesicle Burst to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.VesicleBurst),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_rhizomorphic_hunger",
                        displayName: "Unlock Rhizomorphic Hunger",
                        description: "Add Rhizomorphic Hunger to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.RhizomorphicHunger),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_mycelial_crescendo",
                        displayName: "Unlock Mycelial Crescendo",
                        description: "Add Mycelial Crescendo to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.MycelialCrescendo),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_ossified_advance",
                        displayName: "Unlock Ossified Advance",
                        description: "Add Ossified Advance to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.OssifiedAdvance),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_distal_spore",
                        displayName: "Unlock Distal Spore",
                        description: "Add Distal Spore to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.DistalSpore),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_adaptation_conidia_ascent",
                        displayName: "Unlock Conidia Ascent",
                        description: "Add Conidia Ascent to your future campaign adaptation drafts.",
                        type: MoldinessUnlockType.UnlockAdaptationForDrafting,
                        payloadId: AdaptationIds.ConidiaAscent),
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
            progressionState.unlockedMetaIds ??= new List<string>();
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

            var ownedMetaIds = new HashSet<string>(progressionState.unlockedMetaIds, StringComparer.Ordinal);
            var ownedAdaptationIds = new HashSet<string>(progressionState.unlockedAdaptationIds, StringComparer.Ordinal);
            var eligible = MoldinessUnlockCatalog.All
                .Where(definition => definition.IsRepeatable || !ownedMetaIds.Contains(definition.Id))
                .Where(definition => definition.Type != MoldinessUnlockType.UnlockAdaptationForDrafting || !ownedAdaptationIds.Contains(definition.PayloadId))
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
            progressionState.unlockedMetaIds ??= new List<string>();
            progressionState.unlockedAdaptationIds ??= new List<string>();

            if (!MoldinessUnlockCatalog.TryGetById(unlockId, out var definition))
            {
                return new MoldinessUnlockApplicationResult(false, null);
            }

            if (!definition.IsRepeatable && progressionState.unlockedMetaIds.Contains(definition.Id))
            {
                return new MoldinessUnlockApplicationResult(false, definition);
            }

            if (!definition.IsRepeatable)
            {
                progressionState.unlockedMetaIds.Add(definition.Id);
            }

            if (definition.Type == MoldinessUnlockType.UnlockAdaptationForDrafting
                && !progressionState.unlockedAdaptationIds.Contains(definition.PayloadId))
            {
                progressionState.unlockedAdaptationIds.Add(definition.PayloadId);
            }

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
