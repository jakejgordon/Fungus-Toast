using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mutations
{
    /// <summary>One of the contained named prerequisites must be met. Groups combine with other requirements using AND.</summary>
    public sealed class MutationAnyPrerequisiteGroup
    {
        public IReadOnlyList<MutationPrerequisite> Alternatives { get; }

        public MutationAnyPrerequisiteGroup(params MutationPrerequisite[] alternatives)
        {
            if (alternatives == null || alternatives.Length == 0)
                throw new ArgumentException("An ANY prerequisite group needs at least one alternative.", nameof(alternatives));

            Alternatives = alternatives.ToList();
        }

        public bool IsMet(Player player) => Alternatives.Any(requirement =>
            player.GetMutationLevel(requirement.MutationId) >= requirement.RequiredLevel);

        public bool Includes(Mutation mutation) => Alternatives.Any(requirement => requirement.MutationId == mutation.Id);
    }
}
