using System.Text.Json;
using System.Text.Json.Serialization;

namespace FungusToast.Simulation.Experiments;

public static class ExperimentManifestJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static ExperimentManifest Deserialize(string json)
    {
        var manifest = JsonSerializer.Deserialize<ExperimentManifest>(json, SerializerOptions);
        return manifest ?? throw new JsonException("Experiment manifest must contain a JSON object.");
    }

    public static string Serialize(ExperimentManifest manifest) => JsonSerializer.Serialize(manifest, SerializerOptions);

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
