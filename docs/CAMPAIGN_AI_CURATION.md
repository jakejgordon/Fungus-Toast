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
- `AI4` and `AI5` were the biggest uncertainty cases; fresh authored-board safe-proxy screens on 2026-03-27 say **do not use them to bulk up the Campaign5-7 bridge**. They either fail to help or actively make that band harsher.
- Campaign5 is now authored as a curated 5-of-6 pool on the real board asset, adding `CMP_Reclaim_Scavenger_Easy` to the existing five-opponent lineup. Early asset-backed safe-proxy validation across seeds `20260327` and `20260328` landed at `25.0%` won (`10/40`), avg living cells `247.9`, avg dead cells `175.3`.
- Campaign6 is now authored as a curated 4-of-5 pool on the real board asset, adding `CMP_TierCap_GrowthResilience_Easy` to the existing four-opponent lineup. Early asset-backed safe-proxy validation across seeds `20260327` and `20260328` landed at `25.0%` won (`10/40`), avg living cells `678.9`, avg dead cells `464.3`.
- Campaign7 remains fixed for now; the earlier pooled bridge prototype still left that board in a harsh ~`4-6%` proxy band, so more variety there needs a real softening pass rather than simple add-one-easy pooling.
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
- CMP_Defense_ResilientShell_Easy
- CMP_Defense_ReclaimShell_Easy
- CMP_Reclaim_InfiltrationSurge_Easy

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
| Campaign1 | very small / onboarding+ | Off | 1 easy | `AI12`, `Growth/Resilience`, `CMP_Reclaim_InfiltrationSurge_Easy`, `CMP_Defense_ResilientShell_Easy`, `CMP_Defense_ReclaimShell_Easy` |
| Campaign2 | small / first multi-AI | Off | 1 training + 1 easy/hard teaser | `AI6`, `AI12`, `AI13` |
| Campaign3 | small / first medium intro | Off | 1 easy + 1 medium + 1 easy | `AI12`, `Growth/Resilience`, `AI8`, `AI7` |
| Campaign4 | larger / nutrient intro | On | 1 easy + 2 medium + 1 easy | `CMP_Reclaim_InfiltrationSurge_Easy`, `Growth/Resilience`, `AI7`, `AI8`, `AI9` |
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

## Campaign AI evaluation approach

Static difficulty labels are not enough, because campaign AI strength can vary a lot by:
- board size
- player count
- nutrient patches
- opponent mix
- later, starting Adaptations

So campaign placement should use **contextual fit**, not just a single universal difficulty label.

### Representative scenario buckets

Use a small set of reusable campaign-like test scenarios to characterize AIs before locking them to levels:

1. **S1: Tiny duel**
   - ~10x10 to 15x15
   - 2 players
   - nutrients off
   - purpose: onboarding / tutorial fit

2. **S2: Small skirmish**
   - ~20x20 to 30x30
   - 3-4 players
   - nutrients off
   - purpose: early multi-opponent fit

3. **S3: Medium growth field**
   - ~40x40 to 75x75
   - 5-6 players
   - nutrients on
   - purpose: first real campaign complexity band

4. **S4: Mid-large pressure field**
   - ~90x90 to 110x110
   - 6-7 players
   - nutrients on
   - purpose: mid-campaign pressure testing

5. **S5: Large late-game field**
   - ~120x120 to 140x140
   - 7-8 players
   - nutrients on
   - purpose: hard/elite candidate testing

6. **S6: Endgame gauntlet**
   - ~150x150 to 160x160
   - 8 players
   - nutrients on
   - purpose: final-level / boss-support fit

### Evaluation matrix

For each campaign-eligible AI, maintain a matrix with:
- current strategy ID
- proposed campaign name / mold display name later
- theme
- default `CampaignDifficulty`
- readability (design judgment)
- volatility (design judgment + later simulation evidence)
- scenario fit notes for S1-S6
- proxy-facing difficulty observations
- keep / review / boss-only / retire recommendation

### Practical process

1. characterize individual AIs in representative scenario buckets
2. classify where each AI seems to fit best
3. assemble level pools from AIs with compatible contextual fit
4. validate actual level lineups with the player-proxy harness
5. retest only the levels or AIs that look suspicious

