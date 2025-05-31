using FungusToast.Core.Death;
using System;

namespace FungusToast.Core.Board
{
    public class FungalCell
    {
        public int OriginalOwnerPlayerId { get; private set; }
        public int OwnerPlayerId { get; internal set; }
        public int TileId { get; private set; }

        public bool IsAlive { get; internal set; } = true;
        public int ToxinLevel { get; private set; } = 0;
        public int GrowthCycleAge { get; private set; } = 0;

        public DeathReason? CauseOfDeath { get; private set; }

        /// <summary>
        /// The owner at the moment the cell died. Used for attribution in game result summaries.
        /// </summary>
        public int? LastOwnerPlayerId { get; private set; } = null;

        public int ReclaimCount { get; private set; } = 0;

        // Toxin state
        public bool IsToxin { get; internal set; } = false;

        public FungalCell() { }

        public FungalCell(int ownerPlayerId, int tileId)
        {
            OwnerPlayerId = ownerPlayerId;
            OriginalOwnerPlayerId = ownerPlayerId;
            TileId = tileId;
            IsAlive = true;
            ToxinLevel = 0;
        }

        /// <summary>
        /// Alternate constructor for a cell that starts as a toxin.
        /// </summary>
        public FungalCell(int ownerPlayerId, int tileId, int toxinExpirationCycle, DeathReason reason)
        {
            OwnerPlayerId = ownerPlayerId;
            OriginalOwnerPlayerId = ownerPlayerId;
            TileId = tileId;
            IsAlive = false;
            IsToxin = true;
            CauseOfDeath = reason;
        }

        public void MarkAsToxin(int expirationCycle)
        {
            IsAlive = false;
            IsToxin = true;
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

            OwnerPlayerId = newOwnerPlayerId;
            IsAlive = true;
            GrowthCycleAge = 0;
            ToxinLevel = 0;
            CauseOfDeath = null;
            LastOwnerPlayerId = null;
            IsToxin = false;
            ReclaimCount++;
        }

        public void IncreaseToxin(int amount)
        {
            if (amount > 0)
                ToxinLevel += amount;
        }

        public void DecreaseToxin(int amount)
        {
            if (amount > 0)
                ToxinLevel = Math.Max(0, ToxinLevel - amount);
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
    }
}
