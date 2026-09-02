using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Phases;

public class TurnEngineMutationPhaseTests
{
    [Fact]
    public void AssignMutationPointIncome_defers_strategy_spending()
    {
        var board = new GameBoard(width: 4, height: 4, playerCount: 1);
        var player = new Player(0, "AI", PlayerTypeEnum.AI);
        var strategy = new CountingStrategy();
        player.SetBaseMutationPoints(4);
        player.SetMutationStrategy(strategy);
        board.Players.Add(player);

        TurnEngine.AssignMutationPointIncome(
            board,
            board.Players,
            MutationRegistry.GetAll().ToList(),
            new Random(123),
            new TestSimulationObserver());

        Assert.Equal(4, player.MutationPoints);
        Assert.Equal(0, strategy.SpendingCalls);
    }

    [Fact]
    public void AssignMutationPoints_preserves_single_step_strategy_spending()
    {
        var board = new GameBoard(width: 4, height: 4, playerCount: 1);
        var player = new Player(0, "AI", PlayerTypeEnum.AI);
        var strategy = new CountingStrategy();
        player.SetBaseMutationPoints(4);
        player.SetMutationStrategy(strategy);
        board.Players.Add(player);

        TurnEngine.AssignMutationPoints(
            board,
            board.Players,
            MutationRegistry.GetAll().ToList(),
            new Random(123),
            new TestSimulationObserver());

        Assert.Equal(4, player.MutationPoints);
        Assert.Equal(1, strategy.SpendingCalls);
    }

    private sealed class CountingStrategy : IMutationSpendingStrategy
    {
        public int SpendingCalls { get; private set; }
        public string StrategyName => "Counting";
        public MutationTier? MaxTier => null;
        public bool? PrioritizeHighTier => null;
        public bool? UsesGrowth => null;
        public bool? UsesCellularResilience => null;
        public bool? UsesFungicide => null;
        public bool? UsesGeneticDrift => null;
        public bool? UsesSubstrateEcology => null;

        public void SpendMutationPoints(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            Random rnd,
            ISimulationObserver simulationObserver)
        {
            SpendingCalls++;
        }
    }
}
