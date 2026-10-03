# AI Content Tag Vocabulary — Proposal

Status: **Proposed 2026-10-03, awaiting Jake's approval.** Nothing here is
implemented. This is step 1 of the *Proposed content-to-profile coverage
review* in `docs/WORKLOG.md`: the compact compatibility metadata that every
mutation and Mycovariant carries, so a coverage report can explain which
strategies a new piece of content plausibly belongs in. The decision-record
format (*Add for evaluation* / *Consider later* / *Not applicable*) is a
separate follow-up proposal.

## What the tags are for

- **For:** authoring-time review. The report reads the tags on new or changed
  content plus a profile derived from each strategy's executable plan, and
  lists plausible matches with a one-line reason each.
- **Not for:** runtime AI behavior, draft scoring, or player-facing copy. No
  tag changes how any AI plays, and the report never edits a strategy.
- **Not a second build list.** Strategy plans stay in `ParameterizedAIStrategy`
  configuration; the strategy side of the match is derived from it.

## Design principles

1. **Mechanics, not categories.** `MutationCategory` and `MycovariantCategory`
   already exist and are too broad to match on (Fungicide alone holds ten
   Mycovariants). Tags say what a piece of content does on the board.
2. **Explainable matches.** Every match must read as a sentence: "Detrital
   Enzymes *needs* dead cells, which this plan's Autolytic Surge *creates*."
   That is why the relations below are directional.
3. **Small and closed.** Tags are C# enums, not free text, so a typo is a
   compile error and the report can be tested. Adding a tag is a reviewed
   change to this vocabulary.
4. **Derive what is already known.** Activation form (passive, surge, one-time,
   targeted), tier, prerequisites, and Bait status are already on the
   definitions; they are not re-tagged.

## The model

Each mutation and non-Bait Mycovariant gets one content profile:

| Field | Meaning | Rule |
|---|---|---|
| **Capabilities** | The jobs it does. | Required, 1–3. |
| **Amplifies** | Capabilities it makes stronger when the colony already has them. | Optional. |
| **Needs** | Board conditions it depends on to do anything useful. | Optional. |
| **Creates** | Board conditions it produces as a main effect or side effect. | Optional. |
| **Removes** | Board conditions it destroys as a side effect or counter-effect. | Optional; only where the removal is large enough to undercut a plan. |

Matching then works on four kinds of reasons, each tied to one relation:

- **Same job:** content shares a capability with the plan's leading capabilities.
- **Amplifier:** content amplifies a capability the plan already has.
- **Feeds / fed by:** content needs a condition the plan creates, or creates one
  the plan needs.
- **Tension:** content removes a condition the plan needs, or the plan removes
  one the content needs. Tension is reported, not hidden, so a reviewer can
  record *Not applicable* with the reason.

## Vocabulary

### Capabilities (20)

| Group | Tag | Definition |
|---|---|---|
| Territory | `BaseGrowth` | Raises ordinary growth chance with no board condition. |
| | `DiagonalGrowth` | Adds or strengthens diagonal (Tendril) growth. |
| | `ConditionalGrowth` | Growth bonus gated on a local board condition; the condition goes in **Needs**. |
| | `RemotePlacement` | Places living cells away from the colony's growing edge (lines, runners, jumps). |
| | `Repositioning` | Moves existing cells rather than adding new ones. |
| Survival | `DecayResistance` | Keeps own living cells alive through decay or age. |
| | `ResistantCells` | Makes own cells Resistant. |
| | `ToxinCleanup` | Clears toxins near own cells. |
| Death and corpses | `SelfReclamation` | Turns own dead cells back into living cells. |
| | `CorpseCapture` | Turns enemy dead cells or kills into own living cells. |
| | `CorpseDenial` | Removes own dead cells before enemies can reclaim them. |
| | `Composting` | Turns dead cells into nutrient patches. |
| Offense | `ToxinPlacement` | Puts new toxins on the board. |
| | `ToxinLongevity` | Makes own toxins last longer. |
| | `ToxinMobility` | Moves own toxins toward enemies. |
| | `DirectKill` | Kills enemy living cells. |
| | `LeaderFocus` | Aims its effect at the strongest colony. |
| Economy | `PointIncome` | Grants mutation points. |
| | `FreeUpgrades` | Grants mutation levels without spending points. |
| | `TreePivot` | Reshapes the colony's own mutation tree. |

