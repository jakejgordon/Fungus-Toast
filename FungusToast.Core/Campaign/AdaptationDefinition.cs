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

        public AdaptationDefinition(string id, string name, string description, string? iconId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Adaptation id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Adaptation name is required.", nameof(name));

            Id = id;
            Name = name;
            Description = description ?? string.Empty;
            IconId = string.IsNullOrWhiteSpace(iconId) ? id : iconId;
        }
    }
}
