using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class AscusWagerMycovariantTests
{
    [Fact]
    public void Factory_exposes_ascus_wager_with_expected_metadata()
    {
        var mycovariant = MycovariantFactory.GetAll().Single(myco => myco.Id == MycovariantIds.AscusWagerId);

        Assert.Equal("Ascus Wager", mycovariant.Name);
        Assert.Equal("myco_ascus_wager", mycovariant.IconId);
        Assert.Equal(MycovariantType.Economy, mycovariant.Type);
        Assert.Equal(MycovariantCategory.Economy, mycovariant.Category);
        Assert.False(mycovariant.IsUniversal);
        Assert.True(mycovariant.AutoMarkTriggered);
        Assert.Contains("random Tier 5 mutation", mycovariant.Description);
        Assert.NotNull(mycovariant.ApplyEffect);
    }

    [Fact]
    public void ResolveAscusWager_grants_the_only_available_tier5_level_without_spending_points_or_meeting_prerequisites()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.AI) { MutationPoints = 9 };
        board.Players.Add(player);
        var observer = new TestSimulationObserver();
        var mycovariant = MycovariantRepository.GetById(MycovariantIds.AscusWagerId);
        var playerMyco = new PlayerMycovariant(player.PlayerId, mycovariant.Id, mycovariant);

        var tier5Mutations = MutationRegistry.All.Values
            .Where(mutation => mutation.Tier == MutationTier.Tier5)
            .OrderBy(mutation => mutation.Id)
            .ToList();

        var targetMutation = Assert.Single(tier5Mutations.Take(1));

        foreach (var tier5Mutation in tier5Mutations.Skip(1))
        {
            player.SetMutationLevel(tier5Mutation.Id, tier5Mutation.MaxLevel, currentRound: 2);
        }

        bool upgraded = MycovariantEffectProcessor.ResolveAscusWager(playerMyco, board, new Random(123), observer);

        Assert.True(upgraded);
        Assert.Equal(MycovariantGameBalance.AscusWagerTier5LevelsGranted, player.GetMutationLevel(targetMutation.Id));
        Assert.Equal(9, player.MutationPoints);
        Assert.True(playerMyco.HasTriggered);
        Assert.Equal(
            MycovariantGameBalance.AscusWagerTier5LevelsGranted,
            playerMyco.EffectCounts[MycovariantEffectType.Tier5MutationLevelsGranted]);
        Assert.Equal("mycovariant", observer.LastUpgradeSource);
        Assert.Equal(1, observer.UpgradeEventCount);
    }

    [Fact]
    public void RunDraft_with_ascus_wager_permanently_removes_it_from_future_non_universal_choices()
    {
        var ascusWager = MycovariantRepository.GetById(MycovariantIds.AscusWagerId);
        var universal = new Mycovariant
        {
            Id = MycovariantIds.PlasmidBountyId,
            Name = "Plasmid Bounty I",
            IsUniversal = true
        };

        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        var firstPlayer = new Player(0, "P0", PlayerTypeEnum.Human);
        var secondPlayer = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(firstPlayer);
        board.Players.Add(secondPlayer);

        var poolManager = new MycovariantPoolManager();
        poolManager.InitializePool(new List<Mycovariant> { ascusWager, universal }, new Random(5));

        MycovariantDraftManager.RunDraft(
            new List<Player> { firstPlayer },
            poolManager,
            board,
            new Random(1),
            new TestSimulationObserver(),
            choicesCount: 2,
            humanSelectionCallback: (_, choices) => choices.Single(choice => choice.Id == MycovariantIds.AscusWagerId));

        var secondPlayerEligibleIds = poolManager
            .GetEligibleMycovariantsForPlayer(secondPlayer)
            .Select(candidate => candidate.Id)
            .ToList();

        Assert.DoesNotContain(MycovariantIds.AscusWagerId, secondPlayerEligibleIds);
        Assert.Contains(MycovariantIds.PlasmidBountyId, secondPlayerEligibleIds);
        Assert.True(firstPlayer.GetMycovariant(MycovariantIds.AscusWagerId)!.HasTriggered);
    }
}
