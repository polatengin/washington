using Washington.Models;

namespace Washington.Mappers;

public class DefenderForCloudMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Security/pricings";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Microsoft Defender for Cloud",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Defender for Cloud - usage-based (per resource/node)");

        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"Defender for Cloud @ ${price.UnitPrice:F2}/node/mo");
    }
}
