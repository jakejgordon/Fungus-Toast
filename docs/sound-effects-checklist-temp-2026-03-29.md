# Temporary Sound Effects Checklist

This file is a temporary implementation checklist for the current sound pass.

Delete it after the sound backlog is complete or once the final decisions have been folded into permanent docs and implementation.

## Status Key

- `implemented` = wired in game and available for testing
- `ready` = priority agreed, hook point identified, asset can be authored next
- `planned` = useful but lower priority after the core set is stable

## Priority Order

| Priority | Sound effect | Recommended file name | Recommended duration | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| 1 | Mutation upgrade success | `sfx_ui_mutation_upgrade_success_01.wav` | `0.25s` to `0.45s` | `implemented` | First live cue. Already wired to successful mutation upgrades. |
| 2 | Store mutation points | `sfx_ui_mutation_store_points_01.wav` | `0.18s` to `0.35s` | `ready` | Soft deliberate confirmation, not reward-like. |
| 3 | Mutation phase start | `sfx_phase_mutation_start_01.wav` | `0.60s` to `1.00s` | `ready` | Highest-value phase cue because it frames player decision time. |
| 4 | Growth phase start | `sfx_phase_growth_start_01.wav` | `0.60s` to `1.00s` | `ready` | Should pair with the existing growth banner. |
| 5 | Decay phase start | `sfx_phase_decay_start_01.wav` | `0.50s` to `0.90s` | `ready` | Shorter and darker than growth. |
| 6 | Drafting phase start | `sfx_phase_draft_start_01.wav` | `0.80s` to `1.20s` | `ready` | Slightly more ceremonial because it is rarer. |
| 7 | Growth cycle tick | `sfx_phase_growth_cycle_tick_01.wav` | `0.08s` to `0.18s` | `ready` | Must stay subtle to avoid fatigue. |
| 8 | Invalid mutation click | `sfx_ui_mutation_invalid_01.wav` | `0.08s` to `0.20s` | `planned` | Quiet negative cue only if it does not become annoying. |
| 9 | Round complete transition | `sfx_phase_round_complete_01.wav` | `0.50s` to `0.90s` | `planned` | Useful once phase language is stable. |
| 10 | Nutrient patch claim / board reward | `sfx_board_nutrient_claim_01.wav` | `0.20s` to `0.50s` | `planned` | Good reward cue after core loop sounds are in. |

## Current Working Order

1. Mutation upgrade success
2. Store mutation points
3. Mutation phase start
4. Growth phase start
5. Decay phase start
6. Drafting phase start
7. Growth cycle tick
8. Invalid mutation click
9. Round complete transition
10. Nutrient patch claim / board reward

## Hook Notes

| Sound effect | Primary Unity hook |
| --- | --- |
| Mutation upgrade success | `UI_MutationManager.TryUpgradeMutation(...)` and targeted success path in `ResolveChemotacticBeaconUpgrade(...)` |
| Store mutation points | `UI_MutationManager.OnStoreMutationPointsClicked()` |
| Mutation phase start | `GameManager.StartNextRound()` |
| Growth phase start | `GameManager.StartGrowthPhase()` or `GrowthPhaseRunner.StartGrowthPhase()` |
| Decay phase start | `GameManager.StartDecayPhase()` |
| Drafting phase start | `GameManager.StartMycovariantDraftPhase(...)` |
| Growth cycle tick | `GrowthPhaseRunner.RunNextCycle(...)` |
| Invalid mutation click | `UI_MutationManager.TryUpgradeMutation(...)` failed path |
| Round complete transition | `GameManager.OnRoundComplete()` |
| Nutrient patch claim / board reward | board reward presentation path after identifying the preferred Unity-side consumer |

## Temporary Decisions

- Keep this checklist separate from `FungusToast.Core/docs/SOUNDS.md`.
- Use this file as the scratch backlog while the sound pass is active.
- Remove this file once the backlog is complete or superseded.
