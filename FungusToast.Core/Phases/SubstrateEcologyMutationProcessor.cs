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
            return sourceTile.FungalCell is { IsAlive: true, GrowthCycleAge: >= GameBalance.AeratedFrontierMinimumEligibleGrowthCycleAge }
                && CountOpenOrthogonalSpaces(board, sourceTile)
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

        public static bool QualifiesForCrustwardTropism(GameBoard board, BoardTile sourceTile, BoardTile targetTile)
        {
            int? sourceDistance = board.GetPlayableEdgeDistance(sourceTile.TileId);
            int? targetDistance = board.GetPlayableEdgeDistance(targetTile.TileId);
            return sourceDistance.HasValue
                && targetDistance.HasValue
                && targetDistance.Value < sourceDistance.Value;
        }

        public static float GetCrustwardTropismGrowthBonus(Player player, GameBoard board, BoardTile sourceTile, BoardTile targetTile)
        {
            int level = player.GetMutationLevel(MutationIds.CrustwardTropism);
            if (level <= 0 || !QualifiesForCrustwardTropism(board, sourceTile, targetTile))
            {
                return 0f;
            }

            float bonus = level * GameBalance.CrustwardTropismEffectPerLevel;
            return System.Math.Min(bonus, GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
        }

        public static int CountNonToxinDeadOrthogonalNeighbors(GameBoard board, BoardTile targetTile)
        {
            return board.GetOrthogonalNeighbors(targetTile.X, targetTile.Y)
                .Count(tile => tile.FungalCell?.IsReclaimable == true);
        }

        public static bool QualifiesForDetritalEnzymes(GameBoard board, BoardTile targetTile)
        {
            return CountNonToxinDeadOrthogonalNeighbors(board, targetTile) > 0;
        }

        public static float GetDetritalEnzymesDenseDeadMatterBonus(Player player, GameBoard board, BoardTile targetTile)
        {
            return player.GetMutationLevel(MutationIds.DetritalEnzymes) >= GameBalance.DetritalEnzymesMaxLevel
                && CountNonToxinDeadOrthogonalNeighbors(board, targetTile) >= GameBalance.DetritalEnzymesDenseDeadMatterRequiredNeighbors
                ? GameBalance.DetritalEnzymesDenseDeadMatterBonus
                : 0f;
        }

        public static float GetDetritalEnzymesGrowthBonus(Player player, GameBoard board, BoardTile targetTile)
        {
            int level = player.GetMutationLevel(MutationIds.DetritalEnzymes);
            if (level <= 0 || !QualifiesForDetritalEnzymes(board, targetTile))
            {
                return 0f;
            }

            float bonus = level * GameBalance.DetritalEnzymesEffectPerLevel
                + GetDetritalEnzymesDenseDeadMatterBonus(player, board, targetTile);
            return System.Math.Min(bonus, GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
        }

        public static bool IsCrustwardTropismAutomaticCrustArrival(
            Player player,
            GameBoard board,
            BoardTile sourceTile,
            BoardTile targetTile)
        {
            return player.GetMutationLevel(MutationIds.CrustwardTropism) >= GameBalance.CrustwardTropismMaxLevel
                && board.IsPlayableEdgeTile(targetTile.TileId)
                && QualifiesForCrustwardTropism(board, sourceTile, targetTile);
        }

        private static bool IsOpenForAeratedFrontier(GameBoard board, BoardTile tile)
        {
            return !tile.IsOccupiedForSporePlacement
                && !board.IsTileBlockedForOccupation(tile.TileId);
        }
    }
}
