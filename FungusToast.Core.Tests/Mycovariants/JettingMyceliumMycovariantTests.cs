using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class JettingMyceliumMycovariantTests
{
    [Fact]
    public void EvaluatePlacement_ignores_resistant_enemy_cells_for_infest_and_toxify_scoring()
    {
        var board = new GameBoard(width: 12, height: 11, playerCount: 2);
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.Players.Add(enemy);

        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 1, y: 5);
        board.PlaceInitialSpore(playerId: enemy.PlayerId, x: 11, y: 10);

        PlaceResistantLivingCell(board, enemy, tileId: GetTileId(board, x: 2, y: 5));
        PlaceResistantLivingCell(board, enemy, tileId: GetTileId(board, x: 6, y: 6));

        var sourceCell = board.GetCell(owner.StartingTileId!.Value)!;

        float score = JettingMyceliumHelper.EvaluatePlacement(sourceCell, CardinalDirection.East, board, owner);

        Assert.Equal(0f, score);
    }

    [Fact]
    public void ResolveJettingMycelium_leaves_resistant_enemy_cells_alive_in_line_and_cone()
    {
        var board = new GameBoard(width: 12, height: 11, playerCount: 2);
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.Players.Add(enemy);

        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 1, y: 5);
        board.PlaceInitialSpore(playerId: enemy.PlayerId, x: 11, y: 10);

        int resistantLineTileId = GetTileId(board, x: 2, y: 5);
        int resistantConeTileId = GetTileId(board, x: 6, y: 6);
        PlaceResistantLivingCell(board, enemy, tileId: resistantLineTileId);
        PlaceResistantLivingCell(board, enemy, tileId: resistantConeTileId);

        foreach (int coneTileId in board.GetTileCone(owner.StartingTileId!.Value, CardinalDirection.East))
        {
            if (coneTileId == resistantLineTileId || coneTileId == resistantConeTileId)
            {
                continue;
            }

            PlaceOwnedLivingCell(board, owner, coneTileId);
        }

        var playerMyco = new PlayerMycovariant(
            owner.PlayerId,
            MycovariantIds.JettingMyceliumEastId,
            new Mycovariant { Id = MycovariantIds.JettingMyceliumEastId, Name = "Jetting Mycelium (East)" });

        MycovariantEffectProcessor.ResolveJettingMycelium(
            playerMyco,
            owner,
            board,
            tileId: owner.StartingTileId!.Value,
            direction: CardinalDirection.East,
            rng: new Random(123),
            observer: new TestSimulationObserver());

        AssertOwnedResistantLivingCell(board, resistantLineTileId, enemy.PlayerId);
        AssertOwnedResistantLivingCell(board, resistantConeTileId, enemy.PlayerId);

        Assert.Equal(0, playerMyco.EffectCounts.GetValueOrDefault(MycovariantEffectType.Infested));
        Assert.Equal(0, playerMyco.EffectCounts.GetValueOrDefault(MycovariantEffectType.Poisoned));
        Assert.Equal(0, playerMyco.EffectCounts.GetValueOrDefault(MycovariantEffectType.Colonized));
    }

    private static void PlaceResistantLivingCell(GameBoard board, Player owner, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.MakeResistant();
        board.PlaceFungalCell(cell);
    }

    private static void PlaceOwnedLivingCell(GameBoard board, Player owner, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
    }

    private static void AssertOwnedResistantLivingCell(GameBoard board, int tileId, int ownerPlayerId)
    {
        var cell = board.GetCell(tileId);
        Assert.NotNull(cell);
        Assert.True(cell!.IsAlive);
        Assert.True(cell.IsResistant);
        Assert.Equal(ownerPlayerId, cell.OwnerPlayerId);
    }

    private static int GetTileId(GameBoard board, int x, int y)
    {
        return y * board.Width + x;
    }
}