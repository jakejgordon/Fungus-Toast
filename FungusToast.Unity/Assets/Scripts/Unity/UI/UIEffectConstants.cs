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
        public const float ToxinDropAnimationDurationSeconds = 0.9f; // was 0.8f, +0.1s for more hang time

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
        /// Hold duration passed to the phase banner so the total Conidial Relay overlay reads for roughly 1.5 seconds including fade in/out.
        /// </summary>
        public const float ConidialRelayBannerHoldSeconds = 0.5f;

        /// <summary>
        /// Hold duration for the Retrograde Bloom mutation exchange banner.
        /// </summary>
        public const float RetrogradeBloomBannerHoldSeconds = 0.75f;

        /// <summary>
        /// Hold duration for the Aegis Hyphae post-growth banner.
        /// </summary>
        public const float AegisHyphaeBannerHoldSeconds = 0.55f;

        /// <summary>
        /// Hold duration for the Saprophage Ring decay banner.
        /// </summary>
        public const float SaprophageRingBannerHoldSeconds = 0.65f;

        /// <summary>
        /// Total duration for the Conidial Relay presentation animation.
        /// </summary>
        public const float ConidialRelayTotalDurationSeconds = 1.5f;

        /// <summary>
        /// Total duration of the Mycotoxic Lash special-death emphasis step.
        /// </summary>
        public const float MycotoxicLashAnimationDurationSeconds = 1f;

        /// <summary>
        /// Total duration of the Retrograde Bloom board pulse.
        /// </summary>
        public const float RetrogradeBloomAnimationDurationSeconds = 1f;

        /// <summary>
        /// Total duration of the Saprophage Ring consumed-cell emphasis.
        /// </summary>
        public const float SaprophageRingAnimationDurationSeconds = 0.95f;

        /// <summary>
        /// Portion of the Mycotoxic Lash emphasis step spent fading the killed cells to black.
        /// </summary>
        public const float MycotoxicLashFadeToBlackPortion = 0.2f;

        /// <summary>
        /// Duration of the source emphasis before the relay launches.
        /// </summary>
        public const float ConidialRelaySourceEmphasisDurationSeconds = 0.15f;

        /// <summary>
        /// Duration of the Conidial Relay airborne arc.
        /// </summary>
        public const float ConidialRelayArcDurationSeconds = 0.9f;

        /// <summary>
        /// Duration of the landing squash and settle.
        /// </summary>
        public const float ConidialRelayLandingDurationSeconds = 0.45f;

        /// <summary>
        /// Starting scale for the source emphasis pose.
        /// </summary>
        public const float ConidialRelaySourceStartScale = 0.9f;

        /// <summary>
        /// Scale reached at the end of the source emphasis lift-off.
        /// </summary>
        public const float ConidialRelayLiftScale = 1.15f;

        /// <summary>
        /// Peak scale reached near the top of the relay arc.
        /// </summary>
        public const float ConidialRelayPeakScale = 1.8f;

        /// <summary>
        /// Scale used at the end of the descent, just before impact.
        /// </summary>
        public const float ConidialRelayDescentScale = 0.7f;

        /// <summary>
        /// Vertical lift applied during source emphasis.
        /// </summary>
        public const float ConidialRelayLiftYOffset = 0.3f;

        /// <summary>
        /// Base world-space arc height for Conidial Relay.
        /// </summary>
        public const float ConidialRelayArcBaseHeightWorld = 1.9f;

        /// <summary>
        /// Additional arc height per tile of travel distance.
        /// </summary>
        public const float ConidialRelayArcHeightPerTile = 0.11f;

        /// <summary>
        /// Rotation at the end of the source emphasis beat.
        /// </summary>
        public const float ConidialRelaySourceSpinDegrees = 24f;

        /// <summary>
        /// Total rotation during the airborne arc.
        /// </summary>
        public const float ConidialRelayArcSpinDegrees = 540f;

        /// <summary>
        /// Shield scale relative to the mold sprite for Conidial Relay.
        /// </summary>
        public const float ConidialRelayShieldScale = 0.52f;

        /// <summary>
        /// X scale used during the landing impact stretch.
        /// </summary>
        public const float ConidialRelayLandingStretchX = 1.25f;

        /// <summary>
        /// Y scale used during the landing impact squash.
        /// </summary>
        public const float ConidialRelayLandingStretchY = 0.78f;

        /// <summary>
        /// Portion of landing time used by the initial impact squash.
        /// </summary>
        public const float ConidialRelayLandingImpactPortion = 0.42f;

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
        public const float NewGrowthFinalAlpha = 0.78f;

        // ==================== TOOLTIP HIGHLIGHTING ====================
        /// <summary>
        /// Growth cycle age threshold (exclusive) below which living cells' age is highlighted in tooltips.
        /// </summary>
        public const int GrowthCycleAgeHighlightTextThreshold = 5;

        // ==================== TOXIN DROP (DROP-FROM-ABOVE) ====================
        /// <summary>
        /// Starting local Y offset applied to the toxin overlay tile during the drop.
        /// </summary>
        public const float ToxinDropStartYOffset = 2.7f; // was 1.8f; ~50% higher for more noticeable drop
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

        // ==================== SURGICAL INOCULATION (RESISTANT DROP) ====================
        /// <summary>
        /// Duration of the large shield drop animation for Surgical Inoculation.
        /// </summary>
        public const float SurgicalInoculationDropDurationSeconds = 1.0f; // was 0.8f
        /// <summary>
        /// Starting local Y offset used for the shield drop.
        /// </summary>
        public const float SurgicalInoculationDropStartYOffset = 3.0f;
        /// <summary>
        /// Increased to make the starting shield extremely large on big boards
        /// </summary>
        public const float SurgicalInoculationDropStartScale = 100.0f;
        /// <summary>
        /// Number of spins (turns) the shield makes during the drop phase.
        /// </summary>
        public const float SurgicalInoculationDropSpinTurns = 1.25f;
        /// <summary>
        /// Impact squash scale on X (wider on impact).
        /// </summary>
        public const float SurgicalInoculationImpactSquashX = 1.15f;
        /// <summary>
        /// Impact squash scale on Y (flatter on impact).
        /// </summary>
        public const float SurgicalInoculationImpactSquashY = 0.85f;
        /// <summary>
        /// Portion of total duration used by the drop phase [0..1].
        /// </summary>
        public const float SurgicalInoculationDropPortion = 0.45f;
        /// <summary>
        /// Portion of total duration used by the impact squash phase [0..1].
        /// </summary>
        public const float SurgicalInoculationImpactPortion = 0.20f;
        /// <summary>
        /// Portion of total duration used by the settle phase [0..1].
        /// </summary>
        public const float SurgicalInoculationSettlePortion = 0.15f;
        /// <summary>
        /// Duration for a quick ring pulse on impact (optional ripple effect).
        /// </summary>
        public const float SurgicalInoculationRingPulseDurationSeconds = 0.18f;

        // ==================== MYCELIAL BASTION PULSE ====================
        /// <summary>
        /// Duration (in seconds) for mycelial bastion pulse animation.
        /// </summary>
        public const float MycelialBastionPulseDurationSeconds = 0.9f; // was 0.6f
        /// <summary>
        /// Maximum scale for mycelial bastion pulse.
        /// </summary>
        public const float MycelialBastionPulseMaxScale = 13.0f; // was 1.6f
        /// <summary>
        /// Portion of mycelial bastion pulse duration for the outward scaling animation.
        /// </summary>
        public const float MycelialBastionPulseOutPortion = 0.45f;
        /// <summary>
        /// Portion of mycelial bastion pulse duration for the inward scaling animation.
        /// </summary>
        public const float MycelialBastionPulseInPortion = 0.55f;
        /// <summary>
        /// Vertical pop height for Bastion pulse.
        /// </summary>
        public const float MycelialBastionPulseYOffset = 2.4f; // pronounced pop-up height

        // ==================== SURGICAL INOCULATION ARC (PROJECTILE) ====================
        /// <summary>
        /// Duration of the arc animation for Surgical Inoculation.
        /// </summary>
        public const float SurgicalInoculationArcDurationSeconds = 0.9f; // sync with Bastion feel
        /// <summary>
        /// Base height of the arc in world units.
        /// </summary>
        public const float SurgicalInoculationArcBaseHeightWorld = 0.8f; // world units of extra height
        /// <summary>
        /// Additional height per tile of distance for the arc.
        /// </summary>
        public const float SurgicalInoculationArcHeightPerTile = 0.10f; // addl height per tile of distance
        /// <summary>
        /// Scale boost per tile of height for the arc.
        /// </summary>
        public const float SurgicalInoculationArcScalePerHeightTile = 0.08f; // scale boost per tile of height
        /// <summary>
        /// Peak visual scale at the apex of the arc (~10x requested).
        /// </summary>
        public const float SurgicalInoculationArcPeakScale = 10.0f;

        // ==================== REGENERATIVE HYPHAE ====================
        /// <summary>
        /// Duration of the rise animation for Regenerative Hyphae. (Doubled from 0.22f)
        /// </summary>
        public const float RegenerativeHyphaeRiseDurationSeconds = 0.44f; // was 0.22f
        /// <summary>
        /// Duration of the fade swap animation for Regenerative Hyphae. (Doubled from 0.20f)
        /// </summary>
        public const float RegenerativeHyphaeFadeSwapDurationSeconds = 0.40f; // was 0.20f
        /// <summary>
        /// Duration of the settle animation for Regenerative Hyphae. (Doubled from 0.23f)
        /// </summary>
        public const float RegenerativeHyphaeSettleDurationSeconds = 0.46f; // was 0.23f
        /// <summary>
        /// Maximum scale for Regenerative Hyphae.
        /// </summary>
        public const float RegenerativeHyphaeMaxScale = 1.18f;
        /// <summary>
        /// Overshoot scale for Regenerative Hyphae.
        /// </summary>
        public const float RegenerativeHyphaeOvershootScale = 1.05f;
        /// <summary>
        /// Lift offset for Regenerative Hyphae.
        /// </summary>
        public const float RegenerativeHyphaeLiftOffset = 0.25f;
        /// <summary>
        /// Simplified threshold for Regenerative Hyphae.
        /// </summary>
        public const int RegenerativeHyphaeSimplifiedThreshold = 40;
        /// <summary>
        /// Minimum scale multiplier for Regenerative Hyphae.
        /// </summary>
        public const float RegenerativeHyphaeMinScaleMultiplier = 0.55f;
        /// <summary>
        /// Maximum load for scale dampening in Regenerative Hyphae.
        /// </summary>
        public const int RegenerativeHyphaeMaxLoadForScaleDampen = 160;
        /// <summary>
        /// Base hold phase (peak pause) duration used for proportional scaling of full Regenerative Hyphae animation.
        /// </summary>
        public const float RegenerativeHyphaeHoldBaseSeconds = 0.15f;
        /// <summary>
        /// Total base duration (sum of rise + hold + swap + settle) for full Regenerative Hyphae reclaim animation.
        /// </summary>
        public const float RegenerativeHyphaeTotalBaseDurationSeconds = RegenerativeHyphaeRiseDurationSeconds + RegenerativeHyphaeFadeSwapDurationSeconds + RegenerativeHyphaeSettleDurationSeconds + RegenerativeHyphaeHoldBaseSeconds;
        /// <summary>
        /// Portion of total base duration used by the rise phase.
        /// </summary>
        public const float RegenerativeHyphaeRisePortion = RegenerativeHyphaeRiseDurationSeconds / RegenerativeHyphaeTotalBaseDurationSeconds;
        /// <summary>
        /// Portion of total base duration used by the hold (peak) phase.
        /// </summary>
        public const float RegenerativeHyphaeHoldPortion = RegenerativeHyphaeHoldBaseSeconds / RegenerativeHyphaeTotalBaseDurationSeconds;
        /// <summary>
        /// Portion of total base duration used by the fade/swap phase.
        /// </summary>
        public const float RegenerativeHyphaeSwapPortion = RegenerativeHyphaeFadeSwapDurationSeconds / RegenerativeHyphaeTotalBaseDurationSeconds;
        /// <summary>
        /// Portion of total base duration used by the settle phase.
        /// </summary>
        public const float RegenerativeHyphaeSettlePortion = RegenerativeHyphaeSettleDurationSeconds / RegenerativeHyphaeTotalBaseDurationSeconds;
        /// <summary>
        /// Total target duration (in seconds) for a Regenerative Hyphae reclaim animation batch when using the simplified (lite) animation.
        /// Passed directly to PlayRegenerativeHyphaeReclaimBatch when explicit timing is desired.
        /// </summary>
        public const float RegenerativeHyphaeReclaimTotalDurationSeconds = 2.0f;

        // ==================== DRAFT CAMERA RECENTER ====================
        /// <summary>
        /// Duration (in seconds) for smooth camera restore to initial framing at draft start.
        /// </summary>
        public const float DraftCameraRecenteringDurationSeconds = 0.5f;
    }
}
