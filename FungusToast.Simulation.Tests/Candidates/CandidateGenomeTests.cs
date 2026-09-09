using System.Text.Json;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

public sealed class CandidateGenomeTests
{
    private const string ControlStrategyName = "TST_BalancedGeneralistControl";

    /// <summary>
    /// The load-bearing test for P6.1. If any behavior dimension of
    /// ParameterizedSpendingStrategy were missing from CandidateGeneSet, extracting and
    /// re-materializing a strategy under its own name would drop that dimension and Core's
    /// definition fingerprint would change. Running it over every registered strategy is what
    /// makes "the genome covers the safe surface" a checked claim rather than an assertion.
    /// </summary>
    [Fact]
    public void ExtractedGenes_RematerializeEveryRegisteredStrategyWithAnIdenticalFingerprint()
    {
        var definitions = GetParameterizedDefinitions();
        Assert.NotEmpty(definitions);

        foreach (var (definition, parameterized) in definitions)
        {
            var genes = CandidateGenomeFactory.ExtractGenes(parameterized);
            var rematerialized = CandidateGenomeFactory.Materialize(genes, parameterized.StrategyName);

            Assert.Equal(
                definition.DefinitionFingerprint,
                StrategyIdentity.GetDefinitionFingerprint(rematerialized));
        }
    }

    [Fact]
    public void CloneOfEveryRegisteredStrategy_ValidatesAndVariesNothing()
    {
        var definitions = GetParameterizedDefinitions();
        var index = 0;
        var failures = new List<string>();

        foreach (var (definition, parameterized) in definitions)
        {
            var genome = CandidateGenomeFactory.CreateFromParent(
                definition,
                CandidateGenomeFactory.ExtractGenes(parameterized),
                $"RosterClone{index++}");

            foreach (var error in CandidateGenomeValidator.Validate(genome))
                failures.Add($"{parameterized.StrategyName}: {error}");
            Assert.Empty(genome.VariedGenes);
        }

        Assert.Equal(string.Empty, string.Join("\n", failures));
    }

    [Fact]
    public void GoalReorder_IsDetectedAsTheSingleVariedGene()
    {
        var genome = CreateOpenerOrderCandidate();

        Assert.Equal(new[] { CandidateGene.TargetMutationGoals }, genome.VariedGenes);
        Assert.Empty(CandidateGenomeValidator.Validate(genome));
        Assert.StartsWith("candidate.tst-balancedgeneralistcontrol.", genome.CandidateId);
    }

    [Fact]
    public void UndeclaredSecondDifference_FailsValidation()
    {
        var genome = CreateOpenerOrderCandidate();
        // Change a second gene while leaving variedGenes claiming only the goal reorder.
        var tampered = WithGenes(genome, new CandidateGeneSet
        {
            PrioritizeHighTier = genome.Genes.PrioritizeHighTier,
            MaxTier = genome.Genes.MaxTier,
            PriorityMutationCategories = genome.Genes.PriorityMutationCategories,
            TargetMutationGoals = genome.Genes.TargetMutationGoals,
            SurgePriorityIds = genome.Genes.SurgePriorityIds,
            SurgeAttemptTurnFrequency = genome.Genes.SurgeAttemptTurnFrequency,
            EconomyBias = EconomyBias.MaxEconomy,
            MycovariantPreferences = genome.Genes.MycovariantPreferences,
            ExcludedMutationIds = genome.Genes.ExcludedMutationIds,
            StartingSporeEdgeOffset = genome.Genes.StartingSporeEdgeOffset
        });

        var errors = CandidateGenomeValidator.Validate(tampered);
        Assert.Contains(errors, error => error.Contains("omits genes that actually differ", StringComparison.Ordinal)
            && error.Contains(nameof(CandidateGene.EconomyBias), StringComparison.Ordinal));
    }

    [Fact]
    public void DeclaredGeneThatDidNotChange_FailsValidation()
    {
        var genome = CreateOpenerOrderCandidate();
        var overclaimed = new CandidateGenome
        {
            SchemaVersion = genome.SchemaVersion,
            CandidateId = genome.CandidateId,
            DisplayName = genome.DisplayName,
            Lineage = genome.Lineage,
            Genes = genome.Genes,
            VariedGenes = new[] { CandidateGene.TargetMutationGoals, CandidateGene.MaxTier }
        };

        var errors = CandidateGenomeValidator.Validate(overclaimed);
        Assert.Contains(errors, error => error.Contains("declares genes that do not differ", StringComparison.Ordinal)
            && error.Contains(nameof(CandidateGene.MaxTier), StringComparison.Ordinal));
    }

