using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Board
{
    public partial class GameBoard
    {
        private readonly HashSet<int> rotTileIds = new();
        public IReadOnlyCollection<int> RotTileIds => rotTileIds.OrderBy(id => id).ToArray();
        public float RotAdjacentDeathChance { get; private set; }
        public event Action<int>? RotPlaced;

        public void ConfigureRot(float adjacentDeathChance)
        {
            if (float.IsNaN(adjacentDeathChance) || float.IsInfinity(adjacentDeathChance)
                || adjacentDeathChance < 0f || adjacentDeathChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(adjacentDeathChance));
            RotAdjacentDeathChance = adjacentDeathChance;
        }

        public bool TryPlaceRot(int tileId)
        {
            var tile = GetTileById(tileId);
            if (tile == null || tile.IsOccupiedForSporePlacement || IsTileBlockedForOccupation(tileId))
                return false;
            tile.PlaceRot();
            rotTileIds.Add(tileId);
            RotPlaced?.Invoke(tileId);
            return true;
        }

        public bool IsAdjacentToRot(int tileId)
            => rotTileIds.Count > 0 && GetTileById(tileId) != null
                && GetOrthogonalNeighbors(tileId).Any(tile => tile.HasRot);
    }
}
