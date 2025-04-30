using FungusToast.Core.Mutations;

namespace FungusToast.Core.Players
{
    public class PlayerMutation
    {
        public int PlayerId { get; }
        public int MutationId { get; }
        public int CurrentLevel { get; private set; }

        public Mutation Mutation { get; }

        public PlayerMutation(int playerId, int mutationId, Mutation mutation)
        {
            PlayerId = playerId;
            MutationId = mutationId;
            Mutation = mutation;
            CurrentLevel = 0;
        }

        public void Upgrade()
        {
            if (Mutation != null && CurrentLevel < Mutation.MaxLevel)
            {
                CurrentLevel++;
            }
        }

        public float GetTotalEffect()
        {
            return (CurrentLevel * Mutation.EffectPerLevel);
        }
    }
}
