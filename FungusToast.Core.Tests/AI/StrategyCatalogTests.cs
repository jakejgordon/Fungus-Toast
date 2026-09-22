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
    /// pooled parity-normalized board share; the authored role and power tier did not move,
    /// because measurement says how strong it is and authoring says what it is for.
    /// </summary>
    [Fact]
    public void Campaign_ai13_keeps_its_boss_identity_and_measured_elite_tier()
    {
        var definition = Assert.IsType<StrategyDefinition>(
            StrategyRegistry.GetDefinition(StrategySetEnum.Campaign, "AI13"));

        Assert.Equal(StrategyPowerTier.Strong, definition.Metadata.PowerTier);
        Assert.Equal(StrategyRole.Boss, definition.Metadata.Role);
        Assert.Contains(DifficultyBand.Hard, definition.Metadata.DifficultyBands);
        Assert.Contains(DifficultyBand.Elite, definition.Metadata.DifficultyBands);
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
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.ProvenStrategiesByName["Filament Regrowth"]);

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
        Assert.Equal(StrategyPowerTier.Standard, catalogEntry.PowerTier);
        Assert.Equal(StrategyRole.Spice, catalogEntry.Role);
        Assert.Equal(StrategyLifecycle.Active, catalogEntry.Lifecycle);
        Assert.Equal(StrategyArchetype.Defense, catalogEntry.Archetype);
        Assert.True(catalogEntry.Pools.HasFlag(StrategyPool.SimulationBaseline));
    }

    [Fact]
    public void Verdant_reclaimer_promotion_preserves_bloom20_behavior_and_player_facing_identity()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.ProvenStrategiesByName["Verdant Reclaimer"]);

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
        Assert.Equal(StrategyPowerTier.Strong, definition.Metadata.PowerTier);
        Assert.Equal(StrategyLifecycle.Active, definition.Metadata.Lifecycle);
        Assert.Contains(DifficultyBand.Elite, definition.Metadata.DifficultyBands);
        Assert.True(definition.Metadata.Pools.HasFlag(StrategyPool.SimulationBaseline));
        Assert.False(definition.Metadata.Pools.HasFlag(StrategyPool.Campaign));
        Assert.DoesNotContain(
            AIRoster.CampaignStrategies,
            candidate => string.Equals(candidate.StrategyName, strategy.StrategyName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filament_overdrive_is_sequenced_after_the_strategy_engine()
    {
        var expectedPredecessors = new Dictionary<string, int>
        {
            ["Creeping>Necrosporulation"] = MutationIds.Necrosporulation,
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

    [Theory]
    [InlineData("TST_HyperEconomyRamp", "TST_HyperEconomyRamp_NoOntogenic")]
    [InlineData("TST_Arch04_DriftGrowth", "TST_Arch04_DriftGrowth_NoOntogenic")]
    public void Ontogenic_ab_controls_remove_only_the_ontogenic_goal(
        string treatmentName,
        string controlName)
    {
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName[treatmentName]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName[controlName]);

        Assert.Contains(treatment.TargetMutationGoals, goal => goal.MutationId == MutationIds.OntogenicRegression);
        Assert.DoesNotContain(control.TargetMutationGoals, goal => goal.MutationId == MutationIds.OntogenicRegression);
        Assert.Contains(MutationIds.OntogenicRegression, control.ExcludedMutationIds);
        Assert.DoesNotContain(MutationIds.OntogenicRegression, treatment.ExcludedMutationIds);
        Assert.Equal(
            treatment.TargetMutationGoals
                .Where(goal => goal.MutationId != MutationIds.OntogenicRegression)
                .Select(goal => (goal.MutationId, goal.TargetLevel)),
            control.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)));
        Assert.Equal(treatment.EconomyProfile, control.EconomyProfile);
        Assert.Equal(treatment.PriorityMutationCategories, control.PriorityMutationCategories);
        Assert.Equal(treatment.PrioritizeHighTier, control.PrioritizeHighTier);
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
    public void Ecology_crust_first_and_frontier_first_strategies_keep_their_approved_target_order()
    {
        var crustFirst = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyCrustFirst"]);
        var frontierFirst = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyFrontierFirst"]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.AeratedFrontier, 10),
                (MutationIds.CrustwardTropism, GameBalance.CrustwardTropismMaxLevel),
                (MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                (MutationIds.DetritalEnzymes, GameBalance.DetritalEnzymesMaxLevel),
                (MutationIds.AeratedFrontier, GameBalance.AeratedFrontierMaxLevel),
                (MutationIds.HypersystemicRegeneration, GameBalance.HypersystemicRegenerationMaxLevel),
                (MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                (MutationIds.NecrohyphalInfiltration, GameBalance.NecrohyphalInfiltrationMaxLevel)
            },
            crustFirst.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());
        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.AeratedFrontier, GameBalance.AeratedFrontierMaxLevel),
                (MutationIds.CrustwardTropism, GameBalance.CrustwardTropismMaxLevel),
                (MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                (MutationIds.DetritalEnzymes, GameBalance.DetritalEnzymesMaxLevel),
                (MutationIds.HypersystemicRegeneration, GameBalance.HypersystemicRegenerationMaxLevel),
                (MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                (MutationIds.NecrohyphalInfiltration, GameBalance.NecrohyphalInfiltrationMaxLevel)
            },
            frontierFirst.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());

        var arch01 = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Arch01_GrowthResilience"]);
        Assert.Equal(arch01.EconomyProfile, crustFirst.EconomyProfile);
        Assert.Equal(arch01.EconomyProfile, frontierFirst.EconomyProfile);
        Assert.Equal(arch01.PriorityMutationCategories, crustFirst.PriorityMutationCategories);
        Assert.Equal(arch01.PriorityMutationCategories, frontierFirst.PriorityMutationCategories);
        Assert.Equal(
            arch01.TargetMutationGoals.Skip(1).Select(goal => (goal.MutationId, goal.TargetLevel)),
            crustFirst.TargetMutationGoals.Skip(5).Select(goal => (goal.MutationId, goal.TargetLevel)));
        Assert.Equal(
            arch01.TargetMutationGoals.Skip(1).Select(goal => (goal.MutationId, goal.TargetLevel)),
            frontierFirst.TargetMutationGoals.Skip(4).Select(goal => (goal.MutationId, goal.TargetLevel)));
    }

    [Fact]
    public void Ecology_toxin_fissioner_targets_the_full_toxin_margin_and_fission_chain()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyToxinFissioner"]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.HomeostaticHarmony, 5),
                (MutationIds.AeratedFrontier, 5),
                (MutationIds.ToxinMargin, GameBalance.ToxinMarginMaxLevel),
                (MutationIds.MycotoxinPotentiation, 5),
                (MutationIds.PutrefactiveMycotoxin, 2),
                (MutationIds.MycotoxinFission, GameBalance.ToxinborneSeedingMaxLevel)
            },
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());
        Assert.Equal(StrategyTheme.Offense, AIRoster.GetThemeForStrategy(strategy));
    }

    [Theory]
    [InlineData("CMP_Bloom_ToxinborneJetting_Medium")]
    [InlineData("CMP_Bloom_ToxinborneBallistospore_Hard")]
    public void Ecology_tracer_bloom_variants_reach_necrophytic_bloom_before_toxinborne_seeding(string strategyName)
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName[strategyName]);

        Assert.Equal(
            new (int MutationId, int? TargetLevel)[]
            {
                (MutationIds.MycotoxinTracer, 5),
                (MutationIds.MycelialBloom, 7),
                (MutationIds.SporicidalBloom, 1),
                (MutationIds.NecrophyticBloom, 1),
                (MutationIds.MycotoxinFission, GameBalance.ToxinborneSeedingMaxLevel),
                (MutationIds.MycotoxinTracer, 10),
                (MutationIds.SporicidalBloom, GameBalance.SporicidalBloomMaxLevel),
                (MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                (MutationIds.MycotoxinPotentiation, 5),
                (MutationIds.PutrefactiveMycotoxin, 2)
            },
            strategy.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)).ToArray());
        Assert.Equal(StrategyTheme.Offense, AIRoster.GetThemeForStrategy(strategy));
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
    public void Ecology_reclaimer_diagnosis_variants_isolate_necrosporulation_and_autolytic_surge()
    {
        var noNecro = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyReclaimer_NoNecro"]);
        var noAutolytic = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyReclaimer_NoAutolytic"]);
        var delayedNecro = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyReclaimer_DelayedNecro"]);

        Assert.Contains(MutationIds.Necrosporulation, noNecro.ExcludedMutationIds);
        Assert.DoesNotContain(noNecro.TargetMutationGoals, goal => goal.MutationId == MutationIds.Necrosporulation);
        Assert.Equal(new[] { MutationIds.HyphalSurge }, noNecro.SurgePriorityIds);

        Assert.Contains(MutationIds.HyphalSurge, noAutolytic.ExcludedMutationIds);
        Assert.DoesNotContain(noAutolytic.TargetMutationGoals, goal => goal.MutationId == MutationIds.HyphalSurge);
        Assert.Empty(noAutolytic.SurgePriorityIds);

        int detritalIndex = delayedNecro.TargetMutationGoals
            .Select((goal, index) => (goal, index))
            .Single(pair => pair.goal.MutationId == MutationIds.DetritalEnzymes).index;
        int necroIndex = delayedNecro.TargetMutationGoals
            .Select((goal, index) => (goal, index))
            .Single(pair => pair.goal.MutationId == MutationIds.Necrosporulation).index;
        Assert.True(necroIndex > detritalIndex);
        Assert.Equal(new[] { MutationIds.HyphalSurge }, delayedNecro.SurgePriorityIds);
    }

    [Fact]
    public void Bare_necro_rush_excludes_autolytic_surge_and_all_substrate_ecology_mutations()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_NecroRush_Bare"]);

        Assert.Equal(MutationIds.Necrosporulation, strategy.TargetMutationGoals[0].MutationId);
        Assert.Equal(
            new[]
            {
                MutationIds.HyphalSurge,
                MutationIds.AeratedFrontier,
                MutationIds.CrustwardTropism,
                MutationIds.DetritalEnzymes
            },
            strategy.ExcludedMutationIds.OrderBy(id => id));
    }

    [Fact]
    public void Bare_necro_rush_exclusions_apply_to_mutator_phenotype_auto_upgrades()
    {
        var strategy = AIRoster.TestingStrategiesByName["TST_NecroRush_Bare"];
        var player = new Player(0, "Bare Necro", PlayerTypeEnum.AI);
        player.SetMutationStrategy(strategy);
        var mutations = MutationRegistry.GetAll().ToList();

        foreach (var mutation in mutations.Where(mutation => mutation.Tier == MutationTier.Tier1 && mutation.Id != MutationIds.AeratedFrontier))
            player.SetMutationLevel(mutation.Id, mutation.MaxLevel, currentRound: 1);

        player.SetMutationLevel(MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel, currentRound: 1);
        GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(
            player,
            mutations,
            new Random(1),
            currentRound: 2,
            new TestSimulationObserver());

        Assert.Equal(0, player.GetMutationLevel(MutationIds.AeratedFrontier));
    }

    [Fact]
    public void Aerated_only_necro_rush_keeps_aerated_frontier_but_excludes_other_ecology_and_autolytic_surge()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_NecroRush_AeratedOnly"]);

        Assert.Contains(strategy.TargetMutationGoals, goal => goal.MutationId == MutationIds.AeratedFrontier);
        Assert.Contains(strategy.TargetMutationGoals, goal => goal.MutationId == MutationIds.Necrosporulation);
        Assert.DoesNotContain(MutationIds.AeratedFrontier, strategy.ExcludedMutationIds);
        Assert.Equal(
            new[] { MutationIds.HyphalSurge, MutationIds.CrustwardTropism, MutationIds.DetritalEnzymes },
            strategy.ExcludedMutationIds.OrderBy(id => id));
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
            "AI12",
            "AI13",
            "AI4",
            "AI5",
            "AI6",
            "CMP_AnabolicBeaconRhizolith_Elite",
            "CMP_Bloom_BeaconRegression_Medium",
            "CMP_Bloom_CreepingNecro_Medium",
            "CMP_Bloom_FortifyMimic_Medium",
            "CMP_Bloom_Thanatophyte_Elite",
            "CMP_Control_AnabolicFirst_Hard",
            "CMP_Control_AnabolicRebirth_Medium",
            "CMP_Control_RebirthFurnace_Medium",
            "CMP_Defense_ResilientShell_Easy",
            "CMP_Economy_LateSpike_Hard",
            "CMP_Economy_TempoReclaim_Medium",
            "CMP_Growth_Pressure_Medium",
            "CMP_Growth_PutridTendrils_Medium",
            "CMP_Growth_WildfireBloom_Medium",
            "CMP_Reclaim_InfiltrationSurge_Easy",
            "CMP_Surge_BeaconSprinter_Medium",
            "CMP_Surge_BeaconTempo_Medium",
            "CMP_Surge_GrowthTempo_Medium",
            "CMP_Surge_Pulsar_Easy",
            "CMP_TierCap_GrowthResilience_Easy",
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
    public void Economancer_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Economy_Economancer_Elite"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_Economancer_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_Economancer_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));

        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId,
                MycovariantIds.AscusWagerId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void Hoardspore_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Economy_HoardsporeRegent_Elite"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_Hoardspore_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_Hoardspore_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));

        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.ReclamationRhizomorphsId,
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.AscusWagerId,
                MycovariantIds.PlasmidBountyId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void IronShell_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Defense_IronShell_Elite"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_IronShell_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_IronShell_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.ReclamationRhizomorphsId,
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.AscusWagerId,
                MycovariantIds.PlasmidBountyId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void AI4_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["AI4"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_AI4_NoPreferenceControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_AI4_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Empty(control.GetMycovariantPreferences());
        Assert.Equal(
            new[]
            {
                MycovariantIds.ReclamationRhizomorphsId,
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.CornerConduitIIIId,
                MycovariantIds.CornerConduitIIId,
                MycovariantIds.CornerConduitIId,
                MycovariantIds.PerimeterProliferatorId,
                MycovariantIds.HyphalDrawId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void AI4_second_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_AI4_NoPreferenceControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_AI4_CuratedMycovariantsV2"]);

        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Empty(control.GetMycovariantPreferences());
        Assert.Equal(
            new[]
            {
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.ReclamationRhizomorphsId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void Necrotoxin_Gauntlet_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Bloom_NecrotoxinGauntlet_Elite"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_NecrotoxinGauntlet_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_NecrotoxinGauntlet_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId,
                MycovariantIds.AscusWagerId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void Harvest_Broker_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Economy_KillReclaim_Medium"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBroker_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBroker_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId,
                MycovariantIds.AscusWagerId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void Creeping_Regression_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Bloom_CreepingRegression_Elite"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_CreepingRegression_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_CreepingRegression_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId,
                MycovariantIds.AscusWagerId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void Voltaic_Rot_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Bloom_AnabolicRegression_Medium"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_VoltaicRot_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_VoltaicRot_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId,
                MycovariantIds.AscusWagerId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
        Assert.All(treatment.GetMycovariantPreferences(), preference => Assert.False(preference.IsCategoryDerived));
    }

    [Fact]
    public void Harvest_Broker_offset_one_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["TST_Campaign7_KillReclaim_Offset1"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset1_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset1_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[] { MycovariantIds.PlasmidBountyIIIId, MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId, MycovariantIds.AscusWagerId },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Harvest_Broker_offset_two_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["TST_Campaign7_KillReclaim_Offset2"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset2_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset2_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[] { MycovariantIds.PlasmidBountyIIIId, MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId, MycovariantIds.AscusWagerId },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Harvest_Broker_offset_three_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["TST_Campaign7_KillReclaim_Offset3"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset3_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset3_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[] { MycovariantIds.PlasmidBountyIIIId, MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId, MycovariantIds.AscusWagerId },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Harvest_Broker_offset_eight_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["TST_Campaign7_KillReclaim_Offset8"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset8_EconomyControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_HarvestBrokerOffset8_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[] { MycovariantIds.PlasmidBountyIIIId, MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId, MycovariantIds.AscusWagerId },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Resilient_Mycelium_offset_one_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["TST_Training_ResilientMycelium_Offset1"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ResilientMyceliumOffset1_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ResilientMyceliumOffset1_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.HyphalResistanceTransferId,
                MycovariantIds.SeptalAlarmId,
                MycovariantIds.MycelialBastionIIIId,
                MycovariantIds.MycelialBastionIIId,
                MycovariantIds.MycelialBastionIId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Overextender_offset_one_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["CMP_Mobility_Overextender_Training_Offset1"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_OverextenderOffset1_GrowthControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_OverextenderOffset1_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.HyphalDrawId,
                MycovariantIds.AggressotropicConduitIIIId,
                MycovariantIds.CornerConduitIIIId,
                MycovariantIds.PerimeterProliferatorId,
                MycovariantIds.AggressotropicConduitIIId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Toxic_Turtle_offset_one_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["CMP_Attrition_ToxicTurtle_Training_Offset1"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ToxicTurtleOffset1_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ToxicTurtleOffset1_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.EnduringToxaphoresId,
                MycovariantIds.HyphalResistanceTransferId,
                MycovariantIds.SeptalAlarmId,
                MycovariantIds.ChemotacticMycotoxinsId,
                MycovariantIds.BallistosporeDischargeIIIId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Resilient_Shell_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["CMP_Defense_ResilientShell_Easy"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ResilientShell_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ResilientShell_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.HyphalResistanceTransferId,
                MycovariantIds.SeptalAlarmId,
                MycovariantIds.MycelialBastionIIIId,
                MycovariantIds.MycelialBastionIIId,
                MycovariantIds.MycelialBastionIId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Reclaim_Shell_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["CMP_Defense_ReclaimShell_Easy"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ReclaimShell_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_ReclaimShell_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.ReclamationRhizomorphsId,
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.HyphalResistanceTransferId,
                MycovariantIds.SeptalAlarmId,
                MycovariantIds.MycelialBastionIIIId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Scavenger_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["CMP_Reclaim_Scavenger_Easy"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_Scavenger_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_Scavenger_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Equal(
            campaign.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)),
            treatment.GetMycovariantPreferences().Select(preference => (preference.MycovariantIds.Single(), preference.Priority)));
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.ReclamationRhizomorphsId,
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.PlasmidBountyIIIId,
                MycovariantIds.PlasmidBountyIIId,
                MycovariantIds.PlasmidBountyId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    [Fact]
    public void Infiltration_Surge_mycovariant_experiment_changes_only_the_preference_plan()
    {
        var campaign = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.CampaignStrategiesByName["CMP_Reclaim_InfiltrationSurge_Easy"]);
        var control = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_InfiltrationSurge_CategoryControl"]);
        var treatment = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_Campaign_InfiltrationSurge_CuratedMycovariants"]);

        AssertStrategyConfigurationEqualExceptMycovariants(campaign, control);
        AssertStrategyConfigurationEqualExceptMycovariants(control, treatment);
        Assert.Single(control.GetMycovariantPreferences());
        Assert.True(control.GetMycovariantPreferences()[0].IsCategoryDerived);
        Assert.Equal(
            new[]
            {
                MycovariantIds.ReclamationRhizomorphsId,
                MycovariantIds.NecrophoricAdaptation,
                MycovariantIds.HyphalDrawId,
                MycovariantIds.AggressotropicConduitIIIId,
                MycovariantIds.AggressotropicConduitIIId
            },
            treatment.GetMycovariantPreferences().SelectMany(preference => preference.MycovariantIds));
    }

    private static void AssertStrategyConfigurationEqualExceptMycovariants(
        ParameterizedSpendingStrategy expected,
        ParameterizedSpendingStrategy actual)
    {
        Assert.Equal(expected.PrioritizeHighTier, actual.PrioritizeHighTier);
        Assert.Equal(expected.MaxTier, actual.MaxTier);
        Assert.Equal(expected.PriorityMutationCategories, actual.PriorityMutationCategories);
        Assert.Equal(
            expected.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)),
            actual.TargetMutationGoals.Select(goal => (goal.MutationId, goal.TargetLevel)));
        Assert.Equal(expected.SurgePriorityIds, actual.SurgePriorityIds);
        Assert.Equal(expected.SurgeAttemptTurnFrequency, actual.SurgeAttemptTurnFrequency);
        Assert.Equal(expected.EconomyProfile, actual.EconomyProfile);
        Assert.Equal(expected.ExcludedMutationIds.OrderBy(id => id), actual.ExcludedMutationIds.OrderBy(id => id));
        Assert.Equal(expected.StartingSporeEdgeOffset, actual.StartingSporeEdgeOffset);
    }

    [Fact]
    public void Campaign_progression_board_presets_only_use_registered_campaign_strategies()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var presetDir = Path.Combine(repoRoot, "FungusToast.Unity", "Assets", "Configs", "Board Presets");
        var strategyNames = Directory
            .EnumerateFiles(presetDir, "*.asset", SearchOption.TopDirectoryOnly)
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"strategyName:\s*([^\r\n]+)")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value.Trim()))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        Assert.NotEmpty(strategyNames);
        Assert.All(strategyNames, name => Assert.True(
            AIRoster.CampaignStrategiesByName.ContainsKey(name),
            $"Board preset references strategy '{name}', which is not registered in the Campaign set."));
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
