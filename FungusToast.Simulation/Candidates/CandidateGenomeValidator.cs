using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Strict structural, semantic, and lineage validation for candidate genomes.
///
/// Two rules carry most of the weight. First, every referenced mutation, mycovariant, and
/// category must exist, so a generated candidate cannot silently target content that was renamed
/// or removed. Second, the declared <see cref="CandidateGenome.VariedGenes"/> must be exactly the
/// genes that differ from the parent - an undeclared difference and a declared gene that did not
/// change both fail, the same contract <c>--compare-manifests</c> already enforces for experiment
/// inputs. That is what keeps a candidate a single-variable treatment by construction rather than
/// by reviewer diligence.
/// </summary>
public static partial class CandidateGenomeValidator
{
    public static IReadOnlyList<string> Validate(CandidateGenome genome)
    {
        ArgumentNullException.ThrowIfNull(genome);
        var errors = new List<string>();

        if (!string.Equals(genome.SchemaVersion, CandidateGenome.CurrentSchemaVersion, StringComparison.Ordinal))
            errors.Add($"schemaVersion must be '{CandidateGenome.CurrentSchemaVersion}'.");

        ValidateDisplayName(genome.DisplayName, errors);

        if (genome.Genes == null)
        {
            errors.Add("genes is required.");
            return errors;
        }

        ValidateGenes(genome.Genes, errors);
        ValidateVariedGeneDeclaration(genome.VariedGenes, errors);
        ValidateLineage(genome, errors);
        return errors;
    }

    private static void ValidateDisplayName(string displayName, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 96 || !DisplayNamePattern().IsMatch(displayName))
        {
            errors.Add($"displayName must be 1-96 characters using only letters, numbers, '.', '_' or '-' and must start with '{CandidateGenome.DisplayNamePrefix}'.");
            return;
        }

