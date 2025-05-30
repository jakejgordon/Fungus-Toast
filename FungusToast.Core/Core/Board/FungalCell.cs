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

        // 🆕 Toxin state
        public bool IsToxin { get; internal set; } = false;
        public int ToxinExpirationCycle { get; internal set; } = -1;

        public FungalCell() { }

        public FungalCell(int ownerPlayerId, int tileId)
        {
            OwnerPlayerId = ownerPlayerId;
            OriginalOwnerPlayerId = ownerPlayerId;
            TileId = tileId;
            IsAlive = true;
            ToxinLevel = 0;
        }

        // Alternate constructor for toxin tile
        public FungalCell(int ownerPlayerId, int tileId, int toxinExpirationCycle,
                  DeathReason reason)
        {
            OwnerPlayerId = ownerPlayerId;
            OriginalOwnerPlayerId = ownerPlayerId;
            TileId = tileId;
            IsAlive = false;
            IsToxin = true;
            ToxinExpirationCycle = toxinExpirationCycle;
            CauseOfDeath = reason;
        }

        /// <summary>
        /// Marks this cell as a toxin tile without implying it died.
        /// Used for cases like failed spore drops where no cell existed.
        /// </summary>
        public void MarkAsToxin(int expirationCycle)
        {
            IsAlive = false;
            IsToxin = true;
            ToxinExpirationCycle = expirationCycle;
            CauseOfDeath = null;
            LastOwnerPlayerId = null;
        }

        public static void ConvertToToxin(
           GameBoard board,
           int tileId,
           int expirationCycle,
           DeathReason? reason = null)
        {
            var cell = board.GetCell(tileId);

            if (cell != null)
            {
                if (cell.IsAlive)
                {
                    // Kill the cell and then mark as toxin
                    cell.Kill(reason ?? DeathReason.Unknown);
                    board.RemoveControlFromPlayer(tileId);
                }

                cell.ConvertToToxin(
                    expirationCycle: expirationCycle,
                    lastOwnerPlayerId: cell.LastOwnerPlayerId,
                    reason: cell.CauseOfDeath);
            }
            else
            {
                // No fungal cell existed — create a new toxin with no death reason
                var toxin = new FungalCell(-1, tileId);
                toxin.ConvertToToxin(expirationCycle);
                board.PlaceCell(tileId, toxin);
            }

            // Always place visual toxin overlay
            var overlayTile = board.GetTileById(tileId);
            overlayTile?.PlaceToxin(cell?.OwnerPlayerId ?? -1, expirationCycle);
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
            ToxinExpirationCycle = -1;
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
