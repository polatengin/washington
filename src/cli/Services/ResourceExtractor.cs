using System.Text.Json;
using System.Text.RegularExpressions;
using Washington.Models;

namespace Washington.Services;

public class ResourceExtractor
{
    private const string DefaultRegion = "eastus";

    public List<ResourceDescriptor> Extract(string armTemplateJson)
    {
        var descriptors = new List<ResourceDescriptor>();

        using var doc = JsonDocument.Parse(armTemplateJson);
        var root = doc.RootElement;

        var paramDefaults = ExtractParameterDefaults(root);

        if (root.TryGetProperty("resources", out var resources))
        {
            ExtractResources(resources, descriptors, paramDefaults);
        }

        return descriptors;
    }

    private static Dictionary<string, string> ExtractParameterDefaults(JsonElement root)
    {
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Object)
        {
            foreach (var param in parameters.EnumerateObject())
            {
                if (param.Value.TryGetProperty("defaultValue", out var defaultValue) &&
                    defaultValue.ValueKind == JsonValueKind.String)
                {
                    defaults[param.Name] = defaultValue.GetString() ?? string.Empty;
                }
            }
        }
        return defaults;
    }

    private void ExtractResources(JsonElement resources, List<ResourceDescriptor> descriptors, Dictionary<string, string> paramDefaults)
    {
        if (resources.ValueKind != JsonValueKind.Array)
            return;

        foreach (var resource in resources.EnumerateArray())
        {
            var type = resource.GetStringProperty("type");
            var apiVersion = resource.GetStringProperty("apiVersion");
            var name = resource.GetStringProperty("name");
            var location = ResolveArmExpression(resource.GetStringProperty("location"), paramDefaults);

            if (string.IsNullOrEmpty(type))
                continue;

            // Handle copy loops
            if (resource.TryGetProperty("copy", out var copy))
            {
                var count = GetCopyCount(copy);
                for (int i = 0; i < count; i++)
                {
                    var copyName = name.Replace("[copyIndex()]", i.ToString());
                    descriptors.Add(BuildDescriptor(resource, type, apiVersion, copyName, location, paramDefaults));
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

                descriptors.Add(BuildDescriptor(resource, type, apiVersion, name, location, paramDefaults));
            }

            // Recurse into nested/child resources
            if (resource.TryGetProperty("resources", out var childResources))
            {
                ExtractResources(childResources, descriptors, paramDefaults);
            }
        }
    }

    /// <summary>
    /// Resolves ARM template expressions (e.g. "[parameters('location')]", "[resourceGroup().location]")
    /// to a concrete value. Falls back to DefaultRegion if the expression cannot be resolved.
    /// </summary>
    private static string ResolveArmExpression(string value, Dictionary<string, string> paramDefaults)
    {
        if (string.IsNullOrEmpty(value))
            return DefaultRegion;

        // Not an ARM expression — use as-is
        if (!value.StartsWith('[') || !value.EndsWith(']'))
            return value;

        // Try to resolve [parameters('paramName')] by looking up default values
        var paramMatch = Regex.Match(value, @"^\[parameters\('(\w+)'\)\]$");
        if (paramMatch.Success)
        {
            var paramName = paramMatch.Groups[1].Value;
            if (paramDefaults.TryGetValue(paramName, out var defaultVal) && !IsArmExpression(defaultVal))
            {
                return defaultVal;
            }
        }

        // Unresolvable expression — fall back to default region
        return DefaultRegion;
    }

    private static bool IsArmExpression(string value) =>
        value.StartsWith('[') && value.EndsWith(']');

    private static ResourceDescriptor BuildDescriptor(
        JsonElement resource, string type, string apiVersion, string name, string location,
        Dictionary<string, string> paramDefaults)
    {
        var sku = new Dictionary<string, JsonElement>();
        if (resource.TryGetProperty("sku", out var skuElement) && skuElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in skuElement.EnumerateObject())
            {
                sku[prop.Name] = ResolveJsonElement(prop.Value, paramDefaults);
            }
        }

        var properties = new Dictionary<string, JsonElement>();
        if (resource.TryGetProperty("properties", out var propsElement) && propsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in propsElement.EnumerateObject())
            {
                properties[prop.Name] = ResolveJsonElement(prop.Value, paramDefaults);
            }
        }

        // Also store the full "kind" if present
        if (resource.TryGetProperty("kind", out var kindElement))
        {
            properties["_kind"] = ResolveJsonElement(kindElement, paramDefaults);
        }

        return new ResourceDescriptor(type, apiVersion, name, location, sku, properties);
    }

    /// <summary>
    /// Recursively resolves ARM parameter expressions in a JsonElement tree.
    /// </summary>
    private static JsonElement ResolveJsonElement(JsonElement element, Dictionary<string, string> paramDefaults)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var strVal = element.GetString() ?? string.Empty;
                if (IsArmExpression(strVal))
                {
                    var resolved = ResolveArmStringExpression(strVal, paramDefaults);
                    if (resolved != null)
                        return JsonDocument.Parse($"\"{EscapeJsonString(resolved)}\"").RootElement.Clone();
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
                            ResolveJsonElement(item, paramDefaults).WriteTo(writer);
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
                            ResolveJsonElement(prop.Value, paramDefaults).WriteTo(writer2);
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
    private static string? ResolveArmStringExpression(string value, Dictionary<string, string> paramDefaults)
    {
        var paramMatch = Regex.Match(value, @"^\[parameters\('(\w+)'\)\]$");
        if (paramMatch.Success)
        {
            var paramName = paramMatch.Groups[1].Value;
            if (paramDefaults.TryGetValue(paramName, out var defaultVal) && !IsArmExpression(defaultVal))
            {
                return defaultVal;
            }
        }
        return null;
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
