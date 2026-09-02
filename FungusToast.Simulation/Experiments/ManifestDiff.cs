using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FungusToast.Simulation.Experiments;

public sealed class ManifestDifference
{
    public required string Path { get; init; }
    public required string ControlValue { get; init; }
    public required string TreatmentValue { get; init; }
    public required bool IsAllowed { get; init; }
}

public sealed class ManifestComparisonResult
{
    public required IReadOnlyList<ManifestDifference> Differences { get; init; }
    public required IReadOnlyList<string> UnusedAllowedPaths { get; init; }

    public IReadOnlyList<ManifestDifference> UnexpectedDifferences =>
        Differences.Where(difference => !difference.IsAllowed).ToList();

    public bool IsClean => UnexpectedDifferences.Count == 0 && UnusedAllowedPaths.Count == 0;
}

public static class ManifestDiff
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static ManifestComparisonResult Compare<T>(
        T control,
        T treatment,
        IEnumerable<string>? allowedDifferencePaths = null)
    {
        var allowed = (allowedDifferencePaths ?? Array.Empty<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        var rawDifferences = new List<(string Path, string Control, string Treatment)>();
        CompareNodes(
            JsonSerializer.SerializeToNode(control, SerializerOptions),
            JsonSerializer.SerializeToNode(treatment, SerializerOptions),
            path: string.Empty,
            rawDifferences);

        var usedAllowedPaths = new HashSet<string>(StringComparer.Ordinal);
        var differences = rawDifferences.Select(difference =>
        {
            var matchingAllowedPath = allowed.FirstOrDefault(path => IsPathCovered(difference.Path, path));
            if (matchingAllowedPath != null) usedAllowedPaths.Add(matchingAllowedPath);
            return new ManifestDifference
            {
                Path = difference.Path,
                ControlValue = difference.Control,
                TreatmentValue = difference.Treatment,
                IsAllowed = matchingAllowedPath != null
            };
        }).ToList();

        return new ManifestComparisonResult
        {
            Differences = differences,
            UnusedAllowedPaths = allowed.Where(path => !usedAllowedPaths.Contains(path)).ToList()
        };
    }

    private static void CompareNodes(
        JsonNode? control,
        JsonNode? treatment,
        string path,
        ICollection<(string Path, string Control, string Treatment)> differences)
    {
        if (JsonNode.DeepEquals(control, treatment)) return;

        if (control is JsonObject controlObject && treatment is JsonObject treatmentObject)
        {
            foreach (var propertyName in controlObject.Select(property => property.Key)
                         .Concat(treatmentObject.Select(property => property.Key))
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(name => name, StringComparer.Ordinal))
            {
                CompareNodes(
                    controlObject[propertyName],
                    treatmentObject[propertyName],
                    AppendProperty(path, propertyName),
                    differences);
            }
            return;
        }

        if (control is JsonArray controlArray && treatment is JsonArray treatmentArray)
        {
            if (controlArray.Count != treatmentArray.Count)
                differences.Add(($"{path}.count", controlArray.Count.ToString(), treatmentArray.Count.ToString()));
            for (var index = 0; index < Math.Min(controlArray.Count, treatmentArray.Count); index++)
                CompareNodes(controlArray[index], treatmentArray[index], $"{path}[{index}]", differences);
            return;
        }

        differences.Add((
            string.IsNullOrEmpty(path) ? "$" : path,
            FormatValue(control),
            FormatValue(treatment)));
    }

    private static string AppendProperty(string path, string propertyName) =>
        string.IsNullOrEmpty(path) ? propertyName : $"{path}.{propertyName}";

    private static bool IsPathCovered(string differencePath, string allowedPath) =>
        string.Equals(differencePath, allowedPath, StringComparison.Ordinal) ||
        differencePath.StartsWith($"{allowedPath}.", StringComparison.Ordinal) ||
        differencePath.StartsWith($"{allowedPath}[", StringComparison.Ordinal);

    private static string FormatValue(JsonNode? node) => node?.ToJsonString(SerializerOptions) ?? "<missing-or-null>";

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