This should produce a much more robust campaign curve than relying on one static difficulty label per AI.

## First-pass campaign AI evaluation matrix

This is an intentionally rough first-pass matrix for discussion. It combines current metadata, simulation results, and design judgment. It should be revised as we gather more campaign-harness evidence.

Legend:
- **Readability**: how easily a player can understand the mold's gameplan from play/board state
- **Volatility**: how swingy or inconsistent the mold is likely to feel across runs
- **Scenario fit**: rough fit across S1-S6 (`poor`, `ok`, `good`, `best`)

| Strategy | Theme | CampaignDifficulty | Readability | Volatility | S1 Tiny Duel | S2 Small Skirmish | S3 Medium Growth | S4 Mid-Large Pressure | S5 Large Hard | S6 Endgame | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| AI6 | TierCap / defense | Training | High | Low | best | good | ok | poor | poor | poor | Great tutorial mold; simple and readable. |
| AI12 | Control | Easy | High | Low | good | best | good | ok | poor | poor | Good early bridge mold. |
| Growth/Resilience | TierCap / defense | Easy | High | Low | good | good | good | ok | poor | poor | Solid readable filler; may be slightly stronger than pure onboarding molds. |
| AI7 | Surge tempo | Medium | Medium | Medium | poor | ok | best | good | ok | poor | Better once board/player count supports tempo patterns. |
| AI8 | Control | Medium | Medium | Medium | poor | good | best | good | ok | poor | Good mid-campaign generalist. |
| AI9 | Control / cascade | Medium | Medium | Medium | poor | ok | good | best | good | ok | Better when the board is large enough for escalation. |
| AI11 | Surge tempo | Medium | Medium | Medium/High | poor | ok | good | best | good | ok | More chaotic/swingy than simpler control lines. |
| Grow>Kill>Reclaim(Econ) | Economy / reclamation | Medium | Medium | Medium | poor | ok | good | best | good | ok | Good generalist pressure/economy hybrid. |
| Grow>Kill>Reclaim(Econ/Reclaim) | Reclamation | Medium | Medium | Medium | poor | ok | good | good | good | ok | Similar fit to the econ variant; may be redundant. |
| Creeping>Necrosporulation | Bloom | Medium | High | Medium | poor | ok | good | best | good | ok | Readable bloom identity; good for teaching themed opponents. |
| TST_AnabolicBeaconNecroRegressionCascade | Bloom / control | Medium | Medium | Medium/High | poor | poor | ok | good | ok | poor | More complex line; probably not for early campaign. |
| TST_AnabolicCreepingNecroRegressionCascade | Bloom / control | Medium | Medium | Medium | poor | poor | good | good | good | ok | Better mid/late than early; more coherent than beacon version. |
| AI13 | Control | Hard | Medium | Medium | poor | poor | ok | good | best | good | Hard control option; likely overlaps some with modern control lines. |
| TST_BalancedControl_AnabolicFirst | Control | Hard | Medium | Low/Medium | poor | poor | ok | good | best | good | Strong stable hard control candidate. |
| Power Mutations Max Econ | Economy spike | Hard | Low/Medium | High | poor | poor | ok | good | best | best | Strong late-game / spike identity; probably too swingy for early use. |
| AI1 | Bloom / pressure | Elite | Medium | Medium/High | poor | poor | poor | ok | best | best | Elite late-game anchor. |
| AI2 | Bloom / reclamation | Elite | Medium | Medium | poor | poor | poor | ok | best | best | Strong elite anchor; performed very well in late campaign sweep. |
| AI3 | Control / cascade | Elite | Low/Medium | High | poor | poor | poor | ok | good | best | Strong but probably lower-readability than simpler molds. |
| AI10 | Bloom / late spike | Elite | Medium | High | poor | poor | poor | ok | good | best | Good endgame support/boss-adjacent mold. |
| TST_CreepingNecroRegressionCascade | Bloom / control | Elite | Medium | Medium | poor | poor | ok | good | best | best | Best proven Bloom line from recent sims; strong late-campaign candidate. |

