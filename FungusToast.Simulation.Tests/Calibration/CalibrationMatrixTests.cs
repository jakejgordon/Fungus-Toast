using System.Text.Json;
using FungusToast.Core.AI;
using FungusToast.Simulation.Calibration;
using FungusToast.Simulation.Models;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class CalibrationMatrixTests
{
    [Fact]
    public void TheCheckedInReferenceMatrix_DeserializesAndValidates()
    {
        _ = AIRoster.TestingStrategies.Count;
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "calibration-matrix.v1.example.json");
        var matrix = CalibrationMatrixJson.Deserialize(File.ReadAllText(path));

        Assert.Equal(string.Empty, string.Join("; ", CalibrationMatrixValidator.Validate(matrix)));
        Assert.Equal(1600, matrix.PlannedGames);
        Assert.Equal(7, matrix.PlannedConditions);

        // The panel is exactly what solo play draws from, and every context reaches the minimum.
        Assert.Equal(StrategySetEnum.Proven, matrix.StrategySet);
        var panelSize = matrix.ResolvePanelSize();
        Assert.All(
            matrix.AllContexts,
            context => Assert.True(
                context.ExpectedGamesPerStrategy(panelSize) >= matrix.MinimumGamesPerStrategy,
                $"{context.ContextId} gives only {context.ExpectedGamesPerStrategy(panelSize):0.#} games per strategy."));
    }

    /// <summary>
    /// A context ID that does not match its own derived classes would let a matrix claim coverage
    /// it does not have, which is the one thing a frozen matrix must not be able to do.
    /// </summary>
    [Fact]
    public void AContextIdThatMisdescribesItsContext_FailsValidation()
    {
        var matrix = CreateMatrix();
        // Claims to be a duel; actually four players.
        var mislabeled = WithCalibrationContexts(matrix, new[]
        {
            Context("duel.small.square.rectangle.generated.lie", players: 4, width: 80, height: 80, seed: 1),
            matrix.CalibrationContexts[1]
        });

        Assert.Contains(
            CalibrationMatrixValidator.Validate(mislabeled),
            error => error.Contains("must start with its derived rollup key", StringComparison.Ordinal));
    }

    /// <summary>
    /// A holdout that reuses a calibration board is not independent evidence; it re-measures what
    /// the thresholds were fitted on.
    /// </summary>
    [Fact]
    public void AHoldoutReusingACalibrationBoard_FailsValidation()
    {
        var matrix = CreateMatrix();
        var reused = WithHoldoutContexts(matrix, new[]
        {
            Context("duel.small.square.rectangle.generated.reused", players: 2, width: 80, height: 80, seed: 999)
        });

        Assert.Contains(
            CalibrationMatrixValidator.Validate(reused),
            error => error.Contains("reuses a calibration board", StringComparison.Ordinal));
    }

    [Fact]
    public void AHoldoutReusingACalibrationSeed_FailsValidation()
    {
        var matrix = CreateMatrix();
        var reused = WithHoldoutContexts(matrix, new[]
        {
            Context("duel.medium.wide.rectangle.generated.h", players: 2, width: 140, height: 100,
                seed: matrix.CalibrationContexts[0].BaseSeed)
        });

        Assert.Contains(
            CalibrationMatrixValidator.Validate(reused),
            error => error.Contains("reuses a calibration base seed", StringComparison.Ordinal));
    }

    /// <summary>
    /// A single-context matrix produces a global label with no evidence it generalizes, which is
    /// the failure this initiative exists to stop.
    /// </summary>
    [Fact]
    public void AMatrixThatVariesNeitherPlayerCountNorBoardScale_FailsValidation()
    {
        var matrix = CreateMatrix();
        var narrow = WithCalibrationContexts(matrix, new[]
        {
            Context("duel.small.square.rectangle.generated.a", players: 2, width: 80, height: 80, seed: 1),
            Context("duel.small.square.rectangle.generated.b", players: 2, width: 78, height: 78, seed: 2)
        });

        var errors = CalibrationMatrixValidator.Validate(narrow);
        Assert.Contains(errors, error => error.Contains("two player-count classes", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("two board-scale classes", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateContextIds_FailValidation()
    {
        var matrix = CreateMatrix();
        var duplicated = WithCalibrationContexts(matrix, new[]
        {
            Context("duel.small.square.rectangle.generated.same", players: 2, width: 80, height: 80, seed: 1),
            Context("duel.small.square.rectangle.generated.same", players: 2, width: 80, height: 80, seed: 2)
        });

        Assert.Contains(
            CalibrationMatrixValidator.Validate(duplicated),
            error => error.Contains("appears more than once", StringComparison.Ordinal));
    }

    [Fact]
    public void PlannedGamesExceedingTheBudget_FailValidation()
    {
        var matrix = CreateMatrix();
        var starved = new CalibrationMatrix
        {
            SchemaVersion = matrix.SchemaVersion,
            MatrixId = matrix.MatrixId,
            Purpose = matrix.Purpose,
            StrategySet = matrix.StrategySet,
            StrategyNames = matrix.StrategyNames,
            SelectionPolicy = matrix.SelectionPolicy,
            MinimumGamesPerStrategy = matrix.MinimumGamesPerStrategy,
            CalibrationContexts = matrix.CalibrationContexts,
            HoldoutContexts = matrix.HoldoutContexts,
            TotalGameBudget = 10,
            RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
        };

        Assert.Contains(
            CalibrationMatrixValidator.Validate(starved),
            error => error.Contains("exceed totalGameBudget", StringComparison.Ordinal));
    }

    [Fact]
    public void AContextExceedingThePerConditionCeiling_FailsValidation()
    {
        var matrix = CreateMatrix();
        var oversized = WithCalibrationContexts(matrix, new[]
        {
            Context("duel.small.square.rectangle.generated.big", players: 2, width: 80, height: 80, seed: 1,
                games: CalibrationMatrix.MaximumGamesPerContext + 1),
            matrix.CalibrationContexts[1]
        });

        Assert.Contains(
            CalibrationMatrixValidator.Validate(oversized),
            error => error.Contains("gamesPerCondition must be between", StringComparison.Ordinal));
    }

    [Fact]
    public void AnUnregisteredStrategyName_FailsValidation()
    {
        _ = AIRoster.TestingStrategies.Count;
        var matrix = CreateMatrix();
        var unknown = new CalibrationMatrix
        {
            SchemaVersion = matrix.SchemaVersion,
            MatrixId = matrix.MatrixId,
            Purpose = matrix.Purpose,
            StrategySet = matrix.StrategySet,
            StrategyNames = new[] { "TST_DoesNotExist" },
            MinimumGamesPerStrategy = matrix.MinimumGamesPerStrategy,
            CalibrationContexts = matrix.CalibrationContexts,
            HoldoutContexts = matrix.HoldoutContexts,
            TotalGameBudget = matrix.TotalGameBudget,
            RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
        };

        Assert.Contains(
            CalibrationMatrixValidator.Validate(unknown),
            error => error.Contains("is not registered", StringComparison.Ordinal));
    }

    [Fact]
    public void AMatrix_RoundTripsThroughJson()
    {
        _ = AIRoster.TestingStrategies.Count;
        var matrix = CreateMatrix();
        var roundTripped = CalibrationMatrixJson.Deserialize(CalibrationMatrixJson.Serialize(matrix));

        Assert.Empty(CalibrationMatrixValidator.Validate(roundTripped));
        Assert.Equal(matrix.PlannedGames, roundTripped.PlannedGames);
        Assert.Equal(
            matrix.CalibrationContexts.Select(context => context.ContextId),
            roundTripped.CalibrationContexts.Select(context => context.ContextId));
    }

    [Fact]
    public void Deserialize_RejectsUnknownFields()
    {
        var json = CalibrationMatrixJson.Serialize(CreateMatrix())
            .Replace("\"matrixId\":", "\"typoField\": true,\n  \"matrixId\":", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CalibrationMatrixJson.Deserialize(json));
    }

    /// <summary>
    /// A panel larger than a lineup must be sampled randomly. StratifiedCycle takes a sliding
    /// window over a fixed ordering, so each strategy would face only its neighbours - that
    /// measures particular matchups and reports them as general strength.
    /// </summary>
    [Fact]
    public void ANonRandomSelectionPolicyOverALargePanel_FailsValidation()
    {
        var matrix = CreateMatrix();
        var cycled = new CalibrationMatrix
        {
            SchemaVersion = matrix.SchemaVersion,
            MatrixId = matrix.MatrixId,
            Purpose = matrix.Purpose,
            StrategySet = matrix.StrategySet,
            StrategyNames = matrix.StrategyNames,
            SelectionPolicy = StrategySelectionPolicy.StratifiedCycle,
            MinimumGamesPerStrategy = matrix.MinimumGamesPerStrategy,
            CalibrationContexts = matrix.CalibrationContexts,
            HoldoutContexts = matrix.HoldoutContexts,
            TotalGameBudget = matrix.TotalGameBudget,
            RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
        };

        Assert.Contains(
            CalibrationMatrixValidator.Validate(cycled),
            error => error.Contains("it fixes which strategies meet", StringComparison.Ordinal));
    }

    /// <summary>
    /// A context too small to give each strategy enough games would produce intervals too wide to
    /// band, so it fails before it runs rather than after hours of simulation.
    /// </summary>
    [Fact]
    public void AContextTooSmallForThePanel_FailsValidationWithTheRepeatsItNeeds()
    {
        _ = AIRoster.TestingStrategies.Count;
        var matrix = CreateMatrix();
        var underPowered = new CalibrationMatrix
        {
            SchemaVersion = matrix.SchemaVersion,
            MatrixId = matrix.MatrixId,
            Purpose = matrix.Purpose,
            StrategySet = StrategySetEnum.Proven,
            SelectionPolicy = matrix.SelectionPolicy,
            MinimumGamesPerStrategy = 30,
            CalibrationContexts = matrix.CalibrationContexts,
            HoldoutContexts = matrix.HoldoutContexts,
            TotalGameBudget = matrix.TotalGameBudget,
            RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
        };

        var errors = CalibrationMatrixValidator.Validate(underPowered);
        Assert.Contains(errors, error => error.Contains("below the declared minimum of 30", StringComparison.Ordinal));
        // The message says what to do about it, not just that it is wrong.
        Assert.Contains(errors, error => error.Contains("repeats of", StringComparison.Ordinal));
    }

    /// <summary>
    /// Per-game seeds run from a condition's base, so overlapping spans would replay the same games
    /// and stop two contexts being independent evidence.
    /// </summary>
    [Fact]
    public void ContextsWithOverlappingSeedRanges_FailValidation()
    {
        var matrix = CreateMatrix();
        var overlapping = WithCalibrationContexts(matrix, new[]
        {
            Context("duel.small.square.rectangle.generated.a", players: 2, width: 80, height: 80, seed: 5000),
            Context("smalltable.large.square.rectangle.generated.b", players: 4, width: 180, height: 180, seed: 5010)
        });

        Assert.Contains(
            CalibrationMatrixValidator.Validate(overlapping),
            error => error.Contains("overlapping seed ranges", StringComparison.Ordinal));
    }

    [Fact]
    public void RepeatsGetConsecutiveSeedBlocks_SoNoGameIsReplayed()
    {
        var context = Context("duel.small.square.rectangle.generated.r", players: 2, width: 80, height: 80, seed: 500, games: 100);
        var repeated = new CalibrationContext
        {
            ContextId = context.ContextId,
            PlayerCount = context.PlayerCount,
            BoardWidth = context.BoardWidth,
            BoardHeight = context.BoardHeight,
            BaseSeed = context.BaseSeed,
            GamesPerCondition = context.GamesPerCondition,
            Repeats = 3
        };

        Assert.Equal(new[] { 500, 600, 700 }, Enumerable.Range(0, 3).Select(repeated.SeedForRepeat));
        Assert.Equal((500, 799), repeated.SeedFootprint);
        Assert.Equal(300, repeated.TotalGames);
        Assert.Throws<ArgumentOutOfRangeException>(() => repeated.SeedForRepeat(3));
    }

    private static CalibrationMatrix CreateMatrix()
    {
        _ = AIRoster.TestingStrategies.Count;
        return new CalibrationMatrix
        {
            SchemaVersion = CalibrationMatrix.CurrentSchemaVersion,
            MatrixId = "test-matrix",
            Purpose = "Exercise the frozen matrix contract.",
            StrategySet = StrategySetEnum.Testing,
            MinimumGamesPerStrategy = 1,
            CalibrationContexts = new[]
            {
                Context("duel.small.square.rectangle.generated.80x80", players: 2, width: 80, height: 80, seed: 1000),
                Context("smalltable.large.square.rectangle.generated.180x180", players: 4, width: 180, height: 180, seed: 2000)
            },
            HoldoutContexts = new[]
            {
                Context("duel.medium.wide.rectangle.generated.140x100", players: 2, width: 140, height: 100, seed: 3000)
            },
            TotalGameBudget = 1000,
            RuntimeBudgetSeconds = 3600
        };
    }

    private static CalibrationContext Context(
        string contextId,
        int players,
        int width,
        int height,
        int seed,
        int games = 50) => new()
    {
        ContextId = contextId,
        PlayerCount = players,
        BoardWidth = width,
        BoardHeight = height,
        BaseSeed = seed,
        GamesPerCondition = games
    };

    private static CalibrationMatrix WithCalibrationContexts(
        CalibrationMatrix matrix,
        IReadOnlyList<CalibrationContext> contexts) => new()
    {
        SchemaVersion = matrix.SchemaVersion,
        MatrixId = matrix.MatrixId,
        Purpose = matrix.Purpose,
        StrategySet = matrix.StrategySet,
        StrategyNames = matrix.StrategyNames,
        SelectionPolicy = matrix.SelectionPolicy,
        MinimumGamesPerStrategy = matrix.MinimumGamesPerStrategy,
        CalibrationContexts = contexts,
        HoldoutContexts = matrix.HoldoutContexts,
        TotalGameBudget = matrix.TotalGameBudget,
        RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
    };

    private static CalibrationMatrix WithHoldoutContexts(
        CalibrationMatrix matrix,
        IReadOnlyList<CalibrationContext> contexts) => new()
    {
        SchemaVersion = matrix.SchemaVersion,
        MatrixId = matrix.MatrixId,
        Purpose = matrix.Purpose,
        StrategySet = matrix.StrategySet,
        StrategyNames = matrix.StrategyNames,
        SelectionPolicy = matrix.SelectionPolicy,
        MinimumGamesPerStrategy = matrix.MinimumGamesPerStrategy,
        CalibrationContexts = matrix.CalibrationContexts,
        HoldoutContexts = contexts,
        TotalGameBudget = matrix.TotalGameBudget,
        RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
    };
}
