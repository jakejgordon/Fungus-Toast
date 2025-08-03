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

            // Actually perform the upgrades, attributing each point appropriately
            int mutatorPoints = 0;
            int hyperadaptivePoints = 0;

            for (int i = 0; i < upgrades; i++)
            {
                bool upgraded = player.TryAutoUpgrade(pick, currentRound);
                if (!upgraded) break;

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
                }
            }

            // Notify observer
            if (observer != null)
            {
                if (mutatorPoints > 0)
                    observer.RecordMutatorPhenotypeMutationPointsEarned(player.PlayerId, mutatorPoints);

                if (hyperadaptivePoints > 0)
                    observer.RecordHyperadaptiveDriftMutationPointsEarned(player.PlayerId, hyperadaptivePoints);
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
                observer?.RecordToxinCatabolism(player.PlayerId, toxinsMetabolized, pointsSoFar);
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
                observer?.ReportNecrophyticBloomSporeDrop(player.PlayerId, totalSpores, reclaims);
            }
        }

        /// <summary>
        /// Triggers Necrophytic Bloom on individual cell death.
        /// </summary>
        public static void TriggerNecrophyticBloomOnCellDeath(
           Player owner,
           GameBoard board,
           Random rng,
           float occupiedPercent,
           ISimulationObserver observer)
        {
            int level = owner.GetMutationLevel(MutationIds.NecrophyticBloom);
            if (level <= 0) return;

            float damping = GetNecrophyticBloomDamping(occupiedPercent);
            int spores = (int)Math.Floor(
                level * GameBalance.NecrophyticBloomSporesPerDeathPerLevel * damping);

            if (spores <= 0) return;

            var allTileIds = board.AllTiles().Select(t => t.TileId).ToList();
            int reclaims = 0;

            for (int i = 0; i < spores; i++)
            {
                int randomTileId = allTileIds[rng.Next(allTileIds.Count)];
                bool success = board.TryReclaimDeadCell(owner.PlayerId, randomTileId, Growth.GrowthSource.NecrophyticBloom);
                if (success) reclaims++;
            }

            observer?.ReportNecrophyticBloomSporeDrop(owner.PlayerId, spores, reclaims);
        }

        /// <summary>
        /// Checks if a mutation can be auto-upgraded (without requiring mutation points).
        /// </summary>
        private static bool CanAutoUpgrade(Player player, Mutation mutation)
        {
            if (mutation == null) return false;

            // Surge: can't upgrade while active
            if (mutation.IsSurge && player.IsSurgeActive(mutation.Id))
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
    }
}