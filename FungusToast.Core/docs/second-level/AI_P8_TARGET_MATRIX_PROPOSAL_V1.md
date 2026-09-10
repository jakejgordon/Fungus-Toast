# AI P8 Target Roster Matrix Proposal V1

**Status:** proposed policy; no strategy, pool, campaign preset, or balance value
changes in this document. It defines the decision surface P8.3–P8.5 must satisfy
if approved.

## Scope and evidence boundary

This proposal covers the 19-strategy `Proven` solo-player panel. Its evidence is:

- [P7 reference bands](AI_P7_REFERENCE_BANDS_V1.md): 1,600 frozen-context games
  place every member with sufficient overall evidence.
- [P8 observed behavior](AI_P8_ROSTER_BEHAVIOR_V1.md): the same panel produces 15
  distinct raw mutation builds under the deterministic characterization script.

Four pairs have identical scripted builds, but each pair has separate behavioral
fingerprints and different P7 aggregate shares. They are review candidates, not
retirement decisions. There is no matched opponent-pair matrix, so this proposal
makes no claim about counters.

## Proposed solo target

Cap the active solo pool at **15 behaviorally distinct strategies**, split as:

| Measured band | Target | Player-facing jobs | Current distinct builds | Gap |
|---|---:|---|---:|---:|
| Easy | 4 | onboarding baseline; resilience/territory learner; readable pressure line; economy learner | 7 | -3 |
| Normal | 4 | generalist baseline; defense; reclamation; flexible control | 4 | 0 |
| Hard | 4 | economy/control; reclaim pressure; surge tempo; contextual specialist | 2 | +2 |
| Elite | 3 | apex generalist; large-board scaler; crowded-table/boss specialist | 2 | +1 |
| **Total** | **15** | a progression with multiple readable identities at every step | **15** | **0** |

The count is deliberately small. Fifteen is the current number of distinct
observed builds after collapsing only exact scripted matches, so it is a stable
maintenance ceiling rather than an invented expansion target. Four examples per
non-Elite band give players variety without making a strength band indistinct;
three Elite opponents preserve a special endgame tier without turning every late
game into the same apex controller.

The distribution, not the current entries, is the decision. Current evidence is
bottom-heavy (7 Easy, 4 Normal, 2 Hard, 2 Elite distinct builds). P8 must move
three weak identities out of the active progression only when a replacement or
an independently valuable contextual role is evidenced, and must create or
promote two Hard and one Elite identities. Nothing in this policy permits
relabeling a strategy upward without new measured evidence.

## Required differentiation

Every retained slot must state one primary job and one observable differentiator:

| Differentiator | Evidence required |
|---|---|
| Build identity | Raw mutation build differs from every retained peer, or a documented reactive/draft/surge distinction survives a controlled behavior test. |
| Strength slot | P7-style calibration and holdout evidence places the strategy in the stated measured band. |
| Context niche | A material-context result is named and reproduced; otherwise the strategy is a generalist. |
| Counter claim | A preregistered matched matchup comparison, 50 games at comparison and 100 games at holdout, supports the claimed direction. |
| Player value | The friendly name, intent, and authored role explain a choice the player can perceive; implementation parameters alone do not qualify. |

An exact raw-build match may remain only if its non-spending distinction clears the
same evidence bar. An authored `CounterTag`, theme, power tier, or difficulty
label is not that evidence.

## Phase handoff

P8.3 should inventory the four exact-build pairs and the three Easy excess slots,
then choose **retain, merge, retire, or replace** only with the differentiation
evidence above. P8.4 should search specifically for the two Hard and one Elite
gaps, constraining candidate diversity against the retained fifteen rather than
against the old 19-name roster. P8.5 should promote only after calibration,
holdout, identity, and player-comprehensibility gates all pass.
