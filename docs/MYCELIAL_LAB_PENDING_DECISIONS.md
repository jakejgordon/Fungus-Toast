# Mycelial Lab Pending Decisions

> Temporary decision log created 2026-08-17. This file records the approval gate before Slice 10; it does not change gameplay. Once every decision is resolved, fold the answers into the canonical mutation documents and remove this file only with Jake's approval.
>
> Detailed mechanic source: [Substrate Ecology Roster Proposal](../FungusToast.Core/docs/second-level/SUBSTRATE_ECOLOGY_ROSTER.md). Active implementation plan: [WORKLOG.md](WORKLOG.md).

## How to Use This Log

- Answer one numbered question at a time with **Approve**, **Approve with changes**, or **Rework**.
- Names, prerequisites, and numeric values remain proposals until their question is resolved.
- Initial numbers are test hypotheses, not final balance commitments.
- Decisions 1-8 define Substrate Ecology. Decision 9 covers later additions to the existing lanes. Decision 10 controls implementation order.

## Decision Status

| # | Decision | Status |
|---|---|---|
| 1 | Substrate Sensing root | Rejected; Aerated Frontier replacement implemented |
| 2 | Crustward Tropism edgeward branch | Approved and implemented |
| 3 | Compaction Pressure branch | Approved and implemented |
| 4 | Detrital Enzymes branch | Approved and implemented |
| 5 | Rival Rhizosphere branch | Pending |
| 6 | Nutrient Afterglow payoff | Pending |
| 7 | Ecological Succession capstone | Pending |
| 8 | Move Necrophytic Bloom to Ecology | Pending |
| 9 | Five later additions to existing lanes | Pending |
| 10 | Implementation sequence | Pending |

## 1. Approve Substrate Sensing as the Tier-1 Ecology Root?

### Proposed mechanic

Before a normal growth roll, an attempt targeting an empty, occupiable tile orthogonally adjacent to an unconsumed nutrient patch receives a growth-chance bonus. The nutrient-patch tile itself does not qualify, and adjacency to multiple patches does not multiply the bonus.

### Proposed shape

- Category/tier: Substrate Ecology, Tier 1 root.
- Initial value: +1.5 percentage points per level.
- Initial maximum: 10 levels, or +15 percentage points before the shared Ecology cap.
- Prerequisites: none.
- Counterplay: consume the patch, occupy its perimeter, or deny legal approach tiles.
- AI: value reachable nutrient-adjacent empty tiles; assign low utility when none exist.
- Tracking: qualifying attempts, qualifying tiles, and successes attributable to the bonus.
- Limits: empty/occupiable targets only; no bonus on the patch itself; one adjacency bonus per attempt.

### Why this is recommended

- It establishes Ecology's identity immediately: read and exploit the local substrate rather than gain unconditional growth.
- It is deterministic to evaluate and requires no pending save state.
- It creates visible counterplay around nutrient patches.
- It does not replace Rhizomorphic Hunger or other mechanics that act directly on nutrient tiles.
- It is a low-risk first mechanic for proving the new category across Core, AI, Simulation, tracking, and Unity.

### Risks and questions inside the proposal

- It has no effect when a board has no nutrient patches or all patches have been consumed. That is acceptable for a contextual lane only if the root remains understandable and AI does not overvalue it.
- A +15-point maximum may be too strong when combined with other conditional bonuses, so the final value requires focused tests and the shared Ecology cap.
- The roster currently gives Nutrient Afterglow a Substrate Sensing 15 prerequisite despite this proposed maximum of 10. Recommendation: retain the 10-level maximum and correct that later prerequisite to level 10.

### Recommendation

Approve the name, mechanic, Tier-1 placement, and 10-level shape. Treat +1.5 points per level and the combined cap as provisional balance values.

### Answer

Rejected (`1C`) on 2026-08-17.

Reasons:

- Nutrient patches are sparse and usually distant from the starting colony, so the mutation would often provide no early benefit despite being the Tier-1 root.
- Its opportunity is self-erasing: growth onto the nutrient patch consumes the patch, making the surrounding bonus transient.

Substrate Sensing must not be implemented in its proposed form. Its approved replacement is recorded below.

### Approved replacement (`1R-A`)

Jake approved promoting **Aerated Frontier** to the Tier-1 root with a revised trigger: a source cell must have at least two orthogonally adjacent open spaces. The implemented first-pass shape is +0.5 percentage points per level, 20 levels. Cells, toxins, nutrient patches, permanent blocks, and active chemobeacons are not open. The bonus applies to cardinal and Tendril growth attempts from a qualifying source.

Implemented across Core, AI, Simulation tracking/reporting, Unity mutation-tree presentation, and focused tests in Slice 10. Two Testing strategies begin with staged Aerated Frontier investment: `TST_EcologyFrontierExpansion` and `TST_EcologyFrontierResilience`.

