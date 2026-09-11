# AI P8 Candidate Search Protocol V1

**Status:** proposed search protocol; no candidate, strategy, pool, campaign
preset, or balance value changes in this document.

## Purpose and prerequisite

P8.2 established a solo target of four Easy, four Normal, four Hard, and three
Elite behaviorally distinct strategies. The frozen P7 panel currently supplies
two distinct Hard builds and two distinct Elite builds, leaving two Hard search
lanes and one Elite search lane.

The candidate generator can make a bounded, reproducible mutation around one
parameterized parent and rejects a build already registered in the roster. That
is necessary but insufficient for P8: the policy requires diversity against the
**retained fifteen**, not merely against the present nineteen-name roster.

P8.3 has completed its four exploratory exact-build screens, but it has not made
a retain, merge, retire, or replace decision. Therefore no final retained-fifteen
reference panel exists yet. This protocol deliberately does not run or promote a
candidate before that panel is recorded. A candidate produced sooner is useful
only as a static/characterization observation; it cannot fill a matrix slot.

## Search lanes

Each lane is a job hypothesis, not a promised difficulty label. The indicated
parent is a source of bounded single-gene proposals; passing a generated
candidate through the evaluation ladder is still required before it receives a
measured band.

| Lane | Target slot | Player-facing job | Initial parent family | Allowed initial operators | Required observable distinction |
|---|---|---|---|---|---|
| H1 | Hard economy/control | Convert an economy opening into durable board control without becoming another MaxEconomy mirror. | `TST_BalancedControl_MaxEconomy` | goal-order adjacent swap; goal-order promotion; economy-bias sweep; high-tier toggle | A raw characterization build that differs from every retained Hard economy/control peer, plus a readable opening-order or resource-policy difference. |
| H2 | Hard surge tempo | Apply pressure through a timed surge line rather than reclamation or static economy control. | `Best_MaxEcon_Surge10_HyphalSurge` or the strongest surviving surge-tempo parent after the retained panel is frozen | surge-attempt-frequency sweep; goal-order swap/promotion; high-tier toggle | A raw build and observed surge timing distinct from every retained Hard peer; it must not be called a counter without a matched matchup result. |
| E1 | Elite large-board scaler | Convert a strong generalist into a robust late-board scaler without duplicating either AnabolicFirst mirror. | `Anabolic>Grow>CatabR>PutreRegen` | goal-order swap/promotion; economy-bias sweep; high-tier toggle; max-tier sweep | A raw build distinct from every retained Elite peer and an explicit large-board performance hypothesis. |

The parent choices come from measured P7 evidence, not authored labels:
BalancedControl MaxEconomy measured Hard overall; the surge parent is deliberately
only a source family because it currently measures Easy; and
`Anabolic>Grow>CatabR>PutreRegen` measured Elite overall with large-board Elite
contexts. The parent never confers its band to its child.

## Frozen candidate-screen rules

Before generation, each concrete plan must record:

1. Its `CandidateGenerationPlan` JSON, including the parent stable ID, operator
   list, value lists, and maximum proposal count.
2. The retained-fifteen reference list and the exact behavior-report version
   used for static diversity screening.
3. One named lane from the table, one primary player-facing job, and one
   observable differentiator. “Higher difficulty,” an authored archetype tag, or
   a parameter change alone is not a differentiator.
4. A preregistered staged evaluation plan: smoke, calibration, 50-game paired
   comparison, then a 100-game holdout whose seed and geometry both differ from
   the screen. The comparison/holdout question must state a primary metric,
   direction, and margin before execution.
5. For E1, at least one large-board context in screening and a different
   large-board holdout. For H2, record observed surge attempts as well as final
   board-share metrics. For H1, record the observed opening purchases and
   economy policy.

Generation rejects known registry duplicates, but P8 static screening must also
reject a candidate whose characterized raw mutation build is identical to a
retained peer. Near matches require a written distinction and the same
calibration/holdout evidence as any other candidate. No fixed cosine cutoff is
introduced here: the existing report makes raw distance an investigative signal,
not a promotion verdict.

