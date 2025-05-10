using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Config;

namespace FungusToast.Core.Mutations
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
            public const int MycotropicInduction = 12;
            public const int PutrefactiveMycotoxin = 13;   // NEW
        }

        private void Awake()
        {
            InitializeMutations();
        }

        private void InitializeMutations()
        {
            Mutation MakeRoot(Mutation m)
            {
                rootMutations[m.Id] = m;
                allMutations[m.Id] = m;
                return m;
            }

            Mutation MakeChild(Mutation m, params MutationPrerequisite[] prereqs)
            {
                m.Prerequisites.AddRange(prereqs);
                allMutations[m.Id] = m;
                foreach (var prereq in prereqs)
                {
                    if (allMutations.TryGetValue(prereq.MutationId, out var parent))
                        parent.Children.Add(m);
                }
                return m;
            }

            /* ---------- Tier-1 Roots ---------- */

            MakeRoot(new Mutation(
                id: MutationIds.MycelialBloom,
                name: "Mycelial Bloom",
                description:
                    $"Mycelium metabolizes carbohydrates at an accelerated rate, enhancing hyphal tip extension. " +
                    $"Grants an additional +{(GameBalance.MycelialBloomEffectPerLevel * 100f):F1}% chance to spread mold into adjacent cells each round.",
                type: MutationType.GrowthChance,
                effectPerLevel: GameBalance.MycelialBloomEffectPerLevel,
                maxLevel: GameBalance.MycelialBloomMaxLevel,
                category: MutationCategory.Growth
            ));

            MakeRoot(new Mutation(
                id: MutationIds.HomeostaticHarmony,
                name: "Homeostatic Harmony",
                description:
                    $"Internal turgor pressure and nutrient recycling mechanisms bolster cell viability under adverse conditions. " +
                    $"Provides an additional +{(GameBalance.HomeostaticHarmonyEffectPerLevel * 100f):F1}% chance for your mold cells to survive decay events.",
                type: MutationType.DefenseSurvival,
                effectPerLevel: GameBalance.HomeostaticHarmonyEffectPerLevel,
                maxLevel: GameBalance.HomeostaticHarmonyMaxLevel,
                category: MutationCategory.CellularResilience
            ));

            MakeRoot(new Mutation(
                id: MutationIds.SilentBlight,
                name: "Silent Blight",
                description:
                    $"Secondary metabolites subtly disrupt competing organisms' cellular respiration and membrane integrity. " +
                    $"Increases the chance by +{(GameBalance.SilentBlightEffectPerLevel * 100f):F1}% that enemy mold cells will die each growth round.",
                type: MutationType.EnemyDecayChance,
                effectPerLevel: GameBalance.SilentBlightEffectPerLevel,
                maxLevel: GameBalance.SilentBlightMaxLevel,
                category: MutationCategory.Fungicide
            ));

            MakeRoot(new Mutation(
                id: MutationIds.AdaptiveExpression,
                name: "Adaptive Expression",
                description:
                    $"Epigenetic modifications increase genomic variability, accelerating adaptive potential. " +
                    $"Adds a +{(GameBalance.AdaptiveExpressionEffectPerLevel * 100f):F1}% chance to gain a bonus mutation point at the start of each turn.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AdaptiveExpressionEffectPerLevel,
                maxLevel: GameBalance.AdaptiveExpressionMaxLevel,
                category: MutationCategory.GeneticDrift
            ));

            /* ---------- Tier-2 ---------- */

            MakeChild(new Mutation(
                    id: MutationIds.ChronoresilientCytoplasm,
                    name: "Chronoresilient Cytoplasm",
                    description:
                        $"Cells that endure long enough undergo cytoplasmic rejuvenation. " +
                        $"When a mold cell survives for a number of cycles equal to (50 − 5 × Level), its age resets to 0 instead of increasing Mycelial Degradation risk.",
                    type: MutationType.SelfAgeResetThreshold,
                    effectPerLevel: GameBalance.ChronoresilientCytoplasmEffectPerLevel,
                    maxLevel: GameBalance.ChronoresilientCytoplasmMaxLevel,
                    category: MutationCategory.CellularResilience
                ),
                new MutationPrerequisite(MutationIds.HomeostaticHarmony, 10));

            MakeChild(new Mutation(
                    id: MutationIds.EncystedSpores,
                    name: "Encysted Spores",
                    description:
                        $"Dormant spores deploy enzymatic agents that accelerate necrosis in isolated enemies. " +
                        $"Increases Silent Blight's decay penalty by +{(GameBalance.EncystedSporesEffectPerLevel * 100f):F1}% when enemy cells are fully surrounded by living fungal cells.",
                    type: MutationType.EncystedSporeMultiplier,
                    effectPerLevel: GameBalance.EncystedSporesEffectPerLevel,
                    maxLevel: GameBalance.EncystedSporesMaxLevel,
                    category: MutationCategory.Fungicide
                ),
                new MutationPrerequisite(MutationIds.SilentBlight, 10));

            CreateDiagonalGrowthMutation(MutationIds.TendrilNorthwest, "Tendril Northwest", MutationType.GrowthDiagonal_NW, MutationIds.MycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilNortheast, "Tendril Northeast", MutationType.GrowthDiagonal_NE, MutationIds.MycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilSoutheast, "Tendril Southeast", MutationType.GrowthDiagonal_SE, MutationIds.MycelialBloom);
            CreateDiagonalGrowthMutation(MutationIds.TendrilSouthwest, "Tendril Southwest", MutationType.GrowthDiagonal_SW, MutationIds.MycelialBloom);

            MakeChild(new Mutation(
                    id: MutationIds.MutatorPhenotype,
                    name: "Mutator Phenotype",
                    description:
                        $"Genomic instability accelerates adaptation. " +
                        $"Grants a {(GameBalance.MutatorPhenotypeEffectPerLevel * 100f):F1}% chance per level to automatically upgrade a random owned mutation at the start of each turn.",
                    type: MutationType.AutoUpgradeRandom,
                    effectPerLevel: GameBalance.MutatorPhenotypeEffectPerLevel,
                    maxLevel: GameBalance.MutatorPhenotypeMaxLevel,
                    category: MutationCategory.GeneticDrift
                ),
                new MutationPrerequisite(MutationIds.AdaptiveExpression, 5));

            /* ---------- Tier-3 ---------- */

            MakeChild(new Mutation(
                    id: MutationIds.Necrosporulation,
                    name: "Necrosporulation",
                    description:
                        $"Weakened mold cells release a final burst of life. " +
                        $"When one of your cells dies, there's a +{(GameBalance.NecrosporulationEffectPerLevel * 100f):F1}% chance per level to spawn a new cell on a random unoccupied tile.",
                    type: MutationType.SporeOnDeathChance,
                    effectPerLevel: GameBalance.NecrosporulationEffectPerLevel,
                    maxLevel: GameBalance.NecrosporulationMaxLevel,
                    category: MutationCategory.CellularResilience
                ),
                new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 5));

            // NEW – Putrefactive Mycotoxin (Fungicide Tier-3)
            MakeChild(new Mutation(
                    id: MutationIds.PutrefactiveMycotoxin,
                    name: "Putrefactive Mycotoxin",
                    description:
                        $"A lytic cocktail seeps outward, liquefying rival colonies before decay begins. " +
                        $"Adjacent enemy cells suffer an additional +{(GameBalance.PutrefactiveMycotoxinEffectPerLevel * 100f):F1}% death chance per level during the Decay phase.",
                    type: MutationType.OpponentExtraDeathChance,
                    effectPerLevel: GameBalance.PutrefactiveMycotoxinEffectPerLevel,
                    maxLevel: GameBalance.PutrefactiveMycotoxinMaxLevel,
                    category: MutationCategory.Fungicide
                ),
                new MutationPrerequisite(MutationIds.EncystedSpores, 5));

            // Tier-3 Growth boost to Tendrils
            MakeChild(new Mutation(
                    id: MutationIds.MycotropicInduction,
                    name: "Mycotropic Induction",
                    description:
                        $"A latent fungal directive harmonizes spore orientation, vastly enhancing diagonal propagation.\n" +
                        $"Boosts the effect of all Tendril mutations by +{(GameBalance.MycotropicInductionEffectPerLevel * 100f):F1}% per level.",
                    type: MutationType.TendrilDirectionalMultiplier,
                    effectPerLevel: GameBalance.MycotropicInductionEffectPerLevel,
                    maxLevel: GameBalance.MycotropicInductionMaxLevel,
                    category: MutationCategory.Growth
                ),
                new MutationPrerequisite(MutationIds.TendrilNorthwest, 1),
                new MutationPrerequisite(MutationIds.TendrilNortheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSoutheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSouthwest, 1));
        }

        private void CreateDiagonalGrowthMutation(int id, string name, MutationType type, int requiredMutationId)
        {
            string direction = name.Split(' ')[1];
            var mutation = new Mutation(
                id: id,
                name: name,
                description:
                    $"Specialized hyphae reach into {direction.ToLower()} territory. " +
                    $"Adds +{(GameBalance.DiagonalGrowthEffectPerLevel * 100f):F1}% chance per level to grow mold {direction.ToLower()}.",
                type: type,
                effectPerLevel: GameBalance.DiagonalGrowthEffectPerLevel,
                maxLevel: GameBalance.DiagonalGrowthMaxLevel,
                category: MutationCategory.Growth
            );
            mutation.Prerequisites.Add(new MutationPrerequisite(requiredMutationId, 10));
            allMutations[mutation.Id] = mutation;
            allMutations[requiredMutationId].Children.Add(mutation);
        }

        /* ---------- Public helpers ---------- */

        public Mutation GetMutationById(int id) =>
            allMutations.TryGetValue(id, out var m) ? m : null;

        public IReadOnlyCollection<Mutation> GetAllMutations() => allMutations.Values;

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
