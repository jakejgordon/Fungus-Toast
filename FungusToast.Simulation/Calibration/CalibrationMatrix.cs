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

    /// <summary>
    /// How many conditions run at this context. The per-condition ceiling is 100 games, but a
    /// panel needs more seats than one condition provides before each strategy has enough games,
    /// so a context is sampled by several conditions at consecutive seed blocks.
    /// </summary>
    public int Repeats { get; init; } = 1;

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

    [JsonIgnore]
    public int TotalGames => Repeats * GamesPerCondition;

    /// <summary>Seats this context offers, which the panel shares between its strategies.</summary>
    [JsonIgnore]
    public int TotalSeats => TotalGames * PlayerCount;

    /// <summary>
    /// Per-game seeds run from the condition's base, so each repeat starts a block past the last.
    /// </summary>
    public int SeedForRepeat(int repeatIndex)
    {
        if (repeatIndex < 0 || repeatIndex >= Repeats)
            throw new ArgumentOutOfRangeException(nameof(repeatIndex), repeatIndex, $"This context has {Repeats} repeats.");
        return BaseSeed + repeatIndex * GamesPerCondition;
    }

    /// <summary>The whole span of seeds this context consumes, used to keep contexts independent.</summary>
    [JsonIgnore]
    public (int First, int Last) SeedFootprint => (BaseSeed, BaseSeed + TotalGames - 1);

    /// <summary>Expected games per strategy when <paramref name="panelSize"/> strategies share the seats.</summary>
    public double ExpectedGamesPerStrategy(int panelSize)
        => panelSize <= 0 ? 0 : (double)TotalSeats / panelSize;
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

    /// <summary>The reference panel, or empty to measure the whole set.</summary>
    public IReadOnlyList<string> StrategyNames { get; init; } = Array.Empty<string>();

    /// <summary>
    /// How each game's lineup is drawn from the panel.
    ///
    /// Random selection is the honest default for a panel larger than a lineup. StratifiedCycle
    /// gives perfectly even exposure, but it takes a sliding window over a fixed ordering, so in
    /// two-player games each strategy would only ever face its two neighbours - that measures
    /// particular matchups and reports them as general strength. CoverageBalanced picks one
    /// strategy per theme, which over-samples rare themes for the same reason. Random opposition
    /// trades exact exposure counts for an unbiased cross-section of the field, which is what a
    /// yardstick needs.
    /// </summary>
    public StrategySelectionPolicy SelectionPolicy { get; init; } = StrategySelectionPolicy.RandomUnique;

    /// <summary>
    /// The smallest number of games any strategy may be measured on in a context. Validation sizes
    /// the contexts against it, so an under-powered matrix fails before it runs rather than
    /// producing intervals too wide to band.
    /// </summary>
    public required int MinimumGamesPerStrategy { get; init; }

    public required IReadOnlyList<CalibrationContext> CalibrationContexts { get; init; }

    public required IReadOnlyList<CalibrationContext> HoldoutContexts { get; init; }

    public required int TotalGameBudget { get; init; }

    public required double RuntimeBudgetSeconds { get; init; }

    [JsonIgnore]
    public IEnumerable<CalibrationContext> AllContexts => CalibrationContexts.Concat(HoldoutContexts);

    /// <summary>Games the whole matrix will consume across every context and repeat.</summary>
    public int PlannedGames => AllContexts.Sum(context => context.TotalGames);

    /// <summary>Conditions the matrix will run, each a separate batch under the 100-game ceiling.</summary>
    public int PlannedConditions => AllContexts.Sum(context => context.Repeats);

    /// <summary>
    /// The panel being measured: the declared names, or the whole registered set when none are
    /// named.
    /// </summary>
    public int ResolvePanelSize()
        => StrategyNames.Count > 0 ? StrategyNames.Count : StrategyRegistry.GetDefinitions(StrategySet).Count;
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
