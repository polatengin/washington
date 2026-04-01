using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class VirtualNetworkGatewayMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/virtualNetworkGateways";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "VPN Gateway",
                ArmRegionName: region,
                ArmSkuName: skuName,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Gateway") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"VPN Gateway {skuName} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"VPN Gateway {skuName} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("name", out var name) &&
            name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "VpnGw1";
        if (resource.Sku.TryGetValue("name", out var skuName) && skuName.ValueKind == JsonValueKind.String)
            return skuName.GetString() ?? "VpnGw1";
        return "VpnGw1";
    }
}
