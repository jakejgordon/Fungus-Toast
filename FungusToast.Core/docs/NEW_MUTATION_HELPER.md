# NEW_MUTATION_HELPER.md

> **🤖 AI Assistant Guide**: Step-by-step implementation pattern for adding new mutations to Fungus Toast

## Overview

This guide provides a systematic approach for adding new mutations to Fungus Toast. Follow these steps in order to ensure proper integration with the game system, simulation tracking, and UI display.

---

## Implementation Checklist

### **1. Core Infrastructure (Required)**

#### **A. Add Mutation ID**
```csharp
// File: FungusToast.Core/Mutations/MutationIds.cs
public const int NewMutationName = 29; // Use next available ID
```

#### **B. Add Mutation Type (if new)**
```csharp
// File: FungusToast.Core/Mutations/MutationTypeEnum.cs
public enum MutationType
{
    // ... existing types ...
    NewMutationType,
}
```

#### **C. Add GameBalance Constants**
```csharp
// File: FungusToast.Core/Config/GameBalance.cs

// For standard mutations:
public const float NewMutationEffectPerLevel = 0.05f;
public const int NewMutationMaxLevel = 5;

// For surge mutations (add all three):
public const int NewSurgePointsPerActivation = 8;
public const int NewSurgeDuration = 4;
public const int NewSurgePointIncreasePerLevel = 2;
public const int NewSurgeMaxLevel = 3;
```

### **2. Mutation Definition**

#### **A. Add to MutationRepository**
```csharp
// File: FungusToast.Core/Mutations/MutationRepository.cs
// Add in appropriate tier section:

MakeChild(new Mutation(
    id: MutationIds.NewMutationName,
    name: "New Mutation Name",
    description: $"Each level grants...",
    flavorText: "Scientific flavor text...",
    type: MutationType.NewMutationType,
    effectPerLevel: GameBalance.NewMutationEffectPerLevel,
    pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
    maxLevel: GameBalance.NewMutationMaxLevel,
    category: MutationCategory.AppropriateCategory,
    tier: MutationTier.Tier3,
    // For surge mutations only:
    isSurge: true,
    surgeDuration: GameBalance.NewSurgeDuration,
    pointsPerActivation: GameBalance.NewSurgePointsPerActivation,
    pointIncreasePerLevel: GameBalance.NewSurgePointIncreasePerLevel
),
new MutationPrerequisite(MutationIds.PrereqMutation1, 1),
new MutationPrerequisite(MutationIds.PrereqMutation2, 3)); // Cross-category recommended for Tier 3+
```

### **3. UI Integration**

#### **A. Add to Mutation Tree Layout**
```csharp
// File: Assets/Scripts/Unity/UI/MutationTree/UI_MutationLayoutProvider.cs
// Add to appropriate category column:

{ MutationIds.NewMutationName, new MutationLayoutMetadata(4, 3, MutationCategory.MycelialSurges) },
```

### **4. Effect Processing Logic**

#### **A. Add Processing Method**
```csharp
// File: FungusToast.Core/Phases/[CategoryName]MutationProcessor.cs
// Example for surge mutations:

public static void OnPostGrowthPhase_NewMutation(
    GameBoard board,
    List<Player> players,
    Random rng,
    ISimulationObserver observer)
{
    foreach (var player in players)
    {
        int level = player.GetMutationLevel(MutationIds.NewMutationName);
        if (level <= 0 || !player.IsSurgeActive(MutationIds.NewMutationName))
            continue;

        // Implementation logic here...
        
        // Track effects
        observer.RecordNewMutationEffect(player.PlayerId, effectCount);
    }
}
```

#### **B. Add to Coordination**
```csharp
// File: FungusToast.Core/Phases/MutationEffectCoordinator.cs
// Add to appropriate phase method:

public static void OnPostGrowthPhase(...)
{
    // ... existing calls ...
    CategoryMutationProcessor.OnPostGrowthPhase_NewMutation(board, players, rng, observer);
}
```

### **5. Simulation Tracking (Required for Analytics)**

#### **A. Add Observer Method**
```csharp
// File: FungusToast.Core/Metrics/ISimulationObserver.cs
void RecordNewMutationEffect(int playerId, int effectCount);
```

#### **B. Implement Tracking**
```csharp
// File: FungusToast.Simulation/Models/SimulationTrackingContext.cs
private readonly Dictionary<int, int> newMutationEffects = new();

public void RecordNewMutationEffect(int playerId, int effectCount)
{
    if (!newMutationEffects.ContainsKey(playerId))
        newMutationEffects[playerId] = 0;
    newMutationEffects[playerId] += effectCount;
}

public int GetNewMutationEffects(int playerId)
    => newMutationEffects.TryGetValue(playerId, out var val) ? val : 0;
```

