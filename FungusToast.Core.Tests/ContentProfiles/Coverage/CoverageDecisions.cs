using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Tests.ContentProfiles.Coverage;

/// <summary>
/// Reviewed coverage decisions: one answer per candidate match between a content item and the
/// strategies it plausibly belongs in. CoverageDecisionTests prints a ready-to-paste record for
/// every candidate that needs one. Rules and dispositions: docs/second-level/AI_COVERAGE_DECISIONS.md.
/// A stale Baseline record (CoverageBaseline.cs) is replaced by a reviewed record here.
/// </summary>
internal static class CoverageDecisions
{
    public static readonly IReadOnlyList<CoverageDecision> Reviewed = new CoverageDecision[]
    {
    };

    public static IEnumerable<CoverageDecision> All => Reviewed.Concat(CoverageBaseline.Records);
}
