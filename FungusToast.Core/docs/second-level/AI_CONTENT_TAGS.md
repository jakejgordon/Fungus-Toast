# AI Content Tags

Every mutation and non-Bait Mycovariant carries a **content profile**: a small,
closed set of tags describing what it does on the board. The profiles feed the
*content-to-profile coverage review* (`AI_STRATEGY_AUTHORING.md`), which asks,
for each new or changed piece of content, which AI strategies it plausibly
belongs in and why.

Approved 2026-10-04 (Jake). Implemented in `FungusToast.Core/ContentProfiles/`.

## Where things live

| What | Where |
|---|---|
| Tag enums and builder | `FungusToast.Core/ContentProfiles/` (`ContentCapability`, `BoardCondition`, `ContentProfile`) |
| A mutation's tags | its `profile:` constructor argument in `FungusToast.Core/Mutations/Factories/` |
| A Mycovariant's tags | its `Profile =` initializer in `FungusToast.Core/Mycovariants/Factories/`; tier families share one `private static readonly` profile |
| Every item's tags, prerequisites, and strategy usage | generated [MUTATION_CATALOG.md](../MUTATION_CATALOG.md) and [MYCOVARIANT_CATALOG.md](../MYCOVARIANT_CATALOG.md) |
| Which strategies each item might belong in, and the recorded decisions | generated [CONTENT_COVERAGE_REPORT.md](../CONTENT_COVERAGE_REPORT.md); rules in [AI_COVERAGE_DECISIONS.md](AI_COVERAGE_DECISIONS.md) |
| Which content carries a given tag | generated [CONTENT_TAG_INDEX.md](../CONTENT_TAG_INDEX.md) |
| Enforcement | `FungusToast.Core.Tests/ContentProfiles/` |

The definitions are the only source of truth. The three generated files are
reading surfaces for people and agents. `ContentCatalogTests` fails when they
are stale, so regenerate them rather than editing them:

```bash
FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentProfiles
```

## What tags are for, and not for

- **For:** authoring-time review. Explaining which strategies new content fits
  and which it works against.
- **Not for:** runtime AI behavior, draft scoring, or player-facing copy. No
  tag changes how any AI plays, and the coverage review never edits a strategy.
  Runtime flags stay separate: `MutationAITags.CatchUp` drives catch-up surge
  use, and `MycovariantBase.SynergyWith` feeds AI draft scores.
- **Not for Bait cards.** Bait Mycovariants are draft traps aimed at the
  leading AI, not build pieces, so they carry no profile.

## The profile

| Field | Builder | Meaning |
|---|---|---|
| **Capabilities** | `ContentProfile.Of(...)` | The jobs it does. Required, 1–3. |
| **Amplifies** | `.Amplifies(...)` | Capabilities it makes stronger when the colony already has them. |
| **Needs** | `.Needs(...)` | Conditions it depends on that a plan should **produce on purpose**. |
| **Uses** | `.Uses(...)` | Conditions it benefits from **when they happen anyway**: salvage, or the environment. Using a condition does not justify producing more of it. |
| **Creates** | `.Creates(...)` | Conditions it produces, as a main effect or a side effect. |
| **Removes** | `.Removes(...)` | Conditions it destroys at a scale that can undercut a plan relying on them. |

Factories alias the enums as `Cap` and `Cond`, so a profile reads as one line:

```csharp
profile: ContentProfile.Of(Cap.SelfReclamation).Uses(Cond.OwnDeadCells)
```

### Needs or Uses?

Ask whether a strategy should deliberately create the condition to make this
content work.

- **Needs:** Necrosporulation needs `OwnCellDeaths`, and a Necrosporulation plan
  wants cells dying. Toxinborne Seeding needs `OwnToxins`.
- **Uses:** Regenerative Hyphae uses `OwnDeadCells`. It is worth having when
  cells die, but it does not mean a reclamation plan should kill its own cells.
  Detrital Enzymes, Necrophoric Adaptation, and Septal Alarm are the same.
- **Always Uses:** conditions a strategy cannot make itself: `EnemyToxins`,
  `EnemyDeadCells`, `EnemyResistantCells`, `EnemyContact`, `OpenSpace`,
  `CrampedSpace`, `BoardEdge`, and `FallingBehind`.

A condition goes under Needs or Uses, never both; the tests enforce this.

## Vocabulary

### Capabilities (20)

| Group | Tag | Definition |
|---|---|---|
| Territory | `BaseGrowth` | Raises ordinary growth chance with no board condition. |
| | `DiagonalGrowth` | Adds or strengthens diagonal (Tendril) growth. |
| | `ConditionalGrowth` | Growth bonus gated on a local board condition, named in Needs or Uses. |
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

### Board conditions (16)

Read every condition from the perspective of the colony that owns the content:
"Own" is that colony, and "Enemy" is everyone else.

| Tag | Meaning | Who can create it |
|---|---|---|
| `OwnCellDeaths` | Own living cells dying (the event). | Own plan |
| `OwnDeadCells` | Own dead cells on the board (the corpses). | Own plan |
| `EnemyDeadCells` | Enemy dead cells on the board. | Own plan or opponents |
| `EnemyCellsKilledByYou` | Enemy living cells killed by own effects. Creating it also counts as creating `EnemyDeadCells`. | Own plan |
| `OwnToxins` | Own toxins on the board. | Own plan |
| `EnemyToxins` | Enemy toxins near own cells. | Opponents |
| `OwnResistantCells` | Own Resistant cells. | Own plan |
| `EnemyResistantCells` | Enemy Resistant cells. | Opponents |
| `EnemyContact` | Long borders with living enemy cells. | Board and opponents |
| `OpenSpace` | Room to branch around own cells. | Board |
| `CrampedSpace` | Own cells with few open sides. | Board and opponents |
| `BoardEdge` | Own territory near the crust or corners. | Board |
| `LargeColony` | The effect scales with own colony size. | Own plan |
| `FallingBehind` | Own colony trails the leaders. | Game state |
| `BankedPoints` | Unspent mutation points carried between rounds. | Own plan |
| `NutrientPatches` | Neutral nutrient patches on the board. | Own plan or board |