### Initial takeaways

- **Best early-campaign candidates**: `AI6`, `AI12`, `Growth/Resilience`
- **Best medium-band candidates**: `AI7`, `AI8`, `AI9`, `AI11`, `Grow>Kill>Reclaim(Econ)`, `Creeping>Necrosporulation`
- **Best hard-band candidates**: `AI13`, `TST_BalancedControl_AnabolicFirst`, `Power Mutations Max Econ`, `TST_CreepingNecroRegressionCascade`
- **Best elite/endgame anchors**: `AI1`, `AI2`, `AI3`, `AI10`, `TST_CreepingNecroRegressionCascade`
- **Most likely overlap to resolve later**:
  - `AI13` vs `TST_BalancedControl_AnabolicFirst`
  - `Grow>Kill>Reclaim(Econ)` vs `Grow>Kill>Reclaim(Econ/Reclaim)`
- **Most likely weak early-campaign fits despite being campaign-eligible**:
  - `TST_AnabolicBeaconNecroRegressionCascade`
  - `Power Mutations Max Econ`
  - elite anchors (`AI1`, `AI2`, `AI3`, `AI10`)

## Recommended keep / review / drop decisions

### Keep in the curated campaign pool

#### Early / onboarding
- `AI6`
- `AI12`
- `Growth/Resilience`

#### Medium / core campaign pool
- `AI7`
- `AI8`
- `AI9`
- `AI11`
- `Grow>Kill>Reclaim(Econ)`
- `Creeping>Necrosporulation`
- `TST_AnabolicCreepingNecroRegressionCascade`

#### Hard / late-campaign pool
- `AI13`
- `TST_BalancedControl_AnabolicFirst`
- `Power Mutations Max Econ`
- `TST_CreepingNecroRegressionCascade`

#### Elite / endgame anchors
- `AI1`
- `AI2`
- `AI3`
- `AI10`

### Keep for review, but not core pool yet
- `Grow>Kill>Reclaim(Econ/Reclaim)`
  - likely redundant with `Grow>Kill>Reclaim(Econ)` unless it proves meaningfully different in campaign feel
- `TST_AnabolicBeaconNecroRegressionCascade`
  - campaign-eligible, but probably too complex / too weakly differentiated for early inclusion

### Review before inclusion
- `AI4`
- `AI5`
  - now partially characterized: both look wrong for the current `Campaign5-7` bridge and should stay out of that band unless a later context-specific screen finds a better home.
  - authored-board 20-game safe-proxy screens with seed `20260327`:
    - `Campaign5` replacing the easy filler with `AI4` -> `25.0% (5/20)`, avg living `246.3`, avg dead `172.8`
    - `Campaign5` replacing the easy filler with `AI5` -> `30.0% (6/20)`, avg living `238.6`, avg dead `165.5`
    - `Campaign6` replacing `CMP_Bloom_CreepingNecro_Medium` with `AI4` -> `25.0% (5/20)`, avg living `657.5`, avg dead `431.2`
    - `Campaign6` replacing `CMP_Bloom_CreepingNecro_Medium` with `AI5` -> `25.0% (5/20)`, avg living `615.0`, avg dead `472.4`
    - `Campaign7` replacing `CMP_TierCap_GrowthResilience_Easy` with `AI4` -> `5.0% (1/20)`, avg living `646.0`, avg dead `393.4`
    - `Campaign7` replacing `CMP_TierCap_GrowthResilience_Easy` with `AI5` -> `5.0% (1/20)`, avg living `639.5`, avg dead `428.9`
  - practical read: `AI4` is roughly side-grade-to-sharper in `Campaign5-6` and still too hard for `Campaign7`; `AI5` is outright dominant on the `Campaign6` board and also not a safe `Campaign7` filler.

### Drop from the first alpha campaign pass
- none permanently retired yet
- but do **not** try to use every eligible AI in the first alpha campaign curation pass

## Recommended redundancy calls

### Keep both for now
- `AI13`
- `TST_BalancedControl_AnabolicFirst`

Reason:
- they likely overlap, but they serve an important purpose as stable hard control options
- we should only collapse them after campaign feel-testing shows they are too similar in practice

