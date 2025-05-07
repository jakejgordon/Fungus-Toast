using System.Collections.Generic;
using FungusToast.Core.Mutations;

namespace FungusToast.UI.MutationTree
{
    public static class MutationLayoutProvider
    {
        public static Dictionary<int, MutationLayoutMetadata> GetDefaultLayout()
        {
            return new Dictionary<int, MutationLayoutMetadata>
            {
                // Growth
                { 0, new MutationLayoutMetadata(0, 0, MutationCategory.Growth) },         // Mycelial Bloom
                { 6, new MutationLayoutMetadata(0, 1, MutationCategory.Growth) },         // Tendril Northwest
                { 7, new MutationLayoutMetadata(0, 2, MutationCategory.Growth) },         // Tendril Northeast
                { 8, new MutationLayoutMetadata(0, 3, MutationCategory.Growth) },         // Tendril Southeast
                { 9, new MutationLayoutMetadata(0, 4, MutationCategory.Growth) },         // Tendril Southwest

                // Defense
                { 1, new MutationLayoutMetadata(1, 0, MutationCategory.CellularResilience) }, // Homeostatic Harmony
                { 4, new MutationLayoutMetadata(1, 1, MutationCategory.CellularResilience) }, // Chronoresilient Cytoplasm
                { 11, new MutationLayoutMetadata(1, 2, MutationCategory.CellularResilience) }, // Necrosporulation (dependent)

                // Offense
                { 2, new MutationLayoutMetadata(2, 0, MutationCategory.Fungicide) },      // Silent Blight
                { 5, new MutationLayoutMetadata(2, 1, MutationCategory.Fungicide) },      // Encysted Spores (dependent)

                // Utility
                { 3, new MutationLayoutMetadata(3, 0, MutationCategory.GeneticDrift) },   // Adaptive Expression
                { 10, new MutationLayoutMetadata(3, 1, MutationCategory.GeneticDrift) },  // Mutator Phenotype
            };
        }
    }
}
