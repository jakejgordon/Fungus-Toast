using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
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

        public static int CountLegalOrthogonalGrowthTargets(GameBoard board, BoardTile sourceTile)
        {
            return board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                .Count(tile => !tile.IsOccupied && !board.IsTileBlockedForOccupation(tile.TileId));
        }

        public static bool QualifiesForCompactionPressure(GameBoard board, BoardTile sourceTile)
        {
            int targetCount = CountLegalOrthogonalGrowthTargets(board, sourceTile);
            return sourceTile.FungalCell?.IsAlive == true
                && targetCount >= GameBalance.CompactionPressureMinimumLegalOrthogonalTargets
                && targetCount <= GameBalance.CompactionPressureMaximumLegalOrthogonalTargets;
        }

        public static float GetCompactionPressureGrowthBonus(Player player, GameBoard board, BoardTile sourceTile)
        {
            int level = player.GetMutationLevel(MutationIds.CompactionPressure);
            if (level <= 0 || !QualifiesForCompactionPressure(board, sourceTile)) return 0f;
            return System.Math.Min(level * GameBalance.CompactionPressureEffectPerLevel, GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
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

        public static int CountEnemyToxinOrthogonalNeighbors(Player player, GameBoard board, BoardTile targetTile)
        {
            return board.GetOrthogonalNeighbors(targetTile.X, targetTile.Y)
                .Count(tile => tile.FungalCell is { IsToxin: true, OwnerPlayerId: int ownerPlayerId }
                    && ownerPlayerId != player.PlayerId);
        }

        public static bool QualifiesForToxinMargin(Player player, GameBoard board, BoardTile targetTile)
            => CountEnemyToxinOrthogonalNeighbors(player, board, targetTile) > 0;

        public static float GetToxinMarginGrowthBonus(Player player, GameBoard board, BoardTile targetTile)
        {
            int level = player.GetMutationLevel(MutationIds.ToxinMargin);
            if (level <= 0 || !QualifiesForToxinMargin(player, board, targetTile))
            {
                return 0f;
            }

            return System.Math.Min(level * GameBalance.ToxinMarginEffectPerLevel, GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
        }

        public static BoardTile? FindFriendlyToxinOrthogonalNeighbor(Player player, GameBoard board, BoardTile targetTile)
        {
            return board.GetOrthogonalNeighbors(targetTile.X, targetTile.Y)
                .Where(tile => tile.FungalCell is { IsToxin: true, OwnerPlayerId: int ownerPlayerId }
                    && ownerPlayerId == player.PlayerId)
                .OrderBy(tile => tile.TileId)
                .FirstOrDefault();
        }

        public static bool QualifiesForToxinborneSeeding(Player player, GameBoard board, BoardTile targetTile)
            => FindFriendlyToxinOrthogonalNeighbor(player, board, targetTile) != null;

        public static float GetToxinborneSeedingGrowthBonus(Player player, GameBoard board, BoardTile targetTile)
        {
            int level = player.GetMutationLevel(MutationIds.MycotoxinFission);
            if (level <= 0 || !QualifiesForToxinborneSeeding(player, board, targetTile))
            {
                return 0f;
            }

            return System.Math.Min(level * GameBalance.ToxinborneSeedingEffectPerLevel, GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
        }

        /// <summary>
        /// Resolves the immediate post-growth toxin relocation. The selected toxin carries the
        /// newly colonized cell to an enemy-adjacent landing site without refreshing its lifespan.
        /// </summary>
        public static ToxinborneSeedingResult TryResolveToxinborneSeeding(
            GameBoard board,
            Player player,
            int colonizedTileId,
            Random rng,
            ISimulationObserver observer)
        {
            int level = player.GetMutationLevel(MutationIds.MycotoxinFission);
            BoardTile? colonizedTile = board.GetTileById(colonizedTileId);
            if (level <= 0 || colonizedTile == null)
            {
                return ToxinborneSeedingResult.None;
            }

            BoardTile? toxinTile = FindFriendlyToxinOrthogonalNeighbor(player, board, colonizedTile);
            FungalCell? toxinCell = toxinTile?.FungalCell;
            if (toxinTile == null || toxinCell is not { IsToxin: true })
            {
                return ToxinborneSeedingResult.None;
            }

            int remainingLifespan = System.Math.Max(1, toxinCell.ToxinExpirationAge - toxinCell.GrowthCycleAge);
            List<BoardTile> candidateTiles = ToxinHelper.FindMycotoxinTargetTiles(board, player)
                .ToList();
            if (candidateTiles.Count == 0)
            {
                return ToxinborneSeedingResult.None;
            }

            BoardTile toxinLandingTile = candidateTiles[rng.Next(candidateTiles.Count)];
            board.RemoveCellInternal(toxinTile.TileId, removeControl: true);
            board.RemoveCellInternal(colonizedTileId, removeControl: true);
            ToxinHelper.ConvertToToxin(board, toxinLandingTile.TileId, remainingLifespan, GrowthSource.ToxinborneSeeding, player);

            List<BoardTile> carriedCellLandingTiles = board.GetOrthogonalNeighbors(toxinLandingTile.TileId)
                .Where(tile => !tile.IsOccupied && !tile.IsResistant && !board.IsTileBlockedForOccupation(tile.TileId))
                .ToList();
            bool carriedCellLanded = carriedCellLandingTiles.Count > 0;
            if (carriedCellLanded)
            {
                BoardTile carriedCellLandingTile = carriedCellLandingTiles[rng.Next(carriedCellLandingTiles.Count)];
                board.PlaceFungalCell(new FungalCell(
                    ownerPlayerId: player.PlayerId,
                    tileId: carriedCellLandingTile.TileId,
                    source: GrowthSource.ToxinborneSeeding,
                    lastOwnerPlayerId: null));
                board.OnHyphalGrowthVisualized(player.PlayerId, toxinLandingTile.TileId, carriedCellLandingTile.TileId);
            }

            observer.RecordToxinborneSeeding(player.PlayerId, toxinRelocated: true, carriedCellLanded);
            return new ToxinborneSeedingResult(toxinRelocated: true, carriedCellLanded);
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

    public readonly struct ToxinborneSeedingResult
    {
        public static ToxinborneSeedingResult None => new ToxinborneSeedingResult(false, false);

        public ToxinborneSeedingResult(bool toxinRelocated, bool carriedCellLanded)
        {
            ToxinRelocated = toxinRelocated;
            CarriedCellLanded = carriedCellLanded;
        }

        public bool ToxinRelocated { get; }
        public bool CarriedCellLanded { get; }
    }
}
