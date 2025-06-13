using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
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
        private readonly int surgeAttemptTurnFrequency;
        private readonly List<MutationCategory>? priorityMutationCategories;
        private readonly List<Mutation> targetPrerequisiteChain;
        private readonly Dictionary<int, int> requiredLevels;

        // NEW: List of surge mutations for this strategy (can be empty)
        private readonly List<int> surgePriorityIds;

        public ParameterizedSpendingStrategy(
        string strategyName,
        bool prioritizeHighTier,
        List<MutationCategory>? priorityMutationCategories = null,
        MutationTier maxTier = MutationTier.Tier10,
        List<int>? targetMutationIds = null,
        List<int>? surgePriorityIds = null,
        int surgeAttemptTurnFrequency = GameBalance.DefaultSurgeAIAttemptTurnFrequency)
        {
            StrategyName = strategyName;
            this.prioritizeHighTier = prioritizeHighTier;
            this.priorityMutationCategories = priorityMutationCategories;
            this.maxTier = maxTier;

            targetPrerequisiteChain = new List<Mutation>();
            requiredLevels = new Dictionary<int, int>();

            this.surgePriorityIds = surgePriorityIds ?? new();
            this.surgeAttemptTurnFrequency = surgeAttemptTurnFrequency;

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

            foreach (int targetId in targetMutationIds)
            {
                if (!MutationRepository.All.TryGetValue(targetId, out var target))
                {
                    continue;
                }
                Visit(target, requiredLevel: target.MaxLevel);
            }

            void Visit(Mutation mutation, int requiredLevel)
            {
                if (requiredLevels.TryGetValue(mutation.Id, out var existingLevel))
                {
                    if (requiredLevel > existingLevel)
                    {
                        requiredLevels[mutation.Id] = requiredLevel;
                    }
                }
                else
                {
                    requiredLevels[mutation.Id] = requiredLevel;
                }

                if (!visited.Add(mutation.Id))
                    return; // already processed

                foreach (var prereq in mutation.Prerequisites)
                {
                    if (MutationRepository.All.TryGetValue(prereq.MutationId, out var prereqMutation))
                    {
                        Visit(prereqMutation, prereq.RequiredLevel);
                    }
                }
                targetPrerequisiteChain.Add(mutation);
            }
        }

        protected override void PerformSpendingLogic(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver? simulationObserver = null)
        {
            SpendOnTargetChain(player, allMutations, board, simulationObserver);

            // Try to activate surges on the Nth round before fallback spending
            if (TrySpendOnSurges(player, allMutations, board, simulationObserver, onlyOnNthRound: true))
                return; // If a surge is triggered, stop spending for this turn

            SpendFallbackPoints(player, allMutations, board, simulationObserver);

            // After ALL other spending, always try to activate surges as last resort (any turn)
            TrySpendOnSurges(player, allMutations, board, simulationObserver, onlyOnNthRound: false);
        }


        private void SpendOnTargetChain(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver? simulationObserver = null)
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
                        if (TryUpgradeWithTendrilAwareness(player, mutation, allMutations, board, simulationObserver))
                        {
                            upgraded = true;
                            break;
                        }
                    }
                }

            } while (upgraded && player.MutationPoints > 0);
        }

        /// <summary>
        /// Attempts to activate a surge mutation for the player, following these rules:
        /// - If onlyOnNthRound is true, will only attempt surges on rounds where (CurrentRound % surgeAttemptTurnFrequency == 0).
        /// - If onlyOnNthRound is false, will always attempt to activate surges as a fallback **if there are no upgradable non-surge mutations** (i.e., as a true last resort, every turn).
        /// 
        /// Surges are always considered in priority order as defined in surgePriorityIds.
        /// Returns true if a surge was activated.
        /// </summary>
        private bool TrySpendOnSurges(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver? simulationObserver,
            bool onlyOnNthRound)
        {
            int currentRound = board.CurrentRound;
            bool nthRound = surgeAttemptTurnFrequency > 0 &&
                            (currentRound > 0) &&
                            (currentRound % surgeAttemptTurnFrequency == 0);

            var availableSurges = surgePriorityIds
                .Select(id => allMutations.FirstOrDefault(m => m.Id == id && m.IsSurge))
                .Where(m => m != null)
                .ToList();

            if (onlyOnNthRound)
            {
                // Only allow surge attempts on Nth round
                if (!nthRound)
                    return false;

                foreach (var surge in availableSurges)
                {
                    if (!player.IsSurgeActive(surge.Id))
                    {
                        int currentLevel = player.GetMutationLevel(surge.Id);
                        int cost = surge.GetSurgeActivationCost(currentLevel);

                        if (player.MutationPoints >= cost)
                        {
                            player.TryUpgradeMutation(surge, simulationObserver);
                            return true;
                        }
                    }
                }
            }
            else
            {
                // "True last resort": Only attempt surges if all non-surge mutations are maxed (no upgrades left)
                var upgradableNonSurge = allMutations
                    .Where(m => !m.IsSurge && player.CanUpgrade(m))
                    .ToList();

                if (upgradableNonSurge.Count == 0)
                {
                    foreach (var surge in availableSurges)
                    {
                        if (!player.IsSurgeActive(surge.Id))
                        {
                            int currentLevel = player.GetMutationLevel(surge.Id);
                            int cost = surge.GetSurgeActivationCost(currentLevel);

                            if (player.MutationPoints >= cost)
                            {
                                player.TryUpgradeMutation(surge, simulationObserver);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }


        private void SpendFallbackPoints(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver? simulationObserver = null)
        {
            bool spent;
            do
            {
                spent = TrySpendByCategory(player, allMutations, board, simulationObserver)
                     || TrySpendFallback(player, allMutations, board, simulationObserver)
                     || TrySpendRandomly(player, allMutations, simulationObserver);
            }
            while (spent && player.MutationPoints > 0);

            while (player.MutationPoints > 0)
            {
                var anyUpgradable = allMutations.Where(m => player.CanUpgrade(m)).ToList();
                if (anyUpgradable.Count == 0)
                    break;
                player.TryUpgradeMutation(anyUpgradable[0]);
            }
        }

        private bool TrySpendByCategory(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver? simulationObserver)
        {
            foreach (var category in GetShuffledCategories())
            {
                var candidates = allMutations
                    .Where(m => m.Category == category
                                && (int)m.Tier <= (int)maxTier
                                && player.CanUpgrade(m))
                    .ToList();

                if (TrySpendWithinCategory(player, board, candidates, simulationObserver))
                    return true;
            }

            return false;
        }

        private bool TrySpendFallback(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver? simulationObserver)
        {
            var fallbackCandidates = allMutations
                .Where(m => (int)m.Tier <= (int)maxTier && player.CanUpgrade(m))
                .ToList();

            return TrySpendWithinCategory(player, board, fallbackCandidates, simulationObserver);
        }

        private bool TrySpendRandomly(
            Player player,
            List<Mutation> allMutations,
            ISimulationObserver? simulationObserver)
        {
            return MutationSpendingHelper.TrySpendRandomly(player, allMutations, simulationObserver);
        }

        private List<MutationCategory> GetCategories()
        {
            return priorityMutationCategories ?? Enum.GetValues(typeof(MutationCategory)).Cast<MutationCategory>().ToList();
        }

        private bool TrySpendWithinCategory(
            Player player,
            GameBoard board,
            List<Mutation> candidates,
            ISimulationObserver? simulationObserver)
        {
            if (prioritizeHighTier)
            {
                foreach (var m in candidates
                    .Where(m => m.Prerequisites.Any())
                    .OrderByDescending(m => m.Tier))
                {
                    if (TryUpgradeWithTendrilAwareness(player, m, candidates, board, simulationObserver))
                        return true;
                }
            }

            foreach (var m in candidates)
            {
                if (TryUpgradeWithTendrilAwareness(player, m, candidates, board, simulationObserver))
                    return true;
            }

            return false;
        }

        private bool TryUpgradeWithTendrilAwareness(
            Player player,
            Mutation candidate,
            List<Mutation> allCandidates,
            GameBoard board,
            ISimulationObserver? simulationObserver)
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