### Board conditions (16), used by Needs / Creates / Removes

| Tag | Meaning | Who can create it |
|---|---|---|
| `OwnCellDeaths` | Own living cells dying (the event). | Own plan |
| `OwnDeadCells` | Own dead cells on the board (the corpses). | Own plan |
| `EnemyDeadCells` | Enemy dead cells on the board. | Own plan or opponents |
| `EnemyKills` | Own effects killing enemy cells. Creating it also counts as creating `EnemyDeadCells`. | Own plan |
| `OwnToxins` | Own toxins on the board. | Own plan |
| `EnemyToxins` | Enemy toxins near own cells. | Opponents |
| `OwnResistantCells` | Own Resistant cells. | Own plan |
| `EnemyResistantCells` | Enemy Resistant cells. | Opponents |
| `EnemyContact` | Long borders with living enemy cells. | Board and opponents |
| `OpenSpace` | Room to branch around own cells. | Board |
| `CrampedSpace` | Own cells with few open sides. | Board and opponents |
| `BoardEdge` | Own territory near the crust or corners. | Board |
| `LargeColony` | Effect scales with own colony size. | Own plan |
| `FallingBehind` | Own colony trails the leaders. | Game state |
| `BankedPoints` | Unspent mutation points carried between rounds. | Own plan |
| `NutrientPatches` | Neutral nutrient patches on the board. | Own plan or board |

Conditions a strategy cannot create itself (board, opponents, game state) are
still useful. Two pieces of content that need the same environment, such as
Aerated Frontier and Chemotactic Beacon both needing `OpenSpace`, are reported
as sharing a situation, at lower weight than a feeds/fed-by match.

## Proposed tags for current content

Blank cells mean none. Tier families share one profile.

### Mutations (42)

| Mutation | Capabilities | Amplifies | Needs | Creates | Removes |
|---|---|---|---|---|---|
| Mycelial Bloom | BaseGrowth | | | OwnCellDeaths | |
| Tendril ×4 | DiagonalGrowth | | | | |
| Mycotropic Induction | DiagonalGrowth | DiagonalGrowth | | | |
| Creeping Mold | Repositioning | | | | |
| Filament Overdrive | DiagonalGrowth, RemotePlacement | DiagonalGrowth | OpenSpace | OwnCellDeaths, OwnDeadCells | |
| Homeostatic Harmony | DecayResistance | | | | |
| Chronoresilient Cytoplasm | DecayResistance | | | | |
| Regenerative Hyphae | SelfReclamation | | OwnDeadCells | | |
| Necrosporulation | RemotePlacement | | OwnCellDeaths | | |
| Necrohyphal Infiltration | CorpseCapture | | EnemyDeadCells, EnemyContact | | |
| Catabolic Rebirth | SelfReclamation | | OwnDeadCells, OwnToxins | | |
| Hypersystemic Regeneration | SelfReclamation, ResistantCells | SelfReclamation | OwnDeadCells | OwnResistantCells | |
| Mycotoxin Tracer | ToxinPlacement | | EnemyContact | OwnToxins | |
| Mycotoxin Potentiation | ToxinLongevity, DirectKill | ToxinPlacement | OwnToxins | EnemyKills | |
| Putrefactive Mycotoxin | DirectKill | | EnemyContact | EnemyKills | |
| Sporicidal Bloom | ToxinPlacement | | LargeColony | OwnToxins | |
| Necrotoxic Conversion | CorpseCapture | | EnemyKills, OwnToxins | | |
| Putrefactive Rejuvenation | DecayResistance | | EnemyKills | | |
| Putrefactive Cascade | DirectKill | DirectKill | EnemyContact | EnemyKills | |
| Mutator Phenotype | FreeUpgrades | | | | |
| Adaptive Expression | PointIncome | | | | |
| Mycotoxin Catabolism | ToxinCleanup, PointIncome | | EnemyToxins | | OwnToxins, EnemyToxins |
| Anabolic Inversion | PointIncome | | FallingBehind | | |
| Latent Polymorphism | PointIncome | | BankedPoints | | |
| Hyperadaptive Drift | FreeUpgrades | FreeUpgrades | | | |
| Ontogenic Regression | TreePivot | | | | |
| Autolytic Surge | BaseGrowth | | | OwnCellDeaths, OwnDeadCells | |
| Necrotic Clearance | CorpseDenial | | OwnDeadCells | | OwnDeadCells |
| Chemotactic Beacon | RemotePlacement | | OpenSpace | | |
| Mimetic Resilience | ResistantCells, LeaderFocus | | EnemyResistantCells, FallingBehind | OwnResistantCells | |
| Competitive Antagonism | LeaderFocus | ToxinPlacement | FallingBehind | | |
| Chitin Fortification | ResistantCells | | | OwnResistantCells | |
| Aerated Frontier | ConditionalGrowth | | OpenSpace | | |
| Crustward Tropism | ConditionalGrowth | | BoardEdge | | |
| Compaction Pressure | ConditionalGrowth | | CrampedSpace | | |
| Detrital Enzymes | ConditionalGrowth | | OwnDeadCells, EnemyDeadCells | | |
| Toxin Margin | ConditionalGrowth | | EnemyToxins | | |
| Necrophytic Bloom | Composting | | OwnDeadCells, EnemyDeadCells | NutrientPatches | |
| Toxinborne Seeding | ConditionalGrowth, ToxinMobility, RemotePlacement | | OwnToxins, EnemyContact | | |

