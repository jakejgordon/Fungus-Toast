# Campaign AI Curation

This document should stay short and current.

Use it for:
- durable campaign-AI curation policy
- current authored late-campaign snapshot
- current keep/review guidance for campaign-safe molds

Do **not** use it for detailed simulation history or long experiment logs. Those belong in:
- `docs/WORKLOG.md`
- experiment-specific notes such as `docs/campaign-modernization-2026-03-27.md`

## Current policy direction

- Only strategies with campaign-safe metadata and current intended use should be authored into campaign presets.
- Campaign placement should be based on **contextual fit** on the real board, not just a static difficulty label.
- Late campaign should be validated with the safe proxy and artifact-backed reporting.
- For balance conclusions, follow `FungusToast.Core/docs/SIMULATION_HELPER.md`.
- Starting Adaptations are now a real authoring lever for curated elites/boss-like opponents.

## Current naming / roster reality

The campaign no longer needs a speculative rename plan as the main focus.

What matters now is the actual authored and campaign-eligible roster in `AIRoster`, including named curated molds such as:
- `CMP_Control_AnabolicFirst_Hard`
- `CMP_Economy_LateSpike_Hard`
- `CMP_Bloom_CreepingRegression_Elite`

Legacy `AI1`-style identifiers still exist and are still used in authored campaign presets where appropriate.

## Current late-campaign authored snapshot

Current real authored presets in Unity:

- `Campaign12` (`130x130 7 AI.asset`)
- `Campaign13` (`140x140 7 AI.asset`)
- `Campaign14` (`150x150 7 AI.asset`)
- `Campaign15` (`160x160 7 AI.asset`)

### Campaign12
Current authored lineup:
- `AI1`
- `AI2`
- `AI3`
- `AI10`
- `AI7`
- `AI8`
- `AI10`

Current status:
- still softer than the intended late-campaign blocker band in recent validation
- keep under review if late-campaign tuning resumes

### Campaign13
Current authored lineup:
- `AI1`
- `AI2`
- `AI3`
- `CMP_Control_AnabolicFirst_Hard`
- `CMP_Economy_LateSpike_Hard`
- `CMP_Bloom_CreepingRegression_Elite`
- `AI1`

Current status:
- accepted for now as a good late-campaign level after confirmation

### Campaign14
Current authored lineup:
- `AI1`
- `AI2`
- `AI3`
- `CMP_Bloom_CreepingRegression_Elite` with starting adaptations:
  - `SporeSalvo`
  - `MycotoxicHalo`
  - `MycotoxicLash`
  - `VesicleBurst`
  - `HyphalBridge`
  - `ApicalYield`
- `AI1`
- `CMP_Control_AnabolicFirst_Hard`
- `CMP_Economy_LateSpike_Hard`

Current status:
- accepted for now as one of the final standard levels
- latest 50-game artifact-backed validation put the safe proxy at `8.0%`
- this is harsh, but currently considered appropriate for the level’s place in the campaign

### Campaign15
Current authored lineup:
- `AI1`
- `AI2`
- `AI3`
- `AI10`
- `AI1`
- `AI2`
- `AI7`

Current status:
- not yet brought up to the same curated state as Campaign13-14
- likely next late-campaign curation target when this thread resumes

## Current campaign-safe mold guidance

### Keep as current hard/elite campaign molds
- `AI1`
- `AI2`
- `AI3`
- `AI10`
- `CMP_Control_AnabolicFirst_Hard`
- `CMP_Economy_LateSpike_Hard`
- `CMP_Bloom_CreepingRegression_Elite`

### Keep as current medium/hard support candidates
- `AI7`
- `AI8`
- `AI9`
- `AI11`
- `AI13`
- `CMP_Control_AnabolicRebirth_Medium`
- `CMP_Economy_KillReclaim_Medium`
- `CMP_Bloom_CreepingNecro_Medium`
- `CMP_Bloom_BeaconRegression_Medium`

### Review before relying on them for campaign curation
- `AI4`
- `AI5`
- any pair of molds that feel functionally redundant in authored boards

## Practical guidance

When tuning campaign levels:
- validate the exact authored Unity board preset, not a remembered lineup
- use `scripts/run_campaign_balance.py` for campaign checks
- treat console output as progress only
- make the final call from exported artifacts / analytics output

For detailed evidence, commands, seeds, and prior experiments, see:
- `docs/WORKLOG.md`
- `docs/campaign-modernization-2026-03-27.md`
