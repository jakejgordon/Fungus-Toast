using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class Tier2MutationTests
{
    [Fact]
    public void AdaptiveExpression_is_tier2_genetic_drift_and_requires_mutator_phenotype_level_five()
    {
        var mutation = RequireMutation(MutationIds.AdaptiveExpression);

        Assert.Equal(MutationCategory.GeneticDrift, mutation.Category);
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.Equal(MutationType.BonusMutationPointChance, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.MutatorPhenotype, prereq.MutationId);
        Assert.Equal(5, prereq.RequiredLevel);
    }

    [Fact]
    public void TryApplyAdaptiveExpression_awards_no_points_when_proc_roll_misses()
    {
        var player = CreatePlayer();
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.AdaptiveExpression, newLevel: 1, currentRound: 1);

        GeneticDriftMutationProcessor.TryApplyAdaptiveExpression(player, new Random(123456), observer);

        Assert.Equal(0, player.MutationPoints);
        Assert.Null(observer.LastApicalYieldBonus);
        Assert.Null(observer.LastMutationPointIncome);
    }

    [Fact]
    public void TryApplyAdaptiveExpression_awards_bonus_points_when_proc_rolls_hit()
    {
        var player = CreatePlayer();
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.AdaptiveExpression, newLevel: 5, currentRound: 1);

        GeneticDriftMutationProcessor.TryApplyAdaptiveExpression(player, new Random(1), observer);

        Assert.InRange(player.MutationPoints, 1, 2);
        Assert.InRange(observer.LastMutationPointIncome.GetValueOrDefault(), 1, 2);
    }

    [Fact]
    public void ChronoresilientCytoplasm_is_tier2_cellular_resilience_and_requires_homeostatic_harmony_level_five()
    {
        var mutation = RequireMutation(MutationIds.ChronoresilientCytoplasm);

        Assert.Equal(MutationCategory.CellularResilience, mutation.Category);
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.Equal(MutationType.AgeAndRandomnessDecayResistance, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.HomeostaticHarmony, prereq.MutationId);
        Assert.Equal(5, prereq.RequiredLevel);
    }

    [Fact]
    public void ChronoresilientCytoplasm_effect_scales_linearly_by_level()
    {
        var mutation = RequireMutation(MutationIds.ChronoresilientCytoplasm);

        Assert.Equal(0f, mutation.GetTotalEffect(0), precision: 6);
        Assert.Equal(3 * GameBalance.ChronoresilientCytoplasmEffectPerLevel, mutation.GetTotalEffect(3), precision: 6);
    }

    [Fact]
    public void MycotoxinPotentiation_is_tier2_fungicide_and_requires_mycotoxin_tracer_level_five()
    {
        var mutation = RequireMutation(MutationIds.MycotoxinPotentiation);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.Equal(MutationType.ToxinKillAura, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.MycotoxinTracer, prereq.MutationId);
        Assert.Equal(5, prereq.RequiredLevel);
    }

    [Fact]
    public void MycotoxinPotentiation_extends_toxin_expiration_age_by_one_growth_cycle_per_level()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.MycotoxinPotentiation, newLevel: 4, currentRound: 1);

        var duration = ToxinHelper.GetToxinExpirationAge(player, baseToxinDuration: 10);

        Assert.Equal(10 + (4 * GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel), duration);
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
