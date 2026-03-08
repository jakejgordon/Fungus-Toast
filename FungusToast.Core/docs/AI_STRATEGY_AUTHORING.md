# AI Strategy Authoring

This document explains how AI mutation spending strategies are configured, selected, and validated for simulation runs.

## Strategy Sets

Strategies are currently grouped into the following sets in `FungusToast.Core/AI/AIRoster.cs`:

- `Proven`: stable baseline roster for general use.
- `Testing`: expanded roster for balance experiments and tuning.
- `Campaign`: simple named variants (`AI1`, `AI2`, etc.) for campaign integration.
- `Mycovariants`: focused permutations for mycovariant studies.

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
6. Run at least one seeded smoke simulation and verify strategy names appear in output.

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
