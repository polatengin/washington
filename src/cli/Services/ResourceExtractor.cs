using System.Text.Json;
using Washington.Models;

namespace Washington.Services;

public class ResourceExtractor
{
    public List<ResourceDescriptor> Extract(string armTemplateJson)
    {
        var descriptors = new List<ResourceDescriptor>();

        using var doc = JsonDocument.Parse(armTemplateJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("resources", out var resources))
        {
            ExtractResources(resources, descriptors);
        }

        return descriptors;
    }

    private void ExtractResources(JsonElement resources, List<ResourceDescriptor> descriptors)
    {
        if (resources.ValueKind != JsonValueKind.Array)
            return;

        foreach (var resource in resources.EnumerateArray())
        {
            var type = resource.GetStringProperty("type");
            var apiVersion = resource.GetStringProperty("apiVersion");
            var name = resource.GetStringProperty("name");
            var location = resource.GetStringProperty("location");

            if (string.IsNullOrEmpty(type))
                continue;

            // Handle copy loops
            if (resource.TryGetProperty("copy", out var copy))
            {
                var count = GetCopyCount(copy);
                for (int i = 0; i < count; i++)
                {
                    var copyName = name.Replace("[copyIndex()]", i.ToString());
                    descriptors.Add(BuildDescriptor(resource, type, apiVersion, copyName, location));
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

                descriptors.Add(BuildDescriptor(resource, type, apiVersion, name, location));
            }

            // Recurse into nested/child resources
            if (resource.TryGetProperty("resources", out var childResources))
            {
                ExtractResources(childResources, descriptors);
            }
        }
    }

    private static ResourceDescriptor BuildDescriptor(
        JsonElement resource, string type, string apiVersion, string name, string location)
    {
        var sku = new Dictionary<string, JsonElement>();
        if (resource.TryGetProperty("sku", out var skuElement) && skuElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in skuElement.EnumerateObject())
            {
                sku[prop.Name] = prop.Value.Clone();
            }
        }

        var properties = new Dictionary<string, JsonElement>();
        if (resource.TryGetProperty("properties", out var propsElement) && propsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in propsElement.EnumerateObject())
            {
                properties[prop.Name] = prop.Value.Clone();
            }
        }

        // Also store the full "kind" if present
        if (resource.TryGetProperty("kind", out var kindElement))
        {
            properties["_kind"] = kindElement.Clone();
        }

        return new ResourceDescriptor(type, apiVersion, name, location, sku, properties);
    }

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
