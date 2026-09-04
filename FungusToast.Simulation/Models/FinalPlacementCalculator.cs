namespace FungusToast.Simulation.Models;

/// <summary>
/// Calculates final placement from the simulation's primary outcome measure:
/// living-cell count. Tied players receive the same competition rank, so a
/// 1st-place tie is represented as ranks 1, 1, 3 rather than 1, 1, 2.
/// </summary>
public static class FinalPlacementCalculator
{
    public static IReadOnlyList<int> GetWinnerIds(IReadOnlyCollection<PlayerResult> players)
    {
        if (players.Count == 0) return Array.Empty<int>();
        var maximumLivingCells = players.Max(result => result.LivingCells);
        return players
            .Where(result => result.LivingCells == maximumLivingCells)
            .Select(result => result.PlayerId)
            .OrderBy(playerId => playerId)
            .ToList();
    }

    public static int GetCompetitionRank(IReadOnlyCollection<PlayerResult> players, int playerId)
    {
        var player = GetPlayer(players, playerId);
        return 1 + players.Count(candidate => candidate.LivingCells > player.LivingCells);
    }

    public static int GetTieCount(IReadOnlyCollection<PlayerResult> players, int playerId)
    {
        var player = GetPlayer(players, playerId);
        return players.Count(candidate => candidate.LivingCells == player.LivingCells);
    }

    private static PlayerResult GetPlayer(IReadOnlyCollection<PlayerResult> players, int playerId) =>
        players.FirstOrDefault(candidate => candidate.PlayerId == playerId)
        ?? throw new ArgumentException($"No player result exists for player {playerId}.", nameof(playerId));
}
