# Campaign AI Curation

## Current policy direction

- Only strategies with `StrategyStatus = Proven` should be eligible for campaign use.
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
| AI6 | `CMP_TierCap_GrowthResilience_Easy` | TierCap / defense | Easy | Keep | Early training | Good readable low-pressure opener. |
| AI7 | `CMP_Surge_BeaconTempo_Normal` | Surge tempo | Medium | Keep | Mid diversity | Good for introducing tempo play. |
| AI8 | `CMP_Control_AnabolicRebirth_Normal` | Control | Medium | Keep | Mid diversity | Reasonable medium control shell. |
| AI9 | `CMP_Control_TendrilCascade_Normal` | Control / cascade | Medium | Keep | Mid diversity | Multi-goal control line with visible escalation. |
| AI10 | `CMP_Bloom_DriftCascade_Elite` | Bloom / late spike | Hard/Elite | Keep | Boss-capable anchor | Strong legacy hard opponent. |
| AI11 | `CMP_Surge_GrowthTempo_Normal` | Surge tempo | Medium | Keep | Mid diversity | Good for surge-heavy campaign slots. |
| AI12 | `CMP_Control_AnabolicCreeping_Easy` | Control | Easy | Keep | Early training | Solid easy bridge strategy. |
| AI13 | `CMP_Control_AnabolicCreeping_Hard` | Control | Hard/Elite | Keep | Late-control option | Metadata currently says hard; campaign feel should confirm. |
| TST_BalancedControl_AnabolicFirst | `CMP_Control_AnabolicCreeping_Hard` | Control | Hard | Keep | Hard pool | Strong proven modern control shell. |
| Grow>Kill>Reclaim(Econ) | `CMP_Economy_KillReclaim_Normal` | Economy / reclamation | Medium | Keep | Mid diversity | Good generalist economy-pressure option. |
| Grow>Kill>Reclaim(Econ/Reclaim) | `CMP_Reclaim_KillReclaim_Normal` | Reclamation | Medium | Keep | Mid diversity | Similar family to above; keep only if both feel distinct enough. |
| Creeping>Necrosporulation | `CMP_Bloom_CreepingNecro_Normal` | Bloom | Medium | Keep | Mid diversity | Readable themed bloom shell. |
| Power Mutations Max Econ | `CMP_Economy_LateSpike_Hard` | Late spike / economy | Hard | Keep | Hard pool / boss support | High-ceiling power strategy. |
| Growth/Resilience | `CMP_TierCap_GrowthResilience_Normal` | TierCap / defense | Easy/Medium | Keep | Early-to-mid filler | Simulation says viable; campaign feel should decide final lane. |
| TST_AnabolicBeaconNecroRegressionCascade | `CMP_Bloom_BeaconRegression_Normal` | Bloom / control | Medium | Keep | Medium pool | Proven and campaign-eligible. |
| TST_AnabolicCreepingNecroRegressionCascade | `CMP_Bloom_AnabolicRegression_Normal` | Bloom / control | Medium | Keep | Medium pool | Proven and campaign-eligible. |
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

## Next steps

1. Confirm or adjust the working themes/difficulty labels in the candidate table.
2. Decide whether AI4 and AI5 stay in the campaign pool.
3. Resolve likely redundant pairs.
4. Draft a target level-by-level pool progression for Campaign0-14.
5. After that, implement a per-level random pool system instead of fixed lineups.
