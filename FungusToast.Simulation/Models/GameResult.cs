using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
using FungusToast.Core.Board;
using FungusToast.Core.Mutations;

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

            // Get the complete death tracking dictionary from the context
            var deathsByPlayerAndReason = tracking.GetAllCellDeathsByPlayerAndReason();

            foreach (var player in players)
            {
                var cells = board.GetAllCellsOwnedBy(player.PlayerId);

                // Get this player's deaths by reason (may be missing if no deaths)
                Dictionary<DeathReason, int> playerDeaths = deathsByPlayerAndReason.TryGetValue(player.PlayerId, out var d) ? d : new();

                var pr = new PlayerResult
                {
                    // Identity and strategy
                    PlayerId = player.PlayerId,
                    StrategyName = player.MutationStrategy?.StrategyName ?? "None",
                    Strategy = player.MutationStrategy!,

                    // Cell stats
                    LivingCells = cells.Count(c => c.IsAlive),
                    DeadCells = cells.Count(c => !c.IsAlive),
                    DeadCellDeathReasons = new List<DeathReason>(), // (optional legacy)
                    ReclaimedCells = tracking.GetReclaimedCells(player.PlayerId),

                    // Mutations
                    MutationLevels = player.PlayerMutations.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.CurrentLevel),

                    // Effective stats
                    EffectiveGrowthChance = player.GetEffectiveGrowthChance(),
                    EffectiveSelfDeathChance = player.GetEffectiveSelfDeathChance(),
                    OffensiveDecayModifier = board.GetAllCells()
                        .Where(c => c.IsAlive && c.OwnerPlayerId != player.PlayerId)
                        .Select(c => player.GetOffensiveDecayModifierAgainst(c, board))
                        .DefaultIfEmpty(0f)
                        .Average(),

                    // Mutation effect counters (from death reasons)
                    PutrefactiveMycotoxinKills = playerDeaths.TryGetValue(DeathReason.PutrefactiveMycotoxin, out var pmKills) ? pmKills : 0,
                    SporocidalKills = playerDeaths.TryGetValue(DeathReason.SporocidalBloom, out var spKills) ? spKills : 0,
                    ToxinAuraKills = playerDeaths.TryGetValue(DeathReason.MycotoxinPotentiation, out var taKills) ? taKills : 0,

                    // Everything else remains unchanged...
                    CreepingMoldMoves = tracking.GetCreepingMoldMoves(player.PlayerId),
                    NecrosporulationSpores = tracking.GetNecrosporeDropCount(player.PlayerId),
                    SporocidalSpores = tracking.GetSporocidalSporeDropCount(player.PlayerId),
                    NecrophyticSpores = tracking.GetNecrophyticBloomSporeDropCount(player.PlayerId),
                    NecrophyticReclaims = tracking.GetNecrophyticBloomReclaimCount(player.PlayerId),
                    MycotoxinTracerSpores = tracking.GetMycotoxinSporeDropCount(player.PlayerId),
                    MycotoxinCatabolisms = tracking.GetToxinCatabolismCount(player.PlayerId),
                    CatabolizedMutationPoints = tracking.GetCatabolizedMutationPoints(player.PlayerId),
                    NecrohyphalInfiltrations = tracking.GetNecrohyphalInfiltrationCount(player.PlayerId),
                    NecrohyphalCascades = tracking.GetNecrohyphalCascadeCount(player.PlayerId),

                    // Necrotoxic Conversion effect
                    NecrotoxicConversionReclaims = tracking.GetNecrotoxicConversionReclaims(player.PlayerId),

                    // Tendril mutation effect counters
                    TendrilNorthwestGrownCells = tracking.GetTendrilNorthwestGrownCells(player.PlayerId),
                    TendrilNortheastGrownCells = tracking.GetTendrilNortheastGrownCells(player.PlayerId),
                    TendrilSoutheastGrownCells = tracking.GetTendrilSoutheastGrownCells(player.PlayerId),
                    TendrilSouthwestGrownCells = tracking.GetTendrilSouthwestGrownCells(player.PlayerId),

                    // Free Mutation Points (SPLIT BY SOURCE)
                    AdaptiveExpressionPointsEarned = tracking.GetAdaptiveExpressionPointsEarned(player.PlayerId),
                    MutatorPhenotypePointsEarned = tracking.GetMutatorPhenotypePointsEarned(player.PlayerId),
                    HyperadaptiveDriftPointsEarned = tracking.GetHyperadaptiveDriftPointsEarned(player.PlayerId),

                    // Death summary by reason
                    DeathsByReason = playerDeaths,

                    // New: Mutation point income and spending by tier
                    MutationPointIncome = tracking.GetMutationPointIncome(player.PlayerId),
                    MutationPointsSpentByTier = tracking.GetMutationPointsSpentByTier(player.PlayerId),
                    TotalMutationPointsSpent = tracking.GetTotalMutationPointsSpent(player.PlayerId),

                    // ──────── New: Hyphal Surge Growths ────────
                    HyphalSurgeGrowths = tracking.GetHyphalSurgeGrowthCount(player.PlayerId)
                };

                playerResultMap[player.PlayerId] = pr;
            }

            // (OPTIONAL) Attach cell death reasons to legacy DeadCellDeathReasons, if you still need per-cell data
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
