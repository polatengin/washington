using Washington.Models;

namespace Washington.Mappers;

public class DigitalTwinsMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DigitalTwins/digitalTwinsInstances";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Digital Twins",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Message") &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Azure Digital Twins — usage-based (messages + queries)");

        // Estimate 100K messages/month
        var messages = 100_000m;
        var monthlyCost = (decimal)price.UnitPrice * (messages / 1_000_000m);
        return new MonthlyCost(monthlyCost, $"Azure Digital Twins ~{messages:N0} messages @ ${price.UnitPrice:F6}/M msgs");
    }
}
