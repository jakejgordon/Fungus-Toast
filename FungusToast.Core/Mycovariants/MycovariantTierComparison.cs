using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Draft offers can put two tiers of one family side by side (Ballistospore Discharge I and
    /// II), whose descriptions differ only in a number or two. This finds those numbers so the
    /// draft card can emphasize them, instead of the player rereading two near-identical
    /// sentences to spot "12" versus "17".
    /// </summary>
    public static class MycovariantTierComparison
    {
        private static readonly Regex TierSuffix = new Regex(@"\s+(?:I|II|III|IV|V)$", RegexOptions.CultureInvariant);
        private static readonly Regex NumberToken = new Regex(@"\d+(?:\.\d+)?%?", RegexOptions.CultureInvariant);

        /// <summary>The family name without its trailing roman tier ("Ballistospore Discharge").</summary>
        public static string GetFamilyName(string name)
            => string.IsNullOrWhiteSpace(name) ? string.Empty : TierSuffix.Replace(name.Trim(), string.Empty);

        /// <summary>
        /// Returns <paramref name="descriptions"/> with <paramref name="emphasize"/> applied to each
        /// number that differs between offered tiers of the same family. A description is left
        /// untouched unless it shares a family with another offer and the two read identically
        /// apart from their numbers - if the prose itself differs, the player has to read it anyway
        /// and emphasizing a stray number would only mislead.
        /// </summary>
        public static string[] EmphasizeTierDifferences(
            IReadOnlyList<string> names,
            IReadOnlyList<string> descriptions,
            Func<string, string> emphasize)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            if (descriptions == null) throw new ArgumentNullException(nameof(descriptions));
            if (emphasize == null) throw new ArgumentNullException(nameof(emphasize));
            if (names.Count != descriptions.Count)
            {
                throw new ArgumentException("Every offer needs a name and a description.", nameof(descriptions));
            }

            var result = descriptions.Select(description => description ?? string.Empty).ToArray();

            var families = Enumerable.Range(0, names.Count)
                .Where(index => TierSuffix.IsMatch(names[index]?.Trim() ?? string.Empty))
                .GroupBy(index => GetFamilyName(names[index]), StringComparer.Ordinal)
                .Where(group => group.Count() > 1);

            foreach (var family in families)
            {
                int[] members = family.ToArray();
                var numbers = members.Select(index => NumberToken.Matches(result[index]).Cast<Match>().ToArray()).ToArray();
                var prose = members.Select(index => NumberToken.Replace(result[index], "#")).ToArray();

                if (prose.Any(text => text != prose[0]))
                {
                    continue;
                }

                var differingPositions = Enumerable.Range(0, numbers[0].Length)
                    .Where(position => numbers.Any(tokens => tokens[position].Value != numbers[0][position].Value))
                    .ToHashSet();
                if (differingPositions.Count == 0)
                {
                    continue;
                }

                for (int member = 0; member < members.Length; member++)
                {
                    int position = 0;
                    result[members[member]] = NumberToken.Replace(
                        result[members[member]],
                        match => differingPositions.Contains(position++) ? emphasize(match.Value) : match.Value);
                }
            }

            return result;
        }
    }
}
