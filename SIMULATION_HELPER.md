# Fungus Toast Simulation Helper

> **📚 Related Documentation**: For technical architecture and design principles, see [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md)

This document contains the most effective commands for running different simulation scenarios and debugging the Fungus Toast game.

## Quick Commands

> **IMPORTANT:** Always run `run_simulation.ps1` from the project root directory (where the script is located),
> NOT from inside the `FungusToast.Simulation` folder. Running from the wrong directory will cause build errors
> due to incorrect relative paths.

## Output File Location

Simulation output files are automatically created in:
```
FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\
```

The full path is displayed in the console after each simulation run, making it easy for AI assistants to locate and read the results.

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

### AI/Cursor File Access
After each simulation, the output file path is displayed in **two places**:

1. **PowerShell script output** (main console):
   ```
   Output will be written to: FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\sim_output_2025-07-04T13-07-22.txt
   ```

2. **Simulation output** (in the simulation window):
   ```
   Simulation output redirected to: C:\Users\jakej\FungusToast\FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\sim_output_2025-07-04T13-07-22.txt
   ```

**AI/Cursor can:**
- **Use the PowerShell path** - Copy the relative path from the main console
- **Use the simulation path** - Copy the absolute path from the simulation window  
- **Find latest file** - If exact path isn't available:
   ```powershell
   Get-ChildItem "FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
   ```

**Note**: The output files are always created in `FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\` with timestamps in the filename.

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

#### Simulation Script Path Errors
- **Always run `run_simulation.ps1` from the project root directory.**
- If you run it from inside `FungusToast.Simulation`, you may see errors like:
  - `MSBUILD : error MSB1009: Project file does not exist.`
  - `Build failed for FungusToast.Core. Exiting.`
- To fix: Change to the root directory and run:
  ```powershell
  .\run_simulation.ps1 --games 1 --players 2
  ```

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
- **Output files**: After simulation, read the full path displayed in console output
- **File location**: All output files are in `FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\`
- **Latest file**: Use `Get-ChildItem` with `Sort-Object LastWriteTime -Descending` to find most recent output 