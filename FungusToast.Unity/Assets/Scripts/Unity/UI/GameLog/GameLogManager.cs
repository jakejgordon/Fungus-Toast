using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;

namespace FungusToast.Unity.UI.GameLog
{
    public class GameLogManager : MonoBehaviour, ISimulationObserver, IGameLogManager
    {
        private static class EventKinds
        {
            public const string Colonized = "Colonized";
            public const string Infested = "Infested";
            public const string Reclaimed = "Reclaimed";
            public const string Overgrown = "Overgrown";
            public const string Toxified = "Toxified";
            public const string Poisoned = "Poisoned";
            public static readonly string[] Ordered = { Colonized, Infested, Reclaimed, Overgrown, Toxified, Poisoned };
        }

        private class PlayerLogEvent { public string Message; public GameLogCategory Category; public int Round; public DateTime Timestamp; public bool IsClearMarker; }
        private class PlayerLogSummary
        {
            public int PlayerId { get; }
            private readonly List<PlayerLogEvent> _events = new();
            public PlayerLogSummary(int id) { PlayerId = id; }
            public void Add(PlayerLogEvent e) => _events.Add(e);
            public IEnumerable<PlayerLogEvent> GetLast(int count) => _events.TakeLast(count);
            public void Clear(int round)
            { _events.Clear(); _events.Add(new PlayerLogEvent { Message = "Log cleared", Category = GameLogCategory.Normal, Round = round, Timestamp = DateTime.Now, IsClearMarker = true }); }
        }
        private struct PlayerSnapshot { public int Living; public int Dead; public int Toxins; }
        private class PlayerLogAggregation
        {
            public Dictionary<string, int> Totals = new();
            public Dictionary<string, Dictionary<GrowthSource, int>> PerSource = new();
            public Dictionary<DeathReason, int> Deaths = new();
            public Dictionary<string, int> FreePointsBySource = new();
            public Dictionary<string, Dictionary<string, int>> FreeUpgradesBySource = new();
            public PlayerSnapshot? DraftStartSnapshot;
            public void ResetCellEvents() { Totals.Clear(); PerSource.Clear(); Deaths.Clear(); }
            public void ResetAll() { ResetCellEvents(); FreePointsBySource.Clear(); FreeUpgradesBySource.Clear(); DraftStartSnapshot = null; }
        }
        private enum LogSegmentType { None, MutationPhaseStart, GrowthPhase, DecayPhase, DraftPhase }

        // NEW: encapsulate pending segment summaries with correct round number
        private class SegmentSummary { public string Message; public int Round; }

        public event Action<GameLogEntry> OnNewLogEntry;
        private GameBoard board; private GameLogRouter router;
        private readonly Dictionary<int, PlayerLogSummary> summaries = new();
        private readonly Dictionary<int, PlayerSnapshot> roundStartSnapshots = new();
        private readonly Dictionary<int, PlayerLogAggregation> aggregations = new();
        private readonly Dictionary<int, Queue<SegmentSummary>> pendingSegmentSummaries = new(); // changed to store round
        private readonly Dictionary<int, string> pendingRoundSummaries = new();
        private readonly Dictionary<int, int> pendingRoundNumber = new();
        private int activePlayerId = -1;
        private bool initialized = false;
        private LogSegmentType currentSegment = LogSegmentType.None;
        private const int MAX_RETURN = 50;
        private bool IsSilentMode => router?.IsSilentMode ?? false;
        private int lastRoundCompletedRound = -1; // last round number processed in OnRoundComplete

        public void Initialize(GameBoard gameBoard)
        {
            if (initialized) return;
            board = gameBoard; if (board == null) return;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
            {
                roundStartSnapshots[hp.PlayerId] = TakeSnapshot(hp.PlayerId);
                aggregations[hp.PlayerId] = new PlayerLogAggregation();
                pendingSegmentSummaries[hp.PlayerId] = new Queue<SegmentSummary>();
            }
            board.CellPoisoned += OnCellPoisoned;
            board.CellColonized += OnCellColonized;
            board.CellInfested += OnCellInfested;
            board.CellReclaimed += OnCellReclaimed;
            board.CellToxified += OnCellToxified;
            board.CellOvergrown += OnCellOvergrown;
            initialized = true;
        }
        public void SetGameLogRouter(GameLogRouter r) => router = r;
        private void OnDestroy()
        {
            if (board == null) return;
            board.CellPoisoned -= OnCellPoisoned;
            board.CellColonized -= OnCellColonized;
            board.CellInfested -= OnCellInfested;
            board.CellReclaimed -= OnCellReclaimed;
            board.CellToxified -= OnCellToxified;
            board.CellOvergrown -= OnCellOvergrown;
        }

