using System;

namespace FungusToast.Core.Campaign
{
    /// <summary>
    /// Immutable campaign adaptation metadata used by Unity campaign reward flow.
    /// </summary>
    public sealed class AdaptationDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string IconId { get; }

        /// <summary>
        /// When true, this adaptation is assigned automatically based on the player's mold selection
        /// and is never offered in mid-run adaptation drafts.
        /// </summary>
        public bool IsStartingAdaptation { get; }

        /// <summary>
        /// When true, this adaptation cannot appear in normal adaptation drafts until it has been
        /// explicitly unlocked by a moldiness reward.
        /// </summary>
        public bool IsLocked { get; }

        /// <summary>
        /// Minimum moldiness unlock level required before this adaptation's unlock reward can appear
        /// in moldiness drafts.
        /// </summary>
        public int RequiredMoldinessUnlockLevel { get; }

        public AdaptationDefinition(
            string id,
            string name,
            string description,
            string? iconId = null,
            bool isStartingAdaptation = false,
            bool isLocked = false,
            int requiredMoldinessUnlockLevel = 0)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Adaptation id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Adaptation name is required.", nameof(name));

            Id = id;
            Name = name;
            Description = description ?? string.Empty;
            IconId = string.IsNullOrWhiteSpace(iconId) ? id : iconId;
            IsStartingAdaptation = isStartingAdaptation;
            IsLocked = isLocked;
            RequiredMoldinessUnlockLevel = Math.Max(0, requiredMoldinessUnlockLevel);
        }
    }
}
