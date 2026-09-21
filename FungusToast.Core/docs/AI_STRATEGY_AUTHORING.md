# AI Strategy Authoring

See also: [README.md](README.md) for the full documentation hierarchy, [SIMULATION_HELPER.md](SIMULATION_HELPER.md) for simulation workflows, and [GAME_BALANCE_CONSTANTS.md](GAME_BALANCE_CONSTANTS.md) for balance-lever guidance.

This document explains how AI mutation spending strategies are configured, selected, and validated for simulation runs.

## Strategy Sets

Strategies are currently grouped into the following sets in `FungusToast.Core/AI/AIRoster.cs`:

- `Proven`: stable baseline roster for general use.
- `Testing`: expanded roster for balance experiments and tuning.
- `Campaign`: simple named variants (`AI1`, `AI2`, etc.) for campaign integration.
- `Mycovariants`: focused permutations for mycovariant studies.

## Stable analytical identity

Every registered strategy now has an additive machine ID and a deterministic
definition fingerprint. Existing `StrategyName` values remain compatibility
aliases and player-facing names remain separate catalog metadata.

- Existing roster IDs use `legacy.<set>.<strategy-name-slug>.v1` and remain
  stable while the compatibility name is retained.
- The definition fingerprint covers the behavior-bearing parameterized
  configuration: ordered goals/categories/surges, surge timing, economy policy,
  mycovariant preferences, exclusions, tier policy, and starting offset.
- `StrategyIdentity.DefinitionSchemaVersion` is the implementation contract. It
  must be incremented whenever strategy behavior changes in a way not represented
  by those fields.
- Analytics joins on strategy ID plus definition fingerprint. Names, themes,
  lifecycle, and display metadata are labels and never analytical keys.

## Strategy Status

Each strategy profile now carries a status tag in `AIRoster`:

- `Testing`: experimental or tuning-oriented strategies.
- `Proven`: baseline strategies considered stable enough for broader comparison.
- `Loser`: reserved for future demotion/blacklist workflows when a strategy should stay cataloged but be treated as a poor baseline.

Current defaulting is roster-based (`Proven`/`Campaign` => `Proven`, `Testing`/`Mycovariants` => `Testing`) with per-strategy overrides available in `ExplicitStrategyStatusesByName`.

## Selection Policies

Simulation commands can choose strategy sampling behavior via:

- `RandomUnique`: random unique picks from the selected set.
- `CoverageBalanced`: tries to cover distinct strategy themes first, then fills remaining slots.
- `StratifiedCycle`: deterministic, theme-ordered cycle that shifts by index.

CLI flag:

```bash
dotnet run -- --strategy-set Testing --selection-policy CoverageBalanced --games 200 --players 8 --no-keyboard
```

## Testing Strategy Catalog

The `Testing` set includes a themed roster intended for robust balance analysis:

| Strategy Name | Theme | Intent |
|---|---|---|
| `TST_HyperEconomyRamp` | EconomyRamp | Front-load economy and scale into high-tier pressure |
| `TST_EarlyReclaimerSwarm` | Reclamation | Retake territory and recover board losses quickly |
| `TST_ToxinSiege` | Offense | Build toxin pressure and force attrition |
| `TST_HyphalSurgeTempo` | SurgeTempo | Leverage timed surge windows for burst turns |
| `TST_FortressResilience` | Defense | Stabilize with durable infrastructure before push |
| `TST_OpportunisticCounterplay` | Counterplay | Flexible path that adapts to opponent plans |
| `TST_Tier3PlateauSpecialist` | TierCap | Maximize efficiency in lower/mid tiers |
| `TST_LateGameSpike` | LateGameSpike | Bank and convert into late high-impact upgrades |
| `TST_BalancedGeneralistControl` | Control | Broad and steady progression across game phases |
| `TST_BalancedControl_NoPreferredMyco` | Control | Control baseline without explicit mycovariant preferences |
| `TST_RebirthAttrition` | Attrition | Win through repeated death/rebirth loops |
| `TST_BalancedControl_MaxEconomy` | Control | Control path with stronger economy weighting |
| `TST_LowTierEconomyGrinder` | TierCap | Prioritize Tier1/2 economy-resilience first, then escalate |
| `TST_LowTierSurgeSkirmisher` | Counterplay | Low-tier fungicide/genetic skirmisher with anti-leader surge |
| `TST_BalancedControl_MinorEconomy` | Control | Control variant with lighter economy bias |
| `TST_CampaignMirror_AI7_Hyphal` | SurgeTempo | Testing mirror of campaign hyphal surge/vectoring line |
| `TST_CampaignMirror_AI12_BalancedControl_AnabolicFirst` | Control | Testing mirror of campaign AI12 progression |
| `TST_CampaignMirror_AI13_BalancedControl_MaxEconomy` | Control | Testing mirror of campaign AI13 progression |
| `TST_EcologyFrontierExpansion` | Control | Start with Aerated Frontier, then convert open-substrate tempo into Growth |
| `TST_EcologyFrontierResilience` | Defense | Start with Aerated Frontier, then stabilize the expanded frontier through Cellular Resilience |

