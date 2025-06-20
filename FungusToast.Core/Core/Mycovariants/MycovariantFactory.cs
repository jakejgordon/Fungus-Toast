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
                MycovariantGameBalance.JettingMyceliumNorthId,
                CardinalDirection.North);

        public static Mycovariant JettingMyceliumEast() =>
            CreateJettingMycelium(
                "East",
                MycovariantGameBalance.JettingMyceliumEastId,
                CardinalDirection.East);

        public static Mycovariant JettingMyceliumSouth() =>
            CreateJettingMycelium(
                "South",
                MycovariantGameBalance.JettingMyceliumSouthId,
                CardinalDirection.South);

        public static Mycovariant JettingMyceliumWest() =>
            CreateJettingMycelium(
                "West",
                MycovariantGameBalance.JettingMyceliumWestId,
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
                Id = MycovariantGameBalance.PlasmidBountyId,
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

    }
}
