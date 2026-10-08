using System.Text.Json;
using Xunit;
using FungusToast.Core.AI;
using FungusToast.Simulation.Experiments;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Tests;

public class QuarantineSimulationTests
{
    private static List<IMutationSpendingStrategy> Strategies() => new() { AIRoster.TestingStrategies.First() };

    [Fact]
    public void Quarantine_and_introductory_layouts_cannot_be_combined()
    {
        Assert.Throws<ArgumentException>(() => GameSimulator.RunSimulation(Strategies(),1,new SimulationTrackingContext(),
            boardWidth:120,boardHeight:120,enableIntroductoryRot:true,enableQuarantineRot:true));
    }

    [Fact]
    public void Quarantine_rejects_uncurated_rectangle_and_custom_start_controls()
    {
        Assert.Throws<ArgumentException>(() => GameSimulator.RunSimulation(Strategies(),1,new SimulationTrackingContext(),
            boardWidth:120,boardHeight:120,enableQuarantineRot:true));
        Assert.Throws<ArgumentException>(() => GameSimulator.RunSimulation(Strategies(),1,new SimulationTrackingContext(),
            boardWidth:120,boardHeight:120,enableQuarantineRot:true,startingPositionOverride:new[] {(10,10)}));
    }

    [Fact]
    public void Empty_replay_override_collections_do_not_count_as_custom_position_controls()
    {
        var error = Assert.Throws<ArgumentException>(() => GameSimulator.RunSimulation(Strategies(),1,new SimulationTrackingContext(),
            boardWidth:120,boardHeight:120,enableQuarantineRot:true,
            strategyStartingSporeEdgeOffsetOverrides:new Dictionary<string,int>()));
        Assert.Contains("pocket",error.Message.ToLowerInvariant()); // rejected by the uncurated mask, not empty controls
    }

    [Fact]
    public void Default_false_optional_flag_preserves_old_serialized_systems_shape()
    {
        var systems = new ExperimentSystems { NutrientPatchesEnabled=true,MycovariantDraftEnabled=true };
        string json = JsonSerializer.Serialize(systems);
        Assert.DoesNotContain(nameof(ExperimentSystems.QuarantineRotEnabled),json);
        Assert.False(JsonSerializer.Deserialize<ExperimentSystems>(json)!.QuarantineRotEnabled);
        string enabled = JsonSerializer.Serialize(new ExperimentSystems
        { NutrientPatchesEnabled=false,MycovariantDraftEnabled=true,QuarantineRotEnabled=true });
        Assert.True(JsonSerializer.Deserialize<ExperimentSystems>(enabled)!.QuarantineRotEnabled);
    }
}
