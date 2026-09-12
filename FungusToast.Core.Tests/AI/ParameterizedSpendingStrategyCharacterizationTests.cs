using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Phases;
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

    /// <summary>
    /// Category-derived ids arrive in repository declaration order, which puts the weakest tier of
    /// each family first. Consuming that as a ranking made every roster strategy draft Mycelial
    /// Bastion I ahead of III, so the set must resolve by AI score instead.
    /// </summary>
    [Fact]
    public void Category_derived_preference_drafts_the_strongest_member_on_offer()
    {
        var weak = CreateMycovariant(101, score: 4f);
        var strong = CreateMycovariant(103, score: 6f);
        var strategy = CreateStrategy(
            preferredMycovariantIds: new CategoryDerivedMycovariantIds(new[] { weak.Id, strong.Id }));
        var (board, player) = CreateBoardAndPlayer();

        var selected = strategy.SelectMycovariantFromChoices(
            player,
            new List<Mycovariant> { weak, strong },
            board,
            new Random(1));

        Assert.Same(strong, selected);
    }

    /// <summary>
    /// A hand-written list is the opposite contract: the author ranked those ids on purpose, so
    /// position still beats score.
    /// </summary>
    [Fact]
    public void Authored_preferred_id_list_still_ranks_by_position()
    {
        var firstListed = CreateMycovariant(101, score: 4f);
        var higherScored = CreateMycovariant(103, score: 6f);
        var strategy = CreateStrategy(
            preferredMycovariantIds: new List<int> { firstListed.Id, higherScored.Id });
        var (board, player) = CreateBoardAndPlayer();

        var selected = strategy.SelectMycovariantFromChoices(
            player,
            new List<Mycovariant> { firstListed, higherScored },
            board,
            new Random(1));

        Assert.Same(firstListed, selected);
    }

    /// <summary>
    /// Owning one member of a category set does not satisfy the whole category, so the remaining
    /// members stay preferred over an unrelated fallback pick.
    /// </summary>
    [Fact]
    public void Category_derived_preference_keeps_offering_members_the_player_lacks()
    {
        var owned = CreateMycovariant(101, score: 4f);
        var remaining = CreateMycovariant(103, score: 5f);
        var unrelated = CreateMycovariant(500, score: 9f);
        var strategy = CreateStrategy(
            preferredMycovariantIds: new CategoryDerivedMycovariantIds(new[] { owned.Id, remaining.Id }));
        var (board, player) = CreateBoardAndPlayer();
        player.AddMycovariant(owned);

        var selected = strategy.SelectMycovariantFromChoices(
            player,
            new List<Mycovariant> { unrelated, remaining },
            board,
            new Random(1));

        Assert.Same(remaining, selected);
    }

    /// <summary>
    /// An authored preference means one want, so owning any member retires it and the strategy
    /// falls through to ordinary scoring.
    /// </summary>
    [Fact]
    public void Authored_preference_is_satisfied_once_any_member_is_owned()
    {
        var owned = CreateMycovariant(101, score: 4f);
        var sibling = CreateMycovariant(103, score: 5f);
        var unrelated = CreateMycovariant(500, score: 9f);
        var strategy = CreateStrategy(mycovariantPreferences: new List<MycovariantPreference>
        {
            new(new[] { owned.Id, sibling.Id }, priority: 5)
        });
        var (board, player) = CreateBoardAndPlayer();
        player.AddMycovariant(owned);

        var selected = strategy.SelectMycovariantFromChoices(
            player,
            new List<Mycovariant> { unrelated, sibling },
            board,
            new Random(1));

        Assert.Same(unrelated, selected);
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
    public void Latent_polymorphism_preserves_a_five_point_reserve_before_the_final_three_rounds()
    {
        var ordinary = CreateMutation(903, points: 2);
        var strategy = CreateStrategy(priorityCategories: new List<MutationCategory> { MutationCategory.Growth });
        var (board, player) = CreateBoardAndPlayer(mutationPoints: 7, round: 71);
        player.SetMutationLevel(MutationIds.LatentPolymorphism, 1, currentRound: 1);
        var observer = new TestSimulationObserver();

        strategy.SpendMutationPoints(
            player,
            new List<Mutation> { ordinary },
            board,
            new Random(1),
            observer);

        Assert.Equal(1, player.GetMutationLevel(ordinary.Id));
        Assert.Equal(5, player.MutationPoints);
        Assert.Equal(5, observer.LastBankedPoints);
        Assert.True(player.WantsToBankPointsThisTurn, "Latent Polymorphism should mark the preserved reserve as banked.");
    }

    [Fact]
    public void Latent_polymorphism_spends_the_reserve_during_the_final_three_rounds()
    {
        var ordinary = CreateMutation(904, points: 2);
        var strategy = CreateStrategy(priorityCategories: new List<MutationCategory> { MutationCategory.Growth });
        var (board, player) = CreateBoardAndPlayer(mutationPoints: 7, round: 72);
        player.SetMutationLevel(MutationIds.LatentPolymorphism, 1, currentRound: 1);
        var observer = new TestSimulationObserver();

        strategy.SpendMutationPoints(
            player,
            new List<Mutation> { ordinary },
            board,
            new Random(1),
            observer);

        Assert.Equal(3, player.GetMutationLevel(ordinary.Id));
        Assert.Equal(1, player.MutationPoints);
        Assert.Null(observer.LastBankedPoints);
        Assert.False(player.WantsToBankPointsThisTurn, "The late-game exception should spend through the Latent Polymorphism reserve.");
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
    public void Interface_exclusions_also_block_free_mutator_phenotype_upgrades()
    {
        var player = new Player(0, "Interface AI", PlayerTypeEnum.AI);
        player.SetMutationStrategy(new InterfaceExclusionStrategy(MutationIds.AeratedFrontier));
        var mutations = MutationRegistry.GetAll().ToList();

        foreach (var mutation in mutations.Where(mutation =>
                     mutation.Tier == MutationTier.Tier1
                     && mutation.Id != MutationIds.AeratedFrontier))
        {
            player.SetMutationLevel(mutation.Id, mutation.MaxLevel, currentRound: 1);
        }

        player.SetMutationLevel(
            MutationIds.MutatorPhenotype,
            GameBalance.MutatorPhenotypeMaxLevel,
            currentRound: 1);

        GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(
            player,
            mutations,
            new Random(1),
            currentRound: 2,
            new TestSimulationObserver());

        Assert.Equal(0, player.GetMutationLevel(MutationIds.AeratedFrontier));
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
        List<int>? preferredMycovariantIds = null,
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
            preferredMycovariantIds: preferredMycovariantIds,
            excludedMutationIds: excludedMutationIds);
    }

    /// <summary>
    /// Tendril choice substitutes the best direction for whichever tendril was offered, so it has
    /// to re-apply the exclusion list. Without that, a strategy could get an excluded tendril back
    /// through the substitution - the same bypass free upgrades had, and the reason an ablation of
    /// a tendril silently failed to hold.
    /// </summary>
    [Fact]
    public void Tendril_choice_never_substitutes_an_excluded_tendril()
    {
        var excluded = MutationRegistry.GetById(MutationIds.TendrilSouthwest)!;
        var allowed = MutationRegistry.GetById(MutationIds.TendrilNortheast)!;
        var strategy = CreateStrategy(
            priorityCategories: new List<MutationCategory> { MutationCategory.Growth },
            excludedMutationIds: new[] { MutationIds.TendrilSouthwest });
        var (board, player) = CreateBoardAndPlayer(mutationPoints: 40, round: 1);

        strategy.SpendMutationPoints(
            player,
            MutationRegistry.GetAll().ToList(),
            board,
            new Random(7),
            new TestSimulationObserver());

        Assert.Equal(0, player.GetMutationLevel(excluded.Id));
        Assert.True(
            player.PlayerMutations.Count > 0,
            "The strategy should still have spent its points on something.");
        _ = allowed;
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

    private sealed class InterfaceExclusionStrategy : IMutationSpendingStrategy
    {
        public InterfaceExclusionStrategy(params int[] excludedMutationIds)
        {
            ExcludedMutationIds = excludedMutationIds;
        }

        public string StrategyName => "Interface exclusion";
        public MutationTier? MaxTier => MutationTier.Tier10;
        public bool? PrioritizeHighTier => false;
        public bool? UsesGrowth => null;
        public bool? UsesCellularResilience => null;
        public bool? UsesFungicide => null;
        public bool? UsesGeneticDrift => true;
        public IReadOnlyCollection<int> ExcludedMutationIds { get; }

        public Mycovariant SelectMycovariantFromChoices(
            Player player,
            List<Mycovariant> choices,
            GameBoard board,
            Random rnd) => choices[0];

        public void SpendMutationPoints(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            Random rnd,
            ISimulationObserver simulationObserver)
        {
        }
    }
}
