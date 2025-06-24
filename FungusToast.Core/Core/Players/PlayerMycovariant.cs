using FungusToast.Core.Mycovariants;
using System.Collections.Generic;

namespace FungusToast.Core.Players
{
    /// <summary>
    /// Represents a Mycovariant acquired by a player, including any trigger state and effect usage tracking.
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

        /// <summary>
        /// Tracks effect counts by effect type.
        /// </summary>
        public Dictionary<MycovariantEffectType, int> EffectCounts { get; private set; } = new();

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

        /// <summary>
        /// Increments the effect count for a given effect type.
        /// </summary>
        public void IncrementEffectCount(MycovariantEffectType effectType, int count)
        {
            if (!EffectCounts.ContainsKey(effectType))
                EffectCounts[effectType] = 0;
            EffectCounts[effectType] += count;
        }
    }
}
