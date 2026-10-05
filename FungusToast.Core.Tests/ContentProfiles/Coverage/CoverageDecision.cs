namespace FungusToast.Core.Tests.ContentProfiles.Coverage;

/// <summary>What the reviewer decided about one content item for one or more strategies.</summary>
public enum CoverageDisposition
{
    /// <summary>Worth testing: a Testing candidate goes through the evidence ladder. The strategy itself is never edited by the record.</summary>
    AddForEvaluation,

    /// <summary>Plausible, but not now. RevisitWhen says what should reopen it.</summary>
    ConsiderLater,

    /// <summary>The apparent fit is wrong; the rationale says why.</summary>
    NotApplicable,

    /// <summary>Present when the review was introduced and never reviewed. Only valid on the import date.</summary>
    Baseline,
}

public enum CoverageDecider
{
    Jake,
    Agent,
    BaselineImport,
}

/// <summary>A mutation, or a Mycovariant tier family keyed by its tier-I id.</summary>
public readonly record struct CoverageContent(bool IsMutation, int Id)
{
    public static CoverageContent Mutation(int mutationId) => new(true, mutationId);

    public static CoverageContent Mycovariant(int mycovariantId) => new(false, mycovariantId);

    public string Key => IsMutation ? $"mutation:{Id}" : $"mycovariant:{Id}";
}

/// <summary>
/// One reviewed answer to "should these strategies use this content?". The recorded reasons
/// are the match the decision was made against; when the current match reasons differ, the
/// decision is stale and must be made again. Rules: docs/second-level/AI_COVERAGE_DECISIONS.md.
/// </summary>
public sealed class CoverageDecision
{
    public CoverageDecision(
        CoverageContent content,
        string[] strategies,
        CoverageDisposition disposition,
        string[] reasons,
        string rationale,
        string date,
        CoverageDecider decidedBy,
        string? followUp = null,
        string? revisitWhen = null)
    {
        Content = content;
        Strategies = strategies;
        Disposition = disposition;
        Reasons = reasons;
        Rationale = rationale;
        Date = date;
        DecidedBy = decidedBy;
        FollowUp = followUp;
        RevisitWhen = revisitWhen;
    }

    public CoverageContent Content { get; }

    /// <summary>Stable strategy IDs the decision covers.</summary>
    public IReadOnlyList<string> Strategies { get; }

    public CoverageDisposition Disposition { get; }

    /// <summary>The scoring reasons at decision time, for example "same job: ResistantCells (top)".</summary>
    public IReadOnlyList<string> Reasons { get; }

    public string Rationale { get; }

    /// <summary>ISO date (yyyy-MM-dd).</summary>
    public string Date { get; }

    public CoverageDecider DecidedBy { get; }

    /// <summary>Required for AddForEvaluation: a Testing candidate name or WORKLOG item.</summary>
    public string? FollowUp { get; }

    public string? RevisitWhen { get; }

    /// <summary>A match recorded by the one-time import, never reviewed.</summary>
    public static CoverageDecision Baseline(CoverageContent content, string[] strategies, params string[] reasons) =>
        new(content, strategies, CoverageDisposition.Baseline, reasons, string.Empty, CoverageBaseline.ImportDate, CoverageDecider.BaselineImport);
}
