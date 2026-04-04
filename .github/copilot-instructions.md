# Fungus Toast - GitHub Copilot Instructions

## Start Here

This file is the top-level router for AI-assisted development in this repository.

### First-hop task entry docs

- **Build + validation basics:** `FungusToast.Core/docs/BUILD_INSTRUCTIONS.md`
- **Local Windows itch.io release workflow:** `FungusToast.Core/docs/BUILD_INSTRUCTIONS.md`
- **Unit test stack and canonical test commands:** `FungusToast.Core/docs/TESTING_HELPER.md`
- **Simulation workflows, reproducibility, fairness testing, and balance runs:** `FungusToast.Core/docs/SIMULATION_HELPER.md`
- **AI strategy authoring and roster metadata:** `FungusToast.Core/docs/AI_STRATEGY_AUTHORING.md`
- **Mutation authoring:** `FungusToast.Core/docs/NEW_MUTATION_HELPER.md`
- **Mycovariant authoring:** `FungusToast.Core/docs/MYCOVARIANT_HELPER.md`
- **Adaptation authoring:** `FungusToast.Core/docs/ADAPTATION_HELPER.md`
- **Campaign systems and progression context:** `FungusToast.Core/docs/CAMPAIGN_HELPER.md`
- **Campaign modernization / recent curated roster tuning results:** `docs/campaign-modernization-2026-03-27.md`
- **Dominance diagnosis / controlled balance investigation:** `FungusToast.Core/docs/DOMINANCE_DIAGNOSIS_WORKFLOW.md`
- **Gameplay balance levers and canonical constants:** `FungusToast.Core/docs/GAME_BALANCE_CONSTANTS.md`
- **Sound design, storage, and trigger guidance:** `FungusToast.Core/docs/SOUNDS.md`
- **Unity UI architecture and service patterns:** `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`
- **Unity UI style rules:** `FungusToast.Core/docs/UI_STYLE_GUIDE.md`
- **Technical architecture context:** `FungusToast.Core/docs/ARCHITECTURE_OVERVIEW.md`
- **Canonical gameplay terminology and state verbs:** `FungusToast.Core/docs/GAMEPLAY_TERMINOLOGY.md`
- **Analytics workflow for simulation exports:** `FungusToast.Analytics/README.md`
- **Unity-front-end-only overview:** `FungusToast.Unity/.github/instructions/project-overview.instructions.md`
- **Complete documentation map / secondary references:** `FungusToast.Core/docs/README.md`

If a task is unclear, route through the most specific document above before making changes.

## Repository Overview

Fungus Toast is a 2D Unity game where each player represents a mold colony trying to take the largest share of the toast.

Main projects:
- **FungusToast.Core**: deterministic game rules, mutations, AI, simulation-facing logic
- **FungusToast.Simulation**: console runner for many-game simulations and balance validation
- **FungusToast.Unity**: Unity front end, presentation, UI, and interaction flow
- **FungusToast.Analytics**: offline analysis tooling for simulation exports

## Hard Rules

- Keep gameplay logic deterministic and Unity-free inside `FungusToast.Core`.
- Do not edit Unity-generated project files such as:
  - `Assembly-CSharp.csproj`
  - `Assembly-CSharp-Editor.csproj`
  - `FungusToast.Unity.csproj`
- No magic constants for tunable gameplay/UI values. Use the appropriate constants file.
- Prefer minimal, scoped changes over opportunistic refactors.
- When touching Unity UI, follow the established patterns in `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`.
- When adding new docs, link them into the documentation hierarchy so they are discoverable.

## Build and Validation Expectations

### Canonical CLI builds

Build these with `dotnet build`:

```bash
dotnet build FungusToast.Core/FungusToast.Core.csproj
dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj
```

See `FungusToast.Core/docs/BUILD_INSTRUCTIONS.md` for the authoritative build commands and platform notes.

### Unity compile validation

For Unity-facing changes, also validate Unity compile health in the Unity environment. Unity-generated project files may exist for editor/tooling support, but the Unity Editor remains the authoritative build/compile surface for Unity-side correctness.

## Quick Validation Checklist

Choose the smallest checklist that matches the change:

### Core-only gameplay change
1. Build Core
2. Build Simulation
3. Run an appropriate smoke simulation if gameplay behavior changed

### Simulation tooling / CLI change
1. Build Core
2. Build Simulation
3. Run a smoke simulation
4. Confirm expected output/help behavior

### Unity-facing change
1. Build Core
2. Build Simulation if shared behavior changed
3. Validate Unity compile health in the Unity environment
4. Verify the affected UI/flow in Unity

## AI Productivity Rules

- Prefer `FungusToast.Core` for rules, data models, and deterministic mechanics.
- Only use `FungusToast.Unity` for view/controllers, interaction flow, and presentation concerns.
- Prefer the existing architecture patterns documented in `ARCHITECTURE_OVERVIEW.md` and `UI_ARCHITECTURE_HELPER.md` rather than inventing new ones.
- Reuse the documentation hierarchy instead of inventing fresh patterns when a helper already exists.

## Documentation Ownership

Use this precedence order when multiple docs seem relevant:

1. **This file** = top-level router + hard repo rules
2. **Specific helper/workflow docs** = authoritative task instructions
3. **`FungusToast.Core/docs/README.md`** = complete documentation map / secondary references
4. **Tracking docs** such as `docs/WORKLOG.md` = current handoff and active thread, not canonical design truth

## Search Guidance

- Simulation/balance tasks → `SIMULATION_HELPER.md`
- Mutation work → `NEW_MUTATION_HELPER.md`
- Mycovariant work → `MYCOVARIANT_HELPER.md`
- Adaptation/campaign work → `ADAPTATION_HELPER.md` and `CAMPAIGN_HELPER.md`
- AI strategy work → `AI_STRATEGY_AUTHORING.md`
- UI/service/tooltip/pooling work → `UI_ARCHITECTURE_HELPER.md` and `UI_STYLE_GUIDE.md`
- Sound planning / audio trigger work → `SOUNDS.md`, `UI_ARCHITECTURE_HELPER.md`, and `ARCHITECTURE_OVERVIEW.md`
- Deep architecture questions → `ARCHITECTURE_OVERVIEW.md`
- Canonical gameplay terms and state-transition verbs → `GAMEPLAY_TERMINOLOGY.md`
- Export analysis → `FungusToast.Analytics/README.md`

## New Documentation Rule

Whenever generating a new `.md` file:
- first decide whether an existing doc should be updated instead
- if a new doc is warranted, add it to the documentation hierarchy
- make sure it is referenced either from this root file or from another indexed doc such as `FungusToast.Core/docs/README.md`
