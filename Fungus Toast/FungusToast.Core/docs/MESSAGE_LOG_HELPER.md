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

### Player Message Log (Left Sidebar - "Activity Log")

#### **Should Include:**
1. **Personal Performance Summaries**
   - End-of-round summaries: "Grew 5 cells", "3 cells died", "Dropped 2 toxins", "X dead cells total"
   - Individual mutation point earnings
   - Direct player actions and their immediate results

2. **High-Impact Personal Events**
   - Free mutation points from special abilities (Mutator Phenotype, Hyperadaptive Drift)
   - Direct attacks on player cells: "Your cell was poisoned!"
   - Successful player attacks: "Poisoned enemy cell", "Jetting Mycelium killed 3 enemy cells"

3. **Non-Visual Important Events**
   - Events that significantly affect the player but aren't easily visible on the board
   - Mycovariant effect summaries (future feature)
   - Mutation-based bonuses and penalties

#### **Should NOT Include:**
- Game state changes (rounds, phases)
- Other players' activities unless directly affecting the human player
- System messages or administrative events

### Global Message Log (Right Sidebar - "Game Events")

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

### Adding New Log Events

#### **For Player Events:**
```csharp
// In GameLogManager.cs
public void RecordPlayerSpecificEvent(int playerId, string details)
{
    if (playerId == humanPlayerId) // Only track human player
    {
        AddLuckyEntry($"Special event: {details}", playerId);
    }
}
```

#### **For Global Events:**
```csharp
// In GlobalGameLogManager.cs  
public void OnGameStateChange(string eventDescription)
{
    AddNormalEntry(eventDescription);
}
```

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