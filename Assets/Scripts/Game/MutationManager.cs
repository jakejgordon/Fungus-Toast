using System.Collections.Generic;
using FungusToast.Core;
using FungusToast.Core.Mutations;
using UnityEngine;

namespace FungusToast.Game
{
    public class MutationManager : MonoBehaviour
    {
        private List<Mutation> rootMutations = new List<Mutation>();

        public IReadOnlyList<Mutation> RootMutations => rootMutations;

        public int CurrentMutationPoints { get; private set; }

        [Header("Mutation Settings")]
        [SerializeField] private int startingMutationPoints = 5;

        private void Awake()
        {
            InitializeMutations();
        }

        private void InitializeMutations()
        {
            var mycelialBloom = new Mutation(
                "Mycelial Bloom",
                $"Mycelium metabolizes carbohydrates at an accelerated rate, enhancing hyphal tip extension. " +
                $"Grants an additional +{(0.005f * 100f):F1}% chance to spread mold into adjacent cells each round.",
                MutationType.GrowthChance,
                baseEffectValue: 0.01f,
                effectGrowthPerLevel: 0.005f
            );

            var homeostaticHarmony = new Mutation(
                "Homeostatic Harmony",
                $"Internal turgor pressure and nutrient recycling mechanisms bolster cell viability under adverse conditions. " +
                $"Provides an additional +{(0.005f * 100f):F1}% chance for your mold cells to survive decay events.",
                MutationType.DefenseSurvival,
                baseEffectValue: 0.01f,
                effectGrowthPerLevel: 0.005f
            );

            var silentBlight = new Mutation(
                "Silent Blight",
                $"Secondary metabolites subtly disrupt competing organisms' cellular respiration and membrane integrity. " +
                $"Increases the chance by +{(0.005f * 100f):F1}% that enemy mold cells will die each growth round.",
                MutationType.EnemyDecayChance,
                baseEffectValue: 0.01f,
                effectGrowthPerLevel: 0.005f
            );

            var adaptiveExpression = new Mutation(
                "Adaptive Expression",
                $"Epigenetic modifications increase genomic variability, accelerating adaptive potential. " +
                $"Adds a +{(0.005f * 100f):F1}% chance to gain a bonus mutation point at the start of each turn.",
                MutationType.BonusMutationPointChance,
                baseEffectValue: 0.01f,
                effectGrowthPerLevel: 0.005f
            );

            rootMutations.Add(mycelialBloom);
            rootMutations.Add(homeostaticHarmony);
            rootMutations.Add(silentBlight);
            rootMutations.Add(adaptiveExpression);
        }


        public bool TryUpgradeMutation(Mutation mutation)
        {
            if (mutation == null)
                return false;

            if (CurrentMutationPoints >= mutation.PointsPerUpgrade && mutation.CanUpgrade())
            {
                CurrentMutationPoints -= mutation.PointsPerUpgrade;
                mutation.CurrentLevel++;
                return true;
            }

            return false;
        }

        public void ResetMutationPoints()
        {
            CurrentMutationPoints = startingMutationPoints;
        }
    }
}
