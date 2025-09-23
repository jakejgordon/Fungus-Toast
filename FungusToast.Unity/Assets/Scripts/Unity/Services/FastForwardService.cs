using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Board;
using FungusToast.Core.AI;
using FungusToast.Core.Growth;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
using FungusToast.Core.Phases;

namespace FungusToast.Unity
{
    public static class GameManagerExtensions
    {
        public static MutationManager GetMutationManager(this GameManager gm) => gm.GetPrivateMutationManager();
        public static Player GetPrimaryHuman(this GameManager gm) => gm.GetPrimaryHumanInternal();
        public static System.Random GetRng(this GameManager gm) => gm.GetRngInternal();
        public static MycovariantPoolManager GetPersistentPool(this GameManager gm) => gm.GetPersistentPoolInternal();
        public static void TriggerEndGameFromFastForward(this GameManager gm) => gm.TriggerEndGameInternal();
    }

    public class FastForwardService
    {
        private readonly GameManager gameManager;
        private readonly Func<bool> getFastForwardFlag; private readonly Action<bool> setFastForwardFlag; private readonly Func<bool> gameEndedFunc;
        public FastForwardService(GameManager gm, Func<bool> getter, Action<bool> setter, Func<bool> gameEnded) { gameManager = gm; getFastForwardFlag = getter; setFastForwardFlag = setter; gameEndedFunc = gameEnded; }
        public void StartFastForward(int target, bool skipToEnd, int? testingMycoId) { gameManager.StartCoroutine(FastForwardRoutine(target, skipToEnd, testingMycoId)); }

        private IEnumerator FastForwardRoutine(int fastForwardRounds, bool skipToEnd, int? testingMycoId)
        {
            var board = gameManager.Board; var ui = gameManager.GameUI; var mutationMgr = gameManager.GetMutationManager();
            ui.GameLogRouter.EnableSilentMode(); setFastForwardFlag(true);
            int startingRound = board.CurrentRound; int requestedValue = fastForwardRounds;
            bool treatAsTargetRound = requestedValue > startingRound; int targetRound = treatAsTargetRound ? requestedValue : (startingRound + requestedValue); if (targetRound < startingRound) targetRound = startingRound; int desiredRounds = targetRound - startingRound; int iterations = 0;
            // NEW: Capture and convert ALL human players (previous code only handled primary human)
            var humanPlayers = board.Players.Where(p => p.PlayerType == PlayerTypeEnum.Human).ToList();
            var originalStates = new List<(Player player, PlayerTypeEnum type, IMutationSpendingStrategy strategy)>();
            // A single fallback strategy is sufficient; assign to any human lacking a strategy
            var fallbackStrategy = AIRoster.GetStrategies(1, StrategySetEnum.Proven).FirstOrDefault();
            foreach (var hp in humanPlayers)
            {
                originalStates.Add((hp, hp.PlayerType, hp.MutationStrategy));
                hp.SetPlayerType(PlayerTypeEnum.AI);
                if (hp.MutationStrategy == null && fallbackStrategy != null)
                    hp.SetMutationStrategy(fallbackStrategy);
            }
            try
            {
                while (board.CurrentRound < targetRound && iterations < desiredRounds && !gameEndedFunc())
                {
                    yield return RunSilentGrowthPhase(board);
                    yield return RunSilentDecayPhase(board);
                    yield return RunSilentMutationPhase(board, mutationMgr, ui);
                    foreach (var p in board.Players) p.TickDownActiveSurges(); board.IncrementRound(); iterations++;
                    if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(board.CurrentRound)) RunSilentDraft(board, ui, testingMycoId);
                }
                // Restore original player types and strategies before UI updates
                foreach (var state in originalStates)
                {
                    state.player.SetPlayerType(state.type);
                    state.player.SetMutationStrategy(state.strategy);
                }
                setFastForwardFlag(false);
                gameManager.gridVisualizer.RenderBoard(board, true); ui.RightSidebar?.UpdatePlayerSummaries(board.Players); float occupancy = board.GetOccupiedTileRatio() * 100f; ui.RightSidebar?.SetRoundAndOccupancy(board.CurrentRound, occupancy); ui.MoldProfileRoot?.ApplyDeferredRefreshIfNeeded();
                if (skipToEnd) { ui.GameLogRouter.DisableSilentMode(); gameManager.TriggerEndGameFromFastForward(); yield break; }
                if (testingMycoId.HasValue) gameManager.StartMycovariantDraftPhase(); else { string msg = treatAsTargetRound ? $"Reached Round {board.CurrentRound}" : $"Fast-forwarded {board.CurrentRound - startingRound} rounds"; ui.PhaseBanner.Show(msg, 2f); gameManager.StartNextRound(); }
            }
            finally
            {
                // Defensive restoration in case of exception prior to normal restoration
                foreach (var state in originalStates)
                {
                    if (state.player.PlayerType != state.type) state.player.SetPlayerType(state.type);
                    if (state.player.MutationStrategy != state.strategy) state.player.SetMutationStrategy(state.strategy);
                }
                setFastForwardFlag(false); ui.GameLogRouter.DisableSilentMode();
            }
        }

        private void RunSilentDraft(GameBoard board, GameUIManager ui, int? testingMycoId)
        {
            Func<Player, List<Mycovariant>, Mycovariant> custom = null; var pool = gameManager.GetPersistentPool(); var rng = gameManager.GetRng();
            if (testingMycoId.HasValue) { var testingMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycoId.Value); if (testingMyco != null && !testingMyco.IsUniversal) { pool.TemporarilyRemoveFromPool(testingMycoId.Value); custom = (player, choices) => choices.Where(c => c.Id != testingMycoId.Value).OrderByDescending(m => m.GetBaseAIScore(player, board)).ThenBy(_ => rng.Next()).FirstOrDefault() ?? choices.First(); } }
            MycovariantDraftManager.RunDraft(board.Players, pool, board, rng, ui.GameLogRouter, MycovariantGameBalance.MycovariantSelectionDraftSize, custom);
            if (testingMycoId.HasValue) { var testingMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycoId.Value); if (testingMyco != null && !testingMyco.IsUniversal) pool.RestoreToPool(testingMycoId.Value); }
        }

        private IEnumerator RunSilentMutationPhase(GameBoard board, MutationManager mm, GameUIManager ui)
        {
            var all = mm.AllMutations.Values.ToList();
            TurnEngine.AssignMutationPoints(board, board.Players, all, gameManager.GetRng(), ui.GameLogRouter);
            yield return null;
        }
        private IEnumerator RunSilentGrowthPhase(GameBoard board)
        {
            TurnEngine.RunGrowthPhase(board, board.Players, gameManager.GetRng(), gameManager.GameUI.GameLogRouter);
            yield return null;
        }
        private IEnumerator RunSilentDecayPhase(GameBoard board)
        {
            var empty = new Dictionary<int, int>();
            TurnEngine.RunDecayPhase(board, board.Players, empty, gameManager.GetRng(), gameManager.GameUI.GameLogRouter);
            yield return null;
        }
        public IEnumerator WaitForFadeInAnimationsToComplete(GridVisualizer gv) { if (gv == null) yield break; while (gv.HasActiveAnimations) yield return null; }
    }
}
