using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;
using System;

namespace FungusToast.Core.Board
{
    public class FungalCell
    {
        public int OriginalOwnerPlayerId { get; private set; }
        public int? OwnerPlayerId { get; internal set; }
        public int TileId { get; private set; }

        // Enum-based cell type
        private FungalCellType _cellType = FungalCellType.Alive;
        public FungalCellType CellType
        {
            get => _cellType;
            private set => _cellType = value;
        }

        // Compatibility
        public bool IsAlive => CellType == FungalCellType.Alive;
        public bool IsDead => CellType == FungalCellType.Dead;
        public bool IsToxin => CellType == FungalCellType.Toxin;
        public bool IsReclaimable => IsDead && !IsToxin;

        public int GrowthCycleAge { get; private set; } = 0;
        
        /// <summary>
        /// The age in growth cycles when this toxin should expire.
        /// Used for age-based toxin expiration mechanics.
        /// </summary>
        public int ToxinExpirationAge { get; internal set; } = 0;

        /// <summary>
        /// Whether this cell was created in the current growth cycle (for fade-in effects)
        /// </summary>
        public bool IsNewlyGrown { get; private set; } = false;

        /// <summary>
        /// Whether this cell is currently dying (for death animation effects)
        /// </summary>
        public bool IsDying { get; private set; } = false;

        /// <summary>
        /// Whether this cell is currently receiving a toxin drop (for toxin drop animation effects)
        /// </summary>
        public bool IsReceivingToxinDrop { get; private set; } = false;

        public DeathReason? CauseOfDeath { get; private set; }
        
        /// <summary>
        /// The source/reason why this cell was created or became alive.
        /// Tracks how the cell came into existence (initial spore, growth, reclaim, etc.)
        /// </summary>
        public GrowthSource? SourceOfGrowth { get; private set; }
        
        public int? LastOwnerPlayerId { get; private set; } = null;
        public int ReclaimCount { get; private set; } = 0;

        /// <summary>
        /// Whether this cell is resistant to all forms of death and cannot be killed or replaced.
        /// </summary>
        public bool IsResistant { get; private set; } = false;

        public FungalCell() { }

        public FungalCell(int? ownerPlayerId, int tileId, GrowthSource source)
        {
            OwnerPlayerId = ownerPlayerId;
            if (ownerPlayerId.HasValue)
                OriginalOwnerPlayerId = ownerPlayerId.Value;
            TileId = tileId;
            GrowthCycleAge = 0;
            SourceOfGrowth = source;
            SetAlive(source);
        }

        /// <summary>
        /// Create a toxin Fungal Cell with specified lifespan.
        /// </summary>
        public FungalCell(int? ownerPlayerId, int tileId, GrowthSource source, int toxinExpirationAge = GameBalance.DefaultToxinDuration)
        {
            OwnerPlayerId = ownerPlayerId;
            if (ownerPlayerId.HasValue)
                OriginalOwnerPlayerId = ownerPlayerId.Value;
            TileId = tileId;
            GrowthCycleAge = 0;
            SourceOfGrowth = source;

            // set toxin stuff
            ToxinExpirationAge = toxinExpirationAge;
            CellType = FungalCellType.Toxin;
        }

        /// <summary>
        /// Changes the owner of this cell, preserving the previous owner in LastOwnerPlayerId.
        /// Also preserves CauseOfDeath when ownership changes occur.
        /// </summary>
        private void ChangeOwnership(int? newOwnerPlayerId, DeathReason? causeOfDeath = null)
        {
            if (OwnerPlayerId != newOwnerPlayerId)
            {
                LastOwnerPlayerId = OwnerPlayerId;
                OwnerPlayerId = newOwnerPlayerId;
                
                // Set cause of death if the ownership change represents a death/displacement
                if (causeOfDeath.HasValue)
                {
                    CauseOfDeath = causeOfDeath;
                }
            }
        }

        // State transitions
        private void SetAlive(GrowthSource source = GrowthSource.Unknown)
        {
            CellType = FungalCellType.Alive;
            GrowthCycleAge = 0;
            // Clear CauseOfDeath when becoming alive - keep LastOwnerPlayerId as historical data
            CauseOfDeath = null;
            SourceOfGrowth = source;
        }

        private void SetDead(DeathReason reason)
        {
            CellType = FungalCellType.Dead;
            CauseOfDeath = reason;
            // For natural deaths (same owner), set LastOwnerPlayerId to track who lost the cell
            LastOwnerPlayerId = OwnerPlayerId;
            // Keep SourceOfGrowth for historical reference
        }

        private void SetToxin(int toxinLifespan, GrowthSource? growthSource = null)
        {
            CellType = FungalCellType.Toxin;
            ToxinExpirationAge = toxinLifespan; // This is what determines expiration
            GrowthCycleAge = 0; // Reset growth cycle age when becoming a toxin
            SourceOfGrowth = growthSource; // Set the provided growth source or null
            // Don't modify ownership tracking here
        }

        /// <summary>
        /// Makes this cell resistant to all forms of death and replacement.
        /// </summary>
        public void MakeResistant()
        {
            IsResistant = true;
        }

