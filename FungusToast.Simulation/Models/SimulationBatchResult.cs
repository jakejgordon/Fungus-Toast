using FungusToast.Core.Death;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FungusToast.Simulation.Models
{
    public class SimulationBatchResult
    {
        public List<GameResult> GameResults { get; set; } = new List<GameResult>();
        public Dictionary<DeathReason, int> CumulativeDeathReasons { get; set; } = new Dictionary<DeathReason, int>();
    }
}
