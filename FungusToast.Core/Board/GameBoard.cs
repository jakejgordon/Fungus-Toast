using FungusToast.Core.Config;
using FungusToast.Core.Events;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Core.Phases;
using FungusToast.Core.Growth;
using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Board
{
    public class GameBoard
    {
        public int Width { get; }
        public int Height { get; }
        public BoardTile[,] Grid { get; }
        public List<Player> Players { get; }

        // Mapping of tileId -> current fungal cell (only for occupied tiles)
        private readonly Dictionary<int, FungalCell> tileIdToCell = new();

        public int CurrentRound { get; private set; } = 1;
        public int CurrentGrowthCycle { get; private set; } = 0;

        /// <summary>
        /// Round-scoped context for tracking per-player, per-effect counters during the current round.
        /// Reset at the start of each round.
        /// </summary>
        public RoundContext CurrentRoundContext { get; private set; } = new();

        /// <summary>
        /// True once Necrophytic Bloom has activated in this game instance.
        /// </summary>
        public bool NecrophyticBloomActivated { get; set; } = false;

        /// <summary>
        /// Cached occupied tile ratio calculated at the start of the decay phase.
        /// Used to avoid expensive recalculation during individual cell death events.
        /// </summary>
        public float CachedOccupiedTileRatio { get; private set; } = 0f;

        /// <summary>
        /// Cached decay phase context for optimized competitive targeting during the decay phase.
        /// Created once per decay phase and reused for all competitive targeting operations.
        /// </summary>
        public DecayPhaseContext? CachedDecayPhaseContext { get; private set; } = null;

        public int TotalTiles => Width * Height;

        #region Events & Delegates
        public delegate void CellColonizedEventHandler(int playerId, int tileId, GrowthSource source);
        public delegate void CellInfestedEventHandler(int playerId, int tileId, int oldOwnerId, GrowthSource source);
        public delegate void CellReclaimedEventHandler(int playerId, int tileId, GrowthSource source);
        public delegate void CellToxifiedEventHandler(int playerId, int tileId, GrowthSource source);
        public delegate void CellPoisonedEventHandler(int playerId, int tileId, int oldOwnerId, GrowthSource source);
        public delegate void CellCatabolizedEventHandler(int playerId, int tileId);
        public delegate void CellDeathEventHandler(int playerId, int tileId, DeathReason reason);
        public delegate void CellSurgeGrowthEventHandler(int playerId, int tileId);
        public delegate void NecrotoxicConversionEventHandler(int playerId, int tileId, int oldOwnerId);
        public delegate void SporeDropEventHandler(int playerId, int tileId, MutationType mutationType);
        public delegate void MutationPointsEarnedEventHandler(int playerId, int amount);
        public delegate void MutationPointsSpentEventHandler(int playerId, MutationTier tier, int amount);
        public delegate void TendrilGrowthEventHandler(int playerId, int tileId, DiagonalDirection direction);
        public delegate void CreepingMoldMoveEventHandler(int playerId, int fromTileId, int toTileId);
        public delegate void JettingMyceliumCatabolicGrowthEventHandler(int playerId, int tileId);
        public delegate void PostGrowthPhaseEventHandler();
        public delegate void PostGrowthPhaseCompletedEventHandler(); // NEW
        public delegate void DecayPhaseEventHandler(Dictionary<int, int> failedGrowthsByPlayerId);
        public delegate void PreGrowthCycleEventHandler();
        public delegate void DecayPhaseWithFailedGrowthsEventHandler(Dictionary<int, int> failedGrowthsByPlayerId);
        public delegate void NecrophyticBloomActivatedEventHandler();
        public delegate void MutationPhaseStartEventHandler();
        public delegate void ToxinPlacedEventHandler(object sender, ToxinPlacedEventArgs e);
        public delegate void ToxinExpiredEventHandler(object sender, ToxinExpiredEventArgs e);
        public delegate void CatabolicRebirthEventHandler(object sender, CatabolicRebirthEventArgs e);
        public delegate void PreGrowthPhaseEventHandler();
        public delegate void ResistanceAppliedBatchEventHandler(int playerId, GrowthSource source, IReadOnlyList<int> tileIds);
        public delegate void RegenerativeHyphaeReclaimedEventHandler(int playerId, int tileId); // NEW for Unity visuals

        public event CellColonizedEventHandler? CellColonized;
        public event CellInfestedEventHandler? CellInfested;
        public event CellReclaimedEventHandler? CellReclaimed;
        public event CellToxifiedEventHandler? CellToxified;
        public event CellPoisonedEventHandler? CellPoisoned;
        public event CellCatabolizedEventHandler? CellCatabolized;
        public event EventHandler<FungalCellDiedEventArgs>? CellDeath;
        public event CellSurgeGrowthEventHandler? CellSurgeGrowth;
        public event NecrotoxicConversionEventHandler? NecrotoxicConversion;
        public event SporeDropEventHandler? SporeDrop;
        public event MutationPointsEarnedEventHandler? MutationPointsEarned;
        public event MutationPointsSpentEventHandler? MutationPointsSpent;
        public event TendrilGrowthEventHandler? TendrilGrowth;
        public event CreepingMoldMoveEventHandler? CreepingMoldMove;
        public event JettingMyceliumCatabolicGrowthEventHandler? JettingMyceliumCatabolicGrowth;
        public event PostGrowthPhaseEventHandler? PostGrowthPhase;
        public event PostGrowthPhaseCompletedEventHandler? PostGrowthPhaseCompleted; // NEW
        public event DecayPhaseEventHandler? DecayPhase;
        public event PreGrowthCycleEventHandler? PreGrowthCycle;
        public event DecayPhaseWithFailedGrowthsEventHandler? DecayPhaseWithFailedGrowths;
        public event NecrophyticBloomActivatedEventHandler? NecrophyticBloomActivatedEvent;
        public event MutationPhaseStartEventHandler? MutationPhaseStart;
        public event ToxinPlacedEventHandler? ToxinPlaced;
        public event ToxinExpiredEventHandler? ToxinExpired;
        public event CatabolicRebirthEventHandler? CatabolicRebirth;
        public event PreGrowthPhaseEventHandler? PreGrowthPhase;
        public event ResistanceAppliedBatchEventHandler? ResistanceAppliedBatch;
        public event RegenerativeHyphaeReclaimedEventHandler? RegenerativeHyphaeReclaimed; // NEW for Unity visuals
        #endregion

        #region Event Invokers
        protected virtual void OnCellColonized(int playerId, int tileId, GrowthSource source) =>
            CellColonized?.Invoke(playerId, tileId, source);
        protected virtual void OnCellInfested(int playerId, int tileId, int oldOwnerId, GrowthSource source) =>
            CellInfested?.Invoke(playerId, tileId, oldOwnerId, source);
        protected virtual void OnCellReclaimed(int playerId, int tileId, GrowthSource source) =>
            CellReclaimed?.Invoke(playerId, tileId, source);
        protected virtual void OnCellToxified(int playerId, int tileId, GrowthSource source) =>
            CellToxified?.Invoke(playerId, tileId, source);
        protected virtual void OnCellPoisoned(int playerId, int tileId, int oldOwnerId, GrowthSource source) =>
            CellPoisoned?.Invoke(playerId, tileId, oldOwnerId, source);
        protected virtual void OnCellCatabolized(int playerId, int tileId) =>
            CellCatabolized?.Invoke(playerId, tileId);
        protected virtual void OnCellDeath(int playerId, int tileId, DeathReason reason, int? killerPlayerId = null, FungalCell? cell = null, int? attackerTileId = null)
        {
            var args = new FungalCellDiedEventArgs(tileId, playerId, reason, killerPlayerId, cell!, attackerTileId);
            CellDeath?.Invoke(this, args);
        }
        protected virtual void OnCellSurgeGrowth(int playerId, int tileId) =>
            CellSurgeGrowth?.Invoke(playerId, tileId);
        protected virtual void OnNecrotoxicConversion(int playerId, int tileId, int oldOwnerId) =>
            NecrotoxicConversion?.Invoke(playerId, tileId, oldOwnerId);
        protected virtual void OnSporeDrop(int playerId, int tileId, MutationType mutationType) =>
            SporeDrop?.Invoke(playerId, tileId, mutationType);
        protected virtual void OnMutationPointsEarned(int playerId, int amount) =>
            MutationPointsEarned?.Invoke(playerId, amount);
        protected virtual void OnMutationPointsSpent(int playerId, MutationTier tier, int amount) =>
            MutationPointsSpent?.Invoke(playerId, tier, amount);
        protected virtual void OnTendrilGrowth(int playerId, int tileId, DiagonalDirection direction) =>
            TendrilGrowth?.Invoke(playerId, tileId, direction);
        protected virtual void OnCreepingMoldMove(int playerId, int fromTileId, int toTileId) =>
            CreepingMoldMove?.Invoke(playerId, fromTileId, toTileId);
        protected virtual void OnToxinExpired(ToxinExpiredEventArgs e) =>
            ToxinExpired?.Invoke(this, e);
        public virtual void OnPostGrowthPhase() =>
            PostGrowthPhase?.Invoke();
        public virtual void OnPostGrowthPhaseCompleted() =>
            PostGrowthPhaseCompleted?.Invoke();
        public virtual void OnDecayPhase(Dictionary<int, int> failedGrowthsByPlayerId) =>
            DecayPhase?.Invoke(failedGrowthsByPlayerId);
        public virtual void OnPreGrowthCycle() =>
            PreGrowthCycle?.Invoke();
        public virtual void OnDecayPhaseWithFailedGrowths(Dictionary<int, int> failedGrowthsByPlayerId) =>
            DecayPhaseWithFailedGrowths?.Invoke(failedGrowthsByPlayerId);
        protected virtual void RaiseToxinExpired(ToxinExpiredEventArgs e) => ToxinExpired?.Invoke(this, e);
        protected virtual void OnRegenerativeHyphaeReclaimed(int playerId, int tileId) =>
            RegenerativeHyphaeReclaimed?.Invoke(playerId, tileId);
        #endregion

        #region Growth Attempt Events
        public event EventHandler<GrowthAttemptEventArgs>? BeforeGrowthAttempt;
        public event EventHandler<GrowthAttemptEventArgs>? AfterGrowthAttempt;
        public event Action<FungalCell, int>? OnDeadCellReclaim;
        protected virtual void OnBeforeGrowthAttempt(GrowthAttemptEventArgs e) =>
            BeforeGrowthAttempt?.Invoke(this, e);
        protected virtual void OnAfterGrowthAttempt(GrowthAttemptEventArgs e) =>
            AfterGrowthAttempt?.Invoke(this, e);
        #endregion

        // NEW: Public invoker for batch resistance application
        public virtual void OnResistanceAppliedBatch(int playerId, GrowthSource source, List<int> tileIds) =>
            ResistanceAppliedBatch?.Invoke(playerId, source, tileIds);

        public GameBoard(int width, int height, int playerCount)
        {
            Width = width;
            Height = height;
            Grid = new BoardTile[width, height];
            Players = new List<Player>(playerCount);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Grid[x, y] = new BoardTile(x, y, width);
        }

        #region Basic Tile Accessors
        public IEnumerable<BoardTile> AllTiles()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    yield return Grid[x, y];
        }

        public (int x, int y) GetXYFromTileId(int tileId)
        {
            int x = tileId % Width;
            int y = tileId / Width;
            return (x, y);
        }

        public BoardTile? GetTile(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
                return Grid[x, y];
            return null;
        }

        public BoardTile? GetTileById(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            return GetTile(x, y);
        }
        #endregion

        #region Neighbor Queries
        public List<BoardTile> GetOrthogonalNeighbors(int x, int y)
        {
            List<BoardTile> neighbors = new();
            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, -1, 0, 1 };
            for (int d = 0; d < 4; d++)
            {
                int nx = x + dx[d];
                int ny = y + dy[d];
                if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
                    neighbors.Add(Grid[nx, ny]);
            }
            return neighbors;
        }
        public List<BoardTile> GetOrthogonalNeighbors(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            return GetOrthogonalNeighbors(x, y);
        }
        public List<BoardTile> GetDiagonalNeighbors(int x, int y)
        {
            List<BoardTile> neighbors = new();
            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] diagonalIndices = { 0, 2, 5, 7 };
            foreach (int d in diagonalIndices)
            {
                int nx = x + dx[d];
                int ny = y + dy[d];
                if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
                    neighbors.Add(Grid[nx, ny]);
            }
            return neighbors;
        }
        public List<BoardTile> GetDiagonalNeighbors(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            return GetDiagonalNeighbors(x, y);
        }
        public List<int> GetAdjacentTileIds(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            List<int> neighbors = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
                        neighbors.Add(ny * Width + nx);
                }
            }
            return neighbors;
        }
        public List<BoardTile> GetAdjacentTiles(int tileId)
        {
            List<BoardTile> result = new();
            foreach (int neighborId in GetAdjacentTileIds(tileId))
            {
                var tile = GetTileById(neighborId);
                if (tile != null)
                    result.Add(tile);
            }
            return result;
        }
        #endregion

        #region Occupancy Helpers (Performance – Priority 1)
        /// <summary>
        /// Fast enumeration of all currently occupied cells (dictionary values).
        /// Integrity is maintained by PlaceFungalCell / RemoveCellInternal.
        /// </summary>
        private IEnumerable<FungalCell> OccupiedCells() => tileIdToCell.Values;

