# Fungus Toast - Gameplay Terminology

> **Related Documentation**: For technical architecture context, see [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md). For mutation authoring workflow, see [NEW_MUTATION_HELPER.md](NEW_MUTATION_HELPER.md). For the copy checklist shared by all three content systems, see [second-level/CONTENT_COPY_CHECKLIST.md](second-level/CONTENT_COPY_CHECKLIST.md). For flavor-text voice, see [UI_STYLE_GUIDE.md](UI_STYLE_GUIDE.md) section 1. For the full documentation hierarchy, see [README.md](README.md).

## Purpose

This document is the canonical terminology reference for gameplay state transitions and related domain terms that must remain consistent across code, analytics, simulation output, and UI copy.

When another document needs one of these terms, link here instead of redefining it.

Every rule below applies equally to Mutation, Mycovariant, and Adaptation copy unless a section says otherwise.

## Cell and Toxin State-Transition Verbs

Fungus Toast uses precise verbs for board-state changes so gameplay logic, analytics labels, and documentation remain unambiguous.

### Fungal Cell Placement and Replacement

| Action | Canonical Verb | Description |
|--------|----------------|-------------|
| Place a new living cell in an empty tile | `Colonize` | A fungal cell spreads into an unoccupied tile. |
| Place a new living cell over any dead cell | `Reclaim` | A fungal cell restores a dead cell to living status by occupying that tile. |
| Place a new living cell over an enemy living cell | `Infest` | A fungal cell kills an enemy living cell and takes the tile. |
| Place a new living cell over a toxin tile | `Overgrow` | A fungal cell removes a toxin tile by growing into it. |
| Place a new living cell regardless of what occupies the tile | `Replace` | Shorthand for an effect that will Colonize, Reclaim, Infest, or Overgrow whatever it lands on. Write it as "replaces any non-Resistant occupant". Use only when the effect really does all four; otherwise name the specific verbs. |

### Toxin Placement and Toxin-Caused Conversion

| Action | Canonical Verb | Description |
|--------|----------------|-------------|
| Place toxin in an empty or dead tile | `Toxify` | A toxin is introduced to a non-living tile. |
| Place toxin over a living cell | `Poison` | A living cell is killed and converted into a toxin tile. |

### Death, Removal, Relocation, and Expiry

| Action | Canonical Verb | Description |
|--------|----------------|-------------|
| A living cell becomes a dead cell | `Kill` / `Die` | Use `kill` when the effect is the actor ("kills an adjacent enemy cell") and `die` when the cell is the subject ("when one of your cells dies"). A kill leaves a dead cell; if it leaves a toxin instead, that is `Poison`. |
| Remove a cell or toxin so the tile becomes empty | `Clear` | The occupant is removed and nothing is placed. Use for toxin neutralization, corpse removal, and any "leaving the tile empty" effect. Do not use *remove*, *consume*, *neutralize*, *destroy*, or *dissolve* in mechanics copy. |
| A cell or toxin leaves one tile and appears on another | `Move` | The same cell or toxin changes tile (Creeping Mold, Hyphal Draw, Toxinborne Seeding, Chemotactic Mycotoxins, Conidial Relay). Do not use *relocate*, *redeploy*, *crawl*, *drift*, or *travel* in mechanics copy; those belong in names and flavor. |
| A toxin reaches the end of its lifespan and disappears | `Expire` | Distinct from `Clear`: expiry is the toxin's own timer running out. "Expired toxin" is the noun form. |

### Summary Table

| Source State | Target State | Canonical Verb |
|--------------|--------------|----------------|
| Empty | Living Cell | `Colonize` |
| Own Dead Cell | Living Cell | `Reclaim` |
| Enemy Dead Cell | Living Cell | `Reclaim` |
| Enemy Living Cell | Living Cell | `Infest` |
| Toxin | Living Cell | `Overgrow` |
| Any non-Resistant occupant | Living Cell | `Replace` (shorthand for the four above) |
| Empty or Dead Cell | Toxin | `Toxify` |
| Living Cell | Toxin | `Poison` |
| Living Cell | Dead Cell | `Kill` / `Die` |
| Living, Dead, or Toxin | Empty | `Clear` |
| Tile A | Tile B (same occupant) | `Move` |
| Toxin (lifespan ends) | Empty | `Expire` |

## Distinction Rules

- Use `Colonize` only for growth into an empty tile.
- Use `Reclaim` for conversion of any dead cell into a living cell.
- Use `Infest` only when an enemy living cell is displaced by another player's living cell.
- Use `Overgrow` only when a toxin tile is removed by new growth.
- Use `Toxify` only when toxin is placed on a non-living tile.
- Use `Poison` only when toxin placement kills a living cell.
- Use `Kill` only when a living cell becomes a dead cell. If a toxin is left behind, it is `Poison`.
- Use `Clear` only when the tile ends up empty. If something is placed in the same step, use the placement verb instead.
- Use `Move` only when the same cell or toxin persists on a new tile. If a new cell is created elsewhere, that is `Colonize`.

