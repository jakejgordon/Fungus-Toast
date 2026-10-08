using System.Text.RegularExpressions;
using FungusToast.Core.ContentProfiles;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Tests.ContentProfiles.Coverage;

/// <summary>
/// Finds which player-facing strategies plausibly belong with each tagged content item, using
/// the matching rules in docs/second-level/AI_CONTENT_TAGS.md. A match scoring at least
/// <see cref="CandidateThreshold"/> is a candidate and needs a recorded decision.
/// </summary>
internal static class ContentCoverageAnalyzer
{
    // Scoring weights. Retuning these changes the candidate set, which the decision tests then
    // report like any other change (new candidates need decisions, vanished ones are orphaned).
    public const int CandidateThreshold = 2;
    private const int SameJobTopPoints = 2;
    private const int SameJobLeadingPoints = 1;
    private const int LeadingCapabilityCount = 3;
    // An amplifier directly strengthens something the plan already invested in (Filament
    // Overdrive for a Tendril plan), so it is a full candidate on its own.
    private const int AmplifiesPoints = 2;
    private const int FeedsPlanPoints = 2;
    private const int FedByPlanPoints = 1;
    // Salvage: the content uses a condition the plan already creates (Detrital Enzymes for a plan
    // that leaves corpses). Safe to suggest, because it never asks the plan to make more of it.
    private const int SalvagesPlanOutputPoints = 1;
    // Related job: same capability family as the plan's top capability (conditional growth for a
    // growth-led plan), but not the same capability.
    private const int RelatedJobPoints = 1;

    private static readonly Dictionary<ContentCapability, string> CapabilityFamilies = new()
    {
        [ContentCapability.BaseGrowth] = "Territory",
        [ContentCapability.DiagonalGrowth] = "Territory",
        [ContentCapability.ConditionalGrowth] = "Territory",
        [ContentCapability.RemotePlacement] = "Territory",
        [ContentCapability.Repositioning] = "Territory",
        [ContentCapability.DecayResistance] = "Survival",
        [ContentCapability.ResistantCells] = "Survival",
        [ContentCapability.ToxinCleanup] = "Survival",
        [ContentCapability.SelfReclamation] = "Death and corpses",
        [ContentCapability.CorpseCapture] = "Death and corpses",
        [ContentCapability.CorpseDenial] = "Death and corpses",
        [ContentCapability.Composting] = "Death and corpses",
        [ContentCapability.ToxinPlacement] = "Offense",
        [ContentCapability.ToxinLongevity] = "Offense",
        [ContentCapability.ToxinMobility] = "Offense",
        [ContentCapability.DirectKill] = "Offense",
        [ContentCapability.LeaderFocus] = "Offense",
        [ContentCapability.PointIncome] = "Economy",
        [ContentCapability.FreeUpgrades] = "Economy",
        [ContentCapability.TreePivot] = "Economy",
    };

    // Conditions that creating another condition implies: a dying cell leaves a corpse.
    private static readonly Dictionary<BoardCondition, BoardCondition> ImpliedConditions = new()
    {
        [BoardCondition.OwnCellDeaths] = BoardCondition.OwnDeadCells,
        [BoardCondition.EnemyCellsKilledByYou] = BoardCondition.EnemyDeadCells,
    };

    // Plan weights: earlier goals define the plan more than later ones; a bridge goal (bought only
    // to the level a later goal requires) barely counts; surges and explicit Mycovariant
    // preferences count at half weight. Category-derived Mycovariant sets do not count at all,
    // because they express almost no intent.
    private const double GoalPositionDecay = 0.25;
    private const double BridgeGoalWeight = 0.2;
    private const double SurgePriorityWeight = 0.5;
    private const double ExplicitMycovariantWeight = 0.5;

    private static readonly Regex TierSuffix = new(@"\s+(I|II|III)$");

    public sealed record ContentItem(CoverageContent Content, string Name, ContentProfile Profile, IReadOnlySet<string> MemberKeys);

