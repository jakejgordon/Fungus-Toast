# Moldiness Helper

This document is the working design reference for Fungus-Toast campaign moldiness progression.

Use this file when changing:
- moldiness progression thresholds or rewards
- moldiness unlock drafting rules
- locked adaptation availability
- repeatable or universal moldiness rewards
- future moldiness unlocks for mycovariants, mutations, or other content
- save/resume behavior for pending moldiness rewards
- campaign reward-flow chaining between moldiness and normal adaptation drafts

`docs/WORKLOG.md` remains the active handoff/task list. This file is the design and implementation helper for the moldiness system itself.

## Core Concepts

### Moldiness

Moldiness is the campaign meta-progression currency.

Current prototype assumptions already recorded in code/worklog:
- level clears award persistent moldiness
- reward amount scales by campaign depth
- threshold overflow carries over
- a single award can cross multiple thresholds

### Moldiness Unlock Level

`MoldinessUnlockLevel` is the progression tier reached by crossing moldiness thresholds.

Important: reaching a moldiness unlock level does **not** automatically make locked adaptations appear in normal adaptation drafts.

Instead, reaching a new moldiness unlock level expands the pool of rewards that can appear in the **moldiness draft**.

### Two Separate Draft Systems

#### 1. Moldiness Draft

This happens when a moldiness threshold is crossed.

The moldiness draft offers a small set of moldiness rewards. These rewards can:
- permanently unlock a locked adaptation for future normal adaptation drafts
- grant or increase a repeatable meta capability
- later unlock mycovariants, mutations, or other content types

This is a meta-progression reward draft, not a normal run reward draft.

#### 2. Adaptation Draft

This is the existing post-victory campaign adaptation reward draft.

It only offers adaptations that are currently eligible for the run.

A locked adaptation should only appear here after the player has explicitly unlocked it through a moldiness draft reward.

## Recommended System Shape

## Layer 1: Moldiness Reward Catalog

Maintain a master list of moldiness reward definitions.

This catalog should support:
- reward id
- display name
- description
- reward type
- eligibility rules
- minimum `MoldinessUnlockLevel`
- repeatable vs one-time rewards
- universal rewards that remain eligible across multiple tiers
- payload data needed to apply the reward

Example reward types:
- unlock a locked adaptation
- add +1 failed-run adaptation carryover capacity
- unlock a mycovariant in the future
- unlock a mutation-related reward in the future

This catalog is the source for what can show up in moldiness drafts.

## Layer 2: Native Game Content Definitions

Actual game content should still live in its native repositories.

For adaptations, `AdaptationDefinition` should carry native locked-content metadata such as:
- `IsLocked`
- `RequiredMoldinessUnlockLevel`

These fields describe the adaptation itself.

However, locked adaptations should still require an explicit moldiness-draft unlock reward before they become eligible for normal adaptation drafts.

So:
- unlock level controls whether a locked adaptation reward can appear in the moldiness draft
- selecting that reward permanently unlocks the adaptation for future normal adaptation drafts

## First Reward Types To Support

### 1. Failed-Run Carryover Reward

This is a five-step permanent moldiness reward line.

Behavior:
- `Spores in Reserve I` has no moldiness-level constraint beyond becoming eligible at level 1
- `Spores in Reserve II` unlocks at Moldiness Level 5
- `Spores in Reserve III` unlocks at Moldiness Level 8
- `Spores in Reserve IV` unlocks at Moldiness Level 10
- `Spores in Reserve V` unlocks at Moldiness Level 12
- each tier is a separate one-time moldiness reward
- each tier drafted increases permanent failed-run adaptation carryover capacity by 1
- on failed campaigns, the player may retain up to that many adaptations into the next run

Suggested state field:
- `failedRunAdaptationCarryoverCount`

### 2. Locked Level-1 Adaptations

Create at least three new adaptations with:
- `IsLocked = true`
- `RequiredMoldinessUnlockLevel = 1`

These should **not** appear automatically in normal adaptation drafts when level 1 is reached.

Instead:
- reaching moldiness unlock level 1 makes their unlock rewards eligible in the moldiness draft
- drafting one of those rewards permanently unlocks that adaptation
- after that, it becomes eligible for future normal adaptation drafts

## Eligibility Model

### Moldiness Draft Eligibility

A moldiness reward is eligible when:
- its minimum moldiness unlock level has been reached
- it is not already exhausted, unless repeatable
- any additional reward-specific rules pass

### Adaptation Draft Eligibility

An adaptation is eligible for normal adaptation drafts only when:
- it is not a starting-only adaptation
- it has not already been selected in the current run
- if it is not locked, normal rules allow it
- if it is locked, the corresponding moldiness reward has already been drafted/applied

## Persistence Expectations

Campaign save state should support at least:
- moldiness progress
- current `MoldinessUnlockLevel`
- pending threshold triggers
- pending moldiness draft choices
- permanently unlocked moldiness reward ids
- permanently unlocked adaptation ids or equivalent content unlock state
- `failedRunAdaptationCarryoverCount`

Save/resume should behave correctly if the player closes the game:
- after crossing a moldiness threshold
- while a moldiness reward draft is pending
- after unlocking content but before resolving the normal adaptation draft

## Reward-Flow Chaining

Current preferred reward order:
1. resolve moldiness threshold awards first
2. if a moldiness draft is needed, present and resolve it
3. then continue to the normal post-victory adaptation draft

This keeps meta-progression legible and avoids normal run rewards obscuring the threshold event.

## Current Open Questions

- How many choices should each moldiness draft present by default?
- Should some moldiness rewards apply immediately to the current run, future runs only, or both?
- How should failed-run adaptation carryover be presented and selected at run failure time?
- Should locked adaptations be linked directly to unlock reward ids, or resolved through a looser content-id mapping?
- What other permanent reward lines should sit alongside the capped Spores in Reserve tiers so the pool stays attractive without crowding out everything else?

## Recommended Near-Term Implementation Order

1. Finalize the hybrid data model:
   - moldiness reward catalog
   - locked adaptation metadata
   - permanent unlock state
   - repeatable universal reward state
2. Add `failedRunAdaptationCarryoverCount` support in progression state
3. Create at least three new level-1 locked adaptations
4. Change moldiness reward definitions so adaptation unlock rewards target those real locked adaptations
5. Remove the temporary auto-apply moldiness shortcut and replace it with a first-class pending moldiness draft state
6. Build the UI flow for moldiness reward choice
7. Verify save/resume and chained reward flow

## Implementation Notes For AI/Human Developers

- Do not conflate moldiness draft eligibility with normal adaptation draft eligibility.
- Reaching a moldiness unlock level broadens the moldiness reward pool. It does not directly inject locked adaptations into the normal adaptation draft.
- Prefer explicit permanent unlock state over implicit inference when resuming saves.
- Keep the moldiness reward system generic enough to support future mycovariant or mutation unlocks, but do not force all gameplay content into the same abstraction layer.
