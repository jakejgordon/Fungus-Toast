# Fungus Toast - GitHub Copilot Instructions

## Repository Overview

Fungus Toast is a 2D Unity game where each player represents a mold colony trying to take the largest share of the toast. The repository contains three main projects:

- **FungusToast.Core**: Core business logic (.NET Standard 2.1) - ~189 C# files containing game mechanics, mutations, AI, and simulation logic
- **FungusToast.Simulation**: Simulation runner (.NET 8.0) for running many-game simulations, AI testing, and balance-tuning
- **FungusToast.Unity**: Unity game project (Unity 6000.0.45f1) with UI, graphics, and game presentation layer

**Key Facts:**
- Total codebase: ~189 C# files with ~2,200+ lines of code
- Language: C# with some PowerShell scripting
- Primary target: Windows development environment with Unity Editor
- Architecture: Clean separation between core logic, simulation, and presentation

## Build Instructions

### Prerequisites
- .NET 8.0 SDK (for FungusToast.Simulation)
- .NET Standard 2.1 compatible SDK (for FungusToast.Core)
- Unity Editor 6000.0.45f1 (for Unity project, optional for core logic work)
- PowerShell (for automation scripts)

### Building Projects

**IMPORTANT: Only build Core and Simulation projects with dotnet CLI. Unity projects are built by the Unity Editor.**

```bash
# Always build in this order to respect dependencies:
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
```

### Common Build Issues and Workarounds

#### Issue: PostBuild Script Fails on Linux/macOS
**Error:** `The command "call "PostBuild_CopyAndTouch.bat"" exited with code 127`

**Cause:** The FungusToast.Core project has a Windows batch file post-build step that copies the compiled DLL to Unity's Assets/Plugins folder.

**Workarounds:**
1. **For non-Unity development:** Temporarily remove the PostBuild target from `FungusToast.Core/FungusToast.Core.csproj`:
   ```xml
   <!-- Comment out or remove these lines: -->
   <!--
   <Target Name="PostBuild" AfterTargets="PostBuildEvent">
     <Exec Command="call &quot;$(ProjectDir)PostBuild_CopyAndTouch.bat&quot;" />
   </Target>
   -->
   ```

2. **For Unity integration:** Manually copy files after build:
   ```bash
   # After successful dotnet build FungusToast.Core/FungusToast.Core.csproj
   mkdir -p FungusToast.Unity/Assets/Plugins
   cp FungusToast.Core/bin/Debug/netstandard2.1/FungusToast.Core.dll FungusToast.Unity/Assets/Plugins/
   cp FungusToast.Core/bin/Debug/netstandard2.1/FungusToast.Core.pdb FungusToast.Unity/Assets/Plugins/
   ```

#### Build Time Expectations
- **FungusToast.Core**: ~6 seconds (clean build), ~2 seconds (incremental)
- **FungusToast.Simulation**: ~5 seconds (clean build), ~1 second (incremental)

#### Common Warnings (Non-blocking)
The codebase has some expected nullable reference warnings that don't prevent compilation:
- `CS8600`: Converting null literal to non-nullable type
- `CS8602`: Dereference of possibly null reference  
- `CS0067`: Unused events

## Running Simulations

### Using PowerShell Script (Recommended)
The simulation includes a comprehensive PowerShell script for building and execution:

```powershell
# Navigate to simulation directory
cd FungusToast.Simulation

# Run simulation with automatic building
.\run_simulation.ps1 --games 100 --players 8

# Custom output filename
.\run_simulation.ps1 --games 50 --players 4 --output "my_test.txt"

# Quick test run
.\run_simulation.ps1 --games 1 --players 2
```

### Direct dotnet Execution
```bash
cd FungusToast.Simulation
dotnet run -- --games 100 --players 8 --output simulation_results.txt
```

### Simulation Output Location
**All simulation output is automatically saved to:**
```
FungusToast.Simulation/bin/Debug/net8.0/SimulationOutput/
```

**Output Files:**
- Auto-generated timestamped filenames: `Simulation_output_YYYY-MM-DDTHH-mm-ss.txt`
- Custom filenames when using `--output` parameter
- Full absolute path is displayed in console after simulation completion

### Command Line Options
```
-g, --games <number>     Number of games to play per matchup (default: 500)
-p, --players <number>   Number of players/strategies to use (default: 8)  
-w, --width <number>     Board width (default: 100)
--height <number>        Board height (default: 100)  
-o, --output <filename>  Custom output filename (default: auto-generated)
--help                   Show help message
```

