using System.Text.Json;
using System.Text.Json.Serialization;
using FungusToast.Simulation.Experiments;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// How one stage attempt ended. The split between evidence and integrity is the important one:
/// an integrity failure says nothing about the candidate and may be retried, while an evidence
/// failure is the answer to a preregistered question. Retrying the latter until it passes is
/// exactly the practice the staged gates exist to prevent.
/// </summary>
public enum CandidateStageResult
{
    /// <summary>The stage completed and the candidate may advance.</summary>
    Passed,

    /// <summary>The stage completed and the candidate did not meet its preregistered threshold.</summary>
    FailedEvidence,

    /// <summary>The stage did not produce usable evidence: a crash, parity failure, or incomplete run.</summary>
    FailedIntegrity
}

public enum CandidateQueueStatus
{
    Pending,

    /// <summary>Cleared every stage including the holdout.</summary>
    Passed,

    /// <summary>Answered its preregistered question and lost. Never retried.</summary>
    StoppedOnEvidence,

    /// <summary>Exhausted its retry budget on integrity failures.</summary>
    StoppedOnIntegrity,

    /// <summary>Dropped without running: it cannot reach the threshold, or another candidate beat it outright.</summary>
    Pruned
}

public sealed class CandidateQueueEntry
{
    public required string CandidateId { get; set; }

    public required string DisplayName { get; set; }

    public CandidateQueueStatus Status { get; set; } = CandidateQueueStatus.Pending;

    /// <summary>Null until a game-consuming stage has passed.</summary>
    public CandidateEvaluationStage? HighestPassedStage { get; set; }

    /// <summary>Attempts made per stage, including the failed ones that consumed budget.</summary>
    public Dictionary<CandidateEvaluationStage, int> Attempts { get; set; } = new();

    /// <summary>Why the candidate stopped, or how it was pruned.</summary>
    public string Detail { get; set; } = string.Empty;

    public double? LatestEstimate { get; set; }

    public double? LatestCi95Low { get; set; }

    public double? LatestCi95High { get; set; }

    /// <summary>
    /// Per-stage measurements, kept rather than overwritten because robustness is about agreement
    /// across contexts: comparison runs in the screening context and holdout in an unseen one, and
    /// only keeping both makes that comparison possible.
    /// </summary>
    public Dictionary<CandidateEvaluationStage, CandidateStageMeasurement> Measurements { get; set; } = new();
}

public sealed class CandidateStageMeasurement
{
    public required double Estimate { get; set; }

    public required double Ci95Low { get; set; }

    public required double Ci95High { get; set; }
}

