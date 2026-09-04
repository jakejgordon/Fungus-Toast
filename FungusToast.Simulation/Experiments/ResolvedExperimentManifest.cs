using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Experiments;

public sealed class ResolvedExperimentManifest
{
    public const string CurrentSchemaVersion = "fungus-toast.experiment-result.v2";

    public required string SchemaVersion { get; init; }
    public required string InputSchemaVersion { get; init; }
    public required string ExperimentId { get; init; }
    public required string ConditionId { get; init; }
    public required string Purpose { get; init; }
    public required DateTime CreatedUtc { get; init; }
    public required ResolvedCodeIdentity Code { get; init; }
    public required ResolvedFingerprints Fingerprints { get; init; }
    public required ExperimentCondition Condition { get; init; }
    public required IReadOnlyList<ResolvedStrategyDefinition> SelectedLineup { get; init; }
    public required ResolvedRandomness Randomness { get; init; }
    public required ResolvedSampling Sampling { get; init; }
    public required IReadOnlyList<ResolvedGameEvidence> Games { get; init; }
    public required ResolvedOutputs Outputs { get; init; }
}

public sealed class ResolvedCodeIdentity
{
    public required string Commit { get; init; }
    public required string SimulationAssemblyVersion { get; init; }
    public required string SimulationAssemblySha256 { get; init; }
    public required string CoreAssemblySha256 { get; init; }
}

public sealed class ResolvedFingerprints
{
    public required string ExecutionSha256 { get; init; }
    public required string OutcomeSha256 { get; init; }
    public required string ConditionSha256 { get; init; }
    public required string BoardGeometrySha256 { get; init; }
    public required string BalanceConfigSha256 { get; init; }
}

public sealed class ResolvedStrategyDefinition
{
    public required int LineupOrder { get; init; }
    public required string StrategyName { get; init; }
    public required string StrategyId { get; init; }
    public required string DefinitionSha256 { get; init; }
    public required SelectedStrategyMetadata Metadata { get; init; }
}

public sealed class ResolvedRandomness
{
    public required int StrategySelectionSeed { get; init; }
    public required IReadOnlyList<int> GameSeedSchedule { get; init; }
}

public sealed class ResolvedSampling
{
    public required int GamesRequested { get; init; }
    public required int GamesCompleted { get; init; }
    public required string CompletionStatus { get; init; }
}

public sealed class ResolvedGameEvidence
{
    public required int GameIndex { get; init; }
    public required int GameSeed { get; init; }
    public required IReadOnlyList<string> AssignedStrategyLineup { get; init; }
    public required IReadOnlyList<ResolvedPlayerStart> PlayerStarts { get; init; }
}

public sealed class ResolvedPlayerStart
{
    public required int PlayerSlot { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required IReadOnlyList<string> AdaptationIds { get; init; }
}

public sealed class ResolvedOutputs
{
    public required IReadOnlyDictionary<string, ResolvedOutputFile> Files { get; init; }
    public required IReadOnlyDictionary<string, int> RowCounts { get; init; }
}

public sealed class ResolvedOutputFile
{
    public required string FileName { get; init; }
    public required string Sha256 { get; init; }
}
