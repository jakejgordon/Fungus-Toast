using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FungusToast.Core.AI
{
    public static class StrategyIdentity
    {
        public const string DefinitionSchemaVersion = "fungus-toast.ai-definition.v2"; // v2: board-state surge opportunity evaluation
        public const string CorpusVersion = "fungus-toast.ai-corpus.phase5-starting-adaptations.v2";

        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> PromotedStableIdsByStrategyName =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Verdant Reclaimer"] = "ai.growth.verdant-reclaimer.v1"
            };

        /// <summary>
        /// Strategies renamed after they accumulated simulation evidence. The legacy name remains
        /// the strategy's identity: it derives the stable ID and feeds the definition fingerprint,
        /// so a rename leaves <c>(strategy_id, fingerprint)</c> — and every artifact comparison keyed
        /// on it — untouched. It also keeps resolving as a lookup alias within its set, so saves,
        /// manifests, and CLI arguments that recorded it still load.
        /// </summary>
        private static readonly StrategyRename[] Renames =
        {
            new(StrategySetEnum.Proven, "Grow>Kill>Reclaim(Econ)", "SporeLedger"),
            new(StrategySetEnum.Proven, "Grow>Kill>Reclaim(Econ/Reclaim)", "ReclaimersLedger"),
            new(StrategySetEnum.Proven, "Mutate>Grow>Kill(Max Econ)", "MutagenBloom"),
            new(StrategySetEnum.Proven, "Creeping>Necrosporulation", "CreepingReclaimer"),
            new(StrategySetEnum.Proven, "Filament Regrowth", "RegrowthLattice"),
            new(StrategySetEnum.Proven, "Power Mutations Max Econ", "RejuvenationEngine"),
            new(StrategySetEnum.Proven, "Growth/Resilience", "RootedCanopy"),
            new(StrategySetEnum.Proven, "Anabolic>Grow>CatabR>PutreRegen", "RebirthFurnace"),
            new(StrategySetEnum.Proven, "Verdant Reclaimer", "VerdantReclaimer"),
            new(StrategySetEnum.Proven, "Grow>Defend>Kill", "PutridTendrils"),
            new(StrategySetEnum.Proven, "Grow>Mutate>Kill(Max Econ)", "AdaptiveBlight"),
            new(StrategySetEnum.Proven, "Best_MaxEcon_Surge10_HyphalSurge", "HyphalPulse")
        };

        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, StrategyRename> RenamesByCurrentName =
            Renames.ToDictionary(rename => rename.CurrentName, StringComparer.OrdinalIgnoreCase);

        public static System.Collections.Generic.IReadOnlyList<StrategyRename> RenamedStrategies => Renames;

        /// <summary>
        /// The name a strategy's stable ID and fingerprint are derived from: its legacy name if it
        /// was renamed, otherwise its current name.
        /// </summary>
        public static string GetIdentityName(IMutationSpendingStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            return RenamesByCurrentName.TryGetValue(strategy.StrategyName, out var rename)
                ? rename.LegacyName
                : strategy.StrategyName;
        }

        /// <summary>Resolves a legacy name recorded before a rename to the strategy's current name in that set.</summary>
        public static bool TryGetCurrentName(StrategySetEnum strategySet, string legacyName, out string currentName)
        {
            var rename = Renames.FirstOrDefault(candidate => candidate.StrategySet == strategySet
                && string.Equals(candidate.LegacyName, legacyName, StringComparison.OrdinalIgnoreCase));
            currentName = rename?.CurrentName ?? string.Empty;
            return rename != null;
        }

        public static string GetStableId(StrategySetEnum strategySet, IMutationSpendingStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            var identityName = GetIdentityName(strategy);
            if (PromotedStableIdsByStrategyName.TryGetValue(identityName, out var promotedId))
            {
                return promotedId;
            }

            var slug = new string(identityName
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
                RandomMutationSpendingStrategy random => string.Join("\n", DefinitionSchemaVersion, random.GetType().FullName, GetIdentityName(random)),
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
                GetIdentityName(strategy),
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

    public sealed class StrategyRename
    {
        public StrategyRename(StrategySetEnum strategySet, string legacyName, string currentName)
        {
            StrategySet = strategySet;
            LegacyName = legacyName;
            CurrentName = currentName;
        }

        public StrategySetEnum StrategySet { get; }
        public string LegacyName { get; }
        public string CurrentName { get; }
    }
}
