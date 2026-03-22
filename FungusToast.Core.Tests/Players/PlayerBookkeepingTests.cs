using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Players;

public class PlayerBookkeepingTests
{
    [Fact]
    public void SetStartingTile_only_sets_the_starting_tile_once()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);

        player.SetStartingTile(10);
        player.SetStartingTile(20);

        Assert.Equal(10, player.StartingTileId);
    }

    [Fact]
    public void RelocateStartingTile_overwrites_the_existing_starting_tile()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);

        player.SetStartingTile(10);
        player.RelocateStartingTile(20);

        Assert.Equal(20, player.StartingTileId);
    }

    [Fact]
    public void AddControlledTile_does_not_add_duplicates()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);

        player.AddControlledTile(7);
        player.AddControlledTile(7);

        var controlledTileId = Assert.Single(player.ControlledTileIds);
        Assert.Equal(7, controlledTileId);
    }

    [Fact]
    public void RemoveControlledTile_removes_existing_tile_id()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);

        player.AddControlledTile(7);
        player.AddControlledTile(8);
        player.RemoveControlledTile(7);

        var remainingTileId = Assert.Single(player.ControlledTileIds);
        Assert.Equal(8, remainingTileId);
    }
}
