namespace FungusToast.Core.Death
{
    public enum DeathReason
    {
        Age,                // Died from age-related decay chance
        EnemyDecayPressure, // Death caused by modifiers from enemy mutations
        Randomness,         // Base chance not tied to age or enemy pressure
        Protected,          // Death was prevented (e.g., last living cell)
        Unknown,             // Fallback/default (should not be common)
        Fungicide
    }
}
