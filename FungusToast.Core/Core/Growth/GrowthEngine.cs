using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Growth
{
    public static class GrowthEngine
    {
        public static Dictionary<int, int> ExecuteGrowthCycle(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null)
        {
            // Fire PreGrowthCycle event for Mycotoxin Catabolism and other pre-growth effects
            board.OnPreGrowthCycle();

            // Track failed growths for simulation analysis
            var failedGrowthsByPlayerId = players.ToDictionary(p => p.PlayerId, _ => 0);
            var activeFungalCells = board.AllLivingFungalCellsWithTiles().ToList();

            Shuffle(activeFungalCells, rng);

            foreach (var (tile, cell) in activeFungalCells)
            {
                if (!cell.OwnerPlayerId.HasValue)
                    continue;

                var owner = players[cell.OwnerPlayerId.Value];
                bool grewOrMoved = TryExpandFromTile(board, tile, owner, rng, observer);

                if (!grewOrMoved)
                    failedGrowthsByPlayerId[owner.PlayerId]++;
            }

            board.IncrementGrowthCycle();

            return failedGrowthsByPlayerId;
        }



        /// <summary>
        /// Attempts to grow or move from a single tile. Tracks orthogonal, diagonal (Tendril), Creeping Mold, and Necrohyphal Infiltration.
        /// </summary>
        private static bool TryExpandFromTile(
            GameBoard board,
            BoardTile sourceTile,
            Player owner,
            Random rng,
            ISimulationObserver? observer = null)
        {
            var sourceCell = sourceTile.FungalCell;
            if (sourceCell == null)
                return false;

            (float baseChance, float surgeBonus) = MutationEffectProcessor.GetGrowthChancesWithHyphalSurge(owner);
            float diagonalMultiplier = MutationEffectProcessor.GetTendrilDiagonalGrowthMultiplier(owner);

            var allTargets = GetAllGrowthTargets(board, sourceTile, owner, baseChance, surgeBonus, diagonalMultiplier);
            Shuffle(allTargets, rng);

            foreach (var target in allTargets)
            {
                if (AttemptStandardOrSurgeGrowth(board, owner, sourceTile.TileId, target, rng, observer))
                    return true;

                if (target == allTargets[0] && AttemptCreepingMold(board, owner, sourceCell, sourceTile, target.Tile, rng, observer))
                    return true;
            }

            return AttemptNecrohyphalInfiltration(board, sourceTile, sourceCell, owner, rng, observer);
        }

        private static List<GrowthTarget> GetAllGrowthTargets(
            GameBoard board,
            BoardTile sourceTile,
            Player owner,
            float baseChance,
            float surgeBonus,
            float diagonalMultiplier)
        {
            var targets = new List<GrowthTarget>();

            foreach (BoardTile tile in board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y))
            {
                if (!tile.IsOccupied && tile.TileId != sourceTile.TileId)
                    targets.Add(new GrowthTarget(tile, baseChance, null, surgeBonus));
            }

            var diagonalDirs = new (int dx, int dy, DiagonalDirection dir)[]
            {
        (-1,  1, DiagonalDirection.Northwest),
        ( 1,  1, DiagonalDirection.Northeast),
        ( 1, -1, DiagonalDirection.Southeast),
        (-1, -1, DiagonalDirection.Southwest),
            };

            foreach (var (dx, dy, dir) in diagonalDirs)
            {
                float chance = owner.GetDiagonalGrowthChance(dir) * diagonalMultiplier;
                if (chance <= 0) continue;

                int nx = sourceTile.X + dx;
                int ny = sourceTile.Y + dy;
                var maybeTile = board.GetTile(nx, ny);
                if (maybeTile is { IsOccupied: false, TileId: var id } && id != sourceTile.TileId)
                    targets.Add(new GrowthTarget(maybeTile, chance, dir, 0f));
            }

            return targets;
        }

        private static bool AttemptStandardOrSurgeGrowth(
            GameBoard board,
            Player owner,
            int sourceTileId,
            GrowthTarget target,
            Random rng,
            ISimulationObserver? observer)
        {
            double roll = rng.NextDouble();

            if (target.SurgeBonus > 0f && target.DiagonalDirection == null)
            {
                if (roll < target.Chance)
                {
                    if (board.TryGrowFungalCell(owner.PlayerId, sourceTileId, target.Tile.TileId, out var failReason))
                    {
                        observer?.RecordStandardGrowth(owner.PlayerId);
                        return true;
                    }
                }
                else if (roll < target.Chance + target.SurgeBonus)
                {
                    if (board.TryGrowFungalCell(owner.PlayerId, sourceTileId, target.Tile.TileId, out var failReason))
                    {
                        observer?.RecordHyphalSurgeGrowth(owner.PlayerId);
                        return true;
                    }
                }
            }
            else
            {
                if (roll < target.Chance)
                {
                    if (board.TryGrowFungalCell(owner.PlayerId, sourceTileId, target.Tile.TileId, out var failReason))
                    {
                        if (target.DiagonalDirection.HasValue)
                            observer?.RecordTendrilGrowth(owner.PlayerId, target.DiagonalDirection.Value);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool AttemptCreepingMold(
            GameBoard board,
            Player owner,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            ISimulationObserver? observer)
        {
            if (MutationEffectProcessor.TryCreepingMoldMove(owner, sourceCell, sourceTile, targetTile, rng, board))
            {
                observer?.RecordCreepingMoldMove(owner.PlayerId);
                return true;
            }

            return false;
        }

        private static bool AttemptNecrohyphalInfiltration(
            GameBoard board,
            BoardTile sourceTile,
            FungalCell sourceCell,
            Player owner,
            Random rng,
            ISimulationObserver? observer)
        {
            return MutationEffectProcessor.TryNecrohyphalInfiltration(board, sourceTile, sourceCell, owner, rng, observer);
        }


        /// <summary>
        /// Helper for shuffling lists with a given RNG.
        /// </summary>
        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Encapsulates a potential growth target, including diagonal direction if applicable.
        /// </summary>
        private sealed class GrowthTarget
        {
            public BoardTile Tile { get; }
            public float Chance { get; }
            public DiagonalDirection? DiagonalDirection { get; }
            public float SurgeBonus { get; } // Only for orthogonal targets

            public GrowthTarget(BoardTile tile, float chance, DiagonalDirection? dir, float surgeBonus)
            {
                Tile = tile;
                Chance = chance;
                DiagonalDirection = dir;
                SurgeBonus = surgeBonus;
            }
        }
    }
}
