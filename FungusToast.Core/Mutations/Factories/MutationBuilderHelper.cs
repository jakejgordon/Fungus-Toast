using System.Collections.Generic;
using System.Globalization;

namespace FungusToast.Core.Mutations.Factories
{
    /// <summary>
    /// Helper class for building mutations in factory classes.
    /// Provides common formatting utilities and mutation creation methods.
    /// </summary>
    public class MutationBuilderHelper
    {
        private readonly Dictionary<int, Mutation> _allMutations;
        private readonly Dictionary<int, Mutation> _rootMutations;

        public MutationBuilderHelper(Dictionary<int, Mutation> allMutations, Dictionary<int, Mutation> rootMutations)
        {
            _allMutations = allMutations;
            _rootMutations = rootMutations;
        }

        public string FormatFloat(float value) => 
            value % 1 == 0 ? ((int)value).ToString() : value.ToString("0.000", CultureInfo.InvariantCulture);

        public string FormatPercent(float value) => 
            value.ToString("P2", CultureInfo.InvariantCulture);

        public Mutation MakeRoot(Mutation m)
        {
            _allMutations[m.Id] = m;
            _rootMutations[m.Id] = m;
            return m;
        }

        public Mutation MakeChild(Mutation m, params MutationPrerequisite[] prereqs)
        {
            m.Prerequisites.AddRange(prereqs);
            _allMutations[m.Id] = m;

            foreach (var prereq in prereqs)
                if (_allMutations.TryGetValue(prereq.MutationId, out var parent))
                    parent.Children.Add(m);

            return m;
        }
    }
}