## Canonical 8-Player Archetype Harness

For ongoing 8-player balance tuning, the `Testing` roster now includes a fixed archetype harness:

- `TST_Arch01_GrowthResilience`
- `TST_Arch02_ResilienceGrowth`
- `TST_Arch03_FungicideSurge`
- `TST_Arch04_DriftGrowth`
- `TST_Arch05_DriftResilience`
- `TST_Arch06_SurgeGrowth`
- `TST_Arch07_DriftFungicide`
- `TST_Arch08_SurgeResilience`

Use these via explicit `--strategy-names` when you want a stable 8-player comparison harness that does not drift as the broader Testing roster evolves.

## ParameterizedSpendingStrategy Precedence

Every `IMutationSpendingStrategy` implementation must explicitly implement
`SelectMycovariantFromChoices`. `MycovariantDraftManager` dispatches through
that interface and does not infer behavior from the concrete strategy type.
`RandomMutationSpendingStrategy` intentionally drafts randomly; parameterized
strategies apply their authored preferences and scoring. New implementations
must make this choice deliberately.

`StrategyRegistry` stores every roster member as one immutable
`StrategyDefinition`: strategy implementation, stable ID, definition
fingerprint, and catalog metadata travel together. Runtime consumers and
simulation artifacts read that record rather than joining name-keyed metadata
maps. The remaining bootstrap override declarations are guarded: duplicate
keys and names that do not resolve to any registered strategy stop roster
initialization. Keep existing strategy names as compatibility aliases; do not
rename them to create machine identity.

Roster selection also requires enough registered definitions for every
requested seat. It never synthesizes numbered `LegacyRandom` strategies; add
an intentional registered definition or reduce the player count instead.

For `ParameterizedSpendingStrategy`, authoring intent is easiest to understand if you think in four layers:

1. **Build order**
   - `targetMutationGoals`
   - This is the main spine of the AI. Goals are processed in list order, one active goal at a time, including prerequisites.
2. **Surge plan**
   - `surgePriorityIds`
   - `surgeAttemptTurnFrequency`
   - Planned surges are the ones the AI will fire ahead of fallback spending. Every activation is still gated by `SurgeOpportunityEvaluator`, which estimates what the surge is worth on the current board (see below), so a planned surge only fires when the board rewards it.
3. **Fallback personality**
   - `priorityMutationCategories`
   - `prioritizeHighTier`
   - `economyBias`
   - These mainly shape spending only when the AI is not currently able to make meaningful progress on its explicit goal chain.
4. **Experiment exclusions**
   - `excludedMutationIds`
   - Removes named mutations from every normal spending candidate set. Use this for controlled simulation variants where fallback spending must not erase the treatment/control distinction.

Actual spending flow, simplified:

1. Early-game economy mutation pass (`MutatorPhenotype`, `AdaptiveExpression`, `HyperadaptiveDrift`)
2. Work through `targetMutationGoals` in order (a surge bought as a prerequisite bypasses the opportunity gate: it is bought for the unlock)
3. Try planned surges: on `surgeAttemptTurnFrequency` rounds any rewarded surge qualifies; off-cadence only a strong opportunity does. The best-scoring surge fires, not the first listed.
4. Try catch-up surge if behind
5. Bank for a near-term planned surge that is unlocked, nearly affordable, and currently rewarded
6. Fallback spending (never touches surges unless `MycelialSurges` is explicitly in `priorityMutationCategories`):
   - preferred categories first
   - then any upgradable mutation
   - then economy-biased random weighting
