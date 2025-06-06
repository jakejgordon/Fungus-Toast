using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.AI;
using FungusToast.Core.Death;
using System;
using FungusToast.Core.Phases;

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

        public IMutationSpendingStrategy? MutationStrategy { get; private set; }

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
            int level = GetMutationLevel(MutationIds.HomeostaticHarmony);
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


        // 🔄  Back-compat helper used by DeathEngine -------------------------
        public float GetOffensiveDecayModifierAgainst(FungalCell targetCell, GameBoard board)
        {
            float boost = GetMutationEffect(MutationType.EnemyDecayChance);

            boost += GetMutationEffect(MutationType.AdjacentFungicide);
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

        private bool IsEligibleForAutoUpgrade(Mutation mutation)
        {
            return (mutation.Tier == MutationTier.Tier1 || mutation.Tier == MutationTier.Tier2)
                && (mutation.Category == MutationCategory.Growth || mutation.Category == MutationCategory.CellularResilience);
        }

        public bool TryAutoUpgrade(Mutation mut)
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
            int level = GetMutationLevel(MutationIds.ChronoresilientCytoplasm);
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

        public int RollAnabolicInversionBonus(List<Player> allPlayers, System.Random rng)
        {
            if (!PlayerMutations.TryGetValue(MutationIds.AnabolicInversion, out var pm) || pm.CurrentLevel <= 0)
                return 0;

            int myCells = ControlledTileIds.Count;
            var others = allPlayers.Where(p => p != this).ToList();
            if (others.Count == 0) return 0;

            float avgOthers = (float)others.Average(p => p.ControlledTileIds.Count);
            if (avgOthers <= 0f) avgOthers = 1f;

            float ratio = Math.Max(0f, Math.Min(1f, myCells / avgOthers)); // clamp between 0 and 1
            float chance = (1f - ratio) + pm.CurrentLevel * GameBalance.AnabolicInversionGapBonusPerLevel;

            if (rng.NextDouble() < chance)
            {
                return rng.Next(1, 2 * pm.CurrentLevel); // 1 to 5 at level 3
            }


            return 0;
        }

        public int AssignMutationPoints(List<Player> allPlayers,
                                        System.Random rng,
                                        IEnumerable<Mutation>? allMutations = null)
        {
            int baseIncome = GetMutationPointIncome();
            int bonus = GetBonusMutationPoints();
            int undergrowth = RollAnabolicInversionBonus(allPlayers, rng);

            MutationPoints = baseIncome + bonus + undergrowth;

            if (allMutations != null)
                MutationEffectProcessor.TryApplyMutatorPhenotype(this, allMutations.ToList(), rng);

            return MutationPoints;
        }
    }
}
