# Build Instructions

See also: [README.md](README.md) for the full documentation hierarchy.

To build the solution from the command line (Cursor/CLI), only build the following projects:

1. FungusToast.Core
2. FungusToast.Simulation

Use these commands:

```sh
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
```

**Note:**
- Unity projects (such as FungusToast.Unity.csproj and Assembly-CSharp.csproj) are built by the Unity Editor, not by dotnet build.
- The Core post-build copy/touch step runs only on Windows. On Linux/WSL/macOS, `dotnet build` succeeds but does not copy the DLL into Unity automatically.
- If you need Unity to pick up fresh Core changes on Linux/WSL/macOS, run `./FungusToast.Core/copy_to_unity.sh` after building. It copies the DLL artifacts into `FungusToast.Unity/Assets/Plugins/` and touches `ForceRecompile.cs`.
- If you encounter Unity script errors, copy and paste them into Cursor for troubleshooting.

This order ensures all dependencies are built correctly and avoids circular dependency issues.

## Local itch.io Release Flow

For Windows-first local releases to itch.io, use the release helper script at `scripts/publish_itch_release.ps1`.

Typical first-time setup:

1. Install butler and confirm `butler version` works, or set `BUTLER_PATH` to your `butler.exe`.
2. Copy `scripts/itch-release.example.json` to `scripts/itch-release.local.json` and fill in your itch.io `itchTarget` and preferred `channel`.
3. Ensure a compatible Unity editor is installed for the version in `FungusToast.Unity/ProjectSettings/ProjectVersion.txt`, or pass `-UnityPath`.

`scripts/itch-release.local.json` is intentionally ignored by git so you can store machine-local publishing settings without committing them.

Build without uploading:

```powershell
./scripts/publish_itch_release.ps1 -Version 1.2.0 -BuildOnly
```

Build and upload:

```powershell
./scripts/publish_itch_release.ps1 -Version 1.2.0
```

Useful options:

- `-DryRun` previews the butler push without uploading.
- `-IfChanged` skips the push when the release folder matches the latest build on the target channel.
- `-ItchTarget`, `-Channel`, `-ButlerPath`, `-UnityPath`, and `-BuildOutputPath` override config values per run.
- `-SkipSimulationBuild` keeps the release flow focused on the Windows build if you do not want the extra validation step.

The script performs these steps:

1. Builds `FungusToast.Core`.
2. Builds `FungusToast.Simulation` unless `-SkipSimulationBuild` is supplied.
3. Runs Unity in batch mode via `ReleaseBuildAutomation.BuildWindowsRelease`.
4. Produces a clean Windows release folder and writes a `version.txt` file for the build.
5. Pushes that folder to itch.io with `butler push ... --userversion <version>` unless `-BuildOnly` is supplied.

Notes:

- The Windows release build is written to `Builds/itch/windows` by default.
- The Unity release script uses the enabled scenes in `FungusToast.Unity/ProjectSettings/EditorBuildSettings.asset`.
- For local interactive publishing, run `butler login` once before your first upload.
- For future CI or headless publishing, prefer the `BUTLER_API_KEY` environment variable instead of storing credentials in the repo.