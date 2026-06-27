using FungusToast.Core.AI;
using FungusToast.Core.Board;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Campaign
{
    public static class CampaignStartingPositionDifficultyResolver
    {
        public static IReadOnlyList<(int x, int y)> GetAllowedStartingPositions(
            CampaignBoardStartingPositionMetadata metadata,
            CampaignDifficulty difficulty)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (metadata.Entries.Count == 0)
            {
                return Array.Empty<(int x, int y)>();
            }

            var orderedEntries = metadata.Entries
                .OrderBy(entry => entry.FavorRank)
                .ThenByDescending(entry => entry.WinPercentage)
                .ThenBy(entry => entry.SlotIndex)
                .ToArray();

            int windowSize = Math.Max(1, (int)Math.Ceiling(orderedEntries.Length / 2.0));
            int maxStartIndex = Math.Max(0, orderedEntries.Length - windowSize);
            int difficultyCount = Enum.GetValues(typeof(CampaignDifficulty)).Length;
            int difficultyIndex = Math.Clamp((int)difficulty, 0, Math.Max(0, difficultyCount - 1));
            int startIndex = maxStartIndex == 0
                ? 0
                : (int)Math.Round(
                    difficultyIndex * (maxStartIndex / (double)Math.Max(1, difficultyCount - 1)),
                    MidpointRounding.AwayFromZero);

            return orderedEntries
                .Skip(startIndex)
                .Take(windowSize)
                .Select(entry => (entry.X, entry.Y))
                .ToArray();
        }
    }
}