## Usage Guidance

### Code and event naming

- Prefer these verbs in event names, helper methods, analytics counters, and documentation headings.
- Avoid introducing near-synonyms for these same transitions unless the mechanic is meaningfully different.

### Analytics and exports

- Use the canonical verbs in player-facing summaries and exported metric labels where practical.
- If a report uses a derived phrase, make sure it still maps cleanly back to one of the verbs in this document.

### UI copy and tooltips

- Tooltips and log text may use more readable sentence forms, but the underlying mechanic name should still map to the canonical verb.
- In player-facing descriptions, use the canonical verb whenever describing one of these exact state changes. Do not substitute near-synonyms such as *invade*, *occupy*, *convert*, *take over*, *remove*, *neutralize*, or *relocate*; those words make players wonder whether a different rule applies.
- Lead with a plain-language benefit, then name the mechanic precisely in the details. On a term's first use in a description, add a short natural-language outcome when it helps comprehension (for example, "reclaim a dead cell, restoring it as your living cell"). Do not re-define the term on every use.
- Flavor text and names may use evocative language such as *infiltration* or *invasion*, provided they are not explaining rules.

## Resistance

`Resistant` is a permanent status on a living cell. It is the most referenced status in the game and the one most often written inconsistently, so the rules are strict.

- **Casing:** always capitalize `Resistant`, `non-Resistant`, and `Resistance` in player-facing copy. Never write *resistant* lowercase in a description.
- **Gaining it:** "becomes Resistant" (singular) / "become Resistant" (plural). Do not write *gains Resistance*, *gains Resistant*, *comes back resistant*, or *is made Resistant*.
- **Losing it:** "loses Resistance".
- **The opposite:** `non-Resistant`. Never *killable*, *vulnerable*, *unprotected*, or *normal*.
- **Noun forms:** "Resistant cell", "your Resistant cells", "enemy Resistant cells".
- **Canonical immunity sentence:** `Resistant cells cannot be killed, infested, or poisoned.` Use this exact sentence when a description needs to explain what Resistance does. Do not shorten it to "cannot be killed" or "cannot be killed or infested".
- **Interaction phrasing:** when an effect skips them, write "skipping Resistant cells" or "Resistant cells are unaffected".

## Adjacency and Distance

- `orthogonal` is the canonical word for the four up/down/left/right neighbors. Do not use *cardinal*.
- `diagonal` is the canonical word for the four corner neighbors.
- On the first orthogonal reference in a description, append the hint `(up / down / left / right)`. Do not repeat the hint later in the same description.
- "orthogonally adjacent" and "diagonally adjacent" are the adjective forms. "adjacent (including diagonally)" is the form for all eight neighbors.
- "orthogonal growth chance" and "diagonal growth chance" are the stat names. Never *cardinal growth chance*.
- "within N tiles" means a square: every tile whose row and column distance from the source are both at most N, diagonals included. Write "within N tiles, including diagonals" on first use. If a mechanic uses a different shape (a line, a circle, a plus), say so.

## Board Nouns

| Term | Meaning | Usage |
|------|---------|-------|
| `toast` | The play surface as a whole, in its fantasy framing. | Preferred in summaries and flavor ("somewhere else on the toast"). |
| `board` | The play surface as a grid. | Acceptable in technical copy where geometry matters ("board size", "center of the board", "off the board"). Either word is fine; do not mix them inside one sentence. |
| `crust` | The outermost ring of playable tiles. | Gloss it on first use in a description as "the board edge (the crust)". |
| `playable crust` | The crust of the current background, which may be irregular. | Use only when the distinction from a rectangular edge matters. |
| `corner` | One of the four corner tiles of the board. | No gloss needed. |
| `starting spore` | The tile a player begins on, and the cell that starts there. | Never *starting cell*, *starting tile*, or *spawn*. |
| `tile` | A position on the board. | Tiles are empty or hold a cell, toxin, nutrient patch, or block. |
| `cell` | A fungal cell occupying a tile. | Living or dead. Toxins are not cells. |
| `nutrient patch` | See the patch terms below. | Lowercase generic; capitalize the typed labels. |
| `permanent block` | A tile that can never be occupied. | Only relevant on backgrounds with blocked tiles. |

## Time and Phases

- A game is a sequence of `rounds`. Each round has a `Mutation Phase`, a `Growth Phase`, and a `Decay Phase`, in that order.
- A `Growth Cycle` is one growth-resolution pass within the Growth Phase. Multiple Growth Cycles occur in a single round.
- Phase and cycle names are always Title Case in player-facing copy: `Growth Phase`, `Decay Phase`, `Mutation Phase`, `Growth Cycle`. Never *during decay*, *at growth start*, or plain *cycles*.
- Timing phrases: "At the start of each Mutation Phase", "Before each Growth Phase", "At the end of each Growth Phase", "During the Decay Phase", "At the end of each Decay Phase". Prefer these exact forms.
- Round references: "At the start of round N", "At the end of round N", "At the start of round N's Mutation Phase".
- Surges: a `Mycelial Surge` is `activated`, is `active` for a number of rounds, and then `ends`. Write "While active, ..." for effects that only apply during the surge.

