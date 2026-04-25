using System.Text.RegularExpressions;
using FungusToast.Core.AI;

namespace FungusToast.Core.Tests.AI;

public class StrategyCatalogTests
{
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
