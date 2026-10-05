# AI Coverage Decision Records — Proposal

Status: **Proposed 2026-10-05, awaiting Jake's approval.** Nothing here is
implemented. This is step 3 of the *content-to-profile coverage review* in
`docs/WORKLOG.md`: the record that stores one reviewed decision per plausible
strategy match, so the coverage report (step 2) can require them. Tags and
matching rules are already approved in `AI_CONTENT_TAGS.md`.

## The problem in numbers

Measured with a throwaway probe against the current roster on 2026-10-05:
74 non-Retired Proven and Campaign strategies and 61 tagged content items,
counting each Mycovariant tier family once.

| Candidate rule | Strategy × content pairs | Per item |
|---|---|---|
| Any match reason at all | 1,480 | median ~20 |
| Weighted score ≥ 2 (proposed below) | 492 | — |
| …and strategies with identical plans share one decision | **331** | **median 2**, max 23; 18 items have none |

Two conclusions shape the format:

- **New content must produce few decisions.** The weighted rule and plan
  grouping give a median of 2, which is reviewable every time.
- **Existing content cannot be backfilled up front.** 331 decisions would stall
  everything else, so the current matches enter as a one-time *Baseline* that
  can be worked down over time.

## What needs a decision

The coverage report finds, for each content item and each player-facing
strategy, the match reasons defined in `AI_CONTENT_TAGS.md`, and scores them:

| Reason | Points |
|---|---|
| Same job as the plan's top capability | 2 |
| Same job as one of the plan's next two capabilities | 1 |
| Amplifies a capability the plan has | 1 |
| Feeds the plan: creates a condition the plan **needs** | 2 |
| Fed by the plan: **needs** a condition the plan creates | 1 |
| Shared situation, or tension | 0; shown for context only |

A pair is a **candidate**, and needs a decision, at **2 points or more**.

- **Excluded:** content the strategy already uses (as a goal, a surge priority,
  or an explicit Mycovariant preference) never needs a decision.
- **Tension is context, not a candidate.** It appears beside a candidate to
  inform the call, and never creates one on its own.
- **One decision per tier family.** A Mycovariant tier family is reviewed
  once, under its tier-I id.
- **One decision per identical plan.** Strategies whose goals, surge
  priorities, and explicit preferences are identical (usually campaign
  difficulty variants) share one decision that lists all their stable IDs.

The weights and threshold live in one place in the report code, so they can be
tuned. Retuning that changes the candidate set behaves like any other change:
new candidates need decisions, and vanished ones are reported.

## The record

One record per candidate:

| Field | Required | Meaning |
|---|---|---|
| **Content** | yes | The mutation id, or the tier-I Mycovariant id for a family, as a `MutationIds` / `MycovariantIds` constant so typos fail to compile. |
| **Strategies** | yes | Stable strategy IDs covered by the decision. Stable IDs survive renames. |
| **Disposition** | yes | `AddForEvaluation`, `ConsiderLater`, `NotApplicable`, or `Baseline`. |
| **Reasons** | yes | The match reasons the decision was made against, as readable text, for example `same job: ResistantCells (top)` and `feeds: creates OwnResistantCells`. |
| **Rationale** | yes, except Baseline | One or two sentences on why. |
| **FollowUp** | `AddForEvaluation` only | Where the work is tracked: a Testing candidate name or a `WORKLOG.md` item. |
| **RevisitWhen** | optional, `ConsiderLater` | The condition that should reopen it, for example "if a Hard Resistance strategy is authored". |
| **Date** | yes | ISO date of the decision. |
| **DecidedBy** | yes | `Jake`, or `agent` when an AI session recorded it. |

### What each disposition commits to

