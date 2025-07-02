using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
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
                yield return new WaitForSeconds(UIEffectConstants.JettingMyceliumAIDelaySeconds);
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
                    },
                    "Select one of your living fungal cells to project mycelium from."
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

        /// <summary>
        /// Handles UI/AI, input, and effect resolution for Mycelial Bastion.
        /// </summary>
        public static IEnumerator HandleMycelialBastion(
            Player player,
            Mycovariant picked,
            Action onComplete,
            GameObject draftPanel,
            GridVisualizer gridVisualizer)
        {
            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                // AI selects cells automatically
                var playerMyco = player.PlayerMycovariants
                    .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                MycovariantEffectProcessor.ResolveMycelialBastion(
                    playerMyco,
                    GameManager.Instance.Board,
                    new System.Random(),
                    null
                );
                
                yield return new WaitForSeconds(UIEffectConstants.MycelialBastionAIDelaySeconds);
                onComplete?.Invoke();
            }
            else
            {
                // Hide draft panel if supplied
                draftPanel?.SetActive(false);

                // Show selection prompt banner
                GameManager.Instance.ShowSelectionPrompt(
                    $"Select up to {MycovariantGameBalance.MycelialBastionMaxResistantCells} of your living cells to become Resistant."
                );

                // Use a multi-selection controller for Mycelial Bastion
                MultiCellSelectionController.Instance.PromptSelectMultipleLivingCells(
                    player.PlayerId,
                    MycovariantGameBalance.MycelialBastionMaxResistantCells,
                    (selectedCells) =>
                    {
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                        // Apply the effect to the selected cells
                        foreach (var cell in selectedCells)
                        {
                            cell.MakeResistant();
                            playerMyco?.IncrementEffectCount(MycovariantEffectType.Bastioned, 1);
                        }

                        GameManager.Instance.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        gridVisualizer.RenderBoard(GameManager.Instance.Board);
                        onComplete?.Invoke();
                    },
                    () =>
                    {
                        GameManager.Instance.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        onComplete?.Invoke();
                    },
                    $"Select up to {MycovariantGameBalance.MycelialBastionMaxResistantCells} of your living cells to make Resistant (invincible)."
                );
            }
        }

        /// <summary>
        /// Handles UI/AI, input, and effect resolution for Surgical Inoculation.
        /// </summary>
        public static IEnumerator HandleSurgicalInoculation(
            Player player,
            Mycovariant picked,
            Action onComplete,
            GameObject draftPanel,
            GridVisualizer gridVisualizer)
        {
            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                // AI logic is handled in the effect processor
                yield return new WaitForSeconds(UIEffectConstants.SurgicalInoculationAIDelaySeconds);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);
                GameManager.Instance.ShowSelectionPrompt(
                    "Select any tile to place your invincible (Resistant) cell.");

                bool done = false;

                // Highlight all valid tiles (not already Resistant)
                Func<BoardTile, bool> isValidTile = tile => tile.FungalCell == null || (!tile.FungalCell.IsResistant);
                var validTileIds = GameManager.Instance.Board.AllTiles()
                    .Where(isValidTile)
                    .Select(tile => tile.TileId)
                    .ToList();
                gridVisualizer.HighlightTiles(validTileIds);

                TileSelectionController.Instance.PromptSelectBoardTile(
                    isValidTile,
                    (tile) =>
                    {
                        if (done) return; // Defensive: prevent double-callback
                        done = true;
                        // Defensive: re-check tile validity
                        if (!isValidTile(tile))
                        {
                            Debug.LogWarning("Selected tile is no longer valid for Surgical Inoculation.");
                            GameManager.Instance.HideSelectionPrompt();
                            gridVisualizer.ClearHighlights();
                            onComplete?.Invoke();
                            return;
                        }
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                        MycovariantEffectProcessor.ResolveSurgicalInoculation(
                            playerMyco,
                            GameManager.Instance.Board,
                            new System.Random(),
                            null
                        );
                        GameManager.Instance.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        gridVisualizer.RenderBoard(GameManager.Instance.Board);
                        onComplete?.Invoke();
                    },
                    () =>
                    {
                        if (done) return; // Defensive: prevent double-callback
                        done = true;
                        GameManager.Instance.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        onComplete?.Invoke();
                    },
                    "Select any valid tile to place your invincible (Resistant) cell."
                );

                while (!done) yield return null;
            }
        }
    }
}
