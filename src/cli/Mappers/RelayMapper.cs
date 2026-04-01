using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class RelayMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Relay/namespaces";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Relay",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Listener") &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Azure Relay ({sku}) — usage-based (listeners + messages)");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"Azure Relay ({sku}) @ ${price.UnitPrice:F4}/listener/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }
}
