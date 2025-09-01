using System;
using UnityEngine;

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

        /// <summary>
        /// Short stagger used when AI selects an active mycovariant: applied before effect animations start
        /// and again after they complete, before reopening the draft window.
        /// </summary>
        public const float AIActiveMycovariantStaggerSeconds = 0.25f;

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

        // ==================== NEW GROWTH VISUAL TWEAKS ====================

        /// <summary>
        /// Duration (in seconds) of the bright green flash when a newly-grown cell reaches full opacity.
        /// </summary>
        public const float NewGrowthFlashDurationSeconds = 0.1f;

        /// <summary>
        /// The color of the flash shown when a newly-grown cell reaches full opacity.
        /// </summary>
        public static readonly Color NewGrowthFlashColor = new Color(0.2f, 1f, 0.2f, 1f);

        /// <summary>
        /// The persistent alpha for cells that grew this round (until the next round begins).
        /// </summary>
        public const float NewGrowthFinalAlpha = 0.9f;

        // ==================== TOOLTIP HIGHLIGHTING ====================
        /// <summary>
        /// Growth cycle age threshold (exclusive) below which living cells' age is highlighted in tooltips.
        /// </summary>
        public const int GrowthCycleAgeHighlightTextThreshold = 5;

        // ==================== TOXIN DROP (DROP-FROM-ABOVE) ====================
        /// <summary>
        /// Starting local Y offset applied to the toxin overlay tile during the drop.
        /// </summary>
        public const float ToxinDropStartYOffset = 1.8f; // was 0.6f; increased 3x for visibility on large grids
        /// <summary>
        /// Portion of the total drop duration spent on the approach (falling) phase [0..1].
        /// </summary>
        public const float ToxinDropApproachPortion = 0.65f;
        /// <summary>
        /// Impact squash scale on X (wider on impact).
        /// </summary>
        public const float ToxinDropImpactSquashX = 1.12f;
        /// <summary>
        /// Impact squash scale on Y (flatter on impact).
        /// </summary>
        public const float ToxinDropImpactSquashY = 0.88f;

        // ==================== HUMAN POST-EFFECT DELAYS ====================
        /// <summary>
        /// Optional delay after human-triggered Jetting Mycelium resolves to allow custom animations.
        /// </summary>
        public const float JettingMyceliumHumanPostEffectDelaySeconds = 0.8f;
        /// <summary>
        /// Optional delay after human-triggered Mycelial Bastion resolves to allow custom animations.
        /// </summary>
        public const float MycelialBastionHumanPostEffectDelaySeconds = 0.65f;
        /// <summary>
        /// Optional delay after human-triggered Surgical Inoculation resolves to allow custom animations.
        /// </summary>
        public const float SurgicalInoculationHumanPostEffectDelaySeconds = 0.6f;
        /// <summary>
        /// Optional delay after human-triggered Ballistospore Discharge resolves to allow custom animations.
        /// </summary>
        public const float BallistosporeDischargeHumanPostEffectDelaySeconds = 0.7f;
        /// <summary>
        /// Optional delay after human-triggered Cytolytic Burst resolves to allow custom animations.
        /// </summary>
        public const float CytolyticBurstHumanPostEffectDelaySeconds = 0.7f;
    }
}
