using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using FungusToast.Simulation.Experiments;
using FungusToast.Simulation.Models;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

/// <summary>
/// These tests publish into the process-wide strategy registry, so they run in one non-parallel
/// collection and each clears the generated set afterwards. Nothing else registers this set, so
/// no authored roster is disturbed.
/// </summary>
[Collection(GeneratedCatalogCollection.Name)]
public sealed class GeneratedCandidateCatalogTests : IDisposable
{
    private const string ControlStrategyId = "legacy.testing.tst-balancedgeneralistcontrol.v1";

    public void Dispose() => GeneratedCandidateCatalog.Clear();

    [Fact]
    public void PublishedCandidates_KeepTheirGenomeIdentityInTheRegistry()
    {
        var candidates = GenerateCandidates();
        var definitions = GeneratedCandidateCatalog.Publish(candidates);

        Assert.Equal(candidates.Count, definitions.Count);
        // The registry identity is the genome's own candidate ID, not a minted legacy.* one, so a
        // candidate is addressed the same way in a genome file and in an exported artifact.
        Assert.Equal(
            candidates.Select(candidate => candidate.CandidateId).OrderBy(id => id, StringComparer.Ordinal),
            definitions.Select(definition => definition.StrategyId).OrderBy(id => id, StringComparer.Ordinal));
        Assert.All(definitions, definition => Assert.StartsWith("candidate.", definition.StrategyId, StringComparison.Ordinal));
    }

    /// <summary>
    /// The structural guarantee behind "no candidate enters a player-facing pool without review":
    /// pool membership is empty, so pool-filtered selection cannot return one at all.
    /// </summary>
    [Fact]
    public void PublishedCandidates_BelongToNoPoolAndNoCampaignDifficulty()
    {
        GeneratedCandidateCatalog.Publish(GenerateCandidates());

        var entries = AIRoster.GetStrategyCatalogEntries(StrategySetEnum.Generated);
        Assert.NotEmpty(entries);
        Assert.All(entries, entry =>
        {
            Assert.Equal(StrategyPool.None, entry.Pools);
            Assert.Null(entry.CampaignDifficulty);
            Assert.Equal(StrategyRole.Experimental, entry.Role);
            Assert.Equal(StrategyLifecycle.Draft, entry.Lifecycle);
            Assert.Empty(entry.DifficultyBands);
        });

        foreach (var pool in new[] { StrategyPool.Campaign, StrategyPool.SimulationBaseline, StrategyPool.SimulationExperimental, StrategyPool.MycovariantLab })
        {
            var matches = AIRoster.GetStrategiesByFilter(
                StrategySetEnum.Generated,
                new StrategyCatalogFilter { Pools = new[] { pool } });
            Assert.Empty(matches);
        }
    }

    /// <summary>
    /// The reason for registering at all: a published candidate must be resolvable by the same
    /// manifest validation path every authored strategy uses, so its evaluation inherits the
    /// existing resolved-manifest and replay guarantees instead of bypassing them.
    /// </summary>
    [Fact]
    public void AManifestNamingAPublishedCandidate_Validates()
    {
        var candidates = GenerateCandidates();
        GeneratedCandidateCatalog.Publish(candidates);

        var manifest = CreateManifest(candidates[0].DisplayName, candidates[1].DisplayName);

        Assert.Empty(ExperimentManifestValidator.Validate(manifest));
    }

    [Fact]
    public void AManifestNamingAnUnpublishedCandidate_FailsValidation()
    {
        GeneratedCandidateCatalog.Publish(GenerateCandidates());

        var manifest = CreateManifest("CAND_NeverPublished", "CAND_AlsoNeverPublished");

        Assert.NotEmpty(ExperimentManifestValidator.Validate(manifest));
    }

