using System.Collections.Generic;

namespace FungusToast.Simulation.GameSimulation.Models
{
    public class PlayerResult
    {
        public int PlayerId { get; set; }
        public string StrategyName { get; set; }
        public int LivingCells { get; set; }
        public int DeadCells { get; set; }
        public Dictionary<int, int> MutationLevels { get; set; } = new();

        // New: Derived metrics for simulation analysis
        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public float OffensiveDecayModifier { get; set; }
    }
}
