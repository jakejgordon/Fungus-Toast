using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.AI
{
    public static class StrategyRegistry
    {
        private static readonly Dictionary<StrategySetEnum, List<RegisteredStrategy>> EntriesBySet = new();

        public static void Reset()
        {
            EntriesBySet.Clear();
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
                .Select(strategy => new RegisteredStrategy(strategy, entryFactory(strategy)))
                .ToList();

            var duplicateNames = registered
                .GroupBy(entry => entry.Strategy.StrategyName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate strategy names found in {strategySet}: {string.Join(", ", duplicateNames)}");
            }

            EntriesBySet[strategySet] = registered;
        }

        public static List<IMutationSpendingStrategy> GetStrategies(StrategySetEnum strategySet)
        {
            return EntriesBySet.TryGetValue(strategySet, out var entries)
                ? entries.Select(e => e.Strategy).ToList()
                : new List<IMutationSpendingStrategy>();
        }

        public static Dictionary<string, IMutationSpendingStrategy> GetStrategyDictionary(StrategySetEnum strategySet)
        {
            if (!EntriesBySet.TryGetValue(strategySet, out var entries))
            {
                return new Dictionary<string, IMutationSpendingStrategy>(StringComparer.OrdinalIgnoreCase);
            }

            var strategies = new Dictionary<string, IMutationSpendingStrategy>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (!strategies.TryAdd(entry.Strategy.StrategyName, entry.Strategy))
                {
                    throw new InvalidOperationException(
                        $"Duplicate strategy name '{entry.Strategy.StrategyName}' found while building {strategySet} strategy dictionary.");
                }
            }

            return strategies;
        }

        public static IReadOnlyList<StrategyCatalogEntry> GetCatalogEntries(StrategySetEnum strategySet)
        {
            return EntriesBySet.TryGetValue(strategySet, out var entries)
                ? entries.Select(e => e.Entry).ToList()
                : Array.Empty<StrategyCatalogEntry>();
        }

        private sealed class RegisteredStrategy
        {
            public RegisteredStrategy(IMutationSpendingStrategy strategy, StrategyCatalogEntry entry)
            {
                Strategy = strategy;
                Entry = entry;
            }

            public IMutationSpendingStrategy Strategy { get; }
            public StrategyCatalogEntry Entry { get; }
        }
    }
}
