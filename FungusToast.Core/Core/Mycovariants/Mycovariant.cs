using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public class Mycovariant : MycovariantBase
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

        /// <summary>
        /// Called when the Mycovariant is equipped. Should subscribe to needed events.
        /// </summary>
        public Action<PlayerMycovariant, GameBoard>? RegisterEventHandlers { get; set; }

        /// <summary>
        /// Called when the Mycovariant is unequipped or game ends. Should unsubscribe handlers.
        /// </summary>
        public Action<PlayerMycovariant, GameBoard>? UnregisterEventHandlers { get; set; }

        /// <summary>
        /// Optional AI score for drafting: returns a score (1-10) for how good a fit this mycovariant is for the AI's current situation.
        /// </summary>
        public Func<Player, GameBoard, float>? AIScore { get; set; }
    }
}
