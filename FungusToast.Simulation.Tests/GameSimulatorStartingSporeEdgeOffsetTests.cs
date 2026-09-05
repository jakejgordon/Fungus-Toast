using FungusToast.Core.AI;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.Models;
using Xunit;

namespace FungusToast.Simulation.Tests;

public class GameSimulatorStartingSporeEdgeOffsetTests
{
    [Fact]
    public void Strategy_edge_offset_override_replaces_authored_offset()
    {
        var authoredOffsetStrategies = new List<IMutationSpendingStrategy>
        {
            new ParameterizedSpendingStrategy("Offset target", prioritizeHighTier: false, startingSporeEdgeOffset: 6),
            new ParameterizedSpendingStrategy("Anchor", prioritizeHighTier: false)
        };
        var zeroOffsetStrategies = new List<IMutationSpendingStrategy>
        {
            new ParameterizedSpendingStrategy("Offset target", prioritizeHighTier: false),
            new ParameterizedSpendingStrategy("Anchor", prioritizeHighTier: false)
        };

        var control = Run(authoredOffsetStrategies);
        var treatment = Run(
            authoredOffsetStrategies,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Offset target"] = 0 });
        var zeroOffsetReference = Run(zeroOffsetStrategies);

        Assert.NotEqual(control.StartingPositionsByPlayerId[0], treatment.StartingPositionsByPlayerId[0]);
        Assert.Equal(zeroOffsetReference.StartingPositionsByPlayerId, treatment.StartingPositionsByPlayerId);
    }

    private static GameResult Run(
        List<IMutationSpendingStrategy> strategies,
        IReadOnlyDictionary<string, int>? overrides = null) =>
        GameSimulator.RunSimulation(
            strategies,
            seed: 13579,
            context: new SimulationTrackingContext(),
            boardWidth: 40,
            boardHeight: 40,
            shuffleStartingSpores: false,
            enableNutrientPatches: false,
            enableMycovariantDraft: false,
            enableStartingAdaptations: false,
            strategyStartingSporeEdgeOffsetOverrides: overrides);
}
