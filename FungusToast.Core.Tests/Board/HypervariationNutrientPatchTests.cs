using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class HypervariationNutrientPatchTests
{
    [Fact]
    public void Consuming_hypervariation_patch_queues_a_pending_single_player_draft()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Player 0", playerType: PlayerTypeEnum.Human);
        board.Players.Add(player);

        var clusterTileIds = new[]
        {
            board.GetTile(1, 1)!.TileId,
            board.GetTile(2, 1)!.TileId,
            board.GetTile(1, 2)!.TileId,
            board.GetTile(2, 2)!.TileId
        };

        var patch = NutrientPatch.CreateHypervariationCluster(clusterId: 5, clusterTileCount: clusterTileIds.Length);
        foreach (int tileId in clusterTileIds)
        {
            Assert.True(board.PlaceNutrientPatch(tileId, patch));
        }

        NutrientPatchType consumedPatchType = default;
        NutrientRewardType consumedRewardType = default;
        int consumedRewardAmount = 0;
        SpecialBoardEventArgs? specialEvent = null;
        board.NutrientPatchConsumed += (_, _, _, patchType, rewardType, rewardAmount) =>
        {
            consumedPatchType = patchType;
            consumedRewardType = rewardType;
            consumedRewardAmount = rewardAmount;
        };
        board.SpecialBoardEventTriggered += (_, args) => specialEvent = args;

        var cell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: clusterTileIds[0], source: GrowthSource.Manual, lastOwnerPlayerId: null);
        cell.MarkAsNewlyGrown();
        cell.SetBirthRound(board.CurrentRound);

        board.PlaceFungalCell(cell);

        Assert.True(board.HasPendingHypervariationDrafts);
        Assert.True(board.TryDequeuePendingHypervariationDraftPlayerId(out int pendingPlayerId));
        Assert.Equal(player.PlayerId, pendingPlayerId);
        Assert.False(board.HasPendingHypervariationDrafts);
        Assert.All(clusterTileIds, tileId => Assert.Null(board.GetTileById(tileId)!.NutrientPatch));
        Assert.Equal(NutrientPatchType.Hypervariation, consumedPatchType);
        Assert.Equal(NutrientRewardType.MycovariantDraft, consumedRewardType);
        Assert.Equal(1, consumedRewardAmount);
        Assert.NotNull(specialEvent);
        Assert.Equal(SpecialBoardEventKind.NutrientPatchConsumed, specialEvent!.EventKind);
        Assert.Equal(NutrientPatchType.Hypervariation, specialEvent.NutrientPatchType);
        Assert.Equal(NutrientRewardType.MycovariantDraft, specialEvent.NutrientRewardType);
        Assert.Equal(1, specialEvent.RewardAmount);
    }
}
