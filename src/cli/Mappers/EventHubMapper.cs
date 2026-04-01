using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class EventHubMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.EventHub/namespaces";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var tier = GetSkuTier(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Event Hubs",
                ArmRegionName: region,
                SkuName: tier,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var tier = GetSkuTier(resource);
        var capacity = GetCapacity(resource);

        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Throughput Unit") || p.MeterName.Contains("Processing Unit")) &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Event Hubs {tier} {capacity} TU — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * capacity;
        return new MonthlyCost(monthlyCost, $"Event Hubs {tier} {capacity} TU @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuTier(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }

    private static int GetCapacity(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 1;
    }
}
