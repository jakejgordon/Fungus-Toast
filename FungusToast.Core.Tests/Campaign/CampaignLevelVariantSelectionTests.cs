using FungusToast.Core.Campaign;

namespace FungusToast.Core.Tests.Campaign;

public class CampaignLevelVariantSelectionTests
{
    private static readonly string[] Ids = { "flatbread-original", "rot-quarantine" };
    private static readonly string[] Presets = { "Campaign11", "Campaign11_QuarantineCorridor" };

    [Fact]
    public void Fresh_choice_is_deterministic_and_does_not_depend_on_gameplay_seed()
    {
        foreach (int seed in new[] { int.MinValue, -1, 0, 1, 4567, int.MaxValue })
            Assert.Equal(CampaignLevelVariantSelection.PickVariantIndex(seed, 11, 2),
                CampaignLevelVariantSelection.ResolveVariantIndex(seed, 11, Ids, Presets));
    }

    [Fact]
    public void Both_variants_are_selected_with_approximately_equal_frequency()
    {
        int quarantine = Enumerable.Range(0, 10000).Count(seed =>
            CampaignLevelVariantSelection.PickVariantIndex(seed, 11, 2) == 1);
        Assert.InRange(quarantine, 4700, 5300);
    }

    [Fact]
    public void Saved_variant_survives_seed_changes_and_reordered_pool()
    {
        Assert.Equal(1, CampaignLevelVariantSelection.ResolveVariantIndex(5, 11, Ids, Presets, Ids[1], Presets[1]));
        Assert.Equal(0, CampaignLevelVariantSelection.ResolveVariantIndex(900, 11,
            Ids.Reverse().ToArray(), Presets.Reverse().ToArray(), Ids[1], Presets[1]));
    }

    [Fact]
    public void Legacy_stage12_save_stays_on_original_board_instead_of_rerolling()
    {
        foreach (int seed in Enumerable.Range(0, 100))
            Assert.Equal(0, CampaignLevelVariantSelection.ResolveVariantIndex(seed, 11, Ids, Presets, savedPresetId: Presets[0]));
    }

    [Fact]
    public void Missing_or_conflicting_saved_identity_is_rejected_without_reroll()
    {
        Assert.Throws<InvalidOperationException>(() => CampaignLevelVariantSelection.ResolveVariantIndex(1, 11, Ids, Presets, "removed"));
        Assert.Throws<InvalidOperationException>(() => CampaignLevelVariantSelection.ResolveVariantIndex(1, 11, Ids, Presets, Ids[1], Presets[0]));
        Assert.Throws<InvalidOperationException>(() => CampaignLevelVariantSelection.ResolveVariantIndex(1, 11, Ids, Presets, savedPresetId: "removed"));
    }

    [Fact]
    public void Invalid_authored_pool_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => CampaignLevelVariantSelection.ResolveVariantIndex(1, 11, new[] { "same", "same" }, Presets));
        Assert.Throws<ArgumentException>(() => CampaignLevelVariantSelection.ResolveVariantIndex(1, 11, Ids, new[] { "same", "same" }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignLevelVariantSelection.PickVariantIndex(1, 11, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignLevelVariantSelection.PickVariantIndex(1, -1, 2));
    }
}
