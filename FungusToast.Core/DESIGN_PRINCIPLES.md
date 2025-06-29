# Fungus Toast – Technical Overview

## Project Description

**Fungus Toast** is a turn-based strategy game built in Unity where players control rival fungal colonies vying for dominance over the surface of a slice of bread. The game features:

- Grid-based territory control
- Probabilistic mycelial growth and decay
- A mutation tree system (with point upgrades)
- Toxin mechanics and cell death
- Drafting for powerful and (sometimes) unique abilities
- Simulation and AI for automated testing and balance

The core codebase is split into a **Unity** front-end and a **headless simulation engine** for fast AI-driven testing. All game logic, state changes, and events are centralized in the `FungusToast.Core` project for maintainability and extensibility.

---

## Game Flow and Phase Structure

Fungus Toast operates on a **round-based system** with distinct phases that repeat until endgame conditions are met. Each round follows a predictable sequence designed to create strategic tension and meaningful choices.

### **Round Structure**

```
Round Start
    ↓
┌─────────────────────────────────────────────────────────────┐
│                    MUTATION PHASE                           │
│  • Players spend mutation points on upgrades               │
│  • AI players use strategy-based spending                  │
│  • Auto-upgrades trigger (Mutator Phenotype, etc.)         │
│  • Human player can bank points for later                  │
└─────────────────────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────────────────────┐
│                    GROWTH PHASE                             │
│  • 5 distinct growth cycles per round                      │
│  • Each cycle: all living cells attempt to expand          │
│  • Pre-growth effects (Mycotoxin Catabolism, etc.)         │
│  • Post-growth effects (Reclaim abilities, etc.)           │
│  • Failed growth attempts tracked for decay phase          │
└─────────────────────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────────────────────┐
│                    DECAY PHASE                              │
│  • All living cells evaluated for death                     │
│  • Age-based, randomness, and mutation-based deaths        │
│  • Toxin effects and spore drops                           │
│  • Necrophytic Bloom activation (if threshold met)         │
│  • Board state updated and rendered                        │
└─────────────────────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────────────────────┐
│              OPTIONAL: MYCOVARIANT DRAFT PHASE             │
│  • Triggers on specific round (configurable)               │
│  • Players draft unique abilities                          │
│  • Draft order based on living cell count                  │
│  • Interrupts normal round flow                            │
└─────────────────────────────────────────────────────────────┘
    ↓
Round End → Check Endgame Conditions → Next Round or Game End
```

### **Phase Details**

#### **Mutation Phase**
- **Purpose**: Strategic resource allocation and progression
- **Duration**: Variable (human player controlled, AI instant)
- **Key Mechanics**:
  - Players spend earned mutation points on upgrades
  - Prerequisites must be satisfied for each mutation
  - Auto-upgrades trigger based on mutation levels
  - Human player can bank points for future rounds
  - AI players use predefined spending strategies

#### **Growth Phase**
- **Purpose**: Territory expansion and board control
- **Duration**: 5 growth cycles per round
- **Key Mechanics**:
  - Each living cell attempts to expand to adjacent tiles
  - Growth chance modified by mutations and board state
  - Failed growth attempts tracked for decay phase effects
  - Pre/post growth effects trigger mutation abilities
  - Diagonal growth mutations provide additional directions

#### **Decay Phase**
- **Purpose**: Population control and death mechanics
- **Duration**: Single evaluation per round
- **Key Mechanics**:
  - All living cells evaluated for death probability
  - Death reasons: Age, Randomness, Mutation effects
  - Toxin placement and spore effects
  - Necrophytic Bloom activation at occupancy threshold
  - Board state finalized for next round

#### **Mycovariant Draft Phase**
- **Purpose**: Strategic ability acquisition
- **Duration**: Interrupts normal round flow
- **Key Mechanics**:
  - Triggers on specific round (MycovariantGameBalance.MycovariantSelectionTriggerRound)
  - Draft order: fewest living cells first
  - Players select from unique ability pool
  - Abilities provide powerful, sometimes game-changing effects
  - Draft size configurable (MycovariantGameBalance.MycovariantSelectionDraftSize)

### **Implementation Guidelines**

#### **Phase Runner Pattern**
```csharp
// Each phase should implement this pattern:
public class PhaseRunner : MonoBehaviour
{
    public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer);
    public void StartPhase();
    private IEnumerator RunPhase();
}
```

