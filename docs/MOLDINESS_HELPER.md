# Moldiness Helper

**Status:** Active.

This document is still an active implementation helper for Fungus-Toast campaign moldiness progression. It is referenced from `.github/copilot-instructions.md`, and it should stay aligned with the live moldiness catalog and campaign reward flow in code.

Use this file when changing:
- moldiness progression thresholds or rewards
- moldiness unlock drafting rules
- adaptation unlock eligibility
- mycovariant unlock eligibility
- permanent campaign moldiness upgrades
- save/resume behavior for pending moldiness rewards
- reward-flow chaining between moldiness rewards, mycovariant drafts, and normal adaptation drafts
- bait mycovariant rules, presentation, or unlocks

`docs/WORKLOG.md` remains the active handoff/task list. This file is the durable helper for how the moldiness system is supposed to work.

## Core Concepts

### Moldiness

Moldiness is the campaign meta-progression currency.

Current implemented assumptions:
- level clears award persistent moldiness
- reward amount scales by campaign depth
- threshold overflow carries over
- a single award can cross multiple thresholds
- threshold crossings enqueue pending moldiness reward triggers before normal adaptation rewards continue

### Moldiness Unlock Level

`MoldinessUnlockLevel` is the progression tier reached by crossing moldiness thresholds.

Important: reaching a moldiness unlock level does **not** automatically inject locked content into normal drafts.

Instead, it expands the pool of rewards eligible for the **moldiness reward draft**.

## Two Separate Reward/Draft Layers

### 1. Moldiness Reward Draft

This happens when a moldiness threshold is crossed.

The moldiness reward draft offers a small set of moldiness rewards. Those rewards can currently:
- permanently unlock a locked adaptation for future campaign adaptation drafts
- permanently unlock a locked mycovariant for future campaign mycovariant drafts
- permanently increase failed-run adaptation carryover capacity
- permanently unlock campaign-layer informational or draft-control upgrades

This is a meta-progression reward draft, not a normal run reward draft.

### 2. Normal Run Drafts

These are the ordinary campaign gameplay drafts, including:
- post-victory adaptation drafts
- campaign mycovariant drafts

Locked content should only appear in these drafts after the player has explicitly unlocked it through moldiness rewards.

## Current Implemented Moldiness Reward Families

The live moldiness catalog is defined in `FungusToast.Unity/Assets/Scripts/Unity/Campaign/MoldinessUnlocks.cs`.

### 1. Adaptation Unlocks

These permanently unlock locked adaptations so they can appear in future campaign adaptation drafts.

Current implemented adaptation unlock rewards:
- `Unlock Spore Salvo` — level 1
- `Unlock Hyphal Bridge` — level 1
- `Unlock Vesicle Burst` — level 1
- `Unlock Hyphal Priming` — level 1
- `Unlock Tropic Lysis` — level 1
- `Unlock Prime Pulse` — level 1
- `Unlock Hyphal Echo` — level 32

Design rule:
- adaptation unlock level controls whether the reward can appear in the moldiness draft
- selecting the reward permanently unlocks that adaptation for future normal adaptation drafts

### 2. Mycovariant Unlocks

These permanently unlock locked mycovariants so they can appear in future campaign mycovariant drafts.

Current implemented mycovariant unlock rewards:
- `Unlock Ascus Bait` — level 1
- `Unlock Septal Seal` — level 1
- `Unlock Sporal Snare` — level 6

Design rule:
- mycovariants still live in the native mycovariant repository
- moldiness progression controls when their unlock rewards can appear
- selecting the reward permanently adds that mycovariant to campaign draft eligibility

### 3. Permanent Campaign Upgrades

These are moldiness rewards that do not unlock a normal card directly, but permanently improve campaign-layer affordances.

Current implemented permanent campaign upgrades:
- `Spores in Reserve I-V`
  - I: level 1
  - II: level 5
  - III: level 8
  - IV: level 10
  - V: level 12
  - each tier permanently increases failed-run adaptation carryover capacity by 1
- `Strain Profiling` — level 3
  - during campaign games, player-summary tooltips reveal campaign AI `FriendlyName` and generated `AIPlayerIntentions`
- `Spore Sifting` — level 5
  - once per campaign level, redraw a **Mycovariant** draft offer before choosing a card
  - this is scoped to Mycovariant drafts only, not normal adaptation drafts or moldiness reward drafts

UI rule:
- if a permanent campaign unlock enables a new action, reveal, or affordance, add lightweight attribution in the relevant UI
- examples already in repo:
  - `Strain Profiling` attribution in campaign AI tooltip content
  - `Spore Sifting` labeling and helper text in the mycovariant draft UI

## Bait Mycovariants

