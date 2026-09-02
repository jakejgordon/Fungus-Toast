using System.Reflection;
using FungusToast.Core.AI;

namespace FungusToast.Simulation.Experiments;

public static class CodeIdentityResolver
{
    public static ResolvedCodeIdentity Resolve()
    {
        var simulationAssembly = Assembly.GetExecutingAssembly();
        var coreAssembly = typeof(AIRoster).Assembly;
        return new ResolvedCodeIdentity
        {
            Commit = ResolveCommit(),
            SimulationAssemblyVersion = simulationAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            SimulationAssemblySha256 = ExperimentFingerprint.ForAssembly(simulationAssembly),
            CoreAssemblySha256 = ExperimentFingerprint.ForAssembly(coreAssembly)
        };
    }

    private static string ResolveCommit()
    {
        var environmentCommit = Environment.GetEnvironmentVariable("FUNGUSTOAST_COMMIT_SHA");
        if (!string.IsNullOrWhiteSpace(environmentCommit)) return environmentCommit.Trim();

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var commit = TryResolveCommitFromAncestors(start);
            if (commit != null) return commit;
        }

        return "unknown";
    }

    private static string? TryResolveCommitFromAncestors(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory != null)
        {
            var dotGitPath = Path.Combine(directory.FullName, ".git");
            var gitDirectory = ResolveGitDirectory(dotGitPath, directory.FullName);
            if (gitDirectory != null)
            {
                var commit = ReadHeadCommit(gitDirectory);
                if (commit != null) return commit;
            }
            directory = directory.Parent;
        }
        return null;
    }

    private static string? ResolveGitDirectory(string dotGitPath, string repositoryRoot)
    {
        if (Directory.Exists(dotGitPath)) return dotGitPath;
        if (!File.Exists(dotGitPath)) return null;
        var content = File.ReadAllText(dotGitPath).Trim();
        const string prefix = "gitdir:";
        if (!content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var path = content[prefix.Length..].Trim();
        return Path.GetFullPath(path, repositoryRoot);
    }

    private static string? ReadHeadCommit(string gitDirectory)
    {
        try
        {
            var head = File.ReadAllText(Path.Combine(gitDirectory, "HEAD")).Trim();
            if (!head.StartsWith("ref:", StringComparison.Ordinal)) return NormalizeCommit(head);

            var reference = head[4..].Trim();
            var looseReferencePath = Path.Combine(gitDirectory, reference.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(looseReferencePath)) return NormalizeCommit(File.ReadAllText(looseReferencePath).Trim());

            var packedReferencesPath = Path.Combine(gitDirectory, "packed-refs");
            if (!File.Exists(packedReferencesPath)) return null;
            var match = File.ReadLines(packedReferencesPath)
                .FirstOrDefault(line => line.EndsWith($" {reference}", StringComparison.Ordinal));
            return match == null ? null : NormalizeCommit(match.Split(' ', 2)[0]);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? NormalizeCommit(string value) =>
        value.Length >= 7 && value.All(Uri.IsHexDigit) ? value.ToLowerInvariant() : null;
}
