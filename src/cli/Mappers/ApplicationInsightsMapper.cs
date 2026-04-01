using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ApplicationInsightsMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Insights/components";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Application Insights",
                ArmRegionName: region,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        // First 5 GB/month free, then per GB
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Data Ingestion") || p.MeterName.Contains("Enterprise Overage")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Application Insights — first 5 GB/mo free + ingestion");

        // Estimate 10 GB/month, 5 GB free
        var billableGb = 5m;
        var monthlyCost = (decimal)price.UnitPrice * billableGb;
        return new MonthlyCost(monthlyCost, $"Application Insights ~10 GB/mo (5 GB billable) @ ${price.UnitPrice:F4}/GB");
    }
}
