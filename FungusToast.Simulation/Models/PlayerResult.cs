using FungusToast.Core.AI;
using FungusToast.Core.Death;
using System.Collections.Generic;

namespace FungusToast.Simulation.Models
{
    public class PlayerResult
    {
        public int PlayerId { get; set; }
        public required IMutationSpendingStrategy Strategy { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public int LivingCells { get; set; }
        public int DeadCells { get; set; }
        public int ReclaimedCells { get; set; }
        public Dictionary<int, int> MutationLevels { get; set; } = new();

        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public float OffensiveDecayModifier { get; set; }

        public List<DeathReason> DeadCellDeathReasons { get; set; } = new();

        public int CreepingMoldMoves { get; set; }

        public int NecrosporulationSpores { get; set; }
        public int SporocidalSpores { get; set; }

        public int NecrophyticSpores { get; set; }
        public int NecrophyticReclaims { get; set; }

        public int MycotoxinTracerSpores { get; set; }
        public int ToxinAuraKills { get; set; }
    }
}
