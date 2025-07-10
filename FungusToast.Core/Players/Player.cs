using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Logging;

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

        public List<PlayerMycovariant> PlayerMycovariants { get; } = new();

        public List<int> ControlledTileIds { get; } = new();

        public bool IsActive { get; set; }
        public int Score { get; set; }

        public bool WantsToBankPointsThisTurn { get; set; }

        private int baseMutationPoints = GameBalance.StartingMutationPoints;

        public IMutationSpendingStrategy? MutationStrategy { get; private set; }

        // ------------------- SURGE STATE TRACKING -------------------

        public class ActiveSurgeInfo
        {
            public int MutationId { get; }
            public int Level { get; }
            public int TurnsRemaining { get; private set; }

            public ActiveSurgeInfo(int mutationId, int level, int duration)
            {
                MutationId = mutationId;
                Level = level;
                TurnsRemaining = duration;
            }

            public void TickDown() => TurnsRemaining--;
            public bool IsExpired => TurnsRemaining <= 0;
        }

        // Only one surge of each type per player at a time
        // Key = MutationId
        public Dictionary<int, ActiveSurgeInfo> ActiveSurges { get; } = new();

        public bool IsSurgeActive(int mutationId) => ActiveSurges.ContainsKey(mutationId);

        public int GetSurgeTurnsRemaining(int mutationId)
            => ActiveSurges.TryGetValue(mutationId, out var surge) ? surge.TurnsRemaining : 0;

        /// <summary>
        /// Call at the end of the round (after the decay phase) to tick down all surges.
        /// Removes surges with 0 turns remaining.
        /// </summary>
        public void TickDownActiveSurges()
        {
            var expired = new List<int>();
            foreach (var kv in ActiveSurges.Values)
            {
                kv.TickDown();
                if (kv.IsExpired)
                    expired.Add(kv.MutationId);
            }
            foreach (var id in expired)
                ActiveSurges.Remove(id);
        }

        // Optional: Remove a specific surge (e.g., on game end, mutation loss, etc.)
        public void RemoveActiveSurge(int mutationId)
        {
            ActiveSurges.Remove(mutationId);
        }

        // -----------------------------------------------------------------------

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

        public bool TryUpgradeMutation(Mutation mutation, ISimulationObserver? simulationObserver, int currentRound)
        {
            if (mutation == null) return false;

            // Get or create the PlayerMutation object
            if (!PlayerMutations.ContainsKey(mutation.Id))
                PlayerMutations[mutation.Id] = new PlayerMutation(PlayerId, mutation.Id, mutation);
            var pm = PlayerMutations[mutation.Id];

            // --- Prerequisite tracking logic ---
            bool prereqsMet = true;
            foreach (var pre in mutation.Prerequisites)
                if (GetMutationLevel(pre.MutationId) < pre.RequiredLevel)
                    prereqsMet = false;
            // Only set PrereqMetRound the first time prerequisites are met
            if (mutation.Prerequisites.Count > 0 && prereqsMet && pm.PrereqMetRound == null)
                pm.PrereqMetRound = currentRound;

            if (mutation.IsSurge)
            {
                // Surge logic
                if (IsSurgeActive(mutation.Id)) return false; // Can't upgrade/activate if active

                int currentLevel = pm.CurrentLevel;
                int activationCost = mutation.GetSurgeActivationCost(currentLevel);

                if (MutationPoints < activationCost || currentLevel >= mutation.MaxLevel)
                    return false;

                // Enforce one-round delay after prereqs met (only for non-root mutations)
                if (mutation.Prerequisites.Count > 0 && pm.PrereqMetRound.HasValue && pm.PrereqMetRound.Value == currentRound)
                    return false;

                // Deduct points and upgrade
                MutationPoints -= activationCost;
                pm.Upgrade(currentRound);
                int newLevel = pm.CurrentLevel;
                int duration = mutation.SurgeDuration;

                // Activate the surge
                ActiveSurges[mutation.Id] = new ActiveSurgeInfo(mutation.Id, newLevel, duration);

                // --- Set PrereqMetRound on dependents ---
                foreach (var dependent in FungusToast.Core.Mutations.MutationRegistry.All.Values)
                {
                    if (dependent.Prerequisites.Any(p => p.MutationId == mutation.Id))
                    {
                        if (!PlayerMutations.ContainsKey(dependent.Id))
                            PlayerMutations[dependent.Id] = new PlayerMutation(PlayerId, dependent.Id, dependent);
                        var depPlayerMutation = PlayerMutations[dependent.Id];
                        bool allMet = dependent.Prerequisites.All(p => GetMutationLevel(p.MutationId) >= p.RequiredLevel);
                        if (dependent.Prerequisites.Count > 0 && allMet && depPlayerMutation.PrereqMetRound == null)
                            depPlayerMutation.PrereqMetRound = currentRound;
                    }
                }

                simulationObserver?.RecordMutationPointsSpent(PlayerId, mutation.Tier, activationCost);
                return true;
            }
            else
            {
                // Standard mutation upgrade
                if (MutationPoints >= mutation.PointsPerUpgrade && pm.CurrentLevel < mutation.MaxLevel)
                {
                    // Enforce one-round delay after prereqs met (only for non-root mutations)
                    if (mutation.Prerequisites.Count > 0 && pm.PrereqMetRound.HasValue && pm.PrereqMetRound.Value == currentRound)
                        return false;

                    MutationPoints -= mutation.PointsPerUpgrade;
                    pm.Upgrade(currentRound);

                    // --- Set PrereqMetRound on dependents ---
                    foreach (var dependent in FungusToast.Core.Mutations.MutationRegistry.All.Values)
                    {
                        if (dependent.Prerequisites.Any(p => p.MutationId == mutation.Id))
                        {
                            if (!PlayerMutations.ContainsKey(dependent.Id))
                                PlayerMutations[dependent.Id] = new PlayerMutation(PlayerId, dependent.Id, dependent);
                            var depPlayerMutation = PlayerMutations[dependent.Id];
                            bool allMet = dependent.Prerequisites.All(p => GetMutationLevel(p.MutationId) >= p.RequiredLevel);
                            if (dependent.Prerequisites.Count > 0 && allMet && depPlayerMutation.PrereqMetRound == null)
                                depPlayerMutation.PrereqMetRound = currentRound;
                        }
                    }

                    simulationObserver?.RecordMutationPointsSpent(PlayerId, mutation.Tier, mutation.PointsPerUpgrade);
                    return true;
                }
                return false;
            }
        }

        public bool CanUpgrade(Mutation mut, int currentRound)
        {
            if (mut == null) return false;

            // Surge: can't upgrade while active
            if (mut.IsSurge && IsSurgeActive(mut.Id))
                return false;

            foreach (var pre in mut.Prerequisites)
                if (GetMutationLevel(pre.MutationId) < pre.RequiredLevel)
                    return false;

            int currentLevel = GetMutationLevel(mut.Id);
            int cost = mut.IsSurge
                ? mut.GetSurgeActivationCost(currentLevel)
                : mut.PointsPerUpgrade;

            // Enforce one-round delay after prereqs met (only for non-root mutations)
            if (mut.Prerequisites.Count > 0 && PlayerMutations.TryGetValue(mut.Id, out var pm) && pm.PrereqMetRound.HasValue && pm.PrereqMetRound.Value == currentRound)
                return false;

            return MutationPoints >= cost && currentLevel < mut.MaxLevel;
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

        public bool TryAutoUpgrade(Mutation mut, int currentRound)
        {
            if (mut == null) return false;

            if (!PlayerMutations.ContainsKey(mut.Id))
                PlayerMutations[mut.Id] = new PlayerMutation(PlayerId, mut.Id, mut);

            var pm = PlayerMutations[mut.Id];
            if (pm.CurrentLevel < mut.MaxLevel)
            {
                pm.Upgrade(currentRound);
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

        public int RollAnabolicInversionBonus(List<Player> allPlayers, System.Random rng, GameBoard board)
        {
            if (!PlayerMutations.TryGetValue(MutationIds.AnabolicInversion, out var pm) || pm.CurrentLevel <= 0)
                return 0;

            // Get living cells for this player
            int myLivingCells = board.GetAllCellsOwnedBy(PlayerId).Count(c => c.IsAlive);
            var others = allPlayers.Where(p => p != this).ToList();
            if (others.Count == 0) return 0;

            // Get living cells for other players
            float avgOthersLivingCells = (float)others.Average(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive));
            if (avgOthersLivingCells <= 0f) avgOthersLivingCells = 1f;

            float ratio = Math.Max(0f, Math.Min(1f, myLivingCells / avgOthersLivingCells)); // clamp between 0 and 1
            float chance = (1f - ratio) + pm.CurrentLevel * GameBalance.AnabolicInversionGapBonusPerLevel;

            if (rng.NextDouble() < chance)
            {
                // Weighted random selection: the further behind you are, the higher chance of getting 5 MP
                // ratio = 0 means you're way behind (get higher MP), ratio = 1 means you're ahead (get lower MP)
                float weight = 1f - ratio; // 0 = ahead, 1 = behind
                
                // Create weighted distribution: higher weight = higher chance of 5 MP
                float rand = (float)rng.NextDouble();
                int bonus;
                
                if (rand < weight * 0.6f) // 60% of weight goes to 4-5 MP
                {
                    bonus = rng.NextDouble() < weight ? 5 : 4;
                }
                else if (rand < weight * 0.8f) // 20% of weight goes to 3 MP
                {
                    bonus = 3;
                }
                else if (rand < weight * 0.9f) // 10% of weight goes to 2 MP
                {
                    bonus = 2;
                }
                else // Remaining goes to 1 MP
                {
                    bonus = 1;
                }
                
                return bonus;
            }

            return 0;
        }

        public int AssignMutationPoints(List<Player> allPlayers,
                                        System.Random rng,
                                        GameBoard board,
                                        IEnumerable<Mutation>? allMutations = null,
                                        ISimulationObserver? simulationObserver = null)
        {
            int baseIncome = GetMutationPointIncome();
            int bonus = GetBonusMutationPoints();
            int undergrowth = RollAnabolicInversionBonus(allPlayers, rng, board);

            int newMutationPoints = baseIncome + bonus + undergrowth;
            if (simulationObserver != null)
            {
                simulationObserver.RecordMutationPointIncome(PlayerId, newMutationPoints);
            }

            AddMutationPoints(newMutationPoints);

            // Record Adaptive Expression bonus, if present and observer is hooked up
            if (simulationObserver != null && bonus > 0)
            {
                simulationObserver.RecordAdaptiveExpressionBonus(PlayerId, bonus);
            }

            // Record Anabolic Inversion bonus, if present and observer is hooked up
            if (simulationObserver != null && undergrowth > 0)
            {
                simulationObserver.RecordAnabolicInversionBonus(PlayerId, undergrowth);
            }

            return MutationPoints;
        }

        public void AddMutationPoints(int amount)
        {
            MutationPoints += amount;
        }


        public bool HasMycovariant(int id) =>
            PlayerMycovariants.Any(m => m.MycovariantId == id);

        public PlayerMycovariant? GetMycovariant(int id) =>
            PlayerMycovariants.FirstOrDefault(m => m.MycovariantId == id);

        public void AddMycovariant(Mycovariant picked)
        {
            // Prevent duplicates
            if (HasMycovariant(picked.Id))
                return;

            var playerMyco = new PlayerMycovariant(PlayerId, picked.Id, picked);
            PlayerMycovariants.Add(playerMyco);
        }

    }
}
