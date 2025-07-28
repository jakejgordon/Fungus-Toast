using FungusToast.Core.Death;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Result of a death chance calculation, providing structured information about potential cell death.
    /// </summary>
    public class DeathCalculationResult
    {
        /// <summary>
        /// The calculated death chance (0.0 to 1.0).
        /// </summary>
        public float Chance { get; }

        /// <summary>
        /// The reason for death, if death should occur. Null if no death should happen.
        /// </summary>
        public DeathReason? Reason { get; }

        /// <summary>
        /// The player responsible for killing this cell, if applicable.
        /// </summary>
        public int? KillerPlayerId { get; }

        /// <summary>
        /// The tile ID of the attacking cell, if applicable. Used for directional effects like Putrefactive Cascade.
        /// </summary>
        public int? AttackerTileId { get; }

        /// <summary>
        /// True if death should occur based on the calculation and random roll.
        /// </summary>
        public bool ShouldDie => Reason.HasValue;

        public DeathCalculationResult(float chance, DeathReason? reason, int? killerPlayerId = null, int? attackerTileId = null)
        {
            Chance = chance;
            Reason = reason;
            KillerPlayerId = killerPlayerId;
            AttackerTileId = attackerTileId;
        }

        /// <summary>
        /// Creates a result indicating no death should occur.
        /// </summary>
        public static DeathCalculationResult NoDeath(float calculatedChance) => 
            new DeathCalculationResult(calculatedChance, null);

        /// <summary>
        /// Creates a result indicating death should occur for the specified reason.
        /// </summary>
        public static DeathCalculationResult Death(float chance, DeathReason reason, int? killerPlayerId = null, int? attackerTileId = null) =>
            new DeathCalculationResult(chance, reason, killerPlayerId, attackerTileId);
    }
}