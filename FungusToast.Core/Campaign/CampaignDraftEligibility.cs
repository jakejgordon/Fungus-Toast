using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Campaign
{
    /// <summary>
    /// Shared eligibility rules for campaign reward drafts.
    /// </summary>
    public static class CampaignDraftEligibility
    {
        public static List<AdaptationDefinition> GetEligibleAdaptations(
            IEnumerable<AdaptationDefinition> allAdaptations,
            IEnumerable<string> activeAdaptationIds,
            IEnumerable<string> unlockedAdaptationIds,
            int currentUnlockLevel)
        {
            if (allAdaptations == null)
            {
                return new List<AdaptationDefinition>();
            }

            var selected = new HashSet<string>(activeAdaptationIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var permanentlyUnlockedAdaptations = new HashSet<string>(
                unlockedAdaptationIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);

            return allAdaptations
                .Where(adaptation => !adaptation.IsStartingAdaptation)
                .Where(adaptation => !adaptation.IsLocked
                    || (adaptation.RequiredMoldinessUnlockLevel <= currentUnlockLevel
                        && permanentlyUnlockedAdaptations.Contains(adaptation.Id)))
                .Where(adaptation => !selected.Contains(adaptation.Id))
                .ToList();
        }
    }
}