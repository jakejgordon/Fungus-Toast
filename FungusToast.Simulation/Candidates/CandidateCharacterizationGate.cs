using System.Globalization;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Why a candidate failed the zero-game characterization stage.
/// </summary>
public enum CandidateCharacterizationFailure
{
    /// <summary>The gene set is valid on paper but will not construct a strategy.</summary>
    ConstructionFailed,

    /// <summary>Mutation spending threw, so the candidate would abort a real game.</summary>
    SpendingThrew,

    /// <summary>The candidate never bought anything across the whole scripted budget.</summary>
    NeverSpent,

    /// <summary>The same seed produced different purchases, so paired comparison would be meaningless.</summary>
    NondeterministicSpending,

    /// <summary>The candidate acquired a mutation its own gene set excludes.</summary>
    ViolatedExclusions,

    /// <summary>Mycovariant drafting threw or returned a choice that was not offered.</summary>
    DraftFailed,

    /// <summary>The same seed produced a different draft pick.</summary>
    NondeterministicDraft
}

public sealed record CandidateCharacterizationFinding(
    string CandidateId,
    string DisplayName,
    CandidateCharacterizationFailure Failure,
    string Detail);

public sealed class CandidateCharacterizationReport
{
    public required IReadOnlyList<CandidateGenome> Passed { get; init; }

    public required IReadOnlyList<CandidateCharacterizationFinding> Findings { get; init; }

    /// <summary>Levels purchased per candidate, keyed by candidate ID then mutation ID. Diagnostic only.</summary>
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> ObservedBuilds { get; init; }
}

/// <summary>
/// Settings for the scripted spending script. These are deliberately not a search dimension: the
/// gate asks whether a candidate is runnable at all, not whether it is good.
/// </summary>
public sealed class CandidateCharacterizationSettings
{
    public int Rounds { get; init; } = 12;

    public int MutationPointsPerRound { get; init; } = 8;

    public int Seed { get; init; } = 6_020_301;

    public int BoardWidth { get; init; } = 12;

    public int BoardHeight { get; init; } = 12;
}

/// <summary>
/// The zero-game half of staged evaluation. Static validation asks whether a candidate is
/// well-formed; this asks whether it actually plays - it constructs, spends without throwing,
/// buys something, honors its own exclusions, drafts a mycovariant, and does all of that
/// identically under a repeated seed.
///
/// It exists because a gene set can be structurally perfect and behaviorally inert. A max-tier
/// sweep, for instance, can cap a candidate below every goal it was given. Catching that here
/// costs no games; catching it at smoke costs a batch, and catching it at comparison costs fifty.
///
/// Determinism is checked rather than assumed: paired inference compares a candidate against its
/// control under matched seeds, so a candidate whose own decisions drift under a fixed seed would
/// silently corrupt every downstream interval.
/// </summary>
public static class CandidateCharacterizationGate
{
    /// <param name="materializer">
    /// How a genome becomes a runnable strategy. Defaults to normal materialization; it is a
    /// parameter because every failure this gate detects is, by design, unreachable through the
    /// real path, so the only way to prove the gate can fail is to hand it a strategy that does.
    /// </param>
    public static CandidateCharacterizationReport Run(
        IEnumerable<CandidateGenome> candidates,
        CandidateCharacterizationSettings? settings = null,
        Func<CandidateGenome, IMutationSpendingStrategy>? materializer = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var effectiveSettings = settings ?? new CandidateCharacterizationSettings();
        var effectiveMaterializer = materializer ?? (genome => CandidateGenomeFactory.Materialize(genome));
        var allMutations = MutationRegistry.GetAll().ToList();
        var draftChoices = BuildDraftChoices();

        var passed = new List<CandidateGenome>();
        var findings = new List<CandidateCharacterizationFinding>();
        var observedBuilds = new Dictionary<string, IReadOnlyDictionary<int, int>>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            var finding = Characterize(candidate, effectiveSettings, effectiveMaterializer, allMutations, draftChoices, out var build);
            if (finding != null)
            {
                findings.Add(finding);
                continue;
            }

            observedBuilds[candidate.CandidateId] = build!;
            passed.Add(candidate);
        }

