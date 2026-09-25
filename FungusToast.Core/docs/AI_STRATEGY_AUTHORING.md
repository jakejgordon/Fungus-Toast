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

### Harvest Broker, offset 8 (`TST_Campaign7_KillReclaim_Offset8`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager, preserving the distinct eight-tile starting-spore offset.
- Comparison seed `2026092112`, 50 pairs: treatment-control normalized board
  share `+0.012222`, 95% CI `[-0.015757, 0.040201]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092113`, 100 pairs: treatment-control normalized board
  share `+0.024827`, 95% CI `[-0.010251, 0.059905]`; the same margin was
  supported. Artifacts use
  `harvestbroker_offset8_myco_{comparison,holdout}_{control,treatment}` with
  their respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Resilient Mycelium, offset 1 (`TST_Training_ResilientMycelium_Offset1`)

- Promoted order: Hyphal Resistance Transfer, Septal Alarm, Mycelial Bastion
  III, Mycelial Bastion II, Mycelial Bastion I. Persistent resistance spread
  and death-triggered hardening reinforce its Chitin Fortification plan before
  the immediate resistant-cell fallbacks.
- Comparison seed `2026092142`, 50 pairs: treatment-control normalized board
  share `+0.048156`, 95% CI `[-0.015318, 0.111630]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092143`, 100 pairs: treatment-control normalized board
  share `+0.012385`, 95% CI `[-0.027588, 0.052357]`; the same margin was
  supported. Artifacts use
  `resilientmycelium_offset1_myco_{comparison,holdout}_{control,treatment}`
  with their respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Overextender, offset 1 (`CMP_Mobility_Overextender_Training_Offset1`)

- Promoted order: Hyphal Draw, Aggressotropic Conduit III, Corner Conduit III,
  Perimeter Proliferator, Aggressotropic Conduit II. The list deliberately
  expresses forward relocation and recurring projection while rewarding the
  one-tile edge offset.
- Comparison seed `2026092172`, 50 pairs: treatment-control normalized board
  share `+0.012282`, 95% CI `[-0.017728, 0.042293]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092173`, 100 pairs: treatment-control normalized board
  share `+0.003127`, 95% CI `[-0.021306, 0.027560]`; the same margin was
  supported. Artifacts use
  `overextender_offset1_myco_{comparison,holdout}_{control,treatment}` with
  their respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Toxic Turtle, offset 1 (`CMP_Attrition_ToxicTurtle_Training_Offset1`)

- Promoted order: Enduring Toxaphores, Hyphal Resistance Transfer, Septal
  Alarm, Chemotactic Mycotoxins, Ballistospore Discharge III. The order keeps
  the slow toxin field alive, hardens the colony through long trades, then
  improves toxin placement and supplies an immediate field fallback.
- Comparison seed `2026092202`, 50 pairs: treatment-control normalized board
  share `+0.024777`, 95% CI `[0.009149, 0.040404]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092203`, 100 pairs: treatment-control normalized board
  share `+0.021767`, 95% CI `[0.011087, 0.032447]`; the same margin was
  supported. Artifacts use
  `toxicturtle_offset1_myco_{comparison,holdout}_{control,treatment}` with
  their respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Resilient Shell (`CMP_Defense_ResilientShell_Easy`) — rejected

- Candidate order: Hyphal Resistance Transfer, Septal Alarm, Mycelial Bastion
  III, Mycelial Bastion II, Mycelial Bastion I. This reused the fortified-cell
  sequence promoted for Resilient Mycelium, but tested it independently with
  Resilient Shell's high-tier and minor-economy behavior intact.
- Comparison seed `2026092242`, 50 pairs: treatment-control normalized board
  share `-0.044802`, 95% CI `[-0.118860, 0.029257]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `resilientshell_myco_comparison_{control,treatment}_2026092242`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  Both comparison arms had zero simulation invariant mismatches; the Testing
  pair remains registered for reproducibility.

### Grave Bastion (`CMP_Defense_ReclaimShell_Easy`)

- Promoted order: Reclamation Rhizomorphs, Necrophoric Adaptation, Hyphal
  Resistance Transfer, Septal Alarm, Mycelial Bastion III. Recurring and
  loss-triggered reclamation lead the rebuild plan; persistent and
  loss-triggered resistance protect the recovered ground before the largest
  immediate bastion fallback.
- Comparison seed `2026092282`, 50 pairs: treatment-control normalized board
  share `+0.033033`, 95% CI `[-0.022931, 0.088996]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092283`, 100 pairs: treatment-control normalized board
  share `+0.015793`, 95% CI `[-0.024985, 0.056570]`; the same margin was
  supported. Artifacts use
  `reclaimshell_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Scavenger Court (`CMP_Reclaim_Scavenger_Easy`)