#if DEBUG
        public void DebugPruneStaleCellMappings()
        {
            var stale = new List<int>();
            foreach (var kv in tileIdToCell)
            {
                var tile = GetTileById(kv.Key);
                if (tile?.FungalCell == null || !ReferenceEquals(tile.FungalCell, kv.Value))
                    stale.Add(kv.Key);
            }
            if (stale.Count > 0)
                foreach (var id in stale) tileIdToCell.Remove(id);
        }
#endif

        private BoardTile GetTileForCell(FungalCell cell)
        {
            var (x, y) = GetXYFromTileId(cell.TileId);
            return Grid[x, y];
        }

        /// <summary>
        /// Safely removes a cell from the board ensuring tile state, dictionary, and player control lists stay in sync.
        /// </summary>
        /// <param name="tileId">Tile id to clear.</param>
        /// <param name="removeControl">Whether to remove control from owning player (true for toxins & living cells).</param>
        internal void RemoveCellInternal(int tileId, bool removeControl = true)
        {
            if (!tileIdToCell.TryGetValue(tileId, out var cell)) return; // Already removed
            var (x, y) = GetXYFromTileId(tileId);
            var tile = Grid[x, y];
            if (tile.FungalCell != null && !ReferenceEquals(tile.FungalCell, cell))
            {
                tileIdToCell.Remove(tileId); // stale mapping
                return;
            }
            tile.RemoveFungalCell();
            tileIdToCell.Remove(tileId);
            if (removeControl && cell.OwnerPlayerId.HasValue)
            {
                int ownerId = cell.OwnerPlayerId.Value;
                if (ownerId >= 0 && ownerId < Players.Count)
                    Players[ownerId].ControlledTileIds.Remove(tileId);
            }
        }

        private int ComputeOccupiedTileCount()
        {
            int count = 0;
            foreach (var kv in tileIdToCell)
            {
                var (x, y) = GetXYFromTileId(kv.Key);
                if (Grid[x, y].FungalCell != null)
                    count++;
            }
            return count;
        }
        #endregion

        #region Cell / Player Queries
        public List<FungalCell> GetAllCells() => tileIdToCell.Values.ToList();
        public List<FungalCell> GetAllCellsOwnedBy(int playerId) => tileIdToCell.Values.Where(c => c.OwnerPlayerId == playerId).ToList();
        public List<int> GetAllTileIds() => tileIdToCell.Keys.ToList();

        public List<BoardTile> GetDeadTiles()
        {
            var snapshot = tileIdToCell.Values.ToList();
            var list = new List<BoardTile>();
            foreach (var cell in snapshot)
            {
                if (cell.CellType == FungalCellType.Dead)
                    list.Add(GetTileForCell(cell));
            }
            return list;
        }

        public float GetOccupiedTileRatio()
        {
            int occupied = ComputeOccupiedTileCount();
            return occupied == 0 ? 0f : (float)occupied / TotalTiles;
        }

        public bool ShouldTriggerEndgame() => GetOccupiedTileRatio() >= GameBalance.GameEndTileOccupancyThreshold;

        public IEnumerable<FungalCell> AllLivingFungalCells()
        {
            var snapshot = tileIdToCell.Values.ToList();
            foreach (var cell in snapshot)
                if (cell.CellType == FungalCellType.Alive)
                    yield return cell;
        }

        public IEnumerable<(BoardTile tile, FungalCell cell)> AllLivingFungalCellsWithTiles()
        {
            var snapshot = tileIdToCell.Values.ToList();
            foreach (var cell in snapshot)
            {
                if (cell.CellType == FungalCellType.Alive)
                    yield return (GetTileForCell(cell), cell);
            }
        }

        public IEnumerable<BoardTile> AllToxinTiles()
        {
            var snapshot = tileIdToCell.Values
                .Where(c => c.CellType == FungalCellType.Toxin)
                .Select(c => GetTileForCell(c))
                .ToList();
            foreach (var tile in snapshot)
            {
                if (tile.FungalCell != null && tile.FungalCell.IsToxin)
                    yield return tile;
            }
        }

        public IEnumerable<FungalCell> AllToxinFungalCells()
        {
            var snapshot = tileIdToCell.Values
                .Where(c => c.CellType == FungalCellType.Toxin)
                .ToList();
            foreach (var cell in snapshot)
            {
                var tile = GetTileForCell(cell);
                if (tile.FungalCell != null && ReferenceEquals(tile.FungalCell, cell) && tile.FungalCell.IsToxin)
                    yield return cell;
            }
        }

        public IEnumerable<BoardTile> GetAdjacentLivingTiles(int tileId, int? excludePlayerId = null)
        {
            foreach (int neighborId in GetAdjacentTileIds(tileId))
            {
                var tile = GetTileById(neighborId);
                if (tile == null) continue;
                var cell = tile.FungalCell;
                if (cell == null || cell.CellType != FungalCellType.Alive) continue;
                if (excludePlayerId.HasValue && cell.OwnerPlayerId == excludePlayerId.Value) continue;
                yield return tile;
            }
        }
        #endregion

        #region Spawning / Placement / Removal
        public void PlaceInitialSpore(int playerId, int x, int y)
        {
            BoardTile tile = Grid[x, y];
            if (!tile.IsOccupied)
            {
                int tileId = y * Width + x;
                var cell = new FungalCell(ownerPlayerId: playerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
                cell.MakeResistant();
                cell.SetBirthRound(CurrentRound);
                tile.PlaceFungalCell(cell);
                tileIdToCell[tileId] = cell;
                Players[playerId].ControlledTileIds.Add(tileId);
                Players[playerId].SetStartingTile(tileId);
            }
        }

        public FungalCell? GetCell(int tileId)
        {
            tileIdToCell.TryGetValue(tileId, out var cell);
            return cell;
        }

        public bool SpawnSporeForPlayer(Player player, int tileId, GrowthSource source)
        {
            var (x, y) = GetXYFromTileId(tileId);
            var tile = GetTile(x, y);
            if (tile == null || tile.IsOccupied) return false;
            var cell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: tileId, source: source, lastOwnerPlayerId: null);
            cell.MarkAsNewlyGrown();
            cell.SetBirthRound(CurrentRound);
            tile.PlaceFungalCell(cell);
            tileIdToCell[tileId] = cell;
            player.ControlledTileIds.Add(tileId);
            return true;
        }

        public void RemoveControlFromPlayer(int tileId)
        {
            foreach (var player in Players)
                player.ControlledTileIds.Remove(tileId);
        }

        internal void PlaceFungalCell(FungalCell cell)
        {
            var (x, y) = GetXYFromTileId(cell.TileId);
            var tile = Grid[x, y];
            var oldCell = tile.FungalCell;

            if (oldCell != null && oldCell.IsResistant)
                return; // Cannot replace resistant

            if (oldCell != null && oldCell.OwnerPlayerId.HasValue)
            {
                int prevOwnerId = oldCell.OwnerPlayerId.Value;
                Players[prevOwnerId].ControlledTileIds.Remove(cell.TileId);
            }

            tile.PlaceFungalCell(cell);
            tileIdToCell[cell.TileId] = cell;

            if (cell.OwnerPlayerId.HasValue)
            {
                int newOwnerId = cell.OwnerPlayerId.Value;
                if (!Players[newOwnerId].ControlledTileIds.Contains(cell.TileId))
                    Players[newOwnerId].ControlledTileIds.Add(cell.TileId);
            }

            if (cell.IsAlive && (cell.SourceOfGrowth ?? GrowthSource.Unknown) != GrowthSource.InitialSpore)
            {
                cell.MarkAsNewlyGrown();
                cell.SetBirthRound(CurrentRound);
            }

            int ownerId = cell.OwnerPlayerId.GetValueOrDefault(-1);
            GrowthSource source = cell.SourceOfGrowth ?? GrowthSource.Unknown;

            if (cell.ReclaimCount > 0)
            {
                OnCellReclaimed(ownerId, cell.TileId, source);
                return;
            }

            // Event dispatch based on what was replaced
            if (oldCell == null)
            {
                if (cell.IsToxin) OnCellToxified(ownerId, cell.TileId, source);
                else OnCellColonized(ownerId, cell.TileId, source);
            }
            else if (oldCell.IsAlive)
            {
                int oldOwnerId = oldCell.OwnerPlayerId.GetValueOrDefault(-1);
                if (oldOwnerId != ownerId) OnCellInfested(ownerId, cell.TileId, oldOwnerId, source);
                else OnCellColonized(ownerId, cell.TileId, source);
            }
            else if (oldCell.IsToxin)
            {
                int oldOwnerId = oldCell.OwnerPlayerId.GetValueOrDefault(-1);
                OnCellPoisoned(ownerId, cell.TileId, oldOwnerId, source);
            }
            else if (oldCell.IsDead)
            {
                if (cell.IsToxin) OnCellToxified(ownerId, cell.TileId, source);
                else OnCellReclaimed(ownerId, cell.TileId, source);
            }
        }

        public void KillFungalCell(FungalCell cell, DeathReason reason, int? killerPlayerId = null, int? attackerTileId = null)
        {
            if (cell.IsResistant) return;
            int tileId = cell.TileId;
            int playerId = cell.OwnerPlayerId ?? -1;
            cell.MarkAsDying();
            cell.Kill(reason);
            RemoveControlFromPlayer(tileId);
            OnCellDeath(playerId, tileId, reason, killerPlayerId, cell, attackerTileId);
        }

        public bool TryReclaimDeadCell(int playerId, int tileId, GrowthSource reclaimGrowthSource)
        {
            var tile = GetTileById(tileId);
            if (tile?.FungalCell == null || !tile.FungalCell.IsDead) return false;
            var cell = tile.FungalCell;
            if (cell.OwnerPlayerId != playerId) return false;
            cell.Reclaim(playerId, reclaimGrowthSource);
            cell.MarkAsNewlyGrown();
            cell.SetBirthRound(CurrentRound);
            tileIdToCell[cell.TileId] = cell;
            Players[playerId].AddControlledTile(tileId);
            OnCellReclaimed(playerId, cell.TileId, reclaimGrowthSource);
            OnDeadCellReclaim?.Invoke(cell, playerId);
            if (reclaimGrowthSource == GrowthSource.RegenerativeHyphae)
                OnRegenerativeHyphaeReclaimed(playerId, cell.TileId);
            return true;
        }
        #endregion

        #region Metrics / Counts
        public int CountReclaimedCellsByPlayer(int playerId)
        {
            return tileIdToCell.Values.Count(c =>
                c.CellType == FungalCellType.Alive &&
                c.OwnerPlayerId == playerId &&
                c.OriginalOwnerPlayerId == playerId &&
                c.ReclaimCount > 0);
        }
        #endregion

        #region Cached / Phase Helpers
        public void OnMutationPhaseStart() => MutationPhaseStart?.Invoke();
        public void OnPreGrowthPhase() => PreGrowthPhase?.Invoke();
        public void UpdateCachedOccupiedTileRatio() => CachedOccupiedTileRatio = GetOccupiedTileRatio();
        public void UpdateCachedDecayPhaseContext()
        {
            if (CachedDecayPhaseContext == null)
                CachedDecayPhaseContext = new DecayPhaseContext(this, Players);
        }
        public void ClearCachedDecayPhaseContext() => CachedDecayPhaseContext = null;
        public void OnNecrophyticBloomActivatedEvent() => NecrophyticBloomActivatedEvent?.Invoke();
        public void OnCatabolicRebirth(CatabolicRebirthEventArgs e) => CatabolicRebirth?.Invoke(this, e);
        public void OnToxinPlaced(ToxinPlacedEventArgs e) => ToxinPlaced?.Invoke(this, e);
        public void FireCellPoisonedEvent(int playerId, int tileId, int oldOwnerId, GrowthSource source) => OnCellPoisoned(playerId, tileId, oldOwnerId, source);
        #endregion

        #region Growth Cycle / Aging / Toxins
        public void ExpireToxinTiles(int currentGrowthCycle, ISimulationObserver observer)
        {
            foreach (var toxinCell in AllToxinFungalCells())
            {
                var adjacentTiles = GetOrthogonalNeighbors(toxinCell.TileId);
                bool shouldAgeDouble = false;
                foreach (var adjTile in adjacentTiles)
                {
                    var adjCell = adjTile.FungalCell;
                    if (adjCell != null && adjCell.IsDead && adjCell.OwnerPlayerId.HasValue)
                    {
                        var owner = Players.FirstOrDefault(p => p.PlayerId == adjCell.OwnerPlayerId.Value);
                        var catabolicRebirth = MutationRegistry.GetById(MutationIds.CatabolicRebirth);
                        if (owner != null && catabolicRebirth != null && owner.GetMutationLevel(MutationIds.CatabolicRebirth) == catabolicRebirth.MaxLevel)
                        {
                            shouldAgeDouble = true;
                            observer.RecordCatabolicRebirthAgedToxin(owner.PlayerId, 1);
                            break;
                        }
                    }
                }
                if (shouldAgeDouble)
                    toxinCell.IncrementGrowthAge();
            }

            var toxinCells = AllToxinFungalCells().ToList();
            foreach (var cell in toxinCells)
            {
                if (cell.HasToxinExpired())
                {
                    int? toxinOwnerId = cell.OwnerPlayerId;
                    RaiseToxinExpired(new ToxinExpiredEventArgs(cell.TileId, toxinOwnerId));
                    RemoveCellInternal(cell.TileId, removeControl: true);
                }
            }
        }
        #endregion

        #region Geometry Helpers
        public List<int> GetTileLine(int startTileId, CardinalDirection direction, int length, bool includeStartingTile = false)
        {
            var result = new List<int>();
            int currentTileId = startTileId;
            if (includeStartingTile)
            {
                result.Add(currentTileId);
                if (result.Count >= length) return result;
            }
            for (int i = 0; i < length; i++)
            {
                int nextTileId = GetNeighborTileId(currentTileId, direction);
                if (nextTileId == -1) break;
                result.Add(nextTileId);
                if (result.Count >= length) break;
                currentTileId = nextTileId;
            }
            return result;
        }

        public List<int> GetTileCone(int startTileId, CardinalDirection direction)
        {
            var result = new List<int>();
            var (startX, startY) = GetXYFromTileId(startTileId);
            var (dirX, dirY) = GetDirectionVector(direction);
            var (perpX, perpY) = GetPerpendicularVector(direction);
            AddConeSection(result, startX, startY, dirX, dirY, perpX, perpY,
                MycovariantGameBalance.JettingMyceliumConeNarrowLength,
                MycovariantGameBalance.JettingMyceliumConeNarrowWidth, 0);
            AddConeSection(result, startX, startY, dirX, dirY, perpX, perpY,
                MycovariantGameBalance.JettingMyceliumConeMediumLength,
                MycovariantGameBalance.JettingMyceliumConeMediumWidth,
                MycovariantGameBalance.JettingMyceliumConeNarrowLength);
            AddConeSection(result, startX, startY, dirX, dirY, perpX, perpY,
                MycovariantGameBalance.JettingMyceliumConeWideLength,
                MycovariantGameBalance.JettingMyceliumConeWideWidth,
                MycovariantGameBalance.JettingMyceliumConeNarrowLength + MycovariantGameBalance.JettingMyceliumConeMediumLength);
            return result;
        }

        private (int dx, int dy) GetDirectionVector(CardinalDirection direction) => direction switch
        {
            CardinalDirection.North => (0, 1),
            CardinalDirection.South => (0, -1),
            CardinalDirection.East => (1, 0),
            CardinalDirection.West => (-1, 0),
            _ => (0, 0)
        };
        private (int perpX, int perpY) GetPerpendicularVector(CardinalDirection direction) => direction switch
        {
            CardinalDirection.North => (1, 0),
            CardinalDirection.South => (1, 0),
            CardinalDirection.East => (0, 1),
            CardinalDirection.West => (0, 1),
            _ => (0, 0)
        };
        private void AddConeSection(List<int> result, int startX, int startY, int dirX, int dirY,
            int perpX, int perpY, int sectionLength, int sectionWidth, int distanceOffset)
        {
            for (int distance = 1; distance <= sectionLength; distance++)
            {
                int centerX = startX + (distance + distanceOffset) * dirX;
                int centerY = startY + (distance + distanceOffset) * dirY;
                int halfWidth = sectionWidth / 2;
                for (int offset = -halfWidth; offset <= halfWidth; offset++)
                {
                    int tileX = centerX + offset * perpX;
                    int tileY = centerY + offset * perpY;
                    if (tileX >= 0 && tileX < Width && tileY >= 0 && tileY < Height)
                    {
                        int tileId = tileY * Width + tileX;
                        if (!result.Contains(tileId)) result.Add(tileId);
                    }
                }
            }
        }

        public int GetNeighborTileId(int tileId, CardinalDirection direction)
        {
            int x = tileId % Width;
            int y = tileId / Width;
            switch (direction)
            {
                case CardinalDirection.North: y += 1; break;
                case CardinalDirection.South: y -= 1; break;
                case CardinalDirection.East: x += 1; break;
                case CardinalDirection.West: x -= 1; break;
            }
            if (x < 0 || x >= Width || y < 0 || y >= Height) return -1;
            return y * Width + x;
        }
        #endregion

        #region Spore / Death Effects
        public void TryTriggerSporeOnDeath(Player player, Random rng, ISimulationObserver observer)
        {
            float dropChance = player.GetMutationEffect(MutationType.Necrosporulation);
            if (dropChance <= 0f || rng.NextDouble() > dropChance) return;
            var candidateTiles = AllTiles()
                .Where(t => !t.IsOccupied)
                .OrderBy(_ => rng.NextDouble())
                .ToList();
            foreach (var tile in candidateTiles)
            {
                bool spawned = SpawnSporeForPlayer(player, tile.TileId, GrowthSource.Necrosporulation);
                if (spawned)
                {
                    OnSporeDrop(player.PlayerId, tile.TileId, MutationType.Necrosporulation);
                    observer.ReportNecrosporeDrop(player.PlayerId, 1);
                    return;
                }
            }
        }
        #endregion

        #region Takeover Logic
        public FungalCellTakeoverResult TakeoverCell(int tileId, int newOwnerPlayerId, bool allowToxin, GrowthSource source, List<Player> players, Random rng, ISimulationObserver observer)
        {
            var tile = GetTileById(tileId);
            if (tile == null) return FungalCellTakeoverResult.Invalid;
            var existing = tile.FungalCell;
            if (existing == null) return FungalCellTakeoverResult.Invalid; // Empty not handled here
            if (existing.IsResistant) return FungalCellTakeoverResult.Invalid;
            if (existing.IsAlive && existing.OwnerPlayerId == newOwnerPlayerId)
                return FungalCellTakeoverResult.AlreadyOwned;

            FungalCellTakeoverResult result;
            var newCell = new FungalCell(ownerPlayerId: newOwnerPlayerId, tileId: tileId, source: source, lastOwnerPlayerId: existing.OwnerPlayerId);
            if (existing.IsAlive)
            {
                PlaceFungalCell(newCell);
                result = FungalCellTakeoverResult.Infested;
            }
            else if (existing.IsDead)
            {
                PlaceFungalCell(newCell);
                result = FungalCellTakeoverResult.Reclaimed;
            }
            else if (existing.IsToxin)
            {
                if (!allowToxin) return FungalCellTakeoverResult.Invalid;
                PlaceFungalCell(newCell);
                result = FungalCellTakeoverResult.Overgrown;
            }
            else
            {
                return FungalCellTakeoverResult.Invalid;
            }
            return result;
        }
        #endregion

        public void IncrementGrowthCycle() => CurrentGrowthCycle++;
        public void IncrementRound()
        {
            CurrentRound++;
            CurrentRoundContext.Reset();
        }
    }
}