#### **Event-Driven Architecture**
- **Pre-phase events**: Allow mutations to prepare
- **Phase events**: Core phase logic execution  
- **Post-phase events**: Cleanup and state updates
- **Observer pattern**: UI updates and analytics

#### **State Management**
- **Board state**: Centralized in GameBoard class
- **Player state**: Mutation levels, points, abilities
- **Round context**: Current round, growth cycle, phase
- **Event tracking**: Death reasons, effect counts, analytics

#### **Timing and Balance**
- **Growth cycles**: 5 per round (GameBalance.TotalGrowthCycles)
- **Draft trigger**: Configurable round (MycovariantGameBalance.MycovariantSelectionTriggerRound)
- **Endgame**: Based on board occupancy threshold
- **Phase timing**: Configurable delays for UI feedback

### **Code References**

#### **Core Phase Classes**
- `FungusToast.Unity.Phases.GrowthPhaseRunner`
- `FungusToast.Unity.Phases.DecayPhaseRunner`
- `FungusToast.Unity.UI.MycovariantDraftController`

#### **Engine Classes**
- `FungusToast.Core.Phases.TurnEngine`
- `FungusToast.Core.Phases.GrowthPhaseProcessor`
- `FungusToast.Core.Death.DeathEngine`

#### **Configuration**
- `FungusToast.Core.Config.GameBalance`
- `FungusToast.Core.Config.MycovariantGameBalance`

---

## Fungal Cell and Toxin Event Terminology

Fungus Toast uses precise terminology for all game state changes to ensure code, analytics, simulation, and UI remain unambiguous. This section defines the canonical event and method names for all key cell and toxin transitions.

### Fungal Cell Placement & Replacement

| **Action**                                   | **Event/Method Name** | **Description**                                                                        |
|----------------------------------------------|-----------------------|----------------------------------------------------------------------------------------|
| Place new living cell in **empty** tile      | `Colonize`            | A fungal cell spreads into an unoccupied (empty) tile.                                 |
| Place new living cell over **any dead cell** | `Reclaim`             | A fungal cell revives one of its own dead cells, returning it to life.                 |
| Place new living cell over **enemy living cell** | `Infest`         | A fungal cell kills an enemy's living cell, then occupies the tile with its own cell.  |

### Toxin Tile Placement & Cell Death

| **Action**                                     | **Event/Method Name** | **Description**                                                                              |
|------------------------------------------------|-----------------------|----------------------------------------------------------------------------------------------|
| Place toxin in **empty** or **dead** cell      | `Toxify`              | A toxin is introduced to an empty tile or a dead cell, contaminating the tile.               |
| Place toxin over a **living cell**             | `Poison`              | A living cell is killed and converted into a toxin tile (distinct from Toxify for clarity).  |

#### Summary Table

| **Source State**   | **Target State**      | **Event/Method** |
|--------------------|----------------------|------------------|
| Empty              | Living Cell           | Colonize         |
| Own Dead Cell      | Living Cell           | Reclaim          |
| Enemy Dead Cell    | Living Cell           | Infest           |
| Enemy Living Cell  | Living Cell           | Infest           |
| Empty / Dead Cell  | Toxin                 | Toxify           |
| Living Cell        | Toxin                 | Poison           |

**Key distinctions:**

- **Colonize** – Always for new growth in empty tiles.
- **Reclaim** – Restores a player's own dead cells.
- **Infest** – Overtakes an enemy's dead or living cell.
- **Toxify** – Places toxin in empty or dead cells.
- **Poison** – Kills a living cell and converts it to toxin.

---

## Mutation Upgrade Tree Guiding Principles

The mutation prerequisite system is crucial for game balance, encouraging strategic diversity, and preventing early access to powerful mutations. These principles ensure a healthy progression system that rewards thoughtful planning while maintaining accessibility.

### Core Principles

#### 1. Category Diversification
- **Rule**: High-tier mutations should require prerequisites from **different categories** to encourage strategic diversity
- **Rationale**: Prevents players from specializing too heavily in one category early on
- **Example**: A Tier 4 Growth mutation should require at least one non-Growth prerequisite
- **Implementation**: Tier 4+ mutations should have prerequisites spanning at least 2 different categories

#### 2. Tier Progression Limits
- **Rule**: No mutation should require more than **2 prerequisites** from the same tier
- **Rationale**: Prevents "tier rushing" where players skip entire tiers by over-investing in one tier
- **Example**: A Tier 4 mutation shouldn't require 3+ Tier 2 mutations
- **Exception**: Related mutations (like all 4 tendrils) can be treated as a single "system" requirement

