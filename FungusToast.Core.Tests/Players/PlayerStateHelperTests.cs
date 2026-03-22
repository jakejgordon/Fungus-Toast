using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Players;

public class PlayerStateHelperTests
{
    [Fact]
    public void SetMutationStrategy_updates_strategy_reference()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        var strategy = new StubMutationSpendingStrategy("test-strategy");

        player.SetMutationStrategy(strategy);

        Assert.Same(strategy, player.MutationStrategy);
    }

    [Fact]
    public void AddMutationPoints_increases_current_mutation_points()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = 3
        };

        player.AddMutationPoints(5);

        Assert.Equal(8, player.MutationPoints);
    }

    [Fact]
    public void AssignMutationPoints_adds_base_income_and_records_it_with_observer()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = 2
        };
        player.SetBaseMutationPoints(4);
        var observer = new TestSimulationObserver();
        var board = new GameBoard(width: 4, height: 4, playerCount: 1);

        var resultingPoints = player.AssignMutationPoints(new List<Player> { player }, new Random(123), board, observer);

        Assert.Equal(6, resultingPoints);
        Assert.Equal(6, player.MutationPoints);
        Assert.Equal(4, observer.LastMutationPointIncome);
    }

    [Fact]
    public void AddMycovariant_adds_once_and_auto_marks_triggered_when_configured()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        var mycovariant = new Mycovariant
        {
            Id = 9991,
            Name = "Auto Triggered Myco",
            AutoMarkTriggered = true
        };

        player.AddMycovariant(mycovariant);
        player.AddMycovariant(mycovariant);

        var acquired = Assert.Single(player.PlayerMycovariants);
        Assert.True(acquired.HasTriggered, "Expected auto-triggered mycovariant to be marked triggered on add.");
        Assert.True(player.HasMycovariant(mycovariant.Id));
        Assert.Same(acquired, player.GetMycovariant(mycovariant.Id));
    }

    [Fact]
    public void WantsToBankPointsThisTurn_round_trips_as_simple_state()
    {
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);

        player.WantsToBankPointsThisTurn = true;

        Assert.True(player.WantsToBankPointsThisTurn);
    }

    private sealed class StubMutationSpendingStrategy : IMutationSpendingStrategy
    {
        public string StrategyName { get; }
        public MutationTier? MaxTier => null;
        public bool? PrioritizeHighTier => null;
        public bool? UsesGrowth => null;
        public bool? UsesCellularResilience => null;
        public bool? UsesFungicide => null;
        public bool? UsesGeneticDrift => null;

        public StubMutationSpendingStrategy(string strategyName)
        {
            StrategyName = strategyName;
        }

        public void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board, Random rnd, ISimulationObserver simulationObserver)
        {
        }
    }
}
