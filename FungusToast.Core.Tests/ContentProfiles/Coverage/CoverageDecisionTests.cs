using System.Globalization;
using System.Reflection;
using System.Text;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Tests.ContentProfiles.Coverage;

/// <summary>
/// The coverage review: every candidate match between a content item and a player-facing
/// strategy needs a current, recorded decision. Rules: docs/second-level/AI_COVERAGE_DECISIONS.md.
/// </summary>
public class CoverageDecisionTests
{
    private const string ReportUpdateVariable = "FUNGUS_UPDATE_CONTENT_CATALOG";
    private const string BaselineWriteVariable = "FUNGUS_WRITE_COVERAGE_BASELINE";
    private const string RulesDoc = "FungusToast.Core/docs/second-level/AI_COVERAGE_DECISIONS.md";

    [Fact]
    public void Every_candidate_has_a_current_decision()
    {
        var analysis = ContentCoverageAnalyzer.Analyze();
        var decisions = IndexDecisions();
        var missing = new List<ContentCoverageAnalyzer.Candidate>();
        var stale = new List<(ContentCoverageAnalyzer.Candidate Candidate, CoverageDecision Decision)>();

        foreach (var candidate in analysis.Candidates)
        {
            foreach (var strategy in candidate.Strategies)
            {
                if (!decisions.TryGetValue((candidate.Item.Content.Key, strategy.StrategyId), out var decision))
                {
                    missing.Add(candidate);
                    break;
                }

                if (!decision.Reasons.SequenceEqual(candidate.Reasons, StringComparer.Ordinal))
                {
                    stale.Add((candidate, decision));
                    break;
                }
            }
        }

        var message = new StringBuilder();
        if (missing.Count > 0)
        {
            message.Append($"{missing.Count} candidate match(es) need a decision. Add a record to CoverageDecisions.Reviewed, ")
                .Append("choose the disposition, and write the rationale (see ").Append(RulesDoc).Append("):\n\n");
            foreach (var candidate in missing)
            {
                message.Append(DescribeCandidate(candidate)).Append(Template(candidate)).Append('\n');
            }
        }

        if (stale.Count > 0)
        {
            message.Append($"{stale.Count} decision(s) are stale: the match changed since they were made. Re-decide, and record the current reasons. ")
                .Append("A stale Baseline line in CoverageBaseline.cs is deleted and replaced by a reviewed record in CoverageDecisions.Reviewed:\n\n");
            foreach (var (candidate, decision) in stale)
            {
                message.Append(DescribeCandidate(candidate))
                    .Append($"//   recorded reasons: {string.Join("; ", decision.Reasons)}\n")
                    .Append(Template(candidate)).Append('\n');
            }
        }

        Assert.True(message.Length == 0, message.ToString());
    }

    [Fact]
    public void No_decision_is_orphaned()
    {
        var current = ContentCoverageAnalyzer.Analyze().Candidates
            .SelectMany(candidate => candidate.Strategies.Select(strategy => (candidate.Item.Content.Key, strategy.StrategyId)))
            .ToHashSet();
        var orphans = CoverageDecisions.All
            .SelectMany(decision => decision.Strategies
                .Where(strategyId => !current.Contains((decision.Content.Key, strategyId)))
                .Select(strategyId => $"{ContentExpression(decision.Content)} / {strategyId} ({decision.Disposition})"))
            .ToList();

        Assert.True(orphans.Count == 0,
            "These decisions no longer match any candidate (the strategy adopted the content, the plan or tags changed, "
            + "or the weights were retuned). Remove these strategy IDs from their records, and delete records left with none; "
            + "git keeps the history:\n" + string.Join("\n", orphans));
    }

