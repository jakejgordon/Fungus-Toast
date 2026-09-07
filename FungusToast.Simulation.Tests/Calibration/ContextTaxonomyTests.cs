using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class ContextTaxonomyTests
{
    [Theory]
    [InlineData(2, PlayerCountClass.Duel)]
    [InlineData(3, PlayerCountClass.SmallTable)]
    [InlineData(4, PlayerCountClass.SmallTable)]
    [InlineData(5, PlayerCountClass.Crowded)]
    [InlineData(6, PlayerCountClass.Crowded)]
    [InlineData(7, PlayerCountClass.Swarm)]
    [InlineData(8, PlayerCountClass.Swarm)]
    public void PlayerCountsClassifyAsSpecified(int playerCount, PlayerCountClass expected)
        => Assert.Equal(expected, ContextTaxonomy.ClassifyPlayerCount(playerCount));

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void UnsupportedPlayerCounts_Throw(int playerCount)
        => Assert.Throws<ArgumentOutOfRangeException>(() => ContextTaxonomy.ClassifyPlayerCount(playerCount));

    /// <summary>
    /// 80x80 and 160x160 are the anchors the plan names, so the boundaries are pinned to them
    /// exactly rather than to a nearby round number.
    /// </summary>
    [Theory]
    [InlineData(80, 80, BoardScaleClass.Small)]
    [InlineData(81, 80, BoardScaleClass.Medium)]
    [InlineData(160, 160, BoardScaleClass.Medium)]
    [InlineData(161, 160, BoardScaleClass.Large)]
    public void BoardScaleBoundariesSitOnTheAnchors(int width, int height, BoardScaleClass expected)
        => Assert.Equal(expected, ContextTaxonomy.ClassifyBoardScale(width, height));

    [Theory]
    [InlineData(100, 100, AspectClass.Square)]
    [InlineData(110, 100, AspectClass.Square)]
    [InlineData(111, 100, AspectClass.Wide)]
    [InlineData(90, 100, AspectClass.Square)]
    [InlineData(89, 100, AspectClass.Tall)]
    public void AspectBoundariesFollowTheDeclaredRatios(int width, int height, AspectClass expected)
        => Assert.Equal(expected, ContextTaxonomy.ClassifyAspect(width, height));

    [Fact]
    public void ABoardWithBlockedTiles_IsMaskedEvenWhenItCallsItselfRectangular()
    {
        Assert.Equal(GeometryClass.Rectangle, ContextTaxonomy.ClassifyGeometry("rectangle", Array.Empty<int>()));
        Assert.Equal(GeometryClass.Masked, ContextTaxonomy.ClassifyGeometry("rectangle", new[] { 4 }));
        Assert.Equal(GeometryClass.Masked, ContextTaxonomy.ClassifyGeometry("torn-bread", Array.Empty<int>()));
    }

    [Theory]
    [InlineData(0, 0, StartRegimeClass.Generated)]
    [InlineData(2, 0, StartRegimeClass.Exact)]
    [InlineData(0, 2, StartRegimeClass.PreferredPool)]
    public void StartRegimesClassifyFromWhatWasActuallySet(int exact, int pools, StartRegimeClass expected)
        => Assert.Equal(expected, ContextTaxonomy.ClassifyStartRegime(exact, pools));

    [Fact]
    public void ExactStartsAndPreferredPoolsTogether_Throw()
        => Assert.Throws<ArgumentException>(() => ContextTaxonomy.ClassifyStartRegime(2, 2));

    [Fact]
    public void TheRollupKeyNamesEveryClass_SoAnIdCannotMisdescribeItsContext()
    {
        var classes = ContextTaxonomy.Classify(
            playerCount: 4,
            width: 180,
            height: 180,
            geometryId: "rectangle",
            blockedTileIds: Array.Empty<int>(),
            exactStartingPositionCount: 0,
            preferredPositionPoolCount: 0);

        Assert.Equal("smalltable.large.square.rectangle.generated", ContextTaxonomy.BuildRollupKey(classes));
    }

    [Fact]
    public void NonPositiveDimensions_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ContextTaxonomy.ClassifyBoardScale(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ContextTaxonomy.ClassifyAspect(10, 0));
    }
}
