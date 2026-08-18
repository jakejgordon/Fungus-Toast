using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Evaluates deterministic board-context bonuses owned by Substrate Ecology.
    /// </summary>
    public static class SubstrateEcologyMutationProcessor
    {
        public static int CountOpenOrthogonalSpaces(GameBoard board, BoardTile sourceTile)
        {
            return board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                .Count(tile => IsOpenForAeratedFrontier(board, tile));
        }

        public static bool QualifiesForAeratedFrontier(GameBoard board, BoardTile sourceTile)
        {
            return CountOpenOrthogonalSpaces(board, sourceTile)
                >= GameBalance.AeratedFrontierRequiredOpenOrthogonalSpaces;
        }

        public static float GetAeratedFrontierGrowthBonus(Player player, GameBoard board, BoardTile sourceTile)
        {
            int level = player.GetMutationLevel(MutationIds.AeratedFrontier);
            if (level <= 0 || !QualifiesForAeratedFrontier(board, sourceTile))
            {
                return 0f;
            }

            float bonus = level * GameBalance.AeratedFrontierEffectPerLevel;
            return System.Math.Min(bonus, GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
        }

        private static bool IsOpenForAeratedFrontier(GameBoard board, BoardTile tile)
        {
            return !tile.IsOccupiedForSporePlacement
                && !board.IsTileBlockedForOccupation(tile.TileId);
        }
    }
}
