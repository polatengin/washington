using System.Text.Json;
using System.Text.RegularExpressions;
using Washington.Models;

namespace Washington.Services;

public class ResourceExtractor
{
    private readonly string _defaultRegion;

    public ResourceExtractor(string defaultRegion = "eastus")
    {
        _defaultRegion = string.IsNullOrWhiteSpace(defaultRegion) ? "eastus" : defaultRegion;
    }

    public List<ResourceDescriptor> Extract(
        string armTemplateJson,
        Dictionary<string, JsonElement>? suppliedParameterValues = null)
    {
        var descriptors = new List<ResourceDescriptor>();

        using var doc = JsonDocument.Parse(armTemplateJson);
        var root = doc.RootElement;

        var parameterValues = ExtractParameterDefaults(root);
        MergeParameterValues(parameterValues, suppliedParameterValues);

        if (root.TryGetProperty("resources", out var resources))
        {
            ExtractResources(resources, descriptors, parameterValues);
        }

        return descriptors;
    }

    private static Dictionary<string, JsonElement> ExtractParameterDefaults(JsonElement root)
    {
        var defaults = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Object)
        {
            foreach (var param in parameters.EnumerateObject())
            {
                if (param.Value.TryGetProperty("defaultValue", out var defaultValue))
                {
                    defaults[param.Name] = defaultValue.Clone();
                }
            }
        }
        return defaults;
    }

    private static void MergeParameterValues(
        Dictionary<string, JsonElement> parameterValues,
        Dictionary<string, JsonElement>? suppliedParameterValues)
    {
        if (suppliedParameterValues == null)
        {
            return;
        }

        foreach (var entry in suppliedParameterValues)
        {
            parameterValues[entry.Key] = entry.Value.Clone();
        }
    }

    private void ExtractResources(
        JsonElement resources,
        List<ResourceDescriptor> descriptors,
        Dictionary<string, JsonElement> parameterValues)
    {
        if (resources.ValueKind != JsonValueKind.Array)
            return;

        foreach (var resource in resources.EnumerateArray())
        {
            var type = resource.GetStringProperty("type");
            var apiVersion = resource.GetStringProperty("apiVersion");
            var rawName = resource.GetStringProperty("name");
            var name = ResolveArmStringExpression(rawName, parameterValues) ?? rawName;
            var location = ResolveArmExpression(resource.GetStringProperty("location"), parameterValues);

            if (string.IsNullOrEmpty(type))
                continue;

            // Handle copy loops
            if (resource.TryGetProperty("copy", out var copy))
            {
                var count = GetCopyCount(copy);
                for (int i = 0; i < count; i++)
                {
                    var copyName = name.Replace("[copyIndex()]", i.ToString());
                    descriptors.Add(BuildDescriptor(resource, type, apiVersion, copyName, location, parameterValues));
                }
            }
            else
            {
                // Skip resources with condition explicitly set to false
                if (resource.TryGetProperty("condition", out var condition) &&
                    condition.ValueKind == JsonValueKind.False)
                {
                    continue;
                }

                descriptors.Add(BuildDescriptor(resource, type, apiVersion, name, location, parameterValues));
            }

            // Recurse into nested/child resources
            if (resource.TryGetProperty("resources", out var childResources))
            {
                ExtractResources(childResources, descriptors, parameterValues);
            }
        }
    }

    /// <summary>
    /// Resolves ARM template expressions (e.g. "[parameters('location')]", "[resourceGroup().location]")
    /// to a concrete value. Falls back to DefaultRegion if the expression cannot be resolved.
    /// </summary>
    private string ResolveArmExpression(string value, Dictionary<string, JsonElement> parameterValues)
    {
        if (string.IsNullOrEmpty(value))
            return _defaultRegion;

        // Not an ARM expression - use as-is
        if (!value.StartsWith('[') || !value.EndsWith(']'))
            return value;

        // Try to resolve [parameters('paramName')] by looking up default values
        if (TryResolveParameterReference(value, parameterValues, out var resolvedValue) &&
            TryGetScalarString(resolvedValue, out var resolvedString))
        {
            return resolvedString;
        }

        // Unresolvable expression - fall back to default region
        return _defaultRegion;
    }

    private static bool IsArmExpression(string value) =>
        value.StartsWith('[') && value.EndsWith(']');

    private static ResourceDescriptor BuildDescriptor(
        JsonElement resource, string type, string apiVersion, string name, string location,
        Dictionary<string, JsonElement> parameterValues)
    {
        var sku = new Dictionary<string, JsonElement>();
        if (resource.TryGetProperty("sku", out var skuElement) && skuElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in skuElement.EnumerateObject())
            {
                sku[prop.Name] = ResolveJsonElement(prop.Value, parameterValues);
            }
        }

        var properties = new Dictionary<string, JsonElement>();
        if (resource.TryGetProperty("properties", out var propsElement) && propsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in propsElement.EnumerateObject())
            {
                properties[prop.Name] = ResolveJsonElement(prop.Value, parameterValues);
            }
        }

        // Also store the full "kind" if present
        if (resource.TryGetProperty("kind", out var kindElement))
        {
            properties["_kind"] = ResolveJsonElement(kindElement, parameterValues);
        }

        if (resource.TryGetProperty("reserved", out var reservedElement))
        {
            properties["reserved"] = ResolveJsonElement(reservedElement, parameterValues);
        }

        return new ResourceDescriptor(type, apiVersion, name, location, sku, properties);
    }

    /// <summary>
    /// Recursively resolves ARM parameter expressions in a JsonElement tree.
    /// </summary>
    private static JsonElement ResolveJsonElement(
        JsonElement element,
        Dictionary<string, JsonElement> parameterValues)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var strVal = element.GetString() ?? string.Empty;
                if (IsArmExpression(strVal))
                {
                    if (TryResolveParameterReference(strVal, parameterValues, out var resolved))
                    {
                        return resolved.Clone();
                    }
                }
                return element.Clone();

            case JsonValueKind.Array:
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(ms))
                    {
                        writer.WriteStartArray();
                        foreach (var item in element.EnumerateArray())
                        {
                            ResolveJsonElement(item, parameterValues).WriteTo(writer);
                        }
                        writer.WriteEndArray();
                    }
                    return JsonDocument.Parse(ms.ToArray()).RootElement.Clone();
                }

            case JsonValueKind.Object:
                using (var ms2 = new System.IO.MemoryStream())
                {
                    using (var writer2 = new Utf8JsonWriter(ms2))
                    {
                        writer2.WriteStartObject();
                        foreach (var prop in element.EnumerateObject())
                        {
                            writer2.WritePropertyName(prop.Name);
                            ResolveJsonElement(prop.Value, parameterValues).WriteTo(writer2);
                        }
                        writer2.WriteEndObject();
                    }
                    return JsonDocument.Parse(ms2.ToArray()).RootElement.Clone();
                }

            default:
                return element.Clone();
        }
    }

    /// <summary>
    /// Resolves an ARM expression string to a plain string, or returns null if unresolvable.
    /// </summary>
    private static string? ResolveArmStringExpression(string value, Dictionary<string, JsonElement> parameterValues)
    {
        if (TryResolveParameterReference(value, parameterValues, out var resolved) &&
            TryGetScalarString(resolved, out var resolvedString))
        {
            return resolvedString;
        }
        return null;
    }

    private static bool TryResolveParameterReference(
        string value,
        Dictionary<string, JsonElement> parameterValues,
        out JsonElement resolvedValue)
    {
        var paramMatch = Regex.Match(value, @"^\[parameters\('(\w+)'\)\]$");
        if (paramMatch.Success)
        {
            var paramName = paramMatch.Groups[1].Value;
            if (parameterValues.TryGetValue(paramName, out resolvedValue))
            {
                if (resolvedValue.ValueKind == JsonValueKind.String &&
                    IsArmExpression(resolvedValue.GetString() ?? string.Empty))
                {
                    resolvedValue = default;
                    return false;
                }

                return true;
            }
        }

        resolvedValue = default;
        return false;
    }

    private static bool TryGetScalarString(JsonElement element, out string value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                value = element.GetString() ?? string.Empty;
                return true;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                value = element.ToString();
                return true;
            default:
                value = string.Empty;
                return false;
        }
    }

    private static string EscapeJsonString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static int GetCopyCount(JsonElement copy)
    {
        if (copy.TryGetProperty("count", out var countElement))
        {
            if (countElement.ValueKind == JsonValueKind.Number)
                return countElement.GetInt32();
        }
        return 1;
    }
}

internal static class JsonElementExtensions
{
    public static string GetStringProperty(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString() ?? string.Empty;
        return string.Empty;
    }
}
