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
        public Tilemap HoverTileMap;      // Mouseover highlights

        [Header("Tiles")]
        public Tile baseTile;             // Toast base
        public Tile deadTile;             // Dead mold
        public Tile[] playerMoldTiles;    // Living mold, indexed by PlayerId
        public Tile toxinOverlayTile;     // Toxin icon overlay
        [SerializeField] private Tile solidHighlightTile;

        private GameBoard board;
        private List<Vector3Int> highlightedPositions = new List<Vector3Int>();
        private Coroutine pulseHighlightCoroutine;


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

        public void HighlightPlayerTiles(int playerId)
        {
            HoverTileMap.ClearAllTiles();
            highlightedPositions.Clear();

            foreach (var tile in board.AllTiles())
            {
                if (tile.FungalCell?.CellType == FungalCellType.Alive &&
                    tile.FungalCell.OwnerPlayerId == playerId)
                {
                    Vector3Int pos = new Vector3Int(tile.X, tile.Y, 0);
                    HoverTileMap.SetTile(pos, solidHighlightTile);
                    HoverTileMap.SetTileFlags(pos, TileFlags.None);
                    HoverTileMap.SetColor(pos, Color.white);
                    highlightedPositions.Add(pos);
                }
            }

            // Start pulsing if any positions
            if (highlightedPositions.Count > 0)
            {
                if (pulseHighlightCoroutine != null)
                    StopCoroutine(pulseHighlightCoroutine);

                pulseHighlightCoroutine = StartCoroutine(PulseHighlightTiles());
            }
        }

        public void ClearHighlights()
        {
            HoverTileMap.ClearAllTiles();
            highlightedPositions.Clear();
            if (pulseHighlightCoroutine != null)
            {
                StopCoroutine(pulseHighlightCoroutine);
                pulseHighlightCoroutine = null;
            }
        }

        private IEnumerator PulseHighlightTiles()
        {
            float duration = 0.4f;         // Much faster pulse
            float baseAlpha = 0.8f;        // Minimum alpha is high
            float pulseAlpha = 1.0f;       // Maximum alpha
            Color colorA = new Color(1f, 1f, 0.1f, 1f);    // Bright yellow
            Color colorB = new Color(0.1f, 1f, 1f, 1f);    // Bright cyan

            while (true)
            {
                float t = Mathf.PingPong(Time.time * (2f / duration), 1f); // Loops between 0 and 1
                float alpha = Mathf.Lerp(baseAlpha, pulseAlpha, t);
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
                        moldTilemap.SetTileFlags(pos, TileFlags.None); // << CRITICAL!
                        moldTilemap.SetTile(pos, playerMoldTiles[ownerId]);
                        moldTilemap.SetColor(pos, new Color(1f, 1f, 1f, 0.25f)); // 25% visible (very faded)
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
                        // Optionally show faint mold color under the toxin overlay
                        moldTile = playerMoldTiles[idT];
                        moldColor = new Color(1f, 1f, 1f, 0.4f);
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