        public void OnLogSegmentStart(string segmentName)
        {
            if (IsSilentMode || board == null) return;
            var newType = ParseSegment(segmentName);
            FlushPreviousSegmentIntoQueues();
            currentSegment = newType;
            if (newType == LogSegmentType.DraftPhase)
                foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
                    aggregations[hp.PlayerId].DraftStartSnapshot = TakeSnapshot(hp.PlayerId);
        }
        private LogSegmentType ParseSegment(string name) => name switch
        {
            "MutationPhaseStart" => LogSegmentType.MutationPhaseStart,
            "GrowthPhase" => LogSegmentType.GrowthPhase,
            "DecayPhase" => LogSegmentType.DecayPhase,
            "DraftPhase" => LogSegmentType.DraftPhase,
            _ => LogSegmentType.None
        };

        private void FlushPreviousSegmentIntoQueues()
        {
            if (board == null) return;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
            {
                if (!aggregations.TryGetValue(hp.PlayerId, out var agg)) continue;
                var summaryText = BuildSegmentSummary(agg, currentSegment, hp.PlayerId);
                if (!string.IsNullOrEmpty(summaryText))
                {
                    int roundForSummary;
                    if (currentSegment == LogSegmentType.DecayPhase)
                    {
                        // Decay segment belongs to the round that just completed (stored in lastRoundCompletedRound)
                        roundForSummary = lastRoundCompletedRound >= 0 ? lastRoundCompletedRound : (board.CurrentRound - 1);
                    }
                    else
                    {
                        // Other segments occur inside the current round
                        roundForSummary = board.CurrentRound;
                    }
                    pendingSegmentSummaries[hp.PlayerId].Enqueue(new SegmentSummary { Message = summaryText, Round = roundForSummary });
                }
                agg.ResetAll();
            }
        }

        public void EmitPendingSegmentSummariesFor(int playerId)
        {
            if (playerId < 0) return;
            if (pendingSegmentSummaries.TryGetValue(playerId, out var q))
            {
                while (q.Count > 0)
                {
                    var seg = q.Dequeue();
                    AddPlayerEvent(playerId, seg.Message, GameLogCategory.Normal, explicitRound: seg.Round);
                }
            }
            if (pendingRoundSummaries.TryGetValue(playerId, out var roundMsg))
            {
                int roundNumber = pendingRoundNumber.TryGetValue(playerId, out var rn) ? rn : (board?.CurrentRound ?? 0) - 1;
                AddPlayerEvent(playerId, roundMsg, GameLogCategory.Normal, explicitRound: roundNumber);
                pendingRoundSummaries.Remove(playerId);
                pendingRoundNumber.Remove(playerId);
            }
        }
        public bool HasPendingSummaries(int playerId) => (pendingSegmentSummaries.TryGetValue(playerId, out var q) && q.Count > 0) || pendingRoundSummaries.ContainsKey(playerId);

