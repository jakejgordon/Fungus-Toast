# Fungus Toast - Gameplay Terminology

> **Related Documentation**: For technical architecture context, see [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md). For mutation authoring workflow, see [NEW_MUTATION_HELPER.md](NEW_MUTATION_HELPER.md). For the full documentation hierarchy, see [README.md](README.md).

## Purpose

This document is the canonical terminology reference for gameplay state transitions and related domain terms that must remain consistent across code, analytics, simulation output, and UI copy.

When another document needs one of these terms, link here instead of redefining it.

## Cell and Toxin State-Transition Verbs

Fungus Toast uses precise verbs for board-state changes so gameplay logic, analytics labels, and documentation remain unambiguous.

### Fungal Cell Placement and Replacement

| Action | Canonical Verb | Description |
|--------|----------------|-------------|
| Place a new living cell in an empty tile | `Colonize` | A fungal cell spreads into an unoccupied tile. |
| Place a new living cell over any dead cell | `Reclaim` | A fungal cell restores a dead cell to living status by occupying that tile. |
| Place a new living cell over an enemy living cell | `Infest` | A fungal cell kills an enemy living cell and takes the tile. |
| Place a new living cell over a toxin tile | `Overgrow` | A fungal cell removes a toxin tile by growing into it. |

### Toxin Placement and Toxin-Caused Conversion

| Action | Canonical Verb | Description |
|--------|----------------|-------------|
| Place toxin in an empty or dead tile | `Toxify` | A toxin is introduced to a non-living tile. |
| Place toxin over a living cell | `Poison` | A living cell is killed and converted into a toxin tile. |

### Summary Table

| Source State | Target State | Canonical Verb |
|--------------|--------------|----------------|
| Empty | Living Cell | `Colonize` |
| Own Dead Cell | Living Cell | `Reclaim` |
| Enemy Dead Cell | Living Cell | `Reclaim` |
| Enemy Living Cell | Living Cell | `Infest` |
| Toxin | Living Cell | `Overgrow` |
| Empty or Dead Cell | Toxin | `Toxify` |
| Living Cell | Toxin | `Poison` |

## Distinction Rules

- Use `Colonize` only for growth into an empty tile.
- Use `Reclaim` for conversion of any dead cell into a living cell.
- Use `Infest` only when an enemy living cell is displaced by another player's living cell.
- Use `Overgrow` only when a toxin tile is removed by new growth.
- Use `Toxify` only when toxin is placed on a non-living tile.
- Use `Poison` only when toxin placement kills a living cell.

## Usage Guidance

### Code and event naming

- Prefer these verbs in event names, helper methods, analytics counters, and documentation headings.
- Avoid introducing near-synonyms for these same transitions unless the mechanic is meaningfully different.

### Analytics and exports

- Use the canonical verbs in player-facing summaries and exported metric labels where practical.
- If a report uses a derived phrase, make sure it still maps cleanly back to one of the verbs in this document.

### UI copy and tooltips

- Tooltips and log text may use more readable sentence forms, but the underlying mechanic name should still map to the canonical verb.

## Related Domain Terms

### Living cell

A fungal cell currently occupying a tile and participating in growth, death, and mutation-driven effects.

### Dead cell

A tile representing fungal remains rather than an active colony cell. Dead cells may be reclaimed or otherwise interacted with by mutations.

### Toxin tile

A tile occupied by toxin rather than a living cell. Toxin tiles may block or transform later growth depending on the mechanic.

### Growth cycle

One growth-resolution pass within the Growth Phase. Multiple growth cycles occur in a single round.

### Surge

A manually activated mutation effect with a limited duration and escalating activation cost.

### Nutrient patch

A multi-tile board resource cluster claimed by the first living cell that grows onto any tile in that cluster. The cluster resolves as one reward event rather than as separate per-tile pickups.

### Adaptogen patch

A nutrient patch type that grants Mutation Points equal to the claimed cluster size.

### Sporemeal patch

A nutrient patch type that grants free growth across the rest of the claimed cluster instead of mutation-point income.
