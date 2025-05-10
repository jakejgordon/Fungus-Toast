using System.Collections.Generic;
using FungusToast.Core.Mutations;

namespace FungusToast.Unity.UI.MutationTree
{
    public static class MutationLayoutProvider
    {
        /// <summary>
        /// Column order: 0-Growth, 1-Cellular Resilience, 2-Fungicide, 3-Genetic Drift  
        /// Row index increments downward within each column.
        /// </summary>
        public static Dictionary<int, MutationLayoutMetadata> GetDefaultLayout() =>
            new Dictionary<int, MutationLayoutMetadata>
            {
                /* ---------------- Growth (col 0) -------------------- */
                { MutationManager.MutationIds.MycelialBloom,       new MutationLayoutMetadata(0, 0, MutationCategory.Growth) },
                { MutationManager.MutationIds.TendrilNorthwest,    new MutationLayoutMetadata(0, 1, MutationCategory.Growth) },
                { MutationManager.MutationIds.TendrilNortheast,    new MutationLayoutMetadata(0, 2, MutationCategory.Growth) },
                { MutationManager.MutationIds.TendrilSoutheast,    new MutationLayoutMetadata(0, 3, MutationCategory.Growth) },
                { MutationManager.MutationIds.TendrilSouthwest,    new MutationLayoutMetadata(0, 4, MutationCategory.Growth) },
                { MutationManager.MutationIds.MycotropicInduction, new MutationLayoutMetadata(0, 5, MutationCategory.Growth) },

                /* ------------ Cellular Resilience (col 1) ----------- */
                { MutationManager.MutationIds.HomeostaticHarmony,       new MutationLayoutMetadata(1, 0, MutationCategory.CellularResilience) },
                { MutationManager.MutationIds.ChronoresilientCytoplasm, new MutationLayoutMetadata(1, 1, MutationCategory.CellularResilience) },
                { MutationManager.MutationIds.Necrosporulation,         new MutationLayoutMetadata(1, 2, MutationCategory.CellularResilience) },

                /* ---------------- Fungicide (col 2) ----------------- */
                { MutationManager.MutationIds.SilentBlight,         new MutationLayoutMetadata(2, 0, MutationCategory.Fungicide) },
                { MutationManager.MutationIds.EncystedSpores,       new MutationLayoutMetadata(2, 1, MutationCategory.Fungicide) },
                { MutationManager.MutationIds.PutrefactiveMycotoxin,new MutationLayoutMetadata(2, 2, MutationCategory.Fungicide) }, // NEW

                /* --------------- Genetic Drift (col 3) -------------- */
                { MutationManager.MutationIds.AdaptiveExpression, new MutationLayoutMetadata(3, 0, MutationCategory.GeneticDrift) },
                { MutationManager.MutationIds.MutatorPhenotype,   new MutationLayoutMetadata(3, 1, MutationCategory.GeneticDrift) },
            };
    }
}
