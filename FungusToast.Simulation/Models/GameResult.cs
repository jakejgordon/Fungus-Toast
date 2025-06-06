using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
using FungusToast.Core.Board;

namespace FungusToast.Simulation.Models
{
    public class GameResult
    {
        public int WinnerId { get; set; }
        public int TurnsPlayed { get; set; }
        public List<PlayerResult> PlayerResults { get; set; } = new();

        public Dictionary<int, int> SporesFromSporocidalBloom { get; set; } = new();
        public Dictionary<int, int> SporesFromNecrosporulation { get; set; } = new();
        public Dictionary<int, int> SporesFromMycotoxinTracer { get; set; } = new();

        public int ToxicTileCount { get; set; }

        public static GameResult From(GameBoard board, List<Player> players, int turns, SimulationTrackingContext tracking)
        {
            var playerResultMap = new Dictionary<int, PlayerResult>();

            foreach (var player in players)
            {
                var cells = board.GetAllCellsOwnedBy(player.PlayerId);

                var pr = new PlayerResult
                {
                    PlayerId = player.PlayerId,
                    StrategyName = player.MutationStrategy?.StrategyName ?? "None",
                    Strategy = player.MutationStrategy!,
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

                    DeadCellDeathReasons = new List<DeathReason>(),
                    NecrophyticReclaims = tracking.GetNecrophyticBloomReclaimCount(player.PlayerId),
                    NecrophyticSpores = tracking.GetNecrophyticBloomSporeDropCount(player.PlayerId),
                    ReclaimedCells = tracking.GetReclaimedCells(player.PlayerId),
                    CreepingMoldMoves = tracking.GetCreepingMoldMoves(player.PlayerId),
                    MycotoxinTracerSpores = tracking.GetMycotoxinSporeDropCount(player.PlayerId),
                    ToxinAuraKills = tracking.GetToxinAuraKillCount(player.PlayerId),
                    SporocidalSpores = tracking.GetSporocidalSporeDropCount(player.PlayerId)
                };

                playerResultMap[player.PlayerId] = pr;
            }

            foreach (var cell in board.GetAllCells())
            {
                if (!cell.IsAlive && cell.CauseOfDeath.HasValue &&
                    cell.LastOwnerPlayerId.HasValue &&
                    playerResultMap.TryGetValue(cell.LastOwnerPlayerId.Value, out var pr))
                {
                    pr.DeadCellDeathReasons.Add(cell.CauseOfDeath.Value);
                }
            }

            return new GameResult
            {
                WinnerId = playerResultMap.Values.OrderByDescending(r => r.LivingCells).First().PlayerId,
                TurnsPlayed = turns,
                PlayerResults = playerResultMap.Values.ToList(),
                SporesFromSporocidalBloom = tracking.GetSporocidalSpores(),
                SporesFromNecrosporulation = tracking.GetNecroSpores(),
                SporesFromMycotoxinTracer = tracking.GetMycotoxinTracerSporeDrops(),
                ToxicTileCount = board.GetAllCells().Count(c => c.IsToxin)
            };

        }

    }
}
