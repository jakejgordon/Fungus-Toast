using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Campaign;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Resolves an AI's complete starting Adaptation loadout. The mold-matched
    /// starting Adaptation is mandatory; authored additions take precedence;
    /// campaign difficulty fills any remaining quota without replacement.
    /// </summary>
    public static class AIStartingAdaptationResolver
    {
        public static IReadOnlyList<string> Resolve(
            int moldIndex,
            CampaignDifficulty? campaignDifficulty,
            IEnumerable<string>? authoredAdditionalIds,
            IEnumerable<AdaptationSynergySet>? suggestedAdaptationSets,
            Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var resolved = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            bool hasMoldAdaptation = AddIfKnown(
                MoldCatalog.GetStartingAdaptationId(moldIndex),
                resolved,
                seen);

            foreach (var adaptationId in authoredAdditionalIds ?? Array.Empty<string>())
            {
                AddIfKnown(adaptationId, resolved, seen);
            }

            int baselineCount = hasMoldAdaptation ? 1 : 0;
            int authoredExtraCount = Math.Max(0, resolved.Count - baselineCount);
            int remainingExtraCount = Math.Max(
                0,
                GetAdditionalAdaptationCount(campaignDifficulty) - authoredExtraCount);

            var themedCandidates = (suggestedAdaptationSets ?? Array.Empty<AdaptationSynergySet>())
                .SelectMany(set => set.AdaptationIds)
                .Where(IsEligibleAdditionalAdaptation)
                .Where(id => !seen.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            AddRandomWithoutReplacement(themedCandidates, remainingExtraCount, resolved, seen, rng);
            remainingExtraCount = Math.Max(
                0,
                GetAdditionalAdaptationCount(campaignDifficulty) - Math.Max(0, resolved.Count - baselineCount));

            if (remainingExtraCount > 0)
            {
                var fallbackCandidates = AdaptationRepository.All
                    .Where(adaptation => !adaptation.IsStartingAdaptation)
                    .Select(adaptation => adaptation.Id)
                    .Where(id => !seen.Contains(id))
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList();
                AddRandomWithoutReplacement(fallbackCandidates, remainingExtraCount, resolved, seen, rng);
            }

            return resolved;
        }

        public static int GetAdditionalAdaptationCount(CampaignDifficulty? campaignDifficulty)
        {
            return campaignDifficulty switch
            {
                CampaignDifficulty.Training => 0,
                CampaignDifficulty.Easy => 1,
                CampaignDifficulty.Medium => 2,
                CampaignDifficulty.Hard => 3,
                CampaignDifficulty.Elite => 4,
                CampaignDifficulty.Boss => 5,
                _ => 0
            };
        }

        private static bool IsEligibleAdditionalAdaptation(string adaptationId)
        {
            return AdaptationRepository.TryGetById(adaptationId, out var adaptation)
                && adaptation != null
                && !adaptation.IsStartingAdaptation;
        }

        private static bool AddIfKnown(
            string adaptationId,
            ICollection<string> resolved,
            ISet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(adaptationId)
                || seen.Contains(adaptationId)
                || !AdaptationRepository.TryGetById(adaptationId, out _))
            {
                return false;
            }

            seen.Add(adaptationId);
            resolved.Add(adaptationId);
            return true;
        }

        private static void AddRandomWithoutReplacement(
            IList<string> candidates,
            int count,
            ICollection<string> resolved,
            ISet<string> seen,
            Random rng)
        {
            int remaining = Math.Min(count, candidates.Count);
            while (remaining > 0 && candidates.Count > 0)
            {
                int index = rng.Next(candidates.Count);
                string selected = candidates[index];
                candidates.RemoveAt(index);
                if (!seen.Add(selected))
                {
                    continue;
                }

                resolved.Add(selected);
                remaining--;
            }
        }
    }
}
