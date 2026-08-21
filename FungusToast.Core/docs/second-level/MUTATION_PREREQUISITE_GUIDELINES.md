# Mutation Prerequisite Guidelines

> **Related Documentation**: For mutation implementation workflow, see [../NEW_MUTATION_HELPER.md](../NEW_MUTATION_HELPER.md). For gameplay architecture context, see [../ARCHITECTURE_OVERVIEW.md](../ARCHITECTURE_OVERVIEW.md). For the full documentation hierarchy, see [../README.md](../README.md).

## Purpose

This document captures the design intent behind mutation categories, prerequisite structure, and progression rules. Use it when adding or reworking mutations so the upgrade tree stays strategically diverse and reachable.

## Mutation System Categories

The mutation tree is organized into six categories, each supporting a different strategic lane.

### Growth

- Focus: territory expansion and colonization
- Common mechanics: cardinal growth bonuses, diagonal tendrils, growth amplification, movement-based spread
- Strategic role: board control and expansion tempo
- Representative mutations: Mycelial Bloom, Tendril variants, Mycotropic Induction, Creeping Mold

### Cellular Resilience

- Focus: survival, death resistance, and recovery
- Common mechanics: death-probability reduction, lifespan extension, spore-on-death, reclamation, dead-cell interaction
- Strategic role: defensive stability and long-game persistence
- Representative mutations: Homeostatic Harmony, Chronoresilient Cytoplasm, Necrosporulation, Regenerative Hyphae

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

### Substrate Ecology

- Focus: environmental opportunity, substrate condition, and territory context
- Candidate mechanics: nutrient-patch interaction, composting and dead-zone use, crowded versus open substrate, edge/corner behavior, contested-tile ecology, and environmental conditioning
- Strategic role: adapt expansion and recovery to the local board environment
- Boundary: does not absorb raw expansion, mutation-point generation, direct toxin offense, or temporary activated effects from the existing categories
- Representative mutation: Aerated Frontier, the Tier-1 root that rewards growth from cells with at least two open orthogonal neighbors
- Status: active Core/AI category as of Slice 10; later roster entries remain individually approval-gated

## Category Design Philosophy

### Readable specialization with selective bridges

- Most progression should form readable local ladders within a category.
- Cross-category prerequisites are selective bridges, not a requirement imposed by tier.
- A bridge should express a mechanical relationship players can predict and deliberately build toward.
- Categories should provide both early foundations and late specialization without forcing every strong build through the same prerequisite package.

### Synergistic interactions

- Categories should complement each other rather than function as isolated ladders.
- Example: Growth plus Fungicide supports expansion protected by toxin pressure.
- Example: Cellular Resilience plus Genetic Drift supports survival with accelerated progression.

### Tier progression

- Tier 1 establishes root capabilities.
- Mid tiers deepen a category and may introduce an occasional thematic bridge.
- Higher tiers may combine systems when the destination mutation genuinely behaves like a fusion or capstone.

## Strategic Complexity Principles

All current prerequisites are `AND` requirements. Adding another prerequisite therefore adds obligation, not choice: it makes the build more prescribed unless the required mechanics already form a coherent strategy. More links should not be treated as inherently deeper or more interesting progression.

Use cross-category complexity when it:

- makes the destination feel like a recognizable fusion of the required systems;
- rewards a coherent hybrid build the player may already want;
- communicates the destination mutation's intended strategy; or
- makes a rare, transformative capstone feel earned.

Avoid cross-category complexity when it:

- forces an otherwise unwanted purchase merely to slow access;
- substitutes unrelated mutations for a point or investment gate;
- makes unrelated builds converge on the same prerequisite package;
- requires tracing several distant chains to understand one destination; or
- cannot be explained in one short thematic sentence.

When broad investment is required only for balance, prefer higher required levels, upgrade costs, or a future explicit category-investment gate over unrelated named prerequisites. If the rules later support `ANY` requirements, use them sparingly and present them distinctly: alternatives can create real build choice, while additional `ALL` requirements cannot.

### Aggregate foundation gates

