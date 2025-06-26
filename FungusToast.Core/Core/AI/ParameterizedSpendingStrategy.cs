using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.AI
{
    public enum EconomyBias
    {
        Neutral,
        IgnoreEconomy,
        MinorEconomy,
        ModerateEconomy,
        MaxEconomy
    }

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
        private readonly List<int> surgePriorityIds;

        // New: Bias mode for economy investment
        private readonly EconomyBias economyBias;

        public ParameterizedSpendingStrategy(
            string strategyName,
            bool prioritizeHighTier,
            List<MutationCategory>? priorityMutationCategories = null,
            MutationTier maxTier = MutationTier.Tier10,
            List<int>? targetMutationIds = null,
            List<int>? surgePriorityIds = null,
            int surgeAttemptTurnFrequency = GameBalance.DefaultSurgeAIAttemptTurnFrequency,
            EconomyBias economyBias = EconomyBias.Neutral)
        {
            StrategyName = strategyName;
            this.prioritizeHighTier = prioritizeHighTier;
            this.priorityMutationCategories = priorityMutationCategories;
            this.maxTier = maxTier;

            targetPrerequisiteChain = new List<Mutation>();
            requiredLevels = new Dictionary<int, int>();

            this.surgePriorityIds = surgePriorityIds ?? new();
            this.surgeAttemptTurnFrequency = surgeAttemptTurnFrequency;
            this.economyBias = economyBias;

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
            Random rnd,
            ISimulationObserver? simulationObserver = null)
        {
            SpendOnTargetChain(player, allMutations, board, simulationObserver);

            // Try to activate surges on the Nth round before fallback spending
            if (TrySpendOnSurges(player, allMutations, board, simulationObserver, onlyOnNthRound: true))
                return; // If a surge is triggered, stop spending for this turn

            SpendFallbackPoints(player, allMutations, board, rnd, simulationObserver);

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
            Random rnd,
            ISimulationObserver? simulationObserver = null)
        {
            bool spent;
            do
            {
                spent = TrySpendByCategory(player, allMutations, board, simulationObserver)
                     || TrySpendFallback(player, allMutations, board, simulationObserver)
                     || TrySpendEconomyBiasedRandomly(player, allMutations, board, rnd, simulationObserver);
            }
            while (spent && player.MutationPoints > 0);

            // Burn off leftovers if any upgradable mutations remain (should almost never be necessary)
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

        /// <summary>
        /// Main fallback for random spending, but modulates probability of Genetic Drift mutations
        /// based on economyBias and game round. Non-drift mutations are selected at normal rate.
        /// </summary>
        private bool TrySpendEconomyBiasedRandomly(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            Random rnd,
            ISimulationObserver? simulationObserver)
        {
            var upgradable = allMutations
                .Where(m => player.CanUpgrade(m) && (int)m.Tier <= (int)maxTier)
                .ToList();

            if (upgradable.Count == 0)
                return false;

            // Split drift/non-drift
            var nonDrift = upgradable.Where(m => m.Category != MutationCategory.GeneticDrift).ToList();
            var drift = upgradable.Where(m => m.Category == MutationCategory.GeneticDrift).ToList();

            if (economyBias == EconomyBias.IgnoreEconomy)
            {
                // Only upgrade drift if in target chain
                drift = drift.Where(m => IsInTargetChain(m.Id)).ToList();
                if (nonDrift.Count == 0 && drift.Count > 0)
                    return TryUpgradeWithTendrilAwareness(player, drift[0], upgradable, board, simulationObserver);
                if (nonDrift.Count > 0)
                    return TryUpgradeWithTendrilAwareness(player, nonDrift[0], upgradable, board, simulationObserver);
                return false;
            }

            // NEW: NEUTRAL LOGIC — treat all equally
            if (economyBias == EconomyBias.Neutral)
            {
                // Just pick randomly from all upgradable mutations
                int idx = rnd.Next(upgradable.Count);
                var chosen = upgradable[idx];
                return TryUpgradeWithTendrilAwareness(player, chosen, upgradable, board, simulationObserver);
            }

            // Economy bias: filter or adjust drift mutations as needed
            float driftWeight = GetDriftWeight(board.CurrentRound);

            // Build a weighted pool for random selection
            var weightedPool = new List<Mutation>();
            int baseWeight = 100; // Arbitrary, relative

            foreach (var m in nonDrift)
            {
                weightedPool.Add(m);
            }
            foreach (var m in drift)
            {
                int weight = (int)(baseWeight * driftWeight);
                // At low weights, add 0 or 1 entries (so drift is rare/never selected)
                for (int i = 0; i < weight; i++)
                {
                    weightedPool.Add(m);
                }
            }
            if (weightedPool.Count == 0)
                return false;

            // Pick randomly from weighted pool
            int idx2 = rnd.Next(weightedPool.Count);
            var chosen2 = weightedPool[idx2];

            return TryUpgradeWithTendrilAwareness(player, chosen2, upgradable, board, simulationObserver);
        }


        /// <summary>
        /// Returns a scaling factor for drift upgrades, 0–1+
        /// Stronger starting weights and slower declines for more balanced drift investment.
        /// </summary>
        private float GetDriftWeight(int currentRound)
        {
            switch (economyBias)
            {
                case EconomyBias.MinorEconomy:
                    // Starts at 0.4, drops more slowly over 16 rounds
                    return Math.Max(0.05f, 0.4f - 0.022f * currentRound);
                case EconomyBias.ModerateEconomy:
                    // Starts at 0.7, drops to 0.07 at round 20 (so a little always remains)
                    return Math.Max(0.07f, 0.7f - 0.0315f * currentRound);
                case EconomyBias.MaxEconomy:
                    // Always strong, never drops below 0.8
                    return 0.8f;
                default:
                    // IgnoreEconomy and others: should never be called, but just in case
                    return 0.0f;
            }
        }


        private bool IsInTargetChain(int mutationId)
        {
            return targetPrerequisiteChain.Any(m => m.Id == mutationId);
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

        private List<MutationCategory> GetCategories()
        {
            return priorityMutationCategories ?? Enum.GetValues(typeof(MutationCategory)).Cast<MutationCategory>().ToList();
        }

        private List<MutationCategory> GetShuffledCategories()
        {
            return GetCategories()
                .OrderBy(_ => Guid.NewGuid())
                .ToList();
        }
    }
}
