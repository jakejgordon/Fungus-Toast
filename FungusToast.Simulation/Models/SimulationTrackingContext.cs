using System.Collections.Generic;
using FungusToast.Core.Core.Metrics;
using FungusToast.Core.Metrics;

namespace FungusToast.Simulation.Models
{
    public class SimulationTrackingContext : IGrowthObserver, ISporeDropObserver
    {
        private readonly Dictionary<int, int> creepingMoldMoves = new();
        private readonly Dictionary<int, int> reclaimedCells = new();
        private readonly Dictionary<int, int> mycotoxinTracerSporeDrops = new();
        private readonly Dictionary<int, int> sporocidalSporeDrops = new();
        private readonly Dictionary<int, int> necrosporeDrops = new();
        private readonly Dictionary<int, int> necrophyticBloomSpores = new();
        private readonly Dictionary<int, int> necrophyticBloomReclaims = new();

        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();

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

        public int GetCreepingMoldMoves(int playerId) =>
            creepingMoldMoves.TryGetValue(playerId, out var val) ? val : 0;

        public int GetReclaimedCells(int playerId) =>
            reclaimedCells.TryGetValue(playerId, out var val) ? val : 0;

        public int GetNecrophyticBloomSpores(int playerId) =>
            necrophyticBloomSpores.TryGetValue(playerId, out var val) ? val : 0;

        public int GetNecrophyticBloomReclaims(int playerId) =>
            necrophyticBloomReclaims.TryGetValue(playerId, out var val) ? val : 0;

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
            if (!necrosporeDrops.ContainsKey(playerId))
                necrosporeDrops[playerId] = 0;

            necrosporeDrops[playerId] += count;
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

        public void RecordFailedGrowth(int playerId)
        {
            if (!FailedGrowthsByPlayerId.ContainsKey(playerId))
                FailedGrowthsByPlayerId[playerId] = 0;

            FailedGrowthsByPlayerId[playerId]++;
        }

        public int GetFailedGrowthCount(int playerId) =>
            FailedGrowthsByPlayerId.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetSporocidalSpores() => new(sporocidalSporeDrops);
        public Dictionary<int, int> GetNecroSpores() => new(necrosporeDrops);
        public Dictionary<int, int> GetNecrophyticBloomSpores() => new(necrophyticBloomSpores);
        public Dictionary<int, int> GetNecrophyticBloomReclaims() => new(necrophyticBloomReclaims);
        public Dictionary<int, int> GetMycotoxinTracerSpores() => new(mycotoxinTracerSporeDrops);

        public int GetSporocidalSporeDropCount(int playerId) =>
            sporocidalSporeDrops.TryGetValue(playerId, out var val) ? val : 0;

        public int GetNecrosporulationSporeDropCount(int playerId) =>
            necrosporeDrops.TryGetValue(playerId, out var val) ? val : 0;

        public int GetMycotoxinTracerSporeDropCount(int playerId) =>
            mycotoxinTracerSporeDrops.TryGetValue(playerId, out var val) ? val : 0;

    }
}
