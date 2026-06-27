---
name: diagnose-dominance
description: Controlled workflow for explaining why a Fungus Toast strategy appears dominant. Use when a simulation batch suggests a strategy is overperforming and the task is to isolate the cause with fixed lineups, artifact analysis, and single-variable tuning passes.
---

# Diagnose Dominance

Read these docs first:

1. `FungusToast.Core/docs/DOMINANCE_DIAGNOSIS_WORKFLOW.md`
2. `FungusToast.Core/docs/SIMULATION_HELPER.md`
3. `FungusToast.Core/docs/AI_STRATEGY_AUTHORING.md`
4. `FungusToast.Analytics/README.md`

## Workflow

1. Distinguish screening runs from controlled diagnosis runs. Use explicit lineups for diagnosis.
2. Establish one fixed baseline lineup containing the suspect strategy and a stable set of comparison anchors.
3. Keep seed, board size, slot policy, and experiment ID fixed for the baseline.
4. Write down the causal hypothesis before changing anything.
5. Inspect exported artifacts in this priority order:
   - `upgrade_events.parquet`
   - `mutations.parquet`
   - `mycovariants.parquet`
   - `players.parquet`
   - `manifest.json`
6. Change one main lever at a time.
7. After a tuning change, rerun the same baseline before trying an alternate seed or geometry.
8. Do not collapse diagnosis into a generic "run more sims" loop.

## Reporting

Record and report:

1. The baseline lineup and experiment IDs.
2. The hypothesis being tested.
3. The single variable changed.
4. The before and after results.
5. The specific evidence that supports the conclusion.
6. Whether the issue looks like a real gameplay imbalance, lineup noise, or still unresolved.