## 2. Replace the Vacated Tier-2 Open-Substrate Branch?

### Superseded proposal

Aerated Frontier was originally proposed as a Tier-2 branch using five open tiles among all eight neighbors. It has instead been promoted and reworked into the approved Tier-1 root described under Decision 1.

### Answer

Approved as **Crustward Tropism** on 2026-08-22 and implemented as the Tier-2 replacement.

- Requires Aerated Frontier 10 and costs 2 mutation points per upgrade.
- Each of its 5 levels adds +0.75 percentage points to legal cardinal or enabled Tendril diagonal growth attempts whose target is strictly closer to the shape-aware playable crust than the source. Aerated Frontier is +0.4 percentage points per level for 20 levels after the approved light retune on 2026-08-22.
- At max level, once per Growth Cycle, the first qualifying attempt that would place a new cell on the playable crust succeeds automatically. The allowance is per player, resets each cycle, and leaves the normal RNG draw in sequence.
- It records qualifying attempts, bonus-attributable growths, and automatic crust arrivals for Simulation.
- The active testing AIs max Crustward Tropism, then Creeping Mold, then Detrital Enzymes before their remaining Growth/Resilience build; their Aerated Frontier timing remains the distinguishing choice.

## 3. Approve Compaction Pressure as the Crowded-Substrate Branch?

### Proposed mechanic

Growth attempts from a living owned cell with only one or two legal neighboring growth targets gain a bonus. A fully sealed cell receives no synthetic attempt.

### Proposed shape

- Tier 2; proposed replacement prerequisite is Aerated Frontier 10.
- +2 percentage points per level; 5 levels.
- Helps colonies escape congestion without attacking, replacing, or bypassing occupants.
- Included in the shared Ecology cap.

### Recommendation

Approve. It creates the opposite spatial incentive from Aerated Frontier while keeping the qualification rules mutually clear.

### Answer

Approved on 2026-08-23 and implemented as the Tier-2 sibling of Crustward Tropism.

- Requires Aerated Frontier 10 and costs 2 mutation points per upgrade.
- Each of its 5 levels adds +2 percentage points to every legal cardinal or enabled Tendril diagonal growth attempt from a living source with one or two legal orthogonal growth targets. Fully sealed sources do not gain a synthetic attempt.
- Detrital Enzymes now requires level 1 of either Crustward Tropism or Compaction Pressure. The prerequisite model represents this as a general ANY group: all ordinary requirements still apply, and each ANY group needs one satisfied alternative.
- `TST_EcologyCompactionFirst` explicitly maxes Compaction Pressure, excludes Crustward Tropism from both paid and automatic spending, then follows the established Creeping Mold, Detrital Enzymes, and Growth/Resilience runway.

## 4. Approve Detrital Enzymes as the Dead-Matter Branch?

### Proposed mechanic

Growth into an empty tile adjacent to any dead, non-toxin cell gains a bonus. The dead cell is a catalyst; it is not reclaimed or consumed by this mutation.

### Proposed shape

- Tier 3; requires Crustward Tropism 1, which already requires Aerated Frontier 10.
- +1 percentage point per level; 5 levels.
- Own and enemy non-toxic dead cells qualify. At max level, a target beside two or more qualifying dead cells gains an additional +1 percentage point; further dead neighbors do not stack.
- Reclamation, composting, or occupation removes the opportunity.

### Recommendation

Approve. It gives Ecology a clear relationship with dead zones without stealing direct reclamation from Cellular Resilience.

### Answer

Approved on 2026-08-23 and implemented as the Tier-3 dead-matter branch. It remains entirely within Substrate Ecology and requires level 1 of either Tier-2 Ecology branch: Crustward Tropism or Compaction Pressure.

## 5. Approve Rival Rhizosphere as the Contested-Boundary Branch?

### Proposed mechanic

Growth into an empty tile adjacent to an enemy living cell gains a bonus. It never attacks or overgrows the enemy cell.

### Proposed shape

- Tier 3; proposed requirements are Aerated Frontier 5, Compaction Pressure 3, and Mycotoxin Tracer 5.
- +3 percentage points per level; 5 levels.
- Enemy dead and toxin cells do not qualify; multiple adjacent rivals do not multiply the bonus.
- Opponents can fill, toxify, or retreat from the contested boundary.

### Recommendation

Approve. It rewards boundary competition while leaving direct offense in Fungicide.

### Answer

Pending.

## 6. Approve Nutrient Afterglow as the Patch-Consumption Payoff?

### Proposed mechanic

Consuming a nutrient patch stores a capped budget that strengthens a limited number of Ecology-qualified growth attempts during the owner's next Growth Phase, then expires.

### Proposed shape

