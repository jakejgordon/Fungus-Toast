using System.Collections;
using System.Collections.Generic;
using FungusToast.Unity.UI;
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

        private readonly List<Vector3Int> _livingPreviewPositions = new();
        private readonly List<Vector3Int> _toxinPreviewPositions = new();
        private Coroutine _previewPulseCoroutine;
        private static readonly Matrix4x4 InspectionHoverMatrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(1.08f, 1.08f, 1f));

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
                _hoverOverlayTileMap.SetTransformMatrix(cellPos, InspectionHoverMatrix);
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
                _hoverOverlayTileMap.SetColor(_currentHoveredPosition.Value, Color.white);
                _hoverOverlayTileMap.SetTransformMatrix(_currentHoveredPosition.Value, Matrix4x4.identity);
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
            const float pulseDuration = 1.5f;
            Color dimColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, 0.38f);
            Color brightColor = UIStyleTokens.WithAlpha(UIStyleTokens.Text.Primary, 0.82f);

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

        /// <summary>
        /// Shows a pulsing multi-tile preview on the hover overlay.
        /// Living-cell projection tiles pulse cyan/teal; toxin-cone tiles pulse orange/amber.
        /// </summary>
        public void ShowPreviewTiles(IEnumerable<Vector3Int> livingCellPositions, IEnumerable<Vector3Int> toxinCellPositions)
        {
            ClearPreviewTiles();
            if (_solidHighlightTile == null || _hoverOverlayTileMap == null) return;

            foreach (var pos in livingCellPositions)
            {
                _hoverOverlayTileMap.SetTile(pos, _solidHighlightTile);
                _hoverOverlayTileMap.SetTileFlags(pos, TileFlags.None);
                _livingPreviewPositions.Add(pos);
            }
            foreach (var pos in toxinCellPositions)
            {
                _hoverOverlayTileMap.SetTile(pos, _solidHighlightTile);
                _hoverOverlayTileMap.SetTileFlags(pos, TileFlags.None);
                _toxinPreviewPositions.Add(pos);
            }

            if (_livingPreviewPositions.Count + _toxinPreviewPositions.Count > 0)
                _previewPulseCoroutine = _runner.StartCoroutine(PreviewPulseAnimation());
        }

        public void ShowSolidPreviewTiles(IEnumerable<Vector3Int> positions, Color color)
        {
            ClearPreviewTiles();
            if (_solidHighlightTile == null || _hoverOverlayTileMap == null)
            {
                return;
            }

            foreach (var pos in positions)
            {
                _hoverOverlayTileMap.SetTile(pos, _solidHighlightTile);
                _hoverOverlayTileMap.SetTileFlags(pos, TileFlags.None);
                _hoverOverlayTileMap.SetColor(pos, color);
                _livingPreviewPositions.Add(pos);
            }
        }

        /// <summary>
        /// Clears all preview tiles from the hover overlay and stops the pulse animation.
        /// </summary>
        public void ClearPreviewTiles()
        {
            if (_previewPulseCoroutine != null)
            {
                _runner.StopCoroutine(_previewPulseCoroutine);
                _previewPulseCoroutine = null;
            }
            if (_hoverOverlayTileMap != null)
            {
                foreach (var pos in _livingPreviewPositions)
                    _hoverOverlayTileMap.SetTile(pos, null);
                foreach (var pos in _toxinPreviewPositions)
                    _hoverOverlayTileMap.SetTile(pos, null);
            }
            _livingPreviewPositions.Clear();
            _toxinPreviewPositions.Clear();
        }

        private IEnumerator PreviewPulseAnimation()
        {
            float pulseDuration = UIEffectConstants.JettingMyceliumPreviewPulseDurationSeconds;
            Color livingDim    = UIEffectConstants.JettingMyceliumPreviewLivingDimColor;
            Color livingBright = UIEffectConstants.JettingMyceliumPreviewLivingBrightColor;
            Color toxinDim     = UIEffectConstants.JettingMyceliumPreviewToxinDimColor;
            Color toxinBright  = UIEffectConstants.JettingMyceliumPreviewToxinBrightColor;

            while ((_livingPreviewPositions.Count + _toxinPreviewPositions.Count) > 0 && _hoverOverlayTileMap != null)
            {
                float time = Time.time / pulseDuration;
                float t = (Mathf.Sin(time * 2f * Mathf.PI) + 1f) * 0.5f;
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                Color livingColor = Color.Lerp(livingDim, livingBright, easedT);
                Color toxinColor  = Color.Lerp(toxinDim,  toxinBright,  easedT);

                foreach (var pos in _livingPreviewPositions)
                {
                    if (_hoverOverlayTileMap.HasTile(pos))
                        _hoverOverlayTileMap.SetColor(pos, livingColor);
                }
                foreach (var pos in _toxinPreviewPositions)
                {
                    if (_hoverOverlayTileMap.HasTile(pos))
                        _hoverOverlayTileMap.SetColor(pos, toxinColor);
                }

                yield return null;
            }
        }
    }
}
