# Campaign Helper

See also: [README.md](README.md) for the full documentation hierarchy and [ADAPTATION_HELPER.md](ADAPTATION_HELPER.md) for Adaptation-specific workflow.

This guide documents the current Campaign mode vision and where to configure progression, AI lineup/pools, campaign tiers, and adaptation rewards.

## Vision

Campaign is a roguelike-style run:
- Player starts on a small board versus a limited AI lineup.
- Losing ends the run (run state resets).
- Winning a non-final level grants an Adaptation draft (3 choices, pick 1, no duplicates across the run).
- After selecting an Adaptation, player can Continue Campaign or return to Main Menu.
- Final level win shows completion messaging.

## Current Data Model

Campaign state is persisted in:
- `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignState.cs`

Important fields:
- `levelIndex`: current level (0-based)
- `selectedAdaptationIds`: unique adaptation IDs chosen this run
- `pendingAdaptationSelection`: true between level victory and adaptation selection
- `campaignCompleted`: true after final victory
- `pendingVictorySnapshot`: persisted endgame snapshot shown when resuming into a pending adaptation pick
- `boardPresetId`, `boardWidth`, `boardHeight`

Save file:
- `Application.persistentDataPath/campaign_save.json`

## Adaptations

Core adaptation catalog:
- `FungusToast.Core/Campaign/AdaptationRepository.cs`

Rules:
- Adaptations are unique per run.
- Campaign reward draft draws from remaining (unselected) adaptations.
- If no adaptations remain, campaign advances without a reward pick.

## Campaign Progression Configuration

### 1) Level sequence
Configured in:
- `FungusToast.Unity/Assets/Configs/Campaign/CampaignProgression.asset`

Each level points to a `BoardPreset` asset and can independently enable or disable nutrient patches via `enableNutrientPatches`.

Nutrient patches are optional board resource clusters: `Adaptogen` patches grant mutation-point income, while `Sporemeal` patches grant free growth across the rest of the claimed cluster.

### 2) Board + AI lineup or pool per level
Configured in assets under:
- `FungusToast.Unity/Assets/Configs/Board Presets/`

`BoardPreset` fields:
- `presetId`
- `boardWidth`, `boardHeight`
- `aiPlayers`: ordered fixed lineup of AI specs; preserves current behavior and remains the preferred option when a level wants exact opponents
- `pooledAiPlayerCount`: optional active AI count when using a strategy pool instead of a fixed lineup
- `aiStrategyPool`: optional pool of eligible strategy IDs for the level

Runtime usage:
- In campaign mode, fixed lineups still win: if `BoardPreset.aiPlayers` is populated, `PlayerInitializer` resolves those exact names via:
  - `AIRoster.CampaignStrategiesByName`
  - fallback: `AIRoster.ProvenStrategiesByName`
- If the fixed lineup is empty but `aiStrategyPool` is populated, `CampaignController` deterministically selects `pooledAiPlayerCount` unique strategy names from the eligible pool for that run/level and persists the resolved lineup in campaign save state so resume does not reshuffle opponents.

If the resolved lineup is invalid/incomplete, a random campaign fallback is used.

Current authored usage:
- Campaign0 and Campaign1 are the first levels intentionally converted to pooled selection while still spawning exactly 1 AI each.
- Later levels are currently still authored primarily as fixed lineups while campaign tuning continues.

## Official internal terminology

### Campaign Tier
A **Campaign Tier** is the internal difficulty grouping for a set of campaign levels that broadly share the same eligible AI pool and difficulty expectations.

Current intended tier language:
- `Training Tier`
- `Easy Tier`
- `Medium Tier`
- `Hard Tier`
- `Elite Tier`
- optional later: `Boss Tier`

Use **Campaign Tier** for the conceptual difficulty grouping or level band.

Examples:
- `Campaign0-2` are mainly in the `Training/Easy Tier` space
- `Campaign5-9` are mainly in the `Medium Tier`
- `Campaign12-14` are in `Hard/Elite Tier` territory

### AI Pool
An **AI Pool** is the actual set of eligible campaign strategy IDs that a level or campaign tier can draw from.

Examples:
- a `Medium Tier` pool may contain several curated `CMP_*` medium molds
- a specific `BoardPreset` may override the broader tier pool with a curated fixed lineup or a smaller per-level pool