        return new CandidateCharacterizationReport
        {
            Passed = passed,
            Findings = findings,
            ObservedBuilds = observedBuilds
        };
    }

    private static CandidateCharacterizationFinding? Characterize(
        CandidateGenome candidate,
        CandidateCharacterizationSettings settings,
        Func<CandidateGenome, IMutationSpendingStrategy> materializer,
        IReadOnlyList<Mutation> allMutations,
        IReadOnlyList<Mycovariant> draftChoices,
        out IReadOnlyDictionary<int, int>? build)
    {
        build = null;

        IMutationSpendingStrategy strategy;
        try
        {
            strategy = materializer(candidate);
        }
        catch (Exception exception)
        {
            return Finding(candidate, CandidateCharacterizationFailure.ConstructionFailed, exception.Message);
        }

        Dictionary<int, int> firstBuild;
        Dictionary<int, int> secondBuild;
        try
        {
            firstBuild = RunSpendingScript(strategy, settings, allMutations);
            secondBuild = RunSpendingScript(strategy, settings, allMutations);
        }
        catch (Exception exception)
        {
            return Finding(candidate, CandidateCharacterizationFailure.SpendingThrew, exception.Message);
        }

        if (!BuildsMatch(firstBuild, secondBuild))
        {
            return Finding(
                candidate,
                CandidateCharacterizationFailure.NondeterministicSpending,
                $"Seed {settings.Seed} produced {DescribeBuild(firstBuild)} then {DescribeBuild(secondBuild)}.");
        }

        if (firstBuild.Count == 0)
        {
            return Finding(
                candidate,
                CandidateCharacterizationFailure.NeverSpent,
                $"Bought nothing across {settings.Rounds} rounds of {settings.MutationPointsPerRound} points.");
        }

        var violated = firstBuild.Keys.Where(candidate.Genes.ExcludedMutationIds.Contains).OrderBy(id => id).ToList();
        if (violated.Count > 0)
        {
            return Finding(
                candidate,
                CandidateCharacterizationFailure.ViolatedExclusions,
                $"Acquired excluded mutation IDs {string.Join(", ", violated)}.");
        }

        Mycovariant firstPick;
        Mycovariant secondPick;
        try
        {
            firstPick = DraftOnce(strategy, settings, draftChoices);
            secondPick = DraftOnce(strategy, settings, draftChoices);
        }
        catch (Exception exception)
        {
            return Finding(candidate, CandidateCharacterizationFailure.DraftFailed, exception.Message);
        }

        if (!draftChoices.Any(choice => choice.Id == firstPick.Id))
        {
            return Finding(
                candidate,
                CandidateCharacterizationFailure.DraftFailed,
                $"Selected mycovariant {firstPick.Id}, which was not among the offered choices.");
        }

        if (firstPick.Id != secondPick.Id)
        {
            return Finding(
                candidate,
                CandidateCharacterizationFailure.NondeterministicDraft,
                $"Seed {settings.Seed} drafted mycovariant {firstPick.Id} then {secondPick.Id}.");
        }

        build = firstBuild;
        return null;
    }

    /// <summary>
    /// Runs the same scripted spend against any strategy and returns the levels it bought.
    ///
    /// Exposed because ranking needs a parent's build on identical terms to a candidate's: a
    /// behavioral comparison is only meaningful if both sides were observed under the same script.
    /// </summary>
    public static IReadOnlyDictionary<int, int> ObserveBuild(
        IMutationSpendingStrategy strategy,
        CandidateCharacterizationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        return RunSpendingScript(
            strategy,
            settings ?? new CandidateCharacterizationSettings(),
            MutationRegistry.GetAll().ToList());
    }

    /// <summary>
    /// Grants a fixed budget each round and lets the strategy spend it, exactly as a real mutation
    /// phase would, without running growth, decay, or any other board simulation.
    /// </summary>
    private static Dictionary<int, int> RunSpendingScript(
        IMutationSpendingStrategy strategy,
        CandidateCharacterizationSettings settings,
        IReadOnlyList<Mutation> allMutations)
    {
        var board = new GameBoard(settings.BoardWidth, settings.BoardHeight, playerCount: 1);
        var player = new Player(0, "Characterization Candidate", PlayerTypeEnum.AI);
        board.Players.Add(player);
        // The real tracking observer, so the analytics path a candidate exercises here is
        // the same one a scored game would use rather than a stub that cannot fail the same way.
        var observer = new SimulationTrackingContext();

        for (var round = 1; round <= settings.Rounds; round++)
        {
            board.RestoreRoundState(round, currentGrowthCycle: 0, necrophyticBloomActivated: false, pendingHypervariationDraftPlayerIds: null);
            player.MutationPoints = settings.MutationPointsPerRound;
            // A fresh Random per round keyed off the round keeps the script reproducible without
            // depending on how many draws the strategy happens to take in earlier rounds.
            strategy.SpendMutationPoints(player, allMutations.ToList(), board, new Random(settings.Seed + round), observer);
        }

        return player.PlayerMutations
            .Where(entry => entry.Value.CurrentLevel > 0)
            .ToDictionary(entry => entry.Key, entry => entry.Value.CurrentLevel);
    }

    private static Mycovariant DraftOnce(
        IMutationSpendingStrategy strategy,
        CandidateCharacterizationSettings settings,
        IReadOnlyList<Mycovariant> draftChoices)
    {
        var board = new GameBoard(settings.BoardWidth, settings.BoardHeight, playerCount: 1);
        var player = new Player(0, "Characterization Candidate", PlayerTypeEnum.AI);
        board.Players.Add(player);

        return strategy.SelectMycovariantFromChoices(
            player,
            draftChoices.ToList(),
            board,
            new Random(settings.Seed));
    }

    /// <summary>
    /// A fixed, ordered slice of the real mycovariant pool. Using real entries means the draft
    /// path exercises real scoring rather than a stub that cannot fail the way production does.
    /// </summary>
    private static IReadOnlyList<Mycovariant> BuildDraftChoices()
        => MycovariantRepository.All.OrderBy(mycovariant => mycovariant.Id).Take(6).ToList();

    private static bool BuildsMatch(Dictionary<int, int> first, Dictionary<int, int> second)
        => first.Count == second.Count
            && first.All(entry => second.TryGetValue(entry.Key, out var level) && level == entry.Value);

    private static string DescribeBuild(Dictionary<int, int> build)
    {
        if (build.Count == 0) return "nothing";
        return string.Join(
            ",",
            build.OrderBy(entry => entry.Key)
                .Select(entry => entry.Key.ToString(CultureInfo.InvariantCulture) + ":" + entry.Value.ToString(CultureInfo.InvariantCulture)));
    }

    private static CandidateCharacterizationFinding Finding(
        CandidateGenome candidate,
        CandidateCharacterizationFailure failure,
        string detail)
        => new(candidate.CandidateId, candidate.DisplayName, failure, detail);
}
