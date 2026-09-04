using FungusToast.Core.AI;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Experiments;
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
            var livingCellSourceRows = BuildLivingCellSourceRows(batchResult, metadata);
            var mutationRows = BuildMutationRows(batchResult, metadata);
            var mycovariantRows = BuildMycovariantRows(batchResult, metadata);
            var upgradeEventRows = BuildUpgradeEventRows(batchResult, metadata);

            string gamesPath = Path.Combine(runFolder, "games.parquet");
            string playersPath = Path.Combine(runFolder, "players.parquet");
            string livingCellSourcesPath = Path.Combine(runFolder, "living_cell_sources.parquet");
            string mutationsPath = Path.Combine(runFolder, "mutations.parquet");
            string mycovariantsPath = Path.Combine(runFolder, "mycovariants.parquet");
            string upgradeEventsPath = Path.Combine(runFolder, "upgrade_events.parquet");

            bool wroteGames = WriteParquet(gamesPath, gameRows);
            bool wrotePlayers = WriteParquet(playersPath, playerRows);
            bool wroteLivingCellSources = WriteParquet(livingCellSourcesPath, livingCellSourceRows);
            bool wroteMutations = WriteParquet(mutationsPath, mutationRows);
            bool wroteMycovariants = WriteParquet(mycovariantsPath, mycovariantRows);
            bool wroteUpgradeEvents = WriteParquet(upgradeEventsPath, upgradeEventRows);

            var manifest = new
            {
                schemaVersion = "v5",
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
                    livingCellSources = wroteLivingCellSources ? Path.GetFileName(livingCellSourcesPath) : null,
                    mutations = wroteMutations ? Path.GetFileName(mutationsPath) : null,
                    mycovariants = wroteMycovariants ? Path.GetFileName(mycovariantsPath) : null,
                    upgradeEvents = wroteUpgradeEvents ? Path.GetFileName(upgradeEventsPath) : null
                },
                rowCounts = new
                {
                    games = gameRows.Count,
                    players = playerRows.Count,
                    livingCellSources = livingCellSourceRows.Count,
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

            var resolvedFiles = new Dictionary<string, ResolvedOutputFile>(StringComparer.Ordinal);
            AddResolvedOutputFile(resolvedFiles, "games", gamesPath, wroteGames);
            AddResolvedOutputFile(resolvedFiles, "players", playersPath, wrotePlayers);
            AddResolvedOutputFile(resolvedFiles, "livingCellSources", livingCellSourcesPath, wroteLivingCellSources);
            AddResolvedOutputFile(resolvedFiles, "mutations", mutationsPath, wroteMutations);
            AddResolvedOutputFile(resolvedFiles, "mycovariants", mycovariantsPath, wroteMycovariants);
            AddResolvedOutputFile(resolvedFiles, "upgradeEvents", upgradeEventsPath, wroteUpgradeEvents);
            AddResolvedOutputFile(resolvedFiles, "legacyManifest", manifestPath, wrote: true);

            var resolvedManifest = ResolvedExperimentManifestFactory.Create(
                batchResult,
                metadata,
                resolvedFiles,
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["games"] = gameRows.Count,
                    ["players"] = playerRows.Count,
                    ["livingCellSources"] = livingCellSourceRows.Count,
                    ["mutations"] = mutationRows.Count,
                    ["mycovariants"] = mycovariantRows.Count,
                    ["upgradeEvents"] = upgradeEventRows.Count
                });
            string resolvedManifestPath = Path.Combine(runFolder, "resolved-manifest.json");
            File.WriteAllText(resolvedManifestPath, ResolvedExperimentManifestJson.Serialize(resolvedManifest));
            var resolvedManifestSha256 = ExperimentFingerprint.ForFile(resolvedManifestPath);
            File.WriteAllText(
                Path.Combine(runFolder, "resolved-manifest.sha256"),
                $"{resolvedManifestSha256}  {Path.GetFileName(resolvedManifestPath)}{Environment.NewLine}");
            ExperimentRunStateStore.MarkFinished(
                metadata,
                resolvedManifest.Sampling.CompletionStatus,
                resolvedManifestSha256);

            return runFolder;
        }

        private static void AddResolvedOutputFile(
            IDictionary<string, ResolvedOutputFile> files,
            string key,
            string path,
            bool wrote)
        {
            if (!wrote) return;
            files[key] = new ResolvedOutputFile
            {
                FileName = Path.GetFileName(path),
                Sha256 = ExperimentFingerprint.ForFile(path)
            };
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
            var conditionFingerprint = ExperimentFingerprint.ForCondition(metadata.Condition);
            var boardFingerprint = ExperimentFingerprint.ForBoard(metadata.Condition.Board);

            foreach (var game in batchResult.GameResults)
            {
                rows.Add(new GameExportRow
                {
                    ExperimentId = metadata.ExperimentId,
                    ConditionId = metadata.Condition.ConditionId,
                    ConditionFingerprint = conditionFingerprint,
                    RunTimestampUtc = metadata.RunTimestampUtc,
                    GameIndex = game.GameIndex,
                    GameSeed = game.GameSeed,
                    StrategySet = metadata.StrategySet.ToString(),
                    StrategySelectionPolicy = metadata.StrategySelectionPolicy.ToString(),
                    StrategySelectionSource = metadata.StrategySelectionSource.ToString(),
                    SelectedStrategyLineup = string.Join("|", metadata.SelectedStrategies.OrderBy(s => s.LineupOrder).Select(s => s.StrategyName)),
                    AssignedStrategyLineup = string.Join("|", game.PlayerResults.OrderBy(player => player.PlayerId).Select(player => player.StrategyName)),
                    SlotAssignmentPolicy = metadata.SlotAssignmentPolicy.ToString(),
                    BoardWidth = metadata.BoardWidth,
                    BoardHeight = metadata.BoardHeight,
                    BoardGeometryId = metadata.Condition.Board.GeometryId,
                    BoardGeometryFingerprint = boardFingerprint,
                    BlockedTileCount = metadata.Condition.Board.BlockedTileIds.Count,
                    BlockedTileIds = string.Join(",", metadata.Condition.Board.BlockedTileIds.OrderBy(id => id)),
                    PlayerCount = game.PlayerResults.Count,
                    NutrientPatchesEnabled = metadata.Condition.Systems.NutrientPatchesEnabled,
                    MycovariantDraftEnabled = metadata.Condition.Systems.MycovariantDraftEnabled,
                    StartingPositionMode = GetStartingPositionMode(metadata.Condition.Positioning),
                    ConfiguredStartingPositions = FormatCoordinates(metadata.Condition.Positioning.ExactStartingPositions),
                    ConfiguredPreferredPositionPools = FormatPositionPools(metadata.Condition.Positioning.PreferredPositionPools),
                    ConfiguredStartingAdaptations = FormatConfiguredAdaptations(metadata.Condition.Systems.StartingAdaptations),
                    ActualStartingPositions = FormatActualStartingPositions(game),
                    ActualStartingAdaptations = FormatActualStartingAdaptations(game),
                    TurnsPlayed = game.TurnsPlayed,
                    WinnerPlayerId = game.WinnerId,
                    WinnerPlayerIds = string.Join("|", game.WinnerIds.OrderBy(id => id)),
                    ToxicTileCount = game.ToxicTileCount,
                    NutrientPatchCount = game.NutrientPatchCount,
                    ParityAllPassed = game.ParityInvariantReport?.AllPassed ?? true
                });
            }

            return rows;
        }

        private static List<PlayerExportRow> BuildPlayerRows(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            var rows = new List<PlayerExportRow>();
            var conditionFingerprint = ExperimentFingerprint.ForCondition(metadata.Condition);
            var boardFingerprint = ExperimentFingerprint.ForBoard(metadata.Condition.Board);

            foreach (var game in batchResult.GameResults)
            {
                int totalLivingCells = game.PlayerResults.Sum(player => player.LivingCells);
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
                        ConditionId = metadata.Condition.ConditionId,
                        ConditionFingerprint = conditionFingerprint,
                        InputSchemaVersion = metadata.InputSchemaVersion,
                        StrategySet = metadata.Condition.Strategies.StrategySet.ToString(),
                        StrategySelectionPolicy = metadata.Condition.Strategies.SelectionPolicy.ToString(),
                        SlotAssignmentPolicy = metadata.Condition.SlotAssignmentPolicy.ToString(),
                        StartingPositionMode = GetStartingPositionMode(metadata.Condition.Positioning),
                        GameIndex = game.GameIndex,
                        GameSeed = game.GameSeed,
                        PlayerId = player.PlayerId,
                        AssignedSlot = player.PlayerId,
                        SelectedLineupOrder = lineupEntry?.LineupOrder ?? 0,
                        StrategyName = player.StrategyName,
                        StrategyTheme = AIRoster.GetThemeForStrategy(player.Strategy).ToString(),
                        StrategyStatus = lineupEntry?.StrategyStatus ?? AIRoster.GetStatusForStrategy(player.Strategy, metadata.StrategySet).ToString(),
                        StartingX = game.StartingPositionsByPlayerId[player.PlayerId].x,
                        StartingY = game.StartingPositionsByPlayerId[player.PlayerId].y,
                        StartingAdaptationIds = game.StartingAdaptationIdsByPlayerId.TryGetValue(player.PlayerId, out var adaptationIds)
                            ? string.Join("|", adaptationIds.OrderBy(id => id, StringComparer.Ordinal))
                            : string.Empty,
                        BoardWidth = metadata.BoardWidth,
                        BoardHeight = metadata.BoardHeight,
                        BoardGeometryId = metadata.Condition.Board.GeometryId,
                        BoardGeometryFingerprint = boardFingerprint,
                        BlockedTileCount = metadata.Condition.Board.BlockedTileIds.Count,
                        PlayerCount = metadata.Condition.PlayerCount,
                        NutrientPatchesEnabled = metadata.Condition.Systems.NutrientPatchesEnabled,
                        MycovariantDraftEnabled = metadata.Condition.Systems.MycovariantDraftEnabled,
                        DominantOpponentTheme = dominantOpponentTheme,
                        OpponentThemeSet = opponentThemeSet,
                        UniqueOpponentThemes = opponentThemes.Distinct(StringComparer.Ordinal).Count(),
                        IsWinner = game.IsWinningPlayer(player.PlayerId),
                        WinCredit = game.GetWinCredit(player.PlayerId),
                        OutcomeStatus = game.IsWinningPlayer(player.PlayerId)
                            ? game.WinnerIds.Count > 1 ? "co_winner" : "winner"
                            : "loser",
                        LivingCells = player.LivingCells,
                        TotalLivingCells = totalLivingCells,
                        FinalRank = FinalPlacementCalculator.GetCompetitionRank(game.PlayerResults, player.PlayerId),
                        PlayersTiedAtFinalRank = FinalPlacementCalculator.GetTieCount(game.PlayerResults, player.PlayerId),
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
                        FilamentOverdriveTriggers = player.FilamentOverdriveTriggers,
                        FilamentOverdriveBonusCells = player.FilamentOverdriveBonusCells,
                        FilamentOverdriveSourceDeaths = player.FilamentOverdriveTriggers,
                        FilamentOverdriveNetImmediateCells = player.FilamentOverdriveBonusCells - player.FilamentOverdriveTriggers,
                        AvgAIScoreAtDraft = player.AvgAIScoreAtDraft
                    });
                }
            }

            return rows;
        }

        private static List<LivingCellSourceExportRow> BuildLivingCellSourceRows(SimulationBatchResult batchResult, SimulationRunMetadata metadata)
        {
            var rows = new List<LivingCellSourceExportRow>();

            foreach (var game in batchResult.GameResults)
            {
                foreach (var player in game.PlayerResults)
                {
                    var lineupEntry = metadata.SelectedStrategies
                        .FirstOrDefault(s => string.Equals(s.StrategyName, player.StrategyName, StringComparison.OrdinalIgnoreCase));

                    foreach (var livingSource in player.LivingCellsBySource.OrderBy(kvp => kvp.Key.ToString(), StringComparer.Ordinal))
                    {
                        rows.Add(new LivingCellSourceExportRow
                        {
                            ExperimentId = metadata.ExperimentId,
                            GameIndex = game.GameIndex,
                            GameSeed = game.GameSeed,
                            PlayerId = player.PlayerId,
                            AssignedSlot = player.PlayerId,
                            SelectedLineupOrder = lineupEntry?.LineupOrder ?? 0,
                            StrategyName = player.StrategyName,
                            StrategyTheme = AIRoster.GetThemeForStrategy(player.Strategy).ToString(),
                            GrowthSource = livingSource.Key.ToString(),
                            GrowthSourceDisplayName = GrowthSourceDisplayNames.GetDisplayName(livingSource.Key),
                            LivingCellCount = livingSource.Value
                        });
                    }
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

        private static string GetStartingPositionMode(ExperimentPositioning positioning)
        {
            if (positioning.ExactStartingPositions.Count > 0) return "exact";
            if (positioning.PreferredPositionPools.Count > 0) return "preferred-pools";
            return "generated";
        }

        private static string FormatCoordinates(IEnumerable<BoardCoordinate> coordinates) =>
            string.Join("|", coordinates.Select(coordinate => $"{coordinate.X}:{coordinate.Y}"));

        private static string FormatPositionPools(IEnumerable<PlayerStartingPositionPool> pools) =>
            string.Join("|", pools.OrderBy(pool => pool.PlayerSlot).Select(pool =>
                $"{pool.PlayerSlot}={string.Join(";", pool.Positions.Select(position => $"{position.X}:{position.Y}"))}"));

        private static string FormatConfiguredAdaptations(IEnumerable<PlayerStartingAdaptations> loadouts) =>
            string.Join("|", loadouts.OrderBy(loadout => loadout.PlayerSlot).Select(loadout =>
                $"{loadout.PlayerSlot}={string.Join(",", loadout.AdaptationIds.OrderBy(id => id, StringComparer.Ordinal))}"));

        private static string FormatActualStartingPositions(GameResult game) =>
            string.Join("|", game.StartingPositionsByPlayerId.OrderBy(entry => entry.Key).Select(entry =>
                $"{entry.Key}={entry.Value.x}:{entry.Value.y}"));

        private static string FormatActualStartingAdaptations(GameResult game) =>
            string.Join("|", game.StartingAdaptationIdsByPlayerId.OrderBy(entry => entry.Key).Select(entry =>
                $"{entry.Key}={string.Join(",", entry.Value.OrderBy(id => id, StringComparer.Ordinal))}"));
    }
}
