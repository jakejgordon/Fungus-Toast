# Substrate Ecology Roster Proposal

> Status: Aerated Frontier is implemented as the Tier-1 root. Every other entry remains a proposal and does not change gameplay by itself.
>
> Related: [../NEW_MUTATION_HELPER.md](../NEW_MUTATION_HELPER.md), [MUTATION_PREREQUISITE_GUIDELINES.md](MUTATION_PREREQUISITE_GUIDELINES.md), [../../../docs/WORKLOG.md](../../../docs/WORKLOG.md), and the temporary [Mycelial Lab Pending Decisions](../../../docs/MYCELIAL_LAB_PENDING_DECISIONS.md) approval queue.

## Lane Contract

Substrate Ecology rewards reading local board context. Its mechanics may modify growth around nutrients, open or compacted territory, dead matter, and rival boundaries. It does not own unconditional growth, direct death resistance/reclamation, direct toxin offense, mutation-point generation, or temporary activation.

The initial implementation target remains seven new mutations plus the proposed move of Necrophytic Bloom. Promoting Aerated Frontier to replace the rejected Substrate Sensing proposal leaves one Tier-2 open-substrate slot to redesign. Numeric values for unimplemented mutations remain starting hypotheses. Context bonuses share a configurable combined cap so stacking the lane cannot make growth automatic.

## Proposed Tree

| Tier | Recommended name | Role | Proposed prerequisites | Initial scaling hypothesis |
|---|---|---|---|---|
| 1 | Aerated Frontier | Open-space root (implemented) | Root | +0.5 percentage points/level, 20 levels |
| 2 | Open specialization TBD | Replacement branch | Aerated Frontier 10 | Mechanic and scaling require approval |
| 2 | Compaction Pressure | Crowded-substrate branch | Aerated Frontier 10 | +2 points/level, 5 levels |
| 3 | Detrital Enzymes | Dead-matter branch | Aerated Frontier 5, open specialization 3, Necrosporulation 3 | +3 points/level, 5 levels |
| 3 | Rival Rhizosphere | Contested-boundary branch | Aerated Frontier 5, Compaction Pressure 3, Mycotoxin Tracer 5 | +3 points/level, 5 levels |
| 4 | Nutrient Afterglow | Patch-consumption payoff | Aerated Frontier 15, open specialization 3, Adaptive Expression 3 | +4 points/level to a capped attempt budget, 5 levels |
| 4 | Necrophytic Bloom | Existing compost mechanic; proposed category move | Detrital Enzymes 3, Necrosporulation 5, Anabolic Inversion 3, Aerated Frontier 5 | Existing values; prerequisite rebalance isolated separately |
| 5 | Ecological Succession | Whole-lane capstone | Detrital Enzymes 5, Rival Rhizosphere 5, Nutrient Afterglow 3, Mycelial Bloom 15 | One retry/phase; +5 points/level, 3 levels |

### 1. Aerated Frontier (Implemented)

**Summary:** Growth attempts from living cells with at least two orthogonally adjacent open spaces gain a small bonus.

- Trigger/timing: per cardinal or Tendril growth attempt, before its success roll; qualification is evaluated from the source cell's current orthogonal neighbors.
- Scaling: +0.5 percentage points per level, 20 levels, included in the configurable Ecology combined cap.
- Limits: cells, toxins, nutrient patches, permanent blocks, and active chemobeacons are not open. Off-board positions are absent rather than counted as closed, so corner cells can qualify with their two available neighbors.
- AI: available as a normal Ecology root. Two Testing strategies begin with staged investment before branching into Growth or Cellular Resilience.
- Interaction/counterplay: occupation and environmental blockers suppress the bonus; new frontier cells naturally create new opportunities as the colony expands.
- Tracking: qualifying attempts and successes attributable specifically to the bonus are exported through Simulation.
- Focused tests: root registration; exact scaling; 1/2-open threshold; cells/nutrients/blocks excluded; corner qualification; deterministic bonus-only success attribution; AI goal ordering.
- Name shortlist checked before approval: **Aerated Frontier**, Porous Frontier, Open Hyphae, Sparse Branching, Frontier Aeration.

### 2. Open-Substrate Specialization (Replacement Needed)

The original Aerated Frontier Tier-2 proposal was promoted and reworked into the Tier-1 root. This slot needs a distinct mechanic that deepens open-substrate play without repeating the root bonus.

- Required before implementation: mechanic, name shortlist, scaling, AI valuation, counterplay, tracking, and focused tests.
- Intended prerequisite direction: Aerated Frontier 10.
- Compaction Pressure remains the proposed opposite Tier-2 specialization.

