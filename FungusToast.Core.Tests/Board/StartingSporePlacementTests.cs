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

        var startingTileId = Assert.IsType<int>(players[0].StartingTileId);
        var humanPosition = (startingTileId % board.Width, startingTileId / board.Width);
        Assert.Contains(humanPosition, overridePositions);
    }

    [Fact]
    public void PlaceStartingSpores_with_preferred_player_positions_reserves_that_slot_for_the_requested_player()
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

        StartingSporeUtility.PlaceStartingSpores(
            board,
            players,
            rng,
            shufflePlayerOrder: true,
            overridePositions: overridePositions,
            preferredPositionsByPlayerId: new Dictionary<int, (int x, int y)>
            {
                [0] = overridePositions[1]
            });

        var humanStartTileId = Assert.IsType<int>(players[0].StartingTileId);
        Assert.Equal((18, 1), (humanStartTileId % board.Width, humanStartTileId / board.Width));

        var occupiedTiles = overridePositions
            .Select(position => board.GetTile(position.Item1, position.Item2))
            .Where(tile => tile is { IsOccupied: true })
            .ToArray();

        Assert.Equal(4, occupiedTiles.Length);
        Assert.Equal(4, players.Select(player => player.StartingTileId).Distinct().Count());
    }

    [Fact]
    public void PlaceStartingSpores_relocates_blocked_slots_to_nearest_playable_tiles()
    {
        var blockedTileIds = new[] { 0, 4, 20, 24 };
        var board = CreateBoard(width: 5, height: 5, playerCount: 4, blockedTileIds);
        var players = board.Players;
        var overridePositions = new[]
        {
            (0, 0),
            (4, 0),
            (0, 4),
            (4, 4),
        };

        StartingSporeUtility.PlaceStartingSpores(board, players, new Random(7), shufflePlayerOrder: false, overridePositions);

        Assert.All(players, player => Assert.True(player.StartingTileId.HasValue));
        Assert.DoesNotContain(players.Select(player => player.StartingTileId!.Value), blockedTileIds.Contains);
        Assert.All(players, player => Assert.False(board.GetTileById(player.StartingTileId!.Value)!.IsBlocked));
    }

    [Fact]
    public void PlaceStartingSpores_keeps_all_players_inside_a_narrow_playable_strip()
    {
        const int width = 120;
        const int height = 120;

        var blockedTileIds = Enumerable.Range(0, width * height)
            .Where(tileId =>
            {
                int y = tileId / width;
                return y < 35 || y > 84;
            })
            .ToArray();

        for (int playerCount = 2; playerCount <= 8; playerCount++)
        {
            var board = CreateBoard(width, height, playerCount, blockedTileIds);
            var players = board.Players;
            var preferredPositions = StartingSporeUtility.GetStartingPositions(width, height, playerCount);

            StartingSporeUtility.PlaceStartingSpores(board, players, new Random(11), shufflePlayerOrder: false);

            Assert.All(players, player => Assert.True(player.StartingTileId.HasValue));
            Assert.Equal(playerCount, players.Select(player => player.StartingTileId!.Value).Distinct().Count());
            Assert.All(players, player => Assert.False(board.GetTileById(player.StartingTileId!.Value)!.IsBlocked));

            bool anyRelocated = players
                .Select((player, index) =>
                {
                    int tileId = player.StartingTileId!.Value;
                    var finalPosition = (tileId % width, tileId / width);
                    return finalPosition != preferredPositions[index];
                })
                .Any(relocated => relocated);

            if (playerCount >= 3)
            {
                Assert.True(anyRelocated, $"Expected at least one relocation for player count {playerCount}.");
            }
        }
    }

    [Fact]
    public void PlaceStartingSpores_relocates_edge_preferences_at_least_three_tiles_inward_when_possible()
    {
        var blockedTileIds = Enumerable.Range(0, 12 * 12)
            .Where(tileId =>
            {
                int x = tileId % 12;
                int y = tileId / 12;
                return x < 2 || x > 9 || y < 2 || y > 9;
            })
            .ToArray();

        var board = CreateBoard(width: 12, height: 12, playerCount: 4, blockedTileIds);
        var players = board.Players;
        StartingSporeUtility.PlaceStartingSpores(board, players, new Random(17), shufflePlayerOrder: false);

        var finalPositions = players
            .Select(player =>
            {
                int tileId = Assert.IsType<int>(player.StartingTileId);
                return (x: tileId % board.Width, y: tileId / board.Width);
            })
            .ToArray();

        Assert.Equal(4, finalPositions.Distinct().Count());
        Assert.All(finalPositions, position =>
        {
            Assert.InRange(position.x, 5, 6);
            Assert.InRange(position.y, 5, 6);
        });
    }

    [Fact]
    public void PlaceStartingSpores_allows_authored_exceptions_to_bypass_minimum_edge_distance()
    {
        var blockedTileIds = Enumerable.Range(0, 12 * 12)
            .Where(tileId =>
            {
                int x = tileId % 12;
                int y = tileId / 12;
                return x < 2 || x > 9 || y < 2 || y > 9;
            })
            .ToArray();

        var board = CreateBoard(width: 12, height: 12, playerCount: 2, blockedTileIds);
        var players = board.Players;

        StartingSporeUtility.PlaceStartingSpores(
            board,
            players,
            new Random(17),
            shufflePlayerOrder: false,
            preferredPositionsByPlayerId: new Dictionary<int, (int x, int y)>
            {
                [0] = (2, 2)
            },
            enforceMinimumPlayableEdgeDistanceForPreferredPositions: true,
            ignoreMinimumPlayableEdgeDistancePlayerIds: new HashSet<int> { 0 });

        int playerZeroTileId = Assert.IsType<int>(players[0].StartingTileId);
        int playerOneTileId = Assert.IsType<int>(players[1].StartingTileId);

        Assert.Equal((2, 2), (playerZeroTileId % board.Width, playerZeroTileId / board.Width));
        Assert.Equal((6, 6), (playerOneTileId % board.Width, playerOneTileId / board.Width));
    }

    private static GameBoard CreateBoard(int width, int height, int playerCount, IEnumerable<int>? blockedTileIds = null)
    {
        var board = new GameBoard(width, height, playerCount, blockedTileIds);
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
