using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Game;
using UnityEngine; // ✅ Needed for Debug.Log

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

        public float GrowthChance { get; set; } = 0.10f;

        public Dictionary<int, PlayerMutation> PlayerMutations { get; private set; } = new();
        public List<int> ControlledTileIds { get; private set; } = new();

        public bool IsActive { get; set; }
        public int Score { get; set; }

        private int baseMutationPoints = 5;

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

            // Auto-acquire if not already owned
            if (!PlayerMutations.ContainsKey(mutation.Id))
            {
                // Optional: validate prerequisites here
                PlayerMutations[mutation.Id] = new PlayerMutation(PlayerId, mutation.Id, mutation);
                Debug.Log($"🧬 Player {PlayerId} auto-acquired {mutation.Name}");
            }

            var playerMutation = PlayerMutations[mutation.Id];

            if (MutationPoints >= mutation.PointsPerUpgrade && playerMutation.CurrentLevel < mutation.MaxLevel)
            {
                MutationPoints -= mutation.PointsPerUpgrade;
                playerMutation.Upgrade();
                Debug.Log($"✅ Player {PlayerId} upgraded {mutation.Name} to Level {playerMutation.CurrentLevel} | Remaining MP: {MutationPoints}");
                return true;
            }

            Debug.LogWarning($"❌ Upgrade failed for {mutation.Name}: Not enough MP or already maxed.");
            return false;
        }

        public bool CanUpgrade(Mutation mutation)
        {
            return mutation != null &&
                   PlayerMutations.TryGetValue(mutation.Id, out var playerMutation) &&
                   MutationPoints >= mutation.PointsPerUpgrade &&
                   playerMutation.CurrentLevel < mutation.MaxLevel;
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
                    total += playerMutation.GetEffect(); // ✅ uses refactored method
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
            float baseChance = 0.05f;
            float bonus = GetMutationEffect(MutationType.GrowthChance);
            return baseChance + bonus;
        }

        public float GetEffectiveSelfDeathChance()
        {
            float baseChance = DeathEngine.BaseDeathChance;
            float survivalBonus = GetMutationEffect(MutationType.DefenseSurvival);
            return Mathf.Max(0f, baseChance - survivalBonus);
        }

        public float GetEffectiveDeathChanceFrom(Player attacker, FungalCell targetCell, GameBoard board)
        {
            float baseChance = GetEffectiveSelfDeathChance();
            float decayBoost = attacker.GetMutationEffect(MutationType.EnemyDecayChance);

            if (DeathEngine.IsCellSurrounded(targetCell.TileId, board))
            {
                float encystedSporeMultiplier = 1f + attacker.GetMutationEffect(MutationType.EncystedSporeMultiplier);
                decayBoost *= encystedSporeMultiplier;
            }

            return baseChance + decayBoost;
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
                Debug.Log($"🧬 Player owns: {pm.Mutation.Name} (Level {pm.CurrentLevel}) [ID {pm.Mutation.Id}]");
            }
        }
    }
}
