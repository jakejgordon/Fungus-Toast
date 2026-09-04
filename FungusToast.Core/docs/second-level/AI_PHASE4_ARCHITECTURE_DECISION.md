# Phase 4 AI Architecture Decision

## Verdict: INVALIDATED

**Question:** Does a fully composed AI controller materially outperform a
minimal cleanup of `ParameterizedSpendingStrategy` for three concrete missing
behaviors, without changing disabled behavior or exceeding the preregistered
runtime envelope?

**Decision:** No. Retain and revise the parameterized design. Add narrow typed
interfaces only when they remove a demonstrated correctness cliff, as the
mycovariant draft interface already does. Do not build the proposed general
controller/context/action/policy framework now.

This decision applies to the current backlog and evidence. It does not prohibit
reopening composition if future real behaviors cannot be expressed cleanly.

## Frozen test

The inputs and gate were committed in
`AI_ARCHITECTURE_BALANCE_OVERHAUL.md` before either prototype was written. The
throwaway implementation lived under ignored
`TEMP/phase4-ai-architecture-bakeoff/` and was not merged into production.

Both designs implemented the same three behaviors:

- B1: below-0.75 normalized-share defensive fallback after round 10;
- B2: below-parity or at-least-65%-occupancy scheduled surge window, while
  retaining a separately expressible catch-up surge;
- B3: owned-mutation-category mycovariant synergy bonus with stable ID
  tie-breaking.

The runnable harness tested the declared boundary cases, 1,000 generated legacy
decision fixtures with all new behaviors disabled, source-derived logical-line
and touched-file metrics, and five timed samples of 1,000,000 mixed decisions
per design after warm-up.

## Evidence

Both prototypes passed every B1–B3 acceptance case and matched all 1,000
disabled-behavior legacy fixtures.

| Behavior | Revised parameterized logical lines / files | Composed logical lines / files | Difference |
|---|---:|---:|---|
| B1 trailing defense | 7 / 2 | 16 / 1 | parameterized used 56.3% fewer lines |
| B2 surge window | 5 / 2 | 9 / 1 | parameterized used 44.4% fewer lines |
| B3 draft synergy | 9 / 2 | 10 / 1 | parameterized used 10.0% fewer lines |

Composition kept behavior implementations out of a shared decision-controller
path; the parameterized prototype modified a shared controller and options
record. That isolation advantage is real. It did not outweigh the frozen
authoring-cost and runtime gates for this backlog.

Five fresh-process benchmark runs produced these median results:

| Run | Parameterized | Composed | Composed slowdown |
|---|---:|---:|---:|
| 1 | 85.494 ms | 103.819 ms | 21.43% |
| 2 | 79.851 ms | 99.837 ms | 25.03% |
| 3 | 81.212 ms | 100.757 ms | 24.07% |
| 4 | 81.327 ms | 100.325 ms | 23.36% |
| 5 | 82.820 ms | 102.049 ms | 23.22% |

Every run exceeded the preregistered 5% runtime envelope. The revised
parameterized design also met the symmetric decisive authoring-cost rule by
using at least 20% fewer behavior lines for B1 and B2.

## What worked

- Immutable decision inputs made both designs easy to test.
- Composition isolated each behavior in its own implementation and made the
  policy seam explicit.
- The revised parameterized approach expressed all three behaviors without a
  new catch-all framework and with lower implementation volume.
- Disabled behavior was exactly reproducible in both prototypes.

## What failed or surprised us

- Interface dispatch and extra policy objects imposed a consistent 21–25%
  microbenchmark cost in this decision-heavy fixture, not a marginal cost.
- Composition needed substantially more logic for the two simplest behaviors.
- The parameterized design's shared-path coupling remains a maintainability
  weakness. It should be controlled with focused helpers and characterization
  tests, not used as justification for an otherwise unproven framework.

## Production consequence

1. Keep `ParameterizedSpendingStrategy` as the current mutation/surge owner.
2. Add immutable focused inputs or pure helper methods for approved reactive
   behavior instead of a general policy graph.
3. Keep mycovariant selection on the required strategy interface; consider a
   separate policy only if multiple real implementations need independent
   composition.
4. Make traces opt-in and sampled. Do not add a universal action/result layer.
5. Implement none of B1–B3 merely because they were useful spike fixtures.
   Each still needs product approval and a paired, preregistered behavioral
   experiment before roster behavior changes.
6. Reopen this decision only when at least three real pending behaviors fail
   the smaller design's expressibility or isolation constraints.
