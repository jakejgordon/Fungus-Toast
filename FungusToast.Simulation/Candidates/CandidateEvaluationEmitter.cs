using System.Globalization;
using FungusToast.Core.AI;
using FungusToast.Simulation.Experiments;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// The two arms one stage runs, and the pairing group that joins them.
/// </summary>
public sealed class CandidateStageManifests
{
    public required CandidateEvaluationStage Stage { get; init; }

    public required string PairingGroupId { get; init; }

    public required ExperimentManifest Control { get; init; }

    public required ExperimentManifest Treatment { get; init; }
}

/// <summary>
/// Emits the paired experiment manifests for one stage of a candidate's evaluation, and enforces
/// the progression rules between stages.
///
/// A stage produces two single-condition manifests - a control arm running the parent and a
/// treatment arm running the candidate - sharing one pairing group and identical in players,
/// board, seed, slot policy, and enabled systems. That single-swap shape is what lets the paired
/// analyzer attribute a difference to the candidate rather than to the context.
///
/// Two separate runs rather than one two-condition manifest, for two converging reasons. The batch
/// runner derives its conditions from config strata (player counts x boards x strategy sets), so it
/// cannot express two conditions of identical shape differing only in lineup. And the offline
/// analyzer pairs two artifact *folders*, one control and one treatment, which is exactly what two
/// runs produce. Each arm is therefore directly runnable today; RenderCommandLine emits the
/// invocation.
///
/// Progression is refused rather than warned about. A stage cannot be emitted until every earlier
/// game-consuming stage has passed, because the staged gates exist to stop a candidate cheaply -
/// letting a caller skip smoke and spend a hundred games would defeat the point.
/// </summary>
public static class CandidateEvaluationEmitter
{
    /// <summary>Must match the offline analyzer's ANALYSIS_VERSION or it refuses to issue a verdict.</summary>
    public const string AnalysisVersionContract = "fungus-toast.analysis.v2";

    private static readonly IReadOnlyList<CandidateEvaluationStage> GameConsumingStages = new[]
    {
        CandidateEvaluationStage.Smoke,
        CandidateEvaluationStage.Calibration,
        CandidateEvaluationStage.Comparison,
        CandidateEvaluationStage.Holdout
    };

    public static CandidateStageManifests Emit(
        CandidateEvaluationPlan plan,
        CandidateEvaluationStage stage,
        IReadOnlyCollection<CandidateEvaluationStage> passedStages)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(passedStages);