## Promotion boundary

A lane remains open until a candidate both clears the staged evidence ladder and
has a recorded player-comprehensible identity. Passing against its parent alone
does not establish a Hard or Elite placement; P7-style contextual calibration
must place it in the requested band. The three Easy excess slots remain unchanged
until a separately evidenced replacement or contextual role makes a roster action
appropriate.

## Provisional retained-panel reference (2026-09-10)

The panel below is an evidence-backed **search reference**, not a player-facing
pool change. It retains the strongest four Easy measured identities and one
representative for each exact-build group. Their unresolved counterparts stay in
the roster and remain eligible for P8.3 holdout review; this only prevents a
candidate search from treating them as independent diversity targets.

| Measured band | Reference identities | Rationale |
|---|---|---|
| Easy | 'TST_AnabolicCreepingNecroRegressionCascade', 'TST_CampaignPlayer_SafeBaseline', 'Grow>Mutate>Kill(Max Econ)', 'TST_CreepingNecroRegressionCascade' | These are the four highest pooled Easy shares: 0.705, 0.620, 0.492, and 0.490. |
| Normal | 'Grow>Defend>Kill', 'Filament Regrowth', 'Mutate>Grow>Kill(Max Econ)', 'Power Mutations Max Econ' | Filament Regrowth (1.058) is the provisional representative of its exact-build pair over Creeping>Necrosporulation (0.965). |
| Hard | 'Grow>Kill>Reclaim(Econ/Reclaim)', 'TST_BalancedControl_MaxEconomy' | Reclaim(Econ/Reclaim) (1.373) and BalancedControl MaxEconomy (1.347) provisionally represent their near-even exact-build pairs. |
| Elite | 'TST_CampaignMirror_AI13_AnabolicFirst', 'Anabolic>Grow>CatabR>PutreRegen' | CampaignMirror AnabolicFirst (1.915) is the provisional representative of the AnabolicFirst pair; the latter is the independent measured Elite line. |
| Open slots | H1, H2, E1 | These two Hard and one Elite slots are the only targets for new candidate search. |

The three Easy identities excluded from active-progression planning are
'TST_AnabolicBeaconNecroRegressionCascade' (0.366),
'Growth/Resilience' (0.333), and 'Best_MaxEcon_Surge10_HyphalSurge' (0.153).
They are not deleted or relabeled by this decision. The last line is specifically
not an H2 parent: P7's causal checks show its Tier-cap and timing changes did not
repair its structural lack of growth, so using it as the surge-tempo seed would
repeat a disproven branch.

## Immediate handoff

Three bounded plans now seed H1, H2, and E1. They produce candidates only in the
generated testing catalog; all remain outside player-facing pools until they
clear the evidence gates above.

Materialize a plan without hand-editing a catalog:

```bash
dotnet run --project FungusToast.Simulation -- \
  --generate-candidate-catalog FungusToast.Simulation/Examples/candidate-plan.p8-hard-economy-control.v1.json \
  --write-candidate-catalog /tmp/p8-h1.catalog.json \
  --candidate-reference 'Proven:Grow>Kill>Reclaim(Econ/Reclaim)'
```

The parent is always included as a reference. Repeat `--candidate-reference`
for each authored opponent required by a later paired evaluation. The command
prints every rejected proposal and writes only the accepted candidates plus the
declared references; it does not change any authored strategy set or pool.

## H1 preregistered evaluation — opening-order candidate `959c5428`

The first H1 static screen is frozen before game execution. It generated five
novel candidates from `TST_BalancedControl_MaxEconomy`; three reproduce the
parent's deterministic observed build and are not evaluated. Candidate
`candidate.tst-balancedcontrol-maxeconomy.959c542898cc` (`CAND_p8-hard-econ-control-v1_GoalOrderAdjacentSwap_959c5428`)
is the selected screen candidate. It swaps one adjacent build-goal pair,
produces an observed build distinct from both retained Hard representatives,
and supplies the required readable opening-order distinction. Its near match to
the retained Normal `Power Mutations Max Econ` is a review signal, not a
duplicate: its raw distance is `0.003`, while its build is not identical.

