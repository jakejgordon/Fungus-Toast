using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
using FungusToast.Simulation.GameSimulation.Models;

namespace FungusToast.Simulation.GameSimulation.Models
{
    public class GameResult
    {
        public int WinnerId { get; set; }
        public int TurnsPlayed { get; set; }
        public List<PlayerResult> PlayerResults { get; set; }

        public static GameResult From(GameBoard board, List<Player> players, int turns)
        {
            var playerResultMap = new Dictionary<int, PlayerResult>();

            foreach (var player in players)
            {
                var cells = board.GetAllCellsOwnedBy(player.PlayerId);

                var pr = new PlayerResult
                {
                    PlayerId = player.PlayerId,
                    StrategyName = player.MutationStrategy?.GetType().Name ?? "None",
                    LivingCells = cells.Count(c => c.IsAlive),
                    DeadCells = cells.Count(c => !c.IsAlive),
                    MutationLevels = player.PlayerMutations.ToDictionary(kv => kv.Key, kv => kv.Value.CurrentLevel),

                    EffectiveGrowthChance = player.GetEffectiveGrowthChance(),
                    EffectiveSelfDeathChance = player.GetEffectiveSelfDeathChance(),
                    OffensiveDecayModifier = board.GetAllCells()
                        .Where(c => c.IsAlive && c.OwnerPlayerId != player.PlayerId)
                        .Select(c => player.GetOffensiveDecayModifierAgainst(c, board))
                        .DefaultIfEmpty(0f)
                        .Average(),

                    DeadCellDeathReasons = new List<DeathReason>()
                };

                playerResultMap[player.PlayerId] = pr;
            }

            // Assign death reasons to the appropriate PlayerResult
            foreach (var cell in board.GetAllCells())
            {
                if (!cell.IsAlive && cell.CauseOfDeath.HasValue &&
                    playerResultMap.TryGetValue(cell.OwnerPlayerId, out var pr))
                {
                    pr.DeadCellDeathReasons.Add(cell.CauseOfDeath.Value);
                }
            }

            var results = playerResultMap.Values.ToList();

            return new GameResult
            {
                WinnerId = results.OrderByDescending(r => r.LivingCells).First().PlayerId,
                TurnsPlayed = turns,
                PlayerResults = results
            };
        }
    }
}
