using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FungusToast.Core.AI;
using FungusToast.Simulation.Models;

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

    public static string ForOutcomes(SimulationBatchResult batchResult)
    {
        var builder = new StringBuilder();
        AppendCanonicalValue(builder, batchResult, depth: 0);
        return ForText(builder.ToString());
    }

    public static string ForExecution(SimulationRunMetadata metadata, ResolvedCodeIdentity code)
    {
        var conditionFingerprint = ForCondition(metadata.Condition);
        return ForText(string.Join("\n", new[]
        {
            conditionFingerprint,
            code.CoreAssemblySha256,
            code.SimulationAssemblySha256,
            string.Join("|", metadata.SelectedStrategies.OrderBy(strategy => strategy.LineupOrder).Select(strategy =>
                $"{strategy.StrategyId}:{strategy.DefinitionFingerprint}")),
            string.Join(",", metadata.GameSeedSchedule)
        }));
    }

    private static void AppendCanonicalValue(StringBuilder builder, object? value, int depth)
    {
        if (depth > 20) throw new InvalidOperationException("Outcome fingerprint object graph exceeded maximum depth.");
        if (value == null) { builder.Append("null"); return; }

        switch (value)
        {
            case string text:
                builder.Append(JsonSerializer.Serialize(text));
                return;
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                return;
            case Enum enumValue:
                builder.Append(enumValue.GetType().FullName).Append(':').Append(enumValue);
                return;
            case DateTime dateTime:
                builder.Append(dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                return;
            case IFormattable formattable when value.GetType().IsPrimitive || value is decimal:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
            case IDictionary dictionary:
                builder.Append('{');
                var keys = dictionary.Keys.Cast<object?>()
                    .OrderBy(key => key?.ToString(), StringComparer.Ordinal)
                    .ToList();
                foreach (var key in keys)
                {
                    AppendCanonicalValue(builder, key?.ToString(), depth + 1);
                    builder.Append(':');
                    AppendCanonicalValue(builder, dictionary[key!], depth + 1);
                    builder.Append(';');
                }
                builder.Append('}');
                return;
            case IEnumerable enumerable:
                builder.Append('[');
                foreach (var item in enumerable)
                {
                    AppendCanonicalValue(builder, item, depth + 1);
                    builder.Append(';');
                }
                builder.Append(']');
                return;
        }

        builder.Append(value.GetType().FullName).Append('{');
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                     .Where(property => property.Name is not "Strategy" and not "TrackingContext" and not "RuntimeMilliseconds")
                     .OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            builder.Append(property.Name).Append('=');
            AppendCanonicalValue(builder, property.GetValue(value), depth + 1);
            builder.Append(';');
        }
        foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                     .OrderBy(field => field.Name, StringComparer.Ordinal))
        {
            builder.Append(field.Name).Append('=');
            AppendCanonicalValue(builder, field.GetValue(value), depth + 1);
            builder.Append(';');
        }
        builder.Append('}');
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