### Prefer keeping this one in the core pool
- keep: `Grow>Kill>Reclaim(Econ)`
- review only: `Grow>Kill>Reclaim(Econ/Reclaim)`

Reason:
- the econ variant is the cleaner generalist inclusion
- the reclaim-heavy variant can stay as a reserve if we later need more reclamation flavor

### Prefer keeping this one in the core pool
- keep: `TST_AnabolicCreepingNecroRegressionCascade`
- review only: `TST_AnabolicBeaconNecroRegressionCascade`

Reason:
- the anabolic-creeping version seems like the cleaner medium-campaign Bloom/control candidate
- the beacon version feels more niche and less obviously useful for first-pass campaign curation

## Cleaner first-alpha campaign pool

If we want a practical first shipping pool before the full random-pool system exists, I would use this:

### Training / easy
- `AI6`
- `AI12`
- `Growth/Resilience`

### Medium
- `AI7`
- `AI8`
- `AI9`
- `AI11`
- `Grow>Kill>Reclaim(Econ)`
- `Creeping>Necrosporulation`
- `TST_AnabolicCreepingNecroRegressionCascade`

### Hard
- `AI13`
- `TST_BalancedControl_AnabolicFirst`
- `Power Mutations Max Econ`
- `TST_CreepingNecroRegressionCascade`

### Elite
- `AI1`
- `AI2`
- `AI3`
- `AI10`

This gives a pool that is:
- broad enough to feel varied
- small enough to reason about
- less cluttered by near-duplicates and unreviewed strategies

## First-alpha progression guidance

Using the cleaned-up pool, the progression should roughly become:
- **Campaign0-2:** training/easy only
- **Campaign3-5:** easy + medium
- **Campaign6-8:** medium-heavy with occasional hard preview
- **Campaign9-11:** medium + hard
- **Campaign12-14:** hard + elite, with occasional medium support

That should produce a cleaner curve than trying to cram every campaign-eligible AI into the first alpha.

## Earliest recommended campaign introduction levels

This is a working guardrail for future tuning and for the upcoming pool system. Do not introduce these molds earlier than the levels below unless we are intentionally stress-testing difficulty.

| Strategy | Earliest recommended level | Notes |
|---|---:|---|
| `CMP_Reclaim_Scavenger_Easy` | 1 | Safe early duel-pool mold; direct 15x15 safe-proxy screen landed near the current Campaign1 band. |
| `CMP_Surge_Pulsar_Easy` | 1 | Soft early duel-pool mold; weaker than Scavenger, but safe to include in the first easy pool. |
| `CMP_TierCap_GrowthResilience_Easy` | 4 | Soft bridge mold; useful when a level needs to get slightly harder without spiking. |
| `CMP_Economy_KillReclaim_Medium` | 5 | First strong modern medium generalist. |
| `CMP_Bloom_CreepingNecro_Medium` | 6 | First readable Bloom-style medium. |
| `CMP_Bloom_AnabolicRegression_Medium` | 8 | Better as a later-medium mold once players are handling layered opponents. Follow-up screens kept it out of `Campaign6-7`: `Campaign6` swap test landed at only `20.0% (4/20)` proxy wins, and a `Campaign7` 50-game confirmation replacing the easy filler still held the proxy at `6.0% (3/50)`, effectively no softer than the current authored lineup. |
| `CMP_Bloom_BeaconRegression_Medium` | 8 | Still sharp, but it held the current Campaign8 proxy near `10%`; acceptable as the first stronger medium insert. |
| `CMP_Surge_GrowthTempo_Medium` | 9 | Reserve for later medium / hard-preview use. A `Campaign6` swap screen still landed at only `15.0% (3/20)` proxy wins, so it is not a good answer for filling the early-to-medium bridge yet. |
| `CMP_Bloom_FortifyMimic_Medium` | 9 | Campaign8 first-introduction test overshot badly; keep it later than Beacon for now. |
| `CMP_Growth_Pressure_Medium` | 9 | Too sharp for Campaign5 in direct swap testing; reserve for later medium / hard-preview use. |
| `CMP_Economy_LateSpike_Hard` | 10 | Good first hard introduction candidate. |

