using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Persistence;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Board;

public class RotTests
{
    private static (GameBoard board, Player player) CreateBoard(int width = 5, int height = 5)
    {
        var board = new GameBoard(width, height, 1);
        var player = new Player(0, "Mold", PlayerTypeEnum.AI);
        board.Players.Add(player);
        return (board, player);
    }

    [Fact]
    public void Rot_is_ownerless_exclusive_and_emits_only_one_placement_event()
    {
        var (board, player) = CreateBoard();
        var events = new List<int>();
        board.RotPlaced += events.Add;
        Assert.True(board.TryPlaceRot(12));
        Assert.False(board.TryPlaceRot(12));
        Assert.False(board.TryPlaceRot(-1));
        Assert.False(board.TryPlaceRot(25));
        Assert.True(board.GetTileById(12)!.HasRot);
        Assert.True(board.GetTileById(12)!.IsOccupiedForSporePlacement);
        Assert.True(board.IsTileBlockedForOccupation(12));
        Assert.False(board.SpawnSporeForPlayer(player, 12, GrowthSource.Manual));
        board.PlaceInitialSpore(0, 2, 2);
        Assert.False(board.PlaceNutrientPatch(12, NutrientPatch.CreateHypervariationCluster(1, 1)));
        ToxinHelper.ConvertToToxin(board, 12, GrowthSource.Manual, player);
        Assert.False(board.TryPlaceChemobeacon(0, 12, 1, 2));
        Assert.Null(board.GetCell(12));
        Assert.Empty(player.ControlledTileIds);
        Assert.Empty(board.GetAllCells());
        Assert.Equal(new[] { 12 }, events);
    }

