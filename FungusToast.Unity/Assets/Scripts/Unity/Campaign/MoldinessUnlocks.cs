using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FungusToast.Unity.Campaign
{
    public enum MoldinessUnlockType
    {
        UnlockContent = 0,
    }

    [Serializable]
    public class MoldinessUnlockChoiceState
    {
        public int triggerTierIndex;
        public List<string> offeredUnlockIds = new();
    }

    public sealed class MoldinessUnlockDefinition
    {
        public MoldinessUnlockDefinition(string id, string displayName, string description, MoldinessUnlockType type, string contentId, int requiredUnlockLevel, bool isRepeatable = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Unlock id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Unlock display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(contentId)) throw new ArgumentException("Unlock content id is required.", nameof(contentId));

            Id = id;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            Type = type;
            ContentId = contentId;
            RequiredUnlockLevel = Math.Max(0, requiredUnlockLevel);
            IsRepeatable = isRepeatable;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public MoldinessUnlockType Type { get; }
        public string ContentId { get; }
        public int RequiredUnlockLevel { get; }
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
                        id: "moldiness_unlock_content_adaptation_spore_salvo",
                        displayName: "Locked Adaptation, Spore Salvo",
                        description: "Unlock a new draftable adaptation blueprint tied to Spore Salvo concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_spore_salvo",
                        requiredUnlockLevel: 1),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_content_adaptation_hyphal_bridge",
                        displayName: "Locked Adaptation, Hyphal Bridge",
                        description: "Unlock a new draftable adaptation blueprint tied to Hyphal Bridge concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_hyphal_bridge",
                        requiredUnlockLevel: 2),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_content_adaptation_vesicle_burst",
                        displayName: "Locked Adaptation, Vesicle Burst",
                        description: "Unlock a new draftable adaptation blueprint tied to Vesicle Burst concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_vesicle_burst",
                        requiredUnlockLevel: 2),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_content_adaptation_rhizomorphic_hunger",
                        displayName: "Locked Adaptation, Rhizomorphic Hunger",
                        description: "Unlock a new draftable adaptation blueprint tied to Rhizomorphic Hunger concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_rhizomorphic_hunger",
                        requiredUnlockLevel: 3),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_content_adaptation_mycelial_crescendo",
                        displayName: "Locked Adaptation, Mycelial Crescendo",
                        description: "Unlock a new draftable adaptation blueprint tied to Mycelial Crescendo concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_mycelial_crescendo",
                        requiredUnlockLevel: 3),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_content_adaptation_ossified_advance",
                        displayName: "Locked Adaptation, Ossified Advance",
                        description: "Unlock a new draftable adaptation blueprint tied to Ossified Advance concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_ossified_advance",
                        requiredUnlockLevel: 4),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_content_adaptation_distal_spore",
                        displayName: "Locked Adaptation, Distal Spore",
                        description: "Unlock a new draftable adaptation blueprint tied to Distal Spore concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_distal_spore",
                        requiredUnlockLevel: 4),
                    new MoldinessUnlockDefinition(
                        id: "moldiness_unlock_content_adaptation_conidia_ascent",
                        displayName: "Locked Adaptation, Conidia Ascent",
                        description: "Unlock a new draftable adaptation blueprint tied to Conidia Ascent concepts.",
                        type: MoldinessUnlockType.UnlockContent,
                        contentId: "locked_adaptation_conidia_ascent",
                        requiredUnlockLevel: 5),
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
            progressionState.unlockedContentIds ??= new List<string>();
            progressionState.pendingUnlockChoice ??= null;

            if (progressionState.pendingUnlockChoice != null && progressionState.pendingUnlockChoice.offeredUnlockIds?.Count > 0)
            {
                return progressionState.pendingUnlockChoice.offeredUnlockIds
                    .Select(id => MoldinessUnlockCatalog.TryGetById(id, out var definition) ? definition : null)
                    .Where(definition => definition != null)
                    .Take(Math.Max(1, count))
                    .ToList();
            }

            var ownedContentIds = new HashSet<string>(progressionState.unlockedContentIds, StringComparer.Ordinal);
            int currentUnlockLevel = Math.Max(0, progressionState.unlockLevel);
            var eligible = MoldinessUnlockCatalog.All
                .Where(definition => definition.RequiredUnlockLevel <= currentUnlockLevel + 1)
                .Where(definition => definition.IsRepeatable || !ownedContentIds.Contains(definition.ContentId))
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
            progressionState.unlockedContentIds ??= new List<string>();

            if (!MoldinessUnlockCatalog.TryGetById(unlockId, out var definition))
            {
                return new MoldinessUnlockApplicationResult(false, null);
            }

            if (!definition.IsRepeatable && progressionState.unlockedContentIds.Contains(definition.ContentId))
            {
                return new MoldinessUnlockApplicationResult(false, definition);
            }

            if (!definition.IsRepeatable)
            {
                progressionState.unlockedContentIds.Add(definition.ContentId);
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
