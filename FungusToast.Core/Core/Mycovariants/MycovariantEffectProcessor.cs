using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
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

        int livingLength = MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles;
        int toxinCount = MycovariantGameBalance.JettingMyceliumNumberOfToxinTiles;
        int totalLength = livingLength + toxinCount;

        var line = board.GetTileLine(tileId, direction, totalLength, includeStartingTile: false);

        // Outcome tallies
        int infested = 0;
        int reclaimed = 0;
        int catabolicGrowth = 0;
        int alreadyOwned = 0;
        int poisoned = 0; // Replaces "toxified"
        int colonized = 0;
        int invalid = 0;

        List<FungalCell> affectedCells = new();

        // 1. For each tile in the line, up to livingLength, create or replace with a living cell for the player
        for (int i = 0; i < line.Count && i < livingLength; i++)
        {
            var targetTile = board.GetTileById(line[i]);
            if (targetTile == null) { invalid++; continue; }

            var prevCell = targetTile.FungalCell;

            if (prevCell == null)
            {
                // Place a new living cell
                var newCell = new FungalCell(playerId, line[i]);
                targetTile.PlaceFungalCell(newCell);
                board.Players[playerId].ControlledTileIds.Add(line[i]);
                colonized++;
                affectedCells.Add(newCell);
            }
            else
            {
                // Takeover replaces with living cell for the player (allowToxin = true to overwrite toxins too)
                var takeoverResult = prevCell.Takeover(playerId, allowToxin: true);

                switch (takeoverResult)
                {
                    case FungalCellTakeoverResult.Parasitized: infested++; break;
                    case FungalCellTakeoverResult.Reclaimed: reclaimed++; break;
                    case FungalCellTakeoverResult.CatabolicGrowth: catabolicGrowth++; break;
                    case FungalCellTakeoverResult.AlreadyOwned: alreadyOwned++; break;
                    case FungalCellTakeoverResult.Invalid: invalid++; break;
                }
                var updatedCell = targetTile.FungalCell;
                if (updatedCell != null && updatedCell.OwnerPlayerId == playerId && updatedCell.IsAlive)
                {
                    affectedCells.Add(updatedCell);
                }
            }
        }

        // 2. For the last N tiles (toxinCount), convert to toxin
        for (int i = livingLength; i < line.Count && i < totalLength; i++)
        {
            var targetTile = board.GetTileById(line[i]);
            if (targetTile == null) { invalid++; continue; }

            int baseDuration = MycovariantGameBalance.DefaultJettingMyceliumToxinGrowthCycleDuration;
            int bonus = player.GetMutationLevel(MutationIds.MycotoxinPotentiation) * GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel;
            int expirationCycle = board.CurrentGrowthCycle + baseDuration + bonus;

            var prevCell = targetTile.FungalCell;
            if (prevCell == null || prevCell.IsDead)
            {
                // Toxify empty or dead cell (call it "toxified" = "placed toxin")
                var toxinCell = new FungalCell(playerId, line[i], expirationCycle);
                targetTile.PlaceFungalCell(toxinCell);
                poisoned++; // toxified → poisoned (for simulation output)
            }
            else if (prevCell.IsAlive && prevCell.OwnerPlayerId != playerId)
            {
                // Replace enemy living cell with toxin ("poisoned" = killed by toxin)
                prevCell.ConvertToToxin(expirationCycle, player, DeathReason.JettingMycelium, board.CurrentGrowthCycle);
                poisoned++; // poisoned = killed by toxin
            }
            // Do not overwrite your own living cell with toxin.
        }

        // 3. Record effect counts directly on the mycovariant for reporting
        playerMyco.IncrementEffectCount(MycovariantEffectType.Infested, infested);
        playerMyco.IncrementEffectCount(MycovariantEffectType.Reclaimed, reclaimed);
        playerMyco.IncrementEffectCount(MycovariantEffectType.CatabolicGrowth, catabolicGrowth);
        playerMyco.IncrementEffectCount(MycovariantEffectType.Colonized, colonized);
        playerMyco.IncrementEffectCount(MycovariantEffectType.Poisoned, poisoned);


        // 4. Report results to simulation observer, if available
        if (observer != null)
        {
            if (infested > 0) observer.ReportJettingMyceliumInfested(playerId, infested);
            if (reclaimed > 0) observer.ReportJettingMyceliumReclaimed(playerId, reclaimed);
            if (catabolicGrowth > 0) observer.ReportJettingMyceliumCatabolicGrowth(playerId, catabolicGrowth);
            if (alreadyOwned > 0) observer.ReportJettingMyceliumAlreadyOwned(playerId, alreadyOwned);
            if (poisoned > 0) observer.ReportJettingMyceliumPoisoned(playerId, poisoned);
            if (colonized > 0) observer.ReportJettingMyceliumColonized(playerId, colonized);
            if (invalid > 0) observer.ReportJettingMyceliumInvalid(playerId, invalid);
        }

        playerMyco.MarkTriggered();
    }






}
