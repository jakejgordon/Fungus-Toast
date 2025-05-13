using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Core.Mutations
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

        public void ResetMutationPoints(List<Players.Player> players)
        {
            foreach (var player in players)
            {
                int bonus = player.GetBonusMutationPoints();
                player.MutationPoints = Config.GameBalance.StartingMutationPoints + bonus;
            }
        }
    }
}
