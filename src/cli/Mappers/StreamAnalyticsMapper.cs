using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class StreamAnalyticsMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.StreamAnalytics/streamingjobs";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Stream Analytics",
                ArmRegionName: region,
                MeterName: "Standard Streaming Unit",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var streamingUnits = GetStreamingUnits(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Streaming Unit") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Stream Analytics ({streamingUnits} SU) - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * streamingUnits;
        return new MonthlyCost(monthlyCost, $"Stream Analytics ({streamingUnits} SU) @ ${price.UnitPrice:F4}/SU/hr × {HoursPerMonth} hrs");
    }

    private static int GetStreamingUnits(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("transformation", out var xform) &&
            xform.ValueKind == JsonValueKind.Object &&
            xform.TryGetProperty("properties", out var props) &&
            props.ValueKind == JsonValueKind.Object &&
            props.TryGetProperty("streamingUnits", out var su) &&
            su.ValueKind == JsonValueKind.Number)
            return su.GetInt32();
        return 1;
    }
}
