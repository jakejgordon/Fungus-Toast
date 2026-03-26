# Fungus Toast Simulation Helper

> **📚 Related Documentation**: For technical architecture context, see [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md). For the full documentation hierarchy, see [README.md](README.md).

This document contains the most effective commands for running different simulation scenarios and debugging the Fungus Toast game.

## Quick Commands

> **IMPORTANT:** The `run_simulation.ps1` script is located in the `FungusToast.Simulation` directory.
> You can run it from there directly, or from any directory by specifying the full path.

## Output File Location

**All simulations now automatically create output files** in:
```
FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\
```
Output files are generated with timestamped filenames even when no `--output` parameter is specified. The full path is displayed in the console after each simulation run, making it easy for AI assistants to locate and read the results.

## Minimum Balance Reporting Standard

For balance discussions, report these per-player metrics at minimum:

1. `WinRate` (% of games won)
2. `Avg Alive` (average living cells at game end)
3. `Avg End Toxins` (average toxin cells owned at game end)

Where to find them:

- Console output: `=== Per-Player Summary ===`
- Parquet export: `players.parquet`

Fallback if older output files do not include per-player end-toxin values:

- Use game-level `Avg Lingering Toxic Tiles` from `=== Game-Level Stats ===` and note that it is not per-player.

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

3. **Run Using PowerShell Script:**
   ```powershell
   .\run_simulation.ps1 --games 1 --players 8 --no-keyboard
   ```
   
   **Tip:** Add `--no-keyboard` for non-interactive runs to avoid accidental `Q`/`Escape` interruption.

4. **Direct dotnet run (if scripts fail):**
   ```powershell
   # Build projects first
   dotnet build "../FungusToast.Core/FungusToast.Core.csproj"
   dotnet build "FungusToast.Simulation.csproj"

   # Run simulation
   dotnet run -- --games 1 --players 8 --no-keyboard
   ```

## Automation-Safe Commands

Use these for CI/agents or unattended runs:

```powershell
# Fast non-interactive smoke test
dotnet run -- --games 1 --players 2 --width 40 --height 40 --no-keyboard

# Equivalent alias
dotnet run -- --games 1 --players 2 --width 40 --height 40 --non-interactive
```

### For AI Assistants:

Default workflow preference for this repository:

1. **Run the simulation for the user when asked.**
2. **Run analytics scripts for the user when asked.**
3. Fall back to manual user-run instructions only if tool/terminal execution is blocked.

When automated terminal execution fails:

1. **Inform the user** to run the simulation manually using the steps above
2. **Ask for the output file path** once the simulation completes
3. **Use a file-read tool** (for example `read_file`) to read and analyze simulation results
4. **Expected output path format:** `FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\Simulation_output_YYYY-MM-DDTHH-mm-ss.txt`

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

### Run with Custom Output Filename
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
   No --output specified. Simulation will auto-generate a timestamped filename.
   ```
   OR
   ```
   Output will be written to: bin\Debug\net8.0\SimulationOutput\custom_filename.txt
   ```

2. **Simulation output** (in the simulation window):
   ```
   Simulation output redirected to: C:\Users\cogord\GitHub Repos\Fungus-Toast\FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput\Simulation_output_2025-01-15T14-30-22.txt
   ```

**AI/Cursor can:**
- **Use the simulation path** - Copy the absolute path from the simulation window output
- **Find latest file** - If exact path isn't available:
  ```powershell
  Get-ChildItem "bin\Debug\net8.0\SimulationOutput\*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  ```

