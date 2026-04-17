using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class BuildVersionSync : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string VersionFileName = "version.txt";

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        string releaseVersion = ReadProjectVersion();
        if (string.Equals(PlayerSettings.bundleVersion, releaseVersion, StringComparison.Ordinal))
        {
            return;
        }

        PlayerSettings.bundleVersion = releaseVersion;
        Debug.Log($"Synchronized PlayerSettings.bundleVersion to {releaseVersion} from {GetProjectVersionFilePath()}.");
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report == null)
        {
            return;
        }

        string outputDirectory = ResolveOutputDirectory(report.summary.outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            return;
        }

        string sourceVersionFilePath = GetProjectVersionFilePath();
        string destinationVersionFilePath = Path.Combine(outputDirectory, VersionFileName);

        File.Copy(sourceVersionFilePath, destinationVersionFilePath, overwrite: true);
        Debug.Log($"Copied {VersionFileName} to build output: {destinationVersionFilePath}");
    }

    private static string ReadProjectVersion()
    {
        string versionFilePath = GetProjectVersionFilePath();
        if (!File.Exists(versionFilePath))
        {
            throw new InvalidOperationException($"Required version file was not found at '{versionFilePath}'.");
        }

        string rawContents = File.ReadAllText(versionFilePath).Trim();
        if (string.IsNullOrWhiteSpace(rawContents))
        {
            throw new InvalidOperationException($"Version file '{versionFilePath}' is empty.");
        }

        return rawContents;
    }

    private static string GetProjectVersionFilePath()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath)
            ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectRoot, VersionFileName);
    }

    private static string ResolveOutputDirectory(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return string.Empty;
        }

        string normalizedOutputPath = outputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (Directory.Exists(normalizedOutputPath))
        {
            return normalizedOutputPath;
        }

        return Path.GetDirectoryName(normalizedOutputPath) ?? string.Empty;
    }
}