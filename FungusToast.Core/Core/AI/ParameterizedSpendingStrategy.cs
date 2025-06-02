using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public class ParameterizedSpendingStrategy : MutationSpendingStrategyBase
    {
        public override string StrategyName { get; }
        public override MutationTier? MaxTier => maxTier;
        public override bool? PrioritizeHighTier => prioritizeHighTier;

        public override bool? UsesGrowth => GetCategories().Contains(MutationCategory.Growth);
        public override bool? UsesCellularResilience => GetCategories().Contains(MutationCategory.CellularResilience);
        public override bool? UsesFungicide => GetCategories().Contains(MutationCategory.Fungicide);
        public override bool? UsesGeneticDrift => GetCategories().Contains(MutationCategory.GeneticDrift);

        private readonly MutationTier maxTier;
        private readonly bool prioritizeHighTier;
        private readonly List<MutationCategory>? priorityMutationCategories;
        private readonly List<Mutation> targetPrerequisiteChain;
        private readonly Dictionary<int, int> requiredLevels;

        public ParameterizedSpendingStrategy(
            string strategyName,
            bool prioritizeHighTier,
            List<MutationCategory>? priorityMutationCategories = null,
            MutationTier maxTier = MutationTier.Tier10,
            List<int>? targetMutationIds = null)
        {
            StrategyName = strategyName;
            this.prioritizeHighTier = prioritizeHighTier;
            this.priorityMutationCategories = priorityMutationCategories;
            this.maxTier = maxTier;

            targetPrerequisiteChain = new List<Mutation>();
            requiredLevels = new Dictionary<int, int>();

            if (targetMutationIds != null)
            {
                BuildTargetPrerequisiteChain(targetMutationIds);
            }
        }

        private void BuildTargetPrerequisiteChain(List<int> targetMutationIds)
        {
            var visited = new HashSet<int>();
            targetPrerequisiteChain.Clear();
            requiredLevels.Clear();

            //Console.WriteLine($"\n[{StrategyName}] Building full prerequisite chain for targets: {string.Join(", ", targetMutationIds)}");

            foreach (int targetId in targetMutationIds)
            {
                if (!MutationRepository.All.TryGetValue(targetId, out var target))
                {
                    //Console.WriteLine($"[WARNING] Target mutation ID {targetId} not found.");
                    continue;
                }

                Visit(target, requiredLevel: 1);
            }

            void Visit(Mutation mutation, int requiredLevel)
            {
                // Always track the highest required level seen for this mutation
                if (requiredLevels.TryGetValue(mutation.Id, out var existingLevel))
                {
                    if (requiredLevel > existingLevel)
                    {
                        //Console.WriteLine($" - Updating required level for {mutation.Name} (ID {mutation.Id}) from {existingLevel} to {requiredLevel}");
                        requiredLevels[mutation.Id] = requiredLevel;
                    }
                }
                else
                {
                    requiredLevels[mutation.Id] = requiredLevel;
                }

                if (!visited.Add(mutation.Id))
                    return; // already processed

                // Recurse to prerequisites first
                foreach (var prereq in mutation.Prerequisites)
                {
                    if (MutationRepository.All.TryGetValue(prereq.MutationId, out var prereqMutation))
                    {
                        Visit(prereqMutation, prereq.RequiredLevel);
                    }
                    else
                    {
                        //Console.WriteLine($"[WARNING] Prerequisite ID {prereq.MutationId} not found in repository.");
                    }
                }

                // Then add to chain so prerequisites come first
                //Console.WriteLine($" - Added {mutation.Name} (ID {mutation.Id}), required level = {requiredLevels[mutation.Id]}");
                targetPrerequisiteChain.Add(mutation);
            }
        }

        public override void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board)
        {
            if (player == null || allMutations == null || allMutations.Count == 0)
                return;

            SpendOnTargetChain(player, allMutations, board);
            SpendFallbackPoints(player, allMutations, board);
        }

        private void SpendOnTargetChain(Player player, List<Mutation> allMutations, GameBoard board)
        {
            bool upgraded;
            do
            {
                upgraded = false;

                foreach (var mutation in targetPrerequisiteChain)
                {
                    int requiredLevel = requiredLevels.TryGetValue(mutation.Id, out var level) ? level : 1;
                    int currentLevel = player.GetMutationLevel(mutation.Id);

                    if (currentLevel < requiredLevel && player.CanUpgrade(mutation))
                    {
                        if (TryUpgradeWithTendrilAwareness(player, mutation, allMutations, board))
                        {
                            upgraded = true;
                            break;
                        }
                    }
                }

            } while (upgraded && player.MutationPoints > 0);
        }

        private void SpendFallbackPoints(Player player, List<Mutation> allMutations, GameBoard board)
        {
            bool spent;
            do
            {
                spent = TrySpendByCategory(player, allMutations, board)
                     || TrySpendFallback(player, allMutations, board)
                     || TrySpendRandomly(player, allMutations);
            }
            while (spent && player.MutationPoints > 0);
        }

        private bool TrySpendByCategory(Player player, List<Mutation> allMutations, GameBoard board)
        {
            foreach (var category in GetShuffledCategories())
            {
                var candidates = allMutations
                    .Where(m => m.Category == category
                                && (int)m.Tier <= (int)maxTier
                                && player.CanUpgrade(m))
                    .ToList();

                if (TrySpendWithinCategory(player, board, candidates))
                    return true;
            }

            return false;
        }

        private bool TrySpendFallback(Player player, List<Mutation> allMutations, GameBoard board)
        {
            var fallbackCandidates = allMutations
                .Where(m => (int)m.Tier <= (int)maxTier && player.CanUpgrade(m))
                .ToList();

            return TrySpendWithinCategory(player, board, fallbackCandidates);
        }

        private bool TrySpendRandomly(Player player, List<Mutation> allMutations)
        {
            return MutationSpendingHelper.TrySpendRandomly(player, allMutations);
        }


        private List<MutationCategory> GetCategories()
        {
            return priorityMutationCategories ?? Enum.GetValues(typeof(MutationCategory)).Cast<MutationCategory>().ToList();
        }

        private bool TrySpendWithinCategory(Player player, GameBoard board, List<Mutation> candidates)
        {
            if (prioritizeHighTier)
            {
                foreach (var m in candidates
                    .Where(m => m.Prerequisites.Any())
                    .OrderByDescending(m => m.Tier))
                {
                    if (TryUpgradeWithTendrilAwareness(player, m, candidates, board))
                        return true;
                }
            }

            foreach (var m in candidates)
            {
                if (TryUpgradeWithTendrilAwareness(player, m, candidates, board))
                    return true;
            }

            return false;
        }

        private bool TryUpgradeWithTendrilAwareness(Player player, Mutation candidate, List<Mutation> allCandidates, GameBoard board)
        {
            if (IsTendril(candidate))
            {
                var bestTendril = PickBestTendrilMutation(player, allCandidates.Where(IsTendril).ToList(), board);
                if (bestTendril != null)
                    return player.TryUpgradeMutation(bestTendril);
                return false;
            }

            return player.TryUpgradeMutation(candidate);
        }

        private bool IsTendril(Mutation m)
        {
            return m.Id == MutationIds.TendrilNorthwest
                || m.Id == MutationIds.TendrilNortheast
                || m.Id == MutationIds.TendrilSouthwest
                || m.Id == MutationIds.TendrilSoutheast;
        }

        private List<MutationCategory> GetShuffledCategories()
        {
            return GetCategories()
                .OrderBy(_ => Guid.NewGuid())
                .ToList();
        }

    }
}