### Mycovariants (31 tagged; the four Bait cards are excluded)

| Mycovariant | Capabilities | Amplifies | Needs | Creates | Removes |
|---|---|---|---|---|---|
| Plasmid Bounty I–III | PointIncome | | | | |
| Ascus Wager | FreeUpgrades | | | | |
| Neutralizing Mantle | ToxinCleanup | | EnemyToxins | | EnemyToxins |
| Enduring Toxaphores | ToxinLongevity | ToxinPlacement | OwnToxins | | |
| Ballistospore Discharge I–III | ToxinPlacement | | | OwnToxins | |
| Cytolytic Burst | DirectKill | | OwnToxins, EnemyContact | EnemyKills | |
| Chemotactic Mycotoxins | ToxinMobility | ToxinPlacement | OwnToxins | | |
| Jetting Mycelium I–III | RemotePlacement, ToxinPlacement | | | OwnToxins | |
| Perimeter Proliferator | ConditionalGrowth | | BoardEdge | | |
| Corner Conduit I–III | RemotePlacement | | BoardEdge | | |
| Aggressotropic Conduit I–III | RemotePlacement, LeaderFocus, ResistantCells | | | OwnResistantCells | |
| Hyphal Draw | Repositioning, LeaderFocus | | | | |
| Necrophoric Adaptation | SelfReclamation | | OwnCellDeaths, OwnDeadCells | | |
| Reclamation Rhizomorphs | SelfReclamation | SelfReclamation | OwnDeadCells | | |
| Mycelial Bastion I–III | ResistantCells | | | OwnResistantCells | |
| Surgical Inoculation | ResistantCells, RemotePlacement | | | OwnResistantCells | |
| Hyphal Resistance Transfer | ResistantCells | ResistantCells | OwnResistantCells | OwnResistantCells | |
| Septal Alarm | ResistantCells | | OwnCellDeaths | OwnResistantCells | |
| Septal Seal | ResistantCells | | | OwnResistantCells | |

Every one of the 20 capabilities and 16 conditions is used by at least one
current item, so nothing in the vocabulary is speculative.

## Check against the five strategy briefs

