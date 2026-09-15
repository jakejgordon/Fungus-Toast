# Mycovariant Authoring Style Guide

This guide defines how to write Mycovariant `Description` and `FlavorText` copy for draft UI clarity. Adaptation descriptions follow the same cadence-first standards with the differences called out in [ADAPTATION_HELPER.md](../ADAPTATION_HELPER.md) ("Adaptation Copy Rules").

Vocabulary rules (verbs, Resistance, adjacency, phases, casing) live in [GAMEPLAY_TERMINOLOGY.md](../GAMEPLAY_TERMINOLOGY.md) and are not repeated here. The shared pre-PR checklist is [CONTENT_COPY_CHECKLIST.md](CONTENT_COPY_CHECKLIST.md).

## Purpose

Mycovariants are drafted abilities that either:
- create a **one-time active effect** (typically resolved during draft), or
- create a **passive effect that can trigger repeatedly for the rest of the game**.

Description copy must let a player understand timing and mechanics without external references.
Each Mycovariant also ships with an icon that diagrams the mechanic (a `Draw*` case in `MycovariantIcons.cs`, see `UI_STYLE_GUIDE.md` section 5.9), so the name, description and picture all point at the same behaviour.

---

## Core Standards

### 1) Description must stand alone
- Do not reference other cards for core rules.
- Avoid phrasing like "same as Tier I", "same targeting rules as X", or "standard targeting rules". If the rule matters, state it.

### 2) Always state cadence and duration
Every Mycovariant description opens with one of these exact phrases:
- `One-time on draft:` for effects resolved during the draft.
- `For the rest of the game, <timing>, ...` for passives, where `<timing>` is one of the phrases in the Time and Phases section of the terminology doc (`before each Growth Phase`, `after each Growth Phase`, `at the end of each Decay Phase`, `whenever ...`).

Bait-family cards (Ascus Bait, Sporophore Decoy, Perispore Crown, Sporal Snare) use the pattern `One-time on draft: if Human, <reward>. If AI, <penalty>.` and never describe AI draft preferences in the description; that is AI-roster behaviour, not a card effect.

### 3) Mechanics before flavor
Description should include, in compact form:
- trigger/timing
- target selection
- action (using the canonical verbs)
- limits/caps/exclusions
- stacking behavior (if relevant)

### 4) Use concrete values
- Use named balance constants in source definitions; never hard-code a number in the string.
- Include counts, percentages, durations, and radius values when available.
- Pluralize any value that could be 1.
- Do not express a chance as a formula ("X% where X = ..."). Write "a N% chance per level of Mycotoxin Tracer".

### 5) Keep it concise
- Prefer one compact sentence.
- Two short clauses are acceptable if needed for accuracy.
- Avoid implementation jargon ("minimum divisor", "rounded up" is fine when it changes the answer) and long lore prose.
- "up to N" already implies "fewer if not enough exist"; do not add the parenthetical.

### 6) Use canonical board-action terms
- For an exact board-state change, use the canonical verb in [GAMEPLAY_TERMINOLOGY.md](../GAMEPLAY_TERMINOLOGY.md): `Colonize`, `Reclaim`, `Infest`, `Overgrow`, `Replace`, `Toxify`, `Poison`, `Kill`, `Clear`, `Move`, or `Expire`.
- Pair a term with a short plain-language outcome on its first meaningful use when needed for comprehension; do not replace it with *invade*, *occupy*, *convert*, or *take over*, and do not re-define it every time.
- Names and flavor text may use evocative synonyms, but Description text must remain mechanically precise.

### 7) Resistance
- If the card grants Resistance and the player might not know what that means, append the canonical sentence: `Resistant cells cannot be killed, infested, or poisoned.` Reuse it verbatim; do not paraphrase.

---

## FlavorText Rules

- One sentence preferred.
- Reinforce fungal tone in the lab-notebook register defined in [UI_STYLE_GUIDE.md](../UI_STYLE_GUIDE.md) section 1 ("Flavor Voice"); do not carry required mechanics.
- Optional if UI context cannot display flavor text.

---

## Recommended Templates

- `Description`: `<Cadence/Timing>: <target> <effect> <limits/exclusions>.`
- `FlavorText`: `<Short fungal thematic line>.`

---

## Good vs Bad Examples

### Good
- `For the rest of the game, before each Growth Phase, colonize, reclaim, infest, or overgrow up to N tiles from your starting spore toward the nearest corner.`
- `One-time on draft: launch toxin spores to toxify up to N empty tiles.`
- `For the rest of the game, whenever one of your living cells dies, reclaim one orthogonally adjacent dead cell with N% chance.`

### Bad
- `Works like Corner Conduit I, but better.`
- `The colony awakens ancient instincts and surges with unstoppable intent...` (vague mechanics)
- `Randomly make a portion of your cells Resistant. The portion is 30% divided by your Mycovariant count, minimum divisor 1.` (no cadence, hard-coded number, implementation-speak)
- `... may drift to a living enemy with X% chance (X = 3 x Mycotoxin Tracer level; standard targeting rules).` (formula in prose, external rule reference, wrong verb)

---

## Authoring Checklist

Run [CONTENT_COPY_CHECKLIST.md](CONTENT_COPY_CHECKLIST.md) in full, plus:
- [ ] Is the opening cadence phrase one of the exact forms above?
- [ ] Does the name/icon pairing reinforce the mechanic clearly in draft UI? (Check the icon on the `tools/icon-preview` sheet at the 40px draft-card size, not just at 128px.)
