using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core;

namespace FungusToast.Game
{
    public class MutationManager : MonoBehaviour
    {
        [Header("Mutation Settings")]
        [SerializeField] private int startingMutationPoints = 5;
        public int BasePointsPerCycle => startingMutationPoints;

        private Dictionary<int, Mutation> rootMutations = new Dictionary<int, Mutation>();
        public IReadOnlyDictionary<int, Mutation> RootMutations => rootMutations;

        private void Awake()
        {
            InitializeMutations();
        }

        private void InitializeMutations()
        {
            int id = 0;

            var mycelialBloom = new Mutation(
                "Mycelial Bloom",
                $"Mycelium metabolizes carbohydrates at an accelerated rate, enhancing hyphal tip extension. " +
                $"Grants an additional +{(0.005f * 100f):F1}% chance to spread mold into adjacent cells each round.",
                MutationType.GrowthChance,
                effectPerLevel: 0.005f,
                maxLevel: 100
            );
            rootMutations[id++] = mycelialBloom;

            var homeostaticHarmony = new Mutation(
                "Homeostatic Harmony",
                $"Internal turgor pressure and nutrient recycling mechanisms bolster cell viability under adverse conditions. " +
                $"Provides an additional +{(0.0025f * 100f):F1}% chance for your mold cells to survive decay events.",
                MutationType.DefenseSurvival,
                effectPerLevel: 0.0025f,
                maxLevel: 100
            );
            rootMutations[id++] = homeostaticHarmony;

            var silentBlight = new Mutation(
                "Silent Blight",
                $"Secondary metabolites subtly disrupt competing organisms' cellular respiration and membrane integrity. " +
                $"Increases the chance by +{(0.0025f * 100f):F1}% that enemy mold cells will die each growth round.",
                MutationType.EnemyDecayChance,
                effectPerLevel: 0.0025f,
                maxLevel: 100
            );
            rootMutations[id++] = silentBlight;

            var adaptiveExpression = new Mutation(
                "Adaptive Expression",
                $"Epigenetic modifications increase genomic variability, accelerating adaptive potential. " +
                $"Adds a +{(0.01f * 100f):F1}% chance to gain a bonus mutation point at the start of each turn.",
                MutationType.BonusMutationPointChance,
                effectPerLevel: 0.01f,
                maxLevel: 50
            );
            rootMutations[id++] = adaptiveExpression;

            // Tier 2 mutation, requires Silent Blight
            var encystedSpores = new Mutation(
                "Encysted Spores",
                $"Dormant spores deploy enzymatic agents that accelerate necrosis in isolated enemies. " +
                $"Increases Silent Blight's decay penalty by +{(0.05f * 100f):F1}% when enemy cells are fully surrounded by living fungal cells.",
                MutationType.EncystedSporeMultiplier,
                effectPerLevel: 0.05f,
                maxLevel: 5
            );
            encystedSpores.RequiredMutation = silentBlight;
            silentBlight.Children.Add(encystedSpores);
        }

        public Mutation GetMutationById(int id)
        {
            if (rootMutations.TryGetValue(id, out var mutation))
            {
                return mutation;
            }

            Debug.LogWarning($"\u26a0\ufe0f Mutation ID {id} not found in rootMutations.");
            return null;
        }

        public bool TryUpgradeMutation(Mutation mutation, Player player)
        {
            if (mutation == null || player == null)
            {
                Debug.LogError("\u274c Null mutation or player in TryUpgradeMutation!");
                return false;
            }

            // Prerequisite check for dependent mutations
            if (mutation.RequiredMutation != null &&
                player.GetMutationLevel(mutation.RequiredMutation.GetHashCode()) < 10)
            {
                Debug.LogWarning($"\u26a0\ufe0f Cannot upgrade {mutation.Name}: prerequisite not met.");
                return false;
            }

            if (player.MutationPoints >= mutation.PointsPerUpgrade && mutation.CanUpgrade())
            {
                Debug.Log($"TryUpgradeMutation: Player {player.PlayerId} has {player.MutationPoints} points before upgrade.");

                player.MutationPoints -= mutation.PointsPerUpgrade;
                mutation.CurrentLevel++;
                Debug.Log($"\u2705 Player {player.PlayerId} upgraded {mutation.Name} to Level {mutation.CurrentLevel}");
                return true;
            }

            Debug.LogWarning($"\u26a0\ufe0f Player {player.PlayerId} failed to upgrade {mutation?.Name}: insufficient points or maxed out.");
            return false;
        }

        public void ResetMutationPoints(List<Player> players)
        {
            foreach (var player in players)
            {
                int bonus = player.GetBonusMutationPoints();
                player.MutationPoints = startingMutationPoints + bonus;
            }
        }
    }
}
