#nullable enable

using System;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Shared copy rules for showing an active Mycelial Surge outside the mutation tree. The
    /// sidebar, the player inspector, and the tree glyphs all describe the same
    /// <see cref="Player.ActiveSurgeInfo"/>, so the wording lives here rather than per surface.
    /// </summary>
    public static class SurgePresentation
    {
        private const string TechnicalMarker = "<b>Technical:</b>";

        public static string SectionLabel => "Active Surges";

        public static string FormatRoundsRemaining(int rounds) =>
            rounds == 1 ? "1 round left" : $"{rounds} rounds left";

        /// <summary>
        /// The plain first paragraph of a mutation description. Every surge description is
        /// authored as "one plain sentence, blank line, <b>Technical:</b> …", and the technical
        /// block is what the mutation tree inspector is for; a hover elsewhere only needs the
        /// sentence.
        /// </summary>
        public static string GetPlainSummary(Mutation? mutation)
        {
            string description = mutation?.Description ?? string.Empty;
            int technicalIndex = description.IndexOf(TechnicalMarker, StringComparison.Ordinal);
            if (technicalIndex >= 0)
            {
                description = description.Substring(0, technicalIndex);
            }

            int paragraphBreak = description.IndexOf("\n\n", StringComparison.Ordinal);
            if (paragraphBreak >= 0)
            {
                description = description.Substring(0, paragraphBreak);
            }

            return description.Trim();
        }

        /// <summary>
        /// Tooltip body for one active surge, in the same "name / italic kind line / blank /
        /// body" shape the adaptation and mycovariant tooltips use.
        /// </summary>
        public static string BuildTooltip(Player.ActiveSurgeInfo? surge)
        {
            if (surge == null || !MutationRepository.All.TryGetValue(surge.MutationId, out Mutation mutation))
            {
                return "<b>Mycelial Surge</b>\nUnset";
            }

            string kindLine = $"Mycelial Surge · Level {surge.Level} · {FormatRoundsRemaining(surge.TurnsRemaining)}";
            string summary = GetPlainSummary(mutation);
            return string.IsNullOrEmpty(summary)
                ? $"<b>{mutation.Name}</b>\n<i>{kindLine}</i>"
                : $"<b>{mutation.Name}</b>\n<i>{kindLine}</i>\n\n{summary}";
        }
    }
}
