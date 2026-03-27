# Campaign AI Curation

## Current policy direction

- Only strategies with `StrategyStatus = Proven` should be eligible for campaign use.
- Campaign curation should use formal `CampaignDifficulty` metadata rather than relying only on naming/documentation convention.
- Early campaign should bias toward easier and more readable opponents.
- Starting at level 4, nutrient patches should be enabled.
- Starting around level 7, some AIs should eventually receive theme-appropriate starting Adaptations (not yet implemented).
- By level 14, the pool should be mostly hard AIs with some medium AIs mixed in.
- The boss level should use medium + hard support AIs plus a curated themed boss mold with 4-5 synergistic Adaptations.

## Naming convention (proposed)

Use descriptive stable IDs for curated campaign identities:

`CMP_<Theme>_<CorePlan>_<Difficulty>`

Examples:
- `CMP_Control_AnabolicCreeping_Hard`
- `CMP_Bloom_CreepingRegression_Elite`
- `CMP_TierCap_GrowthResilience_Easy`
- `CMP_Economy_LateSpike_Hard`

Rules:
- `CMP_` prefix for campaign-curated identities
- theme first
- core plan second
- difficulty suffix last
- once used in Unity assets, keep IDs stable

## Current campaign implementation

Campaign currently uses:
- `FungusToast.Unity/Assets/Configs/Campaign/CampaignProgression.asset`
- `FungusToast.Unity/Assets/Configs/Board Presets/*.asset`

Each level points to a `BoardPreset`, and each preset currently stores a fixed ordered list of `aiPlayers` strategy names.
There is not yet a random per-level campaign pool system.

## Current level progression snapshot

- Campaign0: AI6
- Campaign1: AI12
- Campaign2: AI6, AI13
- Campaign3: AI8, AI6, AI12
- Campaign4: AI8, AI9, AI6, AI13
- Campaign5: AI7, AI8, AI9, AI6, AI12
- Campaign6: AI7, AI8, AI9, AI11
- Campaign7: AI7, AI8, AI9, AI11, AI8, AI6
- Campaign8: AI7, AI8, AI9, AI11, AI8, AI9
- Campaign9: AI1, AI7, AI8, AI9, AI11, AI8
- Campaign10: AI1, AI2, AI7, AI8, AI9, AI11
- Campaign11: AI1, AI2, AI3, AI7, AI8, AI9, AI11
- Campaign12: AI1, AI2, AI3, AI10, AI7, AI8, AI9
- Campaign13: AI1, AI2, AI3, AI10, AI1, AI7, AI8
- Campaign14: AI1, AI2, AI3, AI10, AI1, AI2, AI7

## Curated campaign candidate table

This is the current recommended audit starting point for `StrategySetEnum = Campaign` plus additional `StrategyStatus = Proven` strategies worth adding to the campaign pool.

| Current ID | Proposed CMP ID | Theme (working) | Difficulty (working) | Keep? | Campaign role | Notes |
|---|---|---|---|---|---|---|
| AI1 | `CMP_Bloom_CreepingInfiltration_Elite` | Bloom / pressure | Hard/Elite | Keep | Boss-capable anchor | Legacy late-pressure boss mold; likely still valid. |
| AI2 | `CMP_Bloom_CreepingInfiltrationReclaim_Elite` | Bloom / reclamation | Hard/Elite | Keep | Boss-capable anchor | Similar shell to AI1 but with reclaim flavor. |
| AI3 | `CMP_Control_DriftCascade_Elite` | Control / cascade | Hard/Elite | Keep | Boss-capable anchor | Distinct enough to keep if theme feels right in Unity. |
| AI4 | `CMP_Reclaim_CreepingRebirth_Normal` | Reclamation | Medium? | Review | Mid-campaign option | Needs explicit difficulty audit before blessing. |
| AI5 | `CMP_Bloom_CreepingRejuvenation_Normal` | Bloom / attrition | Medium? | Review | Mid-campaign option | Needs explicit difficulty audit before blessing. |
| AI6 | `CMP_TierCap_GrowthResilience_Training` | TierCap / defense | Training | Keep | Early training | Good readable low-pressure opener. |
| AI7 | `CMP_Surge_BeaconTempo_Medium` | Surge tempo | Medium | Keep | Mid diversity | Good for introducing tempo play. |
| AI8 | `CMP_Control_AnabolicRebirth_Medium` | Control | Medium | Keep | Mid diversity | Reasonable medium control shell. |
| AI9 | `CMP_Control_TendrilCascade_Medium` | Control / cascade | Medium | Keep | Mid diversity | Multi-goal control line with visible escalation. |
| AI10 | `CMP_Bloom_DriftCascade_Elite` | Bloom / late spike | Hard/Elite | Keep | Boss-capable anchor | Strong legacy hard opponent. |
| AI11 | `CMP_Surge_GrowthTempo_Medium` | Surge tempo | Medium | Keep | Mid diversity | Good for surge-heavy campaign slots. |
| AI12 | `CMP_Control_AnabolicCreeping_Easy` | Control | Easy | Keep | Early training | Solid easy bridge strategy. |
| AI13 | `CMP_Control_AnabolicCreeping_Hard` | Control | Hard/Elite | Keep | Late-control option | Metadata currently says hard; campaign feel should confirm. |
| TST_BalancedControl_AnabolicFirst | `CMP_Control_AnabolicFirst_Hard` | Control | Hard | Keep | Hard pool | Strong proven modern control shell. |
| Grow>Kill>Reclaim(Econ) | `CMP_Economy_KillReclaim_Medium` | Economy / reclamation | Medium | Keep | Mid diversity | Good generalist economy-pressure option. |
| Grow>Kill>Reclaim(Econ/Reclaim) | `CMP_Reclaim_KillReclaim_Medium` | Reclamation | Medium | Keep | Mid diversity | Similar family to above; keep only if both feel distinct enough. |
| Creeping>Necrosporulation | `CMP_Bloom_CreepingNecro_Medium` | Bloom | Medium | Keep | Mid diversity | Readable themed bloom shell. |
| Power Mutations Max Econ | `CMP_Economy_LateSpike_Hard` | Late spike / economy | Hard | Keep | Hard pool / boss support | High-ceiling power strategy. |
| Growth/Resilience | `CMP_TierCap_GrowthResilience_Easy` | TierCap / defense | Easy | Keep | Early-to-mid filler | Simulation says viable; campaign feel should decide final lane. |
| TST_AnabolicBeaconNecroRegressionCascade | `CMP_Bloom_BeaconRegression_Medium` | Bloom / control | Medium | Keep | Medium pool | Proven and campaign-eligible. |
| TST_AnabolicCreepingNecroRegressionCascade | `CMP_Bloom_AnabolicRegression_Medium` | Bloom / control | Medium | Keep | Medium pool | Proven and campaign-eligible. |
| TST_CreepingNecroRegressionCascade | `CMP_Bloom_CreepingRegression_Elite` | Bloom / control | Hard/Elite | Keep | Hard pool / boss support | Best-performing proven Bloom strategy from latest tests. |