#### 3. Prerequisite Depth Control
- **Rule**: No mutation should have a prerequisite chain longer than **3 levels deep**
- **Rationale**: Prevents overly complex dependency chains that create "dead end" builds
- **Example**: Avoid chains like A → B → C → D where each requires the previous
- **Implementation**: Maximum chain depth should be enforced in `MutationRepository.BuildFullMutationSet()`

#### 4. Cross-Category Synergy
- **Rule**: High-tier mutations should demonstrate **thematic synergy** between their prerequisites
- **Rationale**: Makes prerequisite choices feel meaningful and logical
- **Example**: A "Necrotoxic Conversion" mutation requiring both toxin and death-related prerequisites
- **Implementation**: Prerequisites should support the mutation's theme and mechanics

#### 5. Early Game Accessibility
- **Rule**: Tier 1 mutations should have **no prerequisites** (already followed)
- **Rule**: Tier 2 mutations should require **only Tier 1 prerequisites** (mostly followed)
- **Rationale**: Ensures players have meaningful choices from the start
- **Implementation**: All Tier 1 mutations are root mutations, Tier 2 mutations only reference Tier 1s

#### 6. Power Gating
- **Rule**: The total prerequisite level requirement should scale appropriately with mutation power
- **Rationale**: More powerful mutations should require more investment in prerequisites
- **Example**: A Tier 5 mutation might require 8+ total levels across prerequisites
- **Implementation**: Higher tiers should require higher total prerequisite levels

#### 7. Avoid Circular Dependencies
- **Rule**: No mutation should create circular prerequisite relationships
- **Rationale**: Prevents infinite loops and ensures all mutations are reachable
- **Implementation**: Prerequisite chains should form a directed acyclic graph (DAG)

### Implementation Guidelines

#### Prerequisite Level Requirements
- **Tier 2**: 5-15 total prerequisite levels
- **Tier 3**: 10-25 total prerequisite levels  
- **Tier 4**: 15-35 total prerequisite levels
- **Tier 5**: 25-50 total prerequisite levels

#### Category Distribution
- **Tier 3+**: Should require prerequisites from at least 2 different categories
- **Tier 4+**: Should require prerequisites from at least 2 different categories, with one being non-primary
- **Tier 5**: Should demonstrate clear cross-category synergy

#### Validation Rules
When adding new mutations to `MutationRepository.cs`:

1. **Check category diversity**: Ensure prerequisites span multiple categories for Tier 3+
2. **Verify tier progression**: No more than 2 prerequisites from the same tier
3. **Calculate total levels**: Ensure prerequisite levels scale with mutation tier
4. **Test reachability**: Verify the mutation is reachable from root mutations
5. **Review thematic synergy**: Prerequisites should support the mutation's theme

### Current Issues to Address

1. **Necrophytic Bloom Chain**: `Necrosporulation → Sporocidal Bloom → Necrophytic Bloom` (3-deep chain, all same category)
2. **Fungicide Specialization**: Many high-tier fungicide mutations only require other fungicide mutations
3. **Tier 5 Requirements**: Some Tier 5 mutations have very low prerequisite requirements (e.g., Hyperadaptive Drift only needs 2 levels each of Tier 1s)

### Code Examples

#### Good Prerequisite Structure
```csharp
// Tier 4: Cross-category, thematic synergy
MakeChild(new Mutation(
    id: MutationIds.RegenerativeHyphae,
    // ... mutation definition
),
new MutationPrerequisite(MutationIds.Necrosporulation, 1),     // CellularResilience
new MutationPrerequisite(MutationIds.MycotropicInduction, 1)); // Growth
```

#### Problematic Prerequisite Structure
```csharp
// Tier 4: All same category, deep chain
MakeChild(new Mutation(
    id: MutationIds.NecrophyticBloom,
    // ... mutation definition
),
new MutationPrerequisite(MutationIds.SporocidalBloom, 1)); // Requires Sporocidal Bloom → Necrosporulation chain
```

---

## Code Structure (Brief Overview)

- **FungusToast.Core** – All core logic, data models, and event systems.
- **FungusToast.Simulation** – Headless simulation framework for AI and batch analysis.
- **Unity/Assets/Scripts/Unity/** – UI and Unity-specific integrations.

All state mutations are now event-driven to support analytics, replay, and flexible UI updates. 