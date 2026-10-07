using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.AI;

public class StrategyCatalogTests
{
    [Fact]
    public void Registry_definitions_atomically_bind_behavior_identity_and_metadata()
    {
        _ = AIRoster.ProvenStrategies.Count;

        // Generated holds run-time candidates rather than authored content, so it is empty here
        // and carries identities the simulation supplies instead of GetStableId's legacy.* scheme.
        Assert.Empty(StrategyRegistry.GetDefinitions(StrategySetEnum.Generated));

        foreach (var strategySet in AIRoster.AuthoredStrategySets)
        {
            var definitions = StrategyRegistry.GetDefinitions(strategySet);
            Assert.NotEmpty(definitions);
            Assert.Equal(definitions.Count, definitions.Select(definition => definition.StrategyId).Distinct().Count());

            foreach (var definition in definitions)
            {
                Assert.Equal(definition.Strategy.StrategyName, definition.Metadata.StrategyName);
                Assert.Equal(strategySet, definition.Metadata.StrategySet);
                Assert.Equal(StrategyIdentity.GetStableId(strategySet, definition.Strategy), definition.StrategyId);
                Assert.Equal(StrategyIdentity.GetDefinitionFingerprint(definition.Strategy), definition.DefinitionFingerprint);
                Assert.Same(
                    definition,
                    StrategyRegistry.GetDefinition(strategySet, definition.Strategy.StrategyName));

                foreach (var adaptationSet in definition.Metadata.SuggestedAdaptationSets)
                {
                    Assert.False(string.IsNullOrWhiteSpace(adaptationSet.SetName));
                    Assert.NotEmpty(adaptationSet.AdaptationIds);
                    Assert.Equal(
                        adaptationSet.AdaptationIds.Count,
                        adaptationSet.AdaptationIds.Distinct(StringComparer.Ordinal).Count());
                    Assert.All(
                        adaptationSet.AdaptationIds,
                        adaptationId => Assert.True(
                            AdaptationRepository.TryGetById(adaptationId, out _),
                            $"Unknown adaptation '{adaptationId}' in {definition.StrategyId}/{adaptationSet.SetName}."));
                }
            }
        }
    }

    /// <summary>
    /// AI13 is the campaign's flagship boss, so its identity is pinned against accidental drift.
    /// Its campaign tier moved Hard -> Elite when the P7 campaign matrix measured it at 2.076
    /// pooled parity-normalized board share; the authored role did not move, because measurement
    /// says how strong it is and authoring says what it is for.
    /// </summary>
    [Fact]
    public void Campaign_ai13_keeps_its_boss_identity_and_measured_elite_tier()
    {
        var definition = Assert.IsType<StrategyDefinition>(
            StrategyRegistry.GetDefinition(StrategySetEnum.Campaign, "AI13"));

        Assert.Equal(StrategyRole.Boss, definition.Metadata.Role);
        Assert.Contains(DifficultyBand.Hard, definition.Metadata.IntendedBands);
        Assert.Contains(DifficultyBand.Elite, definition.Metadata.IntendedBands);
        Assert.Equal(DifficultyBand.Elite, definition.MeasuredBand?.Band);
        Assert.Equal(CampaignDifficulty.Elite, definition.Metadata.CampaignDifficulty);
    }

    [Fact]
    public void Roster_selection_rejects_unregistered_fill_seats()
    {
        var available = StrategyRegistry.GetDefinitions(StrategySetEnum.Testing).Count;

        var exception = Assert.Throws<InvalidOperationException>(() => AIRoster.GetStrategies(
            available + 1,
            StrategySetEnum.Testing,
            new Random(1)));

        Assert.Contains("registered strategies", exception.Message);
        Assert.DoesNotContain("LegacyRandom", exception.Message);
    }

