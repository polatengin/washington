using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ApplicationGatewayMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/applicationGateways";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var tier = GetTier(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Application Gateway",
                ArmRegionName: region,
                ProductName: $"Application Gateway {tier}",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var tier = GetTier(resource);
        var capacity = GetCapacity(resource);

        // Look for gateway hour pricing
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Gateway") || p.MeterName.Contains("Fixed Cost")) &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Fallback: any hourly price
        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"App Gateway {tier} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * capacity;
        return new MonthlyCost(monthlyCost, $"App Gateway {tier} {capacity} instance(s) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetTier(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("tier", out var tier) && tier.ValueKind == JsonValueKind.String)
            return tier.GetString() ?? "Standard_v2";
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard_v2";
        return "Standard_v2";
    }

    private static int GetCapacity(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 1;
    }
}