7. Last-resort surge pass with leftover points, again over planned surges only. An unplanned surge is never bought outside a prerequisite purchase: leftover points belong to the goal chain

## Surge Opportunity Evaluation

A surge's max level is its activation budget and every activation raises the next price, so
`SurgeOpportunityEvaluator` (`FungusToast.Core/AI/SurgeOpportunityEvaluator.cs`) estimates each
activation's payoff in cells before any spending path commits to it. The estimate mirrors the
processor it models:

| Surge | Value estimate |
|---|---|
| Autolytic Surge | Extra growth over every open orthogonal target minus the decay penalty on every unprotected living cell. Dead-cell payoffs (Necrosporulation, Regenerative Hyphae, Necrophytic Bloom, Detrital Enzymes) discount the loss. |
| Chitin Fortification | Cells fortified over the window, worth more while cells touch enemy living cells or toxins; declined until the colony can absorb `AiChitinMinimumFullRoundsOfCapacity` rounds of fortification. |
| Necrotic Clearance | Expected corpses cleared, weighting contested corpses (next to enemy living cells) far above uncontested ones. |
| Chemotactic Beacon | Placements the best marker actually delivers, plus enemy cells, toxins, and nutrient tiles on its line. |
| Mimetic Resilience | Expected resistant placements, which scale with the eligible rivals' resistant cell counts; zero when there is nothing to copy. |
| Competitive Antagonism | Toxin output it can redirect (Mycotoxin Tracer and Sporicidal Bloom levels) while a larger rival exists. |

The value must clear `max(AiSurgeMinimumAbsoluteValue, cost * AiSurgeMinimumValuePerMutationPoint)`;
off-cadence planned activations need `AiSurgeOffScheduleValueMultiplier` times that. Windows are
clipped to the rounds left before the round cap. All weights live in `GameBalance` under the
`AiSurge*` / `AiAutolytic*` / `AiChitin*` / `AiNecrotic*` / `AiBeacon*` / `AiMimetic*` /
`AiAntagonism*` constants. `players.parquet` exports `AiSurgeOpportunitiesDeclined`, the number of
times a planned, affordable, unlocked surge was left unfired.

Practical interpretation of the main knobs:

- `targetMutationGoals`
  - Primary build order. Strongest authoring control.
- `surgeAttemptTurnFrequency`
  - Cadence for planned surge activations. Off-cadence rounds still fire a planned surge when the board makes it a strong opportunity.
- `priorityMutationCategories`
  - Category preference during fallback spending.
- `prioritizeHighTier`
  - Within a candidate set, try higher-tier prerequisite-backed options first.
- `economyBias`
  - **Fallback-only** weighting toward `GeneticDrift` mutations. This is not a full global economy strategy dial.
- `excludedMutationIds`
  - An experimental hard exclusion, including fallback spending. A mutation cannot be both targeted and excluded; the constructor rejects that configuration.

## Goal-Level Authoring Rules

- Omit `TargetLevel` only when the intent is to max out a mutation.
- Use `new TargetMutationGoal(MutationIds.X, 1)` when the intent is a single pickup rather than a max target.
- For repeated appearances of the same mutation in a goal list, use explicit ascending targets to represent staged revisits. Example: level 1 early, then level 2 later.
- Prefer `GameBalance.*MaxLevel` constants when writing explicit max targets for readability in long archetype chains.

## Surge Coherence Audit (Built-In)

`AIRoster` runs a startup audit (`AuditSurgeBackboneSynergy`) for `Testing` strategies.

- It checks surge-prioritizing strategies for at least one non-surge backbone category in goals.
- It compares that backbone against `MutationSynergyCatalog` suggestions.
- It emits warnings to `CoreLogger` when surge goals and backbone categories are incoherent.

Authoring implication:

- If you add `surgePriorityIds`, also include supporting non-surge goals in Growth/Resilience/Fungicide/GeneticDrift as appropriate.

## Player-Facing Strategy Profile Contract

