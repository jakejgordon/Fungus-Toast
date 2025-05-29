using System.Collections.Generic;
using FungusToast.Core.Core.Metrics;
using FungusToast.Core.Metrics;

namespace FungusToast.Simulation.Models
{
    public class SimulationTrackingContext : IGrowthObserver, ISporeDropObserver
    {
        private readonly Dictionary<int, int> creepingMoldMoves = new();
        private readonly Dictionary<int, int> reclaimedCells = new();
        private readonly Dictionary<int, int> sporocidalSporeDrops = new();
        private readonly Dictionary<int, int> necrosporeDrops = new();

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

        public int GetSporocidalSporeDropCount(int playerId) =>
            sporocidalSporeDrops.TryGetValue(playerId, out var val) ? val : 0;

        public int GetNecrosporeDropCount(int playerId) =>
            necrosporeDrops.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetSporocidalSpores() => new(sporocidalSporeDrops);

        public Dictionary<int, int> GetNecroSpores() => new(necrosporeDrops);
    }
}
