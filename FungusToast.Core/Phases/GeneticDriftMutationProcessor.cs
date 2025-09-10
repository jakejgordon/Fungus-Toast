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
            float triggerChance = player.GetMutationEffect(MutationType.AutoUpgradeRandom);
            if (triggerChance <= 0f || rng.NextDouble() >= triggerChance) return;

            int hyperadaptiveLevel = player.GetMutationLevel(MutationIds.HyperadaptiveDrift);
            bool hasHyperadaptive = hyperadaptiveLevel > 0;

            float higherTierChance = hasHyperadaptive
                ? GameBalance.HyperadaptiveDriftHigherTierChancePerLevel * hyperadaptiveLevel
                : 0f;

            float bonusTierOneChance = hasHyperadaptive
                ? GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel * hyperadaptiveLevel
                : 0f;

            // Build mutation pools (tiers 1-4 only relevant)
            var pools = BuildUpgradablePools(player, allMutations);
            if (pools.Tier1.Count == 0 && !pools.HasAnyHigherTier) return; // nothing upgradable

            // Select the target tier + pool
            if (!TrySelectTargetTier(hasHyperadaptive, higherTierChance, rng, pools, out var targetTier, out var candidatePool))
                return;

            // Pick specific mutation from chosen pool
            var pick = candidatePool[rng.Next(candidatePool.Count)];

            // Determine number of upgrade attempts (1 or 2 for tier1 double proc)
            int upgradeAttempts = DetermineUpgradeCount(hasHyperadaptive, targetTier, bonusTierOneChance, rng);

            // Execute core upgrades
            var (mutatorPoints, hyperadaptivePoints, upgradedNames) = ExecuteUpgrades(player, pick, upgradeAttempts, targetTier, currentRound);

            // Max-level Hyperadaptive bonus (extra tier1 attempt using original tier1 snapshot)
            if (ShouldApplyMaxLevelBonus(hasHyperadaptive, hyperadaptiveLevel) && pools.Tier1.Count > 0)
            {
                ApplyMaxLevelBonus(player, rng, currentRound, pools.Tier1, ref hyperadaptivePoints, upgradedNames);
            }

            // Observer notifications
            NotifyObserver(observer, player.PlayerId, mutatorPoints, hyperadaptivePoints, upgradedNames);
        }

        #region TryApplyMutatorPhenotype helpers
        private sealed class UpgradablePools
        {
            public List<Mutation> Tier1 { get; } = new();
            public List<Mutation> Tier2 { get; } = new();
            public List<Mutation> Tier3 { get; } = new();
            public List<Mutation> Tier4 { get; } = new();
            public bool HasAnyHigherTier => Tier2.Count > 0 || Tier3.Count > 0 || Tier4.Count > 0;
            public IEnumerable<(List<Mutation> list, MutationTier tier)> EnumerateHigherTiers()
            {
                if (Tier2.Count > 0) yield return (Tier2, MutationTier.Tier2);
                if (Tier3.Count > 0) yield return (Tier3, MutationTier.Tier3);
                if (Tier4.Count > 0) yield return (Tier4, MutationTier.Tier4);
            }
        }

        private static UpgradablePools BuildUpgradablePools(Player player, IEnumerable<Mutation> mutations)
        {
            var pools = new UpgradablePools();
            foreach (var m in mutations)
            {
                if (!CanAutoUpgrade(player, m)) continue;
                switch (m.Tier)
                {
                    case MutationTier.Tier1: pools.Tier1.Add(m); break;
                    case MutationTier.Tier2: pools.Tier2.Add(m); break;
                    case MutationTier.Tier3: pools.Tier3.Add(m); break;
                    case MutationTier.Tier4: pools.Tier4.Add(m); break;
                }
            }
            return pools;
        }

        private static bool TrySelectTargetTier(
            bool hasHyperadaptive,
            float higherTierChance,
            Random rng,
            UpgradablePools pools,
            out MutationTier targetTier,
            out List<Mutation> pool)
        {
            if (hasHyperadaptive && rng.NextDouble() < higherTierChance)
            {
                var higher = pools.EnumerateHigherTiers().ToList();
                if (higher.Count > 0)
                {
                    var chosen = higher[rng.Next(higher.Count)];
                    targetTier = chosen.tier;
                    pool = chosen.list;
                    return true;
                }
                if (pools.Tier1.Count > 0)
                {
                    targetTier = MutationTier.Tier1;
                    pool = pools.Tier1;
                    return true;
                }
                targetTier = default;
                pool = null!;
                return false;
            }
            if (pools.Tier1.Count > 0)
            {
                targetTier = MutationTier.Tier1;
                pool = pools.Tier1;
                return true;
            }
            targetTier = default;
            pool = null!;
            return false;
        }

        private static int DetermineUpgradeCount(bool hasHyperadaptive, MutationTier targetTier, float bonusTierOneChance, Random rng)
            => (hasHyperadaptive && targetTier == MutationTier.Tier1 && rng.NextDouble() < bonusTierOneChance)
                ? Math.Max(2, GameBalance.HyperadaptiveDriftBonusTierOneMutationFreeUpgradeTimes) // ensure at least the old behavior of 2
                : 1;

        private static (int mutatorPoints, int hyperadaptivePoints, List<string> upgradedNames) ExecuteUpgrades(
            Player player,
            Mutation pick,
            int attempts,
            MutationTier targetTier,
            int currentRound)
        {
            int mutatorPoints = 0;
            int hyperadaptivePoints = 0;
            var upgradedNames = new List<string>();

            for (int i = 0; i < attempts; i++)
            {
                if (!player.TryAutoUpgrade(pick, currentRound)) break;
                upgradedNames.Add(pick.Name);
                if (targetTier == MutationTier.Tier1)
                {
                    if (i == 0)
                        mutatorPoints += pick.PointsPerUpgrade;
                    else
                        hyperadaptivePoints += pick.PointsPerUpgrade;
                }
                else
                {
                    hyperadaptivePoints += pick.PointsPerUpgrade;
                }
            }
            return (mutatorPoints, hyperadaptivePoints, upgradedNames);
        }

        private static bool ShouldApplyMaxLevelBonus(bool hasHyperadaptive, int level)
            => hasHyperadaptive && level >= GameBalance.HyperadaptiveDriftMaxLevel;

        private static void ApplyMaxLevelBonus(
            Player player,
            Random rng,
            int currentRound,
            List<Mutation> tier1Pool,
            ref int hyperadaptivePoints,
            List<string> upgradedNames)
        {
            var addPick = tier1Pool[rng.Next(tier1Pool.Count)];
            if (player.TryAutoUpgrade(addPick, currentRound))
            {
                hyperadaptivePoints += addPick.PointsPerUpgrade;
                upgradedNames.Add(addPick.Name);
            }
        }

        private static void NotifyObserver(
            ISimulationObserver observer,
            int playerId,
            int mutatorPoints,
            int hyperadaptivePoints,
            List<string> upgradedNames)
        {
            if (observer == null) return;
            if (mutatorPoints > 0)
                observer.RecordMutatorPhenotypeMutationPointsEarned(playerId, mutatorPoints);
            if (hyperadaptivePoints > 0)
                observer.RecordHyperadaptiveDriftMutationPointsEarned(playerId, hyperadaptivePoints);
            foreach (var name in upgradedNames)
                observer.RecordMutatorPhenotypeUpgrade(playerId, name);
        }
        #endregion

        /// <summary>
        /// Applies Mycotoxin Catabolism effect during pre-growth phase.
        /// </summary>
        public static int ApplyMycotoxinCatabolism(
            Player player,
            GameBoard board,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver observer,
            IReadOnlyDictionary<int,int>? livingCellCounts = null)
        {
            int level = player.GetMutationLevel(MutationIds.MycotoxinCatabolism);
            if (level <= 0) return 0;

            float baseCleanupChance = level * GameBalance.MycotoxinCatabolismCleanupChancePerLevel;
            int toxinsMetabolized = 0;
            var processedToxins = new HashSet<int>();

            int maxPointsPerRound = GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound;
            int pointsSoFar = roundContext.GetEffectCount(player.PlayerId, "CatabolizedMP");
            int bonusPointsEarned = 0; // newly added points this invocation

            // Determine Anabolic Inversion boost eligibility once
            bool hasAnabolicInversionMax = player.GetMutationLevel(MutationIds.AnabolicInversion) >= GameBalance.AnabolicInversionMaxLevel;
            int myLiving = 0;
            if (hasAnabolicInversionMax && livingCellCounts != null)
                livingCellCounts.TryGetValue(player.PlayerId, out myLiving);

            foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!cell.IsAlive) continue;

                foreach (var neighborTile in board.GetOrthogonalNeighbors(cell.TileId))
                {
                    if (neighborTile.FungalCell is not { IsToxin: true } toxinCell) continue;
                    if (!processedToxins.Add(neighborTile.TileId)) continue;

                    float cleanupChance = baseCleanupChance;

                    // Apply boosted cleanup chance only if: player has max Anabolic Inversion, toxin has an owner with more living cells
                    if (hasAnabolicInversionMax && livingCellCounts != null && toxinCell.OwnerPlayerId.HasValue)
                    {
                        livingCellCounts.TryGetValue(toxinCell.OwnerPlayerId.Value, out int ownerLiving);
                        if (ownerLiving > myLiving && myLiving > 0) // ensure meaningful comparison
                        {
                            cleanupChance = Math.Min(1f, cleanupChance * GameBalance.AnabolicInversionCatabolismCleanupMultiplier);
                        }
                    }

                    if (rng.NextDouble() < cleanupChance)
                    {
                        board.RemoveCellInternal(neighborTile.TileId, removeControl: true);
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
        /// Now linearly decreases from 1.0 at 20% occupancy to 0.15 at 100% (previously went to 0).
        /// </summary>
        public static float GetNecrophyticBloomDamping(float occupiedPercent)
        {
            if (occupiedPercent <= 0.20f) return 1f;
            float t = (occupiedPercent - 0.20f) / 0.80f; // 0 at 20%, 1 at 100%
            float value = 1f - 0.85f * t; // 1 -> 0.15 across the interval
            return Math.Clamp(value, 0.15f, 1f);
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
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                TryApplyMutatorPhenotype(player, allMutations, rng, board.CurrentRound, observer);
            }
        }

        public static void OnPreGrowthPhase_MycotoxinCatabolism(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver observer)
        {
            // Precompute living cell counts once for Anabolic Inversion max-level boost logic
            var livingCounts = new Dictionary<int,int>(players.Count);
            foreach (var p in players)
            {
                int count = board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive);
                livingCounts[p.PlayerId] = count;
            }

            foreach (var player in players)
            {
                ApplyMycotoxinCatabolism(player, board, rng, roundContext, observer, livingCounts);
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
                TryApplyOntogenicRegression(player, allMutations, rng, board, observer);
            }
        }

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
            var livingCounts = new Dictionary<int,int>(players.Count);
            foreach (var p in players)
            {
                int count = board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive);
                livingCounts[p.PlayerId] = count;
            }
            foreach (var player in players)
            {
                TryApplyAnabolicInversion(player, players, board, rng, observer, livingCounts);
            }
        }

        // Updated signature to include board so sacrifice mechanic can function
        public static void TryApplyOntogenicRegression(
            Player player,
            List<Mutation> allMutations,
            Random rng,
            GameBoard board,
            ISimulationObserver observer)
        {
            int regressionLevel = player.GetMutationLevel(MutationIds.OntogenicRegression);
            if (regressionLevel <= 0) return;

            float baseChance = GameBalance.OntogenicRegressionChancePerLevel * regressionLevel;
            int maxAttempts = regressionLevel >= GameBalance.OntogenicRegressionMaxLevel ? 2 : 1;
            bool atMaxLevel = regressionLevel >= GameBalance.OntogenicRegressionMaxLevel;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (rng.NextDouble() >= baseChance)
                {
                    int failureBonus = GameBalance.OntogenicRegressionFailureConsolationPoints;
                    player.MutationPoints += failureBonus;
                    observer.RecordOntogenicRegressionFailureBonus(player.PlayerId, failureBonus);
                    observer.RecordMutationPointIncome(player.PlayerId, failureBonus);
                    continue;
                }

                List<FungalCell> sacrificed = new();
                int effectiveLevelsToConsume = GameBalance.OntogenicRegressionTier1LevelsToConsume;
                int reductionsAchieved = 0;
                if (atMaxLevel && board != null)
                {
                    sacrificed = CollectOntogenicRegressionSacrifices(player, board, rng, effectiveLevelsToConsume, out reductionsAchieved);
                    if (reductionsAchieved > 0)
                        effectiveLevelsToConsume = Math.Max(1, effectiveLevelsToConsume - reductionsAchieved);
                }

                var tier1Mutations = allMutations
                    .Where(m => m.Tier == MutationTier.Tier1)
                    .Where(m => player.GetMutationLevel(m.Id) >= effectiveLevelsToConsume)
                    .ToList();

                if (!tier1Mutations.Any())
                {
                    int failureBonus = GameBalance.OntogenicRegressionFailureConsolationPoints;
                    player.MutationPoints += failureBonus;
                    observer.RecordOntogenicRegressionFailureBonus(player.PlayerId, failureBonus);
                    observer.RecordMutationPointIncome(player.PlayerId, failureBonus);
                    continue;
                }

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

                var sourceMutation = tier1Mutations[rng.Next(tier1Mutations.Count)];
                var targetMutation = targetMutations[rng.Next(targetMutations.Count)];

                int currentSourceLevel = player.GetMutationLevel(sourceMutation.Id);
                int newSourceLevel = Math.Max(0, currentSourceLevel - effectiveLevelsToConsume);
                player.SetMutationLevel(sourceMutation.Id, newSourceLevel, board.CurrentRound);

                int currentTargetLevel = player.GetMutationLevel(targetMutation.Id);
                player.SetMutationLevel(targetMutation.Id, currentTargetLevel + 1, board.CurrentRound);

                observer.RecordOntogenicRegressionEffect(player.PlayerId, sourceMutation.Name, effectiveLevelsToConsume, targetMutation.Name, 1);
                if (sacrificed.Count > 0)
                {
                    observer.RecordOntogenicRegressionSacrifices(player.PlayerId, sacrificed.Count, reductionsAchieved);
                }
            }
        }

        private static List<FungalCell> CollectOntogenicRegressionSacrifices(Player player, GameBoard board, Random rng, int baseLevelsToConsume, out int levelReductions)
        {
            levelReductions = 0;
            var enemyCells = new HashSet<FungalCell>();
            foreach (var myCell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!myCell.IsAlive) continue;
                foreach (var neighbor in board.GetOrthogonalNeighbors(myCell.TileId))
                {
                    var c = neighbor.FungalCell;
                    if (c != null && c.IsAlive && c.OwnerPlayerId.HasValue && c.OwnerPlayerId.Value != player.PlayerId && !c.IsResistant)
                    {
                        enemyCells.Add(c);
                    }
                }
            }
            if (enemyCells.Count == 0) return new();

            var list = enemyCells.ToList();
            // Shuffle
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            int killsPerReduction = GameBalance.OntogenicRegressionEnemyKillsPerLevelReduction;
            int maxKillsNeeded = (baseLevelsToConsume - 1) * killsPerReduction;
            int killsPerformed = 0;
            var sacrificed = new List<FungalCell>();
            foreach (var enemy in list)
            {
                if (killsPerformed >= maxKillsNeeded) break;
                // Execute normal kill pipeline so other effects can react
                board.KillFungalCell(enemy, Death.DeathReason.CytolyticBurst, player.PlayerId); // reuse a reason; ideally add new reason later
                sacrificed.Add(enemy);
                killsPerformed++;
            }
            levelReductions = killsPerformed / killsPerReduction;
            return sacrificed;
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
            int bonusPoints = 1;
            player.MutationPoints += 1;
            float secondChance = level * GameBalance.AdaptiveExpressionSecondPointChancePerLevel;
            if (rng.NextDouble() < secondChance)
            {
                bonusPoints++;
                player.MutationPoints += 1;
            }
            observer.RecordAdaptiveExpressionBonus(player.PlayerId, bonusPoints);
            observer.RecordMutationPointIncome(player.PlayerId, bonusPoints);
        }

        public static void TryApplyAnabolicInversion(
            Player player,
            List<Player> allPlayers,
            GameBoard board,
            Random rng,
            ISimulationObserver observer,
            IReadOnlyDictionary<int,int> livingCellCounts)
        {
            int level = player.GetMutationLevel(MutationIds.AnabolicInversion);
            if (level <= 0) return;
            int bonusPoints = player.RollAnabolicInversionBonus(allPlayers, rng, board, livingCellCounts);
            if (bonusPoints > 0)
            {
                player.MutationPoints += bonusPoints;
                observer.RecordAnabolicInversionBonus(player.PlayerId, bonusPoints);
                observer.RecordMutationPointIncome(player.PlayerId, bonusPoints);
            }
        }
    }
}