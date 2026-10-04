# Content Tag Index

> **Generated** from the mutation and Mycovariant definitions by `ContentCatalogTests`. Do not edit by hand.
> Regenerate with `FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentCatalogTests`. Tag meanings: [AI_CONTENT_TAGS.md](second-level/AI_CONTENT_TAGS.md).

Every tag, with the content that carries it. Mycovariants are in *italics*; a tier family is listed once. Use this to answer questions such as "what creates own dead cells?" without reading the factories.

## Capabilities

| Capability | Has it | Amplifies it |
|---|---|---|
| `BaseGrowth` | Autolytic Surge, Mycelial Bloom | — |
| `DiagonalGrowth` | Filament Overdrive, Mycotropic Induction, Tendril Northeast, Tendril Northwest, Tendril Southeast, Tendril Southwest | Filament Overdrive, Mycotropic Induction |
| `ConditionalGrowth` | Aerated Frontier, Compaction Pressure, Crustward Tropism, Detrital Enzymes, *Perimeter Proliferator*, Toxin Margin, Toxinborne Seeding | — |
| `RemotePlacement` | *Aggressotropic Conduit I–III*, Chemotactic Beacon, *Corner Conduit I–III*, Filament Overdrive, *Jetting Mycelium I–III*, Necrosporulation, *Surgical Inoculation*, Toxinborne Seeding | — |
| `Repositioning` | Creeping Mold, *Hyphal Draw* | — |
| `DecayResistance` | Chronoresilient Cytoplasm, Homeostatic Harmony, Putrefactive Rejuvenation | — |
| `ResistantCells` | *Aggressotropic Conduit I–III*, Chitin Fortification, Hypersystemic Regeneration, *Hyphal Resistance Transfer*, Mimetic Resilience, *Mycelial Bastion I–III*, *Septal Alarm*, *Septal Seal*, *Surgical Inoculation* | *Hyphal Resistance Transfer* |
| `ToxinCleanup` | Mycotoxin Catabolism, *Neutralizing Mantle* | — |
| `SelfReclamation` | Catabolic Rebirth, Hypersystemic Regeneration, *Necrophoric Adaptation*, *Reclamation Rhizomorphs*, Regenerative Hyphae | Hypersystemic Regeneration, *Reclamation Rhizomorphs* |
| `CorpseCapture` | Necrohyphal Infiltration, Necrotoxic Conversion | — |
| `CorpseDenial` | Necrotic Clearance | — |
| `Composting` | Necrophytic Bloom | — |
| `ToxinPlacement` | *Ballistospore Discharge I–III*, *Jetting Mycelium I–III*, Mycotoxin Tracer, Sporicidal Bloom | *Chemotactic Mycotoxins*, Competitive Antagonism, *Enduring Toxaphores*, Mycotoxin Potentiation |
| `ToxinLongevity` | *Enduring Toxaphores*, Mycotoxin Potentiation | — |
| `ToxinMobility` | *Chemotactic Mycotoxins*, Toxinborne Seeding | — |
| `DirectKill` | *Cytolytic Burst*, Mycotoxin Potentiation, Putrefactive Cascade, Putrefactive Mycotoxin | Putrefactive Cascade |
| `LeaderFocus` | *Aggressotropic Conduit I–III*, Competitive Antagonism, *Hyphal Draw*, Mimetic Resilience | — |
| `PointIncome` | Adaptive Expression, Anabolic Inversion, Latent Polymorphism, Mycotoxin Catabolism, *Plasmid Bounty I–III* | — |
| `FreeUpgrades` | *Ascus Wager*, Hyperadaptive Drift, Mutator Phenotype | Hyperadaptive Drift |
| `TreePivot` | Ontogenic Regression | — |

## Board conditions

| Condition | Needs | Uses | Creates | Removes |
|---|---|---|---|---|
| `OwnCellDeaths` | Necrosporulation | *Necrophoric Adaptation*, *Septal Alarm* | Autolytic Surge, Filament Overdrive, Mycelial Bloom | — |
| `OwnDeadCells` | — | Catabolic Rebirth, Detrital Enzymes, Hypersystemic Regeneration, *Necrophoric Adaptation*, Necrophytic Bloom, Necrotic Clearance, *Reclamation Rhizomorphs*, Regenerative Hyphae | Autolytic Surge, Filament Overdrive | Necrophytic Bloom, Necrotic Clearance |
| `EnemyDeadCells` | — | Detrital Enzymes, Necrohyphal Infiltration, Necrophytic Bloom | — | Necrophytic Bloom |
| `EnemyCellsKilledByYou` | Necrotoxic Conversion, Putrefactive Rejuvenation | — | *Cytolytic Burst*, Mycotoxin Potentiation, Putrefactive Cascade, Putrefactive Mycotoxin | — |
| `OwnToxins` | Catabolic Rebirth, *Chemotactic Mycotoxins*, *Cytolytic Burst*, *Enduring Toxaphores*, Mycotoxin Potentiation, Necrotoxic Conversion, Toxinborne Seeding | — | *Ballistospore Discharge I–III*, *Jetting Mycelium I–III*, Mycotoxin Tracer, Sporicidal Bloom | Mycotoxin Catabolism |
| `EnemyToxins` | — | Mycotoxin Catabolism, *Neutralizing Mantle*, Toxin Margin | — | Mycotoxin Catabolism, *Neutralizing Mantle* |
| `OwnResistantCells` | *Hyphal Resistance Transfer* | — | *Aggressotropic Conduit I–III*, Chitin Fortification, Hypersystemic Regeneration, *Hyphal Resistance Transfer*, Mimetic Resilience, *Mycelial Bastion I–III*, *Septal Alarm*, *Septal Seal*, *Surgical Inoculation* | — |
| `EnemyResistantCells` | — | Mimetic Resilience | — | — |
| `EnemyContact` | — | *Cytolytic Burst*, Mycotoxin Tracer, Necrohyphal Infiltration, Putrefactive Cascade, Putrefactive Mycotoxin, Toxinborne Seeding | — | — |
| `OpenSpace` | — | Aerated Frontier, Chemotactic Beacon, Filament Overdrive | — | — |
| `CrampedSpace` | — | Compaction Pressure | — | — |
| `BoardEdge` | — | *Corner Conduit I–III*, Crustward Tropism, *Perimeter Proliferator* | — | — |
| `LargeColony` | Sporicidal Bloom | — | — | — |
| `FallingBehind` | — | Anabolic Inversion, Competitive Antagonism, Mimetic Resilience | — | — |
| `BankedPoints` | Latent Polymorphism | — | — | — |
| `NutrientPatches` | — | — | Necrophytic Bloom | — |
