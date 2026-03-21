using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Models;
using Parquet.Serialization;
using System.Text.Json;

namespace FungusToast.Simulation.Export
{
    public static class SimulationParquetExporter
    {
        private const string ExportRootFolderName = "SimulationParquet";

        public static string Export(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            string baseOutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExportRootFolderName);
            string runFolder = Path.Combine(baseOutputDir, metadata.ExperimentId);
            Directory.CreateDirectory(runFolder);

            var gameRows = BuildGameRows(batchResult, metadata);
            var playerRows = BuildPlayerRows(batchResult, metadata);
            var mutationRows = BuildMutationRows(batchResult, metadata);
            var mycovariantRows = BuildMycovariantRows(batchResult, metadata);
            var upgradeEventRows = BuildUpgradeEventRows(batchResult, metadata);

            string gamesPath = Path.Combine(runFolder, "games.parquet");
            string playersPath = Path.Combine(runFolder, "players.parquet");
            string mutationsPath = Path.Combine(runFolder, "mutations.parquet");
            string mycovariantsPath = Path.Combine(runFolder, "mycovariants.parquet");
            string upgradeEventsPath = Path.Combine(runFolder, "upgrade_events.parquet");

            bool wroteGames = WriteParquet(gamesPath, gameRows);
            bool wrotePlayers = WriteParquet(playersPath, playerRows);
            bool wroteMutations = WriteParquet(mutationsPath, mutationRows);
            bool wroteMycovariants = WriteParquet(mycovariantsPath, mycovariantRows);
            bool wroteUpgradeEvents = WriteParquet(upgradeEventsPath, upgradeEventRows);

            var manifest = new
            {
                schemaVersion = "v4",
                metadata.ExperimentId,
                metadata.RunTimestampUtc,
                strategySet = metadata.StrategySet.ToString(),
                strategySelectionPolicy = metadata.StrategySelectionPolicy.ToString(),
                strategySelectionSource = metadata.StrategySelectionSource.ToString(),
                selectedStrategyLineup = metadata.SelectedStrategies,
                metadata.BaseSeed,
                slotAssignmentPolicy = metadata.SlotAssignmentPolicy.ToString(),
                metadata.NumberOfPlayers,
                metadata.NumberOfGamesRequested,
                actualGamesExported = batchResult.GameResults.Count,
                metadata.BoardWidth,
                metadata.BoardHeight,
                files = new
                {
                    games = wroteGames ? Path.GetFileName(gamesPath) : null,
                    players = wrotePlayers ? Path.GetFileName(playersPath) : null,
                    mutations = wroteMutations ? Path.GetFileName(mutationsPath) : null,
                    mycovariants = wroteMycovariants ? Path.GetFileName(mycovariantsPath) : null,
                    upgradeEvents = wroteUpgradeEvents ? Path.GetFileName(upgradeEventsPath) : null
                },
                rowCounts = new
                {
                    games = gameRows.Count,
                    players = playerRows.Count,
                    mutations = mutationRows.Count,
                    mycovariants = mycovariantRows.Count,
                    upgradeEvents = upgradeEventRows.Count
                }
            };

            string manifestPath = Path.Combine(runFolder, "manifest.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            return runFolder;
        }

        private static bool WriteParquet<T>(string filePath, List<T> rows)
        {
            if (rows.Count == 0)
            {
                return false;
            }

            using var stream = File.Create(filePath);
            ParquetSerializer.SerializeAsync(rows, stream).GetAwaiter().GetResult();
            return true;
        }

        private static List<GameExportRow> BuildGameRows(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            var rows = new List<GameExportRow>(batchResult.GameResults.Count);

            foreach (var game in batchResult.GameResults)
            {
                rows.Add(new GameExportRow
                {
                    ExperimentId = metadata.ExperimentId,
                    RunTimestampUtc = metadata.RunTimestampUtc,
                    GameIndex = game.GameIndex,
                    GameSeed = game.GameSeed,
                    StrategySet = metadata.StrategySet.ToString(),
                    StrategySelectionPolicy = metadata.StrategySelectionPolicy.ToString(),
                    StrategySelectionSource = metadata.StrategySelectionSource.ToString(),
                    SelectedStrategyLineup = string.Join("|", metadata.SelectedStrategies.OrderBy(s => s.LineupOrder).Select(s => s.StrategyName)),
                    SlotAssignmentPolicy = metadata.SlotAssignmentPolicy.ToString(),
                    BoardWidth = metadata.BoardWidth,
                    BoardHeight = metadata.BoardHeight,
                    PlayerCount = game.PlayerResults.Count,
                    TurnsPlayed = game.TurnsPlayed,
                    WinnerPlayerId = game.WinnerId,
                    ToxicTileCount = game.ToxicTileCount,
                    ParityAllPassed = game.ParityInvariantReport?.AllPassed ?? true
                });
            }

            return rows;
        }

