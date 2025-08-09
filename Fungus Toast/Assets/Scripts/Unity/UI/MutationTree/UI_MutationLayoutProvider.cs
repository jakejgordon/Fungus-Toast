using System.Collections.Generic;
using FungusToast.Core.Mutations;

namespace FungusToast.Unity.UI.MutationTree
{
    public static class UI_MutationLayoutProvider
    {
        /// <summary>
        /// Column order: 0-Growth, 1-Cellular Resilience, 2-Fungicide, 3-Genetic Drift  
        /// Row index increments downward within each column.
        /// </summary>
        public static Dictionary<int, MutationLayoutMetadata> GetDefaultLayout() =>
            new Dictionary<int, MutationLayoutMetadata>
            {
                /* ---------------- Growth (col 0) -------------------- */
                { MutationIds.MycelialBloom,         new MutationLayoutMetadata(0, 0, MutationCategory.Growth) },
                { MutationIds.TendrilNorthwest,      new MutationLayoutMetadata(0, 1, MutationCategory.Growth) },
                { MutationIds.TendrilNortheast,      new MutationLayoutMetadata(0, 2, MutationCategory.Growth) },
                { MutationIds.TendrilSoutheast,      new MutationLayoutMetadata(0, 3, MutationCategory.Growth) },
                { MutationIds.TendrilSouthwest,      new MutationLayoutMetadata(0, 4, MutationCategory.Growth) },
                { MutationIds.MycotropicInduction,   new MutationLayoutMetadata(0, 5, MutationCategory.Growth) },
                { MutationIds.CreepingMold,          new MutationLayoutMetadata(0, 6, MutationCategory.Growth) },

                /* ------------ Cellular Resilience (col 1) ----------- */
                { MutationIds.HomeostaticHarmony,       new MutationLayoutMetadata(1, 0, MutationCategory.CellularResilience) },
                { MutationIds.ChronoresilientCytoplasm, new MutationLayoutMetadata(1, 1, MutationCategory.CellularResilience) },
                { MutationIds.Necrosporulation,         new MutationLayoutMetadata(1, 2, MutationCategory.CellularResilience) },
                { MutationIds.RegenerativeHyphae,       new MutationLayoutMetadata(1, 3, MutationCategory.CellularResilience) },
                { MutationIds.NecrohyphalInfiltration,  new MutationLayoutMetadata(1, 4, MutationCategory.CellularResilience) },
                { MutationIds.CatabolicRebirth,         new MutationLayoutMetadata(1, 5, MutationCategory.CellularResilience) },
                { MutationIds.HypersystemicRegeneration, new MutationLayoutMetadata(1, 6, MutationCategory.CellularResilience) },

                /* ---------------- Fungicide (col 2) ----------------- */
                { MutationIds.MycotoxinTracer,           new MutationLayoutMetadata(2, 0, MutationCategory.Fungicide) },
                { MutationIds.MycotoxinPotentiation,     new MutationLayoutMetadata(2, 1, MutationCategory.Fungicide) },
                { MutationIds.PutrefactiveMycotoxin,     new MutationLayoutMetadata(2, 2, MutationCategory.Fungicide) },
                { MutationIds.SporocidalBloom,           new MutationLayoutMetadata(2, 3, MutationCategory.Fungicide) },
                { MutationIds.PutrefactiveRejuvenation,  new MutationLayoutMetadata(2, 4, MutationCategory.Fungicide) },
                { MutationIds.NecrotoxicConversion,      new MutationLayoutMetadata(2, 5, MutationCategory.Fungicide) },
                { MutationIds.PutrefactiveCascade,       new MutationLayoutMetadata(2, 6, MutationCategory.Fungicide) },

                /* --------------- Genetic Drift (col 3) -------------- */
                { MutationIds.MutatorPhenotype,       new MutationLayoutMetadata(3, 0, MutationCategory.GeneticDrift) },
                { MutationIds.AdaptiveExpression,     new MutationLayoutMetadata(3, 1, MutationCategory.GeneticDrift) },
                { MutationIds.MycotoxinCatabolism,    new MutationLayoutMetadata(3, 2, MutationCategory.GeneticDrift) }, 
                { MutationIds.AnabolicInversion,      new MutationLayoutMetadata(3, 3, MutationCategory.GeneticDrift) },
                { MutationIds.NecrophyticBloom,       new MutationLayoutMetadata(3, 4, MutationCategory.GeneticDrift) },
                { MutationIds.HyperadaptiveDrift,     new MutationLayoutMetadata(3, 5, MutationCategory.GeneticDrift) },
                { MutationIds.OntogenicRegression,    new MutationLayoutMetadata(3, 6, MutationCategory.GeneticDrift) },

                /* ---------------Mycelial Surge (col 4) -------------- */
                { MutationIds.HyphalSurge,            new MutationLayoutMetadata(4, 0, MutationCategory.MycelialSurges) },
                { MutationIds.HyphalVectoring,        new MutationLayoutMetadata(4, 1, MutationCategory.MycelialSurges) },
                { MutationIds.ChitinFortification,    new MutationLayoutMetadata(4, 2, MutationCategory.MycelialSurges) },
                { MutationIds.MimeticResilience,      new MutationLayoutMetadata(4, 3, MutationCategory.MycelialSurges) },
            };
    }
}