### Fixed lineup vs pooled lineup
- **Fixed lineup**: exact `aiPlayers` list is authored in the preset and used as-is
- **Pooled lineup**: `aiStrategyPool` is authored and the runtime deterministically picks `pooledAiPlayerCount` unique opponents for that run/level

## Difficulty metadata and current campaign tiers

Campaign difficulty now has a formal metadata enum: `CampaignDifficulty`.
Use that as the primary campaign-facing difficulty signal.
`DifficultyBands` still exist as broader catalog/simulation tags, but campaign curation should prefer `CampaignDifficulty` for level-pool decisions.

Current internal Campaign Tier intent:
- **Training/Easy Tier**
  - training molds like `TST_Training_ResilientMycelium`, `TST_Training_Overextender`, `TST_Training_ToxicTurtle`
  - easy curated molds like `CMP_TierCap_GrowthResilience_Easy`, `CMP_Reclaim_Scavenger_Easy`
  - select legacy easy bridges like `AI6`, `AI12`
- **Medium Tier**
  - legacy mediums like `AI7`, `AI8`, `AI9`, `AI11`
  - curated mediums like `CMP_Economy_KillReclaim_Medium`, `CMP_Bloom_CreepingNecro_Medium`, `CMP_Bloom_AnabolicRegression_Medium`, `CMP_Bloom_BeaconRegression_Medium`, `CMP_Bloom_FortifyMimic_Medium`, `CMP_Growth_Pressure_Medium`, `CMP_Surge_Pulsar_Easy` (promoted upward in intent; medium-tier use)
- **Hard Tier**
  - `CMP_Control_AnabolicFirst_Hard`
  - `CMP_Economy_LateSpike_Hard`
  - `AI13`
- **Elite Tier**
  - `AI1`, `AI2`, `AI3`, `AI10`
  - `CMP_Bloom_CreepingRegression_Elite`

These are working curation heuristics and can change as balance data evolves.

## Campaign strategy naming direction

Legacy campaign strategies currently use simple IDs like `AI1..AI13`.
For curated campaign work, prefer a descriptive stable naming convention:

- `CMP_<Theme>_<CorePlan>_<Difficulty>`

Examples:
- `CMP_Control_AnabolicCreeping_Hard`
- `CMP_Bloom_CreepingRegression_Elite`
- `CMP_TierCap_GrowthResilience_Easy`
- `CMP_Bloom_FortifyMimic_Medium`

Guidelines:
- `CMP_` prefix for campaign-curated identities
- concise theme/archetype first (`Control`, `Bloom`, `Economy`, `Surge`, `Defense`, `TierCap`, `Reclaim`)
- core plan second (`AnabolicCreeping`, `CreepingRegression`, `LateSpike`, etc.)
- difficulty suffix last (`Easy`, `Medium`, `Hard`, `Elite`, `Boss`)
- keep names stable once referenced by `BoardPreset` assets

## Runtime Flow Touchpoints

Main campaign orchestration:
- `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignController.cs`

Endgame integration:
- `FungusToast.Unity/Assets/Scripts/Unity/Services/EndgameService.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_EndGamePanel.cs`

Adaptation draft UI reuse:
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MycovariantDraft/MycovariantDraftController.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MycovariantDraft/MycovariantCard.cs`

## Common Tasks

### Add a new campaign level
1. Create a new `BoardPreset` asset under `Assets/Configs/Board Presets/`.
2. Set board size and `aiPlayers` strategy names.
3. Add that preset reference to `CampaignProgression.asset`.
4. Verify `levelIndex` ordering is 0..N-1.

### Add more adaptations
1. Add entries to `AdaptationRepository.All`.
2. Keep IDs stable and unique (`adaptation_#` or slug style).
3. Wire gameplay effects through core/runtime hooks when needed.

### Tune difficulty
1. Adjust the relevant fixed lineup (`BoardPreset.aiPlayers`) or curated AI pool (`aiStrategyPool` + `pooledAiPlayerCount`).
2. Optionally adjust board sizes and nutrient-patch settings.
3. Use campaign-safe `CMP_*` identities where possible so simulation tooling and curated docs stay aligned.
4. Validate in Unity; CLI-side campaign validation now also exists via `scripts/run_campaign_balance.py` using the safe player proxy.

## Notes

- Keep campaign logic deterministic where possible.
- Prefer data changes in assets and repositories over hardcoded branching.
- Keep Core adaptation definitions Unity-free.
- For non-campaign work, default validation is build + smoke simulation. Campaign changes need Unity-side validation instead.
