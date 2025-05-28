using System.Collections.Generic;
using FungusToast.Core.Phases;

namespace FungusToast.Simulation.Models
{
    public class SimulationTrackingContext : IGrowthObserver
    {
        private readonly Dictionary<int, int> creepingMoldMoves = new();
        private readonly Dictionary<int, int> reclaimedCells = new();

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
    }
}
