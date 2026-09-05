using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.Campaign;

namespace FungusToast.Simulation.Experiments;

public static partial class ExperimentManifestValidator
{
    public const int MaximumSupportedPlayers = 8;

    public static IReadOnlyList<string> Validate(ExperimentManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var errors = new List<string>();

        if (!string.Equals(manifest.SchemaVersion, ExperimentManifest.CurrentSchemaVersion, StringComparison.Ordinal))
            errors.Add($"schemaVersion must be '{ExperimentManifest.CurrentSchemaVersion}'.");
        if (string.IsNullOrWhiteSpace(manifest.ExperimentId) || manifest.ExperimentId.Length > 128 || !ExperimentIdPattern().IsMatch(manifest.ExperimentId))
            errors.Add("experimentId must be 1-128 characters using only letters, numbers, '.', '_' or '-'.");
        if (string.IsNullOrWhiteSpace(manifest.Purpose))
            errors.Add("purpose is required.");
        if (manifest.GamesPerCondition < 1 || manifest.GamesPerCondition > ExperimentManifest.MaximumGamesPerCondition)
            errors.Add($"gamesPerCondition must be between 1 and {ExperimentManifest.MaximumGamesPerCondition}.");
        if (manifest.TotalGameBudget < 1)
            errors.Add("totalGameBudget must be positive.");
        if (!double.IsFinite(manifest.RuntimeBudgetSeconds) || manifest.RuntimeBudgetSeconds <= 0)
            errors.Add("runtimeBudgetSeconds must be finite and positive.");
        ValidateAnalysis(manifest.Analysis, manifest.GamesPerCondition, errors);
        if (manifest.Conditions == null || manifest.Conditions.Count == 0)
        {
            errors.Add("conditions must contain at least one condition.");
            return errors;
        }

        long requestedGames = (long)manifest.GamesPerCondition * manifest.Conditions.Count;
        if (requestedGames > manifest.TotalGameBudget)
            errors.Add($"requested condition games ({requestedGames}) exceed totalGameBudget ({manifest.TotalGameBudget}).");

        var conditionIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < manifest.Conditions.Count; index++)
            ValidateCondition(manifest.Conditions[index], index, conditionIds, errors);
        return errors;
    }

    private static void ValidateAnalysis(ExperimentAnalysisPlan? analysis, int gamesPerCondition, ICollection<string> errors)
    {
        if (analysis == null)
        {
            errors.Add("analysis is required.");
            return;
        }
        if (string.IsNullOrWhiteSpace(analysis.AnalysisVersion))
            errors.Add("analysis.analysisVersion is required.");
        switch (analysis.EvidenceStage)
        {
            case ExperimentEvidenceStage.Smoke when gamesPerCondition is < 3 or > 5:
                errors.Add("smoke evidence requires 3-5 games per condition.");
                break;
            case ExperimentEvidenceStage.Calibration when gamesPerCondition != 20:
                errors.Add("calibration evidence requires exactly 20 games per condition.");
                break;
            case ExperimentEvidenceStage.Comparison when gamesPerCondition != 50:
                errors.Add("comparison evidence requires exactly 50 games per condition.");
                break;
            case ExperimentEvidenceStage.Holdout when gamesPerCondition != 100:
                errors.Add("holdout evidence requires exactly 100 games per condition.");
                break;
        }
        var hypothesis = analysis.Hypothesis;
        if (hypothesis == null) return;
        if (analysis.EvidenceStage is not ExperimentEvidenceStage.Comparison and not ExperimentEvidenceStage.Holdout)
            errors.Add("a decision-bearing hypothesis requires comparison or holdout evidence stage.");
        if (string.IsNullOrWhiteSpace(hypothesis.HypothesisId)) errors.Add("analysis.hypothesis.hypothesisId is required.");
        if (string.IsNullOrWhiteSpace(hypothesis.PrimaryContextId)) errors.Add("analysis.hypothesis.primaryContextId is required.");
        if (string.IsNullOrWhiteSpace(hypothesis.TargetStrategyId)) errors.Add("analysis.hypothesis.targetStrategyId is required.");
        if (!double.IsFinite(hypothesis.Margin) || hypothesis.Margin < 0) errors.Add("analysis.hypothesis.margin must be finite and non-negative.");
        if (hypothesis.Estimand != ExperimentEstimand.PairedMeanDifference)
            errors.Add("analysis.hypothesis.estimand must be pairedMeanDifference.");
    }

    public static void ValidateAndThrow(ExperimentManifest manifest)
    {
        var errors = Validate(manifest);
        if (errors.Count > 0) throw new ExperimentManifestValidationException(errors);
    }

    private static void ValidateCondition(ExperimentCondition? condition, int index, ISet<string> conditionIds, ICollection<string> errors)
    {
        var path = $"conditions[{index}]";
        if (condition == null) { errors.Add($"{path} must not be null."); return; }
        if (string.IsNullOrWhiteSpace(condition.ConditionId)) errors.Add($"{path}.conditionId is required.");
        else if (!conditionIds.Add(condition.ConditionId)) errors.Add($"{path}.conditionId '{condition.ConditionId}' is duplicated.");
        if (!string.IsNullOrEmpty(condition.PairingGroupId)
            && (condition.PairingGroupId.Length > 128 || !ExperimentIdPattern().IsMatch(condition.PairingGroupId)))
            errors.Add($"{path}.pairingGroupId must use only letters, numbers, '.', '_' or '-' and be at most 128 characters.");
        if (condition.PlayerCount < 1 || condition.PlayerCount > MaximumSupportedPlayers)
            errors.Add($"{path}.playerCount must be between 1 and {MaximumSupportedPlayers}.");
        if (condition.Board == null) { errors.Add($"{path}.board is required."); return; }
        if (condition.Board.Width < 1 || condition.Board.Height < 1) { errors.Add($"{path}.board width and height must both be positive."); return; }
        if (string.IsNullOrWhiteSpace(condition.Board.GeometryId)) errors.Add($"{path}.board.geometryId is required.");

        var tileCount = (long)condition.Board.Width * condition.Board.Height;
        var blocked = condition.Board.BlockedTileIds ?? Array.Empty<int>();
        ValidateUniqueValues(blocked, $"{path}.board.blockedTileIds", errors);
        if (blocked.Any(tileId => tileId < 0 || tileId >= tileCount)) errors.Add($"{path}.board.blockedTileIds contains a tile outside the board.");
        if (tileCount - blocked.Distinct().Count() < condition.PlayerCount) errors.Add($"{path}.board does not have enough playable tiles for {condition.PlayerCount} players.");

        ValidateStrategies(condition, path, errors);
        ValidateSystems(condition, path, errors);
        ValidatePositioning(condition, blocked, path, errors);
    }

    private static void ValidateStrategies(ExperimentCondition condition, string path, ICollection<string> errors)
    {
        if (condition.Strategies == null) { errors.Add($"{path}.strategies is required."); return; }
        var names = condition.Strategies.ExplicitStrategyNames ?? Array.Empty<string>();
        if (names.Count > 0 && names.Count != condition.PlayerCount) errors.Add($"{path}.strategies.explicitStrategyNames count must match playerCount.");
        if (names.Any(string.IsNullOrWhiteSpace)) errors.Add($"{path}.strategies.explicitStrategyNames must not contain blank names.");
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count) errors.Add($"{path}.strategies.explicitStrategyNames must be unique.");

        if (names.Count > 0)
        {
            AIRoster.GetStrategiesByName(condition.Strategies.StrategySet, names, out var missingNames);
            if (missingNames.Count > 0)
                errors.Add($"{path}.strategies.explicitStrategyNames contains unknown names for {condition.Strategies.StrategySet}: {string.Join(", ", missingNames)}.");
            return;
        }

        var filter = condition.Strategies.Filter;
        if (filter == null)
        {
            errors.Add($"{path}.strategies.filter is required.");
            return;
        }

        if (filter.IsEmpty)
        {
            var unfilteredStrategies = AIRoster.GetStrategiesByFilter(
                condition.Strategies.StrategySet,
                new StrategyCatalogFilter());
            if (unfilteredStrategies.Count < condition.PlayerCount)
            {
                errors.Add(
                    $"{path}.strategies provides {unfilteredStrategies.Count} registered strategies for " +
                    $"{condition.PlayerCount} players; analytical experiments cannot synthesize fallback strategies.");
            }

            return;
        }

        var matchingStrategies = AIRoster.GetStrategiesByFilter(condition.Strategies.StrategySet, new StrategyCatalogFilter
        {
            Archetypes = filter.Archetypes ?? Array.Empty<StrategyArchetype>(),
            PowerTiers = filter.PowerTiers ?? Array.Empty<StrategyPowerTier>(),
            Roles = filter.Roles ?? Array.Empty<StrategyRole>(),
            Lifecycles = filter.Lifecycles ?? Array.Empty<StrategyLifecycle>(),
            DifficultyBands = filter.DifficultyBands ?? Array.Empty<DifficultyBand>(),
            CampaignDifficulties = filter.CampaignDifficulties ?? Array.Empty<CampaignDifficulty>(),
            Pools = filter.Pools ?? Array.Empty<StrategyPool>()
        });
        if (matchingStrategies.Count < condition.PlayerCount)
            errors.Add($"{path}.strategies filter provides {matchingStrategies.Count} strategies for {condition.PlayerCount} players.");
    }

    private static void ValidateSystems(ExperimentCondition condition, string path, ICollection<string> errors)
    {
        if (condition.Systems == null) { errors.Add($"{path}.systems is required."); return; }
        var seenSlots = new HashSet<int>();
        foreach (var loadout in condition.Systems.StartingAdaptations ?? Array.Empty<PlayerStartingAdaptations>())
        {
            if (loadout == null) { errors.Add($"{path}.systems.startingAdaptations must not contain null entries."); continue; }
            if (loadout.PlayerSlot < 0 || loadout.PlayerSlot >= condition.PlayerCount) errors.Add($"{path}.systems.startingAdaptations contains out-of-range playerSlot {loadout.PlayerSlot}.");
            else if (!seenSlots.Add(loadout.PlayerSlot)) errors.Add($"{path}.systems.startingAdaptations repeats playerSlot {loadout.PlayerSlot}.");
            var ids = loadout.AdaptationIds ?? Array.Empty<string>();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
                errors.Add($"{path}.systems.startingAdaptations[{loadout.PlayerSlot}] must contain unique, non-blank IDs.");
            var unknownIds = ids.Where(id => !string.IsNullOrWhiteSpace(id) && !AdaptationRepository.TryGetById(id, out _)).ToList();
            if (unknownIds.Count > 0)
                errors.Add($"{path}.systems.startingAdaptations[{loadout.PlayerSlot}] contains unknown IDs: {string.Join(", ", unknownIds)}.");
        }
    }

    private static void ValidatePositioning(ExperimentCondition condition, IReadOnlyCollection<int> blocked, string path, ICollection<string> errors)
    {
        if (condition.Positioning == null) { errors.Add($"{path}.positioning is required."); return; }
        var exact = condition.Positioning.ExactStartingPositions ?? Array.Empty<BoardCoordinate>();
        var pools = condition.Positioning.PreferredPositionPools ?? Array.Empty<PlayerStartingPositionPool>();
        var edgeOffsetOverrides = condition.Positioning.StrategyEdgeOffsetOverrides ?? Array.Empty<StrategyStartingSporeEdgeOffsetOverride>();
        if (exact.Count > 0 && pools.Count > 0) errors.Add($"{path}.positioning cannot specify both exactStartingPositions and preferredPositionPools.");
        if (exact.Count > 0 && exact.Count != condition.PlayerCount) errors.Add($"{path}.positioning.exactStartingPositions count must match playerCount.");
        ValidateCoordinates(exact, condition.Board, blocked, $"{path}.positioning.exactStartingPositions", errors);
        ValidateUniqueCoordinates(exact, $"{path}.positioning.exactStartingPositions", errors);

        var seenSlots = new HashSet<int>();
        foreach (var pool in pools)
        {
            if (pool == null) { errors.Add($"{path}.positioning.preferredPositionPools must not contain null entries."); continue; }
            if (pool.PlayerSlot < 0 || pool.PlayerSlot >= condition.PlayerCount) errors.Add($"{path}.positioning.preferredPositionPools contains out-of-range playerSlot {pool.PlayerSlot}.");
            else if (!seenSlots.Add(pool.PlayerSlot)) errors.Add($"{path}.positioning.preferredPositionPools repeats playerSlot {pool.PlayerSlot}.");
            var positions = pool.Positions ?? Array.Empty<BoardCoordinate>();
            if (positions.Count == 0) errors.Add($"{path}.positioning.preferredPositionPools[{pool.PlayerSlot}] must contain at least one position.");
            ValidateCoordinates(positions, condition.Board, blocked, $"{path}.positioning.preferredPositionPools[{pool.PlayerSlot}]", errors);
            ValidateUniqueCoordinates(positions, $"{path}.positioning.preferredPositionPools[{pool.PlayerSlot}]", errors);
        }

        var seenStrategyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edgeOffsetOverride in edgeOffsetOverrides)
        {
            if (edgeOffsetOverride == null)
            {
                errors.Add($"{path}.positioning.strategyEdgeOffsetOverrides must not contain null entries.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(edgeOffsetOverride.StrategyName))
            {
                errors.Add($"{path}.positioning.strategyEdgeOffsetOverrides must contain non-blank strategy names.");
            }
            else if (!seenStrategyNames.Add(edgeOffsetOverride.StrategyName))
            {
                errors.Add($"{path}.positioning.strategyEdgeOffsetOverrides repeats strategy '{edgeOffsetOverride.StrategyName}'.");
            }
            else if (StrategyRegistry.GetDefinition(condition.Strategies.StrategySet, edgeOffsetOverride.StrategyName) == null)
            {
                errors.Add($"{path}.positioning.strategyEdgeOffsetOverrides contains unknown strategy '{edgeOffsetOverride.StrategyName}'.");
            }
        }
    }

    private static void ValidateCoordinates(IEnumerable<BoardCoordinate?> coordinates, ExperimentBoard board, IReadOnlyCollection<int> blocked, string path, ICollection<string> errors)
    {
        var blockedSet = blocked.ToHashSet();
        foreach (var coordinate in coordinates)
        {
            if (coordinate == null) { errors.Add($"{path} must not contain null entries."); continue; }
            if (coordinate.X < 0 || coordinate.X >= board.Width || coordinate.Y < 0 || coordinate.Y >= board.Height)
            { errors.Add($"{path} contains out-of-bounds coordinate ({coordinate.X},{coordinate.Y})."); continue; }
            if (blockedSet.Contains(coordinate.Y * board.Width + coordinate.X)) errors.Add($"{path} contains blocked coordinate ({coordinate.X},{coordinate.Y}).");
        }
    }

    private static void ValidateUniqueCoordinates(IEnumerable<BoardCoordinate?> coordinates, string path, ICollection<string> errors)
    {
        var keys = coordinates.Where(value => value != null).Select(value => (value!.X, value.Y)).ToList();
        if (keys.Distinct().Count() != keys.Count) errors.Add($"{path} must not contain duplicate coordinates.");
    }

    private static void ValidateUniqueValues<T>(IEnumerable<T> values, string path, ICollection<string> errors)
    {
        var list = values.ToList();
        if (list.Distinct().Count() != list.Count) errors.Add($"{path} must not contain duplicates.");
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ExperimentIdPattern();
}

public sealed class ExperimentManifestValidationException : Exception
{
    public ExperimentManifestValidationException(IReadOnlyList<string> errors)
        : base($"Experiment manifest is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}") => Errors = errors;

    public IReadOnlyList<string> Errors { get; }
}