    public sealed record Candidate(
        ContentItem Item,
        IReadOnlyList<PlayerFacingStrategies.Entry> Strategies,
        int Score,
        IReadOnlyList<string> Reasons,
        IReadOnlyList<string> Context);

    public sealed record Result(IReadOnlyList<ContentItem> Items, IReadOnlyList<Candidate> Candidates);

    public static Result Analyze()
    {
        var items = LoadItems();
        var plans = PlayerFacingStrategies.Load().Select(BuildPlan).ToList();

        var candidates = new List<Candidate>();
        foreach (var item in items)
        {
            // Strategies with identical plans get identical matches, so they share one candidate.
            foreach (var group in plans.GroupBy(plan => plan.Signature))
            {
                var plan = group.First();
                var match = Evaluate(item, plan);
                if (match == null || match.Value.Score < CandidateThreshold)
                {
                    continue;
                }

                candidates.Add(new Candidate(
                    item,
                    group.Select(p => p.Entry).OrderBy(e => e.Label, StringComparer.Ordinal).ToList(),
                    match.Value.Score,
                    match.Value.Reasons,
                    match.Value.Context));
            }
        }

        return new Result(items, candidates);
    }

    private static IReadOnlyList<ContentItem> LoadItems()
    {
        var mutations = MutationRepository.All.Values
            .Where(m => m.Profile != null)
            .Select(m => new ContentItem(
                CoverageContent.Mutation(m.Id),
                m.Name,
                m.Profile!,
                new HashSet<string> { CoverageContent.Mutation(m.Id).Key }));

        // Tiers of one Mycovariant share a profile and are reviewed once, under the tier-I id.
        var mycovariantFamilies = MycovariantRepository.All
            .Where(m => m.Profile != null)
            .GroupBy(m => TierSuffix.Replace(m.Name, string.Empty))
            .Select(family =>
            {
                var members = family.ToList();
                var representative = members.FirstOrDefault(m => m.Name.EndsWith(" I", StringComparison.Ordinal)) ?? members[0];
                return new ContentItem(
                    CoverageContent.Mycovariant(representative.Id),
                    members.Count > 1 ? $"{family.Key} I–{TierSuffix.Match(members.Last().Name).Groups[1].Value}" : representative.Name,
                    representative.Profile!,
                    members.Select(m => CoverageContent.Mycovariant(m.Id).Key).ToHashSet());
            });

        return mutations.Concat(mycovariantFamilies)
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToList();
    }

    private sealed class Plan
    {
        public required PlayerFacingStrategies.Entry Entry { get; init; }
        public required string Signature { get; init; }
        public Dictionary<ContentCapability, double> CapabilityWeights { get; } = new();
        public HashSet<string> InPlan { get; } = new();
        public Dictionary<BoardCondition, HashSet<string>> Needs { get; } = new();
        public Dictionary<BoardCondition, HashSet<string>> Uses { get; } = new();
        public Dictionary<BoardCondition, HashSet<string>> Creates { get; } = new();
        public Dictionary<BoardCondition, HashSet<string>> Removes { get; } = new();

        public List<ContentCapability> RankedCapabilities => CapabilityWeights
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => pair.Key)
            .ToList();

        public void Add(string key, ContentProfile profile, double weight)
        {
            InPlan.Add(key);
            foreach (var capability in profile.Capabilities)
            {
                CapabilityWeights[capability] = CapabilityWeights.GetValueOrDefault(capability) + weight;
            }

            Record(Needs, profile.NeededConditions, key);
            Record(Uses, profile.UsedConditions, key);
            Record(Creates, WithImplied(profile.CreatedConditions), key);
            Record(Removes, profile.RemovedConditions, key);
        }

