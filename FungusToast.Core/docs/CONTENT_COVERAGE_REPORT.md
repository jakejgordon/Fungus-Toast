# Content Coverage Report

> **Generated** by `CoverageDecisionTests` from the content tags, the player-facing strategy plans, and the recorded decisions. Do not edit by hand.
> Regenerate with `FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentProfiles`.
> How matches are scored and what each decision means: [AI_COVERAGE_DECISIONS.md](second-level/AI_COVERAGE_DECISIONS.md). Tags: [AI_CONTENT_TAGS.md](second-level/AI_CONTENT_TAGS.md).

Each row asks: *should these AI strategies use this content?* A row appears when the content scores at least 2 points against a strategy that does not already use it. Strategies with identical plans share a row; each strategy's goal sentence follows its name.

## Summary

- 61 tagged content items (Mycovariant tier families count once); 47 have at least one candidate.
- 388 candidate rows, covering 572 strategy pairings.

| Status | Rows |
|---|---|
| AddForEvaluation | 8 |
| Baseline (not reviewed) | 379 |
| NotApplicable | 1 |

## Adaptive Expression

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Gravebloom (Proven, Medium)**: A colony that uses an early metabolic boost to sustain spreading decay and regression. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign, Hard)**: Expands carefully and becomes hard to dislodge once its defenses finish taking root. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Thanatophyte (Campaign, Easy)**: Accelerates early, then leans into a sharp decay finish once it has momentum. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_NecrotoxinGauntlet_Elite`] (Campaign, Easy)**: Spreads decay pressure fast and lets poison plus regression do the finishing work. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Voltaic Rot (Campaign, Elite)**: Starts with a strong engine and uses it to fuel a grinding decay plan. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |

## Aggressotropic Conduit I–III

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Grave Bastion (Campaign, Easy)**: Prefers defensive trades, then slowly rebuilds from the wreckage. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 2 | same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Mimic Bastion (Campaign, Easy)**: Builds a defensive bloom and punishes anyone who tries to trade into it head-on. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |

## Anabolic Inversion

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign, Hard)**: Expands carefully and becomes hard to dislodge once its defenses finish taking root. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_NecrotoxinGauntlet_Elite`] (Campaign, Easy)**: Spreads decay pressure fast and lets poison plus regression do the finishing work. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |

## Ascus Wager

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Measured Mycelium (Campaign, Easy)**, **Measured Mycelium (Proven, Easy)**: A reliable, low-volatility colony built around steady mutation value and durable growth. | 2 | same job: FreeUpgrades (top) | — | Baseline: not reviewed |
| **Mutagen Bloom (Proven)**: A colony that invests heavily in adaptation before converting its enlarged growth into decay. | 2 | same job: FreeUpgrades (top) | — | Baseline: not reviewed |

## Autolytic Surge

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 3 | feeds: creates OwnCellDeaths; related job: BaseGrowth (Territory, like top RemotePlacement) | — | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign, Elite)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**Reclaimer's Ledger (Proven, Medium)**: A colony that treats dead ground as capital, reclaiming it to fuel its next spread.<br>**Spore Ledger (Proven, Medium)**: A colony that turns steady expansion and decay into a dependable resource engine. | 3 | feeds: creates OwnCellDeaths; related job: BaseGrowth (Territory, like top Repositioning) | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 3 | feeds: creates OwnCellDeaths; related job: BaseGrowth (Territory, like top Repositioning) | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 3 | feeds: creates OwnCellDeaths; same job: BaseGrowth | — | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Voltaic Rot (Campaign, Elite)**: Starts with a strong engine and uses it to fuel a grinding decay plan. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |

## Ballistospore Discharge I–III

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 4 | feeds: creates OwnToxins; same job: ToxinPlacement (top) | — | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |

## Catabolic Rebirth

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 2 | salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |

## Chemotactic Mycotoxins

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 4 | amplifies: ToxinPlacement; fed by: needs OwnToxins; related job: ToxinMobility (Offense, like top ToxinPlacement) | tension: the plan removes OwnToxins, which this relies on | Baseline: not reviewed |

## Chitin Fortification

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Resilient Canopy (Campaign, Easy)**: Keeps to a simple toolkit, grows safely, and tries to outlast sloppy attacks.<br>**Rooted Canopy (Campaign, Easy)**: A simple, sturdy colony that grows safely within a limited mutation toolkit. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |

## Competitive Antagonism

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Needle Reclaimer (Campaign, Training)**: Sneaks into weak seams, then surges once it has a foothold worth exploiting. | 2 | same job: LeaderFocus (top) | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | amplifies: ToxinPlacement; related job: LeaderFocus (Offense, like top ToxinPlacement) | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | amplifies: ToxinPlacement; related job: LeaderFocus (Offense, like top ToxinPlacement) | — | Baseline: not reviewed |

## Corner Conduit I–III

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |

