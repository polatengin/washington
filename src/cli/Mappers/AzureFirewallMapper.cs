using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class AzureFirewallMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/azureFirewalls";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;
        var skuTier = GetSkuTier(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Firewall",
                ArmRegionName: region,
                SkuName: skuTier,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuTier = GetSkuTier(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Deployment") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Azure Firewall {skuTier} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"Azure Firewall {skuTier} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs + data");
    }

    private static string GetSkuTier(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("tier", out var tier) && tier.ValueKind == JsonValueKind.String)
            return tier.GetString() ?? "Standard";
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("tier", out var tierProp) &&
            tierProp.ValueKind == JsonValueKind.String)
            return tierProp.GetString() ?? "Standard";
        return "Standard";
    }
}
