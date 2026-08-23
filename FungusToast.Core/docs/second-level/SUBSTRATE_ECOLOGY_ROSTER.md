# Substrate Ecology Roster

> Status: Complete. Aerated Frontier, Crustward Tropism, Compaction Pressure, Detrital Enzymes, Toxin Margin, Necrophytic Bloom, and Mycotoxin Fission are the final implemented Substrate Ecology roster. Nutrient Afterglow was not selected; it remains a deliberately unimplemented historical concept.
>
> Related: [../NEW_MUTATION_HELPER.md](../NEW_MUTATION_HELPER.md), [MUTATION_PREREQUISITE_GUIDELINES.md](MUTATION_PREREQUISITE_GUIDELINES.md), [../../../docs/WORKLOG.md](../../../docs/WORKLOG.md), and the temporary [Mycelial Lab Pending Decisions](../../../docs/MYCELIAL_LAB_PENDING_DECISIONS.md) approval queue.

## Lane Contract

Substrate Ecology rewards reading local board context. Its mechanics may modify an existing action or resolve a payoff around nutrients, open or compacted territory, dead matter, rival boundaries, board edges, or other explicit environmental conditions. Its value must be conditional on that context being present and inspectable by the player.

It does not own unconditional growth, extra baseline attempts, direction/range/movement capabilities, direct death resistance or reclamation, direct toxin offense, mutation-point generation, or temporary activation. Those remain Growth, Cellular Resilience, Fungicide, Genetic Drift, and Mycelial Surges respectively. Use the canonical [Growth versus Substrate Ecology ownership test](MUTATION_PREREQUISITE_GUIDELINES.md#growth-versus-substrate-ecology-ownership-test) before proposing a borderline mutation.

Ecology may qualify or boost a legal attempt, but it must not invent an illegal target, bypass occupation or board restrictions, or make context bonuses universal. The shared configurable Ecology cap is the lane-level safeguard against conditional bonuses becoming disguised raw Growth.

The lane is complete with six new mutations plus the Necrophytic Bloom move. Aerated Frontier replaced the rejected Substrate Sensing proposal, and Compaction Pressure fills the second Tier-2 specialization. No further Ecology mutations are planned without a new design decision.

## Final Tree

| Tier | Recommended name | Role | Proposed prerequisites | Initial scaling hypothesis |
|---|---|---|---|---|
| 1 | Aerated Frontier | Open-space root (implemented) | Root | +0.4 percentage points/level, 20 levels |
| 2 | Crustward Tropism | Edgeward branch | Aerated Frontier 10 | +0.75 percentage points/level, 5 levels |
| 2 | Compaction Pressure | Crowded-substrate branch (implemented) | Aerated Frontier 10 | +2 points/level, 5 levels |
| 3 | Detrital Enzymes | Dead-matter branch (implemented) | Crustward Tropism 1 **or** Compaction Pressure 1 | +1 point/level, 5 levels; +1 point at max beside dense dead matter |
| 3 | Toxin Margin | Enemy-toxin response branch (implemented) | Aerated Frontier 5, Homeostatic Harmony 5 | +1.5 points/level, 5 levels |
| 4 | Necrophytic Bloom | Existing compost mechanic (implemented) | Autolytic Surge 2, Detrital Enzymes 3, Adaptive Expression 3 | Existing values |
| 5 | Mycotoxin Fission | Friendly-toxin dispersal capstone (implemented) | Toxin Margin 5, Mycotoxin Potentiation 5, Putrefactive Mycotoxin 2 | +6 percentage points/level next to friendly toxins; up to three toxin splits/level |

### 1. Aerated Frontier (Implemented)

**Summary:** Growth attempts from living cells with at least two orthogonally adjacent open spaces gain a small bonus.

- Trigger/timing: per cardinal or Tendril growth attempt, before its success roll; qualification is evaluated from the source cell's current orthogonal neighbors.
- Scaling: +0.4 percentage points per level, 20 levels, included in the configurable Ecology combined cap.
- Limits: cells, toxins, nutrient patches, permanent blocks, and active chemobeacons are not open. Off-board positions are absent rather than counted as closed, so corner cells can qualify with their two available neighbors.
- AI: available as a normal Ecology root. Two Testing strategies begin with staged investment before branching into Growth or Cellular Resilience.
- Interaction/counterplay: occupation and environmental blockers suppress the bonus; new frontier cells naturally create new opportunities as the colony expands.
- Tracking: qualifying attempts and successes attributable specifically to the bonus are exported through Simulation.
- Focused tests: root registration; exact scaling; 1/2-open threshold; cells/nutrients/blocks excluded; corner qualification; deterministic bonus-only success attribution; AI goal ordering.
- Name shortlist checked before approval: **Aerated Frontier**, Porous Frontier, Open Hyphae, Sparse Branching, Frontier Aeration.

### 2. Crustward Tropism (Implemented)

**Summary:** Legal growth attempts that move closer to the playable crust gain a bonus, making outward routes more reliable without creating a universal growth increase.

- Trigger/timing: per legal cardinal or enabled Tendril diagonal growth attempt, before its success roll. The target must have a strictly lower shape-aware playable-edge distance than the source.
- Scaling: +0.75 percentage points per level, 5 levels, included in the shared Ecology growth-bonus cap.
- Max-level bonus: once per Growth Cycle, the first qualifying attempt that would place a new cell on the playable crust succeeds automatically. The per-player allowance resets at the next cycle and consumes the normal random roll to preserve deterministic RNG sequencing.
- Limits: blocks, toxins, occupied tiles, and off-board directions cannot become targets. A diagonal still requires its matching Tendril to grant a non-zero growth chance. A sideways edge attempt does not qualify.
- AI: the Ecology expansion testing strategy reaches Aerated Frontier 10, then maxes Crustward Tropism before its Growth investment.
- Interaction/counterplay: the bonus ends on routes that do not approach the crust; blocked or occupied outer routes deny it. Test against Perimeter Proliferator, whose source-near-crust multiplier can create a strong but sequential edge strategy.
- Tracking: qualifying attempts, bonus-attributable successes, and max-level automatic crust arrivals are exported through Simulation.
- Focused tests: registration/cost/prerequisite; strict distance reduction; no same-distance bonus; cardinal bonus attribution; one automatic arrival per player per cycle; reset on next cycle; enabled diagonal Tendril arrival.
- Name shortlist checked before approval: **Crustward Tropism**, Boundary Tropism, Edgeward Tropism, Marginward Growth, Peripheral Hyphae.

### 3. Compaction Pressure (Implemented)

**Summary:** Growth attempts from living cells with only one or two legal neighboring growth targets gain a bonus, helping colonies push out of congestion.

- Trigger/timing: per growth attempt after legal target calculation.
- Limits: zero legal targets never creates an attempt; the selected target must already be legal; included in the Ecology combined cap.
- AI: score constrained living cells that still have legal exits; avoid priority on completely sealed colonies.
- Interaction/counterplay: rivals can close the last exits; opening more space removes the bonus. It does not attack or replace occupants.
- Tracking: qualifying source counts by one/two exits, attempts, and bonus-attributable successes.
- Focused tests: 0/1/2/3 targets; irregular edges; resistant enemies/toxins/nutrients; no synthetic target creation; interaction with Aerated Frontier threshold.
- Name shortlist: **Compaction Pressure**, Crowding Response, Dense Escape, Contact Pressure, Thigmic Branching.

### 4. Detrital Enzymes (Implemented)

**Summary:** Growth into empty tiles adjacent to a dead non-toxin cell gains a bonus, using decay as a local catalyst without reclaiming the dead cell.

- Trigger/timing: per legal cardinal or enabled Tendril diagonal growth attempt before its success roll; the target must be orthogonally adjacent to at least one non-toxic dead cell from any owner.
- Scaling: +1 percentage point per level for five levels. At max level, a target beside two or more qualifying dead cells gains one additional percentage point; further dead cells do not stack.
- Prerequisite: either Crustward Tropism 1 or Compaction Pressure 1. Crustward's Aerated Frontier 10 prerequisite gives that route 11 cumulative local Ecology levels without requiring a cross-category toll.
- Limits: target remains empty/occupiable; toxin cells do not qualify; all bonuses remain under the shared Ecology cap.
- AI: available to normal Ecology fallback spending; a dedicated Detrital testing build remains a future balance decision.
- Interaction/counterplay: reclamation, composting, or occupation removes the catalyst; enemies can benefit from the same dead zone if positioned first.
- Tracking: qualifying attempts, bonus-attributable successes, dense-dead-matter attempts, and dense-bonus successes.
- Focused tests: definition/prerequisite/cost; orthogonal versus diagonal dead cells; toxin exclusion; normal and max-level scaling; dense-bonus attribution.
- Name shortlist: **Detrital Enzymes**, Saprotrophic Margin, Necrotic Catalysis, Detritus Foraging, Lytic Frontier.

### 5. Toxin Margin (Implemented)

**Summary:** Growth into empty tiles adjacent to enemy-owned toxins gains a bonus, helping a colony route around chemical blockades without consuming them.

- Trigger/timing: per legal cardinal or enabled Tendril diagonal growth attempt before its success roll; the empty target must be orthogonally adjacent to an enemy-owned toxin.
- Scaling: +1.5 percentage points per level for five levels, included in the shared Ecology cap.
- Prerequisite: Aerated Frontier 5 and Homeostatic Harmony 5. This intentionally makes it a different toxin answer from Mycotoxin Catabolism, which removes toxins.
- Limits: multiple toxins do not stack; toxin tiles remain blocked for normal growth; own toxins do not qualify.
- Tracking: qualifying attempts and bonus-attributable successes.
- Focused tests: approved definition/prerequisites; enemy versus own toxin; cardinal growth attribution; no synthetic target creation.

### 6. Mycotoxin Fission (Implemented)

**Summary:** Successful growth beside a friendly toxin disperses that toxin into new toxin pressure near enemy cells.

- Trigger/timing: each level adds +6 percentage points to growth into an empty cardinal or enabled Tendril-diagonal target orthogonally adjacent to an owned toxin. On success, the lowest-ID qualifying toxin vacates and creates up to three toxins near enemy cells per level.
- Limits: the launched toxins inherit only the source toxin's remaining lifespan. The fission target list excludes the vacated tile. A failed or unavailable toxin placement simply produces fewer splits.
- Max-level bonus: after the toxin vacates, the newly colonized cell automatically grows into that tile. This bonus growth cannot trigger another fission.
- AI: value friendly toxin adjacency plus reachable enemy-adjacent toxin targets; measure whether a purpose-built toxin build can access the capstone in practical games.
- Interaction/counterplay: opponents can deny enemy-adjacent landing sites, while the owner can deliberately place toxins to create the local growth setup.
- Tracking/persistence: toxins created and vacated-tile bridge growths; no deferred runtime state or save-snapshot field is required because the entire resolution is immediate.
- Focused tests: friendly versus enemy toxin qualification; per-level chance; multi-split limits; remaining-lifespan inheritance; max-level bridge; no bridge recursion; unavailable landing sites; deterministic RNG and Time-Lapse parity.

## Proposed Existing Mutation Move

**Necrophytic Bloom** moved from Genetic Drift to Substrate Ecology. Its primary mechanic converts dead-cell clusters into neutral environmental nutrient patches; randomness selects patch outcome, but it is not principally mutation economy or automatic progression.

Preserve mutation ID 18, owned levels, constants, description/effect, tracking, and save compatibility. Isolate these reviewable consequences:

- category factory/registration, Unity lane metadata, investment totals, and category-filtered reporting now use Ecology;
- Decay-end processing remains in `GeneticDriftMutationProcessor` so effect timing stays unchanged;
- approved prerequisites are Autolytic Surge 2, Detrital Enzymes 3, and Adaptive Expression 3;
- Mutation ID 18, levels, constants, effect behavior, tracking, and save compatibility are unchanged;
- measure how often generic AI can reach and benefit from composting before changing weights.

No other existing move is recommended now. Mycotoxin Catabolism remains Genetic Drift because its primary payoff is mutation points; Putrefactive Rejuvenation remains Fungicide because it is driven by toxin expiration; all direct reclamation remains Cellular Resilience.

## Deferred Concepts Outside This Feature

These candidates are unrelated future work. They are not part of the completed Substrate Ecology feature and require separate approval, design, and validation before implementation.

### Growth - Apical Dominance (Tier 4)

Newly grown cells receive a capped bonus on their first outward attempt in the same Growth Phase, reinforcing tip-led expansion without environmental qualification.

- Prerequisite direction: Mycelial Bloom 20, Mycotropic Induction 3.
- AI/tracking: frontier depth and bonus-attributable successes; generally useful Growth priority.
- Risks/tests: explosive chains, same-phase ordering, Tendrils, Creeping Mold, board edges, attempt cap.
- Name shortlist: **Apical Dominance**, Tip Dominance, Apical Momentum, Leading Hyphae, Polarized Branching.

### Cellular Resilience - Septal Isolation (Tier 4)

When a living cell dies, adjacent friendly living cells receive a capped reduction to their next death chance, representing sealed damage compartments.

- Prerequisite direction: Chronoresilient Cytoplasm 8, Regenerative Hyphae 2.
- AI/tracking: recent death density, protections created/consumed, prevented deaths.
- Risks/tests: simultaneous Decay ordering, stacking cap, toxin deaths, save/resume of pending protection.
- Name shortlist: **Septal Isolation**, Chitin Partitioning, Hyphal Quarantine, Septal Memory, Compartment Defense.

### Fungicide - Toxin Anastomosis (Tier 4)

Placing a toxin adjacent to an owned toxin has a chance to extend the older connected toxin's remaining lifetime by one round, capped per toxin.

- Prerequisite direction: Mycotoxin Potentiation 5, Putrefactive Mycotoxin 2.
- AI/tracking: connected toxin placements, extensions, area-denial value.
- Risks/tests: expiration ordering, connected clusters, repeated cap, enemy toxins, Necrotoxic Conversion and Putrefactive Rejuvenation.
- Name shortlist: **Toxin Anastomosis**, Venom Lattice, Toxic Bridging, Mycotoxin Network, Poison Junctions.

### Genetic Drift - Latent Polymorphism (Tier 4)

Banking mutation points earns a capped interest payout, turning delayed spending into a late-game Genetic Drift economy engine.

- Scaling: each of five levels grants 10% interest on the pre-interest banked amount, rounded down. Interest caps at 5 bonus mutation points per Mutation Phase and cannot compound within the same bank action.
- Prerequisites: Mutator Phenotype 7, Adaptive Expression 5, Anabolic Inversion 3.
- AI/tracking: existing deliberate AI bank actions receive the payout; track earned interest mutation points.
- Risks/tests: zero/low banking thresholds, rounding, max cap, interaction with Compound Reserve, human store flow, AI bank flow, and no same-phase compounding.
- Implementation: complete. No pending state or save-snapshot fields are needed because the payout resolves immediately when points are banked.

### Mycelial Surges - Saprotrophic Pulse (Tier 4 surge)

While active, successful growth into an Ecology-qualified dead-adjacent tile has a capped chance to reclaim one adjacent owned dead cell after growth resolves.

- Prerequisite direction: Necrosporulation 3, Detrital Enzymes 3; rising activation cost and short duration.
- AI/tracking: eligible dead-adjacent opportunities, activations, reclaims, wasted turns; CatchUp tag only if simulation supports it.
- Risks/tests: target ordering, no eligible dead cell, enemy dead exclusion, one reclaim/attempt cap, Catabolic Rebirth, active-surge persistence.
- Name shortlist: **Saprotrophic Pulse**, Compost Surge, Detrital Burst, Reclamation Pulse, Necrotic Flush.

## Final Roster Verdict

- Aerated Frontier now establishes the playable contextual-growth lane without owning unconditional expansion or direct rewards.
- Open, crowded, edgeward, dead-adjacent, enemy-toxin-adjacent, friendly-toxin-adjacent, and composting states are mutually legible and testable from board state.
- Necrophytic Bloom is the only recommended move; Regenerative Hyphae remains correctly in Cellular Resilience.
- Ecology bonuses use a shared configurable combined cap and deterministic attribution ordering established by Aerated Frontier.
- Nutrient Afterglow was intentionally not selected because its delayed budget would add persistence and AI complexity without being needed for the final lane.
- Septal Isolation and the other existing-lane candidates remain outside this feature and require separate save/resume design where applicable.
- Substrate Sensing was rejected because nutrient patches are sparse and consuming them makes the opportunity transient. Aerated Frontier replaced it as the implemented Tier-1 root.
- Compaction Pressure is the completed second Tier-2 specialization.

## Local Name Check

Checked 2026-08-17: all 60 shortlist names returned zero exact-phrase hits across Mutation, Mycovariant, and Adaptation C# source. Every candidate is 2-3 words and no more than 28 characters. The eight recommended Ecology names also have distinct first words within the proposed lane. External web uniqueness is not an authoring requirement; repository uniqueness and category clarity are the required gates.
