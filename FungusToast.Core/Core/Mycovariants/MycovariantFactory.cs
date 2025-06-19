using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using System;

namespace FungusToast.Core.Mycovariants
{
    public static class MycovariantFactory
    {
        public static Mycovariant JettingMyceliumNorth() =>
            CreateJettingMycelium("North", MycovariantGameBalance.JettingMyceliumNorthId);

        public static Mycovariant JettingMyceliumEast() =>
            CreateJettingMycelium("East", MycovariantGameBalance.JettingMyceliumEastId);

        public static Mycovariant JettingMyceliumSouth() =>
            CreateJettingMycelium("South", MycovariantGameBalance.JettingMyceliumSouthId);

        public static Mycovariant JettingMyceliumWest() =>
            CreateJettingMycelium("West", MycovariantGameBalance.JettingMyceliumWestId);

        private static Mycovariant CreateJettingMycelium(string direction, int id)
        {
            return new Mycovariant
            {
                Id = id,
                Name = $"Jetting Mycelium ({direction})",
                Description = $"Immediately grow {MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles} mold tiles {direction.ToLower()} from a chosen cell, followed by {MycovariantGameBalance.JettingMyceliumNumberOfToxinTiles} toxin tiles.",
                FlavorText = $"The cap cracks. The colony launches {direction.ToLower()}ward.",
                Type = MycovariantType.Directional,
                ApplyEffect = (playerMyco, board, rng) =>
                {
                    // TODO: Implement direction-specific growth + toxin trail
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
    }
}