/// <summary>
/// Durable, resumable state for evaluating a set of candidates through the staged ladder.
///
/// Unlike the rest of the candidate model this type is mutable: it is a work queue whose whole
/// purpose is to record progress as it happens. Persisting it after every recorded result is what
/// makes a search resumable, in the same spirit as the per-condition run state added in P2.5.
///
/// Budgets are checked before dispatch rather than after. A stage that cannot fit in the remaining
/// budget is never started, so the queue never leaves a half-funded comparison behind.
/// </summary>
public sealed class CandidateEvaluationQueue
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-candidate-queue.v1";

    /// <summary>
    /// No single run may exceed the per-condition ceiling. A stage runs two of them, one per arm,
    /// so a holdout costs 200 games across two batches of 100 rather than one batch of 200.
    /// </summary>
    public const int MaximumGamesPerBatch = ExperimentManifest.MaximumGamesPerCondition;

    public required string SchemaVersion { get; set; }

    public required string QueueId { get; set; }

    public required int TotalGameBudget { get; set; }

    public required double TotalRuntimeBudgetSeconds { get; set; }

    /// <summary>Attempts allowed per stage before a candidate stops on integrity.</summary>
    public required int MaximumAttemptsPerStage { get; set; }

    public int GamesConsumed { get; set; }

    public double RuntimeSecondsConsumed { get; set; }

    public required List<CandidateQueueEntry> Entries { get; set; }

    [JsonIgnore]
    public int RemainingGameBudget => TotalGameBudget - GamesConsumed;

    [JsonIgnore]
    public double RemainingRuntimeBudgetSeconds => TotalRuntimeBudgetSeconds - RuntimeSecondsConsumed;

    public static CandidateEvaluationQueue Create(
        string queueId,
        IEnumerable<CandidateGenome> candidates,
        int totalGameBudget,
        double totalRuntimeBudgetSeconds,
        int maximumAttemptsPerStage = 2)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (string.IsNullOrWhiteSpace(queueId))
            throw new ArgumentException("Queue ID is required.", nameof(queueId));
        if (totalGameBudget < 1)
            throw new ArgumentException("Total game budget must be positive.", nameof(totalGameBudget));
        if (!double.IsFinite(totalRuntimeBudgetSeconds) || totalRuntimeBudgetSeconds <= 0)
            throw new ArgumentException("Total runtime budget must be finite and positive.", nameof(totalRuntimeBudgetSeconds));
        if (maximumAttemptsPerStage < 1)
            throw new ArgumentException("Maximum attempts per stage must be at least 1.", nameof(maximumAttemptsPerStage));

        return new CandidateEvaluationQueue
        {
            SchemaVersion = CurrentSchemaVersion,
            QueueId = queueId,
            TotalGameBudget = totalGameBudget,
            TotalRuntimeBudgetSeconds = totalRuntimeBudgetSeconds,
            MaximumAttemptsPerStage = maximumAttemptsPerStage,
            Entries = candidates
                .Select(candidate => new CandidateQueueEntry
                {
                    CandidateId = candidate.CandidateId,
                    DisplayName = candidate.DisplayName
                })
                .ToList()
        };
    }

    /// <summary>
    /// The next candidate and stage to run, or null when the queue is finished or out of budget.
    /// Candidates advance in list order, and every candidate clears a stage before any candidate
    /// starts the next one, so a cheap stage eliminates the field before an expensive one begins.
    /// </summary>
    public CandidateWorkItem? NextWorkItem()
    {
        foreach (var stage in GameConsumingStages)
        {
            foreach (var entry in Entries)
            {
                if (entry.Status != CandidateQueueStatus.Pending) continue;
                if (StageIndex(entry.HighestPassedStage) >= StageIndex(stage)) continue;

                var cost = StageGameCost(stage);
                if (cost > RemainingGameBudget) return null;
                if (ProjectedRuntimeSeconds(cost) > RemainingRuntimeBudgetSeconds) return null;
                return new CandidateWorkItem(entry.CandidateId, stage, cost);
            }
        }

        return null;
    }

    /// <summary>
    /// Records how a stage attempt ended and advances, stops, or schedules a retry accordingly.
    /// </summary>
    public void RecordStageResult(
        string candidateId,
        CandidateEvaluationStage stage,
        CandidateStageResult result,
        int gamesConsumed,
        double runtimeSecondsConsumed,
        string detail = "",
        double? estimate = null,
        double? ci95Low = null,
        double? ci95High = null)
    {
        var entry = RequireEntry(candidateId);
        GamesConsumed += gamesConsumed;
        RuntimeSecondsConsumed += runtimeSecondsConsumed;
        entry.Attempts[stage] = entry.Attempts.TryGetValue(stage, out var attempts) ? attempts + 1 : 1;
        entry.Detail = detail;
        if (estimate.HasValue) entry.LatestEstimate = estimate;
        if (ci95Low.HasValue) entry.LatestCi95Low = ci95Low;
        if (ci95High.HasValue) entry.LatestCi95High = ci95High;
        if (estimate.HasValue && ci95Low.HasValue && ci95High.HasValue)
        {
            entry.Measurements[stage] = new CandidateStageMeasurement
            {
                Estimate = estimate.Value,
                Ci95Low = ci95Low.Value,
                Ci95High = ci95High.Value
            };
        }

        switch (result)
        {
            case CandidateStageResult.Passed:
                entry.HighestPassedStage = stage;
                if (stage == CandidateEvaluationStage.Holdout) entry.Status = CandidateQueueStatus.Passed;
                break;

            case CandidateStageResult.FailedEvidence:
                // A preregistered question was answered. Retrying it until it passes would turn the
                // staged gates into a search for a favorable sample.
                entry.Status = CandidateQueueStatus.StoppedOnEvidence;
                break;

            case CandidateStageResult.FailedIntegrity:
                if (entry.Attempts[stage] >= MaximumAttemptsPerStage)
                {
                    entry.Status = CandidateQueueStatus.StoppedOnIntegrity;
                    entry.Detail = $"Exhausted {MaximumAttemptsPerStage} attempts at {stage}. {detail}".Trim();
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown stage result.");
        }
    }

    /// <summary>
    /// Drops pending candidates whose own interval shows they cannot reach the threshold: even the
    /// optimistic end of the estimate is below the margin, so more games can only cost budget.
    /// </summary>
    public int PruneFutileCandidates(double margin)
    {
        var pruned = 0;
        foreach (var entry in Entries)
        {
            if (entry.Status != CandidateQueueStatus.Pending) continue;
            if (entry.LatestCi95High is not { } upperBound || upperBound >= margin) continue;

            entry.Status = CandidateQueueStatus.Pruned;
            entry.Detail = $"Upper interval bound {upperBound:0.####} is below the {margin:0.####} margin; it cannot reach the threshold.";
            pruned++;
        }

        return pruned;
    }

    /// <summary>
    /// Drops pending candidates another candidate confidently beats - its whole interval sits above
    /// theirs. Both must have been measured at the same stage, since intervals from different game
    /// counts are not comparable.
    /// </summary>
    public int PruneDominatedCandidates()
    {
        var measured = Entries
            .Where(entry => entry.Status == CandidateQueueStatus.Pending
                && entry.LatestCi95Low.HasValue
                && entry.LatestCi95High.HasValue
                && entry.HighestPassedStage.HasValue)
            .ToList();

        var pruned = 0;
        foreach (var entry in measured)
        {
            var dominator = measured.FirstOrDefault(other =>
                !ReferenceEquals(other, entry)
                && other.HighestPassedStage == entry.HighestPassedStage
                && other.LatestCi95Low > entry.LatestCi95High);
            if (dominator == null) continue;

            entry.Status = CandidateQueueStatus.Pruned;
            entry.Detail =
                $"Dominated by '{dominator.DisplayName}', whose interval lower bound {dominator.LatestCi95Low:0.####} "
                + $"exceeds this candidate's upper bound {entry.LatestCi95High:0.####}.";
            pruned++;
        }

        return pruned;
    }

    /// <summary>
    /// What a stage is expected to cost in seconds, projected from this queue's own measured
    /// throughput rather than an authored guess. Before any games have run there is nothing to
    /// project from, so only an already-exhausted budget blocks dispatch.
    /// </summary>
    public double ProjectedRuntimeSeconds(int gameCount)
    {
        if (RemainingRuntimeBudgetSeconds <= 0) return double.PositiveInfinity;
        if (GamesConsumed <= 0) return 0;
        return RuntimeSecondsConsumed / GamesConsumed * gameCount;
    }

    /// <summary>Games one stage costs across both arms.</summary>
    public static int StageGameCost(CandidateEvaluationStage stage) => GamesPerArm(stage) * 2;

    public static int GamesPerArm(CandidateEvaluationStage stage) => stage switch
    {
        CandidateEvaluationStage.Smoke => CandidateEvaluationPlan.DefaultSmokeGames,
        CandidateEvaluationStage.Calibration => 20,
        CandidateEvaluationStage.Comparison => 50,
        CandidateEvaluationStage.Holdout => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Stage consumes no games.")
    };

    private CandidateQueueEntry RequireEntry(string candidateId)
        => Entries.FirstOrDefault(entry => string.Equals(entry.CandidateId, candidateId, StringComparison.Ordinal))
            ?? throw new ArgumentException($"Candidate '{candidateId}' is not in queue '{QueueId}'.", nameof(candidateId));

    private static readonly CandidateEvaluationStage[] GameConsumingStages =
    {
        CandidateEvaluationStage.Smoke,
        CandidateEvaluationStage.Calibration,
        CandidateEvaluationStage.Comparison,
        CandidateEvaluationStage.Holdout
    };

    private static int StageIndex(CandidateEvaluationStage? stage) => stage.HasValue ? (int)stage.Value : -1;
}

