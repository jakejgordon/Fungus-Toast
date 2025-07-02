# Build Instructions

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
- If you encounter Unity script errors, copy and paste them into Cursor for troubleshooting.

This order ensures all dependencies are built correctly and avoids circular dependency issues. 