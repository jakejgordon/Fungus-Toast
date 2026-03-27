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
using FungusToast.Unity.Campaign;

namespace FungusToast.Unity
{
    public class PlayerInitializer
    {
        private readonly GridVisualizer gridVisualizer;
        private readonly GameUIManager ui;
        private readonly Func<int> getConfiguredHumanCount;
        private readonly Func<GameMode> getGameMode;
        private readonly Func<BoardPreset> getCampaignBoardPreset;
        private readonly Func<IReadOnlyList<string>> getResolvedCampaignAiStrategyNames;

        public PlayerInitializer(
            GridVisualizer gridVisualizer,
            GameUIManager ui,
            Func<int> getConfiguredHumanCount,
            Func<GameMode> getGameMode,
            Func<BoardPreset> getCampaignBoardPreset,
            Func<IReadOnlyList<string>> getResolvedCampaignAiStrategyNames)
        {
            this.gridVisualizer = gridVisualizer;
            this.ui = ui;
            this.getConfiguredHumanCount = getConfiguredHumanCount;
            this.getGameMode = getGameMode;
            this.getCampaignBoardPreset = getCampaignBoardPreset;
            this.getResolvedCampaignAiStrategyNames = getResolvedCampaignAiStrategyNames;
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
                var aiStrats = ResolveAIStrategiesForCurrentMode(remaining);
                for (int i = 0; i < aiStrats.Count && i < remaining; i++)
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

        private List<IMutationSpendingStrategy> ResolveAIStrategiesForCurrentMode(int remaining)
        {
            if (remaining <= 0)
            {
                return new List<IMutationSpendingStrategy>();
            }

            if (getGameMode() == GameMode.Campaign)
            {
                var preset = getCampaignBoardPreset();
                var configuredNames = getResolvedCampaignAiStrategyNames?.Invoke() ?? Array.Empty<string>();
                if (preset != null && configuredNames.Count > 0)
                {
                    var resolved = new List<IMutationSpendingStrategy>();
                    for (int i = 0; i < configuredNames.Count && resolved.Count < remaining; i++)
                    {
                        string strategyName = configuredNames[i];
                        if (string.IsNullOrWhiteSpace(strategyName))
                        {
                            continue;
                        }

                        if (AIRoster.CampaignStrategiesByName.TryGetValue(strategyName, out var campaignStrategy))
                        {
                            resolved.Add(campaignStrategy);
                            continue;
                        }

                        if (AIRoster.ProvenStrategiesByName.TryGetValue(strategyName, out var provenStrategy))
                        {
                            resolved.Add(provenStrategy);
                            continue;
                        }

                        Debug.LogWarning($"[PlayerInitializer] Unknown campaign AI strategy '{strategyName}'.");
                    }

                    if (resolved.Count == remaining)
                    {
                        return resolved;
                    }

                    Debug.LogWarning($"[PlayerInitializer] Campaign preset '{preset.presetId}' resolved {resolved.Count}/{remaining} AI strategies; filling with random campaign strategies.");
                    var fallbackCampaign = AIRoster.GetStrategies(remaining, StrategySetEnum.Campaign)
                        .OrderBy(_ => UnityEngine.Random.value)
                        .ToList();
                    return fallbackCampaign;
                }
            }

            return AIRoster.GetStrategies(remaining, StrategySetEnum.Proven)
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();
        }
    }
}
