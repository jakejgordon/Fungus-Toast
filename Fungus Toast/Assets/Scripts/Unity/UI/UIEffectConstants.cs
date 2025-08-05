using System;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Central location for UI effect timing/delay constants used in mycovariant effect coroutines and game phase animations.
    /// </summary>
    public static class UIEffectConstants
    {
        /// <summary>
        /// Delay (in seconds) after AI resolves Jetting Mycelium before continuing.
        /// </summary>
        public const float JettingMyceliumAIDelaySeconds = 0.6f;

        /// <summary>
        /// Delay (in seconds) after AI resolves Mycelial Bastion before continuing.
        /// </summary>
        public const float MycelialBastionAIDelaySeconds = 0.6f;

        /// <summary>
        /// Default delay (in seconds) for AI "thinking" after resolving any mycovariant effect.
        /// </summary>
        public const float DefaultAIThinkingDelay = 0.6f;

        /// <summary>
        /// Duration (in seconds) for cell death crossfade animation.
        /// </summary>
        public const float CellDeathAnimationDurationSeconds = 0.9f;

        /// <summary>
        /// Duration (in seconds) for cell growth fade-in animation.
        /// </summary>
        public const float CellGrowthFadeInDurationSeconds = 0.3f;

        /// <summary>
        /// Duration (in seconds) for toxin drop animation.
        /// </summary>
        public const float ToxinDropAnimationDurationSeconds = 0.8f;

        /// <summary>
        /// Delay (in seconds) for AI to "think" before picking a mycovariant during draft.
        /// </summary>
        public const float AIDraftPickDelaySeconds = 0.7f;

        // ==================== GAME PHASE TIMING CONSTANTS ====================
        
        /// <summary>
        /// Delay (in seconds) before rendering the board after decay phase processing.
        /// </summary>
        public const float TimeBeforeDecayRender = 0.5f;

        /// <summary>
        /// Delay (in seconds) after rendering the board during decay phase before proceeding.
        /// </summary>
        public const float TimeAfterDecayRender = 0.5f;

        /// <summary>
        /// Duration (in seconds) between growth cycles during the growth phase.
        /// </summary>
        public const float TimeBetweenGrowthCycles = 1f;
    }
}