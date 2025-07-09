using FungusToast.Core.Mutations;

namespace FungusToast.Core.Players
{
    /// <summary>
    /// Represents a mutation acquired by a player, tracking their upgrade level.
    /// </summary>
    public class PlayerMutation
    {
        /// <summary>
        /// The ID of the player who owns this mutation.
        /// </summary>
        public int PlayerId { get; }

        /// <summary>
        /// The ID of the associated mutation.
        /// </summary>
        public int MutationId { get; }

        /// <summary>
        /// The current level of the mutation for this player.
        /// </summary>
        public int CurrentLevel { get; private set; }

        /// <summary>
        /// The shared static mutation definition.
        /// </summary>
        public Mutation Mutation { get; }

        /// <summary>
        /// Indicates whether this mutation has reached its maximum level.
        /// </summary>
        public bool IsMaxedOut => Mutation != null && CurrentLevel >= Mutation.MaxLevel;

        /// <summary>
        /// The round number when the mutation was first upgraded.
        /// </summary>
        public int? FirstUpgradeRound { get; private set; }

        /// <summary>
        /// The round number when all prerequisites were first met (null if never met).
        /// </summary>
        public int? PrereqMetRound { get; set; }

        public PlayerMutation(int playerId, int mutationId, Mutation mutation)
        {
            PlayerId = playerId;
            MutationId = mutationId;
            Mutation = mutation;
            CurrentLevel = 0;
        }

        /// <summary>
        /// Increases the current level by 1, up to the mutation's maximum level.
        /// </summary>
        public void Upgrade(int currentRound)
        {
            if (Mutation != null && CurrentLevel < Mutation.MaxLevel)
            {
                if (CurrentLevel == 0 && FirstUpgradeRound == null)
                {
                    FirstUpgradeRound = currentRound;
                }
                CurrentLevel++;
            }
        }

        /// <summary>
        /// Returns the total effect of the mutation at its current level.
        /// </summary>
        public float GetEffect()
        {
            return Mutation?.GetTotalEffect(CurrentLevel) ?? 0f;
        }

        public bool CanAutoUpgrade()
        {
            return Mutation != null && CurrentLevel < Mutation.MaxLevel;
        }
    }
}
