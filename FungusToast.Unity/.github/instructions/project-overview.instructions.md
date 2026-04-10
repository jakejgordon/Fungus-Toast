# Project Overview

This project contains .cs script files for the Unity front-end of the Fungus Toast game.

## Folder Structure

```
FungusToast.Unity/
├── FungusToast.Unity.csproj        # Unity-generated project file (DO NOT EDIT)
├── Assembly-CSharp.csproj           # Unity-generated assembly (DO NOT EDIT)
├── .vsconfig                        # Visual Studio workload configuration
├── .editorconfig                    # Code formatting rules
├── Assets/                          # Unity assets and scripts
│   ├── Plugins/                     # Contains the checked-in FungusToast.Core.dll used by Unity and cloud builds
│   ├── Scripts/Unity/               # Unity-specific C# scripts
│   ├── Scenes/                      # Unity scene files
│   └── ...                          # Other Unity assets
├── Packages/                        # Unity package manager files
├── ProjectSettings/                 # Unity project configuration
└── Library/                         # Unity build cache (ignored)
```

## Libraries and Frameworks

**FungusToast.Unity Dependencies:**
- Unity 6000.3.10f1
- References FungusToast.Core.dll from Assets/Plugins
- TextMeshPro (Unity package)
- Unity UI system

## Core DLL Note

- `Assets/Plugins/FungusToast.Core.dll` is intentionally checked into git for Unity Cloud Build and similar Unity-only CI flows that do not run `dotnet build` before opening the project.
- Local Windows workflows may still refresh that DLL through the Core post-build copy step, but the committed binary remains the cloud build baseline.
- If `FungusToast.Core` source changes, rebuild Core and update the checked-in plugin DLL so Unity and CI are using the same gameplay assembly.

## Coding Standards

- Follow clean code standards and avoid files with > 300 lines of code