Every new player-facing strategy, and every existing non-Testing strategy, must
have a concise four-field profile. The roster exposes the first two fields
through its explicit presentation metadata: `FriendlyName` is the **Name** and
`AIPlayerIntentions` is the one-sentence **Fantasy**. Testing-only controls may
retain technical names while they are being evaluated. This is a design card for
authors and players, not a release dossier or a substitute for measured balance
evidence.

| Element | Requirement |
|---|---|
| **Name** | A short player-facing identity. Existing stable IDs and measured-band evidence remain elsewhere. |
| **Fantasy** | One sentence describing the colony's biological or ecological character. |
| **Mutation plan** | The ordered mutation backbone in gameplay terms: which capabilities it establishes first and what it develops later. |
| **Mycovariant plan** | A curated, ordered list of intended Mycovariants. Whole-category preferences alone are insufficient because they delegate too much of the draft to `AIScore`. |

Active-ability use, Adaptations, visible tells, counterplay, context, and
evidence belong in the implementation and evaluation records when needed; they
are not required to write the profile. Do not describe ordinary passive
upgrades as attacks, bursts, or blitzes.

The profile comes before a generated candidate plan. It supplies the named job
and the mutation/Mycovariant palette that make the plan a designed experiment
rather than a parameter sweep.

`StrategyCatalogEntry` derives **Mutation plan** from the ordered
`targetMutationGoals`, and **Mycovariant plan** from the executable preference
records. It names category-derived preference sets explicitly as equal-score
sets rather than misrepresenting them as an ordered draft. These derived fields
are the canonical roster summary: do not duplicate the lists in a separate
profile document.

## Content-to-Profile Coverage Review

Adding a mutation or Mycovariant must trigger an automated **coverage review**
of the active strategy profiles. Its purpose is to find builds whose stated
Mutation Plan or Mycovariant Plan suggests that the new content belongs in the
build; it is not permission to silently alter a player-facing strategy.

The review should use explicit authoring metadata on the new content (for
example, capability and interaction tags) and the profile-derived strategy
metadata. It should emit an auditable candidate list with one disposition per
plausible match:

- **Add for evaluation** — create a specifically reviewed Testing candidate or
  patch for the strategy.
- **Consider later** — retain the match and its reason without changing a
  build.
- **Not applicable** — record why the apparent thematic match does not fit.

The tool must report missing or stale profiles as well as matches. A reviewed
decision is required for every candidate before the new content's authoring
work is complete. Only an explicit, separately reviewed strategy change may
update executable mutation goals or Mycovariant preference order; normal
strategy validation and evidence gates still apply.

Keep the profile representation derived from executable strategy configuration
where possible. The coverage report is a maintenance aid and must not create a
second, independently maintained ordered build list in documentation.

### Required up-front profile decision

Before implementing a new AI, write its four profile fields: **Name**,
**Fantasy**, **Ordered Mutation Plan**, and **Ordered Mycovariant Preferences**.
Player-facing and Campaign strategies must name specific Mycovariants in draft
order. A category-derived set is useful for a broad Testing control, but it is
not a finished player-facing preference plan and must not be promoted as one.

Before implementing a new Mutation or Mycovariant, record its capability tags,
important interactions, exclusions, and the existing non-Testing profiles that
appear to match. Give every match one of the dispositions above. "Add for
evaluation" means a matched Testing candidate or patch plus simulation
evidence; it does not authorize changing the live Campaign or Proven strategy.
This decision is part of initial scoping, not a cleanup step after the content
is implemented.

## Authoring Checklist

1. Define Name, Fantasy, Ordered Mutation Plan, and Ordered Mycovariant Preferences before implementation.
2. Add a uniquely named strategy in the appropriate roster list.
3. Add/adjust theme mapping in `ExplicitStrategyThemesByName` when needed.
4. Keep mutation-goal chains coherent: early economy, mid stabilization, late finish.
5. For every player-facing or Campaign AI, author a specific ordered Mycovariant list. Reserve category-derived sets for broad Testing controls.
6. Build `FungusToast.Core` and `FungusToast.Simulation` after any roster changes.
7. Run at least one seeded smoke simulation and verify strategy names/statuses appear in exported metadata.
8. For comparison runs, prefer a fixed `--seed`, record the `--selection-policy`, and keep the manifest's selected lineup with the results.
9. For canonical balance experiments, prefer explicit `--strategy-names` instead of sampled rosters so roster composition does not drift with future roster edits.
10. Explicit strategy-name experiments are single-roster by design: all names must come from the chosen `--strategy-set`, so do not mix `Proven`/`Testing`/`Campaign`/`Mycovariants` names in one run.
11. If a design doc says a mutation name without `Max` or `Level N`, treat that as ambiguous and resolve it before implementation; for the current archetype harness, unlabeled steps were encoded as one upgrade.
12. When introducing a mutation or Mycovariant, run the content-to-profile
    coverage review, record a disposition for each candidate, and implement
    only explicitly approved strategy changes.