#### **C. Add to PlayerResult**
```csharp
// File: FungusToast.Simulation/Models/PlayerResult.cs
public int NewMutationEffectCount { get; set; }
```

#### **D. Populate in GameResult**
```csharp
// File: FungusToast.Simulation/Models/GameResult.cs
// In the GameResult.From() method:
NewMutationEffectCount = tracking.GetNewMutationEffects(player.PlayerId),
```

#### **E. Add to Usage Tracker**
```csharp
// File: FungusToast.Simulation/Analysis/PlayerMutationUsageTracker.cs
// In GetMutationEffects() method:

case MutationIds.NewMutationName:
    if (player.NewMutationEffectCount > 0)
        effects["Effect Name"] = player.NewMutationEffectCount;
    break;

// For mutations with multiple effect types:
case MutationIds.MimeticResilience:
    if (player.MimeticResilienceInfestations > 0)
        effects["Mimetic Infestations"] = player.MimeticResilienceInfestations;
    if (player.MimeticResilienceDrops > 0)
        effects["Mimetic Drops"] = player.MimeticResilienceDrops;
    break;
```

### **6. Optional: Add GrowthSource (if creates cells)**
```csharp
// File: FungusToast.Core/Growth/GrowthSource.cs
/// <summary>
/// Description of new growth source
/// </summary>
NewMutationGrowth,
```

---

## Verification Steps

1. **Build Test**: Run `run_build` to ensure no compilation errors
2. **UI Test**: Check mutation appears in tree with correct prerequisites
3. **Simulation Test**: Run simulation to verify tracking works
4. **Effect Test**: Activate mutation in-game to confirm logic works

---

## Common Patterns

### **Standard Mutations**
- Most mutations provide passive effects based on level
- Effect calculated as `level * effectPerLevel`
- Processed during appropriate phase (pre/post growth, decay, etc.)

### **Surge Mutations**
- Temporary activated abilities with escalating costs
- Must check `player.IsSurgeActive(mutationId)` before processing
- Cannot upgrade while active
- Use `isSurge: true` and set surge-specific properties

### **Cross-Category Prerequisites**
- Tier 3+ mutations should require prerequisites from multiple categories
- Encourages strategic diversity and prevents over-specialization
- Creates thematic synergy between mutation systems

### **Event-Driven Architecture**
- All effects should fire through observer pattern
- Enables simulation tracking and UI updates
- Maintains separation between core logic and tracking

### **Factory Pattern Guidelines**
- Each mutation belongs to exactly one category factory
- Use `helper.MakeRoot()` for Tier 1 mutations (no prerequisites)
- Use `helper.MakeChild()` for all other mutations (with prerequisites)
- Leverage `helper.FormatPercent()` and `helper.FormatFloat()` for consistent formatting

---

## File Location Quick Reference

| **Component** | **File Path** |
|---------------|---------------|
| Mutation IDs | `FungusToast.Core/Mutations/MutationIds.cs` |
| Mutation Types | `FungusToast.Core/Mutations/MutationTypeEnum.cs` |
| Balance Constants | `FungusToast.Core/Config/GameBalance.cs` |
| **Growth Mutations** | `FungusToast.Core/Mutations/Factories/GrowthMutationFactory.cs` |
| **CellularResilience Mutations** | `FungusToast.Core/Mutations/Factories/CellularResilienceMutationFactory.cs` |
| **Fungicide Mutations** | `FungusToast.Core/Mutations/Factories/FungicideMutationFactory.cs` |
| **GeneticDrift Mutations** | `FungusToast.Core/Mutations/Factories/GeneticDriftMutationFactory.cs` |
| **MycelialSurges Mutations** | `FungusToast.Core/Mutations/Factories/MycelialSurgesMutationFactory.cs` |
| **Mutation Builder Helper** | `FungusToast.Core/Mutations/Factories/MutationBuilderHelper.cs` |
| Main Repository (Coordinator) | `FungusToast.Core/Mutations/MutationRepository.cs` |
| UI Layout | `Assets/Scripts/Unity/UI/MutationTree/UI_MutationLayoutProvider.cs` |
| Effect Processing | `FungusToast.Core/Phases/[Category]MutationProcessor.cs` |
| Effect Coordination | `FungusToast.Core/Phases/MutationEffectCoordinator.cs` |
| Growth Sources | `FungusToast.Core/Growth/GrowthSource.cs` |
| Observer Interface | `FungusToast.Core/Metrics/ISimulationObserver.cs` |
| Tracking Implementation | `FungusToast.Simulation/Models/SimulationTrackingContext.cs` |
| Result Data | `FungusToast.Simulation/Models/PlayerResult.cs` |
| Result Population | `FungusToast.Simulation/Models/GameResult.cs` |
| Usage Display | `FungusToast.Simulation/Analysis/PlayerMutationUsageTracker.cs` |
| **Unity UI Logging** | `Assets/Scripts/Unity/UI/GameLog/GameLogManager.cs` |
| **Unity UI Router** | `Assets/Scripts/Unity/UI/GameLog/GameLogRouter.cs` |

