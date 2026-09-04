namespace FungusToast.Simulation.Experiments;

public static class ResolvedExperimentComparator
{
    public static ManifestComparisonResult Compare(
        ResolvedExperimentManifest control,
        ResolvedExperimentManifest treatment,
        IEnumerable<string>? allowedDifferencePaths = null)
    {
        return ManifestDiff.Compare(
            CreateCausalSnapshot(control),
            CreateCausalSnapshot(treatment),
            allowedDifferencePaths);
    }

    public static ManifestComparisonResult CompareFiles(
        string controlManifestPath,
        string treatmentManifestPath,
        IEnumerable<string>? allowedDifferencePaths = null)
    {
        var control = ResolvedExperimentManifestJson.Deserialize(File.ReadAllText(controlManifestPath));
        var treatment = ResolvedExperimentManifestJson.Deserialize(File.ReadAllText(treatmentManifestPath));
        return Compare(control, treatment, allowedDifferencePaths);
    }

    private static object CreateCausalSnapshot(ResolvedExperimentManifest manifest) => new
    {
        manifest.InputSchemaVersion,
        manifest.AiCorpusVersion,
        Code = new
        {
            manifest.Code.CoreAssemblySha256,
            manifest.Code.SimulationAssemblySha256
        },
        manifest.Condition,
        SelectedLineup = manifest.SelectedLineup
            .OrderBy(strategy => strategy.LineupOrder)
            .Select(strategy => new
            {
                strategy.LineupOrder,
                strategy.StrategyName,
                strategy.DefinitionSha256
            })
            .ToList(),
        manifest.Randomness,
        manifest.Sampling.GamesRequested
    };
}