| Brief | What the tags let the report see |
|---|---|
| Steady Growth, Iron Shell | `BaseGrowth` + `ResistantCells` backbone; every Resistance Mycovariant matches on *same job*, and Hyphal Resistance Transfer additionally as an *amplifier*. |
| Murderous Necrosporulation | Necrosporulation *needs* `OwnCellDeaths`, which Autolytic Surge and Mycelial Bloom *create*; toxin content matches on `ToxinPlacement`. |
| Substrate Spore Drop | Toxinborne Seeding *needs* `OwnToxins`, which Sporicidal Bloom and Jetting Mycelium *create*. Mycotoxin Catabolism is flagged as **tension** because it removes `OwnToxins`. |
| Death and Reclamation | Filament Overdrive *creates* `OwnDeadCells`, which Regenerative Hyphae and the reclamation Mycovariants *need*. Necrotic Clearance is flagged as **tension**. |
| Economy at All Costs | Matches on `PointIncome` / `FreeUpgrades` / `TreePivot`, so Plasmid Bounty and Ascus Wager surface from outside Genetic Drift. Mycotoxin Catabolism matches for its point income but is flagged as **tension** with any toxin plan it would share a colony with. |

The "avoid category-only matches" requirement holds in the other direction too.
Toxin Margin is Substrate Ecology, but it only matches plans that share its need
for `EnemyToxins`, not every plan that buys Substrate Ecology.

## How the strategy side is derived (preview)

This is not part of the approval, but it shows the vocabulary is enough.
A strategy's plan profile is the union of the profiles of its ordered
`TargetMutationGoal`s, its surge priorities, and its explicitly ordered
Mycovariant preferences, weighted toward the front of each order.
Category-derived preference sets (`CategoryDerivedMycovariantIds`) contribute at
low weight, because they express almost no intent. A plan's *leading
capabilities* are its top few by weight. No profile is stored separately.

## Where the tags live

Proposed: on the definitions, next to the content they describe. Mutations
would get an optional `profile:` constructor argument, like the existing
`aiTags:`. Mycovariants would get an init property, with tier families sharing
one static profile. A Core test fails when a non-Bait mutation or Mycovariant
has no capability, when a Bait card has a profile, or when a relation lists a
condition that is not in the vocabulary. Authors see the requirement when they
add content, and `NEW_MUTATION_HELPER.md` / `MYCOVARIANT_HELPER.md` gain a short
"tag it" step.

The alternative is a central catalog file keyed by id, like
`MutationSynergyCatalog`. It is easier to review in one place, but it is easier
to forget when adding content.

## Relationship to existing tag-like data

| Existing | Purpose | Proposal |
|---|---|---|
| `MutationAITags.CatchUp` | Runtime: lets the AI fire catch-up surges. | Keep. A test can assert every `CatchUp` surge **Needs** `FallingBehind`; both current ones (Mimetic Resilience, Competitive Antagonism) do. |
| `MutationSynergyCatalog` | Authoring hint: surge backbone categories, plus one buff pair (Chemotactic Beacon ← Putrefactive Mycotoxin), used by the roster coherence check. | Keep until the report exists, then decide whether the coherence check should read profiles instead. |
| `MycovariantBase.SynergyWith` | Runtime: AI draft-score bonus. | Keep; unrelated to authoring review. |
| Draft-card tags (Passive, One-time, Bait) | Player-facing copy. | Unrelated. |

## Decisions needed from Jake

1. **Is this the right size?** There are 20 capabilities and 16 conditions. A
   smaller set would merge things such as `ToxinLongevity` into
   `ToxinPlacement`, and `CorpseDenial` and `Composting` into one tag, which
   makes the reasons vaguer.
2. **Keep `Amplifies` as its own relation?** The alternative is to fold it into
   Capabilities. That is simpler, but loses the "makes your existing X
   stronger" reason.
3. **Necrophytic Bloom and `Removes`.** It composts dead clusters of any
   colony, which arguably removes corpses that reclamation needs. The
   Death-and-Reclamation brief deliberately pairs them, so it is left
   unflagged here. Should it be flagged?
4. **Exclude Bait cards entirely?** They are draft traps, not build pieces, so
   the proposal gives them no profile and keeps them out of coverage.
5. **On the definitions, or a central catalog?** The recommendation is on the
   definitions.
6. **Any tag assignment above that reads wrong to you.** These are first
   drafts from the rules text; you know the intended play better.