## Cytolytic Burst

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 2 | feeds: creates EnemyCellsKilledByYou | shares: EnemyContact; shares: OwnToxins | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 2 | feeds: creates EnemyCellsKilledByYou | shares: EnemyContact; shares: OwnToxins | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 2 | fed by: needs OwnToxins; same job: DirectKill | shares: EnemyContact; tension: the plan removes OwnToxins, which this relies on | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 2 | fed by: needs OwnToxins; same job: DirectKill | shares: EnemyContact; tension: the plan removes OwnToxins, which this relies on | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |

## Detrital Enzymes

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Adaptive Blight (Proven)**: A colony that grows, mutates aggressively, then develops into a high-investment decay plan. | 2 | related job: ConditionalGrowth (Territory, like top Repositioning); salvages: uses EnemyDeadCells the plan creates | — | Baseline: not reviewed |
| **Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign, Training)**: Looks for sudden openings, quick repositioning, and short explosive turns instead of slow pressure. | 2 | related job: ConditionalGrowth (Territory, like top BaseGrowth); salvages: uses OwnDeadCells the plan creates | — | Baseline: not reviewed |
| **Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign, Training)**: Plays for tempo swings, using brief setup windows to launch sudden pressure. | 2 | related job: ConditionalGrowth (Territory, like top BaseGrowth); salvages: uses OwnDeadCells the plan creates | — | Baseline: not reviewed |
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 2 | related job: ConditionalGrowth (Territory, like top RemotePlacement); salvages: uses EnemyDeadCells the plan creates | — | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 2 | related job: ConditionalGrowth (Territory, like top RemotePlacement); salvages: uses OwnDeadCells the plan creates | shares: EnemyDeadCells; shares: OwnDeadCells | Baseline: not reviewed |
| **Gravebloom (Proven, Elite)**: A colony that grows a broad creeping base, then turns it into a field of collapse and renewal.<br>**The Necrotoxin Gauntlet [`TST_AI10_CreepingRegression`] (Campaign, Easy)**: Pushes decay hard and wants every poisoned foothold to spiral into collapse. | 2 | related job: ConditionalGrowth (Territory, like top Repositioning); salvages: uses EnemyDeadCells the plan creates; salvages: uses OwnDeadCells the plan creates | shares: EnemyDeadCells; shares: OwnDeadCells | Baseline: not reviewed |
| **Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign, Elite)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**Reclaimer's Ledger (Proven, Medium)**: A colony that treats dead ground as capital, reclaiming it to fuel its next spread.<br>**Spore Ledger (Proven, Medium)**: A colony that turns steady expansion and decay into a dependable resource engine. | 2 | related job: ConditionalGrowth (Territory, like top Repositioning); salvages: uses EnemyDeadCells the plan creates | shares: EnemyDeadCells | Baseline: not reviewed |
| **Hyphal Pulse (Proven)**: A colony that builds an economy and prepares Hyphal Surge as its defining active event.<br>**Pulse Runner (Campaign, Training)**: Grows first, then turns that board presence into quick tempo bursts. | 2 | related job: ConditionalGrowth (Territory, like top BaseGrowth); salvages: uses OwnDeadCells the plan creates | — | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 2 | related job: ConditionalGrowth (Territory, like top DiagonalGrowth); salvages: uses EnemyDeadCells the plan creates | shares: OwnDeadCells | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 2 | related job: ConditionalGrowth (Territory, like top DiagonalGrowth); salvages: uses EnemyDeadCells the plan creates | shares: OwnDeadCells | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 2 | related job: ConditionalGrowth (Territory, like top RemotePlacement); salvages: uses OwnDeadCells the plan creates | shares: EnemyDeadCells; shares: OwnDeadCells | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 2 | related job: ConditionalGrowth (Territory, like top DiagonalGrowth); salvages: uses OwnDeadCells the plan creates | shares: EnemyDeadCells; shares: OwnDeadCells | Baseline: not reviewed |

## Enduring Toxaphores

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 4 | amplifies: ToxinPlacement; fed by: needs OwnToxins; related job: ToxinLongevity (Offense, like top ToxinPlacement) | tension: the plan removes OwnToxins, which this relies on | Baseline: not reviewed |

