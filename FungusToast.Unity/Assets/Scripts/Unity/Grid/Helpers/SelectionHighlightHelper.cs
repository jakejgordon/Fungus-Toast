using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
    /// <summary>
    /// Encapsulates selection highlighting and selected tiles rendering on tilemaps.
    /// GridVisualizer remains the coroutine owner; this helper exposes logic and IEnumerators.
    /// </summary>
    internal class SelectionHighlightHelper
    {
        private readonly Tilemap _selectionHighlightTileMap;
        private readonly Tilemap _selectedTileMap;
        private readonly Tile _solidHighlightTile;

        public SelectionHighlightHelper(Tilemap selectionHighlightTileMap, Tilemap selectedTileMap, Tile solidHighlightTile)
        {
            _selectionHighlightTileMap = selectionHighlightTileMap;
            _selectedTileMap = selectedTileMap;
            _solidHighlightTile = solidHighlightTile;
        }

        public void HighlightTiles(IEnumerable<int> tileIds, GameBoard board, List<Vector3Int> highlightedPositions)
        {
            _selectionHighlightTileMap.ClearAllTiles();
            highlightedPositions.Clear();

            foreach (var tileId in tileIds)
            {
                var xy = board.GetXYFromTileId(tileId);
                Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);
                _selectionHighlightTileMap.SetTile(pos, _solidHighlightTile);
                _selectionHighlightTileMap.SetTileFlags(pos, TileFlags.None);
                _selectionHighlightTileMap.SetColor(pos, Color.white);
                highlightedPositions.Add(pos);
            }
        }

        public void HighlightTiles(IDictionary<int, (Color colorA, Color colorB)> tileHighlights, GameBoard board, List<Vector3Int> highlightedPositions)
        {
            _selectionHighlightTileMap.ClearAllTiles();
            highlightedPositions.Clear();

            foreach (var kvp in tileHighlights)
            {
                int tileId = kvp.Key;
                var (colorA, _) = kvp.Value;
                var xy = board.GetXYFromTileId(tileId);
                Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);
                _selectionHighlightTileMap.SetTile(pos, _solidHighlightTile);
                _selectionHighlightTileMap.SetTileFlags(pos, TileFlags.None);
                _selectionHighlightTileMap.SetColor(pos, colorA);
                highlightedPositions.Add(pos);
            }
        }

        public IEnumerator PulseHighlightTiles(List<Vector3Int> highlightedPositions, Color transparentColor, Color brightColor, float pulseDuration)
        {
            while (true)
            {
                float colorT = Mathf.PingPong(Time.time / pulseDuration, 1f);
                float easedColorT = colorT < 0.5f
                    ? 2f * colorT * colorT
                    : 1f - 2f * (1f - colorT) * (1f - colorT);

                Color pulseColor = Color.Lerp(transparentColor, brightColor, easedColorT);

                foreach (var pos in highlightedPositions)
                {
                    if (_selectionHighlightTileMap.HasTile(pos))
                    {
                        _selectionHighlightTileMap.SetColor(pos, pulseColor);
                    }
                }

                yield return null;
            }
        }

        public void ShowSelectedTiles(IEnumerable<int> tileIds, GameBoard board, Color? selectedColor = null)
        {
            _selectedTileMap.ClearAllTiles();
            Color color = selectedColor ?? new Color(1f, 0.8f, 0.2f, 1f);

            foreach (var tileId in tileIds)
            {
                var xy = board.GetXYFromTileId(tileId);
                Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);
                _selectedTileMap.SetTile(pos, _solidHighlightTile);
                _selectedTileMap.SetTileFlags(pos, TileFlags.None);
                _selectedTileMap.SetColor(pos, color);
            }
        }

        public void ClearSelectedTiles()
        {
            _selectedTileMap.ClearAllTiles();
        }

        public void ClearHighlights(List<Vector3Int> highlightedPositions)
        {
            _selectionHighlightTileMap.ClearAllTiles();
            highlightedPositions.Clear();
            if (_selectionHighlightTileMap != null)
                _selectionHighlightTileMap.transform.localScale = Vector3.one;
        }
    }
}
