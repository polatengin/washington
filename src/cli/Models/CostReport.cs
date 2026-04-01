using System.Text.Json.Serialization;

namespace Washington.Models;

public record CostReport(
    [property: JsonPropertyName("lines")] List<ResourceCostLine> Lines,
    [property: JsonPropertyName("grandTotal")] decimal GrandTotal,
    [property: JsonPropertyName("warnings")] List<string> Warnings
);

public record ResourceCostLine(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("resourceName")] string ResourceName,
    [property: JsonPropertyName("pricingDetails")] string PricingDetails,
    [property: JsonPropertyName("hourlyCost")] decimal HourlyCost,
    [property: JsonPropertyName("monthlyCost")] decimal MonthlyCost
);
