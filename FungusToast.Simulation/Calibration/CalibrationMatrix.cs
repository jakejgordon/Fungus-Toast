using System.Text.Json;
using System.Text.Json.Serialization;
using FungusToast.Core.AI;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// One context every measured strategy is run in.
/// </summary>
public sealed class CalibrationContext
{
    /// <summary>
    /// Derived, not authored: it must equal the taxonomy rollup key plus a discriminator, so an ID
    /// can never claim to be a duel on a small board when it is anything else.
    /// </summary>
    public required string ContextId { get; init; }

    public required int PlayerCount { get; init; }

    public required int BoardWidth { get; init; }

    public required int BoardHeight { get; init; }

    public required int BaseSeed { get; init; }

    public required int GamesPerCondition { get; init; }

    public string GeometryId { get; init; } = "rectangle";

    public IReadOnlyList<int> BlockedTileIds { get; init; } = Array.Empty<int>();

    public bool NutrientPatchesEnabled { get; init; }

    public bool MycovariantDraftEnabled { get; init; }

    public bool StartingAdaptationsEnabled { get; init; }

    /// <summary>Rotating slots by default, so slot advantage averages out within a context rather than confounding it.</summary>
    public SlotAssignmentPolicy SlotAssignmentPolicy { get; init; } = SlotAssignmentPolicy.RotateByGame;

    [JsonIgnore]
    public ContextClasses Classes => ContextTaxonomy.Classify(
        PlayerCount, BoardWidth, BoardHeight, GeometryId, BlockedTileIds, 0, 0);
}

/// <summary>
/// The frozen set of contexts strategy performance is measured in, split into a calibration half
/// used to set thresholds and a holdout half used to confirm them.
///
/// Freezing this before any measurement is the whole point. Choosing contexts after seeing results
/// lets a strategy be declared strong by picking the boards it happens to like, and choosing
/// thresholds afterwards does the same thing one step later. The split exists so a band fitted on
/// the calibration contexts has somewhere independent to be checked.
/// </summary>
public sealed class CalibrationMatrix
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-calibration-matrix.v1";

    /// <summary>Per-condition ceiling, inherited from the staged evidence gates.</summary>
    public const int MaximumGamesPerContext = 100;

    public required string SchemaVersion { get; init; }

    public required string MatrixId { get; init; }

    public required string Purpose { get; init; }

    /// <summary>The roster being measured. Every strategy in the set is measured in every context.</summary>
    public required StrategySetEnum StrategySet { get; init; }

    /// <summary>Explicit lineup, or empty to measure the whole set.</summary>
    public IReadOnlyList<string> StrategyNames { get; init; } = Array.Empty<string>();

    public required IReadOnlyList<CalibrationContext> CalibrationContexts { get; init; }

    public required IReadOnlyList<CalibrationContext> HoldoutContexts { get; init; }

    public required int TotalGameBudget { get; init; }

    public required double RuntimeBudgetSeconds { get; init; }

    [JsonIgnore]
    public IEnumerable<CalibrationContext> AllContexts => CalibrationContexts.Concat(HoldoutContexts);

    /// <summary>Games the whole matrix will consume, before any strategy-level fan-out.</summary>
    public int PlannedGames => AllContexts.Sum(context => context.GamesPerCondition);
}

public static class CalibrationMatrixJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static CalibrationMatrix Deserialize(string json)
    {
        var matrix = JsonSerializer.Deserialize<CalibrationMatrix>(json, SerializerOptions);
        return matrix ?? throw new JsonException("Calibration matrix must contain a JSON object.");
    }

    public static string Serialize(CalibrationMatrix matrix) => JsonSerializer.Serialize(matrix, SerializerOptions);

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
