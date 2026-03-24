using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mutations
{
    /// <summary>
    /// Centralized mutation synergy hints used by AI strategy authoring/auditing.
    ///
    /// This is intentionally lightweight: it does not change gameplay behavior,
    /// only provides guidance for strategy coherence checks.
    /// </summary>
    public static class MutationSynergyCatalog
    {
        private static readonly Dictionary<int, HashSet<MutationCategory>> SuggestedBackboneCategoriesByMutationId =
            new()
            {
                // Surges
                [MutationIds.HyphalSurge] = new HashSet<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.GeneticDrift
                },
                [MutationIds.ChemotacticBeacon] = new HashSet<MutationCategory>
                {
                    MutationCategory.Growth
                },
                [MutationIds.ChitinFortification] = new HashSet<MutationCategory>
                {
                    MutationCategory.CellularResilience,
                    MutationCategory.Fungicide
                },
                [MutationIds.MimeticResilience] = new HashSet<MutationCategory>
                {
                    MutationCategory.CellularResilience
                },
                [MutationIds.CompetitiveAntagonism] = new HashSet<MutationCategory>
                {
                    MutationCategory.Fungicide,
                    MutationCategory.GeneticDrift
                },
            };

        private static readonly Dictionary<int, List<int>> BuffingMutationIdsByMutationId =
            new()
            {
                [MutationIds.ChemotacticBeacon] = new List<int>
                {
                    MutationIds.PutrefactiveMycotoxin
                }
            };

        public static IReadOnlyCollection<MutationCategory> GetSuggestedBackboneCategories(int mutationId)
        {
            if (SuggestedBackboneCategoriesByMutationId.TryGetValue(mutationId, out var categories))
            {
                return categories;
            }

            return new List<MutationCategory>();
        }

        public static string DescribeBackboneCategories(int mutationId)
        {
            var categories = GetSuggestedBackboneCategories(mutationId);
            if (categories.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", categories.Select(c => c.ToString()));
        }

        public static IReadOnlyCollection<int> GetBuffingMutationIds(int mutationId)
        {
            if (BuffingMutationIdsByMutationId.TryGetValue(mutationId, out var mutationIds))
            {
                return mutationIds;
            }

            return new List<int>();
        }

        public static string DescribeBuffingMutations(int mutationId, IReadOnlyDictionary<int, Mutation> allMutations)
        {
            var mutationIds = GetBuffingMutationIds(mutationId);
            if (mutationIds.Count == 0)
            {
                return string.Empty;
            }

            var mutationNames = mutationIds
                .Select(id => allMutations.TryGetValue(id, out var mutation) ? mutation.Name : null)
                .Where(name => !string.IsNullOrWhiteSpace(name));

            return string.Join(", ", mutationNames!);
        }
    }
}
