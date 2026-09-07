using FungusToast.Core.AI;
using FungusToast.Core.Common;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// Draws each game's lineup from a panel larger than the table.
///
/// The runner otherwise resolves one lineup per run and rotates only slots within it, which is
/// correct when the panel is the table - eight strategies in an eight-player game - and silently
/// wrong when it is not. A nineteen-strategy panel in two-player games would measure one pair
/// three hundred times and report it as a calibration of the field.
///
/// Selection is a pure function of the game's own seed, so a replay reproduces the same lineups,
/// and it draws from a purpose-scoped stream rather than the gameplay stream, so how many draws it
/// takes cannot perturb the game itself. That is the same guarantee the P3.R3 random-stream
/// contract gives every other AI decision.
/// </summary>
public static class PerGameLineupSelector
{
    /// <summary>Names this selector's stream so its draws can never collide with another purpose.</summary>
    public const string DecisionKind = "lineup-selection";

    /// <summary>
    /// Builds a selector over <paramref name="panel"/>, or returns null when the panel is not
    /// larger than the table and the historical fixed-lineup behaviour is already correct.
    /// </summary>
    public static Func<int, int, List<IMutationSpendingStrategy>>? Create(
        IReadOnlyList<IMutationSpendingStrategy> panel,
        int playerCount)
    {
        ArgumentNullException.ThrowIfNull(panel);
        if (playerCount < 1) throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount, "Player count must be positive.");
        if (panel.Count < playerCount)
            throw new ArgumentException($"A panel of {panel.Count} cannot fill {playerCount} seats.", nameof(panel));
        if (panel.Count == playerCount) return null;

        return (_, gameSeed) => SelectLineup(panel, playerCount, gameSeed);
    }

    /// <summary>
    /// The lineup for one game. Ordering the panel by name first makes the draw independent of how
    /// the registry happened to enumerate it, so the same seed gives the same lineup across runs.
    /// </summary>
    public static List<IMutationSpendingStrategy> SelectLineup(
        IReadOnlyList<IMutationSpendingStrategy> panel,
        int playerCount,
        int gameSeed)
    {
        ArgumentNullException.ThrowIfNull(panel);
        var random = new RandomStreamContract(gameSeed).CreateAiDecisionRandom(
            playerId: -1,
            round: 0,
            decisionKind: DecisionKind);

        return panel
            .OrderBy(strategy => strategy.StrategyName, StringComparer.Ordinal)
            .OrderBy(_ => random.Next())
            .Take(playerCount)
            .ToList();
    }
}