## How the coverage review uses the tags

The review is implemented in `FungusToast.Core.Tests/ContentProfiles/Coverage/`.
Scoring, the candidate threshold, and how decisions are recorded are in
[AI_COVERAGE_DECISIONS.md](AI_COVERAGE_DECISIONS.md); the generated
[CONTENT_COVERAGE_REPORT.md](../CONTENT_COVERAGE_REPORT.md) shows every current
candidate and its decision.

**Strategy side.** A strategy's plan profile is derived from its executable
configuration: the union of the profiles of its ordered `TargetMutationGoal`s,
its surge priorities, and its explicitly ordered Mycovariant preferences,
weighted toward the front of each order. Category-derived preference sets
(`CategoryDerivedMycovariantIds`) do not count, because they express almost no
intent. Nothing is stored separately from the strategy.

**Bridge goals.** A goal bought only up to the level a later goal requires is a
*bridge* and counts at low weight. A plan that buys Chronoresilient Cytoplasm 5
only to unlock Regenerative Hyphae is not a decay-resistance plan.

**Match reasons**, each one a readable sentence:

| Reason | Fires when |
|---|---|
| Same job | Content shares a capability with the plan's leading capabilities. |
| Related job | Content is in the same capability family as the plan's top capability, such as conditional growth for a growth-led plan. |
| Amplifier | Content amplifies a capability the plan already has. |
| Feeds the plan | Content creates a condition the plan **needs**. |
| Fed by the plan | Content **needs** a condition the plan creates. |
| Salvages | Content **uses** a condition the plan already creates, such as Detrital Enzymes for a plan that leaves corpses. |
| Shared situation | Content and plan use or need the same condition, such as `OpenSpace`. Context only. |
| Tension | Content removes a condition the plan needs or uses, or the plan removes one the content needs or uses. Context only, shown so a reviewer can record *Not applicable* with the reason. |

**Rules that keep matches honest:**

1. **Never suggest making more of something only *used*.** A plan that uses
   dead cells (Regenerative Hyphae) is never matched to content that creates
   them (Autolytic Surge). The reverse is fine: content that uses dead cells is
   suggested to a plan that already creates them (*salvages*).
2. **Making something rarer is not tension.** Chronoresilient Cytoplasm
   (`DecayResistance`) means fewer cells die, but it removes no corpses, so it
   is never flagged against Regenerative Hyphae. Only an explicit **Removes**
   produces tension.
3. **The tree is not tension.** Content is never flagged against its own
   prerequisite chain, because the tree forces that pairing.
4. **Category alone never matches.** `MutationCategory` and
   `MycovariantCategory` are display groupings, too broad to signal fit.

## Tagging new or changed content

1. Pick 1–3 **capabilities** for the jobs it does. Choose the closest existing
   tags; the catalogs show how similar content is tagged.
2. Add **Amplifies** when it strengthens a capability rather than adding one.
3. List the conditions it **Needs** or **Uses**, applying the test above.
4. List what it **Creates**, including side effects such as own cells dying.
5. Add **Removes** only when it destroys a condition at a scale that would
   undercut a plan relying on it, as Mycotoxin Catabolism does to own toxins.
6. Tier families share one `private static readonly ContentProfile`.
7. Run `dotnet test FungusToast.Core.Tests --filter ContentProfiles`. Record a
   decision for every coverage candidate it reports, per
   [AI_COVERAGE_DECISIONS.md](AI_COVERAGE_DECISIONS.md). Then regenerate the
   catalogs and the coverage report, and commit them with the content.

## Extending the vocabulary

Add a tag only when new content introduces a mechanic that no existing tag
describes honestly. Stretching a tag until it means two things breaks every
match that relies on it.

1. Check that no existing capability or condition fits, and that the mechanic is
   not just a narrower case of one. `ConditionalGrowth` plus a condition covers
   most new growth bonuses, for example.
2. Add the value to `ContentCapability` or `BoardCondition` with a one-line XML
   summary, and add a row to the matching table above.
3. Retag any existing content the new value describes better.
4. Regenerate the catalogs and note the addition in `docs/WORKLOG.md`.

## Validation against the strategy briefs

Each of the five authored briefs in `docs/WORKLOG.md` is visible through the
tags:

| Brief | What the tags show |
|---|---|
| Steady Growth, Iron Shell | `BaseGrowth` + `ResistantCells`. Every Resistance Mycovariant is the same job, and Hyphal Resistance Transfer is also an amplifier. |
| Murderous Necrosporulation | Necrosporulation **needs** `OwnCellDeaths`, which Autolytic Surge and Mycelial Bloom create. Toxin content matches on `ToxinPlacement`. |
| Substrate Spore Drop | Toxinborne Seeding **needs** `OwnToxins`, which Sporicidal Bloom and Jetting Mycelium create. Mycotoxin Catabolism is flagged as tension because it removes own toxins. |
| Death and Reclamation | Reclamation content **uses** the corpses Filament Overdrive creates, and matches the plan as the same job. Necrotic Clearance and Necrophytic Bloom are flagged as tension because both remove own dead cells. |
| Economy at All Costs | `PointIncome` / `FreeUpgrades` / `TreePivot` surface Plasmid Bounty and Ascus Wager from outside Genetic Drift. |