## Filament Overdrive

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 4 | feeds: creates OwnCellDeaths; same job: RemotePlacement (top) | shares: OpenSpace | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 2 | same job: RemotePlacement (top) | shares: OpenSpace | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Grave Bastion (Campaign, Easy)**: Prefers defensive trades, then slowly rebuilds from the wreckage. | 2 | amplifies: DiagonalGrowth | — | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign, Elite)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**Reclaimer's Ledger (Proven, Medium)**: A colony that treats dead ground as capital, reclaiming it to fuel its next spread.<br>**Spore Ledger (Proven, Medium)**: A colony that turns steady expansion and decay into a dependable resource engine. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Measured Mycelium (Campaign, Easy)**, **Measured Mycelium (Proven, Easy)**: A reliable, low-volatility colony built around steady mutation value and durable growth. | 3 | amplifies: DiagonalGrowth; same job: DiagonalGrowth | — | Baseline: not reviewed |
| **Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 4 | amplifies: DiagonalGrowth; same job: DiagonalGrowth (top) | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |
| **Overextender [`CMP_Mobility_Overextender_Training_Offset2`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training_Offset3`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 4 | amplifies: DiagonalGrowth; same job: DiagonalGrowth (top) | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |
| **Pressure Bloom (Campaign, Easy)**: Pushes outward constantly and would rather suffocate space than wait. | 3 | amplifies: DiagonalGrowth; same job: RemotePlacement | — | Baseline: not reviewed |
| **Pulsar Sprout (Campaign, Easy)**: Waits for timing windows, then spends hard in short bursts to catch opponents off balance. | 2 | amplifies: DiagonalGrowth | — | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 4 | amplifies: DiagonalGrowth; same job: DiagonalGrowth (top) | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 4 | amplifies: DiagonalGrowth; same job: DiagonalGrowth (top) | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games. | 3 | amplifies: DiagonalGrowth; same job: DiagonalGrowth | — | Baseline: not reviewed |
| **Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 3 | amplifies: DiagonalGrowth; same job: DiagonalGrowth | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 2 | same job: RemotePlacement (top) | shares: OpenSpace | Baseline: not reviewed |
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 4 | amplifies: DiagonalGrowth; feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 5 | amplifies: DiagonalGrowth; feeds: creates OwnCellDeaths; same job: DiagonalGrowth; same job: RemotePlacement | shares: OpenSpace | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | amplifies: DiagonalGrowth | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 3 | amplifies: DiagonalGrowth; same job: DiagonalGrowth | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Voltaic Rot (Campaign, Elite)**: Starts with a strong engine and uses it to fuel a grinding decay plan. | 3 | feeds: creates OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 4 | amplifies: DiagonalGrowth; same job: DiagonalGrowth (top) | — | **AddForEvaluation**: Filament Overdrive is the newest growth mutation and belongs in the most growth-oriented strategies (Jake). Its prerequisites (Creeping Mold 3, Autolytic Surge 1, Aerated Frontier 5) pull Substrate Ecology in with it; the 2026-08-30 call that keeping Putrid Tendrils and Wildfire Bloom off it avoided an unprofitable detour must be revisited, not ignored. Follow-up: WORKLOG: Bring Filament Overdrive and Substrate Ecology into growth AI builds. (Jake, 2026-10-07) |

## Hyperadaptive Drift

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 3 | amplifies: FreeUpgrades; related job: FreeUpgrades (Economy, like top PointIncome) | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 3 | amplifies: FreeUpgrades; related job: FreeUpgrades (Economy, like top PointIncome) | — | Baseline: not reviewed |
| **Measured Mycelium (Campaign, Easy)**, **Measured Mycelium (Proven, Easy)**: A reliable, low-volatility colony built around steady mutation value and durable growth. | 4 | amplifies: FreeUpgrades; same job: FreeUpgrades (top) | — | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | amplifies: FreeUpgrades | — | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 3 | amplifies: FreeUpgrades; related job: FreeUpgrades (Economy, like top PointIncome) | — | Baseline: not reviewed |
| **Thanatophyte (Campaign, Easy)**: Accelerates early, then leans into a sharp decay finish once it has momentum. | 3 | amplifies: FreeUpgrades; related job: FreeUpgrades (Economy, like top PointIncome) | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 3 | amplifies: FreeUpgrades; related job: FreeUpgrades (Economy, like top PointIncome) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 3 | amplifies: FreeUpgrades; related job: FreeUpgrades (Economy, like top PointIncome) | — | Baseline: not reviewed |
| **Voltaic Rot (Campaign, Elite)**: Starts with a strong engine and uses it to fuel a grinding decay plan. | 3 | amplifies: FreeUpgrades; related job: FreeUpgrades (Economy, like top PointIncome) | — | Baseline: not reviewed |

## Hypersystemic Regeneration

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | amplifies: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 4 | amplifies: SelfReclamation; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 5 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Grave Bastion (Campaign, Easy)**: Prefers defensive trades, then slowly rebuilds from the wreckage. | 6 | amplifies: SelfReclamation; feeds: creates OwnResistantCells; same job: ResistantCells (top) | tension: the plan removes OwnDeadCells, which this relies on | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 4 | amplifies: SelfReclamation; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 2 | amplifies: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign, Hard)**: Expands carefully and becomes hard to dislodge once its defenses finish taking root. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 4 | amplifies: SelfReclamation; same job: ResistantCells (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Mimic Bastion (Campaign, Easy)**: Builds a defensive bloom and punishes anyone who tries to trade into it head-on. | 6 | amplifies: SelfReclamation; feeds: creates OwnResistantCells; same job: ResistantCells (top) | tension: the plan removes OwnDeadCells, which this relies on | Baseline: not reviewed |
| **Needle Reclaimer (Campaign, Training)**: Sneaks into weak seams, then surges once it has a foothold worth exploiting. | 3 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells | Baseline: not reviewed |
| **Pressure Bloom (Campaign, Easy)**: Pushes outward constantly and would rather suffocate space than wait. | 2 | amplifies: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Pulsar Sprout (Campaign, Easy)**: Waits for timing windows, then spends hard in short bursts to catch opponents off balance. | 5 | feeds: creates OwnResistantCells; salvages: uses OwnDeadCells the plan creates; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 4 | amplifies: SelfReclamation; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Resilient Canopy (Campaign, Easy)**: Keeps to a simple toolkit, grows safely, and tries to outlast sloppy attacks.<br>**Rooted Canopy (Campaign, Easy)**: A simple, sturdy colony that grows safely within a limited mutation toolkit. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 6 | amplifies: SelfReclamation; feeds: creates OwnResistantCells; same job: ResistantCells (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 4 | feeds: creates OwnResistantCells; salvages: uses OwnDeadCells the plan creates; same job: ResistantCells | tension: the plan removes OwnDeadCells, which this relies on | Baseline: not reviewed |
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 4 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 2 | amplifies: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 5 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 3 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 4 | amplifies: SelfReclamation; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 4 | feeds: creates OwnResistantCells; salvages: uses OwnDeadCells the plan creates; same job: ResistantCells | tension: the plan removes OwnDeadCells, which this relies on | Baseline: not reviewed |

## Hyphal Draw

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Adaptive Blight (Proven)**: A colony that grows, mutates aggressively, then develops into a high-investment decay plan. | 2 | same job: Repositioning (top) | — | Baseline: not reviewed |
| **Gravebloom (Proven, Elite)**: A colony that grows a broad creeping base, then turns it into a field of collapse and renewal.<br>**The Necrotoxin Gauntlet [`TST_AI10_CreepingRegression`] (Campaign, Easy)**: Pushes decay hard and wants every poisoned foothold to spiral into collapse. | 2 | same job: Repositioning (top) | — | Baseline: not reviewed |
| **Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign, Elite)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**Reclaimer's Ledger (Proven, Medium)**: A colony that treats dead ground as capital, reclaiming it to fuel its next spread.<br>**Spore Ledger (Proven, Medium)**: A colony that turns steady expansion and decay into a dependable resource engine. | 2 | same job: Repositioning (top) | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 2 | same job: Repositioning (top) | — | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | same job: Repositioning (top) | — | Baseline: not reviewed |

## Hyphal Resistance Transfer

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure. | 5 | amplifies: ResistantCells; fed by: needs OwnResistantCells; same job: ResistantCells (top) | — | **NotApplicable**: This Iron Shell is an entry-level campaign opponent kept deliberately weak to demonstrate Resistance. The transfer would fit its theme but make it stronger than its teaching role allows (Jake). (Jake, 2026-10-07) |
| **Needle Reclaimer (Campaign, Training)**: Sneaks into weak seams, then surges once it has a foothold worth exploiting. | 3 | amplifies: ResistantCells; fed by: needs OwnResistantCells | — | Baseline: not reviewed |
| **Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 3 | amplifies: ResistantCells; fed by: needs OwnResistantCells | — | Baseline: not reviewed |
| **Pressure Bloom (Campaign, Easy)**: Pushes outward constantly and would rather suffocate space than wait. | 3 | amplifies: ResistantCells; fed by: needs OwnResistantCells | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 3 | amplifies: ResistantCells; fed by: needs OwnResistantCells | — | Baseline: not reviewed |
| **Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 5 | amplifies: ResistantCells; fed by: needs OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 3 | amplifies: ResistantCells; fed by: needs OwnResistantCells | — | Baseline: not reviewed |

## Jetting Mycelium I–III

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 3 | feeds: creates OwnToxins; same job: RemotePlacement | — | Baseline: not reviewed |
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 3 | feeds: creates OwnToxins; same job: RemotePlacement | — | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 3 | feeds: creates OwnToxins; same job: RemotePlacement | — | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 3 | feeds: creates OwnToxins; same job: RemotePlacement | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 3 | feeds: creates OwnToxins; same job: RemotePlacement | — | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 3 | feeds: creates OwnToxins; related job: RemotePlacement (Territory, like top DiagonalGrowth) | — | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 3 | feeds: creates OwnToxins; related job: RemotePlacement (Territory, like top DiagonalGrowth) | — | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 3 | feeds: creates OwnToxins; same job: RemotePlacement | — | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 3 | feeds: creates OwnToxins; related job: RemotePlacement (Territory, like top Repositioning) | — | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 4 | feeds: creates OwnToxins; same job: ToxinPlacement (top) | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 4 | feeds: creates OwnToxins; same job: ToxinPlacement (top) | — | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 3 | feeds: creates OwnToxins; same job: RemotePlacement | — | Baseline: not reviewed |

## Latent Polymorphism

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Gravebloom (Proven, Medium)**: A colony that uses an early metabolic boost to sustain spreading decay and regression. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign, Hard)**: Expands carefully and becomes hard to dislodge once its defenses finish taking root. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Thanatophyte (Campaign, Easy)**: Accelerates early, then leans into a sharp decay finish once it has momentum. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_NecrotoxinGauntlet_Elite`] (Campaign, Easy)**: Spreads decay pressure fast and lets poison plus regression do the finishing work. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Voltaic Rot (Campaign, Elite)**: Starts with a strong engine and uses it to fuel a grinding decay plan. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |

