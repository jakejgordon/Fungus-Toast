﻿using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Unity.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Tilemaps")]
        public Tilemap toastTilemap;      // Base toast layer
        public Tilemap moldTilemap;       // Player mold layer (including faded icons)
        public Tilemap overlayTilemap;    // Dead/toxin overlays
        public Tilemap SelectionHighlightTileMap;   // Persistent selection highlights (prompt mode, pulsing)
        public Tilemap HoverOverlayTileMap;         // Temporary mouse hover overlays (single-tile, always on top)

        [Header("Tiles")]
        public Tile baseTile;             // Toast base
        public Tile deadTile;             // Dead mold
        public Tile[] playerMoldTiles;    // Living mold, indexed by PlayerId
        public Tile toxinOverlayTile;     // Toxin icon overlay
        [SerializeField] private Tile solidHighlightTile; // For pulses/highlights
        public Tile goldShieldOverlayTile; // Gold shield for resistant cells

        private GameBoard board;
        private List<Vector3Int> highlightedPositions = new List<Vector3Int>();
        private Coroutine pulseHighlightCoroutine;
        
        // Fade-in effect tracking
        private HashSet<int> newlyGrownTileIds = new HashSet<int>();
        private Dictionary<int, Coroutine> fadeInCoroutines = new Dictionary<int, Coroutine>();

        // Death animation tracking
        private HashSet<int> dyingTileIds = new HashSet<int>();
        private Dictionary<int, Coroutine> deathAnimationCoroutines = new Dictionary<int, Coroutine>();

        // Toxin drop animation tracking
        private HashSet<int> toxinDropTileIds = new HashSet<int>();
        private Dictionary<int, Coroutine> toxinDropCoroutines = new Dictionary<int, Coroutine>();

        // Pulse color scheme, can be set by HighlightTiles
        private Color pulseColorA = new Color(1f, 1f, 0.1f, 1f); // Default: yellow
        private Color pulseColorB = new Color(0.1f, 1f, 1f, 1f); // Default: cyan

        public void Initialize(GameBoard board)
        {
            this.board = board;
        }

        public void RenderBoard(GameBoard board)
        {
            // Clear any existing fade-in coroutines
            foreach (var coroutine in fadeInCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            fadeInCoroutines.Clear();
            
            // Clear any existing death animation coroutines
            foreach (var coroutine in deathAnimationCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            deathAnimationCoroutines.Clear();
            
            // Clear any existing toxin drop coroutines
            foreach (var coroutine in toxinDropCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            toxinDropCoroutines.Clear();
            
            // Track newly grown cells for fade-in effect
            newlyGrownTileIds.Clear();
            // Track dying cells for death animation effect
            dyingTileIds.Clear();
            // Track toxin drop cells for toxin drop animation effect
            toxinDropTileIds.Clear();
            
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var tile = board.Grid[x, y];
                    if (tile.FungalCell?.IsNewlyGrown == true)
                    {
                        newlyGrownTileIds.Add(tile.TileId);
                    }
                    if (tile.FungalCell?.IsDying == true)
                    {
                        dyingTileIds.Add(tile.TileId);
                    }
                    if (tile.FungalCell?.IsReceivingToxinDrop == true)
                    {
                        toxinDropTileIds.Add(tile.TileId);
                    }
                }
            }

            toastTilemap.ClearAllTiles();
            moldTilemap.ClearAllTiles();
            overlayTilemap.ClearAllTiles();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    BoardTile tile = board.Grid[x, y];

                    toastTilemap.SetTile(pos, baseTile);
                    toastTilemap.SetTileFlags(pos, TileFlags.None);
                    toastTilemap.SetColor(pos, Color.white);

                    RenderFungalCellOverlay(tile, pos);
                }
            }
            
            // Start fade-in animations for newly grown cells
            StartFadeInAnimations();
            
            // Start death animations for dying cells
            StartDeathAnimations();
            
            // Start toxin drop animations for toxin drop cells
            StartToxinDropAnimations();
        }

        /// <summary>
        /// Highlights all living tiles for a given player, with default (yellow/cyan) pulse.
        /// </summary>
        public void HighlightPlayerTiles(int playerId)
        {
            HighlightTiles(
                board.AllTiles()
                    .Where(t =>
                        t.FungalCell != null &&
                        t.FungalCell.OwnerPlayerId == playerId &&
                        (t.FungalCell.CellType == FungalCellType.Alive || t.FungalCell.CellType == FungalCellType.Toxin)
                    )
                    .Select(t => t.TileId),
                new Color(1f, 1f, 0.1f, 1f),  // Bright yellow
                new Color(0.1f, 1f, 1f, 1f)   // Bright cyan
            );
        }

        /// <summary>
        /// Highlights any set of tiles with a pulsing color. Use for selection, special events, etc.
        /// </summary>
        /// <param name="tileIds">The tile IDs to highlight</param>
        /// <param name="colorA">Pulse color A (optional, defaults to yellow)</param>
        /// <param name="colorB">Pulse color B (optional, defaults to cyan)</param>
        public void HighlightTiles(IEnumerable<int> tileIds, Color? colorA = null, Color? colorB = null)
        {
            SelectionHighlightTileMap.ClearAllTiles();
            highlightedPositions.Clear();

            foreach (var tileId in tileIds)
            {
                var (x, y) = board.GetXYFromTileId(tileId);
                Vector3Int pos = new Vector3Int(x, y, 0);
                SelectionHighlightTileMap.SetTile(pos, solidHighlightTile);
                SelectionHighlightTileMap.SetTileFlags(pos, TileFlags.None);
                SelectionHighlightTileMap.SetColor(pos, Color.white);
                highlightedPositions.Add(pos);
            }

            // Set pulse colors if provided, else use defaults
            pulseColorA = colorA ?? new Color(1f, 1f, 0.1f, 1f);
            pulseColorB = colorB ?? new Color(0.1f, 1f, 1f, 1f);

            if (highlightedPositions.Count > 0)
            {
                if (pulseHighlightCoroutine != null)
                    StopCoroutine(pulseHighlightCoroutine);

                pulseHighlightCoroutine = StartCoroutine(PulseHighlightTiles());
            }
        }

        /// <summary>
        /// Highlights tiles with per-tile color support. Each tileId maps to a (colorA, colorB) tuple.
        /// </summary>
        public void HighlightTiles(IDictionary<int, (Color colorA, Color colorB)> tileHighlights)
        {
            SelectionHighlightTileMap.ClearAllTiles();
            highlightedPositions.Clear();

            foreach (var kvp in tileHighlights)
            {
                int tileId = kvp.Key;
                var (colorA, colorB) = kvp.Value;
                var (x, y) = board.GetXYFromTileId(tileId);
                Vector3Int pos = new Vector3Int(x, y, 0);
                SelectionHighlightTileMap.SetTile(pos, solidHighlightTile);
                SelectionHighlightTileMap.SetTileFlags(pos, TileFlags.None);
                // Use colorA for now (could pulse between colorA/colorB if needed)
                SelectionHighlightTileMap.SetColor(pos, colorA);
                highlightedPositions.Add(pos);
            }

            // No pulsing for per-tile highlights (could be added if needed)
            if (pulseHighlightCoroutine != null)
            {
                StopCoroutine(pulseHighlightCoroutine);
                pulseHighlightCoroutine = null;
            }
        }

        /// <summary>
        /// Clears all highlights and stops pulsing.
        /// </summary>
        public void ClearHighlights()
        {
            SelectionHighlightTileMap.ClearAllTiles();
            highlightedPositions.Clear();
            if (pulseHighlightCoroutine != null)
            {
                StopCoroutine(pulseHighlightCoroutine);
                pulseHighlightCoroutine = null;
            }
            // Reset scale!
            if (SelectionHighlightTileMap != null)
                SelectionHighlightTileMap.transform.localScale = Vector3.one;
        }

        private IEnumerator PulseHighlightTiles()
        {
            float duration = 0.8f;         // Slow, visible pulse (tweak as needed)
            float minAlpha = 0f;           // Fully transparent at the low end
            float maxAlpha = 1f;           // Fully opaque at the peak
            Color colorA = new Color(1f, 0.15f, 0.8f, 1f);    // Bright magenta-pink
            Color colorB = new Color(1f, 1f, 1f, 1f);         // White

            while (true)
            {
                float t = Mathf.PingPong(Time.time / duration, 1f); // Loops 0→1→0
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);    // Fades to 0, back to 1
                Color pulseColor = Color.Lerp(colorA, colorB, t);
                pulseColor.a = alpha;

                foreach (var pos in highlightedPositions)
                {
                    if (SelectionHighlightTileMap.HasTile(pos))
                    {
                        SelectionHighlightTileMap.SetColor(pos, pulseColor);
                    }
                }
                yield return null;
            }
        }

        private void RenderFungalCellOverlay(BoardTile tile, Vector3Int pos)
        {
            TileBase moldTile = null;
            TileBase overlayTile = null;
            Color moldColor = Color.white;
            Color overlayColor = Color.white;

            var cell = tile.FungalCell;
            if (cell == null)
                return;

            switch (cell.CellType)
            {
                case FungalCellType.Alive:
                    if (cell.OwnerPlayerId is int idA && idA >= 0 && idA < playerMoldTiles.Length)
                    {
                        moldTile = playerMoldTiles[idA];
                        // Start newly grown cells with low alpha for fade-in effect
                        moldColor = cell.IsNewlyGrown ? new Color(1f, 1f, 1f, 0.1f) : Color.white;
                    }
                    // If the cell is resistant, show the shield overlay
                    if (cell.IsResistant && goldShieldOverlayTile != null)
                    {
                        overlayTile = goldShieldOverlayTile;
                        overlayColor = Color.white;
                    }
                    break;

                case FungalCellType.Dead:
                    if (cell.OwnerPlayerId is int ownerId && ownerId >= 0 && ownerId < playerMoldTiles.Length)
                    {
                        moldTilemap.SetTileFlags(pos, TileFlags.None);
                        moldTilemap.SetTile(pos, playerMoldTiles[ownerId]);
                        
                        // If the cell is dying, show it as living for the crossfade animation
                        if (cell.IsDying)
                        {
                            moldColor = Color.white; // Full opacity for living appearance
                        }
                        else
                        {
                            moldTilemap.SetColor(pos, new Color(1f, 1f, 1f, 0.8f)); // Increased from 0.55f to 0.8f for better visibility
                            moldTilemap.RefreshTile(pos);
                        }
                    }
                    overlayTile = deadTile;
                    // If the cell is dying, start the overlay transparent for crossfade
                    overlayColor = cell.IsDying ? new Color(1f, 1f, 1f, 0f) : Color.white;
                    break;

                case FungalCellType.Toxin:
                    if (cell.OwnerPlayerId is int idT && idT >= 0 && idT < playerMoldTiles.Length)
                    {
                        // Show more visible, slightly gray mold color under the toxin overlay
                        moldTile = playerMoldTiles[idT];
                        
                        // If the cell is receiving a toxin drop, show it as normal for the drop animation
                        if (cell.IsReceivingToxinDrop)
                        {
                            moldColor = Color.white; // Normal appearance for drop animation
                        }
                        else
                        {
                            moldColor = new Color(0.8f, 0.8f, 0.8f, 0.8f); // Increased alpha from 0.55f to 0.8f for better visibility
                        }
                    }
                    overlayTile = toxinOverlayTile;
                    // If the cell is receiving a toxin drop, start the overlay transparent for drop animation
                    overlayColor = cell.IsReceivingToxinDrop ? new Color(1f, 1f, 1f, 0f) : Color.white;
                    break;

                default:
                    // Unoccupied or unknown: nothing to render
                    return;
            }

            // Set faded (or normal) mold tile
            if (moldTile != null)
            {
                moldTilemap.SetTile(pos, moldTile);
                moldTilemap.SetTileFlags(pos, TileFlags.None);
                moldTilemap.SetColor(pos, moldColor);
                moldTilemap.RefreshTile(pos);
            }

            // Set overlay tile (dead, toxin, or shield)
            if (overlayTile != null)
            {
                overlayTilemap.SetTile(pos, overlayTile);
                overlayTilemap.SetTileFlags(pos, TileFlags.None);
                overlayTilemap.SetColor(pos, overlayColor);
                overlayTilemap.RefreshTile(pos);
            }
        }

        private void SetOverlayTile(Vector3Int pos, TileBase tile, Color color)
        {
            overlayTilemap.SetTile(pos, tile);
            overlayTilemap.SetTileFlags(pos, TileFlags.None);
            overlayTilemap.SetColor(pos, color);
            overlayTilemap.RefreshTile(pos);
        }

        public Tile GetTileForPlayer(int playerId)
        {
            if (playerMoldTiles != null && playerId >= 0 && playerId < playerMoldTiles.Length)
                return playerMoldTiles[playerId];

            Debug.LogWarning($"No tile found for Player ID {playerId}.");
            return null;
        }

        /// <summary>
        /// Starts fade-in animations for all newly grown cells
        /// </summary>
        private void StartFadeInAnimations()
        {
            foreach (int tileId in newlyGrownTileIds)
            {
                if (fadeInCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(fadeInCoroutines[tileId]);
                }
                fadeInCoroutines[tileId] = StartCoroutine(FadeInCell(tileId));
            }
        }

        /// <summary>
        /// Coroutine that fades in a newly grown cell from low alpha to full opacity
        /// </summary>
        private IEnumerator FadeInCell(int tileId)
        {
            var (x, y) = board.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(x, y, 0);
            
            float duration = UIEffectConstants.CellGrowthFadeInDurationSeconds; // Fast fade-in to not slow down the game
            float startAlpha = 0.1f;
            float targetAlpha = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                // Update the mold tile color
                if (moldTilemap.HasTile(pos))
                {
                    Color currentColor = moldTilemap.GetColor(pos);
                    currentColor.a = currentAlpha;
                    moldTilemap.SetColor(pos, currentColor);
                }
                
                yield return null;
            }

            // Ensure final alpha is exactly 1.0
            if (moldTilemap.HasTile(pos))
            {
                Color finalColor = moldTilemap.GetColor(pos);
                finalColor.a = 1f;
                moldTilemap.SetColor(pos, finalColor);
            }

            // Clear the newly grown flag on the cell
            var tile = board.GetTileById(tileId);
            if (tile?.FungalCell != null)
            {
                tile.FungalCell.ClearNewlyGrownFlag();
            }

            // Clean up the coroutine reference
            fadeInCoroutines.Remove(tileId);
        }

        /// <summary>
        /// Starts death animations for all dying cells
        /// </summary>
        private void StartDeathAnimations()
        {
            foreach (int tileId in dyingTileIds)
            {
                if (deathAnimationCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(deathAnimationCoroutines[tileId]);
                }
                deathAnimationCoroutines[tileId] = StartCoroutine(DeathAnimation(tileId));
            }
        }

        /// <summary>
        /// Manually triggers a death animation for a specific tile (for testing)
        /// </summary>
        public void TriggerDeathAnimation(int tileId)
        {
            var tile = board.GetTileById(tileId);
            if (tile?.FungalCell != null)
            {
                tile.FungalCell.MarkAsDying();
                dyingTileIds.Add(tileId);
                if (deathAnimationCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(deathAnimationCoroutines[tileId]);
                }
                deathAnimationCoroutines[tileId] = StartCoroutine(DeathAnimation(tileId));
            }
        }

        /// <summary>
        /// Coroutine that handles the death animation for a dying cell
        /// </summary>
        private IEnumerator DeathAnimation(int tileId)
        {
            var (x, y) = board.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(x, y, 0);
            
            float duration = UIEffectConstants.CellDeathAnimationDurationSeconds; // Crossfade duration
            float elapsed = 0f;

            // Get the current cell to determine the death animation parameters
            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (cell == null)
            {
                deathAnimationCoroutines.Remove(tileId);
                yield break;
            }

            // Store initial colors
            Color initialLivingColor = moldTilemap.HasTile(pos) ? moldTilemap.GetColor(pos) : Color.white;
            Color initialOverlayColor = overlayTilemap.HasTile(pos) ? overlayTilemap.GetColor(pos) : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Fade out living cell (from full opacity to 0.8f for dead cell)
                if (moldTilemap.HasTile(pos))
                {
                    Color livingColor = initialLivingColor;
                    livingColor.a = Mathf.Lerp(1f, 0.8f, progress); // Changed from 0.55f to 0.8f to match new dead cell opacity
                    moldTilemap.SetColor(pos, livingColor);
                }
                
                // Fade in dead cell overlay (from 0 to full opacity)
                if (overlayTilemap.HasTile(pos))
                {
                    Color overlayColor = initialOverlayColor;
                    overlayColor.a = Mathf.Lerp(0f, 1f, progress);
                    overlayTilemap.SetColor(pos, overlayColor);
                }
                
                yield return null;
            }

            // Ensure final state matches the dead cell appearance
            if (moldTilemap.HasTile(pos))
            {
                Color finalLivingColor = initialLivingColor;
                finalLivingColor.a = 0.8f; // Changed from 0.55f to 0.8f to match new dead cell opacity
                moldTilemap.SetColor(pos, finalLivingColor);
            }
            
            if (overlayTilemap.HasTile(pos))
            {
                Color finalOverlayColor = initialOverlayColor;
                finalOverlayColor.a = 1f; // Full overlay opacity
                overlayTilemap.SetColor(pos, finalOverlayColor);
            }

            // Clear the dying flag on the cell
            cell.ClearDyingFlag();

            // Clean up the coroutine reference
            deathAnimationCoroutines.Remove(tileId);
        }

        /// <summary>
        /// Starts toxin drop animations for all toxin drop cells
        /// </summary>
        private void StartToxinDropAnimations()
        {
            foreach (int tileId in toxinDropTileIds)
            {
                if (toxinDropCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(toxinDropCoroutines[tileId]);
                }
                toxinDropCoroutines[tileId] = StartCoroutine(ToxinDropAnimation(tileId));
            }
        }

        /// <summary>
        /// Manually triggers a toxin drop animation for a specific tile (for testing)
        /// </summary>
        public void TriggerToxinDropAnimation(int tileId)
        {
            var tile = board.GetTileById(tileId);
            if (tile?.FungalCell != null)
            {
                tile.FungalCell.MarkAsReceivingToxinDrop();
                toxinDropTileIds.Add(tileId);
                if (toxinDropCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(toxinDropCoroutines[tileId]);
                }
                toxinDropCoroutines[tileId] = StartCoroutine(ToxinDropAnimation(tileId));
            }
        }

        /// <summary>
        /// Coroutine that handles the toxin drop animation for a cell receiving a toxin
        /// </summary>
        private IEnumerator ToxinDropAnimation(int tileId)
        {
            var (x, y) = board.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(x, y, 0);
            
            float duration = UIEffectConstants.ToxinDropAnimationDurationSeconds;

            // Get the current cell to determine the animation parameters
            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (cell == null)
            {
                toxinDropCoroutines.Remove(tileId);
                yield break;
            }

            // Store initial colors
            Color initialMoldColor = moldTilemap.HasTile(pos) ? moldTilemap.GetColor(pos) : Color.white;
            Color initialOverlayColor = overlayTilemap.HasTile(pos) ? overlayTilemap.GetColor(pos) : Color.white;

            // Phase 1: Drop appears (0-20% of duration)
            float dropPhaseDuration = duration * 0.2f;
            float dropPhaseElapsed = 0f;
            
            while (dropPhaseElapsed < dropPhaseDuration)
            {
                dropPhaseElapsed += Time.deltaTime;
                float progress = dropPhaseElapsed / dropPhaseDuration;
                
                // Create a toxic green drop effect by tinting the tile
                Color toxicTint = Color.Lerp(Color.white, new Color(0f, 1f, 0.25f, 0.3f), progress);
                
                if (moldTilemap.HasTile(pos))
                {
                    moldTilemap.SetColor(pos, toxicTint);
                }
                
                yield return null;
            }

            // Phase 2: Drop spreads (20-70% of duration)
            float spreadPhaseDuration = duration * 0.5f;
            float spreadPhaseElapsed = 0f;
            
            while (spreadPhaseElapsed < spreadPhaseDuration)
            {
                spreadPhaseElapsed += Time.deltaTime;
                float progress = spreadPhaseElapsed / spreadPhaseDuration;
                
                // Spread the toxic effect and intensify it
                Color toxicSpread = Color.Lerp(
                    new Color(0f, 1f, 0.25f, 0.3f), 
                    new Color(0f, 1f, 0.25f, 0.8f), 
                    progress
                );
                
                if (moldTilemap.HasTile(pos))
                {
                    moldTilemap.SetColor(pos, toxicSpread);
                }
                
                yield return null;
            }

            // Phase 3: Settle into final toxin state (70-100% of duration)
            float settlePhaseDuration = duration * 0.3f;
            float settlePhaseElapsed = 0f;
            
            while (settlePhaseElapsed < settlePhaseDuration)
            {
                settlePhaseElapsed += Time.deltaTime;
                float progress = settlePhaseElapsed / settlePhaseDuration;
                
                // Transition to final toxin appearance
                Color finalToxinColor = Color.Lerp(
                    new Color(0f, 1f, 0.25f, 0.8f),
                    new Color(0.8f, 0.8f, 0.8f, 0.8f), // Changed alpha from 0.55f to 0.8f to match new toxin cell opacity
                    progress
                );
                
                Color finalOverlayColor = Color.Lerp(
                    Color.clear,
                    Color.white, // Final toxin overlay color
                    progress
                );
                
                if (moldTilemap.HasTile(pos))
                {
                    moldTilemap.SetColor(pos, finalToxinColor);
                }
                
                if (overlayTilemap.HasTile(pos))
                {
                    overlayTilemap.SetColor(pos, finalOverlayColor);
                }
                
                yield return null;
            }

            // Ensure final state matches the toxin appearance
            if (moldTilemap.HasTile(pos))
            {
                Color finalMoldColor = new Color(0.8f, 0.8f, 0.8f, 0.8f); // Changed alpha from 0.55f to 0.8f to match new toxin cell opacity
                moldTilemap.SetColor(pos, finalMoldColor);
            }
            
            if (overlayTilemap.HasTile(pos))
            {
                overlayTilemap.SetColor(pos, Color.white); // Full toxin overlay opacity
            }

            // Clear the toxin drop flag on the cell
            cell.ClearToxinDropFlag();

            // Clean up the coroutine reference
            toxinDropCoroutines.Remove(tileId);
        }
    }
}
