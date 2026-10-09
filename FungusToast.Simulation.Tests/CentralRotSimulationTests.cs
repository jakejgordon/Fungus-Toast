using Xunit;
using System.Text.Json;
using FungusToast.Core.AI;
using FungusToast.Simulation.Experiments;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Tests;
public class CentralRotSimulationTests
{
    [Fact]
    public void Central_flag_is_optional_serialized_and_roundtrips()
    {
        var old = new ExperimentSystems { NutrientPatchesEnabled=false,MycovariantDraftEnabled=true };
        Assert.DoesNotContain(nameof(ExperimentSystems.CentralRotEnabled),JsonSerializer.Serialize(old));
        var enabled = new ExperimentSystems { NutrientPatchesEnabled=false,MycovariantDraftEnabled=true,CentralRotEnabled=true };
        Assert.True(JsonSerializer.Deserialize<ExperimentSystems>(JsonSerializer.Serialize(enabled))!.CentralRotEnabled);
    }
    [Fact]
    public void Central_rot_rejects_other_layouts_and_wrong_dimensions()
    {
        var strategies = new List<IMutationSpendingStrategy> { AIRoster.TestingStrategies.First() };
        Assert.Throws<ArgumentException>(() => GameSimulator.RunSimulation(strategies,1,new SimulationTrackingContext(),enableCentralRot:true,enableIntroductoryRot:true));
        Assert.Throws<ArgumentException>(() => GameSimulator.RunSimulation(strategies,1,new SimulationTrackingContext(),boardWidth:40,boardHeight:40,enableCentralRot:true));
    }
}
