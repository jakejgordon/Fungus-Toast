using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.AI;

public class StrategyCatalogTests
{
    [Theory]
    [InlineData("TST_EcologyFrontierExpansion")]
    [InlineData("TST_EcologyFrontierResilience")]
    public void Ecology_testing_strategies_begin_with_aerated_frontier(string strategyName)
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(AIRoster.TestingStrategiesByName[strategyName]);

        Assert.Equal(MutationIds.AeratedFrontier, strategy.TargetMutationGoals[0].MutationId);
        Assert.Equal(EconomyBias.IgnoreEconomy, strategy.EconomyProfile);
        Assert.Equal(MutationCategory.SubstrateEcology, strategy.PriorityMutationCategories![0]);
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