- Promoted order: Reclamation Rhizomorphs, Necrophoric Adaptation, Plasmid
  Bounty III, Plasmid Bounty II, Plasmid Bounty I. Recurring and
  loss-triggered reclamation lead the fantasy, followed by deterministic
  mutation-point payouts that respect its Tier 4 cap.
- Comparison seed `2026092312`, 50 pairs: treatment-control normalized board
  share `+0.009146`, 95% CI `[-0.032623, 0.050915]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092313`, 100 pairs: treatment-control normalized board
  share `-0.010485`, 95% CI `[-0.031378, 0.010408]`; the same margin was
  supported. Artifacts use
  `scavenger_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Needle Reclaimer (`CMP_Reclaim_InfiltrationSurge_Easy`)

- Promoted order: Reclamation Rhizomorphs, Necrophoric Adaptation, Hyphal
  Draw, Aggressotropic Conduit III, Aggressotropic Conduit II. Reclamation
  consolidates weak-seam footholds first; directed pull and recurring enemy-
  facing projection then express the surge half of the strategy.
- Comparison seed `2026092352`, 50 pairs: treatment-control normalized board
  share `+0.016298`, 95% CI `[-0.044665, 0.077261]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092353`, 100 pairs: treatment-control normalized board
  share `+0.009714`, 95% CI `[-0.036407, 0.055835]`; the same margin was
  supported. Artifacts use
  `infiltrationsurge_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Tempo Harvester (`CMP_Economy_TempoReclaim_Medium`)

- Promoted order: Plasmid Bounty III, Reclamation Rhizomorphs, Necrophoric
  Adaptation, Plasmid Bounty II, Plasmid Bounty I, Ascus Wager. The largest
  immediate payout starts the tempo engine; recurring and loss-triggered
  reclamation convert that tempo into board presence before smaller economy
  fallbacks.
- Comparison seed `2026092392`, 50 pairs: treatment-control normalized board
  share `-0.010586`, 95% CI `[-0.026581, 0.005409]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092393`, 100 pairs: treatment-control normalized board
  share `+0.000864`, 95% CI `[-0.021518, 0.023247]`; the same margin was
  supported. Artifacts use
  `temporeclaim_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Pulsar Sprout (`CMP_Surge_Pulsar_Easy`)

- Promoted order: Hyphal Resistance Transfer, Aggressotropic Conduit III,
  Septal Alarm, Hyphal Draw, Mycelial Bastion III. Persistent resistance
  spreads after its growth bursts; recurring enemy-facing projection and
  loss-triggered hardening help the exposed surge hold ground before the
  one-time positioning and reinforcement fallbacks.
- Comparison seed `2026092422`, 50 pairs: treatment-control normalized board
  share `-0.002939`, 95% CI `[-0.040927, 0.035049]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092423`, 100 pairs: treatment-control normalized board
  share `+0.005724`, 95% CI `[-0.016361, 0.027809]`; the same margin was
  supported. Artifacts use
  `pulsar_myco_{comparison,holdout}_{control,treatment}` with their respective
  seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Resilient Canopy (`CMP_TierCap_GrowthResilience_Easy`)

- Promoted order: Hyphal Resistance Transfer, Septal Alarm, Perimeter
  Proliferator, Mycelial Bastion III, Aggressotropic Conduit III. Persistent
  and loss-triggered resistance reinforce its simple growth-and-resilience
  toolkit; crust growth and immediate or recurring fortified expansion provide
  complementary fallbacks.
- Comparison seed `2026092432`, 50 pairs: treatment-control normalized board
  share `+0.044713`, 95% CI `[+0.008011, +0.081415]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092433`, 100 pairs: treatment-control normalized board
  share `+0.049778`, 95% CI `[+0.020180, +0.079375]`; the same margin was
  supported. Artifacts use
  `resilientcanopy_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### The Economancer (`CMP_Economy_LateSpike_Hard`)

- Promoted order: Plasmid Bounty III, Plasmid Bounty II, Plasmid Bounty I,
  Ascus Wager. Immediate mutation-point payouts fund the max-economy plan in
  descending order; the free Tier 5 level remains a late fallback once the
  stockpile engine is established.
