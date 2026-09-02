using System.Text.Json;
using FungusToast.Core.AI;
using FungusToast.Simulation.Experiments;
using FungusToast.Simulation.Models;
using FungusToast.Simulation.Analysis;
using Xunit;

namespace FungusToast.Simulation.Tests.Experiments;

public sealed class ExperimentManifestTests
{
    [Fact]
    public void CheckedInExample_DeserializesAndValidates()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "experiment-input.v1.example.json");
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
        const string json = "{ \"schemaVersion\": \"fungus-toast.experiment-input.v1\" }";
        Assert.Throws<JsonException>(() => ExperimentManifestJson.Deserialize(json));
    }

    [Fact]
    public void Validate_RejectsBatchAboveOneHundredGames()
    {
        var errors = ExperimentManifestValidator.Validate(CreateValidManifest(gamesPerCondition: 101));
        Assert.Contains(errors, error => error.Contains("gamesPerCondition", StringComparison.Ordinal));
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

    private static ExperimentManifest CreateValidManifest(int gamesPerCondition = 100, ExperimentCondition? condition = null) => new()
    {
        SchemaVersion = ExperimentManifest.CurrentSchemaVersion,
        ExperimentId = "manifest_contract_test",
        Purpose = "contract test",
        GamesPerCondition = gamesPerCondition,
        BaseSeed = 12345,
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