The paired swap uses the candidate against parent
`TST_BalancedControl_MaxEconomy`, with fixed opponent
`Grow>Kill>Reclaim(Econ/Reclaim)`. Nutrient patches, Mycovariants, and starting
Adaptations are disabled; slots rotate. The screening context is 160x160 with
base seed `2026091101`; the holdout is 180x140 with base seed `2026091201`.
The holdout changes both geometry and seed as required. The stages are 5-game
smoke, 20-game calibration, a 50-game comparison, and a 100-game holdout,
with the standard paired normalized-board-share increase margin of `0.05`.
The candidate must clear each stage in order. This evaluation tests an
opening-order candidate against its parent; it does not by itself award a Hard
band or promote the candidate into any player-facing pool.

## H1 preregistered evaluation — resource-policy candidate `f2ae5a58`

After the opening-order candidate stopped at calibration, the only remaining H1
proposal with an observed raw build distinct from the parent is
`candidate.tst-balancedcontrol-maxeconomy.f2ae5a58b951`
(`CAND_p8-hard-econ-control-v1_EconomyBiasSweep_f2ae5a58`). It changes the
parent's economy policy to `IgnoreEconomy`; that is an explicit resource-policy
distinction, not a claim that the candidate is already Hard or desirable.

This independent paired swap uses the same parent and fixed opponent as the
first H1 treatment. Screening uses 160x160, rotating slots, seed `2026091102`,
and nutrients, Mycovariants, and starting Adaptations disabled. Its holdout is
180x140 with seed `2026091202`. The gate remains 5-game smoke, 20-game
calibration, 50-game comparison, then 100-game holdout, with paired normalized
board-share increase margin `0.05`. A calibration interval whose upper bound is
below `-0.05` stops this candidate without comparison or holdout.

The five-game smoke completed with zero parity mismatches. The 20-game
calibration completed with checksum-valid control and treatment artifacts. The
candidate's paired normalized-board-share difference was `-0.1075` (95% CI
`-0.2727..+0.0577`). Its upper interval bound is not below the frozen `-0.05`
regression boundary, so it clears calibration and advances to the 50-game
comparison. This is not a promotion, band, or efficacy result.

The 50-game comparison completed with checksum-valid artifacts and emitted the
preregistered verdict `not_supported`: candidate-minus-parent normalized board
share was `-0.1613` (95% CI `-0.2726..-0.0500`) across 50 pairs. The combined
runtime was 635.866 seconds within the 900-second budget. This stops the
candidate on evidence; no holdout may run.

## H2 preregistered evaluation — surge-window candidate `dcb18b64`

The H2 plan generated seven valid candidates from
`TST_Arch06_SurgeGrowth` after two duplicate rejections. The generated-catalog
behavior report confirms that its timing-sweep candidates share the parent's
deterministic mutation build but do not duplicate either retained Hard
representative; this is expected because the bounded treatment changes a
timing gene, not its acquisition plan. Candidate
`candidate.tst-arch06-surgegrowth.dcb18b642321`
(`CAND_p8-hard-surge-tempo-v1_SurgeAttemptTurnFrequencySweep_dcb18b64`) is
selected because it changes the parent from a five-round to a three-round
surge-attempt cadence: the required player-readable distinction is earlier,
more frequent surge windows. The screen records surge upgrade-event rounds as
well as outcome metrics.

The paired swap uses the candidate against parent `TST_Arch06_SurgeGrowth`,
with retained Hard `TST_BalancedControl_MaxEconomy` fixed as opponent. Screening
uses 160x160, rotating slots, seed `2026091103`, and nutrients, Mycovariants,
and starting Adaptations disabled. Its holdout is 180x140 with seed
`2026091203`. The gate is 5-game smoke, 20-game calibration, 50-game
comparison, then 100-game holdout, with the standard paired normalized-board-
share increase margin of `0.05`. A calibration interval whose upper bound is
below `-0.05` stops this candidate without comparison or holdout.
