using System.Collections.Generic;
using FungusToast.Core.Core.Metrics;
using FungusToast.Core.Metrics;

namespace FungusToast.Simulation.Models
{
    public class SimulationTrackingContext : IGrowthObserver, ISporeDropObserver, IMutationPointObserver
    {
        // ────────────────────────────
        //  FIELDS / DATA TRACKERS
        // ────────────────────────────

        private readonly Dictionary<int, int> creepingMoldMoves = new();
        private readonly Dictionary<int, int> reclaimedCells = new();
        private readonly Dictionary<int, int> mycotoxinTracerSporeDrops = new();
        private readonly Dictionary<int, int> sporocidalSporeDrops = new();
        private readonly Dictionary<int, int> necrosporulationSporeDrops = new();
        private readonly Dictionary<int, int> necrophyticBloomSpores = new();
        private readonly Dictionary<int, int> necrophyticBloomReclaims = new();
        private readonly Dictionary<int, int> toxinAuraKills = new();
        private readonly Dictionary<int, int> toxinCatabolisms = new();
        private readonly Dictionary<int, int> catabolizedMutationPoints = new();

        // NEW: Separate tracking for mutation point sources
        private readonly Dictionary<int, int> mutatorPhenotypePointsEarned = new();
        private readonly Dictionary<int, int> hyperadaptiveDriftPointsEarned = new();

        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();

        // ────────────────────────────
        //  MUTATORS / TRACKING METHODS
        // ────────────────────────────

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

        public void ReportAuraKill(int playerId, int killCount)
        {
            if (!toxinAuraKills.ContainsKey(playerId))
                toxinAuraKills[playerId] = 0;
            toxinAuraKills[playerId] += killCount;
        }

        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int mutationPointsCatabolized)
        {
            // Track number of toxins catabolized
            if (!toxinCatabolisms.ContainsKey(playerId))
                toxinCatabolisms[playerId] = 0;
            toxinCatabolisms[playerId] += toxinsCatabolized;

            // Track number of mutation points earned via catabolism
            if (!catabolizedMutationPoints.ContainsKey(playerId))
                catabolizedMutationPoints[playerId] = 0;
            catabolizedMutationPoints[playerId] += mutationPointsCatabolized;
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

        // ────────────────────────────
        //  GETTERS / ACCESSORS
        // ────────────────────────────

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

        public int GetMycotoxinSporeDropCount(int playerId) =>
            mycotoxinTracerSporeDrops.TryGetValue(playerId, out var val) ? val : 0;

        public int GetToxinAuraKillCount(int playerId) =>
            toxinAuraKills.TryGetValue(playerId, out var val) ? val : 0;

        public int GetCatabolizedMutationPoints(int playerId) =>
            catabolizedMutationPoints.TryGetValue(playerId, out var val) ? val : 0;

        public int GetMutatorPhenotypePointsEarned(int playerId) =>
            mutatorPhenotypePointsEarned.TryGetValue(playerId, out var val) ? val : 0;

        public int GetHyperadaptiveDriftPointsEarned(int playerId) =>
            hyperadaptiveDriftPointsEarned.TryGetValue(playerId, out var val) ? val : 0;

        // Bulk accessors for summaries/statistics
        public Dictionary<int, int> GetSporocidalSpores() => new(sporocidalSporeDrops);
        public Dictionary<int, int> GetNecroSpores() => new(necrosporulationSporeDrops);
        public Dictionary<int, int> GetNecrophyticBloomSpores() => new(necrophyticBloomSpores);
        public Dictionary<int, int> GetNecrophyticBloomReclaims() => new(necrophyticBloomReclaims);
        public Dictionary<int, int> GetMycotoxinTracerSporeDrops() => new(mycotoxinTracerSporeDrops);
        public Dictionary<int, int> GetToxinAuraKills() => new(toxinAuraKills);
        public Dictionary<int, int> GetToxinCatabolisms() => new(toxinCatabolisms);
        public Dictionary<int, int> GetCatabolizedMutationPoints() => new(catabolizedMutationPoints);

        public Dictionary<int, int> GetAllMutatorPhenotypePointsEarned() => new(mutatorPhenotypePointsEarned);
        public Dictionary<int, int> GetAllHyperadaptiveDriftPointsEarned() => new(hyperadaptiveDriftPointsEarned);
    }
}
