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
        const decimal estimatedGbPerMonth = 150m;

        var price = GetIngestionPrice(prices, estimatedGbPerMonth, out var includedGbPerMonth);

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Log Analytics {sku} - base cost + ingestion");

        var billableGbPerMonth = Math.Max(0m, estimatedGbPerMonth - includedGbPerMonth);
        var monthlyCost = (decimal)price.UnitPrice * billableGbPerMonth;

        var usageDetails = includedGbPerMonth > 0
            ? $"~{estimatedGbPerMonth} GB/mo ({billableGbPerMonth} billable after {includedGbPerMonth} GB included)"
            : $"~{estimatedGbPerMonth} GB/mo";

        return new MonthlyCost(monthlyCost, $"Log Analytics {sku} {usageDetails} @ ${price.UnitPrice:F4}/GB + retention");
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

    private static PriceRecord? GetIngestionPrice(
        List<PriceRecord> prices,
        decimal estimatedGbPerMonth,
        out decimal includedGbPerMonth)
    {
        var ingestionPrices = prices
            .Where(IsIngestionPrice)
            .OrderBy(p => p.TierMinimumUnits)
            .ToList();

        includedGbPerMonth = 0m;

        var paidPrice = ingestionPrices
            .Where(p => p.UnitPrice > 0 && (decimal)p.TierMinimumUnits <= estimatedGbPerMonth)
            .OrderByDescending(p => p.TierMinimumUnits)
            .FirstOrDefault()
            ?? ingestionPrices
                .Where(p => p.UnitPrice > 0)
                .OrderBy(p => p.TierMinimumUnits)
                .FirstOrDefault();

        if (paidPrice == null)
            return ingestionPrices.FirstOrDefault();

        if (ingestionPrices.Any(p => p.UnitPrice == 0) && paidPrice.TierMinimumUnits > 0)
        {
            includedGbPerMonth = (decimal)paidPrice.TierMinimumUnits;
        }

        return paidPrice;
    }

    private static bool IsIngestionPrice(PriceRecord price) =>
        price.MeterName != null
        && (price.MeterName.Contains("Data Ingestion", StringComparison.OrdinalIgnoreCase)
            || price.MeterName.Contains("Data Analyzed", StringComparison.OrdinalIgnoreCase)
            || price.MeterName.Contains("Pay-as-you-go", StringComparison.OrdinalIgnoreCase))
        && string.Equals(price.UnitOfMeasure, "1 GB", StringComparison.OrdinalIgnoreCase);
}