## Mimetic Resilience

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Grave Bastion (Campaign, Easy)**: Prefers defensive trades, then slowly rebuilds from the wreckage. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 2 | same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Needle Reclaimer (Campaign, Training)**: Sneaks into weak seams, then surges once it has a foothold worth exploiting. | 2 | same job: LeaderFocus (top) | — | Baseline: not reviewed |
| **Pulsar Sprout (Campaign, Easy)**: Waits for timing windows, then spends hard in short bursts to catch opponents off balance. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Canopy (Campaign, Easy)**: Keeps to a simple toolkit, grows safely, and tries to outlast sloppy attacks.<br>**Rooted Canopy (Campaign, Easy)**: A simple, sturdy colony that grows safely within a limited mutation toolkit. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 3 | feeds: creates OwnResistantCells; same job: LeaderFocus; same job: ResistantCells | — | Baseline: not reviewed |

## Mutator Phenotype

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Mutagen Bloom (Proven)**: A colony that invests heavily in adaptation before converting its enlarged growth into decay. | 2 | same job: FreeUpgrades (top) | — | Baseline: not reviewed |

## Mycelial Bastion I–III

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 2 | same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |

## Mycelial Bloom

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign, Training)**: Looks for sudden openings, quick repositioning, and short explosive turns instead of slow pressure. | 2 | same job: BaseGrowth (top) | — | Baseline: not reviewed |
| **Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign, Training)**: Plays for tempo swings, using brief setup windows to launch sudden pressure. | 2 | same job: BaseGrowth (top) | — | Baseline: not reviewed |
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 3 | feeds: creates OwnCellDeaths; related job: BaseGrowth (Territory, like top RemotePlacement) | — | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign, Elite)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**Reclaimer's Ledger (Proven, Medium)**: A colony that treats dead ground as capital, reclaiming it to fuel its next spread.<br>**Spore Ledger (Proven, Medium)**: A colony that turns steady expansion and decay into a dependable resource engine. | 3 | feeds: creates OwnCellDeaths; related job: BaseGrowth (Territory, like top Repositioning) | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 3 | feeds: creates OwnCellDeaths; related job: BaseGrowth (Territory, like top Repositioning) | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Hyphal Pulse (Proven)**: A colony that builds an economy and prepares Hyphal Surge as its defining active event.<br>**Pulse Runner (Campaign, Training)**: Grows first, then turns that board presence into quick tempo bursts. | 2 | same job: BaseGrowth (top) | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |
| **Voltaic Rot (Campaign, Elite)**: Starts with a strong engine and uses it to fuel a grinding decay plan. | 2 | feeds: creates OwnCellDeaths | — | Baseline: not reviewed |

