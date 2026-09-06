using System.Globalization;
using System.Text;
using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

public sealed record CandidateGeneDifference(CandidateGene Gene, string ParentValue, string CandidateValue);

public sealed class CandidateStageRecord
{
    public required CandidateEvaluationStage Stage { get; init; }

    public required string ControlExperimentId { get; init; }

    public required string TreatmentExperimentId { get; init; }

    public required string Context { get; init; }

    public required int GamesPerArm { get; init; }

    public required int Attempts { get; init; }

    public CandidateStageMeasurement? Measurement { get; init; }
}

/// <summary>
/// Everything a person needs to decide whether a candidate should be promoted, assembled in one
/// place: what it changed, where it came from, which artifacts back it, what was measured, and what
/// went wrong along the way.
///
/// The packet reports mechanical eligibility and explicitly stops there. Phase 6's gate is that no
/// candidate enters a player-facing pool without review, so a packet that recommended promotion
/// would be quietly doing the reviewing. <see cref="Blockers"/> lists what disqualifies a
/// candidate; an empty list means "nothing mechanical is in the way", not "promote this".
/// </summary>
public sealed class CandidatePromotionPacket
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-promotion-packet.v1";

    public required string SchemaVersion { get; init; }

    public required string CandidateId { get; init; }

    public required string DisplayName { get; init; }

    public required CandidateLineage Lineage { get; init; }

    /// <summary>The genes that differ from the parent, with both canonical values.</summary>
    public required IReadOnlyList<CandidateGeneDifference> GeneDiff { get; init; }

    public required CandidateQueueStatus Status { get; init; }

    public required CandidateRankingRow Ranking { get; init; }

    public required IReadOnlyList<CandidateStageRecord> Stages { get; init; }

    /// <summary>Integrity failures, evidence failures, and pruning reasons recorded during the run.</summary>
    public required IReadOnlyList<string> Failures { get; init; }

    /// <summary>
    /// Reasons this candidate is not mechanically eligible. Empty means nothing is in the way; it
    /// is not a recommendation to promote.
    /// </summary>
    public required IReadOnlyList<string> Blockers { get; init; }

    public bool IsMechanicallyEligible => Blockers.Count == 0;

    public static CandidatePromotionPacket Build(
        CandidateEvaluationPlan plan,
        CandidateEvaluationQueue queue,
        CandidateGenome genome,
        StrategyDefinition parentDefinition,
        CandidateRankingRow ranking)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(genome);
        ArgumentNullException.ThrowIfNull(parentDefinition);
        ArgumentNullException.ThrowIfNull(ranking);

        var entry = queue.Entries.FirstOrDefault(candidate =>
                        string.Equals(candidate.CandidateId, genome.CandidateId, StringComparison.Ordinal))
                    ?? throw new ArgumentException(
                        $"Candidate '{genome.CandidateId}' is not in queue '{queue.QueueId}'.", nameof(queue));

        if (parentDefinition.Strategy is not ParameterizedSpendingStrategy parameterizedParent)
            throw new ArgumentException("The parent must be a parameterized strategy.", nameof(parentDefinition));

        return new CandidatePromotionPacket
        {
            SchemaVersion = CurrentSchemaVersion,
            CandidateId = genome.CandidateId,
            DisplayName = genome.DisplayName,
            Lineage = genome.Lineage,
            GeneDiff = BuildGeneDiff(CandidateGenomeFactory.ExtractGenes(parameterizedParent), genome.Genes),
            Status = entry.Status,
            Ranking = ranking,
            Stages = BuildStageRecords(plan, entry),
            Failures = BuildFailures(entry),
            Blockers = BuildBlockers(entry, ranking)
        };
    }

    private static IReadOnlyList<CandidateGeneDifference> BuildGeneDiff(CandidateGeneSet parent, CandidateGeneSet candidate)
    {
        var parentFragments = CandidateGenomeFingerprint.BuildGeneFragments(parent).ToDictionary(f => f.Key, f => f.Value);
        var candidateFragments = CandidateGenomeFingerprint.BuildGeneFragments(candidate);

        return candidateFragments
            .Where(fragment => !string.Equals(parentFragments[fragment.Key], fragment.Value, StringComparison.Ordinal))
            .Select(fragment => new CandidateGeneDifference(fragment.Key, parentFragments[fragment.Key], fragment.Value))
            .ToList();
    }

    private static IReadOnlyList<CandidateStageRecord> BuildStageRecords(
        CandidateEvaluationPlan plan,
        CandidateQueueEntry entry)
    {
        var records = new List<CandidateStageRecord>();
        foreach (var stage in new[]
                 {
                     CandidateEvaluationStage.Smoke, CandidateEvaluationStage.Calibration,
                     CandidateEvaluationStage.Comparison, CandidateEvaluationStage.Holdout
                 })
        {
            if (!entry.Attempts.TryGetValue(stage, out var attempts) || attempts == 0) continue;

            var isHoldout = stage == CandidateEvaluationStage.Holdout;
            var context = isHoldout ? plan.HoldoutContext : plan.ScreeningContext;
            var stageSlug = stage.ToString().ToLowerInvariant();
            records.Add(new CandidateStageRecord
            {
                Stage = stage,
                ControlExperimentId = $"{plan.EvaluationId}_{stageSlug}_control",
                TreatmentExperimentId = $"{plan.EvaluationId}_{stageSlug}_treatment",
                Context = $"{context.BoardWidth}x{context.BoardHeight} seed {context.BaseSeed.ToString(CultureInfo.InvariantCulture)}",
                GamesPerArm = CandidateEvaluationQueue.GamesPerArm(stage),
                Attempts = attempts,
                Measurement = entry.Measurements.TryGetValue(stage, out var measurement) ? measurement : null
            });
        }

        return records;
    }

    private static IReadOnlyList<string> BuildFailures(CandidateQueueEntry entry)
    {
        var failures = new List<string>();
        foreach (var (stage, attempts) in entry.Attempts.OrderBy(pair => (int)pair.Key))
        {
            if (attempts > 1) failures.Add($"{stage} needed {attempts} attempts.");
        }

        if (!string.IsNullOrWhiteSpace(entry.Detail)) failures.Add(entry.Detail);
        return failures;
    }

    private static IReadOnlyList<string> BuildBlockers(CandidateQueueEntry entry, CandidateRankingRow ranking)
    {
        var blockers = new List<string>();
        if (entry.Status != CandidateQueueStatus.Passed)
            blockers.Add($"Status is {entry.Status}; only a candidate that cleared the holdout is eligible.");
        if (entry.HighestPassedStage != CandidateEvaluationStage.Holdout)
            blockers.Add("The holdout stage has not passed.");
        if (ranking.Robustness is not { IsDemonstrated: true })
            blockers.Add("Robustness is not demonstrated; it was measured in fewer than two contexts.");
        else if (!ranking.Robustness.SignConsistent)
            blockers.Add("The measured advantage reversed sign between contexts, so it did not transfer.");

        return blockers;
    }
}

