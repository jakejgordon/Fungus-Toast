using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Events;
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
        private SpriteRenderer generatedCrustRenderer;
        private Sprite generatedCrustSprite;
        private Texture2D generatedCrustTexture;
        private string generatedCrustCacheKey;

        private GameBoard board;
        private readonly List<int> playerMoldAssignments = new();
        public GameBoard ActiveBoard => board ?? GameManager.Instance?.Board; // now public for helper access
        public BoardMediumConfig ActiveBoardMedium => runtimeBoardMedium != null ? runtimeBoardMedium : defaultBoardMedium;
        public int CurrentBoardVisualPaddingTiles => board == null ? 0 : GetCrustThickness(board);
        public int PlayerMoldTileCount => playerMoldTiles?.Length ?? 0;

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
        private readonly Dictionary<int, ExpiringToxinVisualSnapshot> pendingToxinExpirySnapshots = new();
        private readonly Dictionary<int, Coroutine> toxinExpiryCoroutines = new();
        private readonly HashSet<int> deferredResistanceOverlayTileIds = new();
        private readonly HashSet<int> preAnimationHiddenPreviewTileIds = new();

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
        private Animation.CompositeLaunchArcAnimator _launchArcAnimator;

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
            _launchArcAnimator = new Animation.CompositeLaunchArcAnimator(this);
        }

        private void OnDestroy()
        {
            UnsubscribeFromBoardEvents();
            ClearPendingToxinExpirySnapshots();
            StopAndClearToxinExpiryAnimations();
            DestroyGeneratedCrustAssets();
        }

        public void Initialize(GameBoard board)
        {
            if (ReferenceEquals(this.board, board))
            {
                return;
            }

            UnsubscribeFromBoardEvents();
            ClearPendingToxinExpirySnapshots();
            StopAndClearToxinExpiryAnimations();

            this.board = board;

            if (this.board != null)
            {
                this.board.ToxinExpired += HandleToxinExpired;
            }
        }
        public void SetBoardMedium(BoardMediumConfig boardMedium) => runtimeBoardMedium = boardMedium;
        public void ClearBoardMediumOverride() => runtimeBoardMedium = null;
        public void SetPlayerMoldAssignments(IReadOnlyList<int> assignments)
        {
            playerMoldAssignments.Clear();
            if (assignments == null)
            {
                return;
            }

            for (int i = 0; i < assignments.Count; i++)
            {
                playerMoldAssignments.Add(assignments[i]);
            }
        }
        public void ClearPlayerMoldAssignments() => playerMoldAssignments.Clear();
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
            => _launchArcAnimator != null ? _launchArcAnimator.Play(playerId, sourceTileId, destinationTileId) : null;
        public IEnumerator PlayDistalSporeAnimation(int playerId, int sourceTileId, int destinationTileId)
            => _launchArcAnimator != null ? _launchArcAnimator.Play(playerId, sourceTileId, destinationTileId, preserveSourceCell: true) : null;
        public IEnumerator PlaySporeSalvoAnimation(int playerId, int sourceTileId, int destinationTileId)
            => _launchArcAnimator != null
                ? _launchArcAnimator.Play(
                    playerId,
                    sourceTileId,
                    destinationTileId,
                    preserveSourceCell: true,
                    overlaySprite: toxinOverlayTile != null ? toxinOverlayTile.sprite : null,
                    overlayScale: UIEffectConstants.SporeSalvoOverlayScale,
                    restoreBoardStateOnFinish: true)
                : null;
        public IEnumerator PlayHyphalBridgeAnimation(int playerId, int sourceTileId, IReadOnlyList<int> destinationTileIds)
            => _launchArcAnimator != null
                ? _launchArcAnimator.PlaySequence(
                    playerId,
                    sourceTileId,
                    destinationTileIds,
                    preserveSourceCell: true,
                    durationScale: UIEffectConstants.HyphalBridgeSegmentDurationScale)
                : null;
        public IEnumerator PlayHyphalDrawAnimation(int playerId, IReadOnlyList<(int sourceTileId, int destinationTileId)> moves)
            => _launchArcAnimator != null
                ? _launchArcAnimator.PlayBatch(
                    playerId,
                    moves,
                    preserveSourceCell: false,
                    overlaySprite: null,
                    overlayScale: 1f,
                    restoreBoardStateOnFinish: false,
                    durationScale: 1f,
                    allowOverlayFallback: false)
                : null;
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

        private void UnsubscribeFromBoardEvents()
        {
            if (board != null)
            {
                board.ToxinExpired -= HandleToxinExpired;
            }
        }

        private void HandleToxinExpired(object sender, ToxinExpiredEventArgs e)
        {
            if (!ReferenceEquals(sender, board))
            {
                return;
            }

            var pos = GetPositionForTileId(e.TileId);

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

            if (moldTile == null && e.ToxinOwnerPlayerId is int ownerPlayerId)
            {
                moldTile = GetTileForPlayer(ownerPlayerId);
            }

            TileBase overlayTile = toxinOverlayTile;
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

            pendingToxinExpirySnapshots[e.TileId] = new ExpiringToxinVisualSnapshot(
                e.TileId,
                moldTile,
                moldColor,
                overlayTile,
                overlayColor);
        }

        private void ClearPendingToxinExpirySnapshots()
        {
            pendingToxinExpirySnapshots.Clear();
        }

        private void StartPendingToxinExpiryAnimations()
        {
            if (pendingToxinExpirySnapshots.Count == 0)
            {
                return;
            }

            var pendingSnapshots = pendingToxinExpirySnapshots.Values.ToList();
            pendingToxinExpirySnapshots.Clear();

            foreach (var snapshot in pendingSnapshots)
            {
                var tile = board?.GetTileById(snapshot.TileId);
                if (tile?.FungalCell?.IsToxin == true)
                {
                    continue;
                }

                if (toxinExpiryCoroutines.ContainsKey(snapshot.TileId))
                {
                    CancelToxinExpiryAnimation(snapshot.TileId);
                }

                toxinExpiryCoroutines[snapshot.TileId] = StartCoroutine(ToxinExpiryDissolveAnimation(snapshot));
            }
        }

        private void StopAndClearToxinExpiryAnimations()
        {
            foreach (int tileId in toxinExpiryCoroutines.Keys.ToList())
            {
                CancelToxinExpiryAnimation(tileId);
            }

            toxinExpiryCoroutines.Clear();
        }

        private void CancelToxinExpiryAnimation(int tileId)
        {
            if (toxinExpiryCoroutines.TryGetValue(tileId, out var coroutine) && coroutine != null)
            {
                StopCoroutine(coroutine);
                toxinExpiryCoroutines.Remove(tileId);
                EndAnimation();
            }

            ClearToxinExpiryVisualTile(tileId);
        }

        private IEnumerator ToxinExpiryDissolveAnimation(ExpiringToxinVisualSnapshot snapshot)
        {
            Vector3Int pos = GetPositionForTileId(snapshot.TileId);
            bool shouldRenderMold = snapshot.MoldTile != null && moldTilemap != null && !moldTilemap.HasTile(pos);
            bool shouldRenderOverlay = snapshot.OverlayTile != null && overlayTilemap != null && !overlayTilemap.HasTile(pos);

            if (!shouldRenderMold && !shouldRenderOverlay)
            {
                toxinExpiryCoroutines.Remove(snapshot.TileId);
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

            BeginAnimation();
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
                toxinExpiryCoroutines.Remove(snapshot.TileId);
                ClearToxinExpiryVisualTile(snapshot.TileId);
                EndAnimation();
            }
        }

        private void ClearToxinExpiryVisualTile(int tileId)
        {
            var pos = GetPositionForTileId(tileId);

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
                    if (cell.OwnerPlayerId is int idA)
                    {
                        moldTile = GetTileForPlayer(idA);
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
                    if (cell.OwnerPlayerId is int ownerId)
                    {
                        moldTilemap.SetTileFlags(pos, TileFlags.None);
                        moldTilemap.SetTile(pos, GetTileForPlayer(ownerId));
                        
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
                    if (cell.OwnerPlayerId is int idT)
                    {
                        moldTile = GetTileForPlayer(idT);
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
            if (playerMoldTiles == null || playerMoldTiles.Length == 0 || playerId < 0)
            {
                Debug.LogWarning($"No tile found for Player ID {playerId}.");
                return null;
            }

            int moldIndex = ResolvePlayerMoldIndex(playerId);
            if (moldIndex >= 0 && moldIndex < playerMoldTiles.Length)
            {
                return playerMoldTiles[moldIndex];
            }

            Debug.LogWarning($"No tile found for Player ID {playerId}.");
            return null;
        }
        public void RegisterPreAnimationHiddenPreviewTiles(IEnumerable<int> tileIds)
        {
            if (tileIds == null)
            {
                return;
            }

            foreach (int tileId in tileIds.Where(tileId => tileId >= 0))
            {
                preAnimationHiddenPreviewTileIds.Add(tileId);
            }
        }

        public void RevealPreAnimationPreviewTile(int tileId)
        {
            preAnimationHiddenPreviewTileIds.Remove(tileId);
        }

        public void ClearPreAnimationPreviewTiles()
        {
            preAnimationHiddenPreviewTileIds.Clear();
        }

        private int ResolvePlayerMoldIndex(int playerId)
        {
            if (playerId >= 0 && playerId < playerMoldAssignments.Count)
            {
                int assignedIndex = playerMoldAssignments[playerId];
                if (assignedIndex >= 0 && assignedIndex < playerMoldTiles.Length)
                {
                    return assignedIndex;
                }
            }

            return playerId;
        }

        public void RenderTileFromBoard(int tileId)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null)
            {
                return;
            }

            var tile = activeBoard.GetTileById(tileId);
            var pos = GetPositionForTileId(tileId);

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

            if (tile?.FungalCell != null)
            {
                RenderFungalCellOverlay(tile, pos);
                ApplyPreAnimationPreviewHiddenState(tileId, pos);
            }
        }

        private void ApplyPreAnimationPreviewHiddenState(int tileId, Vector3Int pos)
        {
            if (!preAnimationHiddenPreviewTileIds.Contains(tileId))
            {
                return;
            }

            if (moldTilemap != null && moldTilemap.HasTile(pos))
            {
                moldTilemap.SetTileFlags(pos, TileFlags.None);
                var moldColor = moldTilemap.GetColor(pos);
                moldColor.a = 0f;
                moldTilemap.SetColor(pos, moldColor);
            }

            if (overlayTilemap != null && overlayTilemap.HasTile(pos))
            {
                overlayTilemap.SetTileFlags(pos, TileFlags.None);
                var overlayColor = overlayTilemap.GetColor(pos);
                overlayColor.a = 0f;
                overlayTilemap.SetColor(pos, overlayColor);
            }
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
            if (suppressAnimations)
            {
                ClearPendingToxinExpirySnapshots();
            }

            // Stop any in-flight animation coroutines
            foreach (var c in fadeInCoroutines.Values) if (c != null) { StopCoroutine(c); EndAnimation(); }
            fadeInCoroutines.Clear();
            foreach (var c in deathAnimationCoroutines.Values) if (c != null) { StopCoroutine(c); EndAnimation(); }
            deathAnimationCoroutines.Clear();
            foreach (var c in toxinDropCoroutines.Values) if (c != null) { StopCoroutine(c); EndAnimation(); }
            toxinDropCoroutines.Clear();
            StopAndClearToxinExpiryAnimations();

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
                    ApplyPreAnimationPreviewHiddenState(tile.TileId, pos);
                }
            }

            RenderDecorativeCrust(board);

            if (!suppressAnimations)
            {
                StartFadeInAnimations();
                StartDeathAnimations();
                StartToxinDropAnimations();
                StartPendingToxinExpiryAnimations();
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
            return crustTilemap != null ? crustTilemap : null;
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
                ClearGeneratedCrustVisual();
                return;
            }

            float visualCrustThickness = activeMedium.GetVisualCrustThickness(activeBoard.Width, activeBoard.Height);
            if (visualCrustThickness <= 0f)
            {
                ClearGeneratedCrustVisual();
                return;
            }

            var targetCrustTilemap = GetCrustTargetTilemap();
            if (targetCrustTilemap != null)
            {
                targetCrustTilemap.ClearAllTiles();
            }

            EnsureGeneratedCrustVisual(activeBoard, activeMedium, visualCrustThickness);
        }

        private void EnsureGeneratedCrustVisual(GameBoard activeBoard, BoardMediumConfig activeMedium, float visualCrustThickness)
        {
            string cacheKey = BuildGeneratedCrustCacheKey(activeBoard, activeMedium, visualCrustThickness);
            if (generatedCrustRenderer == null)
            {
                generatedCrustRenderer = CreateGeneratedCrustRenderer();
            }

            if (generatedCrustRenderer == null)
            {
                return;
            }

            Transform visualParent = GetGeneratedCrustVisualParent();
            if (generatedCrustRenderer.transform.parent != visualParent)
            {
                generatedCrustRenderer.transform.SetParent(visualParent, false);
            }

            if (generatedCrustCacheKey != cacheKey || generatedCrustSprite == null || generatedCrustTexture == null)
            {
                RebuildGeneratedCrustSprite(activeBoard, activeMedium, visualCrustThickness, cacheKey);
            }

            PositionGeneratedCrustRenderer(activeBoard);
            generatedCrustRenderer.enabled = generatedCrustSprite != null;
        }

        private SpriteRenderer CreateGeneratedCrustRenderer()
        {
            var crustObject = new GameObject("GeneratedCrustVisual");
            crustObject.transform.SetParent(GetGeneratedCrustVisualParent(), false);
            var spriteRenderer = crustObject.AddComponent<SpriteRenderer>();

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

        private Transform GetGeneratedCrustVisualParent()
        {
            return toastTilemap != null ? toastTilemap.transform : transform;
        }

        private void RebuildGeneratedCrustSprite(GameBoard activeBoard, BoardMediumConfig activeMedium, float visualCrustThickness, string cacheKey)
        {
            DestroyGeneratedCrustAssets();

            int pixelsPerUnit = GetBackdropPixelsPerUnit(activeBoard, visualCrustThickness);
            int textureWidth = Mathf.Max(1, Mathf.CeilToInt((activeBoard.Width + (visualCrustThickness * 2f)) * pixelsPerUnit));
            int textureHeight = Mathf.Max(1, Mathf.CeilToInt((activeBoard.Height + (visualCrustThickness * 2f)) * pixelsPerUnit));

            generatedCrustTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                alphaIsTransparency = true
            };

            var pixels = new Color32[textureWidth * textureHeight];
            float outerFeather = 1.75f / pixelsPerUnit;

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

            generatedCrustTexture.SetPixels32(pixels);
            generatedCrustTexture.Apply(false, false);
            generatedCrustSprite = Sprite.Create(
                generatedCrustTexture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            generatedCrustRenderer.sprite = generatedCrustSprite;
            generatedCrustCacheKey = cacheKey;
        }

        private void PositionGeneratedCrustRenderer(GameBoard activeBoard)
        {
            if (generatedCrustRenderer == null)
            {
                return;
            }

            generatedCrustRenderer.transform.localPosition = new Vector3(activeBoard.Width * 0.5f, activeBoard.Height * 0.5f, 0f);
            generatedCrustRenderer.transform.localRotation = Quaternion.identity;
            generatedCrustRenderer.transform.localScale = Vector3.one;
        }

        private int GetBackdropPixelsPerUnit(GameBoard activeBoard, float visualCrustThickness)
        {
            float fullWidth = activeBoard.Width + (visualCrustThickness * 2f);
            float fullHeight = activeBoard.Height + (visualCrustThickness * 2f);
            float longestSide = Mathf.Max(fullWidth, fullHeight);
            int pixelsPerUnit = Mathf.FloorToInt(2048f / Mathf.Max(1f, longestSide));
            return Mathf.Clamp(pixelsPerUnit, 8, 24);
        }

        private string BuildGeneratedCrustCacheKey(GameBoard activeBoard, BoardMediumConfig activeMedium, float visualCrustThickness)
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

        private void ClearGeneratedCrustVisual()
        {
            if (generatedCrustRenderer != null)
            {
                generatedCrustRenderer.sprite = null;
                generatedCrustRenderer.enabled = false;
            }

            DestroyGeneratedCrustAssets();
            generatedCrustCacheKey = null;
        }

        private void DestroyGeneratedCrustAssets()
        {
            if (generatedCrustSprite != null)
            {
                Destroy(generatedCrustSprite);
                generatedCrustSprite = null;
            }

            if (generatedCrustTexture != null)
            {
                Destroy(generatedCrustTexture);
                generatedCrustTexture = null;
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
            Color finalColor = outerEdgeDistance < visualCrustThickness
                ? crustGradientColor
                : breadColor;

            float variationStrength = Mathf.Clamp(activeMedium.crustColorVariation, 0f, 0.2f);
            if (variationStrength > 0f)
            {
                float variation = EvaluateCoordinateNoise(activeMedium, Mathf.RoundToInt(x * 12f), Mathf.RoundToInt(y * 12f));
                float brightness = 1f + ((variation * 2f) - 1f) * variationStrength;
                finalColor *= brightness;
                finalColor.a = 1f;
            }

            float breadVariationStrength = Mathf.Clamp(activeMedium.breadColorVariation, 0f, 0.15f);
            if (breadVariationStrength > 0f && outerEdgeDistance >= visualCrustThickness)
            {
                float variation = EvaluateCoordinateNoise(activeMedium, Mathf.RoundToInt(x * 6f) + 187, Mathf.RoundToInt(y * 10f) + 911);
                float brightness = 1f + ((variation * 2f) - 1f) * breadVariationStrength;
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
