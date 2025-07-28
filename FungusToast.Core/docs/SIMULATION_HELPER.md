# Fungus Toast Simulation Helper

> **📚 Related Documentation**: For technical architecture and design principles, see [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md)

This document contains the most effective commands for running different simulation scenarios and debugging the Fungus Toast game.

## Quick Commands

> **IMPORTANT:** The `run_simulation.ps1` script is located in the `FungusToast.Simulation` directory.
> You can run it from there directly, or from any directory by specifying the full path.

## Output File Location

Simulation output files are automatically created in:
```
FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\
```
The full path is displayed in the console after each simulation run, making it easy for AI assistants to locate and read the results.

## Running Simulations

### Using the PowerShell Script (Recommended)
The PowerShell script automatically builds projects and launches simulations in new windows:
```powershell
# From FungusToast.Simulation directory:
cd FungusToast.Simulation
.\run_simulation.ps1 --games 100 --players 8

# Or from any directory using full path:
.\FungusToast.Simulation\run_simulation.ps1 --games 100 --players 8
```

### Direct dotnet run (Alternative)
```powershell
cd FungusToast.Simulation
dotnet run -- --games 100 --players 8
```

## Manual Execution (When Automated Tools Fail)

> **When to Use:** If automated execution via GitHub Copilot tools is hanging or failing, use this manual approach.

### For Humans in Visual Studio:

1. **Open Terminal in Visual Studio:**
   - Go to `View → Terminal` or press `Ctrl+`` (backtick)
   - Ensure you're using PowerShell (not Command Prompt)

2. **Navigate to Simulation Directory:**
   ```powershell
   cd FungusToast.Simulation
   ```

3. **Run Using Simple Script:**
   ```powershell
   .\run_simulation_simple.ps1 -Games 1 -Players 8
   ```
   
   **Alternative scripts available:**
   - `.\run_simulation_simple.ps1` - User-friendly with clear feedback
   - `.\run_simulation.ps1` - Original script (manual mode)

4. **Direct dotnet run (if scripts fail):**
   ```powershell
   # Build projects first
   dotnet build "../FungusToast.Core/FungusToast.Core.csproj"
   dotnet build "FungusToast.Simulation.csproj"

   # Run simulation
   dotnet run -- --games 1 --players 8
   ```

### For AI Assistants:

When automated terminal execution fails:

1. **Inform the user** to run the simulation manually using the steps above
2. **Ask for the output file path** once the simulation completes
3. **Use `get_file` tool** to read and analyze the simulation results
4. **Expected output path format:** `FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\sim_output_YYYY-MM-DDTHH-mm-ss.txt`

### Common Manual Execution Issues:

- **PowerShell Execution Policy:** If scripts won't run, try:
  ```powershell
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
  ```
- **Build Errors:** Ensure both Core and Simulation projects build successfully before running
- **Path Issues:** Always run from the `FungusToast.Simulation` directory

## Simulation Scenarios

### Quick Testing (1 game)
```powershell
.\run_simulation.ps1 --games 1 --players 2
```

### Mutation Testing (focus on specific mutations)
```powershell
.\run_simulation.ps1 --games 10 --players 2
```

### Balance Testing (longer games)
```powershell
.\run_simulation.ps1 --games 50 --players 4
```

### Stress Testing (many games)
```powershell
.\run_simulation.ps1 --games 100 --players 2
```

### AI Strategy Testing
```powershell
.\run_simulation.ps1 --games 20 --players 3
```

### Board Size Testing
```powershell
# Test with smaller board
.\run_simulation.ps1 --games 10 --players 4 --width 50 --height 50

# Test with larger board
.\run_simulation.ps1 --games 10 --players 8 --width 150 --height 120

