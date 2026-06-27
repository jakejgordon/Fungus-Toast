---
name: create-mutation
description: Guide for adding or substantially revising a Fungus Toast mutation. Use when asked to create a new mutation or make a mutation-level content change that affects copy, prerequisites, processing logic, simulation tracking, tree layout, or validation.
---

# Create Mutation

Read these docs first:

1. `FungusToast.Core/docs/NEW_MUTATION_HELPER.md`
2. `FungusToast.Core/docs/second-level/MUTATION_PREREQUISITE_GUIDELINES.md`
3. `FungusToast.Core/docs/GAMEPLAY_TERMINOLOGY.md`

Read `FungusToast.Core/docs/second-level/SIMULATION_TRACKING_IMPLEMENTATION.md` before adding new analytics seams.

## Workflow

1. Confirm the mutation's intent, category, tier, trigger timing, scaling, and expected player-facing summary.
2. Propose the validation plan up front: happy path, edge cases, important interactions, and likely regressions.
3. Update the mutation definition seams:
   - `FungusToast.Core/Mutations/MutationIds.cs`
   - `FungusToast.Core/Mutations/MutationTypeEnum.cs` when a new type is actually needed
   - `FungusToast.Core/Config/GameBalance.cs`
   - the appropriate factory under `FungusToast.Core/Mutations/Factories/`
4. Follow the helper's naming workflow and description rules exactly. Keep tooltip copy readable first, implementation-accurate second.
5. Wire gameplay behavior through the correct processor and coordinator path in `FungusToast.Core`.
6. Add simulation-tracking hooks when the mutation creates meaningful analytics-visible behavior.
7. Update Unity mutation-tree placement in `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/UI_MutationLayoutProvider.cs` when the new mutation needs a node.
8. Reuse existing patterns before inventing new mutation-specific seams.

## Validation

1. Build `FungusToast.Core/FungusToast.Core.csproj`.
2. Build `FungusToast.Simulation/FungusToast.Simulation.csproj` when shared gameplay behavior changed.
3. Run targeted tests when the affected area already has them.
4. Run a smoke simulation when gameplay behavior changed.
5. If Unity-facing UI or tree layout changed, call out the required Unity verification explicitly.

## Output

Report:

1. The mutation intent and the main files changed.
2. The tests and builds that were run.
3. Any manual Unity follow-up still required.
