using System;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

var board = new GameBoard(width: 12, height: 11, playerCount: 2);
var owner = new Player(0, "P0", PlayerTypeEnum.AI);
var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
board.Players.Add(owner);
board.Players.Add(enemy);
board.PlaceInitialSpore(playerId: owner.PlayerId, x: 1, y: 5);
board.PlaceInitialSpore(playerId: enemy.PlayerId, x: 11, y: 10);
int resistantLineTileId = 5 * board.Width + 2;
int resistantConeTileId = 6 * board.Width + 6;
var resistantLine = new FungalCell(enemy.PlayerId, resistantLineTileId, GrowthSource.InitialSpore, null);
resistantLine.MakeResistant();
board.PlaceFungalCell(resistantLine);
var resistantCone = new FungalCell(enemy.PlayerId, resistantConeTileId, GrowthSource.InitialSpore, null);
resistantCone.MakeResistant();
board.PlaceFungalCell(resistantCone);
foreach (int coneTileId in board.GetTileCone(owner.StartingTileId.Value, CardinalDirection.East))
{
    if (coneTileId == resistantLineTileId || coneTileId == resistantConeTileId)
    {
        continue;
    }

    var cell = new FungalCell(owner.PlayerId, coneTileId, GrowthSource.InitialSpore, null);
    board.PlaceFungalCell(cell);
}
var playerMyco = new PlayerMycovariant(owner.PlayerId, MycovariantIds.JettingMyceliumEastId, new Mycovariant { Id = MycovariantIds.JettingMyceliumEastId, Name = "Jetting Mycelium (East)" });
MycovariantEffectProcessor.ResolveJettingMycelium(playerMyco, owner, board, owner.StartingTileId.Value, CardinalDirection.East, new Random(123), new TestSimulationObserver());
Console.WriteLine($"Effects: {string.Join(", ", playerMyco.EffectCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
Console.WriteLine($"Line cell owner={board.GetCell(resistantLineTileId)?.OwnerPlayerId} alive={board.GetCell(resistantLineTileId)?.IsAlive} resistant={board.GetCell(resistantLineTileId)?.IsResistant} toxin={board.GetCell(resistantLineTileId)?.IsToxin}");
Console.WriteLine($"Cone cell owner={board.GetCell(resistantConeTileId)?.OwnerPlayerId} alive={board.GetCell(resistantConeTileId)?.IsAlive} resistant={board.GetCell(resistantConeTileId)?.IsResistant} toxin={board.GetCell(resistantConeTileId)?.IsToxin}");
