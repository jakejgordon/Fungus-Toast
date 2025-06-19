using System.Collections.Generic;

namespace FungusToast.Core.Mycovariants
{
    public static class MycovariantRepository
    {
        public static List<Mycovariant> BuildAll()
        {
            return new List<Mycovariant>
            {
                MycovariantFactory.NecrophoricAdaptation(),
                MycovariantFactory.SaprobicRelay(),
                MycovariantFactory.SporodochialGrowth(),
                MycovariantFactory.JettingMyceliumNorth(),
                MycovariantFactory.JettingMyceliumEast(),
                MycovariantFactory.JettingMyceliumSouth(),
                MycovariantFactory.JettingMyceliumWest()
            };
        }
    }
}