    [Fact]
    public void Filament_regrowth_is_a_measured_standard_single_player_growth_regeneration_strategy()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.ProvenStrategiesByName["RegrowthLattice"]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                (MutationIds.RegenerativeHyphae, GameBalance.RegenerativeHyphaeMaxLevel),
                (MutationIds.Necrosporulation, GameBalance.NecrosporulationMaxLevel),
                (MutationIds.FilamentOverdrive, GameBalance.FilamentOverdriveMaxLevel),
                (MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                (MutationIds.HypersystemicRegeneration, GameBalance.HypersystemicRegenerationMaxLevel)
            },
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());

        var catalogEntry = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Proven, strategy.StrategyName);
        Assert.NotNull(catalogEntry);
        // Authored Strong until the P7 calibration measured it at 1.058 normalized board share
        // over 187 games, which is parity rather than above it. See AI_P7_REFERENCE_BANDS_V1.
        var measured = StrategyRegistry.GetDefinition(StrategySetEnum.Proven, strategy.StrategyName)?.MeasuredBand;
        Assert.Equal(DifficultyBand.Normal, measured?.Band);
        Assert.Equal(1.058, measured?.PooledNormalizedBoardShare);
        Assert.Equal(187, measured?.Games);
        Assert.Equal(StrategyRole.Spice, catalogEntry.Role);
        Assert.Equal(StrategyLifecycle.Active, catalogEntry.Lifecycle);
        Assert.Equal(StrategyArchetype.Defense, catalogEntry.Archetype);
        Assert.True(catalogEntry.Pools.HasFlag(StrategyPool.SimulationBaseline));
    }

    [Fact]
    public void Verdant_reclaimer_promotion_preserves_bloom20_behavior_and_player_facing_identity()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.ProvenStrategiesByName["VerdantReclaimer"]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.AnabolicInversion, null),
                (MutationIds.MycelialBloom, 20),
                (MutationIds.MycotropicInduction, 1),
                (MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                (MutationIds.PutrefactiveRejuvenation, GameBalance.PutrefactiveRejuvenationMaxLevel)
            },
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());
        Assert.Equal(EconomyBias.ModerateEconomy, strategy.EconomyProfile);

        var definition = Assert.IsType<StrategyDefinition>(
            StrategyRegistry.GetDefinition(StrategySetEnum.Proven, strategy.StrategyName));
        Assert.Equal("ai.growth.verdant-reclaimer.v1", definition.StrategyId);
        Assert.Equal("Verdant Reclaimer", definition.Metadata.FriendlyName);
        Assert.Equal(
            "Builds a deep growth engine, then reclaims territory after the board breaks open.",
            definition.Metadata.AIPlayerIntentions);
        Assert.Equal(StrategyArchetype.Reclamation, definition.Metadata.Archetype);
        Assert.Equal(StrategyStatus.Proven, definition.Metadata.Status);
        Assert.Equal(DifficultyBand.Elite, definition.MeasuredBand?.Band);
        Assert.Equal("p8-bloom20-contextual-v1", definition.MeasuredBand?.MatrixId);
        Assert.Equal(StrategyLifecycle.Active, definition.Metadata.Lifecycle);
        Assert.Contains(DifficultyBand.Elite, definition.Metadata.IntendedBands);
        Assert.True(definition.Metadata.Pools.HasFlag(StrategyPool.SimulationBaseline));
        Assert.False(definition.Metadata.Pools.HasFlag(StrategyPool.Campaign));

        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Growth_VerdantReclaimer_Elite"]);
        Assert.Equal(
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)),
            campaign.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)));
        Assert.Equal(strategy.EconomyProfile, campaign.EconomyProfile);
        Assert.Equal(strategy.PrioritizeHighTier, campaign.PrioritizeHighTier);
        Assert.Equal(
            new[]
            {
                MycovariantIds.PerimeterProliferatorId,
                MycovariantIds.CornerConduitIIIId,
                MycovariantIds.ReclamationRhizomorphsId,
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.HyphalDrawId
            },
            campaign.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(campaign.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));

        var campaignDefinition = Assert.IsType<StrategyDefinition>(
            StrategyRegistry.GetDefinition(StrategySetEnum.Campaign, campaign.StrategyName));
        Assert.Equal("Verdant Reclaimer", campaignDefinition.Metadata.FriendlyName);
        Assert.Equal(CampaignDifficulty.Elite, campaignDefinition.Metadata.CampaignDifficulty);
        Assert.True(campaignDefinition.Metadata.Pools.HasFlag(StrategyPool.Campaign));
    }

    [Fact]
    public void Filament_overdrive_is_sequenced_after_the_strategy_engine()
    {
        var expectedPredecessors = new Dictionary<string, int>
        {
            ["CreepingReclaimer"] = MutationIds.Necrosporulation,
            ["TST_AnabolicCreepingNecroRegressionCascade"] = MutationIds.NecrophyticBloom,
            ["TST_CreepingNecroRegressionCascade"] = MutationIds.NecrophyticBloom,
            ["TST_AI10_CreepingRegression"] = MutationIds.NecrophyticBloom,
            ["CMP_Bloom_CreepingRegression_Elite"] = MutationIds.OntogenicRegression
        };

        foreach (var (strategyName, predecessorId) in expectedPredecessors)
        {
            var strategy = Assert.IsType<ParameterizedSpendingStrategy>(
                AIRoster.ProvenStrategiesByName.TryGetValue(strategyName, out var proven)
                    ? proven
                    : AIRoster.CampaignStrategiesByName[strategyName]);
            var goalIds = strategy.TargetMutationGoals.Select(goal => goal.MutationId).ToArray();
            var filamentIndex = Array.IndexOf(goalIds, MutationIds.FilamentOverdrive);

            Assert.True(filamentIndex > 0, $"{strategyName} must target Filament Overdrive after its engine.");
            Assert.Equal(predecessorId, goalIds[filamentIndex - 1]);
            Assert.Equal(GameBalance.FilamentOverdriveMaxLevel, strategy.TargetMutationGoals[filamentIndex].TargetLevel);
        }
    }

    [Theory]
    [InlineData("CMP_Growth_PutridTendrils_Medium")]
    [InlineData("CMP_Growth_WildfireBloom_Medium")]
    [InlineData("CMP_Bloom_CreepingNecro_Medium")]
    public void Campaign_strategies_do_not_take_an_unprofitable_filament_detour(string strategyName)
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName[strategyName]);

        Assert.DoesNotContain(strategy.TargetMutationGoals, goal => goal.MutationId == MutationIds.FilamentOverdrive);
    }

    [Fact]
    public void Economy_at_all_costs_completes_the_economy_ladder_around_ontogenic_regression()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_EconomyAtAllCosts"]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel),
                (MutationIds.AdaptiveExpression, GameBalance.AdaptiveExpressionMaxLevel),
                (MutationIds.MycotoxinCatabolism, GameBalance.MycotoxinCatabolismMaxLevel),
                (MutationIds.AnabolicInversion, GameBalance.AnabolicInversionMaxLevel),
                (MutationIds.ChitinFortification, 1),
                (MutationIds.HyperadaptiveDrift, 2),
                (MutationIds.OntogenicRegression, GameBalance.OntogenicRegressionMaxLevel),
                (MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                (MutationIds.LatentPolymorphism, GameBalance.LatentPolymorphismMaxLevel)
            },
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());
        Assert.Equal(EconomyBias.MaxEconomy, strategy.EconomyProfile);
    }

    [Theory]
    [InlineData("TST_EcologyFrontierExpansion")]
    [InlineData("TST_EcologyFrontierResilience")]
    public void Ecology_testing_strategies_begin_with_aerated_frontier(string strategyName)
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName[strategyName]);

        Assert.Equal(MutationIds.AeratedFrontier, strategy.TargetMutationGoals[0].MutationId);
        Assert.True(strategy.UsesSubstrateEcology);
    }

    [Theory]
    [InlineData("TST_EcologyFrontierExpansion")]
    [InlineData("TST_EcologyFrontierResilience")]
    public void Ecology_testing_strategies_buy_aerated_frontier_first(string strategyName)
    {
        var strategy = AIRoster.TestingStrategiesByName[strategyName];
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = new Player(0, "Ecology AI", PlayerTypeEnum.AI) { MutationPoints = 1 };
        board.Players.Add(player);

        strategy.SpendMutationPoints(
            player,
            MutationRegistry.GetAll().ToList(),
            board,
            new Random(1),
            new TestSimulationObserver());

        Assert.Equal(1, player.GetMutationLevel(MutationIds.AeratedFrontier));
        Assert.Equal(0, player.GetMutationLevel(MutationIds.MutatorPhenotype));
    }

    [Fact]
    public void Ecology_autolytic_detrital_strategy_keeps_its_approved_staged_surge_order()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyAutolyticDetrital"]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.AeratedFrontier, GameBalance.AeratedFrontierMaxLevel),
                (MutationIds.HyphalSurge, 1),
                (MutationIds.CrustwardTropism, GameBalance.CrustwardTropismMaxLevel),
                (MutationIds.HyphalSurge, 2),
                (MutationIds.CreepingMold, 1),
                (MutationIds.HyphalSurge, 3),
                (MutationIds.DetritalEnzymes, GameBalance.DetritalEnzymesMaxLevel),
                (MutationIds.NecrophyticBloom, GameBalance.NecrophyticBloomMaxLevel),
                (MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel)
            },
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());
        Assert.Equal(new[] { MutationIds.HyphalSurge }, strategy.SurgePriorityIds);
    }

    [Fact]
    public void Ecology_autolytic_reclaimer_strategy_brings_death_recovery_online_before_later_surges()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyAutolyticReclaimer"]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.AeratedFrontier, GameBalance.AeratedFrontierMaxLevel),
                (MutationIds.HyphalSurge, 1),
                (MutationIds.Necrosporulation, GameBalance.NecrosporulationMaxLevel),
                (MutationIds.HyphalSurge, 2),
                (MutationIds.CrustwardTropism, GameBalance.CrustwardTropismMaxLevel),
                (MutationIds.CreepingMold, 1),
                (MutationIds.HyphalSurge, 3),
                (MutationIds.DetritalEnzymes, GameBalance.DetritalEnzymesMaxLevel),
                (MutationIds.RegenerativeHyphae, GameBalance.RegenerativeHyphaeMaxLevel),
                (MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel)
            },
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());

        Assert.DoesNotContain(strategy.TargetMutationGoals, goal => goal.MutationId == MutationIds.NecrophyticBloom);
        Assert.Equal(new[] { MutationIds.HyphalSurge }, strategy.SurgePriorityIds);
    }

    [Fact]
    public void Hyperadaptive_goal_deliberately_activates_chitin_fortification_prerequisite()
    {
        var strategy = new ParameterizedSpendingStrategy(
            strategyName: "Hyperadaptive prerequisite test",
            prioritizeHighTier: true,
            targetMutationGoals: new List<TargetMutationGoal>
            {
                new(MutationIds.HyperadaptiveDrift, 1)
            },
            economyBias: EconomyBias.IgnoreEconomy);
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = new Player(0, "Hyperadaptive AI", PlayerTypeEnum.AI) { MutationPoints = 20 };
        board.Players.Add(player);

        player.SetMutationLevel(MutationIds.HomeostaticHarmony, 5, currentRound: 0);
        player.SetMutationLevel(MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel - 2, currentRound: 0);
        player.SetMutationLevel(MutationIds.AdaptiveExpression, 3, currentRound: 0);
        player.SetMutationLevel(MutationIds.AnabolicInversion, GameBalance.AnabolicInversionMaxLevel, currentRound: 0);

        strategy.SpendMutationPoints(
            player,
            MutationRegistry.GetAll().ToList(),
            board,
            new Random(1),
            new TestSimulationObserver());

        Assert.Equal(1, player.GetMutationLevel(MutationIds.ChitinFortification));
        Assert.True(player.IsSurgeActive(MutationIds.ChitinFortification));
    }

    [Fact]
    public void Ontogenic_goal_finishes_the_three_closest_tier_one_category_foundations()
    {
        var strategy = new ParameterizedSpendingStrategy(
            strategyName: "Ontogenic category prerequisite test",
            prioritizeHighTier: true,
            targetMutationGoals: new List<TargetMutationGoal>
            {
                new(MutationIds.OntogenicRegression, 1)
            },
            economyBias: EconomyBias.IgnoreEconomy);
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        // Exactly the two Tier-1 levels the chain needs, so fallback spending cannot blur which
        // foundations the goal chose.
        var player = new Player(0, "Ontogenic AI", PlayerTypeEnum.AI)
        {
            MutationPoints = 2 * GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1)
        };
        board.Players.Add(player);

        player.SetMutationLevel(MutationIds.MutatorPhenotype, 10, currentRound: 0);
        player.SetMutationLevel(MutationIds.AdaptiveExpression, 3, currentRound: 0);
        player.SetMutationLevel(MutationIds.AnabolicInversion, 3, currentRound: 0);
        player.SetMutationLevel(MutationIds.HomeostaticHarmony, 9, currentRound: 0);
        player.SetMutationLevel(MutationIds.ChitinFortification, 1, currentRound: 0);
        player.SetMutationLevel(MutationIds.HyperadaptiveDrift, 2, currentRound: 0);
        player.SetMutationLevel(MutationIds.MycotoxinTracer, 9, currentRound: 0);
        player.SetMutationLevel(MutationIds.MycelialBloom, 8, currentRound: 0);

        strategy.SpendMutationPoints(
            player,
            MutationRegistry.GetAll().ToList(),
            board,
            new Random(1),
            new TestSimulationObserver());

        Assert.Equal(10, player.GetMutationLevel(MutationIds.MutatorPhenotype));
        Assert.Equal(10, player.GetMutationLevel(MutationIds.HomeostaticHarmony));
        Assert.Equal(10, player.GetMutationLevel(MutationIds.MycotoxinTracer));
        Assert.Equal(8, player.GetMutationLevel(MutationIds.MycelialBloom));
        Assert.Equal(0, player.GetMutationLevel(MutationIds.OntogenicRegression));
        Assert.Equal(
            board.CurrentRound,
            player.PlayerMutations[MutationIds.OntogenicRegression].PrereqMetRound);
    }

    [Fact]
    public void Non_testing_catalog_entries_expose_player_facing_name_and_fantasy()
    {
        var entries = AIRoster.GetStrategyCatalogEntries(StrategySetEnum.Proven)
            .Concat(AIRoster.GetStrategyCatalogEntries(StrategySetEnum.Campaign))
            .Where(entry => entry.Status != StrategyStatus.Testing)
            .ToList();

        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.FriendlyName), $"Expected player-facing name for {entry.StrategyName}");
            Assert.False(string.IsNullOrWhiteSpace(entry.AIPlayerIntentions), $"Expected fantasy for {entry.StrategyName}");
            Assert.DoesNotContain("TST_", entry.FriendlyName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CMP_", entry.FriendlyName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(">", entry.FriendlyName, StringComparison.Ordinal);
            Assert.EndsWith(".", entry.AIPlayerIntentions);
            Assert.False(string.IsNullOrWhiteSpace(entry.MutationPlan), $"Expected mutation plan for {entry.StrategyName}");
            Assert.False(string.IsNullOrWhiteSpace(entry.MycovariantPlan), $"Expected Mycovariant plan for {entry.StrategyName}");
        }
    }

    [Fact]
    public void Campaign_player_facing_mycovariant_authoring_debt_does_not_expand()
    {
        _ = AIRoster.CampaignStrategies.Count;

        // This frozen legacy set is migration debt, not an approved authoring pattern. Remove a
        // name when its specific ordered plan is supported by matched simulation evidence. Never
        // add a new name here merely to make the test pass: new Campaign AIs must start curated.
        var knownLegacyDebt = new[]
        {
            "AI13",
            "AI4",
            "CMP_Bloom_BeaconRegression_Medium",
            "CMP_Bloom_CreepingNecro_Medium",
            "CMP_Control_AnabolicFirst_Hard",
            "CMP_Control_AnabolicRebirth_Medium",
            "CMP_Control_RebirthFurnace_Medium",
            "CMP_Defense_ResilientShell_Easy",
            "CMP_Growth_PutridTendrils_Medium",
            "CMP_Surge_BeaconSprinter_Medium",
            "CMP_Surge_BeaconTempo_Medium",
            "CMP_Surge_GrowthTempo_Medium",
            "TST_AI10_BeaconRegression",
            "TST_AI10_CreepingRegression",
            "TST_Campaign7_KillReclaim_Offset2"
        };

        var debt = StrategyRegistry.GetDefinitions(StrategySetEnum.Campaign)
            .Where(definition => definition.Metadata.Status != StrategyStatus.Testing)
            .Select(definition => definition.Strategy)
            .OfType<ParameterizedSpendingStrategy>()
            .Where(strategy =>
            {
                var preferences = strategy.GetMycovariantPreferences();
                return preferences.Count == 0 || preferences.Any(preference => preference.IsCategoryDerived);
            })
            .Select(strategy => strategy.StrategyName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(knownLegacyDebt, debt);
    }

    [Fact]
    public void Campaign_progression_board_presets_only_use_registered_campaign_strategies()
    {
        var strategyNames = ReadBoardPresetStrategyNames();

        Assert.NotEmpty(strategyNames);
        Assert.All(strategyNames, name => Assert.True(
            AIRoster.CampaignStrategiesByName.ContainsKey(name),
            $"Board preset references strategy '{name}', which is not registered in the Campaign set."));
    }

    /// <summary>
    /// A campaign opponent measuring outside its intended band is a broken promise to the player,
    /// so new mismatches fail. Known ones are acknowledged here until their intent is re-authored
    /// or the strategy is retuned; resolving one requires removing it from the list.
    /// </summary>
    [Fact]
    public void Campaign_preset_strategies_measure_inside_their_intended_band()
    {
        // Measured by the P7 Campaign panel before the 2026-09-07 reroster, which placed these by
        // measured strength without updating their authored intent. Re-author intent alongside the
        // strategy profiles, or retune, then remove the entry.
        var acknowledged = new HashSet<string>(StringComparer.Ordinal)
        {
            "AI12",
            "CMP_AnabolicBeaconRhizolith_Elite",
            "CMP_Bloom_AnabolicRegression_Medium",
            "CMP_Bloom_BeaconRegression_Medium",
            "CMP_Bloom_CreepingRegression_Elite",
            "CMP_Bloom_FortifyMimic_Medium",
            "CMP_Bloom_NecrotoxinGauntlet_Elite",
            "CMP_Bloom_Thanatophyte_Elite",
            "CMP_Control_AnabolicFirst_Hard",
            "CMP_Control_AnabolicRebirth_Medium",
            "CMP_Control_RebirthFurnace_Medium",
            "CMP_Economy_KillReclaim_Medium",
            "CMP_Economy_LateSpike_Hard",
            "CMP_Growth_PutridTendrils_Medium",
            "CMP_Growth_WildfireBloom_Medium",
            "CMP_Reclaim_Scavenger_Easy",
            "CMP_Surge_BeaconSprinter_Medium",
            "CMP_Surge_BeaconTempo_Medium",
            "CMP_Surge_GrowthTempo_Medium",
            "CMP_Surge_Pulsar_Easy",
            "TST_AI10_BeaconRegression",
            "TST_AI10_CreepingRegression",
            "TST_Campaign7_KillReclaim_Offset1",
            "TST_Campaign7_KillReclaim_Offset2",
            "TST_Campaign7_KillReclaim_Offset3",
            "TST_Campaign7_KillReclaim_Offset8",
        };

        var mismatches = ReadBoardPresetStrategyNames()
            .Distinct(StringComparer.Ordinal)
            .Select(name => StrategyRegistry.GetDefinition(StrategySetEnum.Campaign, name))
            .OfType<StrategyDefinition>()
            .Where(definition => definition.Metadata.IntendedBands.Count > 0
                && definition.MeasuredBand?.Band is { } band
                && !definition.Metadata.IntendedBands.Contains(band))
            .ToDictionary(
                definition => definition.Strategy.StrategyName,
                definition => $"{definition.Strategy.StrategyName}: intended {string.Join("/", definition.Metadata.IntendedBands)}, "
                    + $"measured {definition.MeasuredBand!.Band} ({definition.MeasuredBand.MatrixId})",
                StringComparer.Ordinal);

        var unacknowledged = mismatches.Keys.Except(acknowledged).OrderBy(name => name, StringComparer.Ordinal).ToList();
        var resolved = acknowledged.Except(mismatches.Keys).OrderBy(name => name, StringComparer.Ordinal).ToList();
        Assert.True(unacknowledged.Count == 0,
            "Campaign opponents measure outside their intended band:\n"
            + string.Join("\n", unacknowledged.Select(name => mismatches[name])));
        Assert.True(resolved.Count == 0,
            "These acknowledged mismatches are resolved; remove them from the list: " + string.Join(", ", resolved));
    }

    /// <summary>Every strategy a board preset can field: fixed seats and pooled entries.</summary>
    private static List<string> ReadBoardPresetStrategyNames()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var presetDir = Path.Combine(repoRoot, "FungusToast.Unity", "Assets", "Configs", "Board Presets");
        return Directory
            .EnumerateFiles(presetDir, "*.asset", SearchOption.TopDirectoryOnly)
            .SelectMany(path =>
            {
                var text = File.ReadAllText(path);
                var seats = Regex.Matches(text, @"strategyName:\s*([^\r\n]+)")
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value);
                var pooled = Regex.Matches(text, @"aiStrategyPool:[ \t]*\r?\n((?:[ \t]*- [^\r\n]+\r?\n)+)")
                    .Cast<Match>()
                    .SelectMany(match => Regex.Matches(match.Groups[1].Value, @"- ([^\r\n]+)")
                        .Cast<Match>()
                        .Select(item => item.Groups[1].Value));
                return seats.Concat(pooled);
            })
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
    }

    [Fact]
    public void Campaign_boss_profiles_use_curated_friendly_names()
    {
        var economyBoss = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, "CMP_Economy_Economancer_Elite");
        var controlBoss = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, "CMP_Control_AnabolicFirst_Hard");

        Assert.NotNull(economyBoss);
        Assert.Equal("The Economancer", economyBoss!.FriendlyName);
        Assert.NotNull(controlBoss);
        Assert.Equal("Voltaic Bloom", controlBoss!.FriendlyName);
    }

    [Fact]
    public void Campaign_training_profiles_use_curated_fantasy()
    {
        var trainingEntry = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, "CMP_Mobility_Overextender_Training");

        Assert.NotNull(trainingEntry);
        Assert.Equal("Overextender", trainingEntry!.FriendlyName);
        Assert.Contains("chasing space", trainingEntry.AIPlayerIntentions, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".", trainingEntry.AIPlayerIntentions);
    }

    [Fact]
    public void Campaign_legacy_strategy_names_resolve_to_cmp_entries()
    {
        var legacyEntry = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, "AI1");
        var renamedEntry = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, "CMP_Economy_Economancer_Elite");

        Assert.NotNull(legacyEntry);
        Assert.NotNull(renamedEntry);
        Assert.Equal(renamedEntry!.StrategyName, legacyEntry!.StrategyName);
    }
}
