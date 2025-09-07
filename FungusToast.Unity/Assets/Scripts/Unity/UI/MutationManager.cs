using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Config;

namespace FungusToast.Unity.UI
{
    public class MutationManager : MonoBehaviour
    {
        private Dictionary<int, Mutation> rootMutations = new();
        public IReadOnlyDictionary<int, Mutation> RootMutations => rootMutations;

        private Dictionary<int, Mutation> allMutations = new();
        public IReadOnlyDictionary<int, Mutation> AllMutations => allMutations;

        private void Awake()
        {
            (allMutations, rootMutations) = MutationRepository.BuildFullMutationSet();
        }

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
