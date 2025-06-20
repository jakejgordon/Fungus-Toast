using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System.Collections.Generic;
using System.Linq;

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
        public SimulationTrackingContext TrackingContext { get; set; }

        // ──────────────
        // PLAYER RESULTS
        // ──────────────
        public List<PlayerResult> PlayerResults { get; set; } = new();

        // ──────────────
        // GLOBAL MUTATION EFFECT COUNTS (AGGREGATES BY PLAYER)
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
            var deathsByPlayerAndReason = tracking.GetAllCellDeathsByPlayerAndReason();

            foreach (var player in players)
            {
                var cells = board.GetAllCellsOwnedBy(player.PlayerId);
                Dictionary<DeathReason, int> playerDeaths = deathsByPlayerAndReason.TryGetValue(player.PlayerId, out var d) ? d : new();

                var pr = new PlayerResult
                {
                    // --- Core identity ---
                    PlayerId = player.PlayerId,
                    StrategyName = player.MutationStrategy?.StrategyName ?? "None",
                    Strategy = player.MutationStrategy!,

                    // --- End-state board stats ---
                    LivingCells = cells.Count(c => c.IsAlive),
                    DeadCells = cells.Count(c => !c.IsAlive),

                    // --- Death reason statistics ---
                    DeadCellDeathReasons = new List<DeathReason>(), // Populated below!
                    DeathsByReason = playerDeaths,
                    DeathsFromRandomness = playerDeaths.TryGetValue(DeathReason.Randomness, out var dfr) ? dfr : 0,
                    DeathsFromAge = playerDeaths.TryGetValue(DeathReason.Age, out var dfa) ? dfa : 0,

                    // --- Mutation tree ---
                    MutationLevels = player.PlayerMutations.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.CurrentLevel),

                    // --- Effective rates (final snapshot) ---
                    EffectiveGrowthChance = player.GetEffectiveGrowthChance(),
                    EffectiveSelfDeathChance = player.GetEffectiveSelfDeathChance(),
                    OffensiveDecayModifier = board.GetAllCells()
                        .Where(c => c.IsAlive && c.OwnerPlayerId != player.PlayerId)
                        .Select(c => player.GetOffensiveDecayModifierAgainst(c, board))
                        .DefaultIfEmpty(0f)
                        .Average(),

                    // --- Per-mutation event counters ---
                    RegenerativeHyphaeReclaims = tracking.GetRegenerativeHyphaeReclaims(player.PlayerId),
                    CreepingMoldMoves = tracking.GetCreepingMoldMoves(player.PlayerId),
                    NecrosporulationSpores = tracking.GetNecrosporeDropCount(player.PlayerId),
                    SporocidalSpores = tracking.GetSporocidalSporeDropCount(player.PlayerId),
                    SporocidalKills = playerDeaths.TryGetValue(DeathReason.SporocidalBloom, out var spKills) ? spKills : 0,
                    NecrophyticSpores = tracking.GetNecrophyticBloomSporeDropCount(player.PlayerId),
                    NecrophyticReclaims = tracking.GetNecrophyticBloomReclaims(player.PlayerId),
                    MycotoxinTracerSpores = tracking.GetMycotoxinSporeDropCount(player.PlayerId),
                    MycotoxinCatabolisms = tracking.GetToxinCatabolismCount(player.PlayerId),
                    CatabolizedMutationPoints = tracking.GetCatabolizedMutationPoints(player.PlayerId),
                    ToxinAuraKills = playerDeaths.TryGetValue(DeathReason.MycotoxinPotentiation, out var taKills) ? taKills : 0,
                    NecrohyphalInfiltrations = tracking.GetNecrohyphalInfiltrationCount(player.PlayerId),
                    NecrohyphalCascades = tracking.GetNecrohyphalCascadeCount(player.PlayerId),
                    PutrefactiveMycotoxinKills = playerDeaths.TryGetValue(DeathReason.PutrefactiveMycotoxin, out var pmKills) ? pmKills : 0,
                    NecrotoxicConversionReclaims = tracking.GetNecrotoxicConversionReclaims(player.PlayerId),
                    HyphalSurgeGrowths = tracking.GetHyphalSurgeGrowthCount(player.PlayerId),
                    HyphalVectoringGrowths = tracking.GetHyphalVectoringGrowthCount(player.PlayerId),

                    // --- Tendril (directional growth) stats ---
                    TendrilNorthwestGrownCells = tracking.GetTendrilNorthwestGrownCells(player.PlayerId),
                    TendrilNortheastGrownCells = tracking.GetTendrilNortheastGrownCells(player.PlayerId),
                    TendrilSoutheastGrownCells = tracking.GetTendrilSoutheastGrownCells(player.PlayerId),
                    TendrilSouthwestGrownCells = tracking.GetTendrilSouthwestGrownCells(player.PlayerId),

                    // --- Mutation point income and spending ---
                    AdaptiveExpressionPointsEarned = tracking.GetAdaptiveExpressionPointsEarned(player.PlayerId),
                    MutatorPhenotypePointsEarned = tracking.GetMutatorPhenotypePointsEarned(player.PlayerId),
                    HyperadaptiveDriftPointsEarned = tracking.GetHyperadaptiveDriftPointsEarned(player.PlayerId),
                    MutationPointIncome = tracking.GetMutationPointIncome(player.PlayerId),
                    MutationPointsSpentByTier = tracking.GetMutationPointsSpentByTier(player.PlayerId),
                    TotalMutationPointsSpent = tracking.GetTotalMutationPointsSpent(player.PlayerId),

                    HyphalSurgeGrowths = tracking.GetHyphalSurgeGrowthCount(player.PlayerId),
                    HyphalVectoringGrowths = tracking.GetHyphalVectoringGrowthCount(player.PlayerId),

                    // Mycovariant summary
                    Mycovariants = BuildMycovariantResults(player, tracking)
                };

                playerResultMap[player.PlayerId] = pr;
            }

            // --- Assign death reasons to the per-player list (for more granular reporting) ---
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
                SporesFromSporocidalBloom = tracking.GetSporocidalSporeDropCounts(),
                SporesFromNecrosporulation = tracking.GetNecrosporulationSporeDropCounts(),
                SporesFromMycotoxinTracer = tracking.GetMycotoxinSporeDropCounts(),
                ToxicTileCount = board.GetAllCells().Count(c => c.IsToxin),
                TrackingContext = tracking
            };

        }


        private static List<MycovariantResult> BuildMycovariantResults(Player player, SimulationTrackingContext tracking)
        {
            var results = new List<MycovariantResult>();

            foreach (var myco in player.Mycovariants)
            {
                string effect = "";

                // Add effect logic per mycovariant
                switch (myco.MycovariantId)
                {
                    case var id when id == MycovariantGameBalance.PlasmidBountyId:
                        // Award is always a constant; show it
                        effect = $"+{MycovariantGameBalance.PlasmidBountyMutationPointAward} MP";
                        break;

                    case var id when id == MycovariantGameBalance.JettingMyceliumNorthId ||
                                     id == MycovariantGameBalance.JettingMyceliumEastId ||
                                     id == MycovariantGameBalance.JettingMyceliumSouthId ||
                                     id == MycovariantGameBalance.JettingMyceliumWestId:
                        {
                            // Get all tracked results for Jetting Mycelium for this player
                            int parasitized = tracking.GetJettingMyceliumParasitized(player.PlayerId);
                            int reclaimed = tracking.GetJettingMyceliumReclaimed(player.PlayerId);
                            int catabolic = tracking.GetJettingMyceliumCatabolicGrowth(player.PlayerId);
                            int alreadyOwned = tracking.GetJettingMyceliumAlreadyOwned(player.PlayerId);

                            // Build effect summary string (only show nonzero effects)
                            var parts = new List<string>();
                            if (parasitized > 0) parts.Add($"{parasitized} parasitized");
                            if (reclaimed > 0) parts.Add($"{reclaimed} reclaimed");
                            if (catabolic > 0) parts.Add($"{catabolic} catabolic");
                            if (alreadyOwned > 0) parts.Add($"{alreadyOwned} owned");
                            effect = string.Join(", ", parts);
                            break;
                        }

                    // Add more cases for other mycovariants as needed...

                    default:
                        effect = "";
                        break;
                }

                results.Add(new MycovariantResult
                {
                    MycovariantId = myco.MycovariantId,
                    MycovariantName = myco.Mycovariant.Name,
                    MycovariantType = myco.Mycovariant.Type.ToString(),
                    Triggered = myco.HasTriggered,
                    EffectSummary = effect
                });
            }
            return results;
        }


    }
}
