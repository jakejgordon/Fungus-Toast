using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Players;

namespace FungusToast.Simulation.GameSimulation.Models
{
    public class GameResult
    {
        public int WinnerId { get; set; }
        public int TurnsPlayed { get; set; }

        public static GameResult From(GameBoard board, List<Player> players, int turns)
        {
            var ranked = players
                .OrderByDescending(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .ThenByDescending(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => !c.IsAlive))
                .ToList();

            return new GameResult
            {
                WinnerId = ranked.First().PlayerId,
                TurnsPlayed = turns
            };
        }
    }
}
