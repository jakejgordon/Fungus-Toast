using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.AI
{
    public class TargetMutationGoal
    {
        public int MutationId { get; }
        public int? TargetLevel { get; }

        public TargetMutationGoal(int mutationId, int? targetLevel = null)
        {
            MutationId = mutationId;
            TargetLevel = targetLevel;
        }
    }
}
