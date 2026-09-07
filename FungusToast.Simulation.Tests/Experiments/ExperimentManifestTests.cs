using System.Text.Json;
using FungusToast.Core.AI;
using FungusToast.Simulation.Experiments;
using FungusToast.Simulation.Models;
using FungusToast.Simulation.Analysis;
using Xunit;

namespace FungusToast.Simulation.Tests.Experiments;

public sealed class ExperimentManifestTests
{
    /// <summary>
    /// The staged ceiling is a promotion safeguard: it stops a candidate buying significance with a
    /// larger batch. An exploratory run cannot carry a hypothesis or emit a verdict, so nothing
    /// advances on its evidence and it may measure as widely as its budget allows.
    /// </summary>
    [Fact]
    public void ExploratoryRunsMayExceedTheStagedGamesCeiling_ButStagedOnesMayNot()
    {
        Assert.Equal(100, ExperimentManifest.MaximumGamesForStage(ExperimentEvidenceStage.Holdout));
        Assert.Equal(100, ExperimentManifest.MaximumGamesForStage(ExperimentEvidenceStage.Comparison));
        Assert.Equal(
            ExperimentManifest.MaximumExploratoryGamesPerCondition,
            ExperimentManifest.MaximumGamesForStage(ExperimentEvidenceStage.Exploratory));
        Assert.True(ExperimentManifest.MaximumExploratoryGamesPerCondition > ExperimentManifest.MaximumGamesPerCondition);
    }

