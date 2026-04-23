using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class AscusBaitMycovariantTests
{
    [Fact]
    public void Factory_exposes_ascus_bait_with_expected_metadata()
    {
        var mycovariant = MycovariantFactory.GetAll().Single(myco => myco.Id == MycovariantIds.AscusBaitId);

        Assert.Equal("Ascus Bait", mycovariant.Name);
        Assert.Equal("myco_ascus_bait", mycovariant.IconId);
        Assert.Equal(MycovariantType.Economy, mycovariant.Type);
        Assert.Equal(MycovariantCategory.Economy, mycovariant.Category);
        Assert.True(mycovariant.IsUniversal);
        Assert.True(mycovariant.IsLocked);
        Assert.Equal(1, mycovariant.RequiredMoldinessUnlockLevel);
        Assert.True(mycovariant.AutoMarkTriggered);
        Assert.Contains("if Human", mycovariant.Description);
        Assert.Contains("die at random", mycovariant.Description);
        Assert.NotNull(mycovariant.ApplyEffect);
    }

    [Fact]
    public void ResolveAscusBait_grants_human_player_mutation_points()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.Human) { MutationPoints = 3 };
        board.Players.Add(player);
        var mycovariant = MycovariantRepository.GetById(MycovariantIds.AscusBaitId);
        var playerMyco = new PlayerMycovariant(player.PlayerId, mycovariant.Id, mycovariant);

        int result = MycovariantEffectProcessor.ResolveAscusBait(playerMyco, board, new Random(123), new TestSimulationObserver());

        Assert.Equal(0, result);
        Assert.Equal(3 + MycovariantGameBalance.AscusBaitMutationPointAward, player.MutationPoints);
        Assert.True(playerMyco.HasTriggered);
    }

    [Fact]
    public void ResolveAscusBait_kills_random_non_resistant_ai_cells_with_ascus_bait_reason()
    {
        var board = new GameBoard(width: 6, height: 6, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 0, y: 0);
        SeedOwnedLivingCells(board, player, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });
        board.GetCell(11)!.MakeResistant();

        var mycovariant = MycovariantRepository.GetById(MycovariantIds.AscusBaitId);
        var playerMyco = new PlayerMycovariant(player.PlayerId, mycovariant.Id, mycovariant);
        var candidates = board.GetAllCellsOwnedBy(player.PlayerId)
            .Where(cell => cell.IsAlive && !cell.IsResistant)
            .Select(cell => cell.TileId)
            .ToList();
        int expectedKillCount = (int)Math.Ceiling(candidates.Count * MycovariantGameBalance.AscusBaitSelfCullPercentage);
        var expectedKilledTileIds = SelectExpectedTileIds(candidates, expectedKillCount, seed: 17);
        var deathEvents = new List<FungalCellDiedEventArgs>();
        board.CellDeath += (_, args) => deathEvents.Add(args);

        int result = MycovariantEffectProcessor.ResolveAscusBait(playerMyco, board, new Random(17), new TestSimulationObserver());

        Assert.Equal(expectedKillCount, result);
        Assert.True(playerMyco.HasTriggered);
        Assert.Equal(expectedKilledTileIds, deathEvents.Select(args => args.TileId).ToList());
        Assert.All(deathEvents, args => Assert.Equal(DeathReason.AscusBait, args.Reason));
        Assert.All(expectedKilledTileIds, tileId => Assert.True(board.GetCell(tileId)!.IsDead));
        Assert.True(board.GetCell(0)!.IsAlive);
        Assert.True(board.GetCell(0)!.IsResistant);
        Assert.True(board.GetCell(11)!.IsAlive);
        Assert.True(board.GetCell(11)!.IsResistant);
    }

    [Fact]
    public void SelectMycovariantFromChoices_prefers_ascus_bait_over_preferred_choice_when_score_hits_always_pick_threshold()
    {
        var strategy = new ParameterizedSpendingStrategy(
            strategyName: "Test",
            prioritizeHighTier: true,
            preferredMycovariantIds: new List<int> { MycovariantIds.PlasmidBountyId });
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        player.IsLastAiMycovariantDrafterForCurrentDraft = true;
        board.Players.Add(player);
        var ascusBait = MycovariantRepository.GetById(MycovariantIds.AscusBaitId);
        var plasmidBounty = MycovariantRepository.GetById(MycovariantIds.PlasmidBountyId);

        var picked = strategy.SelectMycovariantFromChoices(player, new List<Mycovariant> { plasmidBounty, ascusBait }, board);

        Assert.Equal(MycovariantIds.AscusBaitId, picked.Id);
    }

    [Fact]
    public void AscusBait_ai_score_is_high_only_for_last_ai_drafter()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(player);
        var ascusBait = MycovariantRepository.GetById(MycovariantIds.AscusBaitId);

        player.IsLastAiMycovariantDrafterForCurrentDraft = false;
        float normalScore = ascusBait.GetBaseAIScore(player, board);

        player.IsLastAiMycovariantDrafterForCurrentDraft = true;
        float lastAiScore = ascusBait.GetBaseAIScore(player, board);

        Assert.Equal(0f, normalScore);
        Assert.Equal(MycovariantGameBalance.AscusBaitPreferredAIScore, lastAiScore);
    }

    [Fact]
    public void GetEligibleMycovariantsForPlayer_hides_ascus_bait_from_non_final_ai_only()
    {
        var firstAi = new Player(0, "P0", PlayerTypeEnum.AI);
        var lastAi = new Player(1, "P1", PlayerTypeEnum.AI)
        {
            IsLastAiMycovariantDrafterForCurrentDraft = true
        };
        var human = new Player(2, "Human", PlayerTypeEnum.Human);

        var ascusBait = MycovariantRepository.GetById(MycovariantIds.AscusBaitId);
        var plasmidBounty = MycovariantRepository.GetById(MycovariantIds.PlasmidBountyId);
        var ascusWager = MycovariantRepository.GetById(MycovariantIds.AscusWagerId);
        var poolManager = new MycovariantPoolManager();
        poolManager.InitializePool(new List<Mycovariant> { ascusBait, plasmidBounty, ascusWager }, new Random(5));

        var firstAiEligible = poolManager.GetEligibleMycovariantsForPlayer(firstAi);
        var lastAiEligible = poolManager.GetEligibleMycovariantsForPlayer(lastAi);
        var humanEligible = poolManager.GetEligibleMycovariantsForPlayer(human);

        Assert.DoesNotContain(firstAiEligible, myco => myco.Id == MycovariantIds.AscusBaitId);
        Assert.Contains(lastAiEligible, myco => myco.Id == MycovariantIds.AscusBaitId);
        Assert.Contains(humanEligible, myco => myco.Id == MycovariantIds.AscusBaitId);
    }

    [Fact]
    public void RunDraft_only_final_ai_drafter_takes_ascus_bait_when_other_ai_prefers_plasmid_bounty()
    {
        var board = new GameBoard(width: 6, height: 6, playerCount: 2);
        var firstAi = new Player(0, "P0", PlayerTypeEnum.AI);
        var lastAi = new Player(1, "P1", PlayerTypeEnum.AI);
        var strategy = new ParameterizedSpendingStrategy(
            strategyName: "Test",
            prioritizeHighTier: true,
            preferredMycovariantIds: new List<int> { MycovariantIds.PlasmidBountyId });
        firstAi.SetMutationStrategy(strategy);
        lastAi.SetMutationStrategy(strategy);
        board.Players.Add(firstAi);
        board.Players.Add(lastAi);

        board.PlaceInitialSpore(playerId: firstAi.PlayerId, x: 0, y: 0);
        board.PlaceInitialSpore(playerId: lastAi.PlayerId, x: 5, y: 0);
        SeedOwnedLivingCells(board, firstAi, new[] { 6, 7 });
        SeedOwnedLivingCells(board, lastAi, new[] { 11, 12, 13, 14 });

        var ascusBait = MycovariantRepository.GetById(MycovariantIds.AscusBaitId);
        var plasmidBounty = MycovariantRepository.GetById(MycovariantIds.PlasmidBountyId);
        var poolManager = new MycovariantPoolManager();
        poolManager.InitializePool(new List<Mycovariant> { ascusBait, plasmidBounty }, new Random(5));

        MycovariantDraftManager.RunDraft(
            new List<Player> { firstAi, lastAi },
            poolManager,
            board,
            new Random(11),
            new TestSimulationObserver(),
            choicesCount: 2);

        Assert.NotNull(firstAi.GetMycovariant(MycovariantIds.PlasmidBountyId));
        Assert.Null(firstAi.GetMycovariant(MycovariantIds.AscusBaitId));
        var lastAiAscusBait = Assert.IsType<PlayerMycovariant>(lastAi.GetMycovariant(MycovariantIds.AscusBaitId));
        Assert.Equal(MycovariantGameBalance.AscusBaitPreferredAIScore, lastAiAscusBait.AIScoreAtDraft);
        Assert.False(firstAi.IsLastAiMycovariantDrafterForCurrentDraft);
        Assert.False(lastAi.IsLastAiMycovariantDrafterForCurrentDraft);
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

    private static void SeedOwnedLivingCells(GameBoard board, Player owner, IEnumerable<int> tileIds)
    {
        foreach (var tileId in tileIds)
        {
            var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
            board.PlaceFungalCell(cell);
            owner.AddControlledTile(tileId);
        }
    }
}