### Campaign5-7 pooled bridge follow-up (2026-03-27 late)

First-pass pooled screens were run by taking the current authored fixed lineup for each level, adding one already-screened easy bridge mold, then enumerating the omit-one resolved subsets (10 games each, seed `20260327`) with the safe proxy on the exact authored board.

- **Campaign5** (`50x50`, 5 AI): current lineup plus `CMP_Reclaim_Scavenger_Easy` produced a plausible pool band.
  - Resolved variants landed between `10.0%` and `30.0%` proxy wins, averaging about `18.3%` across the six omit-one subsets.
  - Best short-screen survivors were:
    - omit `CMP_Control_AnabolicRebirth_Medium` -> `30.0% (3/10), avg living 262.1, avg dead 195.8`
    - omit `CMP_Reclaim_Scavenger_Easy` (the current fixed lineup) -> `30.0% (3/10), avg living 232.9, avg dead 215.3`
  - Practical read: poolification here looks **viable**, but not obviously better than the current fixed lineup yet.

- **Campaign6** (`75x75`, 4 AI): current lineup plus `CMP_TierCap_GrowthResilience_Easy` also stayed in a reasonable bridge band.
  - Resolved variants landed between `20.0%` and `30.0%`, averaging about `24.0%` across the five omit-one subsets.
  - Short-screen highs were:
    - omit `CMP_Bloom_CreepingNecro_Medium` -> `30.0% (3/10), avg living 663.8, avg dead 505.7`
    - omit `CMP_TierCap_GrowthResilience_Easy` (the current fixed lineup) -> `30.0% (3/10), avg living 656.5, avg dead 466.7`
  - Practical read: poolification here also looks **viable**, with no evidence yet that the easy bridge breaks the level.

- **Campaign7** (`90x90`, 6 AI): adding a single easy bridge did **not** solve the harshness problem.
  - Current lineup plus `CMP_Reclaim_Scavenger_Easy` resolved between `0.0%` and `10.0%`, averaging only about `4.3%` across seven omit-one subsets.
  - Current lineup plus `CMP_Surge_Pulsar_Easy` resolved between `0.0%` and `10.0%`, averaging only about `5.7%` across seven omit-one subsets.
  - Representative results:
    - Scavenger pool, omit `CMP_Surge_BeaconTempo_Medium` -> `10.0% (1/10), avg living 668.0, avg dead 442.7`
    - Pulsar pool, omit `AI9` -> `10.0% (1/10), avg living 651.4, avg dead 458.2`
    - several other resolved subsets for both pools stayed at `0.0% (0/10)`.
  - Practical read: if `Campaign7` is going to be pooled, it probably needs a **more structural softening** than just adding one extra easy mold to the current medium-heavy shell.
| `AI13` | 10 | Legacy hard control option; use carefully if kept alongside modern hard controls. |
| `CMP_Control_AnabolicFirst_Hard` | 11 | Too punishing as an early hard preview. |
| `CMP_Bloom_CreepingRegression_Elite` | 12 | Modern elite Bloom/control anchor. |
| `AI10` | 12 | Elite late-spike pressure mold. |
| `AI3` | 12 | Elite control/cascade; lower readability than simpler molds. |
| `AI2` | 13 | Very strong elite anchor. |
| `AI1` | 13 | Spiky elite pressure mold; too punishing when introduced too early. |

### Rule of thumb by band
- **Campaign0-4:** training / easy only
- **Campaign5-7:** core mediums only
- **Campaign8-9:** stronger or sharper mediums, but still avoid true hard spikes unless carefully cushioned
- **Campaign10-11:** first real hard molds
- **Campaign12-14:** hard + elite territory

## Next steps

1. Review and adjust the keep/review/drop recommendations.
2. Decide whether `AI4` and `AI5` need dedicated characterization runs before alpha.
3. Use the cleaned-up pool to refine the target Campaign0-14 progression.
4. After that, do the actual `CMP_*` rename migration and update Unity board presets.
5. Then implement the campaign AI pool system.
