using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Unity.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
	internal sealed class GridBoardMediumRenderer
	{
		private readonly Func<BoardMediumConfig> _getActiveMedium;
		private readonly Func<Tilemap> _getToastTilemap;
		private readonly Func<Tilemap> _getCrustTilemap;
		private readonly Func<Transform> _getVisualParent;

		private SpriteRenderer _generatedCrustRenderer;
		private Sprite _generatedCrustSprite;
		private Texture2D _generatedCrustTexture;
		private string _generatedCrustCacheKey;

		public GridBoardMediumRenderer(
			Func<BoardMediumConfig> getActiveMedium,
			Func<Tilemap> getToastTilemap,
			Func<Tilemap> getCrustTilemap,
			Func<Transform> getVisualParent)
		{
			_getActiveMedium = getActiveMedium;
			_getToastTilemap = getToastTilemap;
			_getCrustTilemap = getCrustTilemap;
			_getVisualParent = getVisualParent;
		}

		public TileBase GetSurfaceTile(int x, int y, TileBase fallbackTile)
		{
			var activeMedium = _getActiveMedium();
			if (activeMedium == null || !activeMedium.ShouldOverridePlayableSurface)
			{
				return fallbackTile;
			}

			return activeMedium.GetSurfaceTile(x, y) ?? activeMedium.boardSurfaceTile;
		}

		public Color GetSurfaceColor(int x, int y, int boardWidth, int boardHeight)
		{
			var activeMedium = _getActiveMedium();
			if (activeMedium == null || !activeMedium.IsPerimeterTintEnabled)
			{
				return Color.white;
			}

			int distanceToEdge = Mathf.Min(x, y, boardWidth - 1 - x, boardHeight - 1 - y);
			if (distanceToEdge >= activeMedium.perimeterTintDepth)
			{
				return Color.white;
			}

			float depth = activeMedium.perimeterTintDepth <= 1
				? 1f
				: 1f - (distanceToEdge / (float)(activeMedium.perimeterTintDepth - 1));
			return Color.Lerp(Color.white, activeMedium.perimeterTint, depth);
		}

		public int GetCrustThickness(GameBoard activeBoard)
		{
			var activeMedium = _getActiveMedium();
			return activeMedium?.GetCrustThickness(activeBoard.Width, activeBoard.Height) ?? 0;
		}

		public Matrix4x4 GetPlayableSurfaceTileMatrix()
		{
			float scale = Mathf.Max(1f, _getActiveMedium()?.playableSurfaceTileScale ?? 1f);
			if (Mathf.Approximately(scale, 1f))
			{
				return Matrix4x4.identity;
			}

			return Matrix4x4.Scale(new Vector3(scale, scale, 1f));
		}

		public void ClearDecorativeCrustTilemap()
		{
			var crustTilemap = _getCrustTilemap();
			if (crustTilemap != null)
			{
				crustTilemap.ClearAllTiles();
			}
		}

		public void RenderDecorativeCrust(GameBoard activeBoard)
		{
			var activeMedium = _getActiveMedium();
			if (activeBoard == null || activeMedium == null)
			{
				ResetGeneratedCrustVisual();
				return;
			}

			float visualCrustThickness = activeMedium.GetVisualCrustThickness(activeBoard.Width, activeBoard.Height);
			if (visualCrustThickness <= 0f)
			{
				ResetGeneratedCrustVisual();
				return;
			}

			ClearDecorativeCrustTilemap();
			EnsureGeneratedCrustVisual(activeBoard, activeMedium, visualCrustThickness);
		}

		public void ResetGeneratedCrustVisual()
		{
			if (_generatedCrustRenderer != null)
			{
				_generatedCrustRenderer.sprite = null;
				_generatedCrustRenderer.enabled = false;
			}

			DestroyGeneratedCrustAssets();
			_generatedCrustCacheKey = null;
		}

		public void Dispose()
		{
			ResetGeneratedCrustVisual();

			if (_generatedCrustRenderer != null)
			{
				_generatedCrustRenderer = null;
			}
		}

		private void EnsureGeneratedCrustVisual(GameBoard activeBoard, BoardMediumConfig activeMedium, float visualCrustThickness)
		{
			string cacheKey = BuildGeneratedCrustCacheKey(activeBoard, activeMedium, visualCrustThickness);
			if (_generatedCrustRenderer == null)
			{
				_generatedCrustRenderer = CreateGeneratedCrustRenderer();
			}

			if (_generatedCrustRenderer == null)
			{
				return;
			}

			Transform visualParent = _getVisualParent();
			if (_generatedCrustRenderer.transform.parent != visualParent)
			{
				_generatedCrustRenderer.transform.SetParent(visualParent, false);
			}

			if (_generatedCrustCacheKey != cacheKey || _generatedCrustSprite == null || _generatedCrustTexture == null)
			{
				RebuildGeneratedCrustSprite(activeBoard, activeMedium, visualCrustThickness, cacheKey);
			}

			PositionGeneratedCrustRenderer(activeBoard);
			_generatedCrustRenderer.enabled = _generatedCrustSprite != null;
		}

		private SpriteRenderer CreateGeneratedCrustRenderer()
		{
			var crustObject = new GameObject("GeneratedCrustVisual");
			crustObject.transform.SetParent(_getVisualParent(), false);
			var spriteRenderer = crustObject.AddComponent<SpriteRenderer>();

			var toastTilemap = _getToastTilemap();
			if (toastTilemap != null)
			{
				var tilemapRenderer = toastTilemap.GetComponent<TilemapRenderer>();
				if (tilemapRenderer != null)
				{
					spriteRenderer.sortingLayerID = tilemapRenderer.sortingLayerID;
					spriteRenderer.sortingOrder = tilemapRenderer.sortingOrder - 1;
				}
			}

			return spriteRenderer;
		}

		private void RebuildGeneratedCrustSprite(GameBoard activeBoard, BoardMediumConfig activeMedium, float visualCrustThickness, string cacheKey)
		{
			DestroyGeneratedCrustAssets();

			int pixelsPerUnit = GetBackdropPixelsPerUnit(activeBoard, visualCrustThickness);
			int textureWidth = Mathf.Max(1, Mathf.CeilToInt((activeBoard.Width + (visualCrustThickness * 2f)) * pixelsPerUnit));
			int textureHeight = Mathf.Max(1, Mathf.CeilToInt((activeBoard.Height + (visualCrustThickness * 2f)) * pixelsPerUnit));

			_generatedCrustTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				alphaIsTransparency = true
			};

			var pixels = new Color32[textureWidth * textureHeight];
			float outerFeather = 4f / pixelsPerUnit;

			for (int py = 0; py < textureHeight; py++)
			{
				float y = ((py + 0.5f) / pixelsPerUnit) - visualCrustThickness;
				if (!TryGetBreadOuterBoundsForY(activeMedium, activeBoard, visualCrustThickness, y, out float minX, out float maxX))
				{
					continue;
				}

				for (int px = 0; px < textureWidth; px++)
				{
					float x = ((px + 0.5f) / pixelsPerUnit) - visualCrustThickness;
					if (x < minX || x > maxX)
					{
						continue;
					}

					float outerEdgeDistance = Mathf.Min(x - minX, maxX - x, y + visualCrustThickness, activeBoard.Height + visualCrustThickness - y);
					float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(outerEdgeDistance / outerFeather));
					if (alpha <= 0f)
					{
						continue;
					}

					Color color = EvaluateToastSliceColor(activeMedium, activeBoard, visualCrustThickness, x, y, outerEdgeDistance);
					color.a = alpha;
					pixels[(py * textureWidth) + px] = color;
				}
			}

			_generatedCrustTexture.SetPixels32(pixels);
			_generatedCrustTexture.Apply(false, false);
			_generatedCrustSprite = Sprite.Create(
				_generatedCrustTexture,
				new Rect(0f, 0f, textureWidth, textureHeight),
				new Vector2(0.5f, 0.5f),
				pixelsPerUnit,
				0,
				SpriteMeshType.FullRect);
			_generatedCrustRenderer.sprite = _generatedCrustSprite;
			_generatedCrustCacheKey = cacheKey;
		}

		private void PositionGeneratedCrustRenderer(GameBoard activeBoard)
		{
			if (_generatedCrustRenderer == null)
			{
				return;
			}

			_generatedCrustRenderer.transform.localPosition = new Vector3(activeBoard.Width * 0.5f, activeBoard.Height * 0.5f, 0f);
			_generatedCrustRenderer.transform.localRotation = Quaternion.identity;
			_generatedCrustRenderer.transform.localScale = Vector3.one;
		}

		private static int GetBackdropPixelsPerUnit(GameBoard activeBoard, float visualCrustThickness)
		{
			float fullWidth = activeBoard.Width + (visualCrustThickness * 2f);
			float fullHeight = activeBoard.Height + (visualCrustThickness * 2f);
			float longestSide = Mathf.Max(fullWidth, fullHeight);
			int pixelsPerUnit = Mathf.FloorToInt(2048f / Mathf.Max(1f, longestSide));
			return Mathf.Clamp(pixelsPerUnit, 8, 24);
		}

		private static string BuildGeneratedCrustCacheKey(GameBoard activeBoard, BoardMediumConfig activeMedium, float visualCrustThickness)
		{
			return string.Join("|",
				activeBoard.Width,
				activeBoard.Height,
				visualCrustThickness,
				activeMedium.mediumId,
				activeMedium.topCrustRoundness,
				activeMedium.bottomCrustRoundness,
				activeMedium.crustInnerColor,
				activeMedium.crustMidColor,
				activeMedium.crustOuterColor,
				activeMedium.crustTopDarkening,
				activeMedium.crustColorVariation,
				activeMedium.minVisualCrustThickness,
				activeMedium.maxVisualCrustThickness);
		}

		private void DestroyGeneratedCrustAssets()
		{
			if (_generatedCrustSprite != null)
			{
				UnityEngine.Object.Destroy(_generatedCrustSprite);
				_generatedCrustSprite = null;
			}

			if (_generatedCrustTexture != null)
			{
				UnityEngine.Object.Destroy(_generatedCrustTexture);
				_generatedCrustTexture = null;
			}
		}

		private static bool TryGetBreadOuterBoundsForY(BoardMediumConfig activeMedium, GameBoard activeBoard, float visualCrustThickness, float y, out float minX, out float maxX)
		{
			minX = 0f;
			maxX = 0f;

			if (y < -visualCrustThickness || y > activeBoard.Height + visualCrustThickness)
			{
				return false;
			}

			float horizontalOverhang = 0f;
			float sideBulge = 0f;
			float inset = 0f;
			if (activeMedium.useBreadSliceSilhouette && visualCrustThickness > 0f)
			{
				horizontalOverhang = Mathf.Min(visualCrustThickness * 0.45f, activeBoard.Width * 0.045f);

				float fullHeight = activeBoard.Height + (visualCrustThickness * 2f);
				float verticalProgress = Mathf.Clamp01((y + visualCrustThickness) / Mathf.Max(0.001f, fullHeight));
				float shoulderCurve = Mathf.Sin(verticalProgress * Mathf.PI);
				float maxBulge = Mathf.Min(visualCrustThickness * 0.55f, activeBoard.Width * 0.05f);
				sideBulge = shoulderCurve * shoulderCurve * maxBulge;

				if (y > activeBoard.Height)
				{
					float topProgress = Mathf.Clamp01((y - activeBoard.Height) / Mathf.Max(0.001f, visualCrustThickness));
					float maxTopInset = Mathf.Max(activeMedium.topCrustRoundness * visualCrustThickness, activeBoard.Width * 0.1f * activeMedium.topCrustRoundness);
					inset += Mathf.Pow(topProgress, 1.55f) * maxTopInset;
				}

				float bottomShoulderDepth = Mathf.Max(visualCrustThickness * 1.05f, activeBoard.Height * 0.07f);
				float bottomEndY = bottomShoulderDepth;
				if (y <= bottomEndY)
				{
					float bottomProgress = Mathf.Clamp01((bottomEndY - y) / Mathf.Max(0.001f, bottomEndY + visualCrustThickness));
					float maxBottomInset = Mathf.Max(activeMedium.bottomCrustRoundness * visualCrustThickness * 1.1f, activeBoard.Width * 0.05f * activeMedium.bottomCrustRoundness);
					inset += Mathf.Pow(bottomProgress, 1.9f) * maxBottomInset;
				}
			}

			minX = -visualCrustThickness - horizontalOverhang - sideBulge + inset;
			maxX = activeBoard.Width + visualCrustThickness + horizontalOverhang + sideBulge - inset;
			return minX < maxX;
		}

		private static Color EvaluateToastSliceColor(BoardMediumConfig activeMedium, GameBoard activeBoard, float visualCrustThickness, float x, float y, float outerEdgeDistance)
		{
			float crustBlend = visualCrustThickness <= 0f
				? 1f
				: Mathf.Clamp01(outerEdgeDistance / visualCrustThickness);
			Color crustGradientColor = crustBlend < 0.35f
				? Color.Lerp(activeMedium.crustOuterColor, activeMedium.crustMidColor, crustBlend / 0.35f)
				: crustBlend < 0.75f
					? Color.Lerp(activeMedium.crustMidColor, activeMedium.crustInnerColor, (crustBlend - 0.35f) / 0.4f)
					: Color.Lerp(activeMedium.crustInnerColor, activeMedium.breadShadeColor, (crustBlend - 0.75f) / 0.25f);

			float interiorMix = visualCrustThickness <= 0f
				? 1f
				: Mathf.Clamp01((outerEdgeDistance - visualCrustThickness) / Mathf.Max(0.001f, visualCrustThickness * 1.4f));
			Color breadColor = Color.Lerp(activeMedium.breadShadeColor, activeMedium.breadInteriorColor, interiorMix);

			float blendZoneHalf = visualCrustThickness * 0.12f;
			float crustToBreadBlend = Mathf.SmoothStep(0f, 1f,
				Mathf.Clamp01((outerEdgeDistance - (visualCrustThickness - blendZoneHalf)) / Mathf.Max(0.001f, blendZoneHalf * 2f)));
			Color finalColor = Color.Lerp(crustGradientColor, breadColor, crustToBreadBlend);

			float variationStrength = Mathf.Clamp(activeMedium.crustColorVariation, 0f, 0.2f);
			if (variationStrength > 0f)
			{
				float coarseVariation = EvaluateCoordinateNoise(activeMedium, Mathf.RoundToInt(x * 5f), Mathf.RoundToInt(y * 5f));
				float fineVariation = EvaluateCoordinateNoise(activeMedium, Mathf.RoundToInt(x * 14f) + 73, Mathf.RoundToInt(y * 13f) + 101);
				float variation = coarseVariation * 0.6f + fineVariation * 0.4f;
				float brightness = 1f + ((variation * 2f) - 1f) * variationStrength;
				finalColor *= brightness;
				finalColor.a = 1f;
			}

			float breadVariationStrength = Mathf.Clamp(activeMedium.breadColorVariation, 0f, 0.15f);
			if (breadVariationStrength > 0f && outerEdgeDistance >= visualCrustThickness * 0.9f)
			{
				float coarseVariation = EvaluateCoordinateNoise(activeMedium, Mathf.RoundToInt(x * 3f) + 187, Mathf.RoundToInt(y * 4f) + 911);
				float fineVariation = EvaluateCoordinateNoise(activeMedium, Mathf.RoundToInt(x * 9f) + 421, Mathf.RoundToInt(y * 8f) + 647);
				float variation = coarseVariation * 0.65f + fineVariation * 0.35f;
				float breadBlendWeight = Mathf.Clamp01((outerEdgeDistance - visualCrustThickness * 0.9f) / Mathf.Max(0.001f, visualCrustThickness * 0.3f));
				float brightness = 1f + ((variation * 2f) - 1f) * breadVariationStrength * breadBlendWeight;
				finalColor *= brightness;
				finalColor.a = 1f;
			}

			float verticalShade = Mathf.Clamp01((y + visualCrustThickness) / Mathf.Max(0.001f, activeBoard.Height + (visualCrustThickness * 2f)));
			finalColor = Color.Lerp(finalColor, activeMedium.breadShadeColor, (1f - verticalShade) * 0.08f);

			if (y > activeBoard.Height)
			{
				float topDepth = visualCrustThickness <= 0f
					? 0f
					: Mathf.Clamp01((y - activeBoard.Height) / visualCrustThickness);
				finalColor = Color.Lerp(finalColor, activeMedium.crustOuterColor, topDepth * activeMedium.crustTopDarkening);
			}

			return finalColor;
		}

		private static float EvaluateCoordinateNoise(BoardMediumConfig activeMedium, int x, int y)
		{
			unchecked
			{
				uint hash = 2166136261u;
				hash = (hash ^ (uint)x) * 16777619u;
				hash = (hash ^ (uint)y) * 16777619u;
				string mediumId = activeMedium?.mediumId;
				if (!string.IsNullOrEmpty(mediumId))
				{
					for (int i = 0; i < mediumId.Length; i++)
					{
						hash = (hash ^ mediumId[i]) * 16777619u;
					}
				}

				return (hash & 1023u) / 1023f;
			}
		}
	}

	internal sealed class GridCellStateAnimationController
	{
		private sealed class ExpiringToxinVisualSnapshot
		{
			public ExpiringToxinVisualSnapshot(int tileId, TileBase moldTile, Color moldColor, TileBase overlayTile, Color overlayColor)
			{
				TileId = tileId;
				MoldTile = moldTile;
				MoldColor = moldColor;
				OverlayTile = overlayTile;
				OverlayColor = overlayColor;
			}

			public int TileId { get; }
			public TileBase MoldTile { get; }
			public Color MoldColor { get; }
			public TileBase OverlayTile { get; }
			public Color OverlayColor { get; }
		}

		private readonly Func<GameBoard> _getBoard;
		private readonly Func<Tilemap> _getMoldTilemap;
		private readonly Func<Tilemap> _getOverlayTilemap;
		private readonly Func<Tilemap> _getTransientTilemap;
		private readonly Func<int, Vector3Int> _getPositionForTileId;
		private readonly Func<int, TileBase> _getTileForPlayer;
		private readonly Func<TileBase> _getToxinOverlayTile;
		private readonly Func<IEnumerator, Coroutine> _startCoroutine;
		private readonly Action<Coroutine> _stopCoroutine;
		private readonly Action _beginAnimation;
		private readonly Action _endAnimation;

		private readonly HashSet<int> _newlyGrownTileIds = new();
		private readonly HashSet<int> _newlyGrownAnimationPlayedTileIds = new();
		private readonly Dictionary<int, Coroutine> _fadeInCoroutines = new();
		private readonly HashSet<int> _dyingTileIds = new();
		private readonly Dictionary<int, Coroutine> _deathAnimationCoroutines = new();
		private readonly HashSet<int> _toxinDropTileIds = new();
		private readonly Dictionary<int, Coroutine> _toxinDropCoroutines = new();
		private readonly Dictionary<int, ExpiringToxinVisualSnapshot> _pendingToxinExpirySnapshots = new();
		private readonly Dictionary<int, Coroutine> _toxinExpiryCoroutines = new();
		private readonly Dictionary<int, Coroutine> _chemobeaconExpiryCoroutines = new();

		public GridCellStateAnimationController(
			Func<GameBoard> getBoard,
			Func<Tilemap> getMoldTilemap,
			Func<Tilemap> getOverlayTilemap,
			Func<Tilemap> getTransientTilemap,
			Func<int, Vector3Int> getPositionForTileId,
			Func<int, TileBase> getTileForPlayer,
			Func<TileBase> getToxinOverlayTile,
			Func<IEnumerator, Coroutine> startCoroutine,
			Action<Coroutine> stopCoroutine,
			Action beginAnimation,
			Action endAnimation)
		{
			_getBoard = getBoard;
			_getMoldTilemap = getMoldTilemap;
			_getOverlayTilemap = getOverlayTilemap;
			_getTransientTilemap = getTransientTilemap;
			_getPositionForTileId = getPositionForTileId;
			_getTileForPlayer = getTileForPlayer;
			_getToxinOverlayTile = getToxinOverlayTile;
			_startCoroutine = startCoroutine;
			_stopCoroutine = stopCoroutine;
			_beginAnimation = beginAnimation;
			_endAnimation = endAnimation;
		}

		public void PrepareForBoardRender(GameBoard board, bool suppressAnimations)
		{
			if (suppressAnimations)
			{
				ClearPendingToxinExpirySnapshots();
			}

			StopAndClearFadeInAnimations();
			StopAndClearDeathAnimations();
			StopAndClearToxinDropAnimations();
			StopAndClearToxinExpiryAnimations();

			_newlyGrownTileIds.Clear();
			_dyingTileIds.Clear();
			_toxinDropTileIds.Clear();

			if (board == null)
			{
				if (suppressAnimations)
				{
					_newlyGrownAnimationPlayedTileIds.Clear();
				}
				return;
			}

			if (suppressAnimations)
			{
				foreach (var tile in board.AllTiles())
				{
					var cell = tile.FungalCell;
					if (cell == null)
					{
						continue;
					}

					if (cell.IsNewlyGrown)
					{
						cell.ClearNewlyGrownFlag();
					}

					if (cell.IsDying)
					{
						cell.ClearDyingFlag();
					}

					if (cell.IsReceivingToxinDrop)
					{
						cell.ClearToxinDropFlag();
					}
				}

				_newlyGrownAnimationPlayedTileIds.Clear();
				return;
			}

			for (int x = 0; x < board.Width; x++)
			{
				for (int y = 0; y < board.Height; y++)
				{
					var tile = board.Grid[x, y];
					if (tile.FungalCell?.IsNewlyGrown == true)
					{
						_newlyGrownTileIds.Add(tile.TileId);
					}

					if (tile.FungalCell?.IsDying == true)
					{
						_dyingTileIds.Add(tile.TileId);
					}

					if (tile.FungalCell?.IsReceivingToxinDrop == true)
					{
						_toxinDropTileIds.Add(tile.TileId);
					}
				}
			}
		}

		public void StartQueuedAnimations()
		{
			StartFadeInAnimations();
			StartDeathAnimations();
			StartToxinDropAnimations();
			StartPendingToxinExpiryAnimations();
		}

		public float GetAliveCellAlpha(int tileId, FungalCell cell)
		{
			if (cell == null)
			{
				return 1f;
			}

			if (cell.IsNewlyGrown)
			{
				return _newlyGrownAnimationPlayedTileIds.Contains(tileId)
					? UIEffectConstants.NewGrowthFinalAlpha
					: UIEffectConstants.CellGrowthFadeInStartAlpha;
			}

			return cell.GrowthCycleAge < UIEffectConstants.GrowthCycleAgeHighlightTextThreshold
				? UIEffectConstants.NewGrowthFinalAlpha
				: 1f;
		}

		public void ClearPendingToxinExpirySnapshots()
		{
			_pendingToxinExpirySnapshots.Clear();
		}

		public void StopAndClearToxinExpiryAnimations()
		{
			foreach (int tileId in _toxinExpiryCoroutines.Keys.ToList())
			{
				CancelToxinExpiryAnimation(tileId);
			}

			_toxinExpiryCoroutines.Clear();
		}

		public void StopAndClearChemobeaconExpiryAnimations()
		{
			foreach (int tileId in _chemobeaconExpiryCoroutines.Keys.ToList())
			{
				CancelChemobeaconExpiryAnimation(tileId);
			}

			_chemobeaconExpiryCoroutines.Clear();
		}

		public void ResetRuntimeState()
		{
			ClearPendingToxinExpirySnapshots();
			StopAndClearFadeInAnimations();
			StopAndClearDeathAnimations();
			StopAndClearToxinDropAnimations();
			StopAndClearToxinExpiryAnimations();
			StopAndClearChemobeaconExpiryAnimations();

			_newlyGrownTileIds.Clear();
			_newlyGrownAnimationPlayedTileIds.Clear();
			_dyingTileIds.Clear();
			_toxinDropTileIds.Clear();
		}

		public void Dispose()
		{
			ResetRuntimeState();
		}

		public void CaptureToxinExpirySnapshot(ToxinExpiredEventArgs eventArgs)
		{
			var pos = _getPositionForTileId(eventArgs.TileId);
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();

			TileBase moldTile = null;
			Color moldColor = Color.white;
			if (moldTilemap != null)
			{
				moldTile = moldTilemap.GetTile(pos);
				if (moldTile != null)
				{
					moldColor = moldTilemap.GetColor(pos);
				}
			}

			if (moldTile == null && eventArgs.ToxinOwnerPlayerId is int ownerPlayerId)
			{
				moldTile = _getTileForPlayer(ownerPlayerId);
			}

			TileBase overlayTile = _getToxinOverlayTile();
			Color overlayColor = Color.white;
			if (overlayTilemap != null)
			{
				var currentOverlayTile = overlayTilemap.GetTile(pos);
				if (currentOverlayTile != null)
				{
					overlayTile = currentOverlayTile;
					overlayColor = overlayTilemap.GetColor(pos);
				}
			}

			if (moldTile == null && overlayTile == null)
			{
				return;
			}

			_pendingToxinExpirySnapshots[eventArgs.TileId] = new ExpiringToxinVisualSnapshot(
				eventArgs.TileId,
				moldTile,
				moldColor,
				overlayTile,
				overlayColor);
		}

		public void CancelChemobeaconExpiryAnimation(int tileId)
		{
			if (_chemobeaconExpiryCoroutines.TryGetValue(tileId, out var coroutine) && coroutine != null)
			{
				_stopCoroutine(coroutine);
				_chemobeaconExpiryCoroutines.Remove(tileId);
				_endAnimation();
			}

			ClearChemobeaconTransientVisual(tileId);
		}

		public void StartChemobeaconExpiryAnimation(int playerId, int tileId)
		{
			CancelChemobeaconExpiryAnimation(tileId);
			_chemobeaconExpiryCoroutines[tileId] = _startCoroutine(PlayChemobeaconEvaporationAnimation(playerId, tileId));
		}

		public void ClearNewlyGrownFlagsForNextGrowthPhase()
		{
			var board = _getBoard();
			if (board == null)
			{
				return;
			}

			foreach (var tile in board.AllTiles())
			{
				var cell = tile.FungalCell;
				if (cell?.IsNewlyGrown == true)
				{
					cell.ClearNewlyGrownFlag();
				}
			}

			_newlyGrownTileIds.Clear();
			_newlyGrownAnimationPlayedTileIds.Clear();
		}

		public void TriggerDeathAnimation(int tileId)
		{
			var board = _getBoard();
			var tile = board?.GetTileById(tileId);
			if (tile?.FungalCell == null)
			{
				return;
			}

			tile.FungalCell.MarkAsDying();
			_dyingTileIds.Add(tileId);
			StopTrackedAnimation(_deathAnimationCoroutines, tileId);
			_deathAnimationCoroutines[tileId] = _startCoroutine(DeathAnimation(tileId));
		}

		public void TriggerToxinDropAnimation(int tileId)
		{
			var board = _getBoard();
			var tile = board?.GetTileById(tileId);
			if (tile?.FungalCell == null)
			{
				return;
			}

			tile.FungalCell.MarkAsReceivingToxinDrop();
			_toxinDropTileIds.Add(tileId);
			StopTrackedAnimation(_toxinDropCoroutines, tileId);
			_toxinDropCoroutines[tileId] = _startCoroutine(ToxinDropAnimation(tileId));
		}

		private void StartPendingToxinExpiryAnimations()
		{
			if (_pendingToxinExpirySnapshots.Count == 0)
			{
				return;
			}

			var board = _getBoard();
			var pendingSnapshots = _pendingToxinExpirySnapshots.Values.ToList();
			_pendingToxinExpirySnapshots.Clear();

			foreach (var snapshot in pendingSnapshots)
			{
				var tile = board?.GetTileById(snapshot.TileId);
				if (tile?.FungalCell?.IsToxin == true)
				{
					continue;
				}

				CancelToxinExpiryAnimation(snapshot.TileId);
				_toxinExpiryCoroutines[snapshot.TileId] = _startCoroutine(ToxinExpiryDissolveAnimation(snapshot));
			}
		}

		private void StartFadeInAnimations()
		{
			foreach (int tileId in _newlyGrownTileIds)
			{
				if (_newlyGrownAnimationPlayedTileIds.Contains(tileId))
				{
					continue;
				}

				StopTrackedAnimation(_fadeInCoroutines, tileId);
				_fadeInCoroutines[tileId] = _startCoroutine(FadeInCell(tileId));
				_newlyGrownAnimationPlayedTileIds.Add(tileId);
			}
		}

		private IEnumerator FadeInCell(int tileId)
		{
			var board = _getBoard();
			var moldTilemap = _getMoldTilemap();
			if (board == null || moldTilemap == null)
			{
				_fadeInCoroutines.Remove(tileId);
				yield break;
			}

			var xy = board.GetXYFromTileId(tileId);
			var pos = new Vector3Int(xy.Item1, xy.Item2, 0);
			float duration = UIEffectConstants.CellGrowthFadeInDurationSeconds;
			float settleDuration = UIEffectConstants.CellGrowthSettleDurationSeconds;
			float startAlpha = UIEffectConstants.CellGrowthFadeInStartAlpha;
			float targetAlpha = 1f;
			float elapsed = 0f;

			_beginAnimation();
			try
			{
				while (elapsed < duration)
				{
					elapsed += Time.deltaTime;
					float t = duration <= 0f ? 1f : elapsed / duration;
					float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

					if (moldTilemap.HasTile(pos))
					{
						Color currentColor = moldTilemap.GetColor(pos);
						currentColor.a = currentAlpha;
						moldTilemap.SetColor(pos, currentColor);
					}

					yield return null;
				}

				if (moldTilemap.HasTile(pos))
				{
					Color finalColor = moldTilemap.GetColor(pos);
					finalColor.a = 1f;
					moldTilemap.SetColor(pos, finalColor);
				}

				float settleElapsed = 0f;
				while (settleElapsed < settleDuration)
				{
					settleElapsed += Time.deltaTime;
					if (moldTilemap.HasTile(pos))
					{
						Color settleColor = moldTilemap.GetColor(pos);
						float t = settleDuration <= 0f ? 1f : Mathf.Clamp01(settleElapsed / settleDuration);
						settleColor.a = Mathf.Lerp(1f, UIEffectConstants.NewGrowthFinalAlpha, t);
						moldTilemap.SetColor(pos, settleColor);
					}
					yield return null;
				}

				if (moldTilemap.HasTile(pos))
				{
					Color settleColor = Color.white;
					settleColor.a = UIEffectConstants.NewGrowthFinalAlpha;
					moldTilemap.SetColor(pos, settleColor);
				}
			}
			finally
			{
				_fadeInCoroutines.Remove(tileId);
				_endAnimation();
			}
		}

		private void StartDeathAnimations()
		{
			foreach (int tileId in _dyingTileIds)
			{
				StopTrackedAnimation(_deathAnimationCoroutines, tileId);
				_deathAnimationCoroutines[tileId] = _startCoroutine(DeathAnimation(tileId));
			}
		}

		private IEnumerator DeathAnimation(int tileId)
		{
			var board = _getBoard();
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();
			if (board == null || moldTilemap == null || overlayTilemap == null)
			{
				_deathAnimationCoroutines.Remove(tileId);
				yield break;
			}

			var xy = board.GetXYFromTileId(tileId);
			var pos = new Vector3Int(xy.Item1, xy.Item2, 0);
			float duration = UIEffectConstants.CellDeathAnimationDurationSeconds;

			var tile = board.GetTileById(tileId);
			var cell = tile?.FungalCell;
			if (cell == null)
			{
				_deathAnimationCoroutines.Remove(tileId);
				yield break;
			}

			_beginAnimation();
			try
			{
				Color initialLivingColor = moldTilemap.HasTile(pos) ? moldTilemap.GetColor(pos) : Color.white;
				Color initialOverlayColor = overlayTilemap.HasTile(pos) ? overlayTilemap.GetColor(pos) : Color.white;
				Color deathFlashColor = new(1f, 0.2f, 0.2f, 1f);

				float flashDuration = duration * 0.15f;
				float flashElapsed = 0f;
				while (flashElapsed < flashDuration)
				{
					flashElapsed += Time.deltaTime;
					float flashProgress = flashElapsed / flashDuration;
					Color flashColor = Color.Lerp(deathFlashColor, initialLivingColor, flashProgress);

					if (moldTilemap.HasTile(pos))
					{
						moldTilemap.SetColor(pos, flashColor);
					}

					yield return null;
				}

				float mainDuration = duration - flashDuration;
				float mainElapsed = 0f;
				while (mainElapsed < mainDuration)
				{
					mainElapsed += Time.deltaTime;
					float progress = mainElapsed / mainDuration;
					float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
					float scaleAmount = Mathf.Lerp(1f, 0.85f, easedProgress);
					var scaleMatrix = Matrix4x4.Scale(new Vector3(scaleAmount, scaleAmount, 1f));
					Color currentLivingColor = Color.Lerp(
						initialLivingColor,
						new Color(
							initialLivingColor.r * 0.7f,
							initialLivingColor.g * 0.7f,
							initialLivingColor.b * 0.7f,
							Mathf.Lerp(1f, 0.8f, easedProgress)),
						easedProgress);

					if (moldTilemap.HasTile(pos))
					{
						moldTilemap.SetColor(pos, currentLivingColor);
						moldTilemap.SetTransformMatrix(pos, scaleMatrix);
					}

					if (overlayTilemap.HasTile(pos))
					{
						Color overlayColor = initialOverlayColor;
						overlayColor.a = Mathf.Lerp(0f, 1f, easedProgress);
						overlayTilemap.SetColor(pos, overlayColor);
					}

					yield return null;
				}

				if (moldTilemap.HasTile(pos))
				{
					Color finalLivingColor = initialLivingColor;
					finalLivingColor.a = 0.8f;
					moldTilemap.SetColor(pos, finalLivingColor);
					moldTilemap.SetTransformMatrix(pos, Matrix4x4.Scale(new Vector3(0.85f, 0.85f, 1f)));
				}

				if (overlayTilemap.HasTile(pos))
				{
					Color finalOverlayColor = initialOverlayColor;
					finalOverlayColor.a = 1f;
					overlayTilemap.SetColor(pos, finalOverlayColor);
				}

				cell.ClearDyingFlag();
			}
			finally
			{
				_deathAnimationCoroutines.Remove(tileId);
				_endAnimation();
			}
		}

		private void StartToxinDropAnimations()
		{
			foreach (int tileId in _toxinDropTileIds)
			{
				StopTrackedAnimation(_toxinDropCoroutines, tileId);
				_toxinDropCoroutines[tileId] = _startCoroutine(ToxinDropAnimation(tileId));
			}
		}

		private IEnumerator ToxinDropAnimation(int tileId)
		{
			var board = _getBoard();
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();
			if (board == null || moldTilemap == null || overlayTilemap == null)
			{
				_toxinDropCoroutines.Remove(tileId);
				yield break;
			}

			var xy = board.GetXYFromTileId(tileId);
			var pos = new Vector3Int(xy.Item1, xy.Item2, 0);
			float duration = UIEffectConstants.ToxinDropAnimationDurationSeconds;

			var tile = board.GetTileById(tileId);
			var cell = tile?.FungalCell;
			if (cell == null)
			{
				_toxinDropCoroutines.Remove(tileId);
				yield break;
			}

			_beginAnimation();
			try
			{
				if (!overlayTilemap.HasTile(pos) && _getToxinOverlayTile() != null)
				{
					overlayTilemap.SetTile(pos, _getToxinOverlayTile());
					overlayTilemap.SetTileFlags(pos, TileFlags.None);
					overlayTilemap.SetColor(pos, Color.clear);
				}

				if (moldTilemap.HasTile(pos))
				{
					var moldColor = moldTilemap.GetColor(pos);
					moldColor.a = 0f;
					moldTilemap.SetColor(pos, moldColor);
				}

				float startYOffset = UIEffectConstants.ToxinDropStartYOffset;
				ApplyOverlayTransform(overlayTilemap, pos, new Vector3(0f, startYOffset, 0f), Vector3.one);

				float approachPortion = Mathf.Clamp01(UIEffectConstants.ToxinDropApproachPortion);
				float approachDuration = duration * approachPortion;
				float approachElapsed = 0f;
				while (approachElapsed < approachDuration)
				{
					approachElapsed += Time.deltaTime;
					float t = Mathf.Clamp01(approachElapsed / approachDuration);
					float eased = t * t * t;
					float yOffset = Mathf.Lerp(startYOffset, 0f, eased);
					ApplyOverlayTransform(overlayTilemap, pos, new Vector3(0f, yOffset, 0f), Vector3.one);
					yield return null;
				}

				float squashX = UIEffectConstants.ToxinDropImpactSquashX;
				float squashY = UIEffectConstants.ToxinDropImpactSquashY;
				float impactPortion = 1f - approachPortion;
				float impactDuration = duration * impactPortion * 0.35f;
				float settleDuration = duration * impactPortion - impactDuration;

				float impactElapsed = 0f;
				while (impactElapsed < impactDuration)
				{
					impactElapsed += Time.deltaTime;
					float t = Mathf.Clamp01(impactElapsed / impactDuration);
					float sx = Mathf.Lerp(1f, squashX, 1f - (1f - t) * (1f - t));
					float sy = Mathf.Lerp(1f, squashY, 1f - (1f - t) * (1f - t));
					ApplyOverlayTransform(overlayTilemap, pos, Vector3.zero, new Vector3(sx, sy, 1f));

					if (overlayTilemap.HasTile(pos))
					{
						Color overlayColor = overlayTilemap.GetColor(pos);
						overlayColor.a = Mathf.Lerp(0f, 1f, t);
						overlayTilemap.SetColor(pos, overlayColor);
					}

					yield return null;
				}

				float settleElapsed = 0f;
				while (settleElapsed < settleDuration)
				{
					settleElapsed += Time.deltaTime;
					float t = Mathf.Clamp01(settleElapsed / settleDuration);
					float sx = Mathf.Lerp(squashX, 1f, t);
					float sy = Mathf.Lerp(squashY, 1f, t);
					ApplyOverlayTransform(overlayTilemap, pos, Vector3.zero, new Vector3(sx, sy, 1f));
					yield return null;
				}

				ApplyOverlayTransform(overlayTilemap, pos, Vector3.zero, Vector3.one);
				if (overlayTilemap.HasTile(pos))
				{
					overlayTilemap.SetColor(pos, Color.white);
				}

				if (moldTilemap.HasTile(pos))
				{
					moldTilemap.SetColor(pos, new Color(0.8f, 0.8f, 0.8f, 0.8f));
				}

				cell.ClearToxinDropFlag();
			}
			finally
			{
				_toxinDropCoroutines.Remove(tileId);
				_endAnimation();
			}
		}

		private void StopAndClearFadeInAnimations()
		{
			StopTrackedAnimations(_fadeInCoroutines);
		}

		private void StopAndClearDeathAnimations()
		{
			StopTrackedAnimations(_deathAnimationCoroutines);
		}

		private void StopAndClearToxinDropAnimations()
		{
			StopTrackedAnimations(_toxinDropCoroutines);
		}

		private void StopTrackedAnimations(Dictionary<int, Coroutine> coroutines)
		{
			foreach (var coroutine in coroutines.Values)
			{
				if (coroutine != null)
				{
					_stopCoroutine(coroutine);
					_endAnimation();
				}
			}

			coroutines.Clear();
		}

		private void StopTrackedAnimation(Dictionary<int, Coroutine> coroutines, int tileId)
		{
			if (coroutines.TryGetValue(tileId, out var coroutine) && coroutine != null)
			{
				_stopCoroutine(coroutine);
				coroutines.Remove(tileId);
				_endAnimation();
			}
		}

		private IEnumerator PlayChemobeaconEvaporationAnimation(int playerId, int tileId)
		{
			var board = _getBoard();
			var targetTilemap = _getTransientTilemap();
			TileBase chemobeaconTile = _getTileForPlayer(playerId);
			if (board == null || targetTilemap == null || chemobeaconTile == null)
			{
				_chemobeaconExpiryCoroutines.Remove(tileId);
				yield break;
			}

			Vector3Int pos = _getPositionForTileId(tileId);
			targetTilemap.SetTile(pos, chemobeaconTile);
			targetTilemap.SetTileFlags(pos, TileFlags.None);

			float duration = UIEffectConstants.ChemobeaconEvaporationDurationSeconds;
			_beginAnimation();
			try
			{
				float elapsed = 0f;
				while (elapsed < duration)
				{
					elapsed += Time.deltaTime;
					float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
					float eased = 1f - Mathf.Pow(1f - t, 3f);
					float scale = Mathf.Lerp(UIEffectConstants.ChemobeaconIdleScale, UIEffectConstants.ChemobeaconEvaporationFinalScale, eased);
					float lift = Mathf.Lerp(0f, UIEffectConstants.ChemobeaconEvaporationLiftWorld, eased);
					Color color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), eased);

					targetTilemap.SetColor(pos, color);
					targetTilemap.SetTransformMatrix(pos, Matrix4x4.TRS(new Vector3(0f, lift, 0f), Quaternion.identity, new Vector3(scale, scale, 1f)));
					yield return null;
				}
			}
			finally
			{
				_chemobeaconExpiryCoroutines.Remove(tileId);
				ClearChemobeaconTransientVisual(tileId);
				_endAnimation();
			}
		}

		private void CancelToxinExpiryAnimation(int tileId)
		{
			if (_toxinExpiryCoroutines.TryGetValue(tileId, out var coroutine) && coroutine != null)
			{
				_stopCoroutine(coroutine);
				_toxinExpiryCoroutines.Remove(tileId);
				_endAnimation();
			}

			ClearToxinExpiryVisualTile(tileId);
		}

		private IEnumerator ToxinExpiryDissolveAnimation(ExpiringToxinVisualSnapshot snapshot)
		{
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();
			if (moldTilemap == null || overlayTilemap == null)
			{
				_toxinExpiryCoroutines.Remove(snapshot.TileId);
				yield break;
			}

			Vector3Int pos = _getPositionForTileId(snapshot.TileId);
			bool shouldRenderMold = snapshot.MoldTile != null && !moldTilemap.HasTile(pos);
			bool shouldRenderOverlay = snapshot.OverlayTile != null && !overlayTilemap.HasTile(pos);

			if (!shouldRenderMold && !shouldRenderOverlay)
			{
				_toxinExpiryCoroutines.Remove(snapshot.TileId);
				yield break;
			}

			if (shouldRenderMold)
			{
				moldTilemap.SetTile(pos, snapshot.MoldTile);
				moldTilemap.SetTileFlags(pos, TileFlags.None);
				moldTilemap.SetColor(pos, snapshot.MoldColor);
				moldTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
				moldTilemap.RefreshTile(pos);
			}

			if (shouldRenderOverlay)
			{
				overlayTilemap.SetTile(pos, snapshot.OverlayTile);
				overlayTilemap.SetTileFlags(pos, TileFlags.None);
				overlayTilemap.SetColor(pos, snapshot.OverlayColor);
				overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
				overlayTilemap.RefreshTile(pos);
			}

			float duration = UIEffectConstants.ToxinExpiryDissolveDurationSeconds;
			float elapsed = 0f;

			_beginAnimation();
			try
			{
				while (elapsed < duration)
				{
					elapsed += Time.deltaTime;
					float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
					float eased = 1f - Mathf.Pow(1f - t, 3f);
					float flicker = 0.92f + 0.08f * Mathf.Sin((t * UIEffectConstants.ToxinExpiryDissolveFlickerFrequency) + snapshot.TileId * 0.71f);
					float alphaFactor = Mathf.Clamp01((1f - eased) * flicker);
					float scale = Mathf.Lerp(1f, UIEffectConstants.ToxinExpiryDissolveFinalScale, eased);
					float verticalLift = Mathf.Lerp(0f, UIEffectConstants.ToxinExpiryDissolveLiftWorld, eased);
					float rotation = Mathf.Sin(t * UIEffectConstants.ToxinExpiryDissolveFlickerFrequency) * UIEffectConstants.ToxinExpiryDissolveRotationDegrees * (1f - eased);
					var matrix = Matrix4x4.TRS(
						new Vector3(0f, verticalLift, 0f),
						Quaternion.Euler(0f, 0f, rotation),
						new Vector3(scale, scale, 1f));

					if (shouldRenderMold)
					{
						Color moldColor = Color.Lerp(
							snapshot.MoldColor,
							new Color(snapshot.MoldColor.r * 0.45f, snapshot.MoldColor.g * 0.32f, snapshot.MoldColor.b * 0.26f, 0f),
							eased);
						moldColor.a = snapshot.MoldColor.a * alphaFactor;
						moldTilemap.SetColor(pos, moldColor);
						moldTilemap.SetTransformMatrix(pos, matrix);
					}

					if (shouldRenderOverlay)
					{
						Color overlayColor = Color.Lerp(
							snapshot.OverlayColor,
							new Color(snapshot.OverlayColor.r * 0.3f, snapshot.OverlayColor.g * 0.26f, snapshot.OverlayColor.b * 0.2f, 0f),
							eased);
						overlayColor.a = snapshot.OverlayColor.a * alphaFactor;
						overlayTilemap.SetColor(pos, overlayColor);
						overlayTilemap.SetTransformMatrix(
							pos,
							Matrix4x4.TRS(
								new Vector3(0f, verticalLift, 0f),
								Quaternion.Euler(0f, 0f, rotation),
								Vector3.one * Mathf.Lerp(1f, UIEffectConstants.ToxinExpiryDissolveOverlayScale, eased)));
					}

					yield return null;
				}
			}
			finally
			{
				_toxinExpiryCoroutines.Remove(snapshot.TileId);
				ClearToxinExpiryVisualTile(snapshot.TileId);
				_endAnimation();
			}
		}

		private void ClearToxinExpiryVisualTile(int tileId)
		{
			Vector3Int pos = _getPositionForTileId(tileId);
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();

			if (moldTilemap != null)
			{
				moldTilemap.SetTile(pos, null);
				moldTilemap.SetColor(pos, Color.white);
				moldTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			}

			if (overlayTilemap != null)
			{
				overlayTilemap.SetTile(pos, null);
				overlayTilemap.SetColor(pos, Color.white);
				overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			}
		}

		private void ClearChemobeaconTransientVisual(int tileId)
		{
			var targetTilemap = _getTransientTilemap();
			if (targetTilemap == null)
			{
				return;
			}

			Vector3Int pos = _getPositionForTileId(tileId);
			targetTilemap.SetTile(pos, null);
			targetTilemap.SetColor(pos, Color.white);
			targetTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
		}

		private static void ApplyOverlayTransform(Tilemap overlayTilemap, Vector3Int pos, Vector3 localOffset, Vector3 localScale)
		{
			var transformMatrix = Matrix4x4.TRS(localOffset, Quaternion.identity, localScale);
			overlayTilemap.SetTransformMatrix(pos, transformMatrix);
		}
	}

	internal sealed class GridResistanceOverlayController
	{
		private readonly Func<GameBoard> _getBoard;
		private readonly Func<Tilemap> _getOverlayTilemap;
		private readonly Func<TileBase> _getShieldTile;
		private readonly Func<int, Vector3Int> _getPositionForTileId;
		private readonly Func<float> _getResistancePulseTotal;
		private readonly Func<float> _getPostGrowthDurationMultiplier;
		private readonly Func<IEnumerator, Coroutine> _startCoroutine;
		private readonly Action _beginAnimation;
		private readonly Action _endAnimation;
		private readonly Action<Vector3Int, float, float, Color, Tilemap> _drawRing;
		private readonly Action<Tilemap> _clearRing;
		private readonly Func<Tilemap> _getPingOverlayTilemap;
		private readonly Func<Tilemap> _getHoverOverlayTilemap;
		private readonly Func<TileBase> _getSolidHighlightTile;

		private readonly HashSet<int> _deferredResistanceOverlayTileIds = new();

		public GridResistanceOverlayController(
			Func<GameBoard> getBoard,
			Func<Tilemap> getOverlayTilemap,
			Func<TileBase> getShieldTile,
			Func<int, Vector3Int> getPositionForTileId,
			Func<float> getResistancePulseTotal,
			Func<float> getPostGrowthDurationMultiplier,
			Func<IEnumerator, Coroutine> startCoroutine,
			Action beginAnimation,
			Action endAnimation,
			Action<Vector3Int, float, float, Color, Tilemap> drawRing,
			Action<Tilemap> clearRing,
			Func<Tilemap> getPingOverlayTilemap,
			Func<Tilemap> getHoverOverlayTilemap,
			Func<TileBase> getSolidHighlightTile)
		{
			_getBoard = getBoard;
			_getOverlayTilemap = getOverlayTilemap;
			_getShieldTile = getShieldTile;
			_getPositionForTileId = getPositionForTileId;
			_getResistancePulseTotal = getResistancePulseTotal;
			_getPostGrowthDurationMultiplier = getPostGrowthDurationMultiplier;
			_startCoroutine = startCoroutine;
			_beginAnimation = beginAnimation;
			_endAnimation = endAnimation;
			_drawRing = drawRing;
			_clearRing = clearRing;
			_getPingOverlayTilemap = getPingOverlayTilemap;
			_getHoverOverlayTilemap = getHoverOverlayTilemap;
			_getSolidHighlightTile = getSolidHighlightTile;
		}

		public void ResetRuntimeState()
		{
			_deferredResistanceOverlayTileIds.Clear();
		}

		public bool ShouldRenderResistanceOverlay(int tileId, FungalCell cell)
		{
			return cell != null
				&& cell.CellType == FungalCellType.Alive
				&& cell.IsResistant
				&& _getShieldTile() != null
				&& !_deferredResistanceOverlayTileIds.Contains(tileId);
		}

		public void ClearResistanceOverlayTile(int tileId)
		{
			var overlayTilemap = _getOverlayTilemap();
			TileBase shieldTile = _getShieldTile();
			if (overlayTilemap == null)
			{
				return;
			}

			Vector3Int pos = _getPositionForTileId(tileId);
			overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			if (overlayTilemap.GetTile(pos) == shieldTile)
			{
				overlayTilemap.SetTile(pos, null);
				overlayTilemap.RefreshTile(pos);
			}
		}

		public void RestoreResistanceOverlayTile(int tileId)
		{
			var activeBoard = _getBoard();
			var overlayTilemap = _getOverlayTilemap();
			TileBase shieldTile = _getShieldTile();
			if (activeBoard == null || overlayTilemap == null || shieldTile == null)
			{
				return;
			}

			var tile = activeBoard.GetTileById(tileId);
			var cell = tile?.FungalCell;
			if (!ShouldRenderResistanceOverlay(tileId, cell))
			{
				return;
			}

			Vector3Int pos = _getPositionForTileId(tileId);
			overlayTilemap.SetTile(pos, shieldTile);
			overlayTilemap.SetTileFlags(pos, TileFlags.None);
			overlayTilemap.SetColor(pos, Color.white);
			overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			overlayTilemap.RefreshTile(pos);
		}

		public void DeferResistanceOverlayReveal(IReadOnlyList<int> tileIds)
		{
			if (tileIds == null || tileIds.Count == 0)
			{
				return;
			}

			foreach (int tileId in tileIds)
			{
				_deferredResistanceOverlayTileIds.Add(tileId);
				ClearResistanceOverlayTile(tileId);
			}
		}

		public void RevealDeferredResistanceOverlays(IReadOnlyList<int> tileIds)
		{
			if (tileIds == null || tileIds.Count == 0)
			{
				return;
			}

			foreach (int tileId in tileIds)
			{
				_deferredResistanceOverlayTileIds.Remove(tileId);
				RestoreResistanceOverlayTile(tileId);
			}
		}

		public IEnumerator ResistantDropAnimation(int tileId, float finalScale = 1f, float durationScale = 1f)
		{
			var activeBoard = _getBoard();
			var overlayTilemap = _getOverlayTilemap();
			TileBase shieldTile = _getShieldTile();
			if (activeBoard == null || overlayTilemap == null || shieldTile == null)
			{
				yield break;
			}

			Vector3Int pos = _getPositionForTileId(tileId);
			float total = UIEffectConstants.SurgicalInoculationDropDurationSeconds * Mathf.Max(0.01f, durationScale);
			float dropT = Mathf.Clamp01(UIEffectConstants.SurgicalInoculationDropPortion);
			float impactT = Mathf.Clamp01(UIEffectConstants.SurgicalInoculationImpactPortion);
			float settleT = Mathf.Clamp01(UIEffectConstants.SurgicalInoculationSettlePortion);
			float normSum = dropT + impactT + settleT;
			if (normSum <= 0f)
			{
				normSum = 1f;
			}
			dropT /= normSum;
			impactT /= normSum;
			settleT /= normSum;

			float dropDur = total * dropT;
			float impactDur = total * impactT;
			float settleDur = total * settleT;

			overlayTilemap.SetTile(pos, shieldTile);
			overlayTilemap.SetTileFlags(pos, TileFlags.None);
			overlayTilemap.SetColor(pos, Color.white);

			_beginAnimation();
			try
			{
				float startYOffset = UIEffectConstants.SurgicalInoculationDropStartYOffset;
				float startScale = UIEffectConstants.SurgicalInoculationDropStartScale * finalScale;
				float spinTurns = UIEffectConstants.SurgicalInoculationDropSpinTurns;

				float t = 0f;
				while (t < dropDur)
				{
					t += Time.deltaTime;
					float u = Mathf.Clamp01(t / dropDur);
					float eased = u * u * u;
					float yOff = Mathf.Lerp(startYOffset, 0f, eased);
					float s = Mathf.Lerp(startScale, finalScale, eased);
					float angle = Mathf.Lerp(0f, 360f * spinTurns, eased);
					var rotation = Quaternion.Euler(0f, 0f, angle);
					var transformMatrix = Matrix4x4.TRS(new Vector3(0f, yOff, 0f), rotation, new Vector3(s, s, 1f));
					overlayTilemap.SetTransformMatrix(pos, transformMatrix);
					yield return null;
				}

				float squashX = UIEffectConstants.SurgicalInoculationImpactSquashX * finalScale;
				float squashY = UIEffectConstants.SurgicalInoculationImpactSquashY * finalScale;
				t = 0f;
				_startCoroutine(ImpactRingPulse(pos));
				while (t < impactDur)
				{
					t += Time.deltaTime;
					float u = Mathf.Clamp01(t / impactDur);
					float eased = 1f - (1f - u) * (1f - u);
					float sx = Mathf.Lerp(finalScale, squashX, eased);
					float sy = Mathf.Lerp(finalScale, squashY, eased);
					var transformMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));
					overlayTilemap.SetTransformMatrix(pos, transformMatrix);
					yield return null;
				}

				t = 0f;
				while (t < settleDur)
				{
					t += Time.deltaTime;
					float u = Mathf.Clamp01(t / settleDur);
					float sx = Mathf.Lerp(squashX, finalScale, u);
					float sy = Mathf.Lerp(squashY, finalScale, u);
					var transformMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));
					overlayTilemap.SetTransformMatrix(pos, transformMatrix);
					yield return null;
				}

				overlayTilemap.SetTransformMatrix(
					pos,
					Mathf.Approximately(finalScale, 1f)
						? Matrix4x4.identity
						: Matrix4x4.Scale(new Vector3(finalScale, finalScale, 1f)));
			}
			finally
			{
				_deferredResistanceOverlayTileIds.Remove(tileId);
				RestoreResistanceOverlayTile(tileId);
				_endAnimation();
			}
		}

		public IEnumerator BastionResistantPulseAnimation(int tileId, float scaleMultiplier = 1f)
		{
			var activeBoard = _getBoard();
			var overlayTilemap = _getOverlayTilemap();
			TileBase shieldTile = _getShieldTile();
			if (activeBoard == null || overlayTilemap == null || shieldTile == null)
			{
				yield break;
			}

			var xy = activeBoard.GetXYFromTileId(tileId);
			Vector3Int pos = new(xy.Item1, xy.Item2, 0);
			_deferredResistanceOverlayTileIds.Add(tileId);
			ClearResistanceOverlayTile(tileId);

			float baseTotal = UIEffectConstants.MycelialBastionPulseDurationSeconds;
			float configuredTotal = _getResistancePulseTotal();
			float total = configuredTotal > 0f ? configuredTotal : baseTotal * _getPostGrowthDurationMultiplier();
			float outT = Mathf.Clamp01(UIEffectConstants.MycelialBastionPulseOutPortion);
			float inT = Mathf.Clamp01(UIEffectConstants.MycelialBastionPulseInPortion);
			float norm = outT + inT;
			if (norm <= 0f)
			{
				norm = 1f;
			}
			outT /= norm;
			inT /= norm;
			float outDur = total * outT;
			float inDur = total * inT;
			float baseMaxScale = Mathf.Max(1f, UIEffectConstants.MycelialBastionPulseMaxScale);
			float clampedScaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.1f, 10f);
			float maxScale = Mathf.Max(1f, baseMaxScale * clampedScaleMultiplier);
			float yPop = UIEffectConstants.MycelialBastionPulseYOffset * clampedScaleMultiplier;

			overlayTilemap.SetTile(pos, shieldTile);
			overlayTilemap.SetTileFlags(pos, TileFlags.None);
			overlayTilemap.SetColor(pos, Color.white);

			_beginAnimation();
			try
			{
				float t = 0f;
				while (t < outDur)
				{
					t += Time.deltaTime;
					float u = Mathf.Clamp01(t / outDur);
					float eased = 1f - (1f - u) * (1f - u);
					float s = Mathf.Lerp(1f, maxScale, eased);
					float y = Mathf.Lerp(0f, yPop, eased);
					var transformMatrix = Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
					overlayTilemap.SetTransformMatrix(pos, transformMatrix);
					yield return null;
				}

				t = 0f;
				while (t < inDur)
				{
					t += Time.deltaTime;
					float u = Mathf.Clamp01(t / inDur);
					float eased = u * u;
					float s = Mathf.Lerp(maxScale, 1f, eased);
					float y = Mathf.Lerp(yPop, 0f, eased);
					var transformMatrix = Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
					overlayTilemap.SetTransformMatrix(pos, transformMatrix);
					yield return null;
				}

				overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			}
			finally
			{
				_deferredResistanceOverlayTileIds.Remove(tileId);
				RestoreResistanceOverlayTile(tileId);
				_endAnimation();
			}
		}

		public void PlayResistancePulseBatchScaled(IReadOnlyList<int> tileIds, float scaleMultiplier)
		{
			if (tileIds == null || tileIds.Count == 0)
			{
				return;
			}

			foreach (int tileId in tileIds)
			{
				_startCoroutine(BastionResistantPulseAnimation(tileId, scaleMultiplier));
			}
		}

		public void PlayResistanceDropBatch(IReadOnlyList<int> tileIds, float finalScale)
		{
			if (tileIds == null || tileIds.Count == 0)
			{
				return;
			}

			foreach (int tileId in tileIds)
			{
				_startCoroutine(ResistantDropAnimation(tileId, finalScale));
			}
		}

		public IEnumerator ImpactRingPulse(Vector3Int centerPos)
		{
			var targetTilemap = _getPingOverlayTilemap() ?? _getHoverOverlayTilemap();
			if (targetTilemap == null || _getSolidHighlightTile() == null)
			{
				yield break;
			}

			float duration = UIEffectConstants.SurgicalInoculationRingPulseDurationSeconds;
			float maxRadius = 2.5f;
			float ringThickness = 0.6f;
			float startTime = Time.time;
			while (Time.time - startTime < duration)
			{
				float u = Mathf.Clamp01((Time.time - startTime) / duration);
				float radius = Mathf.Lerp(0.3f, maxRadius, u);
				Color ringColor = new(1f, 0.95f, 0.5f, 0.9f * (1f - u));
				_drawRing(centerPos, radius, ringThickness, ringColor, targetTilemap);
				yield return null;
			}

			_clearRing(targetTilemap);
		}
	}

	internal sealed class GridBoardStateRenderer
	{
		private readonly Func<GameBoard> _getActiveBoard;
		private readonly Func<Tilemap> _getMoldTilemap;
		private readonly Func<Tilemap> _getOverlayTilemap;
		private readonly Func<TileBase> _getGoldShieldOverlayTile;
		private readonly Func<TileBase> _getDeadTile;
		private readonly Func<TileBase> _getToxinOverlayTile;
		private readonly Func<int, Vector3Int> _getPositionForTileId;
		private readonly Func<int, Tile> _getTileForPlayer;
		private readonly Func<int, FungalCell, bool> _shouldRenderResistanceOverlay;
		private readonly Func<int, FungalCell, float> _getAliveCellAlpha;
		private readonly Func<int, bool> _isPreviewHiddenTile;
		private readonly Action<int> _removeTrackedNutrientTile;
		private readonly Action<BoardTile, Vector3Int> _renderNutrientPatchOverlay;

		public GridBoardStateRenderer(
			Func<GameBoard> getActiveBoard,
			Func<Tilemap> getMoldTilemap,
			Func<Tilemap> getOverlayTilemap,
			Func<TileBase> getGoldShieldOverlayTile,
			Func<TileBase> getDeadTile,
			Func<TileBase> getToxinOverlayTile,
			Func<int, Vector3Int> getPositionForTileId,
			Func<int, Tile> getTileForPlayer,
			Func<int, FungalCell, bool> shouldRenderResistanceOverlay,
			Func<int, FungalCell, float> getAliveCellAlpha,
			Func<int, bool> isPreviewHiddenTile,
			Action<int> removeTrackedNutrientTile,
			Action<BoardTile, Vector3Int> renderNutrientPatchOverlay)
		{
			_getActiveBoard = getActiveBoard;
			_getMoldTilemap = getMoldTilemap;
			_getOverlayTilemap = getOverlayTilemap;
			_getGoldShieldOverlayTile = getGoldShieldOverlayTile;
			_getDeadTile = getDeadTile;
			_getToxinOverlayTile = getToxinOverlayTile;
			_getPositionForTileId = getPositionForTileId;
			_getTileForPlayer = getTileForPlayer;
			_shouldRenderResistanceOverlay = shouldRenderResistanceOverlay;
			_getAliveCellAlpha = getAliveCellAlpha;
			_isPreviewHiddenTile = isPreviewHiddenTile;
			_removeTrackedNutrientTile = removeTrackedNutrientTile;
			_renderNutrientPatchOverlay = renderNutrientPatchOverlay;
		}

		public void RenderFungalCellOverlay(BoardTile tile, Vector3Int pos)
		{
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();
			if (tile == null || moldTilemap == null || overlayTilemap == null)
			{
				return;
			}

			TileBase moldTile = null;
			TileBase overlayTile = null;
			Color moldColor = Color.white;
			Color overlayColor = Color.white;

			var cell = tile.FungalCell;
			if (cell == null)
			{
				return;
			}

			switch (cell.CellType)
			{
				case FungalCellType.Alive:
					if (cell.OwnerPlayerId is int aliveOwnerId)
					{
						moldTile = _getTileForPlayer(aliveOwnerId);
							moldColor = new Color(1f, 1f, 1f, _getAliveCellAlpha(tile.TileId, cell));
					}

					if (_shouldRenderResistanceOverlay(tile.TileId, cell))
					{
						overlayTile = _getGoldShieldOverlayTile();
						overlayColor = Color.white;
					}
					break;

				case FungalCellType.Dead:
					if (cell.OwnerPlayerId is int deadOwnerId)
					{
						moldTilemap.SetTileFlags(pos, TileFlags.None);
						moldTilemap.SetTile(pos, _getTileForPlayer(deadOwnerId));

						if (cell.IsDying)
						{
							moldColor = Color.white;
						}
						else
						{
							moldTilemap.SetColor(pos, new Color(1f, 1f, 1f, 0.8f));
							moldTilemap.RefreshTile(pos);
						}
					}

					overlayTile = _getDeadTile();
					overlayColor = cell.IsDying ? new Color(1f, 1f, 1f, 0f) : Color.white;
					break;

				case FungalCellType.Toxin:
					if (cell.OwnerPlayerId is int toxinOwnerId)
					{
						moldTile = _getTileForPlayer(toxinOwnerId);
						moldColor = Color.white;
					}

					overlayTile = _getToxinOverlayTile();
					overlayColor = cell.IsReceivingToxinDrop ? new Color(1f, 1f, 1f, 0f) : Color.white;
					break;

				default:
					return;
			}

			if (moldTile != null)
			{
				moldTilemap.SetTile(pos, moldTile);
				moldTilemap.SetTileFlags(pos, TileFlags.None);
				moldTilemap.SetColor(pos, moldColor);
				moldTilemap.RefreshTile(pos);
			}

			if (overlayTile != null)
			{
				overlayTilemap.SetTile(pos, overlayTile);
				overlayTilemap.SetTileFlags(pos, TileFlags.None);
				overlayTilemap.SetColor(pos, overlayColor);
				overlayTilemap.RefreshTile(pos);
			}
		}

		public void RenderTileFromBoard(int tileId)
		{
			var activeBoard = _getActiveBoard();
			if (activeBoard == null)
			{
				return;
			}

			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();
			var tile = activeBoard.GetTileById(tileId);
			var pos = _getPositionForTileId(tileId);

			if (moldTilemap != null)
			{
				moldTilemap.SetTile(pos, null);
				moldTilemap.SetColor(pos, Color.white);
				moldTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			}

			if (overlayTilemap != null)
			{
				overlayTilemap.SetTile(pos, null);
				overlayTilemap.SetColor(pos, Color.white);
				overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			}

			_removeTrackedNutrientTile?.Invoke(tileId);

			if (tile?.FungalCell != null)
			{
				RenderFungalCellOverlay(tile, pos);
				ApplyPreAnimationPreviewHiddenState(tileId, pos);
				return;
			}

			if (tile?.HasNutrientPatch == true)
			{
				_renderNutrientPatchOverlay?.Invoke(tile, pos);
			}
		}

		public void ApplyPreAnimationPreviewHiddenState(int tileId, Vector3Int pos)
		{
			if (!_isPreviewHiddenTile(tileId))
			{
				return;
			}

			var moldTilemap = _getMoldTilemap();
			if (moldTilemap != null && moldTilemap.HasTile(pos))
			{
				moldTilemap.SetTileFlags(pos, TileFlags.None);
				var moldColor = moldTilemap.GetColor(pos);
				moldColor.a = 0f;
				moldTilemap.SetColor(pos, moldColor);
			}

			var overlayTilemap = _getOverlayTilemap();
			if (overlayTilemap != null && overlayTilemap.HasTile(pos))
			{
				overlayTilemap.SetTileFlags(pos, TileFlags.None);
				var overlayColor = overlayTilemap.GetColor(pos);
				overlayColor.a = 0f;
				overlayTilemap.SetColor(pos, overlayColor);
			}
		}
	}
}
