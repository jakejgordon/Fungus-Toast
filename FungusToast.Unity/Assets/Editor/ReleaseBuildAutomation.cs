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
    private const string MacAppName = "Fungus Toast.app";

    public static void BuildWindowsRelease()
    {
        string[] args = Environment.GetCommandLineArgs();
        string outputDirectory = GetRequiredArgument(args, OutputPathArgumentName);
        string releaseVersion = GetRequiredArgument(args, VersionArgumentName);

        string[] enabledScenes = GetEnabledScenes();
        string fullOutputDirectory = ResolveOutputDirectory(outputDirectory);

        string executablePath = Path.Combine(fullOutputDirectory, ExecutableName);
        BuildRelease(enabledScenes, executablePath, BuildTarget.StandaloneWindows64, releaseVersion, "Windows");

        Debug.Log($"Windows release build completed successfully at '{fullOutputDirectory}'.");
    }

    public static void BuildMacOSRelease()
    {
        string[] args = Environment.GetCommandLineArgs();
        string outputDirectory = GetRequiredArgument(args, OutputPathArgumentName);
        string releaseVersion = GetRequiredArgument(args, VersionArgumentName);

        string[] enabledScenes = GetEnabledScenes();
        string fullOutputDirectory = ResolveOutputDirectory(outputDirectory);

        string appPath = Path.Combine(fullOutputDirectory, MacAppName);
        BuildRelease(enabledScenes, appPath, BuildTarget.StandaloneOSX, releaseVersion, "macOS");

        Debug.Log($"macOS release build completed successfully at '{fullOutputDirectory}'.");
    }

    private static void BuildRelease(
        string[] enabledScenes,
        string outputPath,
        BuildTarget target,
        string releaseVersion,
        string platformName)
    {
        string fullOutputDirectory = Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException($"Could not determine output directory for '{outputPath}'.");

        Directory.CreateDirectory(fullOutputDirectory);
        PlayerSettings.bundleVersion = releaseVersion;

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = enabledScenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"{platformName} release build failed with result {report.summary.result}.");
        }

        string versionFilePath = Path.Combine(fullOutputDirectory, "version.txt");
        File.WriteAllText(versionFilePath, releaseVersion + Environment.NewLine);
        AssetDatabase.SaveAssets();
    }

    private static string[] GetEnabledScenes()
    {
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        if (enabledScenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes were found in EditorBuildSettings.");
        }

        return enabledScenes;
    }

    private static string ResolveOutputDirectory(string outputDirectory)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath)
            ?? Directory.GetCurrentDirectory();
        string combinedPath = Path.IsPathRooted(outputDirectory)
            ? outputDirectory
            : Path.Combine(projectRoot, outputDirectory);
        string fullOutputDirectory = Path.GetFullPath(combinedPath);

        Directory.CreateDirectory(fullOutputDirectory);
        return fullOutputDirectory;
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