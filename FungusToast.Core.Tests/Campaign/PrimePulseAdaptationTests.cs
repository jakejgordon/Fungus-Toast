using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Campaign;

public class PrimePulseAdaptationTests
{
    [Theory]
    [InlineData(1, 7)]
    [InlineData(5, 11)]
    [InlineData(0, 13)]
    public void PrimePulse_rolls_one_prime_round_and_awards_matching_mutation_points_once(int seed, int expectedRound)
    {
        var board = CreateBoardWithPlayer(out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.PrimePulse));

        AdaptationEffectProcessor.OnStartingSporesEstablished(board, board.Players, new Random(seed));

        for (int round = 1; round <= expectedRound; round++)
        {
            AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

            if (round < expectedRound)
            {
                Assert.Equal(0, player.MutationPoints);
                Assert.False(player.GetAdaptation(AdaptationIds.PrimePulse)!.HasTriggered);
                board.IncrementRound();
                continue;
            }

            Assert.Equal(expectedRound, player.MutationPoints);
            Assert.Equal(expectedRound, observer.LastMutationPointIncome);
            Assert.True(player.GetAdaptation(AdaptationIds.PrimePulse)!.HasTriggered);
        }

        board.IncrementRound();
        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.Equal(expectedRound, player.MutationPoints);
    }

    private static GameBoard CreateBoardWithPlayer(out Player player)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);
        return board;
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}
