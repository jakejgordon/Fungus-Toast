using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Unity.Grid.Helpers;
using FungusToast.Unity.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
        private GridBoardMediumRenderer boardMediumRenderer;
        private GridOverlayRenderer overlayRenderer;
        private GridCellStateAnimationController cellStateAnimationController;
        private GridSpecialPresentationEffects presentationEffects;
        private GridResistanceOverlayController resistanceOverlayController;
        private GridBoardStateRenderer boardStateRenderer;

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
        private readonly HashSet<int> preAnimationHiddenPreviewTileIds = new();

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
            boardMediumRenderer = new GridBoardMediumRenderer(
                () => ActiveBoardMedium,
                () => toastTilemap,
                () => crustTilemap,
                () => toastTilemap != null ? toastTilemap.transform : transform);
            overlayRenderer = new GridOverlayRenderer(
                () => ActiveBoard,
                () => moldTilemap,
                () => overlayTilemap,
                () => PingOverlayTileMap != null ? PingOverlayTileMap : HoverOverlayTileMap,
                GetPositionForTileId,
                GetTileForPlayer,
                () => solidHighlightTile,
                () => baseTile,
                () => toxinOverlayTile);
            cellStateAnimationController = new GridCellStateAnimationController(
                () => ActiveBoard,
                () => moldTilemap,
                () => overlayTilemap,
                () => PingOverlayTileMap != null ? PingOverlayTileMap : HoverOverlayTileMap,
                GetPositionForTileId,
                playerId => GetTileForPlayer(playerId),
                () => toxinOverlayTile,
                coroutine => StartCoroutine(coroutine),
                coroutine =>
                {
                    if (coroutine != null)
                    {
                        StopCoroutine(coroutine);
                    }
                },
                BeginAnimation,
                EndAnimation);
            resistanceOverlayController = new GridResistanceOverlayController(
                () => ActiveBoard,
                () => overlayTilemap,
                () => goldShieldOverlayTile,
                GetPositionForTileId,
                () => _timingContext.ResistancePulseTotal,
                () => postGrowthPhaseDurationMultiplier,
                coroutine => StartCoroutine(coroutine),
                BeginAnimation,
                EndAnimation,
                (center, radius, thickness, color, tilemap) => InternalDrawRing(center, radius, thickness, color, tilemap),
                tilemap => InternalClearRing(tilemap),
                () => PingOverlayTileMap,
                () => HoverOverlayTileMap,
                () => solidHighlightTile);
            boardStateRenderer = new GridBoardStateRenderer(
                () => ActiveBoard,
                () => moldTilemap,
                () => overlayTilemap,
                () => goldShieldOverlayTile,
                () => deadTile,
                () => toxinOverlayTile,
                GetPositionForTileId,
                GetTileForPlayer,
                (tileId, cell) => ShouldRenderResistanceOverlay(tileId, cell),
                (tileId, cell) => cellStateAnimationController != null ? cellStateAnimationController.GetAliveCellAlpha(tileId, cell) : 1f,
                tileId => preAnimationHiddenPreviewTileIds.Contains(tileId),
                tileId => overlayRenderer?.RemoveTrackedNutrientTile(tileId),
                (tile, pos) => RenderNutrientPatchOverlay(tile, pos));
            presentationEffects = new GridSpecialPresentationEffects(
                () => ActiveBoard,
                () => moldTilemap,
                () => overlayTilemap,
                () => PingOverlayTileMap,
                () => HoverOverlayTileMap,
                () => solidHighlightTile,
                () => transform,
                GetPositionForTileId,
                patchType => GetNutrientPatchTile(patchType),
                (tileId, scaleMultiplier) => BastionResistantPulseAnimation(tileId, scaleMultiplier),
                (center, radius, thickness, color, tilemap) => InternalDrawRing(center, radius, thickness, color, tilemap),
                tilemap => InternalClearRing(tilemap),
                coroutine => StartCoroutine(coroutine),
                RevealPreAnimationPreviewTile,
                RenderTileFromBoard,
                BeginAnimation,
                EndAnimation);
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
            cellStateAnimationController?.Dispose();
            presentationEffects?.DestroyLingeringToasts();
            boardMediumRenderer?.Dispose();
            overlayRenderer?.Dispose();
        }

        private void LateUpdate()
        {
            UpdateNutrientPulseVisuals();
            UpdateChemobeaconPulseVisuals();
        }

        public void Initialize(GameBoard board)
        {
            if (ReferenceEquals(this.board, board))
            {
                return;
            }

            UnsubscribeFromBoardEvents();
            cellStateAnimationController?.ClearPendingToxinExpirySnapshots();
            cellStateAnimationController?.StopAndClearToxinExpiryAnimations();
            presentationEffects?.DestroyLingeringToasts();

            this.board = board;

            if (this.board != null)
            {
                this.board.ToxinExpired += HandleToxinExpired;
                this.board.ChemobeaconPlaced += HandleChemobeaconPlaced;
                this.board.ChemobeaconExpired += HandleChemobeaconExpired;
            }
        }

        public void ResetForGameTransition()
        {
            StopAllCoroutines();
            UnsubscribeFromBoardEvents();
            board = null;
            presentationEffects?.DestroyLingeringToasts();

            _activeAnimationCount = 0;
            pulseHighlightCoroutine = null;
            startingTilePingCoroutine = null;
            lastPingTilemap = null;

            cellStateAnimationController?.ResetRuntimeState();
            resistanceOverlayController?.ResetRuntimeState();
            preAnimationHiddenPreviewTileIds.Clear();
            overlayRenderer?.ResetRuntimeState();

            ClearAllHighlights();
            ClearHoverEffect();

            toastTilemap?.ClearAllTiles();
            moldTilemap?.ClearAllTiles();
            overlayTilemap?.ClearAllTiles();
            PingOverlayTileMap?.ClearAllTiles();
            HoverOverlayTileMap?.ClearAllTiles();
            SelectedTileMap?.ClearAllTiles();
            SelectionHighlightTileMap?.ClearAllTiles();

            boardMediumRenderer?.ClearDecorativeCrustTilemap();
            boardMediumRenderer?.ResetGeneratedCrustVisual();
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
        public void PlayNutrientPatchConsumptionAnimationAsync(int nutrientTileId, int destinationTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount)
            => StartCoroutine(PlayNutrientPatchConsumptionAnimation(nutrientTileId, destinationTileId, patchType, rewardType, rewardAmount));
        public IEnumerator PlayMycotoxicLashAnimation(IReadOnlyList<int> tileIds)
            => presentationEffects != null ? presentationEffects.PlayMycotoxicLashAnimation(tileIds) : null;

        public IEnumerator PlayRetrogradeBloomAnimation(int anchorTileId)
            => presentationEffects != null ? presentationEffects.PlayRetrogradeBloomAnimation(anchorTileId) : null;

        public IEnumerator PlaySaprophageRingAnimation(IReadOnlyList<int> resistantTileIds, IReadOnlyList<int> consumedTileIds)
            => presentationEffects != null ? presentationEffects.PlaySaprophageRingAnimation(resistantTileIds, consumedTileIds) : null;

        public IEnumerator PlayNutrientPatchConsumptionAnimation(int nutrientTileId, int destinationTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount)
            => presentationEffects != null ? presentationEffects.PlayNutrientPatchConsumptionAnimation(nutrientTileId, destinationTileId, patchType, rewardType, rewardAmount) : null;

        private void UnsubscribeFromBoardEvents()
        {
            if (board != null)
            {
                board.ToxinExpired -= HandleToxinExpired;
                board.ChemobeaconPlaced -= HandleChemobeaconPlaced;
                board.ChemobeaconExpired -= HandleChemobeaconExpired;
            }
        }

        private void HandleChemobeaconPlaced(int playerId, int tileId)
        {
            if (board == null || overlayTilemap == null || moldTilemap == null)
            {
                return;
            }

            cellStateAnimationController?.CancelChemobeaconExpiryAnimation(tileId);
            RenderChemobeaconOverlay(tileId, GetPositionForTileId(tileId));
        }

        private void HandleChemobeaconExpired(int playerId, int tileId)
        {
            ClearChemobeaconOverlay(tileId);
            cellStateAnimationController?.StartChemobeaconExpiryAnimation(playerId, tileId);
        }

        private void HandleToxinExpired(object sender, ToxinExpiredEventArgs e)
        {
            if (!ReferenceEquals(sender, board))
            {
                return;
            }
            cellStateAnimationController?.CaptureToxinExpirySnapshot(e);
        }

        private void RenderFungalCellOverlay(BoardTile tile, Vector3Int pos) => boardStateRenderer?.RenderFungalCellOverlay(tile, pos);

        private void RenderNutrientPatchOverlay(BoardTile tile, Vector3Int pos) => overlayRenderer?.RenderNutrientPatchOverlay(tile, pos);

        private void RenderChemobeaconOverlay(int tileId, Vector3Int pos) => overlayRenderer?.RenderChemobeaconOverlay(tileId, pos);

        private void ClearChemobeaconOverlay(int tileId) => overlayRenderer?.ClearChemobeaconOverlay(tileId);

        private void ClearChemobeaconTransientOverlay(int tileId) => overlayRenderer?.ClearChemobeaconTransientOverlay(tileId);

        private void UpdateChemobeaconPulseVisuals() => overlayRenderer?.UpdateChemobeaconPulseVisuals();

        private TileBase GetNutrientPatchTile(NutrientPatch nutrientPatch)
        {
            return GetNutrientPatchTile(nutrientPatch?.PatchType ?? NutrientPatchType.Adaptogen);
        }

        private TileBase GetNutrientPatchTile(NutrientPatchType patchType)
        {
            return overlayRenderer != null ? overlayRenderer.GetNutrientPatchTile(patchType) : baseTile;
        }

        private void UpdateNutrientPulseVisuals() => overlayRenderer?.UpdateNutrientPulseVisuals();

        private bool ShouldRenderResistanceOverlay(int tileId, FungalCell cell)
            => resistanceOverlayController?.ShouldRenderResistanceOverlay(tileId, cell) == true;

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

        private void ClearResistanceOverlayTile(int tileId) => resistanceOverlayController?.ClearResistanceOverlayTile(tileId);

        private void RestoreResistanceOverlayTile(int tileId) => resistanceOverlayController?.RestoreResistanceOverlayTile(tileId);

        public void DeferResistanceOverlayReveal(IReadOnlyList<int> tileIds) => resistanceOverlayController?.DeferResistanceOverlayReveal(tileIds);

        public void RevealDeferredResistanceOverlays(IReadOnlyList<int> tileIds) => resistanceOverlayController?.RevealDeferredResistanceOverlays(tileIds);

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
            boardStateRenderer?.RenderTileFromBoard(tileId);
        }

        private void ApplyPreAnimationPreviewHiddenState(int tileId, Vector3Int pos) => boardStateRenderer?.ApplyPreAnimationPreviewHiddenState(tileId, pos);

        public void ClearNewlyGrownFlagsForNextGrowthPhase()
            => cellStateAnimationController?.ClearNewlyGrownFlagsForNextGrowthPhase();

        public void TriggerDeathAnimation(int tileId)
            => cellStateAnimationController?.TriggerDeathAnimation(tileId);

        public void TriggerToxinDropAnimation(int tileId)
            => cellStateAnimationController?.TriggerToxinDropAnimation(tileId);

        // NEW: Resistant drop animation for Surgical Inoculation (Option A)
        public IEnumerator ResistantDropAnimation(int tileId, float finalScale = 1f, float durationScale = 1f)
            => resistanceOverlayController != null ? resistanceOverlayController.ResistantDropAnimation(tileId, finalScale, durationScale) : null;

        // NEW: Shield pulse for Mycelial Bastion on a single tile (zoom out, then back in, no spin)
        public IEnumerator BastionResistantPulseAnimation(int tileId, float scaleMultiplier = 1f)
            => resistanceOverlayController != null ? resistanceOverlayController.BastionResistantPulseAnimation(tileId, scaleMultiplier) : null;

        // ADD timing-aware version before back compat overloads
        public void PlayResistancePulseBatchScaled(IReadOnlyList<int> tileIds, float scaleMultiplier, bool useTimingContext)
        {
            resistanceOverlayController?.PlayResistancePulseBatchScaled(tileIds, scaleMultiplier);
        }

        public void PlayResistanceDropBatch(IReadOnlyList<int> tileIds, float finalScale = 1f)
        {
            resistanceOverlayController?.PlayResistanceDropBatch(tileIds, finalScale);
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
            => resistanceOverlayController != null ? resistanceOverlayController.ImpactRingPulse(centerPos) : null;

        #region Public Interaction / Rendering API (restored)
        public void RenderBoard(GameBoard board, bool suppressAnimations)
        {
            cellStateAnimationController?.PrepareForBoardRender(board, suppressAnimations);
            overlayRenderer?.ResetRuntimeState();

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
                    RenderNutrientPatchOverlay(tile, pos);
                    RenderChemobeaconOverlay(tile.TileId, pos);
                    ApplyPreAnimationPreviewHiddenState(tile.TileId, pos);
                }
            }

            RenderDecorativeCrust(board);

            if (!suppressAnimations)
            {
                cellStateAnimationController?.StartQueuedAnimations();
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

        public void PlayDirectedVectorSurgePresentation(int playerId, int originTileId, IReadOnlyList<int> affectedTileIds)
        {
            if (affectedTileIds == null || affectedTileIds.Count == 0)
            {
                return;
            }

            var hiddenTileIds = affectedTileIds.Where(tileId => tileId >= 0).Distinct().ToList();
            if (hiddenTileIds.Count == 0)
            {
                return;
            }

            RegisterPreAnimationHiddenPreviewTiles(hiddenTileIds);
            for (int i = 0; i < hiddenTileIds.Count; i++)
            {
                RenderTileFromBoard(hiddenTileIds[i]);
            }

            if (presentationEffects != null)
            {
                StartCoroutine(presentationEffects.RunDirectedVectorSurgePresentation(playerId, originTileId, affectedTileIds));
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
            return boardMediumRenderer != null
                ? boardMediumRenderer.GetSurfaceTile(x, y, baseTile)
                : baseTile;
        }

        public Sprite GetChemobeaconLegendSprite()
        {
            return overlayRenderer?.GetChemobeaconLegendSprite();
        }

        private Color GetSurfaceColor(int x, int y, int boardWidth, int boardHeight)
        {
            return boardMediumRenderer != null
                ? boardMediumRenderer.GetSurfaceColor(x, y, boardWidth, boardHeight)
                : Color.white;
        }

        private int GetCrustThickness(GameBoard activeBoard)
        {
            return boardMediumRenderer?.GetCrustThickness(activeBoard) ?? 0;
        }

        private Matrix4x4 GetPlayableSurfaceTileMatrix()
        {
            return boardMediumRenderer?.GetPlayableSurfaceTileMatrix() ?? IdentityMatrix;
        }

        private void RenderDecorativeCrust(GameBoard activeBoard)
        {
            boardMediumRenderer?.RenderDecorativeCrust(activeBoard);
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
