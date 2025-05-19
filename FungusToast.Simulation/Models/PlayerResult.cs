using System.Collections.Generic;
using FungusToast.Core.Death;

namespace FungusToast.Simulation.GameSimulation.Models
{
    public class PlayerResult
    {
        public int PlayerId { get; set; }
        public string StrategyName { get; set; }
        public int LivingCells { get; set; }
        public int DeadCells { get; set; }

        // 🧠 New: Total number of reclaimed cells
        public int ReclaimedCells { get; set; }

        public Dictionary<int, int> MutationLevels { get; set; } = new();

        // Derived metrics for simulation analysis
        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public float OffensiveDecayModifier { get; set; }

        // 🔍 Track cause of death for all dead cells this game
        public List<DeathReason> DeadCellDeathReasons { get; set; } = new();
    }
}