- Comparison seed `2026092442`, 50 pairs: treatment-control normalized board
  share `+0.060493`, 95% CI `[+0.003701, +0.117286]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092443`, 100 pairs: treatment-control normalized board
  share `+0.114086`, 95% CI `[+0.054407, +0.173766]`; the same margin was
  supported. Artifacts use
  `latespike_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Pressure Bloom (`CMP_Growth_Pressure_Medium`)

- Promoted order: Aggressotropic Conduit III, Hyphal Draw, Perimeter
  Proliferator, Plasmid Bounty III, Corner Conduit III. Recurring projection
  toward enemy biomass leads the plan; immediate repositioning and crust-lane
  expansion sustain outward pressure before economy and corner projection
  fallbacks.
- Comparison seed `2026092452`, 50 pairs: treatment-control normalized board
  share `+0.005614`, 95% CI `[-0.043183, +0.054412]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092453`, 100 pairs: treatment-control normalized board
  share `+0.001245`, 95% CI `[-0.036197, +0.038688]`; the same margin was
  supported. Artifacts use
  `growthpressure_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Putrid Tendrils (`CMP_Growth_PutridTendrils_Medium`) — rejected

- Candidate order: Aggressotropic Conduit III, Hyphal Draw, Necrophoric
  Adaptation, Reclamation Rhizomorphs, Perimeter Proliferator. Enemy-facing
  projection and forward biomass movement led the tendril plan; loss-triggered
  and repeat reclamation supported its late rebirth engine before the crust
  growth fallback.
- Comparison seed `2026092462`, 50 pairs: treatment-control normalized board
  share `+0.026835`, 95% CI `[-0.069383, +0.123053]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `putridtendrils_myco_comparison_{control,treatment}_2026092462`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  Both comparison arms had zero simulation invariant mismatches; the Testing
  pair remains registered for reproducibility.

### Wildfire Bloom (`CMP_Growth_WildfireBloom_Medium`)

- Promoted order: Aggressotropic Conduit III, Perimeter Proliferator, Hyphal
  Resistance Transfer, Septal Alarm, Hyphal Draw. Recurring enemy-facing and
  crust expansion lead the plan; persistent and loss-triggered resistance help
  its wide lanes survive before the one-time forward-movement fallback.
- Comparison seed `2026092472`, 50 pairs: treatment-control normalized board
  share `+0.058225`, 95% CI `[+0.007895, +0.108556]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092473`, 100 pairs: treatment-control normalized board
  share `+0.088428`, 95% CI `[+0.055696, +0.121160]`; the same margin was
  supported. Artifacts use
  `wildfirebloom_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Beacon Sprinter (`CMP_Surge_BeaconSprinter_Medium`) — rejected

- Candidate order: Hyphal Draw, Aggressotropic Conduit III, Ballistospore
  Discharge III, Plasmid Bounty III, Corner Conduit III. Immediate forward
  movement led the tempo plan, followed by recurring projection, one-shot
  reinforcement, surge funding, and an alternate route.
- Comparison seed `2026092482`, 50 pairs: treatment-control normalized board
  share `-0.177084`, 95% CI `[-0.394998, +0.040830]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `beaconsprinter_myco_comparison_{control,treatment}_2026092482`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  Both comparison arms had zero simulation invariant mismatches; the Testing
  pair remains registered for reproducibility.

### Beacon Tempo (`CMP_Surge_BeaconTempo_Medium`) — rejected

- Candidate order: Plasmid Bounty III, Aggressotropic Conduit III, Hyphal
  Draw, Septal Alarm, Corner Conduit III. The immediate mutation-point payout
  led the more patient tempo plan, followed by recurring enemy projection,
  forward repositioning, loss-triggered resistance, and an alternate route.
- Comparison seed `2026092492`, 50 pairs: treatment-control normalized board
  share `-0.042978`, 95% CI `[-0.142379, +0.056423]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `beacontempo_myco_comparison_{control,treatment}_2026092492`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  Both comparison arms had zero simulation invariant mismatches; the Testing
  pair remains registered for reproducibility.

### Growth Tempo (`CMP_Surge_GrowthTempo_Medium`) — rejected

- Candidate order: Perimeter Proliferator, Aggressotropic Conduit III, Plasmid
  Bounty III, Hyphal Draw, Ballistospore Discharge III. Recurring crust growth
  and enemy-facing projection established the board presence for later tempo
  bursts; immediate funding, repositioning, and reinforcement were the
  fallbacks.
