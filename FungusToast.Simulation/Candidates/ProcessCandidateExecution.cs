using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// The default, process-backed implementations the driver uses in a real run: one simulator
/// process per arm, then the offline analyzer over the pair.
///
/// Arms run as separate processes deliberately. It is the same isolation the replay contract
/// relies on to prove determinism, and it is what forces a candidate to arrive through the catalog
/// file rather than surviving in memory from whatever generated it - so an unattended run
/// exercises exactly the path a hand-run experiment does.
/// </summary>
public static class ProcessCandidateExecution
{
    /// <summary>Where the simulator writes its per-condition exports, relative to its own base directory.</summary>
    public const string ExportRootFolderName = "SimulationParquet";

    /// <summary>
    /// Runs one arm and returns where its artifact landed. The experiment ID is read back out of
    /// the arguments rather than passed separately, so the artifact path can never disagree with
    /// what was actually run.
    /// </summary>
    public static CandidateArmOutcome RunArm(
        IReadOnlyList<string> arguments,
        string simulationProjectPath,
        string exportRoot,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var experimentId = ReadArgument(arguments, "--experiment-id")
            ?? throw new ArgumentException("Arm arguments must include --experiment-id.", nameof(arguments));
        var artifactPath = Path.Combine(exportRoot, experimentId);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(simulationProjectPath);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--");
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        return Execute(startInfo, timeout, out var stdout, out var stderr, out var elapsedSeconds)
            ? new CandidateArmOutcome(
                Directory.Exists(artifactPath),
                artifactPath,
                elapsedSeconds,
                Directory.Exists(artifactPath) ? "Completed." : $"No artifact at '{artifactPath}'. {Tail(stdout)}")
            : new CandidateArmOutcome(false, artifactPath, elapsedSeconds, Tail(stderr, stdout));
    }

    /// <summary>
    /// Runs the paired analyzer over a control and treatment artifact and reads the emitted
    /// verdict. A stage below comparison produces no verdict file, which is expected rather than a
    /// failure, so the paired summary alone is enough for those stages.
    /// </summary>
    public static CandidateStageAnalysis AnalyzePair(
        string controlArtifactPath,
        string treatmentArtifactPath,
        string analyzerScriptPath,
        bool emitVerdict,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(analyzerScriptPath);
        startInfo.ArgumentList.Add("--run-folder");
        startInfo.ArgumentList.Add(controlArtifactPath);
        startInfo.ArgumentList.Add("--paired-treatment-folder");
        startInfo.ArgumentList.Add(treatmentArtifactPath);
        if (emitVerdict) startInfo.ArgumentList.Add("--emit-verdict");

        if (!Execute(startInfo, timeout, out var stdout, out var stderr, out _))
            return new CandidateStageAnalysis(false, null, null, null, null, Tail(stderr, stdout));

        return emitVerdict
            ? ReadVerdict(controlArtifactPath)
            : ReadPairedSummary(controlArtifactPath);
    }

    private static CandidateStageAnalysis ReadVerdict(string controlArtifactPath)
    {
        var path = Path.Combine(controlArtifactPath, "preregistered_verdict.json");
        if (!File.Exists(path))
            return new CandidateStageAnalysis(false, null, null, null, null, $"No verdict at '{path}'.");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        return new CandidateStageAnalysis(
            true,
            ReadDouble(root, "estimate"),
            ReadDouble(root, "ci95_low"),
            ReadDouble(root, "ci95_high"),
            root.TryGetProperty("verdict", out var verdict) ? verdict.GetString() : null,
            "Verdict read.");
    }

    /// <summary>
    /// Screening stages have no verdict, so the paired difference is taken from the analyzer's
    /// paired comparison output instead.
    /// </summary>
    private static CandidateStageAnalysis ReadPairedSummary(string controlArtifactPath)
    {
        var path = Path.Combine(controlArtifactPath, "paired_comparison.csv");
        if (!File.Exists(path))
            return new CandidateStageAnalysis(true, null, null, null, null, "Paired comparison produced no summary file.");

        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            return new CandidateStageAnalysis(true, null, null, null, null, "Paired comparison summary was empty.");

        var headers = SplitCsv(lines[0]);
        var values = SelectSwappedRow(headers, lines.Skip(1).Select(SplitCsv).ToList());
        if (values == null)
            return new CandidateStageAnalysis(false, null, null, null, null,
                "Paired comparison contained no row where the control and treatment strategies differ.");

        return new CandidateStageAnalysis(
            true,
            ReadColumn(headers, values, "paired_difference_normalized_board_share"),
            ReadColumn(headers, values, "paired_difference_normalized_board_share_ci95_low"),
            ReadColumn(headers, values, "paired_difference_normalized_board_share_ci95_high"),
            null,
            "Paired summary read.");
    }

    private static bool Execute(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        out string stdout,
        out string stderr,
        out double elapsedSeconds)
    {
        var stopwatch = Stopwatch.StartNew();
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{startInfo.FileName}'.");

        // Read both streams before waiting, or a full pipe buffer deadlocks the child.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            stopwatch.Stop();
            stdout = string.Empty;
            stderr = $"Timed out after {timeout.TotalSeconds:0} seconds.";
            elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            return false;
        }

        stopwatch.Stop();
        stdout = stdoutTask.GetAwaiter().GetResult();
        stderr = stderrTask.GetAwaiter().GetResult();
        elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        return process.ExitCode == 0;
    }

    private static string? ReadArgument(IReadOnlyList<string> arguments, string name)
    {
        for (var index = 0; index + 1 < arguments.Count; index++)
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                return arguments[index + 1];
        return null;
    }

    private static double? ReadDouble(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    /// <summary>
    /// A swap produces one paired row per distinct control/treatment identity pair: one for the
    /// slot that changed, and one for the opponent, who is the same strategy in both arms. Only the
    /// changed row measures the candidate, so reading whichever came first would sometimes report
    /// the opponent's difference instead.
    /// </summary>
    public static IReadOnlyList<string>? SelectSwappedRow(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var controlIndex = headers.ToList().IndexOf("strategy_id_control");
        var treatmentIndex = headers.ToList().IndexOf("strategy_id_treatment");
        if (controlIndex < 0 || treatmentIndex < 0) return rows.FirstOrDefault();

        return rows.FirstOrDefault(row =>
            controlIndex < row.Count
            && treatmentIndex < row.Count
            && !string.Equals(row[controlIndex], row[treatmentIndex], StringComparison.Ordinal));
    }

    private static double? ReadColumn(IReadOnlyList<string> headers, IReadOnlyList<string> values, string column)
    {
        var index = headers.ToList().IndexOf(column);
        if (index < 0 || index >= values.Count) return null;
        return double.TryParse(values[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string[] SplitCsv(string line) => line.Split(',');

    private static string Tail(params string[] streams)
    {
        var text = string.Join(Environment.NewLine, streams.Where(stream => !string.IsNullOrWhiteSpace(stream)));
        return text.Length <= 400 ? text : text[^400..];
    }
}