## Important cleanup observations

- The current campaign assets still reference legacy `AI1..AI13` IDs.
- Several of the working theme labels above are inferred from mutation goals and should be reviewed in Unity feel-testing.
- `AI4` and `AI5` are the biggest uncertainty cases and should be audited before they are treated as permanent campaign pool members.
- There is likely overlap between `AI13` and `TST_BalancedControl_AnabolicFirst`; we may want to keep both for now, then consolidate later if they feel redundant.
- `Grow>Kill>Reclaim(Econ)` and `Grow>Kill>Reclaim(Econ/Reclaim)` may also be too similar to both keep in the final curated campaign pool.

## Recommended keep / review / retire pass

### Keep now
- AI1, AI2, AI3, AI6, AI7, AI8, AI9, AI10, AI11, AI12, AI13
- TST_BalancedControl_AnabolicFirst
- Grow>Kill>Reclaim(Econ)
- Grow>Kill>Reclaim(Econ/Reclaim)
- Creeping>Necrosporulation
- Power Mutations Max Econ
- Growth/Resilience
- TST_AnabolicBeaconNecroRegressionCascade
- TST_AnabolicCreepingNecroRegressionCascade
- TST_CreepingNecroRegressionCascade

### Review before permanent inclusion
- AI4
- AI5
- overlap between AI13 and `TST_BalancedControl_AnabolicFirst`
- overlap between the two `Grow>Kill>Reclaim(...)` variants

### Retire later if redundant
- none yet; defer until the curated pool is tested in campaign progression

## Proposed campaign difficulty lanes

### Early / training
- AI6
- AI12
- Growth/Resilience
- possibly a future true weak/random-style strategy

### Medium / normal
- AI7
- AI8
- AI9
- AI11
- Grow>Kill>Reclaim(Econ)
- Grow>Kill>Reclaim(Econ/Reclaim)
- Creeping>Necrosporulation
- TST_AnabolicBeaconNecroRegressionCascade
- TST_AnabolicCreepingNecroRegressionCascade

### Hard / elite
- AI1
- AI2
- AI3
- AI10
- AI13
- TST_BalancedControl_AnabolicFirst
- Power Mutations Max Econ
- TST_CreepingNecroRegressionCascade

## Target Campaign0–14 progression draft

This is the first intentional target plan for future level pools. It is not yet implemented as random-pool data; it is the design baseline we should curate toward.

