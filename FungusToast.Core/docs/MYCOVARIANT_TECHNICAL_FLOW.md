# Mycovariant Technical Implementation Flow

This guide covers technical implementation and integration for Mycovariants in Core, Simulation, and Unity draft flow.

## Quick Workflow

1. Add/confirm ID in `MycovariantIds.cs`.
2. Add/confirm tunables in `MycovariantGameBalance.cs`.
3. Define/update Mycovariant in the correct category factory under `Mycovariants/Factories/`.
4. Implement/update effect logic in core processors/helpers.
5. Wire draft behavior correctly for simulation and Unity paths.
6. Add simulation tracking/reporting if needed.
7. Validate by building Core + Simulation and running smoke checks.

---

## Source of Truth

Mycovariants are authored in category factories:
- `DirectionalMycovariantFactory.cs`
- `EconomyMycovariantFactory.cs`
- `ResistanceMycovariantFactory.cs`
- `GrowthMycovariantFactory.cs`
- `FungicideMycovariantFactory.cs`
- `ReclamationMycovariantFactory.cs`

`MycovariantFactory.GetAll()` aggregates all categories.
`MycovariantRepository` consumes that aggregate list.

Use Id-based lookups from the aggregated set; avoid creating new ad hoc aggregator methods.

---

## Step-by-Step

### 1) IDs and Balance
- Add unique ID in `MycovariantIds.cs`.
- Add tunables in `MycovariantGameBalance.cs`.
- Do not hard-code gameplay numbers in effect code when they should be configurable.

### 2) Factory Definition
- Add private builder method to the proper category factory.
- Add `yield return` in that factory’s `CreateAll()`.
- Set fields coherently (`Type`, `Category`, `IsUniversal`, `AutoMarkTriggered`, `SynergyWith`, `AIScore`).

### 3) Core Effect Logic
- Implement logic in `MycovariantEffectProcessor.cs` and/or helper classes.
- Keep logic deterministic and Unity-free.
- Reuse existing helper patterns when available.

### 4) Draft Integration (Critical)

There are two execution contexts:
- **Silent draft / simulation**: core execution path.
- **Unity draft UI**: resolver/helper-driven path for interactive behavior.

For active Mycovariants that require selection/input:
- Ensure simulation/AI path resolves safely in core.
- Ensure Unity draft human path resolves via UI helper.
- Ensure Unity draft AI path is explicitly handled in Unity context where required.
- Ensure board visuals/animations complete before draft flow continues.

For passive or instant no-input Mycovariants:
- Core definition and effect logic are usually sufficient, but still verify Unity behavior.

### Canonical Implementation Examples

#### A) Active, one-time on draft (requires selection)

Use this pattern when the Mycovariant is an active pick that requires tile/cell selection in Unity drafts.

```csharp
private static Mycovariant ExampleActiveBlast() => new Mycovariant
{
	Id = MycovariantIds.ExampleActiveBlastId,
	Name = "Example Active Blast",
	Description = "One-time on draft: choose one of your toxins and burst in radius R.",
	Type = MycovariantType.Active,
	Category = MycovariantCategory.Fungicide,
	ApplyEffect = (playerMyco, board, rng, observer) =>
	{
		// Silent/simulation path (and core AI-safe execution path)
		var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
		if (player.PlayerType == PlayerTypeEnum.AI)
		{
			int? chosenTile = FindBestTileForAI(player, board, rng);
			if (chosenTile.HasValue)
				MycovariantEffectProcessor.ResolveExampleActiveBlast(playerMyco, board, chosenTile.Value, rng, observer);
		}
	}
};
```

Unity draft resolver/helper must also handle:
- human interactive selection flow,
- Unity AI path when routed through UI draft,
- `RenderBoard(...)` + wait for animations before completing the draft step.

#### B) Passive, recurring rest-of-game effect

Use this pattern when the Mycovariant modifies recurring phase/event behavior.

```csharp
private static Mycovariant ExamplePassiveAura() => new Mycovariant
{
	Id = MycovariantIds.ExamplePassiveAuraId,
	Name = "Example Passive Aura",
	Description = "For the rest of the game, after each growth phase, adjacent living cells gain X% chance to become Resistant.",
	Type = MycovariantType.Passive,
	Category = MycovariantCategory.Resistance,
	AutoMarkTriggered = true,
	// No one-time draft resolution required; behavior is applied by recurring phase/event logic.
};
```

In recurring processors/events:
- check whether the player has the Mycovariant,
- apply chance/effect,
- increment tracked counts if this effect should appear in simulation output.

### 5) Event and Phase Wiring
If timing depends on lifecycle hooks, wire into appropriate systems, such as:
- `GameRulesEventSubscriber.cs`
- `TurnEngine.cs`
- phase-specific processors/runners

### 6) Simulation Tracking and Reporting
If effect output should appear in simulation results:
- Add/extend `MycovariantEffectType` values as needed.
- Record effect counts on `PlayerMycovariant` and/or observer methods.
- Map counts into simulation output builders.
- Extend observer interface/implementation when introducing new tracked events.

### 7) Synergy and AI Scoring
- Add/update synergy groups in `MycovariantSynergyListFactory.cs`.
- Keep AI scoring bounded and explainable.
- Prefer existing scoring patterns unless new behavior demands otherwise.

---

## Common Pitfalls

- Treating Unity draft execution as identical to silent simulation execution.
- Implementing active logic for human draft only and missing Unity AI draft behavior.
- Forgetting simulation reporting hooks for effects intended to be analyzed.
- Defining unclear cadence in copy (`one-time` vs recurring).

---

## Validation Checklist

After Mycovariant changes:
1. `dotnet build FungusToast.Core/FungusToast.Core.csproj`
2. `dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj`
3. Run a smoke simulation and inspect output for expected Mycovariant behavior.
4. If Unity draft flow changed, verify both AI and human draft paths in Unity.
5. Complete `MYCOVARIANT_PR_CHECKLIST.md` before requesting review.
