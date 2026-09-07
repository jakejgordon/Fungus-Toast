using FungusToast.Core.AI;
using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class PerGameLineupSelectorTests
{
    /// <summary>
    /// The defect this exists to fix. The runner otherwise resolves one lineup per run, so a
    /// nineteen-strategy panel in two-player games measured the same pair every game and reported
    /// it as a calibration of the field.
    /// </summary>
    [Fact]
    public void OverManyGames_ThePanelIsMeasuredRatherThanOnePairing()
    {
        var panel = LoadPanel();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var game = 0; game < 200; game++)
        foreach (var strategy in PerGameLineupSelector.SelectLineup(panel, playerCount: 2, gameSeed: 5000 + game))
            seen.Add(strategy.StrategyName);

        Assert.Equal(panel.Count, seen.Count);
    }

    /// <summary>
    /// Selection must be a pure function of the game's seed, or a replay would not reproduce the
    /// lineups it is replaying.
    /// </summary>
    [Fact]
    public void TheSameSeed_AlwaysDrawsTheSameLineup()
    {
        var panel = LoadPanel();

        var first = PerGameLineupSelector.SelectLineup(panel, playerCount: 4, gameSeed: 4242);
        var second = PerGameLineupSelector.SelectLineup(panel, playerCount: 4, gameSeed: 4242);

        Assert.Equal(
            first.Select(strategy => strategy.StrategyName),
            second.Select(strategy => strategy.StrategyName));
    }

    [Fact]
    public void DifferentSeeds_DrawDifferentLineups()
    {
        var panel = LoadPanel();

        var lineups = Enumerable.Range(0, 20)
            .Select(offset => string.Join("|", PerGameLineupSelector
                .SelectLineup(panel, playerCount: 2, gameSeed: 9000 + offset)
                .Select(strategy => strategy.StrategyName)))
            .Distinct(StringComparer.Ordinal)
            .Count();

        Assert.True(lineups > 1, "Every seed produced the same lineup, which is the defect this fixes.");
    }

    [Fact]
    public void ALineup_HasExactlyThePlayerCountAndNoRepeats()
    {
        var panel = LoadPanel();

        var lineup = PerGameLineupSelector.SelectLineup(panel, playerCount: 4, gameSeed: 77);

        Assert.Equal(4, lineup.Count);
        Assert.Equal(4, lineup.Select(strategy => strategy.StrategyName).Distinct(StringComparer.Ordinal).Count());
        Assert.All(lineup, strategy => Assert.Contains(panel, candidate => ReferenceEquals(candidate, strategy)));
    }

    /// <summary>
    /// When the panel is the table there is nothing to draw, and the historical fixed-lineup
    /// behaviour is already correct - so no selector is created and nothing changes.
    /// </summary>
    [Fact]
    public void APanelTheSizeOfTheTable_GetsNoSelector()
    {
        var panel = LoadPanel();

        Assert.Null(PerGameLineupSelector.Create(panel, panel.Count));
        Assert.NotNull(PerGameLineupSelector.Create(panel, 2));
    }

    [Fact]
    public void APanelTooSmallForTheTable_Throws()
    {
        var panel = LoadPanel().Take(2).ToList();

        Assert.Throws<ArgumentException>(() => PerGameLineupSelector.Create(panel, 4));
    }

    /// <summary>
    /// Drawing from a purpose-scoped stream means the number of draws cannot perturb gameplay,
    /// which is the guarantee the random-stream contract gives every other AI decision.
    /// </summary>
    [Fact]
    public void LineupSelection_UsesItsOwnNamedStream()
    {
        Assert.Equal("lineup-selection", PerGameLineupSelector.DecisionKind);
    }

    private static IReadOnlyList<IMutationSpendingStrategy> LoadPanel()
    {
        _ = AIRoster.TestingStrategies.Count;
        return StrategyRegistry.GetStrategies(StrategySetEnum.Proven);
    }
}
