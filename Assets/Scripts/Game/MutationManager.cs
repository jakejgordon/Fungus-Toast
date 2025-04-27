using System.Collections.Generic;
using FungusToast.Core;
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
                MutationType.GrowthChance,
                baseEffectValue: 0.01f, // +1% spread chance base
                effectGrowthPerLevel: 0.005f // +0.5% per additional upgrade
            );

            var homeostaticHarmony = new Mutation(
                "Homeostatic Harmony",
                MutationType.DefenseSurvival,
                baseEffectValue: 0.01f, // +1% survival base
                effectGrowthPerLevel: 0.005f // +0.5% per upgrade
            );

            var silentBlight = new Mutation(
                "Silent Blight",
                MutationType.EnemyDecayChance,
                baseEffectValue: 0.01f, // +1% enemy death base
                effectGrowthPerLevel: 0.005f
            );

            var adaptiveExpression = new Mutation(
                "Adaptive Expression",
                MutationType.BonusMutationPointChance,
                baseEffectValue: 0.01f, // +1% bonus point chance
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
