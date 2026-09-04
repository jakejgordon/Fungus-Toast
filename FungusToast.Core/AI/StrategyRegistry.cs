using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.AI
{
    internal sealed class UniqueKeyDictionary<TValue> : Dictionary<string, TValue>
    {
        public UniqueKeyDictionary(IEqualityComparer<string> comparer)
            : base(comparer)
        {
        }

        public new TValue this[string key]
        {
            get => base[key];
            set
            {
                if (ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate metadata definition for strategy '{key}'.");
                }

                Add(key, value);
            }
        }
    }

    public static class StrategyRegistry
    {
        private static readonly Dictionary<StrategySetEnum, List<StrategyDefinition>> DefinitionsBySet = new();

        public static void Reset()
        {
            DefinitionsBySet.Clear();
        }

        public static void Register(
            StrategySetEnum strategySet,
            IEnumerable<IMutationSpendingStrategy> strategies,
            Func<IMutationSpendingStrategy, StrategyCatalogEntry> entryFactory)
        {
            if (entryFactory == null)
            {
                throw new ArgumentNullException(nameof(entryFactory));
            }

            var registered = strategies
                .Select(strategy => new StrategyDefinition(
                    strategy,
                    entryFactory(strategy),
                    StrategyIdentity.GetStableId(strategySet, strategy),
                    StrategyIdentity.GetDefinitionFingerprint(strategy)))
                .ToList();

            var duplicateNames = registered
                .GroupBy(definition => definition.Strategy.StrategyName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate strategy names found in {strategySet}: {string.Join(", ", duplicateNames)}");
            }

            var duplicateIds = registered
                .GroupBy(definition => definition.StrategyId, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            if (duplicateIds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate stable strategy IDs found in {strategySet}: {string.Join(", ", duplicateIds)}");
            }

            DefinitionsBySet[strategySet] = registered;
        }

        public static IReadOnlyList<StrategyDefinition> GetDefinitions(StrategySetEnum strategySet)
        {
            return DefinitionsBySet.TryGetValue(strategySet, out var definitions)
                ? definitions.ToList()
                : Array.Empty<StrategyDefinition>();
        }

        public static StrategyDefinition? GetDefinition(StrategySetEnum strategySet, string strategyName)
        {
            return DefinitionsBySet.TryGetValue(strategySet, out var definitions)
                ? definitions.FirstOrDefault(definition => string.Equals(
                    definition.Strategy.StrategyName,
                    strategyName,
                    StringComparison.OrdinalIgnoreCase))
                : null;
        }

        public static StrategyDefinition? GetDefinition(IMutationSpendingStrategy strategy)
        {
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            return DefinitionsBySet.Values
                .SelectMany(definitions => definitions)
                .FirstOrDefault(definition => ReferenceEquals(definition.Strategy, strategy));
        }

        public static List<IMutationSpendingStrategy> GetStrategies(StrategySetEnum strategySet)
        {
            return DefinitionsBySet.TryGetValue(strategySet, out var definitions)
                ? definitions.Select(definition => definition.Strategy).ToList()
                : new List<IMutationSpendingStrategy>();
        }

        public static Dictionary<string, IMutationSpendingStrategy> GetStrategyDictionary(StrategySetEnum strategySet)
        {
            if (!DefinitionsBySet.TryGetValue(strategySet, out var definitions))
            {
                return new Dictionary<string, IMutationSpendingStrategy>(StringComparer.OrdinalIgnoreCase);
            }

            var strategies = new Dictionary<string, IMutationSpendingStrategy>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                if (!strategies.TryAdd(definition.Strategy.StrategyName, definition.Strategy))
                {
                    throw new InvalidOperationException(
                        $"Duplicate strategy name '{definition.Strategy.StrategyName}' found while building {strategySet} strategy dictionary.");
                }
            }

            return strategies;
        }

        public static IReadOnlyList<StrategyCatalogEntry> GetCatalogEntries(StrategySetEnum strategySet)
        {
            return DefinitionsBySet.TryGetValue(strategySet, out var definitions)
                ? definitions.Select(definition => definition.Metadata).ToList()
                : Array.Empty<StrategyCatalogEntry>();
        }
    }

    /// <summary>
    /// Immutable registry record that keeps a strategy implementation, its
    /// machine identity, behavior fingerprint, and catalog metadata together.
    /// Consumers must read this record instead of joining parallel name maps.
    /// </summary>
    public sealed class StrategyDefinition
    {
        public StrategyDefinition(
            IMutationSpendingStrategy strategy,
            StrategyCatalogEntry metadata,
            string strategyId,
            string definitionFingerprint)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            StrategyId = string.IsNullOrWhiteSpace(strategyId)
                ? throw new ArgumentException("Strategy ID is required.", nameof(strategyId))
                : strategyId;
            DefinitionFingerprint = string.IsNullOrWhiteSpace(definitionFingerprint)
                ? throw new ArgumentException("Definition fingerprint is required.", nameof(definitionFingerprint))
                : definitionFingerprint;

            if (!string.Equals(strategy.StrategyName, metadata.StrategyName, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Strategy name '{strategy.StrategyName}' does not match metadata name '{metadata.StrategyName}'.",
                    nameof(metadata));
            }
        }

        public IMutationSpendingStrategy Strategy { get; }
        public StrategyCatalogEntry Metadata { get; }
        public string StrategyId { get; }
        public string DefinitionFingerprint { get; }
    }
}