| Level | Board / Tier intent | Nutrient patches | Intended pool mix | Candidate pool |
|---|---|---|---|---|
| Campaign0 | very small / onboarding | Off | 1 training | `AI6` |
| Campaign1 | very small / onboarding+ | Off | 1 easy | `AI12`, `Growth/Resilience` |
| Campaign2 | small / first multi-AI | Off | 1 training + 1 easy/hard teaser | `AI6`, `AI12`, `AI13` |
| Campaign3 | small / first medium intro | Off | 1 easy + 1 medium + 1 easy | `AI12`, `Growth/Resilience`, `AI8`, `AI7` |
| Campaign4 | larger / nutrient intro | On | 1 easy + 2 medium + 1 easy | `AI12`, `Growth/Resilience`, `AI7`, `AI8`, `AI9` |
| Campaign5 | larger / more archetype variety | On | 1 easy + 3 medium + 1 medium/easy | `AI7`, `AI8`, `AI9`, `AI11`, `Grow>Kill>Reclaim(Econ)`, `Creeping>Necrosporulation`, `Growth/Resilience` |
| Campaign6 | mid-board / stable medium field | On | 4-5 medium | `AI7`, `AI8`, `AI9`, `AI11`, `Grow>Kill>Reclaim(Econ)`, `Grow>Kill>Reclaim(Econ/Reclaim)`, `Creeping>Necrosporulation` |
| Campaign7 | mid-board / adaptation era begins | On | 3-4 medium + 1 hard | `AI7`, `AI8`, `AI9`, `AI11`, `TST_AnabolicBeaconNecroRegressionCascade`, `TST_AnabolicCreepingNecroRegressionCascade`, `AI13`, `TST_BalancedControl_AnabolicFirst` |
| Campaign8 | mid-large / stronger themes | On | 3 medium + 2 hard | `AI8`, `AI9`, `AI11`, `Grow>Kill>Reclaim(Econ)`, `Creeping>Necrosporulation`, `AI13`, `TST_BalancedControl_AnabolicFirst`, `Power Mutations Max Econ` |
| Campaign9 | large / hard pool arrives | On | 2 medium + 3 hard | `AI9`, `AI11`, `TST_AnabolicBeaconNecroRegressionCascade`, `TST_AnabolicCreepingNecroRegressionCascade`, `AI13`, `TST_BalancedControl_AnabolicFirst`, `Power Mutations Max Econ`, `AI1` |
| Campaign10 | large / hard mix deepens | On | 2 medium + 4 hard | `AI11`, `Grow>Kill>Reclaim(Econ)`, `Grow>Kill>Reclaim(Econ/Reclaim)`, `AI1`, `AI2`, `AI13`, `TST_BalancedControl_AnabolicFirst`, `Power Mutations Max Econ` |
| Campaign11 | larger / elite previews | On | 2 medium + 4 hard/elite | `Creeping>Necrosporulation`, `TST_AnabolicCreepingNecroRegressionCascade`, `AI1`, `AI2`, `AI3`, `AI13`, `TST_BalancedControl_AnabolicFirst`, `TST_CreepingNecroRegressionCascade` |
| Campaign12 | late game / elite mix | On | 1-2 medium + 5 hard/elite | `AI1`, `AI2`, `AI3`, `AI10`, `AI13`, `Power Mutations Max Econ`, `TST_CreepingNecroRegressionCascade`, plus one medium support pick |
| Campaign13 | pre-boss gauntlet | On | mostly hard/elite with one medium flex | `AI1`, `AI2`, `AI3`, `AI10`, `AI13`, `TST_BalancedControl_AnabolicFirst`, `Power Mutations Max Econ`, `TST_CreepingNecroRegressionCascade`, optional `AI11`/`Grow>Kill>Reclaim(Econ)` |
| Campaign14 | final standard level before boss design | On | mostly hard/elite with medium support | `AI1`, `AI2`, `AI3`, `AI10`, `AI13`, `TST_BalancedControl_AnabolicFirst`, `Power Mutations Max Econ`, `TST_CreepingNecroRegressionCascade`, one medium support |

## Boss level direction

Not yet implemented, but target shape should be:

- support pool: medium + hard AI strategies only
- one curated boss mold identity
- boss starts with 4-5 synergistic theme-appropriate Adaptations
- likely boss archetype candidates:
  - `CMP_Bloom_CreepingRegression_Elite`
  - `CMP_Economy_LateSpike_Hard`
  - `CMP_Control_DriftCascade_Elite`

## Rename migration plan

Do the naming migration in one controlled pass:

1. finalize the keep/review list
2. assign final `CMP_*` IDs
3. rename strategy IDs in `AIRoster`
4. update every Unity `BoardPreset.aiPlayers` reference in the same change
5. run build + campaign asset sanity check
6. only then begin level-pool authoring on the renamed IDs

This avoids half-migrated states where campaign assets reference stale strategy names.

## Active backlog location

The live implementation backlog for Fungus-Toast should be kept in:

- `/home/jakejgordon/.openclaw/workspace/FUNGUS_TOAST_TASKS.md`

Keep this document focused on durable campaign design/curation decisions rather than the day-to-day running task list.

## Next steps

1. Confirm or adjust the working themes/difficulty labels in the candidate table.
2. Decide whether AI4 and AI5 stay in the campaign pool.
3. Resolve likely redundant pairs.
4. Refine the target level-by-level pool progression for Campaign0-14.
5. After that, do the actual `CMP_*` rename migration and update Unity board presets.
6. Then implement the campaign AI pool system and campaign-balance simulation harness.
