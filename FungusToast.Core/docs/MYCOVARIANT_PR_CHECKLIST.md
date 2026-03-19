# Mycovariant PR Checklist

Use this checklist before opening or approving a PR that changes Mycovariants.

## Scope and Definitions

- [ ] IDs are unique and added/updated in `MycovariantIds.cs`.
- [ ] Balance constants are defined in `MycovariantGameBalance.cs` (no hidden magic numbers).
- [ ] Mycovariant is defined in the correct category factory and returned by `CreateAll()`.
- [ ] Name follows `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`, and 5 candidate names were considered before finalizing.
- [ ] `IconId` is set and mapped to a unique icon rather than generic fallback art.

## Description and Flavor Quality

- [ ] Description stands alone (no “same as X” references).
- [ ] Cadence/duration is explicit (`one-time`, `each phase`, `rest of game`, etc.).
- [ ] Description includes critical mechanics (target, action, caps/exclusions, stacking where relevant).
- [ ] FlavorText is optional/thematic and does not carry required mechanics.

## Technical Integration

- [ ] Core effect logic is implemented/updated in the correct processor/helper.
- [ ] Icon/art lookup wiring is correct for draft cards, tooltips, and any persistent Mycovariant UI.
- [ ] Event/phase wiring is correct for intended timing.
- [ ] Active draft effects handle both simulation path and Unity draft path.
- [ ] Unity draft flow waits for visual completion before continuing.

## Simulation Tracking (if applicable)

- [ ] Effect types/counters are added or updated when analytics are expected.
- [ ] Observer/reporting mappings are updated so results appear in simulation output.

## Synergy and AI

- [ ] Synergy lists are updated where relevant.
- [ ] AI scoring is bounded and consistent with existing patterns.

## Validation

- [ ] `dotnet build FungusToast.Core/FungusToast.Core.csproj`
- [ ] `dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj`
- [ ] Smoke simulation run completed when behavior changed.
- [ ] Unity draft behavior spot-checked (human + AI paths) when relevant.

## Documentation

- [ ] Updated docs if behavior, cadence, or implementation flow changed:
  - `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`
  - `MYCOVARIANT_AUTHORING_STYLE.md`
  - `MYCOVARIANT_TECHNICAL_FLOW.md`
  - `MYCOVARIANT_HELPER.md` (index links)
