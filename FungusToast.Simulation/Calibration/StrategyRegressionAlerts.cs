using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FungusToast.Simulation.Calibration;

/// <summary>Observable execution health for one strategy across a calibration pass.</summary>
public sealed record StrategyExecutionHealth(
    string StrategyName,
    int Decisions,
    int FallbackDecisions,
    int DecisionFailures,
    int ReplayParityFailures)
{
    public double FallbackRate => Decisions == 0 ? 0 : (double)FallbackDecisions / Decisions;
    public double DecisionFailureRate => Decisions == 0 ? 0 : (double)DecisionFailures / Decisions;
}

/// <summary>
/// A durable comparison point for a classified roster. Category profiles come from the same
/// characterization script in both passes; an omitted profile is an evidence gap, not a zero.
/// </summary>
public sealed class StrategyRegressionSnapshot
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-regression-snapshot.v1";

    public required string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string SnapshotId { get; init; }
    public required string MatrixId { get; init; }
    public required DateTime CreatedUtc { get; init; }
    public required string ClassifierVersion { get; init; }
    public required IReadOnlyList<StrategyBandResult> Bands { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<MutationCategory, int>> CategoryProfiles { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<MutationCategory, int>>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, StrategyExecutionHealth> ExecutionHealth { get; init; }
        = new Dictionary<string, StrategyExecutionHealth>(StringComparer.Ordinal);
}

public static class StrategyRegressionSnapshotJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(StrategyRegressionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Validate(snapshot);
        return JsonSerializer.Serialize(snapshot, SerializerOptions);
    }

    public static StrategyRegressionSnapshot Deserialize(string json)
    {
        var snapshot = JsonSerializer.Deserialize<StrategyRegressionSnapshot>(json, SerializerOptions)
            ?? throw new JsonException("Regression snapshot must contain a JSON object.");
        Validate(snapshot);
        return snapshot;
    }

    private static void Validate(StrategyRegressionSnapshot snapshot)
    {
        if (!string.Equals(snapshot.SchemaVersion, StrategyRegressionSnapshot.CurrentSchemaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Regression snapshot schema '{snapshot.SchemaVersion}' is not supported.");
        if (string.IsNullOrWhiteSpace(snapshot.SnapshotId))
            throw new InvalidOperationException("Regression snapshot ID is required.");
        if (string.IsNullOrWhiteSpace(snapshot.MatrixId))
            throw new InvalidOperationException("Regression snapshot matrix ID is required.");
        if (string.IsNullOrWhiteSpace(snapshot.ClassifierVersion))
            throw new InvalidOperationException("Regression snapshot classifier version is required.");
    }
}

public enum StrategyRegressionAlertKind
{
    ClassifierVersionChanged,
    BandMoved,
    RobustnessLost,
    ArchetypeDrift,
    ReplayParityFailure,
    AbnormalFallbackRate,
    AbnormalDecisionFailureRate,
    EvidenceGap
}

public sealed record StrategyRegressionAlert(
    StrategyRegressionAlertKind Kind,
    string StrategyName,
    string Message);

/// <summary>
/// Compares two calibration snapshots without silently turning absent telemetry into healthy
/// telemetry. These are alerts, not promotion verdicts: they identify what needs investigation.
/// </summary>
public static class StrategyRegressionAlerts
{
    public const double MinimumArchetypeSimilarity = 0.95;
    public const int MinimumDecisionsForRateAlert = 20;
    public const double MinimumRateIncrease = 0.05;

    public static IReadOnlyList<StrategyRegressionAlert> Compare(
        StrategyRegressionSnapshot baseline,
        StrategyRegressionSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        var alerts = new List<StrategyRegressionAlert>();
        if (!string.Equals(baseline.ClassifierVersion, current.ClassifierVersion, StringComparison.Ordinal))
        {
            alerts.Add(new StrategyRegressionAlert(
                StrategyRegressionAlertKind.ClassifierVersionChanged,
                "*",
                $"Classifier changed from '{baseline.ClassifierVersion}' to '{current.ClassifierVersion}'; band movement is not comparable until reclassified."));
            return alerts;
        }

        var baselineByName = baseline.Bands.ToDictionary(result => result.StrategyName, StringComparer.Ordinal);
        foreach (var candidate in current.Bands.OrderBy(result => result.StrategyName, StringComparer.Ordinal))
        {
            if (!baselineByName.TryGetValue(candidate.StrategyName, out var prior))
            {
                alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.EvidenceGap, candidate.StrategyName,
                    "No baseline classification exists for this strategy."));
                continue;
            }

            CompareBands(prior, candidate, alerts);
            CompareArchetype(baseline, current, candidate.StrategyName, alerts);
            CompareExecutionHealth(baseline, current, candidate.StrategyName, alerts);
        }

        return alerts;
    }

    private static void CompareBands(StrategyBandResult prior, StrategyBandResult candidate, ICollection<StrategyRegressionAlert> alerts)
    {
        if (prior.OverallEvidence != BandEvidence.Sufficient || candidate.OverallEvidence != BandEvidence.Sufficient)
        {
            if (prior.OverallEvidence == BandEvidence.Sufficient && candidate.OverallEvidence != BandEvidence.Sufficient)
                alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.RobustnessLost, candidate.StrategyName,
                    $"Overall evidence regressed from sufficient to {candidate.OverallEvidence}."));
            return;
        }

        if (prior.OverallBand != candidate.OverallBand)
            alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.BandMoved, candidate.StrategyName,
                $"Overall measured band moved from {prior.OverallBand} to {candidate.OverallBand}."));

        var lostContexts = prior.MaterialContexts.Except(candidate.MaterialContexts, StringComparer.Ordinal).ToList();
        var gainedContexts = candidate.MaterialContexts.Except(prior.MaterialContexts, StringComparer.Ordinal).ToList();
        if (lostContexts.Count > 0 || gainedContexts.Count > 0)
            alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.RobustnessLost, candidate.StrategyName,
                $"Material contexts changed: lost [{string.Join(", ", lostContexts)}], gained [{string.Join(", ", gainedContexts)}]."));
    }

    private static void CompareArchetype(
        StrategyRegressionSnapshot baseline,
        StrategyRegressionSnapshot current,
        string strategyName,
        ICollection<StrategyRegressionAlert> alerts)
    {
        if (!baseline.CategoryProfiles.TryGetValue(strategyName, out var prior)
            || !current.CategoryProfiles.TryGetValue(strategyName, out var candidate))
        {
            alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.EvidenceGap, strategyName,
                "Archetype drift cannot be assessed because one characterization profile is missing."));
            return;
        }

        var similarity = CosineSimilarity(prior, candidate);
        if (similarity < MinimumArchetypeSimilarity)
            alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.ArchetypeDrift, strategyName,
                $"Observed category-profile similarity fell to {similarity:0.000}, below {MinimumArchetypeSimilarity:0.000}."));
    }

    private static void CompareExecutionHealth(
        StrategyRegressionSnapshot baseline,
        StrategyRegressionSnapshot current,
        string strategyName,
        ICollection<StrategyRegressionAlert> alerts)
    {
        if (!baseline.ExecutionHealth.TryGetValue(strategyName, out var prior)
            || !current.ExecutionHealth.TryGetValue(strategyName, out var candidate))
        {
            alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.EvidenceGap, strategyName,
                "Fallback, decision-failure, and replay-parity rates cannot be assessed because execution health is missing."));
            return;
        }

        if (candidate.ReplayParityFailures > 0)
            alerts.Add(new StrategyRegressionAlert(StrategyRegressionAlertKind.ReplayParityFailure, strategyName,
                $"Observed {candidate.ReplayParityFailures} replay parity failure(s)."));

        CompareRate("fallback", prior.FallbackRate, candidate.FallbackRate, candidate.Decisions,
            StrategyRegressionAlertKind.AbnormalFallbackRate, strategyName, alerts);
        CompareRate("decision-failure", prior.DecisionFailureRate, candidate.DecisionFailureRate, candidate.Decisions,
            StrategyRegressionAlertKind.AbnormalDecisionFailureRate, strategyName, alerts);
    }

    private static void CompareRate(string label, double prior, double candidate, int decisions,
        StrategyRegressionAlertKind kind, string strategyName, ICollection<StrategyRegressionAlert> alerts)
    {
        if (decisions < MinimumDecisionsForRateAlert) return;
        if (candidate - prior < MinimumRateIncrease) return;
        alerts.Add(new StrategyRegressionAlert(kind, strategyName,
            $"{label} rate increased from {prior:P1} to {candidate:P1} across {decisions} decisions."));
    }

    private static double CosineSimilarity(
        IReadOnlyDictionary<MutationCategory, int> left,
        IReadOnlyDictionary<MutationCategory, int> right)
    {
        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;
        foreach (var category in Enum.GetValues<MutationCategory>())
        {
            var leftValue = left.GetValueOrDefault(category);
            var rightValue = right.GetValueOrDefault(category);
            dot += leftValue * rightValue;
            leftMagnitude += leftValue * leftValue;
            rightMagnitude += rightValue * rightValue;
        }

        return leftMagnitude <= 0 || rightMagnitude <= 0
            ? 0
            : dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}

