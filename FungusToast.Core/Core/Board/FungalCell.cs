﻿using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
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
        private int _toxinExpirationCycle = 0;
        public int ToxinExpirationCycle
        {
            get => _toxinExpirationCycle;
            internal set { _toxinExpirationCycle = value; }
        }

        public DeathReason? CauseOfDeath { get; private set; }
        public int? LastOwnerPlayerId { get; private set; } = null;
        public int ReclaimCount { get; private set; } = 0;

        /// <summary>
        /// Whether this cell is resistant to all forms of death and cannot be killed or replaced.
        /// </summary>
        public bool IsResistant { get; private set; } = false;

        public FungalCell() { }

        public FungalCell(int? ownerPlayerId, int tileId)
        {
            OwnerPlayerId = ownerPlayerId;
            if (ownerPlayerId.HasValue)
                OriginalOwnerPlayerId = ownerPlayerId.Value;
            TileId = tileId;
            SetAlive();
        }

        /// <summary>
        /// Create a toxin Fungal Cell.
        /// </summary>
        public FungalCell(int? ownerPlayerId, int tileId, int toxinExpirationCycle)
        {
            if (toxinExpirationCycle <= 0)
                throw new ArgumentOutOfRangeException(nameof(toxinExpirationCycle), "Expiration must be greater than 0.");

            OwnerPlayerId = ownerPlayerId;
            if (ownerPlayerId.HasValue)
                OriginalOwnerPlayerId = ownerPlayerId.Value;

            TileId = tileId;
            SetToxin(toxinExpirationCycle);
        }

        // State transitions
        private void SetAlive()
        {
            CellType = FungalCellType.Alive;
            GrowthCycleAge = 0;
            ToxinExpirationCycle = 0;
            CauseOfDeath = null;
            LastOwnerPlayerId = null;
        }

        private void SetDead(DeathReason reason)
        {
            CellType = FungalCellType.Dead;
            CauseOfDeath = reason;
            LastOwnerPlayerId = OwnerPlayerId;
            ToxinExpirationCycle = 0;
        }

        private void SetToxin(int expirationCycle)
        {
            CellType = FungalCellType.Toxin;
            ToxinExpirationCycle = expirationCycle;
            // Optionally clear other states
        }

        /// <summary>
        /// Makes this cell resistant to all forms of death and replacement.
        /// </summary>
        public void MakeResistant()
        {
            IsResistant = true;
        }

        /// <summary>
        /// Kills this cell (living → dead), for any non-toxin death (Killed/Withered/Poisoned/etc).
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
        public void Reclaim(int newOwnerPlayerId)
        {
            if (!IsReclaimable)
                throw new InvalidOperationException("Cannot reclaim a non-reclaimable cell.");

            OwnerPlayerId = newOwnerPlayerId;
            SetAlive();
            ReclaimCount++;
        }

        /// <summary>
        /// Attempts to take over this cell as the given player, regardless of prior state.
        /// Returns the outcome for simulation/stat tracking.
        /// Resistant cells cannot be taken over.
        /// </summary>
        public FungalCellTakeoverResult Takeover(int newOwnerPlayerId, bool allowToxin = false)
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
                Kill(DeathReason.Infested); // Cell killed by rival, so "Infested"
                OwnerPlayerId = newOwnerPlayerId;
                SetAlive();
                return FungalCellTakeoverResult.Infested;
            }

            // Dead/reclaimable: Reclaim
            if (IsReclaimable)
            {
                OwnerPlayerId = newOwnerPlayerId;
                SetAlive();
                ReclaimCount++;
                return FungalCellTakeoverResult.Reclaimed;
            }

            // Toxin: Only allowed if flag set
            if (IsToxin && allowToxin)
            {
                OwnerPlayerId = newOwnerPlayerId;
                SetAlive();
                // Optionally reset toxin-related fields/stats here
                return FungalCellTakeoverResult.CatabolicGrowth;
            }

            return FungalCellTakeoverResult.Invalid;
        }

        public void IncrementGrowthAge() => GrowthCycleAge++;
        public void ResetGrowthCycleAge() => GrowthCycleAge = 0;
        public void SetGrowthCycleAge(int age) => GrowthCycleAge = age;

        /// <summary>
        /// Mark a dead cell as a toxin (Toxified). Used for dropping toxin on empty/dead cells.
        /// </summary>
        public void MarkAsToxin(int expirationCycle, Player? owner = null, int? baseCycle = null)
        {
            if (IsAlive)
                throw new InvalidOperationException("Cannot mark a living cell as toxin.");
            if (expirationCycle <= 0)
                throw new ArgumentOutOfRangeException(nameof(expirationCycle), "Expiration must be greater than 0.");

            SetToxin(CalculateAdjustedExpiration(expirationCycle, owner, baseCycle));
        }

        /// <summary>
        /// Converts a cell to toxin, killing if alive (Poisoned) or overwriting dead/empty (Toxified).
        /// Resistant cells cannot be converted to toxins.
        /// </summary>
        public void ConvertToToxin(int expirationCycle, Player? owner = null, DeathReason? reason = null, int? baseCycle = null)
        {
            // Resistant cells cannot be converted to toxins
            if (IsResistant)
                return;

            if (IsAlive)
                Kill(reason ?? DeathReason.Poisoned); // Poisoned is now the default death by toxin

            if (owner != null)
                OwnerPlayerId = owner.PlayerId;

            SetToxin(CalculateAdjustedExpiration(expirationCycle, owner, baseCycle));
        }

        private int CalculateAdjustedExpiration(int expirationCycle, Player? owner, int? baseCycle)
        {
            if (owner == null || baseCycle == null)
                return expirationCycle;

            int bonus = owner.GetMutationLevel(MutationIds.MycotoxinPotentiation)
                       * GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel;

            return baseCycle.Value + (expirationCycle - baseCycle.Value) + bonus;
        }

        public void ClearToxinState()
        {
            if (IsToxin)
            {
                CellType = FungalCellType.Dead;
                ToxinExpirationCycle = 0;
            }
        }

        public bool HasToxinExpired(int currentGrowthCycle)
        {
            return IsToxin && currentGrowthCycle >= ToxinExpirationCycle;
        }

        public int ReduceGrowthCycleAge(int amount)
        {
            int oldAge = GrowthCycleAge;
            GrowthCycleAge = Math.Max(0, GrowthCycleAge - amount);
            return oldAge - GrowthCycleAge;
        }
    }
}
