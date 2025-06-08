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
        // ──────────────
        // GAME SUMMARY
        // ──────────────
        public int WinnerId { get; set; }
        public int TurnsPlayed { get; set; }
        public int ToxicTileCount { get; set; }

        // ──────────────
        // PLAYER RESULTS
        // ──────────────
        public List<PlayerResult> PlayerResults { get; set; } = new();

        // ──────────────
        // MUTATION EFFECT COUNTS (by playerId)
        // ──────────────
        public Dictionary<int, int> SporesFromSporocidalBloom { get; set; } = new();
        public Dictionary<int, int> SporesFromNecrosporulation { get; set; } = new();
        public Dictionary<int, int> SporesFromMycotoxinTracer { get; set; } = new();

        // ──────────────
        // FACTORY METHOD
        // ──────────────
        public static GameResult From(GameBoard board, List<Player> players, int turns, SimulationTrackingContext tracking)
        {
            var playerResultMap = new Dictionary<int, PlayerResult>();

            foreach (var player in players)
            {
                var cells = board.GetAllCellsOwnedBy(player.PlayerId);

                var pr = new PlayerResult
                {
                    // Identity and strategy
                    PlayerId = player.PlayerId,
                    StrategyName = player.MutationStrategy?.StrategyName ?? "None",
                    Strategy = player.MutationStrategy!,

                    // Cell stats
                    LivingCells = cells.Count(c => c.IsAlive),
                    DeadCells = cells.Count(c => !c.IsAlive),
                    DeadCellDeathReasons = new List<DeathReason>(),
                    ReclaimedCells = tracking.GetReclaimedCells(player.PlayerId),

                    // Mutations
                    MutationLevels = player.PlayerMutations.ToDictionary(kv => kv.Key, kv => kv.Value.CurrentLevel),

                    // Effective stats
                    EffectiveGrowthChance = player.GetEffectiveGrowthChance(),
                    EffectiveSelfDeathChance = player.GetEffectiveSelfDeathChance(),
                    OffensiveDecayModifier = board.GetAllCells()
                        .Where(c => c.IsAlive && c.OwnerPlayerId != player.PlayerId)
                        .Select(c => player.GetOffensiveDecayModifierAgainst(c, board))
                        .DefaultIfEmpty(0f)
                        .Average(),

                    // Mutation effect counters
                    CreepingMoldMoves = tracking.GetCreepingMoldMoves(player.PlayerId),
                    NecrosporulationSpores = tracking.GetNecrosporeDropCount(player.PlayerId),
                    SporocidalSpores = tracking.GetSporocidalSporeDropCount(player.PlayerId),
                    SporocidalKills = tracking.GetSporocidalKillCount(player.PlayerId),
                    NecrophyticSpores = tracking.GetNecrophyticBloomSporeDropCount(player.PlayerId),
                    NecrophyticReclaims = tracking.GetNecrophyticBloomReclaimCount(player.PlayerId),
                    MycotoxinTracerSpores = tracking.GetMycotoxinSporeDropCount(player.PlayerId),
                    MycotoxinCatabolisms = tracking.GetToxinCatabolismCount(player.PlayerId),
                    ToxinAuraKills = tracking.GetToxinAuraKillCount(player.PlayerId),
                    CatabolizedMutationPoints = tracking.GetCatabolizedMutationPoints(player.PlayerId),
                    NecrohyphalInfiltrations = tracking.GetNecrohyphalInfiltrationCount(player.PlayerId),
                    NecrohyphalCascades = tracking.GetNecrohyphalCascadeCount(player.PlayerId),

                    // ──────────────
                    // Free Mutation Points (SPLIT BY SOURCE)
                    // ──────────────
                    MutatorPhenotypePointsEarned = tracking.GetMutatorPhenotypePointsEarned(player.PlayerId),
                    HyperadaptiveDriftPointsEarned = tracking.GetHyperadaptiveDriftPointsEarned(player.PlayerId)
                };

                playerResultMap[player.PlayerId] = pr;
            }

            // Attach death reasons to appropriate player result
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
