# Mycovariant Implementation Guide

This guide describes the step-by-step process for adding a new mycovariant to the Fungus Toast codebase. Follow these steps to ensure your new mycovariant is fully integrated with both gameplay and simulation output.

---

## Steps to Add a New Mycovariant

1. **Concept & Naming**
   - The human describes the new mycovariant's concept and desired gameplay effect.
   - Cursor (the AI) provides a list of fungally-themed name ideas that fit the effect.

2. **Name Selection**
   - The human selects a name and confirms the effect details (including any numbers or balance parameters).

3. **Implementation Steps:**

   **3.1) Core Definition**
   - Add a new constant in `@MycovariantIds.cs` for the mycovariant's unique ID.
   - Add a new entry in `@MycovariantFactory.cs`:
     - Use constants from `@MycovariantGameBalance.cs` for all numbers (never hard-code).
     - Write a clear Description (with numbers where possible) and a fun, thematic FlavorText.
   - Add any new balance constants to `@MycovariantGameBalance.cs`.

   **3.2) Registration**
   - Add the new mycovariant to the canonical list in `@MycovariantRepository.cs`.

   **3.3) Effect Logic & Simulation Output**
   - Implement effect logic in `@MycovariantEffectProcessor.cs` (and/or other relevant processors).
   - If the effect should be tracked in simulation output:
     - Add a new effect type in `@MycovariantEffectType.cs`.
     - Add a case in `@GameResult.cs` to report the effect using the simulation tracking context. **For each new mycovariant, ensure you add a case for its ID(s) in the switch statement in `BuildMycovariantResults` so its effect count appears in simulation output.**
     - Add a method to `@ISimulationObserver.cs` and implement it in `@SimulationTrackingContext.cs` to record the effect.
     - If you (the human) do not specify what to record, Cursor should ask for clarification.

   **3.4) Draft System Integration (CRITICAL)**
   
   **Understanding Draft Types:**
   There are two different draft systems that handle mycovariant effects differently:
   
   - **Silent Drafts**: Used during fast-forward/simulation. Effects are handled via the `ApplyEffect` property in `@MycovariantFactory.cs`.
   - **Unity UI Drafts**: Used during normal gameplay. Effects are handled via `@MycovariantEffectResolver.cs` and `@MycovariantEffectHelpers.cs`.
   
   **For Active Mycovariants (require player input):**
   
   If your mycovariant requires player selection (tiles, cells, etc.), you MUST implement both paths:
   
   a) **Factory ApplyEffect Logic:**ApplyEffect = (playerMyco, board, rng, observer) =>
{
    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
    
    bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
    
    if (shouldUseCoreLogic)
    {
        // AI or Simulation: Core handles everything (selection + effect application)
        // Auto-select appropriate targets and execute effect
        // Example: var livingCells = board.GetAllCellsOwnedBy(player.PlayerId).Where(c => c.IsAlive).ToList();
        // if (livingCells.Count > 0) { /* auto-select and execute */ }
    }
    // Human in Unity: UI layer handles selection + effect application
    // (ApplyEffect does nothing, avoiding double execution)
   },   
   b) **Unity UI Effect Helper:**
   Add a handler method in `@MycovariantEffectHelpers.cs`:public static IEnumerator HandleYourMycovariant(
    Player player, Mycovariant picked, Action onComplete,
    GameObject draftPanel, GridVisualizer gridVisualizer)
{
    if (player.PlayerType == PlayerTypeEnum.AI)
    {
        // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
        var playerMyco = player.PlayerMycovariants
            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
        
        if (playerMyco != null)
        {
            // Call your effect processor method directly
            MycovariantEffectProcessor.ResolveYourMycovariant(
                playerMyco, player, GameManager.Instance.Board, /* params */, rng, null);
        }
        
        yield return new WaitForSeconds(UIEffectConstants.DefaultAIThinkingDelay);
        onComplete?.Invoke();
    }
    else
    {
        // Human: Implement UI selection logic
        // Show selection prompts, handle tile/cell selection, etc.
       }
   }   
   c) **Effect Resolver Integration:**
   Add your mycovariant case to `@MycovariantEffectResolver.cs` in the `ResolveEffect` method:else if (mycovariant.Id == MycovariantIds.YourMycovariantId)
{
    yield return StartCoroutine(
           MycovariantEffectHelpers.HandleYourMycovariant(
               player, mycovariant, onComplete, draftPanel, gridVisualizer));
   }
   **For Passive Mycovariants (no player input):**
   
   If your mycovariant has an instant effect or purely passive behavior, you only need the `ApplyEffect` logic in the factory. The Unity draft system will call this directly.

   **3.5) Event Subscription**
   - Subscribe to relevant events in `@GameRulesEventSubscriber.cs` to ensure the effect logic is triggered at the right time.

   **3.6) Game Lifecycle Integration**
   - Insert effect logic into the game lifecycle as needed:
     - For simulation: `@TurnEngine.cs`
     - For Unity: `@GameManager.cs`, `@GrowthPhaseRunner.cs`, `@DecayPhaseRunner.cs`, or other relevant files.

---

## Common Pitfalls to Avoid

### ? **CRITICAL ERROR: Assuming ApplyEffect is Called in Unity Drafts**

**Problem:** Assuming that the `ApplyEffect` property will be called for AI players during Unity drafts.

**Reality:** Unity drafts completely bypass the `ApplyEffect` method and go straight to `MycovariantEffectResolver`. This means:
- ? **Silent draft AI**: Gets effects via `ApplyEffect`
- ? **Unity draft AI**: Gets NOTHING if you only implement `ApplyEffect`
- ? **Unity draft Human**: Gets effects via UI selection + manual execution

**Solution:** Always implement both the `ApplyEffect` logic AND the Unity UI handler for active mycovariants.

### ? **Incomplete Unity AI Implementation**

**Problem:** Implementing the human UI path but forgetting the AI execution path in `MycovariantEffectHelpers`.

**Wrong:**if (player.PlayerType == PlayerTypeEnum.AI)
{
    // AI: Core ApplyEffect handles everything  ? WRONG! ApplyEffect isn't called
    yield return new WaitForSeconds(delay);
    onComplete?.Invoke();
}
**Correct:**
if (player.PlayerType == PlayerTypeEnum.AI)
{
    // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
    var playerMyco = player.PlayerMycovariants.FirstOrDefault(pm => pm.MycovariantId == picked.Id);
    if (playerMyco != null)
    {
        MycovariantEffectProcessor.ResolveYourEffect(playerMyco, /* params */);
    }
    yield return new WaitForSeconds(delay);
    onComplete?.Invoke();
}
---

## Additional Tips

- **Avoid Magic Numbers:** Always use named constants from `@MycovariantGameBalance.cs` for any tunable values.
- **Simulation Output:** If you want the effect to appear in simulation analytics, you must record it via the observer and tracking context, and report it in `@GameResult.cs`.
- **Ask for Clarification:** If the effect is complex or has multiple components, Cursor should ask the human for clarification on what should be tracked and reported.
- **Test Both Draft Types:** Always test your mycovariant in both silent drafts (via simulation/fast-forward) and Unity UI drafts to ensure both AI paths work correctly.
- **Debug Logging:** Add logging to verify that your effects are being executed correctly in both contexts.
- **Auto-Trigger Declaration:** Use `Auto