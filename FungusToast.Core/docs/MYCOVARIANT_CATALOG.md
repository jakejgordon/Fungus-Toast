# Mycovariant Catalog

> **Generated** from the Mycovariant definitions by `ContentCatalogTests`. Do not edit by hand: change the definition, then regenerate with
> `FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentProfiles`.
> Tag meanings and authoring rules: [AI_CONTENT_TAGS.md](second-level/AI_CONTENT_TAGS.md). Cross-content lookup by tag: [CONTENT_TAG_INDEX.md](CONTENT_TAG_INDEX.md).

"Preferred by" counts player-facing strategies (Proven and Campaign sets, excluding Retired) that name the Mycovariant in an explicit, ordered draft preference. "In category sets of" counts those that only reach it through a whole-category preference, where AI score picks among equal options.

## Summary

| Mycovariant | Category | Type | Capabilities | Preferred by | In category sets of |
|---|---|---|---|---|---|
| [Ascus Wager](#ascus-wager) | Economy | Economy | FreeUpgrades | 14 | 13 |
| [Plasmid Bounty I](#plasmid-bounty-i) | Economy | Economy | PointIncome | 14 | 13 |
| [Plasmid Bounty II](#plasmid-bounty-ii) | Economy | Economy | PointIncome | 14 | 13 |
| [Plasmid Bounty III](#plasmid-bounty-iii) | Economy | Economy | PointIncome | 17 | 13 |
| [Aggressotropic Conduit I](#aggressotropic-conduit-i) | Growth | Passive | RemotePlacement, LeaderFocus, ResistantCells | 0 | 20 |
| [Aggressotropic Conduit II](#aggressotropic-conduit-ii) | Growth | Passive | RemotePlacement, LeaderFocus, ResistantCells | 2 | 20 |
| [Aggressotropic Conduit III](#aggressotropic-conduit-iii) | Growth | Passive | RemotePlacement, LeaderFocus, ResistantCells | 9 | 20 |
| [Corner Conduit I](#corner-conduit-i) | Growth | Passive | RemotePlacement | 0 | 20 |
| [Corner Conduit II](#corner-conduit-ii) | Growth | Passive | RemotePlacement | 0 | 20 |
| [Corner Conduit III](#corner-conduit-iii) | Growth | Passive | RemotePlacement | 4 | 20 |
| [Hyphal Draw](#hyphal-draw) | Growth | Active | Repositioning, LeaderFocus | 7 | 20 |
| [Perimeter Proliferator](#perimeter-proliferator) | Growth | Passive | ConditionalGrowth | 7 | 20 |
| [Hyphal Resistance Transfer](#hyphal-resistance-transfer) | Resistance | Passive | ResistantCells | 9 | 8 |
| [Mycelial Bastion I](#mycelial-bastion-i) | Resistance | Active | ResistantCells | 1 | 8 |
| [Mycelial Bastion II](#mycelial-bastion-ii) | Resistance | Active | ResistantCells | 1 | 8 |
| [Mycelial Bastion III](#mycelial-bastion-iii) | Resistance | Active | ResistantCells | 6 | 8 |
| [Septal Alarm](#septal-alarm) | Resistance | Passive | ResistantCells | 9 | 8 |
| [Septal Seal](#septal-seal) | Resistance | Active | ResistantCells | 0 | 8 |
| [Surgical Inoculation](#surgical-inoculation) | Resistance | Active | ResistantCells, RemotePlacement | 0 | 8 |
| [Ballistospore Discharge I](#ballistospore-discharge-i) | Fungicide | Active | ToxinPlacement | 0 | 2 |
| [Ballistospore Discharge II](#ballistospore-discharge-ii) | Fungicide | Active | ToxinPlacement | 0 | 2 |
| [Ballistospore Discharge III](#ballistospore-discharge-iii) | Fungicide | Active | ToxinPlacement | 1 | 2 |
| [Chemotactic Mycotoxins](#chemotactic-mycotoxins) | Fungicide | Passive | ToxinMobility | 1 | 2 |
| [Cytolytic Burst](#cytolytic-burst) | Fungicide | Active | DirectKill | 0 | 2 |
| [Enduring Toxaphores](#enduring-toxaphores) | Fungicide | Passive | ToxinLongevity | 1 | 2 |
| [Jetting Mycelium I](#jetting-mycelium-i) | Fungicide | Directional | RemotePlacement, ToxinPlacement | 0 | 2 |
| [Jetting Mycelium II](#jetting-mycelium-ii) | Fungicide | Directional | RemotePlacement, ToxinPlacement | 0 | 2 |
| [Jetting Mycelium III](#jetting-mycelium-iii) | Fungicide | Directional | RemotePlacement, ToxinPlacement | 0 | 2 |
| [Necrophoric Adaptation](#necrophoric-adaptation) | Reclamation | Passive | SelfReclamation | 10 | 7 |
| [Reclamation Rhizomorphs](#reclamation-rhizomorphs) | Reclamation | Passive | SelfReclamation | 10 | 7 |
| [Neutralizing Mantle](#neutralizing-mantle) | Defense | Passive | ToxinCleanup | 0 | 0 |

## Economy

### Ascus Wager

One-time on draft: gain 1 free level of a random Tier 5 mutation, ignoring prerequisites.

| | |
|---|---|
| Id | 1034 |
| Category · type | Economy · Economy |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | FreeUpgrades |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Preferred by | 14: Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Rejuvenation Engine (Campaign), Tempo Harvester (Campaign), and 6 more |
| In category sets of | 13 strategies |

### Plasmid Bounty I

One-time on draft: absorb foreign plasmids and gain 7 mutation points.

| | |
|---|---|
| Id | 1000 |
| Category · type | Economy · Economy |
| Availability | Universal (stays in the pool) |
| Capabilities | PointIncome |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Preferred by | 14: Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Scavenger Court (Campaign), Tempo Harvester (Campaign), and 6 more |
| In category sets of | 13 strategies |

### Plasmid Bounty II

One-time on draft: absorb foreign plasmids and gain 11 mutation points.

| | |
|---|---|
| Id | 1001 |
| Category · type | Economy · Economy |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | PointIncome |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Preferred by | 14: Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Scavenger Court (Campaign), Tempo Harvester (Campaign), and 6 more |
| In category sets of | 13 strategies |

### Plasmid Bounty III

One-time on draft: absorb foreign plasmids and gain 15 mutation points.

| | |
|---|---|
| Id | 1002 |
| Category · type | Economy · Economy |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | PointIncome |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Preferred by | 17: Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign), Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Pressure Bloom (Campaign), Rejuvenation Engine (Campaign), and 9 more |
| In category sets of | 13 strategies |

## Growth

### Aggressotropic Conduit I

Before each Growth Phase, colonize, reclaim, infest, or overgrow up to 1 tile from your starting spore toward the enemy starting spore with the most living cells. The last cell placed becomes Resistant. Skips your living cells and enemy Resistant cells. Stacks with other Aggressotropic Conduits and grows stronger the later it is drafted.

| | |
|---|---|
| Id | 1027 |
| Category · type | Growth · Passive |
| Availability | Universal (stays in the pool) |
| Capabilities | RemotePlacement, LeaderFocus, ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | none |
| In category sets of | 20 strategies |

### Aggressotropic Conduit II

Before each Growth Phase, colonize, reclaim, infest, or overgrow up to 2 tiles from your starting spore toward the enemy starting spore with the most living cells. The last cell placed becomes Resistant. Skips your living cells and enemy Resistant cells. Stacks with other Aggressotropic Conduits and grows stronger the later it is drafted.

| | |
|---|---|
| Id | 1028 |
| Category · type | Growth · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | RemotePlacement, LeaderFocus, ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | 2: Needle Reclaimer (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign) |
| In category sets of | 20 strategies |

### Aggressotropic Conduit III

Before each Growth Phase, colonize, reclaim, infest, or overgrow up to 3 tiles from your starting spore toward the enemy starting spore with the most living cells. The last cell placed becomes Resistant. Skips your living cells and enemy Resistant cells. Stacks with other Aggressotropic Conduits and grows stronger the later it is drafted.

| | |
|---|---|
| Id | 1029 |
| Category · type | Growth · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | RemotePlacement, LeaderFocus, ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | 9: Needle Reclaimer (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign), Pressure Bloom (Campaign), Pulsar Sprout (Campaign), Resilient Canopy (Campaign), Rhizolith Crown (Campaign), Rooted Canopy (Campaign), Voltaic Bloom [`AI12`] (Campaign), and 1 more |
| In category sets of | 20 strategies |

### Corner Conduit I

Before each Growth Phase, colonize, reclaim, infest, or overgrow up to 2 tiles from your starting spore toward the nearest corner. Skips your living cells and enemy Resistant cells. Grows stronger the later it is drafted.

| | |
|---|---|
| Id | 1024 |
| Category · type | Growth · Passive |
| Availability | Universal (stays in the pool) |
| Capabilities | RemotePlacement |
| Amplifies | — |
| Needs | — |
| Uses | BoardEdge |
| Creates | — |
| Removes | — |
| Preferred by | none |
| In category sets of | 20 strategies |

### Corner Conduit II

Before each Growth Phase, colonize, reclaim, infest, or overgrow up to 3 tiles from your starting spore toward the nearest corner. Skips your living cells and enemy Resistant cells. Grows stronger the later it is drafted.

| | |
|---|---|
| Id | 1025 |
| Category · type | Growth · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | RemotePlacement |
| Amplifies | — |
| Needs | — |
| Uses | BoardEdge |
| Creates | — |
| Removes | — |
| Preferred by | none |
| In category sets of | 20 strategies |

### Corner Conduit III

Before each Growth Phase, colonize, reclaim, infest, or overgrow up to 4 tiles from your starting spore toward the nearest corner. Skips your living cells and enemy Resistant cells. Grows stronger the later it is drafted.

| | |
|---|---|
| Id | 1026 |
| Category · type | Growth · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | RemotePlacement |
| Amplifies | — |
| Needs | — |
| Uses | BoardEdge |
| Creates | — |
| Removes | — |
| Preferred by | 4: Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign), Pressure Bloom (Campaign), Rhizolith Crown (Campaign), Verdant Reclaimer (Campaign) |
| In category sets of | 20 strategies |

### Hyphal Draw

One-time on draft: trace from your starting spore toward the enemy start with the most living cells, pick up your non-Resistant living cells on that path, then move them to the enemy end of the path and refill it back toward you, skipping Resistant cells.

| | |
|---|---|
| Id | 1030 |
| Category · type | Growth · Active |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | Repositioning, LeaderFocus |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | — |
| Removes | — |
| Preferred by | 7: Needle Reclaimer (Campaign), Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign), Pressure Bloom (Campaign), Pulsar Sprout (Campaign), Verdant Reclaimer (Campaign), Voltaic Bloom [`AI12`] (Campaign), Wildfire Bloom (Campaign) |
| In category sets of | 20 strategies |

### Perimeter Proliferator

For the rest of the game, your growth gets a 2.5x multiplier within 2 tiles of the board edge (the crust).

| | |
|---|---|
| Id | 1012 |
| Category · type | Growth · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ConditionalGrowth |
| Amplifies | — |
| Needs | — |
| Uses | BoardEdge |
| Creates | — |
| Removes | — |
| Preferred by | 7: Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign), Pressure Bloom (Campaign), Resilient Canopy (Campaign), Rooted Canopy (Campaign), Verdant Reclaimer (Campaign), Voltaic Bloom [`AI12`] (Campaign), Wildfire Bloom (Campaign) |
| In category sets of | 20 strategies |

## Resistance

### Hyphal Resistance Transfer

For the rest of the game, after each Growth Phase, each of your non-Resistant living cells adjacent (including diagonally) to a Resistant cell becomes Resistant with 12% chance.

| | |
|---|---|
| Id | 1015 |
| Category · type | Resistance · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ResistantCells |
| Amplifies | ResistantCells |
| Needs | OwnResistantCells |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | 9: Grave Bastion (Campaign), Mimic Bastion (Campaign), Pulsar Sprout (Campaign), Resilient Canopy (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign), Rhizolith Crown (Campaign), Rooted Canopy (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign), and 1 more |
| In category sets of | 8 strategies |

### Mycelial Bastion I

One-time on draft: select up to 8 living cells to become Resistant. Resistant cells cannot be killed, infested, or poisoned for the rest of the game.

| | |
|---|---|
| Id | 1010 |
| Category · type | Resistance · Active |
| Availability | Universal (stays in the pool) |
| Capabilities | ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | 1: Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign) |
| In category sets of | 8 strategies |

### Mycelial Bastion II

One-time on draft: select up to 11 living cells to become Resistant. Resistant cells cannot be killed, infested, or poisoned for the rest of the game.

| | |
|---|---|
| Id | 1013 |
| Category · type | Resistance · Active |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | 1: Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign) |
| In category sets of | 8 strategies |

### Mycelial Bastion III

One-time on draft: select up to 16 living cells to become Resistant. Resistant cells cannot be killed, infested, or poisoned for the rest of the game.

| | |
|---|---|
| Id | 1014 |
| Category · type | Resistance · Active |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | 6: Grave Bastion (Campaign), Mimic Bastion (Campaign), Pulsar Sprout (Campaign), Resilient Canopy (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign), Rooted Canopy (Campaign) |
| In category sets of | 8 strategies |

### Septal Alarm

For the rest of the game, whenever one of your living cells dies, each of your non-Resistant living cells orthogonally adjacent (up / down / left / right) to it has a 15% chance to become Resistant.

| | |
|---|---|
| Id | 1031 |
| Category · type | Resistance · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | OwnCellDeaths |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | 9: Grave Bastion (Campaign), Mimic Bastion (Campaign), Pulsar Sprout (Campaign), Resilient Canopy (Campaign), Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign), Rhizolith Crown (Campaign), Rooted Canopy (Campaign), Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign), and 1 more |
| In category sets of | 8 strategies |

### Septal Seal

One-time on draft: a random share of your non-Resistant living cells become Resistant: 30% divided by the number of Mycovariants you already own, rounded up to at least 1 cell. Resistant cells cannot be killed, infested, or poisoned.

| | |
|---|---|
| Id | 1036 |
| Category · type | Resistance · Active |
| Availability | Unique (leaves the pool once drafted) · unlocks at Moldiness level 1 |
| Capabilities | ResistantCells |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | none |
| In category sets of | 8 strategies |

### Surgical Inoculation

One-time on draft: place one Resistant cell on any valid tile except a tile already occupied by a Resistant cell.

| | |
|---|---|
| Id | 1011 |
| Category · type | Resistance · Active |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ResistantCells, RemotePlacement |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnResistantCells |
| Removes | — |
| Preferred by | none |
| In category sets of | 8 strategies |

## Fungicide

### Ballistospore Discharge I

One-time on draft: launch toxin spores to toxify up to 12 empty tiles.

| | |
|---|---|
| Id | 1018 |
| Category · type | Fungicide · Active |
| Availability | Universal (stays in the pool) |
| Capabilities | ToxinPlacement |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnToxins |
| Removes | — |
| Preferred by | none |
| In category sets of | 2 strategies |

### Ballistospore Discharge II

One-time on draft: launch toxin spores to toxify up to 17 empty tiles.

| | |
|---|---|
| Id | 1019 |
| Category · type | Fungicide · Active |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ToxinPlacement |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnToxins |
| Removes | — |
| Preferred by | none |
| In category sets of | 2 strategies |

### Ballistospore Discharge III

One-time on draft: launch toxin spores to toxify up to 22 empty tiles.

| | |
|---|---|
| Id | 1020 |
| Category · type | Fungicide · Active |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ToxinPlacement |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnToxins |
| Removes | — |
| Preferred by | 1: Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign) |
| In category sets of | 2 strategies |

### Chemotactic Mycotoxins

For the rest of the game, at the end of each Decay Phase, each of your toxins with no orthogonally adjacent (up / down / left / right) enemy living cell has a 3% chance per Mycotoxin Tracer level to move to a random empty tile next to an enemy living cell.

| | |
|---|---|
| Id | 1023 |
| Category · type | Fungicide · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ToxinMobility |
| Amplifies | ToxinPlacement |
| Needs | OwnToxins |
| Uses | — |
| Creates | — |
| Removes | — |
| Preferred by | 1: Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign) |
| In category sets of | 2 strategies |

### Cytolytic Burst

One-time on draft: choose one of your toxins to burst in a 4-tile radius. Each tile in range has 65% chance to poison a non-Resistant living cell or toxify an empty tile or dead cell.

| | |
|---|---|
| Id | 1022 |
| Category · type | Fungicide · Active |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | DirectKill |
| Amplifies | — |
| Needs | OwnToxins |
| Uses | EnemyContact |
| Creates | EnemyCellsKilledByYou |
| Removes | — |
| Preferred by | none |
| In category sets of | 2 strategies |

### Enduring Toxaphores

One-time on draft: extend all of your current toxins by 4 Growth Cycles. For the rest of the game, new toxins you place last 8 extra Growth Cycles.

| | |
|---|---|
| Id | 1016 |
| Category · type | Fungicide · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ToxinLongevity |
| Amplifies | ToxinPlacement |
| Needs | OwnToxins |
| Uses | — |
| Creates | — |
| Removes | — |
| Preferred by | 1: Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign) |
| In category sets of | 2 strategies |

### Jetting Mycelium I

One-time on draft: aim a spore-jet from one of your living cells in one orthogonal direction (up / down / left / right). It colonizes, reclaims, infests, or overgrows up to 3 tiles in a line, then a widening toxin fan up to 7 tiles wide poisons non-Resistant enemy living cells and toxifies empty tiles and dead cells.

| | |
|---|---|
| Id | 1005 |
| Category · type | Fungicide · Directional |
| Availability | Universal (stays in the pool) |
| Capabilities | RemotePlacement, ToxinPlacement |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnToxins |
| Removes | — |
| Preferred by | none |
| In category sets of | 2 strategies |

### Jetting Mycelium II

One-time on draft: aim a spore-jet from one of your living cells in one orthogonal direction (up / down / left / right). It colonizes, reclaims, infests, or overgrows up to 3 tiles in a line, then a widening toxin fan up to 9 tiles wide poisons non-Resistant enemy living cells and toxifies empty tiles and dead cells.

| | |
|---|---|
| Id | 1032 |
| Category · type | Fungicide · Directional |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | RemotePlacement, ToxinPlacement |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnToxins |
| Removes | — |
| Preferred by | none |
| In category sets of | 2 strategies |

### Jetting Mycelium III

One-time on draft: aim a spore-jet from one of your living cells in one orthogonal direction (up / down / left / right). It colonizes, reclaims, infests, or overgrows up to 4 tiles in a line, then a widening toxin fan up to 11 tiles wide poisons non-Resistant enemy living cells and toxifies empty tiles and dead cells.

| | |
|---|---|
| Id | 1033 |
| Category · type | Fungicide · Directional |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | RemotePlacement, ToxinPlacement |
| Amplifies | — |
| Needs | — |
| Uses | — |
| Creates | OwnToxins |
| Removes | — |
| Preferred by | none |
| In category sets of | 2 strategies |

## Reclamation

### Necrophoric Adaptation

For the rest of the game, whenever one of your living cells dies, reclaim one orthogonally adjacent (up / down / left / right) dead cell with 15% chance.

| | |
|---|---|
| Id | 1021 |
| Category · type | Reclamation · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | SelfReclamation |
| Amplifies | — |
| Needs | — |
| Uses | OwnCellDeaths, OwnDeadCells |
| Creates | — |
| Removes | — |
| Preferred by | 10: Grave Bastion (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Mimic Bastion (Campaign), Needle Reclaimer (Campaign), Rejuvenation Engine (Campaign), Scavenger Court (Campaign), Tempo Harvester (Campaign), and 2 more |
| In category sets of | 7 strategies |

### Reclamation Rhizomorphs

For the rest of the game, whenever your reclaim attempt fails, immediately make one extra reclaim attempt with 30% chance.

| | |
|---|---|
| Id | 1017 |
| Category · type | Reclamation · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | SelfReclamation |
| Amplifies | SelfReclamation |
| Needs | — |
| Uses | OwnDeadCells |
| Creates | — |
| Removes | — |
| Preferred by | 10: Grave Bastion (Campaign), Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign), Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign), Mimic Bastion (Campaign), Needle Reclaimer (Campaign), Rejuvenation Engine (Campaign), Scavenger Court (Campaign), Tempo Harvester (Campaign), and 2 more |
| In category sets of | 7 strategies |

## Defense

### Neutralizing Mantle

For the rest of the game, whenever an enemy toxin appears orthogonally adjacent (up / down / left / right) to one of your living cells, clear it immediately with 20% chance.

| | |
|---|---|
| Id | 1009 |
| Category · type | Defense · Passive |
| Availability | Unique (leaves the pool once drafted) |
| Capabilities | ToxinCleanup |
| Amplifies | — |
| Needs | — |
| Uses | EnemyToxins |
| Creates | — |
| Removes | EnemyToxins |
| Preferred by | none |
| In category sets of | 0 strategies |

## Bait cards (untagged)

Bait cards are draft traps aimed at the leading AI, not build pieces, so they carry no content profile and stay out of the coverage review.

- **Ascus Bait** (id 1035): One-time on draft: if Human, gain 8 mutation points. If AI, 10% of your non-Resistant living cells die at random (rounded up).
- **Perispore Crown** (id 1038): One-time on draft: if Human, gain 10 mutation points. If AI, Human toxins erupt in a circle around your starting spore, poisoning every non-Human, non-Resistant living cell and toxifying every empty tile, dead cell, and non-Human toxin in range.
- **Sporal Snare** (id 1037): One-time on draft: if Human, gain 10 mutation points. If AI, the Human colonizes, reclaims, infests, or overgrows up to 8 tiles along the line from the Human's starting spore to yours, skipping Resistant cells.
- **Sporophore Decoy** (id 1039): One-time on draft: if Human, gain 8 mutation points. If AI, all of your Resistant cells except your starting spore immediately lose Resistance.
