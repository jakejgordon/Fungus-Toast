using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
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

    /// <summary>
    /// Defines a preference for a specific mycovariant with optional priority level
    /// </summary>
    public class MycovariantPreference
    {
        public IReadOnlyCollection<int> MycovariantIds { get; }
        public int Priority { get; } // Higher number = higher priority
        public string Description { get; }

        public MycovariantPreference(int mycovariantId, int priority = 1, string description = "")
        {
            MycovariantIds = new List<int> { mycovariantId };
            Priority = priority;
            Description = description;
        }
        public MycovariantPreference(IEnumerable<int> mycovariantIds, int priority = 1, string description = "")
        {
            MycovariantIds = mycovariantIds.ToList();
            Priority = priority;
            Description = description;
        }
    }

    /// <summary>
    /// Parameterized strategy supporting a sequential list of mutation goals (with optional level targets).
    /// Will automatically build up prerequisites as needed. Falls back to category/priority/random logic when all goals are met.
    /// </summary>
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
        private readonly List<TargetMutationGoal> targetMutationGoals;
        private readonly List<int> surgePriorityIds;
        private readonly EconomyBias economyBias;
        private readonly List<MycovariantPreference> mycovariantPreferences;

        // ==== NEW: Dynamic Timing Awareness ====
        private enum GamePhase
        {
            EarlyGame,    // Rounds 1-10
            MidGame,      // Rounds 11-25  
            LateGame      // Rounds 26+
        }

        public ParameterizedSpendingStrategy(
            string strategyName,
            bool prioritizeHighTier,
            List<MutationCategory>? priorityMutationCategories = null,
            MutationTier maxTier = MutationTier.Tier10,
            List<TargetMutationGoal>? targetMutationGoals = null,
            List<int>? surgePriorityIds = null,
            int surgeAttemptTurnFrequency = GameBalance.DefaultSurgeAIAttemptTurnFrequency,
            EconomyBias economyBias = EconomyBias.Neutral,
            List<MycovariantPreference>? mycovariantPreferences = null,
            List<int>? preferredMycovariantIds = null)
        {
            StrategyName = strategyName;
            this.prioritizeHighTier = prioritizeHighTier;
            this.priorityMutationCategories = priorityMutationCategories;
            this.maxTier = maxTier;
            this.targetMutationGoals = targetMutationGoals ?? new List<TargetMutationGoal>();
            this.surgePriorityIds = surgePriorityIds ?? new();
            this.surgeAttemptTurnFrequency = surgeAttemptTurnFrequency;
            this.economyBias = economyBias;
            this.mycovariantPreferences = mycovariantPreferences ?? new();
            
            // Convert preferred mycovariant IDs to preferences if provided
            if (preferredMycovariantIds != null)
            {
                var convertedPreferences = new List<MycovariantPreference>();
                for (int i = 0; i < preferredMycovariantIds.Count; i++)
                {
                    // Priority decreases with position in list (first = highest priority)
                    int priority = 1000 - i; // Start at 1000 to ensure these are higher than existing preferences
                    convertedPreferences.Add(new MycovariantPreference(
                        preferredMycovariantIds[i], 
                        priority, 
                        $"Preferred #{i + 1}"));
                }
                
                // Merge with existing preferences, keeping the higher priorities
                this.mycovariantPreferences = this.mycovariantPreferences
                    .Concat(convertedPreferences)
                    .OrderByDescending(p => p.Priority)
                    .ToList();
            }
        }

        // ==== NEW: Game Phase Detection ====
        private GamePhase GetCurrentPhase(int currentRound)
        {
            if (currentRound <= 10) return GamePhase.EarlyGame;
            if (currentRound <= 25) return GamePhase.MidGame;
            return GamePhase.LateGame;
        }

        // ==== NEW: Smart Early-Game Economy Mutation Prioritization ====
        private bool ShouldPrioritizeEconomyMutation(Mutation mutation, GamePhase phase)
        {
            if (phase != GamePhase.EarlyGame) return false;
            
            // Prioritize these mutations in early game for maximum value
            return mutation.Id == MutationIds.MutatorPhenotype ||
                   mutation.Id == MutationIds.AdaptiveExpression ||
                   mutation.Id == MutationIds.HyperadaptiveDrift;
        }

        // ==== NEW: Get Economy Mutations for Early Game ====
        private List<Mutation> GetEarlyGameEconomyMutations(List<Mutation> allMutations, Player player, int currentRound)
        {
            return allMutations
                .Where(m => ShouldPrioritizeEconomyMutation(m, GetCurrentPhase(currentRound)) &&
                           player.CanUpgrade(m, currentRound) &&
                           (int)m.Tier <= (int)maxTier)
                .ToList();
        }

        /// <summary>
        /// For a given goal (mutationId, targetLevel), returns the full prerequisite chain (ordered, no duplicates),
        /// with correct required levels for all prerequisites.
        /// </summary>
        private List<(Mutation mutation, int requiredLevel)> BuildPrerequisiteChainWithLevels(TargetMutationGoal goal)
        {
            var chain = new List<(Mutation mutation, int requiredLevel)>();
            var visited = new HashSet<int>();

            void Visit(int mutationId, int requiredLevel)
            {
                if (!MutationRepository.All.TryGetValue(mutationId, out var mutation))
                    return;

                // Required level can't be higher than actual max level
                int cappedLevel = Math.Min(requiredLevel, mutation.MaxLevel);

                // Only keep highest required level if seen more than once
                var existing = chain.FirstOrDefault(x => x.mutation.Id == mutationId);
                if (!Equals(existing, default((Mutation mutation, int requiredLevel))))
                {
                    if (cappedLevel > existing.requiredLevel)
                    {
                        // Replace with higher level requirement
                        chain.Remove(existing);
                        chain.Add((mutation, cappedLevel));
                    }
                    // Else skip, already added at equal or higher level
                    return;
                }

                // Recurse on prerequisites
                foreach (var prereq in mutation.Prerequisites)
                    Visit(prereq.MutationId, prereq.RequiredLevel);

                chain.Add((mutation, cappedLevel));
            }

            int targetLevel = goal.TargetLevel ?? (MutationRepository.All.TryGetValue(goal.MutationId, out var mut) ? mut.MaxLevel : 1);
            Visit(goal.MutationId, targetLevel);

            // Return in prerequisite-first order (so build prereqs before main mutation)
            return chain
                .Distinct()
                .OrderBy(x => x.mutation.Tier)
                .ThenBy(x => x.mutation.Id)
                .ToList();
        }

        // ==== NEW: Mycovariant Preference Logic ====
        /// <summary>
        /// Returns a list of preferred mycovariants for this strategy, ordered by priority
        /// </summary>
        public List<MycovariantPreference> GetMycovariantPreferences()
        {
            return mycovariantPreferences
                .OrderByDescending(p => p.Priority)
                .ToList();
        }

        /// <summary>
        /// Gets the highest priority mycovariant preference that the player doesn't already have
        /// </summary>
        public MycovariantPreference? GetPreferredMycovariant(Player player)
        {
            var availableMycovariants = MycovariantRepository.All.ToList();
            foreach (var preference in mycovariantPreferences.OrderByDescending(p => p.Priority))
            {
                // Check if player already has any of these mycovariants
                if (player.PlayerMycovariants.Any(pm => preference.MycovariantIds.Contains(pm.Mycovariant.Id)))
                    continue;
                // Check if any of these mycovariants are available in the draft
                if (availableMycovariants.Any(m => preference.MycovariantIds.Contains(m.Id)))
                    return preference;
            }
            return null;
        }

        /// <summary>
        /// Selects the best mycovariant from the given choices, preferring mycovariants in the preferred list first.
        /// If no preferred mycovariants are available, falls back to normal AI scoring.
        /// </summary>
        public Mycovariant SelectMycovariantFromChoices(Player player, List<Mycovariant> choices, GameBoard board)
        {
            // First, check if any preferred mycovariants are in the choices
            foreach (var preference in mycovariantPreferences.OrderByDescending(p => p.Priority))
            {
                // Skip if player already has any of these mycovariants
                if (player.PlayerMycovariants.Any(pm => preference.MycovariantIds.Contains(pm.MycovariantId)))
                    continue;
                    
                // Look for any preferred mycovariant in the current choices
                var preferredChoice = choices.FirstOrDefault(c => preference.MycovariantIds.Contains(c.Id));
                if (preferredChoice != null)
                {
                    return preferredChoice;
                }
            }
            
            // No preferred mycovariants available, fall back to AI scoring
            var scoredChoices = choices
                .Select(m => new { Mycovariant = m, Score = m.GetBaseAIScore(player, board) })
                .OrderByDescending(x => x.Score)
                .ToList();
                
            // Return the highest scoring mycovariant, or first if all have the same score
            return scoredChoices.First().Mycovariant;
        }

        protected override void PerformSpendingLogic(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            Random rnd,
            ISimulationObserver simulationObserver)
        {
            var currentPhase = GetCurrentPhase(board.CurrentRound);
            
            // ==== NEW: Early Game Economy Priority ====
            if (currentPhase == GamePhase.EarlyGame)
            {
                var economyMutations = GetEarlyGameEconomyMutations(allMutations, player, board.CurrentRound);
                if (economyMutations.Count > 0)
                {
                    // Prioritize economy mutations in early game
                    foreach (var economyMutation in economyMutations)
                    {
                        if (player.MutationPoints > 0 && player.CanUpgrade(economyMutation, board.CurrentRound))
                        {
                            if (player.TryUpgradeMutation(economyMutation, simulationObserver, board.CurrentRound))
                            {
                                // Successfully upgraded an economy mutation, continue with normal logic
                                break;
                            }
                        }
                    }
                }
            }

            // Sequentially work through target goals (only one goal at a time)
            foreach (var goal in targetMutationGoals)
            {
                if (!MutationRepository.All.TryGetValue(goal.MutationId, out var targetMutation))
                    continue;

                int goalTargetLevel = goal.TargetLevel ?? targetMutation.MaxLevel;
                // Do not try to exceed mutation's max level
                goalTargetLevel = Math.Min(goalTargetLevel, targetMutation.MaxLevel);
                if (goalTargetLevel < 1)
                    continue;

                int currentLevel = player.GetMutationLevel(targetMutation.Id);
                if (currentLevel >= goalTargetLevel)
                    continue; // goal already satisfied

                // Build and attempt to upgrade prereqs first, then the goal
                var prereqChain = BuildPrerequisiteChainWithLevels(new TargetMutationGoal(goal.MutationId, goalTargetLevel));
                bool upgraded = false;
                bool shouldBankForNextMutation = false;

                // First, upgrade all prerequisites
                foreach (var (mutation, reqLevel) in prereqChain)
                {
                    // Skip the final target mutation - we'll handle it separately
                    if (mutation.Id == goal.MutationId)
                        continue;

                    int curLvl = player.GetMutationLevel(mutation.Id);
                    int needed = Math.Min(reqLevel, mutation.MaxLevel);

                    // Check if we need to bank for the NEXT mutation in the chain
                    if (curLvl < needed)
                    {
                        // Find the next mutation in the chain that we need to upgrade
                        var nextMutation = prereqChain.SkipWhile(x => x.mutation.Id != mutation.Id).Skip(1).FirstOrDefault();
                        if (nextMutation.mutation != null)
                        {
                            int nextCost = nextMutation.mutation.PointsPerUpgrade;
                            // Only bank if the next mutation is expensive AND we don't have enough points
                            // AND it's not the final target mutation (allow spending on final target)
                            bool isFinalTarget = nextMutation.mutation.Id == goal.MutationId;
                            if (nextCost > 5 && player.MutationPoints < nextCost && !isFinalTarget)
                            {
                                // The next mutation costs more than 5 points and we don't have enough
                                shouldBankForNextMutation = true;
                                break;
                            }
                        }
                        // If there's no next mutation, this is the final target, so don't bank
                    }

                    while (curLvl < needed && player.MutationPoints > 0 && player.CanUpgrade(mutation, board.CurrentRound))
                    {
                        if (player.TryUpgradeMutation(mutation, simulationObserver, board.CurrentRound))
                        {
                            curLvl++;
                            upgraded = true;
                        }
                        else
                        {
                            break; // couldn't upgrade further
                        }
                    }
                }

                // If we need to bank for the next mutation, don't proceed to fallback spending
                if (shouldBankForNextMutation)
                {
                    // Record that we're banking points for expensive mutations
                    simulationObserver.RecordBankedPoints(player.PlayerId, player.MutationPoints);
                    return; // Bank points for next turn
                }

                // Now try to upgrade the actual target mutation
                if (player.CanUpgrade(targetMutation, board.CurrentRound))
                {
                    while (player.GetMutationLevel(targetMutation.Id) < goalTargetLevel && 
                           player.MutationPoints > 0 && 
                           player.CanUpgrade(targetMutation, board.CurrentRound))
                    {
                        if (player.TryUpgradeMutation(targetMutation, simulationObserver, board.CurrentRound))
                        {
                            upgraded = true;
                        }
                        else
                        {
                            break; // couldn't upgrade further
                        }
                    }
                }

                // Only work on one target at a time, then fallback
                // If we upgraded anything (prerequisites or target), we're done with this goal
                // Don't break here - let the loop continue to try the target mutation
                // Only break if we actually upgraded the target mutation itself
                if (player.GetMutationLevel(targetMutation.Id) > currentLevel)
                {
                    break; // Successfully upgraded the target mutation, move to next goal
                }
                else if (upgraded)
                {
                    // We upgraded prerequisites but not the target yet, continue to next iteration
                    // to try to upgrade the target mutation
                    continue;
                }
            }

            // If spent points on a target, or all targets are complete, try surges then fallback
            // Try to activate surges on the Nth round before fallback spending
            if (TrySpendOnSurges(player, allMutations, board, simulationObserver, onlyOnNthRound: true))
                return; // If a surge is triggered, stop spending for this turn

            // Check if we should bank for surges
            if (ShouldBankForSurges(player, allMutations, board))
            {
                simulationObserver.RecordBankedPoints(player.PlayerId, player.MutationPoints);
                return; // Bank points for surge activation
            }

            // Check if we're in the middle of working on a target goal but haven't completed it yet
            bool hasIncompleteTarget = false;
            foreach (var goal in targetMutationGoals)
            {
                if (!MutationRepository.All.TryGetValue(goal.MutationId, out var targetMutation))
                    continue;

                int goalTargetLevel = goal.TargetLevel ?? targetMutation.MaxLevel;
                goalTargetLevel = Math.Min(goalTargetLevel, targetMutation.MaxLevel);
                
                int currentLevel = player.GetMutationLevel(targetMutation.Id);
                if (currentLevel < goalTargetLevel)
                {
                    // Check if prerequisites are met
                    var prereqChain = BuildPrerequisiteChainWithLevels(new TargetMutationGoal(goal.MutationId, goalTargetLevel));
                    bool prereqsMet = true;
                    foreach (var (mutation, reqLevel) in prereqChain)
                    {
                        if (mutation.Id == goal.MutationId) continue; // Skip the target itself
                        if (player.GetMutationLevel(mutation.Id) < reqLevel)
                        {
                            prereqsMet = false;
                            break;
                        }
                    }
                    
                    if (prereqsMet)
                    {
                        hasIncompleteTarget = true;
                        break;
                    }
                }
            }

            // Only do fallback spending if we don't have an incomplete target with met prerequisites
            if (!hasIncompleteTarget)
            {
                SpendFallbackPoints(player, allMutations, board, rnd, simulationObserver);
            }

            // After ALL other spending, always try to activate surges as last resort (any turn)
            TrySpendOnSurges(player, allMutations, board, simulationObserver, onlyOnNthRound: false);
        }

        private bool ShouldBankForSurges(Player player, List<Mutation> allMutations, GameBoard board)
        {
            // Only bank for surges if we have surge priority IDs and it's close to surge turn
            if (surgePriorityIds.Count == 0 || surgeAttemptTurnFrequency <= 0)
                return false;

            int currentRound = board.CurrentRound;
            int nextSurgeRound = ((currentRound / surgeAttemptTurnFrequency) + 1) * surgeAttemptTurnFrequency;
            int roundsUntilSurge = nextSurgeRound - currentRound;

            // Bank if surge is coming soon (within 2 rounds) and we have a surge mutation
            if (roundsUntilSurge <= 2)
            {
                foreach (var surgeId in surgePriorityIds)
                {
                    var surge = allMutations.FirstOrDefault(m => m.Id == surgeId && m.IsSurge);
                    if (surge != null && player.GetMutationLevel(surge.Id) > 0)
                    {
                        int currentLevel = player.GetMutationLevel(surge.Id);
                        int cost = surge.GetSurgeActivationCost(currentLevel);
                        
                        // Bank if we're close to affording the surge
                        if (player.MutationPoints + roundsUntilSurge * player.GetMutationPointIncome() >= cost &&
                            player.MutationPoints < cost)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool TrySpendOnSurges(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver simulationObserver,
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
                            player.TryUpgradeMutation(surge, simulationObserver, board.CurrentRound);
                            return true;
                        }
                    }
                }
            }
            else
            {
                // Always try to activate surges as last resort, regardless of other options
                foreach (var surge in availableSurges)
                {
                    if (!player.IsSurgeActive(surge.Id))
                    {
                        int currentLevel = player.GetMutationLevel(surge.Id);
                        int cost = surge.GetSurgeActivationCost(currentLevel);

                        if (player.MutationPoints >= cost)
                        {
                            player.TryUpgradeMutation(surge, simulationObserver, board.CurrentRound);
                            return true;
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
            ISimulationObserver simulationObserver)
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
                var anyUpgradable = allMutations.Where(m => player.CanUpgrade(m, board.CurrentRound)).ToList();
                if (anyUpgradable.Count == 0)
                    break;
                player.TryUpgradeMutation(anyUpgradable[0], simulationObserver, board.CurrentRound);
            }
        }

        private bool TrySpendByCategory(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            ISimulationObserver simulationObserver)
        {
            foreach (var category in GetShuffledCategories())
            {
                var candidates = allMutations
                    .Where(m => m.Category == category
                                && (int)m.Tier <= (int)maxTier
                                && player.CanUpgrade(m, board.CurrentRound))
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
            ISimulationObserver simulationObserver)
        {
            var fallbackCandidates = allMutations
                .Where(m => (int)m.Tier <= (int)maxTier && player.CanUpgrade(m, board.CurrentRound))
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
            ISimulationObserver simulationObserver)
        {
            var upgradable = allMutations
                .Where(m => player.CanUpgrade(m, board.CurrentRound) && (int)m.Tier <= (int)maxTier)
                .ToList();

            if (upgradable.Count == 0)
                return false;

            // Split drift/non-drift
            var nonDrift = upgradable.Where(m => m.Category != MutationCategory.GeneticDrift).ToList();
            var drift = upgradable.Where(m => m.Category == MutationCategory.GeneticDrift).ToList();

            if (economyBias == EconomyBias.IgnoreEconomy)
            {
                // Only upgrade drift if in any target goals/prereqs
                drift = drift.Where(m => targetMutationGoals.Any(g => g.MutationId == m.Id)).ToList();
                if (nonDrift.Count == 0 && drift.Count > 0)
                    return TryUpgradeWithTendrilAwareness(player, drift[0], upgradable, board, simulationObserver);
                if (nonDrift.Count > 0)
                    return TryUpgradeWithTendrilAwareness(player, nonDrift[0], upgradable, board, simulationObserver);
                return false;
            }

            // NEUTRAL LOGIC — treat all equally
            if (economyBias == EconomyBias.Neutral)
            {
                int idx = rnd.Next(upgradable.Count);
                var chosen = upgradable[idx];
                return TryUpgradeWithTendrilAwareness(player, chosen, upgradable, board, simulationObserver);
            }

            // Economy bias: filter or adjust drift mutations as needed
            float driftWeight = GetDriftWeight(board.CurrentRound);

            // Build a weighted pool for random selection
            var weightedPool = new List<Mutation>();
            int baseWeight = 100;

            foreach (var m in nonDrift)
            {
                weightedPool.Add(m);
            }
            foreach (var m in drift)
            {
                int weight = (int)(baseWeight * driftWeight);
                for (int i = 0; i < weight; i++)
                {
                    weightedPool.Add(m);
                }
            }
            if (weightedPool.Count == 0)
                return false;

            int idx2 = rnd.Next(weightedPool.Count);
            var chosen2 = weightedPool[idx2];

            return TryUpgradeWithTendrilAwareness(player, chosen2, upgradable, board, simulationObserver);
        }

        private float GetDriftWeight(int currentRound)
        {
            switch (economyBias)
            {
                case EconomyBias.MinorEconomy:
                    return Math.Max(0.05f, 0.4f - 0.022f * currentRound);
                case EconomyBias.ModerateEconomy:
                    return Math.Max(0.07f, 0.7f - 0.0315f * currentRound);
                case EconomyBias.MaxEconomy:
                    return 0.8f;
                default:
                    return 0.0f;
            }
        }

        private bool TrySpendWithinCategory(
            Player player,
            GameBoard board,
            List<Mutation> candidates,
            ISimulationObserver simulationObserver)
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

        private bool IsTendril(Mutation m)
        {
            return m.Id == MutationIds.TendrilNorthwest
                || m.Id == MutationIds.TendrilNortheast
                || m.Id == MutationIds.TendrilSouthwest
                || m.Id == MutationIds.TendrilSoutheast;
        }

        private bool TryUpgradeWithTendrilAwareness(
            Player player,
            Mutation candidate,
            List<Mutation> allCandidates,
            GameBoard board,
            ISimulationObserver simulationObserver)
        {
            if (IsTendril(candidate))
            {
                var bestTendril = PickBestTendrilMutation(player, allCandidates.Where(IsTendril).ToList(), board);
                if (bestTendril != null)
                    return player.TryUpgradeMutation(bestTendril, simulationObserver, board.CurrentRound);
                return false;
            }

            return player.TryUpgradeMutation(candidate, simulationObserver, board.CurrentRound);
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
