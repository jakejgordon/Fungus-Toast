using FungusToast.Core.Board;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mycovariants;

public class MycovariantEffectProcessorHelperTests
{
    [Fact]
    public void GenerateBresenhamLine_returns_single_point_when_start_and_end_match()
    {
        var line = MycovariantEffectProcessor.GenerateBresenhamLine(2, 3, 2, 3);

        Assert.Equal(new[] { (2, 3) }, line);
    }

    [Fact]
    public void GenerateBresenhamLine_returns_inclusive_horizontal_line()
    {
        var line = MycovariantEffectProcessor.GenerateBresenhamLine(1, 2, 4, 2);

        Assert.Equal(new[] { (1, 2), (2, 2), (3, 2), (4, 2) }, line);
    }

    [Fact]
    public void GenerateBresenhamLine_returns_inclusive_vertical_line()
    {
        var line = MycovariantEffectProcessor.GenerateBresenhamLine(2, 1, 2, 4);

        Assert.Equal(new[] { (2, 1), (2, 2), (2, 3), (2, 4) }, line);
    }

    [Fact]
    public void GenerateBresenhamLine_returns_inclusive_diagonal_line()
    {
        var line = MycovariantEffectProcessor.GenerateBresenhamLine(1, 1, 4, 4);

        Assert.Equal(new[] { (1, 1), (2, 2), (3, 3), (4, 4) }, line);
    }

    [Fact]
    public void GenerateBresenhamLine_handles_steep_lines()
    {
        var line = MycovariantEffectProcessor.GenerateBresenhamLine(1, 1, 3, 6);

        Assert.Equal((1, 1), line.First());
        Assert.Equal((3, 6), line.Last());
        Assert.Equal(6, line.Count);
        Assert.All(line.Zip(line.Skip(1)), step =>
        {
            int dx = Math.Abs(step.First.Item1 - step.Second.Item1);
            int dy = Math.Abs(step.First.Item2 - step.Second.Item2);
            Assert.InRange(dx, 0, 1);
            Assert.InRange(dy, 0, 1);
            Assert.True(dx + dy >= 1, "Expected Bresenham line to advance at least one axis per step.");
        });
    }

    [Fact]
    public void EvaluateHyphalDrawScore_returns_fallback_score_when_no_plan_exists()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);

        var score = MycovariantEffectProcessor.EvaluateHyphalDrawScore(player, board);

        Assert.Equal(1f, score);
    }

    [Fact]
    public void EvaluateHyphalDrawScore_returns_bounded_score_when_plan_exists()
    {
        var board = new GameBoard(width: 7, height: 3, playerCount: 2);
        var player0 = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        var player1 = new Player(playerId: 1, playerName: "P1", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player0);
        board.Players.Add(player1);
        board.PlaceInitialSpore(playerId: 0, x: 0, y: 1);
        board.PlaceInitialSpore(playerId: 1, x: 6, y: 1);
        board.SpawnSporeForPlayer(player0, tileId: 8, FungusToast.Core.Growth.GrowthSource.HyphalOutgrowth);
        board.SpawnSporeForPlayer(player0, tileId: 9, FungusToast.Core.Growth.GrowthSource.HyphalOutgrowth);

        var score = MycovariantEffectProcessor.EvaluateHyphalDrawScore(player0, board);

        Assert.InRange(score, 1f, 10f);
        Assert.True(score > 1f, "Expected a viable Hyphal Draw plan to score above the fallback value.");
    }
}
