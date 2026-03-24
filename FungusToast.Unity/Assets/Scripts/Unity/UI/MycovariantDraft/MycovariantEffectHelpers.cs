using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
using System;
using System.Collections;
using System.Collections.Generic;
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
            GridVisualizer gridVisualizer,
            GameManager gameManager)
        {
            if (gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var direction = DirectionFromMycovariantId(picked.Id);
            var board = gameManager.Board;
            var gameLogRouter = gameManager.GameUI.GameLogRouter;

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                draftPanel?.SetActive(false);
                // Pre-animation stagger
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);

                var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(c => c.IsAlive)
                    .ToList();
                
                if (livingCells.Count > 0)
                {
                    var sourceCell = livingCells[UnityEngine.Random.Range(0, livingCells.Count)];
                    var playerMyco = player.PlayerMycovariants
                        .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                    if (playerMyco != null)
                    {
                        MycovariantEffectProcessor.ResolveJettingMycelium(
                            playerMyco,
                            player,
                            board,
                            sourceCell.TileId,
                            direction,
                            new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                            gameLogRouter
                        );
                        gridVisualizer.RenderBoard(board);
                        yield return gridVisualizer.WaitForAllAnimations();
                    }
                }
                // Post-animation stagger
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);

                var validCells = board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(c => c.IsAlive)
                    .ToList();
                var validTileIds = validCells.Select(c => c.TileId).ToList();

                gridVisualizer.HighlightTiles(
                    validTileIds,
                    new Color(1f, 0.2f, 0.8f, 1f),
                    new Color(1f, 0.7f, 1f, 1f)
                );
                gameManager.ShowSelectionPrompt("Select one of your living fungal cells to project mycelium from.");

                bool done = false;
                bool selectionResolved = false;
                bool executed = false;

                TileSelectionController.Instance.SetHoverPreviewCallback((tileId) =>
                {
                    if (tileId < 0)
                    {
                        gridVisualizer.ClearJettingMyceliumPreview();
                    }
                    else
                    {
                        var livingLine = board.GetTileLine(tileId, direction, MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles, false);
                        var toxinCone  = board.GetTileCone(tileId, direction);
                        gridVisualizer.ShowJettingMyceliumPreview(livingLine, toxinCone);
                    }
                });

                TileSelectionController.Instance.PromptSelectLivingCell(
                    player.PlayerId,
                    (cell) =>
                    {
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                        MycovariantEffectProcessor.ResolveJettingMycelium(
                            playerMyco,
                            player,
                            board,
                            cell.TileId,
                            direction,
                            new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                            gameLogRouter
                        );
                        gridVisualizer.ClearJettingMyceliumPreview();
                        gridVisualizer.RenderBoard(board);
                        gridVisualizer.ClearHighlights();
                        gameManager.HideSelectionPrompt();
                        done = true;
                        executed = true;
                        selectionResolved = true;
                    },
                    () =>
                    {
                        gridVisualizer.ClearJettingMyceliumPreview();
                        gridVisualizer.ClearHighlights();
                        gameManager.HideSelectionPrompt();
                        done = true;
                        selectionResolved = true;
                    },
                    "Select one of your living fungal cells to project mycelium from."
                );

                while (!done) yield return null;
                while (!selectionResolved) yield return null;

                if (executed)
                {
                    yield return gridVisualizer.WaitForAllAnimations();
                }

                onComplete?.Invoke();
            }
        }

        public static IEnumerator HandleHyphalDraw(
            Player player,
            Mycovariant picked,
            Action onComplete,
            GameObject draftPanel,
            GridVisualizer gridVisualizer,
            GameManager gameManager)
        {
            if (gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var board = gameManager.Board;
            var gameLogRouter = gameManager.GameUI.GameLogRouter;
            var playerMyco = player.PlayerMycovariants
                .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

            if (playerMyco == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            draftPanel?.SetActive(false);

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
            }

            var resolution = MycovariantEffectProcessor.ResolveHyphalDraw(
                playerMyco,
                board,
                new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                gameLogRouter);

            gridVisualizer.RenderBoard(board);

            if (resolution?.HasAnyMovement == true)
            {
                var movePairs = resolution.Moves
                    .Select(move => (move.SourceTileId, move.DestinationTileId))
                    .ToList();

                yield return gridVisualizer.PlayHyphalDrawAnimation(player.PlayerId, movePairs);
                yield return gridVisualizer.WaitForAllAnimations();
            }

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
            }

            onComplete?.Invoke();
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
            GridVisualizer gridVisualizer,
            GameManager gameManager)
        {
            if (gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var board = gameManager.Board;
            var gameLogRouter = gameManager.GameUI.GameLogRouter;

            int maxCellsAllowed = picked.Id switch
            {
                MycovariantIds.MycelialBastionIId => MycovariantGameBalance.MycelialBastionIMaxResistantCells,
                MycovariantIds.MycelialBastionIIId => MycovariantGameBalance.MycelialBastionIIMaxResistantCells,
                MycovariantIds.MycelialBastionIIIId => MycovariantGameBalance.MycelialBastionIIIMaxResistantCells,
                _ => MycovariantGameBalance.MycelialBastionIMaxResistantCells
            };

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                draftPanel?.SetActive(false);
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);

                var playerMyco = player.PlayerMycovariants
                    .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                if (playerMyco != null)
                {
                    var preResistant = board.AllTiles()
                        .Where(t => t.FungalCell != null && t.FungalCell.IsResistant)
                        .Select(t => t.TileId)
                        .ToHashSet();

                    MycovariantEffectProcessor.ResolveMycelialBastion(
                        playerMyco,
                        board,
                        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                        gameLogRouter
                    );

                    gridVisualizer.RenderBoard(board);

                    var newResistant = board.AllTiles()
                        .Where(t => t.FungalCell != null && t.FungalCell.IsResistant && !preResistant.Contains(t.TileId))
                        .Select(t => t.TileId)
                        .ToList();

                    // Start all pulses in parallel
                    foreach (var tileId in newResistant)
                    {
                        gameManager.StartCoroutine(gridVisualizer.BastionResistantPulseAnimation(tileId));
                    }

                    yield return gridVisualizer.WaitForAllAnimations();
                }
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);
                // Show selection prompt banner
                gameManager.ShowSelectionPrompt(
                    $"Select up to {maxCellsAllowed} of your living cells to become Resistant."
                );

                bool selectionResolved = false;
                bool executed = false;
                List<int> selectedTileIds = new List<int>();

                // Use a multi-selection controller for Mycelial Bastion
                MultiCellSelectionController.Instance.PromptSelectMultipleLivingCells(
                    player.PlayerId,
                    maxCellsAllowed,
                    (selectedCells) =>
                    {
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                        selectedTileIds.Clear();
                        foreach (var cell in selectedCells)
                        {
                            selectedTileIds.Add(cell.TileId);
                            cell.MakeResistant("Mycelial Bastion");
                            MycovariantEffectProcessor.OnResistantCellPlaced(
                                board,
                                player.PlayerId,
                                cell.TileId,
                                gameLogRouter
                            );
                            playerMyco?.IncrementEffectCount(MycovariantEffectType.Bastioned, 1);
                        }

                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        gridVisualizer.RenderBoard(board);
                        executed = true;
                        selectionResolved = true;
                    },
                    () =>
                    {
                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        selectionResolved = true;
                    },
                    $"Select up to {maxCellsAllowed} of your living cells to make Resistant (invincible)."
                );

                while (!selectionResolved) yield return null;

                if (executed && selectedTileIds.Count > 0)
                {
                    // Start all pulses in parallel
                    foreach (var tileId in selectedTileIds)
                    {
                        gameManager.StartCoroutine(gridVisualizer.BastionResistantPulseAnimation(tileId));
                    }
                    yield return gridVisualizer.WaitForAllAnimations();
                }

                onComplete?.Invoke();
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
            GridVisualizer gridVisualizer,
            GameManager gameManager)
        {
            if (gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var board = gameManager.Board;
            var gameLogRouter = gameManager.GameUI.GameLogRouter;

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                draftPanel?.SetActive(false);
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                var playerMyco = player.PlayerMycovariants
                    .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                int placedTileId = -1;
                if (playerMyco != null)
                {
                    // Capture pre-state to detect placement
                    var preTiles = board.AllTiles()
                        .Where(t => t.FungalCell != null && t.FungalCell.IsResistant)
                        .Select(t => t.TileId)
                        .ToHashSet();

                    MycovariantEffectProcessor.ResolveSurgicalInoculationAI(
                        playerMyco,
                        board,
                        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                        gameLogRouter
                    );
                    gridVisualizer.RenderBoard(board);

                    // Detect new resistant placement
                    var postTile = board.AllTiles()
                        .FirstOrDefault(t => t.FungalCell != null && t.FungalCell.IsResistant && !preTiles.Contains(t.TileId));
                    if (postTile != null)
                        placedTileId = postTile.TileId;

                    if (placedTileId >= 0)
                    {
                        // Use arc from player's start if available
                        var shieldSprite = gridVisualizer.goldShieldOverlayTile != null ? gridVisualizer.goldShieldOverlayTile.sprite : null;
                        if (player.StartingTileId.HasValue && shieldSprite != null)
                            yield return gridVisualizer.SurgicalInoculationArcAnimation(player.PlayerId, placedTileId, shieldSprite);
                        else
                            yield return gridVisualizer.ResistantDropAnimation(
                                placedTileId,
                                durationScale: UIEffectConstants.SurgicalInoculationDropDurationScale);
                    }
                    yield return gridVisualizer.WaitForAllAnimations();
                }
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);
                gameManager.ShowSelectionPrompt(
                    "Select any tile to place your invincible (Resistant) cell.");

                bool done = false;
                bool selectionResolved = false;
                bool executed = false;

                Func<BoardTile, bool> isValidTile = tile => tile.FungalCell == null || (!tile.FungalCell.IsResistant);
                var validTileIds = board.AllTiles()
                    .Where(isValidTile)
                    .Select(tile => tile.TileId)
                    .ToList();
                gridVisualizer.HighlightTiles(validTileIds);

                int placedTileId = -1;
                TileSelectionController.Instance.PromptSelectBoardTile(
                    isValidTile,
                    (tile) =>
                    {
                        if (done) return;
                        done = true;
                        if (!isValidTile(tile))
                        {
                            gameManager.HideSelectionPrompt();
                            gridVisualizer.ClearHighlights();
                            selectionResolved = true;
                            return;
                        }
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                        placedTileId = tile.TileId;
                        MycovariantEffectProcessor.ResolveSurgicalInoculationHuman(
                            playerMyco,
                            board,
                            player.PlayerId,
                            placedTileId,
                            gameLogRouter
                        );
                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        gridVisualizer.RenderBoard(board);
                        executed = true;
                        selectionResolved = true;
                    },
                    () =>
                    {
                        if (done) return;
                        done = true;
                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        selectionResolved = true;
                    },
                    "Select any valid tile to place your invincible (Resistant) cell."
                );
                while (!done) yield return null;
                while (!selectionResolved) yield return null;
                if (executed && placedTileId >= 0)
                {
                    var shieldSprite = gridVisualizer.goldShieldOverlayTile != null ? gridVisualizer.goldShieldOverlayTile.sprite : null;
                    if (player.StartingTileId.HasValue && shieldSprite != null)
                        yield return gridVisualizer.SurgicalInoculationArcAnimation(player.PlayerId, placedTileId, shieldSprite);
                    else
                        yield return gridVisualizer.ResistantDropAnimation(
                            placedTileId,
                            durationScale: UIEffectConstants.SurgicalInoculationDropDurationScale);
                    yield return gridVisualizer.WaitForAllAnimations();
                }
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Handles UI/AI, input, and effect resolution for Ballistospore Discharge.
        /// </summary>
        public static IEnumerator HandleBallistosporeDischarge(
            Player player,
            Mycovariant picked,
            Action onComplete,
            GameObject draftPanel,
            GridVisualizer gridVisualizer,
            GameManager gameManager)
        {
            if (gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var board = gameManager.Board;
            var gameLogRouter = gameManager.GameUI.GameLogRouter;

            int maxSporesAllowed = picked.Id switch
            {
                MycovariantIds.BallistosporeDischargeIId => MycovariantGameBalance.BallistosporeDischargeISpores,
                MycovariantIds.BallistosporeDischargeIIId => MycovariantGameBalance.BallistosporeDischargeIISpores,
                MycovariantIds.BallistosporeDischargeIIIId => MycovariantGameBalance.BallistosporeDischargeIIISpores,
                _ => MycovariantGameBalance.BallistosporeDischargeISpores
            };

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                draftPanel?.SetActive(false);
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                var playerMyco = player.PlayerMycovariants.FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                if (playerMyco != null)
                {
                    BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
                        playerMyco,
                        board,
                        maxSporesAllowed,
                        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                        gameLogRouter
                    );
                    gridVisualizer.RenderBoard(board);
                    yield return gridVisualizer.WaitForAllAnimations();
                }
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);
                Func<BoardTile, bool> isValidTile = tile => tile.FungalCell == null;
                var validTileIds = board.AllTiles()
                    .Where(isValidTile)
                    .Select(tile => tile.TileId)
                    .ToList();
                int emptyTileCount = validTileIds.Count;
                int maxSpores = Math.Min(emptyTileCount, maxSporesAllowed);
                gameManager.ShowSelectionPrompt(
                    $"Select up to {maxSpores} empty tiles to drop toxin spores."
                );

                bool done = false;
                bool selectionResolved = false;
                bool executed = false;
                var playerMyco = player.PlayerMycovariants.FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                MultiTileSelectionController.Instance.PromptSelectMultipleTiles(
                    isValidTile,
                    maxSpores,
                    (selectedTiles) =>
                    {
                        foreach (var tile in selectedTiles)
                        {
                            BallistosporeDischargeHelper.ResolveBallistosporeDischargeHuman(
                                playerMyco,
                                board,
                                tile.TileId,
                                gameLogRouter
                            );
                        }
                        gridVisualizer.RenderBoard(board);
                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        done = true;
                        executed = true;
                        selectionResolved = true;
                    },
                    () =>
                    {
                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        done = true;
                        selectionResolved = true;
                    },
                    $"Select up to {maxSpores} empty tiles to drop toxin spores."
                );

                while (!done) yield return null;
                while (!selectionResolved) yield return null;

                if (executed)
                {
                    yield return gridVisualizer.WaitForAllAnimations();
                }

                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Handles UI/AI, input, and effect resolution for Cytolytic Burst.
        /// </summary>
        public static IEnumerator HandleCytolyticBurst(
            Player player,
            Mycovariant picked,
            Action onComplete,
            GameObject draftPanel,
            GridVisualizer gridVisualizer,
            GameManager gameManager)
        {
            if (gameManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var board = gameManager.Board;
            var gameLogRouter = gameManager.GameUI.GameLogRouter;

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                draftPanel?.SetActive(false);
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                var playerMyco = player.PlayerMycovariants
                    .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                if (playerMyco != null)
                {
                    var bestToxin = CytolyticBurstHelper.FindBestToxinToExplode(player, board);
                    
                    if (bestToxin.HasValue)
                    {
                        MycovariantEffectProcessor.ResolveCytolyticBurst(
                            playerMyco,
                            board,
                            bestToxin.Value.tileId,
                            new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                            gameLogRouter
                        );
                        gridVisualizer.RenderBoard(board);
                        yield return gridVisualizer.WaitForAllAnimations();
                    }
                }
                yield return new WaitForSeconds(UIEffectConstants.AIActiveMycovariantStaggerSeconds);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);
                
                var playerToxins = board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(c => c.IsToxin)
                    .ToList();
                
                if (playerToxins.Count == 0)
                {
                    gameManager.ShowSelectionPrompt("No toxins available to explode!");
                    yield return new WaitForSeconds(2f);
                    gameManager.HideSelectionPrompt();
                    onComplete?.Invoke();
                    yield break;
                }

                gameManager.ShowSelectionPrompt(
                    $"Select one of your toxins to explode in a {MycovariantGameBalance.CytolyticBurstRadius}-tile radius."
                );

                bool done = false;
                bool selectionResolved = false;
                bool executed = false;

                var toxinTileIds = playerToxins.Select(c => c.TileId).ToList();
                gridVisualizer.HighlightTiles(toxinTileIds);

                Func<BoardTile, bool> isValidToxin = tile => 
                    tile.FungalCell != null && 
                    tile.FungalCell.IsToxin && 
                    tile.FungalCell.OwnerPlayerId == player.PlayerId;

                TileSelectionController.Instance.PromptSelectBoardTile(
                    isValidToxin,
                    (tile) =>
                    {
                        if (done) return;
                        done = true;
                        
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                        if (playerMyco != null)
                        {
                            MycovariantEffectProcessor.ResolveCytolyticBurst(
                                playerMyco,
                                board,
                                tile.TileId,
                                new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                                gameLogRouter
                            );
                        }

                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        gridVisualizer.RenderBoard(board);
                        executed = true;
                        selectionResolved = true;
                    },
                    () =>
                    {
                        if (done) return;
                        done = true;
                        gameManager.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        selectionResolved = true;
                    },
                    "Select one of your toxins to explode."
                );

                while (!done) yield return null;
                while (!selectionResolved) yield return null;

                if (executed)
                {
                    yield return gridVisualizer.WaitForAllAnimations();
                }

                onComplete?.Invoke();
            }
        }
    }
}
