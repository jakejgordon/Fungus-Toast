using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;

namespace FungusToast.Core.Mycovariants
{
    internal static class GrowthMycovariantFactory
    {
        public static IEnumerable<Mycovariant> CreateAll()
        {
            yield return PerimeterProliferator();
            yield return CornerConduitI();
            yield return CornerConduitII();
            yield return CornerConduitIII();
        }

        private static Mycovariant PerimeterProliferator() => new Mycovariant
        {
            Id = MycovariantIds.PerimeterProliferatorId,
            Name = "Perimeter Proliferator",
            Description = $"Multiplies the growth rate of your mold by {MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier}x when it is within {MycovariantGameBalance.PerimeterProliferatorEdgeDistance} tiles of the edge of the board (the crust).",
            FlavorText = "At the bread's edge, the colony finds untapped vigor, racing along the crust in a surge of expansion.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = true,
            AIScore = (player, board) =>
            {
                int borderCells = 0;
                foreach (var tile in board.AllTiles())
                {
                    if (Board.BoardUtilities.IsWithinEdgeDistance(tile, board.Width, board.Height, MycovariantGameBalance.PerimeterProliferatorEdgeDistance) &&
                        tile.FungalCell?.IsAlive == true &&
                        tile.FungalCell.OwnerPlayerId == player.PlayerId)
                    {
                        borderCells++;
                    }
                }
                float score = System.Math.Min(10f, System.Math.Max(1f, 1f + (borderCells * 9f / 100f)));
                return score;
            }
        };

        private static Mycovariant CornerConduitI() => new Mycovariant
        {
            Id = MycovariantIds.CornerConduitIId,
            Name = "Corner Conduit I",
            Description = $"Pre-growth: path to nearest corner. Replace up to {MycovariantGameBalance.CornerConduitIReplacementsPerPhase} actionable tile (Empty=Colonize, Dead=Reclaim, Enemy=Infest, Toxin=Overgrow). Skips friendly living & enemy Resistant.",
            FlavorText = "Hyphae prioritize a direct arterial route to a strategic corner, exploiting vulnerabilities along the corridor.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = true,
            AutoMarkTriggered = true,
            AIScore = (player, board) => CornerConduitScore(player, board, 6f, 4f, 2f)
        };

        private static Mycovariant CornerConduitII() => new Mycovariant
        {
            Id = MycovariantIds.CornerConduitIIId,
            Name = "Corner Conduit II",
            Description = $"Pre-growth: corner path. Replace up to {MycovariantGameBalance.CornerConduitIIReplacementsPerPhase} actionable tiles (same rules as I).",
            FlavorText = "Hyphal arterial routing intensifies, widening strategic throughput toward a dominant corner nexus.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = true,
            AIScore = (player, board) => CornerConduitScore(player, board, 7f, 5f, 3f)
        };

        private static Mycovariant CornerConduitIII() => new Mycovariant
        {
            Id = MycovariantIds.CornerConduitIIIId,
            Name = "Corner Conduit III",
            Description = $"Pre-growth: corner path. Replace up to {MycovariantGameBalance.CornerConduitIIIReplacementsPerPhase} actionable tiles (same rules as I).",
            FlavorText = "A fully vascularized hyphal highway surges toward strategic dominance, overwhelming resistance in a focused advance.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = true,
            AIScore = (player, board) => CornerConduitScore(player, board, 8f, 6f, 4f)
        };

        private static float CornerConduitScore(Players.Player player, Board.GameBoard board, float high, float mid, float low)
        {
            if (!player.StartingTileId.HasValue) return 1f;
            var (sx, sy) = board.GetXYFromTileId(player.StartingTileId.Value);
            var corners = new System.Collections.Generic.List<(int x,int y)>{(0,0),(board.Width-1,0),(board.Width-1,board.Height-1),(0,board.Height-1)};
            int bestIndex = 0; int bestDist = int.MaxValue;
            for(int i=0;i<corners.Count;i++)
            { var c = corners[i]; int dist = System.Math.Abs(c.x - sx) + System.Math.Abs(c.y - sy); if (dist < bestDist){ bestDist = dist; bestIndex = i; } }
            var target = corners[bestIndex];
            var line = MycovariantEffectProcessor.GenerateBresenhamLine(sx, sy, target.x, target.y);
            if (line.Count <= 1) return 1f;
            int actionable = 0;
            for(int i=1;i<line.Count;i++)
            {
                int tx = line[i].x; int ty = line[i].y; int tileId = ty * board.Width + tx;
                var tile = board.GetTileById(tileId);
                var cell = tile?.FungalCell;
                if (cell != null && cell.IsAlive && cell.OwnerPlayerId == player.PlayerId) continue;
                if (cell != null && cell.IsAlive && cell.OwnerPlayerId != player.PlayerId && cell.IsResistant) continue;
                actionable++;
            }
            int denom = line.Count - 1;
            float fraction = denom > 0 ? (float)actionable / denom : 0f;
            if (fraction >= 0.75f) return high;
            if (fraction >= 0.50f) return mid;
            if (fraction > 0f) return low;
            return 1f;
        }
    }
}
