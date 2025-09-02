using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Unity.Grid.Helpers;
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
        public Tilemap SelectedTileMap;             // Already selected tiles (solid highlighting)
        public Tilemap HoverOverlayTileMap;         // Temporary mouse hover overlays (single-tile, always on top)
        [Tooltip("Optional dedicated tilemap for transient pings (if null, HoverOverlayTileMap is used)")] public Tilemap PingOverlayTileMap;

        [Header("Tiles")]
        public Tile baseTile;             // Toast base
        public Tile deadTile;             // Dead mold
        public Tile[] playerMoldTiles;    // Living mold, indexed by PlayerId
        public Tile toxinOverlayTile;     // Toxin icon overlay
        [SerializeField] private Tile solidHighlightTile; // For pulses/highlights
        public Tile goldShieldOverlayTile; // Gold shield for resistant cells

        private GameBoard board;
        private GameBoard ActiveBoard => board ?? GameManager.Instance?.Board;

        // Selection highlight state
        private readonly List<Vector3Int> highlightedPositions = new List<Vector3Int>();
        private Coroutine pulseHighlightCoroutine;
        private Color pulseColorA = new Color(1f, 1f, 0.1f, 1f); // Default: yellow
        private Color pulseColorB = new Color(0.1f, 1f, 1f, 1f); // Default: cyan

        // Animation effect tracking
        private HashSet<int> newlyGrownTileIds = new HashSet<int>();
        private Dictionary<int, Coroutine> fadeInCoroutines = new Dictionary<int, Coroutine>();
        private HashSet<int> dyingTileIds = new HashSet<int>();
        private Dictionary<int, Coroutine> deathAnimationCoroutines = new Dictionary<int, Coroutine>();
        private HashSet<int> toxinDropTileIds = new HashSet<int>();
        private Dictionary<int, Coroutine> toxinDropCoroutines = new Dictionary<int, Coroutine>();

        private SelectionHighlightHelper selectionHelper;
        private RingHighlightHelper ringHelper;
        private HoverEffectHelper hoverHelper;

        private Coroutine startingTilePingCoroutine; // track current starting tile ping
        private Tilemap lastPingTilemap; // remember which tilemap the ping used

        // Animation tracking so external code can wait for all visual animations to finish
        private int _activeAnimationCount = 0;
        public bool HasActiveAnimations => _activeAnimationCount > 0;
        internal void BeginAnimation() { _activeAnimationCount++; }
        internal void EndAnimation() { _activeAnimationCount = Mathf.Max(0, _activeAnimationCount - 1); }
        public IEnumerator WaitForAllAnimations()
        {
            while (_activeAnimationCount > 0)
                yield return null;
        }

        private struct TileAnimTransform
        {
            public Vector3 positionOffset; // local offset
            public Vector3 scale;          // local scale
            public static TileAnimTransform Identity => new TileAnimTransform { positionOffset = Vector3.zero, scale = Vector3.one };
        }

        private void Awake()
        {
            // Initialize helpers using serialized references
            selectionHelper = new SelectionHighlightHelper(SelectionHighlightTileMap, SelectedTileMap, solidHighlightTile);
            ringHelper = new RingHighlightHelper(PingOverlayTileMap, HoverOverlayTileMap, solidHighlightTile);
            hoverHelper = new HoverEffectHelper(this, HoverOverlayTileMap, solidHighlightTile);
        }

        public void Initialize(GameBoard board)
        {
            this.board = board;
        }

        public void ShowHoverEffect(Vector3Int cellPos)
        {
            hoverHelper.ShowHoverEffect(cellPos);
        }

        public void ClearHoverEffect()
        {
            hoverHelper.ClearHoverEffect();
        }

        private IEnumerator HoverOutlineGlowAnimation(Vector3Int cellPos)
        {
            // No longer used; behavior lives in HoverEffectHelper
            yield break;
        }

        public void RenderBoard(GameBoard board)
        {
            foreach (var coroutine in fadeInCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                    EndAnimation();
                }
            }
            fadeInCoroutines.Clear();
            
            foreach (var coroutine in deathAnimationCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                    EndAnimation();
                }
            }
            deathAnimationCoroutines.Clear();
            
            foreach (var coroutine in toxinDropCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                    EndAnimation();
                }
            }
            toxinDropCoroutines.Clear();
            
            newlyGrownTileIds.Clear();
            dyingTileIds.Clear();
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
            
            StartFadeInAnimations();
            StartDeathAnimations();
            StartToxinDropAnimations();
        }

        public void HighlightPlayerTiles(int playerId, bool includeStartingTilePing = false)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null) return;
            var tileIds = activeBoard.AllTiles()
                .Where(t => t.FungalCell != null && t.FungalCell.OwnerPlayerId == playerId && (t.FungalCell.CellType == FungalCellType.Alive || t.FungalCell.CellType == FungalCellType.Toxin))
                .Select(t => t.TileId);
            HighlightTiles(tileIds, pulseColorA, pulseColorB);
            if (includeStartingTilePing)
            {
                TriggerStartingTilePing(playerId);
            }
        }

        public void TriggerStartingTilePing(int playerId)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null) return;
            int? startingTileId = GetPlayerStartingTile(playerId);
            if (!startingTileId.HasValue) return;
            var xy = activeBoard.GetXYFromTileId(startingTileId.Value);
            Vector3Int center = new Vector3Int(xy.Item1, xy.Item2, 0);

            var targetTilemap = ringHelper.ChoosePingTarget();
            if (targetTilemap == null || solidHighlightTile == null) return;

            if (startingTilePingCoroutine != null)
            {
                StopCoroutine(startingTilePingCoroutine);
                ringHelper.ClearRingHighlight(lastPingTilemap != null ? lastPingTilemap : targetTilemap);
                startingTilePingCoroutine = null;
                EndAnimation();
            }
            lastPingTilemap = targetTilemap;
            BeginAnimation();
            startingTilePingCoroutine = StartCoroutine(RunStartingTilePing(center, targetTilemap));
        }

        private IEnumerator RunStartingTilePing(Vector3Int center, Tilemap targetTilemap)
        {
            float duration = 1.0f;
            float expandPortion = 0.5f;

            var activeBoard = ActiveBoard;
            float maxRadius = Mathf.Min(10f, Mathf.Max(activeBoard.Width, activeBoard.Height) * 0.25f);
            float ringThickness = 0.6f;
            yield return ringHelper.StartingTilePingAnimation(center, targetTilemap, duration, expandPortion, maxRadius, ringThickness);
            EndAnimation();
        }

        private int? GetPlayerStartingTile(int playerId)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard != null && playerId >= 0 && playerId < activeBoard.Players.Count)
            {
                return activeBoard.Players[playerId].StartingTileId;
            }
            return null;
        }

        private IEnumerator StartingTilePingAnimation(Vector3Int centerPos)
        {
            // obsolete
            yield break;
        }

        private IEnumerator StartingTilePingAnimation(Vector3Int centerPos, Tilemap targetTilemap)
        {
            // obsolete
            yield break;
        }

        private void DrawRingHighlight(Vector3Int centerPos, float radius, float thickness, Color color, Tilemap targetTilemap)
        {
            ringHelper.DrawRingHighlight(centerPos, radius, thickness, color, targetTilemap);
        }

        private void ClearRingHighlight(Tilemap targetTilemap)
        {
            ringHelper.ClearRingHighlight(targetTilemap);
        }

        public void HighlightTiles(IEnumerable<int> tileIds, Color? colorA = null, Color? colorB = null)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null) return;
            selectionHelper.HighlightTiles(tileIds, activeBoard, highlightedPositions);

            Color a = colorA ?? new Color(1f, 1f, 0.1f, 1f);
            Color b = colorB ?? new Color(0.1f, 1f, 1f, 1f);

            if (highlightedPositions.Count > 0)
            {
                if (pulseHighlightCoroutine != null)
                    StopCoroutine(pulseHighlightCoroutine);

                pulseHighlightCoroutine = StartCoroutine(selectionHelper.PulseHighlightTiles(
                    highlightedPositions,
                    new Color(1f, 0f, 0.9f, 0f),
                    new Color(1f, 0f, 0.9f, 1f),
                    0.4f));
            }
        }

        public void HighlightTiles(IDictionary<int, (Color colorA, Color colorB)> tileHighlights)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null) return;
            selectionHelper.HighlightTiles(tileHighlights, activeBoard, highlightedPositions);

            if (pulseHighlightCoroutine != null)
            {
                StopCoroutine(pulseHighlightCoroutine);
                pulseHighlightCoroutine = null;
            }
        }

        public void ShowSelectedTiles(IEnumerable<int> tileIds, Color? selectedColor = null)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null) return;
            selectionHelper.ShowSelectedTiles(tileIds, activeBoard, selectedColor);
        }

        public void ClearSelectedTiles()
        {
            selectionHelper.ClearSelectedTiles();
        }

        public void ClearHighlights()
        {
            selectionHelper.ClearHighlights(highlightedPositions);
            if (pulseHighlightCoroutine != null)
            {
                StopCoroutine(pulseHighlightCoroutine);
                pulseHighlightCoroutine = null;
            }
        }

        public void ClearAllHighlights()
        {
            ClearHighlights();
            ClearSelectedTiles();
        }

        private IEnumerator PulseHighlightTiles()
        {
            float pulseDuration = 0.4f;    // Fast pulse for urgency
            Color transparentColor = new Color(1f, 0f, 0.9f, 0f);
            Color brightColor = new Color(1f, 0f, 0.9f, 1f);

            while (true)
            {
                float colorT = Mathf.PingPong(Time.time / pulseDuration, 1f);
                float easedColorT = colorT < 0.5f 
                    ? 2f * colorT * colorT  
                    : 1f - 2f * (1f - colorT) * (1f - colorT);  
                
                Color pulseColor = Color.Lerp(transparentColor, brightColor, easedColorT);

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
                        if (cell.IsNewlyGrown)
                        {
                            moldColor = new Color(1f, 1f, 1f, 0.1f);
                        }
                        else
                        {
                            if (cell.GrowthCycleAge < UIEffectConstants.GrowthCycleAgeHighlightTextThreshold)
                            {
                                moldColor = new Color(1f, 1f, 1f, UIEffectConstants.NewGrowthFinalAlpha);
                            }
                            else
                            {
                                moldColor = Color.white;
                            }
                        }
                    }
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
                    overlayTile = deadTile;
                    overlayColor = cell.IsDying ? new Color(1f, 1f, 1f, 0f) : Color.white;
                    break;

                case FungalCellType.Toxin:
                    if (cell.OwnerPlayerId is int idT && idT >= 0 && idT < playerMoldTiles.Length)
                    {
                        moldTile = playerMoldTiles[idT];
                        moldColor = Color.white;
                    }
                    overlayTile = toxinOverlayTile;
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

        private void StartFadeInAnimations()
        {
            foreach (int tileId in newlyGrownTileIds)
            {
                if (fadeInCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(fadeInCoroutines[tileId]);
                    EndAnimation();
                }
                fadeInCoroutines[tileId] = StartCoroutine(FadeInCell(tileId));
            }
        }

        private IEnumerator FadeInCell(int tileId)
        {
            var xy = board.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);
            
            float duration = UIEffectConstants.CellGrowthFadeInDurationSeconds;
            float startAlpha = 0.1f;
            float targetAlpha = 1f;
            float elapsed = 0f;

            BeginAnimation();
            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
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

                float flashElapsed = 0f;
                Color originalColor = moldTilemap.HasTile(pos) ? moldTilemap.GetColor(pos) : Color.white;
                while (flashElapsed < UIEffectConstants.NewGrowthFlashDurationSeconds)
                {
                    flashElapsed += Time.deltaTime;
                    if (moldTilemap.HasTile(pos))
                    {
                        moldTilemap.SetColor(pos, UIEffectConstants.NewGrowthFlashColor);
                    }
                    yield return null;
                }

                if (moldTilemap.HasTile(pos))
                {
                    Color settleColor = Color.white;
                    settleColor.a = UIEffectConstants.NewGrowthFinalAlpha;
                    moldTilemap.SetColor(pos, settleColor);
                }

                var tile = board.GetTileById(tileId);
                if (tile?.FungalCell != null)
                {
                    tile.FungalCell.ClearNewlyGrownFlag();
                }
            }
            finally
            {
                fadeInCoroutines.Remove(tileId);
                EndAnimation();
            }
        }

        private void StartDeathAnimations()
        {
            foreach (int tileId in dyingTileIds)
            {
                if (deathAnimationCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(deathAnimationCoroutines[tileId]);
                    EndAnimation();
                }
                deathAnimationCoroutines[tileId] = StartCoroutine(DeathAnimation(tileId));
            }
        }

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
                    EndAnimation();
                }
                deathAnimationCoroutines[tileId] = StartCoroutine(DeathAnimation(tileId));
            }
        }

        private IEnumerator DeathAnimation(int tileId)
        {
            var xy = board.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);
            
            float duration = UIEffectConstants.CellDeathAnimationDurationSeconds;

            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (cell == null)
            {
                deathAnimationCoroutines.Remove(tileId);
                yield break;
            }

            BeginAnimation();
            try
            {
                Color initialLivingColor = moldTilemap.HasTile(pos) ? moldTilemap.GetColor(pos) : Color.white;
                Color initialOverlayColor = overlayTilemap.HasTile(pos) ? overlayTilemap.GetColor(pos) : Color.white;
                Matrix4x4 initialTransform = moldTilemap.GetTransformMatrix(pos);

                Color deathFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
                
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
                    Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(scaleAmount, scaleAmount, 1f));
                    
                    Color currentLivingColor = Color.Lerp(initialLivingColor, 
                        new Color(initialLivingColor.r * 0.7f, initialLivingColor.g * 0.7f, initialLivingColor.b * 0.7f, 
                        Mathf.Lerp(1f, 0.8f, easedProgress)), easedProgress);
                    
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
                deathAnimationCoroutines.Remove(tileId);
                EndAnimation();
            }
        }

        private void StartToxinDropAnimations()
        {
            foreach (int tileId in toxinDropTileIds)
            {
                if (toxinDropCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(toxinDropCoroutines[tileId]);
                    EndAnimation();
                }
                toxinDropCoroutines[tileId] = StartCoroutine(ToxinDropAnimation(tileId));
            }
        }

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
                    EndAnimation();
                }
                toxinDropCoroutines[tileId] = StartCoroutine(ToxinDropAnimation(tileId));
            }
        }

        private IEnumerator ToxinDropAnimation(int tileId)
        {
            var xy = board.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);
            
            float duration = UIEffectConstants.ToxinDropAnimationDurationSeconds;

            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (cell == null)
            {
                toxinDropCoroutines.Remove(tileId);
                yield break;
            }

            BeginAnimation();
            try
            {
                Color initialMoldColor = moldTilemap.HasTile(pos) ? moldTilemap.GetColor(pos) : Color.white;
                Color initialOverlayColor = overlayTilemap.HasTile(pos) ? overlayTilemap.GetColor(pos) : Color.white;

                if (!overlayTilemap.HasTile(pos) && toxinOverlayTile != null)
                {
                    overlayTilemap.SetTile(pos, toxinOverlayTile);
                    overlayTilemap.SetTileFlags(pos, TileFlags.None);
                    overlayTilemap.SetColor(pos, Color.clear);
                }

                if (moldTilemap.HasTile(pos))
                {
                    var c = moldTilemap.GetColor(pos);
                    c.a = 0f;
                    moldTilemap.SetColor(pos, c);
                }

                float startYOffset = UIEffectConstants.ToxinDropStartYOffset;
                ApplyOverlayTransform(pos, new Vector3(0f, startYOffset, 0f), Vector3.one);

                float approachPortion = Mathf.Clamp01(UIEffectConstants.ToxinDropApproachPortion);
                float approachDuration = duration * approachPortion;
                float approachElapsed = 0f;
                while (approachElapsed < approachDuration)
                {
                    approachElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(approachElapsed / approachDuration);
                    float eased = t * t * t;
                    float yOffset = Mathf.Lerp(startYOffset, 0f, eased);
                    ApplyOverlayTransform(pos, new Vector3(0f, yOffset, 0f), Vector3.one);
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
                    ApplyOverlayTransform(pos, Vector3.zero, new Vector3(sx, sy, 1f));

                    if (overlayTilemap.HasTile(pos))
                    {
                        Color c = overlayTilemap.GetColor(pos);
                        c.a = Mathf.Lerp(0f, 1f, t);
                        overlayTilemap.SetColor(pos, c);
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
                    ApplyOverlayTransform(pos, Vector3.zero, new Vector3(sx, sy, 1f));
                    yield return null;
                }

                ApplyOverlayTransform(pos, Vector3.zero, Vector3.one);
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
                toxinDropCoroutines.Remove(tileId);
                EndAnimation();
            }
        }

        private void ApplyOverlayTransform(Vector3Int pos, Vector3 localOffset, Vector3 localScale)
        {
            var trs = Matrix4x4.TRS(localOffset, Quaternion.identity, localScale);
            overlayTilemap.SetTransformMatrix(pos, trs);
        }

        // NEW: Resistant drop animation for Surgical Inoculation (Option A)
        public IEnumerator ResistantDropAnimation(int tileId)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null || goldShieldOverlayTile == null || overlayTilemap == null)
                yield break;

            var xy = activeBoard.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);

            float total = UIEffectConstants.SurgicalInoculationDropDurationSeconds;
            float dropT = Mathf.Clamp01(UIEffectConstants.SurgicalInoculationDropPortion);
            float impactT = Mathf.Clamp01(UIEffectConstants.SurgicalInoculationImpactPortion);
            float settleT = Mathf.Clamp01(UIEffectConstants.SurgicalInoculationSettlePortion);
            float normSum = dropT + impactT + settleT;
            if (normSum <= 0f) normSum = 1f;
            dropT /= normSum; impactT /= normSum; settleT /= normSum;

            float dropDur = total * dropT;
            float impactDur = total * impactT;
            float settleDur = total * settleT;

            // Ensure shield tile exists at pos (we animate overlayTilemap transform)
            overlayTilemap.SetTile(pos, goldShieldOverlayTile);
            overlayTilemap.SetTileFlags(pos, TileFlags.None);
            overlayTilemap.SetColor(pos, Color.white);

            // Begin animation tracking
            BeginAnimation();
            try
            {
                // Phase 1: Drop (ease-in cubic), start high and large, spin while shrinking to 1.0
                float startYOffset = UIEffectConstants.SurgicalInoculationDropStartYOffset;
                float startScale = UIEffectConstants.SurgicalInoculationDropStartScale;
                float spinTurns = UIEffectConstants.SurgicalInoculationDropSpinTurns;

                float t = 0f;
                while (t < dropDur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / dropDur);
                    float eased = u * u * u; // ease-in cubic
                    float yOff = Mathf.Lerp(startYOffset, 0f, eased);
                    float s = Mathf.Lerp(startScale, 1f, eased);
                    float angle = Mathf.Lerp(0f, 360f * spinTurns, eased);
                    var rot = Quaternion.Euler(0f, 0f, angle);
                    var trs = Matrix4x4.TRS(new Vector3(0f, yOff, 0f), rot, new Vector3(s, s, 1f));
                    overlayTilemap.SetTransformMatrix(pos, trs);
                    yield return null;
                }

                // Phase 2: Impact squash (ease-out), optional ring ripple
                float squashX = UIEffectConstants.SurgicalInoculationImpactSquashX;
                float squashY = UIEffectConstants.SurgicalInoculationImpactSquashY;
                t = 0f;
                // Trigger ring pulse at impact start
                StartCoroutine(ImpactRingPulse(pos));
                while (t < impactDur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / impactDur);
                    float eased = 1f - (1f - u) * (1f - u); // ease-out
                    float sx = Mathf.Lerp(1f, squashX, eased);
                    float sy = Mathf.Lerp(1f, squashY, eased);
                    var trs = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));
                    overlayTilemap.SetTransformMatrix(pos, trs);
                    yield return null;
                }

                // Phase 3: Settle back to scale 1
                t = 0f;
                while (t < settleDur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / settleDur);
                    float sx = Mathf.Lerp(squashX, 1f, u);
                    float sy = Mathf.Lerp(squashY, 1f, u);
                    var trs = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));
                    overlayTilemap.SetTransformMatrix(pos, trs);
                    yield return null;
                }

                // Ensure final transform reset
                overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
            }
            finally
            {
                EndAnimation();
            }
        }

        private IEnumerator ImpactRingPulse(Vector3Int centerPos)
        {
            var targetTilemap = PingOverlayTileMap != null ? PingOverlayTileMap : HoverOverlayTileMap;
            if (targetTilemap == null || solidHighlightTile == null)
                yield break;

            float duration = UIEffectConstants.SurgicalInoculationRingPulseDurationSeconds;
            float maxRadius = 2.5f;
            float ringThickness = 0.6f;
            float startTime = Time.time;

            while (Time.time - startTime < duration)
            {
                float u = Mathf.Clamp01((Time.time - startTime) / duration);
                float radius = Mathf.Lerp(0.3f, maxRadius, u);
                Color ringColor = new Color(1f, 0.95f, 0.5f, 0.9f * (1f - u));
                DrawRingHighlight(centerPos, radius, ringThickness, ringColor, targetTilemap);
                yield return null;
            }
            ClearRingHighlight(targetTilemap);
        }
    }

    internal static class FungalCellVisualExtensions
    {
        public static bool GrewThisRoundVisual(this FungalCell cell, int currentRound)
        {
            try
            {
                var prop = typeof(FungalCell).GetProperty("BirthRound");
                if (prop != null)
                {
                    object value = prop.GetValue(cell, null);
                    if (value is int birthRound)
                        return birthRound == currentRound;
                }
            }
            catch { }
            return false;
        }
    }
}
