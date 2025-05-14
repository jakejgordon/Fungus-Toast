using FungusToast.Core.Config;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations
{
    public static class MutationRepository
    {
        public static (Dictionary<int, Mutation> all, Dictionary<int, Mutation> roots) BuildFullMutationSet()
        {
            var allMutations = new Dictionary<int, Mutation>();
            var rootMutations = new Dictionary<int, Mutation>();

            Mutation MakeRoot(Mutation m)
            {
                allMutations[m.Id] = m;
                rootMutations[m.Id] = m;
                return m;
            }

            Mutation MakeChild(Mutation m, params MutationPrerequisite[] prereqs)
            {
                m.Prerequisites.AddRange(prereqs);
                allMutations[m.Id] = m;

                foreach (var prereq in prereqs)
                    if (allMutations.TryGetValue(prereq.MutationId, out var parent))
                        parent.Children.Add(m);

                return m;
            }

            // Tier-1
            MakeRoot(new Mutation(MutationIds.MycelialBloom, "Mycelial Bloom", "Grants outgrowth.", MutationType.GrowthChance, GameBalance.MycelialBloomEffectPerLevel, 1, GameBalance.MycelialBloomMaxLevel, MutationCategory.Growth));
            MakeRoot(new Mutation(MutationIds.HomeostaticHarmony, "Homeostatic Harmony", "Improves decay survival.", MutationType.DefenseSurvival, GameBalance.HomeostaticHarmonyEffectPerLevel, 1, GameBalance.HomeostaticHarmonyMaxLevel, MutationCategory.CellularResilience));
            MakeRoot(new Mutation(MutationIds.SilentBlight, "Silent Blight", "Increases enemy death chance.", MutationType.EnemyDecayChance, GameBalance.SilentBlightEffectPerLevel, 1, GameBalance.SilentBlightMaxLevel, MutationCategory.Fungicide));
            MakeRoot(new Mutation(MutationIds.AdaptiveExpression, "Adaptive Expression", "Chance to gain bonus points.", MutationType.BonusMutationPointChance, GameBalance.AdaptiveExpressionEffectPerLevel, 1, GameBalance.AdaptiveExpressionMaxLevel, MutationCategory.GeneticDrift));

            // Tier-2
            MakeChild(new Mutation(MutationIds.ChronoresilientCytoplasm, "Chronoresilient Cytoplasm", "Resets age.", MutationType.SelfAgeResetThreshold, GameBalance.ChronoresilientCytoplasmEffectPerLevel, 1, GameBalance.ChronoresilientCytoplasmMaxLevel, MutationCategory.CellularResilience), new MutationPrerequisite(MutationIds.HomeostaticHarmony, 10));
            MakeChild(new Mutation(MutationIds.EncystedSpores, "Encysted Spores", "Decay bonus for surrounded enemies.", MutationType.EncystedSporeMultiplier, GameBalance.EncystedSporesEffectPerLevel, 1, GameBalance.EncystedSporesMaxLevel, MutationCategory.Fungicide), new MutationPrerequisite(MutationIds.SilentBlight, 10));

            AddTendril(MutationIds.TendrilNorthwest, "Northwest");
            AddTendril(MutationIds.TendrilNortheast, "Northeast");
            AddTendril(MutationIds.TendrilSoutheast, "Southeast");
            AddTendril(MutationIds.TendrilSouthwest, "Southwest");

            MakeChild(new Mutation(MutationIds.MutatorPhenotype, "Mutator Phenotype", "Auto-upgrade mutation.", MutationType.AutoUpgradeRandom, GameBalance.MutatorPhenotypeEffectPerLevel, 1, GameBalance.MutatorPhenotypeMaxLevel, MutationCategory.GeneticDrift), new MutationPrerequisite(MutationIds.AdaptiveExpression, 5));

            // Tier-3
            MakeChild(new Mutation(MutationIds.Necrosporulation, "Necrosporulation", "Spawn new cell on death.", MutationType.SporeOnDeathChance, GameBalance.NecrosporulationEffectPerLevel, 1, GameBalance.NecrosporulationMaxLevel, MutationCategory.CellularResilience), new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 5));

            MakeChild(
                new Mutation(
                    MutationIds.PutrefactiveMycotoxin,
                    "Putrefactive Mycotoxin",
                    "Adds death pressure for each adjacent tile owned by the attacker.",
                    MutationType.OpponentExtraDeathChance,
                    GameBalance.PutrefactiveMycotoxinEffectPerLevel,
                    1,
                    GameBalance.PutrefactiveMycotoxinMaxLevel,
                    MutationCategory.Fungicide
                ),
                new MutationPrerequisite(MutationIds.EncystedSpores, 5)
            );

            MakeChild(new Mutation(MutationIds.MycotropicInduction, "Mycotropic Induction", "Boosts tendril growth.", MutationType.TendrilDirectionalMultiplier, GameBalance.MycotropicInductionEffectPerLevel, 1, GameBalance.MycotropicInductionMaxLevel, MutationCategory.Growth),
                new MutationPrerequisite(MutationIds.TendrilNorthwest, 1),
                new MutationPrerequisite(MutationIds.TendrilNortheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSoutheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSouthwest, 1));

            return (allMutations, rootMutations);

            void AddTendril(int id, string dir)
            {
                MakeChild(new Mutation(id, $"Tendril {dir}", $"Chance to grow {dir.ToLower()}.", dir switch
                {
                    "Northwest" => MutationType.GrowthDiagonal_NW,
                    "Northeast" => MutationType.GrowthDiagonal_NE,
                    "Southeast" => MutationType.GrowthDiagonal_SE,
                    "Southwest" => MutationType.GrowthDiagonal_SW,
                    _ => throw new System.Exception("Invalid direction")
                }, GameBalance.DiagonalGrowthEffectPerLevel, 1, GameBalance.DiagonalGrowthMaxLevel, MutationCategory.Growth), new MutationPrerequisite(MutationIds.MycelialBloom, 10));
            }
        }
    }
}