    [Fact]
    public void CheckedInExample_DeserializesAndValidates()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "experiment-input.v4.example.json");
        var manifest = ExperimentManifestJson.Deserialize(File.ReadAllText(path));
        Assert.Empty(ExperimentManifestValidator.Validate(manifest));
    }

    [Fact]
    public void ValidManifest_RoundTripsAndValidates()
    {
        var roundTripped = ExperimentManifestJson.Deserialize(ExperimentManifestJson.Serialize(CreateValidManifest()));
        Assert.Empty(ExperimentManifestValidator.Validate(roundTripped));
        Assert.Equal(100, roundTripped.GamesPerCondition);
        Assert.Equal(StrategySelectionPolicy.CoverageBalanced, roundTripped.Conditions[0].Strategies.SelectionPolicy);
        Assert.Equal(SlotAssignmentPolicy.RotateByGame, roundTripped.Conditions[0].SlotAssignmentPolicy);
    }

    [Fact]
    public void Deserialize_RejectsUnknownFields()
    {
        var json = ExperimentManifestJson.Serialize(CreateValidManifest()).Replace("\"purpose\": \"contract test\"", "\"purpose\": \"contract test\",\n  \"typoField\": true");
        Assert.Throws<JsonException>(() => ExperimentManifestJson.Deserialize(json));
    }

    [Fact]
    public void Deserialize_RejectsMissingRequiredFields()
    {
        const string json = "{ \"schemaVersion\": \"fungus-toast.experiment-input.v4\" }";
        Assert.Throws<JsonException>(() => ExperimentManifestJson.Deserialize(json));
    }

    [Fact]
    public void Validate_RejectsBatchAboveTheCeilingForItsStage()
    {
        // An exploratory run measures rather than decides, so its ceiling is the wider one - but it
        // is still a ceiling.
        var withinExploratory = ExperimentManifestValidator.Validate(
            CreateValidManifest(gamesPerCondition: 101, totalGameBudget: 10_000));
        Assert.DoesNotContain(withinExploratory, error => error.Contains("gamesPerCondition", StringComparison.Ordinal));

        var aboveExploratory = ExperimentManifestValidator.Validate(CreateValidManifest(
            gamesPerCondition: ExperimentManifest.MaximumExploratoryGamesPerCondition + 1,
            totalGameBudget: 10_000));
        Assert.Contains(aboveExploratory, error => error.Contains("gamesPerCondition", StringComparison.Ordinal));
    }

    /// <summary>
    /// The four staged gates keep their frozen counts: relaxing the exploratory ceiling must not
    /// let a promotion stage buy significance with a larger batch.
    /// </summary>
    [Theory]
    [InlineData(ExperimentEvidenceStage.Smoke, 101)]
    [InlineData(ExperimentEvidenceStage.Calibration, 101)]
    [InlineData(ExperimentEvidenceStage.Comparison, 101)]
    [InlineData(ExperimentEvidenceStage.Holdout, 101)]
    public void Validate_StillPinsTheStagedGatesToTheirFrozenCounts(ExperimentEvidenceStage stage, int games)
    {
        var manifest = CreateValidManifest(gamesPerCondition: games, totalGameBudget: 10_000);
        var staged = new ExperimentManifest
        {
            SchemaVersion = manifest.SchemaVersion,
            ExperimentId = manifest.ExperimentId,
            Purpose = manifest.Purpose,
            GamesPerCondition = manifest.GamesPerCondition,
            BaseSeed = manifest.BaseSeed,
            TotalGameBudget = manifest.TotalGameBudget,
            RuntimeBudgetSeconds = manifest.RuntimeBudgetSeconds,
            Analysis = new ExperimentAnalysisPlan
            {
                AnalysisVersion = "fungus-toast.analysis.v2",
                EvidenceStage = stage
            },
            Conditions = manifest.Conditions
        };

        Assert.NotEmpty(ExperimentManifestValidator.Validate(staged));
    }

    [Fact]
    public void Validate_RejectsRequestedGamesAboveTotalBudget()
    {
        var manifest = CreateValidManifest(gamesPerCondition: 20, totalGameBudget: 19);

        var errors = ExperimentManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("totalGameBudget", StringComparison.Ordinal));
    }

    [Fact]
    public void MatchupRunner_StopsBeforeStartingGameWhenRuntimeBudgetIsExhausted()
    {
        var result = MatchupRunner.RunMatchups(
            new List<IMutationSpendingStrategy>(),
            gamesToPlay: 1,
            runtimeBudgetSeconds: 0);

        Assert.Empty(result.GameResults);
    }

    [Fact]
    public void Validate_RejectsInvalidPairingGroupId()
    {
        var condition = CreateValidCondition() with { PairingGroupId = "invalid pair" };

        var errors = ExperimentManifestValidator.Validate(CreateValidManifest(condition: condition));

        Assert.Contains(errors, error => error.Contains("pairingGroupId", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsPositionOnBlockedTile()
    {
        var condition = CreateValidCondition() with
        {
            Board = new ExperimentBoard { Width = 20, Height = 20, GeometryId = "torn-bread", BlockedTileIds = new[] { 21 } },
            Positioning = new ExperimentPositioning { ExactStartingPositions = new[] { new BoardCoordinate { X = 1, Y = 1 }, new BoardCoordinate { X = 18, Y = 18 } } }
        };
        var errors = ExperimentManifestValidator.Validate(CreateValidManifest(condition: condition));
        Assert.Contains(errors, error => error.Contains("blocked coordinate (1,1)", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsAmbiguousExactAndPreferredPositions()
    {
        var condition = CreateValidCondition() with
        {
            Positioning = new ExperimentPositioning
            {
                ExactStartingPositions = new[] { new BoardCoordinate { X = 1, Y = 1 }, new BoardCoordinate { X = 18, Y = 18 } },
                PreferredPositionPools = new[] { new PlayerStartingPositionPool { PlayerSlot = 0, Positions = new[] { new BoardCoordinate { X = 2, Y = 2 } } } }
            }
        };
        var errors = ExperimentManifestValidator.Validate(CreateValidManifest(condition: condition));
        Assert.Contains(errors, error => error.Contains("cannot specify both", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsUnknownAdaptationIds()
    {
        var condition = CreateValidCondition() with
        {
            Systems = new ExperimentSystems
            {
                NutrientPatchesEnabled = false,
                MycovariantDraftEnabled = false,
                StartingAdaptations = new[]
                {
                    new PlayerStartingAdaptations { PlayerSlot = 0, AdaptationIds = new[] { "adaptation_missing" } }
                }
            }
        };
        var errors = ExperimentManifestValidator.Validate(CreateValidManifest(condition: condition));
        Assert.Contains(errors, error => error.Contains("unknown IDs: adaptation_missing", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsUnknownStrategyEdgeOffsetOverride()
    {
        var condition = CreateValidCondition() with
        {
            Positioning = new ExperimentPositioning
            {
                StrategyEdgeOffsetOverrides = new[]
                {
                    new StrategyStartingSporeEdgeOffsetOverride { StrategyName = "missing-strategy", EdgeOffset = 0 }
                }
            }
        };

        var errors = ExperimentManifestValidator.Validate(CreateValidManifest(condition: condition));

        Assert.Contains(errors, error => error.Contains("unknown strategy 'missing-strategy'", StringComparison.Ordinal));
    }

    [Fact]
    public void Fingerprints_AreStableAndBoardMaskOrderIndependent()
    {
        var first = new ExperimentBoard
        {
            Width = 20,
            Height = 10,
            GeometryId = "custom",
            BlockedTileIds = new[] { 7, 2, 5 }
        };
        var second = new ExperimentBoard
        {
            Width = 20,
            Height = 10,
            GeometryId = "custom",
            BlockedTileIds = new[] { 2, 5, 7 }
        };

        Assert.Equal(ExperimentFingerprint.ForBoard(first), ExperimentFingerprint.ForBoard(second));
        Assert.Equal(64, ExperimentFingerprint.ForBoard(first).Length);
    }

    [Fact]
    public void MatchupRunner_RejectsSeedScheduleWithWrongLength()
    {
        Assert.Throws<ArgumentException>(() => MatchupRunner.RunMatchups(
            new List<IMutationSpendingStrategy>(),
            gamesToPlay: 2,
            gameSeedSchedule: new[] { 123 }));
    }

    [Fact]
    public void OutcomeFingerprint_IsStableAndSensitiveToResults()
    {
        var first = new SimulationBatchResult
        {
            GameResults = new List<GameResult>
            {
                new() { GameIndex = 1, GameSeed = 123, WinnerId = 0, TurnsPlayed = 10 }
            }
        };
        var equivalent = new SimulationBatchResult
        {
            GameResults = new List<GameResult>
            {
                new() { GameIndex = 1, GameSeed = 123, WinnerId = 0, TurnsPlayed = 10 }
            }
        };
        var changed = new SimulationBatchResult
        {
            GameResults = new List<GameResult>
            {
                new() { GameIndex = 1, GameSeed = 123, WinnerId = 1, TurnsPlayed = 10 }
            }
        };

        Assert.Equal(ExperimentFingerprint.ForOutcomes(first), ExperimentFingerprint.ForOutcomes(equivalent));
        Assert.NotEqual(ExperimentFingerprint.ForOutcomes(first), ExperimentFingerprint.ForOutcomes(changed));
    }

    [Fact]
    public void FinalPlacement_UsesCompetitionRanksForTiedLivingCellCounts()
    {
        var strategy = AIRoster.TestingStrategies[0];
        var players = new List<PlayerResult>
        {
            new() { PlayerId = 0, LivingCells = 10, Strategy = strategy },
            new() { PlayerId = 1, LivingCells = 10, Strategy = strategy },
            new() { PlayerId = 2, LivingCells = 7, Strategy = strategy },
            new() { PlayerId = 3, LivingCells = 2, Strategy = strategy }
        };

        Assert.Equal(1, FinalPlacementCalculator.GetCompetitionRank(players, 0));
        Assert.Equal(1, FinalPlacementCalculator.GetCompetitionRank(players, 1));
        Assert.Equal(3, FinalPlacementCalculator.GetCompetitionRank(players, 2));
        Assert.Equal(4, FinalPlacementCalculator.GetCompetitionRank(players, 3));
        Assert.Equal(2, FinalPlacementCalculator.GetTieCount(players, 0));
        Assert.Equal(1, FinalPlacementCalculator.GetTieCount(players, 2));
        Assert.Equal(new[] { 0, 1 }, FinalPlacementCalculator.GetWinnerIds(players));
    }

    [Fact]
    public void GameResult_AssignsFractionalCreditToEveryCoWinner()
    {
        var result = new GameResult
        {
            WinnerId = -1,
            WinnerIds = new[] { 0, 2 }
        };

        Assert.True(result.IsWinningPlayer(0));
        Assert.True(result.IsWinningPlayer(2));
        Assert.False(result.IsWinningPlayer(1));
        Assert.Equal(0.5, result.GetWinCredit(0));
        Assert.Equal(0.5, result.GetWinCredit(2));
        Assert.Equal(0.0, result.GetWinCredit(1));
    }

    [Fact]
    public void ManifestDiff_AllowsOnlyDeclaredTreatmentPaths()
    {
        var control = new { Systems = new { Nutrients = true, Draft = true }, Players = 4 };
        var treatment = new { Systems = new { Nutrients = false, Draft = true }, Players = 4 };

        var comparison = ManifestDiff.Compare(control, treatment, new[] { "systems.nutrients" });

        Assert.True(comparison.IsClean);
        Assert.Single(comparison.Differences);
        Assert.True(comparison.Differences[0].IsAllowed);
    }

    [Fact]
    public void ManifestDiff_RejectsContaminationAndUnusedAllowances()
    {
        var control = new { Systems = new { Nutrients = true }, Players = 4 };
        var treatment = new { Systems = new { Nutrients = false }, Players = 5 };

        var comparison = ManifestDiff.Compare(
            control,
            treatment,
            new[] { "systems.nutrients", "systems.draft" });

        Assert.False(comparison.IsClean);
        Assert.Contains(comparison.UnexpectedDifferences, difference => difference.Path == "players");
        Assert.Contains("systems.draft", comparison.UnusedAllowedPaths);
    }

    private static ExperimentManifest CreateValidManifest(int gamesPerCondition = 100, ExperimentCondition? condition = null, int? totalGameBudget = null) => new()
    {
        SchemaVersion = ExperimentManifest.CurrentSchemaVersion,
        ExperimentId = "manifest_contract_test",
        Purpose = "contract test",
        GamesPerCondition = gamesPerCondition,
        BaseSeed = 12345,
        TotalGameBudget = totalGameBudget ?? gamesPerCondition,
        RuntimeBudgetSeconds = 600,
        Analysis = new ExperimentAnalysisPlan { AnalysisVersion = "fungus-toast.analysis.v2", EvidenceStage = ExperimentEvidenceStage.Exploratory },
        Conditions = new[] { condition ?? CreateValidCondition() }
    };

    private static ExperimentCondition CreateValidCondition() => new()
    {
        ConditionId = "p2.w20.h20.testing",
        PlayerCount = 2,
        Board = new ExperimentBoard { Width = 20, Height = 20, GeometryId = "rectangle" },
        Strategies = new ExperimentStrategySelection { StrategySet = StrategySetEnum.Testing, SelectionPolicy = StrategySelectionPolicy.CoverageBalanced },
        Systems = new ExperimentSystems { NutrientPatchesEnabled = false, MycovariantDraftEnabled = false },
        Positioning = new ExperimentPositioning(),
        SlotAssignmentPolicy = SlotAssignmentPolicy.RotateByGame
    };
}