## Mutation Economy

- `mutation points` is always lowercase. Never *Mutation Points* or *MP* in player-facing copy.
- `bank` / `banking` is the act of choosing to keep unspent mutation points at the end of a Mutation Phase.
- `level` is one upgrade step of a mutation; `max level` is its cap. "gains N free levels" is the phrase for level grants; "free upgrade" is the phrase for a single automatic upgrade attempt.
- "ignoring prerequisites" is the phrase for grants that bypass the tree.
- Mutation tiers are Arabic numerals: `Tier 1` through `Tier 6`. Mycovariant ranks are Roman numerals in the name only: `Plasmid Bounty I`, `Mycelial Bastion III`. Never write *Tier I mutation* or *Corner Conduit 2*.
- `surge` lowercase is the generic noun; `Mycelial Surge(s)` is the proper name of the category and of any mutation in it. Never *Surge Mutations*.

## Proper Nouns and Casing

- `Mutation`, `Mycovariant`, `Adaptation` are capitalized when they name the content type ("draft a Mycovariant", "your Adaptations"). Lowercase `mutation` is acceptable when it is the ordinary noun inside a sentence ("a random Tier 2 mutation").
- Named abilities are always written in full and in Title Case: `Mycotoxin Tracer`, `Chemotactic Beacon`, `Putrefactive Mycotoxin`. Never internal shorthand such as *chemobeacon*, *tracer*, or *putrefactive* on its own.
- The Chemotactic Beacon's placed target is its `marker`. Write "active Chemotactic Beacon marker".
- Nutrient patch labels match the in-game names exactly: `Adaptogen Patch`, `Sporemeal Patch`, `Hypervariation Patch`. Never *Hypervariation Development patch*.
- `Human` and `AI` are capitalized when they name a player type in bait-family Mycovariant copy ("if Human, ...; if AI, ...").

## Opponents

- `enemy` is the word for rival players and their cells in mechanics copy: "enemy living cell", "enemy toxin", "enemy starting spore", "the enemy with the most living cells".
- `rival`, `competitor`, `foe`, and `invader` are reserved for names and flavor text.
- `your` / `you` addresses the player. Do not write *the player* or *the owner* in descriptions.

## Scope Phrases

- Mycovariant passives: `For the rest of the game, ...`
- Adaptation passives: `For the rest of the campaign, ...`
- One-shot Mycovariant effects: `One-time on draft: ...`
- Starting Adaptations (always on from game start) may omit the scope phrase; every draftable Adaptation passive must include it.

## Emphasis and Encoding

- `<b>` is used only for the structural labels `<b>Technical:</b>` and `<b>Max Level Bonus:</b>`. Never bold numbers, names, or phrases inside a sentence.
- ASCII punctuation only: hyphens, not en/em dashes; `x`, not the multiplication sign; straight quotes. This applies to all three content systems.
- Numbers come from named balance constants; format percentages through the shared helpers so precision is consistent.

## Related Domain Terms

### Living cell

A fungal cell currently occupying a tile and participating in growth, death, and mutation-driven effects.

### Dead cell

A tile representing fungal remains rather than an active colony cell. Dead cells may be reclaimed or otherwise interacted with by mutations. Use `dead cell` in mechanics copy; *corpse*, *remains*, and *dead matter* belong in names, flavor, and plain-language summaries only. Do not write *dead tile*.

### Compost / Composting

The conversion of a dead cell into a nutrient patch. Use `Compost` or `Composting` for this transformation. Do not use `Compositing`.

### Toxin tile

A tile occupied by toxin rather than a living cell. Toxin tiles may block or transform later growth depending on the mechanic. "toxin" alone is acceptable once the context is clear; "toxin drop" is a single placement event.

### Growth cycle

See [Time and Phases](#time-and-phases). Written `Growth Cycle` in copy.

### Surge

A manually activated mutation effect with a limited duration and escalating activation cost. See [Mutation Economy](#mutation-economy).

### Nutrient patch

A multi-tile board resource cluster claimed by the first living cell that grows onto any tile in that cluster. The cluster resolves as one reward event rather than as separate per-tile pickups.

### Adaptogen Patch

A nutrient patch type that grants mutation points equal to the claimed cluster size.

### Sporemeal Patch

A nutrient patch type that grants free growth across the rest of the claimed cluster instead of mutation-point income.

### Hypervariation Patch

A nutrient patch type that grants the claiming colony a private Mycovariant draft at the next normal draft timing.
