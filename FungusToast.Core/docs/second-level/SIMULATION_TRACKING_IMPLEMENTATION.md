# Simulation Tracking Implementation

> **Related Documentation**: For mutation authoring workflow, see [../NEW_MUTATION_HELPER.md](../NEW_MUTATION_HELPER.md). For technical architecture and event-driven runtime patterns, see [../ARCHITECTURE_OVERVIEW.md](../ARCHITECTURE_OVERVIEW.md). For simulation commands and output conventions, see [../SIMULATION_HELPER.md](../SIMULATION_HELPER.md). For the full documentation hierarchy, see [../README.md](../README.md).

This document covers the implementation-side wiring required when new gameplay effects need to appear in simulation exports and usage summaries.

Use it for:
- adding new simulation-tracked gameplay effects,
- extending `ISimulationObserver`,
- updating `SimulationTrackingContext` partials,
- surfacing tracked values into `PlayerResult` and summary output.

This is **not** the place for simulation run commands or balance-reporting workflow. Those stay in `SIMULATION_HELPER.md`.

## Event Tracking Checklist

When adding new mutation effects:

1. Create an event args class in `FungusToast.Core/Events/` when the effect needs a Core event surface.
2. Add the event delegate and event to `GameBoard.cs` when Unity or generalized listeners need to react.
3. Add the corresponding `OnEventName` helper to `GameBoard.cs`.
4. Fire the event in the relevant Core processor, commonly `MutationEffectProcessor.cs` or another phase/effect processor.
5. Add a tracking method to `ISimulationObserver.cs` when Simulation exports need an explicit metric.
6. Implement tracking in the most relevant `SimulationTrackingContext` partial file.
7. Add the tracked field to `PlayerResult.cs`.
8. Populate the field in `GameResult.From()`.
9. Add display handling in `PlayerMutationUsageTracker.cs` when the metric should appear in simulation output summaries.

## Typical Wiring Pattern

```csharp
// 1. Event args
public class NewEffectEventArgs : EventArgs
{
    public int PlayerId { get; }
    public int EffectCount { get; }
}

// 2. GameBoard event
public delegate void NewEffectEventHandler(object sender, NewEffectEventArgs e);
public event NewEffectEventHandler? NewEffect;
public virtual void OnNewEffect(NewEffectEventArgs e) => NewEffect?.Invoke(this, e);

// 3. Fire event in Core processor
var args = new NewEffectEventArgs(playerId, count);
board.OnNewEffect(args);

// 4. Track in observer
observer.RecordNewEffect(playerId, count);

// 5. Add to PlayerResult
public int NewEffectCount { get; set; }

// 6. Populate in GameResult
NewEffectCount = tracking.GetNewEffectCount(player.PlayerId),

// 7. Display in tracker
case MutationIds.NewMutation:
    if (player.NewEffectCount > 0)
        effects["Effect Name"] = player.NewEffectCount;
    break;
```

## Current SimulationTrackingContext File Map

Use the most relevant partial file instead of dumping everything into one place.

- `FungusToast.Simulation/Models/SimulationTrackingContext.cs` - partial class shell (`ISimulationObserver` type)
- `FungusToast.Simulation/Models/SimulationTrackingContext.CoreStatMetrics.cs` - mutation points, income, banked points, death-reason totals
- `FungusToast.Simulation/Models/SimulationTrackingContext.FirstUpgradeRoundMetrics.cs` - first-acquired round tracking and summary stats
- `FungusToast.Simulation/Models/SimulationTrackingContext.SupportEconomyMetrics.cs` - surgical inoculation and rejuvenation cycle reduction
- `FungusToast.Simulation/Models/SimulationTrackingContext.DefenseMobilityMetrics.cs` - neutralization, bastion counts, creeping mold toxin jumps
- `FungusToast.Simulation/Models/SimulationTrackingContext.GrowthTransferMetrics.cs` - perimeter proliferator, resistance transfers, enduring toxaphore extensions
- `FungusToast.Simulation/Models/SimulationTrackingContext.ReclaimFortifyMetrics.cs` - reclamation rhizomorphs, necrophoric adaptation, ballistospore, chitin fortification
- `FungusToast.Simulation/Models/SimulationTrackingContext.CombatEffectMetrics.cs` - putrefactive cascade, mimetic resilience, cytolytic burst
- `FungusToast.Simulation/Models/SimulationTrackingContext.RelocationRegenerationMetrics.cs` - chemotactic relocations and hypersystemic regeneration effects
- `FungusToast.Simulation/Models/SimulationTrackingContext.RegressionMetrics.cs` - ontogenic regression and competitive antagonism tracking
- `FungusToast.Simulation/Models/SimulationTrackingContext.GameplayMetrics.cs` - remaining gameplay and observer metrics

## Placement Guidance

- Put simulation-export concerns here or in mutation/mycovariant authoring docs, not in simulation run-command docs.
- Prefer Core events when multiple runtime layers need the signal.
- Prefer `ISimulationObserver` methods when the need is specifically export/analytics visibility.
- Keep the tracked field names aligned across observer methods, tracking dictionaries, `PlayerResult`, and display labels.

## Related Files

- `FungusToast.Core/Metrics/ISimulationObserver.cs`
- `FungusToast.Simulation/Models/SimulationTrackingContext*.cs`
- `FungusToast.Simulation/Models/PlayerResult.cs`
- `FungusToast.Simulation/Models/GameResult.cs`
- `FungusToast.Simulation/Analysis/PlayerMutationUsageTracker.cs`
