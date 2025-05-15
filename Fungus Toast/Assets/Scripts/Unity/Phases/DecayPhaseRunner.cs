using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;

namespace FungusToast.Unity.Phases
{
    public class DecayPhaseRunner : MonoBehaviour
    {
        private GameBoard board;
        private List<Player> players;
        private GridVisualizer gridVisualizer;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.players = players;
            this.gridVisualizer = gridVisualizer;
        }

        public void StartDecayPhase()
        {
            StartCoroutine(RunDecayPhase());
        }

        private IEnumerator RunDecayPhase()
        {
            GameManager.Instance.SetGamePhaseText("Decay Phase");
            Debug.Log("💀 Decay Phase Starting...");

            // Execute deaths
            DeathEngine.ExecuteDeathCycle(board, players);

            // Optional: Wait before rendering deaths for dramatic pause
            yield return new WaitForSeconds(GameBalance.TimeBeforeDecayRender);

            // Render decay results
            gridVisualizer.RenderBoard(board);

            // Update UI (player summaries, etc.)
            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(players);

            // Optional: Additional delay
            yield return new WaitForSeconds(GameBalance.TimeAfterDecayRender);

            // Let GameManager continue
            GameManager.Instance.OnDecayPhaseComplete();
        }
    }
}
