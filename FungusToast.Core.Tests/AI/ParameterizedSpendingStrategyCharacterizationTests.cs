using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.AI;

public class ParameterizedSpendingStrategyCharacterizationTests
{
    [Fact]
    public void SelectMycovariant_uses_authored_preference_below_the_always_pick_threshold()
    {
        var preferred = CreateMycovariant(101, score: 1f);
        var higherScored = CreateMycovariant(102, score: 10f);
        var strategy = CreateStrategy(mycovariantPreferences: new List<MycovariantPreference>
        {
            new(preferred.Id, priority: 5)
        });
        var (board, player) = CreateBoardAndPlayer();

        var selected = strategy.SelectMycovariantFromChoices(
            player,
            new List<Mycovariant> { higherScored, preferred },
            board,
            new Random(1));

        Assert.Same(preferred, selected);
    }

    [Fact]
    public void SelectMycovariant_always_pick_score_overrides_authored_preference()
    {
        var preferred = CreateMycovariant(101, score: 1f);
        var mustPick = CreateMycovariant(
            102,
            score: MycovariantGameBalance.AIDraftAlwaysPickScoreThreshold);
        var strategy = CreateStrategy(mycovariantPreferences: new List<MycovariantPreference>
        {
            new(preferred.Id, priority: 5)
        });
        var (board, player) = CreateBoardAndPlayer();

        var selected = strategy.SelectMycovariantFromChoices(
            player,
            new List<Mycovariant> { preferred, mustPick },
            board,
            new Random(1));

        Assert.Same(mustPick, selected);
    }

    [Fact]
    public void Scheduled_surge_runs_before_ordinary_fallback_spending()
    {
        var ordinary = CreateMutation(901, points: 3);
        var surge = CreateMutation(902, points: 3, isSurge: true);
        var strategy = CreateStrategy(
            priorityCategories: new List<MutationCategory> { MutationCategory.Growth },
            surgePriorityIds: new List<int> { surge.Id },
            surgeFrequency: 2);
        var (board, player) = CreateBoardAndPlayer(mutationPoints: 3, round: 2);

        strategy.SpendMutationPoints(
            player,
            new List<Mutation> { ordinary, surge },
            board,
            new Random(1),
            new TestSimulationObserver());

        Assert.True(player.IsSurgeActive(surge.Id));
        Assert.Equal(1, player.GetMutationLevel(surge.Id));
        Assert.Equal(0, player.GetMutationLevel(ordinary.Id));
    }

    [Fact]
    public void Off_schedule_ordinary_fallback_spending_precedes_last_resort_surge()
    {
        var ordinary = CreateMutation(901, points: 3);
        var surge = CreateMutation(902, points: 3, isSurge: true);
        var strategy = CreateStrategy(
            priorityCategories: new List<MutationCategory> { MutationCategory.Growth },
            surgePriorityIds: new List<int> { surge.Id },
            surgeFrequency: 2);
        var (board, player) = CreateBoardAndPlayer(mutationPoints: 3, round: 1);

        strategy.SpendMutationPoints(
            player,
            new List<Mutation> { ordinary, surge },
            board,
            new Random(1),
            new TestSimulationObserver());

        Assert.False(player.IsSurgeActive(surge.Id));
        Assert.Equal(0, player.GetMutationLevel(surge.Id));
        Assert.Equal(1, player.GetMutationLevel(ordinary.Id));
    }

    [Fact]
    public void Surge_banking_preserves_points_when_activation_is_affordable_by_the_next_window()
    {
        var surge = MutationRegistry.GetById(MutationIds.HyphalSurge)!;
        var strategy = CreateStrategy(
            surgePriorityIds: new List<int> { surge.Id },
            surgeFrequency: 4);
        var (board, player) = CreateBoardAndPlayer(round: 2);
        player.SetMutationLevel(surge.Id, 1, currentRound: 1);
        int pointsBefore = player.GetMutationPointCost(surge) - 1;
        player.MutationPoints = pointsBefore;
        var observer = new TestSimulationObserver();

        strategy.SpendMutationPoints(
            player,
            new List<Mutation> { surge },
            board,
            new Random(1),
            observer);

        Assert.Equal(pointsBefore, player.MutationPoints);
        Assert.Equal(pointsBefore, observer.LastBankedPoints);
        Assert.False(player.IsSurgeActive(surge.Id));
    }