/// <summary>Renders a reviewable alert artifact beside calibration results.</summary>
public static class StrategyRegressionAlertReport
{
    public static string Render(
        StrategyRegressionSnapshot baseline,
        StrategyRegressionSnapshot current,
        IReadOnlyList<StrategyRegressionAlert> alerts)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(alerts);

        var builder = new StringBuilder();
        builder.AppendLine($"# Calibration regression alerts — {current.SnapshotId}");
        builder.AppendLine();
        builder.AppendLine($"- Baseline: `{baseline.SnapshotId}` ({baseline.MatrixId})");
        builder.AppendLine($"- Current: `{current.SnapshotId}` ({current.MatrixId})");
        builder.AppendLine($"- Classifier: `{current.ClassifierVersion}`");
        builder.AppendLine($"- Generated: `{current.CreatedUtc:O}`");
        builder.AppendLine();

        if (alerts.Count == 0)
        {
            builder.AppendLine("No regressions detected.");
            return builder.ToString();
        }

        builder.AppendLine("| Alert | Strategy | Detail |");
        builder.AppendLine("|---|---|---|");
        foreach (var alert in alerts.OrderBy(alert => alert.Kind).ThenBy(alert => alert.StrategyName, StringComparer.Ordinal))
            builder.AppendLine($"| {alert.Kind} | {alert.StrategyName} | {alert.Message} |");
        return builder.ToString();
    }
}
