using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// Validates a calibration matrix before anything is measured.
///
/// Three rules carry the weight. Context IDs must match their own derived classes, so a matrix
/// cannot claim coverage it does not have. Holdout contexts must differ from every calibration
/// context in both board and seed, or the "independent" check is re-measuring the boards the
/// thresholds were fitted on. And the matrix must actually vary player count and board scale,
/// because a single-context matrix produces a global label with no evidence that it generalizes -
/// which is exactly the thing this initiative set out to stop.
/// </summary>
public static partial class CalibrationMatrixValidator
{
    public const int MaximumMatrixIdLength = 48;

    public static IReadOnlyList<string> Validate(CalibrationMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        var errors = new List<string>();

        if (!string.Equals(matrix.SchemaVersion, CalibrationMatrix.CurrentSchemaVersion, StringComparison.Ordinal))
            errors.Add($"schemaVersion must be '{CalibrationMatrix.CurrentSchemaVersion}'.");

        if (string.IsNullOrWhiteSpace(matrix.MatrixId)
            || matrix.MatrixId.Length > MaximumMatrixIdLength
            || !MatrixIdPattern().IsMatch(matrix.MatrixId))
        {
            errors.Add($"matrixId must be 1-{MaximumMatrixIdLength} characters using only letters, numbers, '.', '_' or '-'.");
        }

        if (string.IsNullOrWhiteSpace(matrix.Purpose)) errors.Add("purpose is required.");
        if (!Enum.IsDefined(typeof(StrategySetEnum), matrix.StrategySet))
            errors.Add($"strategySet '{matrix.StrategySet}' is not a defined strategy set.");

        if (matrix.MinimumGamesPerStrategy < 1)
            errors.Add("minimumGamesPerStrategy must be positive.");

        ValidateStrategies(matrix, errors);
        ValidateSelectionPolicy(matrix, errors);
        ValidateContexts("calibrationContexts", matrix.CalibrationContexts, errors);
        ValidateContexts("holdoutContexts", matrix.HoldoutContexts, errors);
        ValidateUniqueIds(matrix, errors);
        ValidateHoldoutIndependence(matrix, errors);
        ValidateCoverage(matrix, errors);
        ValidateSamplingAdequacy(matrix, errors);
        ValidateSeedIndependence(matrix, errors);
        ValidateBudgets(matrix, errors);
        return errors;
    }

