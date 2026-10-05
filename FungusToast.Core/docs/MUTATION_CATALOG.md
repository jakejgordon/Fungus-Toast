# Mutation Catalog

> **Generated** from the mutation definitions by `ContentCatalogTests`. Do not edit by hand: change the definition, then regenerate with
> `FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentProfiles`.
> Tag meanings and authoring rules: [AI_CONTENT_TAGS.md](second-level/AI_CONTENT_TAGS.md). Cross-content lookup by tag: [CONTENT_TAG_INDEX.md](CONTENT_TAG_INDEX.md).

Strategy columns count player-facing strategies (Proven and Campaign sets, excluding Retired) that name the mutation as an ordered mutation goal or a surge priority.

## Summary

| Mutation | Category | Tier | Capabilities | Goal in | Surge priority in |
|---|---|---|---|---|---|
| [Mycelial Bloom](#mycelial-bloom) | Growth | 1 | BaseGrowth | 8 | 0 |
| [Tendril Northeast](#tendril-northeast) | Growth | 2 | DiagonalGrowth | 3 | 0 |
| [Tendril Northwest](#tendril-northwest) | Growth | 2 | DiagonalGrowth | 7 | 0 |
| [Tendril Southeast](#tendril-southeast) | Growth | 2 | DiagonalGrowth | 3 | 0 |
| [Tendril Southwest](#tendril-southwest) | Growth | 2 | DiagonalGrowth | 3 | 0 |
| [Mycotropic Induction](#mycotropic-induction) | Growth | 3 | DiagonalGrowth | 18 | 0 |
| [Creeping Mold](#creeping-mold) | Growth | 4 | Repositioning | 42 | 0 |
| [Filament Overdrive](#filament-overdrive) | Growth | 5 | DiagonalGrowth, RemotePlacement | 6 | 0 |
| [Homeostatic Harmony](#homeostatic-harmony) | CellularResilience | 1 | DecayResistance | 0 | 0 |
| [Chronoresilient Cytoplasm](#chronoresilient-cytoplasm) | CellularResilience | 2 | DecayResistance | 11 | 0 |
| [Regenerative Hyphae](#regenerative-hyphae) | CellularResilience | 3 | SelfReclamation | 9 | 0 |
| [Necrosporulation](#necrosporulation) | CellularResilience | 4 | RemotePlacement | 25 | 0 |
| [Necrohyphal Infiltration](#necrohyphal-infiltration) | CellularResilience | 5 | CorpseCapture | 13 | 0 |
| [Catabolic Rebirth](#catabolic-rebirth) | CellularResilience | 6 | SelfReclamation | 21 | 0 |
| [Hypersystemic Regeneration](#hypersystemic-regeneration) | CellularResilience | 7 | SelfReclamation, ResistantCells | 1 | 0 |
| [Mycotoxin Tracer](#mycotoxin-tracer) | Fungicide | 1 | ToxinPlacement | 5 | 0 |
| [Mycotoxin Potentiation](#mycotoxin-potentiation) | Fungicide | 2 | ToxinLongevity, DirectKill | 5 | 0 |
| [Putrefactive Mycotoxin](#putrefactive-mycotoxin) | Fungicide | 3 | DirectKill | 14 | 0 |
| [Sporicidal Bloom](#sporicidal-bloom) | Fungicide | 4 | ToxinPlacement | 2 | 0 |
| [Necrotoxic Conversion](#necrotoxic-conversion) | Fungicide | 5 | CorpseCapture | 0 | 0 |
| [Putrefactive Rejuvenation](#putrefactive-rejuvenation) | Fungicide | 5 | DecayResistance | 10 | 0 |
| [Putrefactive Cascade](#putrefactive-cascade) | Fungicide | 6 | DirectKill | 13 | 0 |
| [Mutator Phenotype](#mutator-phenotype) | GeneticDrift | 1 | FreeUpgrades | 2 | 0 |
| [Adaptive Expression](#adaptive-expression) | GeneticDrift | 2 | PointIncome | 7 | 0 |
| [Mycotoxin Catabolism](#mycotoxin-catabolism) | GeneticDrift | 2 | ToxinCleanup, PointIncome | 3 | 0 |
| [Anabolic Inversion](#anabolic-inversion) | GeneticDrift | 3 | PointIncome | 18 | 0 |
| [Latent Polymorphism](#latent-polymorphism) | GeneticDrift | 4 | PointIncome | 0 | 0 |
| [Hyperadaptive Drift](#hyperadaptive-drift) | GeneticDrift | 5 | FreeUpgrades | 8 | 0 |
| [Ontogenic Regression](#ontogenic-regression) | GeneticDrift | 6 | TreePivot | 10 | 0 |
| [Autolytic Surge](#autolytic-surge) | MycelialSurges | 2 | BaseGrowth | 6 | 7 |
| [Chemotactic Beacon](#chemotactic-beacon) | MycelialSurges | 2 | RemotePlacement | 7 | 6 |
| [Chitin Fortification](#chitin-fortification) | MycelialSurges | 2 | ResistantCells | 8 | 7 |
| [Necrotic Clearance](#necrotic-clearance) | MycelialSurges | 2 | CorpseDenial | 1 | 1 |
| [Competitive Antagonism](#competitive-antagonism) | MycelialSurges | 3 | LeaderFocus | 0 | 0 |
| [Mimetic Resilience](#mimetic-resilience) | MycelialSurges | 3 | ResistantCells, LeaderFocus | 1 | 1 |
| [Aerated Frontier](#aerated-frontier) | SubstrateEcology | 1 | ConditionalGrowth | 0 | 0 |
| [Compaction Pressure](#compaction-pressure) | SubstrateEcology | 2 | ConditionalGrowth | 0 | 0 |
| [Crustward Tropism](#crustward-tropism) | SubstrateEcology | 2 | ConditionalGrowth | 0 | 0 |
| [Detrital Enzymes](#detrital-enzymes) | SubstrateEcology | 3 | ConditionalGrowth | 0 | 0 |
| [Toxin Margin](#toxin-margin) | SubstrateEcology | 3 | ConditionalGrowth | 0 | 0 |
| [Necrophytic Bloom](#necrophytic-bloom) | SubstrateEcology | 4 | Composting | 11 | 0 |
| [Toxinborne Seeding](#toxinborne-seeding) | SubstrateEcology | 5 | ConditionalGrowth, ToxinMobility, RemotePlacement | 2 | 0 |

## Growth

### Mycelial Bloom

Expands your colony faster in the four orthogonal directions (up / down / left / right), but makes it more vulnerable during the Decay Phase.

| | |
|---|---|
| Id | 0 |
| Category · tier | Growth · Tier 1 |
| Kind | Upgrade · max level 150 |
| Requires | — |
| Unlocks | Autolytic Surge, Chemotactic Beacon, Sporicidal Bloom, Tendril Northeast, Tendril Northwest, Tendril Southeast, Tendril Southwest |
| Capabilities | BaseGrowth |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnCellDeaths |
| Removes | — |
| Goal in | 8: Ballistospore Rot (Campaign), Beacon of Rot (Proven), Jetting Rot (Campaign), Rhizolith (Campaign), Rhizolith Crown (Campaign), Scavenger Court (Campaign), Verdant Reclaimer (Campaign), Verdant Reclaimer (Proven) |

### Tendril Northeast

Pushes growth toward the northeast (up and to the right), trading away some normal spread.

| | |
|---|---|
| Id | 7 |
| Category · tier | Growth · Tier 2 |
| Kind | Upgrade · max level 10 |
| Requires | Mycelial Bloom 10 |
| Unlocks | Mycotropic Induction |
| Capabilities | DiagonalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 3: Putrid Tendrils (Campaign), Putrid Tendrils (Proven), Wildfire Bloom (Campaign) |

### Tendril Northwest

Pushes growth toward the northwest (up and to the left), trading away some normal spread.

| | |
|---|---|
| Id | 6 |
| Category · tier | Growth · Tier 2 |
| Kind | Upgrade · max level 10 |
| Requires | Mycelial Bloom 10 |
| Unlocks | Mycotropic Induction |
| Capabilities | DiagonalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 7: Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset2`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset3`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training`] (Campaign), Putrid Tendrils (Campaign), Putrid Tendrils (Proven), Wildfire Bloom (Campaign) |

### Tendril Southeast

Pushes growth toward the southeast (down and to the right), trading away some normal spread.

| | |
|---|---|
| Id | 8 |
| Category · tier | Growth · Tier 2 |
| Kind | Upgrade · max level 10 |
| Requires | Mycelial Bloom 10 |
| Unlocks | Mycotropic Induction |
| Capabilities | DiagonalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 3: Putrid Tendrils (Campaign), Putrid Tendrils (Proven), Wildfire Bloom (Campaign) |

### Tendril Southwest

Pushes growth toward the southwest (down and to the left), trading away some normal spread.

| | |
|---|---|
| Id | 9 |
| Category · tier | Growth · Tier 2 |
| Kind | Upgrade · max level 10 |
| Requires | Mycelial Bloom 10 |
| Unlocks | Mycotropic Induction |
| Capabilities | DiagonalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 3: Putrid Tendrils (Campaign), Putrid Tendrils (Proven), Wildfire Bloom (Campaign) |

### Mycotropic Induction

Turns all Tendrils into stronger diagonal branches.

| | |
|---|---|
| Id | 12 |
| Category · tier | Growth · Tier 3 |
| Kind | Upgrade · max level 5 |
| Requires | Tendril Northwest 1; Tendril Northeast 1; Tendril Southeast 1; Tendril Southwest 1 |
| Unlocks | Creeping Mold, Hypersystemic Regeneration |
| Capabilities | DiagonalGrowth |
| Amplifies | DiagonalGrowth |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 18: Grave Bastion (Campaign), Measured Mycelium (Campaign), Measured Mycelium (Proven), Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset2`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset3`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training`] (Campaign), Pressure Bloom (Campaign), and 10 more |

### Creeping Mold

Failed growth can become repositioning, letting a cell crawl into the tile it missed.

| | |
|---|---|
| Id | 16 |
| Category · tier | Growth · Tier 4 |
| Kind | Upgrade · max level 4 |
| Requires | Mycotropic Induction 3 |
| Unlocks | Filament Overdrive |
| Capabilities | Repositioning |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 42: Adaptive Blight (Proven), Anabolic Gravebloom (Proven), Anabolic Regent (Proven), Anabolic Steward (Proven), Ballistospore Rot (Campaign), Creeping Reclaimer (Campaign), Creeping Reclaimer (Proven), Gravebloom (Campaign), and 34 more |

### Filament Overdrive

Successful Tendril growth can sacrifice its source cell to drive a longer diagonal runner.

| | |
|---|---|
| Id | 41 |
| Category · tier | Growth · Tier 5 |
| Kind | Upgrade · max level 5 |
| Requires | Creeping Mold 3; Autolytic Surge 1; Aerated Frontier 5 |
| Unlocks | — |
| Capabilities | DiagonalGrowth, RemotePlacement |
| Amplifies | DiagonalGrowth |
| Needs | — |
| Uses | OpenSpace |
| Creates | OwnCellDeaths, OwnDeadCells |
| Removes | — |
| Goal in | 6: Anabolic Gravebloom (Proven), Creeping Reclaimer (Proven), Gravebloom (Proven), Regrowth Lattice (Proven), The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign), The Necrotoxin Gauntlet [`TST_AI10_CreepingRegression`] (Campaign) |

## CellularResilience

### Homeostatic Harmony

Keeps more of your colony alive through the Decay Phase.

| | |
|---|---|
| Id | 1 |
| Category · tier | CellularResilience · Tier 1 |
| Kind | Upgrade · max level 100 |
| Requires | — |
| Unlocks | Chitin Fortification, Chronoresilient Cytoplasm, Necrotic Clearance, Toxin Margin |
| Capabilities | DecayResistance |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | none |

### Chronoresilient Cytoplasm

Lets your older cells stay stable longer before age-based decay starts.

| | |
|---|---|
| Id | 4 |
| Category · tier | CellularResilience · Tier 2 |
| Kind | Upgrade · max level 15 |
| Requires | Homeostatic Harmony 5 |
| Unlocks | Putrefactive Rejuvenation, Regenerative Hyphae |
| Capabilities | DecayResistance |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 11: Grave Bastion (Campaign), Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign), Measured Mycelium (Campaign), Measured Mycelium (Proven), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign), and 3 more |

### Regenerative Hyphae

Reclaims your own dead cells near your living colony.

| | |
|---|---|
| Id | 15 |
| Category · tier | CellularResilience · Tier 3 |
| Kind | Upgrade · max level 5 |
| Requires | Chronoresilient Cytoplasm 5 |
| Unlocks | Hypersystemic Regeneration, Necrosporulation, Necrotoxic Conversion |
| Capabilities | SelfReclamation |
| Amplifies | — |
| Needs | — |
| Uses | OwnDeadCells |
| Creates | — |
| Removes | — |
| Goal in | 9: Creeping Reclaimer (Campaign), Creeping Reclaimer (Proven), Gravebloom (Campaign), Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign), Pressure Bloom (Campaign), Regrowth Lattice (Proven), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign), and 1 more |

### Necrosporulation

A dying cell can colonize an empty tile somewhere else on the toast.

| | |
|---|---|
| Id | 11 |
| Category · tier | CellularResilience · Tier 4 |
| Kind | Upgrade · max level 5 |
| Requires | Regenerative Hyphae 2; Mycotoxin Tracer 7 |
| Unlocks | Catabolic Rebirth, Necrohyphal Infiltration |
| Capabilities | RemotePlacement |
| Amplifies | — |
| Needs | OwnCellDeaths |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 25: Anabolic Regent (Proven), Anabolic Steward (Proven), Beacon of Rot (Campaign), Creeping Reclaimer (Campaign), Creeping Reclaimer (Proven), Gravebloom (Campaign), Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign), and 17 more |

### Necrohyphal Infiltration

Failed expansion can reclaim enemy cells that have been dead long enough.

| | |
|---|---|
| Id | 21 |
| Category · tier | CellularResilience · Tier 5 |
| Kind | Upgrade · max level 5 |
| Requires | Necrosporulation 1; Detrital Enzymes 1 |
| Unlocks | — |
| Capabilities | CorpseCapture |
| Amplifies | — |
| Needs | — |
| Uses | EnemyDeadCells, EnemyContact |
| Creates | — |
| Removes | — |
| Goal in | 13: Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Needle Reclaimer (Campaign), and 5 more |

### Catabolic Rebirth

Expired toxins can reclaim your dead cells instead of simply fading out.

| | |
|---|---|
| Id | 25 |
| Category · tier | CellularResilience · Tier 6 |
| Kind | Upgrade · max level 3 |
| Requires | Necrosporulation 5; Mycotoxin Catabolism 5 |
| Unlocks | — |
| Capabilities | SelfReclamation |
| Amplifies | — |
| Needs | OwnToxins |
| Uses | OwnDeadCells |
| Creates | — |
| Removes | — |
| Goal in | 21: Anabolic Regent (Proven), Anabolic Steward (Proven), Creeping Reclaimer (Campaign), Creeping Reclaimer (Proven), Gravebloom (Campaign), Hoarded Bloom (Proven), Hoardspore Regent (Proven), Hoardspore Regent [`AI13`] (Campaign), and 13 more |

### Hypersystemic Regeneration

Makes Regenerative Hyphae stronger and gives reclaimed cells a chance to come back Resistant.

| | |
|---|---|
| Id | 30 |
| Category · tier | CellularResilience · Tier 7 |
| Kind | Upgrade · max level 3 |
| Requires | Regenerative Hyphae 3; Mycotropic Induction 1 |
| Unlocks | — |
| Capabilities | SelfReclamation, ResistantCells |
| Amplifies | SelfReclamation |
| Needs | — |
| Uses | OwnDeadCells |
| Creates | OwnResistantCells |
| Removes | — |
| Goal in | 1: Regrowth Lattice (Proven) |

## Fungicide

### Mycotoxin Tracer

Toxifies empty tiles along enemy borders to slow expansion.

| | |
|---|---|
| Id | 2 |
| Category · tier | Fungicide · Tier 1 |
| Kind | Upgrade · max level 50 |
| Requires | — |
| Unlocks | Competitive Antagonism, Mimetic Resilience, Mycotoxin Potentiation, Necrosporulation |
| Capabilities | ToxinPlacement |
| Amplifies | — |
| Needs | — |
| Uses | EnemyContact |
| Creates | OwnToxins |
| Removes | — |
| Goal in | 5: Ballistospore Rot (Campaign), Jetting Rot (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign) |

### Mycotoxin Potentiation

Makes each toxin last longer and gives it a chance to kill nearby enemies.

| | |
|---|---|
| Id | 5 |
| Category · tier | Fungicide · Tier 2 |
| Kind | Upgrade · max level 10 |
| Requires | Mycotoxin Tracer 5 |
| Unlocks | Putrefactive Mycotoxin |
| Capabilities | ToxinLongevity, DirectKill |
| Amplifies | ToxinPlacement |
| Needs | OwnToxins |
| Uses | — |
| Creates | EnemyCellsKilledByYou |
| Removes | — |
| Goal in | 5: Ballistospore Rot (Campaign), Jetting Rot (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign) |

### Putrefactive Mycotoxin

Lets living cells kill adjacent enemies just by touching them.

| | |
|---|---|
| Id | 13 |
| Category · tier | Fungicide · Tier 3 |
| Kind | Upgrade · max level 5 |
| Requires | Mycotoxin Potentiation 1 |
| Unlocks | Necrotoxic Conversion, Putrefactive Rejuvenation, Sporicidal Bloom |
| Capabilities | DirectKill |
| Amplifies | — |
| Needs | — |
| Uses | EnemyContact |
| Creates | EnemyCellsKilledByYou |
| Removes | — |
| Goal in | 14: Ballistospore Rot (Campaign), Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Jetting Rot (Campaign), and 6 more |

### Sporicidal Bloom

Turns a large colony into a wave of toxic spore drops.

| | |
|---|---|
| Id | 17 |
| Category · tier | Fungicide · Tier 4 |
| Kind | Upgrade · max level 5 |
| Requires | Putrefactive Mycotoxin 1; Mycelial Bloom 7 |
| Unlocks | Toxinborne Seeding |
| Capabilities | ToxinPlacement |
| Amplifies | — |
| Needs | LargeColony |
| Uses | — |
| Creates | OwnToxins |
| Removes | — |
| Goal in | 2: Ballistospore Rot (Campaign), Jetting Rot (Campaign) |

### Necrotoxic Conversion

Your toxin kills can turn straight into reclaimed living cells.

| | |
|---|---|
| Id | 22 |
| Category · tier | Fungicide · Tier 5 |
| Kind | Upgrade · max level 5 |
| Requires | Putrefactive Mycotoxin 5; Regenerative Hyphae 1 |
| Unlocks | Putrefactive Cascade |
| Capabilities | CorpseCapture |
| Amplifies | — |
| Needs | EnemyCellsKilledByYou, OwnToxins |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | none |

### Putrefactive Rejuvenation

Putrefactive kills can rejuvenate your nearby living cells.

| | |
|---|---|
| Id | 26 |
| Category · tier | Fungicide · Tier 5 |
| Kind | Upgrade · max level 4 |
| Requires | Putrefactive Mycotoxin 2; Chronoresilient Cytoplasm 1 |
| Unlocks | — |
| Capabilities | DecayResistance |
| Amplifies | — |
| Needs | EnemyCellsKilledByYou |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 10: Putrid Tendrils (Campaign), Putrid Tendrils (Proven), Rebirth Conductor (Campaign), Rebirth Furnace (Campaign), Rebirth Furnace (Proven), Rejuvenation Engine (Campaign), Rejuvenation Engine (Proven), The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign), and 2 more |

### Putrefactive Cascade

A putrefactive kill can keep traveling in the same direction through more enemies.

| | |
|---|---|
| Id | 28 |
| Category · tier | Fungicide · Tier 6 |
| Kind | Upgrade · max level 3 |
| Requires | Necrotoxic Conversion 1; Chemotactic Beacon 1 |
| Unlocks | — |
| Capabilities | DirectKill |
| Amplifies | DirectKill |
| Needs | — |
| Uses | EnemyContact |
| Creates | EnemyCellsKilledByYou |
| Removes | — |
| Goal in | 13: Adaptive Blight (Proven), Anabolic Gravebloom (Proven), Beacon of Rot (Campaign), Gravebloom (Proven), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Mutagen Bloom (Proven), Putrid Tendrils (Campaign), Putrid Tendrils (Proven), and 5 more |

## GeneticDrift

### Mutator Phenotype

Grants a chance of generating a free Tier 1 mutation upgrade at the start of the Mutation Phase.

| | |
|---|---|
| Id | 3 |
| Category · tier | GeneticDrift · Tier 1 |
| Kind | Upgrade · max level 10 |
| Requires | — |
| Unlocks | Adaptive Expression, Hyperadaptive Drift, Latent Polymorphism, Mycotoxin Catabolism |
| Capabilities | FreeUpgrades |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 2: Measured Mycelium (Campaign), Measured Mycelium (Proven) |

### Adaptive Expression

Can generate extra mutation points at the start of Mutation Phase.

| | |
|---|---|
| Id | 10 |
| Category · tier | GeneticDrift · Tier 2 |
| Kind | Upgrade · max level 5 |
| Requires | Mutator Phenotype 5 |
| Unlocks | Anabolic Inversion, Latent Polymorphism, Necrophytic Bloom |
| Capabilities | PointIncome |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 7: Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset2`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset3`] (Campaign), Overextender [`CMP_Mobility_Overextender_Training`] (Campaign), Pulsar Sprout (Campaign), Scavenger Court (Campaign), Tempo Harvester (Campaign) |

### Mycotoxin Catabolism

Lets living cells break down nearby toxins for cleanup and occasional mutation points.

| | |
|---|---|
| Id | 19 |
| Category · tier | GeneticDrift · Tier 2 |
| Kind | Upgrade · max level 8 |
| Requires | Mutator Phenotype 2 |
| Unlocks | Catabolic Rebirth |
| Capabilities | ToxinCleanup, PointIncome |
| Amplifies | — |
| Needs | — |
| Uses | EnemyToxins |
| Creates | — |
| Removes | OwnToxins, EnemyToxins |
| Goal in | 3: Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign) |

### Anabolic Inversion

Falling behind can turn into a burst of mutation points.

| | |
|---|---|
| Id | 14 |
| Category · tier | GeneticDrift · Tier 3 |
| Kind | Upgrade · max level 3 |
| Requires | Adaptive Expression 3 |
| Unlocks | Hyperadaptive Drift, Latent Polymorphism |
| Capabilities | PointIncome |
| Amplifies | — |
| Needs | — |
| Uses | FallingBehind |
| Creates | — |
| Removes | — |
| Goal in | 18: Anabolic Gravebloom (Proven), Anabolic Regent (Proven), Anabolic Steward (Proven), Beacon of Rot (Campaign), Hoarded Bloom (Proven), Hoardspore Regent (Proven), Hoardspore Regent [`AI13`] (Campaign), Pressure Bloom (Campaign), and 10 more |

### Latent Polymorphism

Makes banked mutation points earn interest.

| | |
|---|---|
| Id | 39 |
| Category · tier | GeneticDrift · Tier 4 |
| Kind | Upgrade · max level 5 |
| Requires | Mutator Phenotype 7; Adaptive Expression 5; Anabolic Inversion 3 |
| Unlocks | — |
| Capabilities | PointIncome |
| Amplifies | — |
| Needs | BankedPoints |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | none |

### Hyperadaptive Drift

Makes Mutator Phenotype reach further up your tree and sometimes chain extra Tier 1 upgrades.

| | |
|---|---|
| Id | 20 |
| Category · tier | GeneticDrift · Tier 5 |
| Kind | Upgrade · max level 3 |
| Requires | Mutator Phenotype 8; Anabolic Inversion 3; Chitin Fortification 1 |
| Unlocks | Ontogenic Regression |
| Capabilities | FreeUpgrades |
| Amplifies | FreeUpgrades |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 8: Adaptive Blight (Proven), Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign), Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign), Hyphal Pulse (Proven), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Mutagen Bloom (Proven), Pulse Runner (Campaign), The Necrotoxin Gauntlet [`CMP_Bloom_NecrotoxinGauntlet_Elite`] (Campaign) |

### Ontogenic Regression

Can trade away early mutations to steer evolution toward stronger late-game mutations.

| | |
|---|---|
| Id | 31 |
| Category · tier | GeneticDrift · Tier 6 |
| Kind | Upgrade · max level 3 |
| Requires | Hyperadaptive Drift 2; 10+ levels in each of 3 categories at Tier1 |
| Unlocks | — |
| Capabilities | TreePivot |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Goal in | 10: Anabolic Gravebloom (Proven), Beacon of Rot (Campaign), Beacon of Rot (Proven), Gravebloom (Proven), Rhizolith (Campaign), Rhizolith Crown (Campaign), Thanatophyte (Campaign), The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign), and 2 more |

## MycelialSurges

### Autolytic Surge

Accelerates your colony's growth at the cost of making its cells more likely to die.

| | |
|---|---|
| Id | 23 |
| Category · tier | MycelialSurges · Tier 2 |
| Kind | Surge, 3 rounds · max level 10 |
| Requires | Mycelial Bloom 5 |
| Unlocks | Filament Overdrive |
| Capabilities | BaseGrowth |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnCellDeaths, OwnDeadCells |
| Removes | — |
| Goal in | 6: Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign), Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign), Hyphal Pulse (Proven), Needle Reclaimer (Campaign), Pulsar Sprout (Campaign), Pulse Runner (Campaign) |
| Surge priority in | 7: Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign), Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign), Hyphal Pulse (Proven), Needle Reclaimer (Campaign), Pulsar Sprout (Campaign), Pulse Runner (Campaign), Wildfire Bloom (Campaign) |

### Chemotactic Beacon

Lets you place a target marker and grow a straight line toward it.

| | |
|---|---|
| Id | 33 |
| Category · tier | MycelialSurges · Tier 2 |
| Kind | Surge, 4 rounds · max level 5 |
| Requires | Mycelial Bloom 7 |
| Unlocks | Putrefactive Cascade |
| Capabilities | RemotePlacement |
| Amplifies | — |
| Needs | — |
| Uses | OpenSpace |
| Creates | — |
| Removes | — |
| Goal in | 7: Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign), Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign), Beacon of Rot (Campaign), Beacon of Rot (Proven), Rhizolith (Campaign), Rhizolith Crown (Campaign), Tempo Harvester (Campaign) |
| Surge priority in | 6: Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign), Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign), Beacon of Rot (Proven), Rhizolith (Campaign), Rhizolith Crown (Campaign), Tempo Harvester (Campaign) |

### Chitin Fortification

While active, permanently makes part of your colony Resistant before each Growth Phase.

| | |
|---|---|
| Id | 27 |
| Category · tier | MycelialSurges · Tier 2 |
| Kind | Surge, 3 rounds · max level 10 |
| Requires | Homeostatic Harmony 5 |
| Unlocks | Hyperadaptive Drift, Mimetic Resilience |
| Capabilities | ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Goal in | 8: Grave Bastion (Campaign), Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign), Mimic Bastion (Campaign), Pulsar Sprout (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign), Wildfire Bloom (Campaign) |
| Surge priority in | 7: Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign), Mimic Bastion (Campaign), Pulsar Sprout (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign), Wildfire Bloom (Campaign) |

### Necrotic Clearance

Lets your living cells clear nearby dead cells before enemies can reclaim them.

| | |
|---|---|
| Id | 42 |
| Category · tier | MycelialSurges · Tier 2 |
| Kind | Surge, 3 rounds · max level 3 |
| Requires | Homeostatic Harmony 5 |
| Unlocks | — |
| Capabilities | CorpseDenial |
| Amplifies | — |
| Needs | — |
| Uses | OwnDeadCells |
| Creates | — |
| Removes | OwnDeadCells |
| Goal in | 1: Grave Bastion (Campaign) |
| Surge priority in | 1: Grave Bastion (Campaign) |

### Competitive Antagonism

Makes your toxin pressure focus more on stronger colonies.

| | |
|---|---|
| Id | 32 |
| Category · tier | MycelialSurges · Tier 3 |
| Kind | Surge, 5 rounds · max level 5 |
| Requires | Mycotoxin Tracer 15 |
| Unlocks | — |
| Capabilities | LeaderFocus |
| Amplifies | ToxinPlacement |
| Needs | — |
| Uses | FallingBehind |
| Creates | — |
| Removes | — |
| Goal in | none |
| Surge priority in | none |

### Mimetic Resilience

Lets you try to copy Resistant footholds from stronger enemies.

| | |
|---|---|
| Id | 29 |
| Category · tier | MycelialSurges · Tier 3 |
| Kind | Surge, 5 rounds · max level 3 |
| Requires | Chitin Fortification 1; Mycotoxin Tracer 10 |
| Unlocks | — |
| Capabilities | ResistantCells, LeaderFocus |
| Amplifies | — |
| Needs | — |
| Uses | EnemyResistantCells, FallingBehind |
| Creates | OwnResistantCells |
| Removes | — |
| Goal in | 1: Mimic Bastion (Campaign) |
| Surge priority in | 1: Mimic Bastion (Campaign) |

## SubstrateEcology

### Aerated Frontier

Helps your colony spread from established cells with room to branch.

| | |
|---|---|
| Id | 34 |
| Category · tier | SubstrateEcology · Tier 1 |
| Kind | Upgrade · max level 20 |
| Requires | — |
| Unlocks | Compaction Pressure, Crustward Tropism, Filament Overdrive, Toxin Margin |
| Capabilities | ConditionalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | OpenSpace |
| Creates | — |
| Removes | — |
| Goal in | none |

### Compaction Pressure

Helps your colony push through cramped territory.

| | |
|---|---|
| Id | 37 |
| Category · tier | SubstrateEcology · Tier 2 |
| Kind | Upgrade · max level 5 |
| Requires | Aerated Frontier 10 |
| Unlocks | Detrital Enzymes |
| Capabilities | ConditionalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | CrampedSpace |
| Creates | — |
| Removes | — |
| Goal in | none |

### Crustward Tropism

Helps your colony press outward toward the crust.

| | |
|---|---|
| Id | 35 |
| Category · tier | SubstrateEcology · Tier 2 |
| Kind | Upgrade · max level 5 |
| Requires | Aerated Frontier 10 |
| Unlocks | Detrital Enzymes |
| Capabilities | ConditionalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | BoardEdge |
| Creates | — |
| Removes | — |
| Goal in | none |

### Detrital Enzymes

Helps your colony spread through nearby dead matter.

| | |
|---|---|
| Id | 36 |
| Category · tier | SubstrateEcology · Tier 3 |
| Kind | Upgrade · max level 5 |
| Requires | one of Crustward Tropism 1 or Compaction Pressure 1 |
| Unlocks | Necrohyphal Infiltration, Necrophytic Bloom |
| Capabilities | ConditionalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | OwnDeadCells, EnemyDeadCells |
| Creates | — |
| Removes | — |
| Goal in | none |

### Toxin Margin

Helps your colony grow around enemy toxin fields.

| | |
|---|---|
| Id | 38 |
| Category · tier | SubstrateEcology · Tier 3 |
| Kind | Upgrade · max level 5 |
| Requires | Aerated Frontier 5; Homeostatic Harmony 5 |
| Unlocks | — |
| Capabilities | ConditionalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | EnemyToxins |
| Creates | — |
| Removes | — |
| Goal in | none |

### Necrophytic Bloom

Large clusters of your dead cells can compost into neutral nutrient patches.

| | |
|---|---|
| Id | 18 |
| Category · tier | SubstrateEcology · Tier 4 |
| Kind | Upgrade · max level 5 |
| Requires | Detrital Enzymes 3; Adaptive Expression 3 |
| Unlocks | Toxinborne Seeding |
| Capabilities | Composting |
| Amplifies | — |
| Needs | — |
| Uses | OwnDeadCells, EnemyDeadCells |
| Creates | NutrientPatches |
| Removes | OwnDeadCells, EnemyDeadCells |
| Goal in | 11: Anabolic Gravebloom (Proven), Ballistospore Rot (Campaign), Beacon of Rot (Proven), Gravebloom (Proven), Jetting Rot (Campaign), Mimic Bastion (Campaign), Rhizolith (Campaign), Rhizolith Crown (Campaign), and 3 more |

### Toxinborne Seeding

Lets a mobile toxin carry a newly grown cell into enemy territory.

| | |
|---|---|
| Id | 40 |
| Category · tier | SubstrateEcology · Tier 5 |
| Kind | Upgrade · max level 3 |
| Requires | Necrophytic Bloom 1; Sporicidal Bloom 1 |
| Unlocks | — |
| Capabilities | ConditionalGrowth, ToxinMobility, RemotePlacement |
| Amplifies | — |
| Needs | OwnToxins |
| Uses | EnemyContact |
| Creates | — |
| Removes | — |
| Goal in | 2: Ballistospore Rot (Campaign), Jetting Rot (Campaign) |