**Note**: Output files are **always created automatically** in `bin\Debug\net8.0\SimulationOutput\` with either:
- Auto-generated timestamped filenames: `Simulation_output_YYYY-MM-DDTHH-mm-ss.txt`
- Custom filenames when using `--output filename.txt`

## Command Line Options

The simulation supports the following command-line parameters:

| Parameter | Short | Description | Default |
|-----------|-------|-------------|---------|
| `--games` | `-g` | Number of games to play per matchup | 1 |
| `--players` | `-p` | Number of players/strategies to use | 8 |
| `--width` | `-w` | Board width (number of tiles) | 160 |
| `--height` | | Board height (number of tiles) | 160 |
| `--strategy-set` | `-s` | Strategy pool to sample from (`Proven`, `Testing`, `Mycovariants`, `Campaign`) | `Testing` |
| `--strategy-names` | | Explicit strategy names for single-run mode; overrides `--players` | Off |
| `--selection-policy` | | Strategy sampler (`RandomUnique`, `CoverageBalanced`, `StratifiedCycle`) | `CoverageBalanced` |
| `--player-counts` | | Batch strata list for players (CSV), e.g. `2,4,8` | Off |
| `--board-sizes` | | Batch strata list for board sizes (CSV), e.g. `80x80,160x160` | Off |
| `--strategy-sets` | | Batch strata list for strategy sets (CSV) | Off |
| `--seed` | | Base deterministic seed for strategy sampling and per-game seeds | 0 |
| `--rotate-slots` | | Rotate strategy-to-player slot assignment every game | Off |
| `--fixed-slots` | | Keep strategy-to-player slot assignment fixed | On |
| `--experiment-id` | | Export tag for analytics artifacts | Auto timestamp ID |
| `--parquet` | | Export canonical Parquet datasets | On |
| `--no-parquet` | | Disable Parquet export | Off |
| `--output` | `-o` | Specify output filename (optional) | Auto-generated timestamp |
| `--no-keyboard` | | Disable keyboard interruption (`Q`/`Escape`) | Off |
| `--non-interactive` | | Alias for `--no-keyboard` | Off |
| `--help` | | Show help message | |

### Examples:
```powershell
# Run with defaults - creates auto-generated output file
dotnet run

# Run 10 games per matchup - creates auto-generated output file
dotnet run --games 10

# Run 4 players, 20 games each - creates auto-generated output file
dotnet run --players 4 --games 20

# Run 6 players, 15 games each (short form) - creates auto-generated output file
dotnet run -p 6 -g 15

# Run with custom board dimensions - creates auto-generated output file
dotnet run --width 50 --height 75

# Run 4 players on 200x100 board - creates auto-generated output file
dotnet run -w 200 -p 4

# Combine multiple options with custom output filename
dotnet run --width 150 --height 120 --players 6 --games 25 --output large_board_test.txt

# Deterministic proven strategy experiment with slot rotation and Parquet export
dotnet run --games 200 --players 8 --strategy-set Proven --seed 12345 --rotate-slots --experiment-id proven_seed12345 --no-keyboard

# Fixed explicit lineup for reproducible balance passes (single-run mode only)
# Note: explicit names are resolved only inside the selected --strategy-set; do not mix Proven/Testing/Campaign/Mycovariants names in one --strategy-names list.
# Current behavior: --fixed-slots also keeps starting-spore assignment fixed, so it is a true position test.
# For balance/fairness evaluation, prefer --rotate-slots. For true position studies, use --fixed-slots with an explicit lineup.
dotnet run --games 200 --strategy-set Proven --strategy-names StrategyA,StrategyB,StrategyC,StrategyD --seed 12345 --rotate-slots --experiment-id proven_fixed_lineup --no-keyboard

# Coverage-balanced testing run for statistically diverse strategy lineups
dotnet run --games 200 --players 8 --strategy-set Testing --selection-policy CoverageBalanced --seed 12345 --rotate-slots --experiment-id testing_cov_balanced --no-keyboard

# Best practice: keep the emitted manifest.json with analysis artifacts so lineup provenance stays attached to results

# Deterministic strategy cycle across strata
dotnet run --games 150 --player-counts 4,8 --board-sizes 120x120,160x160 --strategy-sets Testing --selection-policy StratifiedCycle --seed 12345 --experiment-id testing_cycle --no-keyboard

# Stratified batch run across player counts, board sizes, and strategy sets
dotnet run --games 50 --player-counts 2,4,8 --board-sizes 80x80,160x160 --strategy-sets Testing,Proven --seed 12345 --rotate-slots --experiment-id stratified_v1 --no-keyboard

