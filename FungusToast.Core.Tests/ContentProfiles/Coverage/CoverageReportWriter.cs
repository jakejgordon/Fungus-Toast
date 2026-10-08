using System.Text;

namespace FungusToast.Core.Tests.ContentProfiles.Coverage;

/// <summary>Builds the generated FungusToast.Core/docs/CONTENT_COVERAGE_REPORT.md.</summary>
internal static class CoverageReportWriter
{
    public static string Build(
        ContentCoverageAnalyzer.Result analysis,
        IReadOnlyDictionary<(string ContentKey, string StrategyId), CoverageDecision> decisions)
    {
        // Strategies with identical plans share a candidate, but they can be decided differently
        // (a deliberately weak variant may decline what its siblings adopt), so each row holds only
        // strategies that share one decision record, or share having none.
        var rows = analysis.Candidates
            .SelectMany(candidate => candidate.Strategies
                .GroupBy(strategy => decisions.TryGetValue((candidate.Item.Content.Key, strategy.StrategyId), out var decision) ? decision : null)
                .Select(group => candidate with { Strategies = group.ToList() }))
            .Select(candidate => (Candidate: candidate, Status: Classify(candidate, decisions)))
            .ToList();

        var sb = new StringBuilder();
        sb.Append("# Content Coverage Report\n\n");
        sb.Append("> **Generated** by `CoverageDecisionTests` from the content tags, the player-facing strategy plans, and the recorded decisions. Do not edit by hand.\n");
        sb.Append("> Regenerate with `FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentProfiles`.\n");
        sb.Append("> How matches are scored and what each decision means: [AI_COVERAGE_DECISIONS.md](second-level/AI_COVERAGE_DECISIONS.md). Tags: [AI_CONTENT_TAGS.md](second-level/AI_CONTENT_TAGS.md).\n\n");
        sb.Append("Each row asks: *should these AI strategies use this content?* A row appears when the content scores at least ")
            .Append(ContentCoverageAnalyzer.CandidateThreshold)
            .Append(" points against a strategy that does not already use it. Strategies with identical plans share a row; each strategy's goal sentence follows its name.\n\n");

        sb.Append("## Summary\n\n");
        var withCandidates = rows.Select(r => r.Candidate.Item.Content.Key).Distinct().Count();
        sb.Append($"- {analysis.Items.Count} tagged content items (Mycovariant tier families count once); {withCandidates} have at least one candidate.\n");
        sb.Append($"- {rows.Count} candidate rows, covering {rows.Sum(r => r.Candidate.Strategies.Count)} strategy pairings.\n\n");
        sb.Append("| Status | Rows |\n|---|---|\n");
        foreach (var status in rows.GroupBy(r => r.Status.Label).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.Append($"| {status.Key} | {status.Count()} |\n");
        }

        foreach (var item in analysis.Items)
        {
            var itemRows = rows.Where(r => r.Candidate.Item.Content.Key == item.Content.Key).ToList();
            if (itemRows.Count == 0)
            {
                continue;
            }

            sb.Append($"\n## {item.Name}\n\n");
            sb.Append("| Strategies | Score | Reasons | Context | Decision |\n|---|---|---|---|---|\n");
            foreach (var (candidate, status) in itemRows.OrderBy(r => r.Candidate.Strategies[0].Label, StringComparer.Ordinal))
            {
                sb.Append($"| {StrategiesCell(candidate)} ")
                    .Append($"| {candidate.Score} ")
                    .Append($"| {Cell(string.Join("; ", candidate.Reasons))} ")
                    .Append($"| {Cell(candidate.Context.Count == 0 ? "—" : string.Join("; ", candidate.Context))} ")
                    .Append($"| {Cell(status.Text)} |\n");
            }
        }

        var quiet = analysis.Items
            .Where(item => rows.All(r => r.Candidate.Item.Content.Key != item.Content.Key))
            .Select(item => item.Name)
            .ToList();
        if (quiet.Count > 0)
        {
            sb.Append("\n## Content with no candidates\n\n");
            sb.Append("Either already in every plan it fits, or a weak fit for every current strategy: ")
                .Append(string.Join(", ", quiet)).Append(".\n");
        }

        return sb.ToString();
    }

    private static (string Label, string Text) Classify(
        ContentCoverageAnalyzer.Candidate candidate,
        IReadOnlyDictionary<(string ContentKey, string StrategyId), CoverageDecision> decisions)
    {
        var found = candidate.Strategies
            .Select(strategy => decisions.TryGetValue((candidate.Item.Content.Key, strategy.StrategyId), out var d) ? d : null)
            .ToList();
        if (found.Any(d => d == null))
        {
            return ("Open: no decision", "**Open**: no decision recorded");
        }

        var decision = found[0]!;
        if (found.Any(d => !d!.Reasons.SequenceEqual(candidate.Reasons, StringComparer.Ordinal)))
        {
            return ("Open: stale decision", $"**Stale**: decided against \"{string.Join("; ", decision.Reasons)}\"");
        }

        if (decision.Disposition == CoverageDisposition.Baseline)
        {
            return ("Baseline (not reviewed)", "Baseline: not reviewed");
        }

        var text = new StringBuilder($"**{decision.Disposition}**: {decision.Rationale}");
        if (!string.IsNullOrWhiteSpace(decision.FollowUp))
        {
            text.Append($" Follow-up: {decision.FollowUp}.");
        }

        if (!string.IsNullOrWhiteSpace(decision.RevisitWhen))
        {
            text.Append($" Revisit when: {decision.RevisitWhen}.");
        }

        text.Append($" ({decision.DecidedBy}, {decision.Date})");
        return (decision.Disposition.ToString(), text.ToString());
    }

    /// <summary>
    /// Each strategy's name with its one-sentence goal (the profile's Fantasy, AIPlayerIntentions), so a
    /// reader can judge the fit without looking the strategy up. Variants sharing a sentence share a line.
    /// </summary>
    private static string StrategiesCell(ContentCoverageAnalyzer.Candidate candidate) =>
        string.Join("<br>", candidate.Strategies
            .GroupBy(strategy => strategy.Definition.Metadata.AIPlayerIntentions?.Trim() ?? string.Empty)
            .Select(group =>
            {
                var names = string.Join(", ", group.Select(strategy => $"**{Cell(strategy.Label)}**"));
                return string.IsNullOrEmpty(group.Key) ? names : $"{names}: {Cell(group.Key)}";
            }));

    private static string Cell(string text) => text.Replace("|", "\\|").Replace("\n", " ");
}
