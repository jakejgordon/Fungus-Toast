# Build Instructions

To build the solution, always build the projects in this order:

1. FungusToast.Core
2. FungusToast.Simulation
3. FungusToast.Unity

Use these commands:

```sh
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
dotnet build "Fungus Toast/FungusToast.Unity.csproj"
```

This order ensures all dependencies are built correctly and avoids circular dependency issues. 