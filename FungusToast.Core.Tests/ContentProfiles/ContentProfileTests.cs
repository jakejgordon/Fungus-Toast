using System.Text.RegularExpressions;
using FungusToast.Core.ContentProfiles;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Tests.ContentProfiles;

/// <summary>
/// Every mutation and non-Bait Mycovariant carries authoring tags for the content-to-strategy
/// coverage review. These rules keep new content from shipping untagged. How to tag:
/// FungusToast.Core/docs/second-level/AI_CONTENT_TAGS.md.
/// </summary>
public class ContentProfileTests
{
    [Fact]
    public void Every_mutation_has_a_content_profile()
    {
        var problems = MutationRepository.All.Values
            .Select(mutation => (mutation.Name, Problem: DescribeProblem(mutation.Profile)))
            .Where(entry => entry.Problem != null)
            .Select(entry => $"{entry.Name}: {entry.Problem}")
            .ToList();

        Assert.True(problems.Count == 0,
            "Mutations need a content profile (the `profile:` constructor argument). "
            + "See docs/second-level/AI_CONTENT_TAGS.md:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Every_non_bait_mycovariant_has_a_content_profile_and_bait_cards_have_none()
    {
        var problems = new List<string>();
        foreach (var mycovariant in MycovariantRepository.All)
        {
            if (mycovariant.IsBait)
            {
                if (mycovariant.Profile != null)
                {
                    problems.Add($"{mycovariant.Name}: Bait cards are draft traps, not build pieces, and stay untagged.");
                }

                continue;
            }

            var problem = DescribeProblem(mycovariant.Profile);
            if (problem != null)
            {
                problems.Add($"{mycovariant.Name}: {problem}");
            }
        }

        Assert.True(problems.Count == 0,
            "Mycovariants need a content profile (the `Profile =` initializer). "
            + "See docs/second-level/AI_CONTENT_TAGS.md:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Tier_families_share_one_profile_instance()
    {
        // Tiers of one card differ in strength, not in what they do, so they share a single
        // static profile rather than copies that can drift apart.
        var tierSuffix = new Regex(@"\s+(I|II|III)$");
        var drifted = MycovariantRepository.All
            .Where(mycovariant => !mycovariant.IsBait && tierSuffix.IsMatch(mycovariant.Name))
            .GroupBy(mycovariant => tierSuffix.Replace(mycovariant.Name, string.Empty))
            .Where(family => family.Count() > 1 && family.Select(m => m.Profile).Distinct().Count() > 1)
            .Select(family => family.Key)
            .ToList();

        Assert.True(drifted.Count == 0,
            "Each tier family should reference one shared static ContentProfile: " + string.Join(", ", drifted));
    }

    [Fact]
    public void Catch_up_surges_depend_on_falling_behind()
    {
        // MutationAITags.CatchUp is the runtime flag; the authoring tags must tell the same story.
        var inconsistent = MutationRepository.All.Values
            .Where(mutation => mutation.AITags.HasFlag(MutationAITags.CatchUp))
            .Where(mutation => mutation.Profile == null
                || !mutation.Profile.NeededConditions.Concat(mutation.Profile.UsedConditions).Contains(BoardCondition.FallingBehind))
            .Select(mutation => mutation.Name)
            .ToList();

        Assert.True(inconsistent.Count == 0,
            "CatchUp surges should list FallingBehind under Needs or Uses: " + string.Join(", ", inconsistent));
    }

    [Fact]
    public void Profile_builder_deduplicates_and_keeps_relations_separate()
    {
        var profile = ContentProfile.Of(ContentCapability.ToxinPlacement, ContentCapability.ToxinPlacement)
            .Needs(BoardCondition.OwnToxins)
            .Uses(BoardCondition.EnemyContact)
            .Creates(BoardCondition.OwnToxins)
            .Removes(BoardCondition.EnemyToxins)
            .Amplifies(ContentCapability.DirectKill);

        Assert.Equal(new[] { ContentCapability.ToxinPlacement }, profile.Capabilities);
        Assert.Equal(new[] { ContentCapability.DirectKill }, profile.AmplifiedCapabilities);
        Assert.Equal(new[] { BoardCondition.OwnToxins }, profile.NeededConditions);
        Assert.Equal(new[] { BoardCondition.EnemyContact }, profile.UsedConditions);
        Assert.Equal(new[] { BoardCondition.OwnToxins }, profile.CreatedConditions);
        Assert.Equal(new[] { BoardCondition.EnemyToxins }, profile.RemovedConditions);
    }

    private static string? DescribeProblem(ContentProfile? profile)
    {
        if (profile == null)
        {
            return "no profile";
        }

        if (profile.Capabilities.Count == 0 || profile.Capabilities.Count > ContentProfile.MaxCapabilities)
        {
            return $"needs 1 to {ContentProfile.MaxCapabilities} capabilities, has {profile.Capabilities.Count}";
        }

        var neededAndUsed = profile.NeededConditions.Intersect(profile.UsedConditions).ToList();
        if (neededAndUsed.Count > 0)
        {
            return "lists a condition under both Needs and Uses (pick one): " + string.Join(", ", neededAndUsed);
        }

        return null;
    }
}
