using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Simulation.Models
{
    public class SimulationTrackingContext : ISimulationObserver
    {
        #region Mutation Point Tracking

        private readonly Dictionary<int, Dictionary<MutationTier, int>> mutationPointsSpentByTier = new();
        private readonly Dictionary<int, int> mutationPointIncomeByPlayer = new();

        private readonly Dictionary<int, int> creepingMoldMoves = new();
        private readonly Dictionary<int, int> reclaimedCells = new();
        private readonly Dictionary<int, int> mycotoxinTracerSporeDrops = new();
        private readonly Dictionary<int, int> sporocidalSporeDrops = new();
        private readonly Dictionary<int, int> necrosporulationSporeDrops = new();
        private readonly Dictionary<int, int> necrophyticBloomSpores = new();
        private readonly Dictionary<int, int> necrophyticBloomReclaims = new();
        private readonly Dictionary<int, int> toxinCatabolisms = new();
        private readonly Dictionary<int, int> catabolizedMutationPoints = new();
        private readonly Dictionary<int, int> regenerativeHyphaeReclaims = new();

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

        private readonly Dictionary<int, int> hyphalVectoringGrowths = new();

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

        public int GetMutationPointsSpent(int playerId, MutationTier tier)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var tierDict) && tierDict.TryGetValue(tier, out var val) ? val : 0;

        public Dictionary<MutationTier, int> GetMutationPointsSpentByTier(int playerId)
        {
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

        public Dictionary<int, Dictionary<MutationTier, int>> GetAllMutationPointsSpentByTier()
            => mutationPointsSpentByTier.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MutationTier, int>(kvp.Value));

        public IReadOnlyDictionary<int, int> GetAllMutationPointIncome()
        {
            return mutationPointIncomeByPlayer;
        }

        #endregion

        #region Cell Death Tracking

        private readonly Dictionary<int, Dictionary<DeathReason, int>> deathsByPlayerAndReason = new();

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

        public int GetCellDeathCount(int playerId, DeathReason reason)
        {
            if (deathsByPlayerAndReason.TryGetValue(playerId, out var reasonDict))
                if (reasonDict.TryGetValue(reason, out var val))
                    return val;
            return 0;
        }

        public Dictionary<int, Dictionary<DeathReason, int>> GetAllCellDeathsByPlayerAndReason()
            => deathsByPlayerAndReason.ToDictionary(
                kvp => kvp.Key,
                kvp => new Dictionary<DeathReason, int>(kvp.Value)
            );

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

        #endregion

        #region General Stat & Outcome Trackers

        public void RecordCreepingMoldMove(int playerId)
        {
            if (!creepingMoldMoves.ContainsKey(playerId))
                creepingMoldMoves[playerId] = 0;
            creepingMoldMoves[playerId]++;
        }

        public int GetCreepingMoldMoves(int playerId) =>
            creepingMoldMoves.TryGetValue(playerId, out var val) ? val : 0;

        public void SetReclaims(int playerId, int count)
        {
            reclaimedCells[playerId] = count;
        }

        public void RecordRegenerativeHyphaeReclaim(int playerId)
        {
            if (!regenerativeHyphaeReclaims.ContainsKey(playerId))
                regenerativeHyphaeReclaims[playerId] = 0;
            regenerativeHyphaeReclaims[playerId]++;
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
        public int GetReclaimedCells(int playerId) =>
            reclaimedCells.TryGetValue(playerId, out var val) ? val : 0;

        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped)
        {
            if (!mycotoxinTracerSporeDrops.ContainsKey(playerId))
                mycotoxinTracerSporeDrops[playerId] = 0;
            mycotoxinTracerSporeDrops[playerId] += sporesDropped;
        }

        public int GetMycotoxinSporeDropCount(int playerId) =>
            mycotoxinTracerSporeDrops.TryGetValue(playerId, out var val) ? val : 0;

        public void ReportSporocidalSporeDrop(int playerId, int count)
        {
            if (!sporocidalSporeDrops.ContainsKey(playerId))
                sporocidalSporeDrops[playerId] = 0;
            sporocidalSporeDrops[playerId] += count;
        }

        public int GetSporocidalSporeDropCount(int playerId) =>
            sporocidalSporeDrops.TryGetValue(playerId, out var val) ? val : 0;


        public void ReportNecrosporeDrop(int playerId, int count)
        {
            if (!necrosporulationSporeDrops.ContainsKey(playerId))
                necrosporulationSporeDrops[playerId] = 0;
            necrosporulationSporeDrops[playerId] += count;
        }

        public int GetNecrosporeDropCount(int playerId) =>
            necrosporulationSporeDrops.TryGetValue(playerId, out var val) ? val : 0;

        public void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims)
        {
            if (!necrophyticBloomSpores.ContainsKey(playerId))
                necrophyticBloomSpores[playerId] = 0;
            if (!necrophyticBloomReclaims.ContainsKey(playerId))
                necrophyticBloomReclaims[playerId] = 0;
            necrophyticBloomSpores[playerId] += sporesDropped;
            necrophyticBloomReclaims[playerId] += successfulReclaims;
        }

        public int GetNecrophyticBloomSporeDropCount(int playerId) =>
            necrophyticBloomSpores.TryGetValue(playerId, out var val) ? val : 0;

        public int GetNecrophyticBloomReclaims(int playerId) =>
            necrophyticBloomReclaims.TryGetValue(playerId, out var val) ? val : 0;

        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int mutationPointsCatabolized)
        {
            if (!toxinCatabolisms.ContainsKey(playerId))
                toxinCatabolisms[playerId] = 0;
            toxinCatabolisms[playerId] += toxinsCatabolized;

            if (!catabolizedMutationPoints.ContainsKey(playerId))
                catabolizedMutationPoints[playerId] = 0;
            catabolizedMutationPoints[playerId] += mutationPointsCatabolized;
        }

        public int GetToxinCatabolismCount(int playerId) =>
            toxinCatabolisms.TryGetValue(playerId, out var val) ? val : 0;

        public int GetCatabolizedMutationPoints(int playerId) =>
            catabolizedMutationPoints.TryGetValue(playerId, out var val) ? val : 0;

        public void RecordNecrotoxicConversionReclaim(int playerId, int count)
        {
            if (!necrotoxicConversionReclaims.ContainsKey(playerId))
                necrotoxicConversionReclaims[playerId] = 0;
            necrotoxicConversionReclaims[playerId] += count;
        }

        public int GetNecrotoxicConversionReclaims(int playerId) =>
            necrotoxicConversionReclaims.TryGetValue(playerId, out var val) ? val : 0;

        #endregion

        #region Mutation Point Source Tracking

        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (!mutatorPhenotypePointsEarned.ContainsKey(playerId))
                mutatorPhenotypePointsEarned[playerId] = 0;
            mutatorPhenotypePointsEarned[playerId] += freePointsEarned;
        }

        public int GetMutatorPhenotypePointsEarned(int playerId) =>
            mutatorPhenotypePointsEarned.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllMutatorPhenotypePointsEarned() => new(mutatorPhenotypePointsEarned);

        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (!hyperadaptiveDriftPointsEarned.ContainsKey(playerId))
                hyperadaptiveDriftPointsEarned[playerId] = 0;
            hyperadaptiveDriftPointsEarned[playerId] += freePointsEarned;
        }

        public int GetHyperadaptiveDriftPointsEarned(int playerId) =>
            hyperadaptiveDriftPointsEarned.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllHyperadaptiveDriftPointsEarned() => new(hyperadaptiveDriftPointsEarned);

        #endregion

        #region Growth and Special Mutation Stats

        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount)
        {
            if (!necrohyphalInfiltrations.ContainsKey(playerId))
                necrohyphalInfiltrations[playerId] = 0;
            necrohyphalInfiltrations[playerId] += necrohyphalInfiltrationCount;
        }

        public int GetNecrohyphalInfiltrationCount(int playerId) =>
            necrohyphalInfiltrations.TryGetValue(playerId, out var val) ? val : 0;

        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount)
        {
            if (!necrohyphalCascades.ContainsKey(playerId))
                necrohyphalCascades[playerId] = 0;
            necrohyphalCascades[playerId] += cascadeCount;
        }

        public int GetNecrohyphalCascadeCount(int playerId) =>
            necrohyphalCascades.TryGetValue(playerId, out var val) ? val : 0;

        public void RecordTendrilGrowth(int playerId, DiagonalDirection direction)
        {
            switch (direction)
            {
                case DiagonalDirection.Northwest:
                    if (!tendrilNorthwestGrownCells.ContainsKey(playerId))
                        tendrilNorthwestGrownCells[playerId] = 0;
                    tendrilNorthwestGrownCells[playerId]++;
                    break;
                case DiagonalDirection.Northeast:
                    if (!tendrilNortheastGrownCells.ContainsKey(playerId))
                        tendrilNortheastGrownCells[playerId] = 0;
                    tendrilNortheastGrownCells[playerId]++;
                    break;
                case DiagonalDirection.Southeast:
                    if (!tendrilSoutheastGrownCells.ContainsKey(playerId))
                        tendrilSoutheastGrownCells[playerId] = 0;
                    tendrilSoutheastGrownCells[playerId]++;
                    break;
                case DiagonalDirection.Southwest:
                    if (!tendrilSouthwestGrownCells.ContainsKey(playerId))
                        tendrilSouthwestGrownCells[playerId] = 0;
                    tendrilSouthwestGrownCells[playerId]++;
                    break;
            }
        }

        public int GetTendrilNorthwestGrownCells(int playerId) =>
            tendrilNorthwestGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilNortheastGrownCells(int playerId) =>
            tendrilNortheastGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilSoutheastGrownCells(int playerId) =>
            tendrilSoutheastGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilSouthwestGrownCells(int playerId) =>
            tendrilSouthwestGrownCells.TryGetValue(playerId, out var val) ? val : 0;

        public void RecordHyphalSurgeGrowth(int playerId)
        {
            if (!hyphalSurgeGrowths.ContainsKey(playerId))
                hyphalSurgeGrowths[playerId] = 0;
            hyphalSurgeGrowths[playerId]++;
        }

        public int GetHyphalSurgeGrowthCount(int playerId) =>
            hyphalSurgeGrowths.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllHyphalSurgeGrowthCounts() => new(hyphalSurgeGrowths);

        public void RecordHyphalVectoringGrowth(int playerId, int cellCount)
        {
            if (!hyphalVectoringGrowths.ContainsKey(playerId))
                hyphalVectoringGrowths[playerId] = 0;
            hyphalVectoringGrowths[playerId] += cellCount;
        }

        public int GetHyphalVectoringGrowthCount(int playerId) =>
            hyphalVectoringGrowths.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllHyphalVectoringGrowthCounts() => new(hyphalVectoringGrowths);

        #endregion

        #region Failed Growth Tracking

        public int GetRegenerativeHyphaeReclaims(int playerId) =>
            regenerativeHyphaeReclaims.TryGetValue(playerId, out var val) ? val : 0;

        #endregion

        #region Jetting Mycelium Outcomes

        private readonly Dictionary<int, int> jettingMyceliumParasitized = new();
        private readonly Dictionary<int, int> jettingMyceliumReclaimed = new();
        private readonly Dictionary<int, int> jettingMyceliumCatabolicGrowth = new();
        private readonly Dictionary<int, int> jettingMyceliumAlreadyOwned = new();
        private readonly Dictionary<int, int> jettingMyceliumInvalid = new();

        public void ReportJettingMyceliumParasitized(int playerId, int count)
        {
            if (!jettingMyceliumParasitized.ContainsKey(playerId))
                jettingMyceliumParasitized[playerId] = 0;
            jettingMyceliumParasitized[playerId] += count;
        }

        public void ReportJettingMyceliumReclaimed(int playerId, int count)
        {
            if (!jettingMyceliumReclaimed.ContainsKey(playerId))
                jettingMyceliumReclaimed[playerId] = 0;
            jettingMyceliumReclaimed[playerId] += count;
        }

        public void ReportJettingMyceliumCatabolicGrowth(int playerId, int count)
        {
            if (!jettingMyceliumCatabolicGrowth.ContainsKey(playerId))
                jettingMyceliumCatabolicGrowth[playerId] = 0;
            jettingMyceliumCatabolicGrowth[playerId] += count;
        }

        public void ReportJettingMyceliumAlreadyOwned(int playerId, int count)
        {
            if (!jettingMyceliumAlreadyOwned.ContainsKey(playerId))
                jettingMyceliumAlreadyOwned[playerId] = 0;
            jettingMyceliumAlreadyOwned[playerId] += count;
        }

        public void ReportJettingMyceliumInvalid(int playerId, int count)
        {
            if (!jettingMyceliumInvalid.ContainsKey(playerId))
                jettingMyceliumInvalid[playerId] = 0;
            jettingMyceliumInvalid[playerId] += count;
        }

        public int GetJettingMyceliumParasitized(int playerId) =>
            jettingMyceliumParasitized.TryGetValue(playerId, out var val) ? val : 0;

        public int GetJettingMyceliumReclaimed(int playerId) =>
            jettingMyceliumReclaimed.TryGetValue(playerId, out var val) ? val : 0;

        public int GetJettingMyceliumCatabolicGrowth(int playerId) =>
            jettingMyceliumCatabolicGrowth.TryGetValue(playerId, out var val) ? val : 0;

        public int GetJettingMyceliumAlreadyOwned(int playerId) =>
            jettingMyceliumAlreadyOwned.TryGetValue(playerId, out var val) ? val : 0;

        public int GetJettingMyceliumInvalid(int playerId) =>
            jettingMyceliumInvalid.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllJettingMyceliumParasitized() => new(jettingMyceliumParasitized);
        public Dictionary<int, int> GetAllJettingMyceliumReclaimed() => new(jettingMyceliumReclaimed);
        public Dictionary<int, int> GetAllJettingMyceliumCatabolicGrowth() => new(jettingMyceliumCatabolicGrowth);
        public Dictionary<int, int> GetAllJettingMyceliumAlreadyOwned() => new(jettingMyceliumAlreadyOwned);
        public Dictionary<int, int> GetAllJettingMyceliumInvalid() => new(jettingMyceliumInvalid);

        // I'm too lazy to figure out where these should go!
        public Dictionary<int, int> GetSporocidalSporeDropCounts() => new(sporocidalSporeDrops);
        public Dictionary<int, int> GetNecrosporulationSporeDropCounts() => new(necrosporulationSporeDrops);
        public Dictionary<int, int> GetMycotoxinSporeDropCounts() => new(mycotoxinTracerSporeDrops);

        public Dictionary<int, int> GetAllNecrohyphalInfiltrations() => new(necrohyphalInfiltrations);
        public Dictionary<int, int> GetAllNecrohyphalCascades() => new(necrohyphalCascades);
        public Dictionary<int, int> GetAllTendrilNorthwestGrownCells() => new(tendrilNorthwestGrownCells);
        public Dictionary<int, int> GetAllTendrilNortheastGrownCells() => new(tendrilNortheastGrownCells);
        public Dictionary<int, int> GetAllTendrilSoutheastGrownCells() => new(tendrilSoutheastGrownCells);
        public Dictionary<int, int> GetAllTendrilSouthwestGrownCells() => new(tendrilSouthwestGrownCells);
        public Dictionary<int, int> GetAllRegenerativeHyphaeReclaims() => new(regenerativeHyphaeReclaims);

        #endregion
    }
}
