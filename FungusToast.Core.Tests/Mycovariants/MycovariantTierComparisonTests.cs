using System.Linq;
using FungusToast.Core.Mycovariants;
using Xunit;

namespace FungusToast.Core.Tests.Mycovariants
{
    public class MycovariantTierComparisonTests
    {
        private static string Mark(string value) => $"[{value}]";

        [Fact]
        public void Sibling_tiers_emphasize_only_the_number_that_differs()
        {
            var result = MycovariantTierComparison.EmphasizeTierDifferences(
                new[] { "Jetting Mycelium I", "Ballistospore Discharge II", "Ballistospore Discharge I" },
                new[]
                {
                    "One-time on draft: colonize up to 3 tiles.",
                    "One-time on draft: toxify up to 17 empty tiles for 18 cycles.",
                    "One-time on draft: toxify up to 12 empty tiles for 18 cycles.",
                },
                Mark);

            Assert.Equal("One-time on draft: colonize up to 3 tiles.", result[0]);
            Assert.Equal("One-time on draft: toxify up to [17] empty tiles for 18 cycles.", result[1]);
            Assert.Equal("One-time on draft: toxify up to [12] empty tiles for 18 cycles.", result[2]);
        }

        [Fact]
        public void Prose_differences_leave_descriptions_untouched()
        {
            var descriptions = new[]
            {
                "Before each Growth Phase, grow up to 2 tiles toward the nearest corner.",
                "Before each Growth Phase, grow up to 3 tiles toward the nearest corner. The last becomes Resistant.",
            };

            var result = MycovariantTierComparison.EmphasizeTierDifferences(
                new[] { "Corner Conduit I", "Corner Conduit II" }, descriptions, Mark);

            Assert.Equal(descriptions, result);
        }

        [Fact]
        public void Untiered_names_are_never_grouped()
        {
            var descriptions = new[] { "Gain 2 points.", "Gain 3 points." };

            var result = MycovariantTierComparison.EmphasizeTierDifferences(
                new[] { "Plasmid Bounty", "Plasmid Bounty" }, descriptions, Mark);

            Assert.Equal(descriptions, result);
        }

        [Fact]
        public void Every_repository_tier_family_offered_together_reads_the_same_or_is_left_alone()
        {
            // Guards the emphasis against content drift: whatever pair of tiers the draft offers,
            // the helper either marks a number that really differs or leaves both cards alone.
            var families = MycovariantRepository.All
                .GroupBy(m => MycovariantTierComparison.GetFamilyName(m.Name))
                .Where(group => group.Count() > 1);

            foreach (var family in families)
            {
                var members = family.ToArray();
                var result = MycovariantTierComparison.EmphasizeTierDifferences(
                    members.Select(m => m.Name).ToArray(),
                    members.Select(m => m.Description).ToArray(),
                    Mark);

                for (int index = 0; index < members.Length; index++)
                {
                    Assert.Equal(members[index].Description, result[index].Replace("[", string.Empty).Replace("]", string.Empty));
                }
            }
        }

        [Fact]
        public void Ballistospore_tiers_are_distinguished_by_their_spore_count()
        {
            var tiers = MycovariantRepository.All
                .Where(m => MycovariantTierComparison.GetFamilyName(m.Name) == "Ballistospore Discharge")
                .ToArray();
            Assert.True(tiers.Length >= 2);

            var result = MycovariantTierComparison.EmphasizeTierDifferences(
                tiers.Select(m => m.Name).ToArray(),
                tiers.Select(m => m.Description).ToArray(),
                Mark);

            Assert.All(result, description => Assert.Contains("[", description));
        }
    }
}