**Note:** Default game count is 500 for statistical significance, but use `--games 1` for quick testing.

### Simulation Troubleshooting

#### PowerShell Execution Policy Issues
If scripts won't run on Windows:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### Automated Tool Failures
The PowerShell script includes detection for automated execution environments. If GitHub Copilot tools hang:

1. **Manual execution:** Run the simulation manually using Visual Studio terminal
2. **Get output path:** After completion, use the printed absolute path
3. **File analysis:** Use file reading tools to analyze results

## Project Architecture and Layout

### Repository Root Structure
```
.
├── .gitattributes                 # Git configuration
├── .gitignore                     # Comprehensive ignore rules for Unity + .NET
├── LICENSE                        # License file
├── FungusToast.Core/             # Core game logic (.NET Standard 2.1)
├── FungusToast.Simulation/       # Simulation runner (.NET 8.0)
└── FungusToast.Unity/            # Unity project (Unity 6000.0.45f1)
```

### FungusToast.Core Structure
Core business logic with clean domain separation:

```
FungusToast.Core/
├── FungusToast.Core.csproj      # Project file with PostBuild step
├── PostBuild_CopyAndTouch.bat   # Windows batch script (Linux incompatible)
├── AI/                          # AI strategies and player automation
├── Board/                       # Game board, tiles, and cell management
├── Common/                      # Shared utilities and helpers
├── Config/                      # Game balance and configuration
├── Death/                       # Cell death mechanics and toxin systems
├── Events/                      # Game event system and logging
├── Growth/                      # Cell growth and expansion mechanics
├── Logging/                     # Core logging infrastructure
├── Metrics/                     # Performance and game metrics
├── Mutations/                   # Mutation tree system and upgrades
├── Mycovariants/               # Special ability drafting system
├── Phases/                     # Game phase management and processors
├── Players/                    # Player management and state
├── TileState.cs                # Core tile state definitions
└── docs/                       # Comprehensive documentation
    ├── BUILD_INSTRUCTIONS.md   # Basic build commands
    ├── DESIGN_PRINCIPLES.md    # Architecture and design philosophy
    ├── SIMULATION_HELPER.md    # Detailed simulation commands and debugging
    ├── ANIMATION_HELPER.md     # Animation system guidance
    ├── MYCOVARIANT_HELPER.md   # Mycovariant system guide
    └── NEW_MUTATION_HELPER.md  # Adding new mutations guide
```

### FungusToast.Simulation Structure
```
FungusToast.Simulation/
├── FungusToast.Simulation.csproj    # Console application project
├── Program.cs                       # Main entry point with argument parsing
├── run_simulation.ps1               # Comprehensive automation script
├── SimulationRunner.cs              # Core simulation orchestration
├── OutputManager.cs                 # Output file management and redirection
├── GameSimulator.cs                 # Individual game simulation logic
├── Analysis/                        # Specialized test runners and analysis tools
└── Models/                          # Data models for simulation results
```

### FungusToast.Unity Structure
```
FungusToast.Unity/
├── FungusToast.Unity.csproj        # Unity-generated project file (DO NOT EDIT)
├── Assembly-CSharp.csproj           # Unity-generated assembly (DO NOT EDIT)
├── .vsconfig                        # Visual Studio workload configuration
├── .editorconfig                    # Code formatting rules
├── run_simulation.ps1               # Unity-specific simulation script
├── Assets/                          # Unity assets and scripts
│   ├── Plugins/                     # Contains FungusToast.Core.dll (auto-copied)
│   ├── Scripts/Unity/               # Unity-specific C# scripts
│   ├── Scenes/                      # Unity scene files
│   └── ...                          # Other Unity assets
├── Packages/                        # Unity package manager files
├── ProjectSettings/                 # Unity project configuration
└── Library/                         # Unity build cache (ignored)
```

### Key Dependencies and Integration Points

**FungusToast.Core Dependencies:**
- .NET Standard 2.1 (Unity compatible)
- No external NuGet packages (self-contained)
- Post-build integration with Unity via DLL copying

**FungusToast.Simulation Dependencies:**
- References FungusToast.Core project
- .NET 8.0 console application
- No external dependencies

**FungusToast.Unity Dependencies:**
- Unity 6000.0.45f1
- References FungusToast.Core.dll from Assets/Plugins
- TextMeshPro (Unity package)
- Unity UI system

## Development Workflow

