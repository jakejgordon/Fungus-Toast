using System;
using System.Collections.Generic;
using FungusToast.Unity;

namespace FungusToast.Unity.UI.Onboarding
{
    public enum NewPlayerTooltipId
    {
        AlphaMutationPhaseIntro,
        MutationTreeGuidance,
        ScoreboardWinCondition,
    }

    public enum NewPlayerTooltipSurface
    {
        PhaseBanner,
        MutationTreeToast,
        SidebarCoachmark,
    }

    public sealed class NewPlayerTooltipDefinition
    {
        public NewPlayerTooltipDefinition(
            NewPlayerTooltipId id,
            string seenKey,
            string title,
            string body,
            NewPlayerTooltipSurface surface,
            string triggerSummary)
        {
            Id = id;
            SeenKey = seenKey ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Surface = surface;
            TriggerSummary = triggerSummary ?? string.Empty;
        }

        public NewPlayerTooltipId Id { get; }
        public string SeenKey { get; }
        public string Title { get; }
        public string Body { get; }
        public NewPlayerTooltipSurface Surface { get; }
        public string TriggerSummary { get; }
    }

    public static class NewPlayerTooltipCatalog
    {
        private static readonly IReadOnlyList<NewPlayerTooltipDefinition> Definitions = new[]
        {
            new NewPlayerTooltipDefinition(
                NewPlayerTooltipId.AlphaMutationPhaseIntro,
                "Onboarding.AlphaMutationPhaseSeen",
                "Mutation Phase",
                "Goal: control the largest share of the toast.\nSpend mutation points for upgrades now or store them to save for stronger upgrades later.\nAfter that, your colony grows automatically.",
                NewPlayerTooltipSurface.PhaseBanner,
                "Queue on round 1 when at least one human player is present and the game is not fast-forwarding; skip persisted seen-state checks only during forced first-game experience, and otherwise suppress in testing mode or after it has already been seen."),
            new NewPlayerTooltipDefinition(
                NewPlayerTooltipId.MutationTreeGuidance,
                "Onboarding.AlphaMutationTreeGuidanceSeen",
                "Mutation Tree Guidance",
                "Hover upgrades to inspect them, then click an affordable one to buy it.\n\nIf you want stronger upgrades later, use Store Mutation Points at the top of this panel.",
                NewPlayerTooltipSurface.MutationTreeToast,
                "Show when the mutation tree is opened for a player who has not dismissed it this game; skip persisted seen-state checks only during forced first-game experience, and otherwise show once per profile."),
            new NewPlayerTooltipDefinition(
                NewPlayerTooltipId.ScoreboardWinCondition,
                "Onboarding.ScoreboardWinConditionSeen",
                "How to Win",
                "This scoreboard is the clearest way to see who is ahead.\n\nWatch the Alive column. When the toast fills up and the game ends, the colony with the most living cells wins.",
                NewPlayerTooltipSurface.SidebarCoachmark,
                "Show on round 2 or later unless dismissed this game or while fast-forwarding; skip persisted seen-state checks only during forced first-game experience, and otherwise show once per profile."),
        };

        private static readonly Dictionary<NewPlayerTooltipId, NewPlayerTooltipDefinition> DefinitionsById = BuildDefinitionsById();

        public static IReadOnlyList<NewPlayerTooltipDefinition> All => Definitions;

        public static NewPlayerTooltipDefinition Get(NewPlayerTooltipId id)
        {
            if (!DefinitionsById.TryGetValue(id, out NewPlayerTooltipDefinition definition))
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown new-player tooltip id.");
            }

            return definition;
        }

        public static bool HasBeenSeen(NewPlayerTooltipId id)
        {
            return ScopedPlayerPrefs.GetInt(Get(id).SeenKey, 0) != 0;
        }

        public static void MarkSeen(NewPlayerTooltipId id)
        {
            ScopedPlayerPrefs.SetInt(Get(id).SeenKey, 1);
            ScopedPlayerPrefs.Save();
        }

        public static void ResetAllSeenFlags()
        {
            foreach (NewPlayerTooltipDefinition definition in Definitions)
            {
                ScopedPlayerPrefs.SetInt(definition.SeenKey, 0);
            }

            ScopedPlayerPrefs.Save();
        }

        private static Dictionary<NewPlayerTooltipId, NewPlayerTooltipDefinition> BuildDefinitionsById()
        {
            var lookup = new Dictionary<NewPlayerTooltipId, NewPlayerTooltipDefinition>();
            foreach (NewPlayerTooltipDefinition definition in Definitions)
            {
                lookup[definition.Id] = definition;
            }

            return lookup;
        }
    }

    public static class NewPlayerTooltipRules
    {
        public static bool ShouldQueueAlphaMutationPhaseIntro(
            bool forceFirstGameExperience,
            int currentRound,
            int humanPlayerCount,
            bool isFastForwarding,
            bool testingModeEnabled)
        {
            if (currentRound != 1 || humanPlayerCount <= 0 || isFastForwarding)
            {
                return false;
            }

            if (forceFirstGameExperience)
            {
                return true;
            }

            return !testingModeEnabled && !NewPlayerTooltipCatalog.HasBeenSeen(NewPlayerTooltipId.AlphaMutationPhaseIntro);
        }

        public static bool ShouldShowMutationTreeGuidance(bool forceFirstGameExperience, bool hasDismissedThisGame)
        {
            if (hasDismissedThisGame)
            {
                return false;
            }

            return forceFirstGameExperience || !NewPlayerTooltipCatalog.HasBeenSeen(NewPlayerTooltipId.MutationTreeGuidance);
        }

        public static bool ShouldShowScoreboardWinCondition(
            bool forceFirstGameExperience,
            int currentRound,
            bool hasDismissedThisGame,
            bool isFastForwarding)
        {
            if (currentRound < 2 || hasDismissedThisGame || isFastForwarding)
            {
                return false;
            }

            return forceFirstGameExperience || !NewPlayerTooltipCatalog.HasBeenSeen(NewPlayerTooltipId.ScoreboardWinCondition);
        }
    }
}
