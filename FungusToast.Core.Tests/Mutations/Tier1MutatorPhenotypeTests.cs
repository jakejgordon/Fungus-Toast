using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class Tier1MutatorPhenotypeTests
{
    [Fact]
    public void MutatorPhenotype_is_a_root_genetic_drift_mutation_with_no_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.MutatorPhenotype);

        Assert.Equal(MutationCategory.GeneticDrift, mutation.Category);
        Assert.Equal(MutationTier.Tier1, mutation.Tier);
        Assert.Equal(MutationType.AutoUpgradeRandom, mutation.Type);
        Assert.Empty(mutation.Prerequisites);
        Assert.Contains(mutation.Id, MutationRegistry.Roots.Keys);
    }

    [Fact]
    public void MutatorPhenotype_root_upgrade_is_allowed_on_round_one_when_points_are_available()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var mutation = RequireMutation(MutationIds.MutatorPhenotype);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 1);

        Assert.True(canUpgrade, $"Expected root mutation {mutation.Name} to be upgradable without prerequisites.");
    }

    [Fact]
    public void Upgrading_mutator_phenotype_to_level_two_sets_prereq_met_round_for_mycotoxin_catabolism()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var mutation = RequireMutation(MutationIds.MutatorPhenotype);

        player.SetMutationLevel(mutation.Id, newLevel: 1, currentRound: 1);
        var upgraded = player.TryUpgradeMutation(mutation, observer, currentRound: 2);

        Assert.True(upgraded);
        Assert.Equal(2, player.GetMutationLevel(mutation.Id));
        Assert.Equal(2, Assert.IsType<PlayerMutation>(player.PlayerMutations[MutationIds.MycotoxinCatabolism]).PrereqMetRound);
    }

    [Fact]
    public void Upgrading_mutator_phenotype_to_level_five_sets_prereq_met_round_for_adaptive_expression()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var mutation = RequireMutation(MutationIds.MutatorPhenotype);

        player.SetMutationLevel(mutation.Id, newLevel: 4, currentRound: 1);
        var upgraded = player.TryUpgradeMutation(mutation, observer, currentRound: 2);

        Assert.True(upgraded);
        Assert.Equal(5, player.GetMutationLevel(mutation.Id));
        Assert.Equal(2, Assert.IsType<PlayerMutation>(player.PlayerMutations[MutationIds.AdaptiveExpression]).PrereqMetRound);
    }

    [Fact]
    public void TryApplyMutatorPhenotype_does_nothing_when_no_mutations_are_eligible_for_auto_upgrade()
    {
        var player = CreatePlayer();
        var observer = new TestSimulationObserver();
        var allMutations = MutationRegistry.GetAll().ToList();
        player.SetMutationLevel(MutationIds.MutatorPhenotype, newLevel: 10, currentRound: 1);

        foreach (var mutation in allMutations)
        {
            player.SetMutationLevel(mutation.Id, mutation.MaxLevel, currentRound: 1);
        }

        GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(player, allMutations, new Random(123), currentRound: 2, observer);

        Assert.Equal(0, observer.UpgradeEventCount);
        Assert.Null(observer.LastUpgradeSource);
        Assert.Null(observer.LastMutationPointsSpent);
    }

    [Fact]
    public void TryApplyMutatorPhenotype_at_max_level_can_upgrade_a_tier1_mutation_and_records_observer_effects()
    {
        var player = CreatePlayer();
        var observer = new TestSimulationObserver();
        var allMutations = MutationRegistry.GetAll().ToList();
        player.SetMutationLevel(MutationIds.MutatorPhenotype, newLevel: 10, currentRound: 1);

        GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(player, allMutations, new Random(1), currentRound: 2, observer);

        Assert.True(player.GetMutationLevel(MutationIds.MycelialBloom) > 0 ||
                    player.GetMutationLevel(MutationIds.HomeostaticHarmony) > 0 ||
                    player.GetMutationLevel(MutationIds.MycotoxinTracer) > 0,
            "Expected Mutator Phenotype to auto-upgrade at least one Tier 1 mutation when proc chance is guaranteed and eligible roots exist.");
        Assert.True(observer.UpgradeEventCount > 0, "Expected auto-upgrade to be recorded by the observer.");
        Assert.Equal("auto", observer.LastUpgradeSource);
        Assert.True(observer.LastMutationPointsSpent.GetValueOrDefault() == 0, "Expected auto-upgrades not to spend mutation points.");
    }

    private static Player CreatePlayer(int mutationPoints = 0)
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = mutationPoints
        };
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }
}
