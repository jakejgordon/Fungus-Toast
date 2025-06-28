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
        public Tilemap HoverTileMap;      // Mouseover and highlight overlays

        [Header("Tiles")]
        public Tile baseTile;             // Toast base
        public Tile deadTile;             // Dead mold
        public Tile[] playerMoldTiles;    // Living mold, indexed by PlayerId
        public Tile toxinOverlayTile;     // Toxin icon overlay
        [SerializeField] private Tile solidHighlightTile; // For pulses/highlights

        private GameBoard board;
        private List<Vector3Int> highlightedPositions = new List<Vector3Int>();
        private Coroutine pulseHighlightCoroutine;

        // Pulse color scheme, can be set by HighlightTiles
        private Color pulseColorA = new Color(1f, 1f, 0.1f, 1f); // Default: yellow
        private Color pulseColorB = new Color(0.1f, 1f, 1f, 1f); // Default: cyan

        public void Initialize(GameBoard board)
        {
            this.board = board;
        }

        public void RenderBoard(GameBoard board)
        {
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
        }

        /// <summary>
        /// Highlights all living tiles for a given player, with default (yellow/cyan) pulse.
        /// </summary>
        public void HighlightPlayerTiles(int playerId)
        {
            HighlightTiles(
                board.AllTiles()
                    .Where(t => t.FungalCell?.CellType == FungalCellType.Alive &&
                                t.FungalCell.OwnerPlayerId == playerId)
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
            HoverTileMap.ClearAllTiles();
            highlightedPositions.Clear();

            foreach (var tileId in tileIds)
            {
                var (x, y) = board.GetXYFromTileId(tileId);
                Vector3Int pos = new Vector3Int(x, y, 0);
                HoverTileMap.SetTile(pos, solidHighlightTile);
                HoverTileMap.SetTileFlags(pos, TileFlags.None);
                HoverTileMap.SetColor(pos, Color.white);
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
            HoverTileMap.ClearAllTiles();
            highlightedPositions.Clear();
            if (pulseHighlightCoroutine != null)
            {
                StopCoroutine(pulseHighlightCoroutine);
                pulseHighlightCoroutine = null;
            }
            // Reset scale!
            if (HoverTileMap != null)
                HoverTileMap.transform.localScale = Vector3.one;
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
                    if (HoverTileMap.HasTile(pos))
                    {
                        HoverTileMap.SetColor(pos, pulseColor);
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
                        moldColor = Color.white;
                    }
                    break;

                case FungalCellType.Dead:
                    // Fade the player mold, then overlay the deadTile
                    if (cell.LastOwnerPlayerId is int ownerId && ownerId >= 0 && ownerId < playerMoldTiles.Length)
                    {
                        moldTilemap.SetTileFlags(pos, TileFlags.None);
                        moldTilemap.SetTile(pos, playerMoldTiles[ownerId]);
                        moldTilemap.SetColor(pos, new Color(1f, 1f, 1f, 0.35f));
                        moldTilemap.RefreshTile(pos);
                    }
                    // Overlay the dead tile on a separate tilemap
                    overlayTilemap.SetTileFlags(pos, TileFlags.None);
                    overlayTilemap.SetTile(pos, deadTile);
                    overlayTilemap.SetColor(pos, Color.white);
                    overlayTilemap.RefreshTile(pos);
                    break;

                case FungalCellType.Toxin:
                    if (cell.OwnerPlayerId is int idT && idT >= 0 && idT < playerMoldTiles.Length)
                    {
                        // Show very faint, slightly gray mold color under the toxin overlay
                        moldTile = playerMoldTiles[idT];
                        moldColor = new Color(0.8f, 0.8f, 0.8f, 0.35f); // More visible, still faded
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

            // Set overlay tile (dead or toxin)
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
    }
}
