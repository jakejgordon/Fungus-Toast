using System.Text;
using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.ContentProfiles;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Tests.ContentProfiles;

/// <summary>
/// Keeps the generated content catalogs in FungusToast.Core/docs in step with the definitions.
/// The catalogs are reading surfaces for people and agents; the definitions stay the source of
/// truth. Set FUNGUS_UPDATE_CONTENT_CATALOG=1 to rewrite them after changing content or tags.
/// </summary>
public class ContentCatalogTests
{
    private const string UpdateVariable = "FUNGUS_UPDATE_CONTENT_CATALOG";
    private const int MaxListedStrategies = 8;

    private static readonly StrategySetEnum[] PlayerFacingSets = { StrategySetEnum.Proven, StrategySetEnum.Campaign };

    [Fact]
    public void Mutation_catalog_is_current() =>
        AssertCurrent("MUTATION_CATALOG.md", ContentCatalogWriter.BuildMutationCatalog());

    [Fact]
    public void Mycovariant_catalog_is_current() =>
        AssertCurrent("MYCOVARIANT_CATALOG.md", ContentCatalogWriter.BuildMycovariantCatalog());

    [Fact]
    public void Content_tag_index_is_current() =>
        AssertCurrent("CONTENT_TAG_INDEX.md", ContentCatalogWriter.BuildTagIndex());