        private static void Record(Dictionary<BoardCondition, HashSet<string>> map, IEnumerable<BoardCondition> conditions, string key)
        {
            foreach (var condition in conditions)
            {
                if (!map.TryGetValue(condition, out var keys))
                {
                    keys = new HashSet<string>();
                    map[condition] = keys;
                }

                keys.Add(key);
            }
        }
    }

    private static Plan BuildPlan(PlayerFacingStrategies.Entry entry)
    {
        var strategy = entry.Strategy;
        var goals = strategy.TargetMutationGoals.ToList();
        var explicitMycovariants = strategy.GetMycovariantPreferences()
            .Where(preference => !preference.IsCategoryDerived)
            .SelectMany(preference => preference.MycovariantIds)
            .Distinct()
            .ToList();

        var plan = new Plan
        {
            Entry = entry,
            Signature = string.Join(",", goals.Select(goal => $"{goal.MutationId}:{goal.TargetLevel}"))
                + "|" + string.Join(",", strategy.SurgePriorityIds)
                + "|" + string.Join(",", explicitMycovariants.OrderBy(id => id)),
        };

        for (int i = 0; i < goals.Count; i++)
        {
            if (!MutationRepository.All.TryGetValue(goals[i].MutationId, out var mutation))
            {
                continue;
            }

            var key = CoverageContent.Mutation(mutation.Id).Key;
            var isBridge = goals[i].TargetLevel.HasValue && goals.Skip(i + 1).Any(later =>
                MutationRepository.All.TryGetValue(later.MutationId, out var laterMutation)
                && laterMutation.Prerequisites.Any(p => p.MutationId == mutation.Id && p.RequiredLevel == goals[i].TargetLevel));
            var weight = (isBridge ? BridgeGoalWeight : 1.0) / (1.0 + i * GoalPositionDecay);
            if (mutation.Profile != null)
            {
                plan.Add(key, mutation.Profile, weight);
            }
            else
            {
                plan.InPlan.Add(key);
            }
        }

        foreach (var id in strategy.SurgePriorityIds)
        {
            if (MutationRepository.All.TryGetValue(id, out var surge) && surge.Profile != null)
            {
                plan.Add(CoverageContent.Mutation(id).Key, surge.Profile, SurgePriorityWeight);
            }
        }

        foreach (var id in explicitMycovariants)
        {
            var mycovariant = MycovariantRepository.All.FirstOrDefault(m => m.Id == id);
            var key = CoverageContent.Mycovariant(id).Key;
            if (mycovariant?.Profile != null)
            {
                plan.Add(key, mycovariant.Profile, ExplicitMycovariantWeight);
            }
            else
            {
                plan.InPlan.Add(key);
            }
        }

        return plan;
    }

    private static (int Score, IReadOnlyList<string> Reasons, IReadOnlyList<string> Context)? Evaluate(ContentItem item, Plan plan)
    {
        if (item.MemberKeys.Any(plan.InPlan.Contains))
        {
            return null; // already part of the plan
        }

        var profile = item.Profile;
        var ranked = plan.RankedCapabilities;
        var reasons = new List<string>();
        int score = 0;

        if (ranked.Count > 0 && profile.Capabilities.Contains(ranked[0]))
        {
            score += SameJobTopPoints;
            reasons.Add($"same job: {ranked[0]} (top)");
        }
        else
        {
            var leading = profile.Capabilities.Where(ranked.Skip(1).Take(LeadingCapabilityCount - 1).Contains).ToList();
            if (leading.Count > 0)
            {
                score += SameJobLeadingPoints;
                reasons.AddRange(leading.Select(capability => $"same job: {capability}"));
            }
            else if (ranked.Count > 0)
            {
                var related = profile.Capabilities
                    .Where(capability => CapabilityFamilies[capability] == CapabilityFamilies[ranked[0]])
                    .ToList();
                if (related.Count > 0)
                {
                    score += RelatedJobPoints;
                    reasons.AddRange(related.Select(capability => $"related job: {capability} ({CapabilityFamilies[capability]}, like top {ranked[0]})"));
                }
            }
        }

        var amplified = profile.AmplifiedCapabilities.Where(plan.CapabilityWeights.ContainsKey).ToList();
        if (amplified.Count > 0)
        {
            score += AmplifiesPoints;
            reasons.AddRange(amplified.Select(capability => $"amplifies: {capability}"));
        }

        var feeds = WithImplied(profile.CreatedConditions).Where(plan.Needs.ContainsKey).ToList();
        if (feeds.Count > 0)
        {
            score += FeedsPlanPoints;
            reasons.AddRange(feeds.Select(condition => $"feeds: creates {condition}"));
        }

        var fedBy = profile.NeededConditions.Where(plan.Creates.ContainsKey).ToList();
        if (fedBy.Count > 0)
        {
            score += FedByPlanPoints;
            reasons.AddRange(fedBy.Select(condition => $"fed by: needs {condition}"));
        }

        var salvages = profile.UsedConditions.Where(plan.Creates.ContainsKey).ToList();
        if (salvages.Count > 0)
        {
            score += SalvagesPlanOutputPoints;
            reasons.AddRange(salvages.Select(condition => $"salvages: uses {condition} the plan creates"));
        }

        return (score, reasons.OrderBy(r => r, StringComparer.Ordinal).ToList(), BuildContext(item, plan));
    }

    private static IEnumerable<BoardCondition> WithImplied(IEnumerable<BoardCondition> conditions) =>
        conditions
            .SelectMany(condition => ImpliedConditions.TryGetValue(condition, out var implied)
                ? new[] { condition, implied }
                : new[] { condition })
            .Distinct();

    private static IReadOnlyList<string> BuildContext(ContentItem item, Plan plan)
    {
        var profile = item.Profile;
        var context = new List<string>();
        var related = TreeRelatives(item);

        foreach (var condition in profile.RemovedConditions)
        {
            var reliers = Lookup(plan.Needs, condition).Concat(Lookup(plan.Uses, condition));
            if (reliers.Any(key => !related.Contains(key)))
            {
                context.Add($"tension: removes {condition}, which the plan relies on");
            }
        }

        foreach (var condition in profile.NeededConditions.Concat(profile.UsedConditions).Distinct())
        {
            if (Lookup(plan.Removes, condition).Any(key => !related.Contains(key)))
            {
                context.Add($"tension: the plan removes {condition}, which this relies on");
            }
            else if (Lookup(plan.Needs, condition).Concat(Lookup(plan.Uses, condition)).Any())
            {
                context.Add($"shares: {condition}");
            }
        }

        return context.Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
    }

    private static IEnumerable<string> Lookup(Dictionary<BoardCondition, HashSet<string>> map, BoardCondition condition) =>
        map.TryGetValue(condition, out var keys) ? keys : Enumerable.Empty<string>();

    /// <summary>
    /// Mutations in the item's prerequisite chain, in either direction. The tree forces those
    /// pairings, so they are never reported as tension.
    /// </summary>
    private static HashSet<string> TreeRelatives(ContentItem item)
    {
        var related = new HashSet<string>();
        if (!item.Content.IsMutation)
        {
            return related;
        }

        foreach (var ancestor in Ancestors(item.Content.Id))
        {
            related.Add(CoverageContent.Mutation(ancestor).Key);
        }

        foreach (var mutation in MutationRepository.All.Values)
        {
            if (Ancestors(mutation.Id).Contains(item.Content.Id))
            {
                related.Add(CoverageContent.Mutation(mutation.Id).Key);
            }
        }

        return related;
    }

    private static HashSet<int> Ancestors(int mutationId)
    {
        var seen = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(mutationId);
        while (pending.Count > 0)
        {
            if (!MutationRepository.All.TryGetValue(pending.Pop(), out var mutation))
            {
                continue;
            }

            var parents = mutation.Prerequisites.Select(p => p.MutationId)
                .Concat(mutation.AnyPrerequisiteGroups.SelectMany(group => group.Alternatives.Select(p => p.MutationId)));
            foreach (var parent in parents)
            {
                if (seen.Add(parent))
                {
                    pending.Push(parent);
                }
            }
        }

        return seen;
    }
}
