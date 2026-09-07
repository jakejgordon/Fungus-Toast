#nullable enable

using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.MutationTree;

namespace FungusToast.Unity.UI.PlayerInspector
{
    /// <summary>
    /// Single source of truth for the content shown when inspecting a player's mold icon.
    /// The hover tooltip renders these sections today; a docked player inspector can render
    /// the same sections later without duplicating the derivation rules.
    /// </summary>
    public static class PlayerInspectorContent
    {
        /// <summary>Long lists are truncated so a dev block stays readable inside a tooltip.</summary>
        private const int MaxListedItems = 6;

        /// <summary>
        /// The always-visible player summary: identity, highest mutation, mycovariants, adaptations.
        /// </summary>
        public static IReadOnlyList<PlayerInspectorSection> BuildSummarySections(Player? player, GameManager? manager)
        {
            if (player == null)
            {
                return new[]
                {
                    new PlayerInspectorSection(null, new[] { PlayerInspectorLine.Plain("Player: (unset)") })
                };
            }

            var lines = new List<PlayerInspectorLine>();
            StrategyCatalogEntry? campaignAiProfile = GetVisibleCampaignAiProfile(player, manager);

            if (campaignAiProfile != null)
            {
                lines.Add(new PlayerInspectorLine("Opponent", campaignAiProfile.FriendlyName));
                lines.Add(new PlayerInspectorLine("Strategy", campaignAiProfile.AIPlayerIntentions));
                lines.Add(new PlayerInspectorLine("Campaign Unlock", "Strain Profiling"));
            }
            else
            {
                lines.Add(new PlayerInspectorLine("Player Name", player.PlayerName));

                if (IsDevelopmentTestingEnabled(manager))
                {
                    lines.Add(new PlayerInspectorLine("Strategy", GetStrategyDisplayText(player)));
                }
            }

            lines.Add(new PlayerInspectorLine("Highest Mutation", GetHighestMutationText(player)));
            lines.Add(new PlayerInspectorLine("Mycovariants", GetMycovariantsList(player)));
            lines.Add(new PlayerInspectorLine("Adaptations", GetAdaptationsList(player)));

            return new[] { new PlayerInspectorSection(null, lines) };
        }

        /// <summary>
        /// Development Testing only. Exposes the tuning parameters an AI strategy was declared
        /// with, so a tester can tell which roster slot they are facing and how it was configured.
        /// </summary>
        public static IReadOnlyList<PlayerInspectorSection> BuildDevelopmentSections(Player? player)
        {
            if (player == null)
            {
                return System.Array.Empty<PlayerInspectorSection>();
            }

            var sections = new List<PlayerInspectorSection>();
            IMutationSpendingStrategy? strategy = player.MutationStrategy;

            var identity = new List<PlayerInspectorLine>
            {
                new PlayerInspectorLine("Player Type", player.PlayerType.ToString()),
                new PlayerInspectorLine("AI Type", player.AIType.ToString()),
                new PlayerInspectorLine("Strategy", GetStrategyDisplayText(player)),
                new PlayerInspectorLine("Score", player.Score.ToString()),
                new PlayerInspectorLine("Controlled Tiles", player.ControlledTileIds.Count.ToString()),
                new PlayerInspectorLine(
                    "Starting Tile",
                    player.StartingTileId.HasValue ? $"#{player.StartingTileId.Value}" : "None")
            };
            sections.Add(new PlayerInspectorSection("Dev — Identity", identity));

            if (strategy == null)
            {
                return sections;
            }

            var tuning = new List<PlayerInspectorLine>
            {
                new PlayerInspectorLine(
                    "Max Tier",
                    strategy.MaxTier.HasValue ? $"Tier {(int)strategy.MaxTier.Value}" : "Unbounded"),
                new PlayerInspectorLine("Prioritize High Tier", FormatNullableBool(strategy.PrioritizeHighTier))
            };

            if (strategy is ParameterizedSpendingStrategy parameterized)
            {
                tuning.Add(new PlayerInspectorLine("Economy Bias", parameterized.EconomyProfile.ToString()));
                tuning.Add(new PlayerInspectorLine(
                    "Priority Categories",
                    FormatCategories(parameterized.PriorityMutationCategories)));
                tuning.Add(new PlayerInspectorLine(
                    "Surge Attempt Frequency",
                    $"every {parameterized.SurgeAttemptTurnFrequency} turn(s)"));
                tuning.Add(new PlayerInspectorLine(
                    "Starting Spore Edge Offset",
                    parameterized.StartingSporeEdgeOffset.ToString()));
            }

            tuning.Add(new PlayerInspectorLine("Excluded Mutations", FormatMutationIds(strategy.ExcludedMutationIds)));
            sections.Add(new PlayerInspectorSection("Dev — Strategy Tuning", tuning));

            if (strategy is ParameterizedSpendingStrategy withPreferences)
            {
                var preferenceLines = BuildMycovariantPreferenceLines(player, withPreferences);
                if (preferenceLines.Count > 0)
                {
                    sections.Add(new PlayerInspectorSection("Dev — Mycovariant Plan", preferenceLines));
                }
            }

            return sections;
        }

        public static bool IsDevelopmentTestingEnabled(GameManager? manager) =>
            manager != null && manager.IsTestingModeEnabled;

