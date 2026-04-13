# AI Strategy Authoring

See also: [README.md](README.md) for the full documentation hierarchy, [SIMULATION_HELPER.md](SIMULATION_HELPER.md) for simulation workflows, and [GAME_BALANCE_CONSTANTS.md](GAME_BALANCE_CONSTANTS.md) for balance-lever guidance.

This document explains how AI mutation spending strategies are configured, selected, and validated for simulation runs.

## Strategy Sets

Strategies are currently grouped into the following sets in `FungusToast.Core/AI/AIRoster.cs`:

- `Proven`: stable baseline roster for general use.
- `Testing`: expanded roster for balance experiments and tuning.
- `Campaign`: simple named variants (`AI1`, `AI2`, etc.) for campaign integration.
- `Mycovariants`: focused permutations for mycovariant studies.

## Strategy Status

Each strategy profile now carries a status tag in `AIRoster`:

- `Testing`: experimental or tuning-oriented strategies.
- `Proven`: baseline strategies considered stable enough for broader comparison.
- `Loser`: reserved for future demotion/blacklist workflows when a strategy should stay cataloged but be treated as a poor baseline.

Current defaulting is roster-based (`Proven`/`Campaign` => `Proven`, `Testing`/`Mycovariants` => `Testing`) with per-strategy overrides available in `ExplicitStrategyStatusesByName`.

## Selection Policies

Simulation commands can choose strategy sampling behavior via:

- `RandomUnique`: random unique picks from the selected set.
- `CoverageBalanced`: tries to cover distinct strategy themes first, then fills remaining slots.
- `StratifiedCycle`: deterministic, theme-ordered cycle that shifts by index.

CLI flag:

```bash
dotnet run -- --strategy-set Testing --selection-policy CoverageBalanced --games 200 --players 8 --no-keyboard
```

## Testing Strategy Catalog

The `Testing` set includes a themed roster intended for robust balance analysis:

| Strategy Name | Theme | Intent |
|---|---|---|
| `TST_HyperEconomyRamp` | EconomyRamp | Front-load economy and scale into high-tier pressure |
| `TST_EarlyReclaimerSwarm` | Reclamation | Retake territory and recover board losses quickly |
| `TST_ToxinSiege` | Offense | Build toxin pressure and force attrition |
| `TST_HyphalSurgeTempo` | SurgeTempo | Leverage timed surge windows for burst turns |
| `TST_FortressResilience` | Defense | Stabilize with durable infrastructure before push |
| `TST_OpportunisticCounterplay` | Counterplay | Flexible path that adapts to opponent plans |
| `TST_Tier3PlateauSpecialist` | TierCap | Maximize efficiency in lower/mid tiers |
| `TST_LateGameSpike` | LateGameSpike | Bank and convert into late high-impact upgrades |
| `TST_BalancedGeneralistControl` | Control | Broad and steady progression across game phases |
| `TST_BalancedControl_NoPreferredMyco` | Control | Control baseline without explicit mycovariant preferences |
| `TST_RebirthAttrition` | Attrition | Win through repeated death/rebirth loops |
| `TST_BalancedControl_MaxEconomy` | Control | Control path with stronger economy weighting |
| `TST_LowTierEconomyGrinder` | TierCap | Prioritize Tier1/2 economy-resilience first, then escalate |
| `TST_LowTierSurgeSkirmisher` | Counterplay | Low-tier fungicide/genetic skirmisher with anti-leader surge |
| `TST_BalancedControl_MinorEconomy` | Control | Control variant with lighter economy bias |
| `TST_CampaignMirror_AI7_Hyphal` | SurgeTempo | Testing mirror of campaign hyphal surge/vectoring line |
| `TST_CampaignMirror_AI12_BalancedControl_AnabolicFirst` | Control | Testing mirror of campaign AI12 progression |
| `TST_CampaignMirror_AI13_BalancedControl_MaxEconomy` | Control | Testing mirror of campaign AI13 progression |

## Canonical 8-Player Archetype Harness

For ongoing 8-player balance tuning, the `Testing` roster now includes a fixed archetype harness:

- `TST_Arch01_GrowthResilience`
- `TST_Arch02_ResilienceGrowth`
- `TST_Arch03_FungicideSurge`
- `TST_Arch04_DriftGrowth`
- `TST_Arch05_DriftResilience`
- `TST_Arch06_SurgeGrowth`
- `TST_Arch07_DriftFungicide`
- `TST_Arch08_SurgeResilience`

Use these via explicit `--strategy-names` when you want a stable 8-player comparison harness that does not drift as the broader Testing roster evolves.

## ParameterizedSpendingStrategy Precedence

For `ParameterizedSpendingStrategy`, authoring intent is easiest to understand if you think in three layers:

