# Contributing to Fungus Toast

Practical setup and workflow notes for anyone — human or AI-assisted — working in this repo. For what to read and in what order once you're set up, start at [.github/copilot-instructions.md](.github/copilot-instructions.md); it is the router for both AI agents and the project's documentation hierarchy.

## Prerequisites

- .NET SDK 8.0+ (`FungusToast.Simulation` targets `net8.0`; `FungusToast.Core` targets `netstandard2.1`)
- Unity `6000.3.10f1` (see `FungusToast.Unity/ProjectSettings/ProjectVersion.txt` for the exact version this checkout expects) — only needed for `FungusToast.Unity`-facing work
- Python 3 — used by a few repo scripts (`scripts/run_campaign_balance.py`, `scripts/check_markdown_links.py`, and others)

## Quick start

```bash
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
```

That's enough to work on gameplay rules, AI, or simulation without opening Unity. For the full build matrix (Unity compile validation, itch.io release flow, macOS CI build), see [BUILD_INSTRUCTIONS.md](FungusToast.Core/docs/BUILD_INSTRUCTIONS.md).

## Testing

See [TESTING_HELPER.md](FungusToast.Core/docs/TESTING_HELPER.md) for the unit test stack and canonical test commands, and [SIMULATION_HELPER.md](FungusToast.Core/docs/SIMULATION_HELPER.md) for simulation/balance validation workflows. Final balance or campaign calls must be backed by exported simulation artifacts, not just console output.

## Git workflow

For isolated feature/bugfix checkouts using Git worktrees, see [GIT_WORKTREE_WORKFLOW.md](docs/GIT_WORKTREE_WORKFLOW.md).

## AI-assisted development

- [.github/copilot-instructions.md](.github/copilot-instructions.md) is the root router and holds the repo's hard rules (deterministic Core, no magic constants, doc-hierarchy discoverability, and more).
- Canonical agent skills live under `.agents/skills/`. If you're using Claude Code, which only discovers skills under `.claude/skills/`, run the platform script once after cloning so it can see them:

  ```bash
  scripts/link-claude-skills.ps1   # Windows
  scripts/link-claude-skills.sh    # macOS/Linux
  ```

  See [FungusToast.Core/docs/README.md](FungusToast.Core/docs/README.md#5-docs-vs-instructions-vs-skills) for details.

## Adding documentation

New `.md` files must be linked into the documentation hierarchy — see [Documentation Hierarchy Rules](FungusToast.Core/docs/README.md#6-documentation-hierarchy-rules). A CI check ([.github/workflows/docs-check.yml](.github/workflows/docs-check.yml)) verifies relative markdown links resolve; run it locally with:

```bash
python scripts/check_markdown_links.py
```

## Scratch and debug output

Don't commit throwaway files (bakeoff results, diagnostic dumps, one-off patches) anywhere in the tree. Put them in the repo-root `TEMP/` folder, which is gitignored.
