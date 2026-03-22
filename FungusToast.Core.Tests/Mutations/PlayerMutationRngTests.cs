using FungusToast.Core.Board;
using FungusToast.Core.Common;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class PlayerMutationRngTests
{
    [Fact]
    public void GetBonusMutationPoints_returns_zero_when_adaptive_expression_is_absent()
    {
        var player = CreatePlayer();

        var bonus = player.GetBonusMutationPoints(new SequenceRandomSource(0.0, 0.0));

        Assert.Equal(0, bonus);
    }

    [Fact]
    public void GetBonusMutationPoints_returns_zero_when_first_roll_misses()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.AdaptiveExpression, newLevel: 1, currentRound: 1);

        var bonus = player.GetBonusMutationPoints(new SequenceRandomSource(0.99));

        Assert.Equal(0, bonus);
    }

    [Fact]
    public void GetBonusMutationPoints_returns_one_when_first_roll_hits_and_second_roll_misses()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.AdaptiveExpression, newLevel: 1, currentRound: 1);

        var bonus = player.GetBonusMutationPoints(new SequenceRandomSource(0.0, 0.99));

        Assert.Equal(1, bonus);
    }

    [Fact]
    public void GetBonusMutationPoints_returns_two_when_both_rolls_hit()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.AdaptiveExpression, newLevel: 1, currentRound: 1);

        var bonus = player.GetBonusMutationPoints(new SequenceRandomSource(0.0, 0.0));

        Assert.Equal(2, bonus);
    }

    [Fact]
    public void GetBonusMutationPoints_obeys_injected_balance_values()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.AdaptiveExpression, newLevel: 1, currentRound: 1);
        var balance = new TestCoreBalance
        {
            AdaptiveExpressionEffectPerLevel = 1.0f,
            AdaptiveExpressionSecondPointChancePerLevel = 0.0f
        };

        var bonus = player.GetBonusMutationPoints(new SequenceRandomSource(0.5, 0.5), balance);

        Assert.Equal(1, bonus);
    }

    [Fact]
    public void RollAnabolicInversionBonus_returns_zero_when_mutation_is_absent()
    {
        var player = CreatePlayer();
        var other = CreatePlayer(1);
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);

        var bonus = player.RollAnabolicInversionBonus(new List<Player> { player, other }, new SequenceRandomSource(0.0), board, new Dictionary<int, int> { [0] = 1, [1] = 3 });

        Assert.Equal(0, bonus);
    }

    [Fact]
    public void RollAnabolicInversionBonus_returns_zero_when_proc_roll_misses()
    {
        var player = CreatePlayer();
        var other = CreatePlayer(1);
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        player.SetMutationLevel(MutationIds.AnabolicInversion, newLevel: 1, currentRound: 1);

        var bonus = player.RollAnabolicInversionBonus(new List<Player> { player, other }, new SequenceRandomSource(0.99), board, new Dictionary<int, int> { [0] = 1, [1] = 3 });

        Assert.Equal(0, bonus);
    }

    [Fact]
    public void RollAnabolicInversionBonus_returns_one_when_proc_hits_but_reward_roll_is_lowest_band()
    {
        var player = CreatePlayer();
        var other = CreatePlayer(1);
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        player.SetMutationLevel(MutationIds.AnabolicInversion, newLevel: 1, currentRound: 1);

        var bonus = player.RollAnabolicInversionBonus(new List<Player> { player, other }, new SequenceRandomSource(0.0, 0.99), board, new Dictionary<int, int> { [0] = 1, [1] = 3 });

        Assert.Equal(1, bonus);
    }

    [Fact]
    public void RollAnabolicInversionBonus_returns_high_band_reward_when_rolls_favor_it()
    {
        var player = CreatePlayer();
        var other = CreatePlayer(1);
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        player.SetMutationLevel(MutationIds.AnabolicInversion, newLevel: 3, currentRound: 1);

        var bonus = player.RollAnabolicInversionBonus(new List<Player> { player, other }, new SequenceRandomSource(0.0, 0.0, 0.0), board, new Dictionary<int, int> { [0] = 0, [1] = 10 });

        Assert.InRange(bonus, 4, 5);
    }

    [Fact]
    public void RollAnabolicInversionBonus_obeys_injected_balance_values_and_reward_cap()
    {
        var player = CreatePlayer();
        var other = CreatePlayer(1);
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        player.SetMutationLevel(MutationIds.AnabolicInversion, newLevel: 1, currentRound: 1);
        var balance = new TestCoreBalance
        {
            AnabolicInversionGapBonusPerLevel = 1.0f,
            AnabolicInversionHighRewardCutoff = 1.0f,
            AnabolicInversionMidRewardCutoff = 1.0f,
            AnabolicInversionLowRewardCutoff = 1.0f,
            AnabolicInversionMaxMutationPointsPerRound = 2
        };

        var bonus = player.RollAnabolicInversionBonus(
            new List<Player> { player, other },
            new SequenceRandomSource(0.0, 0.0, 0.0),
            board,
            new Dictionary<int, int> { [0] = 0, [1] = 10 },
            balance);

        Assert.Equal(2, bonus);
    }

    private static Player CreatePlayer(int playerId = 0)
    {
        return new Player(playerId: playerId, playerName: $"P{playerId}", playerType: PlayerTypeEnum.AI);
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<double> values;

        public SequenceRandomSource(params double[] values)
        {
            this.values = new Queue<double>(values);
        }

        public double NextDouble()
        {
            return values.Count > 0 ? values.Dequeue() : 0.0;
        }

        public int Next(int maxValue)
        {
            if (maxValue <= 0)
            {
                return 0;
            }

            return (int)Math.Min(maxValue - 1, Math.Floor(NextDouble() * maxValue));
        }
    }

    private sealed class TestCoreBalance : ICoreBalance
    {
        public float AdaptiveExpressionEffectPerLevel { get; set; } = GameBalance.AdaptiveExpressionEffectPerLevel;
        public float AdaptiveExpressionSecondPointChancePerLevel { get; set; } = GameBalance.AdaptiveExpressionSecondPointChancePerLevel;
        public float AnabolicInversionGapBonusPerLevel { get; set; } = GameBalance.AnabolicInversionGapBonusPerLevel;
        public float AnabolicInversionHighRewardCutoff { get; set; } = GameBalance.AnabolicInversionHighRewardCutoff;
        public float AnabolicInversionMidRewardCutoff { get; set; } = GameBalance.AnabolicInversionMidRewardCutoff;
        public float AnabolicInversionLowRewardCutoff { get; set; } = GameBalance.AnabolicInversionLowRewardCutoff;
        public int AnabolicInversionMaxMutationPointsPerRound { get; set; } = GameBalance.AnabolicInversionMaxMutationPointsPerRound;
    }
}
