using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FungusToast.Core.AI;

namespace FungusToast.Simulation.Experiments;

public static class ExperimentFingerprint
{
    private static readonly JsonSerializerOptions CanonicalJsonOptions = CreateCanonicalJsonOptions();

    public static string ForText(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public static string ForFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public static string ForCondition(ExperimentCondition condition) =>
        ForText(JsonSerializer.Serialize(condition, CanonicalJsonOptions));

    public static string ForBoard(ExperimentBoard board)
    {
        var canonical = string.Join("\n", new[]
        {
            board.GeometryId,
            board.Width.ToString(System.Globalization.CultureInfo.InvariantCulture),
            board.Height.ToString(System.Globalization.CultureInfo.InvariantCulture),
            string.Join(",", board.BlockedTileIds.OrderBy(id => id))
        });
        return ForText(canonical);
    }

    public static string ForStrategy(string coreAssemblySha256, StrategySetEnum strategySet, string strategyName) =>
        ForText($"{coreAssemblySha256}\n{strategySet}\n{strategyName}");

    public static string ForAssembly(Assembly assembly)
    {
        var location = assembly.Location;
        return string.IsNullOrWhiteSpace(location) || !File.Exists(location)
            ? "unavailable"
            : ForFile(location);
    }

    private static JsonSerializerOptions CreateCanonicalJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
