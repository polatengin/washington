using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class LogAnalyticsWorkspaceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.OperationalInsights/workspaces";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Log Analytics",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Data Ingestion") || p.MeterName.Contains("Pay-as-you-go")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Log Analytics {sku} — base cost + ingestion");

        // Estimate 5 GB/day ingestion = ~150 GB/month
        var estimatedGbPerMonth = 150m;
        var monthlyCost = (decimal)price.UnitPrice * estimatedGbPerMonth;
        return new MonthlyCost(monthlyCost, $"Log Analytics {sku} ~{estimatedGbPerMonth} GB/mo @ ${price.UnitPrice:F4}/GB + retention");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "PerGB2018";
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("name", out var skuName) &&
            skuName.ValueKind == JsonValueKind.String)
            return skuName.GetString() ?? "PerGB2018";
        return "PerGB2018";
    }
}
