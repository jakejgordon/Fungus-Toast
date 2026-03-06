# Mycovariant Authoring Style Guide

This guide defines how to write Mycovariant `Description` and `FlavorText` copy for draft UI clarity.

## Purpose

Mycovariants are drafted abilities that either:
- create a **one-time active effect** (typically resolved during draft), or
- create a **passive effect that can trigger repeatedly for the rest of the game**.

Description copy must let a player understand timing and mechanics without external references.

---

## Core Standards

### 1) Description must stand alone
- Do not reference other cards for core rules.
- Avoid phrasing like “same as Tier I” or “same targeting rules as X”.

### 2) Always state cadence and duration
When not inherently obvious, explicitly include:
- `One-time on draft`
- `Before each growth phase`
- `After each growth phase`
- `At end of each decay phase`
- `Whenever ...`
- `For the rest of the game`

### 3) Mechanics before flavor
Description should include, in compact form:
- trigger/timing
- target selection
- action
- limits/caps/exclusions
- stacking behavior (if relevant)

### 4) Use concrete values
- Use named balance constants in source definitions.
- Include counts, percentages, durations, and radius values when available.

### 5) Keep it concise
- Prefer one compact sentence.
- Two short clauses are acceptable if needed for accuracy.
- Avoid implementation jargon and long lore prose.

---

## FlavorText Rules

- One sentence preferred.
- Reinforce fungal tone; do not carry required mechanics.
- Optional if UI context cannot display flavor text.

---

## Recommended Templates

- `Description`: `<Cadence/Timing>: <target> <effect> <limits/exclusions>.`
- `FlavorText`: `<Short fungal thematic line>.`

---

## Good vs Bad Examples

### Good
- `For the rest of the game, before each growth phase, trace a path to the nearest corner and resolve up to N actionable tiles.`
- `One-time on draft: launch up to N toxin spores onto empty tiles.`

### Bad
- `Works like Corner Conduit I, but better.`
- `The colony awakens ancient instincts and surges with unstoppable intent...` (vague mechanics)

---

## Authoring Checklist

Before finalizing a Mycovariant description:
- [ ] Is trigger timing explicit?
- [ ] Is effect cadence/duration explicit?
- [ ] Can this be understood without reading another card?
- [ ] Are key numbers included?
- [ ] Is flavor thematic but optional to mechanics?
