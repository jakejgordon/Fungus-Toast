using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class SurgicalInoculationMycovariantTests
{
    [Fact]
    public void ResolveSurgicalInoculationHuman_does_not_place_on_blocked_tiles()
    {
        var blockedTileId = 12;
        var board = new GameBoard(width: 5, height: 5, playerCount: 1, permanentlyBlockedTileIds: new[] { blockedTileId });
        var owner = new Player(0, "P0", PlayerTypeEnum.Human);
        board.Players.Add(owner);
        owner.AddMycovariant(MycovariantRepository.GetById(MycovariantIds.SurgicalInoculationId));
        var playerMyco = owner.GetMycovariant(MycovariantIds.SurgicalInoculationId)!;

        MycovariantEffectProcessor.ResolveSurgicalInoculationHuman(
            playerMyco,
            board,
            owner.PlayerId,
            blockedTileId,
            new TestSimulationObserver());

        Assert.True(board.GetTileById(blockedTileId)!.IsBlocked);
        Assert.Null(board.GetTileById(blockedTileId)?.FungalCell);
        Assert.Equal(0, playerMyco.EffectCounts.GetValueOrDefault(MycovariantEffectType.Drops));
        Assert.Equal(0, playerMyco.EffectCounts.GetValueOrDefault(MycovariantEffectType.ResistantCellPlaced));
    }
}