    [Fact]
    public void Decision_records_are_well_formed()
    {
        var registered = PlayerFacingStrategies.Load().Select(entry => entry.StrategyId).ToHashSet();
        var problems = new List<string>();
        var seen = new HashSet<(string, string)>();

        foreach (var decision in CoverageDecisions.All)
        {
            var label = $"{ContentExpression(decision.Content)} [{string.Join(", ", decision.Strategies)}]";
            if (decision.Strategies.Count == 0)
            {
                problems.Add($"{label}: lists no strategies.");
            }

            foreach (var strategyId in decision.Strategies)
            {
                if (!registered.Contains(strategyId))
                {
                    problems.Add($"{label}: '{strategyId}' is not a player-facing strategy's stable ID.");
                }

                if (!seen.Add((decision.Content.Key, strategyId)))
                {
                    problems.Add($"{label}: '{strategyId}' already has a decision for this content.");
                }
            }

            if (decision.Reasons.Count == 0)
            {
                problems.Add($"{label}: records no reasons.");
            }

            if (!DateTime.TryParseExact(decision.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                problems.Add($"{label}: date '{decision.Date}' is not yyyy-MM-dd.");
            }

            var isBaseline = decision.Disposition == CoverageDisposition.Baseline;
            if (isBaseline != (decision.DecidedBy == CoverageDecider.BaselineImport))
            {
                problems.Add($"{label}: Baseline records, and only they, are decided by BaselineImport.");
            }

            if (isBaseline && decision.Date != CoverageBaseline.ImportDate)
            {
                problems.Add($"{label}: Baseline is only valid on the import date ({CoverageBaseline.ImportDate}); new candidates need a reviewed decision.");
            }

            if (!isBaseline && string.IsNullOrWhiteSpace(decision.Rationale))
            {
                problems.Add($"{label}: needs a rationale.");
            }

            if (decision.Disposition == CoverageDisposition.AddForEvaluation && string.IsNullOrWhiteSpace(decision.FollowUp))
            {
                problems.Add($"{label}: AddForEvaluation needs a FollowUp (Testing candidate or WORKLOG item).");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void Coverage_report_is_current()
    {
        var generated = CoverageReportWriter.Build(ContentCoverageAnalyzer.Analyze(), IndexDecisions());
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../FungusToast.Core/docs/CONTENT_COVERAGE_REPORT.md"));
        if (Environment.GetEnvironmentVariable(ReportUpdateVariable) == "1")
        {
            File.WriteAllText(path, generated);
        }

        var committed = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : string.Empty;
        Assert.True(committed == generated,
            "CONTENT_COVERAGE_REPORT.md is out of date. Regenerate it with "
            + $"`{ReportUpdateVariable}=1 dotnet test FungusToast.Core.Tests --filter ContentProfiles` and commit the result.");
    }

    /// <summary>
    /// One-time import that recorded every candidate present when the review was introduced as
    /// Baseline. It refuses to run once the baseline exists, so new candidates cannot be waved
    /// through as Baseline.
    /// </summary>
    [Fact]
    public void Baseline_import_writer()
    {
        if (Environment.GetEnvironmentVariable(BaselineWriteVariable) != "1")
        {
            return;
        }

        Assert.True(CoverageBaseline.Records.Count == 0, "The coverage baseline was already imported; it is never rewritten.");
        var sb = new StringBuilder();
        sb.Append("using FungusToast.Core.Mutations;\nusing FungusToast.Core.Mycovariants;\n\n");
        sb.Append("namespace FungusToast.Core.Tests.ContentProfiles.Coverage;\n\n");
        sb.Append("/// <summary>\n/// Generated once on the import date: every candidate match that existed when the coverage review\n");
        sb.Append("/// was introduced, recorded without review. Never add lines here. When one goes stale or is\n");
        sb.Append("/// reviewed, delete its line and add a reviewed record to CoverageDecisions.Reviewed.\n/// </summary>\n");
        sb.Append("internal static class CoverageBaseline\n{\n");
        sb.Append($"    public const string ImportDate = \"{CoverageBaseline.ImportDate}\";\n\n");
        sb.Append("    public static readonly IReadOnlyList<CoverageDecision> Records = new CoverageDecision[]\n    {\n");
        foreach (var candidate in ContentCoverageAnalyzer.Analyze().Candidates)
        {
            sb.Append($"        CoverageDecision.Baseline({ContentExpression(candidate.Item.Content)}, {StrategiesExpression(candidate)}, ")
                .Append(string.Join(", ", candidate.Reasons.Select(Quote)))
                .Append("),\n");
        }

        sb.Append("    };\n}\n");
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../ContentProfiles/Coverage/CoverageBaseline.cs"));
        File.WriteAllText(path, sb.ToString());
    }

    internal static Dictionary<(string ContentKey, string StrategyId), CoverageDecision> IndexDecisions()
    {
        var index = new Dictionary<(string, string), CoverageDecision>();
        foreach (var decision in CoverageDecisions.All)
        {
            foreach (var strategyId in decision.Strategies)
            {
                index.TryAdd((decision.Content.Key, strategyId), decision);
            }
        }

        return index;
    }

    private static string DescribeCandidate(ContentCoverageAnalyzer.Candidate candidate) =>
        $"// {candidate.Item.Name} ↔ {string.Join(", ", candidate.Strategies.Select(s => s.Label))} — score {candidate.Score}\n"
        + (candidate.Context.Count > 0 ? $"//   context: {string.Join("; ", candidate.Context)}\n" : string.Empty);

    private static string Template(ContentCoverageAnalyzer.Candidate candidate) =>
        "new CoverageDecision(\n"
        + $"    content: {ContentExpression(candidate.Item.Content)},\n"
        + $"    strategies: {StrategiesExpression(candidate)},\n"
        + "    disposition: CoverageDisposition.ConsiderLater, // AddForEvaluation | ConsiderLater | NotApplicable\n"
        + $"    reasons: new[] {{ {string.Join(", ", candidate.Reasons.Select(Quote))} }},\n"
        + "    rationale: \"\",\n"
        + $"    date: \"{DateTime.UtcNow:yyyy-MM-dd}\",\n"
        + "    decidedBy: CoverageDecider.Agent),\n";

    private static string StrategiesExpression(ContentCoverageAnalyzer.Candidate candidate) =>
        "new[] { " + string.Join(", ", candidate.Strategies.Select(s => Quote(s.StrategyId))) + " }";

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    internal static string ContentExpression(CoverageContent content)
    {
        var (type, factory) = content.IsMutation
            ? (typeof(MutationIds), "Mutation")
            : (typeof(MycovariantIds), "Mycovariant");
        var name = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(int) && (int)field.GetRawConstantValue()! == content.Id)
            .Select(field => field.Name)
            .FirstOrDefault();
        return name == null
            ? $"CoverageContent.{factory}({content.Id})"
            : $"CoverageContent.{factory}({type.Name}.{name})";
    }
}