## Mycotoxin Catabolism

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Gravebloom (Proven, Medium)**: A colony that uses an early metabolic boost to sustain spreading decay and regression. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_IronShell_Elite`] (Campaign, Hard)**: Expands carefully and becomes hard to dislodge once its defenses finish taking root. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Tempo Harvester (Campaign, Medium)**: Stays efficient, then turns small advantages into a steady reclaim snowball. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Thanatophyte (Campaign, Easy)**: Accelerates early, then leans into a sharp decay finish once it has momentum. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_NecrotoxinGauntlet_Elite`] (Campaign, Easy)**: Spreads decay pressure fast and lets poison plus regression do the finishing work. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Voltaic Rot (Campaign, Elite)**: Starts with a strong engine and uses it to fuel a grinding decay plan. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |

## Mycotoxin Potentiation

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | shares: OwnToxins | Baseline: not reviewed |

## Mycotoxin Tracer

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 2 | feeds: creates OwnToxins | shares: EnemyContact | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 2 | feeds: creates OwnToxins | shares: EnemyContact | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |

## Mycotropic Induction

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Gravebloom (Proven, Medium)**: A colony that uses an early metabolic boost to sustain spreading decay and regression. | 2 | amplifies: DiagonalGrowth | — | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | amplifies: DiagonalGrowth | — | Baseline: not reviewed |
| **Gravebloom (Proven, Elite)**: A colony that grows a broad creeping base, then turns it into a field of collapse and renewal.<br>**The Necrotoxin Gauntlet [`TST_AI10_CreepingRegression`] (Campaign, Easy)**: Pushes decay hard and wants every poisoned foothold to spiral into collapse. | 3 | amplifies: DiagonalGrowth; same job: DiagonalGrowth | — | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 4 | amplifies: DiagonalGrowth; same job: DiagonalGrowth (top) | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | amplifies: DiagonalGrowth | — | Baseline: not reviewed |
| **The Necrotoxin Gauntlet [`CMP_Bloom_CreepingRegression_Elite`] (Campaign, Medium)**: Pushes creeping decay from every angle and lets the board collapse behind it. | 2 | amplifies: DiagonalGrowth | — | Baseline: not reviewed |

