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
    private const string FilamentRationale =
        "Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). "
        + "Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the "
        + "2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored.";

    private const string FilamentFollowUp = "WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds";

    public static readonly IReadOnlyList<CoverageDecision> Reviewed = new CoverageDecision[]
    {
        // Filament Overdrive
        new CoverageDecision(
            content: CoverageContent.Mutation(MutationIds.FilamentOverdrive),
            strategies: new[]
            {
                "legacy.campaign.cmp-growth-putridtendrils-medium.v1",
                "legacy.proven.grow-defend-kill.v1", // Putrid Tendrils (Proven)
                "legacy.campaign.cmp-growth-wildfirebloom-medium.v1",
                "legacy.campaign.cmp-mobility-overextender-training.v1",
                "legacy.campaign.cmp-mobility-overextender-training-offset1.v1",
                "legacy.campaign.cmp-mobility-overextender-training-offset2.v1",
                "legacy.campaign.cmp-mobility-overextender-training-offset3.v1",
            },
            disposition: CoverageDisposition.AddForEvaluation,
            reasons: new[] { "amplifies: DiagonalGrowth", "same job: DiagonalGrowth (top)" },
            rationale: FilamentRationale,
            date: "2026-10-07",
            decidedBy: CoverageDecider.Jake,
            followUp: FilamentFollowUp),
        new CoverageDecision(
            content: CoverageContent.Mutation(MutationIds.FilamentOverdrive),
            strategies: new[]
            {
                "legacy.campaign.cmp-control-rebirthfurnace-medium.v1",
                "legacy.proven.anabolic-grow-catabr-putreregen.v1", // Rebirth Furnace (Proven)
                "ai.growth.verdant-reclaimer.v1",
            },
            disposition: CoverageDisposition.AddForEvaluation,
            reasons: new[] { "amplifies: DiagonalGrowth", "same job: DiagonalGrowth" },
            rationale: FilamentRationale,
            date: "2026-10-07",
            decidedBy: CoverageDecider.Jake,
            followUp: FilamentFollowUp),
        new CoverageDecision(
            content: CoverageContent.Mutation(MutationIds.FilamentOverdrive),
            strategies: new[] { "legacy.campaign.cmp-growth-verdantreclaimer-elite.v1" },
            disposition: CoverageDisposition.AddForEvaluation,
            reasons: new[] { "amplifies: DiagonalGrowth" },
            rationale: FilamentRationale,
            date: "2026-10-07",
            decidedBy: CoverageDecider.Jake,
            followUp: FilamentFollowUp),

        // Hyphal Resistance Transfer
        new CoverageDecision(
            content: CoverageContent.Mycovariant(MycovariantIds.HyphalResistanceTransferId),
            strategies: new[] { "legacy.campaign.cmp-defense-resilientshell-easy.v1" },
            disposition: CoverageDisposition.NotApplicable,
            reasons: new[] { "amplifies: ResistantCells", "fed by: needs OwnResistantCells", "same job: ResistantCells (top)" },
            rationale: "This Iron Shell is an entry-level campaign opponent kept deliberately weak to demonstrate Resistance. "
                + "The transfer would fit its theme but make it stronger than its teaching role allows (Jake).",
            date: "2026-10-07",
            decidedBy: CoverageDecider.Jake),
    };

    public static IEnumerable<CoverageDecision> All => Reviewed.Concat(CoverageBaseline.Records);
}
