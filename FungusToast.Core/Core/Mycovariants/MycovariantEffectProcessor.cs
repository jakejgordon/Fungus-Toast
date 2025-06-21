using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System.Numerics;

public static class MycovariantEffectProcessor
{
    public static void CheckAndTrigger(Player player, GameBoard board, ISimulationObserver? observer)
    {
        foreach (var playerMyco in player.PlayerMycovariants)
        {
            if (!playerMyco.HasTriggered &&
                playerMyco.Mycovariant.IsTriggerConditionMet?.Invoke(playerMyco, board) == true)
            {
                playerMyco.Mycovariant.ApplyEffect?.Invoke(playerMyco, board, new Random(), observer);
                playerMyco.MarkTriggered();
            }
        }
    }

    public static void ResolveJettingMycelium(
        PlayerMycovariant playerMyco,
        Player player,
        GameBoard board,
        int tileId,
        CardinalDirection direction,
        ISimulationObserver? observer = null)
    {
        int playerId = playerMyco.PlayerId;

        var line = board.GetTileLine(tileId, direction,
            MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles +
            MycovariantGameBalance.JettingMyceliumNumberOfToxinTiles);

        // Outcome tallies
        int parasitized = 0;
        int reclaimed = 0;
        int catabolicGrowth = 0;
        int alreadyOwned = 0;
        int invalid = 0;

        for (int i = 0; i < line.Count; i++)
        {
            var target = board.GetTileById(line[i]);
            if (target?.FungalCell == null)
                continue;

            // Jetting Mycelium takes over ANY cell, including toxins, so allowToxin: true
            var takeoverResult = target.FungalCell.Takeover(playerId, allowToxin: true);

            switch (takeoverResult)
            {
                case FungalCellTakeoverResult.Parasitized:
                    parasitized++;
                    break;
                case FungalCellTakeoverResult.Reclaimed:
                    reclaimed++;
                    break;
                case FungalCellTakeoverResult.CatabolicGrowth:
                    catabolicGrowth++;
                    break;
                case FungalCellTakeoverResult.AlreadyOwned:
                    alreadyOwned++;
                    break;
                case FungalCellTakeoverResult.Invalid:
                    invalid++;
                    break;
            }
        }

        // Report results to simulation observer, if available
        if (observer != null)
        {
            if (parasitized > 0) observer.ReportJettingMyceliumParasitized(playerId, parasitized);
            if (reclaimed > 0) observer.ReportJettingMyceliumReclaimed(playerId, reclaimed);
            if (catabolicGrowth > 0) observer.ReportJettingMyceliumCatabolicGrowth(playerId, catabolicGrowth);
            if (alreadyOwned > 0) observer.ReportJettingMyceliumAlreadyOwned(playerId, alreadyOwned);
            if (invalid > 0) observer.ReportJettingMyceliumInvalid(playerId, invalid);
        }

        playerMyco.MarkTriggered();
    }



}
