# Dominance Diagnosis Workflow

Use this workflow when a simulation batch suggests that a strategy may be statistically dominant and the goal is to understand **why**.

This is a controlled-experiment process, not just a high-volume simulation process.

## Goal

For a suspicious strategy, determine whether repeated wins are primarily driven by:
- early mutation timing,
- late-game mutation ceiling,
- mycovariant timing or alignment,
- synergy between specific mutations,
- economy pacing,
- or lineup/board-context effects.

## Core Principle

Change **one variable at a time**.

If lineup, board, seed, slot policy, and gameplay constants all change together, the run is not useful for diagnosis.

## Two-Stage Pattern

### 1) Screening runs
Use screening runs to identify suspicious strategies.

Good for:
- finding dominant strategies,
- finding weak strategies,
- finding distorted rosters.

Typical setup:
- `Testing` or `Proven`
- `100-300` games
- seeded
- fixed board size
- `CoverageBalanced` or explicit lineup

### 2) Controlled diagnosis runs
Use controlled runs to explain the result.

Good for:
- isolating causes,
- comparing before/after tuning passes,
- validating whether a change actually fixed the right problem.

Typical setup:
- explicit `--strategy-names`
- fixed seed
- `--rotate-slots`
- fixed board size
- explicit `--experiment-id`

## Recommended Controlled Diagnosis Workflow

### Step 1) Establish a fixed baseline lineup
Create one explicit lineup containing:
- the suspect dominant strategy,
- `2-4` anchor comparison strategies,
- optionally one softer/generalist strategy for contrast.

Use the same lineup for all comparisons in the diagnosis pass.

### Step 2) Run and save the baseline
Run the baseline with:
- fixed seed,
- fixed board size,
- fixed slot policy,
- explicit experiment ID,
- parquet export enabled.

Keep `manifest.json` with the analysis outputs.

### Step 3) Form a causal hypothesis
Before changing anything, write down the suspected cause.

Examples:
- Strategy wins because Mutation X is acquired too early.
- Strategy wins because Tier 4/5 scaling is too strong.
- Strategy wins only when it gets a specific mycovariant early.
- Strategy wins because the mutation point economy is too generous.

### Step 4) Inspect the most useful evidence
Prioritize these files:

1. `upgrade_events.parquet`
   - first upgrades
   - order of upgrades
   - round when key mutations appear
   - points spent / points banked

2. `mutations.parquet`
   - end-state mutation profile
   - final levels
   - which paths actually complete

3. `mycovariants.parquet`
   - mycovariant usage and end-state ownership
   - repeated pairings with strong strategies

4. `players.parquet`
   - win rate
   - average living cells
   - average toxins
   - opponent theme context

5. `manifest.json`
   - exact lineup
   - seed
   - selection source
   - policy

## Mycovariant Timing

Do not only ask **which** mycovariants appear. Also ask **when** they matter.

A mycovariant may be:
- extremely strong early and weak late,
- weak early and decisive late,
- only strong when paired with a specific early mutation path.

When diagnosing dominance, compare:
1. early mutation sequence,
2. mycovariant acquisition/use pattern,
3. whether the strategy wins mainly when a specific mycovariant shows up in time.

If a strategy is only dominant when a mycovariant arrives early enough, that is different from a mutation path being intrinsically overpowered.

## Single-Variable Tuning Rule

When moving from diagnosis to tuning, change only one main lever:
- one mutation cost,
- one mutation effect magnitude,
- one prerequisite,
- one max-level/scaling value,
- one economy pacing knob,
- or one mycovariant tuning value.

Do **not** change multiple gameplay levers in the same diagnosis pass.

## Confirmation Pattern

After a tuning change:
1. rerun the exact same baseline lineup,
2. keep the same seed and slot policy,
3. compare against the baseline,
4. then run one alternate condition:
   - a second seed, or
   - a second board geometry.

A change should survive at least one alternate condition before being trusted.

## What To Record Per Diagnosis Pass

For each pass, record:
- experiment IDs,
- baseline lineup,
- hypothesis,
- single variable changed,
- before/after win rates,
- key early mutation timing differences,
- key mycovariant timing/selection observations,
- go/no-go conclusion.

## Practical Questions To Ask

When a strategy dominates, ask:
1. Which mutations does it acquire first?
2. Which mutations appear earlier than they do for other strategies?
3. Which late-game mutations are common in wins?
4. Which mycovariants recur in wins?
5. Are those mycovariants decisive early, late, or both?
6. Does the strategy still dominate on a different board geometry?
7. Does the strategy still dominate on a different seed?
8. Does dominance disappear if one mutation/system lever is nudged?

## Interpretation Guidance

If a strategy remains dominant across:
- multiple seeds,
- multiple geometries,
- and explicit controlled lineups,

then the likely cause is a real gameplay balance issue rather than lineup noise.

If dominance disappears when lineup or seed changes, the problem may be:
- matchup sensitivity,
- roster distortion,
- or insufficient sample quality.

## Related Docs

- `SIMULATION_HELPER.md`
- `GAME_BALANCE_CONSTANTS.md`
- `AI_STRATEGY_AUTHORING.md`
- `FungusToast.Analytics/README.md`
