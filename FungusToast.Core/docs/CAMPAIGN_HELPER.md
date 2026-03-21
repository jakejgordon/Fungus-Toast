# Campaign Helper

See also: [README.md](README.md) for the full documentation hierarchy and [ADAPTATION_HELPER.md](ADAPTATION_HELPER.md) for Adaptation-specific workflow.

This guide documents the current Campaign mode vision and where to configure progression, AI lineup, and adaptation rewards.

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
- `boardPresetId`, `boardWidth`, `boardHeight`, `unlockedMutationTierMax`

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

### 2) Board + AI lineup per level
Configured in assets under:
- `FungusToast.Unity/Assets/Configs/Board Presets/`

`BoardPreset` fields:
- `presetId`
- `boardWidth`, `boardHeight`
- `mutationTierMax`
- `aiPlayers`: ordered list of strategy names (for campaign this should be `AI1..AI13`)

Runtime usage:
- In campaign mode, `PlayerInitializer` reads `BoardPreset.aiPlayers` and resolves each `strategyName` via:
  - `AIRoster.CampaignStrategiesByName`
  - fallback: `AIRoster.ProvenStrategiesByName`

If lineup is invalid/incomplete, a random campaign fallback is used.

## Difficulty Buckets (Convention)

There is no baked-in weak/moderate/hard enum yet, so use this naming convention when authoring board presets:

- Weak AI:
  - `AI6` (tier-capped strategy)
  - `AI12`
  - `AI13`
- Moderate AI:
  - `AI7`
  - `AI8`
  - `AI9`
  - `AI11`
- Hard AI:
  - `AI1`
  - `AI2`
  - `AI3`
  - `AI10`

These buckets are tuning heuristics and can change as balance data evolves.

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
1. Change strategy names in relevant `BoardPreset.aiPlayers` lists.
2. Optionally adjust board sizes and `mutationTierMax`.
3. Validate in Unity; campaign simulation is not supported yet.

## Notes

- Keep campaign logic deterministic where possible.
- Prefer data changes in assets and repositories over hardcoded branching.
- Keep Core adaptation definitions Unity-free.
- For non-campaign work, default validation is build + smoke simulation. Campaign changes need Unity-side validation instead.
