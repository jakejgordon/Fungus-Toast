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
    /// <summary>
    /// Player-centric log manager. Stores independent logs per human player.
    /// Global game messages are handled elsewhere (GlobalGameLogManager).
    /// </summary>
    public class GameLogManager : MonoBehaviour, ISimulationObserver, IGameLogManager
    {
        #region Internal Models
        private class PlayerLogEvent
        {
            public string Message;
            public GameLogCategory Category;
            public int Round;
            public DateTime Timestamp;
            public bool IsClearMarker;
        }
        private class PlayerLogSummary
        {
            public int PlayerId { get; }
            private readonly List<PlayerLogEvent> _events = new();
            public int LastResetRound { get; private set; } = -1;
            public PlayerLogSummary(int id) { PlayerId = id; }
            public void Add(PlayerLogEvent e) => _events.Add(e);
            public IEnumerable<PlayerLogEvent> GetLast(int count) => _events.TakeLast(count);
            public void Clear(int round)
            {
                _events.Clear();
                LastResetRound = round;
                _events.Add(new PlayerLogEvent
                {
                    Message = "Log cleared",
                    Category = GameLogCategory.Normal,
                    Round = round,
                    Timestamp = DateTime.Now,
                    IsClearMarker = true
                });
            }
        }
        private struct PlayerSnapshot { public int Living; public int Dead; public int Toxins; }
        private class PreMutationTracker
        {
            public int MutatorPhenotype;
            public int HyperadaptiveDrift;
            public int AdaptiveExpression;
            public int AnabolicInversion;
            public int OntogenicRegressionFailure;
            public int OntogenicSacrificeCells;
            public int OntogenicSacrificeLevelOffset;
            public bool HasAny => MutatorPhenotype + HyperadaptiveDrift + AdaptiveExpression + AnabolicInversion + OntogenicRegressionFailure > 0
                                   || OntogenicSacrificeCells > 0 || OntogenicSacrificeLevelOffset != 0;
            public void Reset()
            {
                MutatorPhenotype = HyperadaptiveDrift = AdaptiveExpression = AnabolicInversion = OntogenicRegressionFailure = 0;
                OntogenicSacrificeCells = 0; OntogenicSacrificeLevelOffset = 0;
            }
        }
        #endregion

        #region Fields
        public event Action<GameLogEntry> OnNewLogEntry;

        private GameBoard board;
        private GameLogRouter router;
        private readonly Dictionary<int, PlayerLogSummary> summaries = new();
        private readonly Dictionary<int, PlayerSnapshot> roundStartSnapshots = new();
        private readonly Dictionary<int, PreMutationTracker> preMutationTrackers = new();

        private readonly Dictionary<DeathReason, int> decayPhaseDeaths = new();
        private bool trackDecayPhase;
        private bool trackPreMutation;
        private bool trackGrowthPhase;
        private bool trackResistance;

        private int chemotacticRelocations;
        private int necrophyticReclaims;
        private int regenerativeHyphaeReclaims;
        private int hypersystemicDiagonalReclaims;
        private int hypersystemicResistanceApplications;

        private int activePlayerId = -1; // current UI perspective
        private const int MAX_RETURN = 50;
        private bool IsSilentMode => router?.IsSilentMode ?? false;
        #endregion

        #region Initialization
        public void Initialize(GameBoard gameBoard)
        {
            board = gameBoard;
            if (board == null) return;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
            {
                roundStartSnapshots[hp.PlayerId] = TakeSnapshot(hp.PlayerId);
                preMutationTrackers[hp.PlayerId] = new PreMutationTracker();
            }
            board.CellPoisoned += OnCellPoisoned;
            board.CellColonized += OnCellColonized;
            board.CellInfested += OnCellInfested;
            board.CellReclaimed += OnCellReclaimed;
            board.CellToxified += OnCellToxified;
            board.PreGrowthCycle += OnPreGrowthCycle; // stubs
            board.PostGrowthPhase += OnPostGrowthPhase; // stubs
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
            board.PreGrowthCycle -= OnPreGrowthCycle;
            board.PostGrowthPhase -= OnPostGrowthPhase;
        }
        #endregion

        #region Round / Phase Hooks
        public void OnRoundStart(int round)
        {
            if (board == null) return;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
            {
                roundStartSnapshots[hp.PlayerId] = TakeSnapshot(hp.PlayerId);
                if (!preMutationTrackers.ContainsKey(hp.PlayerId)) preMutationTrackers[hp.PlayerId] = new PreMutationTracker();
                preMutationTrackers[hp.PlayerId].Reset();
            }
            decayPhaseDeaths.Clear();
            chemotacticRelocations = 0; necrophyticReclaims = 0;
            regenerativeHyphaeReclaims = 0; hypersystemicDiagonalReclaims = 0; hypersystemicResistanceApplications = 0;
            trackDecayPhase = true; trackPreMutation = true; trackGrowthPhase = true; trackResistance = true;
        }
        public void OnRoundComplete(int round)
        {
            if (board == null) return;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
            {
                if (!roundStartSnapshots.TryGetValue(hp.PlayerId, out var start)) continue;
                var end = TakeSnapshot(hp.PlayerId);
                int dl = end.Living - start.Living;
                int dd = end.Dead - start.Dead;
                int dt = end.Toxins - start.Toxins;
                if (dl != 0 || dd != 0 || dt != 0)
                {
                    string msg = RoundSummaryFormatter.FormatRoundSummary(round, dl, dd, dt, end.Living, end.Dead, end.Toxins, 0f, true);
                    AddPlayerEvent(hp.PlayerId, msg, GameLogCategory.Normal);
                }
            }
            trackDecayPhase = false;
        }
        public void OnPhaseStart(string phaseName)
        {
            if (phaseName == "Mutation Phase" && trackPreMutation)
            {
                EmitPreMutationSummaries();
                trackPreMutation = false;
            }
        }
        #endregion

        #region Logging Helpers
        private PlayerSnapshot TakeSnapshot(int playerId)
        {
            var cells = board.GetAllCellsOwnedBy(playerId);
            return new PlayerSnapshot
            {
                Living = cells.Count(c => c.IsAlive),
                Dead = cells.Count(c => c.IsDead),
                Toxins = cells.Count(c => c.IsToxin)
            };
        }
        private PlayerLogSummary GetSummary(int playerId)
        {
            if (!summaries.TryGetValue(playerId, out var s))
            {
                s = new PlayerLogSummary(playerId);
                summaries[playerId] = s;
            }
            return s;
        }
        private void AddPlayerEvent(int playerId, string message, GameLogCategory cat)
        {
            if (IsSilentMode || playerId < 0) return;
            var s = GetSummary(playerId);
            s.Add(new PlayerLogEvent { Message = message, Category = cat, Round = board?.CurrentRound ?? 0, Timestamp = DateTime.Now, IsClearMarker = false });
            if (playerId == activePlayerId)
                OnNewLogEntry?.Invoke(new GameLogEntry(message, cat, null, playerId));
        }
        private bool IsHuman(int playerId)
        {
            if (board == null) return false;
            var p = board.Players.FirstOrDefault(pl => pl.PlayerId == playerId);
            return p != null && p.PlayerType == PlayerTypeEnum.Human;
        }
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
        #endregion

        #region Pre-Mutation Summary
        private void EmitPreMutationSummaries()
        {
            if (board == null) return;
            foreach (var hp in board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human))
            {
                if (!preMutationTrackers.TryGetValue(hp.PlayerId, out var t) || !t.HasAny) continue;
                var parts = new List<string>();
                if (t.MutatorPhenotype > 0) parts.Add($"Mutator Phenotype: {t.MutatorPhenotype}");
                if (t.HyperadaptiveDrift > 0) parts.Add($"Hyperadaptive Drift: {t.HyperadaptiveDrift}");
                if (t.AdaptiveExpression > 0) parts.Add($"Adaptive Expression: {t.AdaptiveExpression}");
                if (t.AnabolicInversion > 0) parts.Add($"Anabolic Inversion: {t.AnabolicInversion}");
                if (t.OntogenicRegressionFailure > 0) parts.Add($"Ontogenic Regression: {t.OntogenicRegressionFailure}");
                if (t.OntogenicSacrificeCells > 0) parts.Add($"OR Sacrifices: {t.OntogenicSacrificeCells} cells");
                if (t.OntogenicSacrificeLevelOffset != 0) parts.Add($"OR Level Offset: {t.OntogenicSacrificeLevelOffset}");
                int total = t.MutatorPhenotype + t.HyperadaptiveDrift + t.AdaptiveExpression + t.AnabolicInversion + t.OntogenicRegressionFailure;
                string msg = $"Pre-Mutation Phase: Earned {total} mutation points ({string.Join(", ", parts)})";
                AddPlayerEvent(hp.PlayerId, msg, GameLogCategory.Lucky);
            }
        }
        #endregion

        #region IGameLogManager (UI consumption)
        public IEnumerable<GameLogEntry> GetRecentEntries(int count = 20)
        {
            if (activePlayerId < 0) return Enumerable.Empty<GameLogEntry>();
            if (!summaries.TryGetValue(activePlayerId, out var s)) return Enumerable.Empty<GameLogEntry>();
            return s.GetLast(Math.Min(count, MAX_RETURN))
                .Select(e => new GameLogEntry(e.Message, e.Category, null, activePlayerId));
        }
        public void ClearLog()
        {
            if (activePlayerId < 0) return;
            int round = board?.CurrentRound ?? 0;
            GetSummary(activePlayerId).Clear(round);
            OnNewLogEntry?.Invoke(new GameLogEntry("Log cleared", GameLogCategory.Normal, null, activePlayerId));
        }
        public void AddNormalEntry(string message, int? playerId = null) => AddPlayerEvent(playerId ?? activePlayerId, message, GameLogCategory.Normal);
        public void AddLuckyEntry(string message, int? playerId = null) => AddPlayerEvent(playerId ?? activePlayerId, message, GameLogCategory.Lucky);
        public void AddUnluckyEntry(string message, int? playerId = null) => AddPlayerEvent(playerId ?? activePlayerId, message, GameLogCategory.Unlucky);
        #endregion

        #region Perspective Switch
        public void SetActiveHumanPlayer(int newHumanPlayerId, GameBoard currentBoard)
        {
            if (newHumanPlayerId == activePlayerId) return;
            activePlayerId = newHumanPlayerId;
            if (currentBoard != null && !roundStartSnapshots.ContainsKey(activePlayerId))
                roundStartSnapshots[activePlayerId] = TakeSnapshot(activePlayerId);
            Debug.Log($"[GameLogManager] Active player context switched -> PlayerId={activePlayerId}");
        }
        #endregion

        #region Board Event Handlers (single-event logging)
        private void OnCellPoisoned(int playerId, int tileId, int oldOwnerId, GrowthSource src)
        {
            if (IsSilentMode) return;
            if (IsHuman(playerId)) AddPlayerEvent(playerId, $"{AbilityName(src)} poisoned a cell", GameLogCategory.Lucky);
            else if (IsHuman(oldOwnerId)) AddPlayerEvent(oldOwnerId, $"Cell poisoned by {AbilityName(src)}", GameLogCategory.Unlucky);
        }
        private void OnCellColonized(int playerId, int tileId, GrowthSource src)
        { if (!IsSilentMode && IsHuman(playerId)) AddPlayerEvent(playerId, $"{AbilityName(src)} colonized a tile", GameLogCategory.Lucky); }
        private void OnCellInfested(int playerId, int tileId, int oldOwnerId, GrowthSource src)
        {
            if (IsSilentMode) return;
            if (IsHuman(playerId)) AddPlayerEvent(playerId, $"{AbilityName(src)} killed a cell", GameLogCategory.Lucky);
            if (IsHuman(oldOwnerId)) AddPlayerEvent(oldOwnerId, $"Cell killed by {AbilityName(src)}", GameLogCategory.Unlucky);
        }
        private void OnCellReclaimed(int playerId, int tileId, GrowthSource src)
        {
            if (IsSilentMode) return;
            if (!IsHuman(playerId)) return;
            if (src == GrowthSource.RegenerativeHyphae) { regenerativeHyphaeReclaims++; return; }
            AddPlayerEvent(playerId, $"{AbilityName(src)} reclaimed a cell", GameLogCategory.Lucky);
        }
        private void OnCellToxified(int playerId, int tileId, GrowthSource src)
        { if (!IsSilentMode && IsHuman(playerId)) AddPlayerEvent(playerId, $"{AbilityName(src)} toxified a tile", GameLogCategory.Lucky); }
        private void OnPreGrowthCycle() { }
        private void OnPostGrowthPhase() { }
        #endregion

        #region ISimulationObserver (tracked)
        public void RecordMutationPointIncome(int playerId, int totalMutationPoints) { }
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned) { if (trackPreMutation && IsHuman(playerId)) preMutationTrackers[playerId].MutatorPhenotype += freePointsEarned; }
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned) { if (trackPreMutation && IsHuman(playerId)) preMutationTrackers[playerId].HyperadaptiveDrift += freePointsEarned; }
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus) { if (trackPreMutation && IsHuman(playerId)) preMutationTrackers[playerId].AdaptiveExpression += bonus; }
        public void RecordAnabolicInversionBonus(int playerId, int bonus) { if (trackPreMutation && IsHuman(playerId)) preMutationTrackers[playerId].AnabolicInversion += bonus; }
        public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints) { if (trackPreMutation && IsHuman(playerId)) preMutationTrackers[playerId].OntogenicRegressionFailure += bonusPoints; }
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1) { if (trackDecayPhase && IsHuman(playerId)) { if (!decayPhaseDeaths.ContainsKey(reason)) decayPhaseDeaths[reason] = 0; decayPhaseDeaths[reason] += deathCount; } }
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { if (catabolizedMutationPoints > 0 && IsHuman(playerId)) AddPlayerEvent(playerId, catabolizedMutationPoints == 1 ? "Earned 1 mutation point from Mycotoxin Catabolism" : $"Earned {catabolizedMutationPoints} mutation points from Mycotoxin Catabolism", GameLogCategory.Lucky); }
        public void RecordHypersystemicRegenerationResistance(int playerId) { if (trackResistance && IsHuman(playerId)) hypersystemicResistanceApplications++; }
        public void RecordHypersystemicDiagonalReclaim(int playerId) { if (trackGrowthPhase && IsHuman(playerId)) hypersystemicDiagonalReclaims++; }
        public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName) { if (IsHuman(playerId) && !string.IsNullOrEmpty(mutationName)) AddPlayerEvent(playerId, $"Mutator Phenotype upgraded {mutationName}", GameLogCategory.Lucky); }
        public void RecordSpecificMutationUpgrade(int playerId, string mutationName) => RecordMutatorPhenotypeUpgrade(playerId, mutationName);
        public void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations) { if (IsHuman(playerId) && relocations > 0) chemotacticRelocations += relocations; }
        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained)
        { if (IsHuman(playerId) && sourceLevelsLost > 0 && targetLevelsGained > 0) AddPlayerEvent(playerId, $"Ontogenic Regression: {sourceMutationName} -> {targetMutationName}", GameLogCategory.Lucky); }
        public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset)
        { if (trackPreMutation && IsHuman(playerId)) { if (cellsKilled > 0) preMutationTrackers[playerId].OntogenicSacrificeCells += cellsKilled; if (levelsOffset != 0) preMutationTrackers[playerId].OntogenicSacrificeLevelOffset += levelsOffset; } }
        #endregion

        #region ISimulationObserver (unused stubs)
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
        public void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims) { if (IsHuman(playerId) && successfulReclaims > 0) necrophyticReclaims += successfulReclaims; }
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) { }
        public void RecordBankedPoints(int playerId, int pointsBanked) { }
        public void RecordHyphalSurgeGrowth(int playerId) { }
        public void RecordHyphalVectoringGrowth(int playerId, int cellsPlaced) { }
        public void ReportJettingMyceliumReclaimed(int playerId, int reclaimed) { }
        public void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportJettingMyceliumInvalid(int playerId, int invalid) { }
        public void ReportJettingMyceliumColonized(int playerId, int colonized) { }
        public void ReportJettingMyceliumToxified(int playerId, int toxified) { }
        public void ReportJettingMyceliumPoisoned(int playerId, int poisoned) { }
        public void ReportJettingMyceliumInfested(int playerId, int infested) { }
        public void ReportHyphalVectoringReclaimed(int playerId, int reclaimed) { }
        public void ReportHyphalVectoringCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportHyphalVectoringAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportHyphalVectoringColonized(int playerId, int colonized) { }
        public void ReportHyphalVectoringInvalid(int playerId, int invalid) { }
        public void ReportHyphalVectoringInfested(int playerId, int infested) { }
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
        public void RecordPutrefactiveCascadeToxified(int playerId, int toxified) { }
        public void RecordMimeticResilienceInfestations(int playerId, int infestations) { }
        public void RecordMimeticResilienceDrops(int playerId, int drops) { }
        public void RecordCytolyticBurstToxins(int playerId, int toxinsCreated) { }
        public void RecordCytolyticBurstKills(int playerId, int cellsKilled) { }
        public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected) { }
        #endregion
    }
}