        private string BuildSegmentSummary(PlayerLogAggregation agg, LogSegmentType type, int playerId) => type switch
        {
            LogSegmentType.MutationPhaseStart => FormatMutationPhaseStart(agg),
            LogSegmentType.GrowthPhase => FormatGrowthOrDecay("Growth Phase", agg),
            LogSegmentType.DecayPhase => FormatGrowthOrDecay("Decay Phase", agg),
            LogSegmentType.DraftPhase => FormatDraftPhase(agg, playerId),
            _ => string.Empty
        };
        private string FormatMutationPhaseStart(PlayerLogAggregation agg)
        {
            bool anyPoints = agg.FreePointsBySource.Values.Any(v => v > 0);
            bool anyUpgrades = agg.FreeUpgradesBySource.Values.Any(d => d.Values.Any(v => v > 0));
            if (!anyPoints && !anyUpgrades) return string.Empty;
            var clauses = new List<string>();
            if (anyPoints)
            {
                var parts = agg.FreePointsBySource.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).Select(kv => kv.Value + " from " + kv.Key).ToList();
                clauses.Add("Free Points: " + string.Join(", ", parts));
            }
            if (anyUpgrades)
            {
                var upParts = new List<string>();
                foreach (var src in agg.FreeUpgradesBySource.OrderBy(x => x.Key))
                    foreach (var mut in src.Value.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value))
                        upParts.Add(src.Key + " upgraded " + mut.Value + " level(s) of " + mut.Key);
                if (upParts.Count > 0) clauses.Add("Upgrades: " + string.Join(", ", upParts));
            }
            return "Mutation Phase Start: " + string.Join("; ", clauses);
        }
        private string FormatGrowthOrDecay(string prefix, PlayerLogAggregation agg)
        {
            var blocks = new List<string>();
            foreach (var kind in EventKinds.Ordered)
            {
                if (!agg.Totals.TryGetValue(kind, out int total) || total <= 0) continue;
                if (!agg.PerSource.TryGetValue(kind, out var sourceDict)) continue;
                var perSource = sourceDict.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).Select(kv => kv.Value + " from " + AbilityName(kv.Key)).ToList();
                if (perSource.Count > 0) blocks.Add(kind + " " + total + " (" + string.Join(", ", perSource) + ")");
            }
            if (agg.Deaths.Values.Any(v => v > 0))
            {
                int totalDeaths = agg.Deaths.Values.Sum();
                var reasons = agg.Deaths.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value).ToList();
                blocks.Add("Deaths " + totalDeaths + " (" + string.Join(", ", reasons) + ")");
            }
            if (blocks.Count == 0) return string.Empty;
            return prefix + ": " + string.Join(", ", blocks);
        }
        private string FormatDraftPhase(PlayerLogAggregation agg, int playerId)
        {
            if (!agg.DraftStartSnapshot.HasValue || board == null) return string.Empty;
            var start = agg.DraftStartSnapshot.Value; var end = TakeSnapshot(playerId);
            int dl = end.Living - start.Living; int dd = end.Dead - start.Dead; int dt = end.Toxins - start.Toxins;
            var deltas = new List<string>();
            if (dl != 0) deltas.Add("Living " + (dl > 0 ? "+" + dl : dl.ToString()));
            if (dd != 0) deltas.Add("Dead " + (dd > 0 ? "+" + dd : dd.ToString()));
            if (dt != 0) deltas.Add("Toxins " + (dt > 0 ? "+" + dt : dt.ToString()));
            if (deltas.Count == 0) return string.Empty;
            return "Draft Phase: " + string.Join(", ", deltas);
        }

        public void OnRoundStart(int round)
        {
            if (board == null) return;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
                roundStartSnapshots[hp.PlayerId] = TakeSnapshot(hp.PlayerId);
        }
        public void OnRoundComplete(int round)
        {
            if (board == null) return;
            if (round == lastRoundCompletedRound) return;
            lastRoundCompletedRound = round;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
            {
                if (!roundStartSnapshots.TryGetValue(hp.PlayerId, out var start)) continue;
                var end = TakeSnapshot(hp.PlayerId);
                int dl = end.Living - start.Living; int dd = end.Dead - start.Dead; int dt = end.Toxins - start.Toxins;
                string msg = (dl != 0 || dd != 0 || dt != 0)
                    ? RoundSummaryFormatter.FormatRoundSummary(round, dl, dd, dt, end.Living, end.Dead, end.Toxins, 0f, true).Replace("summary:", "Summary:")
                    : $"Round {round} Summary: no changes";
                pendingRoundSummaries[hp.PlayerId] = msg;
                pendingRoundNumber[hp.PlayerId] = round;
            }
        }

        private PlayerSnapshot TakeSnapshot(int playerId)
        {
            var cells = board.GetAllCellsOwnedBy(playerId);
            return new PlayerSnapshot { Living = cells.Count(c => c.IsAlive), Dead = cells.Count(c => c.IsDead), Toxins = cells.Count(c => c.IsToxin) };
        }
        private PlayerLogSummary GetSummary(int playerId)
        { if (!summaries.TryGetValue(playerId, out var s)) { s = new PlayerLogSummary(playerId); summaries[playerId] = s; } return s; }
        private void AddPlayerEvent(int playerId, String message, GameLogCategory cat, int? explicitRound = null)
        {
            if (IsSilentMode || playerId < 0) return;
            var s = GetSummary(playerId);
            s.Add(new PlayerLogEvent { Message = message, Category = cat, Round = explicitRound ?? (board?.CurrentRound ?? 0), Timestamp = DateTime.Now, IsClearMarker = false });
            if (playerId == activePlayerId) OnNewLogEntry?.Invoke(new GameLogEntry(message, cat, null, playerId, explicitRound));
        }
        private bool IsHuman(int playerId)
        { if (board == null) return false; var p = board.Players.FirstOrDefault(pl => pl.PlayerId == playerId); return p != null && p.PlayerType == PlayerTypeEnum.Human; }
        private string AbilityName(GrowthSource src) => src switch
        {
            GrowthSource.JettingMycelium => "Jetting Mycelium",
            GrowthSource.CytolyticBurst => "Cytolytic Burst",
            GrowthSource.SporicidalBloom => "Sporicidal Bloom",
            GrowthSource.MycotoxinTracer => "Mycotoxin Tracer",
            GrowthSource.PutrefactiveCascade => "Putrefactive Cascade",
            GrowthSource.HyphalVectoring => "Hyphal Vectoring",
            GrowthSource.MimeticResilience => "Mimetic Resilience",
            GrowthSource.SurgicalInoculation => "Surgical Inoculation",
            GrowthSource.Ballistospore => "Ballistospore Discharge",
            GrowthSource.HyphalOutgrowth => "Hyphal Outgrowth",
            GrowthSource.TendrilOutgrowth => "Tendril Outgrowth",
            GrowthSource.RegenerativeHyphae => "Regenerative Hyphae",
            GrowthSource.CornerConduit => "Corner Conduit",
            GrowthSource.AggressotropicConduit => "Aggressotropic Conduit",
            GrowthSource.Manual => "Manual placement",
            _ => src.ToString()
        };

        public void SetActiveHumanPlayer(int newHumanPlayerId, GameBoard currentBoard)
        {
            if (newHumanPlayerId == activePlayerId) return;
            activePlayerId = newHumanPlayerId;
            if (currentBoard != null && !roundStartSnapshots.ContainsKey(activePlayerId))
                roundStartSnapshots[activePlayerId] = TakeSnapshot(activePlayerId);
            Debug.Log($"[GameLogManager] Active player context switched -> PlayerId={activePlayerId}");
        }

        private PlayerLogAggregation Agg(int playerId)
        { if (!aggregations.TryGetValue(playerId, out var agg)) { agg = new PlayerLogAggregation(); aggregations[playerId] = agg; } return agg; }
        private void Inc(string kind, int playerId, GrowthSource src, int amount = 1)
        {
            var agg = Agg(playerId);
            if (!agg.Totals.ContainsKey(kind)) agg.Totals[kind] = 0; agg.Totals[kind] += amount;
            if (!agg.PerSource.TryGetValue(kind, out var dict)) { dict = new Dictionary<GrowthSource, int>(); agg.PerSource[kind] = dict; }
            if (!dict.ContainsKey(src)) dict[src] = 0; dict[src] += amount;
        }
        private void IncDeath(int playerId, DeathReason reason, int count)
        { var agg = Agg(playerId); if (!agg.Deaths.ContainsKey(reason)) agg.Deaths[reason] = 0; agg.Deaths[reason] += count; }
        private void AddFreePoints(int playerId, string source, int points)
        { var agg = Agg(playerId); if (!agg.FreePointsBySource.ContainsKey(source)) agg.FreePointsBySource[source] = 0; agg.FreePointsBySource[source] += points; }
        private void AddFreeUpgrade(int playerId, string source, string mutationName, int levels)
        { var agg = Agg(playerId); if (!agg.FreeUpgradesBySource.TryGetValue(source, out var dict)) { dict = new Dictionary<string, int>(); agg.FreeUpgradesBySource[source] = dict; } if (!dict.ContainsKey(mutationName)) dict[mutationName] = 0; dict[mutationName] += levels; }

        private void OnCellPoisoned(int playerId, int tileId, int oldOwnerId, GrowthSource src) { if (IsSilentMode) return; if (IsHuman(playerId)) Inc(EventKinds.Poisoned, playerId, src); if (IsHuman(oldOwnerId)) IncDeath(oldOwnerId, DeathReason.Poisoned, 1); }
        private void OnCellColonized(int playerId, int tileId, GrowthSource src) { if (!IsSilentMode && IsHuman(playerId)) Inc(EventKinds.Colonized, playerId, src); }
        private void OnCellInfested(int playerId, int tileId, int oldOwnerId, GrowthSource src) { if (IsSilentMode) return; if (IsHuman(playerId)) Inc(EventKinds.Infested, playerId, src); if (IsHuman(oldOwnerId)) IncDeath(oldOwnerId, DeathReason.Infested, 1); }
        private void OnCellOvergrown(int playerId, int tileId, int oldOwnerId, GrowthSource src) { if (!IsSilentMode && IsHuman(playerId)) Inc(EventKinds.Overgrown, playerId, src); }
        private void OnCellReclaimed(int playerId, int tileId, GrowthSource src) { if (!IsSilentMode && IsHuman(playerId)) Inc(EventKinds.Reclaimed, playerId, src); }
        private void OnCellToxified(int playerId, int tileId, GrowthSource src) { if (!IsSilentMode && IsHuman(playerId)) Inc(EventKinds.Toxified, playerId, src); }

        // === ISimulationObserver Implementation (restored) ===
        public void RecordMutationPointIncome(int playerId, int totalMutationPoints) { }
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned) { if (freePointsEarned > 0 && IsHuman(playerId)) AddFreePoints(playerId, "Mutator Phenotype", freePointsEarned); }
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned) { if (freePointsEarned > 0 && IsHuman(playerId)) AddFreePoints(playerId, "Hyperadaptive Drift", freePointsEarned); }
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus) { if (bonus > 0 && IsHuman(playerId)) AddFreePoints(playerId, "Adaptive Expression", bonus); }
        public void RecordAnabolicInversionBonus(int playerId, int bonus) { if (bonus > 0 && IsHuman(playerId)) AddFreePoints(playerId, "Anabolic Inversion", bonus); }
        public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints) { if (bonusPoints > 0 && IsHuman(playerId)) AddFreePoints(playerId, "Ontogenic Regression", bonusPoints); }
        public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset) { }
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1) { if (IsHuman(playerId)) IncDeath(playerId, reason, deathCount); }
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { if (catabolizedMutationPoints > 0 && IsHuman(playerId)) AddPlayerEvent(playerId, catabolizedMutationPoints == 1 ? "Earned 1 mutation point from Mycotoxin Catabolism" : $"Earned {catabolizedMutationPoints} mutation points from Mycotoxin Catabolism", GameLogCategory.Lucky); }
        public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName) { if (IsHuman(playerId) && !string.IsNullOrEmpty(mutationName)) AddFreeUpgrade(playerId, "Mutator Phenotype", mutationName, 1); }
        public void RecordSpecificMutationUpgrade(int playerId, string mutationName) { if (IsHuman(playerId) && !string.IsNullOrEmpty(mutationName)) AddFreeUpgrade(playerId, "Mutator Phenotype", mutationName, 1); }
        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained) { if (IsHuman(playerId) && targetLevelsGained > 0 && !string.IsNullOrEmpty(targetMutationName)) AddFreeUpgrade(playerId, "Ontogenic Regression", targetMutationName, targetLevelsGained); }
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned, bool deprecated = true) { if (freePointsEarned > 0 && IsHuman(playerId)) AddFreePoints(playerId, "Hyperadaptive Drift", freePointsEarned); }
        public void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations) { }
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
        public void RecordRegenerativeHyphaeReclaim(int playerId) { }
        public void ReportSporicidalSporeDrop(int playerId, int count) { }
        public void ReportNecrosporeDrop(int playerId, int count) { }
        public void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims) { if (IsHuman(playerId) && successfulReclaims > 0) Inc(EventKinds.Reclaimed, playerId, GrowthSource.NecrophyticBloom, successfulReclaims); }
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) { }
        public void RecordBankedPoints(int playerId, int pointsBanked) { }
        public void RecordHyphalSurgeGrowth(int playerId) { }
        public void RecordHyphalVectoringGrowth(int playerId, int cellsPlaced) { }
        public void ReportJettingMyceliumReclaimed(int playerId, int reclaimed) { if (IsHuman(playerId) && reclaimed > 0) Inc(EventKinds.Reclaimed, playerId, GrowthSource.JettingMycelium, reclaimed); }
        public void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportJettingMyceliumInvalid(int playerId, int invalid) { }
        public void ReportJettingMyceliumColonized(int playerId, int colonized) { if (IsHuman(playerId) && colonized > 0) Inc(EventKinds.Colonized, playerId, GrowthSource.JettingMycelium, colonized); }
        public void ReportJettingMyceliumToxified(int playerId, int toxified) { if (IsHuman(playerId) && toxified > 0) Inc(EventKinds.Toxified, playerId, GrowthSource.JettingMycelium, toxified); }
        public void ReportJettingMyceliumPoisoned(int playerId, int poisoned) { if (IsHuman(playerId) && poisoned > 0) Inc(EventKinds.Poisoned, playerId, GrowthSource.JettingMycelium, poisoned); }
        public void ReportJettingMyceliumInfested(int playerId, int infested) { if (IsHuman(playerId) && infested > 0) Inc(EventKinds.Infested, playerId, GrowthSource.JettingMycelium, infested); }
        public void ReportHyphalVectoringReclaimed(int playerId, int reclaimed) { if (IsHuman(playerId) && reclaimed > 0) Inc(EventKinds.Reclaimed, playerId, GrowthSource.HyphalVectoring, reclaimed); }
        public void ReportHyphalVectoringCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportHyphalVectoringAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportHyphalVectoringColonized(int playerId, int colonized) { if (IsHuman(playerId) && colonized > 0) Inc(EventKinds.Colonized, playerId, GrowthSource.HyphalVectoring, colonized); }
        public void ReportHyphalVectoringInvalid(int playerId, int invalid) { }
        public void ReportHyphalVectoringInfested(int playerId, int infested) { if (IsHuman(playerId) && infested > 0) Inc(EventKinds.Infested, playerId, GrowthSource.HyphalVectoring, infested); }
        public void RecordStandardGrowth(int playerId) { }
        public void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized) { }
        public void RecordBastionedCells(int playerId, int count) { }
        public void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged) { }
        public void RecordSurgicalInoculationDrop(int playerId, int count) { }
        public void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced) { }
        public void RecordPerimeterProliferatorGrowth(int playerId) { }
        public void RecordHyphalResistanceTransfer(int playerId, int count) { }
        public void RecordEnduringToxaphoresExtendedCycles(int playerId, int cycles) { }
        public void RecordEnduringToxaphoresExistingExtensions(int playerId, int cycles) { }
        public void RecordReclamationRhizomorphsSecondAttempt(int playerId, int count) { }
        public void RecordNecrophoricAdaptationReclamation(int playerId, int count) { }
        public void RecordBallistosporeDischarge(int playerId, int count) { }
        public void RecordChitinFortificationCellsFortified(int playerId, int count) { }
        public void RecordPutrefactiveCascadeKills(int playerId, int cascadeKills) { }
        public void RecordPutrefactiveCascadeToxified(int playerId, int toxified) { if (IsHuman(playerId) && toxified > 0) Inc(EventKinds.Toxified, playerId, GrowthSource.PutrefactiveCascade, toxified); }
        public void RecordMimeticResilienceInfestations(int playerId, int infestations) { if (IsHuman(playerId) && infestations > 0) Inc(EventKinds.Infested, playerId, GrowthSource.MimeticResilience, infestations); }
        public void RecordMimeticResilienceDrops(int playerId, int drops) { }
        public void RecordCytolyticBurstToxins(int playerId, int toxinsCreated) { if (IsHuman(playerId) && toxinsCreated > 0) Inc(EventKinds.Toxified, playerId, GrowthSource.CytolyticBurst, toxinsCreated); }
        public void RecordCytolyticBurstKills(int playerId, int cellsKilled) { }
        public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected) { }
        public void RecordHypersystemicRegenerationResistance(int playerId) { }
        public void RecordHypersystemicDiagonalReclaim(int playerId) { }
        // === End ISimulationObserver implementation ===

        public IEnumerable<GameLogEntry> GetRecentEntries(int count = 20)
        {
            if (activePlayerId < 0) return System.Linq.Enumerable.Empty<GameLogEntry>();
            if (!summaries.TryGetValue(activePlayerId, out var s)) return System.Linq.Enumerable.Empty<GameLogEntry>();
            return s.GetLast(System.Math.Min(count, MAX_RETURN)).Select(e => new GameLogEntry(e.Message, e.Category, null, activePlayerId, e.Round));
        }
        public void ClearLog()
        { if (activePlayerId < 0) return; int round = board?.CurrentRound ?? 0; GetSummary(activePlayerId).Clear(round); OnNewLogEntry?.Invoke(new GameLogEntry("Log cleared", GameLogCategory.Normal, null, activePlayerId)); }
        public void AddNormalEntry(string message, int? playerId = null) => AddPlayerEvent(playerId ?? activePlayerId, message, GameLogCategory.Normal);
        public void AddLuckyEntry(string message, int? playerId = null) => AddPlayerEvent(playerId ?? activePlayerId, message, GameLogCategory.Lucky);
        public void AddUnluckyEntry(string message, int? playerId = null) => AddPlayerEvent(playerId ?? activePlayerId, message, GameLogCategory.Unlucky);
    }
}