    private static void ValidateStrategies(CalibrationMatrix matrix, ICollection<string> errors)
    {
        var names = matrix.StrategyNames ?? Array.Empty<string>();
        var registered = StrategyRegistry.GetDefinitions(matrix.StrategySet);
        if (registered.Count == 0)
        {
            errors.Add($"strategySet '{matrix.StrategySet}' has no registered strategies.");
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) { errors.Add("strategyNames must not contain blank entries."); continue; }
            if (!seen.Add(name)) errors.Add($"strategyNames repeats '{name}'.");
            if (registered.All(definition => !string.Equals(definition.Strategy.StrategyName, name, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"strategyNames contains '{name}', which is not registered in '{matrix.StrategySet}'.");
        }
    }

    /// <summary>
    /// A panel larger than a lineup must be sampled randomly. StratifiedCycle takes a sliding
    /// window over a fixed ordering, so each strategy faces only its neighbours and its rating
    /// measures those matchups rather than general strength; CoverageBalanced picks one strategy
    /// per theme and over-samples rare ones. When the panel is exactly a lineup there is nothing to
    /// choose and any policy is equivalent.
    /// </summary>
    private static void ValidateSelectionPolicy(CalibrationMatrix matrix, ICollection<string> errors)
    {
        if (matrix.SelectionPolicy == StrategySelectionPolicy.RandomUnique) return;

        var panelSize = matrix.ResolvePanelSize();
        var forcedLineups = (matrix.CalibrationContexts ?? Array.Empty<CalibrationContext>())
            .Concat(matrix.HoldoutContexts ?? Array.Empty<CalibrationContext>())
            .All(context => context != null && context.PlayerCount >= panelSize);
        if (forcedLineups) return;

        errors.Add(
            $"selectionPolicy '{matrix.SelectionPolicy}' cannot measure general strength for a panel of {panelSize}: "
            + "it fixes which strategies meet, so the result describes those matchups. Use RandomUnique.");
    }

    /// <summary>
    /// Sizes each context against the declared minimum, so an under-powered matrix fails before it
    /// runs rather than producing intervals too wide to band.
    /// </summary>
    private static void ValidateSamplingAdequacy(CalibrationMatrix matrix, ICollection<string> errors)
    {
        if (matrix.MinimumGamesPerStrategy < 1) return;
        var panelSize = matrix.ResolvePanelSize();
        if (panelSize <= 0) return;

        foreach (var context in matrix.AllContexts)
        {
            if (context == null || context.PlayerCount < 2 || context.GamesPerCondition < 1 || context.Repeats < 1) continue;

            var expected = context.ExpectedGamesPerStrategy(panelSize);
            if (expected >= matrix.MinimumGamesPerStrategy) continue;

            var neededGames = (int)Math.Ceiling((double)matrix.MinimumGamesPerStrategy * panelSize / context.PlayerCount);
            errors.Add(
                $"context '{context.ContextId}' gives each of {panelSize} strategies about {expected:0.#} games, "
                + $"below the declared minimum of {matrix.MinimumGamesPerStrategy}. It needs about {neededGames} games "
                + $"({(int)Math.Ceiling((double)neededGames / context.GamesPerCondition)} repeats of {context.GamesPerCondition}).");
        }
    }

    /// <summary>
    /// Per-game seeds run from a condition's base, so two contexts whose seed spans overlap would
    /// replay the same games and stop being independent evidence.
    /// </summary>
    private static void ValidateSeedIndependence(CalibrationMatrix matrix, ICollection<string> errors)
    {
        var footprints = matrix.AllContexts
            .Where(context => context != null && context.GamesPerCondition >= 1 && context.Repeats >= 1)
            .Select(context => (context.ContextId, Span: context.SeedFootprint))
            .OrderBy(entry => entry.Span.First)
            .ToList();

        for (var index = 1; index < footprints.Count; index++)
        {
            var previous = footprints[index - 1];
            var current = footprints[index];
            if (current.Span.First <= previous.Span.Last)
            {
                errors.Add(
                    $"contexts '{previous.ContextId}' and '{current.ContextId}' consume overlapping seed ranges "
                    + $"([{previous.Span.First}, {previous.Span.Last}] and [{current.Span.First}, {current.Span.Last}]); "
                    + "they would replay the same games.");
            }
        }
    }

    private static void ValidateContexts(string field, IReadOnlyList<CalibrationContext>? contexts, ICollection<string> errors)
    {
        if (contexts == null || contexts.Count == 0)
        {
            errors.Add($"{field} must contain at least one context.");
            return;
        }

        for (var index = 0; index < contexts.Count; index++)
        {
            var context = contexts[index];
            var path = $"{field}[{index}]";
            if (context == null) { errors.Add($"{path} must not be null."); continue; }

            if (context.PlayerCount is < 2 or > 8)
            {
                errors.Add($"{path}.playerCount must be between 2 and 8.");
                continue;
            }

            if (context.BoardWidth < 1 || context.BoardHeight < 1)
            {
                errors.Add($"{path} board dimensions must be positive.");
                continue;
            }

            if (context.GamesPerCondition < 1 || context.GamesPerCondition > CalibrationMatrix.MaximumGamesPerContext)
                errors.Add($"{path}.gamesPerCondition must be between 1 and {CalibrationMatrix.MaximumGamesPerContext}.");
            if (context.Repeats < 1)
                errors.Add($"{path}.repeats must be at least 1.");

            var tileCount = (long)context.BoardWidth * context.BoardHeight;
            var blocked = context.BlockedTileIds ?? Array.Empty<int>();
            if (blocked.Any(tileId => tileId < 0 || tileId >= tileCount))
                errors.Add($"{path}.blockedTileIds contains a tile outside the board.");
            if (tileCount - blocked.Distinct().Count() < context.PlayerCount)
                errors.Add($"{path} does not have enough playable tiles for {context.PlayerCount} players.");

            // A context ID that does not match its own classes would let a matrix claim coverage it
            // does not have, which is the one thing a frozen matrix must not be able to do.
            var expectedPrefix = ContextTaxonomy.BuildRollupKey(context.Classes);
            if (string.IsNullOrWhiteSpace(context.ContextId)
                || !context.ContextId.StartsWith(expectedPrefix, StringComparison.Ordinal))
            {
                errors.Add($"{path}.contextId '{context.ContextId}' must start with its derived rollup key '{expectedPrefix}'.");
            }
        }
    }

    private static void ValidateUniqueIds(CalibrationMatrix matrix, ICollection<string> errors)
    {
        var duplicates = matrix.AllContexts
            .Select(context => context.ContextId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        foreach (var duplicate in duplicates)
            errors.Add($"contextId '{duplicate}' appears more than once; every context needs its own identity.");
    }

    /// <summary>
    /// A holdout that reuses a calibration board or seed is not independent evidence; it
    /// re-measures what the thresholds were fitted on and reports it as confirmation.
    /// </summary>
    private static void ValidateHoldoutIndependence(CalibrationMatrix matrix, ICollection<string> errors)
    {
        var calibration = matrix.CalibrationContexts ?? Array.Empty<CalibrationContext>();
        var holdout = matrix.HoldoutContexts ?? Array.Empty<CalibrationContext>();

        foreach (var context in holdout)
        {
            if (context == null) continue;

            if (calibration.Any(other => other != null
                    && other.BoardWidth == context.BoardWidth
                    && other.BoardHeight == context.BoardHeight
                    && other.PlayerCount == context.PlayerCount))
            {
                errors.Add(
                    $"holdout context '{context.ContextId}' reuses a calibration board and player count; "
                    + "a holdout must be an unseen context.");
            }

            if (calibration.Any(other => other != null && other.BaseSeed == context.BaseSeed))
                errors.Add($"holdout context '{context.ContextId}' reuses a calibration base seed.");
        }
    }

    /// <summary>
    /// The taxonomy says to begin with player count and board scale, so a matrix that varies
    /// neither cannot support a contextual claim at all.
    /// </summary>
    private static void ValidateCoverage(CalibrationMatrix matrix, ICollection<string> errors)
    {
        var contexts = (matrix.CalibrationContexts ?? Array.Empty<CalibrationContext>())
            .Where(context => context != null && context.PlayerCount is >= 2 and <= 8
                && context.BoardWidth > 0 && context.BoardHeight > 0)
            .ToList();
        if (contexts.Count == 0) return;

        if (contexts.Select(context => context.Classes.PlayerCount).Distinct().Count() < 2)
            errors.Add("calibrationContexts must cover at least two player-count classes.");
        if (contexts.Select(context => context.Classes.BoardScale).Distinct().Count() < 2)
            errors.Add("calibrationContexts must cover at least two board-scale classes.");
    }

    private static void ValidateBudgets(CalibrationMatrix matrix, ICollection<string> errors)
    {
        if (matrix.TotalGameBudget < 1) errors.Add("totalGameBudget must be positive.");
        if (!double.IsFinite(matrix.RuntimeBudgetSeconds) || matrix.RuntimeBudgetSeconds <= 0)
            errors.Add("runtimeBudgetSeconds must be finite and positive.");

        var planned = matrix.AllContexts.Sum(context => context?.TotalGames ?? 0);
        if (matrix.TotalGameBudget >= 1 && planned > matrix.TotalGameBudget)
            errors.Add($"planned games ({planned}) exceed totalGameBudget ({matrix.TotalGameBudget}).");
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex MatrixIdPattern();
}
