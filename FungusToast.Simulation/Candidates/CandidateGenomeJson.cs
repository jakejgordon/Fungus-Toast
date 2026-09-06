using System.Text.Json;
using System.Text.Json.Serialization;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Serialization for candidate genomes. Options match ExperimentManifestJson so the two input
/// contracts read and fail the same way: camelCase names, case-sensitive matching, and unmapped
/// members rejected rather than silently dropped.
/// </summary>
public static class CandidateGenomeJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static CandidateGenome Deserialize(string json)
    {
        var genome = JsonSerializer.Deserialize<CandidateGenome>(json, SerializerOptions);
        return genome ?? throw new JsonException("Candidate genome must contain a JSON object.");
    }

    public static string Serialize(CandidateGenome genome) => JsonSerializer.Serialize(genome, SerializerOptions);

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