/// <summary>
/// Renders a packet for human review. Promotion is a judgement call made by a person, and a JSON
/// blob is a poor thing to make one from.
/// </summary>
public static class CandidatePromotionPacketMarkdown
{
    public static string Render(CandidatePromotionPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        var builder = new StringBuilder();

        builder.AppendLine($"# Promotion packet — {packet.DisplayName}");
        builder.AppendLine();
        builder.AppendLine(packet.IsMechanicallyEligible
            ? "**Mechanically eligible for review.** Nothing below disqualifies this candidate. This is not a recommendation to promote."
            : "**Not eligible.** See Blockers.");
        builder.AppendLine();

        builder.AppendLine("## Identity and lineage");
        builder.AppendLine();
        builder.AppendLine($"- Candidate ID: `{packet.CandidateId}`");
        builder.AppendLine($"- Parent: `{packet.Lineage.ParentStrategyId}` ({packet.Lineage.ParentStrategySet})");
        builder.AppendLine($"- Parent definition fingerprint: `{packet.Lineage.ParentDefinitionFingerprint}`");
        builder.AppendLine($"- Status: {packet.Status}");
        if (!string.IsNullOrWhiteSpace(packet.Lineage.Notes))
            builder.AppendLine($"- Origin: {packet.Lineage.Notes}");
        builder.AppendLine();

        builder.AppendLine("## Definition diff");
        builder.AppendLine();
        if (packet.GeneDiff.Count == 0)
        {
            builder.AppendLine("No genes differ from the parent.");
        }
        else
        {
            builder.AppendLine("| Gene | Parent | Candidate |");
            builder.AppendLine("|---|---|---|");
            foreach (var difference in packet.GeneDiff)
                builder.AppendLine($"| {difference.Gene} | `{difference.ParentValue}` | `{difference.CandidateValue}` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Measures");
        builder.AppendLine();
        var strength = packet.Ranking.Strength;
        builder.AppendLine(strength == null
            ? "- Strength: not measured."
            : $"- Strength ({strength.MeasuredStage}): `{strength.Estimate:0.####}` "
              + $"(95% CI `{strength.Ci95Low:0.####}`..`{strength.Ci95High:0.####}`)");
        var robustness = packet.Ranking.Robustness;
        builder.AppendLine(robustness == null
            ? "- Robustness: not measured."
            : $"- Robustness: {robustness.ContextsMeasured} context(s), worst lower bound "
              + $"`{robustness.WorstCi95Low:0.####}`, sign consistent: {robustness.SignConsistent}, "
              + $"demonstrated: {robustness.IsDemonstrated}");
        builder.AppendLine($"- Lineage fidelity: `{packet.Ranking.LineageFidelity:0.####}`");
        builder.AppendLine($"- Behavioral diversity: `{packet.Ranking.BehavioralDiversity:0.####}`");
        builder.AppendLine();
        builder.AppendLine("Observed category profile: "
            + string.Join(", ", packet.Ranking.CategoryProfile
                .Where(pair => pair.Value > 0)
                .Select(pair => $"{pair.Key} {pair.Value}")));
        builder.AppendLine();

        builder.AppendLine("## Stages and artifacts");
        builder.AppendLine();
        if (packet.Stages.Count == 0)
        {
            builder.AppendLine("No stage was attempted.");
        }
        else
        {
            builder.AppendLine("| Stage | Context | Games/arm | Attempts | Estimate | 95% CI | Control artifact | Treatment artifact |");
            builder.AppendLine("|---|---|---:|---:|---|---|---|---|");
            foreach (var stage in packet.Stages)
            {
                var estimate = stage.Measurement == null ? "—" : $"`{stage.Measurement.Estimate:0.####}`";
                var interval = stage.Measurement == null
                    ? "—"
                    : $"`{stage.Measurement.Ci95Low:0.####}`..`{stage.Measurement.Ci95High:0.####}`";
                builder.AppendLine(
                    $"| {stage.Stage} | {stage.Context} | {stage.GamesPerArm} | {stage.Attempts} | {estimate} | {interval} "
                    + $"| `{stage.ControlExperimentId}` | `{stage.TreatmentExperimentId}` |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Failures");
        builder.AppendLine();
        if (packet.Failures.Count == 0) builder.AppendLine("None recorded.");
        else foreach (var failure in packet.Failures) builder.AppendLine($"- {failure}");

        builder.AppendLine();
        builder.AppendLine("## Blockers");
        builder.AppendLine();
        if (packet.Blockers.Count == 0) builder.AppendLine("None. Promotion remains a reviewed decision.");
        else foreach (var blocker in packet.Blockers) builder.AppendLine($"- {blocker}");

        return builder.ToString();
    }
}