- Tier 4; proposed requirements: Aerated Frontier 15, the replacement open specialization at level 3, and Adaptive Expression 3.
- 5 levels; exact bonus and attempt-budget scaling remain unresolved.
- One stored budget with a hard cap; repeated patch consumption cannot stack without limit.
- Requires explicit save/resume state for a budget that survives between consumption and payoff.

### Main decision

Approve the delayed-payoff concept now, or rework it into an immediate/stateless effect? The delayed version is strategically richer but materially more complex to persist, explain, value in AI, and test.

### Recommendation

Approve the concept but defer implementation until its budget formula and save state are designed. The implemented 20-level Aerated Frontier root makes a level-15 root requirement possible, but the complete prerequisite set remains provisional until the replacement Tier-2 branch is designed.

### Answer

Pending.

## 7. Approve Ecological Succession as the Ecology Capstone?

### Proposed mechanic

Once per Growth Phase, the first failed growth attempt that qualified for any Ecology context immediately retries the same still-legal source and target. The retry cannot recurse.

### Proposed shape

- Tier 5; requires Detrital Enzymes 5, Rival Rhizosphere 5, Nutrient Afterglow 3, and Mycelial Bloom 15.
- 3 levels; initial hypothesis adds +5 percentage points per level to the retry.
- One retry per player per phase, regardless of how many Ecology contexts qualified.
- Requires explicit deterministic ordering relative to Creeping Mold's failed-growth behavior.

### Main decision

Approve a once-per-phase retry capstone, with the Creeping Mold interaction settled by focused design/tests before implementation?

### Recommendation

Approve the concept. Prefer Ecological Succession's immediate same-target retry before Creeping Mold can reposition the source, but confirm that ordering during its implementation slice.

### Answer

Pending.

## 8. Move Necrophytic Bloom from Genetic Drift to Substrate Ecology?

### Proposed change

Move the existing compost mechanic into Ecology because it turns dead-cell clusters into neutral environmental nutrient patches. Preserve mutation ID 18, owned levels, constants, timing, effect behavior, tracking, and save compatibility.

### Consequences

- Category-filtered AI spending, Unity placement, investment totals, Simulation ordering, and reporting will change category.
- Existing prerequisites should be rebalanced separately; the revised Ecology chain is Detrital Enzymes 3, Necrosporulation 5, Anabolic Inversion 3, and Aerated Frontier 5.
- Hyperadaptive Drift's prerequisite remains linked by ID and need not change behavior.
- Processor ownership may move out of `GeneticDriftMutationProcessor`, but Decay-end timing must remain unchanged.

### Recommendation

Approve the category move. The mechanic is fundamentally environmental composting rather than mutation-point economy.

### Answer

Pending.

## 9. Approve the Five Later Additions to Existing Lanes?

These are later design inputs, not part of Slice 10:

1. **Apical Dominance - Growth:** newly grown cells get a capped bonus on their first outward attempt in the same Growth Phase.
2. **Septal Isolation - Cellular Resilience:** when a cell dies, adjacent friendly cells receive a capped reduction to their next death chance.
3. **Toxin Anastomosis - Fungicide:** placing an owned toxin beside another can extend the older toxin's life by one round, capped per toxin.
4. **Latent Polymorphism - Genetic Drift:** banking points can grant a capped chance for a later purchase to add a free level to a different Tier-1 mutation.
5. **Saprotrophic Pulse - Mycelial Surges:** while active, Ecology-qualified growth near dead matter can reclaim one adjacent owned dead cell.

### Main decision

Approve these five as concepts for later detailed review, reject any now, or require replacements before the whole-tree target remains 45 mutations?

### Recommendation

Approve them only as backlog concepts. Each should receive its own values, prerequisites, save-state review, and implementation approval when its batch begins.

### Answer

Pending.

## 10. Approve the Implementation Sequence?

### Proposed sequence

1. Add the Core Substrate Ecology category and implement Aerated Frontier end to end. **Completed in Slice 10.**
2. Design a replacement open-substrate specialization, then add it with Compaction Pressure as the stateless spatial pair.
3. Add Detrital Enzymes and Rival Rhizosphere as the stateless contextual pair.
4. Move Necrophytic Bloom in an isolated compatibility-focused slice.
5. Design and add Nutrient Afterglow's pending state.
6. Add Ecological Succession after resolving failed-growth ordering.
7. Review and batch the five existing-lane additions.
8. Run whole-tree simulation, balance, and usability hardening.

### Recommendation

Approve. This proves the category with the least persistent-state risk and keeps every mechanic family independently testable and revertible.

### Answer

Pending.

## Deferred Balance Decisions

The following should not block conceptual approval unless Jake wants to set them now:

- Exact shared cap for stacked Ecology growth bonuses.
- Final per-level values and maximum levels.
- AI weights after effect-correctness tests.
- Exact Nutrient Afterglow budget formula and persistence representation.
- Exact Creeping Mold/Ecological Succession failure ordering.
- Final prerequisite thresholds after reachability and simulation review.