        private static List<PlayerExportRow> BuildPlayerRows(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            var rows = new List<PlayerExportRow>();

            foreach (var game in batchResult.GameResults)
            {
                var playerThemeById = game.PlayerResults
                    .ToDictionary(
                        p => p.PlayerId,
                        p => AIRoster.GetThemeForStrategy(p.Strategy).ToString());

                foreach (var player in game.PlayerResults)
                {
                    var lineupEntry = metadata.SelectedStrategies
                        .FirstOrDefault(s => string.Equals(s.StrategyName, player.StrategyName, StringComparison.OrdinalIgnoreCase));

                    var opponentThemes = game.PlayerResults
                        .Where(p => p.PlayerId != player.PlayerId)
                        .Select(p => playerThemeById[p.PlayerId])
                        .ToList();

                    var dominantOpponentTheme = opponentThemes
                        .GroupBy(t => t, StringComparer.Ordinal)
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => g.Key, StringComparer.Ordinal)
                        .Select(g => g.Key)
                        .FirstOrDefault() ?? "None";

                    var opponentThemeSet = string.Join("|", opponentThemes
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(t => t, StringComparer.Ordinal));

                    rows.Add(new PlayerExportRow
                    {
                        ExperimentId = metadata.ExperimentId,
                        GameIndex = game.GameIndex,
                        GameSeed = game.GameSeed,
                        PlayerId = player.PlayerId,
                        AssignedSlot = player.PlayerId,
                        SelectedLineupOrder = lineupEntry?.LineupOrder ?? 0,
                        StrategyName = player.StrategyName,
                        StrategyTheme = AIRoster.GetThemeForStrategy(player.Strategy).ToString(),
                        StrategyStatus = lineupEntry?.StrategyStatus ?? AIRoster.GetStatusForStrategy(player.Strategy, metadata.StrategySet).ToString(),
                        DominantOpponentTheme = dominantOpponentTheme,
                        OpponentThemeSet = opponentThemeSet,
                        UniqueOpponentThemes = opponentThemes.Distinct(StringComparer.Ordinal).Count(),
                        IsWinner = player.PlayerId == game.WinnerId,
                        LivingCells = player.LivingCells,
                        DeadCells = player.DeadCells,
                        EndGameToxinCells = player.EndGameToxinCells,
                        NutrientClaims = player.NutrientPatchesConsumed,
                        NutrientMutationPointsEarned = player.NutrientMutationPointsEarned,
                        AvgNutrientClusterSize = player.NutrientPatchesConsumed > 0
                            ? (float)player.NutrientMutationPointsEarned / player.NutrientPatchesConsumed
                            : 0f,
                        MutationPointIncome = player.MutationPointIncome,
                        TotalMutationPointsSpent = player.TotalMutationPointsSpent,
                        BankedPoints = player.BankedPoints,
                        EffectiveGrowthChance = player.EffectiveGrowthChance,
                        EffectiveSelfDeathChance = player.EffectiveSelfDeathChance,
                        OffensiveDecayModifier = player.OffensiveDecayModifier,
                        AvgAIScoreAtDraft = player.AvgAIScoreAtDraft
                    });
                }
            }

            return rows;
        }

        private static List<MutationExportRow> BuildMutationRows(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            var rows = new List<MutationExportRow>();

            foreach (var game in batchResult.GameResults)
            {
                foreach (var player in game.PlayerResults)
                {
                    foreach (var mutationLevel in player.MutationLevels)
                    {
                        int mutationId = mutationLevel.Key;
                        int level = mutationLevel.Value;
                        if (level <= 0)
                        {
                            continue;
                        }

                        var mutation = MutationRegistry.GetById(mutationId);
                        if (mutation == null)
                        {
                            continue;
                        }

                        var firstUpgradeStats = game.TrackingContext.GetFirstUpgradeStatsByStrategy(
                            player.PlayerId,
                            player.StrategyName,
                            mutationId);

                        rows.Add(new MutationExportRow
                        {
                            ExperimentId = metadata.ExperimentId,
                            GameIndex = game.GameIndex,
                            GameSeed = game.GameSeed,
                            PlayerId = player.PlayerId,
                            StrategyName = player.StrategyName,
                            StrategyTheme = AIRoster.GetThemeForStrategy(player.Strategy).ToString(),
                            MutationId = mutationId,
                            MutationName = mutation.Name,
                            MutationTier = mutation.Tier.ToString(),
                            MutationCategory = mutation.Category.ToString(),
                            MutationLevel = level,
                            FirstUpgradeRound = firstUpgradeStats.count > 0 ? firstUpgradeStats.min : null
                        });
                    }
                }
            }

            return rows;
        }