    [Fact]
    public void Excluded_mutation_is_not_bought_even_when_it_is_the_only_option()
    {
        var excluded = CreateMutation(903, points: 1);
        var strategy = CreateStrategy(excludedMutationIds: new[] { excluded.Id });
        var (board, player) = CreateBoardAndPlayer(mutationPoints: 1);

        strategy.SpendMutationPoints(
            player,
            new List<Mutation> { excluded },
            board,
            new Random(1),
            new TestSimulationObserver());

        Assert.Equal(1, player.MutationPoints);
        Assert.Equal(0, player.GetMutationLevel(excluded.Id));
    }

    [Fact]
    public void Tendril_choice_prefers_the_direction_with_the_most_open_growth_targets()
    {
        var strategy = CreateStrategy(
            priorityCategories: new List<MutationCategory> { MutationCategory.Growth });
        var (board, player) = CreateBoardAndPlayer(
            width: 3,
            height: 3,
            mutationPoints: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2));
        board.PlaceInitialSpore(player.PlayerId, x: 0, y: 0);
        player.SetMutationLevel(MutationIds.MycelialBloom, 10, currentRound: 0);
        var tendrils = new[]
        {
            MutationRegistry.GetById(MutationIds.TendrilNorthwest)!,
            MutationRegistry.GetById(MutationIds.TendrilNortheast)!,
            MutationRegistry.GetById(MutationIds.TendrilSoutheast)!,
            MutationRegistry.GetById(MutationIds.TendrilSouthwest)!
        };

        strategy.SpendMutationPoints(
            player,
            tendrils.ToList(),
            board,
            new Random(1),
            new TestSimulationObserver());

        Assert.Equal(1, player.GetMutationLevel(MutationIds.TendrilNortheast));
        Assert.All(
            tendrils.Where(mutation => mutation.Id != MutationIds.TendrilNortheast),
            mutation => Assert.Equal(0, player.GetMutationLevel(mutation.Id)));
    }

    private static ParameterizedSpendingStrategy CreateStrategy(
        List<MutationCategory>? priorityCategories = null,
        List<int>? surgePriorityIds = null,
        int surgeFrequency = GameBalance.DefaultSurgeAIAttemptTurnFrequency,
        List<MycovariantPreference>? mycovariantPreferences = null,
        IEnumerable<int>? excludedMutationIds = null)
    {
        return new ParameterizedSpendingStrategy(
            strategyName: "Characterization",
            prioritizeHighTier: false,
            priorityMutationCategories: priorityCategories,
            surgePriorityIds: surgePriorityIds,
            surgeAttemptTurnFrequency: surgeFrequency,
            economyBias: EconomyBias.IgnoreEconomy,
            mycovariantPreferences: mycovariantPreferences,
            excludedMutationIds: excludedMutationIds);
    }

    private static (GameBoard Board, Player Player) CreateBoardAndPlayer(
        int width = 5,
        int height = 5,
        int mutationPoints = 0,
        int round = 1)
    {
        var board = new GameBoard(width, height, playerCount: 1);
        board.RestoreRoundState(round, currentGrowthCycle: 0, necrophyticBloomActivated: false, pendingHypervariationDraftPlayerIds: null);
        var player = new Player(0, "Characterization AI", PlayerTypeEnum.AI)
        {
            MutationPoints = mutationPoints
        };
        board.Players.Add(player);
        return (board, player);
    }

    private static Mutation CreateMutation(int id, int points, bool isSurge = false)
    {
        return new Mutation(
            id,
            $"Mutation {id}",
            description: "Test mutation",
            flavorText: "Test mutation",
            type: MutationType.GrowthChance,
            effectPerLevel: 0.01f,
            pointsPerUpgrade: points,
            maxLevel: 5,
            category: MutationCategory.Growth,
            tier: MutationTier.Tier1,
            isSurge: isSurge,
            surgeDuration: isSurge ? 2 : 0,
            pointsPerActivation: points);
    }

    private static Mycovariant CreateMycovariant(int id, float score)
    {
        return new Mycovariant
        {
            Id = id,
            Name = $"Mycovariant {id}",
            AIScore = (_, _) => score
        };
    }
}
