using System.Text;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Candidates;
using FungusToast.Simulation.Experiments;

namespace FungusToast.Simulation.Calibration;

/// <summary>Renders the P8.1 observed-behavior comparison as a reviewable, provenance-stamped artifact.</summary>
public static class RosterBehaviorReport
{
    public const string SchemaVersion = "fungus-toast.roster-behavior.v1";

    public static string Render(
        StrategySetEnum strategySet,
        IReadOnlyList<StrategyBehaviorProfile> profiles,
        IReadOnlyList<StrategyBehaviorPair> pairs,
        CandidateCharacterizationSettings settings,
        ResolvedCodeIdentity code)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(pairs);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(code);

        var definitions = StrategyRegistry.GetDefinitions(strategySet)
            .ToDictionary(definition => definition.Strategy.StrategyName, StringComparer.Ordinal);
        var builder = new StringBuilder();
        builder.AppendLine($"# Roster behavior comparison — {strategySet}");
        builder.AppendLine();
        builder.AppendLine($"- Schema: `{SchemaVersion}`");
        builder.AppendLine($"- Strategy set: `{strategySet}` ({profiles.Count} strategies, {pairs.Count} pairs)");
        builder.AppendLine($"- Commit: `{code.Commit}`");
        builder.AppendLine($"- Simulation assembly SHA-256: `{code.SimulationAssemblySha256}`");
        builder.AppendLine($"- Core assembly SHA-256: `{code.CoreAssemblySha256}`");
        builder.AppendLine($"- Characterization: {settings.Rounds} rounds × {settings.MutationPointsPerRound} points, seed {settings.Seed}, {settings.BoardWidth}x{settings.BoardHeight} board");
        builder.AppendLine();
        builder.AppendLine("This artifact compares deterministic observed mutation spending, not authored theme or difficulty labels. High raw-build similarity identifies a redundancy-review candidate only; it is not evidence to retire either strategy. Counter claims and player-value claims require contextual matchup evidence.");
        builder.AppendLine();

        var exactMatches = pairs.Where(pair => pair.MutationDistance <= 0).ToList();
        builder.AppendLine("## Exact raw-build matches");
        builder.AppendLine();
        if (exactMatches.Count == 0)
        {
            builder.AppendLine("No pair has an identical observed raw mutation build.");
        }
        else
        {
            builder.AppendLine("These pairs have identical observed mutation builds under this script. Their separate identities may still affect reactive behavior, draft choices, surge timing, or real-game outcomes, so they remain review candidates rather than automatic merge targets.");
            builder.AppendLine();
            builder.AppendLine("| First strategy | Second strategy | Category similarity |");
            builder.AppendLine("|---|---|---:|");
            foreach (var pair in exactMatches)
                builder.AppendLine($"| {pair.FirstStrategyName} | {pair.SecondStrategyName} | {pair.CategorySimilarity:0.000} |");
        }
        builder.AppendLine();

        builder.AppendLine("## Observed profiles");
        builder.AppendLine();
        builder.AppendLine("| Strategy | Stable ID | Definition fingerprint | Mutation build | Category profile |");
        builder.AppendLine("|---|---|---|---|---|");
        foreach (var profile in profiles.OrderBy(profile => profile.StrategyName, StringComparer.Ordinal))
        {
            if (!definitions.TryGetValue(profile.StrategyName, out var definition))
                throw new InvalidOperationException($"'{profile.StrategyName}' is not registered in {strategySet}.");
            builder.AppendLine($"| {profile.StrategyName} | `{definition.StrategyId}` | `{definition.DefinitionFingerprint}` | {DescribeMutationLevels(profile.MutationLevels)} | {DescribeCategoryLevels(profile.CategoryLevels)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Pairwise similarity");
        builder.AppendLine();
        builder.AppendLine("Pairs are ordered by raw mutation-build similarity, then strategy name. Category similarity is explanatory only and can be high when strategies buy different mutations in the same categories.");
        builder.AppendLine();
        builder.AppendLine("| First strategy | Second strategy | Raw mutation similarity | Category similarity | Raw distance |");
        builder.AppendLine("|---|---|---:|---:|---:|");
        foreach (var pair in pairs)
            builder.AppendLine($"| {pair.FirstStrategyName} | {pair.SecondStrategyName} | {pair.MutationSimilarity:0.000} | {pair.CategorySimilarity:0.000} | {pair.MutationDistance:0.000} |");

        return builder.ToString();
    }

    private static string DescribeMutationLevels(IReadOnlyDictionary<int, int> levels)
        => string.Join("; ", levels
            .OrderBy(entry => entry.Key)
            .Select(entry => $"{MutationRegistry.GetById(entry.Key)?.Name ?? $"mutation-{entry.Key}"} {entry.Value}"));

    private static string DescribeCategoryLevels(IReadOnlyDictionary<MutationCategory, int> levels)
        => string.Join("; ", levels
            .Where(entry => entry.Value > 0)
            .OrderBy(entry => entry.Key)
            .Select(entry => $"{entry.Key} {entry.Value}"));
}
