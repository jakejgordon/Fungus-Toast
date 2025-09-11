using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
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
        public SimulationTrackingContext TrackingContext { get; set; } = null!;

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
        public Dictionary<int, int> PerimeterProliferatorGrowthsByPlayer { get; set; } = new();

        // ──────────────
        // FACTORY METHOD
        // ──────────────
        public static GameResult From(GameBoard board, List<Player> players, int turns, SimulationTrackingContext tracking)
        {
            var playerResultMap = new Dictionary<int, PlayerResult>();
            var deathsByPlayerAndReason = tracking.GetAllCellDeathsByPlayerAndReason();

            foreach (var player in players)
            {
                var aiScores = player.PlayerMycovariants
                    .Select(pm => pm.AIScoreAtDraft)
                    .OfType<float>()
                    .ToList();

                var pr = new PlayerResult
                {
                    // --- Core identity ---
                    PlayerId = player.PlayerId,
                    StrategyName = player.MutationStrategy?.StrategyName ?? "None",
                    Strategy = player.MutationStrategy!,

                    // --- End-state board stats ---
                    LivingCells = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsAlive),
                    DeadCells = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => !c.IsAlive),

                    // --- Death reason statistics ---
                    DeadCellDeathReasons = new List<DeathReason>(), // Populated below!
                    DeathsByReason = tracking.GetAllCellDeathsByPlayerAndReason().ContainsKey(player.PlayerId)
                        ? tracking.GetAllCellDeathsByPlayerAndReason()[player.PlayerId]
                        : new Dictionary<DeathReason, int>(),
                    DeathsFromRandomness = tracking.GetCellDeathCount(player.PlayerId, DeathReason.Randomness),
                    DeathsFromAge = tracking.GetCellDeathCount(player.PlayerId, DeathReason.Age),

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
                    CreepingMoldToxinJumps = tracking.GetCreepingMoldToxinJumps(player.PlayerId),
                    NecrosporulationSpores = tracking.GetNecrosporeDropCount(player.PlayerId),
                    SporocidalSpores = tracking.GetSporocidalSporeDropCount(player.PlayerId),
                    SporocidalKills = tracking.GetCellDeathCount(player.PlayerId, DeathReason.SporicidalBloom),
                    NecrophyticSpores = tracking.GetNecrophyticBloomSporeDropCount(player.PlayerId),
                    NecrophyticReclaims = tracking.GetNecrophyticBloomReclaims(player.PlayerId),
                    MycotoxinTracerSpores = tracking.GetMycotoxinSporeDropCount(player.PlayerId),
                    MycotoxinCatabolisms = tracking.GetToxinCatabolismCount(player.PlayerId),
                    CatabolizedMutationPoints = tracking.GetCatabolizedMutationPoints(player.PlayerId),
                    ToxinAuraKills = tracking.GetCellDeathCount(player.PlayerId, DeathReason.MycotoxinPotentiation),
                    NecrohyphalInfiltrations = tracking.GetNecrohyphalInfiltrationCount(player.PlayerId),
                    NecrohyphalCascades = tracking.GetNecrohyphalCascadeCount(player.PlayerId),
                    PutrefactiveMycotoxinKills = tracking.GetCellDeathCount(player.PlayerId, DeathReason.PutrefactiveMycotoxin),
                    NecrotoxicConversionReclaims = tracking.GetNecrotoxicConversionReclaims(player.PlayerId),
                    CatabolicRebirthResurrections = tracking.GetCatabolicRebirthResurrections(player.PlayerId),
                    CatabolicRebirthAgedToxins = tracking.GetCatabolicRebirthAgedToxins(player.PlayerId),
                    HyphalSurgeGrowths = tracking.GetHyphalSurgeGrowthCount(player.PlayerId),
                    HyphalVectoringGrowths = tracking.GetHyphalVectoringGrowthCount(player.PlayerId),
                    HyphalVectoringInfested = tracking.GetHyphalVectoringInfested(player.PlayerId),
                    HyphalVectoringReclaimed = tracking.GetHyphalVectoringReclaimed(player.PlayerId),
                    HyphalVectoringCatabolicGrowth = tracking.GetHyphalVectoringCatabolicGrowth(player.PlayerId),
                    HyphalVectoringAlreadyOwned = tracking.GetHyphalVectoringAlreadyOwned(player.PlayerId),
                    HyphalVectoringColonized = tracking.GetHyphalVectoringColonized(player.PlayerId),
                    HyphalVectoringInvalid = tracking.GetHyphalVectoringInvalid(player.PlayerId),
                    PutrefactiveRejuvenationCyclesReduced = tracking.GetPutrefactiveRejuvenationGrowthCyclesReduced(player.PlayerId),

                    // --- Surge mutation effect counters ---
                    ChitinFortificationCellsFortified = tracking.GetChitinFortificationCellsFortified(player.PlayerId),
                    MimeticResilienceInfestations = tracking.GetMimeticResilienceInfestations(player.PlayerId),
                    MimeticResilienceDrops = tracking.GetMimeticResilienceDrops(player.PlayerId),

                    // --- Cytolytic Burst effect counters ---
                    CytolyticBurstToxins = tracking.GetCytolyticBurstToxins(player.PlayerId),
                    CytolyticBurstKills = tracking.GetCytolyticBurstKills(player.PlayerId),

                    // --- Hypersystemic Regeneration effect counters ---
                    HypersystemicRegenerationResistance = tracking.GetHypersystemicRegenerationResistance(player.PlayerId),
                    HypersystemicDiagonalReclaims = tracking.GetHypersystemicDiagonalReclaims(player.PlayerId),

                    // --- Putrefactive Cascade effect counters ---
                    PutrefactiveCascadeKills = tracking.GetPutrefactiveCascadeKills(player.PlayerId),
                    PutrefactiveCascadeToxified = tracking.GetPutrefactiveCascadeToxified(player.PlayerId),

                    // --- Ontogenic Regression effect counters ---
                    OntogenicRegressionActivations = tracking.GetOntogenicRegressionActivations(player.PlayerId),
                    OntogenicRegressionDevolvedLevels = tracking.GetOntogenicRegressionDevolvedLevels(player.PlayerId),
                    OntogenicRegressionTier5PlusLevels = tracking.GetOntogenicRegressionTier5PlusLevels(player.PlayerId),

                    OntogenicRegressionFailureBonuses = tracking.GetOntogenicRegressionFailureBonuses(player.PlayerId),
                    OntogenicRegressionSacrificeCells = tracking.GetOntogenicRegressionSacrificeCells(player.PlayerId),
                    OntogenicRegressionSacrificeLevelOffset = tracking.GetOntogenicRegressionSacrificeLevelOffset(player.PlayerId),

                    // --- Mutation point income and spending ---
                    AdaptiveExpressionPointsEarned = tracking.GetAdaptiveExpressionPointsEarned(player.PlayerId),
                    MutatorPhenotypePointsEarned = tracking.GetMutatorPhenotypePointsEarned(player.PlayerId),
                    HyperadaptiveDriftPointsEarned = tracking.GetHyperadaptiveDriftPointsEarned(player.PlayerId),
                    AnabolicInversionPointsEarned = tracking.GetAnabolicInversionPointsEarned(player.PlayerId),
                    MutationPointIncome = tracking.GetMutationPointIncome(player.PlayerId),
                    MutationPointsSpentByTier = tracking.GetMutationPointsSpentByTier(player.PlayerId),
                    TotalMutationPointsSpent = tracking.GetTotalMutationPointsSpent(player.PlayerId),
                    BankedPoints = tracking.GetBankedPoints(player.PlayerId),

                    // Mycovariant summary
                    Mycovariants = BuildMycovariantResults(player, tracking),

                    // Compute average AIScoreAtDraft for AI players
                    AvgAIScoreAtDraft = aiScores.Count > 0 ? aiScores.Average() : (float?)null
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
                TrackingContext = tracking,
                PerimeterProliferatorGrowthsByPlayer = tracking.GetAllPerimeterProliferatorGrowths()
            };
        }

        private static List<MycovariantResult> BuildMycovariantResults(Player player, SimulationTrackingContext tracking)
        {
            var results = new List<MycovariantResult>();

            foreach (var myco in player.PlayerMycovariants)
            {
                var effectCounts = new Dictionary<MycovariantEffectType, int>();

                switch (myco.MycovariantId)
                {
                    case var id when id == MycovariantIds.PlasmidBountyId:
                        effectCounts[MycovariantEffectType.MpBonus] = MycovariantGameBalance.PlasmidBountyMutationPointAward;
                        break;

                    case var id when id == MycovariantIds.PlasmidBountyIIId:
                        effectCounts[MycovariantEffectType.MpBonus] = MycovariantGameBalance.PlasmidBountyIIMutationPointAward;
                        break;

                    case var id when id == MycovariantIds.PlasmidBountyIIIId:
                        effectCounts[MycovariantEffectType.MpBonus] = MycovariantGameBalance.PlasmidBountyIIIMutationPointAward;
                        break;

                    case var id when id == MycovariantIds.BallistosporeDischargeIId ||
                                    id == MycovariantIds.BallistosporeDischargeIIId ||
                                    id == MycovariantIds.BallistosporeDischargeIIIId:
                        {
                            int drops = tracking.GetBallistosporeDischargeDrops(player.PlayerId);
                            if (drops > 0) effectCounts[MycovariantEffectType.Drops] = drops;
                            break;
                        }

                    case var id when id == MycovariantIds.NeutralizingMantleId:
                        {
                            int neutralized = tracking.GetNeutralizingMantleEffects(player.PlayerId);
                            if (neutralized > 0) effectCounts[MycovariantEffectType.Neutralizations] = neutralized;
                            break;
                        }

                    case var id when id == MycovariantIds.JettingMyceliumNorthId ||
                                     id == MycovariantIds.JettingMyceliumEastId ||
                                     id == MycovariantIds.JettingMyceliumSouthId ||
                                     id == MycovariantIds.JettingMyceliumWestId:
                        {
                            int infested = tracking.GetJettingMyceliumInfested(player.PlayerId);
                            int reclaimed = tracking.GetJettingMyceliumReclaimed(player.PlayerId);
                            int catabolicGrowth = tracking.GetJettingMyceliumCatabolicGrowth(player.PlayerId);
                            int colonized = tracking.GetJettingMyceliumColonized(player.PlayerId);
                            int poisoned = tracking.GetJettingMyceliumPoisoned(player.PlayerId);

                            if (infested > 0) effectCounts[MycovariantEffectType.Infested] = infested;
                            if (reclaimed > 0) effectCounts[MycovariantEffectType.Reclaimed] = reclaimed;
                            if (catabolicGrowth > 0) effectCounts[MycovariantEffectType.CatabolicGrowth] = catabolicGrowth;
                            if (colonized > 0) effectCounts[MycovariantEffectType.Colonized] = colonized;
                            if (poisoned > 0) effectCounts[MycovariantEffectType.Poisoned] = poisoned;
                            break;
                        }

                    case var id when id == MycovariantIds.MycelialBastionIId ||
                                    id == MycovariantIds.MycelialBastionIIId ||
                                    id == MycovariantIds.MycelialBastionIIIId:
                        {
                            int bastioned = tracking.GetBastionedCells(player.PlayerId);
                            if (bastioned > 0) effectCounts[MycovariantEffectType.FortifiedCells] = bastioned;
                            break;
                        }

                    case var id when id == MycovariantIds.SurgicalInoculationId:
                        {
                            int drops = tracking.GetSurgicalInoculationDrops(player.PlayerId);
                            if (drops > 0) effectCounts[MycovariantEffectType.ResistantDrops] = drops;
                            break;
                        }

                    case var id when id == MycovariantIds.PerimeterProliferatorId:
                        {
                            int perim = tracking.GetPerimeterProliferatorGrowths(player.PlayerId);
                            if (perim > 0) effectCounts[MycovariantEffectType.PerimeterProliferation] = perim;
                            break;
                        }

                    case var id when id == MycovariantIds.EnduringToxaphoresId:
                        {
                            int extended = tracking.GetEnduringToxaphoresExtendedCycles(player.PlayerId);
                            if (extended > 0)
                                effectCounts[MycovariantEffectType.ExtendedCycles] = extended;
                            int existing = tracking.GetEnduringToxaphoresExistingExtensions(player.PlayerId);
                            if (existing > 0)
                                effectCounts[MycovariantEffectType.ExistingExtensions] = existing;
                            break;
                        }

                    case var id when id == MycovariantIds.ReclamationRhizomorphsId:
                        {
                            int secondAttempts = tracking.GetReclamationRhizomorphsSecondAttempts(player.PlayerId);
                            if (secondAttempts > 0)
                                effectCounts[MycovariantEffectType.SecondReclamationAttempts] = secondAttempts;
                            break;
                        }

                    case var id when id == MycovariantIds.NecrophoricAdaptation:
                        {
                            int reclamations = tracking.GetNecrophoricAdaptationReclamations(player.PlayerId);
                            if (reclamations > 0)
                                effectCounts[MycovariantEffectType.NecrophoricAdaptationReclamations] = reclamations;
                            break;
                        }

                    case var id when id == MycovariantIds.CytolyticBurstId:
                        {
                            int toxins = tracking.GetCytolyticBurstToxins(player.PlayerId);
                            int kills = tracking.GetCytolyticBurstKills(player.PlayerId);
                            if (toxins > 0) effectCounts[MycovariantEffectType.CytolyticBurstToxins] = toxins;
                            if (kills > 0) effectCounts[MycovariantEffectType.CytolyticBurstKills] = kills;
                            break;
                        }

                    case var id when id == MycovariantIds.ChemotacticMycotoxinsId:
                        {
                            int relocations = tracking.GetChemotacticMycotoxinsRelocations(player.PlayerId);
                            if (relocations > 0) effectCounts[MycovariantEffectType.Relocations] = relocations;
                            break;
                        }

                    case var id when id == MycovariantIds.CornerConduitIId:
                        {
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.CornerConduitInfestations, out var inf) && inf > 0)
                                effectCounts[MycovariantEffectType.CornerConduitInfestations] = inf;
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.CornerConduitColonizations, out var col) && col > 0)
                                effectCounts[MycovariantEffectType.CornerConduitColonizations] = col;
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.CornerConduitReclaims, out var rec) && rec > 0)
                                effectCounts[MycovariantEffectType.CornerConduitReclaims] = rec;
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.CornerConduitToxinsReplaced, out var tox) && tox > 0)
                                effectCounts[MycovariantEffectType.CornerConduitToxinsReplaced] = tox;
                            break;
                        }

                    case var id when id == MycovariantIds.AggressotropicConduitIId ||
                                     id == MycovariantIds.AggressotropicConduitIIId ||
                                     id == MycovariantIds.AggressotropicConduitIIIId:
                        {
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.AggressotropicConduitInfestations, out var inf) && inf > 0)
                                effectCounts[MycovariantEffectType.AggressotropicConduitInfestations] = inf;
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.AggressotropicConduitColonizations, out var col) && col > 0)
                                effectCounts[MycovariantEffectType.AggressotropicConduitColonizations] = col;
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.AggressotropicConduitReclaims, out var rec) && rec > 0)
                                effectCounts[MycovariantEffectType.AggressotropicConduitReclaims] = rec;
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.AggressotropicConduitToxinsReplaced, out var tox) && tox > 0)
                                effectCounts[MycovariantEffectType.AggressotropicConduitToxinsReplaced] = tox;
                            if (myco.EffectCounts.TryGetValue(MycovariantEffectType.AggressotropicConduitResistantPlacements, out var rp) && rp > 0)
                                effectCounts[MycovariantEffectType.AggressotropicConduitResistantPlacements] = rp;
                            break;
                        }
                }

                // Convert keys to string for MycovariantResult
                var effectCountsAsString = effectCounts.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value
                );

                results.Add(new MycovariantResult
                {
                    MycovariantId = myco.MycovariantId,
                    MycovariantName = myco.Mycovariant.Name,
                    MycovariantType = myco.Mycovariant.Type.ToString(),
                    Triggered = myco.HasTriggered,
                    EffectCounts = effectCountsAsString,
                    AIScoreAtDraft = myco.AIScoreAtDraft
                });
            }
            return results;
        }
    }
}