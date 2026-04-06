using Washington.Models;

namespace Washington.Mappers;

public class MonitorWorkspaceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Monitor/accounts";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Monitor",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Metrics") &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Azure Monitor Workspace - usage-based (metrics ingestion)");

        // Estimate 1M samples/month
        var estimatedSamples = 1_000_000m;
        var monthlyCost = (decimal)price.UnitPrice * (estimatedSamples / 1_000_000m);
        return new MonthlyCost(monthlyCost, $"Azure Monitor Workspace ~{estimatedSamples:N0} samples @ ${price.UnitPrice:F4}/M samples");
    }
}