1. **Build order**
   - `targetMutationGoals`
   - This is the main spine of the AI. Goals are processed in list order, one active goal at a time, including prerequisites.
2. **Surge plan**
   - `surgePriorityIds`
   - `surgeAttemptTurnFrequency`
   - Preferred surges are attempted on scheduled rounds before normal fallback spending, with optional short-term banking if a preferred surge is close.
3. **Fallback personality**
   - `priorityMutationCategories`
   - `prioritizeHighTier`
   - `economyBias`
   - These mainly shape spending only when the AI is not currently able to make meaningful progress on its explicit goal chain.

Actual spending flow, simplified:

1. Early-game economy mutation pass (`MutatorPhenotype`, `AdaptiveExpression`, `HyperadaptiveDrift`)
2. Work through `targetMutationGoals` in order
3. Try scheduled surges (`surgePriorityIds` on `surgeAttemptTurnFrequency` rounds)
4. Try catch-up surge if behind
5. Bank for a near-term preferred surge if appropriate
6. Fallback spending:
   - preferred categories first
   - then any upgradable mutation
   - then economy-biased random weighting
7. Last-resort surge attempt again

Practical interpretation of the main knobs:

- `targetMutationGoals`
  - Primary build order. Strongest authoring control.
- `surgeAttemptTurnFrequency`
  - Timing gate for preferred surge activation attempts.
- `priorityMutationCategories`
  - Category preference during fallback spending.
- `prioritizeHighTier`
  - Within a candidate set, try higher-tier prerequisite-backed options first.
- `economyBias`
  - **Fallback-only** weighting toward `GeneticDrift` mutations. This is not a full global economy strategy dial.

## Goal-Level Authoring Rules

- Omit `TargetLevel` only when the intent is to max out a mutation.
- Use `new TargetMutationGoal(MutationIds.X, 1)` when the intent is a single pickup rather than a max target.
- For repeated appearances of the same mutation in a goal list, use explicit ascending targets to represent staged revisits. Example: level 1 early, then level 2 later.
- Prefer `GameBalance.*MaxLevel` constants when writing explicit max targets for readability in long archetype chains.

## Surge Coherence Audit (Built-In)

`AIRoster` runs a startup audit (`AuditSurgeBackboneSynergy`) for `Testing` strategies.

- It checks surge-prioritizing strategies for at least one non-surge backbone category in goals.
- It compares that backbone against `MutationSynergyCatalog` suggestions.
- It emits warnings to `CoreLogger` when surge goals and backbone categories are incoherent.

Authoring implication:

- If you add `surgePriorityIds`, also include supporting non-surge goals in Growth/Resilience/Fungicide/GeneticDrift as appropriate.

## Authoring Checklist

1. Add a uniquely named strategy in the appropriate roster list.
2. Add/adjust theme mapping in `ExplicitStrategyThemesByName` when needed.
3. Keep mutation-goal chains coherent: early economy, mid stabilization, late finish.
4. Prefer category-specific mycovariant preferences for clearer test intent.
5. Build `FungusToast.Core` and `FungusToast.Simulation` after any roster changes.
6. Run at least one seeded smoke simulation and verify strategy names/statuses appear in exported metadata.
7. For comparison runs, prefer a fixed `--seed`, record the `--selection-policy`, and keep the manifest's selected lineup with the results.
8. For canonical balance experiments, prefer explicit `--strategy-names` instead of sampled rosters so roster composition does not drift with future roster edits.
9. Explicit strategy-name experiments are single-roster by design: all names must come from the chosen `--strategy-set`, so do not mix `Proven`/`Testing`/`Campaign`/`Mycovariants` names in one run.
10. If a design doc says a mutation name without `Max` or `Level N`, treat that as ambiguous and resolve it before implementation; for the current archetype harness, unlabeled steps were encoded as one upgrade.

## Recommended Simulation Pattern

Use batch mode with deterministic seeds and coverage-balanced selection for statistical relevance:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- \
  --games 100 \
  --player-counts 4,8 \
  --board-sizes 120x120,160x160 \
  --strategy-sets Testing \
  --selection-policy CoverageBalanced \
  --seed 12345 \
  --no-keyboard
```

Canonical 8-player archetype run:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- \
  --games 100 \
  --players 8 \
  --strategy-set Testing \
  --strategy-names TST_Arch01_GrowthResilience,TST_Arch02_ResilienceGrowth,TST_Arch03_FungicideSurge,TST_Arch04_DriftGrowth,TST_Arch05_DriftResilience,TST_Arch06_SurgeGrowth,TST_Arch07_DriftFungicide,TST_Arch08_SurgeResilience \
  --seed 12345 \
  --rotate-slots \
  --experiment-id testing_archetype_harness \
  --no-keyboard
```
