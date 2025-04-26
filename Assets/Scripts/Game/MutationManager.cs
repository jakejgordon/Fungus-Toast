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
            var growthBoost = new Mutation("Growth Boost");
            var fastGrowth = new Mutation("Fast Growth");
            var aggressiveExpansion = new Mutation("Aggressive Expansion");

            growthBoost.Children.Add(fastGrowth);
            fastGrowth.Children.Add(aggressiveExpansion);

            var toxinResistance = new Mutation("Toxin Resistance");
            var acidicMold = new Mutation("Acidic Mold");
            var hardenedWalls = new Mutation("Hardened Cell Walls");

            toxinResistance.Children.Add(acidicMold);
            toxinResistance.Children.Add(hardenedWalls);

            rootMutations.Add(growthBoost);
            rootMutations.Add(toxinResistance);
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