---

## Unity UI Logging Integration (Optional)

Most mutations automatically display their effects through existing event systems. However, some mutations may benefit from custom UI messages for special effects.

### **When UI Updates Are Needed**

- **Standard Growth/Reclaim/Kill Effects**: Usually handled automatically via `GrowthSource` events
  - Example: Regenerative Hyphae reclaims appear in growth cycle summaries automatically
  - No additional UI code needed for basic cell placement/reclamation

- **Special Effects**: May need custom observer methods and UI messages
  - Example: Hypersystemic Regeneration's resistance application
  - Example: Free mutation points from various sources

### **Adding Custom UI Messages**

If your mutation has special effects that need unique messaging:

#### **A. Add GrowthSource (if placing cells)**
```csharp
// File: FungusToast.Core/Growth/GrowthSource.cs
/// <summary>
/// Description of your new growth source
/// </summary>
YourNewMutationGrowth,
```

#### **B. Add to GetAbilityDisplayName (if needed)**
```csharp
// File: Assets/Scripts/Unity/UI/GameLog/GameLogManager.cs
// In GetAbilityDisplayName method:

GrowthSource.YourNewMutationGrowth => "Your Mutation Name",
```

#### **C. Add Custom Observer Messages (for special effects)**

For special effects, you have two main approaches:

##### **Simple Individual Messages** (for immediate effects):
```csharp
// File: Assets/Scripts/Unity/UI/GameLog/GameLogManager.cs
// Add to stub implementations section:

public void RecordYourCustomEffect(int playerId, int count) 
{
    if (playerId == humanPlayerId && count > 0)
    {
        string message = count == 1 
            ? "1 cell gained your special effect!" 
            : $"{count} cells gained your special effect!";
        AddLuckyEntry(message, playerId);
    }
}
```

##### **Batched Summary Messages** (for effects that occur multiple times):
```csharp
// File: Assets/Scripts/Unity/UI/GameLog/GameLogManager.cs

// 1. Add tracking fields (with other tracking fields):
private int yourCustomEffectApplications = 0;
private bool isTrackingYourCustomEffect = false;

// 2. Add to OnRoundStart (or appropriate phase start):
yourCustomEffectApplications = 0;
isTrackingYourCustomEffect = true;

// 3. Add summary method:
private void ShowYourCustomEffectSummary()
{
    if (IsSilentMode) return;
    
    if (yourCustomEffectApplications == 0) return;
    
    string message = yourCustomEffectApplications == 1 
        ? "1 cell gained your special effect!"
        : $"{yourCustomEffectApplications} cells gained your special effect!";
    
    AddEntry(new GameLogEntry(message, GameLogCategory.Lucky, null, humanPlayerId));
}

// 4. Add to appropriate phase end (e.g., OnPostGrowthPhase):
if (isTrackingYourCustomEffect)
{
    ShowYourCustomEffectSummary();
    yourCustomEffectApplications = 0;
    isTrackingYourCustomEffect = false;
}

// 5. Update the record method to use batching:
public void RecordYourCustomEffect(int playerId, int count) 
{
    if (isTrackingYourCustomEffect && playerId == humanPlayerId)
    {
        yourCustomEffectApplications += count;
    }
}
```

#### **D. Add Stub to GameLogRouter**
```csharp
// File: Assets/Scripts/Unity/UI/GameLog/GameLogRouter.cs
// Add to stub implementations section:

public void RecordYourCustomEffect(int playerId, int count) { }
```

### **Choosing Between Message Approaches**

- **Individual Messages**: Use for effects that happen infrequently or need immediate feedback
  - Example: Free mutation points from Adaptive Expression
  - Pattern: Simple check and immediate `AddLuckyEntry()` call

- **Batched Summary Messages**: Use for effects that can occur multiple times in rapid succession
  - Example: Hypersystemic Regeneration resistance applications, Decay phase deaths
  - Pattern: Track during phase → Display summary at phase end → Reset counters

---

## Tips for AI Assistants

- **Always follow this order**: ID → Type → Balance → Definition → UI → Logic → Tracking
- **Choose correct factory**: Select the appropriate category-specific factory file
- **Use MutationBuilderHelper**: Leverage formatting and building utilities for consistency
- **Cross-category prerequisites**: Essential for Tier 3+ balance
- **Observer pattern**: Required for all trackable effects
- **UI Integration**: Most mutations work automatically; only add custom UI for special effects
- **Build frequently**: Use `run_build` after each major step
- **Test simulations**: Verify tracking works with simulation runs
- **Reference existing**: Look at similar mutations in the same category factory for patterns

This systematic approach ensures complete integration across all game systems while leveraging the new factory-based architecture.