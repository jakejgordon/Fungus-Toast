# NEW_MUTATION_HELPER.md

> **?? AI Assistant Guide**: Step-by-step implementation pattern for adding new mutations to Fungus Toast

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
    ISimulationObserver? observer = null)
{
    foreach (var player in players)
    {
        int level = player.GetMutationLevel(MutationIds.NewMutationName);
        if (level <= 0 || !player.IsSurgeActive(MutationIds.NewMutationName))
            continue;

        // Implementation logic here...
        
        // Track effects
        observer?.RecordNewMutationEffect(player.PlayerId, effectCount);
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

---

## File Location Quick Reference

| **Component** | **File Path** |
|---------------|---------------|
| Mutation IDs | `FungusToast.Core/Mutations/MutationIds.cs` |
| Mutation Types | `FungusToast.Core/Mutations/MutationTypeEnum.cs` |
| Balance Constants | `FungusToast.Core/Config/GameBalance.cs` |
| Mutation Definitions | `FungusToast.Core/Mutations/MutationRepository.cs` |
| UI Layout | `Assets/Scripts/Unity/UI/MutationTree/UI_MutationLayoutProvider.cs` |
| Effect Processing | `FungusToast.Core/Phases/[Category]MutationProcessor.cs` |
| Effect Coordination | `FungusToast.Core/Phases/MutationEffectCoordinator.cs` |
| Growth Sources | `FungusToast.Core/Growth/GrowthSource.cs` |
| Observer Interface | `FungusToast.Core/Metrics/ISimulationObserver.cs` |
| Tracking Implementation | `FungusToast.Simulation/Models/SimulationTrackingContext.cs` |
| Result Data | `FungusToast.Simulation/Models/PlayerResult.cs` |
| Result Population | `FungusToast.Simulation/Models/GameResult.cs` |
| Usage Display | `FungusToast.Simulation/Analysis/PlayerMutationUsageTracker.cs` |

---

## Tips for AI Assistants

- **Always follow this order**: ID ? Type ? Balance ? Definition ? UI ? Logic ? Tracking
- **Cross-category prerequisites**: Essential for Tier 3+ balance
- **Observer pattern**: Required for all trackable effects
- **Build frequently**: Use `run_build` after each major step
- **Test simulations**: Verify tracking works with simulation runs
- **Reference existing**: Look at similar mutations for patterns

This systematic approach ensures complete integration across all game systems.