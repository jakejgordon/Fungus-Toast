# Mycovariant Implementation Guide

This guide describes the step-by-step process for adding a new mycovariant to the Fungus Toast codebase. Follow these steps to ensure your new mycovariant is fully integrated with both gameplay and simulation output.

---

## Steps to Add a New Mycovariant

1. **Concept & Naming**
   - The human describes the new mycovariant's concept and desired gameplay effect.
   - Cursor (the AI) provides a list of fungally-themed name ideas that fit the effect.

2. **Name Selection**
   - The human selects a name and confirms the effect details (including any numbers or balance parameters).

3. **Implementation Steps (Cursor):**

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
     - Add a case in `@GameResult.cs` to report the effect using the simulation tracking context.
     - Add a method to `@ISimulationObserver.cs` and implement it in `@SimulationTrackingContext.cs` to record the effect.
     - If you (the human) do not specify what to record, Cursor should ask for clarification.

   **3.4) Event Subscription**
   - Subscribe to relevant events in `@GameRulesEventSubscriber.cs` to ensure the effect logic is triggered at the right time.

   **3.5) Game Lifecycle Integration**
   - Insert effect logic into the game lifecycle as needed:
     - For simulation: `@TurnEngine.cs`
     - For Unity: `@GameManager.cs`, `@GrowthPhaseRunner.cs`, `@DecayPhaseRunner.cs`, or other relevant files.

---

### Additional Tips

- **Avoid Magic Numbers:** Always use named constants from `@MycovariantGameBalance.cs` for any tunable values.
- **Simulation Output:** If you want the effect to appear in simulation analytics, you must record it via the observer and tracking context, and report it in `@GameResult.cs`.
- **Ask for Clarification:** If the effect is complex or has multiple components, Cursor should ask the human for clarification on what should be tracked and reported.
- **Update this file if you add new systems or conventions!** 