
namespace FungusToast.Core.Mutations
{
    /// <summary>
    /// Provides a shared, static registry of all defined mutations and their hierarchy.
    /// Safe for use in non-Unity contexts (like simulation or testing).
    /// </summary>
    public static class MutationRegistry
    {
        private static readonly Dictionary<int, Mutation> allMutations;
        private static readonly Dictionary<int, Mutation> rootMutations;

        static MutationRegistry()
        {
            (allMutations, rootMutations) = MutationRepository.BuildFullMutationSet();
        }

        public static IReadOnlyDictionary<int, Mutation> All => allMutations;
        public static IReadOnlyDictionary<int, Mutation> Roots => rootMutations;

        public static Mutation? GetById(int id) =>
            allMutations.TryGetValue(id, out var m) ? m : null;

        public static IReadOnlyCollection<Mutation> GetAll() => allMutations.Values;
    }
}
