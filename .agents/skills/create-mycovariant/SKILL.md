---
name: create-mycovariant
description: Guide for adding or substantially revising a Fungus Toast mycovariant. Use when asked to create a new mycovariant or change one in a way that affects naming, copy, icon expectations, effect wiring, draft behavior, processors, or validation.
---

# Create Mycovariant

Read these docs first:

1. `FungusToast.Core/docs/MYCOVARIANT_HELPER.md`
2. `FungusToast.Core/docs/second-level/MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`
3. `FungusToast.Core/docs/second-level/MYCOVARIANT_AUTHORING_STYLE.md`
4. `FungusToast.Core/docs/second-level/MYCOVARIANT_TECHNICAL_FLOW.md`

Read `FungusToast.Core/docs/second-level/MYCOVARIANT_PR_CHECKLIST.md` before final review.

## Workflow

1. Confirm whether the mycovariant is draft-time active, passive, or both.
2. Propose 5 candidate names that satisfy the naming rules, then confirm the chosen name is unique across mutations, mycovariants, and adaptations with a repo search.
3. Propose the validation plan up front: happy path, edge cases, timing checks, interaction coverage, and likely regressions.
4. Update the definition seams:
   - `FungusToast.Core/Mycovariants/MycovariantIds.cs`
   - the appropriate category factory
   - the relevant processors, observers, and Unity draft hooks
5. Follow the style guide for concise player-facing copy.
6. Ensure the mycovariant has a distinct `IconId` and corresponding icon path instead of falling back to generic art.
7. Reuse existing draft, tooltip, and centralized art-lookup patterns before introducing new UI seams.

## Validation

1. Build `FungusToast.Core/FungusToast.Core.csproj`.
2. Build `FungusToast.Simulation/FungusToast.Simulation.csproj` when shared gameplay behavior changed.
3. Run a smoke simulation when gameplay behavior changed.
4. Verify Unity draft behavior when the mycovariant needs interactive input, custom visuals, or new icon wiring.
5. Finish the PR checklist before calling the work complete.

## Output

Report:

1. The chosen concept, name, and main files changed.
2. The tests, builds, and simulations that were run.
3. Any manual Unity follow-up still required.
