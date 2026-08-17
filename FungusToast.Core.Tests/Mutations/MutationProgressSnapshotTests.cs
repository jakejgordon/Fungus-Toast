using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class MutationProgressSnapshotTests
{
    [Fact]
    public void Snapshot_exposes_cost_projection_levels_and_effect_delta()
    {
        var player = CreatePlayer(mutationPoints: 4);
        var mutation = MutationRegistry.GetById(MutationIds.MycelialBloom)!;
        player.SetMutationLevel(mutation.Id, newLevel: 2, currentRound: 1);

        var snapshot = MutationProgressSnapshot.Create(mutation, player);

        Assert.Equal(2, snapshot.CurrentLevel);
        Assert.Equal(3, snapshot.NextLevel);
        Assert.Equal(mutation.GetTotalEffect(2), snapshot.CurrentTotalEffect, precision: 6);
        Assert.Equal(mutation.GetTotalEffect(3), snapshot.NextTotalEffect!.Value, precision: 6);
        Assert.Equal(mutation.PointsPerUpgrade, snapshot.Cost);
        Assert.Equal(4 - mutation.PointsPerUpgrade, snapshot.ProjectedPointsAfterPurchase);
        Assert.True(snapshot.IsAffordable);
        Assert.False(snapshot.IsMaxed);
    }

    [Fact]
    public void Snapshot_exposes_requirement_progress_and_direct_dependents()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var mutation = MutationRegistry.GetById(MutationIds.RegenerativeHyphae)!;
        player.SetMutationLevel(MutationIds.Necrosporulation, newLevel: 2, currentRound: 1);

        var snapshot = MutationProgressSnapshot.Create(mutation, player);

        Assert.Equal(2, snapshot.Requirements.Count);
        Assert.Contains(snapshot.Requirements, requirement =>
            requirement.MutationId == MutationIds.Necrosporulation && requirement.IsMet);
        Assert.Contains(snapshot.Requirements, requirement =>
            requirement.MutationId == MutationIds.MycotropicInduction && !requirement.IsMet);
        Assert.True(snapshot.HasUnmetPrerequisites);
        Assert.Contains(snapshot.DirectDependents, dependent => dependent.Id == MutationIds.NecrohyphalInfiltration);
    }

    [Fact]
    public void Snapshot_marks_maxed_mutation_without_a_next_level()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var mutation = MutationRegistry.GetById(MutationIds.RegenerativeHyphae)!;
        player.SetMutationLevel(mutation.Id, mutation.MaxLevel, currentRound: 1);

        var snapshot = MutationProgressSnapshot.Create(mutation, player);

        Assert.True(snapshot.IsMaxed);
        Assert.Null(snapshot.NextLevel);
        Assert.Null(snapshot.NextTotalEffect);
    }

    private static Player CreatePlayer(int mutationPoints)
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = mutationPoints
        };
    }
}