        private static List<MycovariantExportRow> BuildMycovariantRows(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            var rows = new List<MycovariantExportRow>();

            foreach (var game in batchResult.GameResults)
            {
                foreach (var player in game.PlayerResults)
                {
                    foreach (var myco in player.Mycovariants)
                    {
                        if (myco.EffectCounts.Count == 0)
                        {
                            rows.Add(new MycovariantExportRow
                            {
                                ExperimentId = metadata.ExperimentId,
                                GameIndex = game.GameIndex,
                                GameSeed = game.GameSeed,
                                PlayerId = player.PlayerId,
                                StrategyName = player.StrategyName,
                                StrategyTheme = AIRoster.GetThemeForStrategy(player.Strategy).ToString(),
                                MycovariantId = myco.MycovariantId,
                                MycovariantName = myco.MycovariantName,
                                MycovariantType = myco.MycovariantType,
                                IsUniversal = myco.IsUniversal,
                                Triggered = myco.Triggered,
                                AIScoreAtDraft = myco.AIScoreAtDraft,
                                EffectType = "-",
                                EffectValue = 0
                            });

                            continue;
                        }

                        foreach (var effect in myco.EffectCounts)
                        {
                            rows.Add(new MycovariantExportRow
                            {
                                ExperimentId = metadata.ExperimentId,
                                GameIndex = game.GameIndex,
                                GameSeed = game.GameSeed,
                                PlayerId = player.PlayerId,
                                StrategyName = player.StrategyName,
                                StrategyTheme = AIRoster.GetThemeForStrategy(player.Strategy).ToString(),
                                MycovariantId = myco.MycovariantId,
                                MycovariantName = myco.MycovariantName,
                                MycovariantType = myco.MycovariantType,
                                IsUniversal = myco.IsUniversal,
                                Triggered = myco.Triggered,
                                AIScoreAtDraft = myco.AIScoreAtDraft,
                                EffectType = effect.Key,
                                EffectValue = effect.Value
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static List<MutationUpgradeEventExportRow> BuildUpgradeEventRows(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            var rows = new List<MutationUpgradeEventExportRow>();

            foreach (var game in batchResult.GameResults)
            {
                foreach (var upgradeEvent in game.TrackingContext.GetMutationUpgradeEvents())
                {
                    var strategyName = game.PlayerResults
                        .FirstOrDefault(pr => pr.PlayerId == upgradeEvent.PlayerId)
                        ?.StrategyName ?? "Unknown";

                    rows.Add(new MutationUpgradeEventExportRow
                    {
                        ExperimentId = metadata.ExperimentId,
                        GameIndex = game.GameIndex,
                        GameSeed = game.GameSeed,
                        PlayerId = upgradeEvent.PlayerId,
                        StrategyName = strategyName,
                        StrategyTheme = TryGetStrategyTheme(game, upgradeEvent.PlayerId),
                        Round = upgradeEvent.Round,
                        MutationId = upgradeEvent.MutationId,
                        MutationName = upgradeEvent.MutationName,
                        MutationTier = upgradeEvent.MutationTier.ToString(),
                        OldLevel = upgradeEvent.OldLevel,
                        NewLevel = upgradeEvent.NewLevel,
                        MutationPointsBefore = upgradeEvent.MutationPointsBefore,
                        MutationPointsAfter = upgradeEvent.MutationPointsAfter,
                        PointsSpent = upgradeEvent.PointsSpent,
                        UpgradeSource = upgradeEvent.UpgradeSource
                    });
                }
            }

            return rows;
        }

        private static string TryGetStrategyTheme(GameResult game, int playerId)
        {
            var player = game.PlayerResults.FirstOrDefault(pr => pr.PlayerId == playerId);
            if (player == null)
            {
                return "Unknown";
            }

            return AIRoster.GetThemeForStrategy(player.Strategy).ToString();
        }
    }
}
