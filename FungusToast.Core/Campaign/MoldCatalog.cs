using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FungusToast.Core.Campaign
{
    /// <summary>
    /// Canonical mapping of mold index to display name and starting adaptation.
    /// Mold indices are 0-based and correspond to the icon slots on the mold selection screen.
    /// Starting adaptations assigned here are never offered in mid-run drafts.
    /// </summary>
    public static class MoldCatalog
    {
        public sealed class MoldEntry
        {
            public int Index { get; }
            public string DisplayName { get; }
            public string StartingAdaptationId { get; }

            public MoldEntry(int index, string displayName, string startingAdaptationId)
            {
                Index = index;
                DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
                StartingAdaptationId = startingAdaptationId ?? throw new ArgumentNullException(nameof(startingAdaptationId));
            }
        }

        private static readonly ReadOnlyCollection<MoldEntry> all =
            new ReadOnlyCollection<MoldEntry>(
                new List<MoldEntry>
                {
                    new MoldEntry(0, "Mycelavis",    AdaptationIds.ObliqueFilament),
                    new MoldEntry(1, "Sporalunea",   AdaptationIds.ThanatrophicRebound),
                    new MoldEntry(2, "Cineramyxa",   AdaptationIds.ToxinPrimacy),
                    new MoldEntry(3, "Velutora",     AdaptationIds.CentripetalGermination),
                    new MoldEntry(4, "Glaucoryza",   AdaptationIds.SignalEconomy),
                    new MoldEntry(5, "Viridomyxa",   AdaptationIds.LiminalSporemeal),
                    new MoldEntry(6, "Noctephyra",   AdaptationIds.PutrefactiveResilience),
                    new MoldEntry(7, "Aureomycella",  AdaptationIds.CompoundReserve),
                });

        public static IReadOnlyList<MoldEntry> All => all;

        /// <summary>
        /// Returns the display name for the given mold index, or a fallback if out of range.
        /// </summary>
        public static string GetDisplayName(int moldIndex)
        {
            if (moldIndex >= 0 && moldIndex < all.Count)
                return all[moldIndex].DisplayName;
            return $"Mold {moldIndex + 1}";
        }

        /// <summary>
        /// Returns the starting adaptation ID for the given mold index,
        /// or an empty string if out of range.
        /// </summary>
        public static string GetStartingAdaptationId(int moldIndex)
        {
            if (moldIndex >= 0 && moldIndex < all.Count)
                return all[moldIndex].StartingAdaptationId;
            return string.Empty;
        }
    }
}
