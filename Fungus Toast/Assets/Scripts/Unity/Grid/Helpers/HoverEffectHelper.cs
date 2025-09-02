using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
    /// <summary>
    /// Encapsulates hover highlight logic on the HoverOverlayTileMap.
    /// </summary>
    internal class HoverEffectHelper
    {
        private readonly Tilemap _hoverOverlayTileMap;
        private readonly Tile _solidHighlightTile;
        private Vector3Int? _currentHoveredPosition = null;
        private Coroutine _hoverGlowCoroutine;
        private readonly MonoBehaviour _runner; // owner to StartCoroutine/StopCoroutine

        public HoverEffectHelper(MonoBehaviour runner, Tilemap hoverOverlayTileMap, Tile solidHighlightTile)
        {
            _runner = runner;
            _hoverOverlayTileMap = hoverOverlayTileMap;
            _solidHighlightTile = solidHighlightTile;
        }

        public void ShowHoverEffect(Vector3Int cellPos)
        {
            ClearHoverEffect();
            _currentHoveredPosition = cellPos;
            if (_solidHighlightTile != null && _hoverOverlayTileMap != null)
            {
                _hoverOverlayTileMap.SetTile(cellPos, _solidHighlightTile);
                _hoverOverlayTileMap.SetTileFlags(cellPos, TileFlags.None);
                if (_hoverGlowCoroutine != null)
                    _runner.StopCoroutine(_hoverGlowCoroutine);
                _hoverGlowCoroutine = _runner.StartCoroutine(HoverOutlineGlowAnimation(cellPos));
            }
        }

        public void ClearHoverEffect()
        {
            if (_currentHoveredPosition.HasValue && _hoverOverlayTileMap != null)
            {
                _hoverOverlayTileMap.SetTile(_currentHoveredPosition.Value, null);
                _currentHoveredPosition = null;

                if (_hoverGlowCoroutine != null)
                {
                    _runner.StopCoroutine(_hoverGlowCoroutine);
                    _hoverGlowCoroutine = null;
                }
            }
        }

        private IEnumerator HoverOutlineGlowAnimation(Vector3Int cellPos)
        {
            float pulseDuration = 1.5f;
            Color dimColor = new Color(0.2f, 0.6f, 1f, 0.2f);
            Color brightColor = new Color(0.4f, 0.8f, 1f, 0.6f);

            while (_currentHoveredPosition == cellPos && _hoverOverlayTileMap != null && _hoverOverlayTileMap.HasTile(cellPos))
            {
                float time = Time.time / pulseDuration;
                float t = (Mathf.Sin(time * 2f * Mathf.PI) + 1f) * 0.5f;
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                Color currentColor = Color.Lerp(dimColor, brightColor, easedT);
                _hoverOverlayTileMap.SetColor(cellPos, currentColor);
                yield return null;
            }
        }
    }
}