        // A generated candidate must never shadow an authored roster strategy: StrategyRegistry
        // rejects duplicate names, and a collision would also make artifacts ambiguous after the
        // fact. The Generated set is excluded because it is the candidate's own home - publishing
        // replaces it wholesale, so re-validating a published candidate must not see itself as a
        // collision. Duplicates within a single publish are caught by the catalog instead.
        var collides = Enum.GetValues(typeof(StrategySetEnum))
            .Cast<StrategySetEnum>()
            .Where(strategySet => strategySet != StrategySetEnum.Generated)
            .SelectMany(StrategyRegistry.GetDefinitions)
            .Any(definition => string.Equals(definition.Strategy.StrategyName, displayName, StringComparison.OrdinalIgnoreCase));
        if (collides)
            errors.Add($"displayName '{displayName}' collides with an authored strategy name.");
    }

    private static void ValidateGenes(CandidateGeneSet genes, ICollection<string> errors)
    {
        if (!Enum.IsDefined(typeof(MutationTier), genes.MaxTier))
            errors.Add($"genes.maxTier '{genes.MaxTier}' is not a defined mutation tier.");
        if (!Enum.IsDefined(typeof(EconomyBias), genes.EconomyBias))
            errors.Add($"genes.economyBias '{genes.EconomyBias}' is not a defined economy bias.");

        if (genes.SurgeAttemptTurnFrequency < CandidateGenome.MinimumSurgeAttemptTurnFrequency
            || genes.SurgeAttemptTurnFrequency > CandidateGenome.MaximumSurgeAttemptTurnFrequency)
        {
            errors.Add($"genes.surgeAttemptTurnFrequency must be between {CandidateGenome.MinimumSurgeAttemptTurnFrequency} and {CandidateGenome.MaximumSurgeAttemptTurnFrequency}.");
        }

        if (genes.StartingSporeEdgeOffset < CandidateGenome.MinimumStartingSporeEdgeOffset
            || genes.StartingSporeEdgeOffset > CandidateGenome.MaximumStartingSporeEdgeOffset)
        {
            errors.Add($"genes.startingSporeEdgeOffset must be between {CandidateGenome.MinimumStartingSporeEdgeOffset} and {CandidateGenome.MaximumStartingSporeEdgeOffset}.");
        }

        ValidateCategories(genes.PriorityMutationCategories, errors);
        var goalIds = ValidateGoals(genes.TargetMutationGoals, errors);
        ValidateSurgeIds(genes.SurgePriorityIds, errors);
        ValidatePreferences(genes.MycovariantPreferences, errors);
        ValidateExclusions(genes.ExcludedMutationIds, goalIds, errors);
    }

    private static void ValidateCategories(IReadOnlyList<MutationCategory>? categories, ICollection<string> errors)
    {
        // Null is legal and meaningful: Core treats it as "every category".
        if (categories == null) return;

        var categoryCount = Enum.GetValues(typeof(MutationCategory)).Length;
        if (categories.Count > categoryCount)
            errors.Add($"genes.priorityMutationCategories cannot contain more than {categoryCount} entries.");

        var seen = new HashSet<MutationCategory>();
        foreach (var category in categories)
        {
            if (!Enum.IsDefined(typeof(MutationCategory), category))
                errors.Add($"genes.priorityMutationCategories contains undefined category '{category}'.");
            else if (!seen.Add(category))
                errors.Add($"genes.priorityMutationCategories repeats category '{category}'.");
        }
    }

    private static HashSet<int> ValidateGoals(IReadOnlyList<CandidateMutationGoal>? goals, ICollection<string> errors)
    {
        var goalIds = new HashSet<int>();
        if (goals == null)
        {
            errors.Add("genes.targetMutationGoals must not be null.");
            return goalIds;
        }

        if (goals.Count > CandidateGenome.MaximumTargetMutationGoals)
            errors.Add($"genes.targetMutationGoals cannot contain more than {CandidateGenome.MaximumTargetMutationGoals} entries.");

        // Repeating a mutation is a real authoring idiom: buy it to a low level early, then come
        // back for more later. It is only legal as a rising ladder, because a repeat at or below
        // the level already targeted is dead configuration that can never trigger a purchase.
        var highestTargetedLevel = new Dictionary<int, int>();

        for (var index = 0; index < goals.Count; index++)
        {
            var goal = goals[index];
            if (goal == null)
            {
                errors.Add($"genes.targetMutationGoals[{index}] must not be null.");
                continue;
            }

            var mutation = MutationRegistry.GetById(goal.MutationId);
            if (mutation == null)
            {
                errors.Add($"genes.targetMutationGoals[{index}] references unknown mutation ID {goal.MutationId}.");
                continue;
            }

            goalIds.Add(goal.MutationId);

            if (goal.TargetLevel is { } explicitLevel && (explicitLevel < 1 || explicitLevel > mutation.MaxLevel))
            {
                errors.Add($"genes.targetMutationGoals[{index}] targetLevel {explicitLevel} is outside 1-{mutation.MaxLevel} for '{mutation.Name}'.");
                continue;
            }

            // A null target means the mutation's maximum level, so it ranks above every explicit level.
            var effectiveLevel = goal.TargetLevel ?? mutation.MaxLevel;
            if (highestTargetedLevel.TryGetValue(goal.MutationId, out var previousLevel) && effectiveLevel <= previousLevel)
            {
                errors.Add($"genes.targetMutationGoals[{index}] repeats '{mutation.Name}' at level {effectiveLevel}, which is not above the level {previousLevel} already targeted earlier.");
                continue;
            }

            highestTargetedLevel[goal.MutationId] = effectiveLevel;
        }

        return goalIds;
    }

    private static void ValidateSurgeIds(IReadOnlyList<int>? surgeIds, ICollection<string> errors)
    {
        if (surgeIds == null)
        {
            errors.Add("genes.surgePriorityIds must not be null.");
            return;
        }

        if (surgeIds.Count > CandidateGenome.MaximumSurgePriorityIds)
            errors.Add($"genes.surgePriorityIds cannot contain more than {CandidateGenome.MaximumSurgePriorityIds} entries.");

        var seen = new HashSet<int>();
        foreach (var surgeId in surgeIds)
        {
            var mutation = MutationRegistry.GetById(surgeId);
            if (mutation == null)
                errors.Add($"genes.surgePriorityIds references unknown mutation ID {surgeId}.");
            else if (mutation.Category != MutationCategory.MycelialSurges)
                errors.Add($"genes.surgePriorityIds contains mutation ID {surgeId} ({mutation.Name}), which is not a Mycelial Surge.");
            if (!seen.Add(surgeId))
                errors.Add($"genes.surgePriorityIds repeats mutation ID {surgeId}.");
        }
    }

    private static void ValidatePreferences(IReadOnlyList<CandidateMycovariantPreference>? preferences, ICollection<string> errors)
    {
        if (preferences == null)
        {
            errors.Add("genes.mycovariantPreferences must not be null.");
            return;
        }

        var mycovariantIds = MycovariantRepository.All.Select(mycovariant => mycovariant.Id).ToHashSet();
        if (preferences.Count > mycovariantIds.Count)
            errors.Add($"genes.mycovariantPreferences cannot contain more than {mycovariantIds.Count} entries.");

        var seenIds = new HashSet<int>();
        int? previousPriority = null;
        for (var index = 0; index < preferences.Count; index++)
        {
            var preference = preferences[index];
            if (preference == null)
            {
                errors.Add($"genes.mycovariantPreferences[{index}] must not be null.");
                continue;
            }

            if (preference.Priority < CandidateGenome.MinimumMycovariantPreferencePriority
                || preference.Priority > CandidateGenome.MaximumMycovariantPreferencePriority)
            {
                errors.Add($"genes.mycovariantPreferences[{index}].priority must be between {CandidateGenome.MinimumMycovariantPreferencePriority} and {CandidateGenome.MaximumMycovariantPreferencePriority}.");
            }

            // Serialized order must equal Core's evaluation order so the file reads the way it runs.
            if (previousPriority is { } previous && preference.Priority > previous)
                errors.Add($"genes.mycovariantPreferences must be ordered by descending priority; entry {index} raises priority from {previous} to {preference.Priority}.");
            previousPriority = preference.Priority;

            var ids = preference.MycovariantIds;
            if (ids == null || ids.Count == 0)
            {
                errors.Add($"genes.mycovariantPreferences[{index}] must contain at least one mycovariant ID.");
                continue;
            }

            foreach (var id in ids)
            {
                if (!mycovariantIds.Contains(id))
                    errors.Add($"genes.mycovariantPreferences[{index}] references unknown mycovariant ID {id}.");
                if (!seenIds.Add(id))
                    errors.Add($"genes.mycovariantPreferences repeats mycovariant ID {id}.");
            }
        }
    }

    private static void ValidateExclusions(IReadOnlyList<int>? exclusions, IReadOnlySet<int> goalIds, ICollection<string> errors)
    {
        if (exclusions == null)
        {
            errors.Add("genes.excludedMutationIds must not be null.");
            return;
        }

        if (exclusions.Count > CandidateGenome.MaximumExcludedMutationIds)
            errors.Add($"genes.excludedMutationIds cannot contain more than {CandidateGenome.MaximumExcludedMutationIds} entries.");

        var seen = new HashSet<int>();
        foreach (var excludedId in exclusions)
        {
            if (MutationRegistry.GetById(excludedId) == null)
                errors.Add($"genes.excludedMutationIds references unknown mutation ID {excludedId}.");
            if (!seen.Add(excludedId))
                errors.Add($"genes.excludedMutationIds repeats mutation ID {excludedId}.");
            // Mirrors ParameterizedSpendingStrategy's own constructor invariant.
            if (goalIds.Contains(excludedId))
                errors.Add($"genes.excludedMutationIds contains mutation ID {excludedId}, which is also a target goal.");
        }
    }

    private static void ValidateVariedGeneDeclaration(IReadOnlyList<CandidateGene>? variedGenes, ICollection<string> errors)
    {
        if (variedGenes == null)
        {
            errors.Add("variedGenes must not be null.");
            return;
        }

        var seen = new HashSet<CandidateGene>();
        foreach (var gene in variedGenes)
        {
            if (!Enum.IsDefined(typeof(CandidateGene), gene))
                errors.Add($"variedGenes contains undefined gene '{gene}'.");
            else if (!seen.Add(gene))
                errors.Add($"variedGenes repeats gene '{gene}'.");
        }
    }

    private static void ValidateLineage(CandidateGenome genome, ICollection<string> errors)
    {
        var lineage = genome.Lineage;
        if (lineage == null)
        {
            errors.Add("lineage is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(lineage.ParentStrategyId))
        {
            errors.Add("lineage.parentStrategyId is required.");
            return;
        }

        if (!Enum.IsDefined(typeof(StrategySetEnum), lineage.ParentStrategySet))
        {
            errors.Add($"lineage.parentStrategySet '{lineage.ParentStrategySet}' is not a defined strategy set.");
            return;
        }

        // The candidate ID is derived, never authored, so an edited or stale ID is caught here.
        var expectedCandidateId = CandidateGenomeFingerprint.DeriveCandidateId(lineage.ParentStrategyId, genome.Genes);
        if (!string.Equals(genome.CandidateId, expectedCandidateId, StringComparison.Ordinal))
            errors.Add($"candidateId must be '{expectedCandidateId}' for this parent and gene set.");

        var parent = StrategyRegistry.GetDefinitions(lineage.ParentStrategySet)
            .FirstOrDefault(definition => string.Equals(definition.StrategyId, lineage.ParentStrategyId, StringComparison.Ordinal));
        if (parent == null)
        {
            errors.Add($"lineage.parentStrategyId '{lineage.ParentStrategyId}' is not registered in strategy set '{lineage.ParentStrategySet}'.");
            return;
        }

        if (!string.Equals(parent.DefinitionFingerprint, lineage.ParentDefinitionFingerprint, StringComparison.Ordinal))
        {
            errors.Add($"lineage.parentDefinitionFingerprint does not match the registered parent fingerprint '{parent.DefinitionFingerprint}'; the parent changed since this candidate was generated.");
            return;
        }

        if (parent.Strategy is not ParameterizedSpendingStrategy parameterizedParent)
        {
            errors.Add($"lineage.parentStrategyId '{lineage.ParentStrategyId}' is not a parameterized strategy and cannot parent a genome.");
            return;
        }

        var parentGenes = CandidateGenomeFactory.ExtractGenes(parameterizedParent);
        var actualDifferences = CandidateGenomeFingerprint.FindDifferences(parentGenes, genome.Genes);
        var declared = genome.VariedGenes?.ToHashSet() ?? new HashSet<CandidateGene>();

        var undeclared = actualDifferences.Where(gene => !declared.Contains(gene)).ToList();
        if (undeclared.Count > 0)
            errors.Add($"variedGenes omits genes that actually differ from the parent: {string.Join(", ", undeclared)}.");

        var unchanged = declared.Where(gene => !actualDifferences.Contains(gene)).OrderBy(gene => gene).ToList();
        if (unchanged.Count > 0)
            errors.Add($"variedGenes declares genes that do not differ from the parent: {string.Join(", ", unchanged)}.");
    }

    [GeneratedRegex("^CAND_[A-Za-z0-9._-]+$")]
    private static partial Regex DisplayNamePattern();
}
