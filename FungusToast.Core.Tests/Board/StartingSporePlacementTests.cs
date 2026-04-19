using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class StartingSporePlacementTests
{
    [Fact]
    public void PlaceStartingSpores_without_shuffle_places_each_player_on_the_matching_slot()
    {
        var board = CreateBoard(width: 20, height: 20, playerCount: 3);
        var players = board.Players;
        var rng = new Random(12345);
        var overridePositions = new[]
        {
            (2, 3),
            (10, 11),
            (17, 5),
        };

        StartingSporeUtility.PlaceStartingSpores(board, players, rng, shufflePlayerOrder: false, overridePositions);

        for (int playerId = 0; playerId < overridePositions.Length; playerId++)
        {
            var (x, y) = overridePositions[playerId];
            var tile = board.GetTile(x, y);

            var occupiedTile = Assert.IsType<BoardTile>(tile);
            Assert.NotNull(occupiedTile.FungalCell);
            var fungalCell = occupiedTile.FungalCell;

            Assert.Equal(playerId, fungalCell!.OwnerPlayerId);
            Assert.Equal(GrowthSource.InitialSpore, fungalCell.SourceOfGrowth);
            Assert.True(fungalCell.IsResistant, $"Expected starting spore for player {playerId} at ({x}, {y}) to be resistant.");
            Assert.Equal("Initial Spore", fungalCell.ResistanceSource);

            int expectedTileId = y * board.Width + x;
            Assert.Equal(expectedTileId, players[playerId].StartingTileId);
            Assert.Contains(expectedTileId, players[playerId].ControlledTileIds);
        }
    }

    [Fact]
    public void PlaceStartingSpores_with_shuffle_places_one_spore_for_each_player_on_unique_tiles()
    {
        var board = CreateBoard(width: 20, height: 20, playerCount: 4);
        var players = board.Players;
        var rng = new Random(12345);
        var overridePositions = new[]
        {
            (1, 1),
            (18, 1),
            (1, 18),
            (18, 18),
        };

        StartingSporeUtility.PlaceStartingSpores(board, players, rng, shufflePlayerOrder: true, overridePositions);

        var occupiedTiles = overridePositions
            .Select(position => board.GetTile(position.Item1, position.Item2))
            .Where(tile => tile is { IsOccupied: true })
            .ToArray();

        Assert.Equal(4, occupiedTiles.Length);
        Assert.Equal(4, players.Count(player => player.StartingTileId.HasValue));
        Assert.All(players, player =>
        {
            var controlledTileId = Assert.Single(player.ControlledTileIds);
            var startingTileId = Assert.IsType<int>(player.StartingTileId);
            Assert.Equal(controlledTileId, startingTileId);
        });
        var ownerIds = occupiedTiles
            .Select(tile => tile!.FungalCell!.OwnerPlayerId)
            .OrderBy(id => id)
            .Cast<int>()
            .ToArray();

        Assert.Equal(new[] { 0, 1, 2, 3 }, ownerIds);
    }

    [Fact]
    public void PlaceStartingSpores_with_shuffle_keeps_the_human_on_an_unoffset_base_slot_when_only_the_ai_has_an_edge_offset()
    {
        var board = CreateBoard(width: 20, height: 20, playerCount: 2);
        var players = board.Players;
        players[0] = CreatePlayer(0, PlayerTypeEnum.Human, startingSporeEdgeOffset: 0);
        players[1] = CreatePlayer(1, PlayerTypeEnum.AI, startingSporeEdgeOffset: 3);

        var rng = new Random(0);
        var overridePositions = new[]
        {
            (2, 10),
            (17, 10),
        };

        StartingSporeUtility.PlaceStartingSpores(
            board,
            players,
            rng,
            shufflePlayerOrder: true,
            overridePositions,
            edgeOffsets: new[] { 0, 3 });

        var humanPosition = (players[0].StartingTileId!.Value % board.Width, players[0].StartingTileId.Value / board.Width);
        Assert.Contains(humanPosition, overridePositions);
    }

    private static GameBoard CreateBoard(int width, int height, int playerCount)
    {
        var board = new GameBoard(width, height, playerCount);
        for (int playerId = 0; playerId < playerCount; playerId++)
        {
            board.Players.Add(new Player(playerId, $"Player {playerId}", PlayerTypeEnum.AI));
        }

        return board;
    }

    private static Player CreatePlayer(int playerId, PlayerTypeEnum playerType, int startingSporeEdgeOffset)
    {
        var player = new Player(playerId, $"Player {playerId}", playerType);
        player.SetMutationStrategy(new ParameterizedSpendingStrategy(
            strategyName: $"TestStrategy{playerId}",
            prioritizeHighTier: false,
            startingSporeEdgeOffset: startingSporeEdgeOffset));

        return player;
    }

    private static void AssertOccupiedByPlayer(GameBoard board, int x, int y, int playerId)
    {
        var tile = Assert.IsType<BoardTile>(board.GetTile(x, y));
        Assert.NotNull(tile.FungalCell);
        Assert.Equal(playerId, tile.FungalCell!.OwnerPlayerId);
    }
}
