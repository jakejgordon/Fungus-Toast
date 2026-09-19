public enum PlayerTypeEnum
{
    Human,
    AI
}

/// <summary>
/// Legacy runtime-snapshot compatibility values. AI behavior is selected by
/// <see cref="FungusToast.Core.AI.IMutationSpendingStrategy"/>, not this enum.
/// Keep the numeric values stable while old snapshots may still contain them.
/// </summary>
public enum AITypeEnum
{
    Random,
    Aggressive,
    Defensive,
    GrowthFocused
}
