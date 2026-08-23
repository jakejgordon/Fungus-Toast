using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;

namespace FungusToast.Core.Tests.Mutations;

public class MycelialBloomTradeoffTests
{
    [Fact]
    public void MycelialBloom_description_mentions_growth_and_random_death_tradeoff()
    {
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        Assert.Contains("four cardinal directions", mutation.Description);
        Assert.Contains("cardinal growth chance", mutation.Description);
        Assert.Contains("random decay chance", mutation.Description);
    }

    [Fact]
    public void MycelialBloom_increases_random_decay_chance_by_level()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 3, currentRound: 1);

        var result = CalculateDeathChance(player);

        Assert.False(result.ShouldDie);
        Assert.Equal(
            GameBalance.BaseRandomDecayChance + (3 * GameBalance.MycelialBloomRandomDecayPenaltyPerLevel),
            result.Chance,
            precision: 6);
    }

    [Fact]
    public void HomeostaticHarmony_offsets_mycelial_bloom_random_decay_penalty()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 4, currentRound: 1);
        player.SetMutationLevel(MutationIds.HomeostaticHarmony, newLevel: 2, currentRound: 1);

        var result = CalculateDeathChance(player);

        Assert.False(result.ShouldDie);
        Assert.Equal(
            Math.Max(
                0f,
                GameBalance.BaseRandomDecayChance
                + (4 * GameBalance.MycelialBloomRandomDecayPenaltyPerLevel)
                - (2 * GameBalance.HomeostaticHarmonyEffectPerLevel)),
            result.Chance,
            precision: 6);
    }

    [Fact]
    public void AutolyticSurge_increases_random_decay_only_while_active()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.HyphalSurge, newLevel: 2, currentRound: 1);

        var inactiveResult = CalculateDeathChance(player);
        player.ActiveSurges[MutationIds.HyphalSurge] = new Player.ActiveSurgeInfo(
            MutationIds.HyphalSurge,
            level: 2,
            duration: GameBalance.HyphalSurgeDurationRounds);
        var activeResult = CalculateDeathChance(player);

        Assert.Equal(GameBalance.BaseRandomDecayChance, inactiveResult.Chance, precision: 6);
        Assert.Equal(
            GameBalance.BaseRandomDecayChance + (2 * GameBalance.HyphalSurgeRandomDecayPenaltyPerLevel),
            activeResult.Chance,
            precision: 6);
    }

    [Fact]
    public void AutolyticSurge_active_growth_bonus_scales_by_level()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.HyphalSurge, newLevel: 3, currentRound: 1);
        player.ActiveSurges[MutationIds.HyphalSurge] = new Player.ActiveSurgeInfo(
            MutationIds.HyphalSurge,
            level: 3,
            duration: GameBalance.HyphalSurgeDurationRounds);

        var (baseChance, surgeBonus) = GrowthMutationProcessor.GetGrowthChancesWithHyphalSurge(player);

        Assert.Equal(GameBalance.BaseGrowthChance, baseChance, precision: 6);
        Assert.Equal(3 * GameBalance.HyphalSurgeEffectPerLevel, surgeBonus, precision: 6);
    }

    [Fact]
    public void AutolyticSurge_definition_describes_the_three_round_growth_and_decay_tradeoff()
    {
        var mutation = RequireMutation(MutationIds.HyphalSurge);

        Assert.Equal("Autolytic Surge", mutation.Name);
        Assert.Equal(GameBalance.HyphalSurgeDurationRounds, mutation.SurgeDuration);
        Assert.Equal(GameBalance.HyphalSurgeEffectPerLevel, mutation.EffectPerLevel);
        Assert.Contains("random decay chance", mutation.Description);
        Assert.Contains("Growth Cycle", mutation.Description);
        Assert.Contains("Decay Phase", mutation.Description);
    }

    private static DeathCalculationResult CalculateDeathChance(Player owner)
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var cell = new FungalCell(owner.PlayerId, tileId: 4, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        return MutationEffectCoordinator.CalculateDeathChance(
            owner,
            cell,
            board,
            new List<Player> { owner },
            roll: 0.99d,
            rng: new Random(1234),
            observer: new TestSimulationObserver());
    }

    private static Player CreatePlayer()
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }
}
