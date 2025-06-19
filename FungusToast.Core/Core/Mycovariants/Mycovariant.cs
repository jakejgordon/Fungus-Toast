using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System;

namespace FungusToast.Core.Mycovariants
{
    public class Mycovariant
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string FlavorText { get; set; } = "";
        public MycovariantType Type { get; set; }

        /// <summary>
        /// Called immediately upon selection. Use for instant effects.
        /// </summary>
        public Action<PlayerMycovariant, GameBoard, Random>? ApplyEffect { get; set; }

        /// <summary>
        /// Optional trigger condition for delayed-effect Mycovariants.
        /// </summary>
        public Func<PlayerMycovariant, GameBoard, bool>? IsTriggerConditionMet { get; set; }
    }
}
