using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.AI;

public class StrategyCatalogTests
{
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
    }

    [Fact]
    public void Ecology_reclaimer_diagnosis_variants_isolate_necrosporulation_and_autolytic_surge()
    {
        var noNecro = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyReclaimer_NoNecro"]);
        var noAutolytic = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyReclaimer_NoAutolytic"]);
        var delayedNecro = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName["TST_EcologyReclaimer_DelayedNecro"]);

        Assert.Contains(MutationIds.Necrosporulation, noNecro.ExcludedMutationIds);
        Assert.DoesNotContain(noNecro.TargetMutationGoals, goal => goal.MutationId == MutationIds.Necrosporulation);

        Assert.Contains(MutationIds.HyphalSurge, noAutolytic.ExcludedMutationIds);
        Assert.DoesNotContain(noAutolytic.TargetMutationGoals, goal => goal.MutationId == MutationIds.HyphalSurge);

        int detritalIndex = delayedNecro.TargetMutationGoals
            .Select((goal, index) => (goal, index))
            .Single(pair => pair.goal.MutationId == MutationIds.DetritalEnzymes).index;
        int necroIndex = delayedNecro.TargetMutationGoals
            .Select((goal, index) => (goal, index))
            .Single(pair => pair.goal.MutationId == MutationIds.Necrosporulation).index;
        Assert.True(necroIndex > detritalIndex);
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
        var player = new Player(0, "Ontogenic AI", PlayerTypeEnum.AI) { MutationPoints = 20 };
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
    public void Campaign_catalog_entries_expose_friendly_name_and_intentions()
    {
        var entries = AIRoster.GetStrategyCatalogEntries(StrategySetEnum.Campaign);

        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.FriendlyName), $"Expected FriendlyName for {entry.StrategyName}");
            Assert.False(string.IsNullOrWhiteSpace(entry.AIPlayerIntentions), $"Expected AIPlayerIntentions for {entry.StrategyName}");
        }
    }

    [Fact]
    public void Campaign_progression_board_presets_only_use_cmp_strategy_names()
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
        Assert.All(strategyNames, name => Assert.StartsWith("CMP_", name));
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
    public void Campaign_training_profiles_use_generated_intentions()
    {
        var trainingEntry = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, "CMP_Mobility_Overextender_Training");

        Assert.NotNull(trainingEntry);
        Assert.Equal("Overextender", trainingEntry!.FriendlyName);
        Assert.Contains("growth", trainingEntry.AIPlayerIntentions, StringComparison.OrdinalIgnoreCase);
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