### 3. Compaction Pressure

**Summary:** Growth attempts from living cells with only one or two legal neighboring growth targets gain a bonus, helping colonies push out of congestion.

- Trigger/timing: per growth attempt after legal target calculation.
- Limits: zero legal targets never creates an attempt; the selected target must already be legal; included in the Ecology combined cap.
- AI: score constrained living cells that still have legal exits; avoid priority on completely sealed colonies.
- Interaction/counterplay: rivals can close the last exits; opening more space removes the bonus. It does not attack or replace occupants.
- Tracking: qualifying source counts by one/two exits, attempts, and bonus-attributable successes.
- Focused tests: 0/1/2/3 targets; irregular edges; resistant enemies/toxins/nutrients; no synthetic target creation; interaction with Aerated Frontier threshold.
- Name shortlist: **Compaction Pressure**, Crowding Response, Dense Escape, Contact Pressure, Thigmic Branching.

### 4. Detrital Enzymes

**Summary:** Growth into empty tiles adjacent to a dead non-toxin cell gains a bonus, using decay as a local catalyst without reclaiming the dead cell.

- Trigger/timing: per growth attempt before success roll; any owner's dead cell may qualify.
- Limits: target remains empty/occupiable; toxin cells do not qualify; multiple dead neighbors do not multiply the bonus.
- AI: score reachable qualifying targets, with higher utility when dead-cell density is high.
- Interaction/counterplay: reclamation, composting, or occupation removes the catalyst; enemies can benefit from the same dead zone if positioned first.
- Tracking: qualifying attempts by dead-cell owner, successes, and dead catalysts later removed.
- Focused tests: own/enemy dead; toxin/non-toxin; multiple neighbors; reclaimed/converted dead cells; target occupancy; Ecology cap.
- Name shortlist: **Detrital Enzymes**, Saprotrophic Margin, Necrotic Catalysis, Detritus Foraging, Lytic Frontier.

### 5. Rival Rhizosphere

**Summary:** Growth into empty tiles adjacent to an enemy living cell gains a bonus, rewarding contested borders without damaging or overgrowing the enemy.

- Trigger/timing: per growth attempt before success roll.
- Limits: only an empty/occupiable target qualifies; enemy dead/toxin cells do not; resistant adjacency may qualify but is never attacked.
- AI: score reachable contested empty tiles and rival diversity; lower utility when isolated.
- Interaction/counterplay: opponents can fill, toxify, or retreat from the boundary. Fungicide remains the direct-offense lane.
- Tracking: qualifying attempts/successes by opposing player and contested tiles claimed.
- Focused tests: friendly/enemy living; resistant adjacency; multiple rivals; enemy dead/toxin; target becomes occupied; no takeover event.
- Name shortlist: **Rival Rhizosphere**, Contested Margin, Boundary Foraging, Competitive Sensing, Interfacial Hyphae.

### 6. Nutrient Afterglow

**Summary:** Consuming a nutrient patch primes a limited number of context-qualified growth attempts in the next Growth Phase.

- Trigger/timing: nutrient-consumption event records a capped attempt budget derived from consumed cluster size; the budget becomes active for the owning player's next Growth Phase and then expires.
- Limits: does not change patch rewards, grant points/drafts, or apply to unconditional attempts; one stored budget with an explicit cap, not unlimited stacking.
- AI: value available patches plus current Ecology-qualified target density; account for delayed payoff.
- Interaction/counterplay: rivals can consume patches first; denying contextual targets wastes the budget.
- Tracking/persistence: patch type/size, budget created/used/expired, qualified successes; include pending budget in runtime save snapshot if saves can occur before consumption and payoff are resolved.
- Focused tests: each patch type; cluster sizes above cap; multiple consumptions; no qualifying attempts; phase expiry; save/resume; AI delayed utility.
- Name shortlist: **Nutrient Afterglow**, Resource Afterglow, Trophic Memory, Patch Priming, Digestive Momentum.

### 7. Ecological Succession

**Summary:** Once per Growth Phase, the first failed attempt that qualified for an Ecology context gets one immediate retry.

