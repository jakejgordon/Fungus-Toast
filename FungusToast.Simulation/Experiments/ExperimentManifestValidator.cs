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
        if (manifest.Conditions == null || manifest.Conditions.Count == 0)
        {
            errors.Add("conditions must contain at least one condition.");
            return errors;
        }

        var conditionIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < manifest.Conditions.Count; index++)
            ValidateCondition(manifest.Conditions[index], index, conditionIds, errors);
        return errors;
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
