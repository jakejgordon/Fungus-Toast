using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Simulation.Models
{
    public class InvariantCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public int Expected { get; set; }
        public int Actual { get; set; }
        public bool IsMatch => Expected == Actual;
    }

    public class ParityInvariantReport
    {
        public int CompletedRounds { get; set; }
        public int TotalGrowthCyclesPerRound { get; set; }
        public List<InvariantCheckResult> Checks { get; set; } = new();
        public bool AllPassed => Checks.All(c => c.IsMatch);
    }
}