13. Before promoting a Testing AI or changing a Campaign/Proven preference
    plan, compare the proposed ordered list against its unchanged control with
    deterministic simulation seeds, then confirm it on a held-out seed.

## Campaign Mycovariant Migration Ledger

Legacy Campaign entries with empty or category-derived plans are frozen by
`Campaign_player_facing_mycovariant_authoring_debt_does_not_expand`. Each name
leaves that baseline only after a specific list clears a matched comparison and
a held-out confirmation.

### The Economancer (`CMP_Economy_Economancer_Elite`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager. The list prioritizes deterministic mutation-point payout and
  excludes AI bait cards.
- Context: eight players, 120x120, rotating slots, Mycovariants enabled,
  nutrients and starting Adaptations disabled. The other seven strategies were
  the fixed `TST_Arch02` through `TST_Arch08` panel.
- Comparison seed `2026091902`, 50 pairs: treatment-control normalized board
  share `-0.005873`, 95% CI `[-0.028119, 0.016373]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `economancer_myco_comparison_{control,treatment}_2026091902`.
- Held-out seed `2026091903`, 100 pairs: treatment-control normalized board
  share `+0.007541`, 95% CI `[-0.007239, 0.022321]`; the same margin was
  supported. Artifacts: `economancer_myco_holdout_{control,treatment}_2026091903`.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing control/treatment pair remains registered
  so the comparison is reproducible.

### Hoardspore Regent (`CMP_Economy_HoardsporeRegent_Elite`)

- Promoted order: Plasmid Bounty III, Reclamation Rhizomorphs, Necrophoric
  Adaptation, Plasmid Bounty II, Ascus Wager, Plasmid Bounty I. This preserves
  an immediate economy opener, then makes the strategy's reclamation fantasy
  explicit before its secondary economy fallbacks; AI bait cards are excluded.
- Context: the same eight-player, 120x120, rotating-slot, fixed `TST_Arch02`
  through `TST_Arch08` panel used for The Economancer, with Mycovariants on and
  nutrients and starting Adaptations off.
- Comparison seed `2026091912`, 50 pairs: treatment-control normalized board
  share `+0.017703`, 95% CI `[-0.044011, 0.079417]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `hoardspore_myco_comparison_{control,treatment}_2026091912`.
- Held-out seed `2026091913`, 100 pairs: treatment-control normalized board
  share `-0.005289`, 95% CI `[-0.021197, 0.010618]`; the same margin was
  supported. Artifacts: `hoardspore_myco_holdout_{control,treatment}_2026091913`.
- Both stages passed manifest contamination checks. The Testing pair remains
  registered for reproducibility.

### Iron Shell (`CMP_Defense_IronShell_Elite`)

- Promoted order: Reclamation Rhizomorphs, Necrophoric Adaptation, Plasmid
  Bounty III, Plasmid Bounty II, Ascus Wager, Plasmid Bounty I. The two
  attrition/reclamation passives lead its defensive fantasy; safe economy
  payoffs follow, and AI bait cards are excluded.
- Context: the same fixed eight-player 120x120 panel and systems controls used
  by the preceding migrations.
