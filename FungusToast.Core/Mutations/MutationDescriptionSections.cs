using System;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations
{
    /// <summary>
    /// Structured, presentation-ready sections parsed from the canonical mutation description format.
    /// Keeps simple and technical UI surfaces sourced from the same authored description.
    /// </summary>
    public sealed class MutationDescriptionSections
    {
        private const string TechnicalMarker = "<b>Technical:</b>";
        private const string MaxLevelBonusMarker = "<b>Max Level Bonus:</b>";
        private const string BuffedByMarker = "Buffed by:";

        public string Summary { get; }
        public string TechnicalDetails { get; }
        public string MaxLevelBonus { get; }
        public IReadOnlyList<string> BuffingMutations { get; }

        public bool HasTechnicalDetails => !string.IsNullOrWhiteSpace(TechnicalDetails);
        public bool HasMaxLevelBonus => !string.IsNullOrWhiteSpace(MaxLevelBonus);
        public bool HasBuffingMutations => BuffingMutations.Count > 0;

        private MutationDescriptionSections(
            string summary,
            string technicalDetails,
            string maxLevelBonus,
            IReadOnlyList<string> buffingMutations)
        {
            Summary = summary;
            TechnicalDetails = technicalDetails;
            MaxLevelBonus = maxLevelBonus;
            BuffingMutations = buffingMutations;
        }

        public static MutationDescriptionSections Parse(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return new MutationDescriptionSections(string.Empty, string.Empty, string.Empty, Array.Empty<string>());
            }

            string normalized = description.Replace("\r\n", "\n").Trim();
            int technicalMarkerIndex = normalized.IndexOf(TechnicalMarker, StringComparison.Ordinal);
            if (technicalMarkerIndex < 0)
            {
                return new MutationDescriptionSections(normalized, string.Empty, string.Empty, Array.Empty<string>());
            }

            string summary = normalized.Substring(0, technicalMarkerIndex).Trim();
            string sectionText = normalized.Substring(technicalMarkerIndex + TechnicalMarker.Length).Trim();
            var technicalLines = new List<string>();
            var maxLevelBonusLines = new List<string>();
            var buffingMutations = new List<string>();
            var seenBuffingMutations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DescriptionSection activeSection = DescriptionSection.Technical;

            foreach (string rawLine in sectionText.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.StartsWith(MaxLevelBonusMarker, StringComparison.Ordinal))
                {
                    activeSection = DescriptionSection.MaxLevelBonus;
                    AddContent(maxLevelBonusLines, line.Substring(MaxLevelBonusMarker.Length));
                    continue;
                }

                if (line.StartsWith(BuffedByMarker, StringComparison.Ordinal))
                {
                    activeSection = DescriptionSection.BuffedBy;
                    AddBuffingMutation(buffingMutations, seenBuffingMutations, line.Substring(BuffedByMarker.Length));
                    continue;
                }

                switch (activeSection)
                {
                    case DescriptionSection.Technical:
                        AddContent(technicalLines, line);
                        break;
                    case DescriptionSection.MaxLevelBonus:
                        AddContent(maxLevelBonusLines, line);
                        break;
                    case DescriptionSection.BuffedBy:
                        AddBuffingMutation(buffingMutations, seenBuffingMutations, line);
                        break;
                }
            }

            return new MutationDescriptionSections(
                summary,
                string.Join("\n", technicalLines),
                string.Join("\n", maxLevelBonusLines),
                buffingMutations);
        }

        private static void AddContent(List<string> lines, string value)
        {
            string trimmed = value.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                lines.Add(trimmed);
            }
        }

        private static void AddBuffingMutation(
            List<string> buffingMutations,
            HashSet<string> seenBuffingMutations,
            string value)
        {
            string trimmed = value.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && seenBuffingMutations.Add(trimmed))
            {
                buffingMutations.Add(trimmed);
            }
        }

        private enum DescriptionSection
        {
            Technical,
            MaxLevelBonus,
            BuffedBy
        }
    }
}
