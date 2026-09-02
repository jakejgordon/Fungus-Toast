using System.Text.Json;
using System.Text.Json.Serialization;

namespace FungusToast.Simulation.Experiments;

public static class ResolvedExperimentManifestJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static ResolvedExperimentManifest Deserialize(string json) =>
        JsonSerializer.Deserialize<ResolvedExperimentManifest>(json, SerializerOptions)
        ?? throw new JsonException("Resolved experiment manifest must contain a JSON object.");

    public static string Serialize(ResolvedExperimentManifest manifest) =>
        JsonSerializer.Serialize(manifest, SerializerOptions);

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