public sealed record CandidateWorkItem(string CandidateId, CandidateEvaluationStage Stage, int GameCost);

/// <summary>
/// Persistence for the queue. Saving after every recorded result is what makes a long search
/// resumable rather than restartable.
/// </summary>
public static class CandidateEvaluationQueueJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static CandidateEvaluationQueue Deserialize(string json)
    {
        var queue = JsonSerializer.Deserialize<CandidateEvaluationQueue>(json, SerializerOptions);
        if (queue == null) throw new JsonException("Candidate queue must contain a JSON object.");
        if (!string.Equals(queue.SchemaVersion, CandidateEvaluationQueue.CurrentSchemaVersion, StringComparison.Ordinal))
            throw new JsonException(
                $"Candidate queue declares schema '{queue.SchemaVersion}'; expected '{CandidateEvaluationQueue.CurrentSchemaVersion}'.");
        return queue;
    }

    public static string Serialize(CandidateEvaluationQueue queue) => JsonSerializer.Serialize(queue, SerializerOptions);

    public static void Save(CandidateEvaluationQueue queue, string path)
    {
        ArgumentNullException.ThrowIfNull(queue);
        File.WriteAllText(path, Serialize(queue));
    }

    public static CandidateEvaluationQueue Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Candidate queue '{path}' does not exist.", path);
        return Deserialize(File.ReadAllText(path));
    }

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