| Disposition | Meaning | Obligation |
|---|---|---|
| `AddForEvaluation` | Worth testing in this strategy. | A Testing candidate goes through the normal evidence ladder. The player-facing strategy is never edited by this record. |
| `ConsiderLater` | Plausible, but not now. | None. A `RevisitWhen` makes it easy to find again. |
| `NotApplicable` | The apparent fit is wrong. | The rationale says why, for example "tension: removes the corpses this plan's reclamation uses". |
| `Baseline` | Present when the review was introduced; never reviewed. | None. Allowed only on the import date, so new candidates cannot be waved through as Baseline. |

## Staleness: the reasons are the fingerprint

A decision is valid only while the match it was made against still holds. The
recorded **Reasons** are compared with the report's current reasons:

| State | Detected when | Effect |
|---|---|---|
| **Missing** | A candidate has no record. | Test fails, listing it. |
| **Stale** | The record's reasons differ from the current reasons. Retagging content, re-ordering a strategy's goals, or retuning weights can each do this. | Test fails: re-decide and update the reasons. |
| **Orphaned** | A record matches no current candidate, for example because the strategy adopted the content or the tags changed. | Test fails: delete the record. Git keeps the history. |

Reason text was chosen over a hash because a reviewer can see *what changed*.
Changes that do not affect the match, such as a strategy's tuning numbers,
leave decisions valid.

## Where records live

A C# list in the test project, mirroring `StrategyMeasuredBands`:
`FungusToast.Core.Tests/ContentProfiles/CoverageDecisions.cs`, grouped by
content item.

An illustrative record (the rationale and follow-up are invented for the
example):

```csharp
new CoverageDecision(
    content: CoverageContent.Mycovariant(MycovariantIds.HyphalResistanceTransferId),
    strategies: new[] { "legacy.campaign.cmp-defense-resilientshell-easy.v1" },
    disposition: CoverageDisposition.AddForEvaluation,
    reasons: new[] { "same job: ResistantCells (top)", "amplifies: ResistantCells" },
    rationale: "Iron Shell banks Resistant cells but never spreads them; transfer turns that into a growing shell.",
    followUp: "TST_IronShell_HyphalTransfer (WORKLOG coverage item)",
    date: "2026-10-12",
    decidedBy: "agent"),
```

Why C# rather than JSON or a markdown table:

- **Compile-checked:** content references are compile-checked, and IDs survive
  renames.
- **Matches the repo:** it follows the existing `StrategyMeasuredBands`
  precedent.
- **Stays out of the game build:** it lives in the test project, so it never
  ships in the Unity Core DLL.
- **Easy for agents:** an agent adds a record with an ordinary code edit, and
  the failing test message can print the exact record to paste, with the
  current reasons filled in.

People read it through a generated **`CONTENT_COVERAGE_REPORT.md`** in
`FungusToast.Core/docs`, alongside the catalogs. It shows, per content item,
the candidates with scores and reasons, any tension, and the decision with its
rationale, plus counts of open, Baseline, and decided records. It is guarded by
the same stale-file test and `FUNGUS_UPDATE_CONTENT_CATALOG=1` switch.

## When the check runs

The record test runs with the rest of `FungusToast.Core.Tests`, so it catches
candidates from either side:

- **New or retagged content:** the author records decisions as part of the
  authoring work. `AI_STRATEGY_AUTHORING.md` already requires this.
- **Strategy changes** that create new candidates, for example a new goal
  that now needs what some content creates. The strategy author records those
  decisions in the same change.

## Decisions needed from Jake

1. **Weights and threshold.** Is a median of about 2 decisions per new item
   the right review load? The alternative is fewer, stronger matches.
2. **Baseline the 331 existing candidates?** The alternative is to review
   them before the check turns on.
3. **Should orphaned records fail the test?** The alternative is to report
   them and let them be deleted later. Failing keeps the ledger truthful; a
   warning is gentler on unrelated strategy edits.
4. **Who may decide?** The proposal lets an agent record any disposition,
   marked `DecidedBy: agent`, because `AddForEvaluation` only starts Testing
   work and never changes a player-facing strategy. The alternative is that
   `AddForEvaluation` needs your sign-off.
5. **C# in the test project?** The alternative is a JSON file.
