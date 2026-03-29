using System;
using System.Collections.Generic;

namespace FungusToast.Core.Campaign
{
    /// <summary>
    /// A named set of starting adaptations curated for a specific strategy identity.
    /// Used to express high-synergy elite or boss adaptation loadouts.
    /// </summary>
    public sealed class AdaptationSynergySet
    {
        public string SetName { get; }
        public string Description { get; }
        public IReadOnlyList<string> AdaptationIds { get; }

        public AdaptationSynergySet(string setName, string description, IReadOnlyList<string> adaptationIds)
        {
            SetName = setName;
            Description = description;
            AdaptationIds = adaptationIds ?? Array.Empty<string>();
        }
    }
}
