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
│   ├── Plugins/                     # Contains FungusToast.Core.dll (auto-copied)
│   ├── Scripts/Unity/               # Unity-specific C# scripts
│   ├── Scenes/                      # Unity scene files
│   └── ...                          # Other Unity assets
├── Packages/                        # Unity package manager files
├── ProjectSettings/                 # Unity project configuration
└── Library/                         # Unity build cache (ignored)
```

## Libraries and Frameworks

**FungusToast.Unity Dependencies:**
- Unity 6000.0.45f1
- References FungusToast.Core.dll from Assets/Plugins
- TextMeshPro (Unity package)
- Unity UI system

## Coding Standards

- Follow clean code standards and avoid files with > 300 lines of code