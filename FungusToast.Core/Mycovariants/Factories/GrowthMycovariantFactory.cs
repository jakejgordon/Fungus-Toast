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
            yield return HyphalDraw();
            yield return CornerConduitI();
            yield return CornerConduitII();
            yield return CornerConduitIII();
        }

        private static Mycovariant PerimeterProliferator() => new Mycovariant
        {
            Id = MycovariantIds.PerimeterProliferatorId,
            Name = "Perimeter Proliferator",
            Description = $"For the rest of the game, your growth gets a {MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier}x multiplier within {MycovariantGameBalance.PerimeterProliferatorEdgeDistance} tiles of the board edge (the crust).",
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
                    if (Board.BoardUtilities.IsWithinEdgeDistance(tile, board, MycovariantGameBalance.PerimeterProliferatorEdgeDistance) &&
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
            Description = CreateCornerConduitDescription(MycovariantGameBalance.CornerConduitIReplacementsPerPhase),
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
            Description = CreateCornerConduitDescription(MycovariantGameBalance.CornerConduitIIReplacementsPerPhase),
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
            Description = CreateCornerConduitDescription(MycovariantGameBalance.CornerConduitIIIReplacementsPerPhase),
            FlavorText = "A fully vascularized hyphal highway surges toward strategic dominance, overwhelming resistance in a focused advance.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = true,
            AIScore = (player, board) => CornerConduitScore(player, board, 8f, 6f, 4f)
        };

        private static Mycovariant HyphalDraw() => new Mycovariant
        {
            Id = MycovariantIds.HyphalDrawId,
            Name = "Hyphal Draw",
            Description = "One-time on draft: trace from your starting spore toward the enemy start with the most living cells, pick up your non-Resistant living cells on that path, then redeploy them from the enemy side back toward you, skipping Resistant tiles.",
            FlavorText = "The colony cinches its vascular strand taut, hauling living biomass forward into a tighter assault lane.",
            IconId = "myco_hyphal_draw",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = false,
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                MycovariantEffectProcessor.ResolveHyphalDraw(playerMyco, board, rng, observer);
            },
            AIScore = (player, board) => MycovariantEffectProcessor.EvaluateHyphalDrawScore(player, board)
        };

        private static string CreateCornerConduitDescription(int tilesPerPhase)
            => MycovariantDescriptionFormatter.BuildGenericCornerConduitDescription(tilesPerPhase);

        private static float CornerConduitScore(Players.Player player, Board.GameBoard board, float high, float mid, float low)
        {
            if (!player.StartingTileId.HasValue) return 1f;
            var (sx, sy) = board.GetXYFromTileId(player.StartingTileId.Value);
            Board.BoardTile? targetTile = null;
            int bestDistance = int.MaxValue;
            foreach (Board.GameBoard.BoardCorner corner in System.Enum.GetValues(typeof(Board.GameBoard.BoardCorner)))
            {
                int? tileId = board.GetCornerTileId(corner);
                if (!tileId.HasValue)
                {
                    continue;
                }

                var tile = board.GetTileById(tileId.Value);
                if (tile == null)
                {
                    continue;
                }

                int distance = System.Math.Abs(tile.X - sx) + System.Math.Abs(tile.Y - sy);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    targetTile = tile;
                }
            }

            if (targetTile == null) return 1f;

            var target = (x: targetTile.X, y: targetTile.Y);
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
