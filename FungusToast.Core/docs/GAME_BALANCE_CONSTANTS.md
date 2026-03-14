# Game Balance Constants

This document is the canonical reference for **balance levers** in Fungus Toast.

Use it when deciding whether a problem should be solved by:
- mutation tuning,
- mycovariant tuning,
- adaptation tuning,
- AI strategy config,
- or simulation/test-roster changes.

## Balancing Principle

If an AI strategy is overpowered, treat that first as a **signal** that some game option, synergy, timing window, or progression path may be too strong for humans too.

Do **not** treat AI strategy-config changes as the primary balance lever unless the goal is explicitly to:
- improve AI personality,
- improve AI coherence,
- reduce AI self-sabotage,
- or improve test coverage.

For real gameplay balance, prefer tuning game systems first.

## Primary Balance Lever Types

### 1) Mutation cost
Typical location:
- `FungusToast.Core/Config/GameBalance.cs`

Use when:
- a mutation is purchased too early too often,
- a mutation is too efficient for its tier,
- or a path spikes too quickly.

### 2) Effect magnitude
Typical location:
- `FungusToast.Core/Config/GameBalance.cs`
- mutation-specific processors/helpers

Examples:
- kill chance
- growth bonus
- resistance chance
- reclaim chance
- toxin duration
- mutation point gain

Use when:
- the mutation effect is directionally correct,
- but numerically too weak or too strong.

### 3) Max level / scaling curve
Typical location:
- `GameBalance.cs`
- mutation definition/factory metadata

Use when:
- early levels are fine but top-end scaling is runaway,
- or a mutation is too weak because it caps too early.

### 4) Tier placement / prerequisites
Typical location:
- mutation definitions / mutation tree authoring

Use when:
- a mutation arrives too early for its impact,
- a synergy comes online too easily,
- or a path should require more commitment.

### 5) Category interactions / synergy structure
Typical location:
- mutation processors
- event subscribers
- progression rules

Use when:
- the problem is not one mutation in isolation,
- but a repeatable combo, loop, or systemic interaction.

### 6) Economy pacing
Typical location:
- `GameBalance.cs`
- mutation point award logic
- auto-upgrade / surge / round pacing rules

Use when:
- multiple strong strategies are all accelerating too quickly,
- or the metagame is being warped by resource flow rather than one isolated mutation.

### 7) Mycovariant tuning
Typical location:
- `FungusToast.Core/Config/MycovariantGameBalance.cs`
- Mycovariant factories/processors

Use when:
- the imbalance is draft-driven,
- the mutation path is only broken with a specific mycovariant,
- or AI dominance is actually coming from mycovariant preference alignment.

### 8) Adaptation tuning
Typical location:
- `FungusToast.Core/Config/AdaptationGameBalance.cs`
- adaptation processors/repository

Use for campaign balance, not main simulation balance, unless the tested mode includes Adaptations.

## Non-Primary Levers

### AI strategy config
Typical location:
- `FungusToast.Core/AI/AIRoster.cs`

Examples:
- target mutation goals
- economy bias
- mycovariant preferences
- category priority order
- surge priorities

Use when:
- AI is incoherent,
- AI is failing to express an intended theme,
- AI is not exercising important game paths,
- or simulation rosters need better coverage.

Do **not** use this as the default response to gameplay balance problems.

### Simulation roster composition
Typical location:
- `AIRoster.cs`
- simulation commands / experiment setup

Use when:
- test fields are distorted,
- a roster is overrepresenting one family of strategies,
- or a comparison set is poor.

This changes **what is being tested**, not the underlying balance of the game.

## Recommended Balance Workflow

1. Identify an overperforming or underperforming strategy/path.
2. Ask which mutation(s), mycovariant(s), or systemic interactions are driving it.
3. Prefer the smallest gameplay-facing lever that addresses the issue.
4. Change one main lever at a time.
5. Re-run seeded explicit-lineup simulations.
6. Compare against the prior baseline.
7. Only then consider AI config changes if the remaining problem is strategy expression rather than gameplay balance.

## Suggested Heuristic

When a strategy is too strong, ask in this order:
1. Is one mutation under-costed?
2. Is one mutation over-scaled?
3. Is one prerequisite path too easy?
4. Is one synergy loop too efficient?
5. Is resource pacing too generous?
6. Only after that: is the AI merely piloting the path too well relative to the field?

## Related Docs

- `SIMULATION_HELPER.md`
- `AI_STRATEGY_AUTHORING.md`
- `NEW_MUTATION_HELPER.md`
- `MUTATION_MYCOVARIANT_ADAPTATION_NAMING.md`
- `FungusToast.Analytics/README.md`
