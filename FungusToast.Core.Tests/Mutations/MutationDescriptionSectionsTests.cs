using FungusToast.Core.Mutations;

namespace FungusToast.Core.Tests.Mutations;

public class MutationDescriptionSectionsTests
{
    [Fact]
    public void Every_registered_mutation_has_simple_and_technical_content()
    {
        foreach (var mutation in MutationRegistry.GetAll())
        {
            var sections = mutation.DescriptionSections;

            Assert.False(string.IsNullOrWhiteSpace(sections.Summary), $"{mutation.Name} has no simple summary.");
            Assert.False(string.IsNullOrWhiteSpace(sections.TechnicalDetails), $"{mutation.Name} has no technical details.");
            Assert.DoesNotContain("<b>Technical:</b>", sections.Summary);
            Assert.DoesNotContain("<b>Max Level Bonus:</b>", sections.TechnicalDetails);
            Assert.DoesNotContain("Buffed by:", sections.TechnicalDetails);
        }
    }

    [Fact]
    public void Parser_separates_optional_max_level_and_synergy_sections()
    {
        const string description =
            "Simple benefit.\n\n" +
            "<b>Technical:</b> Exact trigger and scaling.\n" +
            "<b>Max Level Bonus:</b> A distinct capstone.\n" +
            "Buffed by: Helpful Mutation.";

        var sections = MutationDescriptionSections.Parse(description);

        Assert.Equal("Simple benefit.", sections.Summary);
        Assert.Equal("Exact trigger and scaling.", sections.TechnicalDetails);
        Assert.Equal("A distinct capstone.", sections.MaxLevelBonus);
        Assert.Equal(new[] { "Helpful Mutation." }, sections.BuffingMutations);
    }

    [Fact]
    public void Parser_deduplicates_repeated_synergy_lines()
    {
        const string description =
            "Simple benefit.\n\n" +
            "<b>Technical:</b> Exact trigger.\n" +
            "Buffed by: Helpful Mutation.\n" +
            "Buffed by: Helpful Mutation.";

        var sections = MutationDescriptionSections.Parse(description);

        Assert.Equal(new[] { "Helpful Mutation." }, sections.BuffingMutations);
    }

    [Fact]
    public void ChemotacticBeacon_exposes_its_authored_and_catalog_synergy_once()
    {
        var mutation = MutationRegistry.GetById(MutationIds.ChemotacticBeacon)!;

        Assert.Equal(new[] { "Putrefactive Mycotoxin." }, mutation.DescriptionSections.BuffingMutations);
    }

    [Fact]
    public void Empty_or_legacy_copy_degrades_without_throwing()
    {
        var empty = MutationDescriptionSections.Parse(null);
        var legacy = MutationDescriptionSections.Parse("Legacy summary only.");

        Assert.Equal(string.Empty, empty.Summary);
        Assert.False(empty.HasTechnicalDetails);
        Assert.Equal("Legacy summary only.", legacy.Summary);
        Assert.False(legacy.HasTechnicalDetails);
    }

    [Fact]
    public void Mutation_invalidates_cached_sections_when_description_is_enriched()
    {
        var mutation = MutationRegistry.GetById(MutationIds.MycelialBloom)!;
        var standalone = new Mutation(
            id: 999,
            name: "Test Mutation",
            description: mutation.Description,
            flavorText: string.Empty,
            type: mutation.Type,
            effectPerLevel: mutation.EffectPerLevel);
        var initialSections = standalone.DescriptionSections;
        standalone.AppendDescription("Buffed by: Test Synergy.");

        Assert.NotSame(initialSections, standalone.DescriptionSections);
        Assert.Equal(new[] { "Test Synergy." }, standalone.DescriptionSections.BuffingMutations);
    }
}
