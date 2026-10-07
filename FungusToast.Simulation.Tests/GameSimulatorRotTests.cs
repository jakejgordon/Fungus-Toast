using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Players;
using FungusToast.Simulation.Experiments;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.Models;
using Xunit;

namespace FungusToast.Simulation.Tests;

public class GameSimulatorRotTests
{
    [Fact]
    public void Introductory_rot_is_opt_in_and_default_matches_explicit_off()
    {
        var defaultResult = Run();
        var offResult = Run(false);
        Assert.False(defaultResult.IntroductoryRotEnabled);
        Assert.Empty(defaultResult.RotTileIds);
        Assert.Equal(0f, defaultResult.RotAdjacentDeathChance);
        Assert.All(defaultResult.PlayerResults, player => Assert.Equal(0, player.DeathsFromRot));
        Assert.Equal(Fingerprint(defaultResult), Fingerprint(offResult));
    }

    [Fact]
    public void Introductory_rot_exports_deterministic_static_patch_and_protects_starts()
    {
        var first = Run(true);
        var second = Run(true);
        Assert.True(first.IntroductoryRotEnabled);
        Assert.NotEmpty(first.RotTileIds);
        Assert.True(first.RotAdjacentDeathChance > 0f);
        Assert.Equal(first.RotTileIds, second.RotTileIds);
        Assert.Equal(Fingerprint(first), Fingerprint(second));
        foreach (var start in first.StartingPositionsByPlayerId.Values)
            Assert.DoesNotContain(start.y * first.BoardWidth + start.x, first.RotTileIds);
        foreach (var player in first.PlayerResults)
        {
            Assert.Equal(first.TrackingContext.GetCellDeathCount(player.PlayerId, DeathReason.Rot), player.DeathsFromRot);
            Assert.Equal(0, first.TrackingContext.GetAttributedKillCount(player.PlayerId, DeathReason.Rot));
        }
    }

    [Fact]
    public void Rot_death_metric_counts_victim_without_enemy_credit()
    {
        var tracking = new SimulationTrackingContext();
        tracking.RecordCellDeath(0, DeathReason.Rot, 3);
        var players = new List<Player> { new(0, "Victim", PlayerTypeEnum.AI), new(1, "Opponent", PlayerTypeEnum.AI) };
        var result = GameResult.From(new GameBoard(10, 10, 2), players, 1, tracking);
        Assert.Equal(3, result.PlayerResults[0].DeathsFromRot);
        Assert.Equal(0, result.PlayerResults[1].DeathsFromRot);
        Assert.Empty(tracking.GetAllAttributedKillsByPlayerAndReason());
    }

    private static string Fingerprint(GameResult result) => ExperimentFingerprint.ForOutcomes(
        new SimulationBatchResult { GameResults = new List<GameResult> { result } });

    private static GameResult Run(bool? enabled = null)
    {
        var strategies = new List<IMutationSpendingStrategy>
        {
            new RandomMutationSpendingStrategy("Slot 0"),
            new RandomMutationSpendingStrategy("Slot 1")
        };
        if (enabled.HasValue)
            return GameSimulator.RunSimulation(strategies, 24680, new SimulationTrackingContext(),
                boardWidth: 20, boardHeight: 20, shuffleStartingSpores: false,
                enableNutrientPatches: false, enableMycovariantDraft: false,
                enableStartingAdaptations: false, enableIntroductoryRot: enabled.Value);
        return GameSimulator.RunSimulation(strategies, 24680, new SimulationTrackingContext(),
            boardWidth: 20, boardHeight: 20, shuffleStartingSpores: false,
            enableNutrientPatches: false, enableMycovariantDraft: false, enableStartingAdaptations: false);
    }
}