        /// <summary>
        /// Each preference is marked against what the player actually drafted, so a plan that
        /// never converts is visible without cross-referencing the draft log.
        /// </summary>
        private static List<PlayerInspectorLine> BuildMycovariantPreferenceLines(
            Player player,
            ParameterizedSpendingStrategy strategy)
        {
            var lines = new List<PlayerInspectorLine>();
            List<MycovariantPreference> preferences = strategy.GetMycovariantPreferences();

            if (preferences.Count == 0)
            {
                lines.Add(PlayerInspectorLine.Plain("No declared preferences (random draft)."));
                return lines;
            }

            var ownedIds = new HashSet<int>(
                player.PlayerMycovariants
                    .Where(pm => pm.Mycovariant != null)
                    .Select(pm => pm.Mycovariant.Id));

            foreach (var preference in preferences.Take(MaxListedItems))
            {
                string names = string.Join(" / ", preference.MycovariantIds.Select(GetMycovariantName));
                bool satisfied = preference.MycovariantIds.Any(ownedIds.Contains);
                string marker = satisfied ? "[x]" : "[ ]";
                string description = string.IsNullOrWhiteSpace(preference.Description)
                    ? string.Empty
                    : $" — {preference.Description}";
                lines.Add(PlayerInspectorLine.Plain($"{marker} P{preference.Priority}: {names}{description}"));
            }

            if (preferences.Count > MaxListedItems)
            {
                lines.Add(PlayerInspectorLine.Plain($"…and {preferences.Count - MaxListedItems} more"));
            }

            return lines;
        }

        private static string FormatNullableBool(bool? value) =>
            value.HasValue ? (value.Value ? "Yes" : "No") : "Unset";

        private static string FormatCategories(IReadOnlyList<MutationCategory>? categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", categories.Select(c => MutationCategoryPresentationCatalog.Get(c).DisplayName));
        }

        private static string FormatMutationIds(IReadOnlyCollection<int>? mutationIds)
        {
            if (mutationIds == null || mutationIds.Count == 0)
            {
                return "None";
            }

            var names = mutationIds.Take(MaxListedItems).Select(GetMutationName).ToList();
            if (mutationIds.Count > MaxListedItems)
            {
                names.Add($"…and {mutationIds.Count - MaxListedItems} more");
            }

            return string.Join(", ", names);
        }

        private static string GetMutationName(int mutationId) =>
            MutationRepository.All.TryGetValue(mutationId, out Mutation mutation) ? mutation.Name : $"#{mutationId}";

        private static string GetMycovariantName(int mycovariantId) =>
            MycovariantRepository.All.FirstOrDefault(m => m.Id == mycovariantId)?.Name ?? $"#{mycovariantId}";

        private static string GetStrategyDisplayText(Player player)
        {
            if (player.PlayerType == PlayerTypeEnum.Human)
            {
                return "Human Player";
            }

            return string.IsNullOrWhiteSpace(player.MutationStrategy?.StrategyName)
                ? "Unassigned"
                : player.MutationStrategy!.StrategyName;
        }

        private static StrategyCatalogEntry? GetVisibleCampaignAiProfile(Player player, GameManager? manager)
        {
            if (manager == null || manager.CurrentGameMode != GameMode.Campaign)
            {
                return null;
            }

            if (player.PlayerType == PlayerTypeEnum.Human)
            {
                return null;
            }

            var campaignController = manager.CampaignController;
            if (campaignController == null
                || !campaignController.HasUnlockedMoldinessReward(MoldinessUnlockCatalog.StrainProfilingRewardId))
            {
                return null;
            }

            string? strategyName = player.MutationStrategy?.StrategyName;
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                return null;
            }

            var entry = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, strategyName);
            if (entry == null
                || string.IsNullOrWhiteSpace(entry.FriendlyName)
                || string.IsNullOrWhiteSpace(entry.AIPlayerIntentions))
            {
                return null;
            }

            return entry;
        }

        private static string GetHighestMutationText(Player player)
        {
            if (player.PlayerMutations == null || player.PlayerMutations.Count == 0)
            {
                return "None";
            }

            var top = player.PlayerMutations.Values
                .Where(pm => pm.CurrentLevel > 0)
                .OrderByDescending(pm => pm.Mutation.TierNumber)
                .ThenByDescending(pm => pm.CurrentLevel)
                .FirstOrDefault();

            if (top == null)
            {
                return "None";
            }

            return $"Tier {top.Mutation.TierNumber} – {top.Mutation.Name} (Lv {top.CurrentLevel})";
        }

        private static string GetMycovariantsList(Player player)
        {
            if (player.PlayerMycovariants == null || player.PlayerMycovariants.Count == 0)
            {
                return "None";
            }

            var names = player.PlayerMycovariants
                .Select(pm => pm.Mycovariant?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            return names.Count == 0 ? "None" : string.Join(", ", names);
        }

        private static string GetAdaptationsList(Player player)
        {
            if (player.PlayerAdaptations == null || player.PlayerAdaptations.Count == 0)
            {
                return "None";
            }

            var names = player.PlayerAdaptations
                .Select(pa => pa.Adaptation?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            return names.Count == 0 ? "None" : string.Join(", ", names);
        }
    }
}
