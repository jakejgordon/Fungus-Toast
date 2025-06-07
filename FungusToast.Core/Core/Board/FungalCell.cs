using FungusToast.Core.Config;
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

        public bool IsAlive { get; internal set; } = true;
        public int GrowthCycleAge { get; private set; } = 0;
        private int _toxinExpirationCycle = 0;
        public int ToxinExpirationCycle
        {
            get => _toxinExpirationCycle;
            private set
            {
                // Place a breakpoint here!
                _toxinExpirationCycle = value;
            }
        }

        public bool IsToxin => ToxinExpirationCycle > 0;
        public bool IsDead => !IsAlive && !IsToxin;
        public bool IsReclaimable => IsDead && !IsToxin;

        public DeathReason? CauseOfDeath { get; private set; }

        /// <summary>
        /// The owner at the moment the cell died. Used for attribution in game result summaries.
        /// </summary>
        public int? LastOwnerPlayerId { get; private set; } = null;

        public int ReclaimCount { get; private set; } = 0;

        public FungalCell() { }

        public FungalCell(int? ownerPlayerId, int tileId)
        {
            OwnerPlayerId = ownerPlayerId;
            if (ownerPlayerId.HasValue)
            {
                OriginalOwnerPlayerId = ownerPlayerId.Value;
            }
            TileId = tileId;
            IsAlive = true;
        }

        /// <summary>
        /// Create a toxin Fungal Cell
        /// </summary>
        /// <param name="ownerPlayerId"></param>
        /// <param name="tileId"></param>
        /// <param name="toxinExpirationCycle"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public FungalCell(int? ownerPlayerId, int tileId, int toxinExpirationCycle)
        {
            if (toxinExpirationCycle <= 0)
                throw new ArgumentOutOfRangeException(nameof(toxinExpirationCycle), "Expiration must be greater than 0.");

            OwnerPlayerId = ownerPlayerId;
            if (ownerPlayerId.HasValue)
            {
                OriginalOwnerPlayerId = ownerPlayerId.Value;
            }

            TileId = tileId;
            IsAlive = false;
            ToxinExpirationCycle = toxinExpirationCycle;
            LastOwnerPlayerId = null; // It was never alive
        }


        public void Kill(DeathReason reason)
        {
            if (!IsAlive)
                return;

            IsAlive = false;
            CauseOfDeath = reason;
            LastOwnerPlayerId = OwnerPlayerId;
        }

        public void Reclaim(int newOwnerPlayerId)
        {
            if (IsAlive)
                throw new InvalidOperationException("Cannot reclaim a living cell.");
            if (IsToxin)
                throw new InvalidOperationException("Cannot reclaim a toxic cell.");

            OwnerPlayerId = newOwnerPlayerId;
            IsAlive = true;
            GrowthCycleAge = 0;
            CauseOfDeath = null;
            LastOwnerPlayerId = null;
            ToxinExpirationCycle = 0;
            ReclaimCount++;
        }

        public void IncrementGrowthAge()
        {
            GrowthCycleAge++;
        }

        public void ResetGrowthCycleAge()
        {
            GrowthCycleAge = 0;
        }

        public void SetGrowthCycleAge(int age)
        {
            GrowthCycleAge = age;
        }

        public void MarkAsToxin(int expirationCycle, Player? owner = null, int? baseCycle = null)
        {
            if (IsAlive)
                throw new InvalidOperationException("Cannot mark a living cell as toxin.");
            if (expirationCycle <= 0)
                throw new ArgumentOutOfRangeException(nameof(expirationCycle), "Expiration must be greater than 0.");

            ToxinExpirationCycle = CalculateAdjustedExpiration(expirationCycle, owner, baseCycle);
        }

        public void ConvertToToxin(int expirationCycle,
                                   Player? owner = null,
                                   DeathReason? reason = null,
                                   int? baseCycle = null)
        {
            if (IsAlive)
            {
                Kill(reason ?? DeathReason.Unknown);
            }

            if (owner != null)
            {
                OwnerPlayerId = owner.PlayerId;
            }

            ToxinExpirationCycle = CalculateAdjustedExpiration(expirationCycle, owner, baseCycle);
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
            ToxinExpirationCycle = 0;
        }

        public bool HasToxinExpired(int currentGrowthCycle)
        {
            return IsToxin && currentGrowthCycle >= ToxinExpirationCycle;
        }
    }
}
