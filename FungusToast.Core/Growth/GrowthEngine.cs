using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Mycovariants;
using System.Linq;

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

            // Age all cells at the end of each growth cycle (per design principles)
            AgeCells(board, players);

            // Expire toxins after aging
            board.ExpireToxinTiles(board.CurrentGrowthCycle, observer);

            return failedGrowthsByPlayerId;
        }

        /// <summary>
        /// Ages all living and toxin cells. This should happen at the end of each growth cycle.
        /// </summary>
        private static void AgeCells(GameBoard board, List<Player> players)
        {
            // Age all living cells (with mutation-based age reset logic)
            List<BoardTile> livingTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsAlive: true })
                .ToList();

            foreach (BoardTile tile in livingTiles)
            {
                FungalCell cell = tile.FungalCell!;
                Player owner = players.First(p => p.PlayerId == cell.OwnerPlayerId);
                MutationEffectCoordinator.AdvanceOrResetCellAge(owner, cell);
            }

            // Age all toxin cells (no mutation effects, just simple aging)
            List<BoardTile> toxinTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsToxin: true })
                .ToList();

            foreach (BoardTile tile in toxinTiles)
            {
                FungalCell toxinCell = tile.FungalCell!;
                toxinCell.IncrementGrowthAge();
            }
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

            (float baseChance, float surgeBonus) = MutationEffectCoordinator.GetGrowthChancesWithHyphalSurge(owner);
            float diagonalMultiplier = MutationEffectCoordinator.GetTendrilDiagonalGrowthMultiplier(owner);

            float edgeMultiplier = GetEdgeGrowthMultiplier(owner, sourceTile, board);

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

            bool hasMaxCreepingMold = owner.GetMutationLevel(MutationIds.CreepingMold) == GameBalance.CreepingMoldMaxLevel;

            float edgeMultiplier = GetEdgeGrowthMultiplier(owner, sourceTile, board);

            foreach (BoardTile tile in board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y))
            {
                if (!tile.IsOccupied && tile.TileId != sourceTile.TileId)
                {
                    targets.Add(new GrowthTarget(tile, baseChance * edgeMultiplier, null, surgeBonus));
                }
                else if (hasMaxCreepingMold && tile.FungalCell != null && tile.FungalCell.IsToxin)
                {
                    targets.Add(new GrowthTarget(tile, baseChance * edgeMultiplier, null, surgeBonus));
                }
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
                float chance = owner.GetDiagonalGrowthChance(dir) * diagonalMultiplier * edgeMultiplier;
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
            // Prevent direct growth into any occupied tile (including toxins, dead cells, or living cells)
            if (target.Tile.IsOccupied) return false;

            double roll = rng.NextDouble();

            var (edgeMultiplier, baseChance) = GetPerimeterProliferatorContext(board, owner, sourceTileId, target);

            if (target.SurgeBonus > 0f && target.DiagonalDirection == null)
            {
                if (roll < target.Chance)
                {
                    if (board.TryGrowFungalCell(owner.PlayerId, sourceTileId, target.Tile.TileId, out var failReason))
                    {
                        MaybeRecordPerimeterProliferatorGrowth(observer, owner.PlayerId, edgeMultiplier, roll, baseChance, target.Chance);
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
                        MaybeRecordPerimeterProliferatorGrowth(observer, owner.PlayerId, edgeMultiplier, roll, baseChance, target.Chance);
                        if (target.DiagonalDirection.HasValue)
                            observer?.RecordTendrilGrowth(owner.PlayerId, target.DiagonalDirection.Value);
                        return true;
                    }
                }
            }

            return false;
        }

        // Returns (edgeMultiplier, baseChance)
        private static (float edgeMultiplier, float baseChance) GetPerimeterProliferatorContext(
            GameBoard board,
            Player owner,
            int sourceTileId,
            GrowthTarget target)
        {
            float edgeMultiplier = 1f;
            float baseChance = target.Chance;
            bool hasPerimeterProliferator = owner.PlayerMycovariants.Any(m => m.MycovariantId == MycovariantIds.PerimeterProliferatorId);
            if (hasPerimeterProliferator)
            {
                var sourceTile = board.GetTileById(sourceTileId);
                if (sourceTile != null)
                {
                    bool isEdgeCell = sourceTile.X == 0 || sourceTile.Y == 0 ||
                                      sourceTile.X == board.Width - 1 || sourceTile.Y == board.Height - 1;
                    edgeMultiplier = isEdgeCell ? MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier : 1f;
                }
            }
            if (edgeMultiplier > 1f)
            {
                baseChance = target.Chance / edgeMultiplier;
            }
            return (edgeMultiplier, baseChance);
        }

        private static void MaybeRecordPerimeterProliferatorGrowth(
            ISimulationObserver? observer,
            int playerId,
            float edgeMultiplier,
            double roll,
            float baseChance,
            float targetChance)
        {
            if (edgeMultiplier > 1f && roll >= baseChance && roll < targetChance)
            {
                observer?.RecordPerimeterProliferatorGrowth(playerId);
            }
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
            if (MutationEffectCoordinator.TryCreepingMoldMove(owner, sourceCell, sourceTile, targetTile, rng, board, observer))
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
            return MutationEffectCoordinator.TryNecrohyphalInfiltration(board, sourceTile, sourceCell, owner, rng, observer);
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

        private static float GetEdgeGrowthMultiplier(Player owner, BoardTile sourceTile, GameBoard board)
        {
            bool hasPerimeterProliferator = owner.PlayerMycovariants.Any(m => m.MycovariantId == MycovariantIds.PerimeterProliferatorId);
            bool isEdgeCell = sourceTile.X == 0 || sourceTile.Y == 0 ||
                              sourceTile.X == board.Width - 1 || sourceTile.Y == board.Height - 1;
            return (hasPerimeterProliferator && isEdgeCell)
                ? MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier
                : 1f;
        }
    }
}