- Comparison seed `2026092502`, 50 pairs: treatment-control normalized board
  share `-0.026239`, 95% CI `[-0.076934, +0.024457]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `growthtempo_myco_comparison_{control,treatment}_2026092502`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  Both comparison arms had zero simulation invariant mismatches; the Testing
  pair remains registered for reproducibility.

### Rebirth Conductor (`CMP_Control_AnabolicRebirth_Medium`) — rejected

- Candidate order: Necrophoric Adaptation, Reclamation Rhizomorphs. Deaths
  would seed reclaimed cells for the rebirth loop, then recurring reclamation
  would sustain repeated Catabolic Rebirth and Putrefactive Rejuvenation.
- Comparison seed `2026092512`, 50 pairs: treatment-control normalized board
  share `-0.118289`, 95% CI `[-0.233667, -0.002910]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `anabolicrebirth_myco_comparison_{control,treatment}_2026092512`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  Both comparison arms had zero simulation invariant mismatches; the Testing
  pair remains registered for reproducibility.

### Rebirth Furnace (`CMP_Control_RebirthFurnace_Medium`) — rejected

- Candidate order: Septal Alarm, Hyphal Resistance Transfer, Necrophoric
  Adaptation, Reclamation Rhizomorphs, Aggressotropic Conduit III. Death- and
  resistance-triggered effects led the furnace plan, followed by reclamation
  support and recurring outward pressure for rebuilt territory.
- Comparison seed `2026092522`, 50 pairs: treatment-control normalized board
  share `+0.063075`, 95% CI `[-0.168080, +0.294230]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `rebirthfurnace_myco_comparison_{control,treatment}_2026092522`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  Both comparison arms had zero simulation invariant mismatches; the Testing
  pair remains registered for reproducibility.

### Voltaic Bloom (`AI12`)

- Promoted order: Necrophoric Adaptation, Reclamation Rhizomorphs,
  Aggressotropic Conduit III, Perimeter Proliferator, Hyphal Draw. Reclamation
  supports the Catabolic Rebirth finish, recurring projection and crust growth
  extend the Creeping Mold base, and forward movement remains the fallback.
- Comparison seed `2026092532`, 50 pairs: treatment-control normalized board
  share `+0.133719`, 95% CI `[-0.008831, +0.276268]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092533`, 100 pairs: treatment-control normalized board
  share `+0.118048`, 95% CI `[-0.017135, +0.253230]`; the same margin was
  supported. Artifacts use
  `voltaicbloom_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
- Both stages passed manifest contamination checks and had zero simulation
  invariant mismatches. The Testing pair remains registered for
  reproducibility.

### Legacy Hoardspore Regent (`AI13`) — rejected

- Candidate order: Plasmid Bounty III, Reclamation Rhizomorphs, Necrophoric
  Adaptation, Plasmid Bounty II, Ascus Wager, Plasmid Bounty I. Immediate
  economy accelerates the max-economy control engine, reclamation sustains its
  full lifecycle, and the remaining payouts support its late-game stockpile.
- Comparison seed `2026092542`, 50 pairs: treatment-control normalized board
  share `+0.063230`, 95% CI `[-0.199399, +0.325859]`; the lower bound did not
  support the preregistered `-0.05` non-inferiority margin. Artifacts use
  `legacyhoardspore_myco_comparison_{control,treatment}_2026092542`.
- The candidate was rejected at the comparison gate. No holdout was run, the
  Campaign strategy remains unchanged, and its migration-debt entry remains.
  The positive point estimate with a wide interval is insufficient promotion
  evidence, not evidence of harm. Both comparison arms had zero simulation
  invariant mismatches; the Testing pair remains registered for
  reproducibility.

### Rejuvenation Engine (`AI5`)

- Promoted order: Plasmid Bounty III, Necrophoric Adaptation, Reclamation
  Rhizomorphs, Ascus Wager. Immediate economy develops the deep mutation
  engine, death-triggered and repeat reclamation sustain renewal, and the free
  Tier 5 level advances the late rebirth plan.
- Comparison seed `2026092552`, 50 pairs: treatment-control normalized board
  share `+0.004464`, 95% CI `[-0.022552, +0.031481]`; the `-0.05`
  non-inferiority margin was supported.
- Held-out seed `2026092553`, 100 pairs: treatment-control normalized board
  share `+0.009237`, 95% CI `[-0.028806, +0.047280]`; the same margin was
  supported. Artifacts use
  `rejuvenationengine_myco_{comparison,holdout}_{control,treatment}` with their
  respective seeds.
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
