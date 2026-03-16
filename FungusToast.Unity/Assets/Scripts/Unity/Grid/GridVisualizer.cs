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
    public partial class GridVisualizer : MonoBehaviour
    {
        // Timing context passed in from GameManager per post-growth sequence
        private PostGrowthPhaseTiming _timingContext; // default values = 0 => fallback to constants
        public void SetPostGrowthTiming(PostGrowthPhaseTiming timing) => _timingContext = timing;

        [Header("Timing / Tweaks")]
        [Tooltip("Multiplier for post-growth phase reclaim & resistance animations durations (1 = original speed, >1 slower)")]
        [Range(0.1f,5f)] public float postGrowthPhaseDurationMultiplier = 1.5f;

        [Tooltip("Additional multiplier ONLY for Regenerative Hyphae reclaim durations (stacked on general multiplier). Set higher if animation flashes too fast.")]
        [Range(0.1f,6f)] public float regenerativeHyphaeDurationMultiplier = 3f; // legacy multiplier (may be unused when explicit total supplied)

        [Header("Tilemaps")]
        public Tilemap toastTilemap;
        public Tilemap moldTilemap;
        public Tilemap overlayTilemap;
        [Tooltip("Optional dedicated tilemap for decorative board borders. Falls back to Toast Tilemap when omitted.")]
        public Tilemap crustTilemap;
        public Tilemap SelectionHighlightTileMap;
        public Tilemap SelectedTileMap;
        public Tilemap HoverOverlayTileMap;
        [Tooltip("Optional dedicated tilemap for transient pings (if null, HoverOverlayTileMap is used)")]
        public Tilemap PingOverlayTileMap;

        [Header("Tiles")]
        public Tile baseTile;
        public Tile deadTile;
        public Tile[] playerMoldTiles;
        public Tile toxinOverlayTile;
        [SerializeField] private Tile solidHighlightTile;
        public Tile goldShieldOverlayTile;

        [Header("Board Medium")]
        [SerializeField] private BoardMediumConfig defaultBoardMedium;

        private BoardMediumConfig runtimeBoardMedium;
        private static readonly Matrix4x4 IdentityMatrix = Matrix4x4.identity;

        private GameBoard board;
        public GameBoard ActiveBoard => board ?? GameManager.Instance?.Board; // now public for helper access
        public BoardMediumConfig ActiveBoardMedium => runtimeBoardMedium != null ? runtimeBoardMedium : defaultBoardMedium;
        public int CurrentBoardVisualPaddingTiles => board == null ? 0 : GetCrustThickness(board);

        // Selection highlight state
        private readonly List<Vector3Int> highlightedPositions = new();
        private Coroutine pulseHighlightCoroutine;
        private Color pulseColorA = new(1f, 1f, 0.1f, 1f);
        private Color pulseColorB = new(0.1f, 1f, 1f, 1f);

        // Animation effect tracking
        private readonly HashSet<int> newlyGrownTileIds = new();
        private readonly HashSet<int> newlyGrownAnimationPlayedTileIds = new();
        private readonly Dictionary<int, Coroutine> fadeInCoroutines = new();
        private readonly HashSet<int> dyingTileIds = new();
        private readonly Dictionary<int, Coroutine> deathAnimationCoroutines = new();
        private readonly HashSet<int> toxinDropTileIds = new();
        private readonly Dictionary<int, Coroutine> toxinDropCoroutines = new();
        private readonly HashSet<int> deferredResistanceOverlayTileIds = new();

        private SelectionHighlightHelper selectionHelper;
        private RingHighlightHelper ringHelper;
        private HoverEffectHelper hoverHelper;
        private ArcProjectileHelper arcHelper;

        private Coroutine startingTilePingCoroutine;
        private Tilemap lastPingTilemap;

        // Animators (new helpers replacing partial class split)
        private Animation.RegenerativeHyphaeReclaimAnimator _reclaimAnimator;
        private Animation.SurgicalInoculationAnimator _surgicalAnimator; // ensure correct reference
        private Animation.StartingSporeArrivalAnimator _startingSporeAnimator; // NEW: starting spores
        private Animation.ConidialRelayAnimator _conidialRelayAnimator;

        // Animation tracking so external code can wait for all visual animations to finish
        private int _activeAnimationCount = 0;
        public bool HasActiveAnimations => _activeAnimationCount > 0;
        internal void BeginAnimation() => _activeAnimationCount++;
        internal void EndAnimation() => _activeAnimationCount = Mathf.Max(0, _activeAnimationCount - 1);
        public IEnumerator WaitForAllAnimations() { while (_activeAnimationCount > 0) yield return null; }

        private void Awake()
        {
            selectionHelper = new SelectionHighlightHelper(SelectionHighlightTileMap, SelectedTileMap, solidHighlightTile);
            ringHelper = new RingHighlightHelper(PingOverlayTileMap, HoverOverlayTileMap, solidHighlightTile);
            hoverHelper = new HoverEffectHelper(this, HoverOverlayTileMap, solidHighlightTile);
            arcHelper = new ArcProjectileHelper(this, overlayTilemap);
            _reclaimAnimator = new Animation.RegenerativeHyphaeReclaimAnimator(this);
            _surgicalAnimator = new Animation.SurgicalInoculationAnimator(this);
            _startingSporeAnimator = new Animation.StartingSporeArrivalAnimator(this); // NEW
            _conidialRelayAnimator = new Animation.ConidialRelayAnimator(this);
        }

        public void Initialize(GameBoard board) => this.board = board;
        public void SetBoardMedium(BoardMediumConfig boardMedium) => runtimeBoardMedium = boardMedium;
        public void ClearBoardMediumOverride() => runtimeBoardMedium = null;
        public bool IsPlayableBoardCell(Vector3Int cellPos)
            => board != null && cellPos.x >= 0 && cellPos.x < board.Width && cellPos.y >= 0 && cellPos.y < board.Height;

        // === Public wrappers for extracted reclaim & surgical animations ===
        public void PlayRegenerativeHyphaeReclaimBatch(IReadOnlyList<int> tileIds, float scaleMultiplier, float explicitTotalSeconds)
            => _reclaimAnimator.PlayBatch(tileIds, scaleMultiplier, explicitTotalSeconds);
        public IEnumerator SurgicalInoculationArcAnimation(int playerId, int targetTileId, Sprite sprite)
            => _surgicalAnimator.RunArcAndDrop(playerId, targetTileId, sprite);
        public IEnumerator PlayStartingSporeArrivalAnimation(IEnumerable<int> startingTileIds)
            => _startingSporeAnimator != null ? _startingSporeAnimator.Play(startingTileIds) : null;
        public IEnumerator PlayConidialRelayAnimation(int playerId, int sourceTileId, int destinationTileId)
            => _conidialRelayAnimator != null ? _conidialRelayAnimator.Play(playerId, sourceTileId, destinationTileId) : null;
        public IEnumerator PlayDistalSporeAnimation(int playerId, int sourceTileId, int destinationTileId)
            => _conidialRelayAnimator != null ? _conidialRelayAnimator.Play(playerId, sourceTileId, destinationTileId, preserveSourceCell: true) : null;
        public IEnumerator PlayMycotoxicLashAnimation(IReadOnlyList<int> tileIds)
        {
            if (board == null || tileIds == null || tileIds.Count == 0)
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
                bool hasMold = moldTilemap != null && moldTilemap.HasTile(pos);
                bool hasOverlay = overlayTilemap != null && overlayTilemap.HasTile(pos);
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

            BeginAnimation();
            try
            {
                float elapsed = 0f;
                while (elapsed < fadeToBlackDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = fadeToBlackDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeToBlackDuration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    ApplyMycotoxicLashColors(states, eased);
                    yield return null;
                }

                ApplyMycotoxicLashColors(states, 1f);

                if (blackHoldDuration > 0f)
                {
                    yield return new WaitForSeconds(blackHoldDuration);
                }
            }
            finally
            {
                EndAnimation();
            }
        }

        public IEnumerator PlayRetrogradeBloomAnimation(int anchorTileId)
        {
            if (board == null)
            {
                yield break;
            }

            var highlightTilemap = PingOverlayTileMap != null ? PingOverlayTileMap : HoverOverlayTileMap;
            if (anchorTileId >= 0)
            {
                yield return BastionResistantPulseAnimation(anchorTileId, 0.85f);
            }

            if (highlightTilemap == null || solidHighlightTile == null || anchorTileId < 0)
            {
                yield break;
            }

            var (x, y) = board.GetXYFromTileId(anchorTileId);
            var center = new Vector3Int(x, y, 0);
            float duration = UIEffectConstants.RetrogradeBloomAnimationDurationSeconds;

            BeginAnimation();
            try
            {
                float startTime = Time.time;
                while (Time.time - startTime < duration)
                {
                    float u = Mathf.Clamp01((Time.time - startTime) / duration);
                    float radius = Mathf.Lerp(0.35f, 2.8f, u);
                    Color ringColor = Color.Lerp(new Color(1f, 0.85f, 0.25f, 0.95f), new Color(1f, 0.45f, 0.12f, 0f), u);
                    InternalDrawRing(center, radius, 0.45f, ringColor, highlightTilemap);
                    yield return null;
                }
            }
            finally
            {
                InternalClearRing(highlightTilemap);
                EndAnimation();
            }
        }

        public IEnumerator PlaySaprophageRingAnimation(IReadOnlyList<int> resistantTileIds, IReadOnlyList<int> consumedTileIds)
        {
            if (board == null || consumedTileIds == null || consumedTileIds.Count == 0)
            {
                yield break;
            }

            var highlightTilemap = PingOverlayTileMap != null ? PingOverlayTileMap : HoverOverlayTileMap;
            if (highlightTilemap == null || solidHighlightTile == null)
            {
                yield break;
            }

            var resistantSources = (resistantTileIds ?? System.Array.Empty<int>()).Distinct().ToList();
            foreach (var sourceTileId in resistantSources)
            {
                StartCoroutine(BastionResistantPulseAnimation(sourceTileId, 0.75f));
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
                highlightTilemap.SetTile(pos, solidHighlightTile);
                highlightTilemap.SetTileFlags(pos, TileFlags.None);
            }

            float duration = UIEffectConstants.SaprophageRingAnimationDurationSeconds;
            BeginAnimation();
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

                EndAnimation();
            }
        }

        private void ApplyMycotoxicLashColors(
            IReadOnlyList<(Vector3Int pos, bool hasMold, Color moldColor, bool hasOverlay, Color overlayColor)> states,
            float t)
        {
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state.hasMold && moldTilemap != null && moldTilemap.HasTile(state.pos))
                {
                    Color darkestMold = new Color(0f, 0f, 0f, state.moldColor.a);
                    moldTilemap.SetColor(state.pos, Color.Lerp(state.moldColor, darkestMold, t));
                }

                if (state.hasOverlay && overlayTilemap != null && overlayTilemap.HasTile(state.pos))
                {
                    Color darkestOverlay = new Color(0f, 0f, 0f, state.overlayColor.a);
                    overlayTilemap.SetColor(state.pos, Color.Lerp(state.overlayColor, darkestOverlay, t));
                }
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
                            moldColor = new Color(1f, 1f, 1f, UIEffectConstants.NewGrowthFinalAlpha);
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
                    if (ShouldRenderResistanceOverlay(tile.TileId, cell))
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

        private bool ShouldRenderResistanceOverlay(int tileId, FungalCell cell)
        {
            return cell != null
                && cell.CellType == FungalCellType.Alive
                && cell.IsResistant
                && goldShieldOverlayTile != null
                && !deferredResistanceOverlayTileIds.Contains(tileId);
        }

        private Vector3Int GetPositionForTileId(int tileId)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null)
            {
                return Vector3Int.zero;
            }

            var xy = activeBoard.GetXYFromTileId(tileId);
            return new Vector3Int(xy.Item1, xy.Item2, 0);
        }

        private void ClearResistanceOverlayTile(int tileId)
        {
            if (overlayTilemap == null)
            {
                return;
            }

            var pos = GetPositionForTileId(tileId);
            overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
            if (overlayTilemap.GetTile(pos) == goldShieldOverlayTile)
            {
                overlayTilemap.SetTile(pos, null);
                overlayTilemap.RefreshTile(pos);
            }
        }

        private void RestoreResistanceOverlayTile(int tileId)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null || overlayTilemap == null || goldShieldOverlayTile == null)
            {
                return;
            }

            var tile = activeBoard.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (!ShouldRenderResistanceOverlay(tileId, cell))
            {
                return;
            }

            var pos = GetPositionForTileId(tileId);
            overlayTilemap.SetTile(pos, goldShieldOverlayTile);
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

            foreach (var tileId in tileIds)
            {
                deferredResistanceOverlayTileIds.Add(tileId);
                ClearResistanceOverlayTile(tileId);
            }
        }

        public void RevealDeferredResistanceOverlays(IReadOnlyList<int> tileIds)
        {
            if (tileIds == null || tileIds.Count == 0)
            {
                return;
            }

            foreach (var tileId in tileIds)
            {
                deferredResistanceOverlayTileIds.Remove(tileId);
                RestoreResistanceOverlayTile(tileId);
            }
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
            foreach (int tileId in newlyGrownTileIds
        )
            {
                if (newlyGrownAnimationPlayedTileIds.Contains(tileId))
                {
                    continue;
                }

                if (fadeInCoroutines.ContainsKey(tileId))
                {
                    StopCoroutine(fadeInCoroutines[tileId]);
                    EndAnimation();
                }
                fadeInCoroutines[tileId] = StartCoroutine(FadeInCell(tileId));
                newlyGrownAnimationPlayedTileIds.Add(tileId);
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

            }
            finally
            {
                fadeInCoroutines.Remove(tileId);
                EndAnimation();
            }
        }

        public void ClearNewlyGrownFlagsForNextGrowthPhase()
        {
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

            newlyGrownTileIds.Clear();
            newlyGrownAnimationPlayedTileIds.Clear();
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

        private void ApplyCompositeTransform(Vector3Int pos, Vector3 localOffset, Vector3 localScale)
        {
            var m = Matrix4x4.TRS(localOffset, Quaternion.identity, localScale);
            if (moldTilemap.HasTile(pos)) moldTilemap.SetTransformMatrix(pos, m);
            if (overlayTilemap.HasTile(pos)) overlayTilemap.SetTransformMatrix(pos, m);
        }

        // NEW: Resistant drop animation for Surgical Inoculation (Option A)
        public IEnumerator ResistantDropAnimation(int tileId, float finalScale = 1f)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null || goldShieldOverlayTile == null || overlayTilemap == null)
                yield break;

            Vector3Int pos = GetPositionForTileId(tileId);

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
                float startScale = UIEffectConstants.SurgicalInoculationDropStartScale * finalScale; // scale starting size too
                float spinTurns = UIEffectConstants.SurgicalInoculationDropSpinTurns;

                float t = 0f;
                while (t < dropDur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / dropDur);
                    float eased = u * u * u; // ease-in cubic
                    float yOff = Mathf.Lerp(startYOffset, 0f, eased);
                    float s = Mathf.Lerp(startScale, finalScale, eased);
                    float angle = Mathf.Lerp(0f, 360f * spinTurns, eased);
                    var rot = Quaternion.Euler(0f, 0f, angle);
                    var trs = Matrix4x4.TRS(new Vector3(0f, yOff, 0f), rot, new Vector3(s, s, 1f));
                    overlayTilemap.SetTransformMatrix(pos, trs);
                    yield return null;
                }

                // Phase 2: Impact squash (ease-out), optional ring ripple
                float squashX = UIEffectConstants.SurgicalInoculationImpactSquashX * finalScale;
                float squashY = UIEffectConstants.SurgicalInoculationImpactSquashY * finalScale;
                t = 0f;
                // Trigger ring pulse at impact start
                StartCoroutine(ImpactRingPulse(pos));
                while (t < impactDur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / impactDur);
                    float eased = 1f - (1f - u) * (1f - u); // ease-out
                    float sx = Mathf.Lerp(finalScale, squashX, eased);
                    float sy = Mathf.Lerp(finalScale, squashY, eased);
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
                    float sx = Mathf.Lerp(squashX, finalScale, u);
                    float sy = Mathf.Lerp(squashY, finalScale, u);
                    var trs = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));
                    overlayTilemap.SetTransformMatrix(pos, trs);
                    yield return null;
                }

                // Maintain final reduced scale instead of resetting to identity if custom scale used
                if (Mathf.Approximately(finalScale, 1f))
                    overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
                else
                    overlayTilemap.SetTransformMatrix(pos, Matrix4x4.Scale(new Vector3(finalScale, finalScale, 1f)));
            }
            finally
            {
                deferredResistanceOverlayTileIds.Remove(tileId);
                RestoreResistanceOverlayTile(tileId);
                EndAnimation();
            }
        }

        // NEW: Shield pulse for Mycelial Bastion on a single tile (zoom out, then back in, no spin)
        public IEnumerator BastionResistantPulseAnimation(int tileId, float scaleMultiplier = 1f)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null || goldShieldOverlayTile == null || overlayTilemap == null)
                yield break;
            var xy = activeBoard.GetXYFromTileId(tileId);
            Vector3Int pos = new Vector3Int(xy.Item1, xy.Item2, 0);

            float baseTotal = UIEffectConstants.MycelialBastionPulseDurationSeconds;
            float total = _timingContext.ResistancePulseTotal > 0f ? _timingContext.ResistancePulseTotal : baseTotal * postGrowthPhaseDurationMultiplier;
            float outT = Mathf.Clamp01(UIEffectConstants.MycelialBastionPulseOutPortion);
            float inT = Mathf.Clamp01(UIEffectConstants.MycelialBastionPulseInPortion);
            float norm = outT + inT; if (norm <= 0f) norm = 1f; outT /= norm; inT /= norm;
            float outDur = total * outT; float inDur = total * inT;
            float baseMaxScale = Mathf.Max(1f, UIEffectConstants.MycelialBastionPulseMaxScale);
            float maxScale = Mathf.Max(1f, baseMaxScale * Mathf.Clamp(scaleMultiplier, 0.1f, 10f));
            float yPopBase = UIEffectConstants.MycelialBastionPulseYOffset;
            float yPop = yPopBase * Mathf.Clamp(scaleMultiplier, 0.1f, 10f);

            overlayTilemap.SetTile(pos, goldShieldOverlayTile);
            overlayTilemap.SetTileFlags(pos, TileFlags.None);
            overlayTilemap.SetColor(pos, Color.white);

            BeginAnimation();
            try
            {
                // Outward pulse
                float t = 0f;
                while (t < outDur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / outDur);
                    float eased = 1f - (1f - u) * (1f - u);
                    float s = Mathf.Lerp(1f, maxScale, eased);
                    float y = Mathf.Lerp(0f, yPop, eased);
                    var trs = Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
                    overlayTilemap.SetTransformMatrix(pos, trs);
                    yield return null;
                }
                // Return pulse
                t = 0f;
                while (t < inDur)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / inDur);
                    float eased = u * u;
                    float s = Mathf.Lerp(maxScale, 1f, eased);
                    float y = Mathf.Lerp(yPop, 0f, eased);
                    var trs = Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
                    overlayTilemap.SetTransformMatrix(pos, trs);
                    yield return null;
                }
                overlayTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
            }
            finally
            {
                EndAnimation();
            }
        }

        // ADD timing-aware version before back compat overloads
        public void PlayResistancePulseBatchScaled(IReadOnlyList<int> tileIds, float scaleMultiplier, bool useTimingContext)
        {
            if (tileIds == null || tileIds.Count == 0) return;
            foreach (var id in tileIds)
                StartCoroutine(BastionResistantPulseAnimation(id, scaleMultiplier));
        }

        public void PlayResistanceDropBatch(IReadOnlyList<int> tileIds, float finalScale = 1f)
        {
            if (tileIds == null || tileIds.Count == 0)
            {
                return;
            }

            foreach (var id in tileIds)
            {
                StartCoroutine(ResistantDropAnimation(id, finalScale));
            }
        }
        // BACK COMPAT overloads (old signatures) -----------------
        public void PlayResistancePulseBatchScaled(IReadOnlyList<int> tileIds, float scaleMultiplier)
        {
            PlayResistancePulseBatchScaled(tileIds, scaleMultiplier, true);
        }

        internal ArcProjectileHelper ArcHelper => arcHelper;
        internal void InternalDrawRing(Vector3Int c, float r, float th, Color col, Tilemap tm) => ringHelper.DrawRingHighlight(c,r,th,col,tm);
        internal void InternalClearRing(Tilemap tm) => ringHelper.ClearRingHighlight(tm);

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
                InternalDrawRing(centerPos, radius, ringThickness, ringColor, targetTilemap);
                yield return null;
            }
            InternalClearRing(targetTilemap);
        }

        #region Public Interaction / Rendering API (restored)
        public void RenderBoard(GameBoard board, bool suppressAnimations)
        {
            // Stop any in-flight animation coroutines
            foreach (var c in fadeInCoroutines.Values) if (c != null) { StopCoroutine(c); EndAnimation(); }
            fadeInCoroutines.Clear();
            foreach (var c in deathAnimationCoroutines.Values) if (c != null) { StopCoroutine(c); EndAnimation(); }
            deathAnimationCoroutines.Clear();
            foreach (var c in toxinDropCoroutines.Values) if (c != null) { StopCoroutine(c); EndAnimation(); }
            toxinDropCoroutines.Clear();

            newlyGrownTileIds.Clear();
            dyingTileIds.Clear();
            toxinDropTileIds.Clear();

            // When suppressing animations (fast-forward completion), clear transient flags so no new coroutines would start later.
            if (suppressAnimations)
            {
                foreach (var tile in board.AllTiles())
                {
                    var fc = tile.FungalCell;
                    if (fc == null) continue;
                    if (fc.IsNewlyGrown) fc.ClearNewlyGrownFlag();
                    if (fc.IsDying) fc.ClearDyingFlag();
                    if (fc.IsReceivingToxinDrop) fc.ClearToxinDropFlag();
                }

                newlyGrownAnimationPlayedTileIds.Clear();
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        var tile = board.Grid[x, y];
                        if (tile.FungalCell?.IsNewlyGrown == true) newlyGrownTileIds.Add(tile.TileId);
                        if (tile.FungalCell?.IsDying == true) dyingTileIds.Add(tile.TileId);
                        if (tile.FungalCell?.IsReceivingToxinDrop == true) toxinDropTileIds.Add(tile.TileId);
                    }
                }
            }

            toastTilemap.ClearAllTiles();
            moldTilemap.ClearAllTiles();
            overlayTilemap.ClearAllTiles();
            var targetCrustTilemap = GetCrustTargetTilemap();
            if (targetCrustTilemap != null)
            {
                targetCrustTilemap.ClearAllTiles();
            }

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var tile = board.Grid[x, y];
                    toastTilemap.SetTile(pos, GetSurfaceTile(x, y));
                    toastTilemap.SetTileFlags(pos, TileFlags.None);
                    toastTilemap.SetColor(pos, GetSurfaceColor(x, y, board.Width, board.Height));
                    toastTilemap.SetTransformMatrix(pos, GetPlayableSurfaceTileMatrix());
                    RenderFungalCellOverlay(tile, pos);
                }
            }

            RenderDecorativeCrust(board);

            if (!suppressAnimations)
            {
                StartFadeInAnimations();
                StartDeathAnimations();
                StartToxinDropAnimations();
            }
        }

        // Backwards-compatible single-parameter call (default animations enabled)
        public void RenderBoard(GameBoard board)
        {
            RenderBoard(board, false);
        }

        public void ShowHoverEffect(Vector3Int cellPos) => hoverHelper?.ShowHoverEffect(cellPos);
        public void ClearHoverEffect() => hoverHelper?.ClearHoverEffect();

        public void HighlightPlayerTiles(int playerId, bool includeStartingTilePing = false)
        {
            var active = ActiveBoard; if (active == null) return;
            var ids = active.AllTiles()
                .Where(t => t.FungalCell != null && t.FungalCell.OwnerPlayerId == playerId && (t.FungalCell.CellType == FungalCellType.Alive || t.FungalCell.CellType == FungalCellType.Toxin))
                .Select(t => t.TileId);
            HighlightTiles(ids, pulseColorA, pulseColorB);
            if (includeStartingTilePing) TriggerStartingTilePing(playerId);
        }

        public void HighlightTiles(IEnumerable<int> tileIds, Color? colorA = null, Color? colorB = null)
        {
            var active = ActiveBoard; if (active == null) return;
            selectionHelper.HighlightTiles(tileIds, active, highlightedPositions);
            if (highlightedPositions.Count == 0) return;
            if (pulseHighlightCoroutine != null) StopCoroutine(pulseHighlightCoroutine);
            pulseHighlightCoroutine = StartCoroutine(selectionHelper.PulseHighlightTiles(
                highlightedPositions,
                new Color(1f, 0f, 0.9f, 0f),
                new Color(1f, 0f, 0.9f, 1f),
                0.4f));
        }

        public void HighlightTiles(IDictionary<int, (Color colorA, Color colorB)> tileHighlights)
        {
            var active = ActiveBoard; if (active == null) return;
            selectionHelper.HighlightTiles(tileHighlights, active, highlightedPositions);
            if (pulseHighlightCoroutine != null)
            {
                StopCoroutine(pulseHighlightCoroutine);
                pulseHighlightCoroutine = null;
            }
        }

        public void ShowSelectedTiles(IEnumerable<int> tileIds, Color? selectedColor = null)
        {
            var active = ActiveBoard; if (active == null) return;
            selectionHelper.ShowSelectedTiles(tileIds, active, selectedColor);
        }
        public void ClearSelectedTiles() => selectionHelper.ClearSelectedTiles();
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

        private Tilemap GetCrustTargetTilemap()
        {
            if (crustTilemap != null)
            {
                return crustTilemap;
            }

            return ActiveBoardMedium != null && ActiveBoardMedium.renderCrust ? toastTilemap : null;
        }

        private TileBase GetSurfaceTile(int x, int y)
        {
            var activeMedium = ActiveBoardMedium;
            if (activeMedium == null || !activeMedium.ShouldOverridePlayableSurface)
            {
                return baseTile;
            }

            return activeMedium.GetSurfaceTile(x, y) ?? activeMedium.boardSurfaceTile;
        }

        private Color GetSurfaceColor(int x, int y, int boardWidth, int boardHeight)
        {
            var activeMedium = ActiveBoardMedium;
            if (activeMedium == null || !activeMedium.ShouldOverridePlayableSurface || !activeMedium.IsPerimeterTintEnabled)
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

        private int GetCrustThickness(GameBoard activeBoard)
        {
            var activeMedium = ActiveBoardMedium;
            return activeMedium?.GetCrustThickness(activeBoard.Width, activeBoard.Height) ?? 0;
        }

        private Matrix4x4 GetPlayableSurfaceTileMatrix()
        {
            float scale = Mathf.Max(1f, ActiveBoardMedium?.playableSurfaceTileScale ?? 1f);
            if (Mathf.Approximately(scale, 1f))
            {
                return IdentityMatrix;
            }

            return Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        }

        private void RenderDecorativeCrust(GameBoard activeBoard)
        {
            var activeMedium = ActiveBoardMedium;
            if (activeBoard == null || activeMedium == null)
            {
                return;
            }

            int crustThickness = GetCrustThickness(activeBoard);
            if (crustThickness <= 0)
            {
                return;
            }

            var targetCrustTilemap = GetCrustTargetTilemap();
            if (targetCrustTilemap == null)
            {
                return;
            }

            Matrix4x4 crustTileMatrix = GetCrustTileMatrix();

            int minY = -crustThickness;
            int maxY = activeBoard.Height - 1 + crustThickness;
            for (int y = minY; y <= maxY; y++)
            {
                int rowInset = GetBreadCrustInsetForRow(activeMedium, activeBoard, crustThickness, y);
                int rowMinX = -crustThickness + rowInset;
                int rowMaxX = activeBoard.Width - 1 + crustThickness - rowInset;
                if (rowMinX > rowMaxX)
                {
                    continue;
                }

                for (int x = rowMinX; x <= rowMaxX; x++)
                {
                    if (x >= 0 && x < activeBoard.Width && y >= 0 && y < activeBoard.Height)
                    {
                        continue;
                    }

                    Color crustColor = EvaluateCrustColor(activeMedium, activeBoard, crustThickness, x, y);
                    SetCrustTile(targetCrustTilemap, new Vector3Int(x, y, 0), activeMedium.crustEdgeTile, crustColor, crustTileMatrix);
                }
            }
        }

        private Matrix4x4 GetCrustTileMatrix()
        {
            float scale = Mathf.Max(1f, ActiveBoardMedium?.crustTileScale ?? 1f);
            if (Mathf.Approximately(scale, 1f))
            {
                return IdentityMatrix;
            }

            return Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        }

        private static int GetBreadCrustInsetForRow(BoardMediumConfig activeMedium, GameBoard activeBoard, int crustThickness, int y)
        {
            if (activeMedium == null || !activeMedium.useBreadSliceSilhouette || crustThickness <= 1)
            {
                return 0;
            }

            if (y >= activeBoard.Height)
            {
                int rowAboveBoard = y - activeBoard.Height;
                int maxInset = Mathf.Max(
                    Mathf.RoundToInt(activeMedium.topCrustRoundness * crustThickness),
                    Mathf.RoundToInt(activeBoard.Width * 0.12f * activeMedium.topCrustRoundness));
                return CalculateCrustRowInset(rowAboveBoard, crustThickness, maxInset, 0.72f);
            }

            if (y < 0)
            {
                int rowBelowBoard = -y - 1;
                int maxInset = Mathf.Max(
                    Mathf.RoundToInt(activeMedium.bottomCrustRoundness * crustThickness),
                    Mathf.RoundToInt(activeBoard.Width * 0.035f * activeMedium.bottomCrustRoundness));
                return CalculateCrustRowInset(rowBelowBoard, crustThickness, maxInset, 1.35f);
            }

            return 0;
        }

        private static int CalculateCrustRowInset(int externalRowIndex, int crustThickness, int maxInset, float curvePower)
        {
            if (crustThickness <= 0 || maxInset <= 0)
            {
                return 0;
            }

            float normalizedRow = Mathf.Clamp01((externalRowIndex + 1f) / crustThickness);
            float curvedRow = Mathf.Pow(normalizedRow, curvePower);
            return Mathf.Clamp(Mathf.RoundToInt(curvedRow * maxInset), 0, maxInset);
        }

        private static Color EvaluateCrustColor(BoardMediumConfig activeMedium, GameBoard activeBoard, int crustThickness, int x, int y)
        {
            int horizontalDistance = 0;
            if (x < 0)
            {
                horizontalDistance = -x;
            }
            else if (x >= activeBoard.Width)
            {
                horizontalDistance = x - activeBoard.Width + 1;
            }

            int verticalDistance = 0;
            if (y < 0)
            {
                verticalDistance = -y;
            }
            else if (y >= activeBoard.Height)
            {
                verticalDistance = y - activeBoard.Height + 1;
            }

            int crustDistance = Mathf.Max(horizontalDistance, verticalDistance);
            float t = crustThickness <= 1
                ? 1f
                : Mathf.Clamp01((crustDistance - 1f) / (crustThickness - 1f));
            Color gradientColor = t < 0.5f
                ? Color.Lerp(activeMedium.crustInnerColor, activeMedium.crustMidColor, t / 0.5f)
                : Color.Lerp(activeMedium.crustMidColor, activeMedium.crustOuterColor, (t - 0.5f) / 0.5f);

            float variationStrength = Mathf.Clamp(activeMedium.crustColorVariation, 0f, 0.2f);
            if (variationStrength > 0f)
            {
                float variation = EvaluateCoordinateNoise(activeMedium, x, y);
                float brightness = 1f + ((variation * 2f) - 1f) * variationStrength;
                gradientColor *= brightness;
                gradientColor.a = 1f;
            }

            if (y >= activeBoard.Height)
            {
                float topDepth = crustThickness <= 0
                    ? 0f
                    : Mathf.Clamp01((y - activeBoard.Height + 1f) / crustThickness);
                gradientColor = Color.Lerp(gradientColor, activeMedium.crustOuterColor, topDepth * activeMedium.crustTopDarkening);
            }

            return gradientColor;
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

        private static void SetCrustTile(Tilemap tilemap, Vector3Int position, TileBase tile, Color color, Matrix4x4 transformMatrix)
        {
            if (tilemap == null || tile == null)
            {
                return;
            }

            tilemap.SetTile(position, tile);
            tilemap.SetTileFlags(position, TileFlags.None);
            tilemap.SetColor(position, color);
            tilemap.SetTransformMatrix(position, transformMatrix);
        }

        #region Starting Tile Ping
        public void TriggerStartingTilePing(int playerId)
        {
            var active = ActiveBoard; if (active == null) return;
            int? startId = GetPlayerStartingTile(playerId); if (!startId.HasValue) return;
            var (sx, sy) = active.GetXYFromTileId(startId.Value);
            Vector3Int center = new Vector3Int(sx, sy, 0);
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
            var active = ActiveBoard; float maxRadius = Mathf.Min(10f, Mathf.Max(active.Width, active.Height) * 0.25f);
            float ringThickness = 0.6f;
            yield return ringHelper.StartingTilePingAnimation(center, targetTilemap, duration, expandPortion, maxRadius, ringThickness);
            EndAnimation();
        }

        private int? GetPlayerStartingTile(int playerId)
        {
            var active = ActiveBoard;
            if (active != null && playerId >= 0 && playerId < active.Players.Count)
                return active.Players[playerId].StartingTileId;
            return null;
        }
        #endregion
        #endregion
    }

    // Timing data container
    public struct PostGrowthPhaseTiming
    {
        public float ReclaimRise;
        public float ReclaimSwap;
        public float ReclaimSettle;
        public float ReclaimHold;
        public float ReclaimLiteTotal;
        public float ResistancePulseTotal;
        public float HrtPulseTotal; // if different from resistance pulses (can reuse ResistancePulseTotal if zero)
    }
}
