using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Players
{
    /// <summary>
    /// Represents a Mycovariant acquired by a player, including any trigger state.
    /// </summary>
    public class PlayerMycovariant
    {
        public int PlayerId { get; }
        public int MycovariantId { get; }
        public Mycovariant Mycovariant { get; }

        /// <summary>
        /// Whether this Mycovariant's effect has already been triggered.
        /// </summary>
        public bool HasTriggered { get; private set; } = false;

        public PlayerMycovariant(int playerId, int mycovariantId, Mycovariant mycovariant)
        {
            PlayerId = playerId;
            MycovariantId = mycovariantId;
            Mycovariant = mycovariant;
        }

        public void MarkTriggered()
        {
            HasTriggered = true;
        }
    }
}