- Trigger/timing: after the failed roll; retry the same still-legal source/target once with the original chance plus capstone bonus.
- Limits: one retry per player per phase; no recursive retry; revalidate legality; the retry cannot trigger a second growth attempt or bypass board restrictions.
- AI: value breadth of unlocked Ecology contexts and current qualifying attempt density.
- Interaction/counterplay: the opponent can invalidate the tile before a later phase, but not between the immediate roll and retry; the once-per-phase ceiling keeps the effect legible.
- Tracking: qualifying failure, retry chance, retry result, context(s), and unused phase entitlement.
- Focused tests: first/second failure; multi-context attempt; target legality; deterministic RNG order; no recursion; Time-Lapse parity; save boundary; interaction with Creeping Mold failed-growth handling (explicit ordering required).
- Name shortlist: **Ecological Succession**, Successional Burst, Contextual Plasticity, Habitat Integration, Adaptive Substrate.

## Proposed Existing Mutation Move

Move **Necrophytic Bloom** from Genetic Drift to Substrate Ecology when the Core category is introduced. Its primary mechanic converts dead-cell clusters into neutral environmental nutrient patches; randomness selects patch outcome, but it is not principally mutation economy or automatic progression.

Preserve mutation ID 18, owned levels, constants, description/effect, tracking, and save compatibility. Isolate these reviewable consequences:

- update category factory/registration, Unity lane metadata, investment totals, Simulation sort order, AI category intent, and repository assertions;
- decide whether to move processing ownership out of `GeneticDriftMutationProcessor` without changing Decay-end timing;
- rebalance prerequisites separately because the current Tier-4 gate is below the documented range;
- update Hyperadaptive Drift's prerequisite edge by ID only; its behavior need not change;
- measure how often generic AI can reach and benefit from composting before changing weights.

No other existing move is recommended now. Mycotoxin Catabolism remains Genetic Drift because its primary payoff is mutation points; Putrefactive Rejuvenation remains Fungicide because it is driven by toxin expiration; all direct reclamation remains Cellular Resilience.

## One Addition to Each Existing Lane

These five candidates bring the planned total to about 45 logical mutations. They are design inputs for later batches, not part of the first Substrate Ecology implementation.

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

Banking mutation points gives the first eligible non-surge mutation purchased next turn a capped chance to receive one extra free level in a different Tier-1 mutation.

- Prerequisite direction: Mutator Phenotype 5, Adaptive Expression 3.
- AI/tracking: bank decision utility, trigger/target/failure reason, free level.
- Risks/tests: no valid target, automatic upgrades, cost order, banking zero points, save/resume, Ontogenic Regression.
- Name shortlist: **Latent Polymorphism**, Banked Variation, Dormant Alleles, Stored Plasticity, Genetic Reserve.

### Mycelial Surges - Saprotrophic Pulse (Tier 4 surge)

While active, successful growth into an Ecology-qualified dead-adjacent tile has a capped chance to reclaim one adjacent owned dead cell after growth resolves.

- Prerequisite direction: Necrosporulation 3, Detrital Enzymes 3; rising activation cost and short duration.
- AI/tracking: eligible dead-adjacent opportunities, activations, reclaims, wasted turns; CatchUp tag only if simulation supports it.
- Risks/tests: target ordering, no eligible dead cell, enemy dead exclusion, one reclaim/attempt cap, Catabolic Rebirth, active-surge persistence.
- Name shortlist: **Saprotrophic Pulse**, Compost Surge, Detrital Burst, Reclamation Pulse, Necrotic Flush.

## Whole-Tree Review Verdict

- Aerated Frontier now establishes the playable contextual-growth lane without owning unconditional expansion or direct rewards.
- Open, crowded, nutrient-adjacent, dead-adjacent, and rival-adjacent states are mutually legible and testable from board state.
- Necrophytic Bloom is the only recommended move; Regenerative Hyphae remains correctly in Cellular Resilience.
- Ecology bonuses use a shared configurable combined cap and deterministic attribution ordering established by Aerated Frontier.
- Creeping Mold versus Ecological Succession failure ordering must be decided in Core tests before the capstone ships.
- Nutrient Afterglow and Septal Isolation introduce pending runtime state and therefore require explicit save/resume design; they should not be in the first low-risk implementation batch.
- Substrate Sensing was rejected because nutrient patches are sparse and consuming them makes the opportunity transient. Aerated Frontier replaced it as the implemented Tier-1 root.
- A distinct Tier-2 open-substrate specialization is required to restore the planned seven-new-mutation roster.

## Local Name Check

Checked 2026-08-17: all 60 shortlist names returned zero exact-phrase hits across Mutation, Mycovariant, and Adaptation C# source. Every candidate is 2-3 words and no more than 28 characters. The eight recommended Ecology names also have distinct first words within the proposed lane. External web uniqueness is not an authoring requirement; repository uniqueness and category clarity are the required gates.
