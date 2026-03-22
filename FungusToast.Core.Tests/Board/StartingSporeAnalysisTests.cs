using FungusToast.Core.Board;
using System.Linq;

namespace FungusToast.Core.Tests.Board;

public class StartingSporeAnalysisTests
{
    [Fact]
    public void GetStartingPositionAnalysis_for_one_player_reports_full_board_control_and_rank_one()
    {
        var analysis = StartingSporeUtility.GetStartingPositionAnalysis(boardWidth: 7, boardHeight: 5, playerCount: 1);

        var entry = Assert.Single(analysis.Entries);
        Assert.Equal(0, entry.SlotIndex);
        Assert.Equal((3, 2), (entry.X, entry.Y));
        Assert.Equal(35, entry.UncontestedTileCount);
        Assert.Equal(35, entry.EarlyUncontestedTileCount);
        Assert.Equal(0, entry.TieTileCount);
        Assert.Equal(1, entry.FavorRank);
    }

    [Fact]
    public void GetStartingPositionAnalysis_with_symmetric_two_player_override_gives_equal_metrics_to_both_slots()
    {
        var analysis = StartingSporeUtility.GetStartingPositionAnalysis(
            boardWidth: 9,
            boardHeight: 5,
            playerCount: 2,
            overridePositions: new[] { (2, 2), (6, 2) });

        Assert.Equal(2, analysis.Entries.Count);

        var first = analysis.Entries[0];
        var second = analysis.Entries[1];

        Assert.Equal(first.UncontestedTileCount, second.UncontestedTileCount);
        Assert.Equal(first.EarlyUncontestedTileCount, second.EarlyUncontestedTileCount);
        Assert.Equal(first.TieTileCount, second.TieTileCount);
        Assert.Equal(1, first.FavorRank);
        Assert.Equal(2, second.FavorRank);
    }

    [Fact]
    public void GetStartingPositionAnalysis_with_equidistant_tiles_records_tie_counts_for_both_slots()
    {
        var analysis = StartingSporeUtility.GetStartingPositionAnalysis(
            boardWidth: 3,
            boardHeight: 1,
            playerCount: 2,
            overridePositions: new[] { (0, 0), (2, 0) });

        Assert.Equal(2, analysis.Entries.Count);
        Assert.All(analysis.Entries, entry => Assert.Equal(1, entry.TieTileCount));
        Assert.All(analysis.Entries, entry => Assert.Equal(1, entry.UncontestedTileCount));
        Assert.All(analysis.Entries, entry => Assert.Equal(1, entry.EarlyUncontestedTileCount));
    }

    [Fact]
    public void GetStartingPositionAnalysis_with_asymmetric_override_positions_ranks_more_favored_slot_first()
    {
        var analysis = StartingSporeUtility.GetStartingPositionAnalysis(
            boardWidth: 9,
            boardHeight: 3,
            playerCount: 2,
            overridePositions: new[] { (1, 1), (7, 1) });

        var favoredEntry = analysis.Entries.Single(entry => entry.FavorRank == 1);
        var lessFavoredEntry = analysis.Entries.Single(entry => entry.FavorRank == 2);

        Assert.InRange(
            favoredEntry.EarlyUncontestedTileCount,
            lessFavoredEntry.EarlyUncontestedTileCount,
            int.MaxValue);
        Assert.InRange(
            favoredEntry.UncontestedTileCount,
            lessFavoredEntry.UncontestedTileCount,
            int.MaxValue);
    }
}
