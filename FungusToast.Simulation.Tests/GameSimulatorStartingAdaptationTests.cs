using FungusToast.Core.AI;
using FungusToast.Core.Campaign;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.Models;
using Xunit;

namespace FungusToast.Simulation.Tests;

public class GameSimulatorStartingAdaptationTests
{
    [Fact]
    public void Simulation_grants_slot_matched_mold_adaptations_and_keeps_explicit_additions()
    {
        var result = GameSimulator.RunSimulation(
            strategies: new List<IMutationSpendingStrategy>
            {
                new RandomMutationSpendingStrategy("Slot 0"),
                new RandomMutationSpendingStrategy("Slot 1")
            },
            seed: 24680,
            context: new SimulationTrackingContext(),
            boardWidth: 10,
            boardHeight: 10,
            shuffleStartingSpores: false,
            enableNutrientPatches: false,
            enableMycovariantDraft: false,
            startingAdaptationIds: new List<IReadOnlyList<string>>
            {
                new[] { AdaptationIds.AegisHyphae },
                Array.Empty<string>()
            });

        Assert.Equal(
            new[] { AdaptationIds.AegisHyphae, AdaptationIds.ObliqueFilament }.OrderBy(id => id),
            result.StartingAdaptationIdsByPlayerId[0]);
        Assert.Equal(
            new[] { AdaptationIds.ThanatrophicRebound },
            result.StartingAdaptationIdsByPlayerId[1]);
    }
}
