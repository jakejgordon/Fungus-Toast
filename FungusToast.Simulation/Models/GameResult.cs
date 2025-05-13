using FungusToast.Core.Players;
using FungusToast.Core;
using FungusToast.Simulation.GameSimulation.Models;

public class GameResult
{
    public int WinnerId { get; set; }
    public int TurnsPlayed { get; set; }
    public List<PlayerResult> PlayerResults { get; set; } = new();

    public static GameResult From(GameBoard board, List<Player> players, int turns)
    {
        var results = players.Select(p =>
        {
            var cells = board.GetAllCellsOwnedBy(p.PlayerId);
            return new PlayerResult
            {
                PlayerId = p.PlayerId,
                StrategyName = p.MutationStrategy?.GetType().Name ?? "None",
                LivingCells = cells.Count(c => c.IsAlive),
                DeadCells = cells.Count(c => !c.IsAlive),
                MutationLevels = p.PlayerMutations.ToDictionary(kv => kv.Key, kv => kv.Value.CurrentLevel)
            };
        }).ToList();

        return new GameResult
        {
            WinnerId = results.OrderByDescending(r => r.LivingCells).First().PlayerId,
            TurnsPlayed = turns,
            PlayerResults = results
        };
    }
}