    [Fact]
    public void EditedCandidateId_FailsValidation()
    {
        var genome = CreateOpenerOrderCandidate();
        var tampered = new CandidateGenome
        {
            SchemaVersion = genome.SchemaVersion,
            CandidateId = "candidate.tst-balancedgeneralistcontrol.000000000000",
            DisplayName = genome.DisplayName,
            Lineage = genome.Lineage,
            Genes = genome.Genes,
            VariedGenes = genome.VariedGenes
        };

        Assert.Contains(CandidateGenomeValidator.Validate(tampered), error => error.StartsWith("candidateId must be", StringComparison.Ordinal));
    }

    [Fact]
    public void StaleParentFingerprint_FailsValidation()
    {
        var genome = CreateOpenerOrderCandidate();
        var tampered = new CandidateGenome
        {
            SchemaVersion = genome.SchemaVersion,
            CandidateId = genome.CandidateId,
            DisplayName = genome.DisplayName,
            Lineage = new CandidateLineage
            {
                ParentStrategyId = genome.Lineage.ParentStrategyId,
                ParentDefinitionFingerprint = new string('0', 64),
                ParentStrategySet = genome.Lineage.ParentStrategySet,
                Notes = genome.Lineage.Notes
            },
            Genes = genome.Genes,
            VariedGenes = genome.VariedGenes
        };

        Assert.Contains(CandidateGenomeValidator.Validate(tampered), error => error.Contains("the parent changed since this candidate was generated", StringComparison.Ordinal));
    }

    [Fact]
    public void IdenticalGeneSetsFromDifferentParents_ShareOneBehaviorFingerprint()
    {
        var genes = CreateMinimalGenes();
        var otherGenes = CreateMinimalGenes();

        Assert.Equal(CandidateGenomeFingerprint.Compute(genes), CandidateGenomeFingerprint.Compute(otherGenes));
        Assert.NotEqual(
            CandidateGenomeFingerprint.DeriveCandidateId("legacy.testing.alpha.v1", genes),
            CandidateGenomeFingerprint.DeriveCandidateId("legacy.testing.beta.v1", otherGenes));
    }

    /// <summary>
    /// Core's own definition fingerprint flattens a null category list and an empty one to the
    /// same text even though it treats null as "every category". The genome keeps them apart so
    /// candidate deduplication cannot merge two genuinely different behaviors.
    /// </summary>
    [Fact]
    public void NullAndEmptyCategoryLists_ProduceDifferentFingerprints()
    {
        var unrestricted = CreateMinimalGenes(categories: null);
        var explicitlyEmpty = CreateMinimalGenes(categories: Array.Empty<MutationCategory>());

        Assert.NotEqual(
            CandidateGenomeFingerprint.Compute(unrestricted),
            CandidateGenomeFingerprint.Compute(explicitlyEmpty));
    }

    [Fact]
    public void GoalOrder_ChangesTheFingerprint()
    {
        var forward = CreateMinimalGenes(goals: new[]
        {
            new CandidateMutationGoal { MutationId = MutationIds.CreepingMold },
            new CandidateMutationGoal { MutationId = MutationIds.AnabolicInversion }
        });
        var reversed = CreateMinimalGenes(goals: new[]
        {
            new CandidateMutationGoal { MutationId = MutationIds.AnabolicInversion },
            new CandidateMutationGoal { MutationId = MutationIds.CreepingMold }
        });

        Assert.NotEqual(CandidateGenomeFingerprint.Compute(forward), CandidateGenomeFingerprint.Compute(reversed));
    }

    [Fact]
    public void Genome_RoundTripsThroughJson()
    {
        var genome = CreateOpenerOrderCandidate();
        var roundTripped = CandidateGenomeJson.Deserialize(CandidateGenomeJson.Serialize(genome));

        Assert.Empty(CandidateGenomeValidator.Validate(roundTripped));
        Assert.Equal(genome.CandidateId, roundTripped.CandidateId);
        Assert.Equal(
            CandidateGenomeFingerprint.Compute(genome.Genes),
            CandidateGenomeFingerprint.Compute(roundTripped.Genes));
        Assert.Equal(genome.VariedGenes, roundTripped.VariedGenes);
    }

