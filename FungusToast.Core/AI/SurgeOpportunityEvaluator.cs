using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// What activating a surge right now is expected to be worth. <see cref="Value"/> is in cells
    /// (gained, saved, fortified, or denied over the surge window) and <see cref="Threshold"/> is the
    /// floor the activation cost demands, so <see cref="Margin"/> ranks competing surges.
    /// </summary>
    public readonly struct SurgeOpportunity
    {
        public SurgeOpportunity(float value, float threshold, string reason, bool isModeled = true)
        {
            Value = value;
            Threshold = threshold;
            Reason = reason;
            IsModeled = isModeled;
        }

        public float Value { get; }
        public float Threshold { get; }
        public string Reason { get; }
        public bool IsModeled { get; }
        public float Margin => Value - Threshold;
        public bool IsWorthActivating => Value >= Threshold;

        /// <summary>
        /// True when the value clears the floor by <see cref="GameBalance.AiSurgeOffScheduleValueMultiplier"/>,
        /// which is the bar for firing a planned surge outside its cadence.
        /// </summary>
        public bool IsStrong => IsModeled && Value >= Threshold * GameBalance.AiSurgeOffScheduleValueMultiplier;

        /// <summary>Surges the evaluator has no model for are never blocked by it.</summary>
        public static SurgeOpportunity Unmodeled { get; } = new(0f, 0f, "No opportunity model for this surge.", isModeled: false);
    }

    /// <summary>
    /// Estimates the payoff of activating each Mycelial Surge from the current board so the AI spends
    /// its finite activations (a surge's max level is its activation budget, and each one raises the
    /// next price) only when the board actually rewards them. Every estimate is a cheap expectation
    /// that mirrors the corresponding processor rather than a simulation of it.
    /// </summary>
    public static class SurgeOpportunityEvaluator
    {
        // One turn asks about the same surge several times (ranking, the attempt itself, banking, the
        // decision-health check) and the Beacon estimate walks every tile, so results are memoized per
        // board. The key carries the cheap change signals: round and cycle, the projected level, how many
        // tiles hold a cell, and the player's total mutation levels (which move growth chance and cost).
        private static readonly ConditionalWeakTable<GameBoard, Dictionary<EvaluationKey, SurgeOpportunity>> cache = new();

        private readonly struct EvaluationKey : IEquatable<EvaluationKey>
        {
            private readonly (int PlayerId, int Round, int GrowthCycle, int SurgeId, int ProjectedLevel, int OccupiedTiles, int PlayerMutationLevels) parts;

            public EvaluationKey(int playerId, int round, int growthCycle, int surgeId, int projectedLevel, int occupiedTiles, int playerMutationLevels)
                => parts = (playerId, round, growthCycle, surgeId, projectedLevel, occupiedTiles, playerMutationLevels);

            public bool Equals(EvaluationKey other) => parts.Equals(other.parts);
            public override bool Equals(object? obj) => obj is EvaluationKey other && Equals(other);
            public override int GetHashCode() => parts.GetHashCode();
        }

        public static bool IsWorthActivating(Player player, Mutation surge, GameBoard board)
            => Evaluate(player, surge, board).IsWorthActivating;

        public static SurgeOpportunity Evaluate(
            Player player,
            Mutation surge,
            GameBoard board,
            IReadOnlyDictionary<int, PlayerBoardSummary>? boardSummaries = null)
        {
            if (player == null || surge == null || board == null || !surge.IsSurge)
            {
                return SurgeOpportunity.Unmodeled;
            }

            int projectedLevel = Math.Min(player.GetMutationLevel(surge.Id) + 1, surge.MaxLevel);
            var boardCache = cache.GetValue(board, _ => new());
            var key = new EvaluationKey(
                player.PlayerId,
                board.CurrentRound,
                board.CurrentGrowthCycle,
                surge.Id,
                projectedLevel,
                board.OccupiedTileCount,
                player.PlayerMutations.Values.Sum(pm => pm.CurrentLevel));
            if (boardCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var opportunity = EvaluateUncached(player, surge, board, projectedLevel, boardSummaries);
            boardCache[key] = opportunity;
            return opportunity;
        }

        private static SurgeOpportunity EvaluateUncached(
            Player player,
            Mutation surge,
            GameBoard board,
            int projectedLevel,
            IReadOnlyDictionary<int, PlayerBoardSummary>? boardSummaries)
        {
            int effectiveRounds = GetEffectiveRounds(player, surge, board.CurrentRound);
            float threshold = GetThreshold(player, surge);

            return surge.Id switch
            {
                MutationIds.HyphalSurge => EvaluateAutolyticSurge(player, board, projectedLevel, effectiveRounds, threshold),
                MutationIds.ChitinFortification => EvaluateChitinFortification(player, board, projectedLevel, effectiveRounds, threshold),
                MutationIds.NecroticClearance => EvaluateNecroticClearance(player, board, projectedLevel, effectiveRounds, threshold),
                MutationIds.ChemotacticBeacon => EvaluateChemotacticBeacon(player, board, surge, projectedLevel, effectiveRounds, threshold),
                MutationIds.MimeticResilience => EvaluateMimeticResilience(player, board, projectedLevel, effectiveRounds, threshold, boardSummaries),
                MutationIds.CompetitiveAntagonism => EvaluateCompetitiveAntagonism(player, board, effectiveRounds, threshold, boardSummaries),
                _ => SurgeOpportunity.Unmodeled,
            };
        }

        /// <summary>
        /// Rounds of the surge window that will actually be played. The round cap is exclusive and a surge
        /// bought this round is live for this round's growth and decay, so a 3-round surge on round 72 of
        /// a 75-cap game still gets all three.
        /// </summary>
        public static int GetEffectiveRounds(Player player, Mutation surge, int currentRound)
        {
            int roundsLeft = GameBalance.MaxNumberOfRoundsBeforeGameEndTrigger - currentRound;
            return Math.Max(0, Math.Min(player.GetSurgeDuration(surge), roundsLeft));
        }

        public static float GetThreshold(Player player, Mutation surge)
        {
            int cost = player.GetMutationPointCost(surge);
            return Math.Max(GameBalance.AiSurgeMinimumAbsoluteValue, cost * GameBalance.AiSurgeMinimumValuePerMutationPoint);
        }

        // ------------------------------------------------------------------
        // Autolytic Surge: growth bonus on every open frontier slot versus a
        // random-decay penalty on every unprotected living cell.
        // ------------------------------------------------------------------

        private static SurgeOpportunity EvaluateAutolyticSurge(
            Player player,
            GameBoard board,
            int projectedLevel,
            int effectiveRounds,
            float threshold)
        {
            float baseChance = GrowthMutationProcessor.GetEffectiveOrthogonalGrowthChance(player);
            float bonus = projectedLevel * GameBalance.HyphalSurgeEffectPerLevel;
            float penalty = projectedLevel * GameBalance.HyphalSurgeRandomDecayPenaltyPerLevel;

            float extraGrowthPerCycle = 0f;
            int unprotectedLiving = 0;
            foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!cell.IsAlive)
                    continue;

                if (!cell.IsResistant)
                    unprotectedLiving++;

                int openTargets = board.GetOrthogonalNeighbors(cell.TileId)
                    .Count(tile => !tile.IsOccupied && !board.IsTileBlockedForOccupation(tile.TileId));
                if (openTargets == 0)
                    continue;

                // Each open target is rolled independently, so the surge lifts the chance that at
                // least one roll lands.
                float withSurge = 1f - (float)Math.Pow(1f - Math.Min(1f, baseChance + bonus), openTargets);
                float without = 1f - (float)Math.Pow(1f - Math.Min(1f, baseChance), openTargets);
                extraGrowthPerCycle += withSurge - without;
            }

            float gain = extraGrowthPerCycle * GameBalance.TotalGrowthCycles * effectiveRounds;
            float rawLoss = unprotectedLiving * penalty * effectiveRounds;
            float discount = GetDeathSynergyDiscount(player);
            float loss = rawLoss * GameBalance.AiAutolyticDecayLossWeight * (1f - discount);
            float value = gain - loss;

            return new SurgeOpportunity(
                value,
                threshold,
                $"Autolytic: +{gain:0.0} growth vs -{loss:0.0} decay ({unprotectedLiving} unprotected, synergy discount {discount:P0}).");
        }

        /// <summary>
        /// Share of the Autolytic decay penalty that dead-cell payoffs give back. Necrosporulation and
        /// Regenerative Hyphae convert deaths into cells directly; Necrophytic Bloom and Detrital
        /// Enzymes turn corpses into substrate or growth chance, so they earn a flat allowance.
        /// </summary>
        public static float GetDeathSynergyDiscount(Player player)
        {
            float discount = 0f;
            discount += player.GetMutationLevel(MutationIds.Necrosporulation) * GameBalance.NecrosporulationEffectPerLevel;
            discount += player.GetMutationLevel(MutationIds.RegenerativeHyphae) * GameBalance.RegenerativeHyphaeReclaimChance;
            if (player.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                discount += GameBalance.AiAutolyticNecrophyticBloomDeathDiscount;
            if (player.GetMutationLevel(MutationIds.DetritalEnzymes) > 0)
                discount += GameBalance.AiAutolyticDetritalEnzymesDeathDiscount;
            return Math.Min(GameBalance.AiAutolyticMaxDeathSynergyDiscount, discount);
        }

        // ------------------------------------------------------------------
        // Chitin Fortification: permanent resistance on a fixed number of
        // cells per round, worth more while the colony is under contact.
        // ------------------------------------------------------------------

        private static SurgeOpportunity EvaluateChitinFortification(
            Player player,
            GameBoard board,
            int projectedLevel,
            int effectiveRounds,
            float threshold)
        {
            int unfortified = 0;
            int threatened = 0;
            foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!cell.IsAlive || cell.IsResistant)
                    continue;

                unfortified++;
                if (board.GetOrthogonalNeighbors(cell.TileId).Any(tile =>
                        tile.FungalCell is { } neighbor
                        && neighbor.OwnerPlayerId != player.PlayerId
                        && (neighbor.IsAlive || neighbor.IsToxin)))
                {
                    threatened++;
                }
            }

            int perRound = projectedLevel * GameBalance.ChitinFortificationCellsPerLevel;
            int requiredForFullValue = perRound * GameBalance.AiChitinMinimumFullRoundsOfCapacity;
            if (unfortified < requiredForFullValue)
            {
                return new SurgeOpportunity(
                    0f,
                    threshold,
                    $"Chitin: only {unfortified} unfortified cells for {perRound}/round; waiting for {requiredForFullValue}.");
            }

            int fortified = Math.Min(perRound * effectiveRounds, unfortified);
            float threatFraction = unfortified > 0 ? (float)threatened / unfortified : 0f;
            float value = fortified * GameBalance.AiChitinValuePerFortifiedCell * (1f + threatFraction);

            return new SurgeOpportunity(
                value,
                threshold,
                $"Chitin: {fortified} cells fortified, {threatFraction:P0} of colony in enemy contact.");
        }

        // ------------------------------------------------------------------
        // Necrotic Clearance: one roll per living cell with an adjacent own
        // corpse, doubled and worth far more when that corpse is contested.
        // ------------------------------------------------------------------

        private static SurgeOpportunity EvaluateNecroticClearance(
            Player player,
            GameBoard board,
            int projectedLevel,
            int effectiveRounds,
            float threshold)
        {
            float chancePerLevel = projectedLevel * GameBalance.NecroticClearanceChancePerLevel;
            float expectedValuePerRound = 0f;
            int contestedSources = 0;
            int sources = 0;
            foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!cell.IsAlive)
                    continue;

                bool hasCorpse = false;
                bool hasContestedCorpse = false;
                foreach (var tile in board.GetOrthogonalNeighbors(cell.TileId))
                {
                    if (tile.FungalCell is not { IsDead: true, IsToxin: false } corpse || corpse.OwnerPlayerId != player.PlayerId)
                        continue;

                    hasCorpse = true;
                    if (IsAdjacentToEnemyLiving(board, tile.TileId, player.PlayerId))
                    {
                        hasContestedCorpse = true;
                        break;
                    }
                }

                if (!hasCorpse)
                    continue;

                sources++;
                if (hasContestedCorpse)
                {
                    contestedSources++;
                    expectedValuePerRound += Math.Min(1f, chancePerLevel * 2f) * GameBalance.AiNecroticContestedClearValue;
                }
                else
                {
                    expectedValuePerRound += Math.Min(1f, chancePerLevel) * GameBalance.AiNecroticUncontestedClearValue;
                }
            }

            float value = expectedValuePerRound * effectiveRounds;
            return new SurgeOpportunity(
                value,
                threshold,
                $"Necrotic: {sources} living cells next to own corpses, {contestedSources} contested.");
        }

        private static bool IsAdjacentToEnemyLiving(GameBoard board, int tileId, int playerId)
            => board.GetOrthogonalNeighbors(tileId).Any(tile =>
                tile.FungalCell is { IsAlive: true } neighbor && neighbor.OwnerPlayerId != playerId);

        // ------------------------------------------------------------------
        // Chemotactic Beacon: guaranteed line growth, so the question is only
        // whether the best marker has enough path to spend the budget on.
        // ------------------------------------------------------------------

        private static SurgeOpportunity EvaluateChemotacticBeacon(
            Player player,
            GameBoard board,
            Mutation surge,
            int projectedLevel,
            int effectiveRounds,
            float threshold)
        {
            var candidate = ChemotacticBeaconHelper.TrySelectAITargetCandidate(player, board, projectedLevel, player.GetSurgeDuration(surge));
            if (candidate == null)
            {
                return new SurgeOpportunity(0f, threshold, "Beacon: no legal marker tile.");
            }

            int budget = ChemotacticBeaconHelper.GetTilesPerRound(projectedLevel) * effectiveRounds;
            int placements = Math.Min(candidate.ExpectedPlacements, budget);
            float value = placements * GameBalance.AiBeaconValuePerPlacement
                + candidate.EnemyLivingTilesCrossed * GameBalance.AiBeaconValuePerEnemyLivingCrossed
                + candidate.EnemyToxinsCrossed * GameBalance.AiBeaconValuePerEnemyToxinCrossed
                + candidate.NutrientValue * GameBalance.AiBeaconValuePerNutrientTile;

            return new SurgeOpportunity(
                value,
                threshold,
                $"Beacon: {placements} placements, {candidate.EnemyLivingTilesCrossed} enemy cells and {candidate.NutrientValue} nutrient tiles on the line.");
        }

        // ------------------------------------------------------------------
        // Mimetic Resilience: one resistant placement per rival resistant
        // cell per growth phase, with a 5% decay per success against a rival.
        // ------------------------------------------------------------------

        private static SurgeOpportunity EvaluateMimeticResilience(
            Player player,
            GameBoard board,
            int projectedLevel,
            int effectiveRounds,
            float threshold,
            IReadOnlyDictionary<int, PlayerBoardSummary>? boardSummaries)
        {
            boardSummaries ??= BoardUtilities.GetPlayerBoardSummaries(board.Players, board);
            var targets = MycelialSurgeMutationProcessor.GetMimeticResilienceTargets(player, board.Players, board, boardSummaries);

            float placementsPerRound = 0f;
            int sourceCells = 0;
            foreach (var target in targets)
            {
                if (!boardSummaries.TryGetValue(target.PlayerId, out var summary))
                    continue;

                int sources = Math.Min(summary.ResistantCells, 20);
                sourceCells += summary.ResistantCells;
                for (int success = 0; success < sources; success++)
                {
                    placementsPerRound += Math.Max(0f, 1f - 0.05f * success);
                }
            }

            float value = placementsPerRound * effectiveRounds * GameBalance.AiMimeticValuePerPlacement;
            return new SurgeOpportunity(
                value,
                threshold,
                $"Mimetic: {targets.Count} eligible rivals with {sourceCells} resistant cells to copy.");
        }

        // ------------------------------------------------------------------
        // Competitive Antagonism: a targeting modifier, so its worth scales
        // with the toxin output it redirects.
        // ------------------------------------------------------------------

        private static SurgeOpportunity EvaluateCompetitiveAntagonism(
            Player player,
            GameBoard board,
            int effectiveRounds,
            float threshold,
            IReadOnlyDictionary<int, PlayerBoardSummary>? boardSummaries)
        {
            boardSummaries ??= BoardUtilities.GetPlayerBoardSummaries(board.Players, board);
            var targets = MycelialSurgeMutationProcessor.GetCompetitiveAntagonismTargets(player, board.Players, board, boardSummaries);
            if (targets.Count == 0)
            {
                return new SurgeOpportunity(0f, threshold, "Antagonism: no larger rival to redirect toxins at.");
            }

            int tracerLevel = player.GetMutationLevel(MutationIds.MycotoxinTracer);
            int sporicidalLevel = player.GetMutationLevel(MutationIds.SporicidalBloom);
            float perRound = tracerLevel * GameBalance.AiAntagonismValuePerTracerLevelRound
                + sporicidalLevel * GameBalance.AiAntagonismValuePerSporicidalLevelRound;
            float value = perRound * effectiveRounds;

            return new SurgeOpportunity(
                value,
                threshold,
                $"Antagonism: {targets.Count} larger rivals, Tracer {tracerLevel}, Sporicidal Bloom {sporicidalLevel}.");
        }
    }
}