# Test with rectangular board
.\run_simulation.ps1 --games 5 --players 6 --width 200 --height 75
```

## Debugging Commands

### Check for Build Errors
```powershell
cd FungusToast.Core; dotnet build --verbosity normal
cd FungusToast.Simulation; dotnet build --verbosity normal
```

### Run with Output Redirection
```powershell
.\run_simulation.ps1 --games 1 --players 2 --output test_results.txt
```

### Check Simulation Output Files
```powershell
ls bin\Debug\net8.0\SimulationOutput\
```

### AI/Cursor File Access
After each simulation, the output file path is displayed in **two places**:

1. **PowerShell script output** (main console):
   ```
   Output will be written to: bin\Debug\net8.0\SimulationOutput\sim_output_2025-07-04T13-07-22.txt
   ```

2. **Simulation output** (in the simulation window):
   ```
   Simulation output redirected to: C:\Users\cogord\GitHub Repos\Fungus-Toast\FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\sim_output_2025-07-04T13-07-22.txt
   ```

**AI/Cursor can:**
- **Use the PowerShell path** - Copy the relative path from the main console
- **Use the simulation path** - Copy the absolute path from the simulation window  
- **Find latest file** - If exact path isn't available:
  ```powershell
  Get-ChildItem "bin\Debug\net8.0\SimulationOutput\*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  ```

**Note**: The output files are always created in `bin\Debug\net8.0\SimulationOutput\` with timestamps in the filename.

## Command Line Options

The simulation supports the following command-line parameters:

| Parameter | Short | Description | Default |
|-----------|-------|-------------|---------|
| `--games` | `-g` | Number of games to play per matchup | 100 |
| `--players` | `-p` | Number of players/strategies to use | 8 |
| `--width` | `-w` | Board width (number of tiles) | 100 |
| `--height` | | Board height (number of tiles) | 100 |
| `--output` | `-o` | Redirect output to a file | (console) |
| `--help` | | Show help message | |

### Examples:
```powershell
# Run with defaults (8 players, 100 games each, 100x100 board)
dotnet run

# Run 10 games per matchup
dotnet run --games 10

# Run 4 players, 20 games each
dotnet run --players 4 --games 20

# Run 6 players, 15 games each (short form)
dotnet run -p 6 -g 15

# Run with custom board dimensions
dotnet run --width 50 --height 75

# Run 4 players on 200x100 board
dotnet run -w 200 -p 4

# Combine multiple options
dotnet run --width 150 --height 120 --players 6 --games 25 --output large_board_test.txt
```

## Common Issues & Solutions

### PowerShell Syntax Issues
- Use `Start-Process` instead of `start` for opening new windows
- Use semicolons `;` instead of `&&` for command chaining
- Quote paths with spaces

### Build Issues
- The script automatically builds both `FungusToast.Core` and `FungusToast.Simulation`
- Check for missing using statements or namespace issues
- Verify all event handlers are properly subscribed

### Simulation Issues
- Check that mutation events are firing correctly
- Verify tracking context is recording events
- Ensure PlayerResult fields are populated from tracking context

### Automated Tool Failures
- **GitHub Copilot Terminal Issues:** If `run_command_in_terminal` hangs or fails, use manual execution
- **PowerShell Script Hanging:** Try the simplified scripts or direct `dotnet run`
- **Build Path Errors:** Ensure you're running from the correct directory (`FungusToast.Simulation`)

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

- **Script Location**: The `run_simulation.ps1` script is in `FungusToast.Simulation` directory
- **Usage**: Run from `FungusToast.Simulation` directory or use full path from anywhere
- **Auto-builds**: Script automatically builds both Core and Simulation projects
- **New Window**: Simulation runs in separate PowerShell window for monitoring
- **Manual Fallback**: When automated execution fails, guide users to manual execution steps
- **Output files**: After simulation, read the full path displayed in console output
- **File location**: All output files are in `bin\Debug\net8.0\SimulationOutput\`
- **Latest file**: Use `Get-ChildItem` with `Sort-Object LastWriteTime -Descending` to find most recent output
- **Board dimensions**: Support for custom `--width` and `--height` parameters for testing different board sizes