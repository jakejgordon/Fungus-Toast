using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
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
        /// If true, this mycovariant is always available in the draft (not removed when selected).
        /// </summary>
        public bool IsUniversal { get; set; } = false;

        /// <summary>
        /// Called immediately upon selection. Use for instant effects.
        /// </summary>
        public Action<PlayerMycovariant, GameBoard, Random, ISimulationObserver?>? ApplyEffect { get; set; }

        /// <summary>
        /// Optional trigger condition for delayed-effect Mycovariants.
        /// </summary>
        public Func<PlayerMycovariant, GameBoard, bool>? IsTriggerConditionMet { get; set; }
    }
}
