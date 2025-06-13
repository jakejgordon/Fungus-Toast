using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Models
{
    public class SimulationTrackingContext : ISimulationObserver
    {
        // ────────────────────────────
        //  FIELDS / DATA TRACKERS
        // ────────────────────────────

        // mutation point spending and income
        private readonly Dictionary<int, Dictionary<MutationTier, int>> mutationPointsSpentByTier = new();
        private readonly Dictionary<int, int> mutationPointIncomeByPlayer = new();


        // Centralized death reason tracking: [playerId][DeathReason] => count
        private readonly Dictionary<int, Dictionary<DeathReason, int>> deathsByPlayerAndReason = new();

        private readonly Dictionary<int, int> creepingMoldMoves = new();
        private readonly Dictionary<int, int> reclaimedCells = new();
        private readonly Dictionary<int, int> mycotoxinTracerSporeDrops = new();
        private readonly Dictionary<int, int> sporocidalSporeDrops = new();
        private readonly Dictionary<int, int> necrosporulationSporeDrops = new();
        private readonly Dictionary<int, int> necrophyticBloomSpores = new();
        private readonly Dictionary<int, int> necrophyticBloomReclaims = new();
        private readonly Dictionary<int, int> toxinCatabolisms = new();
        private readonly Dictionary<int, int> catabolizedMutationPoints = new();

        // Necrotoxic Conversion
        private readonly Dictionary<int, int> necrotoxicConversionReclaims = new();

        // Separate tracking for mutation point sources
        private readonly Dictionary<int, int> adaptiveExpressionPointsEarned = new();
        private readonly Dictionary<int, int> mutatorPhenotypePointsEarned = new();
        private readonly Dictionary<int, int> hyperadaptiveDriftPointsEarned = new();

        // Necrohyphal Infiltration tracking
        private readonly Dictionary<int, int> necrohyphalInfiltrations = new();
        private readonly Dictionary<int, int> necrohyphalCascades = new();

        // Tendril Growth Stats
        private readonly Dictionary<int, int> tendrilNorthwestGrownCells = new();
        private readonly Dictionary<int, int> tendrilNortheastGrownCells = new();
        private readonly Dictionary<int, int> tendrilSoutheastGrownCells = new();
        private readonly Dictionary<int, int> tendrilSouthwestGrownCells = new();

        // Hyphal Surge Growth
        private readonly Dictionary<int, int> hyphalSurgeGrowths = new();

        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();

        // ────────────────────────────
        //  MUTATORS / TRACKING METHODS
        // ────────────────────────────

        public void RecordMutationPointsSpent(int playerId, MutationTier tier, int mutationPointsSpent)
        {
            if (!mutationPointsSpentByTier.TryGetValue(playerId, out var tierDict))
            {
                tierDict = new Dictionary<MutationTier, int>();
                mutationPointsSpentByTier[playerId] = tierDict;
            }
            if (!tierDict.ContainsKey(tier))
                tierDict[tier] = 0;

            tierDict[tier] += mutationPointsSpent;
        }

        public void RecordMutationPointIncome(int playerId, int newMutationPoints)
        {
            if (!mutationPointIncomeByPlayer.ContainsKey(playerId))
                mutationPointIncomeByPlayer[playerId] = 0;

            mutationPointIncomeByPlayer[playerId] += newMutationPoints;
        }



        // Unified cell death tracker
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1)
        {
            if (!deathsByPlayerAndReason.TryGetValue(playerId, out var reasonDict))
            {
                reasonDict = new Dictionary<DeathReason, int>();
                deathsByPlayerAndReason[playerId] = reasonDict;
            }
            if (!reasonDict.ContainsKey(reason))
                reasonDict[reason] = 0;
            reasonDict[reason] += deathCount;
        }

        public void RecordCreepingMoldMove(int playerId)
        {
            if (!creepingMoldMoves.ContainsKey(playerId))
                creepingMoldMoves[playerId] = 0;
            creepingMoldMoves[playerId]++;
        }

        public void SetReclaims(int playerId, int count)
        {
            reclaimedCells[playerId] = count;
        }

        public void RecordAdaptiveExpressionBonus(int playerId, int bonusPoints)
        {
            if (!adaptiveExpressionPointsEarned.ContainsKey(playerId))
                adaptiveExpressionPointsEarned[playerId] = 0;
            adaptiveExpressionPointsEarned[playerId] += bonusPoints;
        }
        public int GetAdaptiveExpressionPointsEarned(int playerId) =>
            adaptiveExpressionPointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllAdaptiveExpressionPointsEarned() => new(adaptiveExpressionPointsEarned);

        public void RecordFailedGrowth(int playerId)
        {
            if (!FailedGrowthsByPlayerId.ContainsKey(playerId))
                FailedGrowthsByPlayerId[playerId] = 0;
            FailedGrowthsByPlayerId[playerId]++;
        }

        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped)
        {
            if (!mycotoxinTracerSporeDrops.ContainsKey(playerId))
                mycotoxinTracerSporeDrops[playerId] = 0;
            mycotoxinTracerSporeDrops[playerId] += sporesDropped;
        }

        public void ReportSporocidalSporeDrop(int playerId, int count)
        {
            if (!sporocidalSporeDrops.ContainsKey(playerId))
                sporocidalSporeDrops[playerId] = 0;
            sporocidalSporeDrops[playerId] += count;
        }

        public void ReportNecrosporeDrop(int playerId, int count)
        {
            if (!necrosporulationSporeDrops.ContainsKey(playerId))
                necrosporulationSporeDrops[playerId] = 0;
            necrosporulationSporeDrops[playerId] += count;
        }

        public void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims)
        {
            if (!necrophyticBloomSpores.ContainsKey(playerId))
                necrophyticBloomSpores[playerId] = 0;
            if (!necrophyticBloomReclaims.ContainsKey(playerId))
                necrophyticBloomReclaims[playerId] = 0;
            necrophyticBloomSpores[playerId] += sporesDropped;
            necrophyticBloomReclaims[playerId] += successfulReclaims;
        }

        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int mutationPointsCatabolized)
        {
            if (!toxinCatabolisms.ContainsKey(playerId))
                toxinCatabolisms[playerId] = 0;
            toxinCatabolisms[playerId] += toxinsCatabolized;

            if (!catabolizedMutationPoints.ContainsKey(playerId))
                catabolizedMutationPoints[playerId] = 0;
            catabolizedMutationPoints[playerId] += mutationPointsCatabolized;
        }

        public void RecordHyphalSurgeGrowth(int playerId)
        {
            if (!hyphalSurgeGrowths.ContainsKey(playerId))
                hyphalSurgeGrowths[playerId] = 0;
            hyphalSurgeGrowths[playerId]++;
        }

        // Necrotoxic Conversion
        public void RecordNecrotoxicConversionReclaim(int playerId, int count)
        {
            if (!necrotoxicConversionReclaims.ContainsKey(playerId))
                necrotoxicConversionReclaims[playerId] = 0;
            necrotoxicConversionReclaims[playerId] += count;
        }

        // Necrohyphal Infiltration recorders
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount)
        {
            if (!necrohyphalInfiltrations.ContainsKey(playerId))
                necrohyphalInfiltrations[playerId] = 0;
            necrohyphalInfiltrations[playerId] += necrohyphalInfiltrationCount;
        }

        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount)
        {
            if (!necrohyphalCascades.ContainsKey(playerId))
                necrohyphalCascades[playerId] = 0;
            necrohyphalCascades[playerId] += cascadeCount;
        }

        // IMutationPointObserver implementations
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (!mutatorPhenotypePointsEarned.ContainsKey(playerId))
                mutatorPhenotypePointsEarned[playerId] = 0;
            mutatorPhenotypePointsEarned[playerId] += freePointsEarned;
        }

        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (!hyperadaptiveDriftPointsEarned.ContainsKey(playerId))
                hyperadaptiveDriftPointsEarned[playerId] = 0;
            hyperadaptiveDriftPointsEarned[playerId] += freePointsEarned;
        }

        // Tendril Growth recorders
        public void RecordTendrilGrowth(int playerId, Core.Growth.DiagonalDirection direction)
        {
            switch (direction)
            {
                case Core.Growth.DiagonalDirection.Northwest:
                    if (!tendrilNorthwestGrownCells.ContainsKey(playerId))
                        tendrilNorthwestGrownCells[playerId] = 0;
                    tendrilNorthwestGrownCells[playerId]++;
                    break;
                case Core.Growth.DiagonalDirection.Northeast:
                    if (!tendrilNortheastGrownCells.ContainsKey(playerId))
                        tendrilNortheastGrownCells[playerId] = 0;
                    tendrilNortheastGrownCells[playerId]++;
                    break;
                case Core.Growth.DiagonalDirection.Southeast:
                    if (!tendrilSoutheastGrownCells.ContainsKey(playerId))
                        tendrilSoutheastGrownCells[playerId] = 0;
                    tendrilSoutheastGrownCells[playerId]++;
                    break;
                case Core.Growth.DiagonalDirection.Southwest:
                    if (!tendrilSouthwestGrownCells.ContainsKey(playerId))
                        tendrilSouthwestGrownCells[playerId] = 0;
                    tendrilSouthwestGrownCells[playerId]++;
                    break;
            }
        }

        // ────────────────────────────
        //  GETTERS / ACCESSORS
        // ────────────────────────────

        // New unified accessor for all deaths (for stats, summaries, etc.)
        public int GetMutationPointsSpent(int playerId, MutationTier tier)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var tierDict) && tierDict.TryGetValue(tier, out var val) ? val : 0;

        public Dictionary<MutationTier, int> GetMutationPointsSpentByTier(int playerId)
        {
            // Return a new dictionary (defensive copy)
            if (mutationPointsSpentByTier.TryGetValue(playerId, out var dict))
                return new Dictionary<MutationTier, int>(dict);
            return new Dictionary<MutationTier, int>();
        }

        public int GetTotalMutationPointsSpent(int playerId)
        {
            if (mutationPointsSpentByTier.TryGetValue(playerId, out var dict))
                return dict.Values.Sum();
            return 0;
        }

        public int GetMutationPointIncome(int playerId)
        {
            return mutationPointIncomeByPlayer.TryGetValue(playerId, out var val) ? val : 0;
        }


        // Optionally: Get all per-player tier breakdowns
        public Dictionary<int, Dictionary<MutationTier, int>> GetAllMutationPointsSpentByTier()
            => mutationPointsSpentByTier.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MutationTier, int>(kvp.Value));

        public IReadOnlyDictionary<int, int> GetAllMutationPointIncome()
        {
            return mutationPointIncomeByPlayer;
        }

        public int GetCellDeathCount(int playerId, DeathReason reason)
        {
            if (deathsByPlayerAndReason.TryGetValue(playerId, out var reasonDict))
                if (reasonDict.TryGetValue(reason, out var val))
                    return val;
            return 0;
        }

        public int GetHyphalSurgeGrowthCount(int playerId) =>
            hyphalSurgeGrowths.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, Dictionary<DeathReason, int>> GetAllCellDeathsByPlayerAndReason()
            => deathsByPlayerAndReason.ToDictionary(
                kvp => kvp.Key,
                kvp => new Dictionary<DeathReason, int>(kvp.Value)
            );

        // Example: get all deaths by reason (all players)
        public Dictionary<DeathReason, int> GetTotalDeathsByReason()
        {
            var result = new Dictionary<DeathReason, int>();
            foreach (var playerDict in deathsByPlayerAndReason.Values)
            {
                foreach (var kvp in playerDict)
                {
                    if (!result.ContainsKey(kvp.Key))
                        result[kvp.Key] = 0;
                    result[kvp.Key] += kvp.Value;
                }
            }
            return result;
        }

        public int GetTendrilNorthwestGrownCells(int playerId) =>
            tendrilNorthwestGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilNortheastGrownCells(int playerId) =>
            tendrilNortheastGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilSoutheastGrownCells(int playerId) =>
            tendrilSoutheastGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilSouthwestGrownCells(int playerId) =>
            tendrilSouthwestGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetCreepingMoldMoves(int playerId) =>
            creepingMoldMoves.TryGetValue(playerId, out var val) ? val : 0;
        public int GetReclaimedCells(int playerId) =>
            reclaimedCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrophyticBloomSporeDropCount(int playerId) =>
            necrophyticBloomSpores.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrophyticBloomReclaims(int playerId) =>
            necrophyticBloomReclaims.TryGetValue(playerId, out var val) ? val : 0;
        public int GetFailedGrowthCount(int playerId) =>
            FailedGrowthsByPlayerId.TryGetValue(playerId, out var val) ? val : 0;
        public int GetToxinCatabolismCount(int playerId) =>
            toxinCatabolisms.TryGetValue(playerId, out var val) ? val : 0;
        public int GetSporocidalSporeDropCount(int playerId) =>
            sporocidalSporeDrops.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrosporeDropCount(int playerId) =>
            necrosporulationSporeDrops.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrophyticBloomReclaimCount(int playerId) =>
            necrophyticBloomReclaims.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrotoxicConversionReclaims(int playerId) =>
            necrotoxicConversionReclaims.TryGetValue(playerId, out var val) ? val : 0;
        public int GetMycotoxinSporeDropCount(int playerId) =>
            mycotoxinTracerSporeDrops.TryGetValue(playerId, out var val) ? val : 0;
        public int GetCatabolizedMutationPoints(int playerId) =>
            catabolizedMutationPoints.TryGetValue(playerId, out var val) ? val : 0;
        public int GetMutatorPhenotypePointsEarned(int playerId) =>
            mutatorPhenotypePointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public int GetHyperadaptiveDriftPointsEarned(int playerId) =>
            hyperadaptiveDriftPointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrohyphalInfiltrationCount(int playerId) =>
            necrohyphalInfiltrations.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrohyphalCascadeCount(int playerId) =>
            necrohyphalCascades.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllHyphalSurgeGrowthCounts() => new(hyphalSurgeGrowths);

        // Bulk accessors for summaries/statistics
        public Dictionary<int, int> GetSporocidalSpores() => new(sporocidalSporeDrops);
        public Dictionary<int, int> GetNecroSpores() => new(necrosporulationSporeDrops);
        public Dictionary<int, int> GetNecrophyticBloomSpores() => new(necrophyticBloomSpores);
        public Dictionary<int, int> GetNecrophyticBloomReclaims() => new(necrophyticBloomReclaims);
        public Dictionary<int, int> GetMycotoxinTracerSporeDrops() => new(mycotoxinTracerSporeDrops);
        public Dictionary<int, int> GetAllNecrotoxicConversionReclaims() => new(necrotoxicConversionReclaims);
        public Dictionary<int, int> GetToxinCatabolisms() => new(toxinCatabolisms);
        public Dictionary<int, int> GetCatabolizedMutationPoints() => new(catabolizedMutationPoints);

        public Dictionary<int, int> GetAllMutatorPhenotypePointsEarned() => new(mutatorPhenotypePointsEarned);
        public Dictionary<int, int> GetAllHyperadaptiveDriftPointsEarned() => new(hyperadaptiveDriftPointsEarned);
        public Dictionary<int, int> GetAllNecrohyphalInfiltrations() => new(necrohyphalInfiltrations);
        public Dictionary<int, int> GetAllNecrohyphalCascades() => new(necrohyphalCascades);
        public Dictionary<int, int> GetAllTendrilNorthwestGrownCells() => new(tendrilNorthwestGrownCells);
        public Dictionary<int, int> GetAllTendrilNortheastGrownCells() => new(tendrilNortheastGrownCells);
        public Dictionary<int, int> GetAllTendrilSoutheastGrownCells() => new(tendrilSoutheastGrownCells);
        public Dictionary<int, int> GetAllTendrilSouthwestGrownCells() => new(tendrilSouthwestGrownCells);
    }
}
