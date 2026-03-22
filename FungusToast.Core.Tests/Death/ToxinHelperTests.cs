using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Death;

public class ToxinHelperTests
{
    [Fact]
    public void GetToxinExpirationAge_returns_base_duration_when_player_is_null()
    {
        var duration = ToxinHelper.GetToxinExpirationAge(player: null, baseToxinDuration: 12);

        Assert.Equal(12, duration);
    }

    [Fact]
    public void GetToxinExpirationAge_adds_mycotoxin_potentiation_and_enduring_toxaphores_bonuses()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        player.SetMutationLevel(MutationIds.MycotoxinPotentiation, newLevel: 3, currentRound: 1);
        player.AddMycovariant(new Mycovariant
        {
            Id = MycovariantIds.EnduringToxaphoresId,
            Name = "Enduring Toxaphores"
        });

        var duration = ToxinHelper.GetToxinExpirationAge(player, baseToxinDuration: 10);

        Assert.Equal(
            10
            + (3 * GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel)
            + MycovariantGameBalance.EnduringToxaphoresNewToxinExtension,
            duration);
    }

    [Fact]
    public void ConvertToToxin_on_empty_tile_creates_toxin_cell_with_requested_owner_and_marks_drop_animation()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var owner = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);

        ToxinHelper.ConvertToToxin(board, tileId: 12, toxinLifespan: 7, growthSource: GrowthSource.CytolyticBurst, owner: owner);

        var cell = Assert.IsType<FungalCell>(board.GetCell(12));
        Assert.True(cell.IsToxin);
        Assert.Equal(owner.PlayerId, cell.OwnerPlayerId);
        Assert.Equal(7, cell.ToxinExpirationAge);
        Assert.True(cell.IsReceivingToxinDrop);
    }

    [Fact]
    public void ConvertToToxin_does_not_place_toxin_when_placement_is_neutralized()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        board.ToxinPlaced += (_, args) => args.Neutralized = true;

        ToxinHelper.ConvertToToxin(board, tileId: 12, toxinLifespan: 7, growthSource: GrowthSource.CytolyticBurst);

        Assert.Null(board.GetCell(12));
    }

    [Fact]
    public void KillAndToxify_returns_without_effect_when_tile_has_no_living_cell()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);

        ToxinHelper.KillAndToxify(board, tileId: 12, toxinLifespan: 7, reason: DeathReason.CytolyticBurst, growthSource: GrowthSource.CytolyticBurst);

        Assert.Null(board.GetCell(12));
    }

    [Fact]
    public void FindMycotoxinTargetTiles_returns_empty_open_tiles_adjacent_to_enemy_living_cells()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        var player0 = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        var player1 = new Player(playerId: 1, playerName: "P1", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player0);
        board.Players.Add(player1);
        board.PlaceInitialSpore(playerId: 1, x: 2, y: 2); // tile 12
        board.PlaceNutrientPatch(tileId: 7, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1));

        var targets = ToxinHelper.FindMycotoxinTargetTiles(board, player0)
            .Select(tile => tile.TileId)
            .OrderBy(id => id)
            .ToArray();

        Assert.DoesNotContain(12, targets);
        Assert.DoesNotContain(7, targets);
        Assert.Equal(new[] { 11, 13, 17 }, targets);
    }
}
