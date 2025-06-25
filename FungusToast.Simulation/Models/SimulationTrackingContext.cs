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
        // ────────────────
        // Mutation Points
        // ────────────────

        private readonly Dictionary<int, Dictionary<MutationTier, int>> mutationPointsSpentByTier = new();
        public void RecordMutationPointsSpent(int playerId, MutationTier tier, int mutationPointsSpent)
        {
            if (!mutationPointsSpentByTier.TryGetValue(playerId, out var tierDict))
                mutationPointsSpentByTier[playerId] = tierDict = new Dictionary<MutationTier, int>();
            if (!tierDict.ContainsKey(tier))
                tierDict[tier] = 0;
            tierDict[tier] += mutationPointsSpent;
        }
        public int GetMutationPointsSpent(int playerId, MutationTier tier)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var tierDict) && tierDict.TryGetValue(tier, out var val) ? val : 0;
        public Dictionary<MutationTier, int> GetMutationPointsSpentByTier(int playerId)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var dict) ? new Dictionary<MutationTier, int>(dict) : new();
        public int GetTotalMutationPointsSpent(int playerId)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var dict) ? dict.Values.Sum() : 0;
        public Dictionary<int, Dictionary<MutationTier, int>> GetAllMutationPointsSpentByTier()
            => mutationPointsSpentByTier.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MutationTier, int>(kvp.Value));

        // ────────────────
        // Mutation Point Income
        // ────────────────

        private readonly Dictionary<int, int> mutationPointIncomeByPlayer = new();
        public void RecordMutationPointIncome(int playerId, int newMutationPoints)
        {
            if (!mutationPointIncomeByPlayer.ContainsKey(playerId))
                mutationPointIncomeByPlayer[playerId] = 0;
            mutationPointIncomeByPlayer[playerId] += newMutationPoints;
        }
        public int GetMutationPointIncome(int playerId)
            => mutationPointIncomeByPlayer.TryGetValue(playerId, out var val) ? val : 0;
        public IReadOnlyDictionary<int, int> GetAllMutationPointIncome()
            => mutationPointIncomeByPlayer;

        // ────────────────
        // Cell Deaths by Reason
        // ────────────────

        private readonly Dictionary<int, Dictionary<DeathReason, int>> deathsByPlayerAndReason = new();
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1)
        {
            if (!deathsByPlayerAndReason.TryGetValue(playerId, out var reasonDict))
                deathsByPlayerAndReason[playerId] = reasonDict = new Dictionary<DeathReason, int>();
            if (!reasonDict.ContainsKey(reason))
                reasonDict[reason] = 0;
            reasonDict[reason] += deathCount;
        }
        public int GetCellDeathCount(int playerId, DeathReason reason)
            => deathsByPlayerAndReason.TryGetValue(playerId, out var reasonDict) && reasonDict.TryGetValue(reason, out var val) ? val : 0;
        public Dictionary<int, Dictionary<DeathReason, int>> GetAllCellDeathsByPlayerAndReason()
            => deathsByPlayerAndReason.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<DeathReason, int>(kvp.Value));
        public Dictionary<DeathReason, int> GetTotalDeathsByReason()
        {
            var result = new Dictionary<DeathReason, int>();
            foreach (var playerDict in deathsByPlayerAndReason.Values)
                foreach (var kvp in playerDict)
                    result[kvp.Key] = result.TryGetValue(kvp.Key, out var v) ? v + kvp.Value : kvp.Value;
            return result;
        }

        // ────────────────
        // Creeping Mold Moves
        // ────────────────

        private readonly Dictionary<int, int> creepingMoldMoves = new();
        public void RecordCreepingMoldMove(int playerId)
        {
            if (!creepingMoldMoves.ContainsKey(playerId))
                creepingMoldMoves[playerId] = 0;
            creepingMoldMoves[playerId]++;
        }
        public int GetCreepingMoldMoves(int playerId)
            => creepingMoldMoves.TryGetValue(playerId, out var val) ? val : 0;

        // ────────────────
        // Reclaimed Cells
        // ────────────────

        private readonly Dictionary<int, int> reclaimedCells = new();
        public void SetReclaims(int playerId, int count)
            => reclaimedCells[playerId] = count;
        public int GetReclaimedCells(int playerId)
            => reclaimedCells.TryGetValue(playerId, out var val) ? val : 0;

        // ────────────────
        // Regenerative Hyphae Reclaims
        // ────────────────

        private readonly Dictionary<int, int> regenerativeHyphaeReclaims = new();
        public void RecordRegenerativeHyphaeReclaim(int playerId)
        {
            if (!regenerativeHyphaeReclaims.ContainsKey(playerId))
                regenerativeHyphaeReclaims[playerId] = 0;
            regenerativeHyphaeReclaims[playerId]++;
        }
        public int GetRegenerativeHyphaeReclaims(int playerId)
            => regenerativeHyphaeReclaims.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllRegenerativeHyphaeReclaims() => new(regenerativeHyphaeReclaims);

        // ────────────────
        // Adaptive Expression Bonus
        // ────────────────

        private readonly Dictionary<int, int> adaptiveExpressionPointsEarned = new();
        public void RecordAdaptiveExpressionBonus(int playerId, int bonusPoints)
        {
            if (!adaptiveExpressionPointsEarned.ContainsKey(playerId))
                adaptiveExpressionPointsEarned[playerId] = 0;
            adaptiveExpressionPointsEarned[playerId] += bonusPoints;
        }
        public int GetAdaptiveExpressionPointsEarned(int playerId)
            => adaptiveExpressionPointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllAdaptiveExpressionPointsEarned() => new(adaptiveExpressionPointsEarned);

        // ────────────────
        // Mutator Phenotype Points
        // ────────────────

        private readonly Dictionary<int, int> mutatorPhenotypePointsEarned = new();
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (!mutatorPhenotypePointsEarned.ContainsKey(playerId))
                mutatorPhenotypePointsEarned[playerId] = 0;
            mutatorPhenotypePointsEarned[playerId] += freePointsEarned;
        }
        public int GetMutatorPhenotypePointsEarned(int playerId)
            => mutatorPhenotypePointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllMutatorPhenotypePointsEarned() => new(mutatorPhenotypePointsEarned);

        // ────────────────
        // Hyperadaptive Drift Points
        // ────────────────

        private readonly Dictionary<int, int> hyperadaptiveDriftPointsEarned = new();
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (!hyperadaptiveDriftPointsEarned.ContainsKey(playerId))
                hyperadaptiveDriftPointsEarned[playerId] = 0;
            hyperadaptiveDriftPointsEarned[playerId] += freePointsEarned;
        }
        public int GetHyperadaptiveDriftPointsEarned(int playerId)
            => hyperadaptiveDriftPointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllHyperadaptiveDriftPointsEarned() => new(hyperadaptiveDriftPointsEarned);

        // ────────────────
        // Necrohyphal Infiltration & Cascade
        // ────────────────

        private readonly Dictionary<int, int> necrohyphalInfiltrations = new();
        public void RecordNecrohyphalInfiltration(int playerId, int count)
        {
            if (!necrohyphalInfiltrations.ContainsKey(playerId))
                necrohyphalInfiltrations[playerId] = 0;
            necrohyphalInfiltrations[playerId] += count;
        }
        public int GetNecrohyphalInfiltrationCount(int playerId)
            => necrohyphalInfiltrations.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllNecrohyphalInfiltrations() => new(necrohyphalInfiltrations);

        private readonly Dictionary<int, int> necrohyphalCascades = new();
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int count)
        {
            if (!necrohyphalCascades.ContainsKey(playerId))
                necrohyphalCascades[playerId] = 0;
            necrohyphalCascades[playerId] += count;
        }
        public int GetNecrohyphalCascadeCount(int playerId)
            => necrohyphalCascades.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllNecrohyphalCascades() => new(necrohyphalCascades);

        // ────────────────
        // Necrotoxic Conversion
        // ────────────────

        private readonly Dictionary<int, int> necrotoxicConversionReclaims = new();
        public void RecordNecrotoxicConversionReclaim(int playerId, int count)
        {
            if (!necrotoxicConversionReclaims.ContainsKey(playerId))
                necrotoxicConversionReclaims[playerId] = 0;
            necrotoxicConversionReclaims[playerId] += count;
        }
        public int GetNecrotoxicConversionReclaims(int playerId)
            => necrotoxicConversionReclaims.TryGetValue(playerId, out var val) ? val : 0;

        // ────────────────
        // Mycotoxin Tracer Spores
        // ────────────────

        private readonly Dictionary<int, int> mycotoxinTracerSporeDrops = new();
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped)
        {
            if (!mycotoxinTracerSporeDrops.ContainsKey(playerId))
                mycotoxinTracerSporeDrops[playerId] = 0;
            mycotoxinTracerSporeDrops[playerId] += sporesDropped;
        }
        public int GetMycotoxinSporeDropCount(int playerId)
            => mycotoxinTracerSporeDrops.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetMycotoxinSporeDropCounts() => new(mycotoxinTracerSporeDrops);

        // ────────────────
        // Sporocidal Spore Drops
        // ────────────────

        private readonly Dictionary<int, int> sporocidalSporeDrops = new();
        public void ReportSporocidalSporeDrop(int playerId, int count)
        {
            if (!sporocidalSporeDrops.ContainsKey(playerId))
                sporocidalSporeDrops[playerId] = 0;
            sporocidalSporeDrops[playerId] += count;
        }
        public int GetSporocidalSporeDropCount(int playerId)
            => sporocidalSporeDrops.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetSporocidalSporeDropCounts() => new(sporocidalSporeDrops);

        // ────────────────
        // Necrosporulation Spore Drops
        // ────────────────

        private readonly Dictionary<int, int> necrosporulationSporeDrops = new();
        public void ReportNecrosporeDrop(int playerId, int count)
        {
            if (!necrosporulationSporeDrops.ContainsKey(playerId))
                necrosporulationSporeDrops[playerId] = 0;
            necrosporulationSporeDrops[playerId] += count;
        }
        public int GetNecrosporeDropCount(int playerId)
            => necrosporulationSporeDrops.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetNecrosporulationSporeDropCounts() => new(necrosporulationSporeDrops);

        // ────────────────
        // Necrophytic Bloom Spores & Reclaims
        // ────────────────

        private readonly Dictionary<int, int> necrophyticBloomSpores = new();
        private readonly Dictionary<int, int> necrophyticBloomReclaims = new();
        public void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims)
        {
            if (!necrophyticBloomSpores.ContainsKey(playerId))
                necrophyticBloomSpores[playerId] = 0;
            necrophyticBloomSpores[playerId] += sporesDropped;

            if (!necrophyticBloomReclaims.ContainsKey(playerId))
                necrophyticBloomReclaims[playerId] = 0;
            necrophyticBloomReclaims[playerId] += successfulReclaims;
        }
        public int GetNecrophyticBloomSporeDropCount(int playerId)
            => necrophyticBloomSpores.TryGetValue(playerId, out var val) ? val : 0;
        public int GetNecrophyticBloomReclaims(int playerId)
            => necrophyticBloomReclaims.TryGetValue(playerId, out var val) ? val : 0;

        // ────────────────
        // Toxin Catabolism
        // ────────────────

        private readonly Dictionary<int, int> toxinCatabolisms = new();
        private readonly Dictionary<int, int> catabolizedMutationPoints = new();
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int mutationPointsCatabolized)
        {
            if (!toxinCatabolisms.ContainsKey(playerId))
                toxinCatabolisms[playerId] = 0;
            toxinCatabolisms[playerId] += toxinsCatabolized;
            if (!catabolizedMutationPoints.ContainsKey(playerId))
                catabolizedMutationPoints[playerId] = 0;
            catabolizedMutationPoints[playerId] += mutationPointsCatabolized;
        }
        public int GetToxinCatabolismCount(int playerId)
            => toxinCatabolisms.TryGetValue(playerId, out var val) ? val : 0;
        public int GetCatabolizedMutationPoints(int playerId)
            => catabolizedMutationPoints.TryGetValue(playerId, out var val) ? val : 0;

        // ────────────────
        // Tendril Growth (by Direction)
        // ────────────────

        private readonly Dictionary<int, int> tendrilNorthwestGrownCells = new();
        private readonly Dictionary<int, int> tendrilNortheastGrownCells = new();
        private readonly Dictionary<int, int> tendrilSoutheastGrownCells = new();
        private readonly Dictionary<int, int> tendrilSouthwestGrownCells = new();
        public void RecordTendrilGrowth(int playerId, DiagonalDirection direction)
        {
            Dictionary<int, int> dict = direction switch
            {
                DiagonalDirection.Northwest => tendrilNorthwestGrownCells,
                DiagonalDirection.Northeast => tendrilNortheastGrownCells,
                DiagonalDirection.Southeast => tendrilSoutheastGrownCells,
                DiagonalDirection.Southwest => tendrilSouthwestGrownCells,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid DiagonalDirection value")
            };

            if (dict == null) return;
            if (!dict.ContainsKey(playerId))
                dict[playerId] = 0;
            dict[playerId]++;
        }
        public int GetTendrilNorthwestGrownCells(int playerId) => tendrilNorthwestGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilNortheastGrownCells(int playerId) => tendrilNortheastGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilSoutheastGrownCells(int playerId) => tendrilSoutheastGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public int GetTendrilSouthwestGrownCells(int playerId) => tendrilSouthwestGrownCells.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllTendrilNorthwestGrownCells() => new(tendrilNorthwestGrownCells);
        public Dictionary<int, int> GetAllTendrilNortheastGrownCells() => new(tendrilNortheastGrownCells);
        public Dictionary<int, int> GetAllTendrilSoutheastGrownCells() => new(tendrilSoutheastGrownCells);
        public Dictionary<int, int> GetAllTendrilSouthwestGrownCells() => new(tendrilSouthwestGrownCells);

        // ────────────────
        // Hyphal Surge Growths
        // ────────────────

        private readonly Dictionary<int, int> hyphalSurgeGrowths = new();
        public void RecordHyphalSurgeGrowth(int playerId)
        {
            if (!hyphalSurgeGrowths.ContainsKey(playerId))
                hyphalSurgeGrowths[playerId] = 0;
            hyphalSurgeGrowths[playerId]++;
        }
        public int GetHyphalSurgeGrowthCount(int playerId)
            => hyphalSurgeGrowths.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllHyphalSurgeGrowthCounts() => new(hyphalSurgeGrowths);

        // ────────────────
        // Hyphal Vectoring Growths
        // ────────────────

        private readonly Dictionary<int, int> hyphalVectoringGrowths = new();
        public void RecordHyphalVectoringGrowth(int playerId, int cellCount)
        {
            if (!hyphalVectoringGrowths.ContainsKey(playerId))
                hyphalVectoringGrowths[playerId] = 0;
            hyphalVectoringGrowths[playerId] += cellCount;
        }
        public int GetHyphalVectoringGrowthCount(int playerId)
            => hyphalVectoringGrowths.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllHyphalVectoringGrowthCounts() => new(hyphalVectoringGrowths);

        // ────────────────
        // Jetting Mycelium Outcomes (Grouped)
        // ────────────────

        private readonly Dictionary<int, int> jettingMyceliumInfested = new();
        private readonly Dictionary<int, int> jettingMyceliumReclaimed = new();
        private readonly Dictionary<int, int> jettingMyceliumCatabolicGrowth = new();
        private readonly Dictionary<int, int> jettingMyceliumAlreadyOwned = new();
        private readonly Dictionary<int, int> jettingMyceliumColonized = new();
        private readonly Dictionary<int, int> jettingMyceliumToxified = new();
        private readonly Dictionary<int, int> jettingMyceliumInvalid = new();

        public void ReportJettingMyceliumInfested(int playerId, int count)
        {
            if (!jettingMyceliumInfested.ContainsKey(playerId)) jettingMyceliumInfested[playerId] = 0;
            jettingMyceliumInfested[playerId] += count;
        }
        public void ReportJettingMyceliumReclaimed(int playerId, int count)
        {
            if (!jettingMyceliumReclaimed.ContainsKey(playerId)) jettingMyceliumReclaimed[playerId] = 0;
            jettingMyceliumReclaimed[playerId] += count;
        }
        public void ReportJettingMyceliumCatabolicGrowth(int playerId, int count)
        {
            if (!jettingMyceliumCatabolicGrowth.ContainsKey(playerId)) jettingMyceliumCatabolicGrowth[playerId] = 0;
            jettingMyceliumCatabolicGrowth[playerId] += count;
        }
        public void ReportJettingMyceliumAlreadyOwned(int playerId, int count)
        {
            if (!jettingMyceliumAlreadyOwned.ContainsKey(playerId)) jettingMyceliumAlreadyOwned[playerId] = 0;
            jettingMyceliumAlreadyOwned[playerId] += count;
        }
        public void ReportJettingMyceliumColonized(int playerId, int count)
        {
            if (!jettingMyceliumColonized.ContainsKey(playerId)) jettingMyceliumColonized[playerId] = 0;
            jettingMyceliumColonized[playerId] += count;
        }
        public void ReportJettingMyceliumToxified(int playerId, int count)
        {
            if (!jettingMyceliumToxified.ContainsKey(playerId)) jettingMyceliumToxified[playerId] = 0;
            jettingMyceliumToxified[playerId] += count;
        }
        public void ReportJettingMyceliumInvalid(int playerId, int count)
        {
            if (!jettingMyceliumInvalid.ContainsKey(playerId)) jettingMyceliumInvalid[playerId] = 0;
            jettingMyceliumInvalid[playerId] += count;
        }
        public int GetJettingMyceliumInfested(int playerId)
            => jettingMyceliumInfested.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumReclaimed(int playerId)
            => jettingMyceliumReclaimed.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumCatabolicGrowth(int playerId)
            => jettingMyceliumCatabolicGrowth.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumAlreadyOwned(int playerId)
            => jettingMyceliumAlreadyOwned.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumColonized(int playerId)
            => jettingMyceliumColonized.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumToxified(int playerId)
            => jettingMyceliumToxified.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumInvalid(int playerId)
            => jettingMyceliumInvalid.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllJettingMyceliumInfested() => new(jettingMyceliumInfested);
        public Dictionary<int, int> GetAllJettingMyceliumReclaimed() => new(jettingMyceliumReclaimed);
        public Dictionary<int, int> GetAllJettingMyceliumCatabolicGrowth() => new(jettingMyceliumCatabolicGrowth);
        public Dictionary<int, int> GetAllJettingMyceliumAlreadyOwned() => new(jettingMyceliumAlreadyOwned);
        public Dictionary<int, int> GetAllJettingMyceliumColonized() => new(jettingMyceliumColonized);
        public Dictionary<int, int> GetAllJettingMyceliumToxified() => new(jettingMyceliumToxified);
        public Dictionary<int, int> GetAllJettingMyceliumInvalid() => new(jettingMyceliumInvalid);

        // ────────────────
        // Jetting Mycelium Poisoned Tracking
        // ────────────────

        private readonly Dictionary<int, int> jettingMyceliumPoisoned = new();

        public void ReportJettingMyceliumPoisoned(int playerId, int count)
        {
            if (!jettingMyceliumPoisoned.ContainsKey(playerId))
                jettingMyceliumPoisoned[playerId] = 0;
            jettingMyceliumPoisoned[playerId] += count;
        }

        public int GetJettingMyceliumPoisoned(int playerId)
            => jettingMyceliumPoisoned.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllJettingMyceliumPoisoned() => new(jettingMyceliumPoisoned);


        // ────────────────
        // Failed Growths
        // ────────────────

        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();

        private readonly Dictionary<int, int> standardGrowthsByPlayer = new();
        /// <summary>
        /// Records a standard growth event for the specified player.
        /// </summary>
        /// <param name="playerId">The ID of the player who grew a cell.</param>
        public void RecordStandardGrowth(int playerId)
        {
            if (!standardGrowthsByPlayer.ContainsKey(playerId))
                standardGrowthsByPlayer[playerId] = 0;
            standardGrowthsByPlayer[playerId]++;
        }

        /// <summary>
        /// Gets the total standard growths for the specified player.
        /// </summary>
        public int GetStandardGrowths(int playerId)
        {
            return standardGrowthsByPlayer.TryGetValue(playerId, out var count) ? count : 0;
        }
    }
}
