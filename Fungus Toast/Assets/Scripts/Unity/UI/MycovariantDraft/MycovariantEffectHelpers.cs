using FungusToast.Core.Board;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.UI.MycovariantDraft
{
    public static class MycovariantEffectHelpers
    {
        /// <summary>
        /// Handles UI/AI, input, and effect resolution for Jetting Mycelium.
        /// </summary>
        public static IEnumerator HandleJettingMycelium(
            Player player,
            Mycovariant picked,
            Action onComplete,
            GameObject draftPanel,
            GridVisualizer gridVisualizer)
        {
            var direction = DirectionFromMycovariantId(picked.Id); // You may want to pass this in, or keep helper internal

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                // AI selects a source cell automatically
                var livingCells = GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(c => c.IsAlive)
                    .ToList();
                FungalCell cell = livingCells.Count > 0
                    ? livingCells[UnityEngine.Random.Range(0, livingCells.Count)]
                    : null;

                if (cell != null)
                {
                    var playerMyco = player.PlayerMycovariants
                        .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                    MycovariantEffectProcessor.ResolveJettingMycelium(
                        playerMyco,
                        player,
                        GameManager.Instance.Board,
                        cell.TileId,
                        direction
                    );
                }
                yield return new WaitForSeconds(0.6f);
                onComplete?.Invoke();
            }
            else
            {
                // Hide draft panel if supplied
                draftPanel?.SetActive(false);
                bool done = false;

                TileSelectionController.Instance.PromptSelectLivingCell(
                    player.PlayerId,
                    (cell) =>
                    {
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                        MycovariantEffectProcessor.ResolveJettingMycelium(
                            playerMyco,
                            player,
                            GameManager.Instance.Board,
                            cell.TileId,
                            direction
                        );
                        gridVisualizer.ClearHighlights();
                        onComplete?.Invoke();
                        done = true;
                    },
                    () =>
                    {
                        gridVisualizer.ClearHighlights();
                        onComplete?.Invoke();
                        done = true;
                    }
                );

                while (!done) yield return null;
            }
        }

        /// <summary>
        /// Returns the correct CardinalDirection for a Jetting Mycelium mycovariant ID.
        /// </summary>
        private static CardinalDirection DirectionFromMycovariantId(int mycovariantId)
        {
            if (mycovariantId == MycovariantIds.JettingMyceliumNorthId) return CardinalDirection.North;
            if (mycovariantId == MycovariantIds.JettingMyceliumEastId) return CardinalDirection.East;
            if (mycovariantId == MycovariantIds.JettingMyceliumSouthId) return CardinalDirection.South;
            if (mycovariantId == MycovariantIds.JettingMyceliumWestId) return CardinalDirection.West;
            throw new ArgumentException("Invalid Jetting Mycelium ID");
        }
    }
}