## Necrophoric Adaptation

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | same job: SelfReclamation (top) | shares: OwnCellDeaths; shares: OwnDeadCells | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 3 | salvages: uses OwnCellDeaths the plan creates; salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation (top) | shares: OwnCellDeaths; shares: OwnDeadCells | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 2 | same job: SelfReclamation (top) | shares: OwnCellDeaths; shares: OwnDeadCells | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 3 | salvages: uses OwnCellDeaths the plan creates; salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation (top) | shares: OwnCellDeaths; shares: OwnDeadCells | Baseline: not reviewed |

## Necrophytic Bloom

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | related job: Composting (Death and corpses, like top SelfReclamation); salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells; tension: removes OwnDeadCells, which the plan relies on | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | related job: Composting (Death and corpses, like top SelfReclamation); salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells; tension: removes OwnDeadCells, which the plan relies on | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | related job: Composting (Death and corpses, like top SelfReclamation); salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells; tension: removes OwnDeadCells, which the plan relies on | Baseline: not reviewed |

## Necrosporulation

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Beacon Sprinter [`CMP_Surge_BeaconSprinter_Medium`] (Campaign, Training)**: Looks for sudden openings, quick repositioning, and short explosive turns instead of slow pressure. | 2 | fed by: needs OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Beacon Sprinter [`CMP_Surge_BeaconTempo_Medium`] (Campaign, Training)**: Plays for tempo swings, using brief setup windows to launch sudden pressure. | 2 | fed by: needs OwnCellDeaths; same job: RemotePlacement | — | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 3 | fed by: needs OwnCellDeaths; same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Gravebloom (Proven, Elite)**: A colony that grows a broad creeping base, then turns it into a field of collapse and renewal.<br>**The Necrotoxin Gauntlet [`TST_AI10_CreepingRegression`] (Campaign, Easy)**: Pushes decay hard and wants every poisoned foothold to spiral into collapse. | 2 | fed by: needs OwnCellDeaths; related job: RemotePlacement (Territory, like top Repositioning) | — | Baseline: not reviewed |
| **Hyphal Pulse (Proven)**: A colony that builds an economy and prepares Hyphal Surge as its defining active event.<br>**Pulse Runner (Campaign, Training)**: Grows first, then turns that board presence into quick tempo bursts. | 2 | fed by: needs OwnCellDeaths; related job: RemotePlacement (Territory, like top BaseGrowth) | — | Baseline: not reviewed |
| **Needle Reclaimer (Campaign, Training)**: Sneaks into weak seams, then surges once it has a foothold worth exploiting. | 2 | fed by: needs OwnCellDeaths; same job: RemotePlacement | shares: OwnCellDeaths | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 3 | fed by: needs OwnCellDeaths; same job: RemotePlacement (top) | shares: OwnCellDeaths | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 2 | fed by: needs OwnCellDeaths; related job: RemotePlacement (Territory, like top DiagonalGrowth) | shares: OwnCellDeaths | Baseline: not reviewed |

## Necrotic Clearance

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | related job: CorpseDenial (Death and corpses, like top SelfReclamation); salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells; tension: removes OwnDeadCells, which the plan relies on | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | related job: CorpseDenial (Death and corpses, like top SelfReclamation); salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells; tension: removes OwnDeadCells, which the plan relies on | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | related job: CorpseDenial (Death and corpses, like top SelfReclamation); salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells; tension: removes OwnDeadCells, which the plan relies on | Baseline: not reviewed |

## Plasmid Bounty I–III

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Gravebloom (Proven, Medium)**: A colony that uses an early metabolic boost to sustain spreading decay and regression. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | same job: PointIncome (top) | — | Baseline: not reviewed |

## Putrefactive Cascade

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Harvest Broker [`CMP_Economy_KillReclaim_Medium`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset1`] (Campaign, Hard)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset3`] (Campaign, Elite)**, **Harvest Broker [`TST_Campaign7_KillReclaim_Offset8`] (Campaign, Hard)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**The Economancer [`CMP_Economy_Economancer_Elite`] (Campaign, Elite)**: Builds up safely, hoards mutation economy, and turns that stockpile into a brutal late swing. | 2 | amplifies: DirectKill | shares: EnemyContact | Baseline: not reviewed |
| **Harvest Broker [`TST_Campaign7_KillReclaim_Offset2`] (Campaign, Elite)**: Builds value carefully, then cashes it in by picking fights and reclaiming the aftermath.<br>**Reclaimer's Ledger (Proven, Medium)**: A colony that treats dead ground as capital, reclaiming it to fuel its next spread.<br>**Spore Ledger (Proven, Medium)**: A colony that turns steady expansion and decay into a dependable resource engine. | 3 | amplifies: DirectKill; same job: DirectKill | shares: EnemyContact | Baseline: not reviewed |
| **Hoardspore Regent [`CMP_Economy_HoardsporeRegent_Elite`] (Campaign, Hard)**: Stays patient early, values long-term setup, and becomes much scarier once its engine is running. | 2 | amplifies: DirectKill | shares: EnemyContact | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | amplifies: DirectKill; same job: DirectKill | shares: EnemyContact | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | amplifies: DirectKill; same job: DirectKill | shares: EnemyContact | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |

