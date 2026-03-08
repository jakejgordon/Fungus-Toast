using System;

namespace FungusToast.Core.Mutations
{
    [Flags]
    public enum MutationAITags
    {
        None = 0,
        CatchUp = 1 << 0,
    }
}