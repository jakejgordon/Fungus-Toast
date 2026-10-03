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
        private Vector3Int? _originPreviewPosition;
        private Coroutine _previewPulseCoroutine;
        // Preview and hover share the overlay tilemap and can both be live while aiming. Each preview
        // tile's resting colour is kept so the hover can hand the tile back instead of erasing it.
        private readonly Dictionary<Vector3Int, Color> _previewRestColors = new();
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
                Vector3Int pos = _currentHoveredPosition.Value;
                _currentHoveredPosition = null;
                _hoverOverlayTileMap.SetTransformMatrix(pos, Matrix4x4.identity);
                if (_previewRestColors.TryGetValue(pos, out Color restColor))
                {
                    _hoverOverlayTileMap.SetColor(pos, restColor);
                }
                else
                {
                    _hoverOverlayTileMap.SetTile(pos, null);
                    _hoverOverlayTileMap.SetColor(pos, Color.white);
                }

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
                SetPreviewTile(pos, UIEffectConstants.JettingMyceliumPreviewLivingDimColor);
                _livingPreviewPositions.Add(pos);
            }
            foreach (var pos in toxinCellPositions)
            {
                SetPreviewTile(pos, UIEffectConstants.JettingMyceliumPreviewToxinDimColor);
                _toxinPreviewPositions.Add(pos);
            }

            if (_livingPreviewPositions.Count + _toxinPreviewPositions.Count > 0)
                _previewPulseCoroutine = _runner.StartCoroutine(PreviewPulseAnimation());
        }

        /// <summary>
        /// Shows the Chemotactic Beacon line preview: traversed tiles solid gray, the growth origin pulsing with the
        /// selectable-tile magenta, and growth tiles solid black.
        /// </summary>
        public void ShowChemotacticBeaconPreview(IEnumerable<Vector3Int> traversedPositions, Vector3Int? originPosition, IEnumerable<Vector3Int> growthPositions)
        {
            ClearPreviewTiles();
            if (_solidHighlightTile == null || _hoverOverlayTileMap == null)
            {
                return;
            }

            foreach (var pos in traversedPositions)
            {
                SetPreviewTile(pos, UIEffectConstants.ChemobeaconPreviewTraversedColor);
                _livingPreviewPositions.Add(pos);
            }
            foreach (var pos in growthPositions)
            {
                SetPreviewTile(pos, UIEffectConstants.ChemobeaconPreviewGrowthColor);
                _livingPreviewPositions.Add(pos);
            }

            if (originPosition.HasValue)
            {
                var pos = originPosition.Value;
                SetPreviewTile(pos, UIEffectConstants.SelectableTilePulseBrightColor);
                _originPreviewPosition = pos;
                _previewPulseCoroutine = _runner.StartCoroutine(OriginPreviewPulseAnimation());
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
                // The hovered tile belongs to the hover now; it is released when the hover moves on.
                foreach (var pos in _previewRestColors.Keys)
                {
                    if (pos != _currentHoveredPosition)
                        _hoverOverlayTileMap.SetTile(pos, null);
                }
            }
            _previewRestColors.Clear();
            _livingPreviewPositions.Clear();
            _toxinPreviewPositions.Clear();
            _originPreviewPosition = null;
        }

        /// <summary>
        /// Pulses the beacon growth origin with the same magenta ping-pong used for selectable tiles,
        /// so it reads as "this is where the line starts" against the static gray/black line.
        /// </summary>
        private IEnumerator OriginPreviewPulseAnimation()
        {
            float pulseDuration = UIEffectConstants.SelectableTilePulseDurationSeconds;
            Color dim = UIEffectConstants.SelectableTilePulseDimColor;
            Color bright = UIEffectConstants.SelectableTilePulseBrightColor;

            while (_originPreviewPosition.HasValue && _hoverOverlayTileMap != null && _hoverOverlayTileMap.HasTile(_originPreviewPosition.Value))
            {
                float colorT = Mathf.PingPong(Time.time / pulseDuration, 1f);
                float easedColorT = colorT < 0.5f
                    ? 2f * colorT * colorT
                    : 1f - 2f * (1f - colorT) * (1f - colorT);
                if (_originPreviewPosition != _currentHoveredPosition)
                    _hoverOverlayTileMap.SetColor(_originPreviewPosition.Value, Color.Lerp(dim, bright, easedColorT));
                yield return null;
            }
        }

        private void SetPreviewTile(Vector3Int pos, Color restColor)
        {
            _previewRestColors[pos] = restColor;
            if (pos == _currentHoveredPosition)
            {
                // Re-setting the tile would drop the hover's enlarged transform; ClearHoverEffect restores it.
                return;
            }

            _hoverOverlayTileMap.SetTile(pos, _solidHighlightTile);
            _hoverOverlayTileMap.SetTileFlags(pos, TileFlags.None);
            _hoverOverlayTileMap.SetColor(pos, restColor);
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
                    if (pos != _currentHoveredPosition && _hoverOverlayTileMap.HasTile(pos))
                        _hoverOverlayTileMap.SetColor(pos, livingColor);
                }
                foreach (var pos in _toxinPreviewPositions)
                {
                    if (pos != _currentHoveredPosition && _hoverOverlayTileMap.HasTile(pos))
                        _hoverOverlayTileMap.SetColor(pos, toxinColor);
                }

                yield return null;
            }
        }
    }
}
