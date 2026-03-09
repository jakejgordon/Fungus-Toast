# Adaptation Helper Index

Use this page as the entry point for all Adaptation work.

## What Adaptations Are

Adaptations are campaign rewards that persist across campaign levels for the rest of the current run.

Adaptations commonly do one of two things:
- grant a **start-of-level effect** that is applied each new campaign level, or
- grant a **passive effect** that can trigger repeatedly during every game in the run.

## Primary Docs

- **Naming rules:** [MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md](MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md)
- **Campaign flow and persistence:** [CAMPAIGN_HELPER.md](CAMPAIGN_HELPER.md)
- **Authoring standards for concise mechanics copy:** [MYCOVARIANT_AUTHORING_STYLE.md](MYCOVARIANT_AUTHORING_STYLE.md)
- **Technical reference pattern for passive behavior wiring:** [MYCOVARIANT_TECHNICAL_FLOW.md](MYCOVARIANT_TECHNICAL_FLOW.md)

## Suggested Agent Workflow

1. Read `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md` before naming a new Adaptation.
2. Read `CAMPAIGN_HELPER.md` before changing reward flow, persistence, or campaign state.
3. Read `MYCOVARIANT_AUTHORING_STYLE.md` before writing or revising Adaptation descriptions.
4. Use `MYCOVARIANT_TECHNICAL_FLOW.md` as the reference pattern for passive effect timing, event wiring, and simulation validation until a dedicated Adaptation technical flow doc exists.
5. Implement metadata in the Adaptation catalog and wire gameplay behavior through the appropriate campaign and core runtime hooks.
6. Validate with Core and Simulation builds after behavior changes.

## Common Tasks

### Add a new Adaptation
1. Add or update the entry in `FungusToast.Core/Campaign/AdaptationRepository.cs`.
2. Keep the ID stable and unique.
3. Name it using `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`.
4. Write concise description text using the same cadence-first standards used for Mycovariants.
5. If the Adaptation has gameplay behavior, wire it through the campaign startup seam and any required passive phase/event hooks.

### Add Adaptation UI presence
1. Reuse the existing Unity tooltip system with `ITooltipContentProvider` and `TooltipTrigger`.
2. Reuse a centralized art lookup path for icons rather than binding sprites ad hoc.
3. Keep in-game Adaptation display consistent with the campaign reward draft and sidebar presentation.

### Validate Adaptation behavior
1. Build `FungusToast.Core/FungusToast.Core.csproj`.
2. Build `FungusToast.Simulation/FungusToast.Simulation.csproj`.
3. Verify campaign save/load, reward selection, and any persistent passive effects still behave correctly.

## Notes

- Keep Adaptation logic deterministic and Unity-free in Core.
- Treat campaign state as the ownership source of truth for which Adaptations a run has earned.
- Reuse Mycovariant patterns where helpful, but do not assume Adaptations are drafted or persisted the same way as Mycovariants.