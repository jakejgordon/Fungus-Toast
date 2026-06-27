using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;

namespace FungusToast.Core.Tests.Campaign;

public class CampaignStartingPositionDifficultyResolverTests
{
    [Fact]
    public void GetAllowedStartingPositions_for_training_returns_best_half_of_slots()
    {
        var metadata = CreateMetadata();

        var allowed = CampaignStartingPositionDifficultyResolver.GetAllowedStartingPositions(
            metadata,
            CampaignDifficulty.Training);

        Assert.Equal(new[] { (10, 10), (20, 10), (30, 10) }, allowed);
    }

    [Fact]
    public void GetAllowedStartingPositions_for_boss_returns_worst_half_of_slots()
    {
        var metadata = CreateMetadata();

        var allowed = CampaignStartingPositionDifficultyResolver.GetAllowedStartingPositions(
            metadata,
            CampaignDifficulty.Boss);

        Assert.Equal(new[] { (40, 10), (50, 10), (60, 10) }, allowed);
    }

    [Fact]
    public void GetAllowedStartingPositions_shifts_window_toward_worse_slots_as_difficulty_increases()
    {
        var metadata = CreateMetadata();

        var training = CampaignStartingPositionDifficultyResolver.GetAllowedStartingPositions(metadata, CampaignDifficulty.Training);
        var medium = CampaignStartingPositionDifficultyResolver.GetAllowedStartingPositions(metadata, CampaignDifficulty.Medium);
        var hard = CampaignStartingPositionDifficultyResolver.GetAllowedStartingPositions(metadata, CampaignDifficulty.Hard);
        var boss = CampaignStartingPositionDifficultyResolver.GetAllowedStartingPositions(metadata, CampaignDifficulty.Boss);

        Assert.Contains((10, 10), training);
        Assert.DoesNotContain((10, 10), medium);
        Assert.DoesNotContain((10, 10), boss);

        Assert.Contains((50, 10), hard);
        Assert.Contains((60, 10), boss);
        Assert.DoesNotContain((60, 10), training);
    }

    private static CampaignBoardStartingPositionMetadata CreateMetadata()
    {
        return new CampaignBoardStartingPositionMetadata(
            presetId: "TestPreset",
            shapeKey: "shape",
            boardWidth: 60,
            boardHeight: 60,
            playerCount: 6,
            spriteName: "test.png",
            shapeSource: "baked-mask",
            entries: new[]
            {
                new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 10, y: 10, favorRank: 1, winPercentage: 20.0),
                new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 20, y: 10, favorRank: 2, winPercentage: 18.0),
                new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 30, y: 10, favorRank: 3, winPercentage: 15.0),
                new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 40, y: 10, favorRank: 4, winPercentage: 12.0),
                new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 50, y: 10, favorRank: 5, winPercentage: 9.0),
                new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 60, y: 10, favorRank: 6, winPercentage: 6.0),
            });
    }
}
