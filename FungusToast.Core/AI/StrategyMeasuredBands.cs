using System;
using System.Collections.Generic;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// The most recent classified measurement of a strategy, recorded so its band carries
    /// provenance instead of being asserted. Bands are relative to the panel they were measured
    /// in (<see cref="MatrixId"/>), and a later behavior change can make a record stale.
    /// </summary>
    public sealed class StrategyMeasuredBand
    {
        public StrategyMeasuredBand(
            DifficultyBand? band,
            BandEvidence evidence,
            double? pooledNormalizedBoardShare,
            int? games,
            string matrixId,
            string evidenceDate,
            string source,
            string classifierVersion = StrategyMeasuredBands.ClassifierV1)
        {
            Band = band;
            Evidence = evidence;
            PooledNormalizedBoardShare = pooledNormalizedBoardShare;
            Games = games;
            MatrixId = matrixId ?? throw new ArgumentNullException(nameof(matrixId));
            EvidenceDate = evidenceDate ?? throw new ArgumentNullException(nameof(evidenceDate));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ClassifierVersion = classifierVersion ?? throw new ArgumentNullException(nameof(classifierVersion));
        }

        /// <summary>Null when the evidence could not place the strategy in one band.</summary>
        public DifficultyBand? Band { get; }
        public BandEvidence Evidence { get; }
        public double? PooledNormalizedBoardShare { get; }
        public int? Games { get; }
        public string MatrixId { get; }

        /// <summary>ISO date (yyyy-MM-dd) the measurement was frozen.</summary>
        public string EvidenceDate { get; }

        /// <summary>The evidence document that records the measurement.</summary>
        public string Source { get; }
        public string ClassifierVersion { get; }
    }

    /// <summary>
    /// Measured bands keyed by stable strategy ID, so renames do not orphan evidence. Update a
    /// record from the band report when a new calibration matrix is frozen; never hand-edit a
    /// band to match an authored label.
    /// </summary>
    public static class StrategyMeasuredBands
    {
        public const string ClassifierV1 = "fungus-toast.ai-bands.v1";

        private const string P7Date = "2026-09-07";
        private const string ReferencePanel = "p7-solo-panel-v1";
        private const string ReferencePanelDoc = "FungusToast.Core/docs/second-level/AI_P7_REFERENCE_BANDS_V1.md";
        private const string CampaignPanel = "p7-campaign-panel-v1";
        private const string CampaignPanelDoc = "FungusToast.Core/docs/second-level/AI_P7_CAMPAIGN_BANDS_V1.md";

        private static readonly IReadOnlyDictionary<string, StrategyMeasuredBand> BandsByStrategyId =
            new Dictionary<string, StrategyMeasuredBand>(StringComparer.Ordinal)
            {
                // P7 Proven panel. Comments give the name at measurement time.
                ["legacy.proven.tst-campaignmirror-ai13-anabolicfirst.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.915, 186, ReferencePanel, P7Date, ReferencePanelDoc), // TST_CampaignMirror_AI13_AnabolicFirst
                ["legacy.proven.tst-balancedcontrol-anabolicfirst.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.866, 182, ReferencePanel, P7Date, ReferencePanelDoc), // TST_BalancedControl_AnabolicFirst
                ["legacy.proven.anabolic-grow-catabr-putreregen.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.542, 189, ReferencePanel, P7Date, ReferencePanelDoc), // Anabolic>Grow>CatabR>PutreRegen
                ["legacy.proven.grow-kill-reclaim-econ-reclaim.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.373, 181, ReferencePanel, P7Date, ReferencePanelDoc), // Grow>Kill>Reclaim(Econ/Reclaim)
                ["legacy.proven.grow-kill-reclaim-econ.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.372, 188, ReferencePanel, P7Date, ReferencePanelDoc), // Grow>Kill>Reclaim(Econ)
                ["legacy.proven.tst-balancedcontrol-maxeconomy.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.347, 177, ReferencePanel, P7Date, ReferencePanelDoc), // TST_BalancedControl_MaxEconomy
                ["legacy.proven.tst-campaignmirror-ai13-balancedcontrol-maxeconomy.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.271, 180, ReferencePanel, P7Date, ReferencePanelDoc), // TST_CampaignMirror_AI13_BalancedControl_MaxEconomy
                ["legacy.proven.grow-defend-kill.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.088, 173, ReferencePanel, P7Date, ReferencePanelDoc), // Grow>Defend>Kill
                ["legacy.proven.filament-regrowth.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.058, 187, ReferencePanel, P7Date, ReferencePanelDoc), // Filament Regrowth
                ["legacy.proven.mutate-grow-kill-max-econ.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 0.975, 180, ReferencePanel, P7Date, ReferencePanelDoc), // Mutate>Grow>Kill(Max Econ)
                ["legacy.proven.creeping-necrosporulation.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 0.965, 190, ReferencePanel, P7Date, ReferencePanelDoc), // Creeping>Necrosporulation
                ["legacy.proven.power-mutations-max-econ.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 0.943, 145, ReferencePanel, P7Date, ReferencePanelDoc), // Power Mutations Max Econ
                ["legacy.proven.tst-anaboliccreepingnecroregressioncascade.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.705, 166, ReferencePanel, P7Date, ReferencePanelDoc), // TST_AnabolicCreepingNecroRegressionCascade
                ["legacy.proven.tst-campaignplayer-safebaseline.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.620, 173, ReferencePanel, P7Date, ReferencePanelDoc), // TST_CampaignPlayer_SafeBaseline
                ["legacy.proven.grow-mutate-kill-max-econ.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.492, 173, ReferencePanel, P7Date, ReferencePanelDoc), // Grow>Mutate>Kill(Max Econ)
                ["legacy.proven.tst-creepingnecroregressioncascade.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.490, 183, ReferencePanel, P7Date, ReferencePanelDoc), // TST_CreepingNecroRegressionCascade
                ["legacy.proven.tst-anabolicbeaconnecroregressioncascade.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.366, 195, ReferencePanel, P7Date, ReferencePanelDoc), // TST_AnabolicBeaconNecroRegressionCascade
                ["legacy.proven.growth-resilience.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.333, 185, ReferencePanel, P7Date, ReferencePanelDoc), // Growth/Resilience
                ["legacy.proven.best-maxecon-surge10-hyphalsurge.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.153, 167, ReferencePanel, P7Date, ReferencePanelDoc), // Best_MaxEcon_Surge10_HyphalSurge

                // P8 contextual classification against the frozen P7 Proven panel; the dossier
                // records the band and its holdouts but not a pooled share.
                ["ai.growth.verdant-reclaimer.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, null, null, "p8-bloom20-contextual-v1", "2026-09-16", "FungusToast.Core/docs/second-level/AI_P8_BLOOM20_REVIEW_DOSSIER_V1.md"),

                // P7 Campaign panel. Measured before the 2026-09-07 reslotting and reroster.
                ["legacy.campaign.ai13.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 2.076, 131, CampaignPanel, P7Date, CampaignPanelDoc), // AI13
                ["legacy.campaign.ai12.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.959, 126, CampaignPanel, P7Date, CampaignPanelDoc), // AI12
                ["legacy.campaign.cmp-bloom-anabolicregression-medium.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.934, 149, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Bloom_AnabolicRegression_Medium
                ["legacy.campaign.cmp-control-anabolicfirst-hard.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.727, 137, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Control_AnabolicFirst_Hard
                ["legacy.campaign.cmp-economy-economancer-elite.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.708, 126, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Economy_Economancer_Elite
                ["legacy.campaign.cmp-control-anabolicrebirth-medium.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.619, 122, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Control_AnabolicRebirth_Medium
                ["legacy.campaign.cmp-control-rebirthfurnace-medium.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.613, 120, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Control_RebirthFurnace_Medium
                ["legacy.campaign.cmp-economy-killreclaim-medium.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.610, 128, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Economy_KillReclaim_Medium
                ["legacy.campaign.tst-campaign7-killreclaim-offset2.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.588, 115, CampaignPanel, P7Date, CampaignPanelDoc), // TST_Campaign7_KillReclaim_Offset2
                ["legacy.campaign.cmp-growth-putridtendrils-medium.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.571, 145, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Growth_PutridTendrils_Medium
                ["legacy.campaign.tst-campaign7-killreclaim-offset3.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.501, 113, CampaignPanel, P7Date, CampaignPanelDoc), // TST_Campaign7_KillReclaim_Offset3
                ["legacy.campaign.tst-campaign7-killreclaim-offset1.v1"] = new(DifficultyBand.Elite, BandEvidence.Sufficient, 1.485, 130, CampaignPanel, P7Date, CampaignPanelDoc), // TST_Campaign7_KillReclaim_Offset1
                ["legacy.campaign.cmp-economy-hoardsporeregent-elite.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.366, 114, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Economy_HoardsporeRegent_Elite
                ["legacy.campaign.tst-campaign7-killreclaim-offset8.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.317, 128, CampaignPanel, P7Date, CampaignPanelDoc), // TST_Campaign7_KillReclaim_Offset8
                ["legacy.campaign.cmp-bloom-beaconregression-medium.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.295, 134, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Bloom_BeaconRegression_Medium
                ["legacy.campaign.cmp-defense-ironshell-elite.v1"] = new(DifficultyBand.Hard, BandEvidence.Sufficient, 1.284, 123, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Defense_IronShell_Elite
                ["legacy.campaign.cmp-bloom-creepingnecro-medium.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.212, 145, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Bloom_CreepingNecro_Medium
                ["legacy.campaign.ai4.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.197, 151, CampaignPanel, P7Date, CampaignPanelDoc), // AI4
                ["legacy.campaign.ai5.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.194, 112, CampaignPanel, P7Date, CampaignPanelDoc), // AI5
                ["legacy.campaign.cmp-economy-latespike-hard.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.173, 132, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Economy_LateSpike_Hard
                ["legacy.campaign.cmp-bloom-creepingregression-elite.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.112, 129, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Bloom_CreepingRegression_Elite
                ["legacy.campaign.cmp-economy-temporeclaim-medium.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.111, 134, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Economy_TempoReclaim_Medium
                ["legacy.campaign.cmp-reclaim-scavenger-easy.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 1.092, 127, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Reclaim_Scavenger_Easy
                ["legacy.campaign.cmp-growth-pressure-medium.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 0.934, 106, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Growth_Pressure_Medium
                ["legacy.campaign.tst-campaignplayer-safebaseline.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 0.902, 137, CampaignPanel, P7Date, CampaignPanelDoc), // TST_CampaignPlayer_SafeBaseline
                ["legacy.campaign.cmp-bloom-thanatophyte-elite.v1"] = new(DifficultyBand.Normal, BandEvidence.Sufficient, 0.867, 126, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Bloom_Thanatophyte_Elite
                ["legacy.campaign.cmp-bloom-necrotoxingauntlet-elite.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.831, 140, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Bloom_NecrotoxinGauntlet_Elite
                ["legacy.campaign.cmp-growth-wildfirebloom-medium.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.809, 135, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Growth_WildfireBloom_Medium
                ["legacy.campaign.cmp-bloom-fortifymimic-medium.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.756, 131, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Bloom_FortifyMimic_Medium
                ["legacy.campaign.cmp-tiercap-growthresilience-easy.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.725, 115, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_TierCap_GrowthResilience_Easy
                ["legacy.campaign.ai6.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.720, 139, CampaignPanel, P7Date, CampaignPanelDoc), // AI6
                ["legacy.campaign.cmp-defense-reclaimshell-easy.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.704, 132, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Defense_ReclaimShell_Easy
                ["legacy.campaign.tst-ai10-creepingregression.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.648, 115, CampaignPanel, P7Date, CampaignPanelDoc), // TST_AI10_CreepingRegression
                ["legacy.campaign.cmp-surge-pulsar-easy.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.644, 149, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Surge_Pulsar_Easy
                ["legacy.campaign.tst-ai10-beaconregression.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.524, 111, CampaignPanel, P7Date, CampaignPanelDoc), // TST_AI10_BeaconRegression
                ["legacy.campaign.cmp-anabolicbeaconrhizolith-elite.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.498, 130, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_AnabolicBeaconRhizolith_Elite
                ["legacy.campaign.cmp-mobility-overextender-training.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.490, 133, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Mobility_Overextender_Training
                ["legacy.campaign.cmp-defense-resilientshell-easy.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.455, 129, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Defense_ResilientShell_Easy
                ["legacy.campaign.cmp-mobility-overextender-training-offset1.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.447, 126, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Mobility_Overextender_Training_Offset1
                ["legacy.campaign.cmp-mobility-overextender-training-offset2.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.446, 119, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Mobility_Overextender_Training_Offset2
                ["legacy.campaign.cmp-mobility-overextender-training-offset3.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.415, 132, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Mobility_Overextender_Training_Offset3
                ["legacy.campaign.tst-training-resilientmycelium-offset3.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.415, 123, CampaignPanel, P7Date, CampaignPanelDoc), // TST_Training_ResilientMycelium_Offset3
                ["legacy.campaign.tst-training-resilientmycelium-offset1.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.401, 128, CampaignPanel, P7Date, CampaignPanelDoc), // TST_Training_ResilientMycelium_Offset1
                ["legacy.campaign.tst-training-resilientmycelium.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.370, 126, CampaignPanel, P7Date, CampaignPanelDoc), // TST_Training_ResilientMycelium
                ["legacy.campaign.cmp-surge-beacontempo-medium.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.361, 118, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Surge_BeaconTempo_Medium
                ["legacy.campaign.cmp-surge-beaconsprinter-medium.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.358, 138, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Surge_BeaconSprinter_Medium
                ["legacy.campaign.cmp-surge-growthtempo-medium.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.306, 135, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Surge_GrowthTempo_Medium
                ["legacy.campaign.cmp-reclaim-infiltrationsurge-easy.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.271, 134, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Reclaim_InfiltrationSurge_Easy
                ["legacy.campaign.cmp-attrition-toxicturtle-training.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.152, 105, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Attrition_ToxicTurtle_Training
                ["legacy.campaign.cmp-attrition-toxicturtle-training-offset1.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.141, 130, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Attrition_ToxicTurtle_Training_Offset1
                ["legacy.campaign.cmp-attrition-toxicturtle-training-offset2.v1"] = new(DifficultyBand.Easy, BandEvidence.Sufficient, 0.140, 130, CampaignPanel, P7Date, CampaignPanelDoc), // CMP_Attrition_ToxicTurtle_Training_Offset2
            };

        public static IReadOnlyDictionary<string, StrategyMeasuredBand> All => BandsByStrategyId;

        public static StrategyMeasuredBand? Get(string strategyId)
        {
            return BandsByStrategyId.TryGetValue(strategyId, out var band) ? band : null;
        }
    }
}
