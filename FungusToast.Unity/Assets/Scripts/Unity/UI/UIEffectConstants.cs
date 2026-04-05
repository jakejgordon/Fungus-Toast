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
        public const float CellGrowthFadeInDurationSeconds = 0.5f;

        /// <summary>
        /// Starting alpha used when a newly grown cell first appears before fading to full opacity.
        /// </summary>
        public const float CellGrowthFadeInStartAlpha = 0f;

        /// <summary>
        /// Duration (in seconds) for a newly grown cell to settle back to its highlighted transparency after reaching full opacity.
        /// </summary>
        public const float CellGrowthSettleDurationSeconds = 0.3f;

        /// <summary>
        /// Duration (in seconds) for toxin drop animation.
        /// </summary>
        public const float ToxinDropAnimationDurationSeconds = 1.3f;

        /// <summary>
        /// Duration (in seconds) for the expired-toxin dissolve animation.
        /// </summary>
        public const float ToxinExpiryDissolveDurationSeconds = 0.5f;

        /// <summary>
        /// Final uniform scale reached by the transient expired-toxin dissolve visual.
        /// </summary>
        public const float ToxinExpiryDissolveFinalScale = 0.78f;

        /// <summary>
        /// Final overlay scale reached by the transient expired-toxin dissolve visual.
        /// </summary>
        public const float ToxinExpiryDissolveOverlayScale = 0.72f;

        /// <summary>
        /// World-space upward drift applied while an expired toxin dissolves.
        /// </summary>
        public const float ToxinExpiryDissolveLiftWorld = 0.08f;

        /// <summary>
        /// Maximum transient rotation used to break up the expired-toxin dissolve silhouette.
        /// </summary>
        public const float ToxinExpiryDissolveRotationDegrees = 5f;

        /// <summary>
        /// Flicker frequency used during the expired-toxin dissolve fade.
        /// </summary>
        public const float ToxinExpiryDissolveFlickerFrequency = 18f;
        public const float NutrientPatchPulseMinScale = 0.74f;
        public const float NutrientPatchPulseMaxScale = 1.08f;
        public const float NutrientPatchConsumptionDurationSeconds = 0.65f;
        public const float NutrientPatchToastStartHeightWorld = 0.42f;
        public const float NutrientPatchToastRiseWorld = 0.9f;
        public const float NutrientPatchToastDurationSeconds = 3.3f;
        public const float NutrientPatchToastFontSize = 7.4f;
        public const float BoardToastScaleMultiplier = 2f;
        public const float NutrientPatchToastZoomReferenceOrthographicSize = 18f;
        public const float NutrientPatchToastMaxScaleMultiplier = 4.1f;
        public const float NutrientPatchToastPopScaleMultiplier = 1.38f;
        public const float NutrientPatchToastMinScaleMultiplier = 0.82f;
        public const float NutrientPatchPullOffsetWorld = 0.22f;
        public const float NutrientPatchPulseSpeed = 2.15f;
        public const float NutrientPatchPulseAlphaMin = 0.78f;
        public const float NutrientPatchPulseAlphaMax = 0.98f;
        public const float NutrientPatchPulsePhaseOffsetRadians = 0.37f;

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
        public const float TimeAfterDecayRender = 0.7f;

        /// <summary>
        /// Hold duration passed to the phase banner so the total Conidial Relay overlay reads for roughly 1.5 seconds including fade in/out.
        /// </summary>
        public const float ConidialRelayBannerHoldSeconds = 0.5f;

        /// <summary>
        /// Hold duration for the Retrograde Bloom mutation exchange banner.
        /// </summary>
        public const float RetrogradeBloomBannerHoldSeconds = 0.75f;

        /// <summary>
        /// Hold duration for the Mycelial Crescendo free surge banner.
        /// </summary>
        public const float MycelialCrescendoBannerHoldSeconds = 0.75f;

        /// <summary>
        /// Hold duration for the Aegis Hyphae post-growth banner.
        /// </summary>
        public const float AegisHyphaeBannerHoldSeconds = 0.55f;

        /// <summary>
        /// Hold duration for the Saprophage Ring decay banner.
        /// </summary>
        public const float SaprophageRingBannerHoldSeconds = 0.65f;

        /// <summary>
        /// Hold duration for the Marginal Clamp growth-triggered banner.
        /// </summary>
        public const float MarginalClampBannerHoldSeconds = 0.55f;

        /// <summary>
        /// Hold duration for the Spore Salvo startup volley banner.
        /// </summary>
        public const float SporeSalvoBannerHoldSeconds = 0.7f;

        /// <summary>
        /// Hold duration for the Hyphal Bridge growth-end banner.
        /// </summary>
        public const float HyphalBridgeBannerHoldSeconds = 1.1f;

        /// <summary>
        /// Hold duration for the Conidia Ascent launch banner.
        /// </summary>
        public const float ConidiaAscentBannerHoldSeconds = 0.95f;

        /// <summary>
        /// Hold duration for the directed-vector surge banner.
        /// </summary>
        public const float DirectedVectorBannerHoldSeconds = 1.05f;

        /// <summary>
        /// Lifetime of the floating directed-vector toast.
        /// </summary>
        public const float DirectedVectorToastDurationSeconds = 2.025f;

        /// <summary>
        /// World-space height at which the directed-vector toast begins.
        /// </summary>
        public const float DirectedVectorToastStartHeightWorld = 0.9f;

        /// <summary>
        /// World-space vertical travel applied to the directed-vector toast.
        /// </summary>
        public const float DirectedVectorToastRiseWorld = 1.08f;

        /// <summary>
        /// Font size used for the floating directed-vector toast.
        /// </summary>
        public const float DirectedVectorToastFontSize = 9.6f;

        /// <summary>
        /// Duration of the origin emphasis pulse.
        /// </summary>
        public const float DirectedVectorOriginPulseDurationSeconds = 0.375f;

        /// <summary>
        /// Duration of each directed-vector chunk pulse.
        /// </summary>
        public const float DirectedVectorChunkPulseDurationSeconds = 0.45f;

        /// <summary>
        /// Delay between directed-vector chunk pulses.
        /// </summary>
        public const float DirectedVectorChunkStaggerSeconds = 0.16875f;

        /// <summary>
        /// Minimum number of chunk beats used for larger directed-vector paths.
        /// </summary>
        public const int DirectedVectorChunkCountMin = 3;

        /// <summary>
        /// Maximum number of chunk beats used for larger directed-vector paths.
        /// </summary>
        public const int DirectedVectorChunkCountMax = 6;

        /// <summary>
        /// Peak scale for the directed-vector overlay pulse.
        /// </summary>
        public const float DirectedVectorPulseScale = 1.33f;

        /// <summary>
        /// Total duration for the Conidial Relay presentation animation.
        /// </summary>
        public const float ConidialRelayTotalDurationSeconds = 1.5f;

        /// <summary>
        /// Delay before the abandoned 3x3 source patch is revealed as dead.
        /// </summary>
        public const float ConidiaAscentDeadZoneRevealDelaySeconds = 0.22f;

        /// <summary>
        /// Duration of the accelerating launch phase.
        /// </summary>
        public const float ConidiaAscentLaunchDurationSeconds = 0.85f;

        /// <summary>
        /// Duration of the returning descent arc.
        /// </summary>
        public const float ConidiaAscentReturnDurationSeconds = 0.95f;

        /// <summary>
        /// Maximum scale reached as the payload climbs toward the camera before exiting the screen.
        /// </summary>
        public const float ConidiaAscentAscentMaxScale = 1.72f;

        /// <summary>
        /// Additional world-space height above the board before the payload begins its return arc.
        /// </summary>
        public const float ConidiaAscentOffscreenHeightWorld = 2.8f;

        /// <summary>
        /// Extra world-space arc height applied while the payload descends back toward the board.
        /// </summary>
        public const float ConidiaAscentReturnArcHeightWorld = 1.9f;

        /// <summary>
        /// Lateral sway applied during ascent so the launch does not feel mechanically linear.
        /// </summary>
        public const float ConidiaAscentAscentSwayWorld = 0.18f;

        /// <summary>
        /// Maximum tilt applied to the ascending payload.
        /// </summary>
        public const float ConidiaAscentAscentTiltDegrees = 14f;

        /// <summary>
        /// Duration used to fade the landed 2x2 colony into view.
        /// </summary>
        public const float ConidiaAscentDestinationRevealDurationSeconds = 0.18f;

        /// <summary>
        /// Duration of the landing pop.
        /// </summary>
        public const float ConidiaAscentLandingPopDurationSeconds = 0.16f;

        /// <summary>
        /// Duration of the landing settle after the pop.
        /// </summary>
        public const float ConidiaAscentLandingSettleDurationSeconds = 0.14f;

        /// <summary>
        /// Peak scale used for the landing pop before the payload settles into the destination patch.
        /// </summary>
        public const float ConidiaAscentLandingPopScale = 1.08f;

        /// <summary>
        /// Total duration of the Mycotoxic Lash special-death emphasis step.
        /// </summary>
        public const float MycotoxicLashAnimationDurationSeconds = 1f;
        public const float NecrophyticBloomCompostAnimationDurationSeconds = 0.95f;
        public const float NecrophyticBloomCompostPullWorld = 0.18f;
        public const float NecrophyticBloomCompostStartScale = 1.16f;
        public const float NecrophyticBloomCompostEndScale = 0.9f;
        public const float NecrophyticBloomBannerHoldSeconds = 0.65f;

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
        /// Toxin overlay scale relative to the mold sprite for Spore Salvo launches.
        /// </summary>
        public const float SporeSalvoOverlayScale = 0.72f;

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
        /// Duration scale applied to each Hyphal Bridge hop so four launches fit cleanly into the growth-end presentation.
        /// </summary>
        public const float HyphalBridgeSegmentDurationScale = 0.5f;

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
        public const float ToxinDropApproachPortion = 0.78f;
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
        /// Multiplier used to slow Surgical Inoculation's shield drop animation.
        /// </summary>
        public const float SurgicalInoculationDropDurationScale = 1.2f;
        /// <summary>
        /// Multiplier used to slow the starting resistant spore shield drop animation.
        /// </summary>
        public const float StartingSporeArrivalDropDurationScale = 1.2f;
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

        // ==================== PLAYER HOVER EMPHASIS ====================
        /// <summary>
        /// Total duration of the one-shot colony emphasis pulse triggered by player-identity hovers.
        /// </summary>
        public const float PlayerHoverColonyPulseDurationSeconds = 0.34f;
        /// <summary>
        /// Maximum scale used for sparse player-colony hover pulses.
        /// </summary>
        public const float PlayerHoverColonyPulseSparseMaxScale = 1.8f;
        /// <summary>
        /// Maximum scale used for dense player-colony hover pulses.
        /// </summary>
        public const float PlayerHoverColonyPulseDenseMaxScale = 1.3f;
        /// <summary>
        /// Maximum halo scale used for sparse player-colony hover pulses.
        /// </summary>
        public const float PlayerHoverColonyHaloSparseMaxScale = 1.95f;
        /// <summary>
        /// Maximum halo alpha used for sparse player-colony hover pulses.
        /// </summary>
        public const float PlayerHoverColonyHaloSparseMaxAlpha = 0.92f;
        /// <summary>
        /// Maximum halo scale used for dense player-colony hover pulses.
        /// </summary>
        public const float PlayerHoverColonyHaloDenseMaxScale = 1.35f;
        /// <summary>
        /// Maximum halo alpha used for dense player-colony hover pulses.
        /// </summary>
        public const float PlayerHoverColonyHaloDenseMaxAlpha = 0.58f;
        /// <summary>
        /// Colony sizes at or below this threshold receive the strongest hover emphasis.
        /// </summary>
        public const int PlayerHoverSparseColonyThreshold = 6;
        /// <summary>
        /// Colony sizes at or above this threshold receive the gentlest hover emphasis.
        /// </summary>
        public const int PlayerHoverDenseColonyThreshold = 36;

        /// <summary>
        /// High-contrast halo color for player-colony hover emphasis.
        /// </summary>
        public static readonly Color PlayerHoverColonyHaloColor = new Color(1f, 0.98f, 0.78f, 1f);

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

        // ==================== CHEMOBEACON ====================
        /// <summary>
        /// Baseline scale applied to active Chemobeacon markers.
        /// </summary>
        public const float ChemobeaconIdleScale = 1.0f;
        /// <summary>
        /// Minimum active Chemobeacon scale, corresponding to 75% tile area.
        /// </summary>
        public const float ChemobeaconPulseMinScale = 0.8660254f;
        /// <summary>
        /// Maximum active Chemobeacon scale, corresponding to 200% tile area.
        /// </summary>
        public const float ChemobeaconPulseMaxScale = 1.4142135f;
        /// <summary>
        /// Duration of a full Chemobeacon pulse cycle.
        /// </summary>
        public const float ChemobeaconPulseDurationSeconds = 1.15f;
            /// <summary>
            /// Minimum alpha for the pulsing Chemobeacon owner icon.
            /// </summary>
            public const float ChemobeaconPulseMinAlpha = 0f;
            /// <summary>
            /// Maximum alpha for the pulsing Chemobeacon owner icon.
            /// </summary>
            public const float ChemobeaconPulseMaxAlpha = 1f;
        /// <summary>
        /// Total duration for the Chemobeacon evaporation animation.
        /// </summary>
        public const float ChemobeaconEvaporationDurationSeconds = 0.7f;
        /// <summary>
        /// Final scale reached before the Chemobeacon fully evaporates.
        /// </summary>
        public const float ChemobeaconEvaporationFinalScale = 2.7f;
        /// <summary>
        /// Vertical lift applied during Chemobeacon evaporation.
        /// </summary>
        public const float ChemobeaconEvaporationLiftWorld = 0.18f;

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

        // ==================== JETTING MYCELIUM HOVER PREVIEW ====================
        /// <summary>
        /// Pulse cycle duration (seconds) for the Jetting Mycelium placement preview overlay.
        /// </summary>
        public const float JettingMyceliumPreviewPulseDurationSeconds = 0.8f;

        /// <summary>
        /// Dim color for the living-cell projection preview (cyan/teal, low alpha).
        /// </summary>
        public static readonly Color JettingMyceliumPreviewLivingDimColor    = new Color(0.3f, 1f,   0.5f, 0.2f);

        /// <summary>
        /// Bright color for the living-cell projection preview (cyan/teal, high alpha).
        /// </summary>
        public static readonly Color JettingMyceliumPreviewLivingBrightColor = new Color(0.5f, 1f,   0.7f, 0.8f);

        /// <summary>
        /// Dim color for the toxin-cone preview (orange/amber, low alpha).
        /// </summary>
        public static readonly Color JettingMyceliumPreviewToxinDimColor     = new Color(1f,   0.5f, 0f,   0.2f);

        /// <summary>
        /// Bright color for the toxin-cone preview (orange/amber, high alpha).
        /// </summary>
        public static readonly Color JettingMyceliumPreviewToxinBrightColor  = new Color(1f,   0.7f, 0.1f, 0.8f);
    }
}
