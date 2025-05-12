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

        public static class MutationIds
        {
            public const int MycelialBloom = 0;
            public const int HomeostaticHarmony = 1;
            public const int SilentBlight = 2;
            public const int AdaptiveExpression = 3;
            public const int ChronoresilientCytoplasm = 4;
            public const int EncystedSpores = 5;
            public const int TendrilNorthwest = 6;
            public const int TendrilNortheast = 7;
            public const int TendrilSoutheast = 8;
            public const int TendrilSouthwest = 9;
            public const int MutatorPhenotype = 10;
            public const int Necrosporulation = 11;
            public const int MycotropicInduction = 12;
            public const int PutrefactiveMycotoxin = 13;
        }

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