- Comparison seed `2026091922`, 50 pairs: treatment-control normalized board
  share `+0.032433`, 95% CI `[-0.034393, 0.099259]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `ironshell_myco_comparison_{control,treatment}_2026091922`.
- Held-out seed `2026091923`, 100 pairs: treatment-control normalized board
  share `+0.013053`, 95% CI `[-0.025559, 0.051665]`; the same margin was
  supported. Artifacts: `ironshell_myco_holdout_{control,treatment}_2026091923`.
- Both stages passed manifest contamination checks. The Testing pair remains
  registered for reproducibility.

### AI4 / Creeping Reclaimer (`AI4`) — migration in progress

- Candidate 1 order: Reclamation Rhizomorphs, Necrophoric Adaptation, Corner
  Conduit III, Corner Conduit II, Corner Conduit I, Perimeter Proliferator,
  Hyphal Draw.
- Comparison seed `2026091932`, 50 pairs: treatment-control normalized board
  share `-0.049997`, 95% CI `[-0.163269, 0.063274]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts:
  `ai4_myco_comparison_{control,treatment}_2026091932`.
- Candidate 1 was rejected at the comparison gate. No holdout was run and the
  Campaign strategy was not changed.
- AI4-specific draft evidence showed that the broad list displaced stronger
  scorer-selected choices. Candidate 2 therefore limits authored preferences
  to the fantasy's two signature reclamation cards—Necrophoric Adaptation,
  then Reclamation Rhizomorphs—and leaves all other drafts to normal AI
  scoring. Its Testing treatment is
  `TST_Campaign_AI4_CuratedMycovariantsV2`.
- Candidate 2 comparison seed `2026091952`, 50 pairs: treatment-control
  normalized board share `+0.003065`, 95% CI `[-0.071748, 0.077879]`; the
  lower bound again did not support the `-0.05` non-inferiority margin.
  Artifacts: `ai4_myco_v2_comparison_{control,treatment}_2026091952`.
- Candidate 2 was also rejected at the comparison gate. No holdout was run and
  AI4 remains unchanged. Its near-zero point estimate but wide interval is
  insufficient promotion evidence, not evidence of material harm.

### The Necrotoxin Gauntlet (`CMP_Bloom_NecrotoxinGauntlet_Elite`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager. This replaces the broad Economy-category selector with the
  strongest deterministic mutation-point payouts before its late Tier 5
  fallback, while excluding the three AI-only bait cards.
- Context: the same fixed eight-player, 120x120, rotating-slot panel used by
  the preceding migrations, with Mycovariants on and nutrients and starting
  Adaptations off.
- Comparison seed `2026091972`, 50 pairs: treatment-control normalized board
  share `0.000000`, 95% CI `[0.000000, 0.000000]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `necrotoxin_myco_comparison_{control,treatment}_2026091972`.
- Held-out seed `2026091973`, 100 pairs: treatment-control normalized board
  share `0.000000`, 95% CI `[0.000000, 0.000000]`; the same margin was
  supported. Artifacts:
  `necrotoxin_myco_holdout_{control,treatment}_2026091973`.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. Exact paired parity confirms that the explicit order
  preserves the former category-control behavior for these samples. The
  Testing pair remains registered for reproducibility.

### Harvest Broker (`CMP_Economy_KillReclaim_Medium`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager. The deterministic payout ladder supports its value-building
  fantasy and excludes the three AI-only bait cards.
- Context: the same fixed eight-player, 120x120, rotating-slot panel used by
  the preceding migrations, with Mycovariants on and nutrients and starting
  Adaptations off.
- Comparison seed `2026091992`, 50 pairs: treatment-control normalized board
  share `+0.023827`, 95% CI `[-0.022874, 0.070527]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `harvestbroker_myco_comparison_{control,treatment}_2026091992`.
