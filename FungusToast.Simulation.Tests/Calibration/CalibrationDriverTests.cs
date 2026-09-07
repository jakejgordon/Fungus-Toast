using FungusToast.Core.AI;
using FungusToast.Simulation.Calibration;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class CalibrationDriverTests
{
    [Fact]
    public void TheDriver_RunsEveryConditionInTheMatrixOnce()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);
        var invocations = new List<IReadOnlyList<string>>();
        var driver = new CalibrationDriver(arguments => { invocations.Add(arguments); return Ok(); });

        var report = driver.Run(matrix, state);

        Assert.Equal(matrix.PlannedConditions, report.ConditionsRun);
        Assert.Equal(0, report.ConditionsFailed);
        Assert.Equal(matrix.PlannedGames, report.GamesRun);
        Assert.Equal(matrix.PlannedConditions, invocations.Count);
        Assert.Equal(0, state.RemainingConditions);
    }

    /// <summary>
    /// A calibration campaign is hours of simulation, so an interrupted run must continue rather
    /// than start over.
    /// </summary>
    [Fact]
    public void AnInterruptedRun_ResumesWithoutRepeatingCompletedConditions()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);

        // First pass completes two conditions, then every later attempt fails.
        var completed = 0;
        var failing = new CalibrationDriver(_ => ++completed <= 2 ? Ok() : Failed());
        failing.Run(matrix, state, maximumAttemptsPerCondition: 1);

        var secondPassInvocations = 0;
        var resuming = new CalibrationDriver(_ => { secondPassInvocations++; return Ok(); });
        var report = resuming.Run(matrix, state, maximumAttemptsPerCondition: 5);

        Assert.Equal(2, report.ConditionsSkipped);
        Assert.Equal(matrix.PlannedConditions - 2, secondPassInvocations);
        Assert.Equal(0, state.RemainingConditions);
    }

    /// <summary>
    /// Widening a frozen matrix later should cost only the contexts that were added, not a rerun of
    /// everything already measured.
    /// </summary>
    [Fact]
    public void WideningAMatrix_OnlyRunsTheAddedContexts()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);
        new CalibrationDriver(_ => Ok()).Run(matrix, state);
        var originalConditions = matrix.PlannedConditions;

        var widened = WithExtraCalibrationContext(matrix);
        var invocations = 0;
        var report = new CalibrationDriver(_ => { invocations++; return Ok(); }).Run(widened, state);

        Assert.Equal(1, invocations);
        Assert.Equal(originalConditions, report.ConditionsSkipped);
    }

    /// <summary>
    /// Resuming across matrices would mix evidence from two different frozen designs, which is
    /// exactly what freezing a matrix is meant to prevent.
    /// </summary>
    [Fact]
    public void ResumingAgainstADifferentMatrix_IsRefused()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);
        state.MatrixId = "some-other-matrix";

        var exception = Assert.Throws<InvalidOperationException>(
            () => new CalibrationDriver(_ => Ok()).Run(matrix, state));
        Assert.Contains("different frozen designs", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Validation already refuses a matrix whose plan exceeds its budget, so this guard matters for
    /// a resumed run whose earlier passes already spent most of it.
    /// </summary>
    [Fact]
    public void TheDriver_StopsWhenTheBudgetCannotFundTheNextCondition()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);
        state.GamesRun = matrix.TotalGameBudget - 100;

        var report = new CalibrationDriver(_ => Ok()).Run(matrix, state);

        // The first context needs 300 games and only 100 remain.
        Assert.Equal(0, report.ConditionsRun);
        Assert.Equal("Remaining budget cannot fund the next condition.", report.StopReason);
    }

    /// <summary>
    /// A campaign that abandons a condition on one transient failure leaves a hole that is only
    /// discovered at analysis time, so retries happen inside the pass.
    /// </summary>
    [Fact]
    public void ATransientFailure_IsRetriedWithinTheSamePass()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);
        var attempts = 0;

        var report = new CalibrationDriver(_ => ++attempts == 1 ? Failed() : Ok())
            .Run(matrix, state, maximumAttemptsPerCondition: 2);

        Assert.Equal(matrix.PlannedConditions, report.ConditionsRun);
        Assert.Equal(0, report.ConditionsFailed);
        Assert.Equal(2, state.Conditions[0].Attempts);
        Assert.Equal(CalibrationConditionStatus.Complete, state.Conditions[0].Status);
    }

    [Fact]
    public void AFailedCondition_IsRetriedUpToTheCapThenRecordedAsFailed()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);

        new CalibrationDriver(_ => Failed()).Run(matrix, state, maximumAttemptsPerCondition: 2);

        var first = state.Conditions[0];
        Assert.Equal(CalibrationConditionStatus.Failed, first.Status);
        Assert.Equal(2, first.Attempts);
    }

    [Fact]
    public void RunState_RoundTripsThroughJsonAndKeepsProgress()
    {
        var matrix = LoadReferenceMatrix();
        var state = CalibrationRunState.Create(matrix);
        new CalibrationDriver(_ => Ok()).Run(matrix, state);

        var resumed = CalibrationRunStateJson.Deserialize(CalibrationRunStateJson.Serialize(state));

        Assert.Equal(state.GamesRun, resumed.GamesRun);
        Assert.Equal(0, resumed.RemainingConditions);
        Assert.All(resumed.Conditions, record => Assert.Equal(CalibrationConditionStatus.Complete, record.Status));
    }

    /// <summary>
    /// The rendered command must actually describe the frozen context, or the artifact will not be
    /// the thing the matrix promised.
    /// </summary>
    [Fact]
    public void TheRenderedCommand_MatchesTheFrozenContext()
    {
        var matrix = LoadReferenceMatrix();
        var key = CalibrationConditionEmitter.EnumerateConditions(matrix)[0];
        var context = CalibrationConditionEmitter.ResolveContext(matrix, key);
        var arguments = CalibrationConditionEmitter.RenderCommandLine(matrix, key).ToList();

        Assert.Equal(context.GamesPerCondition.ToString(), ValueOf(arguments, "--games"));
        Assert.Equal(context.BaseSeed.ToString(), ValueOf(arguments, "--seed"));
        Assert.Equal(context.PlayerCount.ToString(), ValueOf(arguments, "--players"));
        Assert.Equal(context.BoardWidth.ToString(), ValueOf(arguments, "--width"));
        Assert.Equal(matrix.StrategySet.ToString(), ValueOf(arguments, "--strategy-set"));
        Assert.Equal(matrix.SelectionPolicy.ToString(), ValueOf(arguments, "--selection-policy"));
        // Calibration measures rather than decides, which is what lets it exceed the staged ceiling.
        Assert.Equal("Exploratory", ValueOf(arguments, "--evidence-stage"));
        Assert.Contains("--no-nutrient-patches", arguments);
        Assert.Contains("--no-mycovariants", arguments);
        Assert.Contains("--no-starting-adaptations", arguments);
        Assert.Contains("--rotate-slots", arguments);
    }

    [Fact]
    public void EveryConditionGetsItsOwnArtifactIdentity()
    {
        var matrix = LoadReferenceMatrix();

        var ids = CalibrationConditionEmitter.EnumerateConditions(matrix)
            .Select(key => CalibrationConditionEmitter.BuildExperimentId(matrix, key))
            .ToList();

        Assert.Equal(matrix.PlannedConditions, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, id => Assert.StartsWith(matrix.MatrixId, id, StringComparison.Ordinal));
    }

    private static string ValueOf(IReadOnlyList<string> arguments, string flag)
    {
        var index = arguments.ToList().IndexOf(flag);
        Assert.True(index >= 0 && index + 1 < arguments.Count, $"{flag} is missing.");
        return arguments[index + 1];
    }

    private static CandidateArmOutcome Ok() => new(true, "artifact", 1.0, "Completed.");

    private static CandidateArmOutcome Failed() => new(false, "artifact", 0.5, "exit code 1");

    private static CalibrationMatrix LoadReferenceMatrix()
    {
        _ = AIRoster.TestingStrategies.Count;
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "calibration-matrix.v1.example.json");
        return CalibrationMatrixJson.Deserialize(File.ReadAllText(path));
    }

    private static CalibrationMatrix WithBudget(CalibrationMatrix matrix, int totalGameBudget) => new()
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
        TotalGameBudget = totalGameBudget,
        RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
    };

    private static CalibrationMatrix WithExtraCalibrationContext(CalibrationMatrix matrix) => new()
    {
        SchemaVersion = matrix.SchemaVersion,
        MatrixId = matrix.MatrixId,
        Purpose = matrix.Purpose,
        StrategySet = matrix.StrategySet,
        StrategyNames = matrix.StrategyNames,
        SelectionPolicy = matrix.SelectionPolicy,
        MinimumGamesPerStrategy = matrix.MinimumGamesPerStrategy,
        CalibrationContexts = matrix.CalibrationContexts
            .Concat(new[]
            {
                new CalibrationContext
                {
                    ContextId = "crowded.medium.square.rectangle.generated.140x140",
                    PlayerCount = 6,
                    BoardWidth = 140,
                    BoardHeight = 140,
                    BaseSeed = 2026099000,
                    GamesPerCondition = 100
                }
            })
            .ToList(),
        HoldoutContexts = matrix.HoldoutContexts,
        TotalGameBudget = matrix.TotalGameBudget + 100,
        RuntimeBudgetSeconds = matrix.RuntimeBudgetSeconds
    };
}
