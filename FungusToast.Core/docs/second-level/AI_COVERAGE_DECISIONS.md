# AI Coverage Decisions

**In one sentence:** whenever a mutation or Mycovariant could plausibly belong
in an AI strategy that does not use it yet, someone records a short answer:
*try it*, *maybe later*, or *no, and here is why*.

Approved 2026-10-05 (Jake). This is the decision half of the
content-to-profile coverage review described in `AI_STRATEGY_AUTHORING.md`.
The tags it reads, and the matching rules, are in
[AI_CONTENT_TAGS.md](AI_CONTENT_TAGS.md).

## Where things live

| What | Where |
|---|---|
| Matching engine | `FungusToast.Core.Tests/ContentProfiles/Coverage/ContentCoverageAnalyzer.cs` (weights and threshold at the top) |
| Reviewed decisions | `FungusToast.Core.Tests/ContentProfiles/Coverage/CoverageDecisions.cs` |
| One-time baseline | `FungusToast.Core.Tests/ContentProfiles/Coverage/CoverageBaseline.cs` |
| Rules (tests) | `FungusToast.Core.Tests/ContentProfiles/Coverage/CoverageDecisionTests.cs` |
| Readable view | generated [CONTENT_COVERAGE_REPORT.md](../CONTENT_COVERAGE_REPORT.md) |

Everything lives in the test project, so none of it ships in the Unity Core DLL.

## What becomes a question (a "candidate")

The review compares every tagged content item with every non-Retired,
parameterized strategy in the player-facing Proven and Campaign sets, and
scores the match:

| Reason | Points |
|---|---|
| Same job as the plan's top capability | 2 |
| Same job as one of the plan's next two capabilities | 1 |
| Amplifies a capability the plan has | 1 |
| Feeds the plan: creates a condition the plan **needs** | 2 |
| Fed by the plan: **needs** a condition the plan creates | 1 |
| Shared situation, or tension | 0; shown as context only |

A match worth **2 points or more** is a candidate and needs a decision.

- **Already in the plan:** content the strategy already uses (as a goal, a
  surge priority, or an explicit Mycovariant preference) never needs one.
- **Tier families:** a Mycovariant tier family is one item, keyed by its
  tier-I id.
- **Identical plans:** strategies whose goals, surge priorities, and explicit
  preferences are identical, usually campaign difficulty variants, share one
  question.
- **How a plan is weighted:** earlier goals count more, and a bridge goal
  (bought only to the level a later goal requires) barely counts.
  Category-derived Mycovariant sets do not count.

When introduced on 2026-10-05, this produced 335 candidates over 61 items:
about 2 per item on average, though a few popular items had many more.

## The decision record

```csharp
new CoverageDecision(
    content: CoverageContent.Mycovariant(MycovariantIds.HyphalResistanceTransferId),
    strategies: new[] { "legacy.campaign.cmp-defense-resilientshell-easy.v1" },
    disposition: CoverageDisposition.AddForEvaluation,
    reasons: new[] { "amplifies: ResistantCells", "same job: ResistantCells (top)" },
    rationale: "Iron Shell banks Resistant cells but never spreads them.",
    date: "2026-10-12",
    decidedBy: CoverageDecider.Agent,
    followUp: "TST_IronShell_HyphalTransfer"),
```

| Field | Required | Meaning |
|---|---|---|
| `content` | yes | `CoverageContent.Mutation(MutationIds.X)` or `CoverageContent.Mycovariant(MycovariantIds.X)`, using the tier-I id for a family. |
| `strategies` | yes | Stable strategy IDs the decision covers. They survive renames. |
| `disposition` | yes | See the next table. |
| `reasons` | yes | The match reasons at decision time, copied from the test output. |
| `rationale` | yes, except Baseline | One or two sentences on why. |
| `date` | yes | `yyyy-MM-dd`. |
| `decidedBy` | yes | `Jake` or `Agent`. `BaselineImport` is reserved for the baseline. |
| `followUp` | `AddForEvaluation` only | The Testing candidate name or `WORKLOG.md` item tracking the work. |
| `revisitWhen` | optional | For `ConsiderLater`: what should reopen it. |

| Disposition | Meaning | What it commits to |
|---|---|---|
| `AddForEvaluation` | Worth testing in this strategy. | A Testing candidate goes through the normal evidence ladder. The player-facing strategy is never edited by the record, and changes only with Jake's approval. |
| `ConsiderLater` | Plausible, but not now. | Nothing. |
| `NotApplicable` | The apparent fit is wrong. | The rationale says why. Tension shown in the report is often the reason. |
| `Baseline` | Existed on 2026-10-05; never reviewed. | Nothing. It cannot be added after the import date. |

An AI session may record any disposition, marked `decidedBy: CoverageDecider.Agent`.

## How to record decisions

1. Run `dotnet test FungusToast.Core.Tests --filter CoverageDecisionTests`.
2. For each missing or stale candidate, the failure message prints a
   ready-to-paste record with the strategies and current reasons filled in,
   plus any tension or shared-situation context.
3. Paste it into `CoverageDecisions.Reviewed`, choose the disposition, and
   write the rationale, adding a `followUp` for `AddForEvaluation`.
4. Regenerate the report:

   ```bash
   FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentProfiles
   ```

## When the tests fail

| Failure | Cause | Fix |
|---|---|---|
| **Missing** | A candidate has no record: new content, retagged content, a strategy change, or retuned weights. | Record a decision. |
| **Stale** | The recorded reasons no longer match the current ones. | Re-decide and record the current reasons. A stale Baseline line is deleted from `CoverageBaseline.cs` and replaced by a reviewed record. |
| **Orphaned** | A record no longer matches any candidate, for example because the strategy now uses the content. | Remove the strategy ID, or delete the record. Git keeps the history. |
| **Malformed** | A missing rationale or follow-up, an unknown stable ID, a duplicate, or a late Baseline. | Correct the record. |

The reasons double as the fingerprint, so changes that do not affect a match,
such as a strategy's tuning numbers, never invalidate a decision.

## Working down the baseline

Baseline records are the backlog of never-reviewed matches. Review them a
content item at a time from the report. Replace each Baseline line with a
reviewed record in `CoverageDecisions.Reviewed`, with the same reasons, and
delete the Baseline line. `CoverageBaseline.cs` is never regenerated: its
writer refuses to run once the baseline exists.
