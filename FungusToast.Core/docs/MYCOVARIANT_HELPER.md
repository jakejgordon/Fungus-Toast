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
   
   a) **Factory ApplyEffect Logic:**
   ```csharp
   ApplyEffect = (playerMyco, board, rng, observer) =>
   {
       var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
       bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
       if (shouldUseCoreLogic)
       {
           // AI or Simulation: Core handles everything (selection + effect application)
           // Auto-select appropriate targets and execute effect
       }
       // Human in Unity: UI layer handles selection + effect application
       // (ApplyEffect does nothing, avoiding double execution)
   },
   ```
   
   b) **Unity UI Effect Helper:**
   ```csharp
   public static IEnumerator HandleYourMycovariant(
       Player player, Mycovariant picked, Action onComplete,
       GameObject draftPanel, GridVisualizer gridVisualizer)
   {
       if (player.PlayerType == PlayerTypeEnum.AI)
       {
           // Unity drafts bypass ApplyEffect for AI; execute directly here
           // ... resolve effect ...
           yield return new WaitForSeconds(UIEffectConstants.DefaultAIThinkingDelay);
           onComplete?.Invoke();
       }
       else
       {
           // Human: Implement UI selection logic
           // After resolving the effect, render and WAIT for animations:
           gridVisualizer.RenderBoard(GameManager.Instance.Board);
           yield return gridVisualizer.WaitForAllAnimations();
           onComplete?.Invoke();
       }
   }
   ```
   
   c) **Effect Resolver Integration:**
   ```csharp
   else if (mycovariant.Id == MycovariantIds.YourMycovariantId)
   {
       yield return StartCoroutine(
           MycovariantEffectHelpers.HandleYourMycovariant(
               player, mycovariant, onComplete, draftPanel, gridVisualizer));
   }
   ```

   d) **Unity animations must be waitable (new):**
   - Grid visuals use an animation tracker so drafts can block until visuals finish.
   - In `GridVisualizer`, wrap each visual coroutine with:
     ```csharp
     BeginAnimation();
     try { /* animation loop */ }
     finally { EndAnimation(); }
     ```
   - If you stop/replace a running animation via `StopCoroutine`, ensure the counter is decremented (call `EndAnimation()` when interrupting) so `WaitForAllAnimations()` does not hang.
   - After you resolve an effect in the helper, call `gridVisualizer.RenderBoard(board)` so any per-cell flags (e.g., `IsNewlyGrown`, `IsDying`, toxin drop flags) start their visuals, then `yield return gridVisualizer.WaitForAllAnimations()` before calling `onComplete`.
   - Avoid calling `onComplete` inside selection callbacks; call it once, after the wait.

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

### ? CRITICAL ERROR: Assuming ApplyEffect is Called in Unity Drafts

**Problem:** Assuming that the `ApplyEffect` property will be called for AI players during Unity drafts.

**Reality:** Unity drafts completely bypass the `ApplyEffect` method and go straight to `MycovariantEffectResolver`. This means:
- ? **Silent draft AI**: Gets effects via `ApplyEffect`
- ? **Unity draft AI**: Gets NOTHING if you only implement `ApplyEffect`
- ? **Unity draft Human**: Gets effects via UI selection + manual execution

**Solution:** Always implement both the `ApplyEffect` logic AND the Unity UI handler for active mycovariants.

### ? Incomplete Unity AI Implementation

**Problem:** Implementing the human UI path but forgetting the AI execution path in `MycovariantEffectHelpers`.

**Wrong:**
```csharp
if (player.PlayerType == PlayerTypeEnum.AI)
{
    // AI: Core ApplyEffect handles everything  – WRONG! ApplyEffect isn't called
    yield return new WaitForSeconds(delay);
    onComplete?.Invoke();
}
```
**Correct:**
```csharp
if (player.PlayerType == PlayerTypeEnum.AI)
{
    // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
    var playerMyco = player.PlayerMycovariants.FirstOrDefault(pm => pm.MycovariantId == picked.Id);
    if (playerMyco != null)
    {
        MycovariantEffectProcessor.ResolveYourEffect(playerMyco /* ... */);
    }
    yield return new WaitForSeconds(delay);
    onComplete?.Invoke();
}
```

### ? Visuals not finishing before draft resumes (new)

**Problem:** Draft resumes immediately after selection, cutting off mycovariant animations.

**Solution:** After resolving the effect, call `gridVisualizer.RenderBoard(board)` and then `yield return gridVisualizer.WaitForAllAnimations()` before invoking `onComplete`. If you add bespoke animations (e.g., a “jetting line”), implement them in `GridVisualizer` and wrap with `BeginAnimation/EndAnimation` so they are included in the wait.

---

## Additional Tips

- **Avoid Magic Numbers:** Always use named constants from `@MycovariantGameBalance.cs` for any tunable values.
- **Simulation Output:** If you want the effect to appear in simulation analytics, you must record it via the observer and tracking context, and report it in `@GameResult.cs`.
- **Ask for Clarification:** If the effect is complex or has multiple components, Cursor should ask the human for clarification on what should be tracked and reported.
- **Test Both Draft Types:** Always test your mycovariant in both silent drafts (via simulation/fast-forward) and Unity UI drafts to ensure both AI paths work correctly.
- **Debug Logging:** Add logging to verify that your effects are being executed correctly in both contexts.
- **Auto-Trigger Declaration:** Use `AutoMarkTriggered = true` for passive mycovariants that don't require player input.
- **Unity Log Manager Stubs:** For any new `ISimulationObserver` methods you add, you must also add stub implementations to both `@GameLogRouter.cs` and `@GameLogManager.cs` in the Unity project. These should follow the pattern of checking `IsSilentMode` in the router and implementing the actual logging logic in the manager. By default, most methods should be stubbed as empty implementations unless they need specific player activity logging.