# Non-interactive run for automation (ignores Q/Escape key interruption)
dotnet run --games 1 --players 2 --no-keyboard
```

## Parquet Analytics Export

When Parquet export is enabled, simulation writes analytics datasets to:

```
FungusToast.Simulation\bin\Debug\net8.0\SimulationParquet\<experiment-id>\
```

Files produced:

- `games.parquet`
- `players.parquet`
- `mutations.parquet`
- `mycovariants.parquet`
- `upgrade_events.parquet`
- `manifest.json`

These files are designed for downstream AI/statistical analysis in `FungusToast.Analytics`.

`manifest.json` is the canonical run-provenance record: it captures strategy set, selection policy, whether the lineup was sampled or explicitly provided, and the exact selected strategy lineup in order.

`upgrade_events.parquet` logs ordered mutation upgrades for each player with round, level change, mutation points before/after spend, and source (`manual`, `surge`, `auto`).

## Canonical Experiment Best Practices

Use this as the default process when a run is meant to support balance decisions or cross-revision comparisons.

For controlled diagnosis of a dominant strategy, including early mutation timing and mycovariant timing questions, use `DOMINANCE_DIAGNOSIS_WORKFLOW.md`.

- Prefer explicit `--strategy-names` for canonical comparison runs so lineup composition does not drift with roster edits.
- Treat sampled-roster runs (`--players` + `--strategy-set`) as exploratory unless you keep the emitted `manifest.json` and intentionally compare the same sampled lineup.
- Keep `--seed`, `--selection-policy`, slot policy (`--fixed-slots`/`--rotate-slots`), board size, player count, and strategy set fixed while comparing revisions.
- Always set an explicit `--experiment-id` for named experiments so downstream analysis folders are stable and easy to diff.
- Keep `manifest.json` with any exported CSV/Parquet analysis outputs; it is the authoritative roster provenance record.
- `--strategy-names` is resolved against exactly one `--strategy-set`. A named-lineup experiment cannot mix roster sets in the same run.
- If you need cross-set coverage, run separate experiments per set (for example `..._proven` and `..._testing`) instead of combining names from multiple rosters.

### Fairness-testing workflow (do this by default)

For starting-position fairness studies and other symmetry-sensitive simulation work:

- **Do not edit source files just to test a candidate layout.** Use simulation parameters instead.
- Use explicit lineups via `--strategy-names` so every slot gets the same AI (or another intentionally fixed lineup).
- For pure slot-fairness testing, disable asymmetry sources unless they are the subject of the test:
  - `--no-nutrient-patches`
  - `--no-mycovariants`
- Use `--starting-positions x1:y1,x2:y2,...` to test a candidate layout without changing `StartingSporeUtility`.
- Use `--fixed-slots` for true position studies; use `--rotate-slots` for general balance tests where slot bias should average out.
- Use explicit `--output` and `--experiment-id` values when comparing multiple candidates so each run can be traced unambiguously.
- For candidate bakeoffs, use a staged process:
  1. short screening runs (for example 20 games each)
  2. longer confirmation runs (for example 100 games) only for the best survivors
- Only promote a layout into the precomputed fast-path after it survives the longer clean validation.

## Starting Position Fairness Notes

`StartingSporeUtility` now exposes two relevant APIs:

- `GetStartingPositions(width, height, playerCount)` → returns the chosen start coordinates
- `GetStartingPositionAnalysis(width, height, playerCount)` → returns the geometric fairness analysis for those positions

The analysis includes, per slot:
- coordinate
- uncontested tile count
- early uncontested tile count (within ~10 tiles)
- tie tile count
- favor rank
- overall layout score

This is intended as a reusable difficulty/balance lever, especially for campaign tuning when certain slots should be intentionally favored or disadvantaged.

### Saved reference: 160x160, 8 players

Current chosen layout:
- P0 `(142,106)`
- P1 `(106,142)`
- P2 `(54,142)`
- P3 `(18,106)`
- P4 `(18,54)`
- P5 `(54,18)`
- P6 `(106,18)`
- P7 `(142,54)`

True-fixed identical-AI validation:
- Seed `20260323`: `16,10,10,14,9,13,14,14`
- Seed `20260324`: `10,9,16,8,13,16,14,14`

Compared to the previous layout, this reduced the observed slot-win range from `15` (`22..7`) to `7-8` (`16..9` and `16..8`) in the 160x160 / 8-player square tests.

### Precomputed fast-path layouts

To avoid expensive startup-time layout searches during normal gameplay, canonical square-board reference layouts are now stored for the most important player counts and scaled to the active board size at runtime.

Current fast-path behavior:
- `1` player: direct center placement (`boardWidth / 2`, `boardHeight / 2`)
- `2-8` players: use precomputed `160x160` reference coordinates, scaled to the target board size
- other player counts: fall back to the search-based layout generator

Current precomputed references:
- 2 players: `(128,80)`, `(32,80)`
- 3 players: `(141,80)`, `(50,133)`, `(50,27)`
- 4 players: `(128,128)`, `(32,128)`, `(32,32)`, `(128,32)`
- 5 players: `(114,104)`, `(67,120)`, `(38,80)`, `(67,40)`, `(114,56)`
- 6 players: `(136,95)`, `(92,126)`, `(37,123)`, `(24,65)`, `(68,34)`, `(123,37)`
- 7 players: `(139,94)`, `(106,135)`, `(54,135)`, `(21,94)`, `(32,42)`, `(80,19)`, `(128,42)`
- 8 players: `(142,106)`, `(106,142)`, `(54,142)`, `(18,106)`, `(18,54)`, `(54,18)`, `(106,18)`, `(142,54)`

Scaling note:
- These reference coordinates are scaled independently across width and height, then de-duplicated if rounding would collide positions on smaller boards.
- This means the game now starts from the intended slot geometry without recomputing an expensive search for player counts `2-8`.

### Saved reference: 160x160, 3 players

Selected current layout:
- P0 `(141,80)`
- P1 `(50,133)`
- P2 `(50,27)`

Clean identical-AI validation (`--fixed-slots --no-nutrient-patches --no-mycovariants`):
- Seed `20260327`: `33,35,32`

Observed win rates:
- P0 `33%`
- P1 `35%`
- P2 `32%`

Observed slot-win range: `3`.

This replaced the prior 3-player auto-selected layout, which had a clean 100-game result of `38,28,34` (range `10`).

### Saved reference: 160x160, 4 players

Selected symmetric square-board layout:
- P0 `(128,128)`
- P1 `(32,128)`
- P2 `(32,32)`
- P3 `(128,32)`

Assumption for square boards:
- For 4 players on a square board, a symmetric equidistant corner layout should be geometrically even enough that dedicated tuning is lower priority than 3/5/6/7-player cases.
- This layout is therefore treated as the canonical 4-player fast-path reference unless future simulation evidence shows a meaningful residual bias.

### Saved reference: 160x160, 5 players

Selected current layout:
- P0 `(114,104)`
- P1 `(67,120)`
- P2 `(38,80)`
- P3 `(67,40)`
- P4 `(114,56)`

Clean identical-AI validation (`--fixed-slots --no-nutrient-patches --no-mycovariants`):
- Seed `20260327`: `22,18,22,16,22`

Observed win rates:
- P0 `22%`
- P1 `18%`
- P2 `22%`
- P3 `16%`
- P4 `22%`

Observed slot-win range: `6`.

This replaced the prior 5-player auto-selected layout, which had a clean 100-game result of `22,15,27,14,22` (range `13`).

### Saved reference: 160x160, 6 players

Selected current layout:
- P0 `(136,95)`
- P1 `(92,126)`
- P2 `(37,123)`
- P3 `(24,65)`
- P4 `(68,34)`
- P5 `(123,37)`

Clean identical-AI validation (`--fixed-slots --no-nutrient-patches --no-mycovariants`):
- Seed `20260327`: `17,12,19,16,18,18`

Observed win rates:
- P0 `17%`
- P1 `12%`
- P2 `19%`
- P3 `16%`
- P4 `18%`
- P5 `18%`

Observed slot-win range: `7`.

This was the best-performing tested 6-player candidate among the 100-game clean confirmations run in this pass.

The search-based fallback remains in place for other player counts and for future tuning passes.

## Repeatability Notes

Current simulation entrypoints are mostly seed-driven:

- roster sampling uses the run seed (or derived stratum seed in batch mode),
- each game gets a deterministic per-game seed derived from the base seed plus game index,
- core simulation phases share that per-game `Random` instance through game setup, draft, growth, decay, and mutation processors.

Strict reproducibility note:

- `ParameterizedSpendingStrategy.GetShuffledCategories()` now uses the same seeded per-game `Random` instance as the rest of the simulation path, removing the prior `Guid.NewGuid()` fallback-ordering divergence.

Practical guidance: for the strongest apples-to-apples comparisons, still keep `--seed`, lineup/selection policy, slot policy, board size, and player count fixed when re-running.

In batch mode (`--player-counts` / `--board-sizes` / `--strategy-sets`), each stratum is exported to a separate folder with suffixes like:

- `<experiment-id>__p8_w160_h160_sTesting`

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
- **GitHub Copilot Terminal Issues:** If terminal execution hangs or fails, use manual execution
- **PowerShell Script Hanging:** Try the simplified scripts or direct `dotnet run`
- **Build Path Errors:** Ensure you're running from the correct directory (`FungusToast.Simulation`)

## Key Files for Debugging

### Core Game Logic
- `FungusToast.Core/Phases/MutationEffectProcessor.cs` - Mutation effects
- `FungusToast.Core/Board/GameBoard.cs` - Game events
- `FungusToast.Core/Events/` - Event definitions

### Simulation Tracking
- `FungusToast.Simulation/Models/SimulationTrackingContext.cs` - Partial class shell (`ISimulationObserver` type)
- `FungusToast.Simulation/Models/SimulationTrackingContext.CoreStatMetrics.cs` - Mutation points, income, banked points, death-reason totals
- `FungusToast.Simulation/Models/SimulationTrackingContext.FirstUpgradeRoundMetrics.cs` - First-acquired round tracking and summary stats
- `FungusToast.Simulation/Models/SimulationTrackingContext.SupportEconomyMetrics.cs` - Surgical inoculation and rejuvenation cycle reduction
- `FungusToast.Simulation/Models/SimulationTrackingContext.DefenseMobilityMetrics.cs` - Neutralization, bastion counts, creeping mold toxin jumps
- `FungusToast.Simulation/Models/SimulationTrackingContext.GrowthTransferMetrics.cs` - Perimeter proliferator, resistance transfers, enduring toxaphore extensions
- `FungusToast.Simulation/Models/SimulationTrackingContext.ReclaimFortifyMetrics.cs` - Reclamation rhizomorphs, necrophoric adaptation, ballistospore, chitin fortification
- `FungusToast.Simulation/Models/SimulationTrackingContext.CombatEffectMetrics.cs` - Putrefactive cascade, mimetic resilience, cytolytic burst
- `FungusToast.Simulation/Models/SimulationTrackingContext.RelocationRegenerationMetrics.cs` - Chemotactic relocations and hypersystemic regeneration effects
- `FungusToast.Simulation/Models/SimulationTrackingContext.RegressionMetrics.cs` - Ontogenic regression and competitive antagonism tracking
- `FungusToast.Simulation/Models/SimulationTrackingContext.GameplayMetrics.cs` - Remaining gameplay/event metrics and observer stubs
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
6. ✅ Implement tracking in `SimulationTrackingContext` partials (place in the most relevant `SimulationTrackingContext.*.cs` file)
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

## Notes for AI Assistant

- **Script Location**: The `run_simulation.ps1` script is in `FungusToast.Simulation` directory
- **Usage**: Run from `FungusToast.Simulation` directory or use full path from anywhere
- **Auto-builds**: Script automatically builds both Core and Simulation projects
- **New Window**: Simulation runs in separate PowerShell window for monitoring
- **Manual Fallback**: When automated execution fails, guide users to manual execution steps
- **Output files**: **Always generated automatically** - check simulation window output for exact filename
- **File location**: All output files are in `bin\Debug\net8.0\SimulationOutput\`
- **Filename format**: `Simulation_output_YYYY-MM-DDTHH-mm-ss.txt` (auto-generated) or custom name with `--output`
- **Latest file**: Use `Get-ChildItem` with `Sort-Object LastWriteTime -Descending` to find most recent output
- **Board dimensions**: Support for custom `--width` and `--height` parameters for testing different board sizes