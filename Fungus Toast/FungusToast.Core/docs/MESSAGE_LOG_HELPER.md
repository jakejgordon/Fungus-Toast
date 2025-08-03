# Message Log System Architecture

This document explains the intent, design principles, and architecture of the Fungus Toast dual message log system, which provides player-specific and global game event tracking.

## System Overview

The message log system consists of two distinct log managers that serve different purposes in providing players with relevant game information:

- **Player Message Log** (Left Sidebar): Personal activities and events specific to the human player
- **Global Message Log** (Right Sidebar): Game-wide events and state changes visible to all players

## Design Principles

### Information Hierarchy
- **Player Log**: Detailed information about actions that directly affect the player
- **Global Log**: High-level game state changes and system messages
- **Avoid Duplication**: Events should not appear in both logs simultaneously
- **Context Awareness**: Messages should provide appropriate detail for their audience

### YAGNI (You Aren't Gonna Need It) Approach
- Simple, focused implementation without over-engineering
- Features can be added incrementally as needed
- No complex filtering or routing until demonstrated necessity
- Clean separation of concerns without excessive abstraction

## Architecture

### Core Components

#### **IGameLogManager Interface**
```csharp
public interface IGameLogManager
{
    event Action<GameLogEntry> OnNewLogEntry;
    IEnumerable<GameLogEntry> GetRecentEntries(int count = 20);
    void ClearLog();
}
```
- Common interface for both log managers
- Enables generic UI component reuse
- Event-driven for real-time updates

#### **GameLogManager** (Player-Specific Events)
- **Location**: `Assets/Scripts/Unity/UI/GameLog/GameLogManager.cs`
- **Purpose**: Tracks human player (ID 0) specific activities
- **Implements**: `ISimulationObserver` for game event tracking
- **Max Entries**: 50 entries
- **Focus**: Personal actions, round summaries, direct effects

#### **GlobalGameLogManager** (Game-Wide Events)
- **Location**: `Assets/Scripts/Unity/UI/GameLog/GlobalGameLogManager.cs`  
- **Purpose**: Tracks game state changes visible to all players
- **Max Entries**: 30 entries (lower volume expected)
- **Focus**: Round transitions, phase changes, endgame events

#### **UI_GameLogPanel** (Generic UI Component)
- **Location**: `Assets/Scripts/Unity/UI/GameLog/UI_GameLogPanel.cs`
- **Purpose**: Reusable UI component that works with any IGameLogManager
- **Features**: Scrolling, auto-scroll, entry limits, clear functionality
- **Reusability**: Same component used for both logs with different managers

### Message Routing

#### Event-Based Communication
- **GameManager** coordinates both log managers
- Each manager filters events relevant to its domain
- No cross-communication between log managers
- Clear ownership rules prevent message duplication

#### Simple Routing Rules
```csharp
// In GameManager methods:
gameUIManager.PlayerActivityLogManager?.OnRoundComplete(roundNumber);  // Player summary
gameUIManager.GlobalEventsLogManager?.OnRoundStart(roundNumber);       // Global event
```

## Message Content Guidelines

### **Player Message Log (Left Sidebar - "Activity Log")**

#### **Should Include:**
1. **Personal Performance Summaries**
   - End-of-round summaries: "Round X summary: Grew Y new cells, Z cells died, dropped W toxins, A dead cells total"
   - Individual mutation point earnings
   - Direct player actions and their immediate results

2. **High-Impact Personal Events**
   - Free mutation points from special abilities (Mutator Phenotype, Hyperadaptive Drift)
   - Aggregated attack results: "Poisoned 3 enemy cells", "Jetting Mycelium killed 5 enemy cells"
   - Direct attacks on player cells: "Your cell was poisoned!"

3. **Non-Visual Important Events**
   - Events that significantly affect the player but aren't easily visible on the board
   - Mycovariant effect summaries (future feature)
   - Mutation-based bonuses and penalties

#### **Should NOT Include:**
- Game state changes (rounds, phases)
- Other players' activities unless directly affecting the human player
- System messages or administrative events

### **Global Message Log (Right Sidebar - "Game Events")**

#### **Should Include:**
1. **Game State Changes**
   - "Round X begins!" messages (including Round 1)
   - Round summaries with net changes: "Round X summary: Y cells grown, Z cells died, W toxins added, board now A% occupied (B living, C dead, D toxins)"
   - Endgame triggers: "Final Round!", "Endgame in 3 rounds"
   - Game completion: "Game Over - Player 1 wins!"

2. **Board-Wide Events**
   - Draft phase notifications: "Mycovariant draft phase begins"
   - System-wide effects or rule changes
   - Administrative messages

3. **Future: Mycovariant Draft Summaries**
   - Auto-triggered mycovariant effects (planned feature)
   - Board-wide mycovariant impact summaries

#### **Should NOT Include:**
- Individual player actions
- Detailed personal statistics
- Phase change notifications (handled by UI_PhaseBanner and UI_PhaseProgressTracker)

## Implementation Patterns

### **Shared Round Summary Formatting**

