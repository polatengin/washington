using System.Text.Json;

namespace Washington.Models;

public record ResourceDescriptor(
    string ResourceType,
    string ApiVersion,
    string Name,
    string Location,
    Dictionary<string, JsonElement> Sku,
    Dictionary<string, JsonElement> Properties
);
