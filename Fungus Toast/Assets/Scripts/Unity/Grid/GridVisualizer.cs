using FungusToast.Core;
using FungusToast.Core.Board;
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
            
            // Track newly grown cells for fade-in effect
            newlyGrownTileIds.Clear();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var tile = board.Grid[x, y];
                    if (tile.FungalCell?.IsNewlyGrown == true)
                    {
                        newlyGrownTileIds.Add(tile.TileId);
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
                    if (cell.LastOwnerPlayerId is int ownerId && ownerId >= 0 && ownerId < playerMoldTiles.Length)
                    {
                        moldTilemap.SetTileFlags(pos, TileFlags.None);
                        moldTilemap.SetTile(pos, playerMoldTiles[ownerId]);
                        moldTilemap.SetColor(pos, new Color(1f, 1f, 1f, 0.55f));
                        moldTilemap.RefreshTile(pos);
                    }
                    overlayTile = deadTile;
                    overlayColor = Color.white;
                    break;

                case FungalCellType.Toxin:
                    if (cell.OwnerPlayerId is int idT && idT >= 0 && idT < playerMoldTiles.Length)
                    {
                        // Show very faint, slightly gray mold color under the toxin overlay
                        moldTile = playerMoldTiles[idT];
                        moldColor = new Color(0.8f, 0.8f, 0.8f, 0.55f); // More visible, still faded
                    }
                    overlayTile = toxinOverlayTile;
                    overlayColor = Color.white;
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
            
            float duration = 0.3f; // Fast fade-in to not slow down the game
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
    }
}
