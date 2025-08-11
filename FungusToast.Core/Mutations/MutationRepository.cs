using FungusToast.Core.Config;
using FungusToast.Core.Mutations.Factories;
using System.Collections.Generic;
using System.Globalization;

namespace FungusToast.Core.Mutations
{
    public static class MutationRepository
    {
        public static readonly Dictionary<int, Mutation> All;
        public static readonly Dictionary<int, Mutation> Roots;

        /// Cached list of prerequisite chains per mutation ID, ordered from root to leaf.
        public static readonly Dictionary<int, List<Mutation>> PrerequisiteChains;

        static MutationRepository()
        {
            (All, Roots) = BuildFullMutationSet();

            PrerequisiteChains = new Dictionary<int, List<Mutation>>();
            foreach (var mutation in All.Values)
            {
                PrerequisiteChains[mutation.Id] = GetPrerequisiteChain(mutation, All);
            }
        }

        public static (Dictionary<int, Mutation> all, Dictionary<int, Mutation> roots) BuildFullMutationSet()
        {
            var allMutations = new Dictionary<int, Mutation>();
            var rootMutations = new Dictionary<int, Mutation>();
            var helper = new MutationBuilderHelper(allMutations, rootMutations);

            // Create mutations using category-specific factories
            GrowthMutationFactory.CreateMutations(allMutations, rootMutations, helper);
            CellularResilienceMutationFactory.CreateMutations(allMutations, rootMutations, helper);
            FungicideMutationFactory.CreateMutations(allMutations, rootMutations, helper);
            GeneticDriftMutationFactory.CreateMutations(allMutations, rootMutations, helper);
            MycelialSurgesMutationFactory.CreateMutations(allMutations, rootMutations, helper);

            return (allMutations, rootMutations);
        }

        private static List<Mutation> GetPrerequisiteChain(Mutation mutation, Dictionary<int, Mutation> allMutations)
        {
            var visited = new HashSet<int>();
            var chain = new List<Mutation>();

            void Visit(Mutation m)
            {
                if (!visited.Add(m.Id))
                    return;

                foreach (var prereq in m.Prerequisites)
                {
                    if (allMutations.TryGetValue(prereq.MutationId, out var prereqMutation))
                    {
                        Visit(prereqMutation);
                    }
                }

                chain.Add(m); // Add after visiting prereqs (post-order)
            }

            Visit(mutation);
            return chain;
        }
    }
}

