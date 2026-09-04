using FungusToast.Core.AI;
using FungusToast.Core.Common;
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
        var executionFingerprint = ExperimentFingerprint.ForExecution(metadata, code);
        return new ResolvedExperimentManifest
        {
            SchemaVersion = ResolvedExperimentManifest.CurrentSchemaVersion,
            InputSchemaVersion = metadata.InputSchemaVersion,
            ExperimentId = metadata.ExperimentId,
            ConditionId = metadata.Condition.ConditionId,
            Purpose = metadata.Purpose,
            AiCorpusVersion = StrategyIdentity.CorpusVersion,
            TotalGameBudget = metadata.TotalGameBudget,
            RuntimeBudgetSeconds = metadata.RuntimeBudgetSeconds,
            Analysis = metadata.Analysis,
            CreatedUtc = metadata.RunTimestampUtc,
            Code = code,
            Fingerprints = new ResolvedFingerprints
            {
                ExecutionSha256 = executionFingerprint,
                OutcomeSha256 = ExperimentFingerprint.ForOutcomes(batchResult),
                ConditionSha256 = conditionFingerprint,
                BoardGeometrySha256 = ExperimentFingerprint.ForBoard(metadata.Condition.Board),
                BalanceConfigSha256 = code.CoreAssemblySha256
            },
            Condition = metadata.Condition,
            SelectedLineup = metadata.SelectedStrategies.Select(strategy => new ResolvedStrategyDefinition
            {
                LineupOrder = strategy.LineupOrder,
                StrategyName = strategy.StrategyName,
                StrategyId = strategy.StrategyId,
                DefinitionSha256 = strategy.DefinitionFingerprint,
                Metadata = strategy
            }).ToList(),
            Randomness = new ResolvedRandomness
            {
                StreamContractVersion = RandomStreamContract.Version,
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
                TerminationReason = game.TerminationReason,
                RuntimeMilliseconds = game.RuntimeMilliseconds,
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
                            : Array.Empty<string>(),
                        EliminationRound = game.EliminationRoundByPlayerId.TryGetValue(entry.Key, out var eliminationRound)
                            ? eliminationRound
                            : -1
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
