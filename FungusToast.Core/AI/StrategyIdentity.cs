using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FungusToast.Core.AI
{
    public static class StrategyIdentity
    {
        public const string DefinitionSchemaVersion = "fungus-toast.ai-definition.v1";
        public const string CorpusVersion = "fungus-toast.ai-corpus.pre-phase5.v1";

        public static string GetStableId(StrategySetEnum strategySet, IMutationSpendingStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            var slug = new string(strategy.StrategyName
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray());
            while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-", StringComparison.Ordinal);
            slug = slug.Trim('-');
            if (slug.Length == 0) throw new InvalidOperationException("Strategy name cannot produce an empty stable ID.");
            return $"legacy.{strategySet.ToString().ToLowerInvariant()}.{slug}.v1";
        }

        public static string GetDefinitionFingerprint(IMutationSpendingStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            var canonical = strategy switch
            {
                ParameterizedSpendingStrategy parameterized => BuildParameterizedDefinition(parameterized),
                RandomMutationSpendingStrategy random => string.Join("\n", DefinitionSchemaVersion, random.GetType().FullName, random.StrategyName),
                _ => throw new NotSupportedException(
                    $"Strategy type '{strategy.GetType().FullName}' needs an explicit definition fingerprint contract.")
            };

            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical)))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        private static string BuildParameterizedDefinition(ParameterizedSpendingStrategy strategy)
        {
            var categories = strategy.PriorityMutationCategories == null
                ? string.Empty
                : string.Join(",", strategy.PriorityMutationCategories.Select(category => ((int)category).ToString(CultureInfo.InvariantCulture)));
            var goals = string.Join(",", strategy.TargetMutationGoals.Select(goal =>
                $"{goal.MutationId.ToString(CultureInfo.InvariantCulture)}:{goal.TargetLevel?.ToString(CultureInfo.InvariantCulture) ?? "max"}"));
            var surges = string.Join(",", strategy.SurgePriorityIds.Select(id => id.ToString(CultureInfo.InvariantCulture)));
            var preferences = string.Join(",", strategy.GetMycovariantPreferences().Select(preference =>
                $"{preference.Priority.ToString(CultureInfo.InvariantCulture)}:{string.Join("+", preference.MycovariantIds.OrderBy(id => id))}"));
            var exclusions = string.Join(",", strategy.ExcludedMutationIds.OrderBy(id => id));

            return string.Join("\n", new[]
            {
                DefinitionSchemaVersion,
                strategy.GetType().FullName ?? strategy.GetType().Name,
                strategy.StrategyName,
                strategy.MaxTier?.ToString() ?? string.Empty,
                strategy.PrioritizeHighTier?.ToString() ?? string.Empty,
                categories,
                goals,
                surges,
                strategy.SurgeAttemptTurnFrequency.ToString(CultureInfo.InvariantCulture),
                strategy.EconomyProfile.ToString(),
                preferences,
                exclusions,
                strategy.StartingSporeEdgeOffset.ToString(CultureInfo.InvariantCulture)
            });
        }
    }
}
