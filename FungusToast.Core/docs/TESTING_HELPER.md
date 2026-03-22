# Testing Helper

See also: [README.md](README.md) for the full documentation hierarchy.

This document covers the unit-testing stack for `FungusToast.Core` and the canonical commands for running tests from the command line.

## Current Test Stack

The current `FungusToast.Core` test project is:

- **Project:** `FungusToast.Core.Tests`
- **Framework:** xUnit
- **Assertions:** xUnit built-in `Assert.*`
- **Coverage collector:** `coverlet.collector`

Why this stack:

- keeps the setup simple and dependency-light
- works cleanly with `dotnet test`
- avoids assertion-library licensing or style debates
- is a good fit for deterministic game-rule tests in `FungusToast.Core`

## Project Layout

- `FungusToast.Core/` — production deterministic game logic
- `FungusToast.Core.Tests/` — unit tests for `FungusToast.Core`

Prefer organizing tests by feature/domain as the suite grows, for example:

- `Board/`
- `Growth/`
- `Mutations/`
- `AI/`
- `Regression/`

## Canonical Commands

Run the core test project:

```sh
dotnet test FungusToast.Core.Tests/FungusToast.Core.Tests.csproj
```

Run tests with coverage collection:

```sh
dotnet test FungusToast.Core.Tests/FungusToast.Core.Tests.csproj --collect:"XPlat Code Coverage"
```

Build the production projects used most often during CLI validation:

```sh
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
```

## Coverage Notes

`coverlet.collector` integrates through `dotnet test --collect:"XPlat Code Coverage"`.

Typical output lands under the test project's `TestResults/` directory. Keep coverage as a supporting signal, not the goal by itself; high-value deterministic tests matter more than chasing a percentage.

## What To Test First

Strong first targets in `FungusToast.Core` are:

1. **Starting spore placement**
   - deterministic
   - high gameplay impact
   - recently tuned and documented
   - has a compact public API in `StartingSporeUtility`

2. **Pure utility/rule calculations**
   - cheap to test
   - fast feedback
   - low fixture overhead

3. **Bug-fix regressions**
   - when a gameplay or simulation bug is fixed, add a targeted regression test nearby

## Recommended First Test Slice

Start with `StartingSporeUtility` and cover:

- `GetStartingPositions(...)` returns the expected count for each player count
- `GetStartingPositions(...)` returns the center tile for 1 player
- precomputed layouts for 2-8 players scale into valid in-bounds coordinates
- override positions are clamped into board bounds
- duplicate override positions are resolved into unique positions
- `GetStartingPositionAnalysis(...)` returns one entry per slot with stable slot ordering

These tests are fast, deterministic, and exercise important startup behavior without needing large game-state fixtures.
