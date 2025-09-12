using System.Collections.Generic;
using FungusToast.Core.Config;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    internal static class EconomyMycovariantFactory
    {
        public static IEnumerable<Mycovariant> CreateAll()
        {
            yield return PlasmidBounty();
            yield return PlasmidBountyII();
            yield return PlasmidBountyIII();
        }

        private static Mycovariant PlasmidBounty() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyId,
                Name = "Plasmid Bounty I",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Horizontal gene transfer introduces novel genetic material, accelerating the colony's evolutionary potential.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = true,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyMutationPointAward);
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 5f
            };

        private static Mycovariant PlasmidBountyII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIId,
                Name = "Plasmid Bounty II",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyIIMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Multiple plasmid integrations trigger a cascade of genetic recombination events across the mycelial network.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = false,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIMutationPointAward);
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 7f
            };

        private static Mycovariant PlasmidBountyIII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIIId,
                Name = "Plasmid Bounty III",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyIIIMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Massive genetic influx overwhelms cellular repair mechanisms, creating unprecedented mutation rates throughout the colony.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = false,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIIMutationPointAward);
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 10f
            };
    }
}
