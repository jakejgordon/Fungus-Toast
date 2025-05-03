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
                // Defense
                { 1, new MutationLayoutMetadata(1, 0, MutationCategory.CellularResilience) }, // Homeostatic Harmony
                // Offense
                { 2, new MutationLayoutMetadata(2, 0, MutationCategory.Fungicide) },      // Silent Blight
                { 4, new MutationLayoutMetadata(2, 1, MutationCategory.Fungicide) },      // Encysted Spores (dependent)
                // Utility
                { 3, new MutationLayoutMetadata(3, 0, MutationCategory.GeneticDrift) },   // Adaptive Expression
            };
        }
    }
}
