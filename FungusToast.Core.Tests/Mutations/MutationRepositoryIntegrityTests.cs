using FungusToast.Core.Mutations;

namespace FungusToast.Core.Tests.Mutations;

public class MutationRepositoryIntegrityTests
{
    [Fact]
    public void Repository_has_unique_ids_and_names()
    {
        var mutations = MutationRegistry.GetAll().ToList();

        Assert.Equal(34, mutations.Count);
        Assert.Equal(mutations.Count, mutations.Select(mutation => mutation.Id).Distinct().Count());
        Assert.Equal(
            mutations.Count,
            mutations.Select(mutation => mutation.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Every_mutation_is_reachable_from_a_registered_root_without_cycles()
    {
        var all = MutationRegistry.All;
        var roots = MutationRegistry.Roots;

        foreach (var mutation in all.Values)
        {
            var activePath = new HashSet<int>();
            AssertReachesRoot(mutation, all, roots, activePath);
        }
    }

    [Fact]
    public void Every_prerequisite_has_the_destination_as_a_reverse_child_edge()
    {
        var all = MutationRegistry.All;

        foreach (var mutation in all.Values)
        {
            foreach (var prerequisite in mutation.Prerequisites)
            {
                Assert.True(all.TryGetValue(prerequisite.MutationId, out var parent));
                Assert.Contains(mutation, parent!.Children);
            }
        }
    }

    private static void AssertReachesRoot(
        Mutation mutation,
        IReadOnlyDictionary<int, Mutation> all,
        IReadOnlyDictionary<int, Mutation> roots,
        HashSet<int> activePath)
    {
        Assert.True(activePath.Add(mutation.Id), $"Mutation prerequisite cycle includes '{mutation.Name}' ({mutation.Id}).");

        if (mutation.Prerequisites.Count == 0)
        {
            Assert.True(roots.ContainsKey(mutation.Id), $"Mutation '{mutation.Name}' has no prerequisites but is not a registered root.");
        }
        else
        {
            Assert.False(roots.ContainsKey(mutation.Id), $"Mutation '{mutation.Name}' has prerequisites but is registered as a root.");

            foreach (var prerequisite in mutation.Prerequisites)
            {
                Assert.True(
                    all.TryGetValue(prerequisite.MutationId, out var parent),
                    $"Mutation '{mutation.Name}' references missing prerequisite ID {prerequisite.MutationId}.");
                AssertReachesRoot(parent!, all, roots, activePath);
            }
        }

        activePath.Remove(mutation.Id);
    }
}
