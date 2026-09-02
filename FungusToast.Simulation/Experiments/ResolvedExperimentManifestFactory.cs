using FungusToast.Core.AI;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Experiments;

public static class ResolvedExperimentManifestFactory
{
    public static ResolvedExperimentManifest Create(
        SimulationBatchResult batchResult,
        SimulationRunMetadata metadata,
        IReadOnlyDictionary<string, ResolvedOutputFile> files,
        IReadOnlyDictionary<string, int> rowCounts)
    {
        var code = CodeIdentityResolver.Resolve();
        var conditionFingerprint = ExperimentFingerprint.ForCondition(metadata.Condition);
        var executionFingerprint = ExperimentFingerprint.ForText(string.Join("\n", new[]
        {
            conditionFingerprint,
            code.CoreAssemblySha256,
            code.SimulationAssemblySha256,
            string.Join("|", metadata.SelectedStrategies.OrderBy(strategy => strategy.LineupOrder).Select(strategy => strategy.StrategyName)),
            string.Join(",", metadata.GameSeedSchedule)
        }));
        return new ResolvedExperimentManifest
        {
            SchemaVersion = ResolvedExperimentManifest.CurrentSchemaVersion,
            InputSchemaVersion = metadata.InputSchemaVersion,
            ExperimentId = metadata.ExperimentId,
            ConditionId = metadata.Condition.ConditionId,
            Purpose = metadata.Purpose,
            CreatedUtc = metadata.RunTimestampUtc,
            Code = code,
            Fingerprints = new ResolvedFingerprints
            {
                ExecutionSha256 = executionFingerprint,
                ConditionSha256 = conditionFingerprint,
                BoardGeometrySha256 = ExperimentFingerprint.ForBoard(metadata.Condition.Board),
                BalanceConfigSha256 = code.CoreAssemblySha256
            },
            Condition = metadata.Condition,
            SelectedLineup = metadata.SelectedStrategies.Select(strategy => new ResolvedStrategyDefinition
            {
                LineupOrder = strategy.LineupOrder,
                StrategyName = strategy.StrategyName,
                DefinitionSha256 = ExperimentFingerprint.ForStrategy(code.CoreAssemblySha256, metadata.StrategySet, strategy.StrategyName),
                Metadata = strategy
            }).ToList(),
            Randomness = new ResolvedRandomness
            {
                StrategySelectionSeed = metadata.BaseSeed,
                GameSeedSchedule = metadata.GameSeedSchedule
            },
            Sampling = new ResolvedSampling
            {
                GamesRequested = metadata.NumberOfGamesRequested,
                GamesCompleted = batchResult.GameResults.Count,
                CompletionStatus = batchResult.GameResults.Count == metadata.NumberOfGamesRequested ? "complete" : "interrupted"
            },
            Games = batchResult.GameResults.Select(game => new ResolvedGameEvidence
            {
                GameIndex = game.GameIndex,
                GameSeed = game.GameSeed,
                AssignedStrategyLineup = game.PlayerResults
                    .OrderBy(player => player.PlayerId)
                    .Select(player => player.StrategyName)
                    .ToList(),
                PlayerStarts = game.StartingPositionsByPlayerId
                    .OrderBy(entry => entry.Key)
                    .Select(entry => new ResolvedPlayerStart
                    {
                        PlayerSlot = entry.Key,
                        X = entry.Value.x,
                        Y = entry.Value.y,
                        AdaptationIds = game.StartingAdaptationIdsByPlayerId.TryGetValue(entry.Key, out var adaptationIds)
                            ? adaptationIds
                            : Array.Empty<string>()
                    })
                    .ToList()
            }).ToList(),
            Outputs = new ResolvedOutputs
            {
                Files = files,
                RowCounts = rowCounts
            }
        };
    }
}
