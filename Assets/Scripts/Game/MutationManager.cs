using System.Collections.Generic;
using FungusToast.Core;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine;

namespace FungusToast.Game
{
    public class MutationManager : MonoBehaviour
    {
        [Header("Mutation Settings")]
        [SerializeField] private int startingMutationPoints = 5;

        private List<Mutation> rootMutations = new List<Mutation>();
        public IReadOnlyList<Mutation> RootMutations => rootMutations;

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
                effectGrowthPerLevel: 0.005f,
                maxLevel: 50
            );

            var homeostaticHarmony = new Mutation(
                "Homeostatic Harmony",
                $"Internal turgor pressure and nutrient recycling mechanisms bolster cell viability under adverse conditions. " +
                $"Provides an additional +{(0.005f * 100f):F1}% chance for your mold cells to survive decay events.",
                MutationType.DefenseSurvival,
                baseEffectValue: 0.01f,
                effectGrowthPerLevel: 0.005f,
                maxLevel: 50
            );

            var silentBlight = new Mutation(
                "Silent Blight",
                $"Secondary metabolites subtly disrupt competing organisms' cellular respiration and membrane integrity. " +
                $"Increases the chance by +{(0.005f * 100f):F1}% that enemy mold cells will die each growth round.",
                MutationType.EnemyDecayChance,
                baseEffectValue: 0.01f,
                effectGrowthPerLevel: 0.005f,
                maxLevel: 50
            );

            var adaptiveExpression = new Mutation(
                "Adaptive Expression",
                $"Epigenetic modifications increase genomic variability, accelerating adaptive potential. " +
                $"Adds a +{(0.005f * 100f):F1}% chance to gain a bonus mutation point at the start of each turn.",
                MutationType.BonusMutationPointChance,
                baseEffectValue: 0.01f,
                effectGrowthPerLevel: 0.005f,
                maxLevel: 50
            );

            rootMutations.Add(mycelialBloom);
            rootMutations.Add(homeostaticHarmony);
            rootMutations.Add(silentBlight);
            rootMutations.Add(adaptiveExpression);
        }

        public bool TryUpgradeMutation(Mutation mutation, Player player)
        {
            if (mutation == null || player == null)
            {
                Debug.LogError("Null mutation or player in TryUpgradeMutation!");
                return false;
            }

            if (player.MutationPoints >= mutation.PointsPerUpgrade && mutation.CanUpgrade())
            {
                Debug.Log($"TryUpgradeMutation: Player {player.PlayerId} has {player.MutationPoints} points before upgrade.");

                player.MutationPoints -= mutation.PointsPerUpgrade;
                mutation.CurrentLevel++;
                Debug.Log($"Player {player.PlayerId} upgraded {mutation.Name} to Level {mutation.CurrentLevel}");
                return true;
            }

            Debug.LogWarning($"Player {player.PlayerId} failed to upgrade {mutation?.Name}: insufficient points or maxed out.");
            return false;
        }

        public void ResetMutationPoints(List<Player> players)
        {
            foreach (var player in players)
            {
                player.MutationPoints = startingMutationPoints;
            }
        }
    }
}
