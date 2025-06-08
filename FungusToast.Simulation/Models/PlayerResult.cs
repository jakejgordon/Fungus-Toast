using FungusToast.Core.AI;
using FungusToast.Core.Death;
using System.Collections.Generic;

namespace FungusToast.Simulation.Models
{
    public class PlayerResult
    {
        // ──────────────
        // CORE IDENTITY
        // ──────────────
        public int PlayerId { get; set; }
        public required IMutationSpendingStrategy Strategy { get; set; }
        public string StrategyName { get; set; } = string.Empty;

        // ──────────────
        // CELL COUNTS
        // ──────────────
        public int LivingCells { get; set; }
        public int DeadCells { get; set; }
        public int ReclaimedCells { get; set; }
        public List<DeathReason> DeadCellDeathReasons { get; set; } = new();

        // ──────────────
        // MUTATIONS
        // ──────────────
        public Dictionary<int, int> MutationLevels { get; set; } = new();

        // ──────────────
        // EFFECTIVE STATS
        // ──────────────
        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public float OffensiveDecayModifier { get; set; }

        // ──────────────
        // MUTATION EFFECT COUNTERS
        // ──────────────
        public int CreepingMoldMoves { get; set; }
        public int NecrosporulationSpores { get; set; }
        public int SporocidalSpores { get; set; }
        public int NecrophyticSpores { get; set; }
        public int NecrophyticReclaims { get; set; }
        public int MycotoxinTracerSpores { get; set; }
        public int MycotoxinCatabolisms { get; set; }
        public int CatabolizedMutationPoints { get; set; }
        public int ToxinAuraKills { get; set; }

        // ──────────────
        // FREE MUTATION POINTS (SPLIT BY SOURCE)
        // ──────────────

        /// <summary>
        /// Free mutation points earned from Mutator Phenotype.
        /// </summary>
        public int MutatorPhenotypePointsEarned { get; set; }

        /// <summary>
        /// Free mutation points earned from Hyperadaptive Drift.
        /// </summary>
        public int HyperadaptiveDriftPointsEarned { get; set; }
    }
}