## Putrefactive Mycotoxin

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 2 | feeds: creates EnemyCellsKilledByYou | shares: EnemyContact | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 2 | feeds: creates EnemyCellsKilledByYou | shares: EnemyContact | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates EnemyCellsKilledByYou | — | Baseline: not reviewed |

## Putrefactive Rejuvenation

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 2 | fed by: needs EnemyCellsKilledByYou; same job: DecayResistance | — | Baseline: not reviewed |

## Reclamation Rhizomorphs

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | amplifies: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 4 | amplifies: SelfReclamation; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 5 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 4 | amplifies: SelfReclamation; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 2 | amplifies: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Pressure Bloom (Campaign, Easy)**: Pushes outward constantly and would rather suffocate space than wait. | 2 | amplifies: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 5 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 3 | amplifies: SelfReclamation; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 3 | amplifies: SelfReclamation; salvages: uses OwnDeadCells the plan creates | shares: OwnDeadCells | Baseline: not reviewed |

## Regenerative Hyphae

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Scavenger Court (Campaign, Medium)**: Thrives on messy boards, reclaiming dead ground and stealing back lost space. | 2 | salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation | shares: OwnDeadCells | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 3 | salvages: uses OwnDeadCells the plan creates; same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 2 | same job: SelfReclamation (top) | shares: OwnDeadCells | Baseline: not reviewed |

## Septal Alarm

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 2 | same job: ResistantCells (top) | — | Baseline: not reviewed |

## Septal Seal

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Grave Bastion (Campaign, Easy)**: Prefers defensive trades, then slowly rebuilds from the wreckage. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 2 | same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Mimic Bastion (Campaign, Easy)**: Builds a defensive bloom and punishes anyone who tries to trade into it head-on. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Pulsar Sprout (Campaign, Easy)**: Waits for timing windows, then spends hard in short bursts to catch opponents off balance. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Canopy (Campaign, Easy)**: Keeps to a simple toolkit, grows safely, and tries to outlast sloppy attacks.<br>**Rooted Canopy (Campaign, Easy)**: A simple, sturdy colony that grows safely within a limited mutation toolkit. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |

