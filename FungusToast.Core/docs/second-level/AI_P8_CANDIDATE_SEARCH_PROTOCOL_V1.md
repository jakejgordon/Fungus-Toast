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

### H1 bounded expansion — durability insertion

The original H1 plan is exhausted, so the next search remains on its frozen
parent and adds one mechanism-specific candidate rather than reopening the
failed order and economy sweeps. Plan `p8-hard-econ-control-v2` inserts
Homeostatic Harmony level 20 after the parent's Creeping Mold and Anabolic
Inversion goals. The player-readable hypothesis is that the MaxEconomy opening
can convert into durable board control by deliberately reducing ongoing decay
before completing its Necrosporulation and Catabolic Rebirth line. The bounded
plan emits exactly one candidate. It must still differ from the retained panel
under deterministic characterization before any smoke run is preregistered;
generation alone establishes no performance or band claim.

Static characterization accepted
`candidate.tst-balancedcontrol-maxeconomy.757f2707fe2e`
(`CAND_p8-hard-econ-control-v2_GoalInsertion_757f2707`). It raises observed
Homeostatic Harmony from 5 to 10 while retaining the parent's MaxEconomy
opening, and it has no exact raw-build match in the declared reference set.
Its nearest declared reference is the parent at raw distance `0.136`; the
candidate is therefore distinct enough to enter the staged screen.

The paired swap uses the candidate against parent
`TST_BalancedControl_MaxEconomy`, with retained Hard
`Grow>Kill>Reclaim(Econ/Reclaim)` fixed as opponent. Screening uses 160x160,
rotating slots, seed `2026091602`, and nutrient patches, Mycovariants, and
starting Adaptations disabled. Its holdout, if earned, is 180x140 with unused
seed `2026091702`. The gate is a five-game smoke, 20-game calibration, 50-game
comparison, then 100-game holdout. The comparison hypothesis is candidate-minus-
parent paired normalized board share, direction Increase, margin `+0.05`; the
calibration regression stop remains an interval whose upper bound is below
`-0.05`. Each stage must clear in order, and the later stages are forbidden
unless earned.

Both five-game smoke arms completed with checksum-valid manifests and zero
parity mismatches. Manifest comparison passed with only the declared treatment
strategy name and definition changes. The 20-game calibration is therefore
authorized in the unchanged 160x160 screening context, seed `2026091602`, with
pairing group `p8-h1-757-calibration`; comparison and holdout remain forbidden
until calibration clears its regression stop.

The 20-game calibration arms completed with checksum-valid manifests, zero
parity mismatches, and only the declared treatment differences. Candidate-minus-
parent paired normalized board share was `-0.4666` (95% CI
`-0.5980..-0.3352`). The entire interval is below the frozen `-0.05` regression
boundary, so candidate `757f2707` stops on evidence. Comparison and holdout must
not run. The result also rejects this specific Harmony-20 insertion as H1's
durability mechanism; it does not justify weakening Homeostatic Harmony itself.

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

The five-game smoke completed with zero parity mismatches. Its surge event rows
confirm the intended timing distinction: the three-round candidate activates
surges in earlier windows than the five-round parent (including rounds 23 and
27 in matched games). The checksum-valid 20-game calibration estimated
candidate-minus-parent normalized board share at `-0.0160` (95% CI
`-0.0564..+0.0244`). Its upper interval bound is not below `-0.05`, so it
clears calibration and advances to the 50-game comparison; this is not a
promotion, band, or efficacy result.

The checksum-valid 50-game comparison emitted `not_supported`: candidate-minus-
parent normalized board share was `-0.0180` (95% CI `-0.0443..+0.0083`) across
50 pairs. Combined runtime was 727.656 seconds within the 900-second budget.
It does not meet the frozen `+0.05` increase margin, so the candidate stops on
evidence and no holdout may run.

### H2 current-behavior re-evaluation

The result above remains valid for the behavior version it tested, but it no
longer settles the current H2 strategy. Board-aware surge opportunity gating
subsequently changed when planned surges fire, rank, bank, and defer, advancing
the AI definition schema to v2. The old parent definition fingerprint is
`93964c...`; the current parent fingerprint is `970e39...`. Because the changed
code directly owns the cadence treatment's mechanism, carrying the old outcome
forward would conflate two materially different surge policies.

