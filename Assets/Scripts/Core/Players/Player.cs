using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Game;

namespace FungusToast.Core.Players
{
    public class Player
    {
        private static readonly System.Random rng = new System.Random();

        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public PlayerTypeEnum PlayerType { get; private set; }
        public AITypeEnum AIType { get; private set; }
        public int MutationPoints { get; set; }

        public Dictionary<int, PlayerMutation> PlayerMutations { get; private set; } = new();
        public List<int> ControlledTileIds { get; private set; } = new();

        public bool IsActive { get; set; }
        public int Score { get; set; }

        private int baseMutationPoints = 5;

        public IMutationSpendingStrategy MutationStrategy { get; private set; }

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

        public void SetBaseMutationPoints(int amount)
        {
            baseMutationPoints = amount;
        }

        public void SetMutationStrategy(IMutationSpendingStrategy strategy)
        {
            MutationStrategy = strategy;
        }

        public int GetMutationPointIncome()
        {
            int bonus = (int)GetMutationEffect(MutationType.BonusMutationPointChance);
            return baseMutationPoints + bonus;
        }

        public void AcquireMutation(int mutationId, MutationManager mutationManager)
        {
            if (!PlayerMutations.ContainsKey(mutationId))
            {
                var mutation = mutationManager.GetMutationById(mutationId);
                if (mutation != null)
                    PlayerMutations[mutationId] = new PlayerMutation(PlayerId, mutationId, mutation);
            }
        }

        public bool TryUpgradeMutation(Mutation mutation)
        {
            if (mutation == null)
                return false;

            if (!PlayerMutations.ContainsKey(mutation.Id))
            {
                PlayerMutations[mutation.Id] = new PlayerMutation(PlayerId, mutation.Id, mutation);
            }

            var playerMutation = PlayerMutations[mutation.Id];

            if (MutationPoints >= mutation.PointsPerUpgrade && playerMutation.CurrentLevel < mutation.MaxLevel)
            {
                MutationPoints -= mutation.PointsPerUpgrade;
                playerMutation.Upgrade();
                return true;
            }

            return false;
        }

        public bool CanUpgrade(Mutation mutation)
        {
            if (mutation == null)
                return false;

            if (mutation.RequiredMutation != null)
            {
                int requiredLevel = GetMutationLevel(mutation.RequiredMutation.Id);
                if (requiredLevel < mutation.RequiredLevel)
                    return false;
            }

            int currentLevel = GetMutationLevel(mutation.Id);
            return MutationPoints >= mutation.PointsPerUpgrade && currentLevel < mutation.MaxLevel;
        }



        public int GetMutationLevel(int mutationId)
        {
            return PlayerMutations.TryGetValue(mutationId, out var pm) ? pm.CurrentLevel : 0;
        }

        public float GetMutationEffect(MutationType type)
        {
            float total = 0f;

            foreach (var playerMutation in PlayerMutations.Values)
            {
                if (playerMutation.Mutation.Type == type)
                    total += playerMutation.GetEffect();
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
            float baseChance = GameBalance.BaseGrowthChance;
            float bonus = GetMutationEffect(MutationType.GrowthChance);
            return baseChance + bonus;
        }

        public float GetEffectiveSelfDeathChance()
        {
            float baseChance = GameBalance.BaseDeathChance;
            float survivalBonus = GetMutationEffect(MutationType.DefenseSurvival);
            return System.Math.Max(0f, baseChance - survivalBonus);
        }

        public float GetOffensiveDecayModifierAgainst(FungalCell targetCell, GameBoard board)
        {
            float decayBoost = GetMutationEffect(MutationType.EnemyDecayChance);

            if (DeathEngine.IsCellSurrounded(targetCell.TileId, board))
            {
                float encystedSporeMultiplier = 1f + GetMutationEffect(MutationType.EncystedSporeMultiplier);
                decayBoost *= encystedSporeMultiplier;
            }

            return decayBoost;
        }

        public int GetBonusMutationPoints()
        {
            int bonusPoints = 0;
            float bonusChance = GetMutationEffect(MutationType.BonusMutationPointChance);
            if (rng.NextDouble() < bonusChance)
                bonusPoints += 1;
            return bonusPoints;
        }

        public void LogOwnedMutations()
        {
            foreach (var m in PlayerMutations)
            {
                var pm = m.Value;
                UnityEngine.Debug.Log($"🧬 Player owns: {pm.Mutation.Name} (Level {pm.CurrentLevel}) [ID {pm.Mutation.Id}]");
            }
        }
    }
}
