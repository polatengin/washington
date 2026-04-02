using System.Text.Json;
using Washington.Mappers;
using Washington.Models;

namespace Washington.Services;

public class CostEstimationService
{
    private static readonly System.Text.RegularExpressions.Regex JsonNumberPattern =
        new(@"^-?\d+(\.\d+)?([eE][+-]?\d+)?$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly BicepCompiler _compiler;
    private readonly ResourceExtractor _extractor;
    private readonly CostAggregator _aggregator;

    public CostEstimationService(BicepCompiler compiler, ResourceExtractor extractor, CostAggregator aggregator)
    {
        _compiler = compiler;
        _extractor = extractor;
        _aggregator = aggregator;
    }

    public async Task<CostReport> EstimateFromBicepAsync(
        string bicepFilePath, string? paramsFilePath = null,
        Dictionary<string, string>? paramOverrides = null)
    {
        var armJson = await _compiler.CompileBicepToArm(bicepFilePath);
        var effectiveParameterValues = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(paramsFilePath) && File.Exists(paramsFilePath))
        {
            var armParamsJson = await _compiler.CompileBicepParamsToArm(paramsFilePath);
            MergeParameterValues(effectiveParameterValues, ExtractParameterValuesFromArmParams(armParamsJson));
        }

        MergeParameterValues(effectiveParameterValues, ParseCommandLineOverrides(paramOverrides));

        return await EstimateFromArmJsonAsync(armJson, effectiveParameterValues);
    }

    public async Task<CostReport> EstimateFromArmJsonAsync(
        string armJson,
        Dictionary<string, JsonElement>? parameterValues = null)
    {
        var resources = _extractor.Extract(armJson, parameterValues);
        return await _aggregator.GenerateReportAsync(resources);
    }

    private static Dictionary<string, JsonElement> ExtractParameterValuesFromArmParams(string armParamsJson)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(armParamsJson);
        if (!doc.RootElement.TryGetProperty("parameters", out var parameters) ||
            parameters.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var parameter in parameters.EnumerateObject())
        {
            if (parameter.Value.TryGetProperty("value", out var value))
            {
                result[parameter.Name] = value.Clone();
            }
        }

        return result;
    }

    private static Dictionary<string, JsonElement> ParseCommandLineOverrides(Dictionary<string, string>? paramOverrides)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (paramOverrides == null)
        {
            return result;
        }

        foreach (var overrideEntry in paramOverrides)
        {
            result[overrideEntry.Key] = ParseOverrideValue(overrideEntry.Value);
        }

        return result;
    }

    private static void MergeParameterValues(
        Dictionary<string, JsonElement> target,
        Dictionary<string, JsonElement> source)
    {
        foreach (var entry in source)
        {
            target[entry.Key] = entry.Value.Clone();
        }
    }

    private static JsonElement ParseOverrideValue(string rawValue)
    {
        var trimmed = rawValue.Trim();

        if (trimmed.StartsWith('{') || trimmed.StartsWith('[') || trimmed.StartsWith('"') ||
            trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) ||
            JsonNumberPattern.IsMatch(trimmed))
        {
            try
            {
                return JsonDocument.Parse(trimmed).RootElement.Clone();
            }
            catch (JsonException)
            {
            }
        }

        return JsonDocument.Parse($"\"{EscapeJsonString(rawValue)}\"").RootElement.Clone();
    }

    private static string EscapeJsonString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