- Use a category-investment prerequisite when a capstone should require broad development without prescribing unrelated named mutations.
- Define the qualifying tier, minimum root-mutation levels per category, and minimum category count explicitly. Levels cannot be pooled across categories unless the prerequisite says so.
- Aggregate gates complement named thematic prerequisites; they do not create synthetic mutation-to-mutation edges in the dependency graph.
- AI planning, unlock timing, saves, progress snapshots, and purchase UI must all consume the shared Core evaluator.
- Present one grouped progress requirement in the UI, including per-category owned/required levels and the number of qualifying categories completed.

## Prerequisite Design Rules

### 1. Category diversification

- Tier 1 mutations are category roots.
- Tier 2 mutations should normally use a simple same-category prerequisite.
- Tier 3 mutations should remain mostly local; an occasional cross-category bridge is appropriate when the mechanics clearly reinforce each other.
- Tier 4 and above should normally have one primary-category prerequisite and no more than one thematic cross-category prerequisite.
- Three or more direct prerequisites are reserved for rare capstones or explicit set-completion mechanics.

### 2. Tier progression limits

- Avoid requiring more than two prerequisites from the same tier.
- Treat tightly linked systems, such as the tendril set, as one system requirement where appropriate rather than stacking same-tier gates excessively.
- Do not add a redundant direct edge when an upstream prerequisite already guarantees the same mutation at an adequate level.

### 3. Prerequisite depth control

- Avoid chains deeper than three levels.
- Deep chains make builds feel scripted and can create dead-end progression paths.

### 4. Cross-category thematic synergy

- High-tier prerequisites should support the mutation's theme.
- The prerequisite set should make the destination mutation feel earned rather than arbitrary.
- Every cross-category edge should have a one-sentence explanation connecting the prerequisite mechanic to the destination mechanic.

### 5. Early-game accessibility

- Tier 1 mutations should remain root mutations.
- Tier 2 mutations should generally depend only on Tier 1 mutations.

### 6. Power gating

- Stronger mutations should require meaningfully more total prerequisite investment.
- Do not under-gate Tier 5 effects compared with their strategic impact.
- Increase required levels or costs before adding a weakly related named prerequisite solely as a delay.

### 7. Reachability and DAG safety

- Prerequisite graphs must remain acyclic.
- Every mutation should remain reachable from available root mutations.

## Guideline Ranges

### Total prerequisite level ranges

These ranges measure cumulative minimum investment across unique ancestors, not only the destination's direct edges. If several paths share an ancestor, count the highest required level for that ancestor once.

- Tier 2: 5-15 total prerequisite levels
- Tier 3: 10-25 total prerequisite levels
- Tier 4: 15-35 total prerequisite levels
- Tier 5: 25-50 total prerequisite levels

### Direct prerequisite complexity budget

| Destination | Direct prerequisites | Cross-category prerequisites | Mechanical themes combined |
| --- | ---: | ---: | ---: |
| Normal mutation | 1-2 | 0-1 | 1-2 |
| Rare capstone or set completion | Up to 3 | Up to 2 | 2-3 |

The target maximum chain depth remains three levels. A tightly linked named set may exceed three direct edges only when the UI groups and explains it as one requirement, rather than presenting it as several unrelated gates.

## Validation Checklist

When adding or revising a mutation definition:

1. Explain each cross-category edge in one short thematic sentence.
2. Verify direct, cross-category, and same-tier prerequisite counts are within the complexity budget.
3. Remove redundant direct edges already guaranteed by the upstream graph.
4. Calculate total prerequisite levels against the target tier.
5. Confirm the mutation is reachable from root mutations without exceeding the depth limit.
6. Check that the requirements reward a coherent build rather than an arbitrary shopping list.

## Current Review Hotspots

1. Ontogenic Regression now uses Hyperadaptive Drift 2 plus the explicit 10 Tier-1 levels in each of three categories foundation gate; acquisition timing still needs simulation evidence.
2. Hypersystemic Regeneration remains a rare four-edge upstream capstone path; the other live category paths now stay within the target three-edge depth.
3. Mycotropic Induction's four Tendril prerequisites remain an explicit set-completion exception and need grouped UI treatment.

## Implementation References

- `FungusToast.Core.Mutations.MutationCategory`
- `FungusToast.Core.Mutations.MutationRepository`
- `FungusToast.Core.Mutations.MutationPrerequisiteEvaluator`
- `FungusToast.Core.Mutations.MutationCategoryInvestmentPrerequisite`
- Category factories under `FungusToast.Core/Mutations/Factories/`
- `FungusToast.Core.Config.GameBalance`
