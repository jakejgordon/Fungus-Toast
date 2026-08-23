# Fungus Toast

A 2D competitive board-control game: each player grows a mold colony, racing to claim the largest share of a slice of toast through mutations, adaptations, and mycovariants. Fungus Toast is released on itch.io for Windows and macOS.

## Project structure

The codebase is split into four projects:

- **FungusToast.Core** — deterministic game rules, mutations, AI, and simulation-facing logic (Unity-free)
- **FungusToast.Simulation** — headless console runner for many-game simulations and balance validation
- **FungusToast.Unity** — the Unity front end: presentation, UI, and interaction flow
- **FungusToast.Analytics** — offline analysis tooling for simulation exports

See [ARCHITECTURE_OVERVIEW.md](FungusToast.Core/docs/ARCHITECTURE_OVERVIEW.md) for how these layers relate and who owns what.

## Getting started

Building, testing, git workflow, and dev-environment setup are covered in [CONTRIBUTORS.md](CONTRIBUTORS.md).

## Documentation

This repo maintains a deliberate documentation hierarchy for both human and AI-assisted contributors:

- [.github/copilot-instructions.md](.github/copilot-instructions.md) — top-level router and repo-wide hard rules
- [FungusToast.Core/docs/README.md](FungusToast.Core/docs/README.md) — full documentation index

Start at whichever one matches what you're trying to do; both cross-link into the task-specific helper docs.

## License

[MIT](LICENSE) — Copyright (c) 2025 Jake Gordon.
