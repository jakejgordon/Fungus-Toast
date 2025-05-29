using FungusToast.Core.Death;

namespace FungusToast.Simulation.Models
{
    public class PlayerResult
    {
        public int PlayerId { get; set; }
        public string StrategyName { get; set; }
        public int LivingCells { get; set; }
        public int DeadCells { get; set; }
        public int ReclaimedCells { get; set; }
        public Dictionary<int, int> MutationLevels { get; set; } = new();

        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public float OffensiveDecayModifier { get; set; }

        public List<DeathReason> DeadCellDeathReasons { get; set; } = new();

        // 🧬 New for Creeping Mold tracking
        public int CreepingMoldMoves { get; set; }

        // 🌱 New for Spore tracking
        public int SporocidalSpores { get; set; }
        public int NecroSpores { get; set; }
    }
}
