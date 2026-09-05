using FungusToast.Core.AI;
using FungusToast.Core.Common;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Experiments;

public static class ResolvedExperimentReplayRunner
{
    public static void Run(string manifestPath, string? replayExperimentId = null)
    {
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("Resolved experiment manifest was not found.", manifestPath);

        var source = ResolvedExperimentManifestJson.Deserialize(File.ReadAllText(manifestPath));
        ValidateForReplay(source);

        var strategyNames = source.SelectedLineup
            .OrderBy(strategy => strategy.LineupOrder)
            .Select(strategy => strategy.StrategyName)
            .ToList();
        var strategies = AIRoster.GetStrategiesByName(source.Condition.Strategies.StrategySet, strategyNames, out var missingNames);
        if (missingNames.Count > 0)
            throw new InvalidOperationException($"Replay strategies are unavailable: {string.Join(", ", missingNames)}.");

        replayExperimentId ??= $"{source.ExperimentId}__replay_{DateTime.UtcNow:yyyyMMddTHHmmssfff}";
        if (replayExperimentId.Length > 128 || replayExperimentId.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
            throw new InvalidOperationException("Replay experiment ID must use only letters, numbers, '.', '_' or '-' and be at most 128 characters.");
        var metadata = new SimulationRunMetadata
        {
            ExperimentId = replayExperimentId,
            RunTimestampUtc = DateTime.UtcNow,
            StrategySet = source.Condition.Strategies.StrategySet,
            BaseSeed = source.Randomness.StrategySelectionSeed,
            SlotAssignmentPolicy = source.Condition.SlotAssignmentPolicy,
            StrategySelectionPolicy = source.Condition.Strategies.SelectionPolicy,
            StrategySelectionSource = StrategySelectionSource.ExplicitLineup,
            NumberOfPlayers = source.Condition.PlayerCount,
            NumberOfGamesRequested = source.Sampling.GamesRequested,
            BoardWidth = source.Condition.Board.Width,
            BoardHeight = source.Condition.Board.Height,
            SelectedStrategies = source.SelectedLineup.OrderBy(strategy => strategy.LineupOrder).Select(strategy => strategy.Metadata).ToList(),
            InputSchemaVersion = source.InputSchemaVersion,
            Purpose = $"Replay of {source.ExperimentId}: {source.Purpose}",
            TotalGameBudget = source.TotalGameBudget,
            RuntimeBudgetSeconds = source.RuntimeBudgetSeconds,
            Analysis = source.Analysis,
            Condition = source.Condition,
            GameSeedSchedule = source.Randomness.GameSeedSchedule
        };

        var exactPositions = source.Condition.Positioning.ExactStartingPositions
            .Select(position => (position.X, position.Y))
            .ToList();
        var preferredPools = source.Condition.Positioning.PreferredPositionPools
            .ToDictionary(
                pool => pool.PlayerSlot,
                pool => (IReadOnlyList<(int x, int y)>)pool.Positions.Select(position => (position.X, position.Y)).ToList());
        var startingAdaptations = Enumerable.Range(0, source.Condition.PlayerCount)
            .Select(slot => (IReadOnlyList<string>)(source.Condition.Systems.StartingAdaptations
                .FirstOrDefault(loadout => loadout.PlayerSlot == slot)?.AdaptationIds ?? Array.Empty<string>()))
            .ToList();
        var strategyEdgeOffsetOverrides = source.Condition.Positioning.StrategyEdgeOffsetOverrides
            .ToDictionary(entry => entry.StrategyName, entry => entry.EdgeOffset, StringComparer.OrdinalIgnoreCase);

        SimulationBatchResult replayResult;
        ExperimentRunStateStore.MarkRunning(metadata);
        try
        {
            replayResult = SimulationRunner.RunStandardSimulation(
                source.Condition.PlayerCount,
                source.Sampling.GamesRequested,
                strategies,
                source.Condition.Board.Width,
                source.Condition.Board.Height,
                enableKeyboardInterrupt: false,
                baseSeed: source.Randomness.StrategySelectionSeed,
                strategySet: source.Condition.Strategies.StrategySet,
                slotAssignmentPolicy: source.Condition.SlotAssignmentPolicy,
                runMetadata: metadata,
                exportParquet: true,
                enableNutrientPatches: source.Condition.Systems.NutrientPatchesEnabled,
                enableMycovariantDraft: source.Condition.Systems.MycovariantDraftEnabled,
                permanentlyBlockedTileIds: source.Condition.Board.BlockedTileIds,
                startingPositionOverride: exactPositions.Count > 0 ? exactPositions : null,
                startingAdaptationIds: startingAdaptations,
                preferredStartingPositionPoolsByPlayerId: preferredPools.Count > 0 ? preferredPools : null,
                runtimeBudgetSeconds: source.RuntimeBudgetSeconds,
                enableStartingAdaptations: source.Condition.Systems.StartingAdaptationsEnabled,
                strategyStartingSporeEdgeOffsetOverrides: strategyEdgeOffsetOverrides);
        }
        catch (Exception exception)
        {
            ExperimentRunStateStore.MarkFailed(metadata, exception);
            throw;
        }

        var replayOutcomeFingerprint = ExperimentFingerprint.ForOutcomes(replayResult);
        if (!string.Equals(replayOutcomeFingerprint, source.Fingerprints.OutcomeSha256, StringComparison.Ordinal))
        {
            var exception = new InvalidOperationException(
                $"Replay outcome mismatch. Expected {source.Fingerprints.OutcomeSha256}, actual {replayOutcomeFingerprint}.");
            ExperimentRunStateStore.MarkFailed(metadata, exception);
            throw exception;
        }

        Console.WriteLine($"Replay outcome verified: {replayOutcomeFingerprint}");
    }

    private static void ValidateForReplay(ResolvedExperimentManifest source)
    {
        if (!string.Equals(source.SchemaVersion, ResolvedExperimentManifest.CurrentSchemaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported resolved manifest schema '{source.SchemaVersion}'.");
        if (!string.Equals(source.Randomness.StreamContractVersion, RandomStreamContract.Version, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported random-stream contract '{source.Randomness.StreamContractVersion}'.");
        if (!string.Equals(source.AiCorpusVersion, StrategyIdentity.CorpusVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported AI corpus version '{source.AiCorpusVersion}'.");
        if (source.Sampling.GamesRequested < 1 || source.Sampling.GamesRequested > ExperimentManifest.MaximumGamesPerCondition)
            throw new InvalidOperationException($"Replay game count must be between 1 and {ExperimentManifest.MaximumGamesPerCondition}.");
        if (source.Randomness.GameSeedSchedule.Count != source.Sampling.GamesRequested)
            throw new InvalidOperationException("Resolved game seed schedule count does not match games requested.");
        if (source.SelectedLineup.Count != source.Condition.PlayerCount)
            throw new InvalidOperationException("Resolved lineup count does not match player count.");

        ExperimentManifestValidator.ValidateAndThrow(new ExperimentManifest
        {
            SchemaVersion = source.InputSchemaVersion,
            ExperimentId = source.ExperimentId,
            Purpose = source.Purpose,
            GamesPerCondition = source.Sampling.GamesRequested,
            BaseSeed = source.Randomness.StrategySelectionSeed,
            TotalGameBudget = source.TotalGameBudget,
            RuntimeBudgetSeconds = source.RuntimeBudgetSeconds,
            Analysis = source.Analysis,
            Conditions = new[] { source.Condition }
        });

        var currentCode = CodeIdentityResolver.Resolve();
        if (!string.Equals(currentCode.CoreAssemblySha256, source.Code.CoreAssemblySha256, StringComparison.Ordinal) ||
            !string.Equals(currentCode.SimulationAssemblySha256, source.Code.SimulationAssemblySha256, StringComparison.Ordinal))
            throw new InvalidOperationException("Replay code fingerprint mismatch. Rebuild or check out the recorded code before replaying.");

        foreach (var strategy in source.SelectedLineup)
        {
            var currentDefinition = StrategyRegistry.GetDefinition(
                source.Condition.Strategies.StrategySet,
                strategy.StrategyName)
                ?? throw new InvalidOperationException($"Replay strategy '{strategy.StrategyName}' is not registered.");
            if (!string.Equals(currentDefinition.StrategyId, strategy.StrategyId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Strategy ID mismatch for '{strategy.StrategyName}'.");
            if (!string.Equals(currentDefinition.DefinitionFingerprint, strategy.DefinitionSha256, StringComparison.Ordinal))
                throw new InvalidOperationException($"Strategy fingerprint mismatch for '{strategy.StrategyName}'.");
        }
    }
}