Plan `p8-hard-surge-tempo-v2` therefore regenerates only the existing
three-round cadence treatment under the current parent. It is not a retry under
unchanged conditions and does not reuse any old outcome. The paired swap uses
current `TST_Arch06_SurgeGrowth` as control and retained Hard
`TST_BalancedControl_MaxEconomy` as fixed opponent. Screening is 160x160,
rotating slots, seed `2026091603`, with nutrient patches, Mycovariants, and
starting Adaptations disabled. Its holdout, if earned, is 180x140 with unused
seed `2026091703`. The gate remains five-game smoke, 20-game calibration,
50-game comparison, and 100-game holdout. The comparison hypothesis is paired
candidate-minus-parent normalized board share, direction Increase, margin
`+0.05`; the calibration regression stop is an interval wholly below `-0.05`.
Smoke must also reproduce a distinct observed surge-timing pattern before
calibration is authorized.

## E1 preregistered evaluation — regeneration-order candidate `ca22a36d`

The E1 plan generated eight valid candidates from
`Anabolic>Grow>CatabR>PutreRegen` after three duplicate rejections. Candidate
`candidate.anabolic-grow-catabr-putreregen.ca22a36d26d7`
(`CAND_p8-elite-large-board-v1_GoalOrderAdjacentSwap_ca22a36d`) swaps the
Catabolic Rebirth and Putrefactive Regeneration goal positions. Its
characterized raw build differs from both retained Elite representatives. The
player-readable hypothesis is that reaching the regeneration line earlier
improves resilient late-board recovery after large exchanges; it is not a claim
that the candidate is already Elite.

The paired swap uses the candidate against its parent, with retained Elite
`TST_CampaignMirror_AI13_AnabolicFirst` fixed as opponent. Screening uses the
required large-board 180x140 context, rotating slots, seed `2026091104`, and
nutrients, Mycovariants, and starting Adaptations disabled. Its holdout is a
different large-board 200x160 context with seed `2026091204`. The gate is
5-game smoke, 20-game calibration, 50-game comparison, then 100-game holdout,
with the standard paired normalized-board-share increase margin of `0.05`. A
calibration interval whose upper bound is below `-0.05` stops this candidate
without comparison or holdout.

The five-game smoke completed with zero parity mismatches. The checksum-valid
20-game large-board calibration estimated candidate-minus-parent normalized
board share at `-0.3404` (95% CI `-0.5505..-0.1303`). Its upper interval bound
is below the frozen `-0.05` regression boundary, so it stops on evidence;
comparison and holdout must not run.

### E1 remaining-field static review (2026-09-13)

The remaining seven generated E1 candidates were re-characterized against the
parent and retained Elite opponent before spending another smoke batch. Three
economy/max-tier variants exactly reproduce the parent's observed build. The
only goal-order variant with materially larger raw distance changes Mycelial
Bloom from 10 to 1 under characterization, contradicting the large-board-scaler
job. The remaining small economy/priority variants either reduce the observed
Genetic Drift total or have no named large-board mechanism beyond their changed
parameter. None supplies the required player-readable, large-board-specific
hypothesis, so none is preregistered for games.

E1's bounded single-gene plan is exhausted without a viable candidate. The next
safe action is to widen generation with an explicit, testable large-board
mechanism (for example a bounded goal-level or goal-inclusion operator), then
repeat static diversity screening before any smoke run. This is a search-space
finding, not evidence that the parent or failed candidate should be promoted.

## E1 preregistered evaluation — Bloom-20 insertion candidate `9c5157e4`

Candidate `candidate.anabolic-grow-catabr-putreregen.9c5157e4e166`
(`CAND_p8-elite-large-board-v1_GoalInsertion_9c5157e4`) inserts a Mycelial Bloom
level-20 goal directly after the Anabolic opener. Static characterization shows
Bloom 21 and Growth 26, versus the parent's Bloom 10 and Growth 16; it differs
from both retained Elite representatives. The hypothesis is explicit: early
scalable growth improves late-board territory recovery, not merely the parent's
overall score.

The paired parent/candidate swap fixes `TST_CampaignMirror_AI13_AnabolicFirst`
as opponent. Screening is 180x140, rotating slots, seed `2026091301`, with
nutrients, Mycovariants, and starting Adaptations disabled; holdout, if earned,
is 200x160 with seed `2026091401`. Gates are 5-game smoke, 20-game calibration,
50-game comparison, then 100-game holdout, with a preregistered normalized-board-
share increase margin of 0.05. Smoke control and treatment both completed with
valid resolved-manifest checksums and no integrity failure, so calibration is
now authorized.

