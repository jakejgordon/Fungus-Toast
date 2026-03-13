# Adaptation Technical Flow

This guide covers technical implementation and integration for Adaptations across Core, campaign runtime, and Unity flow.

## Quick Workflow

1. Add or update metadata in `AdaptationRepository.cs`.
2. Add or update tunables in `AdaptationGameBalance.cs`.
3. Decide whether the Adaptation is start-of-level, passive recurring, or both.
4. Wire effect logic in core processors/helpers.
5. Wire campaign reward selection and startup application paths if needed.
6. Add simulation tracking only when the effect changes shared core behavior worth reporting.
7. Validate with Core + Simulation builds for shared logic, and Unity for campaign flow.

---

## Source of Truth

Adaptation metadata lives in:
- `FungusToast.Core/Campaign/AdaptationRepository.cs`
- `FungusToast.Core/Campaign/AdaptationIds.cs`

Campaign ownership/persistence lives in:
- `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignState.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignController.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/Campaign/CampaignSaveService.cs`

Recurring gameplay hooks currently route through core/runtime processors such as:
- `FungusToast.Core/Phases/AdaptationEffectProcessor.cs`
- `FungusToast.Core/Events/GameRulesEventSubscriber.cs`

Use stable Adaptation IDs from the repository and campaign state; do not duplicate ownership state elsewhere.

---

## Step-by-Step

### 1) IDs, Metadata, and Balance
- Add or update the Adaptation entry in `AdaptationRepository.cs`.
- Keep IDs stable in `AdaptationIds.cs`.
- Put tunable numbers in `AdaptationGameBalance.cs`.
- Reuse balance constants in gameplay logic and user-facing copy.

### 2) Classify the Effect Cadence
Adaptations usually fall into one of these buckets:
- **Start-of-level**: applied when a campaign level begins.
- **Passive recurring**: checked during events/phases throughout a game.
- **Hybrid**: start-of-level setup plus recurring behavior.

Be explicit about cadence in the description and implementation.

### 3) Core Effect Logic
- Keep gameplay logic deterministic and Unity-free.
- Put recurring effect logic in core processors/helpers.
- Prefer extending existing processors before creating new ones.
- Use `player.HasAdaptation(...)` or `player.GetAdaptation(...)` checks at the execution site.

### 4) Campaign Runtime Wiring
If the Adaptation changes campaign flow or level-start state:
- ensure reward selection is represented in `selectedAdaptationIds`,
- ensure the effect is applied when starting/resuming the next level,
- ensure pending reward state survives save/load.

Campaign ownership is driven by `CampaignState`; avoid shadow copies of selected Adaptations.

### 5) Event and Phase Wiring
If timing depends on lifecycle hooks, wire into the appropriate runtime path, such as:
- `GameRulesEventSubscriber.cs`
- `AdaptationEffectProcessor.cs`
- campaign startup/resume paths in `CampaignController` and Unity initialization code

### 6) Simulation Tracking and Reporting
Only add simulation reporting when the Adaptation changes shared core behavior that is useful to analyze.

If reporting is needed:
- extend `ISimulationObserver`,
- implement tracking in `SimulationTrackingContext` partials,
- map data into result/output builders.

Do not treat simulation as the validation path for campaign reward flow itself.

### 7) Unity Validation
After Adaptation changes, verify in Unity:
- campaign save/load,
- reward selection,
- post-victory continue flow,
- resumed pending reward flow,
- any in-game UI display of active Adaptations.

---

## Common Pitfalls

- Treating Adaptations as stackable when current campaign rules are unique-per-run.
- Putting campaign ownership state outside `CampaignState`.
- Reusing Mycovariant draft assumptions for Adaptation reward flow without checking campaign behavior.
- Adding simulation hooks for campaign-only concerns that cannot actually be exercised in simulation.
- Leaving cadence unclear in copy (`start of level` vs recurring passive behavior).

---

## Validation Checklist

After Adaptation changes:
1. `dotnet build FungusToast.Core/FungusToast.Core.csproj`
2. `dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj` when shared core behavior changed
3. Verify the affected campaign flow in Unity
4. If shared gameplay behavior changed, run a smoke simulation for the non-campaign portion of that behavior
