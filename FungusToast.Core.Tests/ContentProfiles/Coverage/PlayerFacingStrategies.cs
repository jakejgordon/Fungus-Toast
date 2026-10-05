using FungusToast.Core.AI;

namespace FungusToast.Core.Tests.ContentProfiles.Coverage;

/// <summary>
/// The strategies the content catalogs and the coverage review look at: every non-Retired,
/// parameterized strategy in the player-facing Proven and Campaign sets.
/// </summary>
internal static class PlayerFacingStrategies
{
    public static readonly StrategySetEnum[] Sets = { StrategySetEnum.Proven, StrategySetEnum.Campaign };

    public sealed record Entry(StrategyDefinition Definition, ParameterizedSpendingStrategy Strategy, StrategySetEnum Set, string Label)
    {
        public string StrategyId => Definition.StrategyId;
    }

    public static IReadOnlyList<Entry> Load()
    {
        Assert.NotEmpty(AIRoster.ProvenStrategies); // registers the authored sets
        var entries = new List<Entry>();
        foreach (var set in Sets)
        {
            var definitions = StrategyRegistry.GetDefinitions(set)
                .Where(definition => definition.Metadata.Lifecycle != StrategyLifecycle.Retired)
                .ToList();
            // Several campaign difficulty variants share a display name; the internal name tells them apart.
            var sharedDisplayNames = definitions
                .GroupBy(DisplayName)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();

            foreach (var definition in definitions)
            {
                if (definition.Strategy is not ParameterizedSpendingStrategy strategy)
                {
                    continue;
                }

                var displayName = DisplayName(definition);
                var label = sharedDisplayNames.Contains(displayName)
                    ? $"{displayName} [`{definition.Metadata.StrategyName}`] ({set})"
                    : $"{displayName} ({set})";
                entries.Add(new Entry(definition, strategy, set, label));
            }
        }

        return entries;
    }

    private static string DisplayName(StrategyDefinition definition) =>
        string.IsNullOrWhiteSpace(definition.Metadata.FriendlyName)
            ? definition.Metadata.StrategyName
            : definition.Metadata.FriendlyName;
}
