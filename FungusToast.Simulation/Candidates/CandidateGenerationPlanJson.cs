using System.Text.Json;
using System.Text.Json.Serialization;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Serialization for candidate generation plans, using the same options as
/// CandidateGenomeJson and ExperimentManifestJson so every input contract in the
/// pipeline reads and fails the same way.
/// </summary>
public static class CandidateGenerationPlanJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static CandidateGenerationPlan Deserialize(string json)
    {
        var plan = JsonSerializer.Deserialize<CandidateGenerationPlan>(json, SerializerOptions);
        return plan ?? throw new JsonException("Candidate generation plan must contain a JSON object.");
    }

    public static string Serialize(CandidateGenerationPlan plan) => JsonSerializer.Serialize(plan, SerializerOptions);

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
