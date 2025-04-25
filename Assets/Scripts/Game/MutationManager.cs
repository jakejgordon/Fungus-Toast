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

        public int CurrentMutationPoints => currentMutationPoints;
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
            mutations = new List<Mutation>
            {
                new Mutation("Growth Boost", "Increase chance to grow into adjacent cells.", 5, 1),
                new Mutation("Spore Production", "Increase chance to release spores onto toast.", 5, 1),
                new Mutation("Toxin Release", "Increase chance to kill adjacent enemy mold.", 5, 1),
                new Mutation("Resilience", "Increase resistance to enemy attacks.", 5, 1),
                new Mutation("Reclamation", "Allow growth into dead toast cells.", 5, 1)
            };
        }

        public bool TryUpgradeMutation(Mutation mutation)
        {
            if (mutation.CanUpgrade() && currentMutationPoints >= mutation.CostPerLevel)
            {
                mutation.Upgrade();
                currentMutationPoints -= mutation.CostPerLevel;
                return true;
            }

            return false;
        }
    }
}