        /// <summary>
        /// Clears the toxin drop flag (called after toxin drop animation completes)
        /// </summary>
        public void ClearToxinDropFlag()
        {
            IsReceivingToxinDrop = false;
        }

        /// <summary>
        /// Kills this cell (living → dead), for any non-toxin death (Killed/Infested/Poisoned/etc).
        /// Resistant cells cannot be killed.
        /// </summary>
        public void Kill(DeathReason reason)
        {
            if (IsResistant)
                return; // Resistant cells cannot be killed
            
            if (IsAlive)
                SetDead(reason);
            // No-op if not alive
        }

        /// <summary>
        /// Reclaims a dead cell (of any prior owner) as a new living cell for this player.
        /// </summary>
        public void Reclaim(int newOwnerPlayerId, GrowthSource source = GrowthSource.Reclaim)
        {
            if (!IsReclaimable)
                throw new InvalidOperationException("Cannot reclaim a non-reclaimable cell.");

            ChangeOwnership(newOwnerPlayerId);
            SetAlive(source);
            ReclaimCount++;
        }

        /// <summary>
        /// Attempts to take over this cell as the given player, regardless of prior state.
        /// Returns the outcome for simulation/stat tracking.
        /// Resistant cells cannot be taken over.
        /// </summary>
        public FungalCellTakeoverResult Takeover(int newOwnerPlayerId, GrowthSource source = GrowthSource.Unknown, bool allowToxin = false)
        {
            // Resistant cells cannot be taken over
            if (IsResistant)
                return FungalCellTakeoverResult.InvalidBecauseResistant;

            // Already owned and alive: no action
            if (OwnerPlayerId == newOwnerPlayerId && IsAlive)
                return FungalCellTakeoverResult.AlreadyOwned;

            // Living enemy cell: Infest (replace enemy cell with yours)
            if (IsAlive && OwnerPlayerId != newOwnerPlayerId)
            {
                // Record the displacement/death first, then revive as new owner's cell
                ChangeOwnership(newOwnerPlayerId, DeathReason.Infested);
                SetAlive(source); // This will clear CauseOfDeath, but LastOwnerPlayerId is preserved from ChangeOwnership
                return FungalCellTakeoverResult.Infested;
            }

            // Dead/reclaimable: Reclaim
            if (IsReclaimable)
            {
                ChangeOwnership(newOwnerPlayerId);
                SetAlive(source);
                ReclaimCount++;
                return FungalCellTakeoverResult.Reclaimed;
            }

            // Toxin: Only allowed if flag set
            if (IsToxin && allowToxin)
            {
                ChangeOwnership(newOwnerPlayerId);
                SetAlive(source);
                // Optionally reset toxin-related fields/stats here
                return FungalCellTakeoverResult.CatabolicGrowth;
            }

            return FungalCellTakeoverResult.Invalid;
        }

        public void IncrementGrowthAge() => GrowthCycleAge++;
        public void ResetGrowthCycleAge() => GrowthCycleAge = 0;
        public void SetGrowthCycleAge(int age)
        {
            GrowthCycleAge = age;
        }

        /// <summary>
        /// Marks this cell as newly grown for fade-in effects
        /// </summary>
        public void MarkAsNewlyGrown()
        {
            IsNewlyGrown = true;
        }

        /// <summary>
        /// Clears the newly grown flag (called after fade-in animation completes)
        /// </summary>
        public void ClearNewlyGrownFlag()
        {
            IsNewlyGrown = false;
        }

        /// <summary>
        /// Marks this cell as dying for death animation effects
        /// </summary>
        public void MarkAsDying()
        {
            IsDying = true;
        }

        /// <summary>
        /// Clears the dying flag (called after death animation completes)
        /// </summary>
        public void ClearDyingFlag()
        {
            IsDying = false;
        }

        /// <summary>
        /// Marks this cell as receiving a toxin drop for toxin drop animation effects
        /// </summary>
        public void MarkAsReceivingToxinDrop()
        {
            IsReceivingToxinDrop = true;
        }


        /// <summary>
        /// Converts a cell to toxin, killing if alive (Poisoned) or overwriting dead/empty (Toxified).
        /// Resistant cells cannot be converted to toxins.
        /// </summary>
        public void ConvertToToxin(int toxinLifespan, GrowthSource growthSource, Player? owner = null, DeathReason? reason = null)
        {
            // Resistant cells cannot be converted to toxins
            if (IsResistant)
                return;

            if (IsAlive)
                Kill(reason ?? DeathReason.Poisoned); // Poisoned is now the default death by toxin

            if (owner != null)
                ChangeOwnership(owner.PlayerId, reason ?? DeathReason.Poisoned);

            SetToxin(toxinLifespan, growthSource);
        }

        public bool HasToxinExpired()
        {
            // Age-based expiration: toxin expires when its age reaches its expiration age
            return IsToxin && GrowthCycleAge >= ToxinExpirationAge;
        }

        public int ReduceGrowthCycleAge(int amount)
        {
            int oldAge = GrowthCycleAge;
            GrowthCycleAge = Math.Max(0, GrowthCycleAge - amount);
            return oldAge - GrowthCycleAge;
        }
    }
}
