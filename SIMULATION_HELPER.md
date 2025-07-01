# Fungus Toast Simulation Helper

> **📚 Related Documentation**: For technical architecture and design principles, see [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md)

This document contains the most effective commands for running different simulation scenarios and debugging the Fungus Toast game.

## Quick Commands

### Open New PowerShell Window for Simulation
```powershell
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'C:\Users\jakej\FungusToast\FungusToast.Simulation'; dotnet run -- --games 1 --players 2"
```

### Build Projects
```powershell
cd FungusToast.Core; dotnet build
cd FungusToast.Simulation; dotnet build
```

### Run Basic Simulation
```powershell
cd FungusToast.Simulation
dotnet run -- --games 1 --players 2
```

## Simulation Scenarios

### Quick Testing (1 game)
```powershell
dotnet run -- --games 1 --players 2
```

### Mutation Testing (focus on specific mutations)
```powershell
dotnet run -- --games 10 --players 2
```

### Balance Testing (longer games)
```powershell
dotnet run -- --games 50 --players 4
```

### Stress Testing (many games)
```powershell
dotnet run -- --games 100 --players 2
```

### AI Strategy Testing
```powershell
dotnet run -- --games 20 --players 3
```

### Test Specific Features
```powershell
# Test Neutralizing Mantle
dotnet run -- --test-neutralizing

# Test Resistant cell system
dotnet run -- --test-resistant

# Test Mycelial Bastion
dotnet run -- --test-bastion
```

## Debugging Commands

### Check for Build Errors
```powershell
cd FungusToast.Core; dotnet build --verbosity normal
cd FungusToast.Simulation; dotnet build --verbosity normal
```

### Run with Output Redirection
```powershell
dotnet run -- --games 1 --players 2 --output test_results.txt
```

### Check Simulation Output Files
```powershell
ls FungusToast.Simulation\SimulationOutput\
```

## Common Issues & Solutions

### PowerShell Syntax Issues
- Use `Start-Process` instead of `start` for opening new windows
- Use semicolons `;` instead of `&&` for command chaining
- Quote paths with spaces: `'C:\Users\jakej\FungusToast\FungusToast.Simulation'`

### Build Issues
- Always build `FungusToast.Core` first, then `FungusToast.Simulation`
- Check for missing using statements or namespace issues
- Verify all event handlers are properly subscribed

### Simulation Issues
- Check that mutation events are firing correctly
- Verify tracking context is recording events
- Ensure PlayerResult fields are populated from tracking context

## Key Files for Debugging

### Core Game Logic
- `FungusToast.Core/Core/Phases/MutationEffectProcessor.cs` - Mutation effects
- `FungusToast.Core/Core/Board/GameBoard.cs` - Game events
- `FungusToast.Core/Events/` - Event definitions

### Simulation Tracking
- `FungusToast.Simulation/Models/SimulationTrackingContext.cs` - Event tracking
- `FungusToast.Simulation/Models/PlayerResult.cs` - Result fields
- `FungusToast.Simulation/Models/GameResult.cs` - Result population

### Output Display
- `FungusToast.Simulation/Analysis/PlayerMutationUsageTracker.cs` - Effect display
- `FungusToast.Simulation/OutputManager.cs` - Output redirection

## Event Tracking Checklist

When adding new mutation effects:

1. ✅ Create event args class in `FungusToast.Core/Events/`
2. ✅ Add event delegate and event to `GameBoard.cs`
3. ✅ Add `OnEventName` method to `GameBoard.cs`
4. ✅ Fire event in `MutationEffectProcessor.cs`
5. ✅ Add tracking method to `ISimulationObserver.cs`
6. ✅ Implement tracking in `SimulationTrackingContext.cs`
7. ✅ Add field to `PlayerResult.cs`
8. ✅ Populate field in `GameResult.From()`
9. ✅ Add display case in `PlayerMutationUsageTracker.cs`

## Useful Patterns

### Adding New Mutation Effect
```csharp
// 1. Event args
public class NewEffectEventArgs : EventArgs
{
    public int PlayerId { get; }
    public int EffectCount { get; }
    // ... other properties
}

// 2. GameBoard event
public delegate void NewEffectEventHandler(object sender, NewEffectEventArgs e);
public event NewEffectEventHandler? NewEffect;
public virtual void OnNewEffect(NewEffectEventArgs e) => NewEffect?.Invoke(this, e);

// 3. Fire event
var args = new NewEffectEventArgs(playerId, count);
board.OnNewEffect(args);

// 4. Track in observer
observer?.RecordNewEffect(playerId, count);

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

## Notes for AI Assistant

- Always use `Start-Process powershell` for new windows
- Build Core project first, then Simulation
- Check event tracking checklist when adding new effects
- Use semicolons for PowerShell command chaining
- Quote paths with spaces
- Reference this file for command syntax 