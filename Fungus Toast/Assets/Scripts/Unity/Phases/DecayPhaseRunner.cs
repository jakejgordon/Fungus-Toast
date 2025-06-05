using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Core.Board;

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

        public void StartDecayPhase(Dictionary<int, int> failedGrowthsByPlayerId)
        {
            StartCoroutine(RunDecayPhase(failedGrowthsByPlayerId));
        }

        private IEnumerator RunDecayPhase(Dictionary<int, int> failedGrowthsByPlayerId)
        {
            Debug.Log("💀 Decay Phase Starting...");

            // Execute deaths
            DeathEngine.ExecuteDeathCycle(board, players, failedGrowthsByPlayerId);

            // Remove expired toxins
            foreach (var tile in board.AllTiles())
            {
                if (tile.FungalCell?.HasToxinExpired(board.CurrentGrowthCycle) == true)
                {
                    tile.FungalCell.ClearToxinState();
                }
            }

            yield return new WaitForSeconds(GameBalance.TimeBeforeDecayRender);

            gridVisualizer.RenderBoard(board);

            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(players);

            yield return new WaitForSeconds(GameBalance.TimeAfterDecayRender);

            GameManager.Instance.OnRoundComplete();
        }

    }
}
