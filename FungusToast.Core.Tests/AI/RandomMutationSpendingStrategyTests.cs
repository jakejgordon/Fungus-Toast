using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.AI;

public class RandomMutationSpendingStrategyTests
{
    [Fact]
    public void SpendMutationPoints_uses_the_supplied_random_source()
    {
        var first = SpendWithSeed(24680);
        var second = SpendWithSeed(24680);
        var differentSeed = SpendWithSeed(13579);

        Assert.Equal(first.RemainingPoints, second.RemainingPoints);
        Assert.Equal(first.MutationLevels, second.MutationLevels);
        Assert.False(first.MutationLevels.SequenceEqual(differentSeed.MutationLevels));
    }

    private static (int RemainingPoints, (int MutationId, int Level)[] MutationLevels) SpendWithSeed(int seed)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(0, "Random AI", PlayerTypeEnum.AI)
        {
            MutationPoints = 20
        };
        board.Players.Add(player);

        var strategy = new RandomMutationSpendingStrategy();
        strategy.SpendMutationPoints(
            player,
            MutationRegistry.GetAll().ToList(),
            board,
            new Random(seed),
            new TestSimulationObserver());

        var mutationLevels = player.PlayerMutations
            .OrderBy(entry => entry.Key)
            .Select(entry => (entry.Key, entry.Value.CurrentLevel))
            .ToArray();

        return (player.MutationPoints, mutationLevels);
    }
}