### Making Changes to Core Logic
1. **Edit code** in FungusToast.Core
2. **Build Core** project: `dotnet build FungusToast.Core/FungusToast.Core.csproj`
3. **For Unity testing:** Manually copy DLL to Unity Assets/Plugins (if on Linux/macOS)
4. **For simulation testing:** Build and run simulation project

### Testing Changes
1. **Unit testing:** Limited formal test infrastructure; uses simulation-based validation
2. **Simulation validation:** Use `run_simulation.ps1` with small game counts for quick validation
3. **Balance testing:** Run longer simulations with multiple strategies

### Common File Modification Patterns

**Adding New Mutations:**
- Primary location: `FungusToast.Core/Mutations/Factories/`
- Reference: `FungusToast.Core/docs/NEW_MUTATION_HELPER.md`
- Integration: Update `MutationRepository.cs`

**Adding New AI Strategies:**
- Primary location: `FungusToast.Core/AI/`
- Registration: Update `AIRoster.cs`
- Testing: Use simulation framework for validation

**Unity UI Changes:**
- Location: `FungusToast.Unity/Assets/Scripts/Unity/`
- Build: Use Unity Editor (not dotnet CLI)
- Testing: Play mode in Unity Editor

## Validation and Verification

### Pre-commit Validation Steps
1. **Build verification:**
   ```bash
   dotnet build FungusToast.Core/FungusToast.Core.csproj
   dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
   ```

2. **Simulation smoke test:**
   ```bash
   cd FungusToast.Simulation
   dotnet run -- --games 1 --players 2
   ```
   **Expected outcome:** Creates timestamped output file in `bin/Debug/net8.0/SimulationOutput/`

3. **Check output generation:**
   - Verify output file creation in `bin/Debug/net8.0/SimulationOutput/`
   - Confirm no obvious errors in console output

### No Formal CI/CD Pipeline
- **Current state:** No GitHub Actions or automated CI
- **Validation approach:** Manual testing and simulation-based verification
- **Quality assurance:** Relies on comprehensive simulation testing for balance and correctness

### Performance Considerations
- **Core logic:** Optimized for simulation speed (thousands of games)
- **Memory usage:** Designed for batch processing of multiple games
- **Simulation runtime:** 1-2 minutes per game typical; varies by strategy complexity and board size
- **Output files:** Can be 20KB+ for detailed single-game results

## Important Notes for Coding Agents

### What to Trust and Verify
1. **Trust these instructions** for build processes and project structure
2. **Verify simulation output** by examining generated files
3. **Check existing documentation** in `FungusToast.Core/docs/` for domain-specific guidance
4. **Test build commands** on your target platform (Linux workarounds may be needed)

### Platform-Specific Considerations
- **Windows:** Full compatibility with all scripts and automation
- **Linux/macOS:** Core and Simulation projects work; Unity and PostBuild script require workarounds
- **Development focus:** Primarily Windows-based with Unity Editor

### Search and Exploration Guidance
- **For mutations:** Start with `FungusToast.Core/Mutations/` and `NEW_MUTATION_HELPER.md`
- **For AI/strategy:** Examine `FungusToast.Core/AI/` and existing strategy implementations
- **For simulation:** Reference `SIMULATION_HELPER.md` for comprehensive command examples
- **For Unity integration:** Check `FungusToast.Unity/Assets/Scripts/Unity/`

### Code Quality and Conventions
- **Nullable reference types:** Enabled but with some legacy warnings
- **Architecture:** Clean domain separation between Core, Simulation, and Unity layers
- **Error handling:** Comprehensive logging and event systems for debugging
- **Testing strategy:** Simulation-based validation rather than unit tests

**When in doubt:** Consult the existing documentation in `FungusToast.Core/docs/` which contains domain-specific implementation guidance written by the project maintainers.

## Validation Examples

### Complete Build and Test Workflow
```bash
# 1. Clean build of both projects
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj

# 2. Quick functionality test
cd FungusToast.Simulation
dotnet run -- --games 1 --players 2

# 3. Verify output file creation
ls bin/Debug/net8.0/SimulationOutput/

# 4. Check help documentation
dotnet run -- --help
```

### Expected Console Output Patterns
**Successful simulation run includes:**
- Build warnings (nullable references) - these are expected
- "Simulation output redirected to: [path]" message
- Real-time game progress with turn counts and winners
- Comprehensive statistical summary at completion
- Mutation usage statistics
- Player-strategy performance breakdown

**Signs of successful completion:**
- Output file created in SimulationOutput directory
- File size typically 20KB+ for single games
- Console shows "Simulation complete." message
- No error or exception messages