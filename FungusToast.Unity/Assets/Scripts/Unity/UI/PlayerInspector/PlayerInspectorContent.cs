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
        /// The name the inspector shows for a player. AI players are constructed with a slot
        /// name ("AI Player 2") that means nothing to the person playing, so an AI is shown under
        /// its roster fantasy name whenever its strategy has one. Humans keep their given name.
        /// </summary>
        public static string GetDisplayName(Player? player, GameManager? manager)
        {
            if (player == null)
            {
                return "(unset)";
            }

            if (player.PlayerType == PlayerTypeEnum.Human)
            {
                return player.PlayerName;
            }

            string? friendlyName = GetStrategyCatalogEntry(player, manager)?.FriendlyName;
            return string.IsNullOrWhiteSpace(friendlyName) ? player.PlayerName : friendlyName!;
        }

        /// <summary>
        /// The always-visible player summary: highest mutation, mycovariants, adaptations, plus
        /// the opponent's style note once Strain Profiling is unlocked. The player's name is not
        /// a line here; the surface shows <see cref="GetDisplayName"/> as its title.
        /// Set <paramref name="includeTraitLines"/> to false on a surface that renders the owned
        /// mycovariants and adaptations as icon grids instead, so the same names are not listed twice.
        /// </summary>
        public static IReadOnlyList<PlayerInspectorSection> BuildSummarySections(
            Player? player,
            GameManager? manager,
            bool includeTraitLines = true)
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
                lines.Add(new PlayerInspectorLine("Strategy", campaignAiProfile.AIPlayerIntentions));
                lines.Add(new PlayerInspectorLine("Campaign Unlock", "Strain Profiling"));
            }
            else if (IsDevelopmentTestingEnabled(manager))
            {
                lines.Add(new PlayerInspectorLine("Strategy", GetStrategyDisplayText(player)));
            }

            lines.Add(new PlayerInspectorLine("Highest Mutation", GetHighestMutationText(player)));

            if (includeTraitLines)
            {
                lines.Add(new PlayerInspectorLine("Mycovariants", GetMycovariantsList(player)));
                lines.Add(new PlayerInspectorLine("Adaptations", GetAdaptationsList(player)));
            }

            return new[] { new PlayerInspectorSection(null, lines) };
        }

        /// <summary>
        /// The player's owned adaptations, for a surface that renders them as icons. Entries with a
        /// missing definition are dropped so a caller can assume <c>Adaptation</c> is non-null.
        /// </summary>
        public static IReadOnlyList<PlayerAdaptation> GetOwnedAdaptations(Player? player)
        {
            if (player?.PlayerAdaptations == null)
            {
                return System.Array.Empty<PlayerAdaptation>();
            }

            return player.PlayerAdaptations.Where(pa => pa?.Adaptation != null).ToList();
        }

        /// <summary>
        /// The player's currently active surges, for a surface that renders them as icons. Sorted
        /// by mutation name so tiles do not reorder as one surge expires; entries whose mutation
        /// is unknown to the repository are dropped.
        /// </summary>
        public static IReadOnlyList<Player.ActiveSurgeInfo> GetActiveSurges(Player? player)
        {
            if (player?.ActiveSurges == null || player.ActiveSurges.Count == 0)
            {
                return System.Array.Empty<Player.ActiveSurgeInfo>();
            }

            return player.ActiveSurges.Values
                .Where(s => s != null && s.TurnsRemaining > 0 && MutationRepository.All.ContainsKey(s.MutationId))
                .OrderBy(s => GetMutationName(s.MutationId))
                .ToList();
        }

        /// <summary>
        /// The player's owned mycovariants, for a surface that renders them as icons. Entries with a
        /// missing definition are dropped so a caller can assume <c>Mycovariant</c> is non-null.
        /// </summary>
        public static IReadOnlyList<PlayerMycovariant> GetOwnedMycovariants(Player? player)
        {
            if (player?.PlayerMycovariants == null)
            {
                return System.Array.Empty<PlayerMycovariant>();
            }

            return player.PlayerMycovariants.Where(pm => pm?.Mycovariant != null).ToList();
        }

        /// <summary>
        /// Development Testing only. Exposes the tuning parameters an AI strategy was declared
        /// with, so a tester can tell which roster slot they are facing and how it was configured.
        /// </summary>
        public static IReadOnlyList<PlayerInspectorSection> BuildDevelopmentSections(Player? player, int currentRound)
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
                new PlayerInspectorLine("Strategy", GetStrategyDisplayText(player)),
                new PlayerInspectorLine("Score", player.Score.ToString()),
                new PlayerInspectorLine("Controlled Tiles", player.ControlledTileIds.Count.ToString()),
                new PlayerInspectorLine(
                    "Starting Tile",
                    player.StartingTileId.HasValue ? $"#{player.StartingTileId.Value}" : "None")
            };
            sections.Add(new PlayerInspectorSection("Dev — Identity", identity));

            sections.Add(new PlayerInspectorSection(
                "Dev — Decision State",
                new[]
                {
                    new PlayerInspectorLine("Mutation Points", player.MutationPoints.ToString()),
                    new PlayerInspectorLine("Mutation Income", player.GetMutationPointIncome().ToString()),
                    new PlayerInspectorLine("Orthogonal Growth", $"{player.GetEffectiveGrowthChance():P2}"),
                    new PlayerInspectorLine("Random Decay", $"{player.GetEffectiveRandomDecayChance(currentRound):P2}"),
                    new PlayerInspectorLine(
                        "Banking Intent",
                        player.WantsToBankPointsThisTurn
                            ? $"Yes ({player.MutationPoints} stored)"
                            : "No")
                }));

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

            var ledgerLines = BuildMutationLedgerLines(player);
            if (ledgerLines.Count > 0)
            {
                sections.Add(new PlayerInspectorSection("Dev — Mutation Ledger", ledgerLines));
            }

            if (strategy is ParameterizedSpendingStrategy withPreferences)
            {
                var goalLines = BuildTargetGoalLines(player, withPreferences);
                if (goalLines.Count > 0)
                {
                    sections.Add(new PlayerInspectorSection("Dev — Mutation Plan", goalLines));
                }

                var preferenceLines = BuildMycovariantPreferenceLines(player, withPreferences);
                if (preferenceLines.Count > 0)
                {
                    sections.Add(new PlayerInspectorSection("Dev — Mycovariant Plan", preferenceLines));
                }
            }

            return sections;
        }

        /// <summary>
        /// Shows the executable goal list rather than re-authoring an inspector-only plan. The first
        /// incomplete goal is marked as active so a tester can distinguish current intent from later
        /// goals without changing AI behavior.
        /// </summary>
        private static List<PlayerInspectorLine> BuildTargetGoalLines(
            Player player,
            ParameterizedSpendingStrategy strategy)
        {
            var goals = strategy.TargetMutationGoals;
            var lines = new List<PlayerInspectorLine>();
            bool markedActiveGoal = false;

            for (int i = 0; i < goals.Count && i < MaxListedItems; i++)
            {
                TargetMutationGoal goal = goals[i];
                int currentLevel = player.GetMutationLevel(goal.MutationId);
                int targetLevel = MutationRepository.All.TryGetValue(goal.MutationId, out Mutation mutation)
                    ? System.Math.Min(goal.TargetLevel ?? mutation.MaxLevel, mutation.MaxLevel)
                    : goal.TargetLevel ?? 1;
                bool complete = currentLevel >= targetLevel;
                string marker;

                if (complete)
                {
                    marker = "[x]";
                }
                else if (!markedActiveGoal)
                {
                    marker = "[>]";
                    markedActiveGoal = true;
                }
                else
                {
                    marker = "[ ]";
                }

                lines.Add(PlayerInspectorLine.Plain(
                    $"{marker} {GetMutationName(goal.MutationId)}: {currentLevel}/{targetLevel}"));
            }

            if (goals.Count > MaxListedItems)
            {
                lines.Add(PlayerInspectorLine.Plain($"…and {goals.Count - MaxListedItems} more"));
            }

            return lines;
        }

        private static List<PlayerInspectorLine> BuildMutationLedgerLines(Player player)
        {
            var acquired = player.PlayerMutations.Values
                .Where(pm => pm.CurrentLevel > 0)
                .OrderBy(pm => pm.FirstUpgradeRound ?? int.MaxValue)
                .ThenBy(pm => pm.Mutation.TierNumber)
                .ThenBy(pm => pm.Mutation.Name)
                .ToList();
            var lines = new List<PlayerInspectorLine>();

            foreach (var playerMutation in acquired.Take(MaxListedItems))
            {
                string acquiredRound = playerMutation.FirstUpgradeRound.HasValue
                    ? $"round {playerMutation.FirstUpgradeRound.Value}"
                    : "round unknown";
                lines.Add(PlayerInspectorLine.Plain(
                    $"{playerMutation.Mutation.Name}: Lv {playerMutation.CurrentLevel}, {acquiredRound}"));
            }

            if (acquired.Count > MaxListedItems)
            {
                lines.Add(PlayerInspectorLine.Plain($"…and {acquired.Count - MaxListedItems} more"));
            }

            return lines;
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

            for (int i = 0; i < preferences.Count && i < MaxListedItems; i++)
            {
                MycovariantPreference preference = preferences[i];
                int rank = i + 1;

                if (preference.IsCategoryDerived)
                {
                    // A category set can hold well over a dozen ids, so list what actually
                    // converted rather than every option.
                    var drafted = preference.MycovariantIds.Where(ownedIds.Contains).ToList();
                    string draftedNames = drafted.Count == 0
                        ? "none yet"
                        : string.Join(", ", drafted.Select(GetMycovariantName));
                    lines.Add(PlayerInspectorLine.Plain(
                        $"#{rank} Category set ({preference.MycovariantIds.Count} options, best available)"));
                    lines.Add(PlayerInspectorLine.Plain(
                        $"    drafted {drafted.Count}/{preference.MycovariantIds.Count}: {draftedNames}"));
                    continue;
                }

                string names = string.Join(" / ", preference.MycovariantIds.Select(GetMycovariantName));
                bool satisfied = preference.MycovariantIds.Any(ownedIds.Contains);
                string marker = satisfied ? "[x]" : "[ ]";
                lines.Add(PlayerInspectorLine.Plain(
                    $"#{rank} {marker} {names}{FormatPreferenceDescription(preference, rank)}"));
            }

            if (preferences.Count > MaxListedItems)
            {
                lines.Add(PlayerInspectorLine.Plain($"…and {preferences.Count - MaxListedItems} more"));
            }

            return lines;
        }

        /// <summary>
        /// Preferences built from a plain id list get an auto-generated "Preferred #N" description
        /// that only restates the rank already shown. Suppress those and keep authored ones.
        /// </summary>
        private static string FormatPreferenceDescription(MycovariantPreference preference, int rank)
        {
            if (string.IsNullOrWhiteSpace(preference.Description)
                || preference.Description == $"Preferred #{rank}")
            {
                return string.Empty;
            }

            return $" — {preference.Description}";
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

        /// <summary>
        /// The catalog entry behind an AI player's strategy. The active mode's roster is checked
        /// first; the other sets are fallbacks so a proven strategy dropped into a campaign preset
        /// (or a campaign strategy in a quick game) still resolves its presentation.
        /// </summary>
        private static StrategyCatalogEntry? GetStrategyCatalogEntry(Player player, GameManager? manager)
        {
            string? strategyName = player.MutationStrategy?.StrategyName;
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                return null;
            }

            bool isCampaign = manager != null && manager.CurrentGameMode == GameMode.Campaign;
            StrategySetEnum preferredSet = isCampaign ? StrategySetEnum.Campaign : StrategySetEnum.Proven;

            StrategyCatalogEntry? entry = AIRoster.GetStrategyCatalogEntry(preferredSet, strategyName!);
            if (entry != null)
            {
                return entry;
            }

            foreach (StrategySetEnum set in (StrategySetEnum[])System.Enum.GetValues(typeof(StrategySetEnum)))
            {
                if (set == preferredSet)
                {
                    continue;
                }

                entry = AIRoster.GetStrategyCatalogEntry(set, strategyName!);
                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// The opponent's style note, shown only in campaign games once Strain Profiling is
        /// unlocked. The fantasy name itself is not gated; see <see cref="GetDisplayName"/>.
        /// </summary>
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

            var entry = GetStrategyCatalogEntry(player, manager);
            if (entry == null || string.IsNullOrWhiteSpace(entry.AIPlayerIntentions))
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
