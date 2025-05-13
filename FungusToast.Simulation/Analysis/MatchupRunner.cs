using System;
using System.Collections.Generic;
using FungusToast.Core.AI;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.GameSimulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class MatchupRunner
    {
        private readonly GameSimulator simulator = new();

        public List<GameResult> RunMatchups(
            IMutationSpendingStrategy strategyA,
            IMutationSpendingStrategy strategyB,
            int gamesToPlay,
            int playersPerGame = 4)
        {
            if (playersPerGame % 2 != 0)
                throw new ArgumentException("playersPerGame must be even for fair A vs B matchup");

            var results = new List<GameResult>();

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

                var result = simulator.RunSimulation(strategies, seed: i);
                results.Add(result);
            }

            return results;
        }
    }
}