    [Fact]
    public void Rot_refuses_cells_nutrients_and_silhouette_terrain()
    {
        var board = new GameBoard(5, 5, 1, new[] { 0 });
        var player = new Player(0, "Mold", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.SpawnSporeForPlayer(player, 6, GrowthSource.Manual);
        board.PlaceNutrientPatch(7, NutrientPatch.CreateHypervariationCluster(1, 1));
        board.TryPlaceChemobeacon(0, 8, 1, 2);
        foreach (int id in new[] { 0, 6, 7, 8 }) Assert.False(board.TryPlaceRot(id));
        Assert.Empty(board.RotTileIds);
    }

    [Fact]
    public void Exposure_is_orthogonal_non_stacking_and_separately_attributed()
    {
        var (board, player) = CreateBoard();
        board.ConfigureRot(RotBalance.AdjacentDeathChance);
        board.TryPlaceRot(11);
        board.TryPlaceRot(13);
        var cell = new FungalCell(0, 12, GrowthSource.Manual, null);
        float baseline = player.GetEffectiveRandomDecayChance(board.CurrentRound);
        var result = MutationEffectCoordinator.CalculateDeathChance(player, cell, board, board.Players,
            baseline + 0.025d, new Random(42), new TestSimulationObserver());
        Assert.Equal((double)(baseline + 0.05f), (double)result.Chance, 6);
        Assert.Equal(DeathReason.Rot, result.Reason);
        Assert.Null(result.KillerPlayerId);
        Assert.Null(result.AttackerTileId);
        Assert.False(board.IsAdjacentToRot(7)); // diagonals to both rot tiles
        Assert.True(board.IsAdjacentToRot(6));
        cell.MakeResistant();
        var resistant = MutationEffectCoordinator.CalculateDeathChance(player, cell, board, board.Players,
            0.99d, new Random(42), new TestSimulationObserver());
        Assert.Equal((double)baseline, (double)resistant.Chance, 6);
    }

    [Fact]
    public void Decay_preserves_resistant_and_last_living_cell_and_leaves_rot_static()
    {
        var (board, player) = CreateBoard();
        board.ConfigureRot(1f);
        board.TryPlaceRot(12);
        board.SpawnSporeForPlayer(player, 11, GrowthSource.Manual);
        for (int round = 0; round < 4; round++)
            TurnEngine.RunDecayPhase(board, board.Players, new Dictionary<int, int>(), new Random(round), new TestSimulationObserver());
        Assert.True(board.GetCell(11)!.IsAlive);
        board.SpawnSporeForPlayer(player, 13, GrowthSource.Manual);
        board.GetCell(13)!.MakeResistant();
        TurnEngine.RunDecayPhase(board, board.Players, new Dictionary<int, int>(), new Random(7), new TestSimulationObserver());
        Assert.True(board.GetCell(13)!.IsAlive);
        Assert.Equal(DeathReason.Rot, board.GetCell(11)!.CauseOfDeath);
        Assert.Equal(new[] { 12 }, board.RotTileIds);
    }

    [Fact]
    public void Rot_counts_for_countdown_but_not_cells_or_player_territory()
    {
        var (board, player) = CreateBoard();
        foreach (int id in Enumerable.Range(0, 20)) Assert.True(board.TryPlaceRot(id));
        Assert.Equal(0.8f, board.GetOccupiedTileRatio());
        Assert.True(board.ShouldTriggerEndgame());
        Assert.Equal(25, board.PlayableTileCount);
        Assert.Empty(board.GetAllCellsOwnedBy(player.PlayerId));
        Assert.Equal(0, board.OccupiedTileCount); // fungal-cell index remains fungal only
    }

    [Fact]
    public void Checkpoint_round_trips_rot_and_old_snapshots_default_to_no_rot()
    {
        var (board, _) = CreateBoard();
        board.ConfigureRot(0.05f);
        board.TryPlaceRot(10);
        board.TryPlaceRot(12);
        var snapshot = RoundStartRuntimeSnapshotFactory.Export(board);
        var (restored, _) = RoundStartRuntimeSnapshotFactory.Restore(snapshot);
        Assert.Equal(board.RotTileIds, restored.RotTileIds);
        Assert.Equal(board.RotAdjacentDeathChance, restored.RotAdjacentDeathChance);
        Assert.True(restored.GetTileById(10)!.HasRot);
        Assert.Equal(board.GetOccupiedTileRatio(), restored.GetOccupiedTileRatio());
        snapshot.RotTileIds = null!; // missing serialized field in a legacy checkpoint
        snapshot.RotAdjacentDeathChance = 0;
        var (legacy, _) = RoundStartRuntimeSnapshotFactory.Restore(snapshot);
        Assert.Empty(legacy.RotTileIds);
        Assert.Equal(0f, legacy.RotAdjacentDeathChance);
    }

    [Fact]
    public void Introductory_patch_is_repeatable_connected_edge_to_middle_and_safe_for_starts()
    {
        var (board, _) = CreateBoard(80, 60);
        board.PlaceInitialSpore(0, 5, 30);
        Assert.True(RotPlacementUtility.PlaceIntroductoryPatch(board) > 100);
        var (same, _) = CreateBoard(80, 60);
        same.PlaceInitialSpore(0, 5, 30);
        RotPlacementUtility.PlaceIntroductoryPatch(same);
        Assert.Equal(board.RotTileIds, same.RotTileIds);
        var tiles = board.RotTileIds.Select(id => board.GetTileById(id)!).ToList();
        Assert.Contains(tiles, tile => tile.X == 0);
        Assert.Contains(tiles, tile => tile.X >= board.Width / 2);
        Assert.All(tiles, tile => Assert.True(tile.DistanceTo(board.GetTileById(2405)!) > 6));
        var visited = new HashSet<int> { tiles[0].TileId };
        var queue = new Queue<int>(visited);
        while (queue.Count > 0)
            foreach (var neighbor in board.GetOrthogonalNeighbors(queue.Dequeue()))
                if (neighbor.HasRot && visited.Add(neighbor.TileId)) queue.Enqueue(neighbor.TileId);
        Assert.Equal(tiles.Count, visited.Count);
        Assert.Contains(board.AllTiles(), t => t.X > 44 && !t.HasRot);
    }

    [Fact]
    public void Introductory_patch_reaches_middle_with_campaign_four_candidate_starts()
    {
        var board = new GameBoard(40, 40, 5);
        var starts = new[] { (28, 26), (16, 29), (9, 20), (16, 10), (28, 14) };
        for (int i = 0; i < starts.Length; i++)
        {
            board.Players.Add(new Player(i, $"Mold {i}", PlayerTypeEnum.AI));
            board.PlaceInitialSpore(i, starts[i].Item1, starts[i].Item2);
        }
        Assert.InRange(RotPlacementUtility.PlaceIntroductoryPatch(board), 30, 200);
        var rot = board.RotTileIds.Select(id => board.GetTileById(id)!).ToList();
        Assert.Contains(rot, tile => tile.X >= 20);
        Assert.Contains(rot, tile => tile.X == 0);
        Assert.All(rot, tile => Assert.All(starts, start =>
            Assert.True(Math.Abs(tile.X - start.Item1) + Math.Abs(tile.Y - start.Item2) > 4)));
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Invalid_rot_tuning_is_rejected(float chance)
        => Assert.Throws<ArgumentOutOfRangeException>(() => CreateBoard().board.ConfigureRot(chance));
}
