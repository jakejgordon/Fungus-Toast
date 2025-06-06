using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.AI;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class MatchupRunner
    {
        private readonly GameSimulator simulator = new();

        /// <summary>
        /// Run a head-to-head matchup between two strategies across many games.
        /// Players alternate between A and B for fairness. Requires even player count.
        /// </summary>
        public List<GameResult> RunMatchups(
            IMutationSpendingStrategy strategyA,
            IMutationSpendingStrategy strategyB,
            int gamesToPlay,
            int playersPerGame = 4)
        {
            if (playersPerGame % 2 != 0)
                throw new ArgumentException("playersPerGame must be even for fair A vs B matchup");

            var results = new List<GameResult>();
            var startTime = DateTime.UtcNow;

            for (int i = 0; i < gamesToPlay; i++)
            {
                var rng = new Random(i); // Deterministic seed for reproducibility

                var strategies = new List<IMutationSpendingStrategy>();
                for (int j = 0; j < playersPerGame; j++)
                {
                    // Alternate assignment for fairness
                    bool flip = (i % 2 == 1);
                    bool assignA = (j % 2 == 0) ^ flip;

                    var strategy = assignA ? strategyA : strategyB;
                    strategies.Add(strategy);
                }

                var context = new SimulationTrackingContext();

                var result = simulator.RunSimulation(
                    strategies,
                    seed: i,
                    gameIndex: i + 1,
                    totalGames: gamesToPlay,
                    startTime: startTime,
                    context: context
                );

                ApplyTrackingContext(result, context);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Run games using a fixed list of strategies per match.
        /// Useful for 1–8 player simulation loops.
        /// </summary>
        public List<GameResult> RunMatchups(List<IMutationSpendingStrategy> strategies, int gamesToPlay)
        {
            var results = new List<GameResult>();
            var startTime = DateTime.UtcNow;

            for (int i = 0; i < gamesToPlay; i++)
            {
                var shuffled = strategies.OrderBy(_ => Guid.NewGuid()).ToList();
                var context = new SimulationTrackingContext();

                var result = simulator.RunSimulation(
                    shuffled,
                    seed: i,
                    gameIndex: i + 1,
                    totalGames: gamesToPlay,
                    startTime: startTime,
                    context: context
                );

                ApplyTrackingContext(result, context);
                results.Add(result);
            }

            return results;
        }

        private void ApplyTrackingContext(GameResult result, SimulationTrackingContext context)
        {
            foreach (var pr in result.PlayerResults)
            {
                pr.CreepingMoldMoves = context.GetCreepingMoldMoves(pr.PlayerId);
                pr.ReclaimedCells = context.GetReclaimedCells(pr.PlayerId);
                pr.SporocidalSpores = context.GetSporocidalSporeDropCount(pr.PlayerId);
                pr.NecrosporulationSpores = context.GetNecrosporeDropCount(pr.PlayerId);
                pr.NecrophyticSpores = context.GetNecrophyticBloomSporeDropCount(pr.PlayerId);      // updated
                pr.NecrophyticReclaims = context.GetNecrophyticBloomReclaims(pr.PlayerId);  // updated
                pr.MycotoxinTracerSpores = context.GetMycotoxinSporeDropCount(pr.PlayerId);
            }
        }

    }
}
