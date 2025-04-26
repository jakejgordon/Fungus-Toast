using System.Collections.Generic;
using FungusToast.Core;
using UnityEngine;

namespace FungusToast.Game
{
    public class MutationManager : MonoBehaviour
    {
        public int startingMutationPoints = 5;

        private int currentMutationPoints;
        private List<Mutation> mutations;
        private List<Mutation> rootMutations = new List<Mutation>();
        public IReadOnlyList<Mutation> RootMutations => rootMutations;

        public int CurrentMutationPoints { get; private set; }
        public IReadOnlyList<Mutation> Mutations => mutations;

        void Start()
        {
            InitializeMutations();
            ResetMutationPoints();
        }

        public void ResetMutationPoints()
        {
            currentMutationPoints = startingMutationPoints;
        }

        

        private void InitializeMutations()
        {
            // Example Tree
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

            // Add to Root Mutations
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

    }
}
