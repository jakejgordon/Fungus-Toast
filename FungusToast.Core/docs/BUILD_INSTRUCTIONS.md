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
- `FungusToast.Unity/Assets/Plugins/FungusToast.Core.dll` is intentionally checked into git for Unity Cloud Build and other Unity-only CI environments that do not run `dotnet build` before opening the Unity project. This is counter to the usual preference to avoid committing build outputs, but it keeps the cloud Unity build self-contained.
- When Core code changes, rebuild `FungusToast.Core`, refresh the Unity plugin copy, and commit the updated DLL together with the source change so local Unity and cloud Unity builds stay in sync.
- If you encounter Unity script errors, copy and paste them into Cursor for troubleshooting.

This order ensures all dependencies are built correctly and avoids circular dependency issues.

## Local itch.io Release Flow

For Windows-first local releases to itch.io, use the release helper script at `scripts/publish_itch_release.ps1`.

### Deployment Version Confirmation

Before any deployment or itch.io publish, confirm the intended semantic version with the requester and update `FungusToast.Unity/version.txt`.

- Use `Major.Minor.BugFix` format.
- Confirm which level should increment before running the deployment.
- For Windows release handoff or AI-assisted publishing, present the three standard next-version choices derived from the current version: `BugFix` (`X.Y.Z` -> `X.Y.(Z+1)`), `Minor` (`X.Y.Z` -> `X.(Y+1).0`), or `Major` (`X.Y.Z` -> `(X+1).0.0`).
- Do not silently choose one of those options; require an explicit selection before updating `FungusToast.Unity/version.txt`.
- Do not assume the next version automatically, even for routine uploads.
- Keep the first line of `FungusToast.Unity/version.txt` set to the release version that should be stamped into the build and passed to butler as `--userversion`.
- `FungusToast.Unity/last-deployed-version.txt` records the last successful Windows itch.io publish. The Windows script rejects duplicate or downgraded releases against it, while the macOS Unity Cloud script only blocks versions older than that Windows release so the same semantic version can still ship on macOS.

Typical first-time setup:

1. Install butler and confirm `butler version` works, or set `BUTLER_PATH` to your `butler.exe`.
2. Copy `scripts/itch-release.example.json` to `scripts/itch-release.local.json` and fill in your itch.io `itchTarget` and preferred `channel`.
3. Ensure a compatible Unity editor is installed for the version in `FungusToast.Unity/ProjectSettings/ProjectVersion.txt`, or pass `-UnityPath`.

`scripts/itch-release.local.json` is intentionally ignored by git so you can store machine-local publishing settings without committing them.

Build without uploading:

```powershell
./scripts/publish_itch_release.ps1 -BuildOnly
```

Build and upload:

```powershell
./scripts/publish_itch_release.ps1
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
6. Updates `FungusToast.Unity/last-deployed-version.txt` after a successful non-dry-run Windows publish.

Notes:

- The Windows release build is written to `Builds/itch/windows` by default.
- The Unity release script uses the enabled scenes in `FungusToast.Unity/ProjectSettings/EditorBuildSettings.asset`.
- For local interactive publishing, run `butler login` once before your first upload.
- For future CI or headless publishing, prefer the `BUTLER_API_KEY` environment variable instead of storing credentials in the repo.
- The build output's `version.txt` file is separate from `FungusToast.Unity/version.txt`, which is the checked-in source of truth for release versioning.

## GitHub-hosted macOS Release Flow

For the public GitHub repository, use the workflow at `.github/workflows/build-macos.yml` to produce a macOS app bundle on `macos-latest` instead of building the distributable mac app on Windows.

This workflow uses `game-ci/unity-builder@v4` and the Unity editor build method `ReleaseBuildAutomation.BuildMacOSRelease` in `FungusToast.Unity/Assets/Editor/ReleaseBuildAutomation.cs`.

Because this workflow builds inside Unity on GitHub-hosted macOS runners and does not perform a preceding `dotnet build FungusToast.Core/FungusToast.Core.csproj`, the checked-in `FungusToast.Unity/Assets/Plugins/FungusToast.Core.dll` is part of the intended setup rather than an accidental binary artifact.

### One-time GitHub setup

Add these repository secrets in GitHub:

1. `UNITY_LICENSE`
2. `UNITY_EMAIL`
3. `UNITY_PASSWORD`

For a Unity Personal license, create `UNITY_LICENSE` using the GameCI activation flow and keep `UNITY_EMAIL` and `UNITY_PASSWORD` available for reactivation during the workflow.

The workflow reads the Unity version directly from `FungusToast.Unity/ProjectSettings/ProjectVersion.txt`.

### Running the workflow

1. Open the repository on GitHub.
2. Go to Actions.
3. Select `Build macOS release`.
4. Click `Run workflow`.
5. Enter the semantic version to stamp into the build, for example `1.2.0`.

On success, GitHub uploads an artifact named `fungustoast-macos-<version>` containing a `.zip` built on macOS.

If you publish that macOS artifact to itch.io through Unity Cloud Build, configure the user script path as `ci/deploy-itch.sh` relative to the Unity project root `FungusToast.Unity`. The real implementation now lives directly at `FungusToast.Unity/ci/deploy-itch.sh`; there is no repo-root duplicate script. It reads `FungusToast.Unity/version.txt` for `--userversion` and treats `FungusToast.Unity/last-deployed-version.txt` as a minimum allowed version based on the last Windows release. That means macOS can publish the same semantic version as Windows, but it cannot publish an older one. For butler, the script now tries the direct post-build download path first using itch's CI-friendly `broth.itch.zone` endpoint and the detected macOS architecture, then falls back to `BUTLER_PATH` only if the download fails. The macOS CI script does not update `FungusToast.Unity/last-deployed-version.txt`; the Windows publish flow remains the only script that records a successful deployment in-repo.

### Why this flow matters

- The macOS app bundle is generated on macOS instead of Windows.
- The packaging step uses `ditto -c -k --sequesterRsrc --keepParent`, which preserves mac app bundle metadata more reliably for distribution.
- The workflow writes a `version.txt` file next to the `.app` bundle before packaging.

### Current artifact output

The workflow writes the raw build output to `Builds/github/macos` inside the Actions workspace, then uploads `Builds/github/fungustoast-macos-<version>.zip` as the downloadable artifact.