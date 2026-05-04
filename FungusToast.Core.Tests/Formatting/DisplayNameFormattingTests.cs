using FungusToast.Core.Death;
using FungusToast.Core.Formatting;
using FungusToast.Core.Growth;
using Xunit;

namespace FungusToast.Core.Tests.Formatting;

public class DisplayNameFormattingTests
{
    [Theory]
    [InlineData("AscusBait", "Ascus Bait")]
    [InlineData("XMLParser", "XML Parser")]
    [InlineData("PutrefactiveCascadePoison", "Putrefactive Cascade Poison")]
    public void HumanizeIdentifier_splits_pascal_case_boundaries(string input, string expected)
    {
        Assert.Equal(expected, DisplayNameHumanizer.HumanizeIdentifier(input));
    }

    [Fact]
    public void DeathReasonDisplayNames_humanizes_unmapped_values()
    {
        Assert.Equal("Ascus Bait", DeathReasonDisplayNames.GetDisplayName(DeathReason.AscusBait));
    }

    [Fact]
    public void GrowthSourceDisplayNames_keeps_existing_named_overrides()
    {
        Assert.Equal("Tendril Outgrowth", GrowthSourceDisplayNames.GetDisplayName(GrowthSource.TendrilOutgrowth));
    }
}