The first manually launched calibration pair omitted `--pairing-group-id`; its
artifacts therefore contain blank pair IDs and are excluded as an integrity
failure, not used as evidence. The corrected 20-game calibration control and
treatment both completed with checksum-valid resolved manifests and shared
pairing group `p8-e1-bloom20-calibration`. Candidate-minus-parent normalized
board share is `+0.3771` (95% CI `+0.0979..+0.6564`) across 20 pairs. Its upper
bound is not below the frozen `-0.05` calibration boundary, so it clears
calibration; this is not a promotion, band, or efficacy verdict.

### E1 Bloom-20 comparison preregistration

The next stage is a 50-game-per-arm paired comparison in the unchanged frozen
screening context (180x140, seed `2026091301`, rotating slots, and nutrients,
Mycovariants, and starting Adaptations disabled), with pairing group
`p8-e1-bloom20-comparison`. The treatment target is
`candidate.anabolic-grow-catabr-putreregen.9c5157e4e166`; its control
counterpart is `legacy.proven.anabolic-grow-catabr-putreregen.v1`; the fixed
opponent remains `TST_CampaignMirror_AI13_AnabolicFirst`. The preregistered
hypothesis ID is `p8-e1-bloom20-comparison`: paired mean difference in
normalized board share, direction Increase, margin `+0.05`. A checksum-valid
`supported` verdict is required to earn the distinct-seed, 200x160 holdout;
any other verdict stops this candidate without holdout.

The comparison control and treatment completed with checksum-valid manifests.
Its preregistered verdict is `supported`: candidate-minus-parent paired
normalized board share is `+0.3513` (95% CI `+0.1634..+0.5391`) across 50
pairs. Combined runtime is 647.956 seconds within the 900-second budget. This
earns the holdout; it is still not a player-facing promotion or measured Elite
band.

### E1 Bloom-20 holdout preregistration

The final test is a 100-game-per-arm paired holdout on the already declared
different large-board context: 200x160, seed `2026091401`, rotating slots,
and nutrients, Mycovariants, and starting Adaptations disabled. Its pairing
group and hypothesis ID are both `p8-e1-bloom20-holdout`; target, control,
fixed opponent, primary metric, Increase direction, and `+0.05` margin remain
unchanged. Only a checksum-valid `supported` verdict can advance this candidate
to contextual classification; otherwise it stops without promotion.

That first holdout control is checksum-valid but incomplete: it reached 97 of
100 games and the runner recorded `interrupted` at its 900-second cap. It is
an integrity artifact only; no treatment or outcome analysis is permitted.
The observed execution pace therefore invalidates the original runtime ceiling,
not the candidate. A fresh replacement holdout is preregistered with the same
200x160 context but unused seed `2026091501`, pairing group and hypothesis ID
`p8-e1-bloom20-holdout2`, and a 2100-second combined-stage runtime budget. The
increased budget is set solely from the incomplete control's 97 games in 900
seconds plus a conservative paired-run allowance; it is not based on outcomes.
All other target, control, opponent, metric, direction, and margin fields are
unchanged. The incomplete artifact remains excluded.

The replacement control and treatment both completed with checksum-valid
manifests. The holdout verdict is `supported`: candidate-minus-parent paired
normalized board share is `+0.5494` (95% CI `+0.4484..+0.6503`) across 100
pairs. Combined runtime is 1757.365 seconds within the preregistered
2100-second budget. The candidate has earned contextual classification; this
does not itself assign a player-facing pool or difficulty band.

### E1 Bloom-20 P7-compatible contextual classification

The candidate was measured alone against the frozen 19-strategy P7 Proven
panel; this is not a recalibration or reclassification of the references. The
first manually launched duel-small artifact omitted `--per-game-lineups` and
therefore sampled only a fixed two-strategy lineup. It is preserved but excluded
from classification. All seven corrected contexts used `RandomUnique`, rotating
slots, per-game lineups, systems off, checksum-valid manifests, and the frozen
P7 seeds/geometries.

Across the five calibration contexts, Bloom-20 measured normalized-board-share
intervals of 1.361–1.573 (30 games), 1.538–1.779 (24), 1.860–2.163 (53),
2.162–2.636 (47), and 1.929–2.605 (29). The 24-game duel-medium cell is
reported as `TooFewGames` by the frozen 25-game floor, consistent with P7;
it remains in the conservative pooled evidence. The pooled conservative lower
bound clears the frozen Elite threshold of 1.25, so
`CAND_p8-bloom20-contextual-v1_GoalInsertion_9c5157e4` classifies **Elite**
under `fungus-toast.ai-bands.v1`. Both untouched holdouts also clear Elite:
duel-wide 1.573–1.790 (26 games) and smalltable-tall 1.574–1.813 (44 games).
This is a measured contextual band, not a player-facing roster or campaign
promotion decision.
