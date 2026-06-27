---
name: create-adaptation
description: Guide for adding or substantially revising a Fungus Toast adaptation. Use when asked to create a new adaptation or change one in a way that affects campaign reward flow, starting-adaptation wiring, persistence, effect processing, icon expectations, or validation.
---

# Create Adaptation

Read these docs first:

1. `FungusToast.Core/docs/ADAPTATION_HELPER.md`
2. `FungusToast.Core/docs/CAMPAIGN_HELPER.md`
3. `FungusToast.Core/docs/second-level/MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`
4. `FungusToast.Core/docs/second-level/MYCOVARIANT_AUTHORING_STYLE.md`
5. `FungusToast.Core/docs/second-level/ADAPTATION_TECHNICAL_FLOW.md`

Read `FungusToast.Core/docs/SAVE_COMPATIBILITY.md` before changing persistence or resume behavior.

## Workflow

1. Confirm whether the adaptation is a normal campaign reward, a starting adaptation, or a revision to an existing one.
2. Confirm whether the effect is start-of-level, passive, or both.
3. Follow the helper's naming workflow exactly before finalizing the chosen name.
4. Propose the validation plan up front: happy path, edge cases, timing checks, interaction coverage, and campaign-specific regressions.
5. Update the catalog seams:
   - `FungusToast.Core/Campaign/AdaptationRepository.cs`
   - `AdaptationIds`, `AdaptationGameBalance`, and related helpers when needed
   - `MoldCatalog` when the change affects starting adaptations
6. Follow the concise copy rules exactly.
7. Ensure the adaptation has a distinct icon keyed from its `IconId`.
8. Wire gameplay behavior through the correct campaign startup seam and passive/runtime hooks in Core.
9. Treat save/resume compatibility as part of the implementation, not an afterthought.

## Validation

1. Build `FungusToast.Core/FungusToast.Core.csproj`.
2. Build `FungusToast.Simulation/FungusToast.Simulation.csproj` when shared gameplay behavior changed.
3. Verify campaign reward selection, save/load, and persistent effects in Unity.
4. Do not rely on simulation alone for campaign flow validation.

## Output

Report:

1. Whether the adaptation is normal or starting-adaptation scoped.
2. The main files changed.
3. The builds and Unity checks that were completed.
4. Any remaining manual campaign verification still required.
