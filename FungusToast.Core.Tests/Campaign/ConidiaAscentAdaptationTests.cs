using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Campaign;

public class ConidiaAscentAdaptationTests
{
    [Fact]
    public void ConidiaAscent_triggers_only_at_round_17()
    {
        var board = CreateBoardWithPlayer(6, 6, out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.ConidiaAscent));
        FillLivingBlock(board, player, startX: 0, startY: 0, size: 3);

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.All(GetBlockTileIds(board, 0, 0, 3), tileId => Assert.True(board.GetCell(tileId)?.IsAlive));

        AdvanceToRound(board, AdaptationGameBalance.ConidiaAscentTriggerRound);
        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.All(GetBlockTileIds(board, 0, 0, 3), tileId => Assert.True(board.GetCell(tileId)?.IsDead));
    }

    [Fact]
    public void ConidiaAscent_does_not_trigger_without_a_full_killable_3x3_source()
    {
        var board = CreateBoardWithPlayer(6, 6, out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.ConidiaAscent));
        AdvanceToRound(board, AdaptationGameBalance.ConidiaAscentTriggerRound);

        FillLivingBlock(board, player, startX: 0, startY: 0, size: 3);
        board.GetCell(GetTileId(2, 2, board.Width))!.MakeResistant();

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.False(player.GetAdaptation(AdaptationIds.ConidiaAscent)!.HasTriggered);
        Assert.All(GetBlockTileIds(board, 0, 0, 3), tileId => Assert.True(board.GetCell(tileId)?.IsAlive));
    }

    [Fact]
    public void ConidiaAscent_does_not_trigger_without_a_completely_empty_2x2_destination()
    {
        var board = CreateBoardWithPlayer(5, 5, out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.ConidiaAscent));
        AdvanceToRound(board, AdaptationGameBalance.ConidiaAscentTriggerRound);

        FillLivingBlock(board, player, startX: 0, startY: 0, size: 3);
        FillAllRemainingTiles(board, player, excludedTileIds: GetBlockTileIds(board, 0, 0, 3));

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.False(player.GetAdaptation(AdaptationIds.ConidiaAscent)!.HasTriggered);
        Assert.All(GetBlockTileIds(board, 0, 0, 3), tileId => Assert.True(board.GetCell(tileId)?.IsAlive));
    }

    [Fact]
    public void ConidiaAscent_uses_the_first_valid_source_block_found_by_board_scan()
    {
        var board = CreateBoardWithPlayer(8, 8, out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.ConidiaAscent));
        AdvanceToRound(board, AdaptationGameBalance.ConidiaAscentTriggerRound);

        FillLivingBlock(board, player, startX: 0, startY: 0, size: 3);
        FillLivingBlock(board, player, startX: 5, startY: 0, size: 3);

        SpecialBoardEventArgs? eventArgs = null;
        board.SpecialBoardEventTriggered += (_, args) => eventArgs = args;

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.NotNull(eventArgs);
        Assert.Equal(SpecialBoardEventKind.ConidiaAscentTriggered, eventArgs!.EventKind);
        Assert.Equal(GetTileId(1, 1, board.Width), eventArgs.SourceTileId);
        Assert.All(GetBlockTileIds(board, 0, 0, 3), tileId => Assert.True(board.GetCell(tileId)?.IsDead));
        Assert.All(GetBlockTileIds(board, 5, 0, 3), tileId => Assert.True(board.GetCell(tileId)?.IsAlive));
    }

    [Fact]
    public void ConidiaAscent_kills_the_3x3_source_and_roots_a_2x2_colony_at_a_valid_destination()
    {
        var board = CreateBoardWithPlayer(6, 6, out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.ConidiaAscent));
        AdvanceToRound(board, AdaptationGameBalance.ConidiaAscentTriggerRound);

        FillLivingBlock(board, player, startX: 0, startY: 0, size: 3);

        SpecialBoardEventArgs? eventArgs = null;
        board.SpecialBoardEventTriggered += (_, args) => eventArgs = args;

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.NotNull(eventArgs);
        Assert.Equal(SpecialBoardEventKind.ConidiaAscentTriggered, eventArgs!.EventKind);
        Assert.Equal(9, eventArgs.AffectedTileIds.Count);
        Assert.All(GetBlockTileIds(board, 0, 0, 3), tileId => Assert.True(board.GetCell(tileId)?.IsDead));

        var destinationTileIds = GetBlockTileIds(board, eventArgs.DestinationTileId % board.Width, eventArgs.DestinationTileId / board.Width, 2);
        Assert.All(destinationTileIds, tileId =>
        {
            var cell = board.GetCell(tileId);
            Assert.NotNull(cell);
            Assert.True(cell!.IsAlive);
            Assert.Equal(player.PlayerId, cell.OwnerPlayerId);
        });
    }

    [Fact]
    public void ConidiaAscent_marks_itself_triggered_after_the_first_successful_launch()
    {
        var board = CreateBoardWithPlayer(6, 6, out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.ConidiaAscent));
        AdvanceToRound(board, AdaptationGameBalance.ConidiaAscentTriggerRound);

        FillLivingBlock(board, player, startX: 0, startY: 0, size: 3);

        int eventCount = 0;
        board.SpecialBoardEventTriggered += (_, args) =>
        {
            if (args.EventKind == SpecialBoardEventKind.ConidiaAscentTriggered)
            {
                eventCount++;
            }
        };

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);
        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.True(player.GetAdaptation(AdaptationIds.ConidiaAscent)!.HasTriggered);
        Assert.Equal(1, eventCount);
    }

    private static GameBoard CreateBoardWithPlayer(int width, int height, out Player player)
    {
        var board = new GameBoard(width, height, playerCount: 1);
        player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);
        return board;
    }

    private static void FillLivingBlock(GameBoard board, Player player, int startX, int startY, int size)
    {
        foreach (int tileId in GetBlockTileIds(board, startX, startY, size))
        {
            bool spawned = board.SpawnSporeForPlayer(player, tileId, GrowthSource.Manual);
            Assert.True(spawned, $"Expected tile {tileId} to be available for setup.");
            board.GetCell(tileId)!.ClearNewlyGrownFlag();
        }
    }

    private static void FillAllRemainingTiles(GameBoard board, Player player, IReadOnlyCollection<int> excludedTileIds)
    {
        var excluded = excludedTileIds.ToHashSet();
        foreach (var tile in board.AllTiles())
        {
            if (excluded.Contains(tile.TileId))
            {
                continue;
            }

            bool spawned = board.SpawnSporeForPlayer(player, tile.TileId, GrowthSource.Manual);
            Assert.True(spawned, $"Expected tile {tile.TileId} to be available for setup.");
            board.GetCell(tile.TileId)!.ClearNewlyGrownFlag();
        }
    }

    private static List<int> GetBlockTileIds(GameBoard board, int startX, int startY, int size)
    {
        var tileIds = new List<int>(size * size);
        for (int y = startY; y < startY + size; y++)
        {
            for (int x = startX; x < startX + size; x++)
            {
                tileIds.Add(GetTileId(x, y, board.Width));
            }
        }

        return tileIds;
    }

    private static int GetTileId(int x, int y, int width)
    {
        return (y * width) + x;
    }

    private static void AdvanceToRound(GameBoard board, int targetRound)
    {
        while (board.CurrentRound < targetRound)
        {
            board.IncrementRound();
        }
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}