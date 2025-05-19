using FungusToast.Core.Death;
using System;

namespace FungusToast.Core.Board
{
    public class FungalCell
    {
        public int OriginalOwnerPlayerId { get; private set; }
        public int OwnerPlayerId { get; private set; }
        public int TileId { get; private set; }

        public bool IsAlive { get; private set; } = true;
        public int ToxinLevel { get; private set; } = 0;
        public int GrowthCycleAge { get; private set; } = 0;

        public DeathReason? CauseOfDeath { get; set; }
        public int ReclaimCount { get; private set; } = 0;

        public FungalCell(int ownerPlayerId, int tileId)
        {
            OwnerPlayerId = ownerPlayerId;
            OriginalOwnerPlayerId = ownerPlayerId; // Initially same
            TileId = tileId;
            IsAlive = true;
            ToxinLevel = 0;
        }

        public void Kill(DeathReason reason = DeathReason.Unknown)
        {
            if (!IsAlive)
                return;

            IsAlive = false;
            CauseOfDeath = reason;
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