Both log managers now use a shared `RoundSummaryFormatter` utility class for consistent messaging and proper Round 1 tracking:

```csharp
// Usage example for global log
string summary = RoundSummaryFormatter.FormatRoundSummary(
    roundNumber, cellsGrown, cellsDied, toxinChange, deadCellChange,
    livingCells, deadCells, toxinCells, occupancyPercent,
    isPlayerSpecific: false);

// Usage example for player log  
string summary = RoundSummaryFormatter.FormatRoundSummary(
    roundNumber, cellsGrown, cellsDied, toxinChange, deadCellChange,
    livingCells, deadCells, toxinCells, 0f,
    isPlayerSpecific: true);
```

### **Event Aggregation System**

The player log now aggregates rapid events to prevent spam and provides ability-specific tracking:

**Offensive Actions (Player Attacking):**
```
Jetting Mycelium poisoned 3 enemy cells
Cytolytic Burst poisoned 2 enemy cells
```

**Defensive Reactions (Player Being Attacked):**
```
3 of your cells were poisoned: 2 by Jetting Mycelium, 1 by Cytolytic Burst
1 of your cells was poisoned: 1 by Sporicidal Bloom
```

Events are aggregated using a 0.5-second delay window, allowing multiple rapid events to be combined into meaningful messages that show both totals and detailed breakdowns by ability.

#### **Consolidation Benefits:**
- **Clear Impact**: Shows total damage in one glance  
- **Strategic Insight**: Reveals which enemy abilities are most threatening
- **Reduced Spam**: Multiple individual messages become one informative summary
- **Extensible Pattern**: Foundation for consolidating other event types (infestations, colonizations, etc.)

#### **Round 1 Tracking Fix & Dead Cell Change Fix**
Both log managers take their initial snapshot during `Initialize()` before any spores are placed, and now properly track changes in dead cell counts:

**Round 1 Fix:**
- **Before Fix**: Round 1 with 1?2 cells showed "Grew 2 cells" 
- **After Fix**: Round 1 with 1?2 cells correctly shows "Grew 1 cell"

**Dead Cell Change Fix:**
- **Before Fix**: Showed total dead cells: "5 dead cells total"
- **After Fix**: Shows changes during round + total: "2 cells died, 5 dead cells total"

This ensures consistent language and formatting between both logs while providing accurate change tracking and current totals.

### Message Categories
- **Normal**: Informational messages (white text)
- **Lucky**: Positive events for the player (green text)
- **Unlucky**: Negative events for the player (red text)

### Entry Management
- Automatic pruning when max entries exceeded
- FIFO queue structure maintains chronological order
- Configurable entry limits per log type

## Technical Integration

### Unity Scene Setup
1. **Create UI hierarchy** with two GameLogPanel instances
2. **Left Sidebar**: PlayerActivityLogPanel ? GameLogManager
3. **Right Sidebar**: GlobalEventsLogPanel ? GlobalGameLogManager  
4. **Wire references** in GameUIManager Inspector
5. **No prefab instantiation** required - scene-based UI approach

### GameManager Integration
```csharp
// Initialize both systems
gameUIManager.PlayerActivityLogManager?.Initialize(Board);
gameUIManager.GlobalEventsLogManager?.Initialize(Board);

// Route events appropriately
gameUIManager.PlayerActivityLogManager?.OnRoundComplete(roundNumber);
gameUIManager.GlobalEventsLogManager?.OnRoundStart(roundNumber);
```

## Future Considerations

### Planned Features (Not Yet Implemented)
- **Mycovariant Effect Summaries**: Detailed reports of mycovariant impacts
- **Auto-triggered Mycovariant Logging**: Global log messages for draft effects
- **Enhanced Round Summaries**: Board-wide statistics in global log

### Potential Enhancements (YAGNI Until Needed)
- Message filtering by category or importance
- Persistent log storage across game sessions
- Export functionality for post-game analysis
- Click-to-highlight functionality for board references

## File Locations

| **Component** | **File Path** |
|---------------|---------------|
| Player Log Manager | `Assets/Scripts/Unity/UI/GameLog/GameLogManager.cs` |
| Global Log Manager | `Assets/Scripts/Unity/UI/GameLog/GlobalGameLogManager.cs` |
| Common Interface | `Assets/Scripts/Unity/UI/GameLog/IGameLogManager.cs` |
| Generic UI Panel | `Assets/Scripts/Unity/UI/GameLog/UI_GameLogPanel.cs` |
| Log Entry Data | `Assets/Scripts/Unity/UI/GameLog/GameLogEntry.cs` |
| UI Integration | `Assets/Scripts/Unity/UI/GameUIManager.cs` |
| Game Coordination | `Assets/Scripts/Unity/GameManager.cs` |

## Key Benefits

1. **Information Overload Prevention**: Separate logs reduce cognitive burden
2. **Contextual Relevance**: Players see information appropriate to their perspective  
3. **Maintainable Architecture**: Clean separation of concerns with reusable components
4. **Extensible Design**: Easy to add new event types or log targets
5. **Simple Implementation**: Follows YAGNI principles without over-engineering