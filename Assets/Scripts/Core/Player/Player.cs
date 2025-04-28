using System.Collections.Generic;
using FungusToast.Core.Mutations; // Assuming your Mutation and PlayerMutation are under Core.Mutations

namespace FungusToast.Core.Player
{
    public class Player
    {
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public PlayerTypeEnum PlayerType { get; private set; }
        public AITypeEnum AIType { get; private set; }
        public int MutationPoints { get; set; }

        // New structure
        public Dictionary<int, PlayerMutation> PlayerMutations { get; private set; } = new Dictionary<int, PlayerMutation>();

        public List<int> ControlledTileIds { get; private set; } = new List<int>();

        public bool IsActive { get; set; }
        public int Score { get; set; }

        public Player(int playerId, string playerName, PlayerTypeEnum playerType, AITypeEnum aiType = AITypeEnum.Random)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            PlayerType = playerType;
            AIType = aiType;
            MutationPoints = 0;
            IsActive = false;
            Score = 0;
        }

        public void AcquireMutation(int mutationId)
        {
            if (!PlayerMutations.ContainsKey(mutationId))
            {
                PlayerMutations.Add(mutationId, new PlayerMutation(PlayerId, mutationId));
            }
        }

        public bool UpgradeMutation(int mutationId)
        {
            if (PlayerMutations.TryGetValue(mutationId, out var playerMutation))
            {
                playerMutation.Upgrade();
                return true;
            }
            return false;
        }

        public int GetMutationLevel(int mutationId)
        {
            if (PlayerMutations.TryGetValue(mutationId, out var playerMutation))
            {
                return playerMutation.CurrentLevel;
            }
            return 0;
        }

        public void AddControlledTile(int tileId)
        {
            if (!ControlledTileIds.Contains(tileId))
                ControlledTileIds.Add(tileId);
        }

        public void RemoveControlledTile(int tileId)
        {
            ControlledTileIds.Remove(tileId);
        }
    }
}
