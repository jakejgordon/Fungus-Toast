using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ReleaseBuildAutomation
{
    private const string OutputPathArgumentName = "-releaseOutputPath";
    private const string VersionArgumentName = "-releaseVersion";
    private const string ExecutableName = "FungusToast.exe";

    public static void BuildWindowsRelease()
    {
        string[] args = Environment.GetCommandLineArgs();
        string outputDirectory = GetRequiredArgument(args, OutputPathArgumentName);
        string releaseVersion = GetRequiredArgument(args, VersionArgumentName);

        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        if (enabledScenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes were found in EditorBuildSettings.");
        }

        string fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        string executablePath = Path.Combine(fullOutputDirectory, ExecutableName);
        PlayerSettings.bundleVersion = releaseVersion;

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = enabledScenes,
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Windows release build failed with result {report.summary.result}.");
        }

        string versionFilePath = Path.Combine(fullOutputDirectory, "version.txt");
        File.WriteAllText(versionFilePath, releaseVersion + Environment.NewLine);
        AssetDatabase.SaveAssets();

        Debug.Log($"Windows release build completed successfully at '{fullOutputDirectory}'.");
    }

    private static string GetRequiredArgument(string[] args, string argumentName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = args[i + 1];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            break;
        }

        throw new InvalidOperationException($"Missing required command-line argument '{argumentName}'.");
    }
}