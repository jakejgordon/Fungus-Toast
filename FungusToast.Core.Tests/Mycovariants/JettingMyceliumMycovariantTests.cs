using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class JettingMyceliumMycovariantTests
{
    [Fact]
    public void Factory_exposes_three_jetting_tiers_with_expected_names_and_universal_flags()
    {
        var jettingMycovariants = MycovariantFactory.GetAll()
            .Where(mycovariant =>
                mycovariant.Id == MycovariantIds.JettingMyceliumIId ||
                mycovariant.Id == MycovariantIds.JettingMyceliumIIId ||
                mycovariant.Id == MycovariantIds.JettingMyceliumIIIId)
            .OrderBy(mycovariant => mycovariant.Id)
            .ToList();

        Assert.Collection(
            jettingMycovariants,
            tierI =>
            {
                Assert.Equal(MycovariantIds.JettingMyceliumIId, tierI.Id);
                Assert.Equal("Jetting Mycelium I", tierI.Name);
                Assert.True(tierI.IsUniversal);
                Assert.Contains("grow 3 living tiles", tierI.Description);
                Assert.Contains("up to 7 tiles wide", tierI.Description);
            },
            tierII =>
            {
                Assert.Equal(MycovariantIds.JettingMyceliumIIId, tierII.Id);
                Assert.Equal("Jetting Mycelium II", tierII.Name);
                Assert.False(tierII.IsUniversal);
                Assert.Contains("grow 3 living tiles", tierII.Description);
                Assert.Contains("up to 9 tiles wide", tierII.Description);
            },
            tierIII =>
            {
                Assert.Equal(MycovariantIds.JettingMyceliumIIIId, tierIII.Id);
                Assert.Equal("Jetting Mycelium III", tierIII.Name);
                Assert.False(tierIII.IsUniversal);
                Assert.Contains("grow 4 living tiles", tierIII.Description);
                Assert.Contains("up to 11 tiles wide", tierIII.Description);
            });
    }

    [Fact]
    public void GetTileCone_uses_requested_jetting_row_pattern_after_the_living_line()
    {
        var board = new GameBoard(width: 20, height: 20, playerCount: 2);

        int sourceTileId = GetTileId(board, x: 4, y: 10);

        var tierI = board.GetTileCone(sourceTileId, CardinalDirection.East, JettingMyceliumHelper.GetToxinRowWidthsForMycovariant(MycovariantIds.JettingMyceliumIId), JettingMyceliumHelper.GetLivingLengthForMycovariant(MycovariantIds.JettingMyceliumIId));
        var tierII = board.GetTileCone(sourceTileId, CardinalDirection.East, JettingMyceliumHelper.GetToxinRowWidthsForMycovariant(MycovariantIds.JettingMyceliumIIId), JettingMyceliumHelper.GetLivingLengthForMycovariant(MycovariantIds.JettingMyceliumIIId));
        var tierIII = board.GetTileCone(sourceTileId, CardinalDirection.East, JettingMyceliumHelper.GetToxinRowWidthsForMycovariant(MycovariantIds.JettingMyceliumIIIId), JettingMyceliumHelper.GetLivingLengthForMycovariant(MycovariantIds.JettingMyceliumIIIId));

        Assert.Equal(new[] { 3, 3, 5, 7 }, GetRowWidthsByColumn(board, tierI));
        Assert.Equal(new[] { 3, 3, 5, 7, 9 }, GetRowWidthsByColumn(board, tierII));
        Assert.Equal(new[] { 3, 3, 5, 7, 9, 11 }, GetRowWidthsByColumn(board, tierIII));

        Assert.DoesNotContain(GetTileId(board, x: 5, y: 10), tierI);
        Assert.DoesNotContain(GetTileId(board, x: 6, y: 10), tierI);
        Assert.DoesNotContain(GetTileId(board, x: 7, y: 10), tierI);
    }

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
            MycovariantIds.JettingMyceliumIId,
            new Mycovariant { Id = MycovariantIds.JettingMyceliumIId, Name = "Jetting Mycelium I" });

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
    }

    [Fact]
    public void FindBestPlacement_prefers_the_highest_scoring_direction()
    {
        var board = new GameBoard(width: 12, height: 11, playerCount: 2);
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.Players.Add(enemy);

        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 1, y: 5);
        board.PlaceInitialSpore(playerId: enemy.PlayerId, x: 11, y: 10);

        PlaceOwnedLivingCell(board, enemy, GetTileId(board, x: 2, y: 5));

        var bestPlacement = JettingMyceliumHelper.FindBestPlacement(owner, board);

        Assert.NotNull(bestPlacement);
        Assert.Equal(owner.StartingTileId!.Value, bestPlacement!.Value.sourceCell.TileId);
        Assert.Equal(CardinalDirection.East, bestPlacement.Value.direction);
        Assert.True(bestPlacement.Value.score > 0f);
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

    private static int[] GetRowWidthsByColumn(GameBoard board, IEnumerable<int> tileIds)
    {
        return tileIds
            .GroupBy(tileId => board.GetXYFromTileId(tileId).x)
            .OrderBy(group => group.Key)
            .Select(group => group.Count())
            .ToArray();
    }
}