    [Fact]
    public void Deserialize_RejectsUnknownFields()
    {
        var json = CandidateGenomeJson.Serialize(CreateOpenerOrderCandidate())
            .Replace("\"schemaVersion\":", "\"typoField\": true,\n  \"schemaVersion\":", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CandidateGenomeJson.Deserialize(json));
    }

    [Fact]
    public void CheckedInExample_DeserializesAndValidates()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "candidate-genome.v1.example.json");
        var genome = CandidateGenomeJson.Deserialize(File.ReadAllText(path));

        Assert.Empty(CandidateGenomeValidator.Validate(genome));
        Assert.Equal(new[] { CandidateGene.TargetMutationGoals }, genome.VariedGenes);
    }

    /// <summary>
    /// The examples record their parent's definition fingerprint and the exact genes that differ
    /// from it, so any change to a parent strategy makes them stale. Comparing against a freshly
    /// built copy catches that at the moment it happens and names the command that fixes it -
    /// the previous check only reported that the file was invalid, which left whoever changed the
    /// parent to work out both the cause and the remedy.
    /// </summary>
    [Fact]
    public void CheckedInExamples_MatchRegeneratedOutput()
    {
        foreach (var example in CandidateGenomeExamples.BuildAll())
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", example.FileName);
            Assert.True(File.Exists(path), $"Checked-in example '{example.FileName}' is missing. Run: {CandidateGenomeExamples.RegenerateCommand}");

            // The repository is canonical LF; normalize so a CRLF checkout cannot fail this.
            var checkedIn = File.ReadAllText(path).Replace("\r\n", "\n");

            Assert.True(
                string.Equals(checkedIn, example.Json, StringComparison.Ordinal),
                $"Checked-in example '{example.FileName}' is stale - its parent strategy changed since it was "
                + $"generated.{Environment.NewLine}Regenerate it with:{Environment.NewLine}  "
                + $"{CandidateGenomeExamples.RegenerateCommand}{Environment.NewLine}"
                + $"Do not hand-edit the file: candidateId, the parent fingerprint, and variedGenes are all derived, "
                + $"and editing one without the others trades this failure for a different one.");
        }
    }

    [Theory]
    [MemberData(nameof(OutOfBoundsGeneSets))]
    public void OutOfBoundsGeneSet_FailsValidation(string expectedErrorFragment, CandidateGeneSet genes)
    {
        var definition = GetControlDefinition();
        var genome = new CandidateGenome
        {
            SchemaVersion = CandidateGenome.CurrentSchemaVersion,
            CandidateId = CandidateGenomeFingerprint.DeriveCandidateId(definition.StrategyId, genes),
            DisplayName = "CAND_BoundsProbe",
            Lineage = new CandidateLineage
            {
                ParentStrategyId = definition.StrategyId,
                ParentDefinitionFingerprint = definition.DefinitionFingerprint,
                ParentStrategySet = StrategySetEnum.Testing
            },
            Genes = genes,
            VariedGenes = CandidateGenomeFingerprint.FindDifferences(
                CandidateGenomeFactory.ExtractGenes(GetControlStrategy()),
                genes)
        };

        Assert.Contains(CandidateGenomeValidator.Validate(genome), error => error.Contains(expectedErrorFragment, StringComparison.Ordinal));
    }

    public static TheoryData<string, CandidateGeneSet> OutOfBoundsGeneSets()
    {
        return new TheoryData<string, CandidateGeneSet>
        {
            {
                "references unknown mutation ID",
                CreateMinimalGenes(goals: new[] { new CandidateMutationGoal { MutationId = -1 } })
            },
            {
                "which is not a Mycelial Surge",
                CreateMinimalGenes(surgeIds: new[] { MutationIds.CreepingMold })
            },
            {
                "which is also a target goal",
                CreateMinimalGenes(
                    goals: new[] { new CandidateMutationGoal { MutationId = MutationIds.CreepingMold } },
                    excluded: new[] { MutationIds.CreepingMold })
            },
            {
                "genes.startingSporeEdgeOffset must be between",
                CreateMinimalGenes(startingSporeEdgeOffset: CandidateGenome.MaximumStartingSporeEdgeOffset + 1)
            },
            {
                "genes.surgeAttemptTurnFrequency must be between",
                CreateMinimalGenes(surgeAttemptTurnFrequency: 0)
            },
            {
                "must be ordered by descending priority",
                CreateMinimalGenes(preferences: new[]
                {
                    new CandidateMycovariantPreference { MycovariantIds = new[] { MycovariantIds.PlasmidBountyId }, Priority = 1 },
                    new CandidateMycovariantPreference { MycovariantIds = new[] { MycovariantIds.PlasmidBountyIIId }, Priority = 9 }
                })
            },
            {
                "references unknown mycovariant ID",
                CreateMinimalGenes(preferences: new[]
                {
                    new CandidateMycovariantPreference { MycovariantIds = new[] { -7 }, Priority = 1 }
                })
            },
            {
                "targetLevel 0 is outside",
                CreateMinimalGenes(goals: new[]
                {
                    new CandidateMutationGoal { MutationId = MutationIds.CreepingMold, TargetLevel = 0 }
                })
            },
            {
                // A flat repeat can never trigger a purchase, so it is dead configuration.
                "which is not above the level 2 already targeted earlier",
                CreateMinimalGenes(goals: new[]
                {
                    new CandidateMutationGoal { MutationId = MutationIds.CreepingMold, TargetLevel = 2 },
                    new CandidateMutationGoal { MutationId = MutationIds.CreepingMold, TargetLevel = 2 }
                })
            },
            {
                // Null means "max level", so nothing may follow it for the same mutation.
                "which is not above the level 4 already targeted earlier",
                CreateMinimalGenes(goals: new[]
                {
                    new CandidateMutationGoal { MutationId = MutationIds.CreepingMold },
                    new CandidateMutationGoal { MutationId = MutationIds.CreepingMold, TargetLevel = 3 }
                })
            }
        };
    }

    /// <summary>
    /// Repeating a mutation at a rising level is a deliberate roster idiom (buy a little early,
    /// return for more later), so the ladder must stay legal while flat and falling repeats fail.
    /// </summary>
    [Fact]
    public void RisingGoalLadder_IsLegal()
    {
        var definition = GetControlDefinition();
        var genes = CandidateGenomeFactory.ExtractGenes(GetControlStrategy());
        var ladder = new CandidateGeneSet
        {
            PrioritizeHighTier = genes.PrioritizeHighTier,
            MaxTier = genes.MaxTier,
            PriorityMutationCategories = genes.PriorityMutationCategories,
            TargetMutationGoals = new[]
            {
                new CandidateMutationGoal { MutationId = MutationIds.CreepingMold, TargetLevel = 1 },
                new CandidateMutationGoal { MutationId = MutationIds.AnabolicInversion, TargetLevel = 1 },
                new CandidateMutationGoal { MutationId = MutationIds.CreepingMold, TargetLevel = 3 },
                new CandidateMutationGoal { MutationId = MutationIds.CreepingMold }
            },
            SurgePriorityIds = genes.SurgePriorityIds,
            SurgeAttemptTurnFrequency = genes.SurgeAttemptTurnFrequency,
            EconomyBias = genes.EconomyBias,
            MycovariantPreferences = genes.MycovariantPreferences,
            ExcludedMutationIds = genes.ExcludedMutationIds,
            StartingSporeEdgeOffset = genes.StartingSporeEdgeOffset
        };

        var genome = CandidateGenomeFactory.CreateFromParent(definition, ladder, "RisingLadder");

        Assert.Empty(CandidateGenomeValidator.Validate(genome));
        Assert.Equal(new[] { CandidateGene.TargetMutationGoals }, genome.VariedGenes);
    }

    [Fact]
    public void DisplayNameWithoutCandidatePrefix_FailsValidation()
    {
        var genome = CreateOpenerOrderCandidate();
        var renamed = new CandidateGenome
        {
            SchemaVersion = genome.SchemaVersion,
            CandidateId = genome.CandidateId,
            DisplayName = "TST_NotACandidate",
            Lineage = genome.Lineage,
            Genes = genome.Genes,
            VariedGenes = genome.VariedGenes
        };

        Assert.Contains(CandidateGenomeValidator.Validate(renamed), error => error.Contains("must start with 'CAND_'", StringComparison.Ordinal));
    }

    [Fact]
    public void Materialize_ProducesAnOrdinaryParameterizedStrategy()
    {
        var genome = CreateOpenerOrderCandidate();
        var strategy = CandidateGenomeFactory.Materialize(genome);

        Assert.Equal(genome.DisplayName, strategy.StrategyName);
        Assert.Equal(
            MutationIds.AnabolicInversion,
            strategy.TargetMutationGoals[0].MutationId);
        // The materialized strategy is re-extractable, so a candidate can itself parent a candidate.
        Assert.Equal(
            CandidateGenomeFingerprint.Compute(genome.Genes),
            CandidateGenomeFingerprint.Compute(CandidateGenomeFactory.ExtractGenes(strategy)));
    }

    /// <summary>
    /// Builds the opener-order candidate that Phase 6 already validated by experiment: the
    /// Balanced Control build plan with Anabolic Inversion moved ahead of Creeping Mold.
    /// </summary>
    private static CandidateGenome CreateOpenerOrderCandidate()
    {
        var definition = GetControlDefinition();
        var genes = CandidateGenomeFactory.ExtractGenes(GetControlStrategy());
        var goals = genes.TargetMutationGoals.ToList();
        (goals[0], goals[1]) = (goals[1], goals[0]);

        return CandidateGenomeFactory.CreateFromParent(
            definition,
            new CandidateGeneSet
            {
                PrioritizeHighTier = genes.PrioritizeHighTier,
                MaxTier = genes.MaxTier,
                PriorityMutationCategories = genes.PriorityMutationCategories,
                TargetMutationGoals = goals,
                SurgePriorityIds = genes.SurgePriorityIds,
                SurgeAttemptTurnFrequency = genes.SurgeAttemptTurnFrequency,
                EconomyBias = genes.EconomyBias,
                MycovariantPreferences = genes.MycovariantPreferences,
                ExcludedMutationIds = genes.ExcludedMutationIds,
                StartingSporeEdgeOffset = genes.StartingSporeEdgeOffset
            },
            "OpenerOrderAnabolicFirst",
            "Moves Anabolic Inversion ahead of Creeping Mold, the contrast confirmed by the Phase 6 opener-order holdout.");
    }

    private static CandidateGenome WithGenes(CandidateGenome genome, CandidateGeneSet genes) => new()
    {
        SchemaVersion = genome.SchemaVersion,
        CandidateId = CandidateGenomeFingerprint.DeriveCandidateId(genome.Lineage.ParentStrategyId, genes),
        DisplayName = genome.DisplayName,
        Lineage = genome.Lineage,
        Genes = genes,
        VariedGenes = genome.VariedGenes
    };

    private static CandidateGeneSet CreateMinimalGenes(
        IReadOnlyList<MutationCategory>? categories = null,
        IReadOnlyList<CandidateMutationGoal>? goals = null,
        IReadOnlyList<int>? surgeIds = null,
        IReadOnlyList<CandidateMycovariantPreference>? preferences = null,
        IReadOnlyList<int>? excluded = null,
        int surgeAttemptTurnFrequency = 5,
        int startingSporeEdgeOffset = 0)
    {
        return new CandidateGeneSet
        {
            PrioritizeHighTier = true,
            MaxTier = MutationTier.Tier10,
            PriorityMutationCategories = categories,
            TargetMutationGoals = goals ?? Array.Empty<CandidateMutationGoal>(),
            SurgePriorityIds = surgeIds ?? Array.Empty<int>(),
            SurgeAttemptTurnFrequency = surgeAttemptTurnFrequency,
            EconomyBias = EconomyBias.ModerateEconomy,
            MycovariantPreferences = preferences ?? Array.Empty<CandidateMycovariantPreference>(),
            ExcludedMutationIds = excluded ?? Array.Empty<int>(),
            StartingSporeEdgeOffset = startingSporeEdgeOffset
        };
    }

    private static StrategyDefinition GetControlDefinition()
    {
        EnsureRosterInitialized();
        return StrategyRegistry.GetDefinition(StrategySetEnum.Testing, ControlStrategyName)
            ?? throw new InvalidOperationException($"Strategy '{ControlStrategyName}' is not registered.");
    }

    private static ParameterizedSpendingStrategy GetControlStrategy() =>
        (ParameterizedSpendingStrategy)GetControlDefinition().Strategy;

    private static IReadOnlyList<(StrategyDefinition Definition, ParameterizedSpendingStrategy Strategy)> GetParameterizedDefinitions()
    {
        EnsureRosterInitialized();
        return Enum.GetValues(typeof(StrategySetEnum))
            .Cast<StrategySetEnum>()
            .SelectMany(StrategyRegistry.GetDefinitions)
            .Where(definition => definition.Strategy is ParameterizedSpendingStrategy)
            .Select(definition => (definition, (ParameterizedSpendingStrategy)definition.Strategy))
            .ToList();
    }

    private static void EnsureRosterInitialized() => _ = AIRoster.TestingStrategies.Count;
}