- Held-out seed `2026091993`, 100 pairs: treatment-control normalized board
  share `+0.011017`, 95% CI `[-0.021961, 0.043996]`; the same margin was
  supported. Artifacts:
  `harvestbroker_myco_holdout_{control,treatment}_2026091993`.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Creeping Regression (`CMP_Bloom_CreepingRegression_Elite`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager. The deterministic payout ladder funds its expensive regression
  engine and excludes the three AI-only bait cards.
- Context: the same fixed eight-player, 120x120, rotating-slot panel used by
  the preceding migrations, with Mycovariants on and nutrients and starting
  Adaptations off.
- Comparison seed `2026092012`, 50 pairs: treatment-control normalized board
  share `+0.006273`, 95% CI `[-0.006022, 0.018567]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `creepingregression_myco_comparison_{control,treatment}_2026092012`.
- Held-out seed `2026092013`, 100 pairs: treatment-control normalized board
  share `0.000000`, 95% CI `[0.000000, 0.000000]`; the same margin was
  supported. Artifacts:
  `creepingregression_myco_holdout_{control,treatment}_2026092013`.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Voltaic Rot (`CMP_Bloom_AnabolicRegression_Medium`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager. The deterministic payout ladder funds its anabolic-to-decay
  engine and excludes the three AI-only bait cards.
- Context: the same fixed eight-player, 120x120, rotating-slot panel used by
  the preceding migrations, with Mycovariants on and nutrients and starting
  Adaptations off.
- Comparison seed `2026092032`, 50 pairs: treatment-control normalized board
  share `+0.000453`, 95% CI `[-0.000435, 0.001341]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `voltaicrot_myco_comparison_{control,treatment}_2026092032`.
- Held-out seed `2026092033`, 100 pairs: treatment-control normalized board
  share `+0.001303`, 95% CI `[-0.002226, 0.004832]`; the same margin was
  supported. Artifacts:
  `voltaicrot_myco_holdout_{control,treatment}_2026092033`.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Harvest Broker, offset 1 (`TST_Campaign7_KillReclaim_Offset1`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager. This keeps the same authored value ladder as Harvest Broker
  while preserving its distinct one-tile starting-spore offset.
- Context: the fixed eight-player, 120x120, rotating-slot panel used by the
  preceding migrations, with Mycovariants on and nutrients and starting
  Adaptations off.
- Comparison seed `2026092052`, 50 pairs: treatment-control normalized board
  share `+0.011774`, 95% CI `[-0.021304, 0.044852]`; preregistered
  non-inferiority margin `-0.05`, supported. Artifacts:
  `harvestbroker_offset1_myco_comparison_{control,treatment}_2026092052`.
- Held-out seed `2026092053`, 100 pairs: treatment-control normalized board
  share `0.000000`, 95% CI `[0.000000, 0.000000]`; the same margin was
  supported. Artifacts:
  `harvestbroker_offset1_myco_holdout_{control,treatment}_2026092053`.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Harvest Broker, offset 2 (`TST_Campaign7_KillReclaim_Offset2`) — rejected

- Candidate order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager.
- Comparison seed `2026092072`, 50 pairs: treatment-control normalized board
  share `0.000000`, 95% CI `[0.000000, 0.000000]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092073`, 100 pairs: treatment-control normalized board
  share `-0.019344`, 95% CI `[-0.051656, 0.012967]`; the lower bound narrowly
  missed the same margin, so the candidate was rejected and the Campaign
  strategy remains unchanged. Artifacts use
  `harvestbroker_offset2_myco_{comparison,holdout}_{control,treatment}` with
  their respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility; the held-out result must not be tuned against.

### Harvest Broker, offset 3 (`TST_Campaign7_KillReclaim_Offset3`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager, preserving the distinct three-tile starting-spore offset.
- Comparison seed `2026092092`, 50 pairs: treatment-control normalized board
  share `+0.009303`, 95% CI `[-0.008931, 0.027538]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092093`, 100 pairs: treatment-control normalized board
  share `+0.002040`, 95% CI `[-0.005455, 0.009534]`; the same margin was
  supported. Artifacts use
  `harvestbroker_offset3_myco_{comparison,holdout}_{control,treatment}` with
  their respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

## Recommended Simulation Pattern

Use batch mode with deterministic seeds and coverage-balanced selection for statistical relevance:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- \
  --games 100 \
  --player-counts 4,8 \
  --board-sizes 120x120,160x160 \
  --strategy-sets Testing \
  --selection-policy CoverageBalanced \
  --seed 12345 \
  --no-keyboard
```

Canonical 8-player archetype run:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- \
  --games 100 \
  --players 8 \
  --strategy-set Testing \
  --strategy-names TST_Arch01_GrowthResilience,TST_Arch02_ResilienceGrowth,TST_Arch03_FungicideSurge,TST_Arch04_DriftGrowth,TST_Arch05_DriftResilience,TST_Arch06_SurgeGrowth,TST_Arch07_DriftFungicide,TST_Arch08_SurgeResilience \
  --seed 12345 \
  --rotate-slots \
  --experiment-id testing_archetype_harness \
  --no-keyboard
```
