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

        /// <summary>
        /// Formats a float with a configurable fixed number of decimal places (default 3).
        /// If the value is an integer (within exact modulo), it is returned without decimals.
        /// Decimals clamped to [0,6].
        /// </summary>
        public string FormatFloat(float value, int decimals = 3)
        {
            if (value % 1 == 0)
                return ((int)value).ToString(CultureInfo.InvariantCulture);
            if (decimals < 0) decimals = 0;
            if (decimals > 6) decimals = 6;
            if (decimals == 0)
                return ((int)System.Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            string format = "0." + new string('0', decimals); // fixed decimals like previous 0.000 pattern
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a fractional value (0..1) as a percentage with a configurable number of decimal places.
        /// Defaults to 2. Decimals clamped to [0,6].
        /// </summary>
        public string FormatPercent(float value, int decimals = 2)
        {
            if (decimals < 0) decimals = 0;
            if (decimals > 6) decimals = 6;
            string format = "P" + decimals; // e.g. P2
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

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