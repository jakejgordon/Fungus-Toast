---
name: validate-campaign-balance
description: Workflow for validating Fungus Toast campaign balance with artifact-backed results. Use when asked to run campaign balance simulations, confirm a tuning change, compare results to target win-rate bands, or make campaign difficulty calls.
---

# Validate Campaign Balance

Read these docs first:

1. `FungusToast.Core/docs/SIMULATION_HELPER.md`
2. `FungusToast.Core/docs/CAMPAIGN_HELPER.md`
3. `FungusToast.Analytics/README.md`

## Workflow

1. Use `scripts/run_campaign_balance.py` unless the user explicitly asks for a lower-level command.
2. Keep the authored board preset, lineup or pool resolution, nutrient-patch settings, and starting-adaptation flow intact.
3. Use the safe proxy `TST_CampaignPlayer_SafeBaseline` unless the user explicitly asks for a different proxy or lineup.
4. Run in non-interactive mode and keep the experiment ID, seed, and output folder.
5. Do not make final balance calls from console output or harness summary text alone.
6. Read the exported artifacts after the run completes.
7. Use the repo-local analytics environment at `FungusToast.Analytics/.venv` when generating summaries.
8. Compare the proxy win rate against the target band from `CAMPAIGN_HELPER.md`, and report the gap explicitly.
9. When confirming a tuning change, keep the same baseline conditions before changing seed or geometry.
10. If artifact reading or analytics fails after using the repo-local environment, stop at `validation blocked`.

## Reporting

Report from artifacts:

1. `Win %`
2. `Avg Living Cells`
3. `Avg Dead Cells`
4. `Avg Toxins`
5. The target band and whether the result lands above, inside, or below it

Include the experiment ID, seed, and the output folder or manifest reference in the summary.
