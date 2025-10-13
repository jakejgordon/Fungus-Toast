using System;
using System.Collections.Generic;
using FungusToast.Core.Players;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.Hotseat;
using UnityEngine;

namespace FungusToast.Unity
{
    public class HotseatTurnManager
    {
        private readonly GameUIManager ui;
        private readonly UI_HotseatTurnPrompt prompt;
        private readonly List<Player> humanPlayers;
        private readonly Func<Player> getPrimaryHuman;
        private readonly Func<bool> isFastForwarding;
        private readonly Func<bool> isTesting;
        private readonly Action onAllHumansFinished;

        private int currentIndex = -1;
        private bool active = false;

        public HotseatTurnManager(GameUIManager ui, UI_HotseatTurnPrompt prompt, List<Player> humanPlayers, Func<Player> getPrimaryHuman, Func<bool> fastForwarding, Func<bool> isTesting, Action onAllFinished)
        {
            this.ui = ui; this.prompt = prompt; this.humanPlayers = humanPlayers; this.getPrimaryHuman = getPrimaryHuman; this.isFastForwarding = fastForwarding; this.isTesting = isTesting; this.onAllHumansFinished = onAllFinished;
        }

        public void BeginHumanMutationPhase()
        {
            if (humanPlayers.Count == 0)
            {
                var primary = getPrimaryHuman();
                if (primary != null) humanPlayers.Add(primary);
            }
            if (humanPlayers.Count == 0)
            {
                Debug.LogError("[HotseatTurnManager] No human players available to start mutation phase.");
                onAllHumansFinished?.Invoke();
                return;
            }
            currentIndex = 0;
            active = true;
            Debug.Log($"[HotseatTurnManager] Starting mutation phase for {humanPlayers.Count} human players.");
            StartTurn();
        }

        private void StartTurn()
        {
            if (currentIndex < 0 || currentIndex >= humanPlayers.Count)
            {
                Debug.LogError($"[HotseatTurnManager] StartTurn index {currentIndex} out of range.");
                onAllHumansFinished?.Invoke();
                return;
            }
            var hp = humanPlayers[currentIndex];
            // Show prompt only if more than one human player
            bool showPrompt = humanPlayers.Count > 1 && prompt != null && !isFastForwarding() && !isTesting();
            Debug.Log($"[HotseatTurnManager] Human turn index={currentIndex} player={hp.PlayerName} showPrompt={showPrompt} promptAssigned={(prompt!=null)}");
            if (showPrompt)
            {
                PreSwitchPlayerUI(hp);
                try { prompt.Show(hp, () => InitializeUI(hp)); }
                catch (Exception ex) { Debug.LogWarning($"[HotseatTurnManager] Prompt Show failed: {ex.Message}. Falling back to direct init."); InitializeUI(hp); }
            }
            else { PreSwitchPlayerUI(hp); InitializeUI(hp); }
        }

        private void PreSwitchPlayerUI(Player hp)
        {
            // Update mold profile root immediately
            if (GameManager.Instance?.GameUI?.MoldProfileRoot != null)
            {
                GameManager.Instance.GameUI.MoldProfileRoot.SwitchPlayer(hp, GameManager.Instance.Board.Players);
            }
            // Pre-load mutation manager with new player (keep panel closed, don't start animations)
            ui.MutationUIManager.ReinitializeForPlayer(hp, keepPanelClosed: true);
        }

        private void InitializeUI(Player hp)
        {
            Debug.Log($"[HotseatTurnManager] Initializing mutation UI for {hp.PlayerName}");
            Debug.Log($"[HotseatTurnManager] Calling ReinitializeForPlayer for {hp.PlayerName}");
            ui.MutationUIManager.StartNewMutationPhase();
            ui.MutationUIManager.ReinitializeForPlayer(hp); // new lightweight swap
            Debug.Log($"[HotseatTurnManager] Returned from ReinitializeForPlayer for {hp.PlayerName}");
            ui.MutationUIManager.SetSpendPointsButtonVisible(true);
            ui.MutationUIManager.RefreshSpendPointsButtonUI();
            // Update mold profile root to new player
            if (GameManager.Instance != null && GameManager.Instance.GameUI != null && GameManager.Instance.GameUI.MoldProfileRoot != null)
            {
                GameManager.Instance.GameUI.MoldProfileRoot.SwitchPlayer(hp, GameManager.Instance.Board.Players);
            }
            ui.MutationUIManager.SetSpendPointsButtonInteractable(true);
            GameManager.Instance?.SetActiveHumanPlayer(hp);
        }

        public void HandleHumanTurnFinished(Player finished)
        {
            if (!active)
            {
                Debug.LogWarning("[HotseatTurnManager] HandleHumanTurnFinished called while inactive.");
                return;
            }
            Debug.Log($"[HotseatTurnManager] Human turn finished: {finished.PlayerName}");
            if (currentIndex < humanPlayers.Count - 1)
            {
                currentIndex++;
                StartTurn();
            }
            else
            {
                Debug.Log("[HotseatTurnManager] All human turns complete.");
                active = false;
                onAllHumansFinished?.Invoke();
            }
        }
    }
}
