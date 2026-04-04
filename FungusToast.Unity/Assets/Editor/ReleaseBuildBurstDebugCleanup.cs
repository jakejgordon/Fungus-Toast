using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class ReleaseBuildBurstDebugCleanup : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report == null)
        {
            return;
        }

        if ((report.summary.options & BuildOptions.Development) != 0)
        {
            return;
        }

        string outputPath = report.summary.outputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        string normalizedOutputPath = outputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string outputDirectory = Directory.Exists(normalizedOutputPath)
            ? normalizedOutputPath
            : Path.GetDirectoryName(normalizedOutputPath);

        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            return;
        }

        string[] burstDebugDirectories = Directory.GetDirectories(
            outputDirectory,
            "*_BurstDebugInformation_DoNotShip",
            SearchOption.TopDirectoryOnly);

        foreach (string burstDebugDirectory in burstDebugDirectories)
        {
            try
            {
                Directory.Delete(burstDebugDirectory, recursive: true);
                Debug.Log($"Deleted Burst debug information directory for release build: {burstDebugDirectory}");
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Failed to delete Burst debug information directory '{burstDebugDirectory}': {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning($"Failed to delete Burst debug information directory '{burstDebugDirectory}': {exception.Message}");
            }
        }
    }
}