        var errors = CandidateEvaluationPlanValidator.Validate(plan);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Candidate evaluation plan is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}",
                nameof(plan));
        }

        if (!GameConsumingStages.Contains(stage))
        {
            throw new ArgumentException(
                $"Stage '{stage}' costs no games and emits no manifest; it is run by the generator and the characterization gate.",
                nameof(stage));
        }

        RequirePriorStages(stage, passedStages);

        var gamesPerCondition = GamesPerCondition(stage, plan);
        var context = stage == CandidateEvaluationStage.Holdout ? plan.HoldoutContext : plan.ScreeningContext;
        var pairingGroupId = $"{plan.EvaluationId}-{stage.ToString().ToLowerInvariant()}";

        // Both arms declare the same analysis plan, budgets included: the analyzer refuses a
        // verdict whose control and treatment budgets disagree.
        var analysis = BuildAnalysisPlan(plan, stage, pairingGroupId);
        return new CandidateStageManifests
        {
            Stage = stage,
            PairingGroupId = pairingGroupId,
            Control = BuildArmManifest(plan, stage, context, pairingGroupId, analysis, gamesPerCondition, isTreatment: false),
            Treatment = BuildArmManifest(plan, stage, context, pairingGroupId, analysis, gamesPerCondition, isTreatment: true)
        };
    }

    /// <summary>
    /// The CLI invocation that runs one arm. The runner builds its manifest from flags rather than
    /// reading one from disk, so this is what turns an emitted plan into an actual artifact.
    /// </summary>
    /// <param name="candidateCatalogPath">
    /// Path to the catalog file the run must load. The simulator is a separate process, so a
    /// candidate published in this one does not exist for it; without the catalog the run fails
    /// resolving its own lineup.
    /// </param>
    public static IReadOnlyList<string> RenderCommandLine(
        CandidateStageManifests stageManifests,
        bool isTreatment,
        string candidateCatalogPath)
    {
        ArgumentNullException.ThrowIfNull(stageManifests);
        if (string.IsNullOrWhiteSpace(candidateCatalogPath))
            throw new ArgumentException("A candidate catalog path is required.", nameof(candidateCatalogPath));

        var manifest = isTreatment ? stageManifests.Treatment : stageManifests.Control;
        var condition = manifest.Conditions[0];

        var arguments = new List<string>
        {
            "--candidate-catalog", candidateCatalogPath,
            "--experiment-id", manifest.ExperimentId,
            "--purpose", manifest.Purpose,
            "--games", manifest.GamesPerCondition.ToString(CultureInfo.InvariantCulture),
            "--seed", manifest.BaseSeed.ToString(CultureInfo.InvariantCulture),
            "--players", condition.PlayerCount.ToString(CultureInfo.InvariantCulture),
            "--width", condition.Board.Width.ToString(CultureInfo.InvariantCulture),
            "--height", condition.Board.Height.ToString(CultureInfo.InvariantCulture),
            "--strategy-set", condition.Strategies.StrategySet.ToString(),
            "--strategy-names", string.Join(",", condition.Strategies.ExplicitStrategyNames),
            "--pairing-group-id", stageManifests.PairingGroupId,
            "--rotate-slots",
            "--no-nutrient-patches",
            "--no-mycovariants",
            "--no-starting-adaptations",
            "--parquet",
            "--no-keyboard",
            "--analysis-version", manifest.Analysis.AnalysisVersion,
            "--evidence-stage", manifest.Analysis.EvidenceStage.ToString(),
            "--total-game-budget", manifest.TotalGameBudget.ToString(CultureInfo.InvariantCulture),
            "--runtime-budget-seconds", manifest.RuntimeBudgetSeconds.ToString(CultureInfo.InvariantCulture)
        };

        if (manifest.Analysis.Hypothesis is { } hypothesis)
        {
            arguments.AddRange(new[]
            {
                "--hypothesis-id", hypothesis.HypothesisId,
                "--target-strategy-id", hypothesis.TargetStrategyId,
                "--control-strategy-id", hypothesis.ControlStrategyId ?? string.Empty,
                "--primary-metric", hypothesis.PrimaryMetric.ToString(),
                "--direction", hypothesis.Direction.ToString(),
                "--margin", hypothesis.Margin.ToString(CultureInfo.InvariantCulture)
            });
        }

        return arguments;
    }

    private static ExperimentManifest BuildArmManifest(
        CandidateEvaluationPlan plan,
        CandidateEvaluationStage stage,
        CandidateEvaluationContext context,
        string pairingGroupId,
        ExperimentAnalysisPlan analysis,
        int gamesPerCondition,
        bool isTreatment)
    {
        var arm = isTreatment ? "treatment" : "control";
        return new ExperimentManifest
        {
            SchemaVersion = ExperimentManifest.CurrentSchemaVersion,
            ExperimentId = $"{plan.EvaluationId}_{stage.ToString().ToLowerInvariant()}_{arm}",
            Purpose = $"{plan.Purpose} ({stage} {arm}: paired swap of {plan.CandidateDisplayName} for {plan.ParentStrategyName}.)",
            GamesPerCondition = gamesPerCondition,
            BaseSeed = context.BaseSeed,
            // Both arms declare the combined budget, because the analyzer checks the two arms'
            // measured runtime against one shared figure.
            TotalGameBudget = gamesPerCondition * 2,
            RuntimeBudgetSeconds = plan.RuntimeBudgetSecondsPerStage,
            Analysis = analysis,
            Conditions = new[] { BuildCondition(plan, stage, context, pairingGroupId, isTreatment) }
        };
    }

    /// <summary>
    /// The stages that must already have passed before <paramref name="stage"/> may run.
    /// </summary>
    public static IReadOnlyList<CandidateEvaluationStage> RequiredPriorStages(CandidateEvaluationStage stage)
    {
        var ordered = Enum.GetValues(typeof(CandidateEvaluationStage))
            .Cast<CandidateEvaluationStage>()
            .OrderBy(value => (int)value)
            .ToList();
        return ordered.Where(value => (int)value < (int)stage).ToList();
    }

    private static void RequirePriorStages(
        CandidateEvaluationStage stage,
        IReadOnlyCollection<CandidateEvaluationStage> passedStages)
    {
        var missing = RequiredPriorStages(stage).Where(required => !passedStages.Contains(required)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot emit the {stage} stage before {string.Join(", ", missing)} has passed. "
                + "Staged evidence exists to stop a candidate cheaply; skipping a stage spends a larger batch to learn the same thing.");
        }
    }

    private static int GamesPerCondition(CandidateEvaluationStage stage, CandidateEvaluationPlan plan) => stage switch
    {
        CandidateEvaluationStage.Smoke => plan.SmokeGames,
        CandidateEvaluationStage.Calibration => 20,
        CandidateEvaluationStage.Comparison => 50,
        CandidateEvaluationStage.Holdout => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Stage consumes no games.")
    };

    /// <summary>
    /// Only comparison and holdout carry a hypothesis. Smoke and calibration exist to catch
    /// integrity failures and obvious regressions, and the frozen gates forbid a decision-bearing
    /// plan below comparison.
    /// </summary>
    private static ExperimentAnalysisPlan BuildAnalysisPlan(
        CandidateEvaluationPlan plan,
        CandidateEvaluationStage stage,
        string pairingGroupId)
    {
        var evidenceStage = stage switch
        {
            CandidateEvaluationStage.Smoke => ExperimentEvidenceStage.Smoke,
            CandidateEvaluationStage.Calibration => ExperimentEvidenceStage.Calibration,
            CandidateEvaluationStage.Comparison => ExperimentEvidenceStage.Comparison,
            CandidateEvaluationStage.Holdout => ExperimentEvidenceStage.Holdout,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Stage consumes no games.")
        };

        var isDecisionBearing = stage is CandidateEvaluationStage.Comparison or CandidateEvaluationStage.Holdout;
        return new ExperimentAnalysisPlan
        {
            AnalysisVersion = AnalysisVersionContract,
            EvidenceStage = evidenceStage,
            Hypothesis = isDecisionBearing
                ? new ExperimentHypothesis
                {
                    HypothesisId = $"{plan.EvaluationId}-{stage.ToString().ToLowerInvariant()}",
                    // The analyzer requires the declared context to equal the pairing group it sees.
                    PrimaryContextId = pairingGroupId,
                    TargetStrategyId = ResolveStrategyId(plan.CandidateDisplayName),
                    // The treatment swaps one strategy, so the target's control-arm counterpart
                    // must be named or the verdict resolves to no rows.
                    ControlStrategyId = ResolveStrategyId(plan.ParentStrategyName),
                    PrimaryMetric = ExperimentPrimaryMetric.NormalizedBoardShare,
                    Estimand = ExperimentEstimand.PairedMeanDifference,
                    Direction = plan.Direction,
                    Margin = plan.Margin
                }
                : null
        };
    }

    private static ExperimentCondition BuildCondition(
        CandidateEvaluationPlan plan,
        CandidateEvaluationStage stage,
        CandidateEvaluationContext context,
        string pairingGroupId,
        bool isTreatment)
    {
        var arm = isTreatment ? "treatment" : "control";
        var subject = isTreatment ? plan.CandidateDisplayName : plan.ParentStrategyName;

        return new ExperimentCondition
        {
            ConditionId = $"{plan.EvaluationId}.{stage.ToString().ToLowerInvariant()}.{arm}",
            PairingGroupId = pairingGroupId,
            PlayerCount = 2,
            Board = new ExperimentBoard { Width = context.BoardWidth, Height = context.BoardHeight },
            Strategies = new ExperimentStrategySelection
            {
                // Both arms name the generated set, which holds the candidate plus the authored
                // parent and opponent references published alongside it.
                StrategySet = StrategySetEnum.Generated,
                SelectionPolicy = StrategySelectionPolicy.RandomUnique,
                ExplicitStrategyNames = new[] { subject, plan.OpponentStrategyName }
            },
            // Everything optional is off so the measured difference is the strategy swap and not a
            // system that happens to favor one build.
            Systems = new ExperimentSystems
            {
                NutrientPatchesEnabled = false,
                MycovariantDraftEnabled = false,
                StartingAdaptationsEnabled = false
            },
            Positioning = new ExperimentPositioning(),
            SlotAssignmentPolicy = SlotAssignmentPolicy.RotateByGame
        };
    }

    private static string ResolveStrategyId(string strategyName)
    {
        var definition = StrategyRegistry.GetDefinition(StrategySetEnum.Generated, strategyName)
            ?? throw new InvalidOperationException(
                $"Strategy '{strategyName}' is not published in the generated catalog; publish the evaluation cast first.");
        return definition.StrategyId;
    }
}
