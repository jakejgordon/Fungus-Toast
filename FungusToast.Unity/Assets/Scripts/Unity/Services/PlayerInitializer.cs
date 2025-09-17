using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Config;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;

namespace FungusToast.Unity
{
    public class PlayerInitializer
    {
        private readonly GridVisualizer gridVisualizer;
        private readonly GameUIManager ui;
        private readonly Func<int> getConfiguredHumanCount;

        public PlayerInitializer(GridVisualizer gridVisualizer, GameUIManager ui, Func<int> getConfiguredHumanCount)
        {
            this.gridVisualizer = gridVisualizer;
            this.ui = ui;
            this.getConfiguredHumanCount = getConfiguredHumanCount;
        }

        // totalPlayers: authoritative total player count for this game (from GameManager)
        public void InitializePlayers(GameBoard board, List<Player> players, List<Player> humanPlayers, out Player primaryHuman, int totalPlayers)
        {
            players.Clear();
            humanPlayers.Clear();
            int baseMP = GameBalance.StartingMutationPoints;

            // Clamp human players against requested totalPlayers instead of board.Players.Count (which is 0 pre-init)
            int configuredHumans = getConfiguredHumanCount();
            if (configuredHumans < 1) configuredHumans = 1;
            if (configuredHumans > totalPlayers) configuredHumans = totalPlayers;
            int desiredHuman = configuredHumans;

            // Create human players
            for (int i = 0; i < desiredHuman; i++)
            {
                var hp = new Player(i, desiredHuman > 1 ? $"Human {i + 1}" : "Human", PlayerTypeEnum.Human, AITypeEnum.Random);
                hp.SetBaseMutationPoints(baseMP);
                players.Add(hp);
                humanPlayers.Add(hp);
            }

            // Safety: ensure at least one human
            if (humanPlayers.Count == 0)
            {
                var fallback = new Player(0, "Human", PlayerTypeEnum.Human, AITypeEnum.Random);
                fallback.SetBaseMutationPoints(baseMP);
                players.Add(fallback);
                humanPlayers.Add(fallback);
            }
            primaryHuman = humanPlayers[0];

            int remaining = totalPlayers - humanPlayers.Count;
            if (remaining > 0)
            {
                var aiStrats = AIRoster.GetStrategies(remaining, StrategySetEnum.Proven).OrderBy(_ => UnityEngine.Random.value).ToList();
                for (int i = 0; i < aiStrats.Count; i++)
                {
                    int id = humanPlayers.Count + i;
                    var ai = new Player(id, $"AI Player {id}", PlayerTypeEnum.AI, AITypeEnum.Random);
                    ai.SetBaseMutationPoints(baseMP);
                    ai.SetMutationStrategy(aiStrats[i]);
                    players.Add(ai);
                }
            }

            board.Players.Clear();
            board.Players.AddRange(players);

            // Icons
            foreach (var p in players)
            {
                var icon = gridVisualizer.GetTileForPlayer(p.PlayerId)?.sprite;
                if (icon != null) ui.PlayerUIBinder.AssignIcon(p, icon);
            }

            ui.RightSidebar?.SetGridVisualizer(gridVisualizer);
            ui.RightSidebar?.InitializePlayerSummaries(board.Players);
        }
    }
}
