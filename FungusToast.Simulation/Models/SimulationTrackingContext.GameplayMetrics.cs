using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;

namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
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
        public int GetCreepingMoldMoves(int playerId) => creepingMoldMoves.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllCreepingMoldMoves() => new(creepingMoldMoves);

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
        // Anabolic Inversion Points
        // ────────────────

        private readonly Dictionary<int, int> anabolicInversionPointsEarned = new();
        public void RecordAnabolicInversionBonus(int playerId, int bonusPoints)
        {
            if (!anabolicInversionPointsEarned.ContainsKey(playerId))
                anabolicInversionPointsEarned[playerId] = 0;
            anabolicInversionPointsEarned[playerId] += bonusPoints;
        }
        public int GetAnabolicInversionPointsEarned(int playerId)
            => anabolicInversionPointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllAnabolicInversionPointsEarned() => new(anabolicInversionPointsEarned);

        // ────────────────
        // Apical Yield Points
        // ────────────────

        private readonly Dictionary<int, int> apicalYieldPointsEarned = new();
        public void RecordApicalYieldBonus(int playerId, string mutationName, int bonusPoints)
        {
            if (!apicalYieldPointsEarned.ContainsKey(playerId))
                apicalYieldPointsEarned[playerId] = 0;
            apicalYieldPointsEarned[playerId] += bonusPoints;
        }
        public int GetApicalYieldPointsEarned(int playerId)
            => apicalYieldPointsEarned.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllApicalYieldPointsEarned() => new(apicalYieldPointsEarned);

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
        // Catabolic Rebirth
        // ────────────────

        private readonly Dictionary<int, int> catabolicRebirthResurrections = new();
        private readonly Dictionary<int, int> catabolicRebirthAgedToxins = new();
        public void RecordCatabolicRebirthResurrection(int playerId, int count)
        {
            if (!catabolicRebirthResurrections.ContainsKey(playerId))
                catabolicRebirthResurrections[playerId] = 0;
            catabolicRebirthResurrections[playerId] += count;
        }
        public void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged)
        {
            if (!catabolicRebirthAgedToxins.ContainsKey(playerId))
                catabolicRebirthAgedToxins[playerId] = 0;
            catabolicRebirthAgedToxins[playerId] += toxinsAged;
        }
        public int GetCatabolicRebirthResurrections(int playerId)
            => catabolicRebirthResurrections.TryGetValue(playerId, out var val) ? val : 0;
        public int GetCatabolicRebirthAgedToxins(int playerId)
            => catabolicRebirthAgedToxins.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllCatabolicRebirthResurrections() => new(catabolicRebirthResurrections);
        public Dictionary<int, int> GetAllCatabolicRebirthAgedToxins() => new(catabolicRebirthAgedToxins);

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
        public void ReportSporicidalSporeDrop(int playerId, int count)
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
        // Necrophytic Bloom Composting
        // ────────────────

        private readonly Dictionary<int, int> necrophyticBloomPatchesCreated = new();
        public void RecordNecrophyticBloomPatchCreation(int playerId, int createdPatchCount)
        {
            if (!necrophyticBloomPatchesCreated.ContainsKey(playerId))
                necrophyticBloomPatchesCreated[playerId] = 0;
            necrophyticBloomPatchesCreated[playerId] += createdPatchCount;
        }
        public int GetNecrophyticBloomPatchCreationCount(int playerId)
            => necrophyticBloomPatchesCreated.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllNecrophyticBloomPatchCreations() => new(necrophyticBloomPatchesCreated);

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
        // Directed Vector Growths
        // ────────────────

        private readonly Dictionary<int, int> directedVectorGrowths = new();
        public void RecordDirectedVectorGrowth(int playerId, int cellCount)
        {
            if (!directedVectorGrowths.ContainsKey(playerId))
                directedVectorGrowths[playerId] = 0;
            directedVectorGrowths[playerId] += cellCount;
        }
        public int GetDirectedVectorGrowthCount(int playerId)
            => directedVectorGrowths.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllDirectedVectorGrowthCounts() => new(directedVectorGrowths);

        // ────────────────
        // Directed Vector Outcomes
        // ────────────────
        private readonly Dictionary<int, int> directedVectorInfested = new();
        private readonly Dictionary<int, int> directedVectorReclaimed = new();
        private readonly Dictionary<int, int> directedVectorCatabolicGrowth = new();
        private readonly Dictionary<int, int> directedVectorAlreadyOwned = new();
        private readonly Dictionary<int, int> directedVectorColonized = new();
        private readonly Dictionary<int, int> directedVectorInvalid = new();

        public void ReportDirectedVectorInfested(int playerId, int count)
        {
            if (!directedVectorInfested.ContainsKey(playerId))
                directedVectorInfested[playerId] = 0;
            directedVectorInfested[playerId] += count;
        }
        public void ReportDirectedVectorReclaimed(int playerId, int count)
        {
            if (!directedVectorReclaimed.ContainsKey(playerId))
                directedVectorReclaimed[playerId] = 0;
            directedVectorReclaimed[playerId] += count;
        }
        public void ReportDirectedVectorCatabolicGrowth(int playerId, int count)
        {
            if (!directedVectorCatabolicGrowth.ContainsKey(playerId))
                directedVectorCatabolicGrowth[playerId] = 0;
            directedVectorCatabolicGrowth[playerId] += count;
        }
        public void ReportDirectedVectorAlreadyOwned(int playerId, int count)
        {
            if (!directedVectorAlreadyOwned.ContainsKey(playerId))
                directedVectorAlreadyOwned[playerId] = 0;
            directedVectorAlreadyOwned[playerId] += count;
        }
        public void ReportDirectedVectorColonized(int playerId, int count)
        {
            if (!directedVectorColonized.ContainsKey(playerId))
                directedVectorColonized[playerId] = 0;
            directedVectorColonized[playerId] += count;
        }
        public void ReportDirectedVectorInvalid(int playerId, int count)
        {
            if (!directedVectorInvalid.ContainsKey(playerId))
                directedVectorInvalid[playerId] = 0;
            directedVectorInvalid[playerId] += count;
        }
        public int GetDirectedVectorInfested(int playerId) => directedVectorInfested.TryGetValue(playerId, out var val) ? val : 0;
        public int GetDirectedVectorReclaimed(int playerId) => directedVectorReclaimed.TryGetValue(playerId, out var val) ? val : 0;
        public int GetDirectedVectorCatabolicGrowth(int playerId) => directedVectorCatabolicGrowth.TryGetValue(playerId, out var val) ? val : 0;
        public int GetDirectedVectorAlreadyOwned(int playerId) => directedVectorAlreadyOwned.TryGetValue(playerId, out var val) ? val : 0;
        public int GetDirectedVectorColonized(int playerId) => directedVectorColonized.TryGetValue(playerId, out var val) ? val : 0;
        public int GetDirectedVectorInvalid(int playerId) => directedVectorInvalid.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllDirectedVectorInfested() => new(directedVectorInfested);
        public Dictionary<int, int> GetAllDirectedVectorReclaimed() => new(directedVectorReclaimed);
        public Dictionary<int, int> GetAllDirectedVectorCatabolicGrowth() => new(directedVectorCatabolicGrowth);
        public Dictionary<int, int> GetAllDirectedVectorAlreadyOwned() => new(directedVectorAlreadyOwned);
        public Dictionary<int, int> GetAllDirectedVectorColonized() => new(directedVectorColonized);
        public Dictionary<int, int> GetAllDirectedVectorInvalid() => new(directedVectorInvalid);

        // ────────────────
        // Jetting Mycelium Outcomes
        // ────────────────
        private readonly Dictionary<int, int> jettingMyceliumInfested = new();
        private readonly Dictionary<int, int> jettingMyceliumReclaimed = new();
        private readonly Dictionary<int, int> jettingMyceliumCatabolicGrowth = new();
        private readonly Dictionary<int, int> jettingMyceliumAlreadyOwned = new();
        private readonly Dictionary<int, int> jettingMyceliumColonized = new();
        private readonly Dictionary<int, int> jettingMyceliumToxified = new();
        private readonly Dictionary<int, int> jettingMyceliumInvalid = new();
        private readonly Dictionary<int, int> jettingMyceliumPoisoned = new();

        public void ReportJettingMyceliumInfested(int playerId, int count)
        {
            if (!jettingMyceliumInfested.ContainsKey(playerId))
                jettingMyceliumInfested[playerId] = 0;
            jettingMyceliumInfested[playerId] += count;
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
        public void ReportJettingMyceliumColonized(int playerId, int count)
        {
            if (!jettingMyceliumColonized.ContainsKey(playerId))
                jettingMyceliumColonized[playerId] = 0;
            jettingMyceliumColonized[playerId] += count;
        }
        public void ReportJettingMyceliumToxified(int playerId, int count)
        {
            if (!jettingMyceliumToxified.ContainsKey(playerId))
                jettingMyceliumToxified[playerId] = 0;
            jettingMyceliumToxified[playerId] += count;
        }
        public void ReportJettingMyceliumInvalid(int playerId, int count)
        {
            if (!jettingMyceliumInvalid.ContainsKey(playerId))
                jettingMyceliumInvalid[playerId] = 0;
            jettingMyceliumInvalid[playerId] += count;
        }
        public void ReportJettingMyceliumPoisoned(int playerId, int count)
        {
            if (!jettingMyceliumPoisoned.ContainsKey(playerId))
                jettingMyceliumPoisoned[playerId] = 0;
            jettingMyceliumPoisoned[playerId] += count;
        }

        public int GetJettingMyceliumInfested(int playerId) => jettingMyceliumInfested.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumReclaimed(int playerId) => jettingMyceliumReclaimed.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumCatabolicGrowth(int playerId) => jettingMyceliumCatabolicGrowth.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumAlreadyOwned(int playerId) => jettingMyceliumAlreadyOwned.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumColonized(int playerId) => jettingMyceliumColonized.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumToxified(int playerId) => jettingMyceliumToxified.TryGetValue(playerId, out var val) ? val : 0;
        public int GetJettingMyceliumPoisoned(int playerId) => jettingMyceliumPoisoned.TryGetValue(playerId, out var val) ? val : 0;

        // ────────────────
        // Failed Growths
        // ────────────────

        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();

        private readonly Dictionary<int, int> standardGrowthsByPlayer = new();
        public void RecordStandardGrowth(int playerId)
        {
            if (!standardGrowthsByPlayer.ContainsKey(playerId))
                standardGrowthsByPlayer[playerId] = 0;
            standardGrowthsByPlayer[playerId]++;
        }

        public int GetStandardGrowths(int playerId)
        {
            return standardGrowthsByPlayer.TryGetValue(playerId, out var count) ? count : 0;
        }

        // ────────────────
        // Mutator Phenotype Upgrades (for UI logging only - not tracked in simulations)
        // ────────────────
        public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName)
        {
        }

        public void RecordSpecificMutationUpgrade(int playerId, string mutationName)
        {
        }

        public void RecordRetrogradeBloomUpgrade(int playerId, string evolvedMutationName, string devolvedMutationSummary, int devolvedPoints)
        {
        }
    }
}
