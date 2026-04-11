# NEW_MUTATION_HELPER.md

> **🤖 AI Assistant Guide**: Step-by-step implementation pattern for adding new mutations to Fungus Toast
>
> **📚 Related Documentation**: For mutation-category philosophy and prerequisite design rules, see [second-level/MUTATION_PREREQUISITE_GUIDELINES.md](second-level/MUTATION_PREREQUISITE_GUIDELINES.md). For canonical gameplay terminology, see [GAMEPLAY_TERMINOLOGY.md](GAMEPLAY_TERMINOLOGY.md). For the full documentation hierarchy, see [README.md](README.md).

## Overview

This guide provides a systematic approach for adding new mutations to Fungus Toast. Follow these steps in order to ensure proper integration with the game system, simulation tracking, and UI display.

When creating a new Mutation, proactively list the proposed test cases that should be run to validate the mechanic. Include the expected happy path, key edge cases, important interactions with existing systems, and any likely regression risks so testing can be planned immediately.

---

## Naming & Description Convention

All mutation copy (name, description, flavor text) must follow these rules to keep the UI readable in tooltips and simulation reports.

### **Name Rules**

| Rule | Detail |
|------|--------|
| **Length** | 2–3 words, **≤ 28 characters** |
| **Uniqueness** | First word should be unique within its category; avoid reusing a keyword for unrelated mechanics across categories |
| **Tone** | Balanced scientific flavor + plain language — at most **one** advanced biological term per name |
| **Pronounceability** | A new player should be able to say it out loud without stumbling |
| **Structure** | Noun phrase that hints at the mechanic (e.g., *Regenerative Hyphae*, *Adaptive Expression*) |

### **Description Rules**

| Rule | Detail |
|------|--------|
| **Length** | 1–3 sentences, **≤ 220 rendered characters** (interpolated values count toward the cap) |
| **Sentence order** | **Trigger / timing → Effect / scaling → Max-level bonus** |
| **Max-level clause** | Use `<b>Max Level Bonus:</b>` on a new line when the max-level effect is mechanically distinct. Example from Necrophytic Bloom: `"...\n<b>Max Level Bonus:</b> Can also create Hypervariation Development patches."` |
| **Jargon** | No unexplained scientific terms — if a bio word appears in the Name, restate the mechanic in plain language in the Description |
| **Formatting** | Use `<b>…</b>` for emphasis on key numbers/bonuses; use `\n` for line breaks before Max Level Bonus; avoid bullet lists and multi-paragraph blocks |
| **Encoding** | No special Unicode bullets or en-dashes that can corrupt — use plain hyphens and standard ASCII punctuation |

### **FlavorText Rules** *(separate policy)*

| Rule | Detail |
|------|--------|
| **Purpose** | Thematic / lore-only — evoke the mutation's fantasy, not restate mechanics |
| **Tone** | Can be more technical and expressive than Description |
| **Constraint** | Must not contradict the Description mechanics |
| **Length** | No hard cap, but aim for 1–2 sentences |

### **Copy Review Checklist** *(run before every PR that adds or changes mutation text)*

1. Name ≤ 28 chars and 2–3 words?
2. Description ≤ 220 rendered chars?
3. Description follows Trigger → Effect → Max-Level order?
4. No unexplained jargon in Description?
5. `<b>Max Level Bonus:</b>` used for distinct max-level effects?
6. No encoding artifacts (replacement chars, smart quotes, Unicode bullets)?
7. FlavorText does not restate or contradict mechanics?

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

#### **D. Add AI Metadata (Optional but Recommended for Surges)**
If the mutation should influence AI behavior (for example, catch-up surges), set `aiTags` in the mutation definition.

```csharp
// File: FungusToast.Core/Mutations/MutationAITags.cs
// Existing tags include: None, CatchUp

new Mutation(
    // ...
    isSurge: true,
    surgeDuration: GameBalance.NewSurgeDuration,
    pointsPerActivation: GameBalance.NewSurgePointsPerActivation,
    pointIncreasePerLevel: GameBalance.NewSurgePointIncreasePerLevel,
    aiTags: MutationAITags.CatchUp
)
```

If the mutation is a surge, also consider adding backbone suggestions in `MutationSynergyCatalog.cs` for roster-audit quality checks.

### **2. Mutation Definition**

#### **A. Add to the Correct Category Factory**
```csharp
// File: FungusToast.Core/Mutations/Factories/[Category]MutationFactory.cs
// Add in the appropriate tier section using helper.MakeRoot/helper.MakeChild:

helper.MakeChild(new Mutation(
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

`MutationRepository` is now the coordinator that invokes category factories; avoid manually adding one-off mutation definitions directly there.

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
// File: FungusToast.Simulation/Models/SimulationTrackingContext.*.cs (choose the most relevant partial)
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

> Note: `SimulationTrackingContext` is split across multiple partial files. Use the domain-appropriate partial file (or add a new `SimulationTrackingContext.*.cs` partial). For the current file map and tracking-wiring checklist, see `second-level/SIMULATION_TRACKING_IMPLEMENTATION.md`.

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

1. **Build Test**:
    - `dotnet build FungusToast.Core/FungusToast.Core.csproj`
    - `dotnet build FungusToast.Simulation/FungusToast.Simulation.csproj`
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
| Tracking Implementation | `FungusToast.Simulation/Models/SimulationTrackingContext.*.cs` (partial files; see `docs/second-level/SIMULATION_TRACKING_IMPLEMENTATION.md`) |
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
- **Build frequently**: Run the Core + Simulation `dotnet build` commands after each major step
- **Test simulations**: Verify tracking works with simulation runs
- **Reference existing**: Look at similar mutations in the same category factory for patterns

This systematic approach ensures complete integration across all game systems while leveraging the new factory-based architecture.