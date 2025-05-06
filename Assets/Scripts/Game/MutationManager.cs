using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core;
using FungusToast.Core.Config;

namespace FungusToast.Game
{
    public class MutationManager : MonoBehaviour
    {
        private Dictionary<int, Mutation> rootMutations = new();
        public IReadOnlyDictionary<int, Mutation> RootMutations => rootMutations;

        private Dictionary<int, Mutation> allMutations = new();
        public IReadOnlyDictionary<int, Mutation> AllMutations => allMutations;

        public static class MutationIds
        {
            public const int MycelialBloom = 0;
            public const int HomeostaticHarmony = 1;
            public const int SilentBlight = 2;
            public const int AdaptiveExpression = 3;
            public const int ChronoresilientCytoplasm = 4;
            public const int EncystedSpores = 5;
            public const int TendrilNorthwest = 6;
            public const int TendrilNortheast = 7;
            public const int TendrilSoutheast = 8;
            public const int TendrilSouthwest = 9;
        }

        private void Awake()
        {
            InitializeMutations();
        }

        private void InitializeMutations()
        {
            var mycelialBloom = new Mutation(
                id: MutationIds.MycelialBloom,
                name: "Mycelial Bloom",
                description: $"Mycelium metabolizes carbohydrates at an accelerated rate, enhancing hyphal tip extension. " +
                             $"Grants an additional +{(0.005f * 100f):F1}% chance to spread mold into adjacent cells each round.",
                type: MutationType.GrowthChance,
                effectPerLevel: 0.005f,
                maxLevel: 100,
                category: MutationCategory.Growth
            );
            rootMutations[mycelialBloom.Id] = mycelialBloom;
            allMutations[mycelialBloom.Id] = mycelialBloom;

            var homeostaticHarmony = new Mutation(
                id: MutationIds.HomeostaticHarmony,
                name: "Homeostatic Harmony",
                description: $"Internal turgor pressure and nutrient recycling mechanisms bolster cell viability under adverse conditions. " +
                             $"Provides an additional +{(0.0025f * 100f):F1}% chance for your mold cells to survive decay events.",
                type: MutationType.DefenseSurvival,
                effectPerLevel: 0.0025f,
                maxLevel: 100,
                category: MutationCategory.CellularResilience
            );
            rootMutations[homeostaticHarmony.Id] = homeostaticHarmony;
            allMutations[homeostaticHarmony.Id] = homeostaticHarmony;

            var silentBlight = new Mutation(
                id: MutationIds.SilentBlight,
                name: "Silent Blight",
                description: $"Secondary metabolites subtly disrupt competing organisms' cellular respiration and membrane integrity. " +
                             $"Increases the chance by +{(0.0025f * 100f):F1}% that enemy mold cells will die each growth round.",
                type: MutationType.EnemyDecayChance,
                effectPerLevel: 0.0025f,
                maxLevel: 100,
                category: MutationCategory.Fungicide
            );
            rootMutations[silentBlight.Id] = silentBlight;
            allMutations[silentBlight.Id] = silentBlight;

            var adaptiveExpression = new Mutation(
                id: MutationIds.AdaptiveExpression,
                name: "Adaptive Expression",
                description: $"Epigenetic modifications increase genomic variability, accelerating adaptive potential. " +
                             $"Adds a +{(0.10f * 100f):F1}% chance to gain a bonus mutation point at the start of each turn.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: 0.10f,
                maxLevel: 10,
                category: MutationCategory.GeneticDrift
            );
            rootMutations[adaptiveExpression.Id] = adaptiveExpression;
            allMutations[adaptiveExpression.Id] = adaptiveExpression;

            var chronoresilientCytoplasm = new Mutation(
                id: MutationIds.ChronoresilientCytoplasm,
                name: "Chronoresilient Cytoplasm",
                description: $"Cells that endure long enough undergo cytoplasmic rejuvenation. " +
                             $"When a mold cell survives for a number of cycles equal to (50 − 5 × Level), its age resets to 0 instead of increasing Mycelial Degradation risk.",
                type: MutationType.SelfAgeResetThreshold,
                effectPerLevel: 5f,
                maxLevel: 5,
                category: MutationCategory.CellularResilience
            );
            chronoresilientCytoplasm.RequiredMutation = homeostaticHarmony;
            chronoresilientCytoplasm.RequiredLevel = 10;
            homeostaticHarmony.Children.Add(chronoresilientCytoplasm);
            allMutations[chronoresilientCytoplasm.Id] = chronoresilientCytoplasm;

            var encystedSpores = new Mutation(
                id: MutationIds.EncystedSpores,
                name: "Encysted Spores",
                description: $"Dormant spores deploy enzymatic agents that accelerate necrosis in isolated enemies. " +
                             $"Increases Silent Blight's decay penalty by +{(0.05f * 100f):F1}% when enemy cells are fully surrounded by living fungal cells.",
                type: MutationType.EncystedSporeMultiplier,
                effectPerLevel: 0.05f,
                maxLevel: 5,
                category: MutationCategory.Fungicide
            );
            encystedSpores.RequiredMutation = silentBlight;
            encystedSpores.RequiredLevel = 10;
            silentBlight.Children.Add(encystedSpores);
            allMutations[encystedSpores.Id] = encystedSpores;

            // Diagonal Growth Mutations
            CreateDiagonalGrowthMutation(MutationIds.TendrilNorthwest, "Tendril Northwest", MutationType.GrowthDiagonal_NW, mycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilNortheast, "Tendril Northeast", MutationType.GrowthDiagonal_NE, mycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilSoutheast, "Tendril Southeast", MutationType.GrowthDiagonal_SE, mycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilSouthwest, "Tendril Southwest", MutationType.GrowthDiagonal_SW, mycelialBloom);
        }

        private void CreateDiagonalGrowthMutation(int id, string name, MutationType type, Mutation required)
        {
            string direction = name.Split(' ')[1];
            var mutation = new Mutation(
                id: id,
                name: name,
                description: $"Specialized hyphae reach into {direction.ToLower()} territory. " +
                             $"Adds +1.0% chance per level to grow mold {direction.ToLower()}.",
                type: type,
                effectPerLevel: 0.01f,
                maxLevel: 10,
                category: MutationCategory.Growth
            );
            mutation.RequiredMutation = required;
            mutation.RequiredLevel = 10;
            required.Children.Add(mutation);
            allMutations[mutation.Id] = mutation;
        }

        public Mutation GetMutationById(int id)
        {
            return allMutations.TryGetValue(id, out var mutation) ? mutation : null;
        }

        public IReadOnlyCollection<Mutation> GetAllMutations()
        {
            return allMutations.Values;
        }

        public void ResetMutationPoints(List<Player> players)
        {
            foreach (var player in players)
            {
                int bonus = player.GetBonusMutationPoints();
                player.MutationPoints = GameBalance.StartingMutationPoints + bonus;
            }
        }
    }
}