## Sporicidal Bloom

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Anabolic Regent (Proven, Hard/Elite)**: A metabolic-first control colony that converts early efficiency into durable board presence.<br>**Anabolic Steward (Proven, Hard)**: A colony that establishes a strong metabolic base before balancing spread, decay, and recovery.<br>**Hoardspore Regent [`AI13`] (Campaign, Elite)**: A resource-rich colony that builds its metabolic base before sustaining a full control lifecycle.<br>**Voltaic Bloom [`CMP_Control_AnabolicFirst_Hard`] (Campaign, Elite)**: Accelerates first, then pivots into flexible board control once it has breathing room. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Creeping Reclaimer (Campaign, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Creeping Reclaimer (Proven, Medium)**: A persistent colony that spreads first, then makes collapse feed its renewal. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Gravebloom (Campaign, Medium)**: Likes slow spreading decay that keeps paying off after the first contact. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Hoarded Bloom (Proven, Hard)**: A patient colony that accumulates mutation potential before expanding into a full control plan.<br>**Hoardspore Regent (Proven, Hard/Elite)**: A resource-rich control colony that lets a deep stockpile support its whole lifecycle. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Putrid Tendrils (Campaign, Elite)**: Reaches outward with aggressive growth lanes and tries to crowd territory early. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Putrid Tendrils (Proven)**: A tendril-driven colony that establishes multiple growth lanes before converting them into decay. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rebirth Conductor (Campaign, Elite)**: Uses steady mutation growth and rebirth loops to stay in control of long games.<br>**Rebirth Furnace (Campaign, Elite)**: Welcomes messy fights and tries to turn every loss into fresh growth.<br>**Rebirth Furnace (Proven)**: A colony that uses metabolic growth to keep its death-and-regrowth cycle burning. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Regrowth Lattice (Proven, Hard)**: A resilient filament network that rebuilds through loss and becomes harder to exhaust over time. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rejuvenation Engine (Campaign, Medium)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Rejuvenation Engine (Proven, Hard)**: A colony that develops a deep economy before repeatedly renewing its territory. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **The Economancer [`CMP_Economy_LateSpike_Hard`] (Campaign, Medium)**: Plays a patient economy game, then turns saved resources into a nasty late spike. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 4 | feeds: creates OwnToxins; same job: ToxinPlacement (top) | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 4 | feeds: creates OwnToxins; same job: ToxinPlacement (top) | — | Baseline: not reviewed |
| **Verdant Reclaimer (Campaign, Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Verdant Reclaimer (Proven, Hard/Elite)**: Builds a deep growth engine, then reclaims territory after the board breaks open. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |
| **Voltaic Bloom [`AI12`] (Campaign, Elite)**: A colony that accelerates its metabolism first, then maintains flexible growth, decay, and recovery. | 2 | feeds: creates OwnToxins | — | Baseline: not reviewed |

## Surgical Inoculation

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Grave Bastion (Campaign, Easy)**: Prefers defensive trades, then slowly rebuilds from the wreckage. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Iron Shell [`CMP_Defense_ResilientShell_Easy`] (Campaign, Training)**: Builds a sturdy shell first and only pushes once the core feels secure.<br>**Resilient Mycelium [`TST_Training_ResilientMycelium_Offset3`] (Campaign, Training)**, **Resilient Mycelium [`TST_Training_ResilientMycelium`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 2 | same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Mimic Bastion (Campaign, Easy)**: Builds a defensive bloom and punishes anyone who tries to trade into it head-on. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Pulsar Sprout (Campaign, Easy)**: Waits for timing windows, then spends hard in short bursts to catch opponents off balance. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Canopy (Campaign, Easy)**: Keeps to a simple toolkit, grows safely, and tries to outlast sloppy attacks.<br>**Rooted Canopy (Campaign, Easy)**: A simple, sturdy colony that grows safely within a limited mutation toolkit. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Resilient Mycelium [`TST_Training_ResilientMycelium_Offset1`] (Campaign, Training)**: Grows methodically, values safety, and rarely gives up space for free. | 4 | feeds: creates OwnResistantCells; same job: ResistantCells (top) | — | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 4 | feeds: creates OwnResistantCells; same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |
| **Wildfire Bloom (Campaign, Easy)**: Overgrows wide lanes quickly and keeps pressing until the board feels smothered. | 3 | feeds: creates OwnResistantCells; same job: ResistantCells | — | Baseline: not reviewed |

## Tendril Northeast

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 2 | same job: DiagonalGrowth (top) | — | Baseline: not reviewed |
| **Overextender [`CMP_Mobility_Overextender_Training_Offset2`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training_Offset3`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 2 | same job: DiagonalGrowth (top) | — | Baseline: not reviewed |

## Tendril Southeast

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 2 | same job: DiagonalGrowth (top) | — | Baseline: not reviewed |
| **Overextender [`CMP_Mobility_Overextender_Training_Offset2`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training_Offset3`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 2 | same job: DiagonalGrowth (top) | — | Baseline: not reviewed |

## Tendril Southwest

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Overextender [`CMP_Mobility_Overextender_Training_Offset1`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 2 | same job: DiagonalGrowth (top) | — | Baseline: not reviewed |
| **Overextender [`CMP_Mobility_Overextender_Training_Offset2`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training_Offset3`] (Campaign, Training)**, **Overextender [`CMP_Mobility_Overextender_Training`] (Campaign, Training)**: Spends itself thin chasing space and can be baited into reaching too far. | 2 | same job: DiagonalGrowth (top) | — | Baseline: not reviewed |

## Toxinborne Seeding

| Strategies | Score | Reasons | Context | Decision |
|---|---|---|---|---|
| **Beacon of Rot (Campaign, Hard)**: Uses guided pressure to open cracks, then deepens them with collapse effects. | 2 | same job: RemotePlacement (top) | shares: EnemyContact | Baseline: not reviewed |
| **Beacon of Rot (Proven, Medium)**: A colony that builds a bloom, establishes a Chemotactic Beacon, then intensifies decay around its reach.<br>**Rhizolith (Campaign, Easy)**: Plants stubborn anchors, then uses guided pressure to squeeze whole lanes shut. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Rhizolith Crown (Campaign, Training)**: Builds a stubborn core, then projects pressure outward from carefully held anchors. | 2 | same job: RemotePlacement (top) | — | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset1`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 2 | fed by: needs OwnToxins; related job: ToxinMobility (Offense, like top ToxinPlacement) | shares: EnemyContact; tension: the plan removes OwnToxins, which this relies on | Baseline: not reviewed |
| **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training_Offset2`] (Campaign, Training)**, **Toxic Turtle [`CMP_Attrition_ToxicTurtle_Training`] (Campaign, Training)**: Holds ground and chips away with stubborn poison pressure instead of racing. | 2 | fed by: needs OwnToxins; related job: ToxinMobility (Offense, like top ToxinPlacement) | shares: EnemyContact; tension: the plan removes OwnToxins, which this relies on | Baseline: not reviewed |

## Content with no candidates

Either already in every plan it fits, or a weak fit for every current strategy: Aerated Frontier, Chemotactic Beacon, Chronoresilient Cytoplasm, Compaction Pressure, Creeping Mold, Crustward Tropism, Homeostatic Harmony, Necrohyphal Infiltration, Necrotoxic Conversion, Neutralizing Mantle, Ontogenic Regression, Perimeter Proliferator, Tendril Northwest, Toxin Margin.
