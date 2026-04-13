# Adaptation Helper Index

Use this page as the entry point for all Adaptation work.

## What Adaptations Are

Adaptations are campaign rewards that persist across campaign levels for the rest of the current run.

Adaptations commonly do one of two things:
- grant a **start-of-level effect** that is applied each new campaign level, or
- grant a **passive effect** that can trigger repeatedly during every game in the run.

## Primary Docs

- **Naming rules:** [second-level/MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md](second-level/MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md)
- **Campaign flow and persistence:** [CAMPAIGN_HELPER.md](CAMPAIGN_HELPER.md)
- **Authoring standards for concise mechanics copy:** [second-level/MYCOVARIANT_AUTHORING_STYLE.md](second-level/MYCOVARIANT_AUTHORING_STYLE.md)
- **Technical implementation flow:** [second-level/ADAPTATION_TECHNICAL_FLOW.md](second-level/ADAPTATION_TECHNICAL_FLOW.md)

## Suggested Agent Workflow

1. Read `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md` before naming a new Adaptation.
2. Read `CAMPAIGN_HELPER.md` before changing reward flow, persistence, or campaign state.
3. Read `MYCOVARIANT_AUTHORING_STYLE.md` before writing or revising Adaptation descriptions.
4. Read `ADAPTATION_TECHNICAL_FLOW.md` before wiring gameplay behavior.
5. Generate a unique icon for the Adaptation so the campaign draft, tooltips, and profile UI do not fall back to generic art. The first pass can be provisional and replaced later, but every new Adaptation should ship with distinct iconography.
6. Implement metadata in the Adaptation catalog and wire gameplay behavior through the appropriate campaign and core runtime hooks.
7. Proactively list the proposed test cases for the new Adaptation, including happy path behavior, edge cases, timing/cadence checks, interaction coverage, and campaign-specific validation points.
8. Validate with Core and Simulation builds for core behavior changes; validate campaign flow in Unity because campaign simulation is not supported yet.

## Common Tasks

### Add a new Adaptation
1. Add or update the entry in `FungusToast.Core/Campaign/AdaptationRepository.cs`.
2. Keep the ID stable and unique.
3. Name it using `second-level/MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`.
4. Write concise description text using the same cadence-first standards used for Mycovariants.
5. Generate a unique icon keyed off the Adaptation's `IconId`. It can be temporary and replaced later, but it should be distinct from every other Adaptation.
6. If the Adaptation has gameplay behavior, wire it through the campaign startup seam and any required passive phase/event hooks.

### Add Adaptation UI presence
1. Reuse the existing Unity tooltip system with `ITooltipContentProvider` and `TooltipTrigger`.
2. Reuse a centralized art lookup path for icons rather than binding sprites ad hoc.
3. Ensure the centralized art repository has a unique generated icon for the Adaptation even if it is only a first-pass placeholder.
4. Keep in-game Adaptation display consistent with the campaign reward draft and sidebar presentation.

### Validate Adaptation behavior
1. Build `FungusToast.Core/FungusToast.Core.csproj`.
2. Build `FungusToast.Simulation/FungusToast.Simulation.csproj` when the change affects shared core behavior.
3. Verify campaign save/load, reward selection, and any persistent passive effects in Unity.
4. Do not rely on simulation for campaign flow validation yet.

## Notes

- Keep Adaptation logic deterministic and Unity-free in Core.
- Treat campaign state as the ownership source of truth for which Adaptations a run has earned.
- Reuse Mycovariant patterns where helpful, but do not assume Adaptations are drafted or persisted the same way as Mycovariants.

## Starting Adaptations

Starting Adaptations are a special subset of Adaptations that are granted automatically when a player starts a new campaign run based on the mold they choose. They are **not** offered in the campaign reward draft.

### Key facts

- `AdaptationDefinition.IsStartingAdaptation == true` marks these adaptations. They are excluded from `GetAdaptationDraftChoices` via `.Where(x => !x.IsStartingAdaptation ...)`.
- **Only the human player** receives a starting adaptation. AI players do not get mold-specific starting bonuses.
- IDs `adaptation_20` through `adaptation_27` are reserved for the eight starting adaptations (one per mold, index 0–7).

### Single source of truth: `MoldCatalog`

`FungusToast.Core.Campaign.MoldCatalog` is the authoritative mapping from mold index to display name and starting adaptation ID:

```csharp
MoldCatalog.GetDisplayName(moldIndex)         // e.g. "Mycelavis"
MoldCatalog.GetStartingAdaptationId(moldIndex) // e.g. "adaptation_20"
```

Use `MoldCatalog` wherever you need mold names or starting adaptation lookups — including Unity UI. Do **not** maintain a parallel `string[]` array of mold names in non-Core code.

### Mold ↔ Adaptation roster

| Index | Mold name     | Starting Adaptation         |
|-------|---------------|-----------------------------|
| 0     | Mycelavis     | Oblique Filament            |
| 1     | Sporalunea    | Thanatrophic Rebound        |
| 2     | Cineramyxa    | Toxin Primacy               |
| 3     | Velutora      | Centripetal Germination     |
| 4     | Glaucoryza    | Signal Economy              |
| 5     | Viridomyxa    | Liminal Sporemeal           |
| 6     | Noctephyra    | Putrefactive Resilience     |
| 7     | Aureomycella  | Compound Reserve            |

### Wiring checklist when adding a new starting adaptation

1. Add the `AdaptationDefinition` to `AdaptationRepository` with `isStartingAdaptation: true`.
2. Register it in `MoldCatalog` at the appropriate mold index.
3. Add a new constant in `AdaptationIds`.
4. Add any tunable balance constants in `AdaptationGameBalance`.
5. Implement gameplay effects through `AdaptationEffectProcessor` (or the appropriate passive site in `Player.cs` / `FungicideMutationProcessor.cs`).
6. The `CampaignController.StartNew` call automatically assigns the starting adaptation via `MoldCatalog`; no additional wiring is needed there.