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
            var direction = DirectionFromMycovariantId(picked.Id);

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] AI player {player.PlayerId} executing Jetting Mycelium {direction}");
                
                // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
                var livingCells = GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId)
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
                            GameManager.Instance.Board,
                            sourceCell.TileId,
                            direction,
                            new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                            null
                        );
                        
                        FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] AI Jetting Mycelium effect completed for player {player.PlayerId}");
                    }
                    else
                    {
                        FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] WARNING: PlayerMycovariant not found for AI player {player.PlayerId}");
                    }
                }
                else
                {
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] WARNING: No living cells found for AI player {player.PlayerId}");
                }
                
                // Wait for the effect to visually complete
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
                            direction,
                            new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                            null
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
            // Determine max cells based on which Mycelial Bastion tier this is
            int maxCellsAllowed = picked.Id switch
            {
                MycovariantIds.MycelialBastionIId => MycovariantGameBalance.MycelialBastionIMaxResistantCells,
                MycovariantIds.MycelialBastionIIId => MycovariantGameBalance.MycelialBastionIIMaxResistantCells,
                MycovariantIds.MycelialBastionIIIId => MycovariantGameBalance.MycelialBastionIIIMaxResistantCells,
                _ => MycovariantGameBalance.MycelialBastionIMaxResistantCells // fallback
            };

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] AI player {player.PlayerId} executing Mycelial Bastion with max {maxCellsAllowed} cells");
                
                // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
                var playerMyco = player.PlayerMycovariants
                    .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                if (playerMyco != null)
                {
                    MycovariantEffectProcessor.ResolveMycelialBastion(
                        playerMyco,
                        GameManager.Instance.Board,
                        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                        null
                    );
                    
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] AI Mycelial Bastion effect completed for player {player.PlayerId}");
                }
                else
                {
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] WARNING: PlayerMycovariant not found for AI player {player.PlayerId}");
                }
                
                // Wait for the effect to visually complete
                yield return new WaitForSeconds(UIEffectConstants.MycelialBastionAIDelaySeconds);
                onComplete?.Invoke();
            }
            else
            {
                // Hide draft panel if supplied
                draftPanel?.SetActive(false);

                // Show selection prompt banner
                GameManager.Instance.ShowSelectionPrompt(
                    $"Select up to {maxCellsAllowed} of your living cells to become Resistant."
                );

                // Use a multi-selection controller for Mycelial Bastion
                MultiCellSelectionController.Instance.PromptSelectMultipleLivingCells(
                    player.PlayerId,
                    maxCellsAllowed,
                    (selectedCells) =>
                    {
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                        // Apply the effect to the selected cells
                        foreach (var cell in selectedCells)
                        {
                            cell.MakeResistant();
                            
                            // Check for Hyphal Resistance Transfer effect
                            MycovariantEffectProcessor.OnResistantCellPlaced(
                                GameManager.Instance.Board,
                                player.PlayerId,
                                cell.TileId,
                                null
                            );
                            
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
                    $"Select up to {maxCellsAllowed} of your living cells to make Resistant (invincible)."
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
                // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
                var playerMyco = player.PlayerMycovariants
                    .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                if (playerMyco != null)
                {
                    MycovariantEffectProcessor.ResolveSurgicalInoculationAI(
                        playerMyco,
                        GameManager.Instance.Board,
                        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                        null
                    );
                }
                
                // Wait for the effect to visually complete
                yield return new WaitForSeconds(UIEffectConstants.DefaultAIThinkingDelay);
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
                        MycovariantEffectProcessor.ResolveSurgicalInoculationHuman(
                            playerMyco,
                            GameManager.Instance.Board,
                            player.PlayerId,
                            tile.TileId,
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

        /// <summary>
        /// Handles UI/AI, input, and effect resolution for Ballistospore Discharge (human player).
        /// </summary>
        public static IEnumerator HandleBallistosporeDischarge(
            Player player,
            Mycovariant picked,
            Action onComplete,
            GameObject draftPanel,
            GridVisualizer gridVisualizer)
        {
            // Calculate max spores allowed for this mycovariant tier
            int maxSporesAllowed = picked.Id switch
            {
                MycovariantIds.BallistosporeDischargeIId => MycovariantGameBalance.BallistosporeDischargeISpores,
                MycovariantIds.BallistosporeDischargeIIId => MycovariantGameBalance.BallistosporeDischargeIISpores,
                MycovariantIds.BallistosporeDischargeIIIId => MycovariantGameBalance.BallistosporeDischargeIIISpores,
                _ => MycovariantGameBalance.BallistosporeDischargeISpores
            };

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
                var playerMyco = player.PlayerMycovariants.FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                if (playerMyco != null)
                {
                    BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
                        playerMyco,
                        GameManager.Instance.Board,
                        maxSporesAllowed,
                        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                        null
                    );
                }
                
                // Wait for the effect to visually complete
                yield return new WaitForSeconds(UIEffectConstants.DefaultAIThinkingDelay);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);
                // Get all valid empty tiles
                Func<BoardTile, bool> isValidTile = tile => tile.FungalCell == null;
                var validTileIds = GameManager.Instance.Board.AllTiles()
                    .Where(isValidTile)
                    .Select(tile => tile.TileId)
                    .ToList();
                int emptyTileCount = validTileIds.Count;
                int maxSpores = Math.Min(emptyTileCount, maxSporesAllowed);
                GameManager.Instance.ShowSelectionPrompt(
                    $"Select up to {maxSpores} empty tiles to drop toxin spores."
                );

                bool done = false;
                var playerMyco = player.PlayerMycovariants.FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                MultiTileSelectionController.Instance.PromptSelectMultipleTiles(
                    isValidTile,
                    maxSpores,
                    (selectedTiles) =>
                    {
                        // Now place real toxins on all selected tiles
                        foreach (var tile in selectedTiles)
                        {
                            BallistosporeDischargeHelper.ResolveBallistosporeDischargeHuman(
                                playerMyco,
                                GameManager.Instance.Board,
                                tile.TileId,
                                null
                            );
                        }
                        gridVisualizer.RenderBoard(GameManager.Instance.Board);
                        GameManager.Instance.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        onComplete?.Invoke();
                        done = true;
                    },
                    () =>
                    {
                        GameManager.Instance.HideSelectionPrompt();
                        gridVisualizer.ClearHighlights();
                        onComplete?.Invoke();
                        done = true;
                    },
                    $"Select up to {maxSpores} empty tiles to drop toxin spores."
                );

                while (!done) yield return null;
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
            GridVisualizer gridVisualizer)
        {
            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                // AI: Execute the effect directly since Unity drafts don't call ApplyEffect
                var playerMyco = player.PlayerMycovariants
                    .FirstOrDefault(pm => pm.MycovariantId == picked.Id);
                
                if (playerMyco != null)
                {
                    // Use helper to find best toxin to explode
                    var bestToxin = CytolyticBurstHelper.FindBestToxinToExplode(player, GameManager.Instance.Board);
                    
                    if (bestToxin.HasValue)
                    {
                        MycovariantEffectProcessor.ResolveCytolyticBurst(
                            playerMyco,
                            GameManager.Instance.Board,
                            bestToxin.Value.tileId,
                            new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                            null
                        );
                    }
                }
                
                // Wait for the effect to visually complete
                yield return new WaitForSeconds(UIEffectConstants.DefaultAIThinkingDelay);
                onComplete?.Invoke();
            }
            else
            {
                draftPanel?.SetActive(false);
                
                // Get all player's toxin tiles
                var playerToxins = GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(c => c.IsToxin)
                    .ToList();
                
                if (playerToxins.Count == 0)
                {
                    GameManager.Instance.ShowSelectionPrompt("No toxins available to explode!");
                    yield return new WaitForSeconds(2f);
                    GameManager.Instance.HideSelectionPrompt();
                    onComplete?.Invoke();
                    yield break;
                }

                GameManager.Instance.ShowSelectionPrompt(
                    $"Select one of your toxins to explode in a {MycovariantGameBalance.CytolyticBurstRadius}-tile radius."
                );

                bool done = false;

                // Highlight all player's toxin tiles
                var toxinTileIds = playerToxins.Select(c => c.TileId).ToList();
                gridVisualizer.HighlightTiles(toxinTileIds);

                // Function to check if tile is a valid toxin for this player
                Func<BoardTile, bool> isValidToxin = tile => 
                    tile.FungalCell != null && 
                    tile.FungalCell.IsToxin && 
                    tile.FungalCell.OwnerPlayerId == player.PlayerId;

                TileSelectionController.Instance.PromptSelectBoardTile(
                    isValidToxin,
                    (tile) =>
                    {
                        if (done) return; // Defensive: prevent double-callback
                        done = true;
                        
                        var playerMyco = player.PlayerMycovariants
                            .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                        if (playerMyco != null)
                        {
                            MycovariantEffectProcessor.ResolveCytolyticBurst(
                                playerMyco,
                                GameManager.Instance.Board,
                                tile.TileId,
                                new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                                null
                            );
                        }

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
                    "Select one of your toxins to explode."
                );

                while (!done) yield return null;
            }
        }
    }
}
