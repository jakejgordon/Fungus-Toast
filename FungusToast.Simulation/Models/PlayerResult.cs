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
        // FINAL CELL COUNTS (GAME END)
        // ──────────────
        public int LivingCells { get; set; }
        public int DeadCells { get; set; }

        // ──────────────
        // DEATH REASONS (GAME END)
        // ──────────────
        public List<DeathReason> DeadCellDeathReasons { get; set; } = new();
        public Dictionary<DeathReason, int> DeathsByReason { get; set; } = new();

        public int DeathsFromRandomness { get; set; }
        public int DeathsFromAge { get; set; }

        // ──────────────
        // MUTATION TREE STATE
        // ──────────────
        public Dictionary<int, int> MutationLevels { get; set; } = new();

        // ──────────────
        // MYCOVARIANTS
        // ──────────────
        public List<MycovariantResult> Mycovariants { get; set; } = new();

        // ──────────────
        // EFFECTIVE STATS
        // ──────────────
        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public float OffensiveDecayModifier { get; set; }

        // ──────────────
        // MUTATION EFFECT COUNTERS (RUNNING TOTALS)
        // ──────────────

        // Core mutation event metrics
        public int RegenerativeHyphaeReclaims { get; set; }    // NEW: Regenerative Hyphae event-based
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

        // ──────────────
        // MYCOVARIANT EFFECT COUNTERS
        // ──────────────
        public int JettingMyceliumKills { get; set; }

        /// <summary>
        /// Number of cells reclaimed using Necrotoxic Conversion (Tier 5 Fungicide).
        /// </summary>
        public int NecrotoxicConversionReclaims { get; set; }
        public int CatabolicRebirthResurrections { get; set; }
        public int HyphalSurgeGrowths { get; set; }
        public int HyphalVectoringGrowths { get; set; }
        public int HyphalVectoringInfested { get; set; }
        public int HyphalVectoringReclaimed { get; set; }
        public int HyphalVectoringCatabolicGrowth { get; set; }
        public int HyphalVectoringAlreadyOwned { get; set; }
        public int HyphalVectoringColonized { get; set; }
        public int HyphalVectoringInvalid { get; set; }
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
        public int AnabolicInversionPointsEarned { get; set; }

        // ──────────────
        // MUTATION POINT ECONOMY
        // ──────────────
        public int MutationPointIncome { get; set; }
        public Dictionary<MutationTier, int> MutationPointsSpentByTier { get; set; } = new();
        public int TotalMutationPointsSpent { get; set; }
        public int BankedPoints { get; set; }

        public int CatabolicRebirthAgedToxins { get; set; }

        // --- Creeping Mold special effect counters ---
        public int CreepingMoldToxinJumps { get; set; }

        public int PutrefactiveRejuvenationCyclesReduced { get; set; }

        public int PerimeterProliferatorGrowths { get; set; }

        /// <summary>
        /// The average AI score at draft time for all mycovariants picked by this player (AI only).
        /// </summary>
        public float? AvgAIScoreAtDraft { get; set; }

        /// <summary>
        /// Tracks which preferred mycovariants this player actually received vs wanted.
        /// Key: Preferred mycovariant ID, Value: Whether player got it (true/false).
        /// </summary>
        public Dictionary<int, bool> PreferredMycovariantResults { get; set; } = new();
    }
}
