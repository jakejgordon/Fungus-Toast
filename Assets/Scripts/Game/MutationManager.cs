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

        private void Awake()
        {
            InitializeMutations();
        }

        private void InitializeMutations()
        {
            int id = 0;

            var mycelialBloom = new Mutation(
                id: id++,
                name: "Mycelial Bloom",
                description: $"Mycelium metabolizes carbohydrates at an accelerated rate, enhancing hyphal tip extension. " +
                             $"Grants an additional +{(0.005f * 100f):F1}% chance to spread mold into adjacent cells each round.",
                type: MutationType.GrowthChance,
                effectPerLevel: 0.005f,
                maxLevel: 100
            );
            rootMutations[mycelialBloom.Id] = mycelialBloom;
            allMutations[mycelialBloom.Id] = mycelialBloom;

            var homeostaticHarmony = new Mutation(
                id: id++,
                name: "Homeostatic Harmony",
                description: $"Internal turgor pressure and nutrient recycling mechanisms bolster cell viability under adverse conditions. " +
                             $"Provides an additional +{(0.0025f * 100f):F1}% chance for your mold cells to survive decay events.",
                type: MutationType.DefenseSurvival,
                effectPerLevel: 0.0025f,
                maxLevel: 100
            );
            rootMutations[homeostaticHarmony.Id] = homeostaticHarmony;
            allMutations[homeostaticHarmony.Id] = homeostaticHarmony;

            var silentBlight = new Mutation(
                id: id++,
                name: "Silent Blight",
                description: $"Secondary metabolites subtly disrupt competing organisms' cellular respiration and membrane integrity. " +
                             $"Increases the chance by +{(0.0025f * 100f):F1}% that enemy mold cells will die each growth round.",
                type: MutationType.EnemyDecayChance,
                effectPerLevel: 0.0025f,
                maxLevel: 100
            );
            rootMutations[silentBlight.Id] = silentBlight;
            allMutations[silentBlight.Id] = silentBlight;

            var adaptiveExpression = new Mutation(
                id: id++,
                name: "Adaptive Expression",
                description: $"Epigenetic modifications increase genomic variability, accelerating adaptive potential. " +
                             $"Adds a +{(0.01f * 100f):F1}% chance to gain a bonus mutation point at the start of each turn.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: 0.10f,
                maxLevel: 10
            );
            rootMutations[adaptiveExpression.Id] = adaptiveExpression;
            allMutations[adaptiveExpression.Id] = adaptiveExpression;

            var encystedSpores = new Mutation(
                id: id++,
                name: "Encysted Spores",
                description: $"Dormant spores deploy enzymatic agents that accelerate necrosis in isolated enemies. " +
                             $"Increases Silent Blight's decay penalty by +{(0.05f * 100f):F1}% when enemy cells are fully surrounded by living fungal cells.",
                type: MutationType.EncystedSporeMultiplier,
                effectPerLevel: 0.05f,
                maxLevel: 5
            );
            encystedSpores.RequiredMutation = silentBlight;
            encystedSpores.RequiredLevel = 10;
            silentBlight.Children.Add(encystedSpores);
            allMutations[encystedSpores.Id] = encystedSpores;

            // Diagonal Growth Mutations
            CreateDiagonalGrowthMutation(ref id, "Tendril Northwest", MutationType.GrowthDiagonal_NW, mycelialBloom);
            CreateDiagonalGrowthMutation(ref id, "Tendril Northeast", MutationType.GrowthDiagonal_NE, mycelialBloom);
            CreateDiagonalGrowthMutation(ref id, "Tendril Southeast", MutationType.GrowthDiagonal_SE, mycelialBloom);
            CreateDiagonalGrowthMutation(ref id, "Tendril Southwest", MutationType.GrowthDiagonal_SW, mycelialBloom);
        }

        private void CreateDiagonalGrowthMutation(ref int id, string name, MutationType type, Mutation required)
        {
            var mutation = new Mutation(
                id: id++,
                name: name,
                description: $"Specialized hyphae reach into {name.Split(' ')[1].ToLower()} territory. " +
                             $"Adds +1.0% chance per level to grow mold {name.Split(' ')[1].ToLower()}.",
                type: type,
                effectPerLevel: 0.01f,
                maxLevel: 10
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
