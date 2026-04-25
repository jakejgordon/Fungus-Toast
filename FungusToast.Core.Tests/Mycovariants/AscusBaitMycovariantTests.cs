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
    public void Factory_exposes_sporal_snare_with_expected_metadata()
    {
        var mycovariant = MycovariantFactory.GetAll().Single(myco => myco.Id == MycovariantIds.SporalSnareId);

        Assert.Equal("Sporal Snare", mycovariant.Name);
        Assert.Equal("myco_sporal_snare", mycovariant.IconId);
        Assert.Equal(MycovariantType.Economy, mycovariant.Type);
        Assert.Equal(MycovariantCategory.Economy, mycovariant.Category);
        Assert.True(mycovariant.IsUniversal);
        Assert.True(mycovariant.IsLocked);
        Assert.Equal(6, mycovariant.RequiredMoldinessUnlockLevel);
        Assert.False(mycovariant.AutoMarkTriggered);
        Assert.Contains("grants 10 mutation points", mycovariant.Description);
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

        var result = MycovariantEffectProcessor.ResolveAscusBait(playerMyco, board, new Random(123), new TestSimulationObserver());

        Assert.Equal(0, result);
        Assert.Equal(3 + MycovariantGameBalance.AscusBaitMutationPointAward, player.MutationPoints);
        Assert.True(playerMyco.HasTriggered);
    }

    [Fact]
    public void ResolveSporalSnare_grants_human_player_mutation_points()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.Human) { MutationPoints = 3 };
        board.Players.Add(player);
        var mycovariant = MycovariantRepository.GetById(MycovariantIds.SporalSnareId);
        var playerMyco = new PlayerMycovariant(player.PlayerId, mycovariant.Id, mycovariant);

        var result = MycovariantEffectProcessor.ResolveSporalSnare(playerMyco, board, new Random(123), new TestSimulationObserver());

        Assert.Null(result);
        Assert.Equal(13, player.MutationPoints);
        Assert.True(playerMyco.HasTriggered);
    }

    [Fact]
    public void ResolveSporalSnare_projects_human_owned_breach_through_enemy_lane()
    {
        var board = new GameBoard(width: 7, height: 1, playerCount: 2);
        var human = new Player(0, "Human", PlayerTypeEnum.Human);
        var ai = new Player(1, "AI", PlayerTypeEnum.AI);
        board.Players.Add(human);
        board.Players.Add(ai);
        board.PlaceInitialSpore(human.PlayerId, 0, 0);
        board.PlaceInitialSpore(ai.PlayerId, 6, 0);

        SeedOwnedLivingCells(board, ai, new[] { 2 });
        var deadEnemyCell = new FungalCell(ownerPlayerId: ai.PlayerId, tileId: 3, source: GrowthSource.HyphalOutgrowth, lastOwnerPlayerId: null);
        board.PlaceFungalCell(deadEnemyCell);
        board.KillFungalCell(deadEnemyCell, DeathReason.CytolyticBurst);
        var toxinCell = new FungalCell(ownerPlayerId: ai.PlayerId, tileId: 4, source: GrowthSource.CytolyticBurst, lastOwnerPlayerId: null);
        toxinCell.ConvertToToxin(10, GrowthSource.CytolyticBurst, ai, DeathReason.CytolyticBurst);
        board.PlaceFungalCell(toxinCell);
        SeedOwnedLivingCells(board, ai, new[] { 5 });
        board.GetCell(5)!.MakeResistant();

        var mycovariant = MycovariantRepository.GetById(MycovariantIds.SporalSnareId);
        var playerMyco = new PlayerMycovariant(ai.PlayerId, mycovariant.Id, mycovariant);

        var result = MycovariantEffectProcessor.ResolveSporalSnare(playerMyco, board, new Random(7), new TestSimulationObserver());

        Assert.NotNull(result);
        Assert.Equal(human.PlayerId, result!.HumanPlayerId);
        Assert.Equal(ai.PlayerId, result.DraftingPlayerId);
        Assert.Equal(1, result.LineStride);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, result.SampledPathTileIds);
        Assert.Equal(human.PlayerId, board.GetCell(1)!.OwnerPlayerId);
        Assert.Equal(human.PlayerId, board.GetCell(2)!.OwnerPlayerId);
        Assert.True(board.GetCell(2)!.IsAlive);
        Assert.Equal(human.PlayerId, board.GetCell(3)!.OwnerPlayerId);
        Assert.True(board.GetCell(3)!.IsAlive);
        Assert.Equal(human.PlayerId, board.GetCell(4)!.OwnerPlayerId);
        Assert.True(board.GetCell(4)!.IsAlive);
        Assert.Equal(ai.PlayerId, board.GetCell(5)!.OwnerPlayerId);
        Assert.True(board.GetCell(5)!.IsResistant);
        Assert.Equal(ai.PlayerId, board.GetCell(6)!.OwnerPlayerId);
        Assert.True(board.GetCell(6)!.IsResistant);
        Assert.Equal(1, result.Colonized);
        Assert.Equal(1, result.Infested);
        Assert.Equal(1, result.Reclaimed);
        Assert.Equal(1, result.Overgrown);
        Assert.Equal(2, result.SkippedResistant);
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
        var expectedKillCount = (int)Math.Ceiling(candidates.Count * MycovariantGameBalance.AscusBaitSelfCullPercentage);
        var expectedKilledTileIds = SelectExpectedTileIds(candidates, expectedKillCount, seed: 17);
        var deathEvents = new List<FungalCellDiedEventArgs>();
        board.CellDeath += (_, args) => deathEvents.Add(args);

        var result = MycovariantEffectProcessor.ResolveAscusBait(playerMyco, board, new Random(17), new TestSimulationObserver());

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
        var normalScore = ascusBait.GetBaseAIScore(player, board);

        player.IsLastAiMycovariantDrafterForCurrentDraft = true;
        var lastAiScore = ascusBait.GetBaseAIScore(player, board);

        Assert.Equal(0f, normalScore);
        Assert.Equal(MycovariantGameBalance.AscusBaitPreferredAIScore, lastAiScore);
    }

    [Fact]
    public void SporalSnare_ai_score_is_high_only_for_last_ai_drafter()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(player);
        var mycovariant = MycovariantRepository.GetById(MycovariantIds.SporalSnareId);

        player.IsLastAiMycovariantDrafterForCurrentDraft = false;
        var normalScore = mycovariant.GetBaseAIScore(player, board);

        player.IsLastAiMycovariantDrafterForCurrentDraft = true;
        var lastAiScore = mycovariant.GetBaseAIScore(player, board);

        Assert.Equal(0f, normalScore);
        Assert.Equal(MycovariantGameBalance.SporalSnarePreferredAIScore, lastAiScore);
    }

    [Fact]
    public void BuildSporalSnarePath_scales_stride_by_board_size()
    {
        var smallBoard = new GameBoard(width: 12, height: 12, playerCount: 1);
        var mediumBoard = new GameBoard(width: 24, height: 24, playerCount: 1);
        var largeBoard = new GameBoard(width: 60, height: 60, playerCount: 1);

        IReadOnlyList<int> smallPath = MycovariantEffectProcessor.BuildSporalSnarePath(smallBoard, 0, 0, 5, 0, out var smallStride);
        IReadOnlyList<int> mediumPath = MycovariantEffectProcessor.BuildSporalSnarePath(mediumBoard, 0, 0, 6, 0, out var mediumStride);
        IReadOnlyList<int> largePath = MycovariantEffectProcessor.BuildSporalSnarePath(largeBoard, 0, 0, 8, 0, out var largeStride);

        Assert.Equal(1, smallStride);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, smallPath);
        Assert.Equal(2, mediumStride);
        Assert.Equal(new[] { 1, 3, 5, 6 }, mediumPath);
        Assert.Equal(3, largeStride);
        Assert.Equal(new[] { 1, 4, 7, 8 }, largePath);
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
    public void GetEligibleMycovariantsForPlayer_hides_sporal_snare_from_non_final_ai_only()
    {
        var firstAi = new Player(0, "P0", PlayerTypeEnum.AI);
        var lastAi = new Player(1, "P1", PlayerTypeEnum.AI)
        {
            IsLastAiMycovariantDrafterForCurrentDraft = true
        };
        var human = new Player(2, "Human", PlayerTypeEnum.Human);

        var sporalSnare = MycovariantRepository.GetById(MycovariantIds.SporalSnareId);
        var plasmidBounty = MycovariantRepository.GetById(MycovariantIds.PlasmidBountyId);
        var poolManager = new MycovariantPoolManager();
        poolManager.InitializePool(new List<Mycovariant> { sporalSnare, plasmidBounty }, new Random(5));

        var firstAiEligible = poolManager.GetEligibleMycovariantsForPlayer(firstAi);
        var lastAiEligible = poolManager.GetEligibleMycovariantsForPlayer(lastAi);
        var humanEligible = poolManager.GetEligibleMycovariantsForPlayer(human);

        Assert.DoesNotContain(firstAiEligible, myco => myco.Id == MycovariantIds.SporalSnareId);
        Assert.Contains(lastAiEligible, myco => myco.Id == MycovariantIds.SporalSnareId);
        Assert.Contains(humanEligible, myco => myco.Id == MycovariantIds.SporalSnareId);
    }

    [Fact]
    public void GetDraftChoices_forced_sporal_snare_is_included_for_last_ai_drafter_when_eligible()
    {
        var ai = new Player(1, "P1", PlayerTypeEnum.AI)
        {
            IsLastAiMycovariantDrafterForCurrentDraft = true
        };

        var sporalSnare = MycovariantRepository.GetById(MycovariantIds.SporalSnareId);
        var plasmidBounty = MycovariantRepository.GetById(MycovariantIds.PlasmidBountyId);
        var plasmidBountyII = MycovariantRepository.GetById(MycovariantIds.PlasmidBountyIIId);
        var ascusWager = MycovariantRepository.GetById(MycovariantIds.AscusWagerId);
        var poolManager = new MycovariantPoolManager();
        poolManager.InitializePool(new List<Mycovariant> { sporalSnare, plasmidBounty, plasmidBountyII, ascusWager }, new Random(5));

        var choices = MycovariantDraftManager.GetDraftChoices(
            ai,
            poolManager,
            choicesCount: 3,
            rng: new Random(11),
            forcedMycovariantId: MycovariantIds.SporalSnareId);

        Assert.Contains(choices, myco => myco.Id == MycovariantIds.SporalSnareId);
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
        for (var i = 0; i < selectionCount; i++)
        {
            var j = rng.Next(i, shuffled.Count);
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