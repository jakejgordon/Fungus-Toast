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
            public const int MutatorPhenotype = 10;
            public const int Necrosporulation = 11;
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
                             $"Grants an additional +{(GameBalance.MycelialBloomEffectPerLevel * 100f):F1}% chance to spread mold into adjacent cells each round.",
                type: MutationType.GrowthChance,
                effectPerLevel: GameBalance.MycelialBloomEffectPerLevel,
                maxLevel: GameBalance.MycelialBloomMaxLevel,
                category: MutationCategory.Growth
            );
            rootMutations[mycelialBloom.Id] = mycelialBloom;
            allMutations[mycelialBloom.Id] = mycelialBloom;

            var homeostaticHarmony = new Mutation(
                id: MutationIds.HomeostaticHarmony,
                name: "Homeostatic Harmony",
                description: $"Internal turgor pressure and nutrient recycling mechanisms bolster cell viability under adverse conditions. " +
                             $"Provides an additional +{(GameBalance.HomeostaticHarmonyEffectPerLevel * 100f):F1}% chance for your mold cells to survive decay events.",
                type: MutationType.DefenseSurvival,
                effectPerLevel: GameBalance.HomeostaticHarmonyEffectPerLevel,
                maxLevel: GameBalance.HomeostaticHarmonyMaxLevel,
                category: MutationCategory.CellularResilience
            );
            rootMutations[homeostaticHarmony.Id] = homeostaticHarmony;
            allMutations[homeostaticHarmony.Id] = homeostaticHarmony;

            var silentBlight = new Mutation(
                id: MutationIds.SilentBlight,
                name: "Silent Blight",
                description: $"Secondary metabolites subtly disrupt competing organisms' cellular respiration and membrane integrity. " +
                             $"Increases the chance by +{(GameBalance.SilentBlightEffectPerLevel * 100f):F1}% that enemy mold cells will die each growth round.",
                type: MutationType.EnemyDecayChance,
                effectPerLevel: GameBalance.SilentBlightEffectPerLevel,
                maxLevel: GameBalance.SilentBlightMaxLevel,
                category: MutationCategory.Fungicide
            );
            rootMutations[silentBlight.Id] = silentBlight;
            allMutations[silentBlight.Id] = silentBlight;

            var adaptiveExpression = new Mutation(
                id: MutationIds.AdaptiveExpression,
                name: "Adaptive Expression",
                description: $"Epigenetic modifications increase genomic variability, accelerating adaptive potential. " +
                             $"Adds a +{(GameBalance.AdaptiveExpressionEffectPerLevel * 100f):F1}% chance to gain a bonus mutation point at the start of each turn.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AdaptiveExpressionEffectPerLevel,
                maxLevel: GameBalance.AdaptiveExpressionMaxLevel,
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
                effectPerLevel: GameBalance.ChronoresilientCytoplasmEffectPerLevel,
                maxLevel: GameBalance.ChronoresilientCytoplasmMaxLevel,
                category: MutationCategory.CellularResilience
            );
            chronoresilientCytoplasm.RequiredMutation = homeostaticHarmony;
            chronoresilientCytoplasm.RequiredLevel = 10;
            homeostaticHarmony.Children.Add(chronoresilientCytoplasm);
            allMutations[chronoresilientCytoplasm.Id] = chronoresilientCytoplasm;

            var necrosporulation = new Mutation(
                id: MutationIds.Necrosporulation,
                name: "Necrosporulation",
                description: $"Weakened mold cells release a last burst of life. " +
                             $"When one of your cells dies, there's a +{(GameBalance.NecrosporulationEffectPerLevel * 100f):F1}% chance per level to spawn a new cell on a random unoccupied tile.",
                type: MutationType.SporeOnDeathChance,
                effectPerLevel: GameBalance.NecrosporulationEffectPerLevel,
                maxLevel: GameBalance.NecrosporulationMaxLevel,
                category: MutationCategory.CellularResilience
            );
            necrosporulation.RequiredMutation = chronoresilientCytoplasm;
            necrosporulation.RequiredLevel = 5;
            chronoresilientCytoplasm.Children.Add(necrosporulation);
            allMutations[necrosporulation.Id] = necrosporulation;

            var encystedSpores = new Mutation(
                id: MutationIds.EncystedSpores,
                name: "Encysted Spores",
                description: $"Dormant spores deploy enzymatic agents that accelerate necrosis in isolated enemies. " +
                             $"Increases Silent Blight's decay penalty by +{(GameBalance.EncystedSporesEffectPerLevel * 100f):F1}% when enemy cells are fully surrounded by living fungal cells.",
                type: MutationType.EncystedSporeMultiplier,
                effectPerLevel: GameBalance.EncystedSporesEffectPerLevel,
                maxLevel: GameBalance.EncystedSporesMaxLevel,
                category: MutationCategory.Fungicide
            );
            encystedSpores.RequiredMutation = silentBlight;
            encystedSpores.RequiredLevel = 10;
            silentBlight.Children.Add(encystedSpores);
            allMutations[encystedSpores.Id] = encystedSpores;

            CreateDiagonalGrowthMutation(MutationIds.TendrilNorthwest, "Tendril Northwest", MutationType.GrowthDiagonal_NW, mycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilNortheast, "Tendril Northeast", MutationType.GrowthDiagonal_NE, mycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilSoutheast, "Tendril Southeast", MutationType.GrowthDiagonal_SE, mycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilSouthwest, "Tendril Southwest", MutationType.GrowthDiagonal_SW, mycelialBloom);

            var mutatorPhenotype = new Mutation(
                id: MutationIds.MutatorPhenotype,
                name: "Mutator Phenotype",
                description: $"Genomic instability accelerates adaptation. " +
                             $"Grants a {(GameBalance.MutatorPhenotypeEffectPerLevel * 100f):F1}% chance per level to automatically upgrade a random owned mutation at the start of each turn.",
                type: MutationType.AutoUpgradeRandom,
                effectPerLevel: GameBalance.MutatorPhenotypeEffectPerLevel,
                maxLevel: GameBalance.MutatorPhenotypeMaxLevel,
                category: MutationCategory.GeneticDrift
            );
            mutatorPhenotype.RequiredMutation = adaptiveExpression;
            mutatorPhenotype.RequiredLevel = 5;
            adaptiveExpression.Children.Add(mutatorPhenotype);
            allMutations[mutatorPhenotype.Id] = mutatorPhenotype;
        }

        private void CreateDiagonalGrowthMutation(int id, string name, MutationType type, Mutation required)
        {
            string direction = name.Split(' ')[1];
            var mutation = new Mutation(
                id: id,
                name: name,
                description: $"Specialized hyphae reach into {direction.ToLower()} territory. " +
                             $"Adds +{(GameBalance.DiagonalGrowthEffectPerLevel * 100f):F1}% chance per level to grow mold {direction.ToLower()}.",
                type: type,
                effectPerLevel: GameBalance.DiagonalGrowthEffectPerLevel,
                maxLevel: GameBalance.DiagonalGrowthMaxLevel,
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