    private static void AssertCurrent(string fileName, string generated)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../FungusToast.Core/docs", fileName));
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(path, generated);
        }

        var committed = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : string.Empty;
        Assert.True(committed == generated,
            $"{fileName} is out of date with the content definitions. Regenerate it with "
            + $"`{UpdateVariable}=1 dotnet test FungusToast.Core.Tests --filter ContentCatalogTests` and commit the result.");
    }

    internal static class ContentCatalogWriter
    {
        private const string TagsDocLink = "[AI_CONTENT_TAGS.md](second-level/AI_CONTENT_TAGS.md)";

        public static string BuildMutationCatalog()
        {
            var usage = StrategyUsage.Build();
            var mutations = MutationRepository.All.Values
                .OrderBy(m => m.Category)
                .ThenBy(m => m.TierNumber)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList();

            var sb = new StringBuilder();
            AppendHeader(sb, "Mutation Catalog", "mutation");
            sb.Append("Strategy columns count player-facing strategies (Proven and Campaign sets, excluding Retired) that name the mutation as an ordered mutation goal or a surge priority.\n\n");

            sb.Append("## Summary\n\n");
            sb.Append("| Mutation | Category | Tier | Capabilities | Goal in | Surge priority in |\n");
            sb.Append("|---|---|---|---|---|---|\n");
            foreach (var mutation in mutations)
            {
                sb.Append($"| [{mutation.Name}](#{Anchor(mutation.Name)}) | {mutation.Category} | {mutation.TierNumber} | {Join(mutation.Profile?.Capabilities)} | {usage.GoalCount(mutation.Id)} | {usage.SurgeCount(mutation.Id)} |\n");
            }

            foreach (var category in mutations.Select(m => m.Category).Distinct())
            {
                sb.Append($"\n## {category}\n");
                foreach (var mutation in mutations.Where(m => m.Category == category))
                {
                    sb.Append($"\n### {mutation.Name}\n\n");
                    sb.Append(Plain(mutation.DescriptionSections.Summary)).Append("\n\n");
                    sb.Append("| | |\n|---|---|\n");
                    var kind = mutation.IsSurge ? $"Surge, {mutation.SurgeDuration} rounds" : "Upgrade";
                    sb.Append($"| Id | {mutation.Id} |\n");
                    sb.Append($"| Category · tier | {mutation.Category} · Tier {mutation.TierNumber} |\n");
                    sb.Append($"| Kind | {kind} · max level {mutation.MaxLevel} |\n");
                    sb.Append($"| Requires | {DescribeRequirements(mutation)} |\n");
                    sb.Append($"| Unlocks | {JoinNames(mutation.Children.Select(child => child.Name))} |\n");
                    AppendProfileRows(sb, mutation.Profile);
                    sb.Append($"| Goal in | {usage.DescribeGoals(mutation.Id)} |\n");
                    if (mutation.IsSurge)
                    {
                        sb.Append($"| Surge priority in | {usage.DescribeSurges(mutation.Id)} |\n");
                    }
                }
            }

            return sb.ToString();
        }

        public static string BuildMycovariantCatalog()
        {
            var usage = StrategyUsage.Build();
            var all = MycovariantRepository.All
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList();
            var tagged = all.Where(m => !m.IsBait).ToList();

            var sb = new StringBuilder();
            AppendHeader(sb, "Mycovariant Catalog", "Mycovariant");
            sb.Append("\"Preferred by\" counts player-facing strategies (Proven and Campaign sets, excluding Retired) that name the Mycovariant in an explicit, ordered draft preference. \"In category sets of\" counts those that only reach it through a whole-category preference, where AI score picks among equal options.\n\n");

            sb.Append("## Summary\n\n");
            sb.Append("| Mycovariant | Category | Type | Capabilities | Preferred by | In category sets of |\n");
            sb.Append("|---|---|---|---|---|---|\n");
            foreach (var mycovariant in tagged)
            {
                sb.Append($"| [{mycovariant.Name}](#{Anchor(mycovariant.Name)}) | {mycovariant.Category} | {mycovariant.Type} | {Join(mycovariant.Profile?.Capabilities)} | {usage.ExplicitPreferenceCount(mycovariant.Id)} | {usage.CategoryPreferenceCount(mycovariant.Id)} |\n");
            }

            foreach (var category in tagged.Select(m => m.Category).Distinct())
            {
                sb.Append($"\n## {category}\n");
                foreach (var mycovariant in tagged.Where(m => m.Category == category))
                {
                    sb.Append($"\n### {mycovariant.Name}\n\n");
                    sb.Append(Plain(mycovariant.Description)).Append("\n\n");
                    sb.Append("| | |\n|---|---|\n");
                    sb.Append($"| Id | {mycovariant.Id} |\n");
                    sb.Append($"| Category · type | {mycovariant.Category} · {mycovariant.Type} |\n");
                    sb.Append($"| Availability | {DescribeAvailability(mycovariant)} |\n");
                    AppendProfileRows(sb, mycovariant.Profile);
                    sb.Append($"| Preferred by | {usage.DescribeExplicitPreferences(mycovariant.Id)} |\n");
                    sb.Append($"| In category sets of | {usage.CategoryPreferenceCount(mycovariant.Id)} strategies |\n");
                }
            }

            var bait = all.Where(m => m.IsBait).ToList();
            if (bait.Count > 0)
            {
                sb.Append("\n## Bait cards (untagged)\n\n");
                sb.Append("Bait cards are draft traps aimed at the leading AI, not build pieces, so they carry no content profile and stay out of the coverage review.\n\n");
                foreach (var mycovariant in bait)
                {
                    sb.Append($"- **{mycovariant.Name}** (id {mycovariant.Id}): {Plain(mycovariant.Description)}\n");
                }
            }

            return sb.ToString();
        }

        public static string BuildTagIndex()
        {
            // Tiers of one Mycovariant share a profile, so each family appears once as "Name I–III".
            var tierSuffix = new Regex(@"\s+(I|II|III)$");
            var mycovariantFamilies = MycovariantRepository.All
                .Where(m => m.Profile != null)
                .GroupBy(m => tierSuffix.Replace(m.Name, string.Empty))
                .Select(family => (
                    Name: family.Count() > 1 ? $"{family.Key} I–{tierSuffix.Match(family.Last().Name).Groups[1].Value}" : family.First().Name,
                    Kind: "Mycovariant",
                    Profile: family.First().Profile!));
            var items = MutationRepository.All.Values
                .Where(m => m.Profile != null)
                .Select(m => (Name: m.Name, Kind: "mutation", Profile: m.Profile!))
                .Concat(mycovariantFamilies)
                .OrderBy(item => item.Name, StringComparer.Ordinal)
                .ToList();

            var sb = new StringBuilder();
            sb.Append("# Content Tag Index\n\n");
            sb.Append("> **Generated** from the mutation and Mycovariant definitions by `ContentCatalogTests`. Do not edit by hand.\n");
            sb.Append($"> Regenerate with `FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentCatalogTests`. Tag meanings: {TagsDocLink}.\n\n");
            sb.Append("Every tag, with the content that carries it. Mycovariants are in *italics*; a tier family is listed once. Use this to answer questions such as \"what creates own dead cells?\" without reading the factories.\n\n");

            sb.Append("## Capabilities\n\n");
            sb.Append("| Capability | Has it | Amplifies it |\n|---|---|---|\n");
            foreach (var capability in Enum.GetValues<ContentCapability>())
            {
                sb.Append($"| `{capability}` | {Names(items, i => i.Profile.Capabilities.Contains(capability))} | {Names(items, i => i.Profile.AmplifiedCapabilities.Contains(capability))} |\n");
            }

            sb.Append("\n## Board conditions\n\n");
            sb.Append("| Condition | Needs | Uses | Creates | Removes |\n|---|---|---|---|---|\n");
            foreach (var condition in Enum.GetValues<BoardCondition>())
            {
                sb.Append($"| `{condition}` | {Names(items, i => i.Profile.NeededConditions.Contains(condition))} | {Names(items, i => i.Profile.UsedConditions.Contains(condition))} | {Names(items, i => i.Profile.CreatedConditions.Contains(condition))} | {Names(items, i => i.Profile.RemovedConditions.Contains(condition))} |\n");
            }

            return sb.ToString();
        }

        private static void AppendHeader(StringBuilder sb, string title, string noun)
        {
            sb.Append($"# {title}\n\n");
            sb.Append($"> **Generated** from the {noun} definitions by `ContentCatalogTests`. Do not edit by hand: change the definition, then regenerate with\n");
            sb.Append("> `FUNGUS_UPDATE_CONTENT_CATALOG=1 dotnet test FungusToast.Core.Tests --filter ContentCatalogTests`.\n");
            sb.Append($"> Tag meanings and authoring rules: {TagsDocLink}. Cross-content lookup by tag: [CONTENT_TAG_INDEX.md](CONTENT_TAG_INDEX.md).\n\n");
        }

        private static void AppendProfileRows(StringBuilder sb, ContentProfile? profile)
        {
            sb.Append($"| Capabilities | {Join(profile?.Capabilities)} |\n");
            sb.Append($"| Amplifies | {Join(profile?.AmplifiedCapabilities)} |\n");
            sb.Append($"| Needs | {Join(profile?.NeededConditions)} |\n");
            sb.Append($"| Uses | {Join(profile?.UsedConditions)} |\n");
            sb.Append($"| Creates | {Join(profile?.CreatedConditions)} |\n");
            sb.Append($"| Removes | {Join(profile?.RemovedConditions)} |\n");
        }

        private static string DescribeRequirements(Mutation mutation)
        {
            var parts = new List<string>();
            parts.AddRange(mutation.Prerequisites.Select(p => $"{MutationName(p.MutationId)} {p.RequiredLevel}"));
            parts.AddRange(mutation.AnyPrerequisiteGroups.Select(group =>
                "one of " + string.Join(" or ", group.Alternatives.Select(p => $"{MutationName(p.MutationId)} {p.RequiredLevel}"))));
            parts.AddRange(mutation.CategoryInvestmentPrerequisites.Select(p =>
                $"{p.RequiredLevelsPerCategory}+ levels in each of {p.RequiredCategoryCount} categories at {p.Tier}"));
            return parts.Count == 0 ? "—" : string.Join("; ", parts);
        }

        private static string DescribeAvailability(Mycovariant mycovariant)
        {
            var parts = new List<string> { mycovariant.IsUniversal ? "Universal (stays in the pool)" : "Unique (leaves the pool once drafted)" };
            if (mycovariant.RequiredMoldinessUnlockLevel > 0)
            {
                parts.Add($"unlocks at Moldiness level {mycovariant.RequiredMoldinessUnlockLevel}");
            }

            return string.Join(" · ", parts);
        }

        private static string MutationName(int id) =>
            MutationRepository.All.TryGetValue(id, out var mutation) ? mutation.Name : $"Unknown mutation {id}";

        private static string Names(IEnumerable<(string Name, string Kind, ContentProfile Profile)> items,
            Func<(string Name, string Kind, ContentProfile Profile), bool> predicate)
        {
            var names = items.Where(predicate).Select(i => i.Kind == "Mycovariant" ? $"*{i.Name}*" : i.Name).ToList();
            return names.Count == 0 ? "—" : string.Join(", ", names);
        }

        private static string Join<T>(IEnumerable<T>? values)
        {
            var list = values?.Select(v => v!.ToString()).ToList() ?? new List<string?>();
            return list.Count == 0 ? "—" : string.Join(", ", list);
        }

        private static string JoinNames(IEnumerable<string> names)
        {
            var list = names.OrderBy(n => n, StringComparer.Ordinal).ToList();
            return list.Count == 0 ? "—" : string.Join(", ", list);
        }

        private static string Plain(string text)
        {
            var noTags = Regex.Replace(text ?? string.Empty, "<[^>]+>", string.Empty);
            var oneLine = Regex.Replace(noTags, @"\s*\n\s*", " ").Trim();
            return oneLine.Replace("|", "\\|");
        }

        private static string Anchor(string heading) =>
            Regex.Replace(heading.ToLowerInvariant(), @"[^a-z0-9 \-]", string.Empty).Replace(' ', '-');
    }

    private sealed class StrategyUsage
    {
        private readonly Dictionary<int, List<string>> goals = new();
        private readonly Dictionary<int, List<string>> surges = new();
        private readonly Dictionary<int, List<string>> explicitPreferences = new();
        private readonly Dictionary<int, int> categoryPreferences = new();

        public static StrategyUsage Build()
        {
            Assert.NotEmpty(AIRoster.ProvenStrategies); // registers the authored sets
            var usage = new StrategyUsage();
            foreach (var set in PlayerFacingSets)
            {
                var definitions = StrategyRegistry.GetDefinitions(set)
                    .Where(definition => definition.Metadata.Lifecycle != StrategyLifecycle.Retired)
                    .ToList();
                string DisplayName(StrategyDefinition definition) =>
                    string.IsNullOrWhiteSpace(definition.Metadata.FriendlyName)
                        ? definition.Metadata.StrategyName
                        : definition.Metadata.FriendlyName;
                // Several campaign difficulty variants share a display name; the internal name tells them apart.
                var sharedDisplayNames = definitions
                    .GroupBy(DisplayName)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToHashSet();

                foreach (var definition in definitions)
                {
                    if (definition.Strategy is not ParameterizedSpendingStrategy strategy)
                    {
                        continue;
                    }

                    var displayName = DisplayName(definition);
                    var label = sharedDisplayNames.Contains(displayName)
                        ? $"{displayName} [`{definition.Metadata.StrategyName}`] ({set})"
                        : $"{displayName} ({set})";

                    foreach (var id in strategy.TargetMutationGoals.Select(goal => goal.MutationId).Distinct())
                    {
                        Add(usage.goals, id, label);
                    }

                    foreach (var id in strategy.SurgePriorityIds.Distinct())
                    {
                        Add(usage.surges, id, label);
                    }

                    var preferences = strategy.GetMycovariantPreferences();
                    var explicitIds = preferences.Where(p => !p.IsCategoryDerived).SelectMany(p => p.MycovariantIds).Distinct().ToHashSet();
                    foreach (var id in explicitIds)
                    {
                        Add(usage.explicitPreferences, id, label);
                    }

                    foreach (var id in preferences.Where(p => p.IsCategoryDerived).SelectMany(p => p.MycovariantIds).Distinct()
                                 .Where(id => !explicitIds.Contains(id)))
                    {
                        usage.categoryPreferences[id] = usage.categoryPreferences.TryGetValue(id, out var count) ? count + 1 : 1;
                    }
                }
            }

            return usage;
        }

        public int GoalCount(int mutationId) => Count(goals, mutationId);
        public int SurgeCount(int mutationId) => Count(surges, mutationId);
        public int ExplicitPreferenceCount(int mycovariantId) => Count(explicitPreferences, mycovariantId);
        public int CategoryPreferenceCount(int mycovariantId) => categoryPreferences.TryGetValue(mycovariantId, out var count) ? count : 0;

        public string DescribeGoals(int mutationId) => Describe(goals, mutationId);
        public string DescribeSurges(int mutationId) => Describe(surges, mutationId);
        public string DescribeExplicitPreferences(int mycovariantId) => Describe(explicitPreferences, mycovariantId);

        private static void Add(Dictionary<int, List<string>> map, int id, string label)
        {
            if (!map.TryGetValue(id, out var labels))
            {
                labels = new List<string>();
                map[id] = labels;
            }

            labels.Add(label);
        }

        private static int Count(Dictionary<int, List<string>> map, int id) =>
            map.TryGetValue(id, out var labels) ? labels.Count : 0;

        private static string Describe(Dictionary<int, List<string>> map, int id)
        {
            if (!map.TryGetValue(id, out var labels) || labels.Count == 0)
            {
                return "none";
            }

            var sorted = labels.OrderBy(l => l, StringComparer.Ordinal).ToList();
            var shown = string.Join(", ", sorted.Take(MaxListedStrategies));
            return sorted.Count > MaxListedStrategies
                ? $"{sorted.Count}: {shown}, and {sorted.Count - MaxListedStrategies} more"
                : $"{sorted.Count}: {shown}";
        }
    }
}