    [Fact]
    public void Publishing_ReplacesThePreviousSearchRatherThanAccumulating()
    {
        var candidates = GenerateCandidates();
        GeneratedCandidateCatalog.Publish(candidates);
        GeneratedCandidateCatalog.Publish(new[] { candidates[0] });

        var definition = Assert.Single(GeneratedCandidateCatalog.GetPublished());
        Assert.Equal(candidates[0].CandidateId, definition.StrategyId);
    }

    [Fact]
    public void Clear_EmptiesTheGeneratedSetAndLeavesAuthoredSetsIntact()
    {
        var authoredTestingCount = StrategyRegistry.GetDefinitions(StrategySetEnum.Testing).Count;
        GeneratedCandidateCatalog.Publish(GenerateCandidates());

        GeneratedCandidateCatalog.Clear();

        Assert.Empty(GeneratedCandidateCatalog.GetPublished());
        Assert.Equal(authoredTestingCount, StrategyRegistry.GetDefinitions(StrategySetEnum.Testing).Count);
        Assert.NotEmpty(StrategyRegistry.GetDefinitions(StrategySetEnum.Proven));
    }

    [Fact]
    public void PublishingAnInvalidCandidate_Throws()
    {
        var candidate = GenerateCandidates()[0];
        var tampered = new CandidateGenome
        {
            SchemaVersion = candidate.SchemaVersion,
            CandidateId = candidate.CandidateId,
            DisplayName = candidate.DisplayName,
            Lineage = candidate.Lineage,
            Genes = candidate.Genes,
            VariedGenes = new[] { CandidateGene.MaxTier }
        };

        var exception = Assert.Throws<ArgumentException>(() => GeneratedCandidateCatalog.Publish(new[] { tampered }));
        Assert.Contains("cannot be published", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PublishingTheSameDisplayNameTwice_Throws()
    {
        var candidate = GenerateCandidates()[0];

        var exception = Assert.Throws<ArgumentException>(() => GeneratedCandidateCatalog.Publish(new[] { candidate, candidate }));
        Assert.Contains("is published twice", exception.Message, StringComparison.Ordinal);
    }

    private static ExperimentManifest CreateManifest(params string[] strategyNames) => new()
    {
        SchemaVersion = ExperimentManifest.CurrentSchemaVersion,
        ExperimentId = "generated_catalog_contract",
        Purpose = "Prove a generated candidate is addressable by the normal manifest pipeline.",
        GamesPerCondition = 3,
        BaseSeed = 2026090601,
        TotalGameBudget = 12,
        RuntimeBudgetSeconds = 120,
        Analysis = new ExperimentAnalysisPlan
        {
            AnalysisVersion = "v2",
            EvidenceStage = ExperimentEvidenceStage.Smoke
        },
        Conditions = new[]
        {
            new ExperimentCondition
            {
                ConditionId = "generated_smoke",
                PlayerCount = strategyNames.Length,
                Board = new ExperimentBoard { Width = 40, Height = 40 },
                Strategies = new ExperimentStrategySelection
                {
                    StrategySet = StrategySetEnum.Generated,
                    SelectionPolicy = StrategySelectionPolicy.RandomUnique,
                    ExplicitStrategyNames = strategyNames
                },
                Systems = new ExperimentSystems
                {
                    NutrientPatchesEnabled = false,
                    MycovariantDraftEnabled = false,
                    StartingAdaptationsEnabled = false
                },
                Positioning = new ExperimentPositioning(),
                SlotAssignmentPolicy = SlotAssignmentPolicy.RotateByGame
            }
        }
    };

    private static IReadOnlyList<CandidateGenome> GenerateCandidates()
    {
        _ = AIRoster.TestingStrategies.Count;
        return CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "catalog",
            Purpose = "Publish a bounded opener-order search into the generated catalog.",
            ParentStrategyId = ControlStrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.GoalOrderAdjacentSwap, CandidateOperator.EconomyBiasSweep },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        }).Accepted;
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GeneratedCatalogCollection
{
    public const string Name = "GeneratedCandidateCatalog";
}
