using FungusToast.Core.AI;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
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
        public Dictionary<DeathReason, int> DeathsByReason { get; set; } = new();

        /// <summary>
        /// Number of cells this player lost due to random (baseline) death.
        /// </summary>
        public int DeathsFromRandomness { get; set; }

        /// <summary>
        /// Number of cells this player lost due to death by age.
        /// </summary>
        public int DeathsFromAge { get; set; }

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
        public int SporocidalKills { get; set; }
        public int NecrophyticSpores { get; set; }
        public int NecrophyticReclaims { get; set; }
        public int MycotoxinTracerSpores { get; set; }
        public int MycotoxinCatabolisms { get; set; }
        public int CatabolizedMutationPoints { get; set; }
        public int ToxinAuraKills { get; set; }
        public int NecrohyphalInfiltrations { get; set; }
        public int NecrohyphalCascades { get; set; }
        public int PutrefactiveMycotoxinKills { get; set; }

        /// <summary>
        /// Number of cells reclaimed using Necrotoxic Conversion (Tier 5 Fungicide).
        /// </summary>
        public int NecrotoxicConversionReclaims { get; set; }

        /// <summary>
        /// Number of successful cell growths attributed to Hyphal Surge mutation.
        /// </summary>
        public int HyphalSurgeGrowths { get; set; }

        /// <summary>
        /// Number of living fungal cells created by Hyphal Vectoring surge mutation.
        /// </summary>
        public int HyphalVectoringGrowths { get; set; } // <--- NEW FIELD

        // ──────────────
        // TENDRIL GROWN CELL COUNTERS
        // ──────────────
        public int TendrilNorthwestGrownCells { get; set; }
        public int TendrilNortheastGrownCells { get; set; }
        public int TendrilSoutheastGrownCells { get; set; }
        public int TendrilSouthwestGrownCells { get; set; }

        // ──────────────
        // FREE MUTATION POINTS (SPLIT BY SOURCE)
        // ──────────────
        public int AdaptiveExpressionPointsEarned { get; set; }
        public int MutatorPhenotypePointsEarned { get; set; }
        public int HyperadaptiveDriftPointsEarned { get; set; }

        // ──────────────
        // MUTATION POINT INCOME/SPENDING (NEW)
        // ──────────────

        /// <summary>
        /// Total mutation points awarded to this player (all sources, including bonuses).
        /// </summary>
        public int MutationPointIncome { get; set; }

        /// <summary>
        /// Mutation points spent by tier (key: MutationTier, value: total points spent at that tier).
        /// </summary>
        public Dictionary<MutationTier, int> MutationPointsSpentByTier { get; set; } = new();

        /// <summary>
        /// Total mutation points spent (across all tiers).
        /// </summary>
        public int TotalMutationPointsSpent { get; set; }
    }
}
