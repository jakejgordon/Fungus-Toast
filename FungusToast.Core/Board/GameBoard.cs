using FungusToast.Core.Config;
using FungusToast.Core.Events;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Core.Phases;
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

        public int TotalTiles => Width * Height;

        public delegate void CellColonizedEventHandler(int playerId, int tileId);
        public delegate void CellInfestedEventHandler(int playerId, int tileId, int oldOwnerId);
        public delegate void CellReclaimedEventHandler(int playerId, int tileId);
        public delegate void CellToxifiedEventHandler(int playerId, int tileId);
        public delegate void CellPoisonedEventHandler(int playerId, int tileId, int oldOwnerId);
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
        public delegate void DecayPhaseEventHandler(Dictionary<int, int> failedGrowthsByPlayerId);
        public delegate void PreGrowthCycleEventHandler();
        public delegate void DecayPhaseWithFailedGrowthsEventHandler(Dictionary<int, int> failedGrowthsByPlayerId);
        public delegate void NecrophyticBloomActivatedEventHandler();
        public delegate void MutationPhaseStartEventHandler();
        public delegate void ToxinPlacedEventHandler(object sender, ToxinPlacedEventArgs e);
        public delegate void ToxinExpiredEventHandler(object sender, ToxinExpiredEventArgs e);
        public delegate void CatabolicRebirthEventHandler(object sender, CatabolicRebirthEventArgs e);
        public delegate void PreGrowthPhaseEventHandler();

        // 2. Events (public, so other components can subscribe)
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
        public event DecayPhaseEventHandler? DecayPhase;
        public event PreGrowthCycleEventHandler? PreGrowthCycle;
        public event DecayPhaseWithFailedGrowthsEventHandler? DecayPhaseWithFailedGrowths;
        public event NecrophyticBloomActivatedEventHandler? NecrophyticBloomActivatedEvent;
        public event MutationPhaseStartEventHandler? MutationPhaseStart;
        public event ToxinPlacedEventHandler? ToxinPlaced;
        public event ToxinExpiredEventHandler? ToxinExpired;
        public event CatabolicRebirthEventHandler? CatabolicRebirth;
        public event PreGrowthPhaseEventHandler? PreGrowthPhase;

        // 3. Helper methods to invoke (recommended: protected virtual, as in standard .NET pattern)
        protected virtual void OnCellColonized(int playerId, int tileId) =>
            CellColonized?.Invoke(playerId, tileId);

        protected virtual void OnCellInfested(int playerId, int tileId, int oldOwnerId) =>
            CellInfested?.Invoke(playerId, tileId, oldOwnerId);

        protected virtual void OnCellReclaimed(int playerId, int tileId) =>
            CellReclaimed?.Invoke(playerId, tileId);

        protected virtual void OnCellToxified(int playerId, int tileId) =>
            CellToxified?.Invoke(playerId, tileId);

        protected virtual void OnCellPoisoned(int playerId, int tileId, int oldOwnerId) =>
            CellPoisoned?.Invoke(playerId, tileId, oldOwnerId);

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

        protected virtual void OnJettingMyceliumCatabolicGrowth(int playerId, int tileId) =>
            JettingMyceliumCatabolicGrowth?.Invoke(playerId, tileId);

        public virtual void OnPostGrowthPhase() =>
            PostGrowthPhase?.Invoke();

        public virtual void OnDecayPhase(Dictionary<int, int> failedGrowthsByPlayerId) =>
            DecayPhase?.Invoke(failedGrowthsByPlayerId);

        public virtual void OnPreGrowthCycle() =>
            PreGrowthCycle?.Invoke();

        public virtual void OnDecayPhaseWithFailedGrowths(Dictionary<int, int> failedGrowthsByPlayerId) =>
            DecayPhaseWithFailedGrowths?.Invoke(failedGrowthsByPlayerId);

        /// <summary>
        /// Fired before a growth attempt. Listeners may cancel the growth.
        /// </summary>
        public event EventHandler<GrowthAttemptEventArgs>? BeforeGrowthAttempt;

        /// <summary>
        /// Fired after a growth attempt, regardless of success. FailureReason can be set for post-mortem analysis.
        /// </summary>
        public event EventHandler<GrowthAttemptEventArgs>? AfterGrowthAttempt;

        /// <summary>
        /// Fired after a successful reclaim of a dead cell
        /// </summary>
        public event Action<FungalCell, int>? OnDeadCellReclaim;

        /// <summary>
        /// Raises the BeforeGrowthAttempt event.
        /// </summary>
        protected virtual void OnBeforeGrowthAttempt(GrowthAttemptEventArgs e) =>
            BeforeGrowthAttempt?.Invoke(this, e);

        /// <summary>
        /// Raises the AfterGrowthAttempt event.
        /// </summary>
        protected virtual void OnAfterGrowthAttempt(GrowthAttemptEventArgs e) =>
            AfterGrowthAttempt?.Invoke(this, e);

        public GameBoard(int width, int height, int playerCount)
        {
            Width = width;
            Height = height;
            Grid = new BoardTile[width, height];
            Players = new List<Player>(playerCount);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid[x, y] = new BoardTile(x, y, width);
                }
            }
        }

        public IEnumerable<BoardTile> AllTiles()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    yield return Grid[x, y];
        }

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

        public BoardTile? GetTile(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
                return Grid[x, y];
            return null;
        }

        public void PlaceInitialSpore(int playerId, int x, int y)
        {
            BoardTile tile = Grid[x, y];
            if (!tile.IsOccupied)
            {
                int tileId = y * Width + x;
                var cell = new FungalCell(playerId, tileId);
                cell.MakeResistant(); // Initial spores MUST be resistant to prevent elimination
                tile.PlaceFungalCell(cell);
                tileIdToCell[tileId] = cell;

                Players[playerId].ControlledTileIds.Add(tileId);
            }
        }


        public FungalCell? GetCell(int tileId)
        {
            tileIdToCell.TryGetValue(tileId, out var cell);
            return cell;
        }

        public void RemoveControlFromPlayer(int tileId)
        {
            foreach (var player in Players)
                player.ControlledTileIds.Remove(tileId);
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
                    {
                        int neighborId = ny * Width + nx;
                        neighbors.Add(neighborId);
                    }
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

        public (int x, int y) GetXYFromTileId(int tileId)
        {
            int x = tileId % Width;
            int y = tileId / Width;
            return (x, y);
        }

        public List<FungalCell> GetAllCells()
        {
            return tileIdToCell.Values.ToList();
        }

        public List<FungalCell> GetAllCellsOwnedBy(int playerId)
        {
            return tileIdToCell.Values.Where(c => c.OwnerPlayerId == playerId).ToList();
        }

        public bool SpawnSporeForPlayer(Player player, int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            var tile = GetTile(x, y);

            if (tile == null || tile.IsOccupied)
                return false;

            var cell = new FungalCell(player.PlayerId, tileId);
            // Note: Spores are NOT resistant by default
            // They only become resistant if adjacent to resistant cells via Hyphal Resistance Transfer
            tile.PlaceFungalCell(cell);
            tileIdToCell[tileId] = cell;

            player.ControlledTileIds.Add(tileId);
            return true;
        }

        public int CountReclaimedCellsByPlayer(int playerId)
        {
            return tileIdToCell.Values.Count(c =>
                c.CellType == FungalCellType.Alive &&
                c.OwnerPlayerId == playerId &&
                c.OriginalOwnerPlayerId == playerId &&
                c.ReclaimCount > 0);
        }

        public BoardTile? GetTileById(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            return GetTile(x, y);
        }

        public List<int> GetAllTileIds() => tileIdToCell.Keys.ToList();

        public List<BoardTile> GetDeadTiles()
        {
            return AllTiles().Where(t => t.FungalCell != null && t.FungalCell.CellType == FungalCellType.Dead).ToList();
        }

        public float GetOccupiedTileRatio()
        {
            int total = Width * Height;
            int occupied = AllTiles().Count(t => t.FungalCell != null);
            return (float)occupied / total;
        }


        public bool ShouldTriggerEndgame()
        {
            return GetOccupiedTileRatio() >= GameBalance.GameEndTileOccupancyThreshold;
        }

        public void IncrementGrowthCycle()
        {
            CurrentGrowthCycle++;
        }

        public IEnumerable<FungalCell> AllLivingFungalCells()
        {
            return AllTiles()
                .Where(t => t.FungalCell != null && t.FungalCell.CellType == FungalCellType.Alive)
                .Select(t => t.FungalCell!);
        }


        public IEnumerable<(BoardTile tile, FungalCell cell)> AllLivingFungalCellsWithTiles()
        {
            return AllTiles()
                .Where(t => t.FungalCell != null && t.FungalCell.CellType == FungalCellType.Alive)
                .Select(t => (t, t.FungalCell!));
        }

        public IEnumerable<BoardTile> AllToxinTiles()
        {
            return AllTiles().Where(t => t.FungalCell?.CellType == FungalCellType.Toxin);
        }

        public IEnumerable<FungalCell> AllToxinFungalCells()
        {
            return AllTiles()
                .Select(t => t.FungalCell)
                .Where(c => c != null && c.CellType == FungalCellType.Toxin)
                .Cast<FungalCell>();
        }

        public IEnumerable<BoardTile> GetAdjacentLivingTiles(int tileId, int? excludePlayerId = null)
        {
            foreach (int neighborId in GetAdjacentTileIds(tileId))
            {
                var tile = GetTileById(neighborId);
                if (tile == null || tile.FungalCell == null || tile.FungalCell.CellType != FungalCellType.Alive)
                    continue;

                var cell = tile.FungalCell;
                if (excludePlayerId.HasValue && cell.OwnerPlayerId == excludePlayerId.Value)
                    continue;

                yield return tile;
            }
        }

        public void ExpireToxinTiles(int currentGrowthCycle, ISimulationObserver? observer = null)
        {
            // Apply Catabolic Rebirth max-level bonus: toxins age twice as fast when adjacent to dead cells
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
                            observer?.RecordCatabolicRebirthAgedToxin(owner.PlayerId, 1);
                            break; // Only need one adjacent dead cell with max Catabolic Rebirth
                        }
                    }
                }
                
                // Age the toxin (this was already done in DeathEngine, but Catabolic Rebirth ages it again)
                if (shouldAgeDouble)
                {
                    toxinCell.IncrementGrowthAge(); // Extra aging for Catabolic Rebirth effect
                }
            }

            var allToxinTiles = AllToxinFungalCells().ToList(); // Snapshot to avoid collection issues
            foreach (var cell in allToxinTiles)
            {
                if (cell.HasToxinExpired())
                {
                    var tile = GetTileById(cell.TileId);
                    int? toxinOwnerId = cell.OwnerPlayerId;
                    
                    // Fire the toxin expired event before removing the cell
                    OnToxinExpired(new ToxinExpiredEventArgs(cell.TileId, toxinOwnerId));
                    
                    tile?.RemoveFungalCell(); // Clear the cell entirely from the board
                }
            }
        }

        public void IncrementRound()
        {
            CurrentRound++;
            // Reset round context at the start of each new round
            CurrentRoundContext.Reset();
        }

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
                if (nextTileId == -1) break; // Edge of board or invalid
                result.Add(nextTileId);
                if (result.Count >= length) break;
                currentTileId = nextTileId;
            }
            return result;
        }

        /// <summary>
        /// Generates a cone-shaped pattern of tiles emanating from the starting tile in the specified direction.
        /// The cone expands in width as it progresses, creating a spreading toxin cloud effect.
        /// </summary>
        /// <param name="startTileId">The starting tile ID (source of the cone)</param>
        /// <param name="direction">The direction the cone spreads</param>
        /// <returns>A list of tile IDs forming the cone pattern, ordered by distance from source</returns>
        public List<int> GetTileCone(int startTileId, CardinalDirection direction)
        {
            var result = new List<int>();
            var (startX, startY) = GetXYFromTileId(startTileId);
            
            // Get direction vectors
            var (dirX, dirY) = GetDirectionVector(direction);
            var (perpX, perpY) = GetPerpendicularVector(direction);
            
            // Narrow section: 1 tile wide for 4 tiles
            AddConeSection(result, startX, startY, dirX, dirY, perpX, perpY, 
                MycovariantGameBalance.JettingMyceliumConeNarrowLength, 
                MycovariantGameBalance.JettingMyceliumConeNarrowWidth, 0);
            
            // Medium section: 3 tiles wide for 3 tiles  
            AddConeSection(result, startX, startY, dirX, dirY, perpX, perpY,
                MycovariantGameBalance.JettingMyceliumConeMediumLength,
                MycovariantGameBalance.JettingMyceliumConeMediumWidth,
                MycovariantGameBalance.JettingMyceliumConeNarrowLength);
            
            // Wide section: 5 tiles wide for 3 tiles
            AddConeSection(result, startX, startY, dirX, dirY, perpX, perpY,
                MycovariantGameBalance.JettingMyceliumConeWideLength,
                MycovariantGameBalance.JettingMyceliumConeWideWidth,
                MycovariantGameBalance.JettingMyceliumConeNarrowLength + MycovariantGameBalance.JettingMyceliumConeMediumLength);
            
            return result;
        }

        /// <summary>
        /// Gets the direction vector (dx, dy) for a cardinal direction.
        /// </summary>
        private (int dx, int dy) GetDirectionVector(CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.North => (0, 1),
                CardinalDirection.South => (0, -1),
                CardinalDirection.East => (1, 0),
                CardinalDirection.West => (-1, 0),
                _ => (0, 0)
            };
        }

        /// <summary>
        /// Gets the perpendicular vector for a cardinal direction (used for cone width).
        /// </summary>
        private (int perpX, int perpY) GetPerpendicularVector(CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.North => (1, 0),  // Perpendicular to north is east/west
                CardinalDirection.South => (1, 0),  // Perpendicular to south is east/west
                CardinalDirection.East => (0, 1),   // Perpendicular to east is north/south
                CardinalDirection.West => (0, 1),   // Perpendicular to west is north/south
                _ => (0, 0)
            };
        }

        /// <summary>
        /// Adds a section of the cone with specified width and length to the result list.
        /// </summary>
        private void AddConeSection(List<int> result, int startX, int startY, int dirX, int dirY, 
            int perpX, int perpY, int sectionLength, int sectionWidth, int distanceOffset)
        {
            for (int distance = 1; distance <= sectionLength; distance++)
            {
                int centerX = startX + (distance + distanceOffset) * dirX;
                int centerY = startY + (distance + distanceOffset) * dirY;
                
                // Add tiles across the width of the cone at this distance
                int halfWidth = sectionWidth / 2;
                for (int offset = -halfWidth; offset <= halfWidth; offset++)
                {
                    int tileX = centerX + offset * perpX;
                    int tileY = centerY + offset * perpY;
                    
                    // Check bounds and add valid tiles
                    if (tileX >= 0 && tileX < Width && tileY >= 0 && tileY < Height)
                    {
                        int tileId = tileY * Width + tileX;
                        if (!result.Contains(tileId)) // Avoid duplicates
                        {
                            result.Add(tileId);
                        }
                    }
                }
            }
        }

        // You'll need a helper to get a neighbor tile ID in a given direction:
        public int GetNeighborTileId(int tileId, CardinalDirection direction)
        {
            // Example assuming a 2D grid and tileId is mapped row-major: tileId = y * width + x
            int x = tileId % Width;
            int y = tileId / Width;

            switch (direction)
            {
                case CardinalDirection.North: y += 1; break; // Fixed: North should increase Y (move up visually)
                case CardinalDirection.South: y -= 1; break; // Fixed: South should decrease Y (move down visually)
                case CardinalDirection.East: x += 1; break;
                case CardinalDirection.West: x -= 1; break;
            }

            // Check for board bounds
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return -1;

            return y * Width + x;
        }


        // NEW STUFF FROM REFACTORING

        /// <summary>
        /// Attempts to trigger a spore drop for the given player after a fungal cell dies.
        /// The chance and behavior are mutation-driven. If a valid spore tile is found,
        /// a new fungal cell is placed and relevant events are fired.
        /// </summary>
        /// <param name="player">The player who may receive a spore drop.</param>
        /// <param name="rng">Random number generator.</param>
        /// <param name="observer">Optional simulation observer to track spore events.</param>
        public void TryTriggerSporeOnDeath(Player player, Random rng, ISimulationObserver? observer = null)
        {
            float dropChance = player.GetMutationEffect(MutationType.SporeOnDeathChance);
            if (dropChance <= 0f || rng.NextDouble() > dropChance)
                return;

            var candidateTiles = AllTiles()
                .Where(t => !t.IsOccupied)
                .OrderBy(_ => rng.NextDouble())
                .ToList();

            foreach (var tile in candidateTiles)
            {
                bool spawned = SpawnSporeForPlayer(player, tile.TileId);
                if (spawned)
                {
                    OnSporeDrop(player.PlayerId, tile.TileId, MutationType.SporeOnDeathChance);
                    observer?.ReportNecrosporeDrop(player.PlayerId, 1);
                    return;
                }
            }
        }


        /// <summary>
        /// Attempts to grow a fungal cell from <paramref name="sourceTileId"/> into <paramref name="targetTileId"/>.
        /// - Fires BeforeGrowthAttempt (cancelable) and AfterGrowthAttempt (result) for all attempts.
        /// - If <paramref name="canReclaimDeadCell"/> is true, allows reclaiming the player's own dead cell as a special case (used for Regenerative Hyphae effects).
        /// - Fires OnDeadCellReclaim after a successful reclaim of a dead cell.
        /// Returns true if growth or reclaim succeeded, false if the attempt failed or was canceled.
        /// </summary>
        /// <param name="playerId">The player ID performing the growth.</param>
        /// <param name="sourceTileId">The tile ID of the source fungal cell (may be -1 if not applicable).</param>
        /// <param name="targetTileId">The tile ID where the cell will attempt to grow.</param>
        /// <param name="failureReason">Outputs the failure reason if growth was not successful.</param>
        /// <param name="canReclaimDeadCell">If true, allows the player to reclaim their own dead cell (for Regenerative Hyphae). Default is false.</param>
        /// <returns>True if growth or reclaim succeeded, false otherwise.</returns>
        public bool TryGrowFungalCell(
           int playerId,
           int sourceTileId,
           int targetTileId,
           out GrowthFailureReason failureReason,
           bool canReclaimDeadCell = false
       )
        {
            failureReason = GrowthFailureReason.None;

            var targetTile = GetTileById(targetTileId);
            if (targetTile == null)
            {
                failureReason = GrowthFailureReason.InvalidTarget;
                return false;
            }

            // Check if target tile has a resistant cell - cannot grow into resistant cells
            if (targetTile.IsResistant)
            {
                failureReason = GrowthFailureReason.OccupiedByResistantCell;
                return false;
            }

            // Standard: Only allow growth into empty tile
            if (!targetTile.IsOccupied)
            {
                var newCell = new FungalCell(playerId, targetTileId);
                newCell.MarkAsNewlyGrown(); // Mark for fade-in effect
                targetTile.PlaceFungalCell(newCell);
                PlaceFungalCell(newCell);
                // Player.AddControlledTile(targetTileId); // Do this elsewhere as needed
                return true;
            }

            // Special: allow reclaiming your own dead cell if allowed
            if (canReclaimDeadCell && targetTile.IsOccupied)
            {
                var deadCell = targetTile.FungalCell;
                // Only allow if it's the player's own dead cell
                if (deadCell != null && deadCell.IsDead && deadCell.OwnerPlayerId == playerId)
                {
                    deadCell.Reclaim(playerId);
                    PlaceFungalCell(deadCell);
                    // Notify event listeners
                    OnDeadCellReclaim?.Invoke(deadCell, playerId);
                    failureReason = GrowthFailureReason.None;
                    return true;
                }
                failureReason = GrowthFailureReason.TileOccupied;
                return false;
            }

            // All other cases: can't grow here
            failureReason = targetTile.IsOccupied ? GrowthFailureReason.TileOccupied : GrowthFailureReason.InvalidTarget;
            return false;
        }

        /// <summary>
        /// Places the given fungal cell on the board, replacing any existing cell at that tile.
        /// - Removes control from any previous owner.
        /// - Adds control to the new owner.
        /// - Updates the board state and fires the appropriate events.
        /// - Resistant cells cannot be replaced.
        /// </summary>
        /// <param name="cell">The fungal cell to place.</param>
        internal void PlaceFungalCell(FungalCell cell)
        {
            var (x, y) = GetXYFromTileId(cell.TileId);
            var tile = Grid[x, y];
            var oldCell = tile.FungalCell;

            // Check if the old cell is resistant - cannot replace resistant cells
            if (oldCell != null && oldCell.IsResistant)
                return;

            // Remove control from previous owner, if any
            if (oldCell != null && oldCell.OwnerPlayerId.HasValue)
            {
                int prevOwnerId = oldCell.OwnerPlayerId.Value;
                Players[prevOwnerId].ControlledTileIds.Remove(cell.TileId);
            }

            // Place the new cell on the tile and update mapping
            tile.PlaceFungalCell(cell);
            tileIdToCell[cell.TileId] = cell;

            // Add control to new owner, if any
            if (cell.OwnerPlayerId.HasValue)
            {
                int newOwnerId = cell.OwnerPlayerId.Value;
                if (!Players[newOwnerId].ControlledTileIds.Contains(cell.TileId))
                    Players[newOwnerId].ControlledTileIds.Add(cell.TileId);
            }

            int ownerId = cell.OwnerPlayerId.GetValueOrDefault(-1);

            // Event firing logic (unchanged)
            if (oldCell == null)
            {
                // Colonization: tile was empty
                OnCellColonized(ownerId, cell.TileId);
            }
            else if (oldCell.IsAlive)
            {
                int oldOwnerId = oldCell.OwnerPlayerId.GetValueOrDefault(-1);
                // Infestation: replace enemy living cell
                if (oldOwnerId != ownerId)
                    OnCellInfested(ownerId, cell.TileId, oldOwnerId);
                else
                    OnCellColonized(ownerId, cell.TileId); // Unusual, but treat as colonization
            }
            else if (oldCell.IsToxin)
            {
                int oldOwnerId = oldCell.OwnerPlayerId.GetValueOrDefault(-1);
                // Poisoning: replace toxin with living cell
                OnCellPoisoned(ownerId, cell.TileId, oldOwnerId);
            }
            else if (oldCell.IsDead)
            {
                int currentOwnerId = oldCell.OwnerPlayerId ?? -1;
                // Reclaim: revive dead cell (could be own or enemy, but should check for ownership)
                if (currentOwnerId == ownerId)
                    OnCellReclaimed(ownerId, cell.TileId);
                else
                    OnCellInfested(ownerId, cell.TileId, currentOwnerId); // Parasitic reclaim
            }
        }


        internal void InternalColonizeCell(int playerId, int tileId)
        {
            var cell = new FungalCell(playerId, tileId);
            tileIdToCell[tileId] = cell;
            var (x, y) = GetXYFromTileId(tileId);
            Grid[x, y].PlaceFungalCell(cell);
            OnCellColonized(playerId, tileId);
        }

        internal void InternalInfestCell(int playerId, int tileId, int oldOwnerId)
        {
            var cell = new FungalCell(playerId, tileId);
            tileIdToCell[tileId] = cell;
            var (x, y) = GetXYFromTileId(tileId);
            Grid[x, y].PlaceFungalCell(cell);
            OnCellInfested(playerId, tileId, oldOwnerId);
        }

        public void KillFungalCell(FungalCell cell, DeathReason reason, int? killerPlayerId = null, int? attackerTileId = null)
        {
            // Resistant cells cannot be killed
            if (cell.IsResistant)
                return;

            int tileId = cell.TileId;
            int playerId = cell.OwnerPlayerId ?? -1;

            // Mark the cell as dying for death animation
            cell.MarkAsDying();

            cell.Kill(reason);
            RemoveControlFromPlayer(tileId);
            OnCellDeath(playerId, tileId, reason, killerPlayerId, cell, attackerTileId);
        }


        /// <summary>
        /// Attempts to reclaim a dead fungal cell at the given tile for the specified player.
        /// If successful, updates control, fires events, and returns true.
        /// </summary>
        public bool TryReclaimDeadCell(int playerId, int tileId)
        {
            var tile = GetTileById(tileId);
            if (tile?.FungalCell == null || !tile.FungalCell.IsDead)
            {
                return false;
            }

            var cell = tile.FungalCell;
            if (cell.OwnerPlayerId != playerId)
            {
                return false;
            }

            cell.Reclaim(playerId);
            PlaceFungalCell(cell); // Fires correct events
            Players[playerId].AddControlledTile(tileId);
            OnDeadCellReclaim?.Invoke(cell, playerId);
            return true;
        }

        public virtual void OnNecrophyticBloomActivatedEvent() =>
            NecrophyticBloomActivatedEvent?.Invoke();

        public virtual void OnMutationPhaseStart() =>
            MutationPhaseStart?.Invoke();

        public virtual void OnToxinPlaced(ToxinPlacedEventArgs e) =>
            ToxinPlaced?.Invoke(this, e);

        public virtual void OnToxinExpired(ToxinExpiredEventArgs e) =>
            ToxinExpired?.Invoke(this, e);

        public virtual void OnCatabolicRebirth(CatabolicRebirthEventArgs e) =>
            CatabolicRebirth?.Invoke(this, e);

        public virtual void OnPreGrowthPhase() =>
            PreGrowthPhase?.Invoke();

        /// <summary>
        /// Attempts to take over a cell at the given tile for the specified player.
        /// Handles all board state, player control, and event firing.
        /// Use this instead of calling FungalCell.Takeover directly.
        /// </summary>
        /// <param name="tileId">The tile to take over.</param>
        /// <param name="newOwnerPlayerId">The player taking over the cell.</param>
        /// <param name="allowToxin">Whether to allow takeover of toxin cells.</param>
        /// <param name="players">List of players (needed for Reclamation Rhizomorphs effect)</param>
        /// <param name="rng">Random number generator (needed for Reclamation Rhizomorphs effect)</param>
        /// <param name="observer">Simulation observer (needed for tracking)</param>
        /// <returns>The result of the takeover attempt.</returns>
        public FungalCellTakeoverResult TakeoverCell(
            int tileId,
            int newOwnerPlayerId,
            bool allowToxin,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            var cell = GetTileById(tileId)?.FungalCell;
            if (cell == null)
                return FungalCellTakeoverResult.Invalid;
            
            var result = cell.Takeover(newOwnerPlayerId, allowToxin);
            
            if (result == FungalCellTakeoverResult.Infested ||
                result == FungalCellTakeoverResult.Reclaimed ||
                result == FungalCellTakeoverResult.CatabolicGrowth)
            {
                PlaceFungalCell(cell);
            }
            return result;
        }

        /// <summary>
        /// Public method to invoke the OnDeadCellReclaim event from outside this class.
        /// </summary>
        public void InvokeDeadCellReclaim(FungalCell cell, int playerId)
        {
            OnDeadCellReclaim?.Invoke(cell, playerId);
        }

    }
}
