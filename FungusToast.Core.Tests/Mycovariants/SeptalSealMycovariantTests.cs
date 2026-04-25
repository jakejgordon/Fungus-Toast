using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class SeptalSealMycovariantTests
{
    [Fact]
    public void Factory_exposes_septal_seal_with_expected_metadata()
    {
        var mycovariant = MycovariantFactory.GetAll().Single(myco => myco.Id == MycovariantIds.SeptalSealId);

        Assert.Equal("Septal Seal", mycovariant.Name);
        Assert.Equal("myco_septal_seal", mycovariant.IconId);
        Assert.Equal(MycovariantType.Active, mycovariant.Type);
        Assert.Equal(MycovariantCategory.Resistance, mycovariant.Category);
        Assert.False(mycovariant.IsUniversal);
        Assert.True(mycovariant.IsLocked);
        Assert.Equal(1, mycovariant.RequiredMoldinessUnlockLevel);
        Assert.Contains("30% divided by your existing Mycovariant count", mycovariant.Description);
        Assert.NotNull(mycovariant.ApplyEffect);

        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(player);

        Assert.Equal(MycovariantGameBalance.SeptalSealAIScore, mycovariant.GetBaseAIScore(player, board));
    }

    [Fact]
    public void ResolveSeptalSeal_targets_only_living_non_resistant_cells_and_emits_resistance_batch()
    {
        var board = CreateBoardWithPlayers(out var owner);
        var mycovariant = MycovariantRepository.GetById(MycovariantIds.SeptalSealId);
        owner.AddMycovariant(mycovariant);
        var playerMyco = owner.GetMycovariant(MycovariantIds.SeptalSealId)!;

        PlaceOwnedLivingCell(board, owner, tileId: 1);
        PlaceOwnedLivingCell(board, owner, tileId: 2);
        PlaceOwnedLivingCell(board, owner, tileId: 3).MakeResistant();
        PlaceOwnedLivingCell(board, owner, tileId: 4).Kill(DeathReason.Age);
        PlaceOwnedLivingCell(board, owner, tileId: 5);
        PlaceOwnedLivingCell(board, owner, tileId: 6);

        int? resistancePlayerId = null;
        GrowthSource? resistanceSource = null;
        IReadOnlyList<int>? resistanceTiles = null;
        board.ResistanceAppliedBatch += (playerId, source, tileIds) =>
        {
            resistancePlayerId = playerId;
            resistanceSource = source;
            resistanceTiles = tileIds;
        };

        MycovariantEffectProcessor.ResolveSeptalSeal(playerMyco, board, new Random(7), new TestSimulationObserver());

        var expectedTileIds = SelectExpectedTileIds(new List<int> { 1, 2, 5, 6 }, selectionCount: 2, seed: 7);
        Assert.Equal(owner.PlayerId, resistancePlayerId);
        Assert.Equal(GrowthSource.SeptalSeal, resistanceSource);
        Assert.Equal(expectedTileIds, resistanceTiles!.ToList());
        Assert.True(playerMyco.HasTriggered);
        Assert.Equal(2, playerMyco.EffectCounts[MycovariantEffectType.SeptalSealResistances]);

        Assert.All(expectedTileIds, tileId =>
        {
            Assert.True(board.GetCell(tileId)!.IsResistant);
            Assert.Equal("Septal Seal", board.GetCell(tileId)!.ResistanceSource);
        });

        Assert.True(board.GetCell(3)!.IsResistant);
        Assert.True(board.GetCell(4)!.IsDead);
    }

    [Fact]
    public void ResolveSeptalSeal_uses_existing_mycovariant_count_before_pickup_for_scaling()
    {
        var board = CreateBoardWithPlayers(out var owner);
        owner.AddMycovariant(MycovariantRepository.GetById(MycovariantIds.PlasmidBountyId));
        owner.AddMycovariant(MycovariantRepository.GetById(MycovariantIds.SurgicalInoculationId));
        owner.AddMycovariant(MycovariantRepository.GetById(MycovariantIds.SeptalSealId));
        var playerMyco = owner.GetMycovariant(MycovariantIds.SeptalSealId)!;

        for (int tileId = 1; tileId <= 10; tileId++)
        {
            PlaceOwnedLivingCell(board, owner, tileId);
        }

        MycovariantEffectProcessor.ResolveSeptalSeal(playerMyco, board, new Random(13), new TestSimulationObserver());

        var expectedTileIds = SelectExpectedTileIds(Enumerable.Range(1, 10).ToList(), selectionCount: 2, seed: 13);
        Assert.Equal(2, playerMyco.EffectCounts[MycovariantEffectType.SeptalSealResistances]);
        Assert.All(expectedTileIds, tileId => Assert.True(board.GetCell(tileId)!.IsResistant));

        var nonSelectedResistantCount = Enumerable.Range(1, 10)
            .Except(expectedTileIds)
            .Count(tileId => board.GetCell(tileId)!.IsResistant);
        Assert.Equal(0, nonSelectedResistantCount);
    }

    private static GameBoard CreateBoardWithPlayers(out Player owner)
    {
        var board = new GameBoard(width: 6, height: 6, playerCount: 1);
        owner = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        return board;
    }

    private static FungalCell PlaceOwnedLivingCell(GameBoard board, Player owner, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        owner.AddControlledTile(tileId);
        return cell;
    }

    private static List<int> SelectExpectedTileIds(List<int> tileIds, int selectionCount, int seed)
    {
        var rng = new Random(seed);
        var shuffled = tileIds.ToList();
        for (int i = 0; i < selectionCount; i++)
        {
            int j = rng.Next(i, shuffled.Count);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled.Take(selectionCount).ToList();
    }
}