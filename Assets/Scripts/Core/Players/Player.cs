using System.Collections.Generic;
using System.Linq;
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

        public int PlayerId { get; }
        public string PlayerName { get; }
        public PlayerTypeEnum PlayerType { get; }
        public AITypeEnum AIType { get; }
        public int MutationPoints { get; set; }

        public Dictionary<int, PlayerMutation> PlayerMutations { get; } = new();
        public List<int> ControlledTileIds { get; } = new();

        public bool IsActive { get; set; }
        public int Score { get; set; }

        private int baseMutationPoints = GameBalance.StartingMutationPoints;

        public IMutationSpendingStrategy MutationStrategy { get; private set; }

        public Player(int playerId, string playerName, PlayerTypeEnum playerType, AITypeEnum aiType = AITypeEnum.Random)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            PlayerType = playerType;
            AIType = aiType;
        }

        /* ---------------- Mutation-point helpers ---------------- */

        public void SetBaseMutationPoints(int amount) => baseMutationPoints = amount;
        public int GetBaseMutationPointIncome() => baseMutationPoints;

        //  🔄  Back-compat shim for older callers ---------------
        public int GetMutationPointIncome() => GetBaseMutationPointIncome();

        public void SetMutationStrategy(IMutationSpendingStrategy strat) => MutationStrategy = strat;

        /* ---------------- Growth / death chance --------------- */

        public float GetEffectiveGrowthChance() =>
            GameBalance.BaseGrowthChance + GetMutationEffect(MutationType.GrowthChance);

        public float GetEffectiveSelfDeathChance()
        {
            float bonusPerLevel = GameBalance.HomeostaticHarmonyEffectPerLevel;
            int level = GetMutationLevel(MutationManager.MutationIds.HomeostaticHarmony);
            return level * bonusPerLevel;
        }

        public float GetBaseMycelialDegradationRisk(List<Player> allPlayers)
        {
            float enemyPressure = allPlayers
                .Where(p => p.PlayerId != PlayerId)
                .Sum(p => p.GetMutationEffect(MutationType.EnemyDecayChance));

            return System.Math.Max(
                0f,
                GameBalance.BaseDeathChance + enemyPressure - GetEffectiveSelfDeathChance());
        }

        public float GetEffectiveDeathChanceFrom(Player attacker, FungalCell targetCell, GameBoard board)
        {
            float chance = GetEffectiveSelfDeathChance();
            float decayBoost = attacker.GetMutationEffect(MutationType.EnemyDecayChance);

            if (DeathEngine.IsCellSurrounded(targetCell.TileId, board))
            {
                float encystMult = 1f + attacker.GetMutationEffect(MutationType.EncystedSporeMultiplier);
                decayBoost *= encystMult;
            }

            float toxinBoost = attacker.GetMutationEffect(MutationType.OpponentExtraDeathChance);
            chance += decayBoost + toxinBoost;

            return System.Math.Max(0f, chance);
        }

        // 🔄  Back-compat helper used by DeathEngine -------------------------
        public float GetOffensiveDecayModifierAgainst(FungalCell targetCell, GameBoard board)
        {
            float boost = GetMutationEffect(MutationType.EnemyDecayChance);

            if (DeathEngine.IsCellSurrounded(targetCell.TileId, board))
            {
                float encysted = 1f + GetMutationEffect(MutationType.EncystedSporeMultiplier);
                boost *= encysted;
            }

            boost += GetMutationEffect(MutationType.OpponentExtraDeathChance);
            return boost;
        }

        /* ---------------- Diagonal growth helpers ------------- */

        public float GetDiagonalGrowthChance(DiagonalDirection dir) =>
            dir switch
            {
                DiagonalDirection.Northwest => GetMutationEffect(MutationType.GrowthDiagonal_NW),
                DiagonalDirection.Northeast => GetMutationEffect(MutationType.GrowthDiagonal_NE),
                DiagonalDirection.Southeast => GetMutationEffect(MutationType.GrowthDiagonal_SE),
                DiagonalDirection.Southwest => GetMutationEffect(MutationType.GrowthDiagonal_SW),
                _ => 0f
            };

        /* ---------------- Mutation level / effect ------------- */

        public int GetMutationLevel(int mutationId) =>
            PlayerMutations.TryGetValue(mutationId, out var pm) ? pm.CurrentLevel : 0;

        public float GetMutationEffect(MutationType type) =>
            PlayerMutations.Values.Where(pm => pm.Mutation.Type == type)
                                  .Sum(pm => pm.GetEffect());

        /* ---------------- Upgrade API ------------------------- */

        public bool TryUpgradeMutation(Mutation mutation)
        {
            if (mutation == null) return false;

            if (!PlayerMutations.ContainsKey(mutation.Id))
                PlayerMutations[mutation.Id] = new PlayerMutation(PlayerId, mutation.Id, mutation);

            var pm = PlayerMutations[mutation.Id];

            if (MutationPoints >= mutation.PointsPerUpgrade && pm.CurrentLevel < mutation.MaxLevel)
            {
                MutationPoints -= mutation.PointsPerUpgrade;
                pm.Upgrade();
                return true;
            }
            return false;
        }

        public bool CanUpgrade(Mutation mut)
        {
            if (mut == null) return false;

            foreach (var pre in mut.Prerequisites)
                if (GetMutationLevel(pre.MutationId) < pre.RequiredLevel)
                    return false;

            return MutationPoints >= mut.PointsPerUpgrade &&
                   GetMutationLevel(mut.Id) < mut.MaxLevel;
        }

        /* ---------------- Bonus MP & auto-upgrade ------------- */

        public int GetBonusMutationPoints()
        {
            float chance = GetMutationEffect(MutationType.BonusMutationPointChance);
            return rng.NextDouble() < chance ? 1 : 0;
        }

        public void TryTriggerAutoUpgrade()
        {
            float chance = GetMutationEffect(MutationType.AutoUpgradeRandom);
            if (rng.NextDouble() >= chance) return;

            var mm = GameManager.Instance?.GetComponentInChildren<MutationManager>();
            if (mm == null) return;

            var eligible = mm.GetAllMutations().Where(CanUpgrade).ToList();
            if (eligible.Count == 0) return;

            var pick = eligible[rng.Next(eligible.Count)];
            TryAutoUpgrade(pick);
        }

        private bool TryAutoUpgrade(Mutation mut)
        {
            if (mut == null) return false;

            if (!PlayerMutations.ContainsKey(mut.Id))
                PlayerMutations[mut.Id] = new PlayerMutation(PlayerId, mut.Id, mut);

            var pm = PlayerMutations[mut.Id];
            if (pm.CurrentLevel < mut.MaxLevel)
            {
                pm.Upgrade();
                return true;
            }
            return false;
        }

        /* ---------------- Age reset threshold ----------------- */

        public int GetSelfAgeResetThreshold()
        {
            int level = GetMutationLevel(MutationManager.MutationIds.ChronoresilientCytoplasm);
            int threshold = GameBalance.BaseAgeResetThreshold -
                            (level * GameBalance.AgeResetReductionPerLevel);
            return System.Math.Max(1, threshold);
        }

        /* ---------------- Tile bookkeeping ------------------- */

        public void AddControlledTile(int id)
        {
            if (!ControlledTileIds.Contains(id))
                ControlledTileIds.Add(id);
        }
        public void RemoveControlledTile(int id) => ControlledTileIds.Remove(id);
    }
}
