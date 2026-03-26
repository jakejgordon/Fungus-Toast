using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Unity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
	internal sealed class GridOverlayRenderer
	{
		private static readonly Color NutrientPatchColor = new(1f, 1f, 1f, 0.92f);

		private readonly Func<GameBoard> _getBoard;
		private readonly Func<Tilemap> _getMoldTilemap;
		private readonly Func<Tilemap> _getOverlayTilemap;
		private readonly Func<Tilemap> _getTransientTilemap;
		private readonly Func<int, Vector3Int> _getPositionForTileId;
		private readonly Func<int, Tile> _getTileForPlayer;
		private readonly Func<Tile> _getSolidHighlightTile;
		private readonly Func<Tile> _getBaseTile;

		private readonly HashSet<int> _nutrientPulseTileIds = new();
		private readonly Dictionary<NutrientPatchType, Tile> _generatedNutrientTiles = new();
		private readonly Dictionary<NutrientPatchType, Sprite> _generatedNutrientSprites = new();
		private readonly Dictionary<NutrientPatchType, Texture2D> _generatedNutrientTextures = new();
		private Tile _generatedChemobeaconEmblemTile;
		private Sprite _generatedChemobeaconEmblemSprite;
		private Texture2D _generatedChemobeaconEmblemTexture;

		public GridOverlayRenderer(
			Func<GameBoard> getBoard,
			Func<Tilemap> getMoldTilemap,
			Func<Tilemap> getOverlayTilemap,
			Func<Tilemap> getTransientTilemap,
			Func<int, Vector3Int> getPositionForTileId,
			Func<int, Tile> getTileForPlayer,
			Func<Tile> getSolidHighlightTile,
			Func<Tile> getBaseTile,
			Func<Tile> getToxinOverlayTile)
		{
			_getBoard = getBoard;
			_getMoldTilemap = getMoldTilemap;
			_getOverlayTilemap = getOverlayTilemap;
			_getTransientTilemap = getTransientTilemap;
			_getPositionForTileId = getPositionForTileId;
			_getTileForPlayer = getTileForPlayer;
			_getSolidHighlightTile = getSolidHighlightTile;
			_getBaseTile = getBaseTile;
		}

		public void RenderNutrientPatchOverlay(BoardTile tile, Vector3Int pos)
		{
			var overlayTilemap = _getOverlayTilemap();
			if (tile == null || !tile.HasNutrientPatch || tile.FungalCell != null || overlayTilemap == null)
			{
				return;
			}

			TileBase nutrientTile = GetNutrientPatchTile(tile.NutrientPatch);
			if (nutrientTile == null)
			{
				return;
			}

			overlayTilemap.SetTile(pos, nutrientTile);
			overlayTilemap.SetTileFlags(pos, TileFlags.None);
			_nutrientPulseTileIds.Add(tile.TileId);
			overlayTilemap.SetColor(pos, GetNutrientPulseColor(tile.TileId));
			overlayTilemap.SetTransformMatrix(pos, GetNutrientPulseMatrix(tile.TileId));
			overlayTilemap.RefreshTile(pos);
		}

		public void RenderChemobeaconOverlay(int tileId, Vector3Int pos)
		{
			var board = _getBoard();
			var overlayTilemap = _getOverlayTilemap();
			var moldTilemap = _getMoldTilemap();
			if (board == null || overlayTilemap == null || moldTilemap == null)
			{
				return;
			}

			var marker = board.GetChemobeaconAtTile(tileId);
			TileBase chemobeaconTile = marker != null ? _getTileForPlayer(marker.PlayerId) : null;
			if (marker == null || chemobeaconTile == null)
			{
				return;
			}

			EnsureGeneratedChemobeaconEmblemTile();

			moldTilemap.SetTile(pos, chemobeaconTile);
			moldTilemap.SetTileFlags(pos, TileFlags.None);
			moldTilemap.SetColor(pos, GetChemobeaconPulseColor(tileId));
			moldTilemap.SetTransformMatrix(pos, GetChemobeaconPulseMatrix(tileId));
			moldTilemap.RefreshTile(pos);

			overlayTilemap.SetTile(pos, _generatedChemobeaconEmblemTile != null ? _generatedChemobeaconEmblemTile : _getSolidHighlightTile());
			overlayTilemap.SetTileFlags(pos, TileFlags.None);
			overlayTilemap.SetColor(pos, Color.white);
			overlayTilemap.SetTransformMatrix(pos, GetChemobeaconEmblemMatrix());
			overlayTilemap.RefreshTile(pos);
		}

		public void ClearChemobeaconOverlay(int tileId)
		{
			var overlayTilemap = _getOverlayTilemap();
			var moldTilemap = _getMoldTilemap();
			if (overlayTilemap == null || moldTilemap == null)
			{
				return;
			}

			Vector3Int pos = _getPositionForTileId(tileId);
			moldTilemap.SetTile(pos, null);
			moldTilemap.SetColor(pos, Color.white);
			moldTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
			overlayTilemap.SetTile(pos, null);
			overlayTilemap.SetColor(pos, Color.white);
			overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
		}

		public void ClearChemobeaconTransientOverlay(int tileId)
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

		public void UpdateChemobeaconPulseVisuals()
		{
			var board = _getBoard();
			var overlayTilemap = _getOverlayTilemap();
			var moldTilemap = _getMoldTilemap();
			if (board == null || overlayTilemap == null || moldTilemap == null)
			{
				return;
			}

			foreach (var marker in board.GetActiveChemobeacons())
			{
				Vector3Int pos = _getPositionForTileId(marker.TileId);
				if (!moldTilemap.HasTile(pos))
				{
					continue;
				}

				moldTilemap.SetColor(pos, GetChemobeaconPulseColor(marker.TileId));
				moldTilemap.SetTransformMatrix(pos, GetChemobeaconPulseMatrix(marker.TileId));
				if (overlayTilemap.HasTile(pos))
				{
					overlayTilemap.SetColor(pos, Color.white);
					overlayTilemap.SetTransformMatrix(pos, GetChemobeaconEmblemMatrix());
				}
			}
		}

		public TileBase GetNutrientPatchTile(NutrientPatch nutrientPatch)
		{
			return GetNutrientPatchTile(nutrientPatch?.PatchType ?? NutrientPatchType.Adaptogen);
		}

		public TileBase GetNutrientPatchTile(NutrientPatchType patchType)
		{
			EnsureGeneratedNutrientTile(patchType);

			if (_generatedNutrientTiles.TryGetValue(patchType, out Tile generatedNutrientTile) && generatedNutrientTile != null)
			{
				return generatedNutrientTile;
			}

			if (_getSolidHighlightTile() != null)
			{
				return _getSolidHighlightTile();
			}

			return _getBaseTile();
		}

		public void UpdateNutrientPulseVisuals()
		{
			var overlayTilemap = _getOverlayTilemap();
			var board = _getBoard();
			if (overlayTilemap == null || board == null || _nutrientPulseTileIds.Count == 0)
			{
				return;
			}

			var staleTileIds = new List<int>();
			foreach (int tileId in _nutrientPulseTileIds)
			{
				BoardTile tile = board.GetTileById(tileId);
				if (tile == null || !tile.HasNutrientPatch || tile.FungalCell != null)
				{
					staleTileIds.Add(tileId);
					continue;
				}

				Vector3Int pos = _getPositionForTileId(tileId);
				if (!overlayTilemap.HasTile(pos))
				{
					continue;
				}

				overlayTilemap.SetTransformMatrix(pos, GetNutrientPulseMatrix(tileId));
				overlayTilemap.SetColor(pos, GetNutrientPulseColor(tileId));
			}

			for (int i = 0; i < staleTileIds.Count; i++)
			{
				_nutrientPulseTileIds.Remove(staleTileIds[i]);
			}
		}

		public void RemoveTrackedNutrientTile(int tileId)
		{
			_nutrientPulseTileIds.Remove(tileId);
		}

		public Sprite GetChemobeaconLegendSprite()
		{
			EnsureGeneratedChemobeaconEmblemTile();
			return _generatedChemobeaconEmblemSprite;
		}

		public void ResetRuntimeState()
		{
			_nutrientPulseTileIds.Clear();
		}

		public void Dispose()
		{
			DestroyGeneratedNutrientAssets();
			DestroyGeneratedChemobeaconAssets();
			_nutrientPulseTileIds.Clear();
		}

		private Matrix4x4 GetChemobeaconPulseMatrix(int tileId)
		{
			float wave = GetChemobeaconPulseFactor(tileId);
			float scale = Mathf.Lerp(UIEffectConstants.ChemobeaconPulseMinScale, UIEffectConstants.ChemobeaconPulseMaxScale, wave);
			return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
		}

		private static Color GetChemobeaconPulseColor(int tileId)
		{
			float wave = GetChemobeaconPulseFactor(tileId);
			float alpha = Mathf.Lerp(UIEffectConstants.ChemobeaconPulseMinAlpha, UIEffectConstants.ChemobeaconPulseMaxAlpha, wave);
			return new Color(1f, 1f, 1f, alpha);
		}

		private static float GetChemobeaconPulseFactor(int tileId)
		{
			float duration = Mathf.Max(0.01f, UIEffectConstants.ChemobeaconPulseDurationSeconds);
			float cycle = Mathf.Repeat(Time.time + tileId * 0.137f, duration) / duration;
			return 0.5f + 0.5f * Mathf.Sin(cycle * Mathf.PI * 2f);
		}

		private static Matrix4x4 GetChemobeaconEmblemMatrix()
		{
			return Matrix4x4.TRS(new Vector3(0f, 0.02f, 0f), Quaternion.identity, new Vector3(0.78f, 0.78f, 1f));
		}

		private void EnsureGeneratedChemobeaconEmblemTile()
		{
			if (_generatedChemobeaconEmblemTile != null && _generatedChemobeaconEmblemSprite != null && _generatedChemobeaconEmblemTexture != null)
			{
				return;
			}

			const int textureSize = 48;
			_generatedChemobeaconEmblemTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				alphaIsTransparency = true
			};

			var pixels = new Color32[textureSize * textureSize];
			for (int py = 0; py < textureSize; py++)
			{
				for (int px = 0; px < textureSize; px++)
				{
					pixels[(py * textureSize) + px] = EvaluateChemobeaconEmblemPixel(textureSize, px, py);
				}
			}

			_generatedChemobeaconEmblemTexture.SetPixels32(pixels);
			_generatedChemobeaconEmblemTexture.Apply(false, false);
			_generatedChemobeaconEmblemSprite = Sprite.Create(
				_generatedChemobeaconEmblemTexture,
				new Rect(0f, 0f, textureSize, textureSize),
				new Vector2(0.5f, 0.5f),
				textureSize,
				0,
				SpriteMeshType.FullRect);

			_generatedChemobeaconEmblemTile = ScriptableObject.CreateInstance<Tile>();
			_generatedChemobeaconEmblemTile.sprite = _generatedChemobeaconEmblemSprite;
			_generatedChemobeaconEmblemTile.color = Color.white;
			_generatedChemobeaconEmblemTile.colliderType = Tile.ColliderType.None;
		}

		private static Color32 EvaluateChemobeaconEmblemPixel(int textureSize, int px, int py)
		{
			float x = (((px + 0.5f) / textureSize) * 2f) - 1f;
			float y = (((py + 0.5f) / textureSize) * 2f) - 1f;

			float tower = RectMask(x, y, -0.12f, 0.12f, -0.42f, 0.34f);
			float cap = TriangleMask(x, y, new Vector2(-0.22f, 0.2f), new Vector2(0.22f, 0.2f), new Vector2(0f, 0.52f));
			float basePlate = RectMask(x, y, -0.3f, 0.3f, -0.58f, -0.42f);
			float door = RectMask(x, y, -0.055f, 0.055f, -0.42f, -0.16f);
			float lampRoom = RectMask(x, y, -0.18f, 0.18f, 0.08f, 0.2f);
			float beamRight = TriangleMask(x, y, new Vector2(0.08f, 0.14f), new Vector2(0.78f, 0.34f), new Vector2(0.78f, -0.04f));
			float beamLeft = TriangleMask(x, y, new Vector2(-0.08f, 0.14f), new Vector2(-0.78f, 0.34f), new Vector2(-0.78f, -0.04f));
			float railing = RectMask(x, y, -0.24f, 0.24f, 0.02f, 0.08f);

			float silhouette = Mathf.Max(basePlate, tower);
			silhouette = Mathf.Max(silhouette, cap);
			silhouette = Mathf.Max(silhouette, lampRoom);
			silhouette = Mathf.Max(silhouette, beamLeft * 0.92f);
			silhouette = Mathf.Max(silhouette, beamRight * 0.92f);
			silhouette = Mathf.Max(silhouette, railing);
			silhouette *= 1f - (door * 0.85f);

			if (silhouette <= 0.02f)
			{
				return new Color32(0, 0, 0, 0);
			}

			byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(silhouette * 255f), 0, 255);
			return new Color32(0, 0, 0, alpha);
		}

		private static float RectMask(float x, float y, float minX, float maxX, float minY, float maxY)
		{
			return x >= minX && x <= maxX && y >= minY && y <= maxY ? 1f : 0f;
		}

		private static float TriangleMask(float x, float y, Vector2 a, Vector2 b, Vector2 c)
		{
			Vector2 p = new(x, y);
			float d1 = Sign(p, a, b);
			float d2 = Sign(p, b, c);
			float d3 = Sign(p, c, a);
			bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
			bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
			return hasNeg && hasPos ? 0f : 1f;
		}

		private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		private void EnsureGeneratedNutrientTile(NutrientPatchType patchType)
		{
			if (_generatedNutrientTiles.ContainsKey(patchType)
				&& _generatedNutrientSprites.ContainsKey(patchType)
				&& _generatedNutrientTextures.ContainsKey(patchType))
			{
				return;
			}

			const int textureSize = 48;
			var generatedNutrientTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				alphaIsTransparency = true
			};

			var pixels = new Color32[textureSize * textureSize];
			for (int py = 0; py < textureSize; py++)
			{
				for (int px = 0; px < textureSize; px++)
				{
					pixels[(py * textureSize) + px] = EvaluateNutrientPixel(textureSize, px, py, patchType);
				}
			}

			generatedNutrientTexture.SetPixels32(pixels);
			generatedNutrientTexture.Apply(false, false);
			var generatedNutrientSprite = Sprite.Create(
				generatedNutrientTexture,
				new Rect(0f, 0f, textureSize, textureSize),
				new Vector2(0.5f, 0.5f),
				textureSize,
				0,
				SpriteMeshType.FullRect);

			var generatedNutrientTile = ScriptableObject.CreateInstance<Tile>();
			generatedNutrientTile.sprite = generatedNutrientSprite;
			generatedNutrientTile.color = Color.white;
			generatedNutrientTile.colliderType = Tile.ColliderType.None;

			_generatedNutrientTextures[patchType] = generatedNutrientTexture;
			_generatedNutrientSprites[patchType] = generatedNutrientSprite;
			_generatedNutrientTiles[patchType] = generatedNutrientTile;
		}

		private static Color32 EvaluateNutrientPixel(int textureSize, int px, int py, NutrientPatchType patchType)
		{
			float x = (((px + 0.5f) / textureSize) * 2f) - 1f;
			float y = (((py + 0.5f) / textureSize) * 2f) - 1f;
			float radius = Mathf.Sqrt((x * x) + (y * y));
			float diamond = (Mathf.Abs(x) * 0.88f) + (Mathf.Abs(y) * 0.88f);
			float outerBody = 1f - Mathf.SmoothStep(0.64f, 0.9f, diamond);
			float core = 1f - Mathf.SmoothStep(0.12f, 0.32f, radius);
			float ring = 1f - Mathf.SmoothStep(0.02f, 0.11f, Mathf.Abs(radius - 0.43f));
			float verticalVein = 1f - Mathf.SmoothStep(0.015f, 0.075f, Mathf.Abs(x));
			float horizontalVein = 1f - Mathf.SmoothStep(0.015f, 0.075f, Mathf.Abs(y));
			float veins = Mathf.Max(verticalVein * core, horizontalVein * core * 0.82f);

			float satelliteNorth = 1f - Mathf.SmoothStep(0.03f, 0.12f, Vector2.Distance(new Vector2(x, y), new Vector2(0f, 0.58f)));
			float satelliteSouth = 1f - Mathf.SmoothStep(0.03f, 0.12f, Vector2.Distance(new Vector2(x, y), new Vector2(0f, -0.58f)));
			float satelliteEast = 1f - Mathf.SmoothStep(0.03f, 0.12f, Vector2.Distance(new Vector2(x, y), new Vector2(0.58f, 0f)));
			float satelliteWest = 1f - Mathf.SmoothStep(0.03f, 0.12f, Vector2.Distance(new Vector2(x, y), new Vector2(-0.58f, 0f)));
			float satellites = Mathf.Max(Mathf.Max(satelliteNorth, satelliteSouth), Mathf.Max(satelliteEast, satelliteWest));

			float alpha = Mathf.Max(outerBody * 0.94f, core * 0.88f);
			alpha = Mathf.Max(alpha, ring * 0.72f);
			alpha = Mathf.Max(alpha, satellites * 0.66f);
			if (alpha <= 0.01f)
			{
				return new Color(0f, 0f, 0f, 0f);
			}

			Color outerColor = new(0.42f, 0.25f, 0.08f, 1f);
			Color bodyColor = new(0.86f, 0.63f, 0.16f, 1f);
			Color ringColor = new(0.96f, 0.8f, 0.33f, 1f);
			Color coreColor = new(0.99f, 0.93f, 0.72f, 1f);
			Color veinColor = new(1f, 0.98f, 0.88f, 1f);

			Color color = Color.Lerp(outerColor, bodyColor, Mathf.Clamp01(outerBody * 0.92f));
			color = Color.Lerp(color, ringColor, ring * 0.85f);
			color = Color.Lerp(color, coreColor, core * 0.9f);
			color = Color.Lerp(color, veinColor, veins * 0.75f);
			color = Color.Lerp(color, ringColor, satellites * 0.55f);

			if (patchType == NutrientPatchType.Adaptogen)
			{
				float helixLeft = 1f - Mathf.SmoothStep(0.018f, 0.07f, Mathf.Abs(x - (0.18f * Mathf.Sin(y * 7.2f)) - 0.16f));
				float helixRight = 1f - Mathf.SmoothStep(0.018f, 0.07f, Mathf.Abs(x + (0.18f * Mathf.Sin(y * 7.2f)) + 0.16f));
				float helix = Mathf.Max(helixLeft, helixRight) * (1f - Mathf.SmoothStep(0.56f, 0.9f, radius));
				float ladder = (1f - Mathf.SmoothStep(0.02f, 0.07f, Mathf.Abs(x)))
					* (0.5f + (0.5f * Mathf.Cos(y * 18f)))
					* (1f - Mathf.SmoothStep(0.2f, 0.72f, Mathf.Abs(y)));

				Color helixColor = new(0.35f, 0.84f, 1f, 1f);
				Color ladderColor = new(0.84f, 0.98f, 1f, 1f);
				color = Color.Lerp(color, helixColor, helix * 0.92f);
				color = Color.Lerp(color, ladderColor, ladder * 0.85f);
				alpha = Mathf.Max(alpha, helix * 0.8f);
				alpha = Mathf.Max(alpha, ladder * 0.72f);
			}
			else if (patchType == NutrientPatchType.Sporemeal)
			{
				float runnerStem = 1f - Mathf.SmoothStep(0.02f, 0.085f, Mathf.Abs(x));
				float branchNorthEast = 1f - Mathf.SmoothStep(0.02f, 0.085f, Mathf.Abs((y - 0.18f) - (x * 0.92f)));
				float branchNorthWest = 1f - Mathf.SmoothStep(0.02f, 0.085f, Mathf.Abs((y - 0.18f) + (x * 0.92f)));
				float branchSouthEast = 1f - Mathf.SmoothStep(0.02f, 0.085f, Mathf.Abs((y + 0.22f) + (x * 1.08f)));
				float branchSouthWest = 1f - Mathf.SmoothStep(0.02f, 0.085f, Mathf.Abs((y + 0.22f) - (x * 1.08f)));
				float runner = Mathf.Max(Mathf.Max(runnerStem, branchNorthEast), Mathf.Max(branchNorthWest, Mathf.Max(branchSouthEast, branchSouthWest)));
				runner *= 1f - Mathf.SmoothStep(0.6f, 0.95f, radius);

				float sporeburst = 1f - Mathf.SmoothStep(0.015f, 0.07f, Mathf.Abs(radius - 0.28f));
				sporeburst *= 0.5f + (0.5f * Mathf.Cos((Mathf.Atan2(y, x) * 6f)));

				Color runnerColor = new(0.72f, 0.9f, 0.42f, 1f);
				Color sporeColor = new(0.98f, 1f, 0.84f, 1f);
				color = Color.Lerp(color, runnerColor, runner * 0.82f);
				color = Color.Lerp(color, sporeColor, sporeburst * 0.56f);
				alpha = Mathf.Max(alpha, runner * 0.76f);
				alpha = Mathf.Max(alpha, sporeburst * 0.44f);
			}
			else
			{
				float corona = 1f - Mathf.SmoothStep(0.06f, 0.16f, Mathf.Abs(radius - 0.34f));
				float starburst = Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(y, x) * 4f)), 2.4f);
				starburst *= 1f - Mathf.SmoothStep(0.18f, 0.82f, radius);
				float nucleus = 1f - Mathf.SmoothStep(0.04f, 0.2f, radius);
				float orbitNorth = 1f - Mathf.SmoothStep(0.03f, 0.11f, Vector2.Distance(new Vector2(x, y), new Vector2(0f, 0.7f)));
				float orbitSouth = 1f - Mathf.SmoothStep(0.03f, 0.11f, Vector2.Distance(new Vector2(x, y), new Vector2(0f, -0.7f)));
				float orbitEast = 1f - Mathf.SmoothStep(0.03f, 0.11f, Vector2.Distance(new Vector2(x, y), new Vector2(0.7f, 0f)));
				float orbitWest = 1f - Mathf.SmoothStep(0.03f, 0.11f, Vector2.Distance(new Vector2(x, y), new Vector2(-0.7f, 0f)));
				float orbitals = Mathf.Max(Mathf.Max(orbitNorth, orbitSouth), Mathf.Max(orbitEast, orbitWest));

				Color coronaColor = new(0.72f, 0.44f, 0.98f, 1f);
				Color starColor = new(0.96f, 0.78f, 1f, 1f);
				Color nucleusColor = new(0.99f, 0.94f, 1f, 1f);
				color = Color.Lerp(color, coronaColor, corona * 0.88f);
				color = Color.Lerp(color, starColor, starburst * 0.82f);
				color = Color.Lerp(color, nucleusColor, nucleus * 0.92f);
				color = Color.Lerp(color, starColor, orbitals * 0.7f);
				alpha = Mathf.Max(alpha, corona * 0.72f);
				alpha = Mathf.Max(alpha, starburst * 0.78f);
				alpha = Mathf.Max(alpha, nucleus * 0.84f);
				alpha = Mathf.Max(alpha, orbitals * 0.58f);
			}

			color.a = Mathf.Clamp01(alpha);
			return color;
		}

		private Matrix4x4 GetNutrientPulseMatrix(int tileId)
		{
			float pulse = GetNutrientPulseFactor(tileId);
			float scale = Mathf.Lerp(UIEffectConstants.NutrientPatchPulseMinScale, UIEffectConstants.NutrientPatchPulseMaxScale, pulse);
			return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
		}

		private Color GetNutrientPulseColor(int tileId)
		{
			float pulse = GetNutrientPulseFactor(tileId);
			float alpha = Mathf.Lerp(UIEffectConstants.NutrientPatchPulseAlphaMin, UIEffectConstants.NutrientPatchPulseAlphaMax, pulse);
			return new Color(NutrientPatchColor.r, NutrientPatchColor.g, NutrientPatchColor.b, alpha);
		}

		private static float GetNutrientPulseFactor(int tileId)
		{
			float phase = (Time.time * UIEffectConstants.NutrientPatchPulseSpeed) + (tileId * UIEffectConstants.NutrientPatchPulsePhaseOffsetRadians);
			float wave = 0.5f + (0.5f * Mathf.Sin(phase));
			return wave * wave * (3f - (2f * wave));
		}

		private void DestroyGeneratedNutrientAssets()
		{
			foreach (Tile generatedNutrientTile in _generatedNutrientTiles.Values)
			{
				if (generatedNutrientTile != null)
				{
					UnityEngine.Object.Destroy(generatedNutrientTile);
				}
			}

			foreach (Sprite generatedNutrientSprite in _generatedNutrientSprites.Values)
			{
				if (generatedNutrientSprite != null)
				{
					UnityEngine.Object.Destroy(generatedNutrientSprite);
				}
			}

			foreach (Texture2D generatedNutrientTexture in _generatedNutrientTextures.Values)
			{
				if (generatedNutrientTexture != null)
				{
					UnityEngine.Object.Destroy(generatedNutrientTexture);
				}
			}

			_generatedNutrientTiles.Clear();
			_generatedNutrientSprites.Clear();
			_generatedNutrientTextures.Clear();
		}

		private void DestroyGeneratedChemobeaconAssets()
		{
			if (_generatedChemobeaconEmblemTile != null)
			{
				UnityEngine.Object.Destroy(_generatedChemobeaconEmblemTile);
				_generatedChemobeaconEmblemTile = null;
			}

			if (_generatedChemobeaconEmblemSprite != null)
			{
				UnityEngine.Object.Destroy(_generatedChemobeaconEmblemSprite);
				_generatedChemobeaconEmblemSprite = null;
			}

			if (_generatedChemobeaconEmblemTexture != null)
			{
				UnityEngine.Object.Destroy(_generatedChemobeaconEmblemTexture);
				_generatedChemobeaconEmblemTexture = null;
			}
		}
	}

	internal sealed class GridSpecialPresentationEffects
	{
		private static readonly Color NutrientPatchColor = new(1f, 1f, 1f, 0.92f);
		private static readonly Color AdaptogenPatchTextColor = new(0.8f, 0.97f, 1f, 1f);
		private static readonly Color SporemealPatchTextColor = new(0.92f, 1f, 0.8f, 1f);
		private static readonly Color HypervariationPatchTextColor = new(0.95f, 0.84f, 1f, 1f);
		private static readonly Color DirectedVectorToastColor = new(0.99f, 1f, 0.8f, 1f);
		private static readonly Color DirectedVectorPulseColor = new(1f, 0.92f, 0.38f, 0.96f);

		private readonly Func<GameBoard> _getBoard;
		private readonly Func<Tilemap> _getMoldTilemap;
		private readonly Func<Tilemap> _getOverlayTilemap;
		private readonly Func<Tilemap> _getPingOverlayTilemap;
		private readonly Func<Tilemap> _getHoverOverlayTilemap;
		private readonly Func<TileBase> _getSolidHighlightTile;
		private readonly Func<Transform> _getToastParent;
		private readonly Func<int, Vector3Int> _getPositionForTileId;
		private readonly Func<NutrientPatchType, TileBase> _getNutrientPatchTile;
		private readonly Func<int, float, IEnumerator> _createResistancePulse;
		private readonly Action<Vector3Int, float, float, Color, Tilemap> _drawRing;
		private readonly Action<Tilemap> _clearRing;
		private readonly Func<IEnumerator, Coroutine> _startCoroutine;
		private readonly Action<int> _revealPreAnimationPreviewTile;
		private readonly Action<int> _renderTileFromBoard;
		private readonly Action _beginAnimation;
		private readonly Action _endAnimation;

		public GridSpecialPresentationEffects(
			Func<GameBoard> getBoard,
			Func<Tilemap> getMoldTilemap,
			Func<Tilemap> getOverlayTilemap,
			Func<Tilemap> getPingOverlayTilemap,
			Func<Tilemap> getHoverOverlayTilemap,
			Func<TileBase> getSolidHighlightTile,
			Func<Transform> getToastParent,
			Func<int, Vector3Int> getPositionForTileId,
			Func<NutrientPatchType, TileBase> getNutrientPatchTile,
			Func<int, float, IEnumerator> createResistancePulse,
			Action<Vector3Int, float, float, Color, Tilemap> drawRing,
			Action<Tilemap> clearRing,
			Func<IEnumerator, Coroutine> startCoroutine,
			Action<int> revealPreAnimationPreviewTile,
			Action<int> renderTileFromBoard,
			Action beginAnimation,
			Action endAnimation)
		{
			_getBoard = getBoard;
			_getMoldTilemap = getMoldTilemap;
			_getOverlayTilemap = getOverlayTilemap;
			_getPingOverlayTilemap = getPingOverlayTilemap;
			_getHoverOverlayTilemap = getHoverOverlayTilemap;
			_getSolidHighlightTile = getSolidHighlightTile;
			_getToastParent = getToastParent;
			_getPositionForTileId = getPositionForTileId;
			_getNutrientPatchTile = getNutrientPatchTile;
			_createResistancePulse = createResistancePulse;
			_drawRing = drawRing;
			_clearRing = clearRing;
			_startCoroutine = startCoroutine;
			_revealPreAnimationPreviewTile = revealPreAnimationPreviewTile;
			_renderTileFromBoard = renderTileFromBoard;
			_beginAnimation = beginAnimation;
			_endAnimation = endAnimation;
		}

		public IEnumerator PlayMycotoxicLashAnimation(IReadOnlyList<int> tileIds)
		{
			var board = _getBoard();
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();
			if (board == null || tileIds == null || tileIds.Count == 0 || moldTilemap == null || overlayTilemap == null)
			{
				yield break;
			}

			var states = new List<(Vector3Int pos, bool hasMold, Color moldColor, bool hasOverlay, Color overlayColor)>();
			var seenTileIds = new HashSet<int>();

			for (int i = 0; i < tileIds.Count; i++)
			{
				int tileId = tileIds[i];
				if (!seenTileIds.Add(tileId))
				{
					continue;
				}

				var (x, y) = board.GetXYFromTileId(tileId);
				var pos = new Vector3Int(x, y, 0);
				bool hasMold = moldTilemap.HasTile(pos);
				bool hasOverlay = overlayTilemap.HasTile(pos);
				if (!hasMold && !hasOverlay)
				{
					continue;
				}

				states.Add((
					pos,
					hasMold,
					hasMold ? moldTilemap.GetColor(pos) : Color.white,
					hasOverlay,
					hasOverlay ? overlayTilemap.GetColor(pos) : Color.white));
			}

			if (states.Count == 0)
			{
				yield break;
			}

			float totalDuration = UIEffectConstants.MycotoxicLashAnimationDurationSeconds;
			float fadeToBlackDuration = totalDuration * UIEffectConstants.MycotoxicLashFadeToBlackPortion;
			float blackHoldDuration = Mathf.Max(0f, totalDuration - fadeToBlackDuration);

			_beginAnimation();
			try
			{
				float elapsed = 0f;
				while (elapsed < fadeToBlackDuration)
				{
					elapsed += Time.deltaTime;
					float t = fadeToBlackDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeToBlackDuration);
					float eased = 1f - Mathf.Pow(1f - t, 3f);
					ApplyMycotoxicLashColors(states, eased, moldTilemap, overlayTilemap);
					yield return null;
				}

				ApplyMycotoxicLashColors(states, 1f, moldTilemap, overlayTilemap);
				if (blackHoldDuration > 0f)
				{
					yield return new WaitForSeconds(blackHoldDuration);
				}
			}
			finally
			{
				_endAnimation();
			}
		}

		public IEnumerator PlayRetrogradeBloomAnimation(int anchorTileId)
		{
			var board = _getBoard();
			var highlightTilemap = GetTransientPulseTilemap();
			if (board == null)
			{
				yield break;
			}

			if (anchorTileId >= 0)
			{
				yield return _createResistancePulse(anchorTileId, 0.85f);
			}

			if (highlightTilemap == null || _getSolidHighlightTile() == null || anchorTileId < 0)
			{
				yield break;
			}

			var (x, y) = board.GetXYFromTileId(anchorTileId);
			var center = new Vector3Int(x, y, 0);
			float duration = UIEffectConstants.RetrogradeBloomAnimationDurationSeconds;

			_beginAnimation();
			try
			{
				float startTime = Time.time;
				while (Time.time - startTime < duration)
				{
					float u = Mathf.Clamp01((Time.time - startTime) / duration);
					float radius = Mathf.Lerp(0.35f, 2.8f, u);
					Color ringColor = Color.Lerp(new Color(1f, 0.85f, 0.25f, 0.95f), new Color(1f, 0.45f, 0.12f, 0f), u);
					_drawRing(center, radius, 0.45f, ringColor, highlightTilemap);
					yield return null;
				}
			}
			finally
			{
				_clearRing(highlightTilemap);
				_endAnimation();
			}
		}

		public IEnumerator PlaySaprophageRingAnimation(IReadOnlyList<int> resistantTileIds, IReadOnlyList<int> consumedTileIds)
		{
			var board = _getBoard();
			var highlightTilemap = GetTransientPulseTilemap();
			var highlightTile = _getSolidHighlightTile();
			if (board == null || consumedTileIds == null || consumedTileIds.Count == 0 || highlightTilemap == null || highlightTile == null)
			{
				yield break;
			}

			var resistantSources = (resistantTileIds ?? System.Array.Empty<int>()).Distinct().ToList();
			foreach (int sourceTileId in resistantSources)
			{
				_startCoroutine(_createResistancePulse(sourceTileId, 0.75f));
			}

			var consumedPositions = consumedTileIds
				.Distinct()
				.Select(tileId =>
				{
					var (x, y) = board.GetXYFromTileId(tileId);
					return new Vector3Int(x, y, 0);
				})
				.ToList();

			foreach (var pos in consumedPositions)
			{
				highlightTilemap.SetTile(pos, highlightTile);
				highlightTilemap.SetTileFlags(pos, TileFlags.None);
			}

			float duration = UIEffectConstants.SaprophageRingAnimationDurationSeconds;
			_beginAnimation();
			try
			{
				float elapsed = 0f;
				while (elapsed < duration)
				{
					elapsed += Time.deltaTime;
					float u = Mathf.Clamp01(elapsed / duration);
					float scale = Mathf.Lerp(1.1f, 0.25f, u);
					Color color = Color.Lerp(new Color(0.42f, 0.95f, 0.62f, 0.85f), new Color(0.07f, 0.16f, 0.08f, 0f), u);

					foreach (var pos in consumedPositions)
					{
						highlightTilemap.SetColor(pos, color);
						highlightTilemap.SetTransformMatrix(pos, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f)));
					}

					yield return null;
				}
			}
			finally
			{
				foreach (var pos in consumedPositions)
				{
					highlightTilemap.SetTile(pos, null);
					highlightTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
					highlightTilemap.SetColor(pos, Color.white);
				}

				_endAnimation();
			}
		}

		public IEnumerator PlayNutrientPatchConsumptionAnimation(int nutrientTileId, int destinationTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount)
		{
			var board = _getBoard();
			var overlayTilemap = _getOverlayTilemap();
			if (board == null || overlayTilemap == null)
			{
				yield break;
			}

			Vector3Int sourcePos = _getPositionForTileId(nutrientTileId);
			Vector3Int destinationPos = _getPositionForTileId(destinationTileId);
			TileBase nutrientTile = _getNutrientPatchTile(patchType);
			if (nutrientTile == null)
			{
				yield break;
			}

			TextMeshPro floatingText = CreateNutrientToastText(destinationPos, patchType, rewardType, rewardAmount, overlayTilemap);
			Vector3 sourceWorld = overlayTilemap.GetCellCenterWorld(sourcePos);
			Vector3 destinationWorld = overlayTilemap.GetCellCenterWorld(destinationPos);
			Vector3 delta = destinationWorld - sourceWorld;
			Vector3 pullOffset = delta.sqrMagnitude > 0.0001f ? delta.normalized * UIEffectConstants.NutrientPatchPullOffsetWorld : Vector3.zero;

			overlayTilemap.SetTile(sourcePos, nutrientTile);
			overlayTilemap.SetTileFlags(sourcePos, TileFlags.None);
			overlayTilemap.SetColor(sourcePos, NutrientPatchColor);

			float duration = UIEffectConstants.NutrientPatchConsumptionDurationSeconds;
			float textDuration = UIEffectConstants.NutrientPatchToastDurationSeconds;
			float animationDuration = Mathf.Max(duration, textDuration);

			try
			{
				float elapsed = 0f;
				bool clearedSourceTile = false;
				while (elapsed < animationDuration)
				{
					elapsed += Time.deltaTime;
					if (elapsed < duration)
					{
						float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
						float eased = 1f - Mathf.Pow(1f - t, 3f);
						float scale = Mathf.Lerp(UIEffectConstants.NutrientPatchPulseMinScale, 0.18f, eased);
						Vector3 translation = Vector3.Lerp(Vector3.zero, pullOffset, eased);
						Color color = Color.Lerp(NutrientPatchColor, new Color(NutrientPatchColor.r, NutrientPatchColor.g, NutrientPatchColor.b, 0f), eased);

						overlayTilemap.SetTransformMatrix(sourcePos, Matrix4x4.TRS(translation, Quaternion.identity, new Vector3(scale, scale, 1f)));
						overlayTilemap.SetColor(sourcePos, color);
					}
					else if (!clearedSourceTile)
					{
						overlayTilemap.SetTile(sourcePos, null);
						overlayTilemap.SetTransformMatrix(sourcePos, Matrix4x4.identity);
						overlayTilemap.SetColor(sourcePos, Color.white);
						clearedSourceTile = true;
					}

					if (floatingText != null)
					{
						float textT = textDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / textDuration);
						Vector3 textStart = destinationWorld + new Vector3(0f, UIEffectConstants.NutrientPatchToastStartHeightWorld, 0f);
						Vector3 textEnd = textStart + new Vector3(0f, UIEffectConstants.NutrientPatchToastRiseWorld, 0f);
						floatingText.transform.position = Vector3.Lerp(textStart, textEnd, textT);
						floatingText.transform.localScale = Vector3.one * GetAnimatedNutrientToastScaleMultiplier(textT);
						Color textColor = GetNutrientToastColor(patchType);
						float fadeT = Mathf.Clamp01((textT - 0.42f) / 0.58f);
						textColor.a = 1f - fadeT;
						floatingText.color = textColor;
					}

					yield return null;
				}
			}
			finally
			{
				overlayTilemap.SetTile(sourcePos, null);
				overlayTilemap.SetTransformMatrix(sourcePos, Matrix4x4.identity);
				overlayTilemap.SetColor(sourcePos, Color.white);
				if (floatingText != null)
				{
					UnityEngine.Object.Destroy(floatingText.gameObject);
				}
			}
		}

		public IEnumerator RunDirectedVectorSurgePresentation(int playerId, int originTileId, IReadOnlyList<int> affectedTileIds)
		{
			var board = _getBoard();
			var pulseTilemap = GetTransientPulseTilemap();
			var overlayTilemap = _getOverlayTilemap();
			if (board == null || pulseTilemap == null || _getSolidHighlightTile() == null || overlayTilemap == null)
			{
				yield break;
			}

			var orderedTileIds = affectedTileIds.Where(tileId => tileId >= 0).Distinct().ToList();
			if (orderedTileIds.Count == 0)
			{
				yield break;
			}

			var presentationTileIds = BuildDirectedVectorPresentationTileIds(board, playerId, originTileId, orderedTileIds);
			int presentationStartTileId = presentationTileIds.Count > 0 ? presentationTileIds[0] : originTileId;
			var sweepTileIds = presentationTileIds.Count > 1
				? presentationTileIds.Skip(1).ToList()
				: orderedTileIds;
			var growthTargetTileIds = new HashSet<int>(orderedTileIds);

			TextMeshPro toast = CreateDirectedVectorToastText(orderedTileIds, overlayTilemap);
			try
			{
				if (presentationStartTileId >= 0)
				{
					Vector3Int originPos = _getPositionForTileId(presentationStartTileId);
					yield return PulseTiles(pulseTilemap, new[] { originPos }, UIEffectConstants.DirectedVectorOriginPulseDurationSeconds, DirectedVectorPulseColor, 1f, UIEffectConstants.DirectedVectorPulseScale);
				}

				foreach (var chunk in BuildDirectedVectorChunks(sweepTileIds))
				{
					var revealTileIds = chunk.Where(growthTargetTileIds.Contains).ToList();
					if (revealTileIds.Count > 0)
					{
						_startCoroutine(AnimateDirectedVectorGrowthReveal(revealTileIds));
					}

					var chunkPositions = chunk.Select(_getPositionForTileId).ToArray();
					_startCoroutine(PulseTiles(pulseTilemap, chunkPositions, UIEffectConstants.DirectedVectorChunkPulseDurationSeconds, DirectedVectorPulseColor, 1f, UIEffectConstants.DirectedVectorPulseScale));
					yield return new WaitForSeconds(UIEffectConstants.DirectedVectorChunkStaggerSeconds);
				}

				if (toast != null)
				{
					yield return AnimateFloatingToast(toast, UIEffectConstants.DirectedVectorToastDurationSeconds, DirectedVectorToastColor, UIEffectConstants.DirectedVectorToastRiseWorld, useAnimatedScale: false);
				}
			}
			finally
			{
				for (int i = 0; i < orderedTileIds.Count; i++)
				{
					_revealPreAnimationPreviewTile?.Invoke(orderedTileIds[i]);
					_renderTileFromBoard?.Invoke(orderedTileIds[i]);
				}

				if (toast != null)
				{
					UnityEngine.Object.Destroy(toast.gameObject);
				}
			}
		}

		private IEnumerator AnimateDirectedVectorGrowthReveal(IReadOnlyList<int> tileIds)
		{
			var board = _getBoard();
			var moldTilemap = _getMoldTilemap();
			var overlayTilemap = _getOverlayTilemap();
			if (board == null || moldTilemap == null || overlayTilemap == null || tileIds == null || tileIds.Count == 0)
			{
				yield break;
			}

			var states = new List<(Vector3Int pos, bool hasMold, Color moldColor, bool hasOverlay, Color overlayColor)>();
			var seenTileIds = new HashSet<int>();

			for (int i = 0; i < tileIds.Count; i++)
			{
				int tileId = tileIds[i];
				if (!seenTileIds.Add(tileId))
				{
					continue;
				}

				_revealPreAnimationPreviewTile?.Invoke(tileId);
				_renderTileFromBoard?.Invoke(tileId);

				Vector3Int pos = _getPositionForTileId(tileId);
				bool hasMold = moldTilemap.HasTile(pos);
				bool hasOverlay = overlayTilemap.HasTile(pos);
				if (!hasMold && !hasOverlay)
				{
					continue;
				}

				Color moldColor = hasMold ? moldTilemap.GetColor(pos) : Color.white;
				Color overlayColor = hasOverlay ? overlayTilemap.GetColor(pos) : Color.white;
				states.Add((pos, hasMold, moldColor, hasOverlay, overlayColor));

				if (hasMold)
				{
					moldTilemap.SetTileFlags(pos, TileFlags.None);
					moldTilemap.SetColor(pos, new Color(moldColor.r, moldColor.g, moldColor.b, 0f));
				}

				if (hasOverlay)
				{
					overlayTilemap.SetTileFlags(pos, TileFlags.None);
					overlayTilemap.SetColor(pos, new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f));
				}
			}

			if (states.Count == 0)
			{
				yield break;
			}

			float duration = Mathf.Max(0.01f, UIEffectConstants.DirectedVectorChunkPulseDurationSeconds * 0.8f);

			_beginAnimation();
			try
			{
				float elapsed = 0f;
				while (elapsed < duration)
				{
					elapsed += Time.deltaTime;
					float t = Mathf.Clamp01(elapsed / duration);
					float eased = 1f - Mathf.Pow(1f - t, 3f);

					for (int i = 0; i < states.Count; i++)
					{
						var state = states[i];
						if (state.hasMold)
						{
							moldTilemap.SetColor(state.pos, new Color(state.moldColor.r, state.moldColor.g, state.moldColor.b, state.moldColor.a * eased));
						}

						if (state.hasOverlay)
						{
							overlayTilemap.SetColor(state.pos, new Color(state.overlayColor.r, state.overlayColor.g, state.overlayColor.b, state.overlayColor.a * eased));
						}
					}

					yield return null;
				}
			}
			finally
			{
				for (int i = 0; i < states.Count; i++)
				{
					var state = states[i];
					if (state.hasMold)
					{
						moldTilemap.SetColor(state.pos, state.moldColor);
					}

					if (state.hasOverlay)
					{
						overlayTilemap.SetColor(state.pos, state.overlayColor);
					}
				}

				_endAnimation();
			}
		}

		private static List<int> BuildDirectedVectorPresentationTileIds(GameBoard board, int playerId, int originTileId, IReadOnlyList<int> affectedTileIds)
		{
			var result = new List<int>();
			var seenTileIds = new HashSet<int>();

			if (affectedTileIds == null || affectedTileIds.Count == 0)
			{
				return result;
			}

			int finalLandingTileId = affectedTileIds[affectedTileIds.Count - 1];
			int startTileId = board.Players.FirstOrDefault(player => player.PlayerId == playerId)?.StartingTileId ?? originTileId;

			AppendSegment(board, startTileId, originTileId, includeStart: true, result, seenTileIds);
			AppendSegment(board, originTileId, finalLandingTileId, includeStart: startTileId == originTileId, result, seenTileIds);

			if (result.Count == 0)
			{
				foreach (int tileId in affectedTileIds)
				{
					if (tileId >= 0 && seenTileIds.Add(tileId))
					{
						result.Add(tileId);
					}
				}
			}

			return result;
		}

		private static void AppendSegment(GameBoard board, int startTileId, int endTileId, bool includeStart, List<int> result, HashSet<int> seenTileIds)
		{
			if (board == null || startTileId < 0 || endTileId < 0)
			{
				return;
			}

			var path = GetInclusiveDirectedVectorPath(board, startTileId, endTileId);
			for (int index = 0; index < path.Count; index++)
			{
				if (!includeStart && index == 0)
				{
					continue;
				}

				int tileId = path[index];
				if (seenTileIds.Add(tileId))
				{
					result.Add(tileId);
				}
			}
		}

		private static List<int> GetInclusiveDirectedVectorPath(GameBoard board, int startTileId, int endTileId)
		{
			var pathTileIds = new List<int>();
			if (board == null || startTileId < 0 || endTileId < 0)
			{
				return pathTileIds;
			}

			var (startX, startY) = board.GetXYFromTileId(startTileId);
			var (endX, endY) = board.GetXYFromTileId(endTileId);
			pathTileIds.Add(startTileId);

			if (startTileId == endTileId)
			{
				return pathTileIds;
			}

			int dx = endX - startX;
			int dy = endY - startY;
			int maxSteps = Math.Max(Math.Abs(dx), Math.Abs(dy));
			if (maxSteps <= 0)
			{
				return pathTileIds;
			}

			float stepX = dx / (float)maxSteps;
			float stepY = dy / (float)maxSteps;
			float cx = startX + 0.5f;
			float cy = startY + 0.5f;

			for (int step = 0; step < maxSteps; step++)
			{
				cx += stepX;
				cy += stepY;

				int ix = (int)Math.Floor(cx);
				int iy = (int)Math.Floor(cy);
				var tile = board.GetTile(ix, iy);
				if (tile == null)
				{
					break;
				}

				if (pathTileIds[pathTileIds.Count - 1] != tile.TileId)
				{
					pathTileIds.Add(tile.TileId);
				}

				if (tile.TileId == endTileId)
				{
					break;
				}
			}

			if (pathTileIds[pathTileIds.Count - 1] != endTileId)
			{
				pathTileIds.Add(endTileId);
			}

			return pathTileIds;
		}

		public void DestroyLingeringToasts()
		{
			Transform toastParent = _getToastParent();
			for (int i = toastParent.childCount - 1; i >= 0; i--)
			{
				Transform child = toastParent.GetChild(i);
				if (child != null && (child.name == "NutrientPatchToast" || child.name == "DirectedVectorToast"))
				{
					UnityEngine.Object.Destroy(child.gameObject);
				}
			}
		}

		private Tilemap GetTransientPulseTilemap()
		{
			return _getPingOverlayTilemap() ?? _getHoverOverlayTilemap() ?? _getOverlayTilemap();
		}

		private static IEnumerable<List<int>> BuildDirectedVectorChunks(IReadOnlyList<int> orderedTileIds)
		{
			int tileCount = orderedTileIds.Count;
			if (tileCount == 0)
			{
				yield break;
			}

			int chunkCount = tileCount <= 2 ? tileCount : Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(tileCount)), UIEffectConstants.DirectedVectorChunkCountMin, UIEffectConstants.DirectedVectorChunkCountMax);
			int chunkSize = Mathf.CeilToInt(tileCount / (float)Mathf.Max(1, chunkCount));

			for (int index = 0; index < tileCount; index += chunkSize)
			{
				yield return orderedTileIds.Skip(index).Take(chunkSize).ToList();
			}
		}

		private IEnumerator PulseTiles(Tilemap targetTilemap, IReadOnlyList<Vector3Int> positions, float duration, Color pulseColor, float maxAlpha, float pulseScale)
		{
			TileBase highlightTile = _getSolidHighlightTile();
			if (targetTilemap == null || positions == null || positions.Count == 0 || highlightTile == null)
			{
				yield break;
			}

			foreach (var pos in positions)
			{
				targetTilemap.SetTile(pos, highlightTile);
				targetTilemap.SetTileFlags(pos, TileFlags.None);
			}

			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
				float eased = Mathf.Sin(t * Mathf.PI);
				float alpha = Mathf.Lerp(0.12f, maxAlpha, eased);
				float scale = Mathf.Lerp(1f, pulseScale, eased);

				foreach (var pos in positions)
				{
					Color color = pulseColor;
					color.a = alpha;
					targetTilemap.SetColor(pos, color);
					targetTilemap.SetTransformMatrix(pos, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f)));
				}

				yield return null;
			}

			ClearPulseTiles(targetTilemap, positions, highlightTile);
		}

		private static void ClearPulseTiles(Tilemap targetTilemap, IEnumerable<Vector3Int> positions, TileBase highlightTile)
		{
			if (targetTilemap == null || positions == null)
			{
				return;
			}

			foreach (var pos in positions)
			{
				targetTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
				targetTilemap.SetColor(pos, Color.white);
				if (targetTilemap.GetTile(pos) == highlightTile)
				{
					targetTilemap.SetTile(pos, null);
				}
			}
		}

		private static void ApplyMycotoxicLashColors(IReadOnlyList<(Vector3Int pos, bool hasMold, Color moldColor, bool hasOverlay, Color overlayColor)> states, float t, Tilemap moldTilemap, Tilemap overlayTilemap)
		{
			for (int i = 0; i < states.Count; i++)
			{
				var state = states[i];
				if (state.hasMold && moldTilemap.HasTile(state.pos))
				{
					Color darkestMold = new(0f, 0f, 0f, state.moldColor.a);
					moldTilemap.SetColor(state.pos, Color.Lerp(state.moldColor, darkestMold, t));
				}

				if (state.hasOverlay && overlayTilemap.HasTile(state.pos))
				{
					Color darkestOverlay = new(0f, 0f, 0f, state.overlayColor.a);
					overlayTilemap.SetColor(state.pos, Color.Lerp(state.overlayColor, darkestOverlay, t));
				}
			}
		}

		private static float GetNutrientToastScaleMultiplier()
		{
			Camera mainCamera = Camera.main;
			if (mainCamera == null || !mainCamera.orthographic)
			{
				return UIEffectConstants.BoardToastScaleMultiplier;
			}

			float normalizedZoom = mainCamera.orthographicSize / UIEffectConstants.NutrientPatchToastZoomReferenceOrthographicSize;
			float zoomScale = Mathf.Sqrt(Mathf.Max(1f, normalizedZoom));
			return Mathf.Clamp(zoomScale, 1f, UIEffectConstants.NutrientPatchToastMaxScaleMultiplier) * UIEffectConstants.BoardToastScaleMultiplier;
		}

		private static float GetAnimatedNutrientToastScaleMultiplier(float textT)
		{
			float baseScale = GetNutrientToastScaleMultiplier();
			float popIn = 1f - Mathf.Pow(1f - Mathf.Clamp01(textT / 0.18f), 3f);
			float settle = Mathf.Clamp01((textT - 0.18f) / 0.26f);
			float minScale = UIEffectConstants.NutrientPatchToastMinScaleMultiplier;
			float popScale = UIEffectConstants.NutrientPatchToastPopScaleMultiplier;
			float animatedScale = Mathf.Lerp(minScale, popScale, popIn);
			animatedScale = Mathf.Lerp(animatedScale, 1f, settle);
			return baseScale * animatedScale;
		}

		private static Color GetNutrientToastColor(NutrientPatchType patchType)
		{
			return patchType switch
			{
				NutrientPatchType.Adaptogen => AdaptogenPatchTextColor,
				NutrientPatchType.Sporemeal => SporemealPatchTextColor,
				NutrientPatchType.Hypervariation => HypervariationPatchTextColor,
				_ => AdaptogenPatchTextColor
			};
		}

		private static string BuildNutrientToastText(NutrientRewardType rewardType, int rewardAmount)
		{
			return rewardType switch
			{
				NutrientRewardType.MutationPoints => rewardAmount == 1 ? "+1 Mutation Point!" : $"+{rewardAmount} Mutation Points!",
				NutrientRewardType.FreeGrowth => rewardAmount == 1 ? "1 Cell Grown!" : $"{rewardAmount} Cells Grown!",
				NutrientRewardType.MycovariantDraft => "Mycovariant Draft!",
				_ => "Nutrient Claimed!"
			};
		}

		private static string BuildDirectedVectorToastText()
		{
			return "Chemotactic vectoring toward beacon!";
		}

		private TextMeshPro CreateNutrientToastText(Vector3Int destinationPos, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount, Tilemap overlayTilemap)
		{
			var textObject = new GameObject("NutrientPatchToast", typeof(TextMeshPro));
			textObject.transform.SetParent(_getToastParent(), false);

			var tmp = textObject.GetComponent<TextMeshPro>();
			tmp.text = BuildNutrientToastText(rewardType, rewardAmount);
			tmp.fontSize = UIEffectConstants.NutrientPatchToastFontSize;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.textWrappingMode = TextWrappingModes.NoWrap;
			tmp.fontStyle = FontStyles.Bold;
			tmp.color = GetNutrientToastColor(patchType);
			tmp.outlineWidth = 0.32f;
			tmp.outlineColor = new Color(0.24f, 0.12f, 0.02f, 1f);
			tmp.transform.position = overlayTilemap.GetCellCenterWorld(destinationPos) + new Vector3(0f, UIEffectConstants.NutrientPatchToastStartHeightWorld, 0f);
			tmp.transform.localScale = Vector3.one * (GetNutrientToastScaleMultiplier() * UIEffectConstants.NutrientPatchToastMinScaleMultiplier);

			var renderer = tmp.GetComponent<MeshRenderer>();
			if (renderer != null)
			{
				renderer.sortingOrder = 60;
			}

			return tmp;
		}

		private TextMeshPro CreateDirectedVectorToastText(IReadOnlyList<int> affectedTileIds, Tilemap overlayTilemap)
		{
			if (affectedTileIds == null || affectedTileIds.Count == 0)
			{
				return null;
			}

			Vector3 averageWorld = Vector3.zero;
			foreach (int tileId in affectedTileIds)
			{
				averageWorld += overlayTilemap.GetCellCenterWorld(_getPositionForTileId(tileId));
			}

			averageWorld /= affectedTileIds.Count;
			var textObject = new GameObject("DirectedVectorToast", typeof(TextMeshPro));
			textObject.transform.SetParent(_getToastParent(), false);

			var tmp = textObject.GetComponent<TextMeshPro>();
			tmp.text = BuildDirectedVectorToastText();
			tmp.fontSize = UIEffectConstants.DirectedVectorToastFontSize;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.textWrappingMode = TextWrappingModes.NoWrap;
			tmp.fontStyle = FontStyles.Bold;
			tmp.color = DirectedVectorToastColor;
			tmp.outlineWidth = 0.32f;
			tmp.outlineColor = new Color(0.18f, 0.11f, 0.02f, 1f);
			tmp.transform.position = averageWorld + new Vector3(0f, UIEffectConstants.DirectedVectorToastStartHeightWorld, 0f);
			tmp.transform.localScale = Vector3.one * GetNutrientToastScaleMultiplier();

			var renderer = tmp.GetComponent<MeshRenderer>();
			if (renderer != null)
			{
				renderer.sortingOrder = 61;
			}

			return tmp;
		}

		private static IEnumerator AnimateFloatingToast(TextMeshPro toast, float duration, Color baseColor, float riseWorld, bool useAnimatedScale)
		{
			if (toast == null)
			{
				yield break;
			}

			Vector3 start = toast.transform.position;
			Vector3 end = start + new Vector3(0f, riseWorld, 0f);
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
				toast.transform.position = Vector3.Lerp(start, end, t);
				toast.transform.localScale = Vector3.one * (useAnimatedScale ? GetAnimatedNutrientToastScaleMultiplier(t) : GetNutrientToastScaleMultiplier());
				Color textColor = baseColor;
				float fadeT = Mathf.Clamp01((t - 0.48f) / 0.52f);
				textColor.a = 1f - fadeT;
				toast.color = textColor;
				yield return null;
			}
		}
	}
}
