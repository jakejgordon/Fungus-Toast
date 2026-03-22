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
        private readonly HashSet<int> deferredResistanceOverlayTileIds = new();
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
            deferredResistanceOverlayTileIds.Clear();
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

            overlayRenderer?.RemoveTrackedNutrientTile(tileId);

            if (tile?.FungalCell != null)
            {
                RenderFungalCellOverlay(tile, pos);
                ApplyPreAnimationPreviewHiddenState(tileId, pos);
                return;
            }

            if (tile?.HasNutrientPatch == true)
            {
                RenderNutrientPatchOverlay(tile, pos);
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

        public void ClearNewlyGrownFlagsForNextGrowthPhase()
            => cellStateAnimationController?.ClearNewlyGrownFlagsForNextGrowthPhase();

        public void TriggerDeathAnimation(int tileId)
            => cellStateAnimationController?.TriggerDeathAnimation(tileId);

        public void TriggerToxinDropAnimation(int tileId)
            => cellStateAnimationController?.TriggerToxinDropAnimation(tileId);

        // NEW: Resistant drop animation for Surgical Inoculation (Option A)
        public IEnumerator ResistantDropAnimation(int tileId, float finalScale = 1f, float durationScale = 1f)
        {
            var activeBoard = ActiveBoard;
            if (activeBoard == null || goldShieldOverlayTile == null || overlayTilemap == null)
                yield break;

            Vector3Int pos = GetPositionForTileId(tileId);

            float total = UIEffectConstants.SurgicalInoculationDropDurationSeconds * Mathf.Max(0.01f, durationScale);
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

            deferredResistanceOverlayTileIds.Add(tileId);
            ClearResistanceOverlayTile(tileId);

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
                deferredResistanceOverlayTileIds.Remove(tileId);
                RestoreResistanceOverlayTile(tileId);
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

        public void PlayHyphalVectoringSurgePresentation(int playerId, int originTileId, IReadOnlyList<int> affectedTileIds)
        {
            if (affectedTileIds == null || affectedTileIds.Count == 0)
            {
                return;
            }

            if (presentationEffects != null)
            {
                StartCoroutine(presentationEffects.RunHyphalVectoringSurgePresentation(playerId, originTileId, affectedTileIds));
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
