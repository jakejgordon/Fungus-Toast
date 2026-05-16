using FungusToast.Core.Board;

namespace FungusToast.Core.Tests.Board;

public class BoardMaskClipSamplingTests
{
    [Fact]
    public void BuildClipBudgetSampleOffsets_includes_the_outer_allowed_boundary_for_three_point_sampling()
    {
        float[] offsets = BoardMaskClipSampling.BuildClipBudgetSampleOffsets(
            playableSurfaceTileScale: 1.01f,
            maximumTileClipFraction: 0.1f,
            sampleResolution: 3);

        Assert.Equal(3, offsets.Length);
        Assert.InRange(offsets[0], -0.4051f, -0.4049f);
        Assert.InRange(offsets[1], -0.0001f, 0.0001f);
        Assert.InRange(offsets[2], 0.4049f, 0.4051f);
    }

    [Fact]
    public void BuildClipBudgetSampleOffsets_clamps_low_sample_resolution_to_the_two_boundary_endpoints()
    {
        float[] offsets = BoardMaskClipSampling.BuildClipBudgetSampleOffsets(
            playableSurfaceTileScale: 1.01f,
            maximumTileClipFraction: 0.1f,
            sampleResolution: 1);

        Assert.Equal(2, offsets.Length);
        Assert.InRange(offsets[0], -0.4051f, -0.4049f);
        Assert.InRange(offsets[1], 0.4049f, 0.4051f);
    }

    [Fact]
    public void BuildClipBudgetSampleOffsets_returns_evenly_spaced_symmetric_offsets()
    {
        float[] offsets = BoardMaskClipSampling.BuildClipBudgetSampleOffsets(
            playableSurfaceTileScale: 1f,
            maximumTileClipFraction: 0.25f,
            sampleResolution: 5);

        Assert.Equal(5, offsets.Length);
        Assert.InRange(offsets[0], -0.2501f, -0.2499f);
        Assert.InRange(offsets[1], -0.1251f, -0.1249f);
        Assert.InRange(offsets[2], -0.0001f, 0.0001f);
        Assert.InRange(offsets[3], 0.1249f, 0.1251f);
        Assert.InRange(offsets[4], 0.2499f, 0.2501f);
    }
}
