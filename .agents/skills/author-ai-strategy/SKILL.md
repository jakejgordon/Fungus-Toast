---
name: author-ai-strategy
description: Guide for authoring or substantially revising a Fungus Toast AI spending strategy. Use when asked to add a new strategy, retune a roster entry, adjust goals or surges, update roster metadata, or validate strategy behavior in simulations.
---

# Author AI Strategy

Read these docs first:

1. `FungusToast.Core/docs/AI_STRATEGY_AUTHORING.md`
2. `FungusToast.Core/docs/SIMULATION_HELPER.md`
3. `FungusToast.Core/docs/GAME_BALANCE_CONSTANTS.md` when the change is tied to balance levers

## Workflow

1. Confirm the target roster set, intended status, theme, and matchup purpose.
2. Treat the strategy as three layers:
   - build order
   - surge plan
   - fallback personality
3. Keep goal chains coherent across early economy, mid stabilization, and late conversion.
4. When authoring surge-heavy strategies, ensure they still have a sensible non-surge backbone.
5. Update the relevant roster entry and associated theme/status mappings in `FungusToast.Core/AI/AIRoster.cs`.
6. Prefer explicit strategy names and fixed seeds for comparison runs so results stay reproducible as the roster evolves.
7. Record the intended lineup, seed, selection policy, and experiment ID whenever validating the change.

## Validation

1. Build `FungusToast.Core/FungusToast.Core.csproj`.
2. Build `FungusToast.Simulation/FungusToast.Simulation.csproj`.
3. Run at least one seeded smoke simulation.
4. Verify the strategy name, status, and selection metadata appear correctly in the exported results.
5. For comparison work, prefer explicit `--strategy-names` over sampled rosters.

## Output

Report:

1. The strategy intent and main roster changes.
2. The seed, lineup, selection policy, and experiment ID used for validation.
3. The builds and simulations that were run.
