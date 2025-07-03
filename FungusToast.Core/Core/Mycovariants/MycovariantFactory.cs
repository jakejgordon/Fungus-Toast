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
                    {
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyMutationPointAward);
                        observer?.RecordMutationPointIncome(player.PlayerId, MycovariantGameBalance.PlasmidBountyMutationPointAward);
                    }
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

        public static Mycovariant MycelialBastion() =>
            new Mycovariant
            {
                Id = MycovariantIds.MycelialBastionId,
                Name = "Mycelial Bastion",
                Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
                FlavorText = "A fortified network of hyphae, woven to withstand any threat.",
                Type = MycovariantType.Active,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
                }
            };

        public static Mycovariant SurgicalInoculation() =>
            new Mycovariant
            {
                Id = 1011,
                Name = "Surgical Inoculation",
                Description = "Place a single Resistant (invincible) fungal cell anywhere on the board, except on top of another Resistant cell.",
                FlavorText = "A single spore, delivered with surgical precision, takes root where none could before.",
                Type = MycovariantType.Active,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null && player.PlayerType == PlayerTypeEnum.AI)
                    {
                        MycovariantEffectProcessor.ResolveSurgicalInoculation(playerMyco, board, rng, observer);
                    }
                    // For human players, do nothing here; the UI handler will handle the effect.
                }
            };

        public static Mycovariant PerimeterProliferator() =>
            new Mycovariant
            {
                Id = MycovariantIds.PerimeterProliferatorId,
                Name = "Perimeter Proliferator",
                Description = $"Multiplies the growth rate of your mold by {MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier}x when it is adjacent to the crust (the outer edge of the board).",
                FlavorText = "At the bread's edge, the colony finds untapped vigor, racing along the crust in a surge of expansion.",
                Type = MycovariantType.Passive,
                IsUniversal = false,
                // This mycovariant's effect should be checked/applied in the growth phase logic:
                // If a cell is adjacent to the board edge, double its growth rate for that cycle.
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Passive: No immediate effect. Growth logic must check for this mycovariant and apply the multiplier.
                }
            };

    }
}
