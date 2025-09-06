using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Handles all mutation effects related to the GeneticDrift category.
    /// </summary>
    public static class GeneticDriftMutationProcessor
    {
        /// <summary>
        /// Tries to apply Mutator Phenotype auto-upgrade effect, including Hyperadaptive Drift bonuses.
        /// </summary>
        public static void TryApplyMutatorPhenotype(
            Player player,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver observer
        )
        {
            float chance = player.GetMutationEffect(MutationType.AutoUpgradeRandom);
            if (chance <= 0f || rng.NextDouble() >= chance) return;

            // Check Hyperadaptive Drift levels and associated per-level effects
            int hyperadaptiveLevel = player.GetMutationLevel(MutationIds.HyperadaptiveDrift);
            bool hasHyperadaptive = hyperadaptiveLevel > 0;

            float higherTierChance = hasHyperadaptive
                ? GameBalance.HyperadaptiveDriftHigherTierChancePerLevel * hyperadaptiveLevel
                : 0f;

            float bonusTierOneChance = hasHyperadaptive
                ? GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel * hyperadaptiveLevel
                : 0f;

            // Gather upgradable mutations by tier - for auto-upgrades, we don't need mutation points
            var tier1 = allMutations.Where(m => m.Tier == MutationTier.Tier1 && CanAutoUpgrade(player, m)).ToList();
            var tier2 = allMutations.Where(m => m.Tier == MutationTier.Tier2 && CanAutoUpgrade(player, m)).ToList();
            var tier3 = allMutations.Where(m => m.Tier == MutationTier.Tier3 && CanAutoUpgrade(player, m)).ToList();
            var tier4 = allMutations.Where(m => m.Tier == MutationTier.Tier4 && CanAutoUpgrade(player, m)).ToList();

            List<Mutation> pool;
            MutationTier targetTier;

            // Hyperadaptive Drift: roll to see if we try for tier 2, 3, or 4 instead of 1
            if (hasHyperadaptive && rng.NextDouble() < higherTierChance)
            {
                // Try tier 2, 3, or 4 randomly, but only among those with upgradable mutations
                var availableHigherTiers = new List<(List<Mutation> mutations, MutationTier tier)>
        {
            (tier2, MutationTier.Tier2),
            (tier3, MutationTier.Tier3),
            (tier4, MutationTier.Tier4)
        }.Where(t => t.mutations.Count > 0).ToList();

                if (availableHigherTiers.Count > 0)
                {
                    var selected = availableHigherTiers[rng.Next(availableHigherTiers.Count)];
                    pool = selected.mutations;
                    targetTier = selected.tier;
                }
                else if (tier1.Count > 0)
                {
                    pool = tier1;
                    targetTier = MutationTier.Tier1;
                }
                else
                {
                    return;
                }
            }
            else if (tier1.Count > 0)
            {
                pool = tier1;
                targetTier = MutationTier.Tier1;
            }
            else
            {
                return;
            }

            // Pick a mutation to auto-upgrade
            var pick = pool[rng.Next(pool.Count)];
            int upgrades = 1;

            // Hyperadaptive Drift: Tier 1 can double-upgrade
            if (hasHyperadaptive && targetTier == MutationTier.Tier1 && rng.NextDouble() < bonusTierOneChance)
            {
                upgrades = 2;
            }

            // Track upgraded mutations for logging
            var upgradedMutations = new List<string>();

            // Actually perform the upgrades, attributing each point appropriately
            int mutatorPoints = 0;
            int hyperadaptivePoints = 0;

            for (int i = 0; i < upgrades; i++)
            {
                bool upgraded = player.TryAutoUpgrade(pick, currentRound);
                if (!upgraded) break;

                // Track the upgrade for logging
                upgradedMutations.Add(pick.Name);

                // Attribution logic:
                if (targetTier == MutationTier.Tier1)
                {
                    if (i == 0)
                    {
                        mutatorPoints += pick.PointsPerUpgrade;
                    }
                    else
                    {
                        hyperadaptivePoints += pick.PointsPerUpgrade;
                    }
                }
                else // Tier 2, 3, or 4
                {
                    hyperadaptivePoints += pick.PointsPerUpgrade;
                }
            }

            // Hyperadaptive Drift Max Level Effect: Automatically upgrade an additional Tier 1 mutation
            if (hasHyperadaptive && hyperadaptiveLevel >= GameBalance.HyperadaptiveDriftMaxLevel && tier1.Count > 0)
            {
                var additionalPick = tier1[rng.Next(tier1.Count)];
                bool additionalUpgraded = player.TryAutoUpgrade(additionalPick, currentRound);
                if (additionalUpgraded)
                {
                    hyperadaptivePoints += additionalPick.PointsPerUpgrade;
                    upgradedMutations.Add(additionalPick.Name);
                }
            }

            // Notify observer
            if (observer != null)
            {
                if (mutatorPoints > 0)
                    observer.RecordMutatorPhenotypeMutationPointsEarned(player.PlayerId, mutatorPoints);

                if (hyperadaptivePoints > 0)
                    observer.RecordHyperadaptiveDriftMutationPointsEarned(player.PlayerId, hyperadaptivePoints);

                // Report each upgraded mutation
                foreach (var mutationName in upgradedMutations)
                {
                    observer.RecordMutatorPhenotypeUpgrade(player.PlayerId, mutationName);
                }
            }
        }

        /// <summary>
        /// Applies Mycotoxin Catabolism effect during pre-growth phase.
        /// </summary>
        public static int ApplyMycotoxinCatabolism(
            Player player,
            GameBoard board,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver observer)
        {
            int level = player.GetMutationLevel(MutationIds.MycotoxinCatabolism);
            if (level <= 0) return 0;

            float cleanupChance = level * GameBalance.MycotoxinCatabolismCleanupChancePerLevel;
            int toxinsMetabolized = 0;
            var processedToxins = new HashSet<int>();

            int maxPointsPerRound = GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound;
            int pointsSoFar = roundContext.GetEffectCount(player.PlayerId, "CatabolizedMP");
            int bonusPointsEarned = 0; // newly added points this invocation

            foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!cell.IsAlive) continue;

                foreach (var neighborTile in board.GetOrthogonalNeighbors(cell.TileId))
                {
                    if (neighborTile.FungalCell is not { IsToxin: true }) continue;
                    if (!processedToxins.Add(neighborTile.TileId)) continue;

                    if (rng.NextDouble() < cleanupChance)
                    {
                        neighborTile.RemoveFungalCell();
                        toxinsMetabolized++;

                        if (pointsSoFar < GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound &&
                            rng.NextDouble() < GameBalance.MycotoxinCatabolismMutationPointChancePerLevel)
                        {
                            player.MutationPoints += 1;
                            roundContext.IncrementEffectCount(player.PlayerId, "CatabolizedMP");
                            pointsSoFar++;
                            bonusPointsEarned++;

                            if (pointsSoFar >= maxPointsPerRound)
                                break;
                        }
                    }
                }
                if (pointsSoFar >= maxPointsPerRound)
                    break;
            }

            if (toxinsMetabolized > 0)
            {
                observer.RecordToxinCatabolism(player.PlayerId, toxinsMetabolized, pointsSoFar);
            }
            if (bonusPointsEarned > 0)
            {
                observer.RecordMutationPointIncome(player.PlayerId, bonusPointsEarned);
            }

            return toxinsMetabolized;
        }

        /// <summary>
        /// Returns the damping factor for Necrophytic Bloom based on occupied percent.
        /// </summary>
        public static float GetNecrophyticBloomDamping(float occupiedPercent)
        {
            if (occupiedPercent <= 0.20f) return 1f;
            float raw = 1f - ((occupiedPercent - 0.20f) / 0.80f);
            return Math.Clamp(raw, 0f, 1f);
        }

        /// <summary>
        /// Handles the initial spore burst from Necrophytic Bloom when it first activates.
        /// </summary>
        public static void TriggerNecrophyticBloomInitialBurst(
            Player player,
            GameBoard board,
            Random rng,
            ISimulationObserver observer)
        {
            int level = player.GetMutationLevel(MutationIds.NecrophyticBloom);
            if (level <= 0) return;

            var deadCells = board.GetAllCellsOwnedBy(player.PlayerId)
                                 .Where(cell => cell.IsDead && !cell.IsToxin)
                                 .ToList();

            float sporesPerDeadCell = level * GameBalance.NecrophyticBloomSporesPerDeathPerLevel;
            int totalSpores = (int)Math.Floor(sporesPerDeadCell * deadCells.Count);

            if (totalSpores <= 0) return;

            var allTiles = board.AllTiles().ToList();
            int reclaims = 0;

            for (int i = 0; i < totalSpores; i++)
            {
                var targetTile = allTiles[rng.Next(allTiles.Count)];
                if (board.TryReclaimDeadCell(player.PlayerId, targetTile.TileId, Growth.GrowthSource.NecrophyticBloom))
                {
                    reclaims++;
                }
            }

            if (reclaims > 0)
            {
                observer.ReportNecrophyticBloomSporeDrop(player.PlayerId, totalSpores, reclaims);
            }
        }

        /// <summary>
        /// Triggers Necrophytic Bloom on individual cell death.
        /// Enhanced with Competitive Antagonism surge targeting.
        /// </summary>
        public static void TriggerNecrophyticBloomOnCellDeath(
           Player owner,
           GameBoard board,
           List<Player> allPlayers,
           Random rng,
           float occupiedPercent,
           ISimulationObserver observer,
           DecayPhaseContext decayPhaseContext)
        {
            int level = owner.GetMutationLevel(MutationIds.NecrophyticBloom);
            if (level <= 0) return;

            float damping = GetNecrophyticBloomDamping(occupiedPercent);
            int spores = (int)Math.Floor(
                level * GameBalance.NecrophyticBloomSporesPerDeathPerLevel * damping);

            if (spores <= 0) return;

            // Check if Competitive Antagonism surge is active for enhanced targeting
            bool hasCompetitiveAntagonism = owner.IsSurgeActive(MutationIds.CompetitiveAntagonism);
            List<int> targetTileIds;

            if (hasCompetitiveAntagonism)
            {
                targetTileIds = GetCompetitiveAntagonismNecrophyticBloomTargets(board, owner, allPlayers, rng, decayPhaseContext);
                
                // Record the competitive targeting effect
                int targetsAffected = Math.Min(spores, targetTileIds.Count);
                if (targetsAffected > 0)
                {
                    observer.RecordCompetitiveAntagonismTargeting(owner.PlayerId, targetsAffected);
                }
            }
            else
            {
                // Use normal targeting logic - all tiles on the board
                targetTileIds = board.AllTiles().Select(t => t.TileId).ToList();
            }

            int reclaims = 0;
            for (int i = 0; i < spores; i++)
            {
                int randomTileId = targetTileIds[rng.Next(targetTileIds.Count)];
                bool success = board.TryReclaimDeadCell(owner.PlayerId, randomTileId, Growth.GrowthSource.NecrophyticBloom);
                if (success) reclaims++;
            }

            observer.ReportNecrophyticBloomSporeDrop(owner.PlayerId, spores, reclaims);
        }

        /// <summary>
        /// Gets prioritized target tile IDs for Necrophytic Bloom when Competitive Antagonism surge is active.
        /// Prioritizes dead cells from larger colony players, removes 75% of smaller colony dead cells.
        /// </summary>
        private static List<int> GetCompetitiveAntagonismNecrophyticBloomTargets(
            GameBoard board, 
            Player currentPlayer, 
            List<Player> allPlayers, 
            Random rng,
            DecayPhaseContext decayPhaseContext)
        {
            // Use DecayPhaseContext for optimized colony size categorization
            var (largerColonies, smallerColonies) = decayPhaseContext.GetColonySizeCategorization(currentPlayer);
            var smallerColonyPlayerIds = smallerColonies.Select(p => p.PlayerId).ToHashSet();

            // Separate tiles by category
            var emptyTiles = new List<int>();
            var largerColonyDeadCells = new List<int>();
            var smallerColonyDeadCells = new List<int>();
            var otherTiles = new List<int>(); // Living cells, toxins, current player's cells

            foreach (var tile in board.AllTiles())
            {
                var cell = tile.FungalCell;
                
                if (cell == null)
                {
                    // Empty tile
                    emptyTiles.Add(tile.TileId);
                }
                else if (cell.IsDead && cell.OwnerPlayerId.HasValue)
                {
                    // Dead cell owned by someone
                    if (cell.OwnerPlayerId == currentPlayer.PlayerId)
                    {
                        // Current player's dead cell - treat as "other" since they can't reclaim their own dead cells
                        otherTiles.Add(tile.TileId);
                    }
                    else if (largerColonies.Any(p => p.PlayerId == cell.OwnerPlayerId.Value))
                    {
                        // Dead cell from larger colony player - high priority
                        largerColonyDeadCells.Add(tile.TileId);
                    }
                    else if (smallerColonyPlayerIds.Contains(cell.OwnerPlayerId.Value))
                    {
                        // Dead cell from smaller colony player - subject to reduction
                        smallerColonyDeadCells.Add(tile.TileId);
                    }
                    else
                    {
                        // Dead cell from player with equal colony size or other edge case
                        otherTiles.Add(tile.TileId);
                    }
                }
                else
                {
                    // Living cell, toxin, or dead cell without owner
                    otherTiles.Add(tile.TileId);
                }
            }

            // Remove 75% of smaller colony dead cells
            int smallerColonyTilesToRemove = (int)Math.Floor(smallerColonyDeadCells.Count * GameBalance.CompetitiveAntagonismNecrophyticBloomSmallerColonyReduction);
            for (int i = 0; i < smallerColonyTilesToRemove && smallerColonyDeadCells.Count > 0; i++)
            {
                int removeIndex = rng.Next(smallerColonyDeadCells.Count);
                smallerColonyDeadCells.RemoveAt(removeIndex);
            }

            // Combine tiles in priority order: larger colony dead cells first, then smaller colony dead cells, then empty, then other
            var prioritizedTiles = new List<int>();
            prioritizedTiles.AddRange(largerColonyDeadCells);
            prioritizedTiles.AddRange(smallerColonyDeadCells);
            prioritizedTiles.AddRange(emptyTiles);
            prioritizedTiles.AddRange(otherTiles);

            return prioritizedTiles;
        }

        /// <summary>
        /// Checks if a mutation can be auto-upgraded (without requiring mutation points).
        /// </summary>
        private static bool CanAutoUpgrade(Player player, Mutation mutation)
        {
            if (mutation == null) return false;

            // Surge mutations cannot be auto-upgraded
            if (mutation.IsSurge)
                return false;

            // MycelialSurges category cannot be auto-upgraded
            if (mutation.Category == MutationCategory.MycelialSurges)
                return false;

            // Check prerequisites
            foreach (var pre in mutation.Prerequisites)
                if (player.GetMutationLevel(pre.MutationId) < pre.RequiredLevel)
                    return false;

            // Check if not at max level
            int currentLevel = player.GetMutationLevel(mutation.Id);
            return currentLevel < mutation.MaxLevel;
        }

        // Phase event handlers
        public static void OnMutationPhaseStart_MutatorPhenotype(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                TryApplyMutatorPhenotype(player, allMutations, rng, currentRound, observer);
            }
        }

        public static void OnPreGrowthPhase_MycotoxinCatabolism(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                ApplyMycotoxinCatabolism(player, board, rng, roundContext, observer);
            }
        }

        public static void OnNecrophyticBloomActivated(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            foreach (var p in players)
            {
                if (p.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                {
                    TriggerNecrophyticBloomInitialBurst(p, board, rng, observer);
                }
            }
        }

        public static void OnMutationPhaseStart_OntogenicRegression(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                TryApplyOntogenicRegression(player, allMutations, rng, board.CurrentRound, observer);
            }
        }

        /// <summary>
        /// Tries to apply Ontogenic Regression - devolves tier 1 mutations into tier 5/6 mutations.
        /// </summary>
        public static void TryApplyOntogenicRegression(
            Player player,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver observer)
        {
            int regressionLevel = player.GetMutationLevel(MutationIds.OntogenicRegression);
            if (regressionLevel <= 0) return;

            float baseChance = GameBalance.OntogenicRegressionChancePerLevel * regressionLevel;
            int maxAttempts = regressionLevel >= GameBalance.OntogenicRegressionMaxLevel ? 2 : 1;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (rng.NextDouble() >= baseChance)
                {
                    // Failed to trigger - award consolation points (record as income)
                    int failureBonus = GameBalance.OntogenicRegressionFailureConsolationPoints;
                    player.MutationPoints += failureBonus;
                    observer.RecordOntogenicRegressionFailureBonus(player.PlayerId, failureBonus);
                    observer.RecordMutationPointIncome(player.PlayerId, failureBonus);
                    continue;
                }

                // Find tier 1 mutations that can be devolved (have enough levels)
                var tier1Mutations = allMutations
                    .Where(m => m.Tier == MutationTier.Tier1)
                    .Where(m => player.GetMutationLevel(m.Id) >= GameBalance.OntogenicRegressionTier1LevelsToConsume)
                    .ToList();

                if (!tier1Mutations.Any())
                {
                    int failureBonus = GameBalance.OntogenicRegressionFailureConsolationPoints;
                    player.MutationPoints += failureBonus;
                    observer.RecordOntogenicRegressionFailureBonus(player.PlayerId, failureBonus);
                    observer.RecordMutationPointIncome(player.PlayerId, failureBonus);
                    continue;
                }

                // Find tier 5 and 6 mutations that can be gained (ignoring prerequisites)
                var targetMutations = allMutations
                    .Where(m => m.Tier == MutationTier.Tier5 || m.Tier == MutationTier.Tier6)
                    .Where(m => player.GetMutationLevel(m.Id) < m.MaxLevel)
                    .ToList();

                if (!targetMutations.Any())
                {
                    int failureBonus = GameBalance.OntogenicRegressionFailureConsolationPoints;
                    player.MutationPoints += failureBonus;
                    observer.RecordOntogenicRegressionFailureBonus(player.PlayerId, failureBonus);
                    observer.RecordMutationPointIncome(player.PlayerId, failureBonus);
                    continue;
                }

                // Select random source and target mutations
                var sourceMutation = tier1Mutations[rng.Next(tier1Mutations.Count)];
                var targetMutation = targetMutations[rng.Next(targetMutations.Count)];

                // Perform the devolution
                int sourceLevelsToRemove = GameBalance.OntogenicRegressionTier1LevelsToConsume;
                int currentSourceLevel = player.GetMutationLevel(sourceMutation.Id);
                int newSourceLevel = Math.Max(0, currentSourceLevel - sourceLevelsToRemove);

                // Remove levels from source mutation
                player.SetMutationLevel(sourceMutation.Id, newSourceLevel, currentRound);

                // Add 1 level to target mutation (ignoring prerequisites) - pass currentRound to properly track PrereqMetRound
                int currentTargetLevel = player.GetMutationLevel(targetMutation.Id);
                player.SetMutationLevel(targetMutation.Id, currentTargetLevel + 1, currentRound);

                // Record the effect for tracking
                observer.RecordOntogenicRegressionEffect(player.PlayerId, sourceMutation.Name, GameBalance.OntogenicRegressionTier1LevelsToConsume, targetMutation.Name, 1);
            }
        }

        public static void TryApplyAdaptiveExpression(
            Player player,
            Random rng,
            ISimulationObserver observer)
        {
            int level = player.GetMutationLevel(MutationIds.AdaptiveExpression);
            if (level <= 0) return;

            float chance = level * GameBalance.AdaptiveExpressionEffectPerLevel;
            if (rng.NextDouble() >= chance) return;

            // First bonus point
            int bonusPoints = 1;
            player.MutationPoints += 1;

            // Check for second bonus point
            float secondChance = level * GameBalance.AdaptiveExpressionSecondPointChancePerLevel;
            if (rng.NextDouble() < secondChance)
            {
                bonusPoints++;
                player.MutationPoints += 1;
            }

            // Record detailed bonus source and aggregate income
            observer.RecordAdaptiveExpressionBonus(player.PlayerId, bonusPoints);
            observer.RecordMutationPointIncome(player.PlayerId, bonusPoints);
        }

        public static void TryApplyAnabolicInversion(
            Player player,
            List<Player> allPlayers,
            GameBoard board,
            Random rng,
            ISimulationObserver observer)
        {
            int level = player.GetMutationLevel(MutationIds.AnabolicInversion);
            if (level <= 0) return;

            // Use the existing RollAnabolicInversionBonus logic
            int bonusPoints = player.RollAnabolicInversionBonus(allPlayers, rng, board);
            if (bonusPoints > 0)
            {
                player.MutationPoints += bonusPoints;
                observer.RecordAnabolicInversionBonus(player.PlayerId, bonusPoints);
                observer.RecordMutationPointIncome(player.PlayerId, bonusPoints);
            }
        }

        // Add new phase event handlers:
        public static void OnMutationPhaseStart_AdaptiveExpression(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                TryApplyAdaptiveExpression(player, rng, observer);
            }
        }

        public static void OnMutationPhaseStart_AnabolicInversion(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                TryApplyAnabolicInversion(player, players, board, rng, observer);
            }
        }
    }
}