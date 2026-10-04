using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.ContentProfiles
{
    /// <summary>
    /// Authoring metadata describing what a mutation or Mycovariant does, for the
    /// content-to-strategy coverage review. It never changes gameplay or AI behavior.
    /// Built fluently on the definition itself, for example:
    /// <code>ContentProfile.Of(Cap.SelfReclamation).Uses(Cond.OwnDeadCells)</code>
    /// Relation meanings and authoring rules: docs/second-level/AI_CONTENT_TAGS.md.
    /// </summary>
    public sealed class ContentProfile
    {
        public const int MaxCapabilities = 3;

        private ContentProfile(
            IReadOnlyList<ContentCapability> capabilities,
            IReadOnlyList<ContentCapability> amplifiedCapabilities,
            IReadOnlyList<BoardCondition> neededConditions,
            IReadOnlyList<BoardCondition> usedConditions,
            IReadOnlyList<BoardCondition> createdConditions,
            IReadOnlyList<BoardCondition> removedConditions)
        {
            Capabilities = capabilities;
            AmplifiedCapabilities = amplifiedCapabilities;
            NeededConditions = neededConditions;
            UsedConditions = usedConditions;
            CreatedConditions = createdConditions;
            RemovedConditions = removedConditions;
        }

        /// <summary>The jobs the content does (1 to <see cref="MaxCapabilities"/>).</summary>
        public IReadOnlyList<ContentCapability> Capabilities { get; }

        /// <summary>Capabilities it makes stronger when the colony already has them.</summary>
        public IReadOnlyList<ContentCapability> AmplifiedCapabilities { get; }

        /// <summary>Conditions it depends on that a plan should produce deliberately.</summary>
        public IReadOnlyList<BoardCondition> NeededConditions { get; }

        /// <summary>
        /// Conditions it benefits from when they occur anyway (salvage, or the environment),
        /// without justifying producing more of them.
        /// </summary>
        public IReadOnlyList<BoardCondition> UsedConditions { get; }

        /// <summary>Conditions it produces as a main effect or side effect.</summary>
        public IReadOnlyList<BoardCondition> CreatedConditions { get; }

        /// <summary>Conditions it destroys at a scale that can undercut a plan relying on them.</summary>
        public IReadOnlyList<BoardCondition> RemovedConditions { get; }

        public static ContentProfile Of(params ContentCapability[] capabilities)
        {
            var empty = Array.Empty<BoardCondition>();
            return new ContentProfile(
                Distinct(capabilities),
                Array.Empty<ContentCapability>(),
                empty,
                empty,
                empty,
                empty);
        }

        public ContentProfile Amplifies(params ContentCapability[] capabilities) =>
            new ContentProfile(Capabilities, Distinct(AmplifiedCapabilities.Concat(capabilities)), NeededConditions, UsedConditions, CreatedConditions, RemovedConditions);

        public ContentProfile Needs(params BoardCondition[] conditions) =>
            new ContentProfile(Capabilities, AmplifiedCapabilities, Distinct(NeededConditions.Concat(conditions)), UsedConditions, CreatedConditions, RemovedConditions);

        public ContentProfile Uses(params BoardCondition[] conditions) =>
            new ContentProfile(Capabilities, AmplifiedCapabilities, NeededConditions, Distinct(UsedConditions.Concat(conditions)), CreatedConditions, RemovedConditions);

        public ContentProfile Creates(params BoardCondition[] conditions) =>
            new ContentProfile(Capabilities, AmplifiedCapabilities, NeededConditions, UsedConditions, Distinct(CreatedConditions.Concat(conditions)), RemovedConditions);

        public ContentProfile Removes(params BoardCondition[] conditions) =>
            new ContentProfile(Capabilities, AmplifiedCapabilities, NeededConditions, UsedConditions, CreatedConditions, Distinct(RemovedConditions.Concat(conditions)));

        private static IReadOnlyList<T> Distinct<T>(IEnumerable<T> values) => values.Distinct().ToArray();
    }
}
