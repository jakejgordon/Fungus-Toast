# Mutation Prerequisite Guidelines

> **Related Documentation**: For mutation implementation workflow, see [../NEW_MUTATION_HELPER.md](../NEW_MUTATION_HELPER.md). For gameplay architecture context, see [../ARCHITECTURE_OVERVIEW.md](../ARCHITECTURE_OVERVIEW.md). For the full documentation hierarchy, see [../README.md](../README.md).

## Purpose

This document captures the design intent behind mutation categories, prerequisite structure, and progression rules. Use it when adding or reworking mutations so the upgrade tree stays strategically diverse and reachable.

## Mutation System Categories

The mutation tree is organized into five categories, each supporting a different strategic lane.

### Growth

- Focus: territory expansion and colonization
- Common mechanics: cardinal growth bonuses, diagonal tendrils, reclaim support, growth amplification, movement-based spread
- Strategic role: board control and expansion tempo
- Representative mutations: Mycelial Bloom, Tendril variants, Mycotropic Induction, Regenerative Hyphae

### Cellular Resilience

- Focus: survival, death resistance, and recovery
- Common mechanics: death-probability reduction, lifespan extension, spore-on-death, reclamation, dead-cell interaction
- Strategic role: defensive stability and long-game persistence
- Representative mutations: Homeostatic Harmony, Chronoresilient Cytoplasm, Necrosporulation, Necrohyphal Infiltration

### Fungicide

- Focus: toxin production and enemy disruption
- Common mechanics: toxin placement, adjacent enemy damage, area denial, toxin longevity, toxin-spore effects
- Strategic role: pressure, denial, and board attrition
- Representative mutations: Mycotoxin Tracer, Mycotoxin Potentiation, Putrefactive Mycotoxin, Sporocidal Bloom

### Genetic Drift

- Focus: economy, randomization, and adaptive progression
- Common mechanics: bonus mutation points, automatic upgrades, catch-up systems, toxin cleanup value, population recovery
- Strategic role: economy acceleration and comeback potential
- Representative mutations: Mutator Phenotype, Adaptive Expression, Anabolic Inversion, Necrophytic Bloom

### Mycelial Surges

- Focus: temporary activated effects with escalating costs
- Common mechanics: manual activation, limited duration, escalating activation cost, cooldown-style lockout while active
- Strategic role: tactical burst effects that reward timing
- Representative mutations: Hyphal Surge, Chemotactic Beacon

## Category Design Philosophy

### Balanced investment

- High-tier mutations should encourage cross-category prerequisites.
- Players should need multiple category investments to reach the strongest effects.
- Categories should provide both early foundations and late specialization.

### Synergistic interactions

- Categories should complement each other rather than function as isolated ladders.
- Example: Growth plus Fungicide supports expansion protected by toxin pressure.
- Example: Cellular Resilience plus Genetic Drift supports survival with accelerated progression.

### Tier progression

- Tier 1 establishes root capabilities.
- Mid tiers begin to demand broader investment.
- Higher tiers should reflect cross-category planning rather than single-category rushing.

## Prerequisite Design Rules

### 1. Category diversification

- High-tier mutations should require prerequisites from different categories.
- Tier 4 and above should include at least one non-primary-category prerequisite.

### 2. Tier progression limits

- Avoid requiring more than two prerequisites from the same tier.
- Treat tightly linked systems, such as the tendril set, as one system requirement where appropriate rather than stacking same-tier gates excessively.

### 3. Prerequisite depth control

- Avoid chains deeper than three levels.
- Deep chains make builds feel scripted and can create dead-end progression paths.

### 4. Cross-category thematic synergy

- High-tier prerequisites should support the mutation's theme.
- The prerequisite set should make the destination mutation feel earned rather than arbitrary.

### 5. Early-game accessibility

- Tier 1 mutations should remain root mutations.
- Tier 2 mutations should generally depend only on Tier 1 mutations.

### 6. Power gating

- Stronger mutations should require meaningfully more total prerequisite investment.
- Do not under-gate Tier 5 effects compared with their strategic impact.

### 7. Reachability and DAG safety

- Prerequisite graphs must remain acyclic.
- Every mutation should remain reachable from available root mutations.

## Guideline Ranges

### Total prerequisite level ranges

- Tier 2: 5-15 total prerequisite levels
- Tier 3: 10-25 total prerequisite levels
- Tier 4: 15-35 total prerequisite levels
- Tier 5: 25-50 total prerequisite levels

### Category distribution targets

- Tier 3 and above: at least two categories where practical
- Tier 4 and above: at least two categories, including one non-primary category
- Tier 5: explicit cross-category synergy expected

## Validation Checklist

When adding or revising a mutation definition:

1. Check category diversity for Tier 3 and above.
2. Verify same-tier prerequisite counts are not overloaded.
3. Calculate total prerequisite levels against the target tier.
4. Confirm the mutation is reachable from root mutations.
5. Review whether the prerequisites reinforce the mutation's theme.

## Current Review Hotspots

1. Necrophytic Bloom chain: `Necrosporulation -> Sporocidal Bloom -> Necrophytic Bloom`
2. Fungicide over-specialization in higher tiers
3. Under-gated Tier 5 requirements such as low total-level investment

## Implementation References

- `FungusToast.Core.Mutations.MutationCategory`
- `FungusToast.Core.Mutations.MutationRepository`
- Category factories under `FungusToast.Core/Mutations/Factories/`
- `FungusToast.Core.Config.GameBalance`