Bait mycovariants are now a first-class documented concept.

### Definition

A bait mycovariant is a player-facing draft pick marked with `IsBait = true`.

Intent:
- it gives the Human player a meaningful upside if drafted
- it is intentionally bad, risky, or trap-like for AI drafters
- some bait mycovariants may also create a direct or indirect advantage for the Human even when an AI takes them

### Current UI Behavior

In the mycovariant draft panel:
- bait mycovariants show a dedicated **`Bait`** badge on the card
- the badge has tooltip copy explaining that bait mycovariants are tuned to favor the Human player or be poor drafts for AI opponents

This presentation lives in `MycovariantCard.cs` and should remain lightweight and obvious.

### Current Implemented Bait Mycovariants

Current bait mycovariants in repo:
- `Ascus Bait`
- `Sporal Snare`

Both are:
- locked behind moldiness unlock rewards
- marked `IsBait = true`
- treated specially in AI draft logic

### Draft Eligibility / AI Handling

Bait mycovariants are not meant to be consumed by early AI drafters before the interesting seat gets a chance to interact with them.

Current pool rule:
- Humans can see eligible bait mycovariants normally
- early AI drafters are prevented from taking `Ascus Bait` and `Sporal Snare`
- the final AI drafter in the current mycovariant draft can be offered them

That rule currently lives in `MycovariantPoolManager.GetEligibleMycovariantsForPlayer`.

### Current Effects

#### Ascus Bait
- if Human drafted: grants bonus mutation points
- if AI drafted: kills a percentage of that AI's non-resistant living cells at random

#### Sporal Snare
- if Human drafted: grants bonus mutation points
- if AI drafted: creates the snare effect described in the mycovariant definition, tuned as a trap-like outcome rather than a straightforward AI-positive pick

Design rule:
- the exact balance numbers can change
- the bait classification should be driven by gameplay intent, player communication, and AI-handling rules together, not by tooltip flavor alone

## Eligibility Model

### Moldiness Reward Draft Eligibility

A moldiness reward is eligible when:
- its minimum unlock level has been reached
- it is not already exhausted, unless repeatable
- any content-specific ownership checks pass
  - locked adaptation already owned -> not eligible
  - locked mycovariant already owned -> not eligible
  - one-time permanent upgrade already owned -> not eligible

### Adaptation Draft Eligibility

An adaptation is eligible for normal campaign adaptation drafts only when:
- it satisfies normal run-level adaptation rules
- if locked, it has already been permanently unlocked through moldiness progression

### Mycovariant Draft Eligibility

A mycovariant is eligible for campaign mycovariant drafts only when:
- it satisfies normal pool/draft rules
- if locked, it has already been permanently unlocked through moldiness progression
- if it is a bait mycovariant, any special AI-seat restrictions still pass

## Persistence Expectations

Campaign save state should preserve at least:
- moldiness progress
- current unlock level
- pending threshold triggers
- pending moldiness reward choices
- unlocked moldiness reward ids
- unlocked adaptation ids
- unlocked mycovariant ids
- failed-run adaptation carryover capacity

In addition, campaign state should preserve any pending campaign-draft state introduced by moldiness upgrades when needed.

Current notable example:
- `Spore Sifting` persists pending mycovariant draft offer state and whether the redraw has already been consumed for the current campaign level

Save/resume should behave correctly if the player closes the game:
- after crossing a moldiness threshold
- while a moldiness reward draft is pending
- after unlocking content but before resolving the normal adaptation draft
- while a `Spore Sifting`-eligible mycovariant draft is pending

## Reward-Flow Chaining

Current preferred order:
1. resolve moldiness threshold awards first
2. if a moldiness reward draft is needed, present and resolve it
3. then continue to the normal post-victory adaptation flow

Important:
- `Spore Sifting` is a mycovariant-draft affordance, not a moldiness-reward-draft affordance
- moldiness UI and mycovariant/adaptation UI should not silently leak features into one another

## Guidance For Future Changes

- Do not conflate moldiness reward eligibility with normal adaptation or mycovariant draft eligibility.
- Reaching a moldiness unlock level broadens the moldiness reward pool. It does not directly inject locked content into normal drafts.
- Prefer explicit permanent unlock state over implicit inference when resuming saves.
- Keep moldiness rewards generic enough to support future content families, but keep real gameplay content in its native repositories.
- If a new permanent campaign upgrade enables a new action, reveal, or rules exception, add lightweight UI attribution where the player encounters it.
- If a new bait mycovariant is added:
  - mark it with `IsBait`
  - decide whether it needs special AI-seat restrictions
  - ensure the draft badge/tooltip remains accurate
  - document its unlock source if it is moldiness-gated
