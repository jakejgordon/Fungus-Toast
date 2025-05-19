using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Phases;
using FungusToast.Unity.Grid;
using FungusToast.Core;
using FungusToast.Core.Config;
using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using System.Linq;

namespace FungusToast.Unity.Phases
{
    public class GrowthPhaseRunner : MonoBehaviour
    {
        private GrowthPhaseProcessor processor;
        private GameBoard board;
        private List<Player> players;
        private GridVisualizer gridVisualizer;
        private bool isRunning = false;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.players = players;
            this.gridVisualizer = gridVisualizer;
            this.processor = new GrowthPhaseProcessor(board, players);
        }

        public void StartGrowthPhase()
        {
            if (!isRunning)
            {
                StartCoroutine(RunFullGrowthPhase());
            }
        }

        private IEnumerator RunFullGrowthPhase()
        {
            isRunning = true;
            GameManager.Instance.SetGamePhaseText("Growth Phase (Cycle 1/" + GameBalance.TotalGrowthCycles + ")");
            Debug.Log("🌱 Growth Phase Starting...");

            for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
            {
                Debug.Log($"🌿 Growth Cycle {cycle + 1}/{GameBalance.TotalGrowthCycles}");

                processor.ExecuteSingleCycle();
                ApplyRegenerativeHyphaeReclaims();
                gridVisualizer.RenderBoard(board);

                GameManager.Instance.SetGamePhaseText($"Growth Phase (Cycle {cycle + 1}/{GameBalance.TotalGrowthCycles})");

                yield return new WaitForSeconds(GameBalance.TimeBetweenGrowthCycles);
            }

            Debug.Log("🌾 Growth Cycles complete. Preparing for decay phase...");
            isRunning = false;

            GameManager.Instance.StartDecayPhase();
        }

        private void ApplyRegenerativeHyphaeReclaims()
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.RegenerativeHyphae);
                if (level <= 0)
                    continue;

                float reclaimChance = GameBalance.RegenerativeHyphaeReclaimChance * level;
                var playerCells = board.GetAllCellsOwnedBy(player.PlayerId);

                foreach (var cell in playerCells)
                {
                    var (x, y) = board.GetXYFromTileId(cell.TileId);
                    var neighbors = board.GetOrthogonalNeighbors(x, y);

                    foreach (var neighbor in neighbors)
                    {
                        var deadCell = neighbor.FungalCell;
                        if (deadCell == null || deadCell.IsAlive)
                            continue;

                        if (deadCell.OriginalOwnerPlayerId != player.PlayerId)
                            continue;

                        if (Random.value < reclaimChance)
                        {
                            deadCell.Reclaim(player.PlayerId);
                            board.RegisterCell(deadCell); // ✅ Make sure dictionary gets updated
                        }
                    }
                }
            }
        }


    }
}
