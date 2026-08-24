# Mycelial Lab Pending Decisions

> Historical decision log created 2026-08-17. It records the approval gate before Slice 10 and does not change gameplay. The final Ecology roster is documented in the canonical [Substrate Ecology Roster](../FungusToast.Core/docs/second-level/SUBSTRATE_ECOLOGY_ROSTER.md); retain this file as decision history unless Jake approves its removal.
>
> Detailed mechanic source: [Substrate Ecology Roster](../FungusToast.Core/docs/second-level/SUBSTRATE_ECOLOGY_ROSTER.md). Active implementation plan: [WORKLOG.md](WORKLOG.md).

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
| 5 | Toxin Margin branch | Approved and implemented |
| 6 | Nutrient Afterglow payoff | Not selected; Ecology roster complete |
| 7 | Toxinborne Seeding capstone | Approved and implemented |
| 8 | Move Necrophytic Bloom to Ecology | Approved and implemented |
| 9 | Five later additions to existing lanes | Latent Polymorphism approved and implemented; remaining entries are deferred outside Ecology |
| 10 | Implementation sequence | Complete; final Ecology roster selected |

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

## 5. Approve Toxin Margin as the Toxin-Response Branch?

### Proposed mechanic

Growth into an empty tile adjacent to an enemy-owned toxin gains a bonus. It never grows into or removes the toxin tile.

### Proposed shape

- Tier 3; requires Aerated Frontier 5 and Homeostatic Harmony 5.
- +1.5 percentage points per level; 5 levels.
- The target must be legal, empty, and orthogonally adjacent to an enemy-owned toxin. Multiple toxins do not multiply the bonus.
- The bonus applies to cardinal and enabled Tendril diagonal attempts, remains under the shared Ecology cap, and never makes toxin tiles legal growth targets.

### Recommendation

Approve. It creates a midgame toxin response that is distinct from Mycotoxin Catabolism: Catabolism removes toxins, while Toxin Margin makes routes around enemy toxin fields more reliable.

### Answer

Approved on 2026-08-23 and implemented as **Toxin Margin**. It records enemy-toxin-adjacent attempts and bonus-attributable growths for Simulation.

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

Not selected on 2026-08-23. The final Substrate Ecology roster is complete without a delayed nutrient-payoff mechanic. This concept remains historical only and requires a new approval if reconsidered.

## 7. Toxinborne Seeding Ecology Capstone

### Approved mechanic

Toxinborne Seeding is Tier 5 Substrate Ecology. Each of its three levels grants +10 percentage points growth chance when an empty growth target is orthogonally adjacent to an owned toxin. A successful qualifying growth relocates the selected toxin to one empty tile next to an enemy living cell, retaining only its remaining lifespan, and carries the newly colonized cell with it. The cell lands in a random open orthogonal tile next to the toxin, or is lost if no such tile exists. A carried cell cannot trigger another seeding.

### Prerequisites

- Toxin Margin 5, to establish toxin-boundary Ecology play.
- Mycotoxin Potentiation 5, to create durable toxin launch sites.
- Putrefactive Mycotoxin 2, to ensure the capstone belongs to an intentional toxin build rather than incidental toxin use.

### Answer

Approved by Jake on 2026-08-23 and implemented. Ecological Succession is retired from the active roster.

## 8. Move Necrophytic Bloom from Genetic Drift to Substrate Ecology?

### Proposed change

Move the existing compost mechanic into Ecology because it turns dead-cell clusters into neutral environmental nutrient patches. Preserve mutation ID 18, owned levels, constants, timing, effect behavior, tracking, and save compatibility.

### Consequences

- Category-filtered AI spending, Unity placement, investment totals, Simulation ordering, and reporting will change category.
- The approved Ecology prerequisites are Autolytic Surge 2, Detrital Enzymes 3, and Adaptive Expression 3.
- Hyperadaptive Drift's prerequisite remains linked by ID and need not change behavior.
- Processor ownership may move out of `GeneticDriftMutationProcessor`, but Decay-end timing must remain unchanged.

### Recommendation

Approve the category move. The mechanic is fundamentally environmental composting rather than mutation-point economy.

### Answer

Approved on 2026-08-23 and implemented. Mutation ID 18, owned levels, constants, Decay-end composting timing, effect behavior, tracking, and save compatibility are unchanged. Its category is now Substrate Ecology and its Tier-4 prerequisites are Autolytic Surge 2, Detrital Enzymes 3, and Adaptive Expression 3.

## 9. Approve the Five Later Additions to Existing Lanes?

These are later design inputs, not part of Slice 10:

1. **Apical Dominance - Growth:** newly grown cells get a capped bonus on their first outward attempt in the same Growth Phase.
2. **Septal Isolation - Cellular Resilience:** when a cell dies, adjacent friendly cells receive a capped reduction to their next death chance.
3. **Toxin Anastomosis - Fungicide:** placing an owned toxin beside another can extend the older toxin's life by one round, capped per toxin.
4. **Latent Polymorphism - Genetic Drift:** approved and implemented as a capped interest payout when points are banked; see answer below.
5. **Saprotrophic Pulse - Mycelial Surges:** while active, Ecology-qualified growth near dead matter can reclaim one adjacent owned dead cell.

### Main decision

Approve these five as concepts for later detailed review, reject any now, or require replacements before the whole-tree target remains 45 mutations?

### Recommendation

Approve them only as backlog concepts. Each should receive its own values, prerequisites, save-state review, and implementation approval when its batch begins.

### Answer

Latent Polymorphism was approved separately as the Tier-4 Genetic Drift replacement for Necrophytic Bloom and implemented on 2026-08-23.

- Requires Mutator Phenotype 7, Adaptive Expression 5, and Anabolic Inversion 3.
- Each of five levels grants 10% interest on the pre-interest banked amount, rounded down, capped at 5 bonus mutation points per Mutation Phase.
- It resolves immediately for the existing human and AI bank actions, so it adds no deferred state or save/resume complexity. Compound Reserve remains a separate, additive adaptation reward.
- The other four later-addition concepts remain pending.

## 10. Approve the Implementation Sequence?

### Proposed sequence

1. Add the Core Substrate Ecology category and implement Aerated Frontier end to end. **Completed in Slice 10.**
2. Design a replacement open-substrate specialization, then add it with Compaction Pressure as the stateless spatial pair.
3. Add Detrital Enzymes and Toxin Margin as the stateless contextual pair. **Completed.**
4. Move Necrophytic Bloom in an isolated compatibility-focused slice. **Completed.**
5. Design and add Nutrient Afterglow's pending state.
6. Add Ecological Succession after resolving failed-growth ordering.
7. Review and batch the five existing-lane additions.
8. Run whole-tree simulation, balance, and usability hardening.

### Recommendation

Approve. This proves the category with the least persistent-state risk and keeps every mechanic family independently testable and revertible.

### Answer

Complete on 2026-08-23. The implemented Ecology roster is Aerated Frontier, Crustward Tropism, Compaction Pressure, Detrital Enzymes, Toxin Margin, Necrophytic Bloom, and Toxinborne Seeding. Nutrient Afterglow and Ecological Succession are not in scope.

## Deferred Balance Decisions

The following should not block conceptual approval unless Jake wants to set them now:

- Exact shared cap for stacked Ecology growth bonuses.
- Final per-level values and maximum levels.
- AI weights after effect-correctness tests.
- Exact Nutrient Afterglow budget formula and persistence representation.
- Exact Creeping Mold/Ecological Succession failure ordering.
- Final prerequisite thresholds after reachability and simulation review.
