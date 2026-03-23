using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Growth;

public class ChemotacticBeaconHelperTests
{
    [Fact]
    public void GetDirectionalRelation_returns_toward_for_closer_distance()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(2, 2));
        var markerTile = Assert.IsType<BoardTile>(board.GetTile(4, 2));

        var relation = ChemotacticBeaconHelper.GetDirectionalRelation(sourceTile, targetTile, board, markerTile.TileId);

        Assert.Equal(ChemotacticBeaconHelper.DirectionalRelation.Toward, relation);
    }

    [Fact]
    public void GetDirectionalRelation_returns_neutral_for_same_distance()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(2, 1));
        var markerTile = Assert.IsType<BoardTile>(board.GetTile(3, 2));

        var relation = ChemotacticBeaconHelper.GetDirectionalRelation(sourceTile, targetTile, board, markerTile.TileId);

        Assert.Equal(ChemotacticBeaconHelper.DirectionalRelation.Neutral, relation);
    }

    [Fact]
    public void GetDirectionalRelation_returns_away_for_farther_distance()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(0, 2));
        var markerTile = Assert.IsType<BoardTile>(board.GetTile(3, 2));

        var relation = ChemotacticBeaconHelper.GetDirectionalRelation(sourceTile, targetTile, board, markerTile.TileId);

        Assert.Equal(ChemotacticBeaconHelper.DirectionalRelation.Away, relation);
    }

    [Fact]
    public void ApplyDirectionalBias_multiplies_closer_growth_after_other_modifiers()
    {
        var (board, player) = CreateBoardWithActiveBeacon(sourceX: 2, sourceY: 2, beaconX: 4, beaconY: 2);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(2, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(3, 2));

        float adjustedChance = ChemotacticBeaconHelper.ApplyDirectionalBias(board, player, sourceTile, targetTile, chance: 0.2f);

        Assert.Equal(0.2f * GameBalance.ChemotacticBeaconTowardGrowthMultiplier, adjustedChance, precision: 3);
    }

    [Fact]
    public void ApplyDirectionalBias_leaves_neutral_growth_unchanged()
    {
        var (board, player) = CreateBoardWithActiveBeacon(sourceX: 1, sourceY: 2, beaconX: 3, beaconY: 2);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(2, 1));

        float adjustedChance = ChemotacticBeaconHelper.ApplyDirectionalBias(board, player, sourceTile, targetTile, chance: 0.2f);

        Assert.Equal(0.2f, adjustedChance, precision: 3);
    }

    [Fact]
    public void ApplyDirectionalBias_multiplies_away_growth_downward()
    {
        var (board, player) = CreateBoardWithActiveBeacon(sourceX: 2, sourceY: 2, beaconX: 4, beaconY: 2);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(2, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));

        float adjustedChance = ChemotacticBeaconHelper.ApplyDirectionalBias(board, player, sourceTile, targetTile, chance: 0.2f);

        Assert.Equal(0.2f * GameBalance.ChemotacticBeaconAwayGrowthMultiplier, adjustedChance, precision: 3);
    }

    private static (GameBoard board, Player player) CreateBoardWithActiveBeacon(int sourceX, int sourceY, int beaconX, int beaconY)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = 99
        };
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, sourceX, sourceY);
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 7, currentRound: 1);

        var mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.ChemotacticBeacon));
        bool activated = player.TryActivateTargetedSurge(mutation, board, beaconY * board.Width + beaconX, new FungusToast.Core.Tests.Mutations.TestSimulationObserver(), currentRound: 2);

        Assert.True(activated);
        return (board, player);
    }
}