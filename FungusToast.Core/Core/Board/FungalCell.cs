using FungusToast.Core.Death;
using FungusToast.Core.Players;

namespace FungusToast.Core.Board
{
    public class FungalCell
    {
        public int OwnerPlayerId { get; private set; }
        public bool IsAlive { get; private set; }
        public int ToxinLevel { get; private set; }
        public int GrowthCycleAge { get; private set; }

        public DeathReason? CauseOfDeath { get; set; }

        public int TileId { get; private set; }  // <-- Added for positional tracking

        public FungalCell(int ownerPlayerId, int tileId)
        {
            OwnerPlayerId = ownerPlayerId;
            TileId = tileId;
            IsAlive = true;
            ToxinLevel = 0;
        }

        public void Kill()
        {
            IsAlive = false;
        }

        public void IncreaseToxin(int amount)
        {
            ToxinLevel += amount;
        }

        public void DecreaseToxin(int amount)
        {
            ToxinLevel = System.Math.Max(0, ToxinLevel - amount);
        }

        public void IncrementGrowthAge()
        {
            GrowthCycleAge++;
        }

        public void ResetGrowthCycleAge()
        {
            GrowthCycleAge = 0;
        }

    }
}
