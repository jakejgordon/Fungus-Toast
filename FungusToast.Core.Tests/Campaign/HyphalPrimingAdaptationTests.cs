using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Campaign;

public class HyphalPrimingAdaptationTests
{
    [Fact]
    public void HyphalPriming_upgrades_a_single_non_surge_tier2_mutation_by_two_levels_on_round_one()
    {
        var board = CreateBoardWithPlayer(out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.HyphalPriming));

        MaxOutEligibleTier2MutationsExcept(player, MutationIds.TendrilNorthwest);

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.Equal(AdaptationGameBalance.HyphalPrimingLevelsGranted, player.GetMutationLevel(MutationIds.TendrilNorthwest));
        Assert.Equal(0, player.GetMutationLevel(MutationIds.HyphalSurge));
        Assert.True(player.GetAdaptation(AdaptationIds.HyphalPriming)!.HasTriggered);
    }

    [Fact]
    public void HyphalPriming_prefers_targets_with_full_two_level_headroom()
    {
        var board = CreateBoardWithPlayer(out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.HyphalPriming));

        MaxOutEligibleTier2MutationsExcept(player, MutationIds.TendrilNorthwest, MutationIds.ChronoresilientCytoplasm);
        var partialTarget = RequireMutation(MutationIds.TendrilNorthwest);
        player.SetMutationLevel(partialTarget.Id, partialTarget.MaxLevel - 1, currentRound: board.CurrentRound);

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.Equal(partialTarget.MaxLevel - 1, player.GetMutationLevel(MutationIds.TendrilNorthwest));
        Assert.Equal(AdaptationGameBalance.HyphalPrimingLevelsGranted, player.GetMutationLevel(MutationIds.ChronoresilientCytoplasm));
    }

    [Fact]
    public void HyphalPriming_allows_a_partial_upgrade_when_no_target_has_full_headroom()
    {
        var board = CreateBoardWithPlayer(out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.HyphalPriming));

        MaxOutEligibleTier2MutationsExcept(player, MutationIds.TendrilNorthwest);
        var target = RequireMutation(MutationIds.TendrilNorthwest);
        player.SetMutationLevel(target.Id, target.MaxLevel - 1, currentRound: board.CurrentRound);

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.Equal(target.MaxLevel, player.GetMutationLevel(MutationIds.TendrilNorthwest));
    }

    [Fact]
    public void HyphalPriming_does_not_trigger_after_round_one()
    {
        var board = CreateBoardWithPlayer(out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.HyphalPriming));

        MaxOutEligibleTier2MutationsExcept(player, MutationIds.TendrilNorthwest);
        board.IncrementRound();

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(0), observer);

        Assert.Equal(0, player.GetMutationLevel(MutationIds.TendrilNorthwest));
        Assert.False(player.GetAdaptation(AdaptationIds.HyphalPriming)!.HasTriggered);
    }

    private static GameBoard CreateBoardWithPlayer(out Player player)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);
        return board;
    }

    private static void MaxOutEligibleTier2MutationsExcept(Player player, params int[] excludedMutationIds)
    {
        var excluded = excludedMutationIds.ToHashSet();
        foreach (var mutation in MutationRegistry.All.Values
                     .Where(mutation => mutation.Tier == MutationTier.Tier2)
                     .Where(mutation => mutation.Category != MutationCategory.MycelialSurges)
                     .Where(mutation => !excluded.Contains(mutation.Id)))
        {
            player.SetMutationLevel(mutation.Id, mutation.MaxLevel, currentRound: 1);
        }
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}