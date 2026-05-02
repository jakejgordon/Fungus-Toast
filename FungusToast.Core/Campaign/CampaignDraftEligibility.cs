using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mycovariants;

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

        public static List<Mycovariant> GetEligibleMycovariants(
            IEnumerable<Mycovariant> allMycovariants,
            IEnumerable<int> unlockedMycovariantIds,
            int currentUnlockLevel,
            int? forcedMycovariantId = null)
        {
            if (allMycovariants == null)
            {
                return new List<Mycovariant>();
            }

            var all = allMycovariants.ToList();
            var permanentlyUnlockedMycovariants = new HashSet<int>(unlockedMycovariantIds ?? Array.Empty<int>());

            var eligible = all
                .Where(mycovariant => !mycovariant.IsLocked
                    || (mycovariant.RequiredMoldinessUnlockLevel <= currentUnlockLevel
                        && permanentlyUnlockedMycovariants.Contains(mycovariant.Id)))
                .ToList();

            if (forcedMycovariantId.HasValue)
            {
                var forced = all.FirstOrDefault(mycovariant => mycovariant.Id == forcedMycovariantId.Value);
                if (forced != null && eligible.All(mycovariant => mycovariant.Id != forced.Id))
                {
                    eligible.Add(forced);
                }
            }

            return eligible;
        }
    }
}