namespace FungusToast.Core.Mutations
{
    public class MutationPrerequisite
    {
        public int MutationId;
        public int RequiredLevel;

        public MutationPrerequisite(int mutationId, int requiredLevel)
        {
            MutationId = mutationId;
            RequiredLevel = requiredLevel;
        }
    }
}

