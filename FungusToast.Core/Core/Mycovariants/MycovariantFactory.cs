using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using System;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public static class MycovariantFactory
    {
        public static Mycovariant JettingMyceliumNorth() =>
            CreateJettingMycelium(
                "North",
                MycovariantIds.JettingMyceliumNorthId,
                CardinalDirection.North);

        public static Mycovariant JettingMyceliumEast() =>
            CreateJettingMycelium(
                "East",
                MycovariantIds.JettingMyceliumEastId,
                CardinalDirection.East);

        public static Mycovariant JettingMyceliumSouth() =>
            CreateJettingMycelium(
                "South",
                MycovariantIds.JettingMyceliumSouthId,
                CardinalDirection.South);

        public static Mycovariant JettingMyceliumWest() =>
            CreateJettingMycelium(
                "West",
                MycovariantIds.JettingMyceliumWestId,
                CardinalDirection.West);

        private static Mycovariant CreateJettingMycelium(
            string directionLabel,
            int id,
            CardinalDirection cardinalDirection)
        {
            return new Mycovariant
            {
                Id = id,
                Name = $"Jetting Mycelium ({directionLabel})",
                Description = $"Immediately grow {MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles} mold tiles {directionLabel.ToLower()} from a chosen cell, followed by {MycovariantGameBalance.JettingMyceliumNumberOfToxinTiles} toxin tiles.",
                FlavorText = $"The cap cracks. The colony launches {directionLabel.ToLower()}ward.",
                Type = MycovariantType.Directional,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // --- For AI/simulation: Pick the first living cell as the launch point
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player == null) return;
                    var launchTile = board.GetAllCellsOwnedBy(player.PlayerId)
                        .FirstOrDefault(c => c.IsAlive);
                    if (launchTile == null) return;

                    // Call the effect processor
                    MycovariantEffectProcessor.ResolveJettingMycelium(
                        playerMyco, player, board, launchTile.TileId, cardinalDirection, observer
                    );
                }
            };
        }


        public static Mycovariant NecrophoricAdaptation() =>
            new Mycovariant
            {
                Id = 1002,
                Name = "Necrophoric Adaptation",
                Description = "When a mold cell dies, there is a chance to reclaim a nearby dead tile.",
                FlavorText = "Even in death, the colony endures.",
                Type = MycovariantType.Passive
            };

        internal static Mycovariant SaprobicRelay()
        {
            throw new NotImplementedException();
        }

        internal static Mycovariant SporodochialGrowth()
        {
            throw new NotImplementedException();
        }

        public static Mycovariant PlasmidBounty() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyId,
                Name = "Plasmid Bounty",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "An ancient plasmid cache is uncovered, its code empowering rapid mutation.",
                Type = MycovariantType.Economy,
                IsUniversal = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyMutationPointAward);
                }
            };

        public static Mycovariant NeutralizingMantle() =>
        new Mycovariant
        {
            Id = MycovariantIds.NeutralizingMantleId,
            Name = "Neutralizing Mantle",
            Description = $"Whenever an enemy toxin is placed adjacent to your living cells, you have a {MycovariantGameBalance.NeutralizingMantleNeutralizeChance * 100f:0}% chance to neutralize (remove) it instantly.",
            FlavorText = "A protective sheath of hyphae, secreting enzymes to break down hostile compounds.",
            Type = MycovariantType.Passive,
            IsUniversal = false
        };


    }
}
