using System.Collections.Generic;
using FungusToast.Core.Growth;
using FungusToast.Game;

namespace FungusToast.Core.Players
{
    public class Player
    {
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public PlayerTypeEnum PlayerType { get; private set; }
        public AITypeEnum AIType { get; private set; }
        public int MutationPoints { get; set; }

        public float GrowthChance { get; set; } = 0.02f;

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

        public void AcquireMutation(int mutationId, MutationManager mutationManager)
        {
            if (!PlayerMutations.ContainsKey(mutationId))
            {
                var mutation = mutationManager.GetMutationById(mutationId);
                if (mutation != null)
                {
                    PlayerMutations.Add(mutationId, new PlayerMutation(PlayerId, mutationId, mutation));
                }
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

        public float GetMutationEffect(MutationType type)
        {
            float total = 0f;

            foreach (var playerMutation in PlayerMutations.Values)
            {
                if (playerMutation.Mutation.Type == type)
                {
                    total += playerMutation.GetTotalEffect();
                }
            }

            return total;
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

        public float GetEffectiveGrowthChance()
        {
            float baseChance = 0.05f; // Base growth chance
            float bonus = GetMutationEffect(MutationType.GrowthChance);
            return baseChance + bonus;
        }

        public float GetEffectiveSelfDeathChance()
        {
            float baseChance = DeathEngine.BaseDeathChance;
            float survivalBonus = GetMutationEffect(MutationType.DefenseSurvival);
            return System.Math.Max(0f, baseChance - survivalBonus);
        }


        public float GetEffectiveDeathChanceFrom(Player otherPlayer)
        {
            float ownChance = GetEffectiveSelfDeathChance();
            float addedRisk = otherPlayer.GetMutationEffect(MutationType.EnemyDecayChance);
            return ownChance + addedRisk;
        }

    }
}
