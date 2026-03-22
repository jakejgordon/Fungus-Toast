using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class PlayerMutationBookkeepingTests
{
    [Fact]
    public void TryAddAdaptation_adds_adaptation_once_and_prevents_duplicates()
    {
        var player = CreatePlayer();
        var adaptation = RequireAdaptation(AdaptationIds.HyphalEconomy);

        var firstAdd = player.TryAddAdaptation(adaptation);
        var secondAdd = player.TryAddAdaptation(adaptation);

        Assert.True(firstAdd, $"Expected adaptation {adaptation.Name} to be added the first time.");
        Assert.False(secondAdd, $"Expected adaptation {adaptation.Name} not to be added twice.");
        Assert.True(player.HasAdaptation(adaptation.Id));
        Assert.Equal(adaptation.Id, Assert.IsType<PlayerAdaptation>(player.GetAdaptation(adaptation.Id)).Adaptation.Id);
    }

    [Fact]
    public void TryUpgradeMutation_raises_MutationsChanged_once_on_successful_manual_upgrade()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var mutation = RequireMutation(MutationIds.MycelialBloom);
        var observer = new TestSimulationObserver();
        int eventCount = 0;
        player.MutationsChanged += _ => eventCount++;

        var upgraded = player.TryUpgradeMutation(mutation, observer, currentRound: 1);

        Assert.True(upgraded);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void TryAutoUpgrade_raises_MutationsChanged_once_on_successful_auto_upgrade()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.HyphalSurge);
        var observer = new TestSimulationObserver();
        int eventCount = 0;
        player.MutationsChanged += _ => eventCount++;

        var upgraded = player.TryAutoUpgrade(mutation, currentRound: 1, observer);

        Assert.True(upgraded);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void SetMutationLevel_raises_MutationsChanged_when_level_changes_but_not_when_level_stays_the_same()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.MycelialBloom);
        int eventCount = 0;
        player.MutationsChanged += _ => eventCount++;

        player.SetMutationLevel(mutation.Id, newLevel: 1, currentRound: 1);
        player.SetMutationLevel(mutation.Id, newLevel: 1, currentRound: 1);

        Assert.Equal(1, player.GetMutationLevel(mutation.Id));
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void SetMutationLevel_clamps_to_valid_range()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        player.SetMutationLevel(mutation.Id, newLevel: -5, currentRound: 1);
        Assert.Equal(0, player.GetMutationLevel(mutation.Id));

        player.SetMutationLevel(mutation.Id, newLevel: mutation.MaxLevel + 10, currentRound: 1);
        Assert.Equal(mutation.MaxLevel, player.GetMutationLevel(mutation.Id));
    }

    [Fact]
    public void SetMutationLevel_removes_mutation_entry_when_level_is_set_to_zero()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        player.SetMutationLevel(mutation.Id, newLevel: 2, currentRound: 1);
        Assert.True(player.PlayerMutations.ContainsKey(mutation.Id));

        player.SetMutationLevel(mutation.Id, newLevel: 0, currentRound: 2);

        Assert.Equal(0, player.GetMutationLevel(mutation.Id));
        Assert.False(player.PlayerMutations.ContainsKey(mutation.Id));
    }

    [Fact]
    public void SetMutationLevel_records_prereq_met_round_for_dependents_when_threshold_is_reached()
    {
        var player = CreatePlayer();
        var prerequisite = RequireMutation(MutationIds.MycelialBloom);
        var dependent = RequireMutation(MutationIds.HyphalSurge);

        player.SetMutationLevel(prerequisite.Id, newLevel: 4, currentRound: 2);
        Assert.Null(player.PlayerMutations.GetValueOrDefault(dependent.Id)?.PrereqMetRound);

        player.SetMutationLevel(prerequisite.Id, newLevel: 5, currentRound: 3);

        var dependentPlayerMutation = Assert.IsType<PlayerMutation>(player.PlayerMutations[dependent.Id]);
        Assert.Equal(3, dependentPlayerMutation.PrereqMetRound);
    }

    [Fact]
    public void GetEffectiveGrowthChance_includes_base_growth_chance_standard_growth_bonus_and_hyphal_surge_bonus()
    {
        var player = CreatePlayer();
        var mycelialBloom = RequireMutation(MutationIds.MycelialBloom);
        var hyphalSurge = RequireMutation(MutationIds.HyphalSurge);

        player.SetMutationLevel(mycelialBloom.Id, newLevel: 2, currentRound: 1);
        player.SetMutationLevel(hyphalSurge.Id, newLevel: 1, currentRound: 1);

        var effectiveGrowthChance = player.GetEffectiveGrowthChance();

        Assert.Equal(
            GameBalance.BaseGrowthChance
            + mycelialBloom.GetTotalEffect(2)
            + hyphalSurge.GetTotalEffect(1),
            effectiveGrowthChance,
            precision: 6);
    }

    [Fact]
    public void GetEffectiveSelfDeathChance_uses_homeostatic_harmony_level()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.HomeostaticHarmony, newLevel: 3, currentRound: 1);

        var selfDeathChance = player.GetEffectiveSelfDeathChance();

        Assert.Equal(3 * GameBalance.HomeostaticHarmonyEffectPerLevel, selfDeathChance, precision: 6